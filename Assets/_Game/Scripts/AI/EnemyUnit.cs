using UnityEngine;
using TacticsGame.Core;

namespace TacticsGame.AI
{
    public enum EnemyType
    {
        Melee,
        Ranged
    }

    public class EnemyUnit : UnitBase
    {
        [Header("Enemy Settings")]
        public EnemyType enemyType = EnemyType.Melee;

        private AIBrain _aiBrain;

        public AIBrain Brain => _aiBrain ??= GetComponent<AIBrain>();

        protected override void Awake()
        {
            base.Awake();
        }

        public override void OnTurnStart()
        {
            ResetAP();
            Debug.Log($"[Enemy] {stats.unitName} turn started. AP: {currentAP}");
        }

        public override void OnTurnEnd()
        {
            // After turn ends, immediately formulate next plan
            if (Brain != null && !IsDead)
            {
                Brain.FormulatePlan();
            }
            Debug.Log($"[Enemy] {stats.unitName} turn ended.");
        }

        protected override void OnDeath()
        {
            base.OnDeath();
            // Clear intent visualization
            if (Brain != null)
            {
                Brain.ClearPlan();
            }
            Debug.Log($"[Enemy] {stats.unitName} has died!");
        }
    }
}
