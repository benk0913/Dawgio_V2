using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Client : MonoBehaviour {

    [SerializeField]
    MockServer server;

    public static Client Instance;

    [SerializeField]
    bool DebugMode = false;

    private void Awake()
    {
        Instance = this;
    }

    #region Send

    public void SendJoinRoom()
    {
        server.OnJoinRoom();
    }

    public void SendLeaveRoom()
    {
        server.OnLeaveRoom();
    }

    public void SendDrawCard(int pile)
    {
        server.OnDrawCard(pile);
    }

    public void SendReady()
    {
        server.OnPlayersReady();
    }

    public void SendReplaceCard(int SlotIndex)
    {
        server.OnReplaceCard(SlotIndex);
    }

    public void SendDumpCard()
    {
        server.OnDumpCard();
    }

    public void SendDeclareVictory()
    {
        server.OnDeclareVictory("Player");
    }
    
    public void SendPeekOnCard(string playerName, int slotIndex)
    {
        server.OnPeekOnCard(playerName, slotIndex);
    }

    public void SendSwapBlindly(string CardAPlayer, int CardAIndex, string CardBPlayer, int CardBIndex)
    {
        server.OnSwapBlindly(CardAPlayer, CardAIndex, CardBPlayer, CardBIndex);
    }

    public void SendPeekAndSwap(string CardAPlayer, int CardAIndex, string CardBPlayer, int CardBIndex)
    {
        server.OnPeekAndSwap(CardAPlayer, CardAIndex, CardBPlayer, CardBIndex);
    }

    public void SendStickCard(string strickerPlayer, string ownerPlayer, int slotIndex)
    {
        server.OnStickCard(strickerPlayer, ownerPlayer, slotIndex);
    }

    public void SendPeekSwapCardResult(string CardAPlayer, int CardAIndex, string CardBPlayer, int CardBIndex, int CardResult)
    {
        server.OnPeekSwapCardResult(CardAPlayer, CardAIndex, CardBPlayer, CardBIndex, CardResult);
    }

    public void SendGivePlayerPenaltyCard(int slotIndex)
    {
        server.OnPlayerGivePenaltyCard(slotIndex);
    }
    #endregion

    #region Receive

    public void OnJoinRoom(List<string> Players)
    {
        Broadcast("Joined Room ");
        Game.Instance.RegisterAction(() =>
        {
            Game.Instance.SetupRoom(Players);
        });
    }

    public void OnNewGame(CardData leftCard, CardData rightCard)
    {
        Broadcast("New Game");
        Game.Instance.RegisterAction(() =>
        {
            Game.Instance.NewGame(rightCard, leftCard);
        });
    }

    internal void OnPlayerPeekOnCard(string playerName, int slotIndex, CardData card = null)
    {
        Game.Instance.RegisterAction(() =>
        {
            Game.Instance.PlayerPeeksOnCard(playerName, slotIndex, card);
        });
    }

    internal void OnSetTurn(string playerName)
    {
        Broadcast(playerName + "'s turn..");
        Game.Instance.SetTurn(playerName);
    }

    public void OnDrawCard(string playerName, int Pile, CardData card = null)
    {
        Broadcast(playerName + " drew a card...");
        Game.Instance.RegisterAction(() =>
        {
            Game.Instance.PlayerDrawCard(playerName, Pile, card);
        });
    }

    internal void OnReplaceCard(string playerName,int slotIndex, CardData replacedCard)
    {
        Broadcast(playerName + " replaced a card...");
        Game.Instance.PlayerReplaceCard(playerName ,slotIndex ,replacedCard);
    }

    public void OnDumpCard(string playerName, CardData dumpedCard)
    {
        Broadcast(playerName + " Dumped a card...");

        Game.Instance.RegisterAction(() =>
        {
            Game.Instance.PlayerDumpCard(playerName, dumpedCard);
        });
    }

    public void OnPlayerDeclareVictory(string Player)
    {
        Broadcast(Player + " Declared Victory");
        Game.Instance.RegisterAction(() =>
        {
            Game.Instance.PlayerDeclareVictory(Player);
        });
    }

    public void OnGameOver(List<PlayerData> players, int victorIndex)
    {
        Broadcast("Game Over");
        Game.Instance.RegisterAction(() => 
        {
            Game.Instance.GameOver(players, victorIndex);
        });
    }

    public void OnUniqueAbility(string playerName, UniqueAbility ability)
    {
        Broadcast(playerName + " used " + ability.ToString());

        Game.Instance.RegisterAction(() => 
        {
            Game.Instance.ActivateUniqueAbility(playerName, ability);
        });
    }

    public void OnPlayerSwapBlindly(string actionPlayer, string playerA, int indexA, string playerB, int indexB)
    {
        Broadcast(playerA + " card was swapped with " + playerB + "'s card...");

        Game.Instance.RegisterAction(() =>
        {
            Game.Instance.PlayerSwapBlindly(actionPlayer ,playerA, indexA, playerB, indexB);
        });
    }

    public void OnPlayerPeekAndSwapPeek(string actionPlayer, string playerA, int indexA, string playerB, int indexB, CardData CardA = null, CardData CardB = null)
    {
        Broadcast(playerA + " card was swapped with " + playerB + "'s card...");

        Game.Instance.RegisterAction(() =>
        {
            Game.Instance.PlayerPeekAndSwapPeek(actionPlayer, playerA, indexA, playerB, indexB, CardA, CardB);
        });
    }

    public void OnStickCorrect(string sticker, string sticked, int slotIndex, CardData card)
    {
        Broadcast(sticker + " has sticked a card to " + sticked + " correctly...");

        Game.Instance.RegisterAction(() =>
        {
            Game.Instance.PlayerSticksCardCorrect(sticker, sticked, slotIndex, card);
        });
    }

    internal void OnStickWrong(string sticker, string sticked, int slotIndex, CardData card)
    {
        Broadcast(sticker + " has sticked a card to " + sticked + " incorrectly...");

        Game.Instance.RegisterAction(() =>
        {
            Game.Instance.PlayerSticksCardIncorrect(sticker, sticked, slotIndex, card);
        });
    }

    public void OnPileState(int PileID, int PileState)
    {
        Broadcast("Pile " + PileID + "'s State is "+PileState);

        Game.Instance.RegisterAction(() =>
        {
            Game.Instance.SetPileState(PileID, PileState);
        });
    }

    public void OnPlayerGavePenalty(string fromPlayer, string toPlayer, int fromIndex, int toIndex)
    {
        Broadcast(fromPlayer + " gave a card as penalty to "+ toPlayer);

        Game.Instance.RegisterAction(() =>
        {
            Game.Instance.PlayerGaveCardPenalty(fromPlayer, toPlayer, fromIndex, toIndex);
        });
    }

    public void OnPlayerLost(PlayerData losingPlayer)
    {
        Broadcast(losingPlayer.pName + " has lost.");

        Game.Instance.RegisterAction(() =>
        {
            Game.Instance.PlayerLost(losingPlayer);
        });
    }

    #endregion

    void Broadcast(string Message)
    {
        if(DebugMode)
        {
            Debug.Log(Message);
        }
    }
}
