using System;
using MultiCharacterSample.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MultiCharacterSample.UI
{
    /// <summary>
    /// 캐릭터 선택 슬라이드의 카드 1장. 캐릭터 카드와 "캐릭터 생성" 카드 두 모드를 가진다.
    /// </summary>
    public class CharacterSlotView : MonoBehaviour
    {
        public GameObject characterRoot;
        public GameObject createRoot;
        public GameObject selectedFrame;
        public TMP_Text nameText;
        public TMP_Text levelText;
        public TMP_Text stageText;
        public Image portrait;
        public Sprite[] portraitSprites; // 캐릭터 선택 카드 원화 후보(빌더가 배정)
        public Button cardButton;
        public Button deleteButton;

        public string CharacterId { get; private set; }
        public bool IsCreateSlot { get; private set; }
        public string CharacterName { get; private set; }

        Action<CharacterSlotView> onTap;
        Action<CharacterSlotView> onDelete;

        void Awake()
        {
            cardButton.onClick.AddListener(HandleTap);
            deleteButton.onClick.AddListener(HandleDelete);
        }

        public void BindCharacter(CharacterData data, Action<CharacterSlotView> tap, Action<CharacterSlotView> delete)
        {
            CharacterId = data.id;
            CharacterName = data.characterName;
            IsCreateSlot = false;
            onTap = tap;
            onDelete = delete;

            characterRoot.SetActive(true);
            createRoot.SetActive(false);
            nameText.text = data.characterName;
            levelText.text = "Lv." + data.Level;
            stageText.text = "스테이지 " + data.stage;
            portrait.sprite = PortraitFor(data.portraitIndex);
            portrait.color = Color.white; // 원화 원색 그대로 표시
            SetSelected(false);
        }

        public void BindCreate(Action<CharacterSlotView> tap)
        {
            CharacterId = null;
            CharacterName = null;
            IsCreateSlot = true;
            onTap = tap;
            onDelete = null;

            characterRoot.SetActive(false);
            createRoot.SetActive(true);
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            selectedFrame.SetActive(selected);
            // 선택 강조: 카드 확대로 즉시 식별(카드 간격 30px 안쪽이라 이웃과 겹치지 않음)
            transform.localScale = selected ? Vector3.one * 1.05f : Vector3.one;
        }

        void HandleTap()
        {
            if (onTap != null) onTap(this);
        }

        void HandleDelete()
        {
            if (onDelete != null) onDelete(this);
        }

        /// <summary>생성 순서 번호를 원화 목록에 순환 매핑한다(랜덤 아님, 저장값 기반).</summary>
        Sprite PortraitFor(int index)
        {
            if (portraitSprites == null || portraitSprites.Length == 0 || index < 0) return portrait.sprite; // 원화 미배치 시 테마 기본 초상 유지
            return portraitSprites[index % portraitSprites.Length];
        }
    }
}
