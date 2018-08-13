using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBot : MonoBehaviour {

    public MockServer Server;
    public PlayerData Player;

    [SerializeField]
    List<PlayerKnowledge> Knowledge = new List<PlayerKnowledge>();

    Coroutine TurnRoutineInstance;

    [SerializeField]
    bool DebugMode;

    string StickingPlayer = "";

    public bool StickMode
    {
        get
        {
            return !string.IsNullOrEmpty(StickingPlayer);
        }
    }
    public void SetInfo(PlayerData player, MockServer server)
    {
        this.Server = server;
        this.Player = player;

        ResetData();
    }

    public void ResetData()
    {
        Knowledge.Clear();

        for (int i = 0; i < Server.Players.Count; i++)
        {
            Knowledge.Add(new PlayerKnowledge(Server.Players[i]));
        }
    }
    
    public void RevealCard(string playerName, int slotIndex)
    {
        GetPlayerKnowledge(playerName).RememberCard(slotIndex);
    }

    public void KnowCardSwapped(string p1, int index1, string p2, int index2)
    {
        PlayerKnowledge playerA = GetPlayerKnowledge(p1);
        PlayerKnowledge playerB = GetPlayerKnowledge(p2);

        CardMemory stashedCard = playerA.Slots[index1];

        playerA.Slots[index1] = playerB.Slots[index2];
        playerB.Slots[index2] = stashedCard;
    }

    public void KnowStickmodeStarted(string playerName)
    {
        StickingPlayer = playerName;
    }

    public void KnowStickmodeDone()
    {
        StickingPlayer = "";
    }

    public void KnowStickOppertunity()
    {
        if(KnowStickOppertunityInstance != null)
        {
            StopCoroutine(KnowStickOppertunityInstance);
        }

        KnowStickOppertunityInstance = StartCoroutine(KnowStickOppertunityRoutine());
    }

    Coroutine KnowStickOppertunityInstance;
    IEnumerator KnowStickOppertunityRoutine()
    {
        foreach (PlayerKnowledge knowledge in Knowledge)
        {
            for (int i = 0; i < knowledge.Slots.Count; i++)
            {
                if (knowledge.Slots[i] == null)
                {
                    continue;
                }

                if (StickMode)
                {
                    KnowStickOppertunityInstance = null;
                    yield break;
                }

                if (knowledge.Slots[i].Card.Power == Server.DumpPile.GetCurrentTopCard().Power && Random.Range(0, 2) == 0)
                {
                    yield return new WaitForSeconds(Random.Range(1.5f, 6f));

                    if (knowledge.Slots[i].Card.Power == Server.DumpPile.GetCurrentTopCard().Power)
                    {

                        Server.OnStickCard(this.Player.pName, knowledge.PlayerRef.pName, i);

                        if (StickMode)
                        {
                            int fromCard;
                            if (GetWorstCard(out fromCard))
                            {
                                yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));

                                Server.OnPlayerGivePenaltyCard(fromCard);
                            }
                            else
                            {
                                BroadcastLog("No worst card?");
                            }
                        }
                    }
                }
            }
        }

        KnowStickOppertunityInstance = null;
    }

    public void KnowCardRemoved(string playerName, int slotIndex)
    {
        GetPlayerKnowledge(playerName).ForgetCard(slotIndex);
    }

    public void KnowCardRepositioned(string fromPlayer, int fromIndex, string toPlayer, int toIndex)
    {
        PlayerKnowledge fromPlayerKnowledge = GetPlayerKnowledge(fromPlayer);

        if (fromPlayerKnowledge.Slots[fromIndex] == null)
        {
            return;
        }

        int lastMemoryScore = fromPlayerKnowledge.Slots[fromIndex].MemoryScore;
        fromPlayerKnowledge.ForgetCard(fromIndex);

        if (Random.Range(0, 2) == 0)
        {
            GetPlayerKnowledge(toPlayer).RememberCard(toIndex, lastMemoryScore);
        }
    }

    public PlayerKnowledge GetPlayerKnowledge(string pName)
    {
        for(int i=0;i<Knowledge.Count;i++)
        {

            if (Knowledge[i].PlayerRef.pName == pName)
            {
                return Knowledge[i];
            }
        }

        return null;
    }

    public void DecayMemory()
    {
        for(int i=0;i<Knowledge.Count;i++)
        {
            Knowledge[i].DecayMemory();
        }
    }

    public void TakeTurn()
    {
        if(TurnRoutineInstance != null)
        {
            Debug.LogError("Mid Turn!?!?");

            StopCoroutine(TurnRoutineInstance);
        }
        TurnRoutineInstance = StartCoroutine(TurnRoutine());
    }

    public void FakeStickTurn()
    {
        StartCoroutine(FakeStickTurnRoutine());
    }

    IEnumerator FakeStickTurnRoutine()
    {
        Server.OnDrawCard(0);

        yield return new WaitForSeconds(1f);

        Server.OnReplaceCard(1);

        yield return new WaitForSeconds(1f);

        GuideManager.Instance.TryTeach(3);
    }

    public void HaltTurn()
    {
        if (TurnRoutineInstance != null)
        {
            Debug.LogError("Mid Turn!?!?");

            StopCoroutine(TurnRoutineInstance);
        }

        
    }

    IEnumerator TurnRoutine()
    {
        if (Server.Players[Server.CurrentTurn].pName != Player.pName)
        {
            Debug.LogError("NOT MY TURN!!!");
            yield break;
        }

        if(!Server.VictoryRound && Player.CardValue <= 5 && Random.Range(0,2) == 0)
        {
            Server.OnDeclareVictory(this.Player.pName);
        }

        yield return new WaitForSeconds(Random.Range(1f, 3f));

        while(StickMode)
        {
            yield return 0;
        }

        if(shouldPickFromDump())
        {
            Server.OnDrawCard(1);
        }
        else
        {
            Server.OnDrawCard(0);
        }

        yield return new WaitForSeconds(Random.Range(1f, 3f));

        while (StickMode)
        {
            yield return 0;
        }

        int targetSlot;
        if(hasGoodReplacement(out targetSlot))
        {
            Server.OnReplaceCard(targetSlot);
        }
        else
        {
            Server.OnDumpCard();

            yield return new WaitForSeconds(3f);

            while (StickMode)
            {
                yield return 0;
            }

            switch (Server.FocusedCard.Ability)
            {
                case UniqueAbility.Peek:
                    {
                        int bestIndex;
                        if (GetBestPeek(out bestIndex))
                        {
                            Server.OnPeekOnCard(this.Player.pName, bestIndex);
                        }
                        else
                        {
                            Server.PostActionCooldown();
                        }
                        break;
                    }
                case UniqueAbility.PeekOnOther:
                    {
                        int bestIndex;
                        string playerName;
                        if (GetBestPeekOthers(out playerName, out bestIndex))
                        {
                            BroadcastLog(Player.pName + "'s found " + playerName + "'s " + bestIndex + " as best Peek");
                            Server.OnPeekOnCard(playerName, bestIndex);
                        }
                        else
                        {
                            Server.PostActionCooldown();
                        }
                        break;
                    }
                case UniqueAbility.SwapBlindly:
                    {
                        int worstIndex;
                        int goodIndex;
                        string playerName;

                        if (   GetWorstCard(out worstIndex) 
                            && GetGoodCard(out playerName, out goodIndex))
                        {
                            BroadcastLog(Player.pName + "'s best swap is " + playerName + "'s " + goodIndex + " with his " + worstIndex);
                            Server.OnSwapBlindly(Player.pName, worstIndex, playerName, goodIndex);
                            break;
                        }

                        string p1;
                        int p1index;
                        string p2;
                        int p2index;

                        if (GetRandomCard(out p1, out p1index) && GetRandomCard(out p2, out p2index))
                        {
                            BroadcastLog(Player.pName + "'s random swap is " + p1 + "'s " + p1index + " with " + p2 + "'s "+p2index);
                            Server.OnSwapBlindly(p1, p1index, p2, p2index);
                        }
                        else
                        {
                            Server.PostActionCooldown();
                        }
                        break;
                    }
                case UniqueAbility.PeekAndSwap:
                    {
                        int worstIndex;
                        int goodIndex;
                        string playerName;

                        if (GetWorstCard(out worstIndex)) // Get my worst card
                        {
                            if(GetGoodCard(out playerName, out goodIndex)) // Get best known card, if its better then mine:
                            {
                                BroadcastLog(Player.pName + "'s best swap is " + playerName + "'s " + goodIndex + " with his " + worstIndex);
                                Server.OnPeekAndSwap(Player.pName, worstIndex, playerName, goodIndex);

                                yield return new WaitForSeconds(1f);
                                if(Server.GetPlayer(playerName).Slots[goodIndex].Power < Player.Slots[worstIndex].Power)
                                {
                                    BroadcastLog(Player.pName + " decides to swap!");
                                    Server.OnPeekSwapCardResult(Player.pName, worstIndex, playerName, goodIndex, 1);
                                }
                                else
                                {
                                    BroadcastLog(Player.pName + " decides to keep...");
                                    Server.OnPeekSwapCardResult(Player.pName, worstIndex, playerName, goodIndex, 0);
                                }
                            }
                            else // Can't find better card...
                            {
                                if (GetRandomCard(out playerName, out goodIndex)) // Get a random card...
                                {
                                    BroadcastLog(Player.pName + "'s random swap is " + playerName + "'s " + goodIndex + " with " + this.Player.pName+ "'s " + worstIndex);
                                    Server.OnPeekAndSwap(Player.pName, worstIndex, playerName, goodIndex);

                                    yield return new WaitForSeconds(1f);
                                    if (Server.GetPlayer(playerName).Slots[goodIndex].Power < Player.Slots[worstIndex].Power)
                                    {
                                        BroadcastLog(Player.pName + " decides to swap!");
                                        Server.OnPeekSwapCardResult(Player.pName, worstIndex, playerName, goodIndex, 1);
                                    }
                                    else
                                    {
                                        BroadcastLog(Player.pName + " decides to keep...");
                                        Server.OnPeekSwapCardResult(Player.pName, worstIndex, playerName, goodIndex, 0);
                                    }
                                }
                                else // Can't find a random card...
                                {
                                    Server.PostActionCooldown();
                                }
                            }
                        }
                        else // Can't find worst card...
                        {
                            string p1;
                            int p1index;
                            string p2;
                            int p2index;

                            if (GetRandomCard(out p1, out p1index) && GetRandomCard(out p2, out p2index)) // Peek and swap randomly...
                            {
                                BroadcastLog(Player.pName + "'s random swap is " + p1 + "'s " + p1index + " with " + p2 + "'s " + p2index);
                                Server.OnSwapBlindly(p1, p1index, p2, p2index);
                            }
                            else // Can't find two random cards... (!?!?!?!?)
                            {
                                Server.PostActionCooldown();
                            }
                        }

                        break;
                    }
            }
        }

        TurnRoutineInstance = null;
    }

    public bool shouldPickFromDump()
    {
        if(Server.DumpPile.Cards.Count == 0)
        {
            return false;
        }

        PlayerKnowledge pKnowledge = GetPlayerKnowledge(this.Player.pName);

        int maxPower = Server.DumpPile.Cards[Server.DumpPile.Cards.Count - 1].Power;
        for(int i=0;i<pKnowledge.Slots.Count;i++)
        {
            if (Player.Slots[i] == null)
            {
                continue;
            }

            if (pKnowledge.Slots[i] == null )
            {
                continue;
            }

            if(pKnowledge.Slots[i].Card.Power > maxPower)
            {
                return true;
            }
        }

        return false;
    }

    public bool hasGoodReplacement(out int targetSlot)
    {
        PlayerKnowledge pKnowledge = GetPlayerKnowledge(this.Player.pName);

        int maxPower = Server.FocusedCard.Power;
        int bestSlot = -1;
        for (int i = 0; i < pKnowledge.Slots.Count; i++)
        {
            if (Player.Slots[i] == null)
            {
                continue;
            }

            if (pKnowledge.Slots[i] == null)
            {
                continue;
            }

            if (pKnowledge.Slots[i].Card.Power > maxPower)
            {
                bestSlot = i;
                maxPower = pKnowledge.Slots[i].Card.Power;
            }
        }

        if(bestSlot != -1)
        {
            targetSlot = bestSlot;
            return true;
        }

        targetSlot = -1;
        return false;
    }

    public bool GetBestPeek(out int bestIndex)
    {
        PlayerKnowledge tempKnowledge = GetPlayerKnowledge(Player.pName);

        bestIndex = -1;
        int lowestScore = 9999;
        for(int i=0;i< tempKnowledge.Slots.Count;i++)
        {
            if (Player.Slots[i] == null)
            {
                continue;
            }

            if (tempKnowledge.Slots[i] == null)
            {
                bestIndex = i;
                return true;
            }
            else if (tempKnowledge.Slots[i].MemoryScore < lowestScore)
            {
                lowestScore = tempKnowledge.Slots[i].MemoryScore;
                bestIndex = i;
            }
        }
        
        return bestIndex != -1;
    }

    public bool GetBestPeekOthers(out string playerName, out int bestIndex)
    {

        for (int i = 0; i < Knowledge.Count; i++)
        {
            for (int c = 0; c < Knowledge[i].Slots.Count; c++)
            {
                if (Knowledge[i].PlayerRef.pName == Player.pName)
                {
                    continue;
                }

                if (Server.Players[i].Slots[c] == null)
                {
                    continue;
                }

                if (Knowledge[i].Slots[c] == null)
                {
                    playerName = Knowledge[i].PlayerRef.pName;
                    bestIndex = c;
                    return true;
                }
            }
        }

        playerName = "";
        bestIndex = -1;
        return false;
    }

    public bool GetWorstCard(out int bestIndex)
    {
        PlayerKnowledge tempKnowledge = GetPlayerKnowledge(Player.pName);

        bestIndex = -1;
        int highestScore = 0;
        int blindOption = -1;
        for (int i = 0; i < tempKnowledge.Slots.Count; i++)
        {
            if(Player.Slots[i] == null)
            {
                continue;
            }
            
            if (tempKnowledge.Slots[i] == null)
            {
                blindOption = i;
            }
            else if (tempKnowledge.Slots[i].Card.Power > highestScore)
            {
                highestScore = tempKnowledge.Slots[i].MemoryScore;
                bestIndex = i;
            }
        }

        if(bestIndex == -1 && blindOption != -1)
        {
            bestIndex = blindOption;
        }

        return (bestIndex != -1);
    }

    public bool GetGoodCard(out string playerName, out int bestIndex)
    {
        playerName = "";
        bestIndex = -1;
        int bestPower = 9999;
        for (int i = 0; i < Knowledge.Count; i++)
        {
            if (Knowledge[i].PlayerRef.pName == Player.pName)
            {
                continue;
            }

            for (int c = 0; c < Knowledge[i].Slots.Count; c++)
            {

                if (Server.Players[i].Slots[c] == null)
                {
                    continue;
                }

                if (Knowledge[i].Slots[c] != null)
                {
                    if (Knowledge[i].Slots[c].Card.Power <= 5 && bestPower > Knowledge[i].Slots[c].Card.Power)
                    {
                        bestIndex = c;
                        bestPower = Knowledge[i].Slots[c].Card.Power;
                        playerName = Knowledge[i].PlayerRef.pName;

                    }
                }
            }
        }

        return (bestIndex != -1);
    }

    public bool GetRandomCard(out string playerName, out int playerIndex)
    {
        List<PlayerData> checkPlayers = new List<PlayerData>();

        int prnd;
        int crnd;
        int index = -1;
        int tries = 20; // Sorry for that... Just too much of a hassle for a temp bot.
        while(tries > 0)
        {
            prnd = Random.Range(0, Server.Players.Count);
            crnd = Random.Range(0, Server.Players[prnd].Slots.Count);
            if(Server.Players[prnd].Slots[crnd] != null)
            {
                playerName = Server.Players[prnd].pName;
                playerIndex = crnd;
                return true;
            }

            tries--;
        }

        playerName = "";
        playerIndex = -1;
        return false;
    }


    public void BroadcastLog(string msg)
    {
        if (DebugMode)
        {
            Debug.Log(msg);
        }
    }

}

