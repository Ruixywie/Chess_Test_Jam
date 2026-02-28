using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TacticsGame.AI;
using TacticsGame.Player;

namespace TacticsGame.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private TurnManager turnManager;

        private GameState currentState = GameState.Initializing;
        private List<UnitBase> allUnits = new List<UnitBase>();
        private PlayerUnit player;

        public GameState CurrentState => currentState;
        public TurnManager TurnManager => turnManager;
        public PlayerUnit Player => player;
        public IReadOnlyList<UnitBase> AllUnits => allUnits;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // Called explicitly by DemoSceneBuilder after NavMesh is ready
        public void InitializeGame()
        {
            Debug.Log("[GameManager] InitializeGame() called.");

            // Find all units in scene
            allUnits = FindObjectsByType<UnitBase>(FindObjectsSortMode.None).ToList();
            player = allUnits.OfType<PlayerUnit>().FirstOrDefault();

            Debug.Log($"[GameManager] Found {allUnits.Count} units, player={player != null}");

            if (player == null)
            {
                Debug.LogError("[GameManager] No PlayerUnit found in scene!");
                return;
            }

            if (turnManager == null)
            {
                Debug.LogError("[GameManager] TurnManager is null! Attempting to find...");
                turnManager = FindFirstObjectByType<TurnManager>();
                if (turnManager == null)
                {
                    Debug.LogError("[GameManager] Could not find TurnManager! Game cannot start.");
                    return;
                }
            }

            // Initialize all units
            foreach (var unit in allUnits)
            {
                if (unit.stats == null)
                {
                    Debug.LogError($"[GameManager] Unit '{unit.name}' has null stats! Skipping.");
                    continue;
                }
                unit.Initialize();
                turnManager.RegisterUnit(unit);
            }

            // Have all enemies formulate their initial plans
            foreach (var enemy in allUnits.OfType<EnemyUnit>())
            {
                if (enemy.Brain != null)
                {
                    try
                    {
                        enemy.Brain.FormulatePlan();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[GameManager] Failed to formulate plan for {enemy.name}: {e.Message}");
                    }
                }
            }

            // Subscribe to events
            EventBus.OnUnitDied += OnUnitDied;

            Debug.Log($"[GameManager] Initialized with {allUnits.Count} units ({allUnits.Count(u => !u.IsPlayer)} enemies)");

            // Start the game
            SetState(GameState.TickRunning);
            turnManager.StartTickLoop();
        }

        public void SetState(GameState newState)
        {
            if (currentState == newState) return;

            currentState = newState;
            EventBus.RaiseGameStateChanged(newState);
            Debug.Log($"[GameManager] State changed to: {newState}");
        }

        private void OnUnitDied(UnitBase unit)
        {
            turnManager.UnregisterUnit(unit);

            if (unit.IsPlayer)
            {
                SetState(GameState.GameOver);
                turnManager.StopTickLoop();
                Debug.Log("[GameManager] GAME OVER - Player defeated!");
            }
            else
            {
                bool allEnemiesDead = allUnits
                    .Where(u => !u.IsPlayer)
                    .All(u => u.IsDead);

                if (allEnemiesDead)
                {
                    SetState(GameState.GameOver);
                    turnManager.StopTickLoop();
                    Debug.Log("[GameManager] VICTORY - All enemies defeated!");
                }
            }
        }

        public void EndPlayerTurn()
        {
            if (currentState != GameState.PlayerTurn) return;
            turnManager.EndCurrentTurn();
        }

        private void OnDestroy()
        {
            EventBus.OnUnitDied -= OnUnitDied;
            EventBus.ClearAll();
        }
    }
}
