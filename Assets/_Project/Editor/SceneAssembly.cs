using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Basket.Core;
using Basket.Gameplay;
using Basket.AI;
using Basket.Input;
using Basket.UI;
using Basket.Bootstrap;

namespace Basket.EditorTools
{
    public static class SceneAssembly
    {
        [MenuItem("Basket/Build Vertical Slice Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildCourt();
            BuildCourtWalls();
            Transform rim = BuildRimAndBackboard();
            BallController ball = BuildBall();
            (PlayerMotor humanMotor, HumanInputProvider humanInput) = BuildHumanPlayer();
            (PlayerMotor aiMotor, AIOpponentController aiController) = BuildAIOpponent();
            PassSystem passSystem = CreateComponent<PassSystem>("PassSystem");
            ShootingSystem shootingSystem = CreateComponent<ShootingSystem>("ShootingSystem");
            DribbleSystem dribbleSystem = CreateComponent<DribbleSystem>("DribbleSystem");
            MatchManager matchManager = CreateComponent<MatchManager>("MatchManager");
            ScoreTrigger scoreTrigger = rim.gameObject.AddComponent<ScoreTrigger>();
            CameraController cameraController = BuildCamera();
            DebugHud debugHud = CreateComponent<DebugHud>("DebugHud");

            BallConfig ballConfig = LoadOrCreateAsset<BallConfig>("Assets/_Project/Data/DefaultBallConfig.asset");
            ShotConfig shotConfig = LoadOrCreateAsset<ShotConfig>("Assets/_Project/Data/DefaultShotConfig.asset");

            // Not in the original plan text: PlayerMotor.config (on both motors) and
            // CameraController.config are private [SerializeField] ScriptableObject
            // references with no default instance. Left unwired, they are null in the
            // assembled scene, and PlayerMotor.Tick()/CameraController.LateUpdate() both
            // dereference config unconditionally every frame once play starts (Tick is
            // called every Update by GameBootstrap for both agents; LateUpdate runs once
            // CameraController.Configure() has set a target, which GameBootstrap.Awake()
            // always does) -- a guaranteed NullReferenceException on frame 1, not a
            // probabilistic one. BallController.config has the same gap but is guarded
            // (only read while Held), so it is a latent bug rather than a guaranteed
            // crash; still wired here for correctness, reusing the same DefaultBallConfig
            // asset already created above. Same LoadOrCreateAsset + SerializedObject
            // pattern already used elsewhere in this method, just completing coverage of
            // it to every component that needs a config asset.
            PlayerMovementConfig playerMovementConfig = LoadOrCreateAsset<PlayerMovementConfig>("Assets/_Project/Data/DefaultPlayerMovementConfig.asset");
            CameraConfig cameraConfig = LoadOrCreateAsset<CameraConfig>("Assets/_Project/Data/DefaultCameraConfig.asset");

            WireConfig(ball, "config", ballConfig);
            ApplyBallPhysicsMaterial(ball, ballConfig);
            WireConfig(humanMotor, "config", playerMovementConfig);
            WireConfig(aiMotor, "config", playerMovementConfig);
            WireConfig(cameraController, "config", cameraConfig);

            var bootstrapGo = new GameObject("GameBootstrap");
            var bootstrap = bootstrapGo.AddComponent<GameBootstrap>();
            var so = new SerializedObject(bootstrap);
            so.FindProperty("humanMotor").objectReferenceValue = humanMotor;
            so.FindProperty("humanInput").objectReferenceValue = humanInput;
            so.FindProperty("aiMotor").objectReferenceValue = aiMotor;
            so.FindProperty("aiController").objectReferenceValue = aiController;
            so.FindProperty("ball").objectReferenceValue = ball;
            so.FindProperty("passSystem").objectReferenceValue = passSystem;
            so.FindProperty("shootingSystem").objectReferenceValue = shootingSystem;
            so.FindProperty("dribbleSystem").objectReferenceValue = dribbleSystem;
            so.FindProperty("matchManager").objectReferenceValue = matchManager;
            so.FindProperty("scoreTrigger").objectReferenceValue = scoreTrigger;
            so.FindProperty("cameraController").objectReferenceValue = cameraController;
            so.FindProperty("debugHud").objectReferenceValue = debugHud;
            so.FindProperty("ballConfig").objectReferenceValue = ballConfig;
            so.FindProperty("shotConfig").objectReferenceValue = shotConfig;
            so.FindProperty("rimTarget").objectReferenceValue = rim;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/01_VerticalSlice_HalfCourt.unity");
            Debug.Log("Vertical slice scene built and saved.");
        }

        [MenuItem("Basket/Add Vertical Slice Scene To Build Settings")]
        public static void AddSceneToBuildSettings()
        {
            const string scenePath = "Assets/_Project/Scenes/01_VerticalSlice_HalfCourt.unity";
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == scenePath)) return;
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void BuildCourt()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "CourtFloor";
            floor.transform.localScale = new Vector3(15f, 0.2f, 14f);
            floor.transform.position = new Vector3(0f, -0.1f, 7f);
        }

