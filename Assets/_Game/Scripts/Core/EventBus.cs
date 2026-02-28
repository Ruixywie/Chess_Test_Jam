using System;
using UnityEngine;
using TacticsGame.Environment;

namespace TacticsGame.Core
{
    /// <summary>
    /// Global event bus for decoupled system communication.
    /// </summary>
    public static class EventBus
    {
        // Turn events
        public static event Action<UnitBase> OnUnitTurnStart;
        public static event Action<UnitBase> OnUnitTurnEnd;
        public static event Action OnTurnOrderChanged;

        // AI events
        public static event Action<UnitBase, ActionPlan> OnPlanCreated;
        public static event Action<UnitBase> OnPlanInterrupted;

        // Combat events
        public static event Action<UnitBase, UnitBase, float> OnUnitDamaged; // attacker, target, damage
        public static event Action<UnitBase> OnUnitDied;

        // Object events
        public static event Action<InteractableObject, float> OnObjectDamaged;
        public static event Action<InteractableObject> OnObjectDestroyed;
        public static event Action<InteractableObject, Vector3, Vector3> OnObjectMoved; // obj, from, to

        // Environment
        public static event Action OnEnvironmentChanged;

        // Game state
        public static event Action<GameState> OnGameStateChanged;

        // --- Raise methods ---

        public static void RaiseUnitTurnStart(UnitBase unit) => OnUnitTurnStart?.Invoke(unit);
        public static void RaiseUnitTurnEnd(UnitBase unit) => OnUnitTurnEnd?.Invoke(unit);
        public static void RaiseTurnOrderChanged() => OnTurnOrderChanged?.Invoke();

        public static void RaisePlanCreated(UnitBase unit, ActionPlan plan) => OnPlanCreated?.Invoke(unit, plan);
        public static void RaisePlanInterrupted(UnitBase unit) => OnPlanInterrupted?.Invoke(unit);

        public static void RaiseUnitDamaged(UnitBase attacker, UnitBase target, float damage) =>
            OnUnitDamaged?.Invoke(attacker, target, damage);
        public static void RaiseUnitDied(UnitBase unit) => OnUnitDied?.Invoke(unit);

        public static void RaiseObjectDamaged(InteractableObject obj, float damage) =>
            OnObjectDamaged?.Invoke(obj, damage);
        public static void RaiseObjectDestroyed(InteractableObject obj) => OnObjectDestroyed?.Invoke(obj);
        public static void RaiseObjectMoved(InteractableObject obj, Vector3 from, Vector3 to) =>
            OnObjectMoved?.Invoke(obj, from, to);

        public static void RaiseEnvironmentChanged() => OnEnvironmentChanged?.Invoke();
        public static void RaiseGameStateChanged(GameState state) => OnGameStateChanged?.Invoke(state);

        /// <summary>
        /// Clear all listeners. Call on scene unload to prevent leaks.
        /// </summary>
        public static void ClearAll()
        {
            OnUnitTurnStart = null;
            OnUnitTurnEnd = null;
            OnTurnOrderChanged = null;
            OnPlanCreated = null;
            OnPlanInterrupted = null;
            OnUnitDamaged = null;
            OnUnitDied = null;
            OnObjectDamaged = null;
            OnObjectDestroyed = null;
            OnObjectMoved = null;
            OnEnvironmentChanged = null;
            OnGameStateChanged = null;
        }
    }
}
