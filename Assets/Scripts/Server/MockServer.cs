using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MockServer : MonoBehaviour
{
    public const int STARTING_CARDS = 4;
    [SerializeField]
    public GameDB DB;

    [SerializeField]
    public Client client;

    [SerializeField]
    public bool DebugMode = false;

    [SerializeField]
    GameRules Rules;

    public PileData GamePile;

    public DumpPileData DumpPile;

    public CardData FocusedCard;
    public CardData CardInAir;

    public List<PlayerData> Players = new List<PlayerData>();
    public List<PlayerBot> Bots = new List<PlayerBot>();

    public int CurrentTurn;
    public int StartTurn;
    public int RoundNumber;

    public int VictoryTurn;
    public bool VictoryRound = false;

    public bool CanStick = true;
    public bool TimerActive = true;
    public bool TutorialMode = false;

    public static MockServer Instance;

    Coroutine TurnTimerInstance;

    string m_StickingPlayer;
    string m_StickedPlayer;

    bool allPlayersLost
    {
        get
        {
            foreach(PlayerData player in Players)
            {
                if(!player.lostGame)
                {
                    return false;
                }
            }

            return true;
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    #region Receive

    internal void OnJoinRoom()
    {
        RemovePlayers();

        AddPlayer("John", true);
        AddPlayer("Eric", true);
        AddPlayer("Bob", true);

        Players.Insert(UnityEngine.Random.Range(0, Players.Count), new PlayerData("Player"));

        for(int i=0;i<Players.Count;i++)
        {
            if(Players[i].isBot)
            {
                AddBot(Players[i]);
            }
        }

        List<string> nameList = new List<string>();

        foreach (PlayerData player in Players)
        {
            nameList.Add(player.pName);
        }

        client.OnJoinRoom(nameList);

        NewGame();
    }

    internal void OnPeekOnCard(string playerName, int slotIndex)
    {
        BroadcastLog(Players[CurrentTurn].pName + " peeks on card");
        client.OnPlayerPeekOnCard(playerName, slotIndex, GetPlayer(playerName).Slots[slotIndex]);

        if (playerName != "Player")
        {
            GetBot(playerName).RevealCard(playerName, slotIndex);
        }

        PostActionCooldown();
    }

    public void OnSwapBlindly(string playerA, int indexA, string playerB, int indexB)
    {
        BroadcastLog(Players[CurrentTurn].pName + " swaps blindly.");
        CardData stashedCard = GetPlayer(playerA).Slots[indexA];
        GetPlayer(playerA).Slots[indexA] = GetPlayer(playerB).Slots[indexB];
        GetPlayer(playerB).Slots[indexB] = stashedCard;

        client.OnPlayerSwapBlindly(Players[CurrentTurn].pName, playerA, indexA, playerB, indexB);

        KnowCardSwapped(playerA, indexA, playerB, indexB);

        PostActionCooldown();
    }

    public void OnPeekAndSwap(string playerA, int indexA, string playerB, int indexB)
    {
        BroadcastLog(Players[CurrentTurn].pName + " peeks and swaps");
        CardData CardA = GetPlayer(playerA).Slots[indexA];
        CardData CardB = GetPlayer(playerB).Slots[indexB];

        if (Players[CurrentTurn].pName != "Player")
        {
            GetBot(Players[CurrentTurn].pName).RevealCard(playerA, indexA);
            GetBot(Players[CurrentTurn].pName).RevealCard(playerB, indexB);
        }

        client.OnPlayerPeekAndSwapPeek(Players[CurrentTurn].pName, playerA, indexA, playerB, indexB, CardA, CardB);

    }

    internal void OnLeaveRoom()
    {
        StopAllCoroutines();
    }

    public void OnPlayersReady()
    {
        if (VictoryRound)
        {
            NewGame();
        }
        else
        {

            SetTurn(StartTurn);
            //SetTurn(UnityEngine.Random.Range(0, Players.Count));
        }
    }

    internal void OnDrawCard(int Pile)
    {
        BroadcastLog(Players[CurrentTurn].pName + " Draws a card from " + (Pile == 0 ? " pile" : " dump pile"));

        CardData card = (Pile == 0)? GamePile.PullCard() : DumpPile.PullCard();

        FocusedCard = card;

        CardInAir = card;

        client.OnDrawCard(Players[CurrentTurn].pName, Pile, card);

        if (GamePile.Cards.Count == 0)
        {
            client.OnPileState(0, 0);
        }
    }

    public void OnReplaceCard(int SlotIndex)
    {
        BroadcastLog(Players[CurrentTurn].pName + " replaces a card in slot " + SlotIndex);
        CardInAir = null;

        client.OnReplaceCard(Players[CurrentTurn].pName, SlotIndex, Players[CurrentTurn].Slots[SlotIndex]);

        DumpPile.PushCard(Players[CurrentTurn].Slots[SlotIndex]);
        Players[CurrentTurn].Slots[SlotIndex] = FocusedCard;

        if (FocusedCard.LastLocation.GetType() == typeof(DumpPileData))
        {
            RevealAll(Players[CurrentTurn].pName, SlotIndex);
        }
        else
        {
            if (Players[CurrentTurn].pName != "Player")
            {
                GetBot(Players[CurrentTurn].pName).RevealCard(Players[CurrentTurn].pName, SlotIndex);
            }
        }

        PostActionCooldown();
    }

    public void OnDumpCard()
    {
        BroadcastLog(Players[CurrentTurn].pName + " dumps a card.");
        CardInAir = null;

        client.OnDumpCard(Players[CurrentTurn].pName, FocusedCard);

        DumpPile.PushCard(FocusedCard);

        if (FocusedCard.Ability == UniqueAbility.Nothing)
        {
            PostActionCooldown();
        }
        else if (FocusedCard.LastLocation.GetType() != typeof(DumpPileData))
        {
            client.OnUniqueAbility(Players[CurrentTurn].pName, FocusedCard.Ability);
        }

        KnowStickOppertunity();

    }

    public void OnDeclareVictory(string playerName)
    {
        if (VictoryRound)
        {
            return;
        }

        if (TutorialMode && playerName != "Player")
        {
            return;
        }

        VictoryRound = true;
        VictoryTurn = CurrentTurn;
        client.OnPlayerDeclareVictory(playerName);
    }

    public void OnPeekSwapCardResult(string playerA, int indexA, string playerB, int indexB, int CardResult)
    {
        if (CardResult == 1)
        {
            OnSwapBlindly(playerA, indexA, playerB, indexB);
        }
        else
        {
            PostActionCooldown();
        }
    }

    //Card sticking should HALT the game, let the sticking player do his sequence and retreive.
    public void OnStickCard(string stickingPlayer, string stickedPlayer, int slotIndex)
    {
        if(DumpPile.Cards.Count <= 0 || !CanStick)
        {
            return;
        }

        //Stop turn timer
        if (TurnTimerInstance != null)
        {
            StopCoroutine(TurnTimerInstance);
            TurnTimerInstance = null;
        }
        
        CardData stickedCard = GetPlayer(stickedPlayer).Slots[slotIndex];


        if (stickedCard == null)
        {
            BroadcastLog(stickingPlayer + " has sticked " + stickingPlayer + " at " + slotIndex + ", But is NULL.");
            return;
        }

        RevealAll(stickedPlayer, slotIndex);

        KnowStickmodeStarted(stickingPlayer);

        m_StickedPlayer = stickedPlayer;
        m_StickingPlayer = stickingPlayer;

        if (stickedCard.Power == DumpPile.Cards[DumpPile.Cards.Count - 1].Power)
        {
            DumpPile.PushCard(GetPlayer(stickedPlayer).Slots[slotIndex]);
            GetPlayer(stickedPlayer).Slots[slotIndex] = null;

            client.OnStickCorrect(stickingPlayer, stickedPlayer, slotIndex, stickedCard);

            KnowCardRemoved(stickedPlayer, slotIndex);

            if (stickingPlayer == stickedPlayer || GetPlayer(stickingPlayer).GetMannedSlots().Count == 0)
            {
                KnowStickmodeDone();
            }

        }
        else
        {
            PlayerData StickingPlayer = GetPlayer(stickingPlayer);
            int emptySlot = StickingPlayer.GetNextEmptyIndex();
            if (emptySlot == -1)
            {
                PlayerLose(stickingPlayer);
            }
            else
            {
                //POTENTIAL EMPTY PILE
                StickingPlayer.Slots[emptySlot] = GamePile.PullCard();
                client.OnStickWrong(stickingPlayer, stickedPlayer, slotIndex, stickedCard);
            }

            KnowStickmodeDone();

        }
    }

    public void OnPlayerGivePenaltyCard(int slotIndex)
    {
        PlayerData stickingPlayer = GetPlayer(m_StickingPlayer);
        PlayerData stickedPlayer = GetPlayer(m_StickedPlayer);

        int nextEmptySlot = stickedPlayer.GetNextEmptyIndex();
        stickedPlayer.Slots[nextEmptySlot] = stickingPlayer.Slots[slotIndex];
        stickingPlayer.Slots[slotIndex] = null;

        KnowCardRepositioned(m_StickingPlayer, slotIndex, m_StickedPlayer, nextEmptySlot);

        client.OnPlayerGavePenalty(stickingPlayer.pName, stickedPlayer.pName, slotIndex, nextEmptySlot);

        KnowStickmodeDone();
        if(TurnTimerInstance != null)
        {
            StopCoroutine(TurnTimerInstance);
        }

        TurnTimerInstance = StartCoroutine(TurnTimer());
    }

    #endregion

    #region Internal



    void NewGame()
    {
        ResetGame();
        GamePile.ResetPile();
        DumpPile.ResetPile();

        ApplyRules();

        for (int i = 0; i < Players.Count; i++)
        {
            for (int c = 0; c < STARTING_CARDS; c++)
            {
                Players[i].Slots[c] = GamePile.PullCard();
            }
        }

        foreach (PlayerBot bot in Bots)
        {
            bot.RevealCard(bot.Player.pName, 1);
            bot.RevealCard(bot.Player.pName, 3);
        }

        GamePile.Cards[GamePile.Cards.Count - 2] = DB.Cards[13];
        
        client.OnNewGame(GetPlayer("Player").Slots[1], GetPlayer("Player").Slots[3]);
    }

    void ResetGame()
    {
        GamePile = new PileData();
        DumpPile = new DumpPileData();
        ResetPlayers();

        VictoryRound = false;
        VictoryTurn = 0;
        RoundNumber = 0;
        StartTurn = Players.IndexOf(GetPlayer("Player"));
    }

    void ResetPlayers()
    {
        for (int i = 0; i < Players.Count; i++)
        {
            Players[i].ResetData();
        }

        foreach(PlayerBot bot in Bots)
        {
            bot.ResetData();
        }
    }

    void ApplyRules()
    {
        if(Rules.isRuleTrue("NoPeekCards"))
        {
            for(int i=0;i<GamePile.Cards.Count;i++)
            {
                if(GamePile.Cards[i].Ability == UniqueAbility.Peek)
                {
                    GamePile.Cards.RemoveAt(i);
                    i--;
                }
            }
        }

        if (Rules.isRuleTrue("NoPeekOtherCards"))
        {
            for (int i = 0; i < GamePile.Cards.Count; i++)
            {
                if (GamePile.Cards[i].Ability == UniqueAbility.PeekOnOther)
                {
                    GamePile.Cards.RemoveAt(i);
                    i--;
                }
            }
        }

        if (Rules.isRuleTrue("NoSwapBlindlyCards"))
        {
            for (int i = 0; i < GamePile.Cards.Count; i++)
            {
                if (GamePile.Cards[i].Ability == UniqueAbility.SwapBlindly)
                {
                    GamePile.Cards.RemoveAt(i);
                    i--;
                }
            }
        }

        if (Rules.isRuleTrue("NoPeekSwapCards"))
        {
            for (int i = 0; i < GamePile.Cards.Count; i++)
            {
                if (GamePile.Cards[i].Ability == UniqueAbility.PeekAndSwap)
                {
                    GamePile.Cards.RemoveAt(i);
                    i--;
                }
            }
        }

        CanStick = !(Rules.isRuleTrue("NoSticking"));
        TimerActive = !(Rules.isRuleTrue("NoTimer"));

        if (Rules.isRuleTrue("HasTutorial"))
        {
            TutorialMode = true;
        }
    }

    public void SetTurn(int playerIndex)
    {
        BroadcastLog(Players[playerIndex].pName + "'s Turn");

        CurrentTurn = playerIndex;

        PlayerData player = Players[playerIndex];

        client.OnSetTurn(player.pName);

        if (PostActionCooldownInstance != null)
        {
            StopCoroutine(PostActionCooldownInstance);
            PostActionCooldownInstance = null;
        }

        if (TurnTimerInstance != null)
        {
            StopCoroutine(TurnTimerInstance);
            TurnTimerInstance = null;
        }
        TurnTimerInstance = StartCoroutine(TurnTimer());

        if (player.pName != "Player")
        {
            if (TutorialMode
                && PlayerInfo.LearningPhase == 2
                && RoundNumber == 0
                && GetNextTurnPlayer(playerIndex).pName == "Player")
            {
                Players[playerIndex].Slots[1] = GetPlayer("Player").Slots[1].Clone();
                GetBot(player.pName).FakeStickTurn();
            }
            else
            {
                GetBot(player.pName).TakeTurn();
                //GenerateBotAction(player);
            }
        }
        else
        {
            if (TutorialMode
                && PlayerInfo.LearningPhase == 3
                && RoundNumber == 2)
            {
                GuideManager.Instance.TryTeach(4);
            }
        }
    }

    public void NextTurn()
    {
        if (CardInAir != null)
        {
            DumpPile.PushCard(CardInAir);
        }

        CurrentTurn++;
        if (CurrentTurn > (Players.Count - 1))
        {
            CurrentTurn = 0;
        }

        if(Players[CurrentTurn].lostGame)
        {
            NextTurn();
            return;
        }

        if (CurrentTurn == StartTurn)
        {
            RoundNumber++;
        }

        if (GamePile.Cards.Count == 0)
        {
            VictoryRound = true;
            VictoryTurn = CurrentTurn;
        }

        if (VictoryRound && CurrentTurn == VictoryTurn)
        {
            GameOver();
            return;
        }

        SetTurn(CurrentTurn);
    }

    public PlayerData GetNextTurnPlayer(int currentTurn)
    {
        if(currentTurn == Players.Count - 1)
        {
            currentTurn = 0;
        }
        else
        {
            currentTurn++;
        }

        return Players[currentTurn];
    }

    void GameOver()
    {
        int LowestNumber = 999999;

        for (int i = 0; i < Players.Count; i++)
        {
            if (LowestNumber > Players[i].CardValue)
            {
                LowestNumber = Players[i].CardValue;
            }
        }

        List<PlayerData> winners = new List<PlayerData>();
        int VictorIndex = 0;

        for (int i = 0; i < Players.Count; i++)
        {
            if (LowestNumber == Players[i].CardValue)
            {
                winners.Add(Players[i]);
            }
        }

        VictorIndex = UnityEngine.Random.Range(0, winners.Count);

        for(int i=0;i<Players.Count;i++)
        {
            if(Players[i].pName == winners[VictorIndex].pName)
            {
                VictorIndex = i;
                break;
            }
        }

        client.OnGameOver(Players, VictorIndex);
    }

    public PlayerData GetPlayer(string gName)
    {
        for (int i = 0; i < Players.Count; i++)
        {
            if (gName == Players[i].pName)
            {
                return Players[i];
            }
        }

        return null;
    }

    public PlayerBot GetBot(string pName)
    {
        for (int i = 0; i < Bots.Count; i++)
        {
            if (pName == Bots[i].Player.pName)
            {
                return Bots[i];
            }
        }

        return null;
    }

    public void AddPlayer(string Name, bool isBot)
    {
        PlayerData player = new PlayerData(Name);
        player.isBot = isBot;
        Players.Add(player);


    }

    public void AddBot(PlayerData player)
    {
        PlayerBot bot = Instantiate(ResourcesLoader.Instance.GetObject("PlayerBot")).GetComponent<PlayerBot>();

        bot.SetInfo(player, this);
        Bots.Add(bot);
    }

    public void RemovePlayers()
    {
        Players.Clear();

        while (Bots.Count > 0)
        {
            Destroy(Bots[0].gameObject);
            Bots.RemoveAt(0);
        }
    }

    IEnumerator TurnTimer()
    {
        if(!TimerActive)
        {
            TurnTimerInstance = null;
            yield break;
        }

        yield return new WaitForSeconds(20);

        TurnTimerInstance = null;

        NextTurn();
    }

    public void BroadcastLog(string msg)
    {
        if(DebugMode)
        {
            Debug.Log("SERVER - "+msg);
        }
    }

    public void PlayerLose(string losingPlayer)
    {
        PlayerData player = GetPlayer(losingPlayer);

        player.lostGame = true;


        for(int i=0;i<player.Slots.Count;i++)
        {
            if(player.Slots[i] == null)
            {
                continue;
            }

            RevealAll(player.pName, i);
        }

        client.OnPlayerLost(player);

        if(Players[CurrentTurn].lostGame)
        {
            NextTurn();
        }

        if(allPlayersLost)
        {
            GameOver();
        }
    }
    #endregion

    public void PostActionCooldown()
    {
        if(PostActionCooldownInstance != null)
        {
            StopCoroutine(PostActionCooldownInstance);
        }

        PostActionCooldownInstance = StartCoroutine(PostActionCooldownRoutine());
    }

    Coroutine PostActionCooldownInstance;
    IEnumerator PostActionCooldownRoutine()
    {
        if(TurnTimerInstance != null)
        {
            StopCoroutine(TurnTimerInstance);
            TurnTimerInstance = null;
        }

        yield return new WaitForSeconds(3f);

        NextTurn();

        PostActionCooldownInstance = null;
    }

    public void DecayBotsMemory()
    {
        foreach(PlayerBot bot in Bots)
        { bot.DecayMemory(); }
    }

    public void RevealAll(string playerName, int slotIndex)
    {
        foreach (PlayerBot bot in Bots)
        {
            bot.RevealCard(playerName, slotIndex);
        }
    }

    public void KnowCardSwapped(string p1, int index1, string p2, int index2)
    {
        foreach (PlayerBot bot in Bots)
        {
            bot.KnowCardSwapped(p1, index1, p2, index2);
        }
    }

    public void KnowStickmodeStarted(string playerName)
    {
        foreach (PlayerBot bot in Bots)
        {
            bot.KnowStickmodeStarted(playerName);
        }
    }

    public void KnowStickmodeDone()
    {
        foreach (PlayerBot bot in Bots)
        {
            bot.KnowStickmodeDone();
        }

        KnowStickOppertunity();
    }

    public void KnowStickOppertunity()
    {
        foreach (PlayerBot bot in Bots)
        {
            bot.KnowStickOppertunity();
        }
    }

    public void KnowCardRemoved(string player, int slotIndex)
    {
        foreach (PlayerBot bot in Bots)
        {
            bot.KnowCardRemoved(player,slotIndex);
        }
    }

    public void KnowCardRepositioned(string fromPlayer, int fromIndex, string toPlayer, int toIndex)
    {
        foreach(PlayerBot bot in Bots)
        {
            bot.KnowCardRepositioned(fromPlayer, fromIndex, toPlayer, toIndex);
        }
    }

}
