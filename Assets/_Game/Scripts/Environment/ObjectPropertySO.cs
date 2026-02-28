using UnityEngine;

namespace TacticsGame.Environment
{
    [CreateAssetMenu(fileName = "ObjectProperty", menuName = "Game/Object Property")]
    public class ObjectPropertySO : ScriptableObject
    {
        public string objectName = "Object";
        public float durability = 30f;
        public float weight = 1f;
        public bool isMovable = true;
        public bool isDestructible = true;
        public bool isStructural = false;
    }
}
