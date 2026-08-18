using System.Collections;
using MultiCharacterSample.Data;
using MultiCharacterSample.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MultiCharacterSample.UI
{
    /// <summary>
    /// 방치형 RPG 게임 화면(뷰). 전투 규칙은 BattleSimulation(순수 로직)이 담당하고,
    /// 이 클래스는 시뮬레이션 이벤트를 받아 화면 갱신과 연출만 수행한다.
    /// 구성: 상단 바(캐릭터/스테이지/골드) + 중앙 전투 + 하단 능력치 강화.
    /// </summary>
    public class GameScreen : MonoBehaviour
    {
        [Header("공통")]
        public UIFlowController flow;
        public Button lobbyButton;

        [Header("상단 바")]
        public TMP_Text characterText;
        public TMP_Text stageText;
        public TMP_Text goldText;

        [Header("전투")]
        public RectTransform heroRoot;
        public RectTransform monsterRoot;
        public TMP_Text heroNameText;
        public TMP_Text monsterNameText;
        public Image heroHpFill;
        public Image monsterHpFill;
        public TMP_Text heroHpText;
        public TMP_Text monsterHpText;
        public RectTransform damageLayer;
        public TMP_Text damageTextTemplate;

        [Header("전투 비주얼")]
        public UISpriteAnimation heroAnimation;
        public Sprite[] heroAttackFrames;
        public UISpriteAnimation monsterAnimation;
        public MonsterVisual[] monsterVisuals;

        [Header("능력치 강화")]
        public StatUpgradeRow[] upgradeRows;

        /// <summary>스테이지별 몬스터 스프라이트 세트. BattleSimulation.MonsterIndex와 순서가 일치한다.</summary>
        [System.Serializable]
        public class MonsterVisual
        {
            public string label; // 인스펙터 표시용(BattleSimulation.MonsterNames에서 채움)
            public Sprite[] frames;
            public Sprite[] attackFrames;
        }

        static readonly string[] StatNames = { "공격력", "체력", "방어력", "치명타", "골드 획득" };
        static readonly Color NormalDamageColor = Color.white;
        static readonly Color CritDamageColor = new Color(0.95f, 0.73f, 0.29f);
        static readonly Color HeroHitColor = new Color(0.9f, 0.28f, 0.3f);

        CharacterData character;
        BattleSimulation battle;
        Coroutine saveLoop;
        bool leaving;
        bool quitAfterSave;
        bool quitSaveInProgress;
        bool lobbyTransitionInProgress;
        bool quitRequested;

        public void Bind(CharacterData data)
        {
            character = data;
        }

        // ---------------------------------------------------------------- 수명주기

        void Awake()
        {
            lobbyButton.onClick.AddListener(OnLobbyClicked);
            foreach (var row in upgradeRows)
            {
                var captured = row;
                row.upgradeButton.onClick.AddListener(() => OnUpgradeClicked(captured));
            }
#if !UNITY_EDITOR && !UNITY_IOS
            Application.wantsToQuit += OnWantsToQuit;
#endif
        }

        void OnDestroy()
        {
#if !UNITY_EDITOR && !UNITY_IOS
            Application.wantsToQuit -= OnWantsToQuit;
#endif
        }

        void OnEnable()
        {
            if (character == null) return;

            leaving = false;
            lobbyTransitionInProgress = false;
            quitRequested = false;
            lobbyButton.interactable = true;
            battle = new BattleSimulation(character);
            battle.HeroAttacked += OnHeroAttacked;
            battle.MonsterAttacked += OnMonsterAttacked;
            battle.MonsterKilled += OnMonsterKilled;

            heroNameText.text = character.characterName;
            ApplyMonsterVisual();
            RefreshAll();
            saveLoop = StartCoroutine(SaveLoop());
        }

        void OnDisable()
        {
            if (saveLoop != null)
            {
                StopCoroutine(saveLoop);
                saveLoop = null;
            }
            battle = null;
            ClearDamageTexts();
        }

        void Update()
        {
            if (battle == null || leaving) return;
            battle.Tick(Time.deltaTime);
            RefreshBattle();
        }

        // ---------------------------------------------------------------- 전투 이벤트 → 연출

        void OnHeroAttacked(long damage, bool crit)
        {
            string text = (crit ? "치명타! " : string.Empty) + damage.ToString("N0");
            ShowDamage(monsterRoot, text, crit ? CritDamageColor : NormalDamageColor);
            heroAnimation.PlayOnce(heroAttackFrames, 14f); // 공격 모션(0.8초 주기 안에 완주)
            StartCoroutine(Punch(monsterRoot)); // 피격 플린치
        }

        void OnMonsterAttacked(long damage)
        {
            ShowDamage(heroRoot, damage.ToString("N0"), HeroHitColor);
            var visual = CurrentMonsterVisual();
            if (visual != null && visual.attackFrames != null && visual.attackFrames.Length > 0)
                monsterAnimation.PlayOnce(visual.attackFrames, visual.attackFrames.Length); // 1초 안에 완주
            StartCoroutine(Punch(heroRoot)); // 피격 플린치
        }

        void OnMonsterKilled(long reward)
        {
            CharacterRepository.MarkDirty();
            ApplyMonsterVisual(); // 다음 몬스터 등장
            RefreshHeader();
            RefreshUpgrades();
        }

        MonsterVisual CurrentMonsterVisual()
        {
            if (monsterVisuals == null || monsterVisuals.Length == 0) return null;
            return monsterVisuals[Mathf.Clamp(battle.MonsterIndex, 0, monsterVisuals.Length - 1)];
        }

        void ApplyMonsterVisual()
        {
            var visual = CurrentMonsterVisual();
            if (visual != null) monsterAnimation.SetFrames(visual.frames);
        }

        // ---------------------------------------------------------------- 입력

        void OnLobbyClicked()
        {
            if (leaving) return;
            StartCoroutine(ReturnToLobby());
        }

        void OnUpgradeClicked(StatUpgradeRow row)
        {
            if (leaving) return;
            long cost = character.GetUpgradeCost(row.stat);
            if (character.gold < cost) return;

            character.gold -= cost;
            character.IncreaseStat(row.stat);
            if (row.stat == StatType.Health) battle.HealHeroFull(); // 체력 강화 시 전체 회복
            CharacterRepository.MarkDirty();
            RefreshAll();
        }

        IEnumerator SaveLoop()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(300f);
                bool success = false;
                string error = null;
                yield return CharacterRepository.SaveSelected((result, message) =>
                {
                    success = result;
                    error = message;
                });
                if (!success) Debug.LogWarning($"[GameScreen] 5분 주기 저장 실패: {error}");
            }
        }

        IEnumerator ReturnToLobby()
        {
            leaving = true;
            lobbyTransitionInProgress = true;
            lobbyButton.interactable = false;
            flow.ShowLoading("플레이 정보 저장 중...");

            bool saved = false;
            string saveError = null;
            yield return CharacterRepository.SaveSelected((result, message) =>
            {
                saved = result;
                saveError = message;
            });

            if (!saved)
            {
                Debug.LogWarning($"[GameScreen] 로비 이동 전 저장 실패: {saveError}");
                flow.HideLoading();
                lobbyTransitionInProgress = false;
                if (quitRequested)
                {
                    if (!quitSaveInProgress) StartCoroutine(SaveBeforeQuit());
                    yield break;
                }

                leaving = false;
                lobbyButton.interactable = true;
                yield break;
            }

            if (quitRequested)
            {
                flow.HideLoading();
                lobbyTransitionInProgress = false;
                quitAfterSave = true;
                Debug.Log("[GameScreen] 종료 전 저장 완료");
                Application.Quit();
                yield break;
            }

            flow.SetLoadingMessage("로비로 이동 중...");
            bool accountReady = false;
            string accountError = null;
            yield return CharacterRepository.ReturnToAccount((result, message) =>
            {
                accountReady = result;
                accountError = message;
            });
            flow.HideLoading();
            lobbyTransitionInProgress = false;

            if (!accountReady)
            {
                flow.ShowLogin();
                flow.loginScreen.ShowMessage(accountError);
                yield break;
            }

            flow.ShowCharacterSelect();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && isActiveAndEnabled && character != null && !leaving)
                StartCoroutine(SaveOnPause());
        }

        IEnumerator SaveOnPause()
        {
            bool success = false;
            string error = null;
            yield return CharacterRepository.SaveSelected((result, message) =>
            {
                success = result;
                error = message;
            });
            if (!success) Debug.LogWarning($"[GameScreen] 백그라운드 전환 저장 실패: {error}");
        }

        bool OnWantsToQuit()
        {
            if (quitAfterSave || !CharacterRepository.NeedsSaveBeforeQuit) return true;

            quitRequested = true;
            if (!lobbyTransitionInProgress && !quitSaveInProgress) StartCoroutine(SaveBeforeQuit());
            return false;
        }

        IEnumerator SaveBeforeQuit()
        {
            quitSaveInProgress = true;
            leaving = true;
            flow.ShowLoading("플레이 정보 저장 중...");

            bool success = false;
            string error = null;
            yield return CharacterRepository.SaveSelected((result, message) =>
            {
                success = result;
                error = message;
            });

            quitSaveInProgress = false;
            if (!success)
            {
                quitRequested = false;
                if (!lobbyTransitionInProgress)
                {
                    leaving = false;
                    lobbyButton.interactable = true;
                }
                Debug.LogError($"[GameScreen] 종료 전 저장 실패, 종료 취소: {error}");
                flow.HideLoading();
                yield break;
            }

            quitAfterSave = true;
            Debug.Log("[GameScreen] 종료 전 저장 완료");
            Application.Quit();
        }

        // ---------------------------------------------------------------- 화면 갱신

        void RefreshAll()
        {
            RefreshHeader();
            RefreshBattle();
            RefreshUpgrades();
        }

        void RefreshHeader()
        {
            characterText.text = character.characterName + "  Lv." + character.Level;
            stageText.text = "스테이지 " + character.stage + "  (" + character.killsInStage + "/" + CharacterData.KillsPerStage + ")";
            goldText.text = character.gold.ToString("N0") + " G";
        }

        void RefreshBattle()
        {
            monsterNameText.text = battle.MonsterName;
            heroHpFill.fillAmount = Mathf.Clamp01(battle.HeroHp / (float)character.MaxHealth);
            monsterHpFill.fillAmount = Mathf.Clamp01(battle.MonsterHp / (float)character.MonsterMaxHealth);
            heroHpText.text = System.Math.Max(0, battle.HeroHp).ToString("N0");
            monsterHpText.text = System.Math.Max(0, battle.MonsterHp).ToString("N0");
        }

        void RefreshUpgrades()
        {
            foreach (var row in upgradeRows)
            {
                long cost = character.GetUpgradeCost(row.stat);
                row.nameText.text = StatNames[(int)row.stat];
                row.levelText.text = "Lv." + character.GetStatLevel(row.stat);
                row.valueText.text = StatValueText(row.stat);
                row.costText.text = cost.ToString("N0") + " G";
                row.upgradeButton.interactable = character.gold >= cost;
            }
        }

        string StatValueText(StatType stat)
        {
            switch (stat)
            {
                case StatType.Attack: return character.Attack.ToString("N0");
                case StatType.Health: return character.MaxHealth.ToString("N0");
                case StatType.Defense: return character.Defense.ToString("N0");
                case StatType.Crit: return character.CritChance.ToString("0.#") + "%";
                default: return "x" + character.GoldMultiplier.ToString("0.0#");
            }
        }

        // ---------------------------------------------------------------- 연출(데미지 텍스트, 타격감)

        void ShowDamage(RectTransform target, string text, Color color)
        {
            var damageText = Instantiate(damageTextTemplate, damageLayer);
            damageText.gameObject.SetActive(true);
            damageText.text = text;
            damageText.color = color;
            damageText.rectTransform.position = target.position;
            damageText.rectTransform.localPosition += new Vector3(Random.Range(-40f, 40f), 10f + Random.Range(0f, 25f), 0f);
            StartCoroutine(FloatAndFade(damageText));
        }

        IEnumerator FloatAndFade(TMP_Text text)
        {
            const float lifetime = 0.7f;
            float elapsed = 0f;
            while (elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                text.rectTransform.localPosition += Vector3.up * (Time.deltaTime * 130f);
                var color = text.color;
                color.a = 1f - elapsed / lifetime;
                text.color = color;
                yield return null;
            }
            Destroy(text.gameObject);
        }

        IEnumerator Punch(RectTransform target)
        {
            const float duration = 0.15f;
            target.localScale = Vector3.one * 1.12f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                target.localScale = Vector3.Lerp(Vector3.one * 1.12f, Vector3.one, elapsed / duration);
                yield return null;
            }
            target.localScale = Vector3.one;
        }

        void ClearDamageTexts()
        {
            for (int i = damageLayer.childCount - 1; i >= 0; i--)
            {
                var child = damageLayer.GetChild(i).gameObject;
                if (child != damageTextTemplate.gameObject) Destroy(child);
            }
        }
    }
}
