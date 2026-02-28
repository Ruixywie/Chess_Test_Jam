using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using TacticsGame.Core;
using TacticsGame.Environment;

namespace TacticsGame.AI
{
    /// <summary>
    /// Attached to all visualization objects (ghost, markers, lines, path colliders)
    /// to link back to the owning IntentionVisualizer for hover detection.
    /// </summary>
    public class VisualizationPart : MonoBehaviour
    {
        public IntentionVisualizer owner;
    }

    public struct ActionAnimInfo
    {
        public ActionType type;
        public Vector3 from;  // 动作起点
        public Vector3 to;    // 动作终点
    }

    public class IntentionVisualizer : MonoBehaviour
    {
        [Header("Visualization Settings")]
        [SerializeField] private Color pathColor = new Color(1f, 0.2f, 0.2f, 0.6f);
        [SerializeField] private Color attackColor = new Color(1f, 0f, 0f, 0.8f);
        [SerializeField] private float pathWidth = 0.08f;
        [SerializeField] private Color ghostColor = new Color(1f, 0.2f, 0.2f, 0.25f);

        private LineRenderer pathLine;
        private List<GameObject> markers = new List<GameObject>();
        private List<GameObject> ghosts = new List<GameObject>();
        private ActionPlan displayedPlan;

        private void Awake()
        {
            pathLine = gameObject.AddComponent<LineRenderer>();
            pathLine.startWidth = pathWidth;
            pathLine.endWidth = pathWidth;
            pathLine.material = new Material(Shader.Find("Sprites/Default"));
            pathLine.startColor = pathColor;
            pathLine.endColor = pathColor;
            pathLine.positionCount = 0;
            pathLine.useWorldSpace = true;
        }

        /// <summary>
        /// Called by IntentionTooltip when mouse hovers over any visualization part of this enemy.
        /// </summary>
        public void SetAnimating(bool active)
        {
            foreach (var ghost in ghosts)
            {
                if (ghost == null) continue;
                var animator = ghost.GetComponent<GhostAnimator>();
                if (animator != null) animator.SetPlaying(active);
            }
        }

        public void ShowPlan(ActionPlan plan)
        {
            ClearVisualization();
            if (plan == null) return;

            displayedPlan = plan;

            // === Step 1: 预计算路径点和动作动画信息 ===
            Vector3 simPos = transform.position;
            bool hasMoved = false;
            List<ActionAnimInfo> actionAnims = new List<ActionAnimInfo>();

            List<Vector3> groundPathPoints = new List<Vector3>();
            groundPathPoints.Add(transform.position);

            List<Vector3> linePoints = new List<Vector3>();
            linePoints.Add(transform.position + Vector3.up * 0.15f);

            Vector3 currentPos = transform.position;

            foreach (var action in plan.actions)
            {
                switch (action.type)
                {
                    case ActionType.Move:
                        simPos = action.targetPosition;
                        hasMoved = true;

                        NavMeshPath navPath = new NavMeshPath();
                        NavMesh.CalculatePath(currentPos, action.targetPosition, NavMesh.AllAreas, navPath);
                        if (navPath.corners != null)
                        {
                            foreach (var corner in navPath.corners)
                            {
                                groundPathPoints.Add(corner);
                                linePoints.Add(corner + Vector3.up * 0.15f);
                            }
                        }
                        currentPos = action.targetPosition;
                        break;

                    case ActionType.MeleeAttack:
                        if (action.targetUnit != null)
                        {
                            actionAnims.Add(new ActionAnimInfo
                            {
                                type = ActionType.MeleeAttack,
                                from = simPos + Vector3.up,
                                to = action.targetPosition + Vector3.up
                            });
                        }
                        break;

                    case ActionType.RangedAttack:
                        actionAnims.Add(new ActionAnimInfo
                        {
                            type = ActionType.RangedAttack,
                            from = simPos + Vector3.up,
                            to = action.targetPosition + Vector3.up
                        });
                        break;

                    case ActionType.MoveObject:
                        if (action.targetObject != null)
                        {
                            actionAnims.Add(new ActionAnimInfo
                            {
                                type = ActionType.MoveObject,
                                from = action.targetObject.transform.position + Vector3.up * 0.5f,
                                to = action.targetPosition + Vector3.up * 0.5f
                            });
                        }
                        break;
                }
            }

            // === Step 2: 创建 ghost（如果有移动）===
            if (hasMoved)
            {
                CreateGhost(simPos, groundPathPoints, actionAnims);
            }

            // === Step 3: 路径线、标记和路径碰撞体 ===
            if (linePoints.Count > 1)
            {
                pathLine.positionCount = linePoints.Count;
                pathLine.SetPositions(linePoints.ToArray());
                pathLine.material.mainTextureScale = new Vector2(linePoints.Count * 2f, 1f);

                // 为路径每段创建不可见的碰撞体用于悬停检测
                for (int i = 0; i < linePoints.Count - 1; i++)
                {
                    CreatePathSegmentCollider(linePoints[i], linePoints[i + 1]);
                }
            }

            // 重新遍历创建标记
            currentPos = transform.position;
            foreach (var action in plan.actions)
            {
                switch (action.type)
                {
                    case ActionType.Move:
                        currentPos = action.targetPosition;
                        break;

                    case ActionType.MeleeAttack:
                        CreateAttackMarker(action.targetPosition, "MELEE", attackColor);
                        break;

                    case ActionType.RangedAttack:
                        CreateAttackMarker(action.targetPosition, "RANGED", attackColor);
                        CreateAttackLine(currentPos, action.targetPosition);
                        break;

                    case ActionType.MoveObject:
                        if (action.targetObject != null)
                        {
                            CreateMoveObjectMarker(action.targetObject.transform.position, action.targetPosition);
                        }
                        break;
                }
            }
        }

