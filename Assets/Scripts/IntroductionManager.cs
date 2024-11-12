using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

using Dhive;

public class IntroductionManager : MonoBehaviour
{

    // *** For testing purposes:
    public GameObject Canvas_skip;
    public Button Button_skip_lottery;
    public Button Button_skip_card;
    



    private DhiveSender sender;
    private DhiveReader _reader;
    public DataLoader dataLoader;


    public GameObject Introduction;
    public GameObject Canvas_intro;
    public GameObject Canvas_numeracy;
    public GameObject Canvas_out;

    public Button Button_intro;
    public InputField InputField_pid;
    public string pidInput;
    public TextMeshProUGUI Text_login_warning;
    public InputField InputField_q1;
    public InputField InputField_q2;
    public InputField InputField_q3;
    public Button Button_numeracy;
    public InputField InputField_matchid;
    public string matchid;
    public TextMeshProUGUI Text_quit_warning;
    public Button Button_out;

    public LotteryManager lotteryManager;
    public SliderManager sliderManager;
    public CardManager_new cardManager;


    // for outcome realization
    public string lotteryOutput;
    public string sliderOutput;
    public string cardOutput;
    public int x = 0;
    public int y = 0;
    public int z = 0;
    public TextMeshProUGUI Text_final_payoffs;

    public float totalPayoff;
    public float lotteryPayoff;
    public float sliderPayoff;
    public float cardPayoff;
    private bool payoffRandomizer = true;
    private bool Displayed = false;
    public string Q1_result;
    public string Q2_result;
    public string Q3_result;
    public string Q1_incorrect;
    public string Q2_incorrect;
    public string Q3_incorrect;
    public List<OutputParameter> NumeracyOutput;
    //////////////////////////////////////


    // sessionID (offloading condition: 1 for offloading) is intialised in this script, then broadcasted to other 3 game scripts upon Start()
    public int sessionID;



    async void Start()
    {
        // *** Disabling testing (for skipping between tasks):
        Canvas_skip.SetActive(true);

        // ** IMPORTANT: Switch between 1 and 0 for offloading and non-offloading conditions ** //
        sessionID = 1;

        // ** Define _reader locally ** //
        _reader = new DhiveReader(DataLoader.ExperimentId);

        // for outcome realization
        // Generate random numbers
        x = UnityEngine.Random.Range(1,27);
        y = UnityEngine.Random.Range(1, 4);
        z = UnityEngine.Random.Range(1, 45);
        //////////////////////////////////////


        // Set all GameObjects to inactive except Introduction and Canvas_intro
        Introduction.SetActive(true);
        Canvas_intro.SetActive(true);
        Canvas_numeracy.SetActive(false);
        Canvas_out.SetActive(false);
        Text_login_warning.gameObject.SetActive(false);
        Text_quit_warning.gameObject.SetActive(false);

        // Add listeners to the buttons
        Button_intro.onClick.AddListener(StartLotteryManager);
        sliderManager.finishButton.onClick.AddListener(FinishSliderTask);
        Button_out.onClick.AddListener(QuitExperiment);
        Button_numeracy.onClick.AddListener(FinishNumeracyTask);


        // Set the login button to inactive for 10 seconds to ensure experiment is loaded
        Button_intro.interactable = false;
        StartCoroutine(EnableButtonAfterSeconds(Button_intro, 10f));         // wait 10 seconds to ensure the experiment is loaded                      
        // Button_intro.interactable = true;




        // **** For testing purposes:
        Button_skip_lottery.onClick.AddListener(StartSliderManager);
        Button_skip_card.onClick.AddListener(FinishCardTask);
    }

    IEnumerator EnableButtonAfterSeconds(Button button, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        button.interactable = true;
    }



