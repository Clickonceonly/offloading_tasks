using System.Threading.Tasks;
using System.Collections.Generic;
using System.Timers;
using JetBrains.Annotations;
using NativeWebSocket;
using UnityEngine;

namespace Dhive
{
    public class DhiveSender
    {
        public bool IsConnected { get; private set; }
        public WebSocketState State => _websocket.State;

        [CanBeNull] private static DhiveSender _instance;
        [CanBeNull] private string _newTrialTaskId;
        private readonly WebSocket _websocket;
        private bool _shouldReconnectWs = true;
        private int _sequenceNumber;
        private Timer _staleTimer;
        private static string _trialId;

        private DhiveSender(string trialId)
        {
            IsConnected = false;

#if UNITY_WEBGL && !UNITY_EDITOR
            var subprotocolsOrHeaders = new List<string>
            {
                "actioncable-v1-json", 
                "actioncable-unsupported", 
                $"Dhive-Trial-Id.{trialId}"
            };
#else
            var subprotocolsOrHeaders = new Dictionary<string, string>
            {
                { "Dhive-Trial-Id", trialId },
                { "Origin", $"https://{WebSocketHandler.BaseURL}" }
            };
#endif

            Debug.Log($"[{GetType()} Websocket] Setting up WS connection object for {WebSocketHandler.BaseURL}");

            _websocket = new WebSocket($"wss://{WebSocketHandler.BaseURL}/cable", subprotocolsOrHeaders);
            _websocket.OnOpen += async () =>
            {
                Debug.Log($"[{nameof(DhiveSender)} Websocket] Connection open!");

                var message = WebSocketHandler.SubscribeToTrialChannel();
                Debug.Log($"[{nameof(DhiveSender)} Websocket] Subscribe: {message}");
                await _websocket.SendText(message);

                message = WebSocketHandler.SubscribeToSessionChannel();
                Debug.Log($"[{nameof(DhiveSender)} Websocket] Subscribe: {message}");
                await _websocket.SendText(message);

                _staleTimer = new Timer(1000);
                _staleTimer.Elapsed += HandleStaleMessages;
                _staleTimer.AutoReset = true;
                _staleTimer.Enabled = true;

                IsConnected = true;
            };
            _websocket.OnClose += (e) =>
            {
                Debug.Log($"[{nameof(DhiveSender)} Websocket] Connection closed! Reason: {e}");
                _shouldReconnectWs = e switch
                {
                    WebSocketCloseCode.Abnormal => false,
                    _ => true
                };
                IsConnected = false;
                _staleTimer?.Dispose();
            };
            _websocket.OnError += async (e) =>
            {
                Debug.LogError($"[{nameof(DhiveSender)} Websocket] Error! {e}");
                await Connect(); // Automatic re-connect
            };
            _websocket.OnMessage += (bytes) =>
            {
                // getting the message as a string
                var message = System.Text.Encoding.UTF8.GetString(bytes);
                Debug.Log($"[{nameof(DhiveSender)} Websocket] InMessage: {message}");

                switch (true)
                {
                    case var _ when WebSocketHandler.IsPingMessage(message):
                        goto default;
                    case var parsed when WebSocketHandler.ParseWebSocketMessage(message) != null:
                        var parsedMessage = WebSocketHandler.ParseWebSocketMessage(message);
                        Debug.Log($"[{nameof(DhiveSender)} Websocket] ParsedMessage: {parsedMessage}");
                        switch (parsedMessage)
                        {
                            case null:
                            case var _ when parsedMessage.IsWelcomeMessage:
                                break;
                            case var _ when parsedMessage.IsConfirmSubscription:
                                IsConnected = true;
                                break;
                            case var _ when parsedMessage.IsNewTrialTaskMessage:
                                _newTrialTaskId = parsedMessage.NewTrialTaskId();
                                break;
                            case var _ when parsedMessage.IsBroadcastMessage:
                                Debug.Log($"Broadcast Session Params: {parsedMessage.Message.Broadcast?.Session}");
                                break;
                            case var _ when parsedMessage.IsReceivedMessage:
                                WebSocketHandler.OnMessageReceived(parsedMessage);
                                break;
                            case var _ when parsedMessage.IsErrorMessage:
                                Debug.LogError($"[{nameof(DhiveSender)} Websocket] {parsedMessage.ErrorMessage}");
                                break;
                        }
                        goto default;
                    default:
                        return;
                }
            };
        }

