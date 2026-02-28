using UnityEngine;
using UnityEngine.AI;
using TacticsGame.Unit;

namespace TacticsGame.Core
{
    public abstract class UnitBase : MonoBehaviour
    {
        [Header("Configuration")]
        public UnitStatsSO stats;

        [Header("Runtime State")]
        public float currentHealth;
        public float currentAP;
        public float actionBar;

        public bool IsDead => currentHealth <= 0f;
        [Header("Team")]
        public bool isPlayerUnit = false;
        public bool IsPlayer => isPlayerUnit;

        protected NavMeshAgent navAgent;

        protected virtual void Awake()
        {
            navAgent = GetComponent<NavMeshAgent>();
        }

        public virtual void Initialize()
        {
            if (stats == null)
            {
                Debug.LogError($"[UnitBase] {name} has null stats! Cannot initialize.");
                return;
            }

            currentHealth = stats.maxHealth;
            currentAP = 0f;
            actionBar = 0f;

            if (navAgent != null)
            {
                navAgent.speed = stats.moveSpeed;
                navAgent.isStopped = true;
            }
        }

        public abstract void OnTurnStart();
        public abstract void OnTurnEnd();

        public void ResetAP()
        {
            currentAP = stats.maxActionPoints;
        }

        public bool HasEnoughAP(float cost)
        {
            return currentAP >= cost;
        }

        public void ConsumeAP(float amount)
        {
            currentAP = Mathf.Max(0f, currentAP - amount);
        }

        public float GetMovementAPCost(float distance)
        {
            return distance * stats.movementAPPerMeter;
        }

        public float GetMaxMoveDistance()
        {
            return currentAP / stats.movementAPPerMeter;
        }

        public virtual void TakeDamage(float damage, UnitBase attacker = null)
        {
            currentHealth = Mathf.Max(0f, currentHealth - damage);
            EventBus.RaiseUnitDamaged(attacker, this, damage);

            if (IsDead)
            {
                OnDeath();
            }
        }

        protected virtual void OnDeath()
        {
            EventBus.RaiseUnitDied(this);
        }
    }
}
