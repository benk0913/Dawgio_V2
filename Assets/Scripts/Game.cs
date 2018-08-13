using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Game : MonoBehaviour {

    #region Configurable

    public const int STARTING_CARDS = 4;

    [SerializeField]
    public GameDB DB;

    [SerializeField]
    public PileUI GamePile;

    [SerializeField]
    public DumpPileUI DumpPile;

    [SerializeField]
    ShockMessageUI ShockMessage;

    [SerializeField]
    List<PlayerModule> Players = new List<PlayerModule>();

    [SerializeField]
    CanvasGroup PickBetweenCardsWindow;

    [SerializeField]
    Text m_txtPickCardsPlayerA;

    [SerializeField]
    Text m_txtPickCardsPlayerB;

    [SerializeField]
    TimerUI TurnTimer;

    [SerializeField]
    GameRules Rules;
    #endregion

    PlayerModule CurrentPlayer;
    public CardUI FocusedCard
    {
        get
        {
            return focusCard;
        }
        set
        {
            if (focusCard != null)
            {
                focusCard.SetSortingLayer(0,"Card");
            }

            focusCard = value;

            if (focusCard != null)
            {
                focusCard.SetSortingLayer(0, "FocusCard");
            }
        }
    }
    CardUI focusCard;
    

    [SerializeField]
    public TurnPhase Phase;

    [SerializeField]
    string LastHandledAction;

    [SerializeField]
    GameObject CurrentStickEffect;

    [SerializeField]
    GameObject turnQuad;

    public UniqueAbility CurrentAbility;

    public bool VictoryRound;

    public bool CanStick;

    public bool PlayerReady;

    public bool TutorialMode = false;

    public static Game Instance;

    public bool isBusy
    {
        set
        {
            if(!value)
            {
                HandleNextAction();
            }

            busy = value;
        }
        get
        {
            return busy;
        }
    }
    bool busy;

    string CurrentSticker = "";
    string m_CurrentSticked = "";

    public bool CanInteract
    {
        get
        {
            if(!string.IsNullOrEmpty(CurrentSticker) && CurrentSticker != "Player")
            {
                SetMessage("Someone is sticking a card!");
            }
            return (string.IsNullOrEmpty(CurrentSticker) || CurrentSticker == "Player") && !GetPlayer("Player").hasLost;
        }
    }

    public List<Action> ActionQue = new List<Action>();

    List<GameObject> markers = new List<GameObject>();

    CardUI InteractionCardA;
    CardUI InteractionCardB;

    int CardSelectionResult = 0;

    Coroutine TurnGlowCoroutineInstnace;

    private void Awake()
    {
        Instance = this;
        Application.targetFrameRate = 60;
    }

    private void Start()
    {
        Intialize();
    }

    void Intialize()
    {
        StartCoroutine(InitializeRoutine());
    }

    IEnumerator InitializeRoutine()
    {
        GuideManager.Instance.ShutCurrent();

        yield return 0;

        while(ResourcesLoader.Instance.m_bLoading)
        {
            yield return 0;
        }

        Client.Instance.SendJoinRoom();
    }

    public void SetupRoom(List<string> givenPlayers)
    {
        DisposeRoom();

        ApplyRules();

        givenPlayers = ResortPlayerList(givenPlayers);

        int playerIndex = 1;
        for (int i=0;i<givenPlayers.Count;i++)
        {
            if (givenPlayers[i] == "Player")
            {
                Players[0].SetInfo("Player");
                Players[0].isPlayer = true;
                continue;
            }

            Players[playerIndex].SetInfo(givenPlayers[i]);
            playerIndex++;
        }
    }

    List<string> ResortPlayerList(List<string> playerList)
    {

        List<string> ResortedList = new List<string>();

        int playerIndex = playerList.IndexOf("Player");

        ResortedList.Add(playerList[playerIndex]);

        if (playerIndex != playerList.Count - 1)
        {
            for (int i = playerIndex + 1; i < playerList.Count; i++)
            {
                ResortedList.Add(playerList[i]);
            }
        }

        for (int i = 0; i < playerIndex; i++)
        {
            ResortedList.Add(playerList[i]);
        }

        return ResortedList;
    }

    public void SetTurn(string playerName)
    {
        HaltPreviousTurn();

        CurrentPlayer = GetPlayer(playerName);

        LerpTurnGlow(CurrentPlayer);

        if(CurrentPlayer.isPlayer)
        {
            SetMessage("Your Turn...");

            Phase = TurnPhase.DrawCard;


            if (TutorialMode)
            {
                GuideManager.Instance.TryTeach(3);
            }
        }
        else
        {
            SetMessage(playerName + "'s Turn...");

            Phase = TurnPhase.OtherPlayer;
        }

        TurnTimer.SetTime(20);

    }

    private void HaltPreviousTurn()
    {
        UnsetMarkers();

        if(Phase == TurnPhase.UseCard)
        {
            DumpPile.PushCard(FocusedCard.Data);
            DestroyFocusedCard();
        }
        else if ((Phase == TurnPhase.AbilityAction || Phase == TurnPhase.TurnEnd) && FocusedCard != null)
        {
            if (FocusedCard.Data.Ability == UniqueAbility.SwapBlindly)
            {
                if (InteractionCardA != null)
                {
                    InteractionCardA.Deselect();
                    InteractionCardA = null;
                }

                if (InteractionCardB != null)
                {
                    InteractionCardB.Deselect();
                    InteractionCardB = null;
                }
            }
            else if (FocusedCard.Data.Ability == UniqueAbility.PeekAndSwap)
            {

                if (InteractionCardA != null)
                {
                    InteractionCardA.Align(InteractionCardA.ParentSlot.transform);
                    InteractionCardA.Deselect();
                    InteractionCardA = null;
                }

                if (InteractionCardB != null)
                {
                    InteractionCardB.Align(InteractionCardB.ParentSlot.transform);
                    InteractionCardB.Deselect();
                    InteractionCardB = null;
                }

                PickBetweenCardsWindow.gameObject.SetActive(false);
            }
        }
        else if(Phase == TurnPhase.OtherPlayer)
        {
            if(FocusedCard != null)
            {
                DumpPile.PushCard(FocusedCard.Data);
                DestroyFocusedCard();
            }
        }
    }

    public void DisposeRoom()
    {
        for(int i=0;i<Players.Count;i++)
        {
            Players[i].Dispose();
        }
    }

    public void NewGame(CardData leftCard, CardData rightCard)
    {
        StartCoroutine(NewGameRoutine(leftCard, rightCard));
    }

    public void ResetGame()
    {
        GamePile.SetPileState(1);
        DumpPile.ResetPile();
        VictoryRound = false;
        DestroyFocusedCard();

        for (int i = 0; i < Players.Count; i++)
        {
            Players[i].ResetPlayer();
        }
    }

    public void ApplyRules()
    {
        CanStick = !(Rules.isRuleTrue("NoSticking"));

        TurnTimer.TimerActive(!Rules.isRuleTrue("NoTimer"));

        if(Rules.isRuleTrue("HasTutorial"))
        {
            TutorialMode = true;
        }
    }

    public void SetMessage(string sMessage)
    {
        ShockMessage.CallMessage(sMessage);
    }
    
    public void SetCardSticked(CardUI card)
    {
        if (CurrentStickEffect == null)
        {
            CurrentStickEffect = ResourcesLoader.Instance.GetRecycledObject("StickParticles");
        }

        CurrentStickEffect.transform.position = card.transform.position;
    }

    public void UnsetCardStickedEffect()
    {
        if(CurrentStickEffect != null)
        {
            CurrentStickEffect.gameObject.SetActive(false);
            CurrentStickEffect = null;
        }
    }

    #region PlayerActions

    public void InteractWithPile(int pile)
    {
        //Stick mode or cant interact
        if(!CanInteract || !string.IsNullOrEmpty(CurrentSticker))
        {
            return;
        }

        if (Phase == TurnPhase.WaitingForAnswer)
        {
            ShockMessage.CallMessage("Please Wait...", Color.red);
            GamePile.BlinkRed();
            return;
        }
        else if (Phase == TurnPhase.OtherPlayer)
        {
            ShockMessage.CallMessage("This is not your turn!", Color.red);
            GamePile.BlinkRed();
            return;
        }
        else if (Phase == TurnPhase.UseCard)
        {
            ShockMessage.CallMessage("You already drew a card!", Color.red);
            GamePile.BlinkRed();
            return;
        }
        else if (Phase == TurnPhase.TurnEnd)
        {
            ShockMessage.CallMessage("You have already finished your turn!", Color.red);
            GamePile.BlinkRed();
            return;
        }

        Phase = TurnPhase.WaitingForAnswer;
        Client.Instance.SendDrawCard(pile);
    }

    public void InteractWithCard(CardUI Card, bool doubleClick = false)
    {
        if (!CanInteract)
        {
            return;
        }

        if (Card.isLerping)
        {
            ShockMessage.CallMessage("Please Wait...", Color.red);

            Card.BlinkRed();
            return;
        }

        if(Phase == TurnPhase.WaitingForAnswer)
        {
            ShockMessage.CallMessage("Please Wait...", Color.red);

            Card.BlinkRed();
            return;
        }

        if (CurrentSticker == "Player")
        {
            if (CurrentSticker == m_CurrentSticked)
            {
                ShockMessage.CallMessage("You already set that to stick.", Color.red);
                return;
            }

            if (Card.ParentPlayer.pName != "Player")
            {
                ShockMessage.CallMessage("Can only give your cards as penalty.", Color.red);
                return;
            }

            Client.Instance.SendGivePlayerPenaltyCard(Card.ParentPlayer.GetSlotIndex(Card));

            return;
        }

        if (FocusedCard != null && Phase == TurnPhase.UseCard && Card.ParentPlayer.isPlayer)
        {
            Phase = TurnPhase.WaitingForAnswer;
            Client.Instance.SendReplaceCard(GetPlayer("Player").GetSlotIndex(Card));
        }
        else if(Phase == TurnPhase.AbilityAction)
        {
            switch(CurrentAbility)
            {
                
                case UniqueAbility.Peek:
                    {
                            if(!Card.ParentPlayer.isPlayer)
                            {
                                ShockMessage.CallMessage("Can only peek on your own cards...", Color.red);
                                Card.BlinkRed();
                                return;
                            }


                            Phase = TurnPhase.WaitingForAnswer;

                            Client.Instance.SendPeekOnCard(Card.ParentPlayer.pName, Card.ParentPlayer.GetSlotIndex(Card));

                            break;
                    }
                case UniqueAbility.PeekOnOther:
                    {
                        if (Card.ParentPlayer.isPlayer)
                        {
                            ShockMessage.CallMessage("Can only peek on other's cards...", Color.red);
                            Card.BlinkRed();
                            return;
                        }

                        Phase = TurnPhase.WaitingForAnswer;

                        Client.Instance.SendPeekOnCard(Card.ParentPlayer.pName, Card.ParentPlayer.GetSlotIndex(Card));

                        break;
                    }
                case UniqueAbility.SwapBlindly:
                    {
                        if(InteractionCardA == null)
                        {
                            InteractionCardA = Card;
                            InteractionCardA.Select();

                            UnsetMarkers();

                            foreach (PlayerModule pPlayer in Players)
                            {
                                if(InteractionCardA.ParentPlayer == pPlayer)
                                {
                                    continue;
                                }

                                foreach (CardSlot slot in pPlayer.CardSlots)
                                {
                                    if (slot.Card != null)
                                    {
                                        SetMarker(slot.Card.transform);
                                    }
                                }
                            }

                            return;
                        }
                        else if (InteractionCardB == null)
                        {
                            if(InteractionCardA.ParentPlayer == Card.ParentPlayer)
                            {
                                return;
                            }

                            InteractionCardB = Card;
                            InteractionCardB.Select();

                            Phase = TurnPhase.WaitingForAnswer;

                            Client.Instance.SendSwapBlindly(
                                InteractionCardA.ParentPlayer.pName,
                                InteractionCardA.ParentPlayer.GetSlotIndex(InteractionCardA),
                                InteractionCardB.ParentPlayer.pName,
                                InteractionCardB.ParentPlayer.GetSlotIndex(InteractionCardB)
                                );


                            break;
                        }

                        Debug.LogError("This is not supposed to happen!");
                        Card.BlinkRed();
                        break;
                    }
                case UniqueAbility.PeekAndSwap:
                    {
                        if (InteractionCardA == null)
                        {
                            InteractionCardA = Card;
                            InteractionCardA.Select();

                            UnsetMarkers();

                            foreach (PlayerModule pPlayer in Players)
                            {
                                if (InteractionCardA.ParentPlayer == pPlayer)
                                {
                                    continue;
                                }

                                foreach (CardSlot slot in pPlayer.CardSlots)
                                {
                                    if (slot.Card != null)
                                    {
                                        SetMarker(slot.Card.transform);
                                    }
                                }
                            }
                            return;
                        }
                        else if (InteractionCardB == null)
                        {
                            if (InteractionCardA.ParentPlayer == Card.ParentPlayer)
                            {
                                return;
                            }

                            InteractionCardB = Card;
                            InteractionCardB.Select();


                            Phase = TurnPhase.WaitingForAnswer;

                            Client.Instance.SendPeekAndSwap(
                                InteractionCardA.ParentPlayer.pName,
                                InteractionCardA.ParentPlayer.GetSlotIndex(InteractionCardA),
                                InteractionCardB.ParentPlayer.pName,
                                InteractionCardB.ParentPlayer.GetSlotIndex(InteractionCardB)
                                );

                            break;
                        }

                        Debug.LogError("This is not supposed to happen!");
                        Card.BlinkRed();
                        break;
                    }
            }
 
            CurrentAbility = UniqueAbility.Nothing;
        }
        else
        {
            if (DumpPile.GetTopCard() != null && doubleClick)
            {
                if (CanStick)
                {
                    SetCardSticked(Card);
                    Client.Instance.SendStickCard("Player", Card.ParentPlayer.pName, Card.ParentPlayer.GetSlotIndex(Card));
                }
            }
            else
            {
                Card.BlinkRed();
            }
        }

    }

    public void InteractWithDumpPile()
    {
        //Stick mode or cant interact
        if (!CanInteract || !string.IsNullOrEmpty(CurrentSticker))
        {
            return;
        }

        if (Phase == TurnPhase.WaitingForAnswer)
        {
            ShockMessage.CallMessage("Please Wait...", Color.red);
            DumpPile.BlinkRed();
            return;
        }

        if (FocusedCard != null && Phase == TurnPhase.UseCard)
        {

            if (FocusedCard.Data.LastLocation.GetType() == typeof(DumpPileUI))
            {
                ShockMessage.CallMessage("Can't return a card to the dump pile!", Color.red);
                DumpPile.BlinkRed();
                return;
            }

            Client.Instance.SendDumpCard();
            Phase = TurnPhase.WaitingForAnswer;
            return;
        }

        if (Phase == TurnPhase.OtherPlayer)
        {
            ShockMessage.CallMessage("This is not your turn!", Color.red);
            DumpPile.BlinkRed();
            return;
        }
        else if (Phase == TurnPhase.TurnEnd)
        {
            ShockMessage.CallMessage("You have got nothing to dump!", Color.red);
            DumpPile.BlinkRed();
            return;
        }

        Phase = TurnPhase.WaitingForAnswer;
        Client.Instance.SendDrawCard(1);
    }

    public void DeclareVictory()
    {
        if (Phase == TurnPhase.WaitingForAnswer)
        {
            ShockMessage.CallMessage("Please Wait...", Color.red);
            return;
        }

        if (Phase != TurnPhase.DrawCard)
        {
            ShockMessage.CallMessage("Can only declare at the start of your turn!");
            return;
        }

        if(VictoryRound)
        {
            ShockMessage.CallMessage("Victory Was Already Declared...");
            return;
        }
        
        Client.Instance.SendDeclareVictory();
        AudioControl.Instance.Play("sound_boobap");
    }

    public void SetCardSelectionResult(int res)
    {
        if (!CanInteract)
        {
            return;
        }

        CardSelectionResult = res;
    }

    #endregion

    #region Events

    public void PlayerDrawCard(string playerName, int Pile, CardData card = null)
    {
        PlayerModule player = GetPlayer(playerName);

        if (player.isPlayer)
        {
            Phase = TurnPhase.UseCard;

            StartCoroutine(DrawCardRoutine(Pile, player, card));
        }
        else
        {
            OtherPlayerDrawCard(player, Pile);
        }
    }

    public void PlayerReplaceCard(string playerName,int slotIndex, CardData replacedCard, CardData focusedCard = null)
    {
        PlayerModule player = GetPlayer(playerName);

        StartCoroutine(ReplaceCardRoutine(replacedCard, slotIndex, player));

        if (player.isPlayer)
        {
            Phase = TurnPhase.TurnEnd;
        }
    }
    
    public void PlayerDumpCard(string playerName, CardData dumpedCard)
    {
        PlayerModule player = GetPlayer(playerName);

        FocusedCard.SetInfo(dumpedCard);

        StartCoroutine(PlayerDumpCardRoutine(player));

        if (player.isPlayer)
        {
            Phase = TurnPhase.TurnEnd;
        }
    }

    public void OtherPlayerDrawCard(PlayerModule player, int Pile)
    {
        StartCoroutine(DrawCardRoutine(Pile, player, null));

    }

    public void PlayerDeclareVictory(string playerName)
    {
        ShockMessage.CallMessage(playerName + " Declares Victory!", Color.green);
        VictoryRound = true;
    }

    public void GameOver(List<PlayerData> playersData, int victorIndex)
    {
        StartCoroutine(GameOverRoutine(playersData, victorIndex));
    }

    public void ActivateUniqueAbility(string playerName, UniqueAbility ability)
    {
        SetMessage(playerName + " Activated Ability " + ability.ToString());

        GameObject effect = ResourcesLoader.Instance.GetRecycledObject("UniqueAbilityParticles");

        effect.transform.position = DumpPile.TopCard.transform.position;
        effect.transform.rotation = DumpPile.TopCard.transform.rotation;

        AudioControl.Instance.Play("sound_reward3");

        PlayerModule player = GetPlayer(playerName);
        if (player.isPlayer)
        {
            Phase = TurnPhase.AbilityAction;
            CurrentAbility = ability;

            UnsetMarkers();

            switch(CurrentAbility)
            {
                case UniqueAbility.Peek :
                    {
                        foreach (CardSlot slot in player.CardSlots)
                        {
                            if (slot.Card != null)
                            {
                                SetMarker(slot.Card.transform);
                            }
                        }

                        break;
                    }
                case UniqueAbility.PeekOnOther:
                    {
                        foreach (PlayerModule pPlayer in Players)
                        {
                            if(pPlayer == player)
                            {
                                continue;
                            }

                            foreach (CardSlot slot in pPlayer.CardSlots)
                            {
                                if (slot.Card != null)
                                {
                                    SetMarker(slot.Card.transform);
                                }
                            }
                        }

                        break;
                    }
                case UniqueAbility.SwapBlindly:
                    {
                        foreach (PlayerModule pPlayer in Players)
                        {
                            foreach (CardSlot slot in pPlayer.CardSlots)
                            {
                                if (slot.Card != null)
                                {
                                    SetMarker(slot.Card.transform);
                                }
                            }
                        }

                        break;
                    }
                case UniqueAbility.PeekAndSwap:
                    {
                        foreach (PlayerModule pPlayer in Players)
                        {
                            foreach (CardSlot slot in pPlayer.CardSlots)
                            {
                                if (slot.Card != null)
                                {
                                    SetMarker(slot.Card.transform);
                                }
                            }
                        }

                        break;
                    }
            }
        }
    }

    internal void PlayerPeeksOnCard(string playerName, int slotIndex, CardData card = null)
    {
        PlayerModule player = GetPlayer(playerName);

        if (player.isPlayer)
        {
            Phase = TurnPhase.TurnEnd;
        }

        StartCoroutine(PlayerPeekOnCard(player, slotIndex, card));
    }

    public void PlayerSwapBlindly(string actingPlayer, string playerA, int indexA, string playerB, int indexB)
    {
        CardSlot SlotA = GetPlayer(playerA).CardSlots[indexA];
        CardSlot SlotB = GetPlayer(playerB).CardSlots[indexB];

        PlayerModule player = GetPlayer(actingPlayer);

        if (player.isPlayer)
        {
            Phase = TurnPhase.TurnEnd;
        }

        StartCoroutine(PlayerSwapBlindlyRoutine(SlotA, SlotB));
    }

    public void PlayerPeekAndSwapPeek(string actionPlayer,string playerA, int indexA, string playerB, int indexB, CardData CardA = null, CardData CardB = null)
    {
        CardSlot SlotA = GetPlayer(playerA).CardSlots[indexA];
        CardSlot SlotB = GetPlayer(playerB).CardSlots[indexB];
        SlotA.Card.SetInfo(CardA);
        SlotB.Card.SetInfo(CardB);

        PlayerModule player = GetPlayer(actionPlayer);

        if (player.isPlayer)
        {
            Phase = TurnPhase.TurnEnd;
        }

        StartCoroutine(PlayerPeekAndSwapPeekRoutine(player, SlotA, SlotB));
    }


    internal void PlayerSticksCardCorrect(string sticker, string sticked, int slotIndex, CardData card)
    {
        CurrentSticker = sticker;
        m_CurrentSticked = sticked;
        UnsetCardStickedEffect();

        StartCoroutine(PlayerSticksCardCorrectRoutine(GetPlayer(sticker), GetPlayer(sticked), GetPlayer(sticked).CardSlots[slotIndex], card));
    }

    internal void PlayerSticksCardIncorrect(string sticker, string sticked, int slotIndex, CardData card)
    {
        CurrentSticker = sticker;
        m_CurrentSticked = sticked;
        UnsetCardStickedEffect();

        StartCoroutine(PlayerSticksCardIncorrectRoutine(GetPlayer(sticker), GetPlayer(sticked), GetPlayer(sticked).CardSlots[slotIndex], card));
    }

    public void SetPileState(int pileID, int pileState)
    {
        if(pileID == 0)
        {
            GamePile.SetPileState(pileState);
        }
    }

    public void PlayerGaveCardPenalty(string fromPlayer, string toPlayer, int fromIndex, int toIndex)
    {
        PlayerModule giver = GetPlayer(fromPlayer);
        PlayerModule taker = GetPlayer(toPlayer);

        StartCoroutine(PlayerGaveCardPenaltyRoutine(giver, taker, fromIndex, toIndex));
    }

    public void PlayerLost(PlayerData losingPlayer)
    {
        SetMessage(losingPlayer + " has lost!");

        PlayerModule player = GetPlayer(losingPlayer.pName);

        for(int i=0;i<player.CardSlots.Count;i++)
        {
            if(player.CardSlots[i].Card != null)
            {
                player.CardSlots[i].Card.SetInfo(losingPlayer.Slots[i]);
                player.CardSlots[i].Card.FaceUp();
            }
        }

        player.hasLost = true;
    }

    #endregion


    public PlayerModule GetPlayer(string sName)
    {
        for(int i=0;i<Players.Count;i++)
        {
            if(Players[i].pName == sName)
            {
                return Players[i];
            }
        }

        return null;
    }

    void DestroyFocusedCard()
    {
        if(FocusedCard == null)
        {
            return;
        }

        if(FocusedCard.transform.parent != null)
        {
            FocusedCard.transform.SetParent(null);
        }

        DestroyCard(FocusedCard);
        FocusedCard = null;
    }

    void DestroyCard(CardUI Card)
    {
        if(Card == null)
        {
            return;
        }

        Card.SetInteractable();
        Card.gameObject.SetActive(false);
    }

    public void SetGameSpeed(int Speed)
    {
        Time.timeScale = Speed;
    }

    #region ActionHandling

    public void RegisterAction(Action action)
    {
        ActionQue.Add(action);

        //string content = "";
        //for (int i = 0; i < ActionQue.Count; i++)
        //{
        //    content += ActionQue[i].Target.GetType().ToString() + " | ";
        //}

        //if (!String.IsNullOrEmpty(content))
        //{
        //    Debug.Log("QUE -" + content);
        //}

        if (!isBusy)
        {
            HandleNextAction();
        }
    }

    public void HandleNextAction()
    {
        if(ActionQue.Count == 0)
        {
            return;
        }

        LastHandledAction = ActionQue[0].Target.GetType().ToString();
        ActionQue[0].Invoke();
        ActionQue.RemoveAt(0);
    }

    public void LerpTurnGlow(PlayerModule player)
    {
        if(TurnGlowCoroutineInstnace != null)
        {
            StopCoroutine(TurnGlowCoroutineInstnace);
        }

        TurnGlowCoroutineInstnace = StartCoroutine(TurnGlowLerpRoutine(player));
    }

    public void SetMarker(Transform target)
    {
        GameObject tempMarker = ResourcesLoader.Instance.GetRecycledObject("MarkerObject");

        tempMarker.transform.position = target.position;
        tempMarker.transform.rotation = target.rotation;

        markers.Add(tempMarker);
    }

    public void UnsetMarkers()
    {
        while (markers.Count > 0)
        {
            markers[0].gameObject.SetActive(false);
            markers.RemoveAt(0);
        }
    }

    #endregion

    IEnumerator NewGameRoutine(CardData leftCard, CardData rightCard)
    {
        isBusy = true;

        yield return 0;

        ResetGame();

        yield return 0;

        for(int i=0;i<Players.Count;i++)
        {
            for(int c=0;c<STARTING_CARDS;c++)
            {
                yield return StartCoroutine(GiveEmptyCard(Players[i].CardSlots[c]));
            }
        }

        Players[0].CardSlots[1].Card.SetInfo(rightCard);
        Players[0].CardSlots[3].Card.SetInfo(leftCard);

        yield return new WaitForSeconds(1f);

        GameCamera.Instance.FocusOn(
            Vector3.Lerp(Players[0].CardSlots[1].Card.transform.position,
            Players[0].CardSlots[3].Card.transform.position, 0.5f));

        Players[0].CardSlots[1].Card.FaceUp();
        Players[0].CardSlots[3].Card.FaceUp();

        AudioControl.Instance.PlayInPosition("fp_flip_cards_face", Players[0].CardSlots[1].Card.transform.position);
        yield return new WaitForSeconds(2f);

        Players[0].CardSlots[1].Card.FaceDown();
        Players[0].CardSlots[3].Card.FaceDown();

        yield return new WaitForSeconds(1f);

        GameCamera.Instance.FocusOff();

        PlayerReady = false;

        yield return StartCoroutine(ShowRulesBrief());

        if(TutorialMode)
        {
            GuideManager.Instance.TryTeach(1);
        }

        Client.Instance.SendReady();

        yield return 0;

        isBusy = false;
    }

    IEnumerator ShowRulesBrief()
    {
        StageRules stageRules = Rules.GetStageRules(PlayerInfo.SelectedStage);

        if(stageRules == null || string.IsNullOrEmpty(stageRules.briefPrefab))
        {
            yield break;
        }

        GameObject rulesPrefab = ResourcesLoader.Instance.GetRecycledObject(stageRules.briefPrefab);
        
        while(!PlayerReady)
        {
            yield return 0;
        }

        rulesPrefab.gameObject.SetActive(false);
    }

    IEnumerator GiveEmptyCard(CardSlot slot)
    {
        isBusy = true;

        yield return 0;

        GameObject card = GamePile.PullCard();

        AudioControl.Instance.PlayInPosition("fp_deal_cards_justwhoosh", card.transform.position);
        yield return new WaitForSeconds(0.1f);

        slot.Card = card.GetComponent<CardUI>();
        slot.Card.Align(slot.transform);

        yield return new WaitForSeconds(0.1f);

        yield return 0;

        isBusy = false;

    }

    IEnumerator DrawCardRoutine(int Pile, PlayerModule player, CardData card = null)
    {
        isBusy = true;

        yield return 0;

        if (Pile == 0)
        {
            FocusedCard = GamePile.PullCard(card).GetComponent<CardUI>();
        }
        else if (Pile == 1)
        {
            FocusedCard = DumpPile.PullCard().GetComponent<CardUI>();
        }

        yield return 0;

        if (player.isPlayer)
        {
            FocusedCard.transform.SetParent(GameCamera.Instance.FocusSpot);
            FocusedCard.Align(GameCamera.Instance.FocusSpot, null, false);
        }
        else
        {
            FocusedCard.Align(player.ViewSpot, player, true, false);
        }

        AudioControl.Instance.PlayInPosition("fp_deal_cards_justwhoosh", FocusedCard.transform.position);
        FocusedCard.SetUninteractable();

        while(FocusedCard != null && FocusedCard.isLerping)
        {
            yield return 0;
        }


        UnsetMarkers();

        if (player.isPlayer)
        {
            foreach(CardSlot slot in player.CardSlots)
            {
                if(slot.Card != null)
                {
                    SetMarker(slot.Card.transform);
                }
            }
        }

        if (TutorialMode)
        {
            GuideManager.Instance.TryTeach(2);
        }

        yield return 0;
            
        isBusy = false;
    }

    IEnumerator ReplaceCardRoutine(CardData replacedCard, int slotIndex, PlayerModule player)
    {
        isBusy = true;

        CardUI cardSet = FocusedCard;

        CardUI dumpedCard = player.CardSlots[slotIndex].Card;

        player.CardSlots[slotIndex].GenerateCard(cardSet.Data);
        player.CardSlots[slotIndex].Card.transform.position = FocusedCard.transform.position;
        player.CardSlots[slotIndex].Card.transform.rotation = FocusedCard.transform.rotation;
        player.CardSlots[slotIndex].Card.Align(player.CardSlots[slotIndex].transform, null, false);

        dumpedCard.SetInfo(replacedCard);


        DestroyFocusedCard();

        UnsetMarkers();

        if (!player.isPlayer)
        {
            dumpedCard.Align(player.ViewSpot, player);
            
            yield return new WaitForSeconds(0.5f);
        }

        dumpedCard.Align(DumpPile.AirPoint, player);

      
        yield return new WaitForSeconds(1f);

        AudioControl.Instance.PlayInPosition("fp_flip_cards_face", dumpedCard.transform.position);
        dumpedCard.Align(DumpPile.TopCard.transform, player);

        while (dumpedCard.isLerping)
        {
            yield return 0;
        }

        DumpPile.PushCard(dumpedCard.Data);
        dumpedCard.gameObject.SetActive(false);

        isBusy = false;
    }

    IEnumerator PlayerDumpCardRoutine(PlayerModule player)
    {
        isBusy = true;

        CardUI cardSet = FocusedCard;

        cardSet.Align(DumpPile.AirPoint, player);

        UnsetMarkers();

        yield return new WaitForSeconds(1f);

        cardSet.Align(DumpPile.TopCard.transform, player);

        while (cardSet.isLerping)
        {
            yield return 0;
        }

        AudioControl.Instance.PlayInPosition("fp_flip_cards_color", cardSet.transform.position);
        DumpPile.PushCard(cardSet.Data);

        DestroyCard(cardSet);

        isBusy = false;
    }

    IEnumerator GameOverRoutine(List<PlayerData> playersData, int victorIndex)
    {
        isBusy = true;

        PlayerModule player;
        for (int i = 0; i < playersData.Count; i++)
        {
            player = GetPlayer(playersData[i].pName);
            for (int c = 0; c < playersData[i].Slots.Count; c++)
            {
                if(player.CardSlots[c].Card == null)
                {
                    continue;
                }

                while (player.CardSlots[c].Card.isLerping)
                {
                    yield return 0;
                }

                player.CardSlots[c].Card.SetInfo(playersData[i].Slots[c]);
                player.CardSlots[c].Card.FaceUp();

                AudioControl.Instance.PlayInPosition("fp_deal_cards_justwhoosh", player.CardSlots[c].Card.transform.position);

                yield return new WaitForSeconds(0.05f);
            }

            player.ShowEndGameScore();

            yield return new WaitForSeconds(0.05f);

        }

        ShockMessage.CallMessage("The winner is " + playersData[victorIndex].pName + "!");

        yield return new WaitForSeconds(3f);

        if (GetPlayer(playersData[victorIndex].pName).isPlayer && PlayerInfo.CurrentStage == PlayerInfo.SelectedStage && PlayerInfo.CurrentStage != PlayerInfo.LastStage)
        {
            PlayerInfo.CurrentStage++;

            ShockMessage.CallMessage("Congratulations! You have unlocked stage " + (PlayerInfo.CurrentStage + 1) + "!");

            yield return new WaitForSeconds(2f);

            SceneManager.LoadScene(0);
        }

        for (int i = 0; i < Players.Count; i++)
        {
            for (int c = 0; c < Players[i].CardSlots.Count; c++)
            {
                if (Players[i].CardSlots[c].Card != null)
                {
                    Players[i].CardSlots[c].Card.FaceDown();
                    
                }

                yield return new WaitForSeconds(0.05f);
            }

            Players[i].HideEndGameScore();

            AudioControl.Instance.PlayInPosition("fp_flip_cards_face", Players[i].transform.position);
            yield return new WaitForSeconds(0.05f);
        }

        yield return new WaitForSeconds(1f);

        
        for (int i = 0; i < Players.Count; i++)
        {
            for (int c = 0; c < Players[i].CardSlots.Count; c++)
            {
                if(Players[i].CardSlots[c].Card == null)
                {
                    continue;
                }

                Players[i].CardSlots[c].Card.Align(GamePile.transform);

                yield return new WaitForSeconds(0.01f);
            }

            yield return new WaitForSeconds(0.01f);
        }

        AudioControl.Instance.PlayInPosition("cardsdrop", GamePile.transform.position);

        yield return new WaitForSeconds(1f);

        Client.Instance.SendReady();

        isBusy = false;
    }
    
    IEnumerator PlayerPeekOnCard(PlayerModule player, int slotIndex, CardData card)
    {

        UnsetMarkers();

        isBusy = true;

        CardUI Card = player.CardSlots[slotIndex].Card;

        if (Card == null)
        {
            isBusy = false;
            yield break;
        }

        Card.SetInfo(card);

        Card.SetSortingLayer(0, "FocusCard");

        AudioControl.Instance.PlayInPosition("fp_deal_cards_justwhoosh", Card.transform.position);
        if (CurrentPlayer.isPlayer)
        {
            Card.Align(GameCamera.Instance.FocusSpot, null, false);
        }
        else
        {
            Card.Align(CurrentPlayer.ViewSpot, CurrentPlayer, true, false);
        }

        yield return new WaitForSeconds(3f);

        Card.Align(player.CardSlots[slotIndex].transform, player);

        Card.SetSortingLayer(0, "Card");

        while (Card.isLerping)
        {
            yield return 0;
        }

        AudioControl.Instance.PlayInPosition("fp_flip_cards_color", Card.transform.position);

        isBusy = false;
    }

    IEnumerator PlayerSwapBlindlyRoutine(CardSlot SlotA, CardSlot SlotB)
    {
        isBusy = true;

        UnsetMarkers();

        yield return new WaitForSeconds(0.5f);

        if (InteractionCardA != null)
        {
            InteractionCardA.Deselect();
            InteractionCardB.Deselect();

            InteractionCardA = null;
            InteractionCardB = null;
        }

        if (SlotA.Card == null || SlotB.Card == null)
        {
            isBusy = false;
            yield break;
        }

        CardUI StashedCard = SlotA.Card;
        SlotA.Card = SlotB.Card;
        SlotB.Card = StashedCard;

        SlotA.Card.Align(SlotA.transform);
        SlotB.Card.Align(SlotB.transform);

        AudioControl.Instance.PlayInPosition("fp_deal_cards_justwhoosh", SlotA.transform.position);

        yield return new WaitForSeconds(0.1f);

        AudioControl.Instance.PlayInPosition("fp_deal_cards_justwhoosh", SlotB.transform.position);

        while (SlotA.Card.isLerping)
        {
            yield return 0;
        }

        AudioControl.Instance.PlayInPosition("fp_choose_bet", SlotB.transform.position);

        isBusy = false;
    }

    IEnumerator PlayerPeekAndSwapPeekRoutine(PlayerModule actionPlayer, CardSlot SlotA, CardSlot SlotB)
    {
        isBusy = true;

        UnsetMarkers();

        yield return new WaitForSeconds(0.5f);

        if (actionPlayer.isPlayer)
        {
            if (InteractionCardA != null)
            {
                InteractionCardA.Deselect();
                InteractionCardB.Deselect();
            }

            if (SlotA.Card == null || SlotB.Card == null)
            {
                isBusy = false;
                yield break;
            }

            SlotA.Card.SetSortingLayer(0, "FocusCard");
            SlotB.Card.SetSortingLayer(0, "FocusCard");

            SlotA.Card.Align(GameCamera.Instance.FocusSpot, actionPlayer, true, false);
            SlotB.Card.Align(GameCamera.Instance.FocusSpot2, actionPlayer, true, false);

            CardSelectionResult = 0;

            PickBetweenCardsWindow.gameObject.SetActive(true);
            m_txtPickCardsPlayerA.text = SlotA.ParentPlayer.pName;
            m_txtPickCardsPlayerB.text = SlotB.ParentPlayer.pName;

            AudioControl.Instance.PlayInPosition("fp_deal_cards_justwhoosh", SlotA.transform.position);

            yield return new WaitForSeconds(0.1f);

            AudioControl.Instance.PlayInPosition("fp_deal_cards_justwhoosh", SlotB.transform.position);


            while (PickBetweenCardsWindow.alpha < 1f)
            {
                PickBetweenCardsWindow.alpha += 1f * Time.deltaTime;

                yield return 0;
            }


            while (CardSelectionResult == 0)
            {
                yield return 0;
            }

            Phase = TurnPhase.WaitingForAnswer;

            InteractionCardA = null;
            InteractionCardB = null;
        
            Client.Instance.SendPeekSwapCardResult(
                SlotA.ParentPlayer.pName,
                SlotA.ParentPlayer.GetSlotIndex(SlotA.Card),
                SlotB.ParentPlayer.pName,
                SlotB.ParentPlayer.GetSlotIndex(SlotB.Card),
                CardSelectionResult);

            SlotA.Card.Align(SlotA.transform, actionPlayer);
            SlotB.Card.Align(SlotB.transform, actionPlayer);

            while (PickBetweenCardsWindow.alpha > 0f)
            {
                PickBetweenCardsWindow.alpha -= 1f * Time.deltaTime;

                yield return 0;
            }

            PickBetweenCardsWindow.gameObject.SetActive(false);
        }
        else
        {
            SlotA.Card.Align(actionPlayer.ViewSpot, actionPlayer);
            SlotB.Card.Align(actionPlayer.ViewSpot, actionPlayer);

            yield return new WaitForSeconds(3f);

            SlotA.Card.Align(SlotA.transform, actionPlayer);
            SlotB.Card.Align(SlotB.transform, actionPlayer);
        }

        SlotA.Card.SetSortingLayer(0, "Card");
        SlotB.Card.SetSortingLayer(0, "Card");

        while (SlotA.Card.isLerping)
        {
            yield return 0;
        }

        AudioControl.Instance.PlayInPosition("fp_choose_bet", SlotA.transform.position);

        yield return new WaitForSeconds(0.1f);

        AudioControl.Instance.PlayInPosition("fp_choose_bet", SlotB.transform.position);


        isBusy = false;
    }

    IEnumerator PlayerSticksCardCorrectRoutine(PlayerModule player1, PlayerModule player2, CardSlot cardSlot, CardData card)
    {
        isBusy = true;

        yield return 0;

        cardSlot.Card.SetInfo(card);
        cardSlot.Card.Align(DumpPile.TopCard.transform, player1);

        AudioControl.Instance.PlayInPosition("fp_deal_cards_justwhoosh", cardSlot.transform.position);

        while (cardSlot.Card.isLerping)
        {
            yield return 0;
        }

        DumpPile.PushCard(cardSlot.Card.Data);

        DestroyCard(cardSlot.Card);
        cardSlot.Card = null;

        GameObject particlesObj = ResourcesLoader.Instance.GetRecycledObject("GoodParticles");
        particlesObj.transform.position = DumpPile.transform.position;

        AudioControl.Instance.PlayInPosition("fp_play_button", particlesObj.transform.position);

        yield return new WaitForSeconds(1f);

        yield return 0;

        if (player1 == player2 || (player1 != player2 && player1.GetNextMannedSlot() == null))
        {
            CurrentSticker = "";
        }
        else
        {

            UnsetMarkers();

            if (player1.isPlayer)
            {
                foreach (CardSlot slot in player1.CardSlots)
                {
                    if (slot.Card != null)
                    {
                        SetMarker(slot.Card.transform);
                    }
                }
            }
        }

        isBusy = false;
    }

    IEnumerator PlayerSticksCardIncorrectRoutine(PlayerModule player1, PlayerModule player2, CardSlot cardSlot, CardData card)
    {
        isBusy = true;

        yield return 0;

        cardSlot.Card.SetInfo(card);
        cardSlot.Card.Align(DumpPile.TopCard.transform, player1);

        AudioControl.Instance.PlayInPosition("fp_deal_cards_justwhoosh", cardSlot.transform.position);

        while (cardSlot.Card.isLerping)
        {
            yield return 0;
        }

        AudioControl.Instance.PlayInPosition("fp_lose_bet", cardSlot.transform.position);

        yield return 0;

        cardSlot.Card.Align(cardSlot.transform, player1);

        RegisterAction( () => { StartCoroutine(GiveEmptyCard(player1.GetNextEmptySlot())); });

        GameObject particlesObj = ResourcesLoader.Instance.GetRecycledObject("BadParticles");
        particlesObj.transform.position = DumpPile.transform.position;
        cardSlot.Card.BlinkRed(3);

        yield return new WaitForSeconds(1f);

        while (cardSlot.Card.isLerping)
        {
            yield return 0;
        }

        yield return 0;

        CurrentSticker = "";

        isBusy = false;
    }

    IEnumerator TurnGlowLerpRoutine(PlayerModule player)
    {
        float t = 0f;
        while(t<1f)
        {
            t += 1f * Time.deltaTime;

            turnQuad.transform.position = Vector3.Lerp(turnQuad.transform.position, player.transform.position, t);

            yield return 0;
        }

        TurnGlowCoroutineInstnace = null;
    }

    IEnumerator PlayerGaveCardPenaltyRoutine(PlayerModule from, PlayerModule to, int fromIndex, int toIndex)
    {
        isBusy = true;

        UnsetMarkers();

        CardSlot slotFrom = from.CardSlots[fromIndex];
        CardSlot slotTo = to.CardSlots[toIndex];

        slotFrom.Card.Align(slotTo.transform, from);
        slotTo.Card = slotFrom.Card;
        slotFrom.Card = null;

        AudioControl.Instance.PlayInPosition("fp_deal_cards_justwhoosh", slotFrom.transform.position);

        while (slotTo.Card.isLerping)
        {
            yield return 0;
        }

        AudioControl.Instance.PlayInPosition("fp_choose_bet", slotTo.transform.position);
        CurrentSticker = "";
        isBusy = false;
    }
    #region Utility

    public static Vector3 SplineLerp(Vector3 source, Vector3 target, float Height, float t)
    {
        Vector3 ST = new Vector3(source.x, source.y + Height, source.z);
        Vector3 TT = new Vector3(target.x, target.y + Height, target.z);

        Vector3 STTTM = Vector3.Lerp(ST, TT, t);

        Vector3 STM = Vector3.Lerp(source, ST, t);
        Vector3 TTM = Vector3.Lerp(TT, target, t);

        Vector3 SplineST = Vector3.Lerp(STM, STTTM, t);
        Vector3 SplineTM = Vector3.Lerp(STTTM, TTM, t);

        return Vector3.Lerp(SplineST, SplineTM, t);
    }

    #endregion

}
