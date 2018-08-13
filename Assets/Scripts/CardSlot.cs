using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardSlot : MonoBehaviour
{
    [SerializeField]
    public CardUI Card
    {
        get
        {
            return this.card;
        }
        set
        {
            this.card = value;

            if (value != null)
            {
                this.card.ParentSlot = this;
                this.card.ParentPlayer = this.ParentPlayer;
            }
        }
    }
    CardUI card;

    [SerializeField]
    public PlayerModule ParentPlayer;

    internal void Dispose()
    {
        if(Card == null)
        {
            return;
        }

        Card.gameObject.SetActive(false);
        Card = null;
    }

    public void GenerateCard(CardData Data)
    {
        GameObject cardObj = ResourcesLoader.Instance.GetRecycledObject("Card");

        Card = cardObj.GetComponent<CardUI>();

        Card.SetInfo(Data);

        cardObj.transform.position = transform.position;
        cardObj.transform.rotation = transform.rotation;


        
    }
}
