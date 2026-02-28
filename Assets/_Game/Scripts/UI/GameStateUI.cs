using UnityEngine;
using TMPro;
using TacticsGame.Core;

namespace TacticsGame.UI
{
    public class GameStateUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI stateText;
        [SerializeField] private TextMeshProUGUI turnInfoText;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TextMeshProUGUI gameOverText;

        private void OnEnable()
        {
            EventBus.OnGameStateChanged += OnGameStateChanged;
            EventBus.OnUnitTurnStart += OnUnitTurnStart;
        }

        private void OnDisable()
        {
            EventBus.OnGameStateChanged -= OnGameStateChanged;
            EventBus.OnUnitTurnStart -= OnUnitTurnStart;
        }

        private void Start()
        {
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);
        }

        private void OnGameStateChanged(GameState state)
        {
            if (stateText == null) return;

            switch (state)
            {
                case GameState.Initializing:
                    stateText.text = "Initializing...";
                    break;
                case GameState.TickRunning:
                    stateText.text = "Tick Running...";
                    break;
                case GameState.PlayerTurn:
                    stateText.text = "Player Turn";
                    break;
                case GameState.EnemyTurn:
                    stateText.text = "Enemy Turn";
                    break;
                case GameState.GameOver:
                    stateText.text = "Game Over";
                    ShowGameOver();
                    break;
            }
        }

        private void OnUnitTurnStart(UnitBase unit)
        {
            if (turnInfoText != null)
            {
                turnInfoText.text = $"Current: {unit.stats.unitName}";
            }
        }

        private void ShowGameOver()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);

                bool playerWon = GameManager.Instance != null &&
                                 GameManager.Instance.Player != null &&
                                 !GameManager.Instance.Player.IsDead;
                if (gameOverText != null)
                {
                    gameOverText.text = playerWon ? "Victory!\nAll enemies defeated" : "Defeat!\nPlayer has fallen";
                }
            }
        }
    }
}
