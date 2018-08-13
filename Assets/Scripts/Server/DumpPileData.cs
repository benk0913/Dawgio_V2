using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DumpPileData {


    public List<CardData> Cards = new List<CardData>();

    public void ResetPile()
    {
        Cards.Clear();
    }

    public void PushCard(CardData data)
    {
        Cards.Add(data);
    }

    public CardData PullCard()
    {
        if(Cards.Count == 0)
        {
            return null;
        }

        CardData card = Cards[Cards.Count - 1];
        Cards.RemoveAt(Cards.Count - 1);

        card.LastLocation = this;

        return card;
    }

    public CardData GetCurrentTopCard()
    {
        if (Cards.Count == 0)
        {
            return null;
        }

        return Cards[Cards.Count - 1];
    }
}
