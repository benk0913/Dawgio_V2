using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "Card", order = 1)]
public class CardData : ScriptableObject
{
    public string Name = "Unknown";
    public Sprite Icon;
    public int Power = 0;

    [TextArea(3,3)]
    public string Description = "Unknown...";

    [SerializeField]
    public UniqueAbility Ability;

    [System.NonSerialized]
    public object LastLocation;



    public CardData Clone ()
    {
        CardData inst = CreateInstance<CardData>();
        inst.SetInfo(this);

        return inst;
    }
    
    public void SetInfo(CardData data)
    {
        this.Name = data.Name;
        this.Icon = data.Icon;
        this.Power = data.Power;
        this.Description = data.Description;
        this.Ability = data.Ability;
    }
}

public enum UniqueAbility
{
    Nothing,
    Peek,
    PeekOnOther,
    SwapBlindly,
    PeekAndSwap,
}

