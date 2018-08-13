using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EntityHoverable : MonoBehaviour {

    [SerializeField]
    UnityEvent OnEnter;

    [SerializeField]
    UnityEvent OnExit;


    public void OnHover()
    {
        OnEnter.Invoke();
    }

    public void OnUnhover()
    {
        OnExit.Invoke();
    }
}