[System.Serializable]
public class PlayerKnowledge
{
    public List<CardMemory> Slots = new List<CardMemory>();

    public PlayerData PlayerRef;

    public PlayerKnowledge(PlayerData player)
    {
        this.PlayerRef = player;
        Slots.Add(null);
        Slots.Add(null);
        Slots.Add(null);
        Slots.Add(null);
        Slots.Add(null);
        Slots.Add(null);
    }

    public void RememberCard(int index, int memoryScore = 5)
    {
        Slots[index] = new CardMemory();
        Slots[index].Card = PlayerRef.Slots[index];
        Slots[index].MemoryScore = 5;
    }

    public void DecayMemory()
    {
        for(int i=0;i< Slots.Count;i++)
        {
            if(Slots[i] != null)
            {
                Slots[i].MemoryScore--;
                if(Slots[i].MemoryScore <= 0)
                {
                    Slots[i] = null;
                }
            }
        }
    }
    
    public void ForgetCard(int index)
    {
        if(Slots[index] != null)
        {
            Slots[index] = null;
        }
    }

    public CardMemory GetMemory(CardData card)
    {
        for(int i=0;i<Slots.Count;i++)
        {
            if(Slots[i].Card == card)
            {
                return Slots[i];
            }
        }

        return null;
    }
}

[System.Serializable]
public class CardMemory
{
    public int MemoryScore;
    public CardData Card;
}
