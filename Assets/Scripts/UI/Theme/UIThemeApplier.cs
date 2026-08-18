using TMPro;
using UnityEngine;

namespace MultiCharacterSample.UI
{
    /// <summary>
    /// 캔버스 루트에서 하위 모든 UIStyleBinder와 TMP 텍스트에 테마를 적용한다.
    /// 에디터에서는 컨텍스트 메뉴 "Apply Theme"로 즉시 반영할 수 있다.
    /// </summary>
    public class UIThemeApplier : MonoBehaviour
    {
        public UITheme theme;

        void Awake()
        {
            Apply();
        }

        [ContextMenu("Apply Theme")]
        public void Apply()
        {
            if (theme == null) return;

            foreach (var binder in GetComponentsInChildren<UIStyleBinder>(true))
                binder.Apply(theme);

            if (theme.font != null)
            {
                foreach (var text in GetComponentsInChildren<TMP_Text>(true))
                    text.font = theme.font;
            }
        }
    }
}
