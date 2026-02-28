using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TacticsGame.AI;
using TacticsGame.Core;

namespace TacticsGame.UI
{
    /// <summary>
    /// Detects mouse hover over enemy visualization (body, ghost, path, markers, lines).
    /// Shows enemy stats in a fixed top-left panel and triggers ghost animation on hover.
    /// </summary>
    public class IntentionTooltip : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI label;

        private RectTransform panelRect;
        private Canvas canvas;
        private Camera mainCam;
        private IntentionVisualizer hoveredVisualizer;

        private void Start()
        {
            canvas = GetComponent<Canvas>();
            mainCam = Camera.main;
            if (panel != null)
            {
                panelRect = panel.GetComponent<RectTransform>();

                // 固定到左上角
                panelRect.anchorMin = new Vector2(0, 1);
                panelRect.anchorMax = new Vector2(0, 1);
                panelRect.pivot = new Vector2(0, 1);
                panelRect.anchoredPosition = new Vector2(10, -10);

                panel.SetActive(false);
            }
        }

        private void Update()
        {
            if (mainCam == null)
            {
                mainCam = Camera.main;
                if (mainCam == null) return;
            }

            Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 100f, ~0, QueryTriggerInteraction.Collide);

            // 找到最近命中所属的 IntentionVisualizer
            IntentionVisualizer newHovered = null;
            float hoveredDist = float.MaxValue;

            foreach (var hit in hits)
            {
                IntentionVisualizer candidate = null;

                // 直接命中敌人本体
                var vis = hit.collider.GetComponent<IntentionVisualizer>();
                if (vis != null) candidate = vis;

                // 命中可视化部件（ghost、标记、路径碰撞体、弹道线等）
                var part = hit.collider.GetComponent<VisualizationPart>();
                if (part != null && part.owner != null) candidate = part.owner;

                if (candidate != null && hit.distance < hoveredDist)
                {
                    newHovered = candidate;
                    hoveredDist = hit.distance;
                }
            }

            // --- 信息面板显示 ---
            if (newHovered != null)
            {
                var unit = newHovered.GetComponent<UnitBase>();
                if (unit != null && unit.stats != null)
                    ShowUnitInfo(unit);
                else
                    Hide();
            }
            else
            {
                Hide();
            }

            // --- 悬停动画切换 ---
            if (newHovered != hoveredVisualizer)
            {
                if (hoveredVisualizer != null) hoveredVisualizer.SetAnimating(false);
                if (newHovered != null) newHovered.SetAnimating(true);
                hoveredVisualizer = newHovered;
            }
        }

        private void ShowUnitInfo(UnitBase unit)
        {
            if (panel == null) return;

            var s = unit.stats;
            float hpRatio = unit.currentHealth / s.maxHealth;
            string hpColor = hpRatio > 0.6f ? "#88FF88" : hpRatio > 0.3f ? "#FFFF44" : "#FF4444";

            string attackType = s.canRangedAttack ? "Ranged" : "Melee";

            label.text =
                $"<b><size=120%>{s.unitName}</size></b>  <color=#AAAAAA><size=90%>{attackType}</size></color>\n" +
                $"HP  <color={hpColor}>{unit.currentHealth:F0}</color> / {s.maxHealth:F0}\n" +
                $"ATK  {s.attackPower:F0}    Range  {s.attackRange:F0}\n" +
                $"Speed  {s.moveSpeed:F0}";

            panel.SetActive(true);
        }

        private void Hide()
        {
            if (panel == null) return;
            panel.SetActive(false);
        }
    }
}
