using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialStrobe : MonoBehaviour {

    [SerializeField]
    MeshRenderer m_Renderer;
    
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
        if (m_Renderer == null)
        {
            m_Renderer = GetComponent<MeshRenderer>();
        }
    }

    private void Update()
    {
        m_Renderer.material.color =new Color(
               m_Renderer.material.color.r
            ,  m_Renderer.material.color.g
            ,  m_Renderer.material.color.b
            , (m_Renderer.material.color.a + (Toggle ? Speed * Time.deltaTime : -Speed * Time.deltaTime)));

        if (!Toggle && m_Renderer.material.color.a <= minValue)
        {
            Toggle = true;
        }
        else if (Toggle && m_Renderer.material.color.a >= maxValue)
        {
            Toggle = false;
        }
    }
}
