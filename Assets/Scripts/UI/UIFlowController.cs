using MultiCharacterSample.Data;
using TMPro;
using UnityEngine;

namespace MultiCharacterSample.UI
{
    /// <summary>
    /// 씬 전환 없이 스크린 오브젝트 활성화로 시퀀스를 구성한다.
    /// 로그인 -> 캐릭터 선택 -> 게임(방치형 RPG).
    /// </summary>
    public class UIFlowController : MonoBehaviour
    {
        public LoginScreen loginScreen;
        public CharacterSelectScreen characterSelectScreen;
        public GameScreen gameScreen;
        public GameObject loadingOverlay;
        public CanvasGroup loadingCanvasGroup;
        public TMP_Text loadingText;
        public RectTransform loadingSpinner;

        bool loadingVisible;
        int loadingRequests;
        float loadingRotation;

        void Awake()
        {
            // 모바일 기준 앱 전역 설정: 60프레임 고정 + 백그라운드에서도 루프 유지
            QualitySettings.vSyncCount = 0; // vSync가 켜져 있으면 targetFrameRate가 무시된다
            Application.targetFrameRate = 60;
            Application.runInBackground = true; // 에디터/데스크톱용. 모바일에서는 OS 정책상 무시된다
            if (loadingOverlay != null) loadingOverlay.SetActive(false);
        }

        void Start()
        {
            ShowLogin();
        }

        void Update()
        {
            if (loadingOverlay == null || loadingCanvasGroup == null) return;

            float targetAlpha = loadingVisible ? 1f : 0f;
            loadingCanvasGroup.alpha = Mathf.MoveTowards(loadingCanvasGroup.alpha, targetAlpha, Time.unscaledDeltaTime * 6f);
            if (!loadingVisible && loadingCanvasGroup.alpha <= 0f)
            {
                loadingOverlay.SetActive(false);
                return;
            }

            loadingRotation = (loadingRotation - Time.unscaledDeltaTime * 180f) % 360f;
            if (loadingSpinner != null) loadingSpinner.localRotation = Quaternion.Euler(0f, 0f, loadingRotation);
        }

        public void ShowLoading(string message)
        {
            loadingRequests++;
            loadingVisible = true;
            SetLoadingMessage(message);
            if (loadingOverlay == null) return;

            if (!loadingOverlay.activeSelf)
            {
                if (loadingCanvasGroup != null) loadingCanvasGroup.alpha = 0f;
                loadingOverlay.SetActive(true);
            }
            loadingOverlay.transform.SetAsLastSibling();
        }

        public void SetLoadingMessage(string message)
        {
            if (loadingText != null) loadingText.text = message;
        }

        public void HideLoading()
        {
            if (loadingRequests > 0) loadingRequests--;
            loadingVisible = loadingRequests > 0;
        }

        public void ShowLogin()
        {
            Show(loginScreen.gameObject);
        }

        public void ShowCharacterSelect()
        {
            Show(characterSelectScreen.gameObject);
        }

        public void StartGame(CharacterData character)
        {
            gameScreen.Bind(character);
            Show(gameScreen.gameObject);
        }

        void Show(GameObject screen)
        {
            loginScreen.gameObject.SetActive(loginScreen.gameObject == screen);
            characterSelectScreen.gameObject.SetActive(characterSelectScreen.gameObject == screen);
            gameScreen.gameObject.SetActive(gameScreen.gameObject == screen);
        }
    }
}
