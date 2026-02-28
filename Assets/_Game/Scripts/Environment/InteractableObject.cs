using UnityEngine;
using UnityEngine.AI;
using TacticsGame.Core;

namespace TacticsGame.Environment
{
    public class InteractableObject : MonoBehaviour
    {
        [Header("Configuration")]
        public ObjectPropertySO properties;

        [Header("Runtime State")]
        public float currentDurability;

        private NavMeshObstacle navObstacle;
        private Rigidbody rb;

        public bool IsDestroyed => currentDurability <= 0f;
        public bool IsMovable => properties != null && properties.isMovable;
        public bool IsDestructible => properties != null && properties.isDestructible;
        public bool IsStructural => properties != null && properties.isStructural;

        private void Awake()
        {
            navObstacle = GetComponent<NavMeshObstacle>();
            rb = GetComponent<Rigidbody>();
        }

        public void Initialize()
        {
            if (properties != null)
            {
                currentDurability = properties.durability;
            }

            if (navObstacle != null)
            {
                navObstacle.carving = true;
            }
        }

        private void Start()
        {
            Initialize();
        }

        public void TakeDamage(float damage, UnitBase attacker = null)
        {
            if (!IsDestructible) return;

            currentDurability = Mathf.Max(0f, currentDurability - damage);
            EventBus.RaiseObjectDamaged(this, damage);

            if (currentDurability <= 0f)
            {
                OnDestroyed();
            }
        }

        public void MoveTo(Vector3 targetPosition)
        {
            if (!IsMovable) return;

            // Adjust Y so the object sits on top of the surface
            var col = GetComponent<Collider>();
            if (col != null)
            {
                targetPosition.y += col.bounds.extents.y;
            }

            Vector3 fromPos = transform.position;
            transform.position = targetPosition;
            EventBus.RaiseObjectMoved(this, fromPos, targetPosition);
            EventBus.RaiseEnvironmentChanged();
        }

        private void OnDestroyed()
        {
            EventBus.RaiseObjectDestroyed(this);
            EventBus.RaiseEnvironmentChanged();

            if (IsStructural)
            {
                HandleStructuralDestruction();
            }

            // Disable visuals and collision, then destroy
            gameObject.SetActive(false);
            Destroy(gameObject, 0.1f);
        }

        private void HandleStructuralDestruction()
        {
            // Check for objects above this structural piece and make them fall
            Vector3 checkPos = transform.position + Vector3.up * 1.5f;
            float checkRadius = Mathf.Max(transform.localScale.x, transform.localScale.z) * 0.5f;

            Collider[] above = Physics.OverlapSphere(checkPos, checkRadius);
            foreach (var col in above)
            {
                if (col.gameObject == gameObject) continue;

                // Make objects above fall
                var rb = col.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                }

                // Damage units that fall
                var unit = col.GetComponent<UnitBase>();
                if (unit != null)
                {
                    unit.TakeDamage(20f);
                    Debug.Log($"[Environment] {unit.stats.unitName} fell due to structural destruction!");
                }

                // Damage interactable objects that fall
                var interactable = col.GetComponent<InteractableObject>();
                if (interactable != null && interactable != this)
                {
                    interactable.TakeDamage(15f);
                }
            }
        }
    }
}