        public void ClearVisualization()
        {
            displayedPlan = null;
            pathLine.positionCount = 0;

            foreach (var marker in markers)
            {
                if (marker != null) Destroy(marker);
            }
            markers.Clear();

            foreach (var ghost in ghosts)
            {
                if (ghost != null) Destroy(ghost);
            }
            ghosts.Clear();
        }

        private void CreateAttackMarker(Vector3 position, string label, Color color)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.transform.position = position + Vector3.up * 0.05f;
            marker.transform.localScale = new Vector3(1.2f, 0.05f, 1.2f);
            marker.name = $"AttackMarker_{label}";

            // 保留碰撞体但设为 trigger（用于悬停检测）
            var col = marker.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            var renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Sprites/Default"));
                renderer.material.color = color;
            }

            // 挂载 VisualizationPart 指回本 visualizer
            var part = marker.AddComponent<VisualizationPart>();
            part.owner = this;

            markers.Add(marker);

            var pulse = marker.AddComponent<PulseEffect>();
            pulse.pulseSpeed = 2f;
            pulse.minScale = 0.8f;
            pulse.maxScale = 1.3f;
        }

        private void CreateAttackLine(Vector3 from, Vector3 to)
        {
            Vector3 lineFrom = from + Vector3.up;
            Vector3 lineTo = to + Vector3.up;

            GameObject lineObj = new GameObject("AttackLine");
            var line = lineObj.AddComponent<LineRenderer>();
            line.startWidth = 0.04f;
            line.endWidth = 0.04f;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = attackColor;
            line.endColor = attackColor;
            line.positionCount = 2;
            line.SetPosition(0, lineFrom);
            line.SetPosition(1, lineTo);
            line.useWorldSpace = true;

            // 添加线段碰撞体用于悬停检测
            SetupLineCollider(lineObj, lineFrom, lineTo);

            var part = lineObj.AddComponent<VisualizationPart>();
            part.owner = this;

            markers.Add(lineObj);
        }

        private void CreateMoveObjectMarker(Vector3 from, Vector3 to)
        {
            Vector3 lineFrom = from + Vector3.up * 0.5f;
            Vector3 lineTo = to + Vector3.up * 0.5f;

            GameObject lineObj = new GameObject("MoveObjectArrow");
            var line = lineObj.AddComponent<LineRenderer>();
            line.startWidth = 0.06f;
            line.endWidth = 0.02f;
            line.material = new Material(Shader.Find("Sprites/Default"));
            Color arrowColor = new Color(1f, 0.6f, 0f, 0.7f);
            line.startColor = arrowColor;
            line.endColor = arrowColor;
            line.positionCount = 2;
            line.SetPosition(0, lineFrom);
            line.SetPosition(1, lineTo);
            line.useWorldSpace = true;

            // 添加线段碰撞体用于悬停检测
            SetupLineCollider(lineObj, lineFrom, lineTo);

            var part = lineObj.AddComponent<VisualizationPart>();
            part.owner = this;

            markers.Add(lineObj);
        }

        /// <summary>
        /// 为路径段创建不可见的碰撞体对象
        /// </summary>
        private void CreatePathSegmentCollider(Vector3 from, Vector3 to)
        {
            float length = Vector3.Distance(from, to);
            if (length < 0.01f) return;

            var obj = new GameObject("PathSegmentCollider");
            SetupLineCollider(obj, from, to);

            var part = obj.AddComponent<VisualizationPart>();
            part.owner = this;

            markers.Add(obj);
        }

        /// <summary>
        /// 在 GameObject 上添加沿线段方向的 CapsuleCollider trigger
        /// </summary>
        private void SetupLineCollider(GameObject obj, Vector3 from, Vector3 to)
        {
            float length = Vector3.Distance(from, to);
            if (length < 0.01f) return;

            Vector3 mid = (from + to) / 2f;
            Vector3 dir = (to - from).normalized;

            obj.transform.position = mid;
            obj.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);

            var col = obj.AddComponent<CapsuleCollider>();
            col.isTrigger = true;
            col.height = length;
            col.radius = 0.15f;
            col.direction = 1; // Y轴方向
        }

        private void CreateGhost(Vector3 position,
                                 List<Vector3> groundPathPoints, List<ActionAnimInfo> anims)
        {
            var visual = transform.Find("Visual");
            Mesh mesh = null;
            if (visual != null)
            {
                var mf = visual.GetComponent<MeshFilter>();
                if (mf != null) mesh = mf.sharedMesh;
            }

            if (mesh == null) return;

            var ghostObj = new GameObject("Ghost_" + gameObject.name);
            ghostObj.transform.position = position; // 默认显示在终点位置

            var ghostVisual = new GameObject("GhostVisual");
            ghostVisual.transform.SetParent(ghostObj.transform, false);
            ghostVisual.transform.localPosition = Vector3.up * 0.5f;
            ghostVisual.transform.localScale = new Vector3(0.6f, 0.5f, 0.6f);

            var ghostMF = ghostVisual.AddComponent<MeshFilter>();
            ghostMF.sharedMesh = mesh;

            var ghostMR = ghostVisual.AddComponent<MeshRenderer>();
            ghostMR.material = CreateTransparentMaterial();

            var col = ghostObj.AddComponent<SphereCollider>();
            col.center = Vector3.up;
            col.radius = 0.5f;
            col.isTrigger = true;

            // VisualizationPart 指回本 visualizer（用于悬停检测）
            var part = ghostObj.AddComponent<VisualizationPart>();
            part.owner = this;

            // GhostAnimator 默认不播放，悬停时才启动
            if (groundPathPoints != null && groundPathPoints.Count > 1)
            {
                var animator = ghostObj.AddComponent<GhostAnimator>();
                animator.Init(groundPathPoints.ToArray(), anims != null ? anims.ToArray() : null, ghostColor);
            }

            // 固定锚点：ghost 动画移走后仍保持悬停检测
            var anchor = new GameObject("GhostAnchor_" + gameObject.name);
            anchor.transform.position = position;
            var anchorCol = anchor.AddComponent<SphereCollider>();
            anchorCol.center = Vector3.up;
            anchorCol.radius = 0.5f;
            anchorCol.isTrigger = true;
            var anchorPart = anchor.AddComponent<VisualizationPart>();
            anchorPart.owner = this;
            markers.Add(anchor); // 由 ClearVisualization 自动清理

            ghosts.Add(ghostObj);
        }

        private Material CreateTransparentMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            var mat = new Material(shader);
            mat.color = ghostColor;

            mat.SetFloat("_Surface", 1f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.renderQueue = 3000;

            return mat;
        }

        private void OnDestroy()
        {
            ClearVisualization();
        }
    }

    /// <summary>
    /// Simple pulsing scale effect for markers.
    /// </summary>
    public class PulseEffect : MonoBehaviour
    {
        public float pulseSpeed = 2f;
        public float minScale = 0.8f;
        public float maxScale = 1.3f;

        private Vector3 baseScale;

        private void Start()
        {
            baseScale = transform.localScale;
        }

        private void Update()
        {
            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            float scale = Mathf.Lerp(minScale, maxScale, t);
            transform.localScale = new Vector3(
                baseScale.x * scale,
                baseScale.y,
                baseScale.z * scale
            );
        }
    }

    /// <summary>
    /// Drives looping animation on a ghost: move along path → play action anims → reset.
    /// Default paused — ghost sits at destination. Activated by hover via SetPlaying().
    /// </summary>
    public class GhostAnimator : MonoBehaviour
    {
        private Vector3[] pathPoints;
        private float moveSpeed = 4f;
        private ActionAnimInfo[] actionAnims;
        private Color ghostColor;
        private bool playing = false;

        private enum State { Moving, PerformingActions, Resetting }
        private State state = State.Moving;

        // Moving state
        private int currentSegment = 0;
        private float segmentProgress = 0f;

        // Action animation state
        private int currentAnimIndex = 0;
        private float animElapsed = 0f;
        private GameObject projectile;
        private const float PROJECTILE_SPEED = 15f;
        private float currentAnimDuration = 0f;

        // Reset
        private float resetTimer = 0f;
        private const float RESET_DELAY = 0.3f;

        public void Init(Vector3[] path, ActionAnimInfo[] anims, Color color)
        {
            pathPoints = path;
            actionAnims = anims;
            ghostColor = color;
            playing = false;

            // 默认显示在终点（目标位置）
            if (pathPoints != null && pathPoints.Length > 0)
                transform.position = pathPoints[pathPoints.Length - 1];
        }

        public void SetPlaying(bool value)
        {
            if (playing == value) return;
            playing = value;

            if (playing)
            {
                // 从起点开始循环动画
                currentSegment = 0;
                segmentProgress = 0f;
                currentAnimIndex = 0;
                animElapsed = 0f;
                state = State.Moving;
                if (pathPoints != null && pathPoints.Length > 0)
                    transform.position = pathPoints[0];
            }
            else
            {
                // 停止动画，回到终点
                CleanupProjectile();
                state = State.Moving;
                currentSegment = 0;
                segmentProgress = 0f;
                if (pathPoints != null && pathPoints.Length > 0)
                    transform.position = pathPoints[pathPoints.Length - 1];
            }
        }

        private void Update()
        {
            if (!playing) return;

            if (pathPoints == null || pathPoints.Length < 2)
            {
                if (actionAnims != null && actionAnims.Length > 0 && state == State.Moving)
                    state = State.PerformingActions;
                else
                    return;
            }

            switch (state)
            {
                case State.Moving:
                    UpdateMoving();
                    break;
                case State.PerformingActions:
                    UpdateActions();
                    break;
                case State.Resetting:
                    UpdateResetting();
                    break;
            }
        }

        private void UpdateMoving()
        {
            if (currentSegment >= pathPoints.Length - 1)
            {
                transform.position = pathPoints[pathPoints.Length - 1];
                if (actionAnims != null && actionAnims.Length > 0)
                {
                    state = State.PerformingActions;
                    currentAnimIndex = 0;
                    animElapsed = 0f;
                }
                else
                {
                    state = State.Resetting;
                    resetTimer = 0f;
                }
                return;
            }

            Vector3 from = pathPoints[currentSegment];
            Vector3 to = pathPoints[currentSegment + 1];
            float segmentLength = Vector3.Distance(from, to);

            if (segmentLength < 0.001f)
            {
                currentSegment++;
                segmentProgress = 0f;
                return;
            }

            segmentProgress += (moveSpeed * Time.deltaTime) / segmentLength;

            if (segmentProgress >= 1f)
            {
                currentSegment++;
                segmentProgress = 0f;
                if (currentSegment < pathPoints.Length - 1)
                    transform.position = pathPoints[currentSegment];
                else
                    transform.position = pathPoints[pathPoints.Length - 1];
            }
            else
            {
                transform.position = Vector3.Lerp(from, to, segmentProgress);
            }

            Vector3 dir = (to - from);
            dir.y = 0;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir);
        }

        private void UpdateActions()
        {
            if (actionAnims == null || currentAnimIndex >= actionAnims.Length)
            {
                CleanupProjectile();
                state = State.Resetting;
                resetTimer = 0f;
                return;
            }

            var anim = actionAnims[currentAnimIndex];

            if (animElapsed == 0f)
            {
                CleanupProjectile();
                CreateProjectile(anim);
                currentAnimDuration = Vector3.Distance(anim.from, anim.to) / PROJECTILE_SPEED;
                if (currentAnimDuration < 0.1f) currentAnimDuration = 0.1f;
            }

            animElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(animElapsed / currentAnimDuration);

            if (projectile != null)
            {
                projectile.transform.position = Vector3.Lerp(anim.from, anim.to, t);
            }

            if (t >= 1f)
            {
                CleanupProjectile();
                currentAnimIndex++;
                animElapsed = 0f;
            }
        }

        private void UpdateResetting()
        {
            resetTimer += Time.deltaTime;
            if (resetTimer >= RESET_DELAY)
            {
                if (pathPoints != null && pathPoints.Length > 0)
                    transform.position = pathPoints[0];

                currentSegment = 0;
                segmentProgress = 0f;
                currentAnimIndex = 0;
                animElapsed = 0f;
                state = State.Moving;
            }
        }

        private void CreateProjectile(ActionAnimInfo anim)
        {
            projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = "GhostProjectile";
            projectile.transform.position = anim.from;
            projectile.transform.localScale = Vector3.one * 0.2f;

            var col = projectile.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var renderer = projectile.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Sprites/Default"));

                if (anim.type == ActionType.MoveObject)
                    mat.color = new Color(1f, 0.6f, 0f, 0.5f);
                else
                    mat.color = new Color(ghostColor.r, ghostColor.g, ghostColor.b, 0.5f);

                renderer.material = mat;
            }
        }

        private void CleanupProjectile()
        {
            if (projectile != null)
            {
                Destroy(projectile);
                projectile = null;
            }
        }

        private void OnDestroy()
        {
            CleanupProjectile();
        }
    }
}
