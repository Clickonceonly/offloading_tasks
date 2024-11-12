using System;
using System.Collections;
using System.Collections.Generic;
// using Cysharp.Threading.Tasks;  // Uses UniTask package for async/await (Delay)
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Data;
using System.Linq;

using Dhive;

public class LotteryManager : MonoBehaviour
{

    public int LotteryDhive_count = 0;

    private DhiveSender sender;
    private DhiveReader _reader;
    public DataLoader dataLoader;

    public IntroductionManager introductionManager;

    public CalculatorManager calculatorManager;
    public Canvas Canvas_calculator;

    public Canvas Canvas0;
    public TextMeshProUGUI Text0;
    public TextMeshProUGUI Text_OT;
    public Button Button0Next;

    public Canvas Canvas1;
    public TextMeshProUGUI Text1;
    public Button Button1L;
    public Button Button1R;
    public Button Button1Next;

    public Canvas Canvas_break;  // Canvas for break time between trials to ensure data saving

    public DataRow row;
    
    public int currentLottery = 0;
    private DataTable lotteryData;

    public int sessionID;

    private string pidInput;

    public int calculatorUsed = 0;
    private float startTime;

    // time for a given trial
    public float time;

    private string chosenProb;
    private string chosenOutcome;

    public List<OutputParameter> LotteryOutput;




    // Input from DataLoader takes CSV in string format
    void CSV_StringToTable(string path)
    {
        // Create a new DataTable
        lotteryData = new DataTable();

        Debug.Log("CSV_StringToTable: " + path);
        // Split the string into lines
        string[] lines = path.Split('\n');

        // Add columns to the DataTable
        string[] headers = lines[0].Split(',');
        foreach (string header in headers)
        {
            lotteryData.Columns.Add(header);
        }

        // Add rows to the DataTable
        for (int i = 1; i < lines.Length; i++)
        {
            string[] fields = lines[i].Split(',');
            lotteryData.Rows.Add(fields);
        }

    }

    

    // Start is called before the first frame update
    async void Start()
    {

        // ** Define _reader locally ** //
        _reader = new DhiveReader(DataLoader.ExperimentId);

        // Store the sessionID (the offloading condition) from IntroductionManager
        sessionID = introductionManager.sessionID;
        pidInput = introductionManager.pidInput;                                                                 

        // Set the visibility of the OFFLOADING instructions
        Text_OT.gameObject.SetActive(sessionID == 1);


        // Canvas 0 setup
        Button0Next.onClick.AddListener(GoToCanvas1);
        
        // ** New load data method ** //
        CSV_StringToTable(dataLoader.LotteryCsvContent);

        // Set Button0 to inactive for 5 seconds to ensure task is loaded
        Button0Next.interactable = false;
        StartCoroutine(WaitForFiveSeconds());
        
        // Set up the initial state
        UpdateState();

        Button1Next.onClick.AddListener(NextLottery);

        // Add listeners to the 'onClick' events of Button1L and Button1R
        Button1L.onClick.AddListener(() => SelectButton(Button1L));
        Button1R.onClick.AddListener(() => SelectButton(Button1R));

        // Disable the 'Next' button initially
        Button1Next.interactable = false;

        // ** Create single trial at the start of the game ** //

        // ** Debug: Check if _reader or DataLoader.session is null
        if (_reader == null || DataLoader.SessionId == null)
        {
            Debug.LogError("_reader or DataLoader.session is null.");
            return;
        }

        // ** Initialize trial at the start of the lottery task ** //
        // ** Create trial ** //
        var parameters = new List<OutputParameter>
        {
            
        };
        DataLoader.trial = await _reader.CreateTrial(DataLoader.SessionId, pidInput, parameters);


        // Initialize "sender" (Locate the trial) and connect to Dhive
        sender = DhiveSender.GetInstance(DataLoader.trial.Id);
        await sender.Connect();

        // ** Debug: Check trial.id
        Debug.Log("DataLoader.Trial created: " + DataLoader.trial.Id);
        
        // ** Debug: Check if DataLoader.trial is null
        if (DataLoader.trial == null)
        {
            Debug.LogError("DataLoader.trial is null.");
            return;
        }

    }


