using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using Dhive;





public class DataLoader : MonoBehaviour
{
    IntroductionManager introductionManager;
    public static Session session;
    public static Trial trial;
    public DhiveReader _reader;
    private DhiveSender sender;
    public static string ExperimentId = "b2065d56-b1ad-4eb8-a7f6-92de44700152";
    
    // ** One fixed Session ** //
    public static string SessionId = "4e2d1ba0-1ca6-4195-98fb-06b1fbe1db1f";
    
    public static string LotteryTaskId = "1542b7c5-28fd-4098-bfab-501d14d06432";
    public static string BudgetTaskId = "666a67f2-6975-44c7-bc59-4c5ca10bfa48";
    public static string CardTaskId = "12856217-741b-4bbc-a0ff-8dbae1f424fe";
    public static string NumeracyTaskId = "57d2590b-db7a-42c4-98ad-e3ca157b2390";

    private Experiment experimentData;
    private Experiment experiment;

    public string lotteryTaskID;
    public string cardTaskID;
    
    // intialise variables for lottery task
    public string i_prob_1;
    public string i_outcome_1;
    public string i_prob_2;
    public string i_outcome_2;
    public string i_dominated;

    // intialise variables for card task
    public string i_win_prob;
    public string i_red;
    public string i_black_1;
    public string i_black_2;
    public string i_fair_prize;
    public string i_deviation;
    public string i_prize;
    public string i_impliedRiskPreference;

    // public string parameters (for storing CSV in string format)
    public string LotteryCsvContent;
    public string CardCsvContent;

