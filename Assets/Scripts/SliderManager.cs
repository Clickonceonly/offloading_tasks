using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

using Dhive;

public class SliderManager : MonoBehaviour
{

    private DhiveSender sender;
    private DhiveReader _reader;
    public DataLoader dataLoader;
    

    public IntroductionManager introductionManager;

    public CalculatorManager calculatorManager;
    public Canvas Canvas_calculator;

    public int sessionID;

    public Text debugText3;

    public Canvas canvas0;
    public Text BeginText0;
    public Text Text_OT;
    public string pidInput;
    public Button nextButton0;

    public Canvas canvas1;
    public Slider probabilitySlider;
    public Text probabilityText;
    public Button nextButton1;

    public Canvas canvas2;
    public Slider combinedPrizeSlider;
    public Text combinedPrizeText;
    public Button nextButton2;

    public Canvas canvas3;
    public Slider canvas3Slider;
    public Text canvas3Text;
    public Button nextButton3;

    public Canvas canvas4;
    public Text canvas4Text;
    public Button finishButton;

    public Canvas Canvas_break;

    private const float K = 4.0f;
    private const float minProbability = 0.05f;
    private const float maxProbability = 0.95f;
    private const float minQ = 0.01f;
    private const float minZ = 3.0f;

    private float fixedProbability;
    private float fixedQ;
    private float sliderQ2;
    private float sliderZ2;

    private float sliderP3;
    private float sliderZ3;

    private float startTime;
    public float time;
    public int calculatorUsed;
    public List<OutputParameter> BudgetOutput;

    public int sliderNumber;

    // for outcome realization
    private float chosenProb;
    private float chosenOutcome1;
    private float chosenOutcome2;
    /////////////////////////////




    async void Start()
    {

        // Initialize sender
        sender = DhiveSender.GetInstance(DataLoader.trial.Id);
        

        // ** Define _reader locally ** //
        _reader = new DhiveReader(DataLoader.ExperimentId);

        // Store the sessionID from IntroductionManager
        sessionID = introductionManager.sessionID;
        pidInput = introductionManager.pidInput;

        // Reset calculatorUsed
        calculatorUsed = 0;

        // Set the visibility of the OFFLOADING instructions
        Text_OT.gameObject.SetActive(sessionID == 1);


        // Update the visibility of the calculator canvas
        Canvas_calculator.gameObject.SetActive(false);

        // Step 0 setup
        nextButton0.onClick.AddListener(GoToStep1);

        // Set Button0 to inactive for 5 seconds to ensure task is loaded
        nextButton0.interactable = false;
        StartCoroutine(WaitForFiveSeconds());
        // nextButton0.interactable = true;


        // Step 1 setup
        probabilitySlider.minValue = minProbability;
        probabilitySlider.maxValue = maxProbability;

        probabilitySlider.onValueChanged.AddListener(UpdateProbabilityText);
        nextButton1.onClick.AddListener(GoToStep2);

        probabilitySlider.value = (probabilitySlider.minValue + probabilitySlider.maxValue) / 2;
        // probabilitySlider.value = 0;

        UpdateProbabilityText(probabilitySlider.value);

        // Step 2 setup
        combinedPrizeSlider.minValue = minQ;
        combinedPrizeSlider.maxValue = (K - minZ) / fixedProbability;
        // combinedPrizeSlider.value = minQ;
        combinedPrizeSlider.value = (combinedPrizeSlider.minValue + combinedPrizeSlider.maxValue) / 2;


        combinedPrizeSlider.onValueChanged.AddListener(UpdateCombinedPrizeText);
        nextButton2.onClick.AddListener(GoToStep3);

        // Step 3 setup
        canvas3Slider.minValue = minProbability;
        canvas3Slider.maxValue = maxProbability;

        canvas3Slider.value = (canvas3Slider.minValue + canvas3Slider.maxValue) / 2;

        canvas3Slider.onValueChanged.AddListener(UpdateCanvas3Text);
        nextButton3.onClick.AddListener(GoToEndStep);

        // Step 4 setup
        finishButton.onClick.AddListener(FinishSlider);

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

        nextButton0.interactable = true;
    }







    // ** Websocket here ** //
    void Update()
    {

    #if !UNITY_WEBGL || UNITY_EDITOR
        sender?.DispatchMessageQueue();
    #endif

    }


