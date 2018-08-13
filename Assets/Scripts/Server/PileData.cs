using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class PileData {

    [SerializeField]
    public List<CardData> Cards = new List<CardData>();

    public void ResetPile()
    {
        Cards.Clear();

        for (int i = 0; i < MockServer.Instance.DB.Cards.Count; i++)
        {
            Cards.Add(Game.Instance.DB.Cards[i].Clone());
            Cards.Add(Game.Instance.DB.Cards[i].Clone());

            if (Game.Instance.DB.Cards[i].Power != 0)
            {
                Cards.Add(Game.Instance.DB.Cards[i].Clone());
                Cards.Add(Game.Instance.DB.Cards[i].Clone());
            }
        }

        Shuffle();
    }

    public void Shuffle()
    {
        System.Random rand = new System.Random();
        Cards = Cards.OrderBy(c => rand.Next()).ToList();
    }

    public CardData PullCard()
    {
        if (Cards.Count == 0)
        {
            return null;
        }

        CardData data = Cards[Cards.Count - 1];
        Cards.RemoveAt(Cards.Count - 1);

        data.LastLocation = this;

        return data;
    }
}
