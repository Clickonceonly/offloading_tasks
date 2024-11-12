using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Dhive;

public class CardManager_new : MonoBehaviour
{
    private DhiveSender sender;
    private DhiveReader _reader;
    public DataLoader dataLoader;
    public IntroductionManager introductionManager;

    public GameObject Canvas0;
    public TextMeshProUGUI Text0;
    public TextMeshProUGUI Text_OT;
    public Button ButtonStart;

    public GameObject Canvas1;
    public TextMeshProUGUI Text1;
    public TextMeshProUGUI Text_round;
    public GameObject Clubs_pos1;
    public GameObject Clubs_pos2;
    public GameObject Clubs_pos3;
    public GameObject Diamonds_pos3;
    public Button ButtonPlay;
    public Button ButtonNoPlay;
    public int PlayStatus;

    public GameObject Canvas2;
    public TextMeshProUGUI Text2;

    public GameObject CanvasOT;

    private List<int> clubDeck;

    private DataTable cardData;

    public int sessionID;
    private string pidInput;
    private int red_fixed;
    private int black_1_fixed;
    private int black_2_fixed;
    public float prize_fixed;
    public float outcome;
    
    public float fair_prize;
    private float win_prob;
    private float deviation;

    public int round;
    private float black2ShowTime;
    private float buttonClickTime;
    public float elapsedTime;


    private bool isCoroutineRunning = false;
    private bool isCoroutine2Running = false;
    public List<OutputParameter> CardOutput;


    // for outcome realization
    public string GetCardOutput()
    {
        return $"round {round - 1} chosen, outcome {outcome}";
    }
    //////////////////////////////////////




    async void Start()
    {
        // ** Test initialization at Start ** //
        // Initialize sender
        sender = DhiveSender.GetInstance(DataLoader.trial.Id);

        // ** Define _reader locally ** //
        _reader = new DhiveReader(DataLoader.ExperimentId);

        // Store the sessionID from IntroductionManager
        sessionID = introductionManager.sessionID;
        pidInput = introductionManager.pidInput;


        // Set the visibility of the OFFLOADING instructions
        Text_OT.gameObject.SetActive(sessionID == 1);



        Canvas0.SetActive(true);
        Canvas1.SetActive(false);
        Canvas2.SetActive(false);
        CanvasOT.SetActive(false);


        // Set ButtonStart to inactive for 5 seconds to ensure task is loaded
        ButtonStart.interactable = false;
        StartCoroutine(WaitForFiveSeconds());

        // ** New Data Loading Functions (From DataLoader) ** //
        CSV_StringToTable(dataLoader.CardCsvContent);
        Debug.Log("cardData.Rows.Count: " + cardData.Rows.Count);

        // Initial round
        round = 1;
        ButtonStart.interactable = true;
        ButtonStart.onClick.AddListener(OnButtonStartClick);

        // ** Debug: Check if _reader or DataLoader.session is null
        if (_reader == null || DataLoader.SessionId == null)
        {
            Debug.LogError("_reader or DataLoader.session is null.");
            return;
        }


    }


    IEnumerator WaitForFiveSeconds()
    {
        yield return new WaitForSeconds(5);
        // Code to execute after 5 seconds

        ButtonStart.interactable = true;
    }



    // ** Websocket here ** //
    void Update()
    {

    #if !UNITY_WEBGL || UNITY_EDITOR
        sender?.DispatchMessageQueue();
    #endif


        Debug.Log(black2ShowTime);

    }




    void OnButtonStartClick()
    {
        StartRound();
        Canvas0.SetActive(false);
    }


    void SetChildrenActive(GameObject parent, bool state)
    {
        foreach (Transform child in parent.transform)
        {
            child.gameObject.SetActive(state);
        }
    }


    // ** Draw a row from cardData ** //
    Tuple<int, int, int, float> DrawRow()
    {
        // Draw the first row from cardData
        DataRow row = cardData.Rows[round - 1];

        // Store the data you need from the row in variables
        int red = int.Parse(row["i_red"].ToString());
        int black_1 = int.Parse(row["i_black_1"].ToString());
        int black_2 = int.Parse(row["i_black_2"].ToString());
        float prize = float.Parse(row["i_prize"].ToString());
        fair_prize = float.Parse(row["i_fair_prize"].ToString());
        win_prob = float.Parse(row["i_win_prob"].ToString());
        deviation = float.Parse(row["i_deviation"].ToString());


        // Return the stored data instead of the row
        return new Tuple<int, int, int, float>(red, black_1, black_2, prize);
    }


