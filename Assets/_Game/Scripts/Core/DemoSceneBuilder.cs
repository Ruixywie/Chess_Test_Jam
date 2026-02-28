using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Unity.AI.Navigation;
using TMPro;
using TacticsGame.AI;
using TacticsGame.Player;
using TacticsGame.Environment;
using TacticsGame.Combat;
using TacticsGame.UI;
using TacticsGame.Unit;

namespace TacticsGame.Core
{
    public class DemoSceneBuilder : MonoBehaviour
    {
        [Header("Unit Stats (assign in inspector)")]
        public UnitStatsSO playerStats;
        public UnitStatsSO meleeEnemyStats;
        public UnitStatsSO rangedEnemyStats;

        [Header("Object Properties (assign in inspector)")]
        public ObjectPropertySO woodenBoxProps;
        public ObjectPropertySO barrelProps;
        public ObjectPropertySO floorProps;
        public ObjectPropertySO stairProps;
        public ObjectPropertySO wallProps;

        // Materials
        private Material groundMat;
        private Material wallMat;
        private Material floorMat;
        private Material stairMat;
        private Material roofMat;
        private Material playerMat;
        private Material meleeEnemyMat;
        private Material rangedEnemyMat;
        private Material boxMat;
        private Material barrelMat;

        private NavMeshSurface navMeshSurface;

