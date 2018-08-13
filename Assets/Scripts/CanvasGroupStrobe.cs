using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class CanvasGroupStrobe : MonoBehaviour {

    [SerializeField]
    CanvasGroup m_CG;

    [SerializeField]
    bool Toggle = false;

    [SerializeField]
    float Speed = 1f;

    [SerializeField]
    float minValue = 0f;

    [SerializeField]
    float maxValue = 1f;


    private void Awake()
    {
        if(m_CG == null)
        {
            m_CG = GetComponent<CanvasGroup>();
        }
    }

    private void Update()
    {
        m_CG.alpha += Toggle ? Speed * Time.deltaTime : -Speed * Time.deltaTime;

        if(!Toggle && m_CG.alpha <= minValue)
        {
            Toggle = true;
        }
        else if (Toggle && m_CG.alpha >= maxValue)
        {
            Toggle = false;
        }
    }
}
