using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TacticsGame.Core;
using TacticsGame.Player;

namespace TacticsGame.UI
{
    public class ActionMenuUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button moveButton;
        [SerializeField] private Button attackButton;
        [SerializeField] private Button moveObjectButton;
        [SerializeField] private Button endTurnButton;

        [Header("Colors")]
        [SerializeField] private Color activeColor = new Color(0.3f, 0.7f, 1f);
        [SerializeField] private Color normalColor = new Color(0.8f, 0.8f, 0.8f);

        private PlayerController playerController;
        private CanvasGroup canvasGroup;
        private bool initialized;

        private void OnEnable()
        {
            EventBus.OnGameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            EventBus.OnGameStateChanged -= OnGameStateChanged;
        }

        private void OnGameStateChanged(GameState state)
        {
            bool show = state == GameState.PlayerTurn;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = show ? 1f : 0f;
                canvasGroup.interactable = show;
                canvasGroup.blocksRaycasts = show;
            }
        }

        private void EnsureInitialized()
        {
            if (initialized) return;

            canvasGroup = GetComponent<CanvasGroup>();

            if (GameManager.Instance != null && GameManager.Instance.Player != null)
            {
                playerController = GameManager.Instance.Player.GetComponent<PlayerController>();
            }

            // Bind button listeners (only if buttons have been set via reflection)
            if (moveButton != null)
            {
                moveButton.onClick.AddListener(() => SetMode(PlayerActionMode.Move));
                if (attackButton != null)
                    attackButton.onClick.AddListener(() => SetMode(PlayerActionMode.Attack));
                if (moveObjectButton != null)
                    moveObjectButton.onClick.AddListener(() => SetMode(PlayerActionMode.MoveObject));
                if (endTurnButton != null)
                    endTurnButton.onClick.AddListener(() =>
                    {
                        if (GameManager.Instance != null)
                            GameManager.Instance.EndPlayerTurn();
                    });

                initialized = true;
            }
        }

        private void Update()
        {
            EnsureInitialized();
            if (playerController == null) return;
            UpdateButtonColors();
        }

        private void SetMode(PlayerActionMode mode)
        {
            if (playerController != null)
                playerController.SetMode(mode);
        }

        private void UpdateButtonColors()
        {
            SetButtonColor(moveButton, playerController.CurrentMode == PlayerActionMode.Move);
            SetButtonColor(attackButton, playerController.CurrentMode == PlayerActionMode.Attack);
            SetButtonColor(moveObjectButton, playerController.CurrentMode == PlayerActionMode.MoveObject);
        }

        private void SetButtonColor(Button btn, bool active)
        {
            if (btn == null) return;
            var colors = btn.colors;
            colors.normalColor = active ? activeColor : normalColor;
            btn.colors = colors;
        }
    }
}