    async void StartLotteryManager()
    {
        // ** Debug: Check if the participant ID is not entered properly
        if (InputField_pid == null || Text_login_warning == null || Canvas_intro == null || lotteryManager == null || _reader == null)
        {
            Debug.LogError("One or more required objects are null.");
            return;
        }
        
        
        // Check if the participant ID is not entered properly //
        if (InputField_pid.text == "Please enter with your Participant ID here..." || InputField_pid.text == "")
        {
            Text_login_warning.gameObject.SetActive(true);
            return;
        }

        else
        {
            // Set Canvas_intro to inactive and start LotteryManager
            Canvas_intro.SetActive(false);
            lotteryManager.gameObject.SetActive(true);

            // store the participant ID
            pidInput = InputField_pid.text;


            // Create a new session for the participant based on pId

            // DataLoader.session = await _reader.GetSession(null, pidInput);                                  // This line will populate a NEW session
            DataLoader.session = await _reader.GetSession(DataLoader.SessionId, pidInput);                     // This line uses an existing session as input



            // ** Debug: Check if DataLoader.session is null
            if (DataLoader.session == null)
            {
                Debug.LogError("DataLoader.session is null.");
                return;
            }
            // ** Debug: SessionID ** //
            Debug.Log($"pid: {pidInput}");
            Debug.Log($"Session ID: {DataLoader.session.Id}");
                    
        }

    }

    

    void Update()
    {

        // ** Websocket here ** //
    #if !UNITY_WEBGL || UNITY_EDITOR
        sender?.DispatchMessageQueue();
    #endif

    
        

      
        // Check if currentLottery + 1 == 27 in LotteryManager
        if (lotteryManager.currentLottery + 1 == 27 && lotteryManager.Button1Next.onClick.GetPersistentEventCount() > 0)
        {
            // Set LotteryManager to inactive and Canvas_intro_slider to active
            lotteryManager.gameObject.SetActive(false);
            StartSliderManager();
        }


        // Check if round == 45 in CardManager
        if (cardManager.round == 45 && Displayed == false)
        {
            // Set CardManager to inactive and Canvas_out to active
            cardManager.gameObject.SetActive(false);
            Canvas_numeracy.SetActive(true);

            if (payoffRandomizer == true)
            {
                // Randomize payoffs
                RandomizePayoffs();
                payoffRandomizer = false;
            }
            
            // ** This line does not display the randomized payoffs ** //
            Text_final_payoffs.text = "Your Completion Code is: CFSCQF01\n\nIn order to receive your bonus, you need to: \n(1) Save this code externally.\n(2) Input this code below and click \"Send\". Wait for the server to save your responses.\n(3) Input this code in the Prolific web page after finishing";

            Debug.Log($"Your outcomes from:\nLottery: {lotteryOutput:F1}\nSlider: {sliderOutput:F1}\nCard: {cardOutput:F1}");

            // Making sure everything here just appears once, since they are inside Update()
            Displayed = true;

            
            
        }

    }


    async void FinishNumeracyTask()
    {

        // compute the results for the numeracy questions
        if (InputField_q1.text == "500")
        {
            Q1_result = "1";
            Debug.Log("Q1 answered correctly");
        }
        else
        {
            Q1_result = "0";
            Debug.Log("Q1 answered wrong");
            Q1_incorrect = InputField_q1.text;
        }

        if (InputField_q2.text == "10")
        {
            Q2_result = "1";
            Debug.Log("Q2 answered correctly");
        }
        else
        {
            Q2_result = "0";
            Debug.Log("Q2 answered wrong");
            Q2_incorrect = InputField_q2.text;
        }

        if (InputField_q3.text == "0.1")
        {
            Q3_result = "1";
            Debug.Log("Q3 answered correctly");
        }
        else  
        {
            Q3_result = "0";
            Debug.Log("Q3 answered wrong");
            Q3_incorrect = InputField_q3.text;
        }


        // ** Old: Save numeracy results to a separate file
        WriteOutputToFile(Q1_result, Q2_result, Q3_result);

        // Initialize "sender" (Locate this trial) and connect to Dhive
        sender = DhiveSender.GetInstance(DataLoader.trial.Id);


        // Proceed to Canvas_out
        Canvas_numeracy.SetActive(false);
        Canvas_out.SetActive(true);
    }



    // ** Local Output: Store the Numeracy results locally ** //
    void WriteOutputToFile(string Q1_result, string Q2_result, string Q3_result)
    {
        string path = $"NumeracyResults_{pidInput}.txt";
        string output = $"{pidInput}, {Q1_result}, {Q2_result}, {Q3_result}\n";

        System.IO.File.AppendAllText(path, output);
    }