    void StartRound()
    {
        Canvas1.SetActive(true);


        // Set black2ShowTime to the current time when black2 is shown (+ 0.5s after round starts)
        black2ShowTime = Time.time + 0.5f;

        // Only enable OFFLOADING canvas when sessionID = 1
        if (sessionID == 1)                                                                     
        {
            CanvasOT.SetActive(true);
        }

        // for outcome realization
        if (round - 1 == introductionManager.z)
        {
            introductionManager.cardOutput = GetCardOutput();
        }
        //////////////////////////////////////



        // Reset all Red and Green objects in CanvasOT to invisible
        foreach (Transform child in CanvasOT.transform)
        {
            Transform redChild = child.Find("Red" + child.name.Replace("club", ""));
            Transform greenChild = child.Find("Green" + child.name.Replace("club", ""));

            if (redChild != null) redChild.gameObject.SetActive(false);
            if (greenChild != null) greenChild.gameObject.SetActive(false);
        }


        // Reset the decks
        clubDeck = Enumerable.Range(1, 10).ToList();
        // Enable ButtonPlay and ButtonNoPlay
        ButtonPlay.interactable = true;
        ButtonNoPlay.interactable = true;
        // Make all cards invisible at the start of the game
        SetChildrenActive(Clubs_pos1, false);
        SetChildrenActive(Clubs_pos2, false);
        SetChildrenActive(Clubs_pos3, false);
        SetChildrenActive(Diamonds_pos3, false);
        // Draw a row from cardData and get the tuple
        Tuple<int, int, int, float> drawnRow = DrawRow();

        // Get red, black_1, and black_2, prize from the tuple
        red_fixed = drawnRow.Item1;
        black_1_fixed = drawnRow.Item2;
        black_2_fixed = drawnRow.Item3;
        prize_fixed = drawnRow.Item4;

        // Display the cost to play and the prize
        Text1.text = $"Cost to play: 10\nPrize: {prize_fixed:F1}";
        Text_round.text = $"Round {round} of 44";

        // Set the corresponding child objects to visible
        SetChildActiveDiamonds(Diamonds_pos3, red_fixed, true);
        SetChildActiveClubs(Clubs_pos1, black_1_fixed, true);

        if (!isCoroutine2Running)
        {
            // Start a coroutine to wait and then set the second club card
            StartCoroutine(SetSecondClubCard(Clubs_pos2, black_2_fixed));
        }

        // Remove black_1 and black_2 from clubDeck
        clubDeck.Remove(black_1_fixed);
        clubDeck.Remove(black_2_fixed);
        // Add listeners to the 'Play' and 'NoPlay' buttons
        ButtonPlay.onClick.AddListener(OnButtonPlayClick);
        ButtonNoPlay.onClick.AddListener(OnButtonNoPlayClick);

        Debug.Log("Round #" + round.ToString());
    }


    // ** New Data Loading Functions (From DataLoader) ** //
    void CSV_StringToTable(string path)
    {
        // Create a new DataTable
        cardData = new DataTable();

        // // Read the file as one string
        // string data = File.ReadAllText(path);

        Debug.Log("CSV_StringToTable: " + path);
        // Split the string into lines
        string[] lines = path.Split('\n');

        // Add columns to the DataTable
        string[] headers = lines[0].Split(',');
        foreach (string header in headers)
        {
            cardData.Columns.Add(header);
        }

        // Add rows to the DataTable
        for (int i = 1; i < lines.Length; i++)
        {
            string[] fields = lines[i].Split(',');
            cardData.Rows.Add(fields);
        }

    }