    // for outcome realization
    public string GetSliderOutput()
    {
        return $"slider {introductionManager.y} chosen, prob {chosenProb} to get {chosenOutcome1}, prob {1 - chosenProb} to get {chosenOutcome2}";
    }
    //////////////////////////////////////



    void GoToStep1()
    {
        canvas0.gameObject.SetActive(false);
        canvas1.gameObject.SetActive(true);
        Canvas_calculator.gameObject.SetActive(sessionID == 1);

        // Record the start time
        startTime = Time.time;

        sliderNumber = 1;

    }

    void UpdateProbabilityText(float probability)
    {
        // Round probability to the nearest increment of 0.01
        probability = Mathf.Round(probability * 100) / 100;

        // Extract probability and prize (q) from slider 1
        fixedProbability = probability;
        fixedQ = K / fixedProbability;

        probabilityText.text = $"With {probability:P0} probability you get {K / probability:F2}, otherwise you get nothing.";
    }

    async void GoToStep2()
    {
        canvas1.gameObject.SetActive(false);
        Canvas_calculator.gameObject.SetActive(sessionID == 1);
        // Canvas_calculator.gameObject.SetActive(false);


        float maxZ = K - (fixedProbability * minQ);

        UpdateCombinedPrizeText(combinedPrizeSlider.value);

        // ** Local Output: Write output for slider1
        time = Time.time - startTime;
        WriteOutputToFile(1, calculatorUsed, time, K, fixedProbability, fixedQ, 0);


        await ToBreakTime();
    }

    void UpdateCombinedPrizeText(float sliderValue)
    {
    // Round sliderValue to the nearest increment of 0.01
        sliderValue = Mathf.Round(sliderValue * 100) / 100;

    // Calculate q based on the normalized value of the slider
        float normalizedValue = (sliderValue - combinedPrizeSlider.minValue) / (combinedPrizeSlider.maxValue - combinedPrizeSlider.minValue);
        float maxQ = (K - minZ) / fixedProbability;
        float q = minQ + normalizedValue * (maxQ - minQ);
        sliderQ2 = q;

        // Calculate z based on q
        float z = K - q * fixedProbability;
        sliderZ2 = z;

        combinedPrizeText.text = $"With {fixedProbability:P0} probability you get {(q + z):F2}, with {(1 - fixedProbability):P2} probability you get {z:F2}.";

    }

    async void GoToStep3()
    {
        canvas2.gameObject.SetActive(false);
        // Canvas_calculator.gameObject.SetActive(false);

        // Ensure the slider's initial value is within its min and max range
        canvas3Slider.value = Mathf.Clamp(canvas3Slider.value, canvas3Slider.minValue, canvas3Slider.maxValue);

        UpdateCanvas3Text(canvas3Slider.value);

        // ** Local Output: Write output for slider2
        time = Time.time - startTime;
        WriteOutputToFile(2, calculatorUsed, time, K, fixedProbability, sliderQ2, sliderZ2);


        await ToBreakTime();

    }

    void UpdateCanvas3Text(float sliderValue)
    {
        // Round sliderValue to the nearest increment of 0.01
        sliderValue = Mathf.Round(sliderValue * 100) / 100;


        float z = K - (fixedQ * sliderValue);

        if (z <= 0)
        {
            z = 0;
            sliderValue = (K - z) / fixedQ;
        }

        sliderP3 = sliderValue;
        sliderZ3 = z;

        canvas3Text.text = $"With {sliderValue:P0} probability you get {fixedQ + z:F2}, with {(1 - sliderValue):P0} probability you get {z:F2}.";

    }

    async void GoToEndStep()
    {
        canvas3.gameObject.SetActive(false);
    

        // ** Local Output: Write output for slider3
        time = Time.time - startTime;
        WriteOutputToFile(3, calculatorUsed, time, K, sliderP3, fixedQ, sliderZ3);


        await ToBreakTime();

        Canvas_calculator.gameObject.SetActive(false);

        
    }




    void FinishSlider()
    {
        canvas4.gameObject.SetActive(false);
    }



    public async Task ToBreakTime()
    {
        canvas1.gameObject.SetActive(false);
        canvas2.gameObject.SetActive(false);
        canvas3.gameObject.SetActive(false);
        Canvas_calculator.gameObject.SetActive(false);
        Canvas_break.gameObject.SetActive(true);

        await OutputToDhive();

        StartCoroutine(WaitForFourSeconds());  // Wait for 4s to ensure data saving

    }


