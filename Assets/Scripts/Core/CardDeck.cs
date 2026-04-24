using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatchTheFly.Core
{
    /// <summary>
    /// 卡牌堆管理
    /// </summary>
    public class CardDeck : MonoBehaviour
    {
        public static CardDeck Instance { get; private set; }

        private List<Card> drawPile = new List<Card>();
        private List<Card> discardPile = new List<Card>();
        private System.Random rng = new System.Random();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// 初始化一副標準48張牌
        /// </summary>
        public void Initialize()
        {
            drawPile.Clear();
            discardPile.Clear();

            int id = 0;

            // 數字牌：1-9，每點數4張，共36張
            for (int value = 1; value <= 9; value++)
            {
                for (int suit = 0; suit < 4; suit++)
                {
                    drawPile.Add(new Card
                    {
                        Id = id++,
                        Type = CardType.Number,
                        Value = value,
                        Suit = (CardSuit)suit
                    });
                }
            }

            // 功能牌：每種2張，共8張
            var specialTypes = new[] { CardType.Reverse, CardType.Skip, CardType.DrawTwo, CardType.Wild };
            foreach (var st in specialTypes)
            {
                for (int suit = 0; suit < 4; suit++)
                {
                    drawPile.Add(new Card
                    {
                        Id = id++,
                        Type = st,
                        Value = 0,
                        Suit = (CardSuit)suit
                    });
                }
            }

            // 烏蠅牌：4張（不計入顏色）
            for (int i = 0; i < 4; i++)
            {
                drawPile.Add(new Card
                {
                    Id = id++,
                    Type = CardType.Fly,
                    Value = 0,
                    Suit = CardSuit.Red  // placeholder
                });
            }

            Debug.Log($"[CardDeck] Initialized with {drawPile.Count} cards");
        }

        /// <summary>
        /// 洗牌
        /// </summary>
        public void Shuffle()
        {
            for (int i = drawPile.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (drawPile[i], drawPile[j]) = (drawPile[j], drawPile[i]);
            }
        }

        /// <summary>
        /// 抽一張牌
        /// </summary>
        public Card Draw()
        {
            if (drawPile.Count == 0)
            {
                // 回收 discard 到 draw
                if (discardPile.Count == 0) return null;
                Card top = discardPile[discardPile.Count - 1];
                discardPile.RemoveAt(discardPile.Count - 1);
                drawPile.AddRange(discardPile);
                discardPile.Clear();
                discardPile.Add(top);
                Shuffle();
            }

            Card c = drawPile[drawPile.Count - 1];
            drawPile.RemoveAt(drawPile.Count - 1);
            return c;
        }

        /// <summary>
        /// 棄牌
        /// </summary>
        public void Discard(Card card)
        {
            discardPile.Add(card);
        }

        /// <summary>
        /// 取得桌面最上面的牌（公共點數）
        /// </summary>
        public Card GetTopDiscard()
        {
            if (discardPile.Count == 0) return null;
            return discardPile[discardPile.Count - 1];
        }

        public int DrawPileCount => drawPile.Count;
        public int DiscardPileCount => discardPile.Count;
    }
}
