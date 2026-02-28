using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TacticsGame.Core
{
    public class TurnManager : MonoBehaviour
    {
        public const float ACTION_BAR_MAX = 100f;

        [Header("Debug")]
        [SerializeField] private float tickSpeed = 0.05f; // Seconds between ticks in visual mode
        [SerializeField] private float animationSpeed = 50f; // How fast action bars animate (ticks per second)

        private List<UnitBase> allUnits = new List<UnitBase>();
        private UnitBase currentTurnUnit;
        private Coroutine tickCoroutine;

        public UnitBase CurrentTurnUnit => currentTurnUnit;
        public IReadOnlyList<UnitBase> AllUnits => allUnits;

        public void RegisterUnit(UnitBase unit)
        {
            if (!allUnits.Contains(unit))
            {
                allUnits.Add(unit);
                unit.actionBar = 0f;
            }
        }

        public void UnregisterUnit(UnitBase unit)
        {
            allUnits.Remove(unit);
        }

        public void StartTickLoop()
        {
            if (tickCoroutine != null)
                StopCoroutine(tickCoroutine);
            tickCoroutine = StartCoroutine(TickLoop());
        }

        public void StopTickLoop()
        {
            if (tickCoroutine != null)
            {
                StopCoroutine(tickCoroutine);
                tickCoroutine = null;
            }
        }

        private IEnumerator TickLoop()
        {
            while (true)
            {
                // Remove dead units
                allUnits.RemoveAll(u => u == null || u.IsDead);

                if (allUnits.Count == 0) yield break;

                // Find the unit closest to 100
                UnitBase nextUnit = null;
                float minTicksNeeded = float.MaxValue;

                foreach (var unit in allUnits)
                {
                    if (unit.IsDead) continue;
                    float ticksNeeded = (ACTION_BAR_MAX - unit.actionBar) / unit.stats.speed;
                    if (ticksNeeded < minTicksNeeded)
                    {
                        minTicksNeeded = ticksNeeded;
                        nextUnit = unit;
                    }
                }

                if (nextUnit == null) yield break;

                // Advance all bars frame-by-frame for smooth animation
                float ticksRemaining = minTicksNeeded;
                while (ticksRemaining > 0f)
                {
                    float ticksThisFrame = animationSpeed * Time.deltaTime;
                    ticksThisFrame = Mathf.Min(ticksThisFrame, ticksRemaining);
                    foreach (var unit in allUnits)
                    {
                        if (unit.IsDead) continue;
                        unit.actionBar += unit.stats.speed * ticksThisFrame;
                    }
                    ticksRemaining -= ticksThisFrame;
                    yield return null;
                }

                // Clamp and find all units at or above 100
                var readyUnits = allUnits
                    .Where(u => !u.IsDead && u.actionBar >= ACTION_BAR_MAX)
                    .OrderByDescending(u => u.stats.speed)
                    .ThenBy(u => u.IsPlayer ? 0 : 1) // Player priority on tie
                    .ToList();

                // Process each ready unit's turn
                foreach (var unit in readyUnits)
                {
                    if (unit.IsDead) continue;

                    currentTurnUnit = unit;
                    unit.actionBar = ACTION_BAR_MAX; // Clamp to max

                    // Notify turn start
                    EventBus.RaiseUnitTurnStart(unit);
                    unit.OnTurnStart();

                    if (unit.IsPlayer)
                    {
                        GameManager.Instance.SetState(GameState.PlayerTurn);
                    }
                    else
                    {
                        GameManager.Instance.SetState(GameState.EnemyTurn);
                    }

                    // Wait until the turn is complete
                    yield return new WaitUntil(() => GameManager.Instance.CurrentState == GameState.TickRunning);

                    EventBus.RaiseTurnOrderChanged();
                }

                currentTurnUnit = null;

                // Small visual delay between ticks
                yield return new WaitForSeconds(tickSpeed);
            }
        }

        public void ResetBar(UnitBase unit)
        {
            unit.actionBar = 0f;
        }

        public void EndCurrentTurn()
        {
            if (currentTurnUnit == null) return;

            currentTurnUnit.OnTurnEnd();
            ResetBar(currentTurnUnit);

            GameManager.Instance.SetState(GameState.TickRunning);
        }

        /// <summary>
        /// Get predicted turn order (who acts next based on current bars).
        /// </summary>
        public List<(UnitBase unit, float ticksUntilTurn)> GetTurnOrder(int count = 10)
        {
            var result = new List<(UnitBase, float)>();
            var simBars = new Dictionary<UnitBase, float>();

            foreach (var unit in allUnits)
            {
                if (!unit.IsDead)
                    simBars[unit] = unit.actionBar;
            }

            float totalTicks = 0f;

            for (int i = 0; i < count && simBars.Count > 0; i++)
            {
                float minTicks = float.MaxValue;
                UnitBase nextUnit = null;

                foreach (var kvp in simBars)
                {
                    float ticks = (ACTION_BAR_MAX - kvp.Value) / kvp.Key.stats.speed;
                    if (ticks < minTicks)
                    {
                        minTicks = ticks;
                        nextUnit = kvp.Key;
                    }
                }

                if (nextUnit == null) break;

                totalTicks += minTicks;

                // Advance all bars
                var keys = simBars.Keys.ToList();
                foreach (var key in keys)
                {
                    simBars[key] += key.stats.speed * minTicks;
                }

                result.Add((nextUnit, totalTicks));
                simBars[nextUnit] = 0f; // Reset after acting
            }

            return result;
        }
    }
}
