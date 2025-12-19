using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// Defines all fixed deck data (for user custom decks, check UserData.cs)
    /// 定义所有卡组数据
    /// </summary>
    
    [CreateAssetMenu(fileName = "DeckData", menuName = "TcgEngine/DeckData", order = 7)]
    public class DeckData : ScriptableObject
    {
        public string id;

        [Header("Display")]
        public string title;

        [Header("Cards")]
        public CardData[] monsters;
        public CardData hero;
        public CardData[] cards;

        [Header("Heroes")]
        [Tooltip("开局时部署的三个英雄棋子，将部署在棋盘最底部的2-4列（居中）")]
        public CardData[] heroes = new CardData[3];

        public static List<DeckData> deck_list = new List<DeckData>();

        public static void Load(string folder = "")
        {
            if(deck_list.Count == 0)
                deck_list.AddRange(Resources.LoadAll<DeckData>(folder));
        }

        public int GetQuantity()
        {
            return cards.Length;
        }

        public bool IsValid()
        {
            return cards.Length >= GameplayData.Get().deck_size;
        }

        public static DeckData Get(string id)
        {
            foreach (DeckData deck in GetAll())
            {
                if (deck.id == id)
                    return deck;
            }
            return null;
        }

        public static List<DeckData> GetAll()
        {
            return deck_list;
        }
    }
}