     IEnumerator WaitForFiveSeconds()
    {
        yield return new WaitForSeconds(5);
        Button0Next.interactable = true;
    }



    // ** Websocket here ** //
    void Update()
    {

    #if !UNITY_WEBGL || UNITY_EDITOR
        sender?.DispatchMessageQueue();
    #endif

    }


    public void UpdateCalculatorCanvasVisibility()
    {
        // Set the calculator canvas to active if sessionID is 1, otherwise set it to inactive
        Canvas_calculator.gameObject.SetActive(sessionID == 1);
    }

    void GoToCanvas1()
    {
        Canvas0.gameObject.SetActive(false);
        Canvas1.gameObject.SetActive(true);

        // Update the visibility of the calculator canvas
        UpdateCalculatorCanvasVisibility();

        // Record the start time
        startTime = Time.time;
        
        Debug.Log(startTime);
    }


    void UpdateState()
    {

        // ** Debug: Check if lotteryData or lotteryData.Rows is null
        if (lotteryData == null || lotteryData.Rows == null)
        {
            Debug.LogError("lotteryData or lotteryData.Rows is null.");
            return;
        }

        
        // Get the current lottery data
        row = lotteryData.Rows[currentLottery];

        // Update the text and button objects
        Text1.text = $"Question {currentLottery + 1} of 26\nChoose one of the two options";
        Button1L.GetComponentInChildren<TextMeshProUGUI>().text = $"With {row["i_prob_1"]} % probability you get {row["i_outcome_1"]}, otherwise nothing";
        Button1R.GetComponentInChildren<TextMeshProUGUI>().text = $"With {row["i_prob_2"]} % probability you get {row["i_outcome_2"]}, otherwise nothing";

        // Reset button colors
        Button1L.GetComponent<Image>().color = Color.white;
        Button1R.GetComponent<Image>().color = Color.white;

    }

    void SelectButton(Button button)
    {
        // Reset the color of all buttons
        Button1L.GetComponent<Image>().color = Color.white;
        Button1R.GetComponent<Image>().color = Color.white;

        // Highlight the selected button
        button.GetComponent<Image>().color = Color.green;

        // enable the 'Next' button
        Button1Next.interactable = true;

        // Store the chosen probability and outcome
        chosenProb = button.GetComponentInChildren<TextMeshProUGUI>().text.Split(' ')[1];
        chosenOutcome = button.GetComponentInChildren<TextMeshProUGUI>().text.Split(' ')[6];
    }




    // for outcome realization
    public string GetLotteryOutput()
    {
        return $"{currentLottery} chosen, prob {chosenProb}, outcome {chosenOutcome}";
    }
    //////////////////////////////////////









