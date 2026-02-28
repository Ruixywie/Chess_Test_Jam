using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TacticsGame.Core;

namespace TacticsGame.UI
{
    public class TurnOrderUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform barContainer;
        [SerializeField] private GameObject unitMarkerPrefab;

        [Header("Settings")]
        [SerializeField] private float barWidth = 600f;
        [SerializeField] private Color playerColor = new Color(0.2f, 0.8f, 0.3f);
        [SerializeField] private Color enemyColor = new Color(0.8f, 0.2f, 0.2f);

        private Dictionary<UnitBase, RectTransform> unitMarkers = new Dictionary<UnitBase, RectTransform>();
        private TurnManager turnManager;
        private bool initialized;

        private void OnEnable()
        {
            EventBus.OnTurnOrderChanged += RefreshDisplay;
            EventBus.OnUnitDied += OnUnitDied;
        }

        private void OnDisable()
        {
            EventBus.OnTurnOrderChanged -= RefreshDisplay;
            EventBus.OnUnitDied -= OnUnitDied;
        }

        private void Update()
        {
            // Lazy init
            if (!initialized)
            {
                if (GameManager.Instance != null && GameManager.Instance.TurnManager != null
                    && GameManager.Instance.TurnManager.AllUnits.Count > 0)
                {
                    turnManager = GameManager.Instance.TurnManager;
                    CreateMarkers();
                    initialized = true;
                }
                else
                {
                    return;
                }
            }

            UpdateMarkerPositions();
        }

        private void CreateMarkers()
        {
            // Create finish line at right end of bar
            var finishLine = new GameObject("FinishLine");
            finishLine.transform.SetParent(barContainer, false);
            var finishImg = finishLine.AddComponent<Image>();
            finishImg.color = new Color(1f, 1f, 1f, 0.6f);
            var finishRect = finishLine.GetComponent<RectTransform>();
            finishRect.sizeDelta = new Vector2(2f, 50f);
            finishRect.anchoredPosition = new Vector2(barWidth * 0.5f, 0);

            foreach (var unit in turnManager.AllUnits)
            {
                if (unit.IsDead) continue;
                CreateMarkerForUnit(unit);
            }
        }

        private void CreateMarkerForUnit(UnitBase unit)
        {
            if (unitMarkers.ContainsKey(unit)) return;

            GameObject marker = new GameObject($"Marker_{unit.stats.unitName}");
            marker.transform.SetParent(barContainer, false);

            var img = marker.AddComponent<Image>();
            img.color = unit.IsPlayer ? playerColor : enemyColor;

            var rect = marker.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(32f, 32f);

            // Initial letter inside the icon
            var letterObj = new GameObject("Letter");
            letterObj.transform.SetParent(marker.transform, false);
            var letter = letterObj.AddComponent<TextMeshProUGUI>();
            string unitName = unit.stats.unitName;
            letter.text = unitName.Length > 0 ? unitName.Substring(0, 1).ToUpper() : "?";
            letter.fontSize = 16;
            letter.fontStyle = FontStyles.Bold;
            letter.alignment = TextAlignmentOptions.Center;
            letter.color = Color.white;
            var letterRect = letterObj.GetComponent<RectTransform>();
            letterRect.anchorMin = Vector2.zero;
            letterRect.anchorMax = Vector2.one;
            letterRect.sizeDelta = Vector2.zero;

            // Name label above the icon
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(marker.transform, false);
            var label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = unitName.Length > 3 ? unitName.Substring(0, 3) : unitName;
            label.fontSize = 9;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            var labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(50f, 16f);
            labelRect.anchoredPosition = new Vector2(0, 24f);

            unitMarkers[unit] = rect;
        }

        private void UpdateMarkerPositions()
        {
            foreach (var kvp in unitMarkers)
            {
                var unit = kvp.Key;
                var rect = kvp.Value;

                if (unit == null || unit.IsDead)
                {
                    rect.gameObject.SetActive(false);
                    continue;
                }

                float progress = unit.actionBar / TurnManager.ACTION_BAR_MAX;
                float xPos = progress * barWidth - barWidth * 0.5f;
                rect.anchoredPosition = new Vector2(xPos, 0);
            }
        }

        private void RefreshDisplay() { }

        private void OnUnitDied(UnitBase unit)
        {
            if (unitMarkers.TryGetValue(unit, out var rect))
            {
                rect.gameObject.SetActive(false);
            }
        }
    }
}