        private IEnumerator Start()
        {
            EnsureDataAssets();
            CreateMaterials();
            BuildEnvironment();
            SpawnInteractableObjects();
            SetupNavMesh();

            // Wait 1 frame for NavMesh to fully register
            yield return null;

            // Spawn units AFTER NavMesh is ready so NavMeshAgents initialize correctly
            SpawnUnits();
            SetupSystems();
            SetupUI();
            SetupCamera();

            // Wait another frame for all components to fully initialize
            yield return null;

            // Now manually initialize the game
            try
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.InitializeGame();
                }
                else
                {
                    Debug.LogError("[DemoSceneBuilder] GameManager.Instance is null after setup!");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DemoSceneBuilder] InitializeGame failed: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// If ScriptableObject references are not assigned, create runtime defaults.
        /// </summary>
        private void EnsureDataAssets()
        {
            if (playerStats == null)
            {
                Debug.LogWarning("[DemoSceneBuilder] playerStats not assigned, creating runtime default.");
                playerStats = ScriptableObject.CreateInstance<UnitStatsSO>();
                playerStats.unitName = "Player";
                playerStats.maxHealth = 150f;
                playerStats.speed = 10f;
                playerStats.maxActionPoints = 10f;
                playerStats.attackPower = 15f;
                playerStats.attackRange = 30f;
                playerStats.moveSpeed = 6f;
                playerStats.movementAPPerMeter = 0.5f;
                playerStats.meleeAttackAPCost = 3f;
                playerStats.rangedAttackAPCost = 4f;
                playerStats.moveObjectAPCost = 3f;
                playerStats.canRangedAttack = true;
                playerStats.canMeleeAttack = false;
            }

            if (meleeEnemyStats == null)
            {
                Debug.LogWarning("[DemoSceneBuilder] meleeEnemyStats not assigned, creating runtime default.");
                meleeEnemyStats = ScriptableObject.CreateInstance<UnitStatsSO>();
                meleeEnemyStats.unitName = "Melee";
                meleeEnemyStats.maxHealth = 80f;
                meleeEnemyStats.speed = 5f;
                meleeEnemyStats.maxActionPoints = 8f;
                meleeEnemyStats.attackPower = 20f;
                meleeEnemyStats.attackRange = 2f;
                meleeEnemyStats.moveSpeed = 4f;
                meleeEnemyStats.movementAPPerMeter = 0.4f;
                meleeEnemyStats.meleeAttackAPCost = 3f;
                meleeEnemyStats.rangedAttackAPCost = 0f;
                meleeEnemyStats.moveObjectAPCost = 3f;
                meleeEnemyStats.canRangedAttack = false;
                meleeEnemyStats.canMeleeAttack = true;
            }

            if (rangedEnemyStats == null)
            {
                Debug.LogWarning("[DemoSceneBuilder] rangedEnemyStats not assigned, creating runtime default.");
                rangedEnemyStats = ScriptableObject.CreateInstance<UnitStatsSO>();
                rangedEnemyStats.unitName = "Ranged";
                rangedEnemyStats.maxHealth = 60f;
                rangedEnemyStats.speed = 4f;
                rangedEnemyStats.maxActionPoints = 8f;
                rangedEnemyStats.attackPower = 12f;
                rangedEnemyStats.attackRange = 20f;
                rangedEnemyStats.moveSpeed = 3f;
                rangedEnemyStats.movementAPPerMeter = 0.5f;
                rangedEnemyStats.meleeAttackAPCost = 0f;
                rangedEnemyStats.rangedAttackAPCost = 4f;
                rangedEnemyStats.moveObjectAPCost = 3f;
                rangedEnemyStats.canRangedAttack = true;
                rangedEnemyStats.canMeleeAttack = false;
            }

            if (woodenBoxProps == null)
            {
                woodenBoxProps = ScriptableObject.CreateInstance<ObjectPropertySO>();
                woodenBoxProps.objectName = "Wooden Box";
                woodenBoxProps.durability = 30f;
                woodenBoxProps.weight = 2f;
                woodenBoxProps.isMovable = true;
                woodenBoxProps.isDestructible = true;
                woodenBoxProps.isStructural = false;
            }

            if (barrelProps == null)
            {
                barrelProps = ScriptableObject.CreateInstance<ObjectPropertySO>();
                barrelProps.objectName = "Barrel";
                barrelProps.durability = 25f;
                barrelProps.weight = 1.5f;
                barrelProps.isMovable = true;
                barrelProps.isDestructible = true;
                barrelProps.isStructural = false;
            }

            if (floorProps == null)
            {
                floorProps = ScriptableObject.CreateInstance<ObjectPropertySO>();
                floorProps.objectName = "Floor";
                floorProps.durability = 50f;
                floorProps.weight = 999f;
                floorProps.isMovable = false;
                floorProps.isDestructible = true;
                floorProps.isStructural = true;
            }

            if (stairProps == null)
            {
                stairProps = ScriptableObject.CreateInstance<ObjectPropertySO>();
                stairProps.objectName = "Stairs";
                stairProps.durability = 40f;
                stairProps.weight = 999f;
                stairProps.isMovable = false;
                stairProps.isDestructible = true;
                stairProps.isStructural = true;
            }

            if (wallProps == null)
            {
                wallProps = ScriptableObject.CreateInstance<ObjectPropertySO>();
                wallProps.objectName = "Wall";
                wallProps.durability = 999f;
                wallProps.weight = 999f;
                wallProps.isMovable = false;
                wallProps.isDestructible = false;
                wallProps.isStructural = true;
            }
        }

        private void CreateMaterials()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null)
                litShader = Shader.Find("Standard");

