using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInfo
{
    public const int LastStage = 2;

    public static int SelectedStage = 1;


    public static int CurrentStage
    {
        get
        {
            return PlayerPrefs.GetInt("CurrentStage", 0);
        }
        set
        {
            PlayerPrefs.SetInt("CurrentStage", value);
            PlayerPrefs.Save();
        }
    }

    public static int LearningPhase
    {
        get
        {
            return PlayerPrefs.GetInt("LearningPhase", -1);
        }
        set
        {
            PlayerPrefs.SetInt("LearningPhase", value);
            PlayerPrefs.Save();
        }
    }

    public static void ResetProgress()
    {
        PlayerPrefs.SetInt("CurrentStage", 0);
        PlayerPrefs.SetInt("LearningPhase", -1);
        PlayerPrefs.Save();

        SelectedStage = 0;
    }
    
    public static void UnlockProgress()
    {
        PlayerPrefs.SetInt("CurrentStage", 999);
        PlayerPrefs.SetInt("LearningPhase", 999);
        PlayerPrefs.Save();

    }

}
