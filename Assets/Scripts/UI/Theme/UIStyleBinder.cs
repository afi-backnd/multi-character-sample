using UnityEngine;
using UnityEngine.UI;

namespace MultiCharacterSample.UI
{
    /// <summary>
    /// Image에 붙어 테마의 역할(Role) 스타일을 적용받는다.
    /// 스킨 교체 시 개별 오브젝트를 만질 필요 없이 UITheme 에셋만 수정하면 된다.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class UIStyleBinder : MonoBehaviour
    {
        public UIStyleRole role;

        [Tooltip("체크하면 테마 색을 무시하고 현재 색을 유지한다(런타임 틴트용).")]
        public bool keepLocalColor;

        public void Apply(UITheme theme)
        {
            var style = theme.GetStyle(role);
            var image = GetComponent<Image>();
            image.sprite = style.sprite;
            image.type = style.imageType;
            if (!keepLocalColor) image.color = style.color;
        }
    }
}
