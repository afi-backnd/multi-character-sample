using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MultiCharacterSample.UI
{
    /// <summary>캐릭터 이름 입력과 원화 선택(좌우 순환)을 받는 생성 팝업.</summary>
    public class CreateCharacterPopup : MonoBehaviour
    {
        public TMP_InputField nameInput;
        public Button confirmButton;
        public Button cancelButton;
        public TMP_Text errorText;
        public Image portraitImage;        // 원화 미리보기(테마 바인더 없이 원본 스프라이트 표시)
        public Button prevPortraitButton;
        public Button nextPortraitButton;
        public TMP_Text portraitLabel;     // "원화 선택 n / N"
        public Sprite[] portraitSprites;

        public int SelectedPortrait { get; private set; }

        Action<string, int> onConfirm;

        void Awake()
        {
            confirmButton.onClick.AddListener(Confirm);
            cancelButton.onClick.AddListener(Close);
            prevPortraitButton.onClick.AddListener(() => ShiftPortrait(-1));
            nextPortraitButton.onClick.AddListener(() => ShiftPortrait(1));
        }

        public void Open(Action<string, int> confirmed)
        {
            onConfirm = confirmed;
            nameInput.text = string.Empty;
            errorText.text = string.Empty;
            SelectedPortrait = 0;
            RefreshPortrait();
            gameObject.SetActive(true);
            nameInput.ActivateInputField();
        }

        void ShiftPortrait(int direction)
        {
            if (portraitSprites == null || portraitSprites.Length == 0) return;
            SelectedPortrait = (SelectedPortrait + direction + portraitSprites.Length) % portraitSprites.Length;
            RefreshPortrait();
        }

        void RefreshPortrait()
        {
            if (portraitSprites == null || portraitSprites.Length == 0) return;
            portraitImage.sprite = portraitSprites[SelectedPortrait];
            portraitLabel.text = "원화 선택  " + (SelectedPortrait + 1) + " / " + portraitSprites.Length;
        }

        void Confirm()
        {
            string characterName = nameInput.text.Trim();
            if (characterName.Length < 1 || characterName.Length > 8)
            {
                errorText.text = "이름은 1~8자로 입력하세요.";
                return;
            }

            var callback = onConfirm;
            int portrait = SelectedPortrait;
            Close();
            if (callback != null) callback(characterName, portrait);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }
    }
}
