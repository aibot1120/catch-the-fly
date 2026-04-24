using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatchTheFly.Core
{
    /// <summary>
    /// 遊戲狀態
    /// </summary>
    public enum GameState
    {
        Menu,
        Dealing,
        Playing,
        RoundEnd,
        GameOver
    }

    /// <summary>
    /// 結算結果
    /// </summary>
    public enum RoundResult
    {
        None,
        SelfDraw,      // 自摸
        GunOut,        // 出銃（對手被銃）
        FourOfAKind,   // 四張同點自摸
        DiscardWin     // 打出致勝牌
    }

    /// <summary>
    /// 遊戲主控制器
    /// MVP 版本
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; } = GameState.Menu;
        public List<Player> Players { get; private set; } = new List<Player>();
        public int CurrentPlayerIndex { get; private set; }
        public int DealerIndex { get; private set; }
        public int Direction = 1; // 1=順時針, -1=逆時針
        public long WinScore = 500;

        // 公共資訊
        public Card TopCard => CardDeck.Instance?.GetTopDiscard();
        public int PublicValue => TopCard?.Value ?? 0;

        // 事件
        public Action<int, Card> OnCardPlayed;
        public Action<Player, RoundResult, int> OnRoundEnd;
        public Action<int> OnTurnStart;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// 開始新遊戲
        /// </summary>
        public void StartNewGame(int playerCount = 4)
        {
            Players.Clear();

            // 建立玩家
            for (int i = 0; i < playerCount; i++)
            {
                Players.Add(new Player
                {
                    Id = i,
                    Name = i == 0 ? "你" : $"玩家{i + 1}",
                    IsAI = i > 0,
                    Score = 0
                });
            }

            // 隨機莊家
            System.Random rng = new System.Random();
            DealerIndex = rng.Next(playerCount);
            CurrentPlayerIndex = DealerIndex;

            State = GameState.Dealing;
            StartNewRound();
        }

        /// <summary>
        /// 開始新一回合
        /// </summary>
        public void StartNewRound()
        {
            // 清除手牌
            foreach (var p in Players)
                p.HandCards.Clear();

            // 初始化牌堆
            CardDeck.Instance.Initialize();
            CardDeck.Instance.Shuffle();

            // 發牌（每人7張）
            for (int i = 0; i < 7 * Players.Count; i++)
            {
                var card = CardDeck.Instance.Draw();
                if (card != null)
                    Players[i % Players.Count].HandCards.Add(card);
            }

            // 翻開第一張
            var first = CardDeck.Instance.Draw();
            if (first != null)
                CardDeck.Instance.Discard(first);

            State = GameState.Playing;
            OnTurnStart?.Invoke(CurrentPlayerIndex);
        }

        /// <summary>
        /// 玩家出一張牌
        /// </summary>
        public bool PlayCard(int playerIndex, Card card)
        {
            if (State != GameState.Playing) return false;
            if (CurrentPlayerIndex != playerIndex) return false;

            var player = Players[playerIndex];
            if (!player.HandCards.Contains(card)) return false;

            // 檢查是否能出（數字匹配或功能牌）
            if (!CanPlayCard(card))
            {
                Debug.Log($"[GameManager] Cannot play card {card.DisplayName}");
                return false;
            }

            // 移除並棄牌
            player.HandCards.Remove(card);
            CardDeck.Instance.Discard(card);

            // 檢查烏蠅牌（強制出烏蠅）
            // （已在 UI 層強制）

            // 處理功能牌
            if (card.Type == CardType.Reverse)
                Direction *= -1;
            else if (card.Type == CardType.Skip)
                AdvanceTurn(); // 跳過下一家（advance兩次）
            else if (card.Type == CardType.DrawTwo)
            {
                int nextIdx = GetNextPlayer();
                DrawCards(nextIdx, 2);
            }

            OnCardPlayed?.Invoke(playerIndex, card);

            // 檢查勝出
            if (player.HandCards.Count == 0)
            {
                EndRound(player, RoundResult.DiscardWin);
                return true;
            }

            AdvanceTurn();
            return true;
        }

        /// <summary>
        /// 玩家抽一張牌
        /// </summary>
        public Card DrawCard(int playerIndex)
        {
            if (State != GameState.Playing) return null;
            if (CurrentPlayerIndex != playerIndex) return null;

            var card = CardDeck.Instance.Draw();
            if (card == null) return null;

            Players[playerIndex].HandCards.Add(card);

            // 檢查自摸（手上所有牌的點數總和等於公共點數）
            var player = Players[playerIndex];
            if (player.TotalHandValue == PublicValue)
            {
                EndRound(player, RoundResult.SelfDraw);
                return card;
            }

            // 檢查四張同點
            if (HasFourOfAKind(player))
            {
                EndRound(player, RoundResult.FourOfAKind);
                return card;
            }

            return card;
        }

        /// <summary>
        /// 檢查是否可出這張牌
        /// </summary>
        public bool CanPlayCard(Card card)
        {
            if (TopCard == null) return true;

            // 烏蠅牌可出任何時候
            if (card.Type == CardType.Fly) return true;

            // 數字匹配
            if (card.Type == CardType.Number && TopCard.Type == CardType.Number)
                return card.Value == TopCard.Value;

            // 顏色匹配
            if (TopCard.Type != CardType.Fly)
                return card.Suit == TopCard.Suit;

            return card.Type == TopCard.Type;
        }

        /// <summary>
        /// 檢查出銃（對手出一張牌，剛好等於自己某張手牌）
        /// </summary>
        public bool CheckGunOut(int shootingPlayerIndex, Card shotCard)
        {
            int nextIdx = GetNextPlayer();
            var nextPlayer = Players[nextIdx];

            // 下一家是否有與 shotCard 同點數的牌
            foreach (var c in nextPlayer.HandCards)
            {
                if (c.Type == CardType.Number && c.Value == shotCard.Value)
                {
                    // 出銃！下一家輸
                    EndRound(Players[shootingPlayerIndex], RoundResult.GunOut);
                    return true;
                }
            }
            return false;
        }

        private void AdvanceTurn()
        {
            CurrentPlayerIndex = GetNextPlayer();
            OnTurnStart?.Invoke(CurrentPlayerIndex);
        }

        private int GetNextPlayer()
        {
            int next = CurrentPlayerIndex + Direction;
            if (next >= Players.Count) next = 0;
            if (next < 0) next = Players.Count - 1;
            return next;
        }

        private void DrawCards(int playerIndex, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var card = CardDeck.Instance.Draw();
                if (card != null)
                    Players[playerIndex].HandCards.Add(card);
            }
        }

        private bool HasFourOfAKind(Player player)
        {
            var counts = new Dictionary<int, int>();
            foreach (var c in player.HandCards)
            {
                if (c.Type == CardType.Number)
                {
                    if (!counts.ContainsKey(c.Value)) counts[c.Value] = 0;
                    counts[c.Value]++;
                    if (counts[c.Value] >= 4) return true;
                }
            }
            return false;
        }

        private void EndRound(Player winner, RoundResult result)
        {
            State = GameState.RoundEnd;

            int loserCards = 0;
            foreach (var p in Players)
                if (p != winner)
                    loserCards += p.HandCards.Count;

            int multiplier = result switch
            {
                RoundResult.FourOfAKind => 4,
                RoundResult.SelfDraw => 2,
                _ => 2
            };

            long points = loserCards * multiplier;
            winner.Score += points;

            Debug.Log($"[GameManager] Round End! Winner: {winner.Name}, Result: {result}, Points: {points}");

            OnRoundEnd?.Invoke(winner, result, (int)points);

            // 檢查遊戲結束
            foreach (var p in Players)
            {
                if (p.Score >= WinScore)
                {
                    State = GameState.GameOver;
                    Debug.Log($"[GameManager] GAME OVER! Winner: {p.Name}");
                    return;
                }
            }

            // 下一局，連莊
            DealerIndex = Players.IndexOf(winner);
            CurrentPlayerIndex = DealerIndex;
        }
    }
}
