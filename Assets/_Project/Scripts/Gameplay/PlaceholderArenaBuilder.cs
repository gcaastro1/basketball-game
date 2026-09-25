using UnityEngine;

namespace Basket.Gameplay
{
    public readonly struct Arena
    {
        public readonly GameObject Root;
        public readonly BallController Ball;
        public readonly HoopController Hoop;

        public Arena(GameObject root, BallController ball, HoopController hoop)
        {
            Root = root;
            Ball = ball;
            Hoop = hoop;
        }
    }

    // Builds a primitive-only half court at runtime from CourtConfig: floor, invisible
    // walls, 3PT line, backboard, a physical rim ring, the hoop detector and the ball.
    // PROVISIONAL (docs/decisoes.md, D-003): replaced by an authored arena scene/prefab
    // once art exists; gameplay only depends on the Ball/Hoop it returns.
    public static class PlaceholderArenaBuilder
    {
        private static readonly Color FloorColor = new Color(0.80f, 0.62f, 0.42f);
        private static readonly Color LineColor = Color.white;
        private static readonly Color RimColor = new Color(1f, 0.45f, 0.1f);
        private static readonly Color BackboardColor = new Color(0.92f, 0.92f, 0.96f);
        private static readonly Color BallColor = new Color(0.9f, 0.42f, 0.12f);

        public static Arena Build(CourtConfig court, BallConfig ballConfig, float threePointRadius)
        {
            var root = new GameObject("Arena");
            BuildLight(root.transform);
            BuildFloor(court, root.transform);
            BuildWalls(court, root.transform);
            BuildThreePointLine(court, threePointRadius, root.transform);
            BuildBackboard(court, root.transform);
            HoopController hoop = BuildRim(court, root.transform);
            BallController ball = BuildBall(court, ballConfig, root.transform);
            hoop.Configure(ball, court.rimRadius);
            return new Arena(root, ball, hoop);
        }

        private static void BuildLight(Transform parent)
        {
            var go = new GameObject("Sun");
            go.transform.SetParent(parent, false);
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
        }

        private static void BuildFloor(CourtConfig court, Transform parent)
        {
            GameObject floor = CreateVisual(PrimitiveType.Cube, "CourtFloor", parent, FloorColor, keepCollider: true);
            floor.transform.localScale = new Vector3(court.width, 0.2f, court.depth);
            floor.transform.position = new Vector3(0f, -0.1f, court.depth * 0.5f);
            floor.AddComponent<CourtSurface>();
            PhysicsLayers.Assign(floor, PhysicsLayers.Court);
        }

        // Invisible boundary colliders around the court's flat floor. Found missing during
        // manual playtesting: with no walls, a player can simply walk off the edge.
        private static void BuildWalls(CourtConfig court, Transform parent)
        {
            float halfWidth = court.width * 0.5f;
            float depth = court.depth;
            float h = court.wallHeight;
            const float t = 0.5f;
            CreateWall("WallWest", parent, new Vector3(-halfWidth - t / 2f, h / 2f, depth / 2f), new Vector3(t, h, depth + t * 2f));
            CreateWall("WallEast", parent, new Vector3(halfWidth + t / 2f, h / 2f, depth / 2f), new Vector3(t, h, depth + t * 2f));
            CreateWall("WallSouth", parent, new Vector3(0f, h / 2f, -t / 2f), new Vector3(halfWidth * 2f, h, t));
            CreateWall("WallNorth", parent, new Vector3(0f, h / 2f, depth + t / 2f), new Vector3(halfWidth * 2f, h, t));
        }

        private static void CreateWall(string name, Transform parent, Vector3 position, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.AddComponent<BoxCollider>().size = size;
        }

