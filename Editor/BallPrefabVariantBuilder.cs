using BounceHeroes.Data;
using BounceHeroes.Gameplay;
using UnityEditor;
using UnityEngine;

namespace BounceHeroes.EditorTools
{
    public static class BallPrefabVariantBuilder
    {
        private const string BasePrefabPath = "Assets/Game Resources/Icons/Prefabs/PF_Ball.prefab";
        private const string VariantFolder = "Assets/Game Resources/Ball/Prefabs";

        [MenuItem("BounceHeroes/Tools/Rebuild Ball Prefab Variants")]
        public static void Rebuild()
        {
            EnsureFolder("Assets/Game Resources/Ball", "Prefabs");

            Ball basePrefab = AssetDatabase.LoadAssetAtPath<Ball>(BasePrefabPath);

            if (basePrefab == null)
            {
                Debug.LogError($"Ball base prefab not found: {BasePrefabPath}");
                return;
            }

            CreateVariant(basePrefab, "PF_Ball_Normal",
                "Assets/Settings/GameData/BallVisuals/SO_BallVisual_Normal.asset",
                "Assets/Game Resources/Ball/Art/Textures/Ball_Nomal_Ball.png");
            CreateVariant(basePrefab, "PF_Ball_Cluster",
                "Assets/Settings/GameData/BallVisuals/SO_BallVisual_Cluster.asset",
                "Assets/Game Resources/Ball/Art/Textures/Ball_Cluster_Ball.png");
            CreateVariant(basePrefab, "PF_Ball_Fire",
                "Assets/Settings/GameData/BallVisuals/SO_BallVisual_Fire.asset",
                "Assets/Game Resources/Ball/Art/Textures/Ball_Fire_ball.png");
            CreateVariant(basePrefab, "PF_Ball_Ghost",
                "Assets/Settings/GameData/BallVisuals/SO_BallVisual_Ghost.asset",
                "Assets/Game Resources/Ball/Art/Textures/Ball_Ghost_Ball.png");
            CreateVariant(basePrefab, "PF_Ball_Ice",
                "Assets/Settings/GameData/BallVisuals/SO_BallVisual_Ice.asset",
                "Assets/Game Resources/Ball/Art/Textures/Ball_Ice_Ball.png");
            CreateVariant(basePrefab, "PF_Ball_Laser",
                "Assets/Settings/GameData/BallVisuals/SO_BallVisual_Laser.asset",
                "Assets/Game Resources/Ball/Art/Textures/Ball_Laser_Ball.png");

            AssignSkillPrefab("Assets/Settings/GameData/Skills/SO_Skill_ClusterBall.asset", $"{VariantFolder}/PF_Ball_Cluster.prefab");
            AssignSkillPrefab("Assets/Settings/GameData/Skills/SO_Skill_FireBall.asset", $"{VariantFolder}/PF_Ball_Fire.prefab");
            AssignSkillPrefab("Assets/Settings/GameData/Skills/SO_Skill_GhostBall.asset", $"{VariantFolder}/PF_Ball_Ghost.prefab");
            AssignSkillPrefab("Assets/Settings/GameData/Skills/SO_Skill_IceBall.asset", $"{VariantFolder}/PF_Ball_Ice.prefab");
            AssignSkillPrefab("Assets/Settings/GameData/Skills/SO_Skill_LaserBall.asset", $"{VariantFolder}/PF_Ball_Laser.prefab");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Ball prefab variants rebuilt.");
        }

        private static void CreateVariant(Ball basePrefab, string variantName, string profilePath, string spritePath)
        {
            BallVisualProfile profile = AssetDatabase.LoadAssetAtPath<BallVisualProfile>(profilePath);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

            if (profile == null)
            {
                Debug.LogError($"Ball visual profile not found: {profilePath}");
                return;
            }

            if (sprite == null)
            {
                Debug.LogError($"Ball sprite not found: {spritePath}");
                return;
            }

            string variantPath = $"{VariantFolder}/{variantName}.prefab";
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab.gameObject);
            instance.name = variantName;

            SpriteRenderer spriteRenderer = instance.GetComponent<SpriteRenderer>();

            if (spriteRenderer != null)
            {
                SerializedObject serializedRenderer = new SerializedObject(spriteRenderer);
                serializedRenderer.FindProperty("m_Sprite").objectReferenceValue = sprite;
                serializedRenderer.FindProperty("m_Color").colorValue = Color.white;
                serializedRenderer.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(spriteRenderer);
            }

            BallVisualController visual = instance.GetComponent<BallVisualController>();

            if (visual != null)
            {
                SerializedObject serializedVisual = new SerializedObject(visual);
                serializedVisual.FindProperty("visualProfile").objectReferenceValue = profile;
                serializedVisual.ApplyModifiedPropertiesWithoutUndo();
                visual.Apply();
                EditorUtility.SetDirty(visual);
            }

            PrefabUtility.SaveAsPrefabAsset(instance, variantPath);
            Object.DestroyImmediate(instance);
        }

        private static void AssignSkillPrefab(string skillPath, string prefabPath)
        {
            ActiveSkillData skill = AssetDatabase.LoadAssetAtPath<ActiveSkillData>(skillPath);
            Ball prefab = AssetDatabase.LoadAssetAtPath<Ball>(prefabPath);

            if (skill == null || prefab == null)
            {
                Debug.LogError($"Failed to assign ball prefab. Skill: {skillPath}, Prefab: {prefabPath}");
                return;
            }

            SerializedObject serializedSkill = new SerializedObject(skill);
            serializedSkill.FindProperty("ballPrefab").objectReferenceValue = prefab;
            serializedSkill.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(skill);
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = $"{parent}/{child}";

            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}