            groundMat = CreateMat(litShader, new Color(0.35f, 0.55f, 0.25f));
            wallMat = CreateMat(litShader, new Color(0.75f, 0.72f, 0.68f));
            floorMat = CreateMat(litShader, new Color(0.55f, 0.38f, 0.22f));
            stairMat = CreateMat(litShader, new Color(0.4f, 0.28f, 0.18f));
            roofMat = CreateMat(litShader, new Color(0.5f, 0.2f, 0.15f));
            playerMat = CreateMat(litShader, new Color(0.2f, 0.7f, 0.3f));
            meleeEnemyMat = CreateMat(litShader, new Color(0.8f, 0.2f, 0.2f));
            rangedEnemyMat = CreateMat(litShader, new Color(0.8f, 0.5f, 0.1f));
            boxMat = CreateMat(litShader, new Color(0.3f, 0.5f, 0.8f));
            barrelMat = CreateMat(litShader, new Color(0.6f, 0.4f, 0.2f));
        }

        private Material CreateMat(Shader shader, Color color)
        {
            var mat = new Material(shader);
            mat.color = color;
            return mat;
        }

        private void BuildEnvironment()
        {
            // === GROUND ===
            var ground = CreateBox("Ground", Vector3.down * 0.25f, new Vector3(40, 0.5f, 40), groundMat);
            ground.isStatic = true;

            // === HOUSE ===
            float houseX = 0f, houseZ = 0f;
            float wallHeight = 3f;
            float wallThick = 0.3f;
            float houseW = 10f;
            float houseD = 8f;

            // First Floor Walls
            CreateBox("Wall_Front_L", new Vector3(houseX - 3f, wallHeight / 2, houseZ - houseD / 2),
                new Vector3(4f, wallHeight, wallThick), wallMat).isStatic = true;
            CreateBox("Wall_Front_R", new Vector3(houseX + 3f, wallHeight / 2, houseZ - houseD / 2),
                new Vector3(4f, wallHeight, wallThick), wallMat).isStatic = true;

            CreateBox("Wall_Back_L", new Vector3(houseX - 3f, wallHeight / 2, houseZ + houseD / 2),
                new Vector3(4f, wallHeight, wallThick), wallMat).isStatic = true;
            CreateBox("Wall_Back_R", new Vector3(houseX + 3f, wallHeight / 2, houseZ + houseD / 2),
                new Vector3(4f, wallHeight, wallThick), wallMat).isStatic = true;

            CreateBox("Wall_Left", new Vector3(houseX - houseW / 2, wallHeight / 2, houseZ),
                new Vector3(wallThick, wallHeight, houseD), wallMat).isStatic = true;

            CreateBox("Wall_Right_Low", new Vector3(houseX + houseW / 2, 0.5f, houseZ),
                new Vector3(wallThick, 1f, houseD), wallMat).isStatic = true;
            CreateBox("Wall_Right_High", new Vector3(houseX + houseW / 2, 2.5f, houseZ),
                new Vector3(wallThick, 1f, houseD), wallMat).isStatic = true;

            // Second Floor
            float floor2Y = wallHeight;
            for (int x = -2; x <= 2; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    var floorTile = CreateBox($"Floor2_{x}_{z}",
                        new Vector3(houseX + x * 2f, floor2Y, houseZ + z * 2.5f),
                        new Vector3(2f, 0.2f, 2.5f), floorMat);
                    AddInteractable(floorTile, floorProps, false);
                }
            }

            float wall2Height = 2.5f;
            float wall2Y = floor2Y + wall2Height / 2 + 0.1f;

            CreateBox("Wall2_Front", new Vector3(houseX, wall2Y, houseZ - houseD / 2),
                new Vector3(houseW, wall2Height, wallThick), wallMat).isStatic = true;
            CreateBox("Wall2_Back", new Vector3(houseX, wall2Y, houseZ + houseD / 2),
                new Vector3(houseW, wall2Height, wallThick), wallMat).isStatic = true;
            CreateBox("Wall2_Left", new Vector3(houseX - houseW / 2, wall2Y, houseZ),
                new Vector3(wallThick, wall2Height, houseD), wallMat).isStatic = true;
            CreateBox("Wall2_Right", new Vector3(houseX + houseW / 2, wall2Y, houseZ),
                new Vector3(wallThick, wall2Height, houseD), wallMat).isStatic = true;

            CreateBox("Wall2_Divider", new Vector3(houseX, wall2Y, houseZ),
                new Vector3(wallThick, wall2Height, 3f), wallMat).isStatic = true;

            CreateBox("Roof", new Vector3(houseX, floor2Y + wall2Height + 0.15f, houseZ),
                new Vector3(houseW + 1f, 0.3f, houseD + 1f), roofMat).isStatic = true;

            // Stairs
            int stairCount = 8;
            float stairWidth = 1.5f;
            float stairDepth = 0.6f;
            float stairStartX = houseX + 2f;
            float stairStartZ = houseZ - 2f;

            for (int i = 0; i < stairCount; i++)
            {
                float y = (float)(i + 1) / stairCount * wallHeight;
                var stair = CreateBox($"Stair_{i}",
                    new Vector3(stairStartX, y - 0.1f, stairStartZ + i * stairDepth),
                    new Vector3(stairWidth, 0.2f, stairDepth), stairMat);
                AddInteractable(stair, stairProps, false);
            }

            // Fences outside
            CreateBox("Fence_1", new Vector3(-8f, 0.5f, -6f), new Vector3(4f, 1f, 0.2f), wallMat).isStatic = true;
            CreateBox("Fence_2", new Vector3(8f, 0.5f, 5f), new Vector3(0.2f, 1f, 3f), wallMat).isStatic = true;
        }

        private void SpawnUnits()
        {
            var playerObj = CreateUnit("Player", new Vector3(0, 0.5f, -1f), playerMat, true);
            var playerUnit = playerObj.AddComponent<PlayerUnit>();
            playerUnit.stats = playerStats;
            // FreeMovementSystem must be added BEFORE PlayerController
            // because PlayerController.Awake() calls GetComponent<FreeMovementSystem>()
            playerObj.AddComponent<TacticsGame.Movement.FreeMovementSystem>();
            playerObj.AddComponent<PlayerController>();

            var enemy1 = CreateUnit("Enemy_Melee_1", new Vector3(0, 0.5f, -12f), meleeEnemyMat, false);
            SetupEnemy(enemy1, meleeEnemyStats, EnemyType.Melee);

            var enemy2 = CreateUnit("Enemy_Melee_2", new Vector3(0, 0.5f, 12f), meleeEnemyMat, false);
            SetupEnemy(enemy2, meleeEnemyStats, EnemyType.Melee);

            var enemy3 = CreateUnit("Enemy_Ranged_1", new Vector3(-12f, 0.5f, 0), rangedEnemyMat, false);
            SetupEnemy(enemy3, rangedEnemyStats, EnemyType.Ranged);

            var enemy4 = CreateUnit("Enemy_Ranged_2", new Vector3(12f, 0.5f, 0), rangedEnemyMat, false);
            SetupEnemy(enemy4, rangedEnemyStats, EnemyType.Ranged);
        }

        private void SpawnInteractableObjects()
        {
            CreateInteractable("Box_1", new Vector3(-2f, 0.5f, 1f), new Vector3(1f, 1f, 1f), boxMat, woodenBoxProps, true);
            CreateInteractable("Box_2", new Vector3(3f, 0.5f, -2f), new Vector3(1f, 1f, 1f), boxMat, woodenBoxProps, true);

            CreateInteractable("Barrel_1", new Vector3(-6f, 0.6f, -3f), new Vector3(0.8f, 1.2f, 0.8f), barrelMat, barrelProps, true, true);
            CreateInteractable("Barrel_2", new Vector3(6f, 0.6f, 4f), new Vector3(0.8f, 1.2f, 0.8f), barrelMat, barrelProps, true, true);
            CreateInteractable("Barrel_3", new Vector3(-4f, 0.6f, 8f), new Vector3(0.8f, 1.2f, 0.8f), barrelMat, barrelProps, true, true);

            CreateInteractable("Box_3", new Vector3(7f, 0.5f, -7f), new Vector3(1.2f, 1.2f, 1.2f), boxMat, woodenBoxProps, true);
            CreateInteractable("Box_4", new Vector3(-7f, 0.5f, 7f), new Vector3(1f, 1f, 1f), boxMat, woodenBoxProps, true);

            CreateInteractable("Box_2F", new Vector3(-2f, 3.5f, 2f), new Vector3(1f, 1f, 1f), boxMat, woodenBoxProps, true);
        }

        private void SetupNavMesh()
        {
            var ground = GameObject.Find("Ground");
            if (ground != null)
            {
                navMeshSurface = ground.AddComponent<NavMeshSurface>();
                navMeshSurface.collectObjects = CollectObjects.All;
                navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
                navMeshSurface.BuildNavMesh();
                Debug.Log("[Scene] NavMesh built successfully!");
            }
        }

        private void SetupSystems()
        {
            var systemsObj = new GameObject("_Systems");

            var turnManager = systemsObj.AddComponent<TurnManager>();
            var gameManager = systemsObj.AddComponent<GameManager>();

            // Set turnManager reference via reflection
            var tmField = typeof(GameManager).GetField("turnManager",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (tmField != null)
            {
                tmField.SetValue(gameManager, turnManager);
            }
            else
            {
                Debug.LogError("[DemoSceneBuilder] Failed to set TurnManager reference on GameManager!");
            }

            systemsObj.AddComponent<CombatManager>();
            systemsObj.AddComponent<EnemyTurnHandler>();
        }

        private void SetupUI()
        {
            var canvasObj = new GameObject("GameCanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();

            // State text (top center)
            CreateUIText(canvasObj.transform, "StateText", "Initializing...",
                new Vector2(0, -30), new Vector2(300, 40), TextAnchor.UpperCenter,
                out var stateText);

            // Turn info text
            CreateUIText(canvasObj.transform, "TurnInfoText", "",
                new Vector2(0, -60), new Vector2(300, 30), TextAnchor.UpperCenter,
                out var turnInfoText);

            // AP Text (bottom left)
            CreateUIText(canvasObj.transform, "APText", "AP: 0 / 0",
                new Vector2(100, 30), new Vector2(200, 40), TextAnchor.LowerLeft,
                out var apText);

            // Action buttons (bottom right)
            float btnY = 40f;
            float btnStartX = -300f;
            var moveBtn = CreateUIButton(canvasObj.transform, "MoveBtn", "[1] Move",
                new Vector2(btnStartX, btnY), new Vector2(100, 35));
            var attackBtn = CreateUIButton(canvasObj.transform, "AttackBtn", "[2] Attack",
                new Vector2(btnStartX + 110, btnY), new Vector2(100, 35));
            var moveObjBtn = CreateUIButton(canvasObj.transform, "MoveObjBtn", "[3] Push",
                new Vector2(btnStartX + 220, btnY), new Vector2(100, 35));
            var endTurnBtn = CreateUIButton(canvasObj.transform, "EndTurnBtn", "[Space] End",
                new Vector2(btnStartX + 330, btnY), new Vector2(120, 35));

            // Turn order bar (bottom)
            var barContainer = new GameObject("TurnOrderBar");
            var barRect = barContainer.AddComponent<RectTransform>();
            barRect.SetParent(canvasObj.transform, false);
            barRect.anchorMin = new Vector2(0.5f, 0);
            barRect.anchorMax = new Vector2(0.5f, 0);
            barRect.pivot = new Vector2(0.5f, 0);
            barRect.anchoredPosition = new Vector2(0, 80);
            barRect.sizeDelta = new Vector2(600, 50);

            var barBg = barContainer.AddComponent<Image>();
            barBg.color = new Color(0, 0, 0, 0.5f);

            // GameState UI component
            var stateUI = canvasObj.AddComponent<GameStateUI>();
            SetPrivateField(stateUI, "stateText", stateText);
            SetPrivateField(stateUI, "turnInfoText", turnInfoText);

            // Action Points UI
            var apUI = canvasObj.AddComponent<ActionPointsUI>();
            SetPrivateField(apUI, "apText", apText);

            // Action Menu UI
            var menuUI = canvasObj.AddComponent<ActionMenuUI>();
            SetPrivateField(menuUI, "moveButton", moveBtn);
            SetPrivateField(menuUI, "attackButton", attackBtn);
            SetPrivateField(menuUI, "moveObjectButton", moveObjBtn);
            SetPrivateField(menuUI, "endTurnButton", endTurnBtn);

            // Turn Order UI
            var turnOrderUI = canvasObj.AddComponent<TurnOrderUI>();
            SetPrivateField(turnOrderUI, "barContainer", barRect);

            // Game Over Panel (hidden)
            var gameOverPanel = new GameObject("GameOverPanel");
            var goPanelRect = gameOverPanel.AddComponent<RectTransform>();
            goPanelRect.SetParent(canvasObj.transform, false);
            goPanelRect.anchorMin = Vector2.zero;
            goPanelRect.anchorMax = Vector2.one;
            goPanelRect.sizeDelta = Vector2.zero;
            var goPanelBg = gameOverPanel.AddComponent<Image>();
            goPanelBg.color = new Color(0, 0, 0, 0.7f);

            CreateUIText(gameOverPanel.transform, "GameOverText", "Game Over",
                Vector2.zero, new Vector2(400, 100), TextAnchor.MiddleCenter,
                out var gameOverText, 36);
            gameOverPanel.SetActive(false);

            SetPrivateField(stateUI, "gameOverPanel", gameOverPanel);
            SetPrivateField(stateUI, "gameOverText", gameOverText);

            // Controls help text
            CreateUIText(canvasObj.transform, "HelpText",
                "WASD=Pan | Q/E=Rot | Scroll=Zoom | 1=Move 2=Attack 3=Push | Space=End Turn | RMB=Cancel",
                new Vector2(0, 10), new Vector2(800, 25), TextAnchor.LowerCenter,
                out _, 12);

            // Enemy Info Panel (top-left, shown on hover)
            var infoPanel = new GameObject("EnemyInfoPanel");
            var infoRect = infoPanel.AddComponent<RectTransform>();
            infoRect.SetParent(canvasObj.transform, false);
            infoRect.anchorMin = new Vector2(0, 1);
            infoRect.anchorMax = new Vector2(0, 1);
            infoRect.pivot = new Vector2(0, 1);
            infoRect.anchoredPosition = new Vector2(10, -10);
            infoRect.sizeDelta = new Vector2(200, 40);
            var infoBg = infoPanel.AddComponent<Image>();
            infoBg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

            var infoLayout = infoPanel.AddComponent<VerticalLayoutGroup>();
            infoLayout.padding = new RectOffset(12, 12, 8, 8);
            infoLayout.childForceExpandWidth = true;
            infoLayout.childForceExpandHeight = false;

            var infoFitter = infoPanel.AddComponent<ContentSizeFitter>();
            infoFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            infoFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var infoTextObj = new GameObject("InfoText");
            var infoTextRect = infoTextObj.AddComponent<RectTransform>();
            infoTextRect.SetParent(infoPanel.transform, false);
            var infoTMP = infoTextObj.AddComponent<TextMeshProUGUI>();
            infoTMP.fontSize = 14;
            infoTMP.color = Color.white;
            infoTMP.richText = true;
            infoTMP.alignment = TextAlignmentOptions.TopLeft;

            infoPanel.SetActive(false);

            var infoTooltip = canvasObj.AddComponent<IntentionTooltip>();
            SetPrivateField(infoTooltip, "panel", infoPanel);
            SetPrivateField(infoTooltip, "label", infoTMP);
        }

        private void SetupCamera()
        {
            var pivotObj = new GameObject("CameraPivot");
            pivotObj.transform.position = new Vector3(0, 0, 0);
            var camCtrl = pivotObj.AddComponent<CameraController>();
            // CameraController will find Camera.main in its Awake and control it directly
            // No need to reparent the camera
        }

        // --- Helper methods ---

        private GameObject CreateBox(string name, Vector3 position, Vector3 scale, Material mat)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.position = position;
            obj.transform.localScale = scale;
            if (mat != null)
                obj.GetComponent<Renderer>().material = mat;
            return obj;
        }

        private GameObject CreateUnit(string name, Vector3 position, Material mat, bool isPlayer)
        {
            var obj = new GameObject(name);
            obj.transform.position = position;

            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(obj.transform, false);
            visual.transform.localPosition = Vector3.up * 0.5f;
            visual.transform.localScale = new Vector3(0.6f, 0.5f, 0.6f);
            if (mat != null) visual.GetComponent<Renderer>().material = mat;

            Destroy(visual.GetComponent<Collider>());
            var col = obj.AddComponent<CapsuleCollider>();
            col.height = 2f;
            col.center = Vector3.up;
            col.radius = 0.3f;

            var agent = obj.AddComponent<NavMeshAgent>();
            agent.radius = 0.3f;
            agent.height = 2f;
            agent.speed = 4f;
            agent.angularSpeed = 360f;
            agent.acceleration = 10f;
            agent.stoppingDistance = 0.2f;
            agent.autoBraking = true;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

            // Warp agent to its position on the NavMesh
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            else
            {
                Debug.LogWarning($"[DemoSceneBuilder] Could not find NavMesh position near {position} for unit {name}");
            }

            obj.layer = LayerMask.NameToLayer("Default");

            return obj;
        }

        private void SetupEnemy(GameObject obj, UnitStatsSO stats, EnemyType type)
        {
            var enemy = obj.AddComponent<EnemyUnit>();
            enemy.stats = stats;
            enemy.enemyType = type;
            obj.AddComponent<AIBrain>();
            obj.AddComponent<IntentionVisualizer>();
        }

        private void AddInteractable(GameObject obj, ObjectPropertySO props, bool addNavObstacle)
        {
            var interactable = obj.AddComponent<InteractableObject>();
            interactable.properties = props;

            if (addNavObstacle)
            {
                var obstacle = obj.AddComponent<NavMeshObstacle>();
                obstacle.carving = true;
                obstacle.shape = NavMeshObstacleShape.Box;
                obstacle.size = Vector3.one;
            }
        }

        private GameObject CreateInteractable(string name, Vector3 pos, Vector3 scale, Material mat,
            ObjectPropertySO props, bool addNavObstacle, bool isCylinder = false)
        {
            var obj = isCylinder
                ? GameObject.CreatePrimitive(PrimitiveType.Cylinder)
                : GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.position = pos;
            obj.transform.localScale = scale;
            if (mat != null) obj.GetComponent<Renderer>().material = mat;

            AddInteractable(obj, props, addNavObstacle);

            var rb = obj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            return obj;
        }

        private void CreateUIText(Transform parent, string name, string text,
            Vector2 position, Vector2 size, TextAnchor anchor,
            out TextMeshProUGUI tmpText, int fontSize = 16)
        {
            var obj = new GameObject(name);
            var rect = obj.AddComponent<RectTransform>();
            rect.SetParent(parent, false);

            switch (anchor)
            {
                case TextAnchor.UpperCenter:
                    rect.anchorMin = new Vector2(0.5f, 1);
                    rect.anchorMax = new Vector2(0.5f, 1);
                    rect.pivot = new Vector2(0.5f, 1);
                    break;
                case TextAnchor.LowerLeft:
                    rect.anchorMin = new Vector2(0, 0);
                    rect.anchorMax = new Vector2(0, 0);
                    rect.pivot = new Vector2(0, 0);
                    break;
                case TextAnchor.LowerCenter:
                    rect.anchorMin = new Vector2(0.5f, 0);
                    rect.anchorMax = new Vector2(0.5f, 0);
                    rect.pivot = new Vector2(0.5f, 0);
                    break;
                case TextAnchor.MiddleCenter:
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    break;
            }

            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            tmpText = obj.AddComponent<TextMeshProUGUI>();
            tmpText.text = text;
            tmpText.fontSize = fontSize;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.color = Color.white;
        }

        private Button CreateUIButton(Transform parent, string name, string label,
            Vector2 position, Vector2 size)
        {
            var obj = new GameObject(name);
            var rect = obj.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(1, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(1, 0);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var image = obj.AddComponent<Image>();
            image.color = new Color(0.25f, 0.25f, 0.3f, 0.9f);

            var button = obj.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(0.25f, 0.25f, 0.3f);
            colors.highlightedColor = new Color(0.35f, 0.35f, 0.45f);
            colors.pressedColor = new Color(0.2f, 0.2f, 0.25f);
            button.colors = colors;

            var textObj = new GameObject("Text");
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.SetParent(obj.transform, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 14;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return button;
        }

        private void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(target, value);
            }
            else
            {
                Debug.LogWarning($"[DemoSceneBuilder] Field '{fieldName}' not found on {target.GetType().Name}");
            }
        }
    }
}
