using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EndScoreUI : MonoBehaviour {

    [SerializeField]
    CanvasGroup m_CG;

    [SerializeField]
    Text m_txtName;

    [SerializeField]
    Text m_txtPower;

    public void Show(string Name, int Power)
    {
        this.gameObject.SetActive(true);

        StopAllCoroutines();

        m_txtName.text  = Name;
        m_txtPower.text = Power.ToString();

        StartCoroutine(ShowRoutine());
    }

    public void Hide()
    {
        StopAllCoroutines();

        StartCoroutine(HideRoutine());
    }

    IEnumerator ShowRoutine()
    {
        m_CG.alpha = 0f;

        while(m_CG.alpha < 1f)
        {
            m_CG.alpha += 1f * Time.deltaTime;

            yield return 0;
        }
    }

    IEnumerator HideRoutine()
    {
        m_CG.alpha = 1f;

        while (m_CG.alpha > 0f)
        {
            m_CG.alpha -= 1f * Time.deltaTime;

            yield return 0;
        }

        this.gameObject.SetActive(false);
    }
}
