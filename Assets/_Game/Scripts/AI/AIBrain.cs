using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using TacticsGame.Core;
using TacticsGame.Environment;

namespace TacticsGame.AI
{
    public class AIBrain : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        private EnemyUnit _owner;
        private NavMeshAgent _navAgent;
        private ActionPlan currentPlan;
        private IntentionVisualizer _intentVisualizer;
        private bool isExecuting;

        private EnemyUnit owner => _owner ??= GetComponent<EnemyUnit>();
        private NavMeshAgent navAgent => _navAgent ??= GetComponent<NavMeshAgent>();
        private IntentionVisualizer intentVisualizer => _intentVisualizer ??= GetComponent<IntentionVisualizer>();

        public ActionPlan CurrentPlan => currentPlan;
        public bool IsExecuting => isExecuting;

        public void FormulatePlan()
        {
            if (owner.IsDead) return;

            currentPlan = CreatePlan();

            if (intentVisualizer != null)
            {
                intentVisualizer.ShowPlan(currentPlan);
            }

            EventBus.RaisePlanCreated(owner, currentPlan);

            if (showDebugLogs)
            {
                Debug.Log($"[AI] {owner.stats.unitName} formulated plan with {currentPlan.actions.Count} actions");
            }
        }

        public void ClearPlan()
        {
            currentPlan = null;
            if (intentVisualizer != null)
            {
                intentVisualizer.ClearVisualization();
            }
        }

        private ActionPlan CreatePlan()
        {
            var plan = new ActionPlan();
            var player = GameManager.Instance.Player;

            if (player == null || player.IsDead) return plan;

            float availableAP = owner.stats.maxActionPoints;
            EnemyUnit enemy = owner;

            if (enemy.enemyType == EnemyType.Melee)
            {
                PlanMeleeActions(plan, player, ref availableAP);
            }
            else
            {
                PlanRangedActions(plan, player, ref availableAP);
            }

            return plan;
        }

        private void PlanMeleeActions(ActionPlan plan, UnitBase player, ref float availableAP)
        {
            float distToPlayer = Vector3.Distance(transform.position, player.transform.position);

            // If close enough, plan attack
            if (distToPlayer <= owner.stats.attackRange + 0.5f)
            {
                if (availableAP >= owner.stats.meleeAttackAPCost)
                {
                    plan.actions.Add(new PlannedAction(
                        ActionType.MeleeAttack,
                        player.transform.position,
                        owner.stats.meleeAttackAPCost
                    ) { targetUnit = player });
                    availableAP -= owner.stats.meleeAttackAPCost;
                }
                return;
            }

            // Otherwise, move toward player then attack if possible
            NavMeshPath path = new NavMeshPath();
            NavMesh.CalculatePath(transform.position, player.transform.position, NavMesh.AllAreas, path);

            if (path.status == NavMeshPathStatus.PathComplete || path.status == NavMeshPathStatus.PathPartial)
            {
                float pathDistance = CalculatePathLength(path);
                float maxMoveDist = availableAP / owner.stats.movementAPPerMeter;

                // Leave AP for attack if possible
                float attackReserve = owner.stats.meleeAttackAPCost;
                float moveAPBudget = availableAP - attackReserve;
                float moveDist = Mathf.Min(pathDistance - owner.stats.attackRange, moveAPBudget / owner.stats.movementAPPerMeter);

                if (moveDist > 0.5f)
                {
                    Vector3 moveTarget = GetPointAlongPath(path, moveDist);
                    float moveCost = moveDist * owner.stats.movementAPPerMeter;

                    var moveAction = new PlannedAction(ActionType.Move, moveTarget, moveCost);
                    moveAction.pathCorners = path.corners;
                    plan.actions.Add(moveAction);
                    availableAP -= moveCost;
                }

                // Check if after moving we'd be in attack range
                float distAfterMove = Mathf.Max(0f, distToPlayer - moveDist);
                if (distAfterMove <= owner.stats.attackRange + 0.5f && availableAP >= owner.stats.meleeAttackAPCost)
                {
                    plan.actions.Add(new PlannedAction(
                        ActionType.MeleeAttack,
                        player.transform.position,
                        owner.stats.meleeAttackAPCost
                    ) { targetUnit = player });
                    availableAP -= owner.stats.meleeAttackAPCost;
                }
            }
            else
            {
                // Can't reach player - move as close as possible
                float maxMove = availableAP / owner.stats.movementAPPerMeter;
                Vector3 dirToPlayer = (player.transform.position - transform.position).normalized;
                Vector3 moveTarget = transform.position + dirToPlayer * maxMove;

                if (NavMesh.SamplePosition(moveTarget, out NavMeshHit hit, maxMove, NavMesh.AllAreas))
                {
                    float moveCost = Vector3.Distance(transform.position, hit.position) * owner.stats.movementAPPerMeter;
                    var moveAction = new PlannedAction(ActionType.Move, hit.position, moveCost);
                    NavMeshPath fallbackPath = new NavMeshPath();
                    NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, fallbackPath);
                    moveAction.pathCorners = fallbackPath.corners;
                    plan.actions.Add(moveAction);
                }
            }
        }

        private void PlanRangedActions(ActionPlan plan, UnitBase player, ref float availableAP)
        {
            float distToPlayer = Vector3.Distance(transform.position, player.transform.position);

            // Check if we have line of sight
            bool hasLOS = CheckLineOfSight(transform.position + Vector3.up, player.transform.position + Vector3.up);

            if (distToPlayer <= owner.stats.attackRange && hasLOS)
            {
                // In range with LOS - just attack
                if (availableAP >= owner.stats.rangedAttackAPCost)
                {
                    plan.actions.Add(new PlannedAction(
                        ActionType.RangedAttack,
                        player.transform.position,
                        owner.stats.rangedAttackAPCost
                    ) { targetUnit = player });
                    availableAP -= owner.stats.rangedAttackAPCost;
                }
            }
            else
            {
                // Need to reposition - find a good firing position
                Vector3 firePos = FindFiringPosition(player.transform.position, availableAP);

                if (firePos != Vector3.zero)
                {
                    float moveDist = Vector3.Distance(transform.position, firePos);
                    float moveCost = moveDist * owner.stats.movementAPPerMeter;

                    if (moveDist > 0.5f)
                    {
                        var moveAction = new PlannedAction(ActionType.Move, firePos, moveCost);
                        NavMeshPath movePath = new NavMeshPath();
                        NavMesh.CalculatePath(transform.position, firePos, NavMesh.AllAreas, movePath);
                        moveAction.pathCorners = movePath.corners;
                        plan.actions.Add(moveAction);
                        availableAP -= moveCost;
                    }

                    // Attack from new position
                    if (availableAP >= owner.stats.rangedAttackAPCost)
                    {
                        plan.actions.Add(new PlannedAction(
                            ActionType.RangedAttack,
                            player.transform.position,
                            owner.stats.rangedAttackAPCost
                        ) { targetUnit = player });
                        availableAP -= owner.stats.rangedAttackAPCost;
                    }
                }
                else
                {
                    // Can't find good position, just move closer
                    float maxMove = availableAP / owner.stats.movementAPPerMeter;
                    Vector3 dirToPlayer = (player.transform.position - transform.position).normalized;
                    float moveAmount = Mathf.Min(maxMove, distToPlayer * 0.7f);
                    Vector3 moveTarget = transform.position + dirToPlayer * moveAmount;

                    if (NavMesh.SamplePosition(moveTarget, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                    {
                        float moveCost = Vector3.Distance(transform.position, hit.position) * owner.stats.movementAPPerMeter;
                        var moveAction = new PlannedAction(ActionType.Move, hit.position, moveCost);
                        NavMeshPath fallbackPath = new NavMeshPath();
                        NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, fallbackPath);
                        moveAction.pathCorners = fallbackPath.corners;
                        plan.actions.Add(moveAction);
                    }
                }
            }
        }

        private Vector3 FindFiringPosition(Vector3 targetPos, float availableAP)
        {
            float maxMoveDist = (availableAP - owner.stats.rangedAttackAPCost) / owner.stats.movementAPPerMeter;
            if (maxMoveDist <= 0) return Vector3.zero;

            // Sample positions around current location
            Vector3 bestPos = Vector3.zero;
            float bestScore = float.MinValue;

            for (int i = 0; i < 12; i++)
            {
                float angle = i * 30f * Mathf.Deg2Rad;
                for (float dist = 3f; dist <= maxMoveDist; dist += 3f)
                {
                    Vector3 candidate = transform.position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * dist;

                    if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                        continue;

                    candidate = hit.position;
                    float distToTarget = Vector3.Distance(candidate, targetPos);

                    // Must be within attack range
                    if (distToTarget > owner.stats.attackRange) continue;

                    // Check LOS from this position
                    if (!CheckLineOfSight(candidate + Vector3.up, targetPos + Vector3.up)) continue;

                    // Score: prefer distance from target (safer), penalize close range
                    float score = distToTarget; // Farther is better for ranged
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestPos = candidate;
                    }
                }
            }

            return bestPos;
        }

        private bool CheckLineOfSight(Vector3 from, Vector3 to)
        {
            Vector3 dir = to - from;
            float dist = dir.magnitude;

            RaycastHit[] hits = Physics.RaycastAll(from, dir.normalized, dist);
            foreach (var h in hits)
            {
                // Skip unit colliders
                if (h.collider.GetComponent<UnitBase>() != null) continue;
                // Hit an environment object - LOS blocked
                return false;
            }
            return true;
        }

        public IEnumerator ExecutePlan()
        {
            if (currentPlan == null || currentPlan.actions.Count == 0)
            {
                yield break;
            }

            isExecuting = true;

            foreach (var action in currentPlan.actions)
            {
                if (owner.IsDead) break;

                bool blocked = false;

                switch (action.type)
                {
                    case ActionType.Move:
                        yield return ExecuteMove(action, result => blocked = result);
                        break;
                    case ActionType.MeleeAttack:
                        yield return ExecuteMeleeAttack(action, result => blocked = result);
                        break;
                    case ActionType.RangedAttack:
                        yield return ExecuteRangedAttack(action, result => blocked = result);
                        break;
                    case ActionType.MoveObject:
                        yield return ExecuteMoveObject(action, result => blocked = result);
                        break;
                }

                if (blocked)
                {
                    if (showDebugLogs)
                        Debug.Log($"[AI] {owner.stats.unitName} plan interrupted! Replanning...");

                    EventBus.RaisePlanInterrupted(owner);

                    // Replan and reset action bar (skip this turn)
                    FormulatePlan();
                    GameManager.Instance.TurnManager.ResetBar(owner);
                    isExecuting = false;
                    yield break;
                }

                owner.ConsumeAP(action.apCost);
            }

            isExecuting = false;
        }

        private IEnumerator ExecuteMove(PlannedAction action, System.Action<bool> onBlocked)
        {
            // 检查保存的路径是否被新障碍物阻断
            if (action.pathCorners != null && action.pathCorners.Length > 1)
            {
                for (int i = 0; i < action.pathCorners.Length - 1; i++)
                {
                    NavMeshHit nmHit;
                    if (NavMesh.Raycast(action.pathCorners[i], action.pathCorners[i + 1], out nmHit, NavMesh.AllAreas))
                    {
                        // 路径被阻断！先走到障碍物旁边
                        navAgent.isStopped = false;
                        navAgent.SetDestination(nmHit.position);

                        float blockStuckTimer = 0f;
                        Vector3 blockLastPos = transform.position;
                        while (navAgent.pathPending ||
                               navAgent.remainingDistance > navAgent.stoppingDistance + 0.3f)
                        {
                            if (Vector3.Distance(transform.position, blockLastPos) < 0.01f)
                            {
                                blockStuckTimer += Time.deltaTime;
                                if (blockStuckTimer > 0.5f) break;
                            }
                            else { blockStuckTimer = 0f; blockLastPos = transform.position; }
                            yield return null;
                        }

                        navAgent.isStopped = true;
                        yield return new WaitForSeconds(0.3f);
                        onBlocked(true);
                        yield break;
                    }
                }
            }

            // 路径未被阻断，按原逻辑执行
            NavMeshPath path = new NavMeshPath();
            NavMesh.CalculatePath(transform.position, action.targetPosition, NavMesh.AllAreas, path);

            if (path.status == NavMeshPathStatus.PathInvalid)
            {
                onBlocked(true);
                yield break;
            }

            navAgent.isStopped = false;
            navAgent.SetPath(path);

            // Wait for movement to complete, checking for obstacles
            float stuckTimer = 0f;
            Vector3 lastPos = transform.position;

            while (navAgent.pathPending || navAgent.remainingDistance > navAgent.stoppingDistance + 0.1f)
            {
                if (owner.IsDead)
                {
                    navAgent.isStopped = true;
                    onBlocked(true);
                    yield break;
                }

                // Check if stuck (hit obstacle)
                if (Vector3.Distance(transform.position, lastPos) < 0.01f)
                {
                    stuckTimer += Time.deltaTime;
                    if (stuckTimer > 1f)
                    {
                        navAgent.isStopped = true;
                        onBlocked(true);
                        yield break;
                    }
                }
                else
                {
                    stuckTimer = 0f;
                    lastPos = transform.position;
                }

                // Check if path has become invalid
                if (navAgent.pathStatus == NavMeshPathStatus.PathInvalid)
                {
                    navAgent.isStopped = true;
                    onBlocked(true);
                    yield break;
                }

                yield return null;
            }

            navAgent.isStopped = true;
            onBlocked(false);
        }

        private IEnumerator ExecuteMeleeAttack(PlannedAction action, System.Action<bool> onBlocked)
        {
            if (action.targetUnit == null || action.targetUnit.IsDead)
            {
                onBlocked(true);
                yield break;
            }

            float dist = Vector3.Distance(transform.position, action.targetUnit.transform.position);
            if (dist > owner.stats.attackRange + 1f)
            {
                onBlocked(true);
                yield break;
            }

            // Face target
            Vector3 dir = (action.targetUnit.transform.position - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(dir);

            // Simple attack animation (scale pulse)
            yield return AttackAnimation();

            action.targetUnit.TakeDamage(owner.stats.attackPower, owner);
            Debug.Log($"[AI] {owner.stats.unitName} melee attacks for {owner.stats.attackPower} damage!");

            onBlocked(false);
        }

        private IEnumerator ExecuteRangedAttack(PlannedAction action, System.Action<bool> onBlocked)
        {
            if (action.targetUnit == null || action.targetUnit.IsDead)
            {
                onBlocked(true);
                yield break;
            }

            // Check LOS
            Vector3 from = transform.position + Vector3.up;
            Vector3 to = action.targetUnit.transform.position + Vector3.up;
            if (!CheckLineOfSight(from, to))
            {
                onBlocked(true);
                yield break;
            }

            // Face target
            Vector3 dir = (action.targetUnit.transform.position - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(dir);

            // Spawn projectile
            yield return FireProjectile(from, to);

            action.targetUnit.TakeDamage(owner.stats.attackPower, owner);
            Debug.Log($"[AI] {owner.stats.unitName} ranged attacks for {owner.stats.attackPower} damage!");

            onBlocked(false);
        }

        private IEnumerator ExecuteMoveObject(PlannedAction action, System.Action<bool> onBlocked)
        {
            if (action.targetObject == null || action.targetObject.IsDestroyed)
            {
                onBlocked(true);
                yield break;
            }

            float dist = Vector3.Distance(transform.position, action.targetObject.transform.position);
            if (dist > 3f) // interact range
            {
                onBlocked(true);
                yield break;
            }

            action.targetObject.MoveTo(action.targetPosition);
            yield return new WaitForSeconds(0.5f);
            onBlocked(false);
        }

        private IEnumerator AttackAnimation()
        {
            Vector3 originalScale = transform.localScale;
            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float scale = 1f + 0.2f * Mathf.Sin(t * Mathf.PI);
                transform.localScale = originalScale * scale;
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localScale = originalScale;
        }

        private IEnumerator FireProjectile(Vector3 from, Vector3 to)
        {
            // Create simple projectile visual
            GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.transform.localScale = Vector3.one * 0.15f;
            projectile.transform.position = from;

            // Remove collider from projectile visual
            var col = projectile.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Set red material
            var renderer = projectile.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.red;
            }

            float speed = 20f;
            float dist = Vector3.Distance(from, to);
            float duration = dist / speed;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                projectile.transform.position = Vector3.Lerp(from, to, t);
                yield return null;
            }

            Destroy(projectile);
        }

        // Utility methods delegate to NavMeshUtils
        private static float CalculatePathLength(NavMeshPath path) => NavMeshUtils.CalculatePathLength(path);
        private static Vector3 GetPointAlongPath(NavMeshPath path, float distance) => NavMeshUtils.GetPointAlongPath(path, distance);
    }
}
