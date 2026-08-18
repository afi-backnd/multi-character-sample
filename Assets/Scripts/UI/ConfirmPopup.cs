using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MultiCharacterSample.UI
{
    /// <summary>확인/취소 공용 팝업. 캐릭터 삭제 확인에 사용한다.</summary>
    public class ConfirmPopup : MonoBehaviour
    {
        public TMP_Text messageText;
        public Button confirmButton;
        public Button cancelButton;

        Action onConfirm;

        void Awake()
        {
            confirmButton.onClick.AddListener(Confirm);
            cancelButton.onClick.AddListener(Close);
        }

        public void Open(string message, Action confirmed)
        {
            messageText.text = message;
            onConfirm = confirmed;
            gameObject.SetActive(true);
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
