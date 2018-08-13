using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PileUI : MonoBehaviour
{

    [SerializeField]
    public Transform topSpot;

    [SerializeField]
    GameObject PileObjects;

    [SerializeField]
    Image TopCardBack;

    [SerializeField]
    ParticleSystem BadParticles;
    
    public void SetPileState(int state)
    {
        if(state == 1)
        {
            PileObjects.gameObject.SetActive(true);
        }
        else
        {
            PileObjects.gameObject.SetActive(false);
        }
    }

    public GameObject PullCard(CardData data = null)
    {
        GameObject cardObject = ResourcesLoader.Instance.GetRecycledObject("Card");

        if (data != null)
        {
            cardObject.GetComponent<CardUI>().SetInfo(data);
            cardObject.GetComponent<CardUI>().Data.LastLocation = this;
        }

        cardObject.transform.position = topSpot.transform.position;
        cardObject.transform.rotation = topSpot.transform.rotation;


        return cardObject;
    }

    public GameObject PullCard()
    {
        GameObject cardObject = ResourcesLoader.Instance.GetRecycledObject("Card");

        cardObject.transform.position = topSpot.transform.position;
        cardObject.transform.rotation = topSpot.transform.rotation;

        if(cardObject.GetComponent<CardUI>().Data != null)
            cardObject.GetComponent<CardUI>().Data.LastLocation = this;

        return cardObject;
    }

    public void BlinkRed()
    {
        StartCoroutine(BlinkRedRoutine());
    }

    IEnumerator BlinkRedRoutine()
    {
        BadParticles.Play();

        TopCardBack.color = Color.red;

        yield return new WaitForSeconds(0.1f);

        TopCardBack.color = Color.white;
    }
}
