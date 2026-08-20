using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MultiCharacterSample.UI
{
    /// <summary>확인/취소 공용 팝업. 삭제 확인(2버튼)과 단순 알림(확인 1버튼) 두 모드로 쓴다.</summary>
    public class ConfirmPopup : MonoBehaviour
    {
        public TMP_Text messageText;
        public Button confirmButton;
        public Button cancelButton;

        Action onConfirm;
        RectTransform confirmRect;
        TMP_Text confirmLabel;
        string confirmDefaultText; // 씬에 작성된 확인 버튼 문구(삭제 확인용)
        float confirmPairedX;      // 2버튼 배치에서의 확인 버튼 X 좌표

        void Awake()
        {
            confirmButton.onClick.AddListener(Confirm);
            cancelButton.onClick.AddListener(Close);
        }

        /// <summary>확인/취소 2버튼 팝업. 확인 버튼 문구는 씬 설정을 그대로 쓴다.</summary>
        public void Open(string message, Action confirmed)
        {
            onConfirm = confirmed;
            Show(message, null, true);
        }

        /// <summary>확인 버튼 1개만 노출하는 알림 팝업.</summary>
        public void OpenAlert(string message)
        {
            onConfirm = null;
            Show(message, "확인", false);
        }

        void Show(string message, string confirmText, bool showCancel)
        {
            if (confirmRect == null) CacheConfirmLayout();

            messageText.text = message;
            confirmLabel.text = confirmText ?? confirmDefaultText;
            cancelButton.gameObject.SetActive(showCancel);

            var position = confirmRect.anchoredPosition;
            position.x = showCancel ? confirmPairedX : 0f;
            confirmRect.anchoredPosition = position;

            gameObject.SetActive(true);
        }

        /// <summary>씬에 작성된 확인 버튼 문구와 위치를 최초 1회 보관한다. 모드 전환 시 복원에 쓴다.</summary>
        void CacheConfirmLayout()
        {
            confirmRect = confirmButton.GetComponent<RectTransform>();
            confirmLabel = confirmButton.GetComponentInChildren<TMP_Text>(true);
            confirmDefaultText = confirmLabel.text;
            confirmPairedX = confirmRect.anchoredPosition.x;
        }

        void Confirm()
        {
            var callback = onConfirm;
            Close();
            if (callback != null) callback();
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }
    }
}