    // locating Club cards from 1st, 2nd, and 3rd draws
    void SetChildActiveClubs(GameObject parent, int cardNumber, bool state)
    {
        // Construct the cardName string
        string cardName = "cardClubs_" + (cardNumber == 1 ? "A" : cardNumber.ToString());

        // Find the child object
        Transform child = parent.transform.Find(cardName);

        // If the child object is found, set its active state
        if (child != null)
        {
            child.gameObject.SetActive(state);
        }
        else
        {
            Debug.Log("Child object not found: " + cardName);
        }
    }

    // locating Diamond card from 1st draw
    void SetChildActiveDiamonds(GameObject parent, int cardNumber, bool state)
    {

        // Construct the cardName string
        string cardName = "cardDiamonds_" + (cardNumber == 1 ? "A" : cardNumber.ToString());

        // Find the child object
        Transform child = parent.transform.Find(cardName);

        Debug.Log("child = " + child);

        // If the child object is found, set its active state
        if (child != null)
        {
            child.gameObject.SetActive(state);
        }
        else
        {
            Debug.Log("Child object not found: " + cardName);
        }
    }


    int DrawCard(List<int> deck)
    {

        // Draw a card from the deck
        int index = UnityEngine.Random.Range(0, deck.Count);
        int card = deck[index];

        // Remove the card from the deck
        deck.RemoveAt(index);

        return card;
    }


    void OnButtonPlayClick()
    {
        if (!isCoroutineRunning)
        {
            PlayStatus = 1;

            // Get the current time
            buttonClickTime = Time.time;

            // Draw a third club card
            int thirdCard = DrawCard(clubDeck);

            Debug.Log("red_fixed: " + red_fixed.ToString());
            Debug.Log("third club: " + thirdCard.ToString());

            // Set the corresponding child object in Clubs_pos3 to visible
            SetChildActiveClubs(Clubs_pos3, thirdCard, true);

            // Highlight the button
            ColorBlock colors = ButtonPlay.colors;
            colors.highlightedColor = Color.blue;
            ButtonPlay.colors = colors;


            // Check if the player wins or loses
            if (thirdCard > red_fixed)
            {
                Text1.text = $"You win {prize_fixed:F1}";
                outcome = prize_fixed - 10;
            }
            else
            {
                Text1.text = "You lose";
                outcome = -10;
            }

            // Disable ButtonPlay and ButtonNoPlay
            ButtonPlay.interactable = false;
            ButtonNoPlay.interactable = false;

            // Move to Canvas2 after 1 second
            StartCoroutine(MoveToCanvas2());
        }
    }


    void OnButtonNoPlayClick()
    {
        if (!isCoroutineRunning)
        {
            PlayStatus = 0;

            // Get the current time
            buttonClickTime = Time.time;

            // Draw a third club card
            int thirdCard = DrawCard(clubDeck);

            // Set the corresponding child object in Clubs_pos3 to visible
            SetChildActiveClubs(Clubs_pos3, thirdCard, true);

            // Highlight the button
            ColorBlock colors = ButtonNoPlay.colors;
            colors.highlightedColor = Color.red;
            ButtonNoPlay.colors = colors;

            // Set Text1 to "Round Skipped"
            Text1.text = "Round Skipped";
            outcome = 0;

            // Disable ButtonPlay and ButtonNoPlay
            ButtonPlay.interactable = false;
            ButtonNoPlay.interactable = false;

            // Move to Canvas2 after 1 second
            StartCoroutine(MoveToCanvas2());
        }
    }



    // 0.5 seconds after the first club card is drawn, the second club card is drawn
    IEnumerator SetSecondClubCard(GameObject parent, int cardNumber)
    {
        isCoroutine2Running = true;

        // Wait for 0.5 seconds
        yield return new WaitForSeconds(0.5f);

        // Set the second club card visible
        SetChildActiveClubs(parent, cardNumber, true);
        

        // Highlight the cards in CanvasOT
        HighlightCardsInCanvasOT();

        isCoroutine2Running = false;


    }



