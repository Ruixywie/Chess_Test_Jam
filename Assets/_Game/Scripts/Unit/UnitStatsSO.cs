using UnityEngine;

namespace TacticsGame.Unit
{
    [CreateAssetMenu(fileName = "UnitStats", menuName = "Game/Unit Stats")]
    public class UnitStatsSO : ScriptableObject
    {
        public string unitName = "Unit";
        public float maxHealth = 100f;
        public float speed = 5f;                  // Action bar fill speed
        public float maxActionPoints = 8f;
        public float attackPower = 10f;
        public float attackRange = 2f;
        public float moveSpeed = 4f;              // NavMesh agent speed
        public float movementAPPerMeter = 0.5f;
        public float meleeAttackAPCost = 3f;
        public float rangedAttackAPCost = 4f;
        public float moveObjectAPCost = 3f;
        public bool canRangedAttack = false;
        public bool canMeleeAttack = true;
    }
}
