using System.Collections;
using UnityEngine;
using TacticsGame.Core;
using TacticsGame.Combat;
using TacticsGame.Movement;
using TacticsGame.Environment;

namespace TacticsGame.Player
{
    public enum PlayerActionMode
    {
        None,
        Move,
        Attack,
        MoveObject
    }

    public class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FreeMovementSystem movementSystem;

        [Header("Attack Preview")]
        [SerializeField] private Color aimLineColor = new Color(1f, 0.9f, 0.2f, 0.8f);

        private PlayerUnit playerUnit;
        private PlayerActionMode currentMode = PlayerActionMode.None;
        private Camera mainCamera;
        private LineRenderer aimLine;
        private InteractableObject selectedObject;
        private bool isActing;

        // Object movement
        private InteractableObject objectToMove;

        public PlayerActionMode CurrentMode => currentMode;
        public bool IsActing => isActing;

        private void Awake()
        {
            playerUnit = GetComponent<PlayerUnit>();
            mainCamera = Camera.main;

            if (movementSystem == null)
                movementSystem = GetComponent<FreeMovementSystem>();

            // Create aim line
            GameObject aimObj = new GameObject("AimLine");
            aimObj.transform.SetParent(transform);
            aimLine = aimObj.AddComponent<LineRenderer>();
            aimLine.startWidth = 0.03f;
            aimLine.endWidth = 0.03f;
            aimLine.material = new Material(Shader.Find("Sprites/Default"));
            aimLine.startColor = aimLineColor;
            aimLine.endColor = aimLineColor;
            aimLine.positionCount = 0;
            aimLine.useWorldSpace = true;
        }

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
            if (state == GameState.PlayerTurn)
            {
                SetMode(PlayerActionMode.Move);
            }
            else
            {
                SetMode(PlayerActionMode.None);
                movementSystem.Cleanup();
                HideAimLine();
            }
        }

        public void SetMode(PlayerActionMode mode)
        {
            currentMode = mode;

            // Cleanup previous mode visuals
            movementSystem.HidePathPreview();
            movementSystem.HideMoveRange();
            HideAimLine();
            objectToMove = null;

            switch (mode)
            {
                case PlayerActionMode.Move:
                    movementSystem.ShowMoveRange(playerUnit);
                    break;
                case PlayerActionMode.Attack:
                    break;
                case PlayerActionMode.MoveObject:
                    break;
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.CurrentState != GameState.PlayerTurn) return;
            if (isActing) return;
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            HandleInput();
        }

        private void HandleInput()
        {
            // Mode switching via keyboard
            if (Input.GetKeyDown(KeyCode.Alpha1)) SetMode(PlayerActionMode.Move);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SetMode(PlayerActionMode.Attack);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SetMode(PlayerActionMode.MoveObject);
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                GameManager.Instance.EndPlayerTurn();
                return;
            }

            // Cancel with right click
            if (Input.GetMouseButtonDown(1))
            {
                SetMode(PlayerActionMode.Move);
                return;
            }

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            switch (currentMode)
            {
                case PlayerActionMode.Move:
                    HandleMoveMode(ray);
                    break;
                case PlayerActionMode.Attack:
                    HandleAttackMode(ray);
                    break;
                case PlayerActionMode.MoveObject:
                    HandleMoveObjectMode(ray);
                    break;
            }
        }

        private void HandleMoveMode(Ray ray)
        {
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                // Preview path to cursor position
                if (UnityEngine.AI.NavMesh.SamplePosition(hit.point, out UnityEngine.AI.NavMeshHit navHit, 2f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    movementSystem.PreviewPath(playerUnit, navHit.position);
                }

                // Click to move
                if (Input.GetMouseButtonDown(0))
                {
                    if (UnityEngine.AI.NavMesh.SamplePosition(hit.point, out navHit, 2f, UnityEngine.AI.NavMesh.AllAreas))
                    {
                        float pathDist = GetPathDistance(playerUnit.transform.position, navHit.position);
                        float apCost = playerUnit.GetMovementAPCost(pathDist);

                        if (playerUnit.HasEnoughAP(apCost))
                        {
                            StartCoroutine(DoMove(navHit.position));
                        }
                    }
                }
            }
        }

        private void HandleAttackMode(Ray ray)
        {
            // Show aim line from player toward cursor
            Vector3 origin = playerUnit.transform.position + Vector3.up;

            if (Physics.Raycast(ray, out RaycastHit cursorHit, 100f))
            {
                Vector3 targetPoint = cursorHit.point + Vector3.up;
                Vector3 direction = (targetPoint - origin).normalized;

                // Predict hit
                var (hitPoint, hitObj) = CombatManager.Instance.PredictRangedHit(origin, direction, playerUnit.stats.attackRange);

                ShowAimLine(origin, hitPoint);

                // Click to fire
                if (Input.GetMouseButtonDown(0))
                {
                    if (playerUnit.HasEnoughAP(playerUnit.stats.rangedAttackAPCost))
                    {
                        StartCoroutine(DoRangedAttack(cursorHit.point));
                    }
                }
            }
        }

        private void HandleMoveObjectMode(Ray ray)
        {
            if (objectToMove == null)
            {
                // Phase 1: Select an object to move
                if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                {
                    var obj = hit.collider.GetComponent<InteractableObject>();
                    if (obj != null && obj.IsMovable)
                    {
                        // Highlight object (could add visual feedback here)
                        if (Input.GetMouseButtonDown(0))
                        {
                            float dist = Vector3.Distance(playerUnit.transform.position, obj.transform.position);
                            if (dist <= 5f) // Interact range
                            {
                                objectToMove = obj;
                                Debug.Log($"[Player] Selected object: {obj.properties?.objectName}. Click to place.");
                            }
                            else
                            {
                                Debug.Log("[Player] Object too far to interact with!");
                            }
                        }
                    }
                }
            }
            else
            {
                // Phase 2: Choose where to place the object
                if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                {
                    // Preview placement
                    // Could show ghost of object at cursor position

                    if (Input.GetMouseButtonDown(0))
                    {
                        if (playerUnit.HasEnoughAP(playerUnit.stats.moveObjectAPCost))
                        {
                            StartCoroutine(DoMoveObject(objectToMove, hit.point));
                        }
                    }
                }
            }
        }

        private IEnumerator DoMove(Vector3 target)
        {
            isActing = true;
            yield return movementSystem.MoveUnit(playerUnit, target);
            isActing = false;

            // Refresh move range after moving
            if (currentMode == PlayerActionMode.Move)
            {
                movementSystem.ShowMoveRange(playerUnit);
            }

            CheckAutoEndTurn();
        }

        private IEnumerator DoRangedAttack(Vector3 targetPos)
        {
            isActing = true;
            HideAimLine();
            yield return CombatManager.Instance.RangedAttack(playerUnit, targetPos);
            isActing = false;

            CheckAutoEndTurn();
        }

        private IEnumerator DoMoveObject(InteractableObject obj, Vector3 targetPos)
        {
            isActing = true;

            obj.MoveTo(targetPos);
            playerUnit.ConsumeAP(playerUnit.stats.moveObjectAPCost);
            objectToMove = null;

            yield return new WaitForSeconds(0.3f);
            isActing = false;

            Debug.Log($"[Player] Moved {obj.properties?.objectName} to {targetPos}");
            CheckAutoEndTurn();
        }

        private void CheckAutoEndTurn()
        {
            // Auto-end turn if no AP left
            if (playerUnit.currentAP <= 0.1f)
            {
                GameManager.Instance.EndPlayerTurn();
            }
        }

        private void ShowAimLine(Vector3 from, Vector3 to)
        {
            aimLine.positionCount = 2;
            aimLine.SetPosition(0, from);
            aimLine.SetPosition(1, to);
        }

        private void HideAimLine()
        {
            aimLine.positionCount = 0;
        }

        private float GetPathDistance(Vector3 from, Vector3 to)
        {
            var path = new UnityEngine.AI.NavMeshPath();
            UnityEngine.AI.NavMesh.CalculatePath(from, to, UnityEngine.AI.NavMesh.AllAreas, path);
            return NavMeshUtils.CalculatePathLength(path);
        }
    }
}
