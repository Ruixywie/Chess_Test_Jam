#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TacticsGame.Core;
using TacticsGame.Unit;
using TacticsGame.Environment;

namespace TacticsGame.Editor
{
    public static class SceneSetupEditor
    {
        [MenuItem("Tools/Tactics Game/Setup Demo Scene")]
        public static void SetupDemoScene()
        {
            // First ensure data assets exist
            DataAssetCreator.CreateAllAssets();

            // Create or find DemoSceneBuilder
            var builder = Object.FindFirstObjectByType<DemoSceneBuilder>();
            if (builder == null)
            {
                var obj = new GameObject("_DemoSceneBuilder");
                builder = obj.AddComponent<DemoSceneBuilder>();
            }

            // Assign references
            builder.playerStats = AssetDatabase.LoadAssetAtPath<UnitStatsSO>("Assets/_Game/Data/UnitStats/PlayerStats.asset");
            builder.meleeEnemyStats = AssetDatabase.LoadAssetAtPath<UnitStatsSO>("Assets/_Game/Data/UnitStats/MeleeEnemyStats.asset");
            builder.rangedEnemyStats = AssetDatabase.LoadAssetAtPath<UnitStatsSO>("Assets/_Game/Data/UnitStats/RangedEnemyStats.asset");

            builder.woodenBoxProps = AssetDatabase.LoadAssetAtPath<ObjectPropertySO>("Assets/_Game/Data/ObjectProperties/WoodenBox.asset");
            builder.barrelProps = AssetDatabase.LoadAssetAtPath<ObjectPropertySO>("Assets/_Game/Data/ObjectProperties/Barrel.asset");
            builder.floorProps = AssetDatabase.LoadAssetAtPath<ObjectPropertySO>("Assets/_Game/Data/ObjectProperties/FloorPanel.asset");
            builder.stairProps = AssetDatabase.LoadAssetAtPath<ObjectPropertySO>("Assets/_Game/Data/ObjectProperties/StairStep.asset");
            builder.wallProps = AssetDatabase.LoadAssetAtPath<ObjectPropertySO>("Assets/_Game/Data/ObjectProperties/Wall.asset");

            // Mark scene as dirty
            EditorUtility.SetDirty(builder);
            EditorSceneManager.MarkSceneDirty(builder.gameObject.scene);

            Debug.Log("[SceneSetup] Demo scene builder configured! Press Play to see the demo.");
            Selection.activeGameObject = builder.gameObject;
        }
    }
}
#endif