    private async void NextLottery()
    {  
        // disable the 'Next' button (RUN THIS FIRST)
        Button1Next.interactable = false;

        // Calculate the elapsed time
        time = Time.time - startTime;

        // Write output for the current lottery
        row = lotteryData.Rows[currentLottery];

        // Debug log for current lotteries
        Debug.Log(row["i_prob_1"].ToString() + " " + row["i_outcome_1"].ToString() + " " + row["i_prob_2"].ToString() + " " + row["i_outcome_2"].ToString());

        // Debug to see if OnNextButtonClick()
        Debug.Log("OnNextButtonClick() has been ran!!!");


        // ** Debug Null ** //
        if (lotteryData == null || lotteryData.Rows == null || lotteryData.Rows.Count <= currentLottery)
        {
            Debug.LogError("lotteryData or its rows are null or currentLottery index is out of range.");
            return;
        }

        if (introductionManager == null)
        {
            Debug.LogError("introductionManager is null.");
            return;
        }

        if (DataLoader.trial == null)
        {
            Debug.LogError("DataLoader.trial is null.");
            return;
        }

        if (string.IsNullOrEmpty(DataLoader.trial.Id))
        {
            Debug.LogError("DataLoader.trial.Id is null or empty.");
            return;
        }

        if (string.IsNullOrEmpty(DataLoader.LotteryTaskId))
        {
            Debug.LogError("DataLoader.LotteryTaskId is null or empty.");
            return;
        }

        // ** Local output: Write output to local file ** //
        WriteOutputToFile(sessionID, pidInput, row["i_prob_1"].ToString(), row["i_outcome_1"].ToString(), row["i_prob_2"].ToString(), row["i_outcome_2"].ToString(), row["i_dominated"].ToString(), calculatorUsed, time, chosenProb, chosenOutcome);
    
        ToBreakTime();
        Debug.Log($"currentLottery: {currentLottery}, x: {introductionManager.x}");

    }

    // ** Output to Dhive ** //
    async void ToBreakTime()
    {
        Canvas1.gameObject.SetActive(false);
        Canvas_calculator.gameObject.SetActive(false);
        Canvas_break.gameObject.SetActive(true);

        await OutputToDhive();

        StartCoroutine(WaitForTwoSeconds());  // Wait for 2s to ensure data saving

    }



    IEnumerator WaitForTwoSeconds()
    {
        yield return new WaitForSeconds(2);
        // Code to execute after 2 seconds

        // Increment the current lottery
        currentLottery++;

        // Update the state
        UpdateState();

        
        // Reset calculatorUsed and startTime
        calculatorUsed = 0;
        startTime = Time.time;

        // for outcome realization
        if (currentLottery == introductionManager.x)
        {
            introductionManager.lotteryOutput = GetLotteryOutput();
        }

        Canvas_break.gameObject.SetActive(false);
        Canvas1.gameObject.SetActive(true);
        UpdateCalculatorCanvasVisibility();
    }



    // ** Output to Dhive ** //
    async Task OutputToDhive()
    {
        LotteryOutput = new List<OutputParameter>
        {
            // new ("SessionID", sessionID),
            new ("ParticipantID", pidInput),
            new ("Prob1", row["i_prob_1"].ToString()),
            new ("Outcome1", row["i_outcome_1"].ToString()),
            new ("Prob2", row["i_prob_2"].ToString()),
            new ("Outcome2", row["i_outcome_2"].ToString()),
            new ("Dominated", row["i_dominated"].ToString()),
            new ("CalculatorUsed", calculatorUsed),
            new ("Time", time),
            new ("ChosenProb", chosenProb),
            new ("ChosenOutcome", chosenOutcome),
            new ("RiskPreference", row["i_impliedRiskPreference"].ToString()),
            new ("LotteryCurrentTime", System.DateTime.Now.ToString("HH:mm:ss"))
        };


        
        string LotteryTrialTask = await sender.NewTrialTask(DataLoader.LotteryTaskId);
        await sender.SaveParameter(LotteryTrialTask, LotteryOutput);
        
        // count how many times OutputToDhive() is called
        LotteryDhive_count += 1;
    }


    // ** Local Output: Write output to local file ** //
    void WriteOutputToFile(int sessionID, string pidInput, string prob1, string outcome1, string prob2, string outcome2, string dominated, int calculatorUsed, float time, string chosen_prob, string chosen_outcome)
    {
        string path = $"lottery_output_{sessionID}_{pidInput}.txt";
        string output = $"{sessionID}, {pidInput}, {prob1}, {outcome1}, {prob2}, {outcome2}, {dominated}, {calculatorUsed}, {time:F2}, {chosen_prob}, {chosen_outcome}\n";

        System.IO.File.AppendAllText(path, output);

        // Debug log for the output
        Debug.Log("Output: " + output);
    }

}