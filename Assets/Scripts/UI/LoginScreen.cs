using System.Collections;
using MultiCharacterSample.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MultiCharacterSample.UI
{
    /// <summary>
    /// 뒤끝 멀티 캐릭터 계정 로그인 화면.
    /// 로그인 실패 시 회원가입 후 다시 로그인한다.
    /// </summary>
    public class LoginScreen : MonoBehaviour
    {
        public UIFlowController flow;
        public TMP_InputField idInput;
        public TMP_InputField passwordInput;
        public Button loginButton;
        public TMP_Text loginButtonLabel;
        public TMP_Text messageText;

        bool busy;
        bool elevationPending;
        string elevationId;
        string elevationPassword;

        void Awake()
        {
            loginButton.onClick.AddListener(OnLogin);
        }

        void OnEnable()
        {
            busy = false;
            elevationPending = false;
            elevationId = null;
            elevationPassword = null;
            loginButton.interactable = true;
            loginButtonLabel.text = "로그인";
            messageText.text = string.Empty;
        }

        void OnLogin()
        {
            if (busy) return;

            string id = idInput.text.Trim();
            string password = passwordInput.text.Trim();
            if (string.IsNullOrWhiteSpace(id))
            {
                messageText.text = "아이디를 입력하세요.";
                return;
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                messageText.text = "비밀번호를 입력하세요.";
                return;
            }

            if (elevationPending && id == elevationId && password == elevationPassword)
            {
                StartCoroutine(ElevateCoroutine());
                return;
            }

            elevationPending = false;
            loginButtonLabel.text = "로그인";

            StartCoroutine(LoginCoroutine(id, password));
        }

        IEnumerator LoginCoroutine(string id, string password)
        {
            busy = true;
            loginButton.interactable = false;
            messageText.text = "서버 연결 및 로그인 중...";
            flow.ShowLoading("로그인 중...");

            bool success = false;
            string error = null;
            yield return CharacterRepository.LoginOrRegister(id, password, (result, message) =>
            {
                success = result;
                error = message;
            });
            flow.HideLoading();

            busy = false;
            loginButton.interactable = true;
            if (!success)
            {
                if (CharacterRepository.IsElevationRequired)
                {
                    elevationPending = true;
                    elevationId = id;
                    elevationPassword = password;
                    loginButtonLabel.text = "멀티 계정 전환";
                    messageText.text = "싱글 계정입니다. 전환이 필요합니다.";
                }
                else if (CharacterRepository.IsProjectChangeRequired)
                {
                    ShowProjectChangeGuide();
                }
                else
                {
                    messageText.text = error;
                }
                yield break;
            }

            flow.ShowCharacterSelect();
        }

        IEnumerator ElevateCoroutine()
        {
            busy = true;
            loginButton.interactable = false;
            messageText.text = "멀티 캐릭터 계정으로 전환 중...";
            flow.ShowLoading("멀티 캐릭터 계정으로 전환 중...");

            bool success = false;
            string error = null;
            yield return CharacterRepository.ElevateToMultiCharacter((result, message) =>
            {
                success = result;
                error = message;
            });
            flow.HideLoading();

            busy = false;
            loginButton.interactable = true;
            if (!success)
            {
                if (CharacterRepository.IsProjectChangeRequired)
                {
                    ShowProjectChangeGuide();
                }
                else
                {
                    messageText.text = error;
                    if (!CharacterRepository.IsElevationRequired)
                    {
                        elevationPending = false;
                        loginButtonLabel.text = "로그인";
                    }
                }
                yield break;
            }

            elevationPending = false;
            elevationId = null;
            elevationPassword = null;
            loginButtonLabel.text = "로그인";
            flow.ShowCharacterSelect();
        }

        public void ShowMessage(string message)
        {
            messageText.text = message;
        }

        void ShowProjectChangeGuide()
        {
            elevationPending = false;
            elevationId = null;
            elevationPassword = null;
            loginButtonLabel.text = "로그인";
            messageText.text = "싱글 캐릭터 프로젝트입니다.\n멀티 캐릭터 프로젝트 키로 변경하세요.";
        }
    }
}
