using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatchTheFly.Core
{
    /// <summary>
    /// 玩家數據
    /// </summary>
    [Serializable]
    public class Player
    {
        public int Id;
        public string Name;
        public long Score;
        public List<Card> HandCards = new List<Card>();
        public bool IsAI;
        public bool IsDealer; // 莊家
        public bool IsMyTurn;

        /// <summary>
        /// 手牌點數總和
        /// </summary>
        public int TotalHandValue
        {
            get
            {
                int sum = 0;
                foreach (var c in HandCards)
                    sum += c.Value;
                return sum;
            }
        }
    }
}