        // Invisible boundary colliders around the court's flat floor (x: -7.5..7.5,
        // z: 0..14). Found missing during manual playtesting: with no walls, a player
        // can simply walk off the edge into empty space with no way back.
        private static void BuildCourtWalls()
        {
            const float halfWidth = 7.5f;
            const float depth = 14f;
            const float wallHeight = 3f;
            const float wallThickness = 0.5f;

            CreateWall("WallWest", new Vector3(-halfWidth - wallThickness / 2f, wallHeight / 2f, depth / 2f), new Vector3(wallThickness, wallHeight, depth + wallThickness * 2f));
            CreateWall("WallEast", new Vector3(halfWidth + wallThickness / 2f, wallHeight / 2f, depth / 2f), new Vector3(wallThickness, wallHeight, depth + wallThickness * 2f));
            CreateWall("WallSouth", new Vector3(0f, wallHeight / 2f, -wallThickness / 2f), new Vector3(halfWidth * 2f, wallHeight, wallThickness));
            CreateWall("WallNorth", new Vector3(0f, wallHeight / 2f, depth + wallThickness / 2f), new Vector3(halfWidth * 2f, wallHeight, wallThickness));
        }

        private static void CreateWall(string name, Vector3 position, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            var collider = go.AddComponent<BoxCollider>();
            collider.size = size;
        }

        private static Transform BuildRimAndBackboard()
        {
            GameObject backboard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backboard.name = "Backboard";
            backboard.transform.localScale = new Vector3(1.8f, 1.05f, 0.05f);
            backboard.transform.position = new Vector3(0f, 3.05f, 13.5f);

            GameObject rim = new GameObject("RimTrigger");
            rim.transform.position = new Vector3(0f, 3.05f, 13f);
            SphereCollider trigger = rim.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.3f;
            rim.AddComponent<Rigidbody>().isKinematic = true;

            return rim.transform;
        }

        private static BallController BuildBall()
        {
            GameObject ballGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ballGo.name = "Ball";
            ballGo.transform.localScale = Vector3.one * 0.24f;
            ballGo.transform.position = new Vector3(0f, 1.1f, 3f);
            ballGo.AddComponent<Rigidbody>();
            return ballGo.AddComponent<BallController>();
        }

        private static (PlayerMotor, HumanInputProvider) BuildHumanPlayer()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "HumanPlayer";
            go.transform.position = new Vector3(-2f, 1f, 3f);
            Object.DestroyImmediate(go.GetComponent<CapsuleCollider>());
            go.AddComponent<CharacterController>();
            go.AddComponent<PlayerMarker>();
            var motor = go.AddComponent<PlayerMotor>();
            var input = go.AddComponent<HumanInputProvider>();
            return (motor, input);
        }

        private static (PlayerMotor, AIOpponentController) BuildAIOpponent()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "AIOpponent";
            go.transform.position = new Vector3(2f, 1f, 10f);
            Object.DestroyImmediate(go.GetComponent<CapsuleCollider>());
            go.AddComponent<CharacterController>();
            go.AddComponent<PlayerMarker>();
            var motor = go.AddComponent<PlayerMotor>();
            var controller = go.AddComponent<AIOpponentController>();
            return (motor, controller);
        }

        private static CameraController BuildCamera()
        {
            var camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            camGo.AddComponent<Camera>();
            return camGo.AddComponent<CameraController>();
        }

        private static T CreateComponent<T>(string goName) where T : Component
        {
            var go = new GameObject(goName);
            return go.AddComponent<T>();
        }

        private static T LoadOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static void WireConfig(Component component, string fieldName, Object configAsset)
        {
            var so = new SerializedObject(component);
            so.FindProperty(fieldName).objectReferenceValue = configAsset;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ApplyBallPhysicsMaterial(BallController ball, BallConfig config)
        {
            var material = new PhysicsMaterial("BallPhysics")
            {
                bounciness = config.bounciness,
                bounceCombine = PhysicsMaterialCombine.Maximum
            };
            ball.GetComponent<SphereCollider>().material = material;
        }
    }
}
