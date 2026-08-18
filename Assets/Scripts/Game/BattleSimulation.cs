using System;
using MultiCharacterSample.Data;

namespace MultiCharacterSample.Game
{
    /// <summary>
    /// 방치형 자동 전투 시뮬레이션. UI/Unity 컴포넌트 의존이 없는 순수 로직이다.
    ///
    /// [서버 권위 안내]
    /// 이 샘플은 분석 편의를 위해 전투/보상 계산을 클라이언트에서 수행한다.
    /// 실서비스에서는 이 클래스의 로직(데미지, 처치 보상, 스테이지 진행)을
    /// 게임 서버에서 권위적으로 실행하고, 클라이언트는 결과만 표시해야 한다.
    /// </summary>
    public class BattleSimulation
    {
        public const float HeroAttackInterval = 0.8f;
        public const float MonsterAttackInterval = 1.2f;

        /// <summary>스테이지 순환 몬스터 이름(단일 진실 원천: 빌더의 비주얼 라벨도 이 배열을 쓴다).</summary>
        public static readonly string[] MonsterNames = { "슬라임", "박쥐", "들쥐", "미믹", "스켈레톤", "플라잉아이", "머쉬룸", "고블린" };

        readonly CharacterData character;
        readonly Random random = new Random();

        float heroTimer;
        float monsterTimer;

        public long HeroHp { get; private set; }
        public long MonsterHp { get; private set; }
        public string MonsterName { get; private set; }

        /// <summary>현재 스테이지의 몬스터 종류 인덱스(비주얼 매칭용).</summary>
        public int MonsterIndex { get { return (character.stage - 1) % MonsterNames.Length; } }

        /// <summary>용사가 공격했다. (데미지, 치명타 여부)</summary>
        public event Action<long, bool> HeroAttacked;
        /// <summary>몬스터가 공격했다. (데미지)</summary>
        public event Action<long> MonsterAttacked;
        /// <summary>몬스터를 처치하고 보상을 받았다. (획득 골드)</summary>
        public event Action<long> MonsterKilled;
        /// <summary>용사가 쓰러져 재정비했다(체력 회복 후 같은 스테이지 재도전).</summary>
        public event Action HeroDefeated;

        public BattleSimulation(CharacterData character)
        {
            this.character = character;
            HeroHp = character.MaxHealth;
            SpawnMonster();
        }

        /// <summary>프레임마다 호출. 공격 주기가 차면 전투를 진행한다.</summary>
        public void Tick(float deltaTime)
        {
            heroTimer += deltaTime;
            monsterTimer += deltaTime;

            if (heroTimer >= HeroAttackInterval)
            {
                heroTimer -= HeroAttackInterval;
                HeroAttack();
            }
            if (monsterTimer >= MonsterAttackInterval)
            {
                monsterTimer -= MonsterAttackInterval;
                MonsterAttack();
            }
        }

        /// <summary>체력 강화 등으로 최대 체력이 올랐을 때 전체 회복시킨다.</summary>
        public void HealHeroFull()
        {
            HeroHp = character.MaxHealth;
        }

        void HeroAttack()
        {
            bool crit = random.NextDouble() * 100.0 < character.CritChance;
            long damage = character.Attack * (crit ? 2 : 1);
            MonsterHp -= damage;
            if (HeroAttacked != null) HeroAttacked(damage, crit);

            if (MonsterHp <= 0) KillMonster();
        }

        void MonsterAttack()
        {
            long damage = Math.Max(1, character.MonsterAttack - character.Defense);
            HeroHp -= damage;
            if (MonsterAttacked != null) MonsterAttacked(damage);

            if (HeroHp <= 0)
            {
                // 패배 시 재정비: 체력을 회복하고 같은 스테이지를 다시 도전한다.
                HeroHp = character.MaxHealth;
                MonsterHp = character.MonsterMaxHealth;
                if (HeroDefeated != null) HeroDefeated();
            }
        }

        void KillMonster()
        {
            long reward = character.MonsterGoldReward;
            character.gold += reward;
            character.killsInStage++;
            if (character.killsInStage >= CharacterData.KillsPerStage)
            {
                character.killsInStage = 0;
                character.stage++;
            }
            SpawnMonster();
            if (MonsterKilled != null) MonsterKilled(reward);
        }

        void SpawnMonster()
        {
            MonsterHp = character.MonsterMaxHealth;
            MonsterName = MonsterNames[(character.stage - 1) % MonsterNames.Length] + " Lv." + character.stage;
        }
    }
}
