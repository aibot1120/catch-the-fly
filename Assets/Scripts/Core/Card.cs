using System;
using UnityEngine;

namespace CatchTheFly.Core
{
    /// <summary>
    /// 卡牌數據
    /// </summary>
    [Serializable]
    public class Card
    {
        public int Id;
        public CardType Type;
        public int Value;  // 1-9 for number cards, 0 for special
        public CardSuit Suit;

        public string DisplayName => Type == CardType.Number ? Value.ToString() : Type.ToString();
        public int Point => Value; // for scoring matching
    }

    public enum CardType
    {
        Number,     // 數字牌 1-9
        Fly,        // 烏蠅牌（萬用）
        Reverse,    // 反轉
        Skip,       // 跳過
        DrawTwo,    // +2
        Wild        // 萬能顏色
    }

    public enum CardSuit
    {
        Red,
        Blue,
        Green,
        Yellow
    }
}
