# Offloading Tasks

Offloading Tasks built with Unity (C#).

## Contains 3 offloading tasks:
1. Binary-Lottery Task
2. Budget-Choice Task
3. Card-Betting (Belief-Updating) Task
4. Additional Numeracy Task at the end

## Instructions

1. To start the game, input a (generic) participant ID and press <kbd>Start</kbd> to proceed
2. While in a trial, use <kbd>left click</kbd> to select a choice (the calculator tool can be accessed via the keypad/keyboard or the mouse)
3. To proceed to the next trial, press <kbd>Next</kbd> or simlar buttons depending on the task
4. (Optional) The buttons <kbd>skip lottery</kbd> and <kbd>skip card</kbd> are for testing purposes. They can be deactivated either in the Unity editor, or in the script.




## ⚠️ Important

**Inputs are read from D-Hive**

**Outputs are stored both locally and to D-Hive**

1. The On/Off switch for the offloading condition is set in `Assets/Scripts/IntroductionManager.cs` at line 85
2. Buttons <kbd>skip lottery</kbd> and <kbd>skip card</kbd> can be deactivated in `Assets/Scripts/IntroductionManager.cs` at line 13
