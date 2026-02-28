using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TacticsGame.Core;
using TacticsGame.Player;

namespace TacticsGame.UI
{
    public class ActionPointsUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI apText;
        [SerializeField] private Image apBar;

        [Header("Colors")]
        [SerializeField] private Color fullColor = new Color(0.2f, 0.8f, 1f);
        [SerializeField] private Color lowColor = new Color(1f, 0.3f, 0.2f);

        private PlayerUnit player;

        private void Update()
        {
            // Lazy init - keep trying until we get the player reference
            if (player == null)
            {
                if (GameManager.Instance != null)
                    player = GameManager.Instance.Player;
                if (player == null) return;
            }

            float ratio = player.currentAP / player.stats.maxActionPoints;

            if (apText != null)
            {
                apText.text = $"AP: {player.currentAP:F1} / {player.stats.maxActionPoints:F0}";
            }

            if (apBar != null)
            {
                apBar.fillAmount = ratio;
                apBar.color = Color.Lerp(lowColor, fullColor, ratio);
            }
        }
    }
}
