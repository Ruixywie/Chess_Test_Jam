#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TacticsGame.Unit;
using TacticsGame.Environment;

namespace TacticsGame.Editor
{
    public static class DataAssetCreator
    {
        [MenuItem("Tools/Tactics Game/Create All Data Assets")]
        public static void CreateAllAssets()
        {
            CreateUnitStats();
            CreateObjectProperties();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DataAssetCreator] All data assets created successfully!");
        }

        private static void CreateUnitStats()
        {
            string folder = "Assets/_Game/Data/UnitStats";
            EnsureFolder(folder);

            // Player
            var player = ScriptableObject.CreateInstance<UnitStatsSO>();
            player.unitName = "Player";
            player.maxHealth = 150f;
            player.speed = 10f;
            player.maxActionPoints = 10f;
            player.attackPower = 15f;
            player.attackRange = 30f;
            player.moveSpeed = 6f;
            player.movementAPPerMeter = 0.5f;
            player.meleeAttackAPCost = 3f;
            player.rangedAttackAPCost = 4f;
            player.moveObjectAPCost = 3f;
            player.canRangedAttack = true;
            player.canMeleeAttack = false;
            AssetDatabase.CreateAsset(player, $"{folder}/PlayerStats.asset");

            // Melee Enemy
            var melee = ScriptableObject.CreateInstance<UnitStatsSO>();
            melee.unitName = "Melee";
            melee.maxHealth = 80f;
            melee.speed = 5f;
            melee.maxActionPoints = 8f;
            melee.attackPower = 20f;
            melee.attackRange = 2f;
            melee.moveSpeed = 4f;
            melee.movementAPPerMeter = 0.4f;
            melee.meleeAttackAPCost = 3f;
            melee.rangedAttackAPCost = 0f;
            melee.moveObjectAPCost = 3f;
            melee.canRangedAttack = false;
            melee.canMeleeAttack = true;
            AssetDatabase.CreateAsset(melee, $"{folder}/MeleeEnemyStats.asset");

            // Ranged Enemy
            var ranged = ScriptableObject.CreateInstance<UnitStatsSO>();
            ranged.unitName = "Ranged";
            ranged.maxHealth = 60f;
            ranged.speed = 4f;
            ranged.maxActionPoints = 8f;
            ranged.attackPower = 12f;
            ranged.attackRange = 20f;
            ranged.moveSpeed = 3f;
            ranged.movementAPPerMeter = 0.5f;
            ranged.meleeAttackAPCost = 0f;
            ranged.rangedAttackAPCost = 4f;
            ranged.moveObjectAPCost = 3f;
            ranged.canRangedAttack = true;
            ranged.canMeleeAttack = false;
            AssetDatabase.CreateAsset(ranged, $"{folder}/RangedEnemyStats.asset");
        }

        private static void CreateObjectProperties()
        {
            string folder = "Assets/_Game/Data/ObjectProperties";
            EnsureFolder(folder);

            // Wooden Box
            var box = ScriptableObject.CreateInstance<ObjectPropertySO>();
            box.objectName = "Wooden Box";
            box.durability = 30f;
            box.weight = 2f;
            box.isMovable = true;
            box.isDestructible = true;
            box.isStructural = false;
            AssetDatabase.CreateAsset(box, $"{folder}/WoodenBox.asset");

            // Barrel
            var barrel = ScriptableObject.CreateInstance<ObjectPropertySO>();
            barrel.objectName = "Barrel";
            barrel.durability = 25f;
            barrel.weight = 1.5f;
            barrel.isMovable = true;
            barrel.isDestructible = true;
            barrel.isStructural = false;
            AssetDatabase.CreateAsset(barrel, $"{folder}/Barrel.asset");

            // Floor Panel
            var floor = ScriptableObject.CreateInstance<ObjectPropertySO>();
            floor.objectName = "Floor";
            floor.durability = 50f;
            floor.weight = 999f;
            floor.isMovable = false;
            floor.isDestructible = true;
            floor.isStructural = true;
            AssetDatabase.CreateAsset(floor, $"{folder}/FloorPanel.asset");

            // Stair Step
            var stair = ScriptableObject.CreateInstance<ObjectPropertySO>();
            stair.objectName = "Stairs";
            stair.durability = 40f;
            stair.weight = 999f;
            stair.isMovable = false;
            stair.isDestructible = true;
            stair.isStructural = true;
            AssetDatabase.CreateAsset(stair, $"{folder}/StairStep.asset");

            // Wall (indestructible in demo)
            var wall = ScriptableObject.CreateInstance<ObjectPropertySO>();
            wall.objectName = "Wall";
            wall.durability = 999f;
            wall.weight = 999f;
            wall.isMovable = false;
            wall.isDestructible = false;
            wall.isStructural = true;
            AssetDatabase.CreateAsset(wall, $"{folder}/Wall.asset");
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}
#endif
