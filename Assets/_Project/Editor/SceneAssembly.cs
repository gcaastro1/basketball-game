using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Basket.AI;
using Basket.Bootstrap;
using Basket.Gameplay;

namespace Basket.EditorTools
{
    // The vertical slice scene only contains a GameBootstrap wired to config assets; the
    // arena, players, camera and HUD are built at runtime from those configs
    // (docs/decisoes.md, D-003). This menu recreates that scene and any missing assets.
    public static class SceneAssembly
    {
        private const string ScenePath = "Assets/_Project/Scenes/01_VerticalSlice_HalfCourt.unity";
        private const string DataFolder = "Assets/_Project/Data";

        [MenuItem("Basket/Build Vertical Slice Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var bootstrap = new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
            var so = new SerializedObject(bootstrap);
            so.FindProperty("matchSetup").objectReferenceValue = LoadOrCreateAsset<MatchSetup>("DefaultMatchSetup");
            so.FindProperty("matchRules").objectReferenceValue = LoadOrCreateAsset<MatchRules>("DefaultMatchRules");
            so.FindProperty("courtConfig").objectReferenceValue = LoadOrCreateAsset<CourtConfig>("DefaultCourtConfig");
            so.FindProperty("ballConfig").objectReferenceValue = LoadOrCreateAsset<BallConfig>("DefaultBallConfig");
            so.FindProperty("shotConfig").objectReferenceValue = LoadOrCreateAsset<ShotConfig>("DefaultShotConfig");
            so.FindProperty("movementConfig").objectReferenceValue = LoadOrCreateAsset<PlayerMovementConfig>("DefaultPlayerMovementConfig");
            so.FindProperty("cameraConfig").objectReferenceValue = LoadOrCreateAsset<CameraConfig>("DefaultCameraConfig");
            so.FindProperty("aiConfig").objectReferenceValue = LoadOrCreateAsset<AIConfig>("DefaultAIConfig");
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();
            Debug.Log("Vertical slice scene built and saved.");
        }

        [MenuItem("Basket/Add Vertical Slice Scene To Build Settings")]
        public static void AddSceneToBuildSettings()
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == ScenePath)) return;
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static T LoadOrCreateAsset<T>(string assetName) where T : ScriptableObject
        {
            string path = $"{DataFolder}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return asset;
        }
    }
}
