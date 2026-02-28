using System.Collections;
using UnityEngine;
using TacticsGame.Core;

namespace TacticsGame.AI
{
    public class EnemyTurnHandler : MonoBehaviour
    {
        [SerializeField] private float delayBeforeAction = 0.5f;
        [SerializeField] private float delayAfterAction = 0.5f;

        private void OnEnable()
        {
            EventBus.OnUnitTurnStart += OnUnitTurnStart;
        }

        private void OnDisable()
        {
            EventBus.OnUnitTurnStart -= OnUnitTurnStart;
        }

        private void OnUnitTurnStart(UnitBase unit)
        {
            if (unit is EnemyUnit enemy)
            {
                StartCoroutine(HandleEnemyTurn(enemy));
            }
        }

        private IEnumerator HandleEnemyTurn(EnemyUnit enemy)
        {
            yield return new WaitForSeconds(delayBeforeAction);

            if (enemy.Brain != null && enemy.Brain.CurrentPlan != null)
            {
                // Clear intent visualization before executing
                var visualizer = enemy.GetComponent<IntentionVisualizer>();
                if (visualizer != null)
                {
                    visualizer.ClearVisualization();
                }

                // Execute the plan
                yield return enemy.Brain.ExecutePlan();

                yield return new WaitForSeconds(delayAfterAction);
            }

            // End the turn
            if (!enemy.IsDead)
            {
                GameManager.Instance.TurnManager.EndCurrentTurn();
            }
            else
            {
                GameManager.Instance.SetState(GameState.TickRunning);
            }
        }
    }
}
