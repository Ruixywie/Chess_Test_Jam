using System.Collections.Generic;
using UnityEngine;
using TacticsGame.Environment;

namespace TacticsGame.Core
{
    public enum ActionType
    {
        Move,
        MeleeAttack,
        RangedAttack,
        MoveObject
    }

    [System.Serializable]
    public class PlannedAction
    {
        public ActionType type;
        public Vector3 targetPosition;
        public UnitBase targetUnit;
        public InteractableObject targetObject;
        public float apCost;
        public Vector3[] pathCorners;  // 保存计划时的路径拐点

        public PlannedAction(ActionType type, Vector3 targetPos, float apCost)
        {
            this.type = type;
            this.targetPosition = targetPos;
            this.apCost = apCost;
        }
    }

    [System.Serializable]
    public class ActionPlan
    {
        public List<PlannedAction> actions = new List<PlannedAction>();
        public bool isValid = true;

        public float TotalAPCost
        {
            get
            {
                float total = 0f;
                foreach (var action in actions)
                    total += action.apCost;
                return total;
            }
        }
    }
}
