using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DumpPileUI : MonoBehaviour {

    [SerializeField]
    public CardUI TopCard;

    [SerializeField]
    public Transform AirPoint;

    List<CardData> Cards = new List<CardData>();

    [SerializeField]
    ParticleSystem BadParticles;

    public void ResetPile()
    {
        if (TopCard != null)
        {
            TopCard.gameObject.SetActive(false);
        }

        Cards.Clear();
    }

    public void PushCard(CardData card)
    {
        Cards.Add(card);

        RefreshPile();
    }

    public GameObject PullCard()
    {
        if (Cards.Count == 0)
        {
            return null;
        }

        CardData RemovedCard = Cards[Cards.Count - 1];
        Cards.RemoveAt(Cards.Count - 1);

        RefreshPile();

        GameObject CardObj = ResourcesLoader.Instance.GetRecycledObject("Card");
        CardObj.transform.position = TopCard.transform.position;
        CardObj.transform.rotation = TopCard.transform.rotation;
        CardObj.GetComponent<CardUI>().SetInfo(RemovedCard);

        RemovedCard.LastLocation = this;

        return CardObj;
    }

    void RefreshPile()
    {
        if (Cards.Count == 0 && TopCard.gameObject.activeInHierarchy)
        {
            TopCard.gameObject.SetActive(false);
            return;
        }
        else
        {
            if (!TopCard.gameObject.activeInHierarchy)
            {
                TopCard.gameObject.SetActive(true);
            }

            TopCard.SetInfo(Cards[Cards.Count - 1]);
        }
    }

    public void Click()
    {
        Game.Instance.InteractWithDumpPile();
    }

    public void BlinkRed()
    {
        BadParticles.Play();

        if (TopCard != null)
        {
            TopCard.BlinkRed();
        }
    }

    public CardData GetTopCard()
    {
        if(Cards.Count == 0)
        {
            return null;
        }

        return Cards[Cards.Count - 1];
    }
}