    IEnumerator MoveToCanvas2()
    {

        isCoroutineRunning = true;

        // Calculate the elapsed time between when black2 is shown and when a button is clicked
        elapsedTime = buttonClickTime - black2ShowTime;

        OutputToDhive();

        // Wait for 1 second
        yield return new WaitForSeconds(1f);

        CanvasOT.SetActive(false);

        if (round < 44)
        {
            // Set Text2 to the same text as Text1 and "Please wait for the next round"
            Text2.text = Text1.text + $"\nPlease wait for round {round + 1} of 44";
        }

        if (round == 44)
        {
            Text2.text = "Well done! You have completed all rounds!";
        }


        // Set Canvas1 to invisible and Canvas2 to visible
        Canvas1.SetActive(false);
        Canvas2.SetActive(true);

        // Wait for 2 seconds
        yield return new WaitForSeconds(2.5f);

        // Set Canvas2 to invisible
        Canvas2.SetActive(false);


        // Reset Text1
        Text1.text = "Choose Play or Don't Play";


        // ** Local Output: Write the output to a file //
        WriteOutputToFile(sessionID, pidInput, elapsedTime, win_prob, red_fixed, black_1_fixed, black_2_fixed, deviation, fair_prize, prize_fixed);

        // Prepare for the next round
        round++;
        StartRound();
        buttonClickTime = Time.time;

        isCoroutineRunning = false;
    }



    async void OutputToDhive()
    {
    
        CardOutput = new List<OutputParameter>
        {
            // new ("SessionID", sessionID),
            new ("ParticipantID", pidInput),
            new ("Time", elapsedTime),
            new ("WinProb", win_prob),
            new ("Red", red_fixed),
            new ("Black1", black_1_fixed),
            new ("Black2", black_2_fixed),
            new ("Deviation", deviation),
            new ("FairPrize", fair_prize),
            new ("Prize", prize_fixed),
            new ("Played?",PlayStatus),
            new ("CardCurretTime", System.DateTime.Now.ToString("HH:mm:ss"))
        };

        string CardTrialTask = await sender.NewTrialTask(DataLoader.CardTaskId);
        await sender.SaveParameter(CardTrialTask, CardOutput);
    }







    // ** Local Output: Write the output to a file ** //
    void WriteOutputToFile(int sessionID, string pidInput, float time, float win_prob, int red, int black1, int black2, float deviation, float fair_prize, float prize)
    {
        string path = $"card_output_{sessionID}_{pidInput}.txt";
        string output = $"{sessionID}, {pidInput}, {elapsedTime:F2}, {win_prob}, {red_fixed}, {black_1_fixed}, {black_2_fixed}, {deviation}, {fair_prize}, {prize_fixed:F2}\n";

        System.IO.File.AppendAllText(path, output);

        // Debug log for the output
        Debug.Log("Output: " + output);
    }



    void HighlightCardsInCanvasOT()
    {
        foreach (Transform child in CanvasOT.transform)
        {
            // Only process child objects that represent cards
            if (child.name.EndsWith("club"))
            {
                // Get the value of the card from its name
                string cardName = child.name.Replace("club", "");
                int cardValue;

                if (!int.TryParse(cardName, out cardValue))
                {
                    Debug.LogError("Invalid card name: " + child.name);
                    continue;
                }

                // Get the SpriteRenderer component of the card
                SpriteRenderer spriteRenderer = child.GetComponent<SpriteRenderer>();

                // Get the Red and Green child objects
                Transform redChild = child.Find("Red" + cardValue);
                Transform greenChild = child.Find("Green" + cardValue);

                if (cardValue == black_1_fixed || cardValue == black_2_fixed)
                {
                    // Set the card to half opaque
                    spriteRenderer.color = new Color(1f, 1f, 1f, 0.5f);

                    // Set both Red and Green objects to invisible
                    if (redChild != null) redChild.gameObject.SetActive(false);
                    if (greenChild != null) greenChild.gameObject.SetActive(false);
                }
                else
                {
                    // Set the card to fully opaque
                    spriteRenderer.color = new Color(1f, 1f, 1f, 1f);

                    if (cardValue > red_fixed)
                    {
                        // Set the Green object to visible and the Red object to invisible
                        if (greenChild != null) greenChild.gameObject.SetActive(true);
                        if (redChild != null) redChild.gameObject.SetActive(false);
                    }
                    else
                    {
                        // Set the Red object to visible and the Green object to invisible
                        if (redChild != null) redChild.gameObject.SetActive(true);
                        if (greenChild != null) greenChild.gameObject.SetActive(false);
                    }
                }
            }
        }

    }





}