    // ** Local Output: Store the payoff locally ** //
    void WriteOutputToFile(float totalPayoff, float lotteryPayoff, float sliderPayoff, float cardPayoff)       
    {
        string path = $"Realized_Payoffs_{pidInput}.txt";
        string output = $"{pidInput}, {matchid}, {totalPayoff:F1}, {lotteryPayoff:F1}, {sliderPayoff:F1}, {cardPayoff:F1}\n";

        System.IO.File.AppendAllText(path, output);
    }

    
    void RandomizePayoffs()
    {
        // Parse the chosen probability and outcome from lotteryOutput
        string[] lotteryParts = lotteryOutput.Split(',');
        float lotteryProb = float.Parse(lotteryParts[1].Split(' ')[2]);
        float lotteryOutcome = float.Parse(lotteryParts[2].Split(' ')[2]);

        // Randomize lotteryPayoff
        lotteryPayoff = UnityEngine.Random.value < lotteryProb ? lotteryOutcome : 0;

        // Parse the chosen probabilities and outcomes from sliderOutput
        string[] sliderParts = sliderOutput.Split(',');
        float sliderProb = float.Parse(sliderParts[1].Split(' ')[2]);
        float sliderOutcome1 = float.Parse(sliderParts[1].Split(' ')[5]);
        float sliderOutcome2 = float.Parse(sliderParts[2].Split(' ')[5]);

        // Randomize sliderPayoff
        sliderPayoff = UnityEngine.Random.value < sliderProb ? sliderOutcome1 : sliderOutcome2;

        // Parse the outcome from cardOutput
        string[] cardParts = cardOutput.Split(',');
        float cardOutcome = float.Parse(cardParts[1].Split(' ')[2]);

        // Set cardPayoff
        cardPayoff = cardOutcome;
    }



    async void StartSliderManager()
    {
        // Set Canvas_intro_slider to inactive and start SliderManager
        sliderManager.gameObject.SetActive(true);

        lotteryManager.gameObject.SetActive(false);
    }

    void FinishSliderTask()
    {
        // Set SliderManager to inactive
        sliderManager.gameObject.SetActive(false);

        cardManager.gameObject.SetActive(true);
    }

    async void QuitExperiment()
    {
        // Check if the Match ID is not entered properly //
        if (InputField_matchid.text != "CFSCQF01")
        {
            Text_quit_warning.gameObject.SetActive(true);
            return;
        }

        if (InputField_matchid.text == "CFSCQF01")
        {
            // store the Match ID
            matchid = InputField_matchid.text;

            totalPayoff = lotteryPayoff + sliderPayoff + cardPayoff;


            // ** Old: Store the payoff locally ** //
            WriteOutputToFile(totalPayoff, lotteryPayoff, sliderPayoff, cardPayoff);


            // "Quit" in WebGL
            ShowQuitMessage();

            // Disable "Quit" button and InputField to prevent repeated sends
            Button_out.interactable = false;
            InputField_matchid.interactable = false;
            

        }

    }

    async void ShowQuitMessage()
    {  
        // ** Output to Dhive: store numeracy output as list ** //
        OutputToDhive();
                
        Debug.Log("Please close the browser tab to quit the application.");
    }


    async void OutputToDhive()
    {

        NumeracyOutput = new List<OutputParameter>
            {
                new ("ParticipantID", pidInput),
                new ("MatchID", matchid),
                new ("Q1", Q1_result),
                new ("Q1_incorrect", Q1_incorrect),
                new ("Q2", Q2_result),
                new ("Q2_incorrect", Q2_incorrect),
                new ("Q3", Q3_result),
                new ("Q3_incorrect", Q3_incorrect),
                new ("TotalPay", totalPayoff),
                new ("LotteryPayoff", lotteryPayoff),
                new ("SliderPayoff", sliderPayoff),
                new ("CardPayoff", cardPayoff)
            };


        
        string NumeracyTrialTask = await sender.NewTrialTask(DataLoader.NumeracyTaskId);
        await sender.SaveParameter(NumeracyTrialTask, NumeracyOutput);

        Text_final_payoffs.text = "Your responses have been saved to server.\n\n You may now close the browser tab.";
    }




    // *** For testing purposes:
    void FinishCardTask()
    {
        cardManager.round = 43;

    }

}