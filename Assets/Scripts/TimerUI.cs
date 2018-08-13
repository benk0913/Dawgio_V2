using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimerUI : MonoBehaviour {

    [SerializeField]
    Text m_txtLabel;

    public bool isTimerActive;

    public void SetTime(int Seconds)
    {
        if (TimerInstance != null)
        {
            StopCoroutine(TimerInstance);
        }

        TimerInstance = StartCoroutine(TimerRoutine(Seconds));
    }

    public void TimerActive(bool state)
    {
        isTimerActive = state;
    }

    Coroutine TimerInstance;
    IEnumerator TimerRoutine(int seconds)
    {
        if(!isTimerActive)
        {
            m_txtLabel.text = "∞";
            TimerInstance = null;
            yield break;
        }

        while(seconds > 0)
        {
            m_txtLabel.text = seconds.ToString();
            yield return new WaitForSeconds(1);
            seconds--;
        }

        TimerInstance = null;
    }
}