        private static void BuildThreePointLine(CourtConfig court, float radius, Transform parent)
        {
            var line = new GameObject("ThreePointLine");
            line.transform.SetParent(parent, false);
            Vector3 center = court.RimFloorProjection;
            const int segments = 48;
            float halfWidth = court.width * 0.5f;
            for (int i = 0; i < segments; i++)
            {
                float a0 = Mathf.Lerp(-90f, 90f, i / (float)segments) * Mathf.Deg2Rad;
                float a1 = Mathf.Lerp(-90f, 90f, (i + 1) / (float)segments) * Mathf.Deg2Rad;
                Vector3 p0 = center + new Vector3(Mathf.Sin(a0), 0f, -Mathf.Cos(a0)) * radius;
                Vector3 p1 = center + new Vector3(Mathf.Sin(a1), 0f, -Mathf.Cos(a1)) * radius;
                if (Mathf.Abs(p0.x) > halfWidth || Mathf.Abs(p1.x) > halfWidth || p0.z < 0f || p1.z < 0f) continue;

                GameObject seg = CreateVisual(PrimitiveType.Cube, "Segment", line.transform, LineColor, keepCollider: false);
                seg.transform.position = (p0 + p1) * 0.5f + Vector3.up * 0.005f;
                seg.transform.rotation = Quaternion.LookRotation(p1 - p0, Vector3.up);
                seg.transform.localScale = new Vector3(0.05f, 0.01f, (p1 - p0).magnitude + 0.01f);
            }
        }

        private static void BuildBackboard(CourtConfig court, Transform parent)
        {
            GameObject board = CreateVisual(PrimitiveType.Cube, "Backboard", parent, BackboardColor, keepCollider: true);
            board.transform.position = court.backboardCenter;
            board.transform.localScale = court.backboardSize;
            PhysicsLayers.Assign(board, PhysicsLayers.Hoop);
        }

        // A ring of thin capsules: the ball can hit the front/back rim and roll around it.
        private static HoopController BuildRim(CourtConfig court, Transform parent)
        {
            var hoopGo = new GameObject("Hoop");
            hoopGo.transform.SetParent(parent, false);
            hoopGo.transform.position = court.rimCenter;
            PhysicsLayers.Assign(hoopGo, PhysicsLayers.Hoop);

            int n = Mathf.Max(8, court.rimSegments);
            float segmentLength = 2f * Mathf.PI * court.rimRadius / n;
            for (int i = 0; i < n; i++)
            {
                float angle = (i + 0.5f) / n * Mathf.PI * 2f;
                Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 tangent = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle));

                // Unit capsule primitive: height 2 along Y, radius 0.5.
                GameObject seg = CreateVisual(PrimitiveType.Capsule, "RimSegment", hoopGo.transform, RimColor, keepCollider: true);
                seg.transform.position = court.rimCenter + radial * court.rimRadius;
                seg.transform.rotation = Quaternion.FromToRotation(Vector3.up, tangent);
                float diameter = court.rimTubeRadius * 2f;
                seg.transform.localScale = new Vector3(diameter, (segmentLength + diameter) * 0.5f, diameter);
                seg.AddComponent<RimSurface>();
                PhysicsLayers.Assign(seg, PhysicsLayers.Hoop);
            }

            return hoopGo.AddComponent<HoopController>();
        }

        private static BallController BuildBall(CourtConfig court, BallConfig config, Transform parent)
        {
            GameObject ballGo = CreateVisual(PrimitiveType.Sphere, "Ball", parent, BallColor, keepCollider: true);
            ballGo.transform.localScale = Vector3.one * 0.24f;
            ballGo.transform.position = court.checkBallSpot + Vector3.up * 1f;
            PhysicsLayers.Assign(ballGo, PhysicsLayers.Ball);

            ballGo.GetComponent<SphereCollider>().material = new PhysicsMaterial("BallPhysics")
            {
                bounciness = config.bounciness,
                bounceCombine = PhysicsMaterialCombine.Maximum
            };
            ballGo.AddComponent<Rigidbody>();
            BallController ball = ballGo.AddComponent<BallController>();
            ball.Configure(config);
            return ball;
        }

        internal static GameObject CreateVisual(PrimitiveType type, string name, Transform parent, Color color, bool keepCollider)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().material.color = color;
            return go;
        }
    }
}
