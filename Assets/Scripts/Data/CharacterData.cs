using System;
using UnityEngine;

namespace MultiCharacterSample.Data
{
    public enum StatType
    {
        Attack = 0,
        Health = 1,
        Defense = 2,
        Crit = 3,
        GoldGain = 4
    }

    /// <summary>
    /// 한 캐릭터의 영속 데이터. 전투/성장 수치는 전부 강화 레벨에서 파생된다.
    /// </summary>
    [Serializable]
    public class CharacterData
    {
        public const int KillsPerStage = 5;

        public string id;
        public string inDate;
        public string rowInDate;
        public string characterName;
        public int portraitIndex = -1; // 선택 카드 원화 번호(생성 순서로 배정, -1 = 미배정 구버전 저장분)
        public int stage = 1;
        public int killsInStage;
        public long gold = 200;
        public int attackLevel = 1;
        public int healthLevel = 1;
        public int defenseLevel = 1;
        public int critLevel = 1;
        public int goldGainLevel = 1;

        static readonly long[] BaseCosts = { 25, 20, 30, 45, 60 };

        /// <summary>표시용 캐릭터 레벨. 강화 레벨 합산으로 계산한다.</summary>
        public int Level { get { return attackLevel + healthLevel + defenseLevel + critLevel + goldGainLevel - 4; } }

        public long Attack { get { return 10 + (attackLevel - 1) * 5L; } }
        public long MaxHealth { get { return 100 + (healthLevel - 1) * 40L; } }
        public long Defense { get { return (defenseLevel - 1) * 2L; } }
        public float CritChance { get { return Mathf.Min(60f, 5f + (critLevel - 1) * 0.5f); } }
        public float GoldMultiplier { get { return 1f + (goldGainLevel - 1) * 0.1f; } }

        public long MonsterMaxHealth { get { return (long)(60.0 * stage * Math.Pow(1.12, stage - 1)); } }
        public long MonsterAttack { get { return 6 + 4L * stage; } }
        public long MonsterGoldReward { get { return (long)((12 + 8L * stage) * GoldMultiplier); } }

        public int GetStatLevel(StatType stat)
        {
            switch (stat)
            {
                case StatType.Attack: return attackLevel;
                case StatType.Health: return healthLevel;
                case StatType.Defense: return defenseLevel;
                case StatType.Crit: return critLevel;
                default: return goldGainLevel;
            }
        }

        public void IncreaseStat(StatType stat)
        {
            switch (stat)
            {
                case StatType.Attack: attackLevel++; break;
                case StatType.Health: healthLevel++; break;
                case StatType.Defense: defenseLevel++; break;
                case StatType.Crit: critLevel++; break;
                default: goldGainLevel++; break;
            }
        }

        public long GetUpgradeCost(StatType stat)
        {
            return (long)(BaseCosts[(int)stat] * Math.Pow(1.17, GetStatLevel(stat) - 1));
        }
    }
}
