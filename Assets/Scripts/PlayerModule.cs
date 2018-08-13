using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerModule : MonoBehaviour {

    public string pName;

    [System.NonSerialized]
    public List<CardSlot> CardSlots = new List<CardSlot>();

    [SerializeField]
    public bool isPlayer = false;

    [SerializeField]
    public Transform ViewSpot;

    [SerializeField]
    EndScoreUI EndScore;

    [SerializeField]
    public Transform Hand;

    [SerializeField]
    public Transform Body;

    [SerializeField]
    Animator m_Anim;

    Vector2 HandReachX;
    Vector2 HandReachZ;
    float BodyMoveMax;
    [SerializeField]
    public Transform RightHandOrigin;

    public Vector3 RightHandRestingSpot;
    public Quaternion RightHandRestingRot;
    Vector3 bodyInitPos;

    public bool hasLost = false;

    private void Awake()
    {
        for(int i=0;i<transform.childCount;i++)
        {
            if(transform.GetChild(i).tag == "Spot")
            {
                CardSlots.Add(transform.GetChild(i).GetComponent<CardSlot>());
            }
        }

        if(Hand != null)
        {
            RightHandRestingSpot = Hand.transform.position;
            RightHandRestingRot = Hand.transform.rotation;
            bodyInitPos = Body.position;
        }
    }

    private void Start()
    {
        InitializeHand();

        if (m_Anim != null)
        {
            m_Anim.SetFloat("IdleSpeed", UnityEngine.Random.Range(-1.5f, 1.5f));
        }
    }

    void InitializeHand()
    {
        float xDist;
        float zDist;

        if (RightHandRestingSpot.x > Game.Instance.DumpPile.transform.position.x)
        {
            xDist = RightHandRestingSpot.x - Game.Instance.DumpPile.transform.position.x;
            HandReachX = new Vector2(Mathf.Lerp(RightHandRestingSpot.x, Game.Instance.DumpPile.transform.position.x, 1f), RightHandRestingSpot.x);
            BodyMoveMax = -5f;
        }
        else
        {
            xDist = Game.Instance.DumpPile.transform.position.x - RightHandRestingSpot.x;
            HandReachX = new Vector2( RightHandRestingSpot.x, Mathf.Lerp(RightHandRestingSpot.x, Game.Instance.DumpPile.transform.position.x, 1f));
            BodyMoveMax = -5f;
        }

        if (RightHandRestingSpot.z > Game.Instance.DumpPile.transform.position.z)
        {
            zDist = RightHandRestingSpot.z - Game.Instance.DumpPile.transform.position.z;
            HandReachZ = new Vector2(Mathf.Lerp(RightHandRestingSpot.z, Game.Instance.DumpPile.transform.position.z, 1f), RightHandRestingSpot.z);
            BodyMoveMax = 5f;
        }
        else
        {
            zDist = Game.Instance.DumpPile.transform.position.z - RightHandRestingSpot.z;
            HandReachZ = new Vector2(RightHandRestingSpot.z, Mathf.Lerp(RightHandRestingSpot.z, Game.Instance.DumpPile.transform.position.z, 1f));
            BodyMoveMax = -5f;
        }

        if(xDist > zDist)
        {
            HandReachZ = Vector2.zero;
        }
        else
        {
            HandReachX = Vector2.zero ;
        }

    }

    public int CardValue
    {
        get
        {
            int Counter = 0;
            foreach (CardSlot slot in CardSlots)
            {
                if (slot.Card != null)
                {
                    Counter += slot.Card.Data.Power;
                }
            }

            return Counter;
        }
    }

    internal void Dispose()
    {
        this.pName = "Empty";
        this.isPlayer = false;

        ResetPlayer();
    }

    internal void SetInfo(string playerName)
    {
        this.pName = playerName;
    }

    internal void ResetPlayer()
    {
        hasLost = false;

        for (int i = 0; i < CardSlots.Count; i++)
        {
            CardSlots[i].Dispose();
        }
    }

    internal int GetSlotIndex(CardUI card)
    {
        for(int i=0;i<CardSlots.Count;i++)
        {
            if(CardSlots[i].Card == card)
            {
                return i;
            }
        }

        Debug.LogError("CANT FIND INDEX");
        return 0;
    }

    public void ShowEndGameScore()
    {
        EndScore.Show(this.pName, this.CardValue);
    }

    public void HideEndGameScore()
    {
        EndScore.Hide();
    }

    internal CardSlot GetNextEmptySlot()
    {
        for(int i=0;i<CardSlots.Count;i++)
        {
            if(CardSlots[i].Card == null)
            {
                return CardSlots[i];
            }
        }

        return null;
    }

    internal CardSlot GetNextMannedSlot()
    {
        for (int i = 0; i < CardSlots.Count; i++)
        {
            if (CardSlots[i].Card != null)
            {
                return CardSlots[i];
            }
        }

        return null;
    }

    public void SendHandBack()
    {
        if (Hand != null)
        {
            if(HandBackInstance != null)
            {
                StopCoroutine(HandBackInstance);
            }

            HandBackInstance = StartCoroutine(SendHandBackRoutine());
        }
    }

    public void PositionHand(Vector3 targetP)
    {
        float clampedValue;
        float bodyMoveValue;
        if (Hand != null)
        {

            if (HandReachZ == Vector2.zero)
            {
                clampedValue = Mathf.Clamp(targetP.x, HandReachX.x, HandReachX.y);
                targetP = new Vector3(clampedValue, targetP.y, targetP.z);

                if (Body.position.x < Game.Instance.DumpPile.transform.position.x)
                {
                    bodyMoveValue = Mathf.Lerp(bodyInitPos.x - BodyMoveMax, bodyInitPos.x + BodyMoveMax /2f, Mathf.InverseLerp(HandReachX.y, HandReachX.x, targetP.x));
                }
                else
                {
                    bodyMoveValue = Mathf.Lerp(bodyInitPos.x - BodyMoveMax, bodyInitPos.x + BodyMoveMax /2f, Mathf.InverseLerp(HandReachX.x, HandReachX.y, targetP.x));
                }

                Body.position = Vector3.Lerp(Body.position, new Vector3(bodyMoveValue, Body.position.y, Body.position.z), Time.deltaTime * 7f);
            }
            else
            {
                clampedValue = Mathf.Clamp(targetP.z, HandReachZ.x, HandReachZ.y);
                targetP = new Vector3(targetP.x, targetP.y, clampedValue);

                bodyMoveValue = Mathf.Lerp(bodyInitPos.z - BodyMoveMax, bodyInitPos.z + BodyMoveMax, Mathf.InverseLerp(HandReachZ.x, HandReachZ.y, targetP.z));
                Body.position = Vector3.Lerp(Body.position, new Vector3(Body.position.x, Body.position.y, bodyMoveValue), Time.deltaTime * 7f);
            }

            
            Hand.transform.position = Vector3.Lerp(Hand.transform.position, targetP, Time.deltaTime * 10f);;
            Hand.LookAt(RightHandOrigin);
        }
    }

    Coroutine HandBackInstance;
    IEnumerator SendHandBackRoutine()
    {

        yield return new WaitForSeconds(1f);

        float t = 0f;
        while(t<1f)
        {
            t += 1f * Time.deltaTime;

            Hand.transform.position = Vector3.Lerp(Hand.transform.position, RightHandRestingSpot, t);
            Hand.transform.rotation = Quaternion.Lerp(Hand.transform.rotation, RightHandRestingRot, t);

            Body.position = Vector3.Lerp(Body.position, bodyInitPos, t);
            yield return 0;
        }

        HandBackInstance = null;
    }

}
