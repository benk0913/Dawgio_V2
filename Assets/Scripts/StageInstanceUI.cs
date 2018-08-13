using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StageInstanceUI : MonoBehaviour {

    [SerializeField]
    Text m_txtTitle;

    [SerializeField]
    Text m_txtDescription;

    [SerializeField]
    GameObject LockObject;

    [SerializeField]
    CanvasGroupStrobe m_Strober;

    [SerializeField]
    Button m_Button;

    [SerializeField]
    public int StageNumber = 0;

    public StageInstanceState State = StageInstanceState.Locked;

    public void SetState(StageInstanceState state)
    {
        this.State = state;

        RefreshUI();
    }

    void RefreshUI()
    {
        if (State == StageInstanceState.Locked)
        {
            LockObject.gameObject.SetActive(true);
            m_txtTitle.color = new Color(Color.white.r, Color.white.g, Color.white.b, 0.5f);

            if (m_txtDescription != null)
            {
                m_txtDescription.color = new Color(Color.white.r, Color.white.g, Color.white.b, 0.5f);
            }

            m_Button.interactable = false;
        }
        else
        {
            LockObject.gameObject.SetActive(false);
            m_txtTitle.color = new Color(Color.white.r, Color.white.g, Color.white.b, 1f);

            if (m_txtDescription != null)
            {
                m_txtDescription.color = new Color(Color.white.r, Color.white.g, Color.white.b, 1f);
            }

            m_Button.interactable = true;
        }

        m_Strober.enabled = (State == StageInstanceState.Selected);
    }

    public enum StageInstanceState
    {
        Locked,
        Available,
        Selected
    }
}
