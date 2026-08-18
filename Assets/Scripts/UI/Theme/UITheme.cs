using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MultiCharacterSample.UI
{
    /// <summary>UI 요소가 참조하는 시각적 역할. 스프라이트 교체는 역할 단위로 이뤄진다.</summary>
    public enum UIStyleRole
    {
        ScreenBackground,
        Panel,
        Card,
        ButtonPrimary,
        ButtonSecondary,
        ButtonDanger,
        InputField,
        Badge,
        ProgressTrack,
        ProgressFillHero,
        ProgressFillEnemy,
        DimOverlay,
        Portrait,
        Highlight,
        ScrollTrack,
        ScrollHandle
    }

    /// <summary>
    /// 스킨 교체 지점. 추후 무료 GUI 에셋을 적용할 때 이 에셋의 스프라이트/색/폰트만 바꾸면
    /// UIStyleBinder가 붙은 모든 UI가 일괄 리스킨된다. 코드 수정 불필요.
    /// </summary>
    [CreateAssetMenu(menuName = "MultiCharacterSample/UI Theme", fileName = "UITheme")]
    public class UITheme : ScriptableObject
    {
        [System.Serializable]
        public class Style
        {
            public Sprite sprite;
            public Color color = Color.white;
            public Image.Type imageType = Image.Type.Sliced;
        }

        public TMP_FontAsset font;

        public Style screenBackground = new Style();
        public Style panel = new Style();
        public Style card = new Style();
        public Style buttonPrimary = new Style();
        public Style buttonSecondary = new Style();
        public Style buttonDanger = new Style();
        public Style inputField = new Style();
        public Style badge = new Style();
        public Style progressTrack = new Style();
        public Style progressFillHero = new Style();
        public Style progressFillEnemy = new Style();
        public Style dimOverlay = new Style();
        public Style portrait = new Style();
        public Style highlight = new Style();
        public Style scrollTrack = new Style();
        public Style scrollHandle = new Style();

        public Style GetStyle(UIStyleRole role)
        {
            switch (role)
            {
                case UIStyleRole.ScreenBackground: return screenBackground;
                case UIStyleRole.Panel: return panel;
                case UIStyleRole.Card: return card;
                case UIStyleRole.ButtonPrimary: return buttonPrimary;
                case UIStyleRole.ButtonSecondary: return buttonSecondary;
                case UIStyleRole.ButtonDanger: return buttonDanger;
                case UIStyleRole.InputField: return inputField;
                case UIStyleRole.Badge: return badge;
                case UIStyleRole.ProgressTrack: return progressTrack;
                case UIStyleRole.ProgressFillHero: return progressFillHero;
                case UIStyleRole.ScrollTrack: return scrollTrack;
                case UIStyleRole.ScrollHandle: return scrollHandle;
                case UIStyleRole.ProgressFillEnemy: return progressFillEnemy;
                case UIStyleRole.DimOverlay: return dimOverlay;
                case UIStyleRole.Portrait: return portrait;
                default: return highlight;
            }
        }
    }
}
