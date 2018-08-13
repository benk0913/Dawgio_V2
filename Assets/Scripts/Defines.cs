using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TurnPhase
{
    OtherPlayer,
    DrawCard,
    UseCard,
    TurnEnd,
    AbilityAction,
    WaitingForAnswer,
}