    async void Start()
    {


        await GetExperimentID();
        lotteryTaskID = GetTaskID(experimentData, "Binary Lotteries");
        cardTaskID = GetTaskID(experimentData, "Card Betting");

        // ** Code to show iterating over Experiment.tasks ** //
        foreach (var task in experimentData.Tasks)
        {
            Debug.Log("Task ID: " + task.Id);

            // ** Get parameters for Binary Lotteries task ** //
            if (task.Name == "Binary Lotteries")
            {
                Debug.Log("Binary Lotteries Task Found");
                var prob_1 = task.Parameters.GetIntListParameter("i_prob_1");
                var outcome_1 = task.Parameters.GetIntListParameter("i_outcome_1");
                var prob_2 = task.Parameters.GetIntListParameter("i_prob_2");
                var outcome_2 = task.Parameters.GetIntListParameter("i_outcome_2");
                var dominated = task.Parameters.GetStringListParameter("i_dominated");
                var riskPref = task.Parameters.GetDoubleListParameter("i_impliedRiskPreference");

                foreach (var i in prob_1)
                {
                    i_prob_1 += $"{i},";
                }

                foreach (var i in outcome_1)
                {
                    i_outcome_1 += $"{i},";
                }

                foreach (var i in prob_2)
                {
                    i_prob_2 += $"{i},";
                }

                foreach (var i in outcome_2)
                {
                    i_outcome_2 += $"{i},";
                }

                foreach (var i in dominated)
                {
                    i_dominated += $"{i},";
                }

                foreach (var i in riskPref)
                {
                    i_impliedRiskPreference += $"{i},";
                }

                // Remove the trailing comma
                i_prob_1 = i_prob_1.TrimEnd(',');
                i_outcome_1 = i_outcome_1.TrimEnd(',');
                i_prob_2 = i_prob_2.TrimEnd(',');
                i_outcome_2 = i_outcome_2.TrimEnd(',');
                i_dominated = i_dominated.TrimEnd(',');
                i_impliedRiskPreference = i_impliedRiskPreference.TrimEnd(',');


                // Create a StringBuilder to store the CSV content
                StringBuilder LotteryCsv = new StringBuilder();
                // Append header row
                LotteryCsv.AppendLine("i_prob_1,i_outcome_1,i_prob_2,i_outcome_2,i_dominated,i_impliedRiskPreference");
                // Determine the maximum length of the lists
                int maxLength = Math.Max(Math.Max(Math.Max(Math.Max(Math.Max(prob_1.Count, outcome_1.Count), prob_2.Count), outcome_2.Count), dominated.Count), riskPref.Count);
                // Append each row
                for (int i = 0; i < maxLength; i++)
                {
                    string prob1 = i < prob_1.Count ? prob_1[i].ToString() : "";
                    string out1 = i < outcome_1.Count ? outcome_1[i].ToString() : "";
                    string prob2 = i < prob_2.Count ? prob_2[i].ToString() : "";
                    string out2 = i < outcome_2.Count ? outcome_2[i].ToString() : "";
                    string dom = i < dominated.Count ? dominated[i].ToString() : "";
                    string riskpref = i < riskPref.Count ? riskPref[i].ToString() : "";

                    LotteryCsv.AppendLine($"{prob1},{out1},{prob2},{out2},{dom},{riskpref}");
                }

                // Convert StringBuilder content to string (csv content as string)
                LotteryCsvContent = LotteryCsv.ToString();

                Debug.Log("CSV Content: " + LotteryCsvContent);

                // Write to a CSV file (checking csv content from local file)
                File.WriteAllText("csv_test_lottery.csv", LotteryCsv.ToString());

            }


            // ** Get parameters for Card Betting task ** //
            if (task.Name == "Card Betting")
            {
                Debug.Log("Card Betting Task Found");
                var win_prob = task.Parameters.GetDoubleListParameter("i_win_prob");
                var red = task.Parameters.GetIntListParameter("i_red");
                var black_1 = task.Parameters.GetIntListParameter("i_black_1");
                var black_2 = task.Parameters.GetIntListParameter("i_black_2");
                var fair_prize = task.Parameters.GetDoubleListParameter("i_fair_prize");
                var deviation = task.Parameters.GetDoubleListParameter("i_deviation");
                var prize = task.Parameters.GetDoubleListParameter("i_prize");

                Debug.Log("itemsInList:" + win_prob.Count);

                foreach (var i in win_prob)
                {
                    i_win_prob += $"{i},";
                }

                foreach (var i in red)
                {
                    i_red += $"{i},";
                }

                foreach (var i in black_1)
                {
                    i_black_1 += $"{i},";
                }

                foreach (var i in black_2)
                {
                    i_black_2 += $"{i},";
                }

                foreach (var i in fair_prize)
                {
                    i_fair_prize += $"{i},";
                }

                foreach (var i in deviation)
                {
                    i_deviation += $"{i},";
                }

                foreach (var i in prize)
                {
                    i_prize += $"{i},";
                }

                // Remove the trailing comma
                i_win_prob = i_win_prob.TrimEnd(',');
                i_red = i_red.TrimEnd(',');
                i_black_1 = i_black_1.TrimEnd(',');
                i_black_2 = i_black_2.TrimEnd(',');
                i_fair_prize = i_fair_prize.TrimEnd(',');
                i_deviation = i_deviation.TrimEnd(',');
                i_prize = i_prize.TrimEnd(',');


                // Create a StringBuilder to store the CSV content
                StringBuilder CardCsv = new StringBuilder();
                // Append header row
                CardCsv.AppendLine("i_win_prob,i_red,i_black_1,i_black_2,i_fair_prize,i_deviation,i_prize");
                // Determine the maximum length of the lists
                int maxLength = Math.Max(Math.Max(Math.Max(Math.Max(Math.Max(Math.Max(win_prob.Count, red.Count), black_1.Count), black_2.Count), fair_prize.Count), deviation.Count), prize.Count);
                // Append each row
                for (int i = 0; i < maxLength; i++)
                {
                    string winProb = i < win_prob.Count ? win_prob[i].ToString() : "";
                    string Red = i < red.Count ? red[i].ToString() : "";
                    string Black1 = i < black_1.Count ? black_1[i].ToString() : "";
                    string Black2 = i < black_2.Count ? black_2[i].ToString() : "";
                    string fairPrize = i < fair_prize.Count ? fair_prize[i].ToString() : "";
                    string Deviation = i < deviation.Count ? deviation[i].ToString() : "";
                    string Prize = i < prize.Count ? prize[i].ToString() : "";

                    CardCsv.AppendLine($"{winProb},{Red},{Black1},{Black2},{fairPrize},{Deviation},{Prize}");
                }

                // Convert StringBuilder content to string (csv content as string)
                CardCsvContent = CardCsv.ToString();

                Debug.Log("CardCSV Content: " + CardCsvContent);

                // Write to a CSV file (checking csv content from local file)
                File.WriteAllText("csv_test_card.csv", CardCsv.ToString());

            }
        }
    }


    public async Task GetExperimentID()
    {
        _reader = new DhiveReader(ExperimentId);

        experimentData = await _reader.GetExperiment();  

        if (experimentData == null)
        {
            // Text_experiment.text = "Experiment cannot be fetched from the server.";
            Debug.LogError("[DHive Reader] Experiment cannot be fetched");
            return;
        }
        
        // Text_experiment.text = "Experiment found on the server: " + experimentData.Id + experimentData.Name;
        Debug.Log("[DHive Reader] Experiment Found. " + experimentData.Id);

    }


    private static string GetTaskID(Experiment experiment, string taskName)
    {
        foreach (var task in experiment.Tasks.Where(Task => Task.Name.Equals(taskName)))
        {
            return task.Id + task.Name;             // task.Name for debugging purposes, remove later

        }

        return "";
    }


}


    