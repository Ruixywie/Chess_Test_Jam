using System.Collections;
using UnityEngine;
using TacticsGame.Core;
using TacticsGame.Environment;

namespace TacticsGame.Combat
{
    public class CombatManager : MonoBehaviour
    {
        public static CombatManager Instance { get; private set; }

        [Header("Projectile Settings")]
        [SerializeField] private float projectileSpeed = 25f;
        [SerializeField] private float projectileSize = 0.12f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Perform a ranged attack from attacker toward target position.
        /// Uses raycast to determine what gets hit first (straight line trajectory).
        /// </summary>
        public IEnumerator RangedAttack(UnitBase attacker, Vector3 targetPos)
        {
            Vector3 origin = attacker.transform.position + Vector3.up;
            Vector3 direction = (targetPos + Vector3.up - origin).normalized;
            float maxRange = attacker.stats.attackRange;

            // Face target
            Vector3 lookDir = new Vector3(direction.x, 0, direction.z);
            if (lookDir != Vector3.zero)
                attacker.transform.rotation = Quaternion.LookRotation(lookDir);

            // Raycast to find what we hit
            RaycastHit hit;
            Vector3 endPos;
            bool hitSomething = Physics.Raycast(origin, direction, out hit, maxRange);

            if (hitSomething)
            {
                endPos = hit.point;
            }
            else
            {
                endPos = origin + direction * maxRange;
            }

            // Fire projectile visual
            yield return FireProjectileVisual(origin, endPos);

            // Apply damage
            if (hitSomething)
            {
                // Check if we hit a unit
                var targetUnit = hit.collider.GetComponent<UnitBase>();
                if (targetUnit != null)
                {
                    targetUnit.TakeDamage(attacker.stats.attackPower, attacker);
                    Debug.Log($"[Combat] {attacker.stats.unitName} shot {targetUnit.stats.unitName} for {attacker.stats.attackPower} damage!");
                }

                // Check if we hit an interactable object
                var targetObj = hit.collider.GetComponent<InteractableObject>();
                if (targetObj != null)
                {
                    targetObj.TakeDamage(attacker.stats.attackPower, attacker);
                    Debug.Log($"[Combat] {attacker.stats.unitName} shot {targetObj.properties?.objectName ?? "object"} for {attacker.stats.attackPower} damage!");
                }
            }

            attacker.ConsumeAP(attacker.stats.rangedAttackAPCost);
        }

        /// <summary>
        /// Check if a ranged attack from origin toward target has line of sight.
        /// Returns the RaycastHit info.
        /// </summary>
        public bool CheckLineOfSight(Vector3 from, Vector3 to, out RaycastHit hitInfo)
        {
            Vector3 direction = (to - from).normalized;
            float distance = Vector3.Distance(from, to);
            return Physics.Raycast(from, direction, out hitInfo, distance);
        }

        /// <summary>
        /// Get the predicted hit point and target for a ranged attack.
        /// </summary>
        public (Vector3 hitPoint, GameObject hitObject) PredictRangedHit(Vector3 origin, Vector3 direction, float maxRange)
        {
            if (Physics.Raycast(origin, direction, out RaycastHit hit, maxRange))
            {
                return (hit.point, hit.collider.gameObject);
            }
            return (origin + direction * maxRange, null);
        }

        private IEnumerator FireProjectileVisual(Vector3 from, Vector3 to)
        {
            GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.transform.localScale = Vector3.one * projectileSize;
            projectile.name = "Projectile";

            // Remove collider
            var col = projectile.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Set color
            var renderer = projectile.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(1f, 0.8f, 0.2f); // Yellow-orange
            }

            float distance = Vector3.Distance(from, to);
            float duration = distance / projectileSpeed;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                projectile.transform.position = Vector3.Lerp(from, to, t);
                yield return null;
            }

            // Impact effect (quick flash)
            projectile.transform.position = to;
            projectile.transform.localScale = Vector3.one * projectileSize * 3f;
            if (renderer != null)
                renderer.material.color = Color.white;

            yield return new WaitForSeconds(0.1f);
            Destroy(projectile);
        }
    }
}
