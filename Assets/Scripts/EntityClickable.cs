using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EntityClickable : MonoBehaviour {

    [SerializeField]
    UnityEvent OnClick;

    public void Click()
    {
        OnClick.Invoke();
    }
}
