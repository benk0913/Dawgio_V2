using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardSetterEntity : MonoBehaviour {

    [SerializeField]
    CardData data;

    [SerializeField]
    CardUI Card;

    private void Awake()
    {
        Card.SetInfo(data);    
    }
}