    IEnumerator WaitForFourSeconds()
    {
        yield return new WaitForSeconds(4);
        // Code to execute after 4 seconds

        
        Canvas_break.gameObject.SetActive(false);

        // Record the start time for slider 3
        startTime = Time.time;
        // Reset calculatorUsed
        calculatorUsed = 0;

        // for outcome realization for slider 1
        if (1 == introductionManager.y)
        {
            chosenProb = fixedProbability;
            chosenOutcome1 = fixedQ;
            chosenOutcome2 = 0;
            introductionManager.sliderOutput = GetSliderOutput();
        }
        // for outcome realization for slider 2
        if (2 == introductionManager.y)
        {
            chosenProb = fixedProbability;
            chosenOutcome1 = sliderQ2 + sliderZ2;
            chosenOutcome2 = sliderZ2;
            introductionManager.sliderOutput = GetSliderOutput();
        }
        // for outcome realization for slider 3
        if (3 == introductionManager.y)
        {
            chosenProb = sliderP3;
            chosenOutcome1 = fixedQ + sliderZ3;
            chosenOutcome2 = sliderZ3;
            introductionManager.sliderOutput = GetSliderOutput();
        }

        Canvas_break.gameObject.SetActive(false);

        // set slider 2 canvas to be active after 4s wait from slider 1
        if (sliderNumber == 1)
        {
            canvas2.gameObject.SetActive(true);
            Canvas_calculator.gameObject.SetActive(sessionID == 1);
            sliderNumber = 2;
        }
        // set slider 3 canvas to be active after 4s wait from slider 2
        else if (sliderNumber == 2)
        {
            canvas3.gameObject.SetActive(true);
            Canvas_calculator.gameObject.SetActive(sessionID == 1);
            sliderNumber = 3;
        }
        // set canvas4 to be active after 4s wait from slider 3
        else if (sliderNumber == 3)
        {
            canvas4.gameObject.SetActive(true);
            Canvas_calculator.gameObject.SetActive(false);
            
        }

    }





    async Task OutputToDhive()
    {
        
        if (sliderNumber == 1)
        {
            BudgetOutput = new List<OutputParameter>
            {
                new ("SliderNumber", sliderNumber),
                new ("ParticipantID", pidInput),
                new ("CalculatorUsed", calculatorUsed),
                new ("Time", time),
                new ("EV", K),
                new ("Prob", fixedProbability),
                new ("RiskyPrize", fixedQ),
                new ("SafePrize", 0),
                new ("Slider1CurrentTime", System.DateTime.Now.ToString("HH:mm:ss"))
            };
        }
        

        if (sliderNumber == 2)
        {
            BudgetOutput = new List<OutputParameter>
            {
                new ("SliderNumber", sliderNumber),
                new ("ParticipantID", pidInput),
                new ("CalculatorUsed", calculatorUsed),
                new ("Time", time),
                new ("EV", K),
                new ("Prob", fixedProbability),
                new ("RiskyPrize", sliderQ2),
                new ("SafePrize", sliderZ2),
                new ("Slider2CurrentTime", System.DateTime.Now.ToString("HH:mm:ss"))
            };
        };

        if (sliderNumber == 3)
        {
            BudgetOutput = new List<OutputParameter>
            {
                new ("SliderNumber", sliderNumber),
                new ("ParticipantID", pidInput),
                new ("CalculatorUsed", calculatorUsed),
                new ("Time", time),
                new ("EV", K),
                new ("Prob", sliderP3),
                new ("RiskyPrize", fixedQ),
                new ("SafePrize", sliderZ3),
                new ("Slider3CurrentTime", System.DateTime.Now.ToString("HH:mm:ss"))
            };
        };

        string BudgetTrialTask = await sender.NewTrialTask(DataLoader.BudgetTaskId);
        await sender.SaveParameter(BudgetTrialTask, BudgetOutput);
    }


    void WriteOutputToFile(int sliderID, int calculatorUsed, float time, float K, float p, float q, float z)
    {
        string path = $"slider_output_{sessionID}_{pidInput}.txt";
        string output = $"{sessionID}, {pidInput}, {sliderID}, {calculatorUsed}, {time:F2}, {K:F2}, {p:F2}, {q:F2}, {z:F2}\n";

        System.IO.File.AppendAllText(path, output);
    }


}

