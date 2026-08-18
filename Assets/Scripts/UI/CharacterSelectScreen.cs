using System.Collections;
using System.Collections.Generic;
using MultiCharacterSample.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MultiCharacterSample.UI
{
    /// <summary>
    /// 뒤끝 계정 컨텍스트에서 캐릭터 목록을 관리하고, 선택한 캐릭터의 UserData를 읽은 뒤 게임으로 진입한다.
    /// </summary>
    public class CharacterSelectScreen : MonoBehaviour
    {
        public UIFlowController flow;
        public ScrollRect scrollRect;
        public RectTransform content;
        public CharacterSlotView slotTemplate;
        public Button startButton;
        public TMP_Text startButtonLabel;
        public CreateCharacterPopup createPopup;
        public ConfirmPopup confirmPopup;

        readonly List<CharacterSlotView> slots = new List<CharacterSlotView>();
        int selectedIndex = -1;
        bool busy;

        void Awake()
        {
            startButton.onClick.AddListener(OnStartGame);
        }

        void OnEnable()
        {
            StartCoroutine(LoadCharacters(false));
        }

        IEnumerator LoadCharacters(bool keepScroll)
        {
            SetBusy(true, "캐릭터 목록 조회 중...");
            flow.ShowLoading("캐릭터 목록 불러오는 중...");

            bool success = false;
            string error = null;
            yield return CharacterRepository.LoadCharacters((result, message) =>
            {
                success = result;
                error = message;
            });

            Rebuild(keepScroll);
            SetBusy(false, success ? null : error);
            flow.HideLoading();
        }

        void Rebuild(bool keepScroll)
        {
            float scroll = scrollRect.horizontalNormalizedPosition;
            var characters = CharacterRepository.Characters;
            int slotCount = Mathf.Max(10, characters.Count + 1);

            if (slots.Count != slotCount)
            {
                foreach (var slot in slots)
                {
                    slot.gameObject.SetActive(false);
                    Destroy(slot.gameObject);
                }
                slots.Clear();

                for (int i = 0; i < slotCount; i++)
                {
                    var slot = Instantiate(slotTemplate, content);
                    slot.gameObject.SetActive(true);
                    slots.Add(slot);
                }
            }

            for (int i = 0; i < slots.Count; i++)
            {
                if (i < characters.Count) slots[i].BindCharacter(characters[i], OnSlotTapped, OnDeleteRequested);
                else slots[i].BindCreate(OnSlotTapped);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            int select = IndexOfCharacter(CharacterRepository.LastSelectedId);
            if (select < 0 && characters.Count > 0) select = 0;
            SelectSlot(select);
            Canvas.ForceUpdateCanvases();
            scrollRect.StopMovement();
            if (keepScroll) scrollRect.horizontalNormalizedPosition = scroll;
            else ScrollTo(select < 0 ? 0 : select);
        }

        void SelectSlot(int index)
        {
            selectedIndex = index;
            for (int i = 0; i < slots.Count; i++)
                slots[i].SetSelected(i == index && !slots[i].IsCreateSlot);

            bool isCharacter = IsCharacterSelected();
            startButton.interactable = !busy && isCharacter;
            startButtonLabel.text = isCharacter ? "게임 시작" : "캐릭터를 선택하세요";
            if (isCharacter) CharacterRepository.LastSelectedId = slots[index].CharacterId;
        }

        bool IsCharacterSelected()
        {
            return selectedIndex >= 0 && selectedIndex < slots.Count && !slots[selectedIndex].IsCreateSlot;
        }

        void SetBusy(bool value, string status)
        {
            busy = value;
            startButton.interactable = !busy && IsCharacterSelected();
            if (!string.IsNullOrEmpty(status))
                startButtonLabel.text = status;
            else
                startButtonLabel.text = IsCharacterSelected() ? "게임 시작" : "캐릭터를 선택하세요";
        }

        void ScrollTo(int index)
        {
            if (index < 0 || index >= slots.Count)
            {
                scrollRect.horizontalNormalizedPosition = 0f;
                return;
            }

            float scrollableWidth = content.rect.width - scrollRect.viewport.rect.width;
            if (scrollableWidth <= 0f)
            {
                scrollRect.horizontalNormalizedPosition = 0f;
                return;
            }

            float targetCenter = slots[index].transform.localPosition.x;
            float viewportCenter = scrollRect.viewport.rect.width * 0.5f;
            scrollRect.horizontalNormalizedPosition = Mathf.Clamp01((targetCenter - viewportCenter) / scrollableWidth);
        }

        int IndexOfCharacter(string id)
        {
            if (string.IsNullOrEmpty(id)) return -1;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].CharacterId == id) return i;
            }
            return -1;
        }

        void OnSlotTapped(CharacterSlotView slot)
        {
            if (busy) return;
            if (slot.IsCreateSlot)
            {
                OpenCreatePopup();
                return;
            }
            SelectSlot(slots.IndexOf(slot));
        }

        public void OpenCreatePopup()
        {
            if (busy) return;
            createPopup.Open((characterName, portraitIndex) => StartCoroutine(CreateCharacter(characterName, portraitIndex)));
        }

        IEnumerator CreateCharacter(string characterName, int portraitIndex)
        {
            SetBusy(true, "캐릭터 생성 중...");
            flow.ShowLoading("캐릭터 생성 중...");

            bool success = false;
            string error = null;
            string createdId = null;
            yield return CharacterRepository.CreateCharacter(characterName, portraitIndex, (result, message, uuid) =>
            {
                success = result;
                error = message;
                createdId = uuid;
            });
            flow.HideLoading();

            if (!CharacterRepository.IsAccountReady)
            {
                flow.ShowLogin();
                flow.loginScreen.ShowMessage(error);
                yield break;
            }

            if (!string.IsNullOrEmpty(createdId))
                yield return LoadCharacters(false);
            if (!success)
                SetBusy(false, error);
        }

        void OnDeleteRequested(CharacterSlotView slot)
        {
            if (busy) return;
            string id = slot.CharacterId;
            confirmPopup.Open("'" + slot.CharacterName + "' 캐릭터를 삭제할까요?\n삭제한 캐릭터는 복구할 수 없습니다.",
                () => StartCoroutine(DeleteCharacter(id)));
        }

        IEnumerator DeleteCharacter(string id)
        {
            bool deletedSelection = id == CharacterRepository.LastSelectedId;
            SetBusy(true, "캐릭터 삭제 중...");
            flow.ShowLoading("캐릭터 삭제 중...");

            bool success = false;
            string error = null;
            yield return CharacterRepository.DeleteCharacter(id, (result, message) =>
            {
                success = result;
                error = message;
            });
            flow.HideLoading();

            if (!success)
            {
                SetBusy(false, error);
                yield break;
            }

            yield return LoadCharacters(!deletedSelection);
        }

        void OnStartGame()
        {
            if (busy || !IsCharacterSelected()) return;
            StartCoroutine(SelectAndStartGame(slots[selectedIndex].CharacterId));
        }

        IEnumerator SelectAndStartGame(string id)
        {
            SetBusy(true, "플레이 정보 조회 중...");
            flow.ShowLoading("플레이 정보 불러오는 중...");

            bool success = false;
            string error = null;
            CharacterData character = null;
            yield return CharacterRepository.SelectAndLoadCharacter(id, (result, message, data) =>
            {
                success = result;
                error = message;
                character = data;
            });
            flow.HideLoading();

            if (!success)
            {
                if (!CharacterRepository.IsAccountReady)
                {
                    flow.ShowLogin();
                    flow.loginScreen.ShowMessage(error);
                    yield break;
                }
                SetBusy(false, error);
                yield break;
            }

            flow.StartGame(character);
        }
    }
}
