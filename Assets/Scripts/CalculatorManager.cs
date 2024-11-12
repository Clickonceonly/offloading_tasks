using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CalculatorManager : MonoBehaviour
{
    public Text displayText;
    public Canvas Canvas_calculator;

    public SliderManager sliderManager;
    public LotteryManager lotteryManager;


    void Update()
    {
        // Check for physical key presses and call HandleInput with the corresponding character
        if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0))
        {
            PressButton("0");
        }
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            PressButton("1");
        }
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            PressButton("2");
        }
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            PressButton("3");
        }
        if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
        {
            PressButton("4");
        }
        if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
        {
            PressButton("5");
        }
        if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6))
        {
            PressButton("6");
        }
        if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7))
        {
            PressButton("7");
        }
        if (Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9))
        {
            PressButton("9");
        }
        if (Input.GetKeyDown(KeyCode.Period) || Input.GetKeyDown(KeyCode.Comma))
        {
            PressButton(".");
        }
        // Check for '+' key with Shift key
        if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            PressButton("+");
        }
        // "=" key only called if Shift is not pressed
        else if (Input.GetKeyDown(KeyCode.Equals) && !(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return))
        {
            PressEquals();
        }
        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            PressButton("-");
        }
        // Check for '*' key with Shift key
        if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.KeypadMultiply))
        {
            PressButton("*");
        }
        // "8" key only called if Shift is not pressed
        else if (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8))
        {
            PressButton("8");
        }
        if (Input.GetKeyDown(KeyCode.Slash) || Input.GetKeyDown(KeyCode.KeypadDivide))
        {
            PressButton("/");
        }
        // if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return))
        // {
        //     PressEquals();
        // }
        if (Input.GetKeyDown(KeyCode.C))
        {
            PressClear();
        }
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            PressBackspace();
        }
    }




    public void UpdateCalculatorCanvasVisibility()
    {
        Canvas_calculator.gameObject.SetActive(false);
    }

    private string input = "";

    public void PressButton(string buttonText)
    {
        
        // Restrict the length of input to 12 characters
        if (input.Length >= 12)
        {
            return;
        }
        
        // Check if buttonText is a decimal point and the last number in input already contains a decimal point
        if (buttonText == "." && input.Split('+', '-', '*', '/').Last().Contains("."))
        {
            return;
        }

        // Check if the last character is an operator and buttonText is also an operator
        if ("+-*/".Contains(buttonText) && input.Length > 0 && "+-*/".Contains(input[input.Length - 1].ToString()))
        {
            return;
        }

        input += buttonText;
        displayText.text = input;
    }

    public void PressEquals()
    {
        // Check if input is a valid mathematical expression
        if (System.Text.RegularExpressions.Regex.IsMatch(input, @"^[0-9+\-*/.()]*$"))
        {
            if (Evaluate(input).ToString().Length >= 12)
            {
                displayText.text = "Out of bound";
                input = "";
                return;
            }
            else
            {
                displayText.text = Evaluate(input).ToString();
                input = "";
            }
        }
        else
        {
            displayText.text = "Invalid input";
            input = "";
        }

        // Update calculatorUsed in slider task
        sliderManager.calculatorUsed = 1;

        // Update calculatorUsed in lottery task
        lotteryManager.calculatorUsed = 1;
    }

    public void PressClear()
    {
        input = "";
        displayText.text = input;
    }

    public void PressBackspace()
    {
        if (input.Length > 0)
        {
            input = input.Remove(input.Length - 1);
            displayText.text = input;
        }
    }

    private float Evaluate(string expression)
    {
        float result = Convert.ToSingle(new System.Data.DataTable().Compute(expression, null));
        return (float)Math.Round(result, 2);
    }
}