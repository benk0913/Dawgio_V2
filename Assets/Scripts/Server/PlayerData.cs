using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerData {

    public string pName;

    public List<CardData> Slots = new List<CardData>();

    public bool isBot;

    public bool lostGame;

    public int CardValue
    {
        get
        {
            int Counter = 0;
            foreach(CardData card in Slots)
            {
                if(card == null)
                {
                    continue;
                }

                Counter += card.Power;
            }

            return Counter;
        }
    }

    public PlayerData(string sName)
    {
        this.pName = sName;

        Slots.Add(null);
        Slots.Add(null);
        Slots.Add(null);
        Slots.Add(null);
        Slots.Add(null);
        Slots.Add(null);
    }

    public void ResetData()
    {
        Slots.Clear();

        Slots.Add(null);
        Slots.Add(null);
        Slots.Add(null);
        Slots.Add(null);
        Slots.Add(null);
        Slots.Add(null);

        lostGame = false;
    }

    public List<CardData> GetMannedSlots()
    {
        List<CardData> tempList = new List<CardData>();

        for(int i=0;i<Slots.Count;i++)
        {
            if(Slots[i] != null)
            {
                tempList.Add(Slots[i]);
            }
        }

        return tempList;
    }

    public int GetSlotIndex(CardData card)
    {
        for(int i=0;i<Slots.Count;i++)
        {
            if(Slots[i] == card)
            {
                return i;
            }
        }

        return -1;
    }

    internal int GetNextEmptyIndex()
    {
        for(int i=0;i<Slots.Count;i++)
        {
            if(Slots[i] == null)
            {
                return i;
            }
        }

        return -1;
    }
}
