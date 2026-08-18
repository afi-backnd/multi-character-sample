using MultiCharacterSample.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MultiCharacterSample.UI
{
    /// <summary>능력치 강화 카드 1장의 뷰. 로직은 GameScreen이 담당한다.</summary>
    public class StatUpgradeRow : MonoBehaviour
    {
        public StatType stat;
        public TMP_Text nameText;
        public TMP_Text levelText;
        public TMP_Text valueText;
        public TMP_Text costText;
        public Button upgradeButton;
    }
}