        public static DhiveSender GetInstance(string trialId)
        {
            Debug.Log($"[{nameof(DhiveSender)}] Getting instance for trial {trialId}");
            _trialId ??= trialId;
            return _instance ??= new DhiveSender(_trialId);
        }

        public async Task Connect()
        {
            Debug.Log($"{GetType()} Websocket] State: {_websocket.State} ShouldReconnect? {_shouldReconnectWs}");
            if (_shouldReconnectWs && _websocket.State != WebSocketState.Open)
            {
                Debug.Log($"[{GetType()} Websocket] Running connection task to {WebSocketHandler.BaseURL} ...");
                await _websocket.Connect();
            }
        }

        public async Task SaveParameter(string trialTaskId, List<OutputParameter> parameters)
        {
            var message = WebSocketHandler.CreateSaveTrialTaskParameterRequest(_trialId, trialTaskId, parameters);
            await _websocket.SendText(message);
        }

        public async Task SaveParameter(string trialTaskId, string name, string value)
        {
            var output = new OutputParameter(name, value);
            await SaveParameter(trialTaskId, output);
        }

        public async Task SaveParameter(string trialTaskId, string name, int value)
        {
            var output = new OutputParameter(name, value);
            await SaveParameter(trialTaskId, output);
        }

        public async Task SaveParameter(string trialTaskId, string name, double value)
        {
            var output = new OutputParameter(name, value);
            await SaveParameter(trialTaskId, output);
        }

        public async Task SaveParameter(string trialTaskId, OutputParameter parameter)
        {
            var parameters = new List<OutputParameter> { parameter };
            await SaveParameter(trialTaskId, parameters);
        }
        
        public async Task SaveTrialParameter(string trialId, List<OutputParameter> parameters)
        {
            var message = WebSocketHandler.CreateSaveTrialParameterRequest(trialId, parameters);
            await _websocket.SendText(message);
        }

        public async Task SaveTrialParameter(string trialId, OutputParameter parameter)
        {
            var parameters = new List<OutputParameter> { parameter };
            await SaveTrialParameter(trialId, parameters);
        }

        public async Task SaveSessionParameter(string sessionId, List<OutputParameter> parameters)
        {
            var message = WebSocketHandler.CreateSaveSessionParameterRequest(_trialId, sessionId, parameters);
            await _websocket.SendText(message);
        }

        /// <summary>
        /// Creates a new trial task for the current task within the current trial.
        /// A sequence number is automatically generated if no sequence number is provided.
        /// </summary>
        /// <param name="taskId">Current task's id</param>
        /// <param name="customSequenceNumber">Overrides the internal sequence number counter</param>
        /// <returns>Newly created trial task's id. To be used in saving parameters.</returns>
        public async Task<string> NewTrialTask(string taskId, [CanBeNull] string customSequenceNumber = null)
        {
            _newTrialTaskId = null;

            var selectedSequenceNumber = SelectSequenceNumber(customSequenceNumber);
            var message = WebSocketHandler.CreateNewTrialTaskRequest(taskId, selectedSequenceNumber);
            await _websocket.SendText(message);

            while (_newTrialTaskId == null)
            {
                await Task.Yield();
            }

            return _newTrialTaskId;
        }

        public async Task LockSession()
        {
            var subscribe = WebSocketHandler.SubscribeToSessionChannel();
            await _websocket.SendText(subscribe);
            var lockMessage = WebSocketHandler.LockSession();
            await _websocket.SendText(lockMessage);
        }

        public void DispatchMessageQueue()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            _websocket.DispatchMessageQueue();
#endif
        }

        public async Task Close()
        {
            await _websocket.Close();
        }

        private string SelectSequenceNumber([CanBeNull] string customSequenceNumber)
        {
            return customSequenceNumber ?? (_sequenceNumber++).ToString();
        }

        private async void HandleStaleMessages(System.Object source, ElapsedEventArgs e)
        {
            var staleMessages = WebSocketHandler.GetStaleMessages();

            foreach (var message in staleMessages)
            {
                await _websocket.SendText(message);
            }
        }
    }
}
