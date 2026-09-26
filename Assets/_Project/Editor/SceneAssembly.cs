using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Basket.AI;
using Basket.Bootstrap;
using Basket.Characters;
using Basket.Gameplay;
using Basket.Meta;

namespace Basket.EditorTools
{
    // The match scenes (3v3 FIBA 3x3; 5v5 full court) only contain a GameBootstrap wired to config assets; the
    // arena, players, camera and HUD are built at runtime from those configs
    // (docs/decisoes.md, D-003). These menus recreate those scenes and any missing assets.
    public static class SceneAssembly
    {
        private const string ScenePath = "Assets/_Project/Scenes/01_VerticalSlice_HalfCourt.unity";
        private const string FullCourtScenePath = "Assets/_Project/Scenes/02_FullCourt_5v5.unity";
        private const string DataFolder = "Assets/_Project/Data";
        private const string MetaFolder = "Assets/_Project/Data/Meta";

        [MenuItem("Basket/Build Vertical Slice Scene")]
        public static void Build() =>
            BuildScene(ScenePath, "MatchSetup3v3", "FIBA3x3MatchRules", "DefaultCourtConfig");

        // Etapa 7: 5v5 on the full court (two baskets), FIBA 5v5 rules.
        [MenuItem("Basket/Build Full Court 5v5 Scene")]
        public static void BuildFullCourt() =>
            BuildScene(FullCourtScenePath, "MatchSetup5v5", "FIBA5v5MatchRules", "Court5v5Config");

        private static void BuildScene(string scenePath, string setup, string rules, string court)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var bootstrap = new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
            var so = new SerializedObject(bootstrap);
            so.FindProperty("matchSetup").objectReferenceValue = LoadOrCreateAsset<MatchSetup>(setup);
            so.FindProperty("matchRules").objectReferenceValue = LoadOrCreateAsset<MatchRules>(rules);
            so.FindProperty("courtConfig").objectReferenceValue = LoadOrCreateAsset<CourtConfig>(court);
            so.FindProperty("ballConfig").objectReferenceValue = LoadOrCreateAsset<BallConfig>("DefaultBallConfig");
            so.FindProperty("shotConfig").objectReferenceValue = LoadOrCreateAsset<ShotConfig>("DefaultShotConfig");
            so.FindProperty("defenseConfig").objectReferenceValue = LoadOrCreateAsset<DefenseConfig>("DefaultDefenseConfig");
            so.FindProperty("movementConfig").objectReferenceValue = LoadOrCreateAsset<PlayerMovementConfig>("DefaultPlayerMovementConfig");
            so.FindProperty("cameraConfig").objectReferenceValue = LoadOrCreateAsset<CameraConfig>("DefaultCameraConfig");
            so.FindProperty("aiConfig").objectReferenceValue = LoadOrCreateAsset<AIConfig>("DefaultAIConfig");
            so.FindProperty("progressionConfig").objectReferenceValue = LoadOrCreateAsset<ProgressionConfig>("DefaultProgressionConfig");
            so.FindProperty("attributeTuning").objectReferenceValue = LoadOrCreateAsset<AttributeTuning>("DefaultAttributeTuning");
            // Meta (Etapa 8.5, I1): sem isto, EnsureConfigs() sempre cai nos defaults em branco
            // em jogo real -- o elenco do perfil nunca entra e a recompensa nunca paga.
            so.FindProperty("itemCatalog").objectReferenceValue = LoadOrCreateAsset<ItemCatalog>("ItemCatalog", MetaFolder);
            so.FindProperty("characterCatalog").objectReferenceValue = LoadOrCreateAsset<CharacterCatalog>("CharacterCatalog", MetaFolder);
            so.FindProperty("obtainRules").objectReferenceValue = LoadOrCreateAsset<CharacterObtainRules>("CharacterObtainRules", MetaFolder);
            so.FindProperty("rewardRules").objectReferenceValue = LoadOrCreateAsset<MatchRewardRules>("MatchRewardRules", MetaFolder);
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, scenePath);
            AddSceneToBuildSettings();
            Debug.Log($"Scene {scenePath} built and saved.");
        }

        [MenuItem("Basket/Add Match Scenes To Build Settings")]
        public static void AddSceneToBuildSettings()
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            string[] ours = { ScenePath, FullCourtScenePath };
            for (int i = 0; i < ours.Length; i++)
            {
                string path = ours[i];
                if (scenes.Exists(s => s.path == path)) continue;
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) continue;
                scenes.Insert(Mathf.Min(i, scenes.Count), new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static T LoadOrCreateAsset<T>(string assetName, string folder = DataFolder) where T : ScriptableObject
        {
            string path = $"{folder}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return asset;
        }
    }
}
