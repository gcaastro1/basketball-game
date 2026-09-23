# Vertical Slice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the smallest playable basketball loop — 1 human player vs 1 AI opponent, half-court, move/dribble/pass/shoot/score/rebound — as a real, testable Unity 6 project.

**Architecture:** A Composition Root (`GameBootstrap`) wires independently-testable systems together by code, not Inspector drag-drop. Pure game logic (state machines, trajectory math, movement math) lives in plain C# classes with no `MonoBehaviour` dependency, so it is unit-testable in EditMode. `MonoBehaviour`s are thin adapters over that pure logic. Six Assembly Definitions enforce the dependency direction: `Core` (contracts only, zero references) ← `Gameplay`/`AI`/`Input`/`UI` (each references only `Core`, never each other) ← `Bootstrap` (references everything, wires it all up).

**Tech Stack:** Unity 6000.6.2f1, Universal Render Pipeline, new Input System (low-level polling API, no `.inputactions` asset needed for this slice), Unity Test Framework (NUnit) for EditMode/PlayMode tests.

**Spec:** [docs/superpowers/specs/2026-09-23-vertical-slice-design.md](../specs/2026-09-23-vertical-slice-design.md)

## Global Constraints

- Unity Editor version: exactly `6000.6.2f1` (see the editor-version note right after this section for why it isn't `6000.6.0f1`; do not let the project auto-upgrade further).
- Render pipeline: URP only. No HDRP, no Built-in RP assets.
- No third-party or downloaded assets of any kind — every visual in this slice is a Unity primitive (cube/capsule/sphere) or code-generated. No character models, no music, no animations from external sources.
- No DI framework (no VContainer/Zenject). Composition Root pattern only, per the approved spec.
- Assembly dependency direction is a hard rule, not a suggestion: `Basket.AI` and `Basket.UI` must never reference `Basket.Gameplay`. Only `Basket.Bootstrap` may reference all of `Core`/`Gameplay`/`AI`/`Input`/`UI`.
- Every cross-system reference (one component needing another) is wired by `GameBootstrap` calling a public `Configure(...)` method — never `FindObjectOfType`, never a static singleton.
- `[SerializeField]` Inspector fields are reserved for ScriptableObject config assets (author-time tuning data) — never for cross-system object references.
- Court size for this slice: half-court, matching 3v3 dimensions (per spec decision).
- No jump physics, no fouls, no shot clock, no out-of-bounds in this slice (explicitly deferred — do not add them even if it seems easy).

---

## Verification Commands (reused across tasks)

Define these once; every task's verification steps reference them by name. Run from a shell with the project at `D:\Projetos\basket\.worktrees\vertical-slice` (this plan executes inside the `vertical-slice-implementation` git worktree, never the main checkout).

> **Editor version note (superseded during Task 1):** Unity Hub silently installed `6000.6.2f1` to its default location instead of using the pre-installed `6000.6.0f1` at `D:\Unity\6000.6.0f1`. Ruling (see SDD ledger): accept `6000.6.2f1` — same `6000.6` minor line, patch releases don't break the public API this plan uses. All commands below target `C:\Program Files\Unity\Hub\Editor\6000.6.2f1\Editor\Unity.exe`.

**COMPILE_CHECK** — confirms the project has zero compile errors:
```bash
"/c/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe" -batchmode -quit -nographics -projectPath "D:\Projetos\basket\.worktrees\vertical-slice" -logFile "D:\Projetos\basket\.worktrees\vertical-slice\Logs\compile.log"
grep -i "error CS" "D:\Projetos\basket\.worktrees\vertical-slice\Logs\compile.log"
```
Expected: the `grep` prints nothing (no matches).

**EDITMODE_TESTS** — runs all EditMode tests:
```bash
"/c/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe" -batchmode -runTests -nographics -projectPath "D:\Projetos\basket\.worktrees\vertical-slice" -testPlatform EditMode -testResults "D:\Projetos\basket\.worktrees\vertical-slice\Logs\editmode-results.xml" -logFile "D:\Projetos\basket\.worktrees\vertical-slice\Logs\editmode.log"
grep -o 'result="[A-Za-z]*"' "D:\Projetos\basket\.worktrees\vertical-slice\Logs\editmode-results.xml" | sort | uniq -c
```
Expected: only `Passed` entries, matching the test count for that task; zero `Failed`.

> **No trailing `-quit`.** Confirmed during Task 3's review: `-runTests` combined with an explicit `-quit` races in this Unity 6000.6.2f1 setup and silently produces no results XML at all (Unity exits before the test run is written) — the implementer sees no file and has no way to get a real Pass/Fail verdict. `-runTests` already closes the Editor itself once the run completes; do not add `-quit` to either EDITMODE_TESTS or PLAYMODE_TESTS. (COMPILE_CHECK is unaffected — it doesn't use `-runTests`, and keeps its own `-quit`.) If a `-runTests` invocation ever hangs instead of exiting, that is a genuine new problem to report, not a reason to reach for `-quit` again.

**PLAYMODE_TESTS** — runs all PlayMode tests (same as above, no `-quit`, with `-testPlatform PlayMode` and `playmode-results.xml`/`playmode.log`).

---

### Task 1: Bootstrap Unity project (manual) + verify — COMPLETE (done by the human partner + controller, see ledger)

**Files:**
- Created (via Unity Hub, not by hand): `Assets/`, `Packages/manifest.json`, `Packages/packages-lock.json`, `ProjectSettings/*`
- Created: `.gitignore`

**Interfaces:** none (infrastructure task).

What actually happened (kept here for the record — later tasks don't need to redo any of this):

1. Unity Hub refused to create a project directly into `.worktrees\vertical-slice` because that folder already existed (it's the git worktree). Worked around by creating a throwaway project `vertical_slice_tmp` as a sibling folder, template **Universal 3D**, then moving its generated `Assets/`, `Packages/`, `ProjectSettings/` into `.worktrees\vertical-slice` and deleting the temp folder.
2. Input System package (`1.20.0`) came pre-included in the Universal 3D template — no manual install needed. Active Input Handling confirmed set to allow the new Input System.
3. `.gitignore` inside the worktree already had `.worktrees/` (added before the worktree was created); the Unity ignores below were appended to it, not written standalone. It also ignores the regenerated `.csproj`/`.sln`/`.slnx`/`.vs/` files Unity/the IDE integration produces on each open.

`.gitignore` (full contents as committed):
```gitignore
.worktrees/

/[Ll]ibrary/
/[Tt]emp/
/[Oo]bj/
/[Bb]uild/
/[Bb]uilds/
/[Ll]ogs/
/[Uu]ser[Ss]ettings/
/[Mm]emoryCaptures/

*.pidb.meta
*.pdb.meta
*.mdb.meta

sysinfo.txt
*.apk
*.aab
*.unitypackage
*.app

crashlytics-build.properties

*.csproj
*.sln
*.slnx
.vs/
```

Verify:
```bash
head -1 "D:\Projetos\basket\.worktrees\vertical-slice\ProjectSettings\ProjectVersion.txt"
grep "render-pipelines.universal" "D:\Projetos\basket\.worktrees\vertical-slice\Packages\manifest.json"
grep "inputsystem" "D:\Projetos\basket\.worktrees\vertical-slice\Packages\manifest.json"
```
Expected: first line is `m_EditorVersion: 6000.6.2f1`; both `grep` calls print a matching line.

Commit:
```bash
cd "/d/Projetos/basket/.worktrees/vertical-slice"
git add .gitignore Assets Packages ProjectSettings
git commit -m "chore: bootstrap Unity 6000.6.2f1 project with URP + Input System"
```

---

### Task 2: Folder structure + Assembly Definitions

**Files:**
- Create: `Assets/_Project/Scripts/Core/Basket.Core.asmdef`
- Create: `Assets/_Project/Scripts/Gameplay/Basket.Gameplay.asmdef`
- Create: `Assets/_Project/Scripts/AI/Basket.AI.asmdef`
- Create: `Assets/_Project/Scripts/Input/Basket.Input.asmdef`
- Create: `Assets/_Project/Scripts/UI/Basket.UI.asmdef`
- Create: `Assets/_Project/Scripts/Bootstrap/Basket.Bootstrap.asmdef`
- Create: `Assets/_Project/Tests/EditMode/Basket.Tests.EditMode.asmdef`
- Create: `Assets/_Project/Tests/PlayMode/Basket.Tests.PlayMode.asmdef`
- Create: `Assets/_Project/Scripts/Gameplay/AssemblyInfo.cs`
- Create: `Assets/_Project/Data/.gitkeep`, `Assets/_Project/Prefabs/.gitkeep`, `Assets/_Project/Art/.gitkeep`, `Assets/_Project/Scenes/.gitkeep`

**Interfaces:** none — this task only establishes assembly boundaries.

- [ ] **Step 1: Create `Basket.Core.asmdef`** (zero references — the dependency floor)

`Assets/_Project/Scripts/Core/Basket.Core.asmdef`:
```json
{
    "name": "Basket.Core",
    "rootNamespace": "Basket.Core",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: Create `Basket.Gameplay.asmdef`**

`Assets/_Project/Scripts/Gameplay/Basket.Gameplay.asmdef`:
```json
{
    "name": "Basket.Gameplay",
    "rootNamespace": "Basket.Gameplay",
    "references": ["Basket.Core"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 3: Create `Basket.AI.asmdef`** (references `Core` only — never `Gameplay`)

`Assets/_Project/Scripts/AI/Basket.AI.asmdef`:
```json
{
    "name": "Basket.AI",
    "rootNamespace": "Basket.AI",
    "references": ["Basket.Core"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 4: Create `Basket.Input.asmdef`**

`Assets/_Project/Scripts/Input/Basket.Input.asmdef`:
```json
{
    "name": "Basket.Input",
    "rootNamespace": "Basket.Input",
    "references": ["Basket.Core", "Unity.InputSystem"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 5: Create `Basket.UI.asmdef`** (references `Core` only — never `Gameplay`)

`Assets/_Project/Scripts/UI/Basket.UI.asmdef`:
```json
{
    "name": "Basket.UI",
    "rootNamespace": "Basket.UI",
    "references": ["Basket.Core"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 6: Create `Basket.Bootstrap.asmdef`** (the composition root — the only assembly allowed to see everything)

`Assets/_Project/Scripts/Bootstrap/Basket.Bootstrap.asmdef`:
```json
{
    "name": "Basket.Bootstrap",
    "rootNamespace": "Basket.Bootstrap",
    "references": ["Basket.Core", "Basket.Gameplay", "Basket.AI", "Basket.Input", "Basket.UI"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 7: Create the EditMode test assembly**

`Assets/_Project/Tests/EditMode/Basket.Tests.EditMode.asmdef`:
```json
{
    "name": "Basket.Tests.EditMode",
    "rootNamespace": "Basket.Tests.EditMode",
    "references": [
        "Basket.Core",
        "Basket.Gameplay",
        "Basket.AI",
        "Basket.Input",
        "Unity.InputSystem",
        "Unity.InputSystem.TestFramework",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": ["Editor"],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": ["nunit.framework.dll"],
    "autoReferenced": true,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 8: Create the PlayMode test assembly**

`Assets/_Project/Tests/PlayMode/Basket.Tests.PlayMode.asmdef`:
```json
{
    "name": "Basket.Tests.PlayMode",
    "rootNamespace": "Basket.Tests.PlayMode",
    "references": [
        "Basket.Core",
        "Basket.Gameplay",
        "Basket.AI",
        "Basket.Input",
        "Basket.Bootstrap",
        "Unity.InputSystem",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": ["nunit.framework.dll"],
    "autoReferenced": true,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 9: Allow the test assemblies to call `Basket.Gameplay`'s test-only `internal` hooks**

Several later tasks (5, 8, 10, 11, 14, 15, 16) add an `internal` method on a `Basket.Gameplay` `MonoBehaviour`/class purely so a `PlayMode`/`EditMode` test can inject a test double (e.g. `PlayerMotor.SetConfigForTest`). `internal` is visible only within the declaring assembly by default — `Basket.Tests.PlayMode`/`Basket.Tests.EditMode` are separate assemblies and cannot see it unless `Basket.Gameplay` explicitly grants them access via `InternalsVisibleTo`. Add that grant now so every later task compiles without revisiting this file.

`Assets/_Project/Scripts/Gameplay/AssemblyInfo.cs`:
```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Basket.Tests.EditMode")]
[assembly: InternalsVisibleTo("Basket.Tests.PlayMode")]
```

- [ ] **Step 10: Create empty placeholder folders**

Create empty files `Assets/_Project/Data/.gitkeep`, `Assets/_Project/Prefabs/.gitkeep`, `Assets/_Project/Art/.gitkeep`, `Assets/_Project/Scenes/.gitkeep` (Unity does not track empty folders; these keep them in git).

- [ ] **Step 11: Verify — run COMPILE_CHECK**

Expected: no `error CS` output. This also makes Unity import all 8 new `.asmdef` files (plus `AssemblyInfo.cs`) and generate their `.meta` companions.

- [ ] **Step 12: Commit**

```bash
cd "/d/Projetos/basket"
git add Assets/_Project
git commit -m "chore: establish assembly boundaries (Core/Gameplay/AI/Input/UI/Bootstrap) + test InternalsVisibleTo"
```

---

### Task 3: Core contracts (enums, structs, interfaces)

**Files:**
- Create: `Assets/_Project/Scripts/Core/BallState.cs`
- Create: `Assets/_Project/Scripts/Core/AIState.cs`
- Create: `Assets/_Project/Scripts/Core/MatchPhase.cs`
- Create: `Assets/_Project/Scripts/Core/AIPerception.cs`
- Create: `Assets/_Project/Scripts/Core/IPlayerAgent.cs`
- Create: `Assets/_Project/Scripts/Core/IAIController.cs`
- Create: `Assets/_Project/Scripts/Core/IMatchState.cs`
- Create: `Assets/_Project/Scripts/Core/IBallStateReadOnly.cs`
- Test: `Assets/_Project/Tests/EditMode/AIPerceptionTests.cs`

**Interfaces:**
- Produces: `BallState` enum (`Free, Held, Passing, Shooting`); `AIState` enum (`Idle, Guard, ContestShot, Chase`); `MatchPhase` enum (`WaitingForInbound, Live, Scored`); `AIPerception` struct; `IPlayerAgent`; `IAIController : IPlayerAgent`; `IMatchState`; `IBallStateReadOnly` — every later task in every assembly consumes these.

- [ ] **Step 1: Write the failing test**

`Assets/_Project/Tests/EditMode/AIPerceptionTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;
using Basket.Core;

public class AIPerceptionTests
{
    [Test]
    public void Constructor_StoresAllFieldsExactly()
    {
        var self = new Vector3(1f, 0f, 2f);
        var opponent = new Vector3(3f, 0f, 4f);
        var ball = new Vector3(5f, 0f, 6f);

        var perception = new AIPerception(self, opponent, ball, opponentHasBall: true, selfHasBall: false);

        Assert.AreEqual(self, perception.SelfPosition);
        Assert.AreEqual(opponent, perception.OpponentPosition);
        Assert.AreEqual(ball, perception.BallPosition);
        Assert.IsTrue(perception.OpponentHasBall);
        Assert.IsFalse(perception.SelfHasBall);
    }
}
```

- [ ] **Step 2: Run EDITMODE_TESTS to verify it fails**

Expected: compile error (`AIPerception` does not exist) — confirms the test is exercising code that doesn't exist yet.

- [ ] **Step 3: Write the minimal implementation**

`Assets/_Project/Scripts/Core/BallState.cs`:
```csharp
namespace Basket.Core
{
    public enum BallState { Free, Held, Passing, Shooting }
}
```

`Assets/_Project/Scripts/Core/AIState.cs`:
```csharp
namespace Basket.Core
{
    public enum AIState { Idle, Guard, ContestShot, Chase }
}
```

`Assets/_Project/Scripts/Core/MatchPhase.cs`:
```csharp
namespace Basket.Core
{
    public enum MatchPhase { WaitingForInbound, Live, Scored }
}
```

`Assets/_Project/Scripts/Core/AIPerception.cs`:
```csharp
using UnityEngine;

namespace Basket.Core
{
    public readonly struct AIPerception
    {
        public readonly Vector3 SelfPosition;
        public readonly Vector3 OpponentPosition;
        public readonly Vector3 BallPosition;
        public readonly bool OpponentHasBall;
        public readonly bool SelfHasBall;

        public AIPerception(Vector3 selfPosition, Vector3 opponentPosition, Vector3 ballPosition, bool opponentHasBall, bool selfHasBall)
        {
            SelfPosition = selfPosition;
            OpponentPosition = opponentPosition;
            BallPosition = ballPosition;
            OpponentHasBall = opponentHasBall;
            SelfHasBall = selfHasBall;
        }
    }
}
```

`Assets/_Project/Scripts/Core/IPlayerAgent.cs`:
```csharp
using UnityEngine;

namespace Basket.Core
{
    public interface IPlayerAgent
    {
        Vector2 GetMoveInput();
        bool WantsSprint();
        bool WantsDribbleAction();
        bool WantsPass();
        bool WantsShoot();
    }
}
```

`Assets/_Project/Scripts/Core/IAIController.cs`:
```csharp
namespace Basket.Core
{
    public interface IAIController : IPlayerAgent
    {
        AIState CurrentState { get; }
        void Tick(AIPerception perception);
    }
}
```

`Assets/_Project/Scripts/Core/IMatchState.cs`:
```csharp
namespace Basket.Core
{
    public interface IMatchState
    {
        int ScoreHome { get; }
        int ScoreAway { get; }
        MatchPhase Phase { get; }
    }
}
```

`Assets/_Project/Scripts/Core/IBallStateReadOnly.cs`:
```csharp
using UnityEngine;

namespace Basket.Core
{
    public interface IBallStateReadOnly
    {
        BallState CurrentState { get; }
        Vector3 Position { get; }
    }
}
```

- [ ] **Step 4: Run EDITMODE_TESTS to verify it passes**

Expected: 1 test, `Passed`.

- [ ] **Step 5: Commit**

```bash
cd "/d/Projetos/basket"
git add Assets/_Project/Scripts/Core Assets/_Project/Tests/EditMode/AIPerceptionTests.cs
git commit -m "feat(core): add shared contracts (BallState, AIState, MatchPhase, AIPerception, IPlayerAgent, IAIController, IMatchState, IBallStateReadOnly)"
```

---

### Task 4: `PlayerMovementConfig` + `PlayerMotorMath`

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/PlayerMovementConfig.cs`
- Create: `Assets/_Project/Scripts/Gameplay/PlayerMotorMath.cs`
- Test: `Assets/_Project/Tests/EditMode/PlayerMotorMathTests.cs`

**Interfaces:**
- Consumes: none.
- Produces: `PlayerMovementConfig : ScriptableObject { maxSpeed, sprintMultiplier, acceleration, deceleration, turnSpeedDegrees }`; `PlayerMotorMath.ComputeVelocity(Vector3 currentVelocity, Vector3 desiredDirection, float maxSpeed, float acceleration, float deceleration, float dt) : Vector3` — consumed by Task 5's `PlayerMotor`.

- [ ] **Step 1: Write the failing test**

`Assets/_Project/Tests/EditMode/PlayerMotorMathTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;
using Basket.Gameplay;

public class PlayerMotorMathTests
{
    [Test]
    public void ComputeVelocity_FromStandstill_AcceleratesTowardDesiredDirection()
    {
        Vector3 result = PlayerMotorMath.ComputeVelocity(
            currentVelocity: Vector3.zero,
            desiredDirection: Vector3.forward,
            maxSpeed: 6f,
            acceleration: 30f,
            deceleration: 40f,
            dt: 0.1f);

        Assert.Greater(result.z, 0f);
        Assert.LessOrEqual(result.magnitude, 6f);
    }

    [Test]
    public void ComputeVelocity_NoInput_DeceleratesTowardZero()
    {
        Vector3 result = PlayerMotorMath.ComputeVelocity(
            currentVelocity: new Vector3(0f, 0f, 6f),
            desiredDirection: Vector3.zero,
            maxSpeed: 6f,
            acceleration: 30f,
            deceleration: 40f,
            dt: 0.1f);

        Assert.Less(result.z, 6f);
        Assert.GreaterOrEqual(result.z, 0f);
    }

    [Test]
    public void ComputeVelocity_NeverExceedsMaxSpeed()
    {
        Vector3 result = PlayerMotorMath.ComputeVelocity(
            currentVelocity: new Vector3(0f, 0f, 5.9f),
            desiredDirection: Vector3.forward,
            maxSpeed: 6f,
            acceleration: 30f,
            deceleration: 40f,
            dt: 1f);

        Assert.LessOrEqual(result.magnitude, 6f + 0.0001f);
    }
}
```

- [ ] **Step 2: Run EDITMODE_TESTS to verify it fails**

Expected: compile error (`PlayerMotorMath` does not exist).

- [ ] **Step 3: Write the minimal implementation**

`Assets/_Project/Scripts/Gameplay/PlayerMovementConfig.cs`:
```csharp
using UnityEngine;

namespace Basket.Gameplay
{
    [CreateAssetMenu(fileName = "PlayerMovementConfig", menuName = "Basket/Player Movement Config")]
    public class PlayerMovementConfig : ScriptableObject
    {
        public float maxSpeed = 6f;
        public float sprintMultiplier = 1.6f;
        public float acceleration = 30f;
        public float deceleration = 40f;
        public float turnSpeedDegrees = 720f;
    }
}
```

`Assets/_Project/Scripts/Gameplay/PlayerMotorMath.cs`:
```csharp
using UnityEngine;

namespace Basket.Gameplay
{
    public static class PlayerMotorMath
    {
        public static Vector3 ComputeVelocity(Vector3 currentVelocity, Vector3 desiredDirection, float maxSpeed, float acceleration, float deceleration, float dt)
        {
            Vector3 desiredVelocity = desiredDirection.sqrMagnitude > 0.0001f
                ? desiredDirection.normalized * maxSpeed
                : Vector3.zero;
            float rate = desiredDirection.sqrMagnitude > 0.0001f ? acceleration : deceleration;
            return Vector3.MoveTowards(currentVelocity, desiredVelocity, rate * dt);
        }
    }
}
```

- [ ] **Step 4: Run EDITMODE_TESTS to verify it passes**

Expected: 3 tests (plus the 1 from Task 3 = 4 total), all `Passed`.

- [ ] **Step 5: Commit**

```bash
cd "/d/Projetos/basket"
git add Assets/_Project/Scripts/Gameplay/PlayerMovementConfig.cs Assets/_Project/Scripts/Gameplay/PlayerMotorMath.cs Assets/_Project/Tests/EditMode/PlayerMotorMathTests.cs
git commit -m "feat(gameplay): add PlayerMovementConfig and PlayerMotorMath"
```

---

### Task 5: `PlayerMotor` MonoBehaviour

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/PlayerMotor.cs`
- Test: `Assets/_Project/Tests/PlayMode/PlayerMotorTests.cs`

**Interfaces:**
- Consumes: `PlayerMotorMath.ComputeVelocity(...)` (Task 4); `PlayerMovementConfig` (Task 4).
- Produces: `PlayerMotor : MonoBehaviour { Vector3 Velocity { get; } void Tick(Vector2 moveInput, bool sprint, float dt) }` — consumed by `GameBootstrap` (Task 18).

- [ ] **Step 1: Write the failing test**

`Assets/_Project/Tests/PlayMode/PlayerMotorTests.cs`:
```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Basket.Gameplay;

public class PlayerMotorTests
{
    [UnityTest]
    public IEnumerator Tick_WithForwardInput_MovesPlayerForwardOverTime()
    {
        var go = new GameObject("TestPlayer");
        go.AddComponent<CharacterController>();
        var motor = go.AddComponent<PlayerMotor>();

        var config = ScriptableObject.CreateInstance<PlayerMovementConfig>();
        motor.SetConfigForTest(config);

        Vector3 startPos = go.transform.position;

        for (int i = 0; i < 30; i++)
        {
            motor.Tick(new Vector2(0f, 1f), sprint: false, dt: 0.02f);
            yield return null;
        }

        Assert.Greater(Vector3.Distance(go.transform.position, startPos), 0.01f);
        Object.Destroy(go);
    }
}
```

- [ ] **Step 2: Run PLAYMODE_TESTS to verify it fails**

Expected: compile error (`PlayerMotor` and `SetConfigForTest` do not exist).

- [ ] **Step 3: Write the minimal implementation**

`Assets/_Project/Scripts/Gameplay/PlayerMotor.cs`:
```csharp
using UnityEngine;

namespace Basket.Gameplay
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        [SerializeField] private PlayerMovementConfig config;

        private CharacterController controller;
        private Vector3 velocity;

        public Vector3 Velocity => velocity;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        public void Tick(Vector2 moveInput, bool sprint, float dt)
        {
            Vector3 desiredDir = new Vector3(moveInput.x, 0f, moveInput.y);
            float speed = config.maxSpeed * (sprint ? config.sprintMultiplier : 1f);
            velocity = PlayerMotorMath.ComputeVelocity(velocity, desiredDir, speed, config.acceleration, config.deceleration, dt);

            if (velocity.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(velocity.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, config.turnSpeedDegrees * dt);
            }

            // CharacterController.SimpleMove(Vector3) silently re-multiplies its argument
            // by Unity's real Time.deltaTime internally, discarding the dt this method was
            // given and making movement depend on real frame timing instead of the caller's
            // explicit dt. Use Move() with a pre-scaled motion vector instead, which moves by
            // exactly the vector given, so a test driving Tick() with a fixed synthetic dt
            // gets deterministic, real-timing-independent displacement. isGrounded needs a
            // continuous small downward push to read true on flat ground (no floor exists
            // in Task 5's unit test, so it free-falls slowly there instead — that's fine,
            // only horizontal displacement is asserted). The stick push is dt-scaled so its
            // cumulative effect doesn't depend on frame rate. The airborne branch below is a
            // deliberately simplified displacement-only fall (not real velocity-integrated
            // gravity) — this slice has no jump/fall gameplay (see Global Constraints), so
            // the player is always grounded in practice and this branch is effectively dead
            // code; do not spend design effort on it here. If a future task actually needs
            // real airborne physics, replace this with a persisted vertical-velocity field
            // integrated each frame, not a bigger patch to this line.
            Vector3 motion = velocity * dt;
            motion.y = controller.isGrounded ? -0.05f * dt : Physics.gravity.y * dt;
            controller.Move(motion);
        }

        internal void SetConfigForTest(PlayerMovementConfig testConfig)
        {
            config = testConfig;
        }
    }
}
```

- [ ] **Step 4: Run PLAYMODE_TESTS to verify it passes**

Expected: 1 test, `Passed`.

- [ ] **Step 5: Commit**

```bash
cd "/d/Projetos/basket"
git add Assets/_Project/Scripts/Gameplay/PlayerMotor.cs Assets/_Project/Tests/PlayMode/PlayerMotorTests.cs
git commit -m "feat(gameplay): add PlayerMotor (CharacterController-based movement)"
```

---

### Task 6: `HumanInputProvider`

**Files:**
- Create: `Assets/_Project/Scripts/Input/HumanInputProvider.cs`
- Test: `Assets/_Project/Tests/EditMode/HumanInputProviderTests.cs`

**Interfaces:**
- Consumes: `IPlayerAgent` (Task 3).
- Produces: `HumanInputProvider : MonoBehaviour, IPlayerAgent` — consumed by `GameBootstrap` (Task 18).

- [ ] **Step 1: Write the failing test**

`Assets/_Project/Tests/EditMode/HumanInputProviderTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Basket.Input;

public class HumanInputProviderTests : InputTestFixture
{
    private Keyboard keyboard;
    private HumanInputProvider provider;
    private GameObject go;

    public override void Setup()
    {
        base.Setup();
        keyboard = InputSystem.AddDevice<Keyboard>();
        go = new GameObject("TestInput");
        provider = go.AddComponent<HumanInputProvider>();
    }

    public override void TearDown()
    {
        // EditMode tests run outside Play Mode, where Object.Destroy() is invalid
        // (Unity requires DestroyImmediate() for edit-time object cleanup).
        Object.DestroyImmediate(go);
        base.TearDown();
    }

    [Test]
    public void GetMoveInput_WPressed_ReturnsForward()
    {
        Press(keyboard.wKey);
        Vector2 input = provider.GetMoveInput();
        Assert.Greater(input.y, 0f);
    }

    [Test]
    public void WantsShoot_SpacePressedThisFrame_ReturnsTrue()
    {
        Press(keyboard.spaceKey);
        Assert.IsTrue(provider.WantsShoot());
    }
}
```

- [ ] **Step 2: Run EDITMODE_TESTS to verify it fails**

Expected: compile error (`HumanInputProvider` does not exist).

- [ ] **Step 3: Write the minimal implementation**

`Assets/_Project/Scripts/Input/HumanInputProvider.cs`:
```csharp
using UnityEngine;
using UnityEngine.InputSystem;
using Basket.Core;

namespace Basket.Input
{
    public class HumanInputProvider : MonoBehaviour, IPlayerAgent
    {
        public Vector2 GetMoveInput()
        {
            Vector2 kb = Vector2.zero;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) kb.y += 1f;
                if (Keyboard.current.sKey.isPressed) kb.y -= 1f;
                if (Keyboard.current.dKey.isPressed) kb.x += 1f;
                if (Keyboard.current.aKey.isPressed) kb.x -= 1f;
            }
            Vector2 pad = Gamepad.current != null ? Gamepad.current.leftStick.ReadValue() : Vector2.zero;
            Vector2 combined = kb.sqrMagnitude >= pad.sqrMagnitude ? kb : pad;
            return Vector2.ClampMagnitude(combined, 1f);
        }

        public bool WantsSprint() =>
            (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed) ||
            (Gamepad.current != null && Gamepad.current.leftStickButton.isPressed);

        public bool WantsDribbleAction() => false;

        public bool WantsPass() =>
            (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame);

        public bool WantsShoot() =>
            (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
    }
}
```

- [ ] **Step 4: Run EDITMODE_TESTS to verify it passes**

Expected: 2 tests, `Passed`.

- [ ] **Step 5: Commit**

```bash
cd "/d/Projetos/basket"
git add Assets/_Project/Scripts/Input Assets/_Project/Tests/EditMode/HumanInputProviderTests.cs
git commit -m "feat(input): add HumanInputProvider (keyboard + gamepad, low-level polling)"
```

---

### Task 7: `BallConfig` / `ShotConfig` + `BallPossessionStateMachine`

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/BallConfig.cs`
- Create: `Assets/_Project/Scripts/Gameplay/ShotConfig.cs`
- Create: `Assets/_Project/Scripts/Gameplay/BallPossessionStateMachine.cs`
- Test: `Assets/_Project/Tests/EditMode/BallPossessionStateMachineTests.cs`

**Interfaces:**
- Consumes: `BallState` (Task 3).
- Produces: `BallConfig`, `ShotConfig` (ScriptableObjects); `BallPossessionStateMachine { BallState CurrentState; event Action<BallState,BallState> OnStateChanged; bool TryTransition(BallState next); }` — consumed by `BallController` (Task 8).

- [ ] **Step 1: Write the failing test**

`Assets/_Project/Tests/EditMode/BallPossessionStateMachineTests.cs`:
```csharp
using NUnit.Framework;
using Basket.Core;
using Basket.Gameplay;

public class BallPossessionStateMachineTests
{
    [Test]
    public void InitialState_IsFree()
    {
        var sm = new BallPossessionStateMachine();
        Assert.AreEqual(BallState.Free, sm.CurrentState);
    }

    [Test]
    public void TryTransition_FreeToHeld_Succeeds()
    {
        var sm = new BallPossessionStateMachine();
        bool ok = sm.TryTransition(BallState.Held);
        Assert.IsTrue(ok);
        Assert.AreEqual(BallState.Held, sm.CurrentState);
    }

    [Test]
    public void TryTransition_FreeToFree_Fails()
    {
        var sm = new BallPossessionStateMachine();
        bool ok = sm.TryTransition(BallState.Free);
        Assert.IsFalse(ok);
    }

    [Test]
    public void TryTransition_HeldToShooting_Succeeds()
    {
        var sm = new BallPossessionStateMachine();
        sm.TryTransition(BallState.Held);
        bool ok = sm.TryTransition(BallState.Shooting);
        Assert.IsTrue(ok);
        Assert.AreEqual(BallState.Shooting, sm.CurrentState);
    }

    [Test]
    public void TryTransition_ValidChange_FiresOnStateChanged()
    {
        var sm = new BallPossessionStateMachine();
        BallState? observedFrom = null;
        BallState? observedTo = null;
        sm.OnStateChanged += (from, to) => { observedFrom = from; observedTo = to; };

        sm.TryTransition(BallState.Held);

        Assert.AreEqual(BallState.Free, observedFrom);
        Assert.AreEqual(BallState.Held, observedTo);
    }
}
```

- [ ] **Step 2: Run EDITMODE_TESTS to verify it fails**

Expected: compile error (`BallPossessionStateMachine` does not exist).

- [ ] **Step 3: Write the minimal implementation**

`Assets/_Project/Scripts/Gameplay/BallConfig.cs`:
```csharp
using UnityEngine;

namespace Basket.Gameplay
{
    [CreateAssetMenu(fileName = "BallConfig", menuName = "Basket/Ball Config")]
    public class BallConfig : ScriptableObject
    {
        public float mass = 0.62f;
        public float drag = 0.05f;
        public float angularDrag = 0.3f;
        public float handHeightOffset = 1.1f;
    }
}
```

`Assets/_Project/Scripts/Gameplay/ShotConfig.cs`:
```csharp
using UnityEngine;

namespace Basket.Gameplay
{
    [CreateAssetMenu(fileName = "ShotConfig", menuName = "Basket/Shot Config")]
    public class ShotConfig : ScriptableObject
    {
        public float arcHeight = 3.5f;
        public float baseAccuracyRadius = 0.35f;
        public float defaultShooterRating = 0.75f;
    }
}
```

`Assets/_Project/Scripts/Gameplay/BallPossessionStateMachine.cs`:
```csharp
using System;
using Basket.Core;

namespace Basket.Gameplay
{
    public sealed class BallPossessionStateMachine
    {
        public BallState CurrentState { get; private set; } = BallState.Free;
        public event Action<BallState, BallState> OnStateChanged;

        public bool TryTransition(BallState next)
        {
            if (!IsValidTransition(CurrentState, next)) return false;
            var prev = CurrentState;
            CurrentState = next;
            OnStateChanged?.Invoke(prev, next);
            return true;
        }

        private static bool IsValidTransition(BallState from, BallState to)
        {
            return (from, to) switch
            {
                (BallState.Free, BallState.Held) => true,
                (BallState.Held, BallState.Passing) => true,
                (BallState.Held, BallState.Shooting) => true,
                (BallState.Passing, BallState.Free) => true,
                (BallState.Shooting, BallState.Free) => true,
                _ => false
            };
        }
    }
}
```

- [ ] **Step 4: Run EDITMODE_TESTS to verify it passes**

Expected: 5 tests, `Passed`.

- [ ] **Step 5: Commit**

```bash
cd "/d/Projetos/basket"
git add Assets/_Project/Scripts/Gameplay/BallConfig.cs Assets/_Project/Scripts/Gameplay/ShotConfig.cs Assets/_Project/Scripts/Gameplay/BallPossessionStateMachine.cs Assets/_Project/Tests/EditMode/BallPossessionStateMachineTests.cs
git commit -m "feat(gameplay): add BallConfig, ShotConfig, BallPossessionStateMachine"
```

---

### Task 8: `BallController` MonoBehaviour

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/PlayerMarker.cs`
- Create: `Assets/_Project/Scripts/Gameplay/BallController.cs`
- Test: `Assets/_Project/Tests/PlayMode/BallControllerTests.cs`

**Interfaces:**
- Consumes: `BallPossessionStateMachine`, `BallConfig` (Task 7); `IBallStateReadOnly` (Task 3).
- Produces: `PlayerMarker : MonoBehaviour`; `BallController : MonoBehaviour, IBallStateReadOnly { BallState CurrentState; Vector3 Position; Transform CurrentHolder; event Action<BallState,BallState> OnStateChanged; event Action OnScored; void Catch(Transform holder); void Release(BallState releaseState, Vector3 velocity); void SetHeldLocalOffset(Vector3 offset); void NotifyScored(); }` — consumed by Tasks 10, 11, 14, 15, 18.

- [ ] **Step 1: Write the failing test**

`Assets/_Project/Tests/PlayMode/BallControllerTests.cs`:
```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Basket.Core;
using Basket.Gameplay;

public class BallControllerTests
{
    private GameObject ballGo;
    private BallController ball;
    private GameObject holderGo;

    [SetUp]
    public void SetUp()
    {
        ballGo = new GameObject("TestBall");
        ballGo.AddComponent<Rigidbody>();
        ball = ballGo.AddComponent<BallController>();
        ball.SetConfigForTest(ScriptableObject.CreateInstance<BallConfig>());

        holderGo = new GameObject("TestHolder");
    }

    [TearDown]
    public void TearDown()
    {
        Object.Destroy(ballGo);
        Object.Destroy(holderGo);
    }

    [UnityTest]
    public IEnumerator Catch_FromFree_TransitionsToHeldAndFollowsHolder()
    {
        yield return null;
        ball.Catch(holderGo.transform);
        Assert.AreEqual(BallState.Held, ball.CurrentState);
        Assert.AreEqual(holderGo.transform, ball.CurrentHolder);
    }

    [UnityTest]
    public IEnumerator Release_FromHeld_TransitionsThroughToFreeAndAppliesVelocity()
    {
        yield return null;
        ball.Catch(holderGo.transform);
        ball.Release(BallState.Shooting, new Vector3(0f, 5f, 3f));

        Assert.AreEqual(BallState.Free, ball.CurrentState);
        Assert.AreEqual(new Vector3(0f, 5f, 3f), ballGo.GetComponent<Rigidbody>().linearVelocity);
    }

    [UnityTest]
    public IEnumerator NotifyScored_FiresOnScoredEvent()
    {
        yield return null;
        bool fired = false;
        ball.OnScored += _ => fired = true;
        ball.Catch(holderGo.transform);
        ball.Release(BallState.Shooting, Vector3.up);

        ball.NotifyScored();

        Assert.IsTrue(fired);
    }
}
```

- [ ] **Step 2: Run PLAYMODE_TESTS to verify it fails**

Expected: compile error (`BallController` does not exist).

- [ ] **Step 3: Write the minimal implementation**

`Assets/_Project/Scripts/Gameplay/PlayerMarker.cs`:
```csharp
namespace Basket.Gameplay
{
    public class PlayerMarker : UnityEngine.MonoBehaviour { }
}
```

`Assets/_Project/Scripts/Gameplay/BallController.cs`:
```csharp
using System;
using UnityEngine;
using Basket.Core;

namespace Basket.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    public class BallController : MonoBehaviour, IBallStateReadOnly
    {
        [SerializeField] private BallConfig config;

        // A freshly-released ball starts touching the releasing player's own collider
        // (the hand socket sits inside their capsule). Without this grace window,
        // OnCollisionEnter would immediately re-catch the ball to the same player,
        // silently nullifying every pass and shot.
        private const float SelfCatchGraceSeconds = 0.25f;

        private Rigidbody rb;
        private readonly BallPossessionStateMachine stateMachine = new();
        private Vector3 heldLocalOffset;
        private Transform lastReleasedBy;
        private float lastReleaseTime = float.NegativeInfinity;

        public BallState CurrentState => stateMachine.CurrentState;
        public Vector3 Position => transform.position;
        public Transform CurrentHolder { get; private set; }

        public event Action<BallState, BallState> OnStateChanged
        {
            add => stateMachine.OnStateChanged += value;
            remove => stateMachine.OnStateChanged -= value;
        }
        public event Action<Transform> OnScored;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            // config may not be assigned yet when Awake fires synchronously from
            // AddComponent (e.g. in tests that call SetConfigForTest afterwards);
            // ApplyConfigToRigidbody re-applies once a config is actually set.
            if (config != null) ApplyConfigToRigidbody();
        }

        private void ApplyConfigToRigidbody()
        {
            rb.mass = config.mass;
            rb.linearDamping = config.drag;
            rb.angularDamping = config.angularDrag;
        }

        public void Catch(Transform holder)
        {
            if (!stateMachine.TryTransition(BallState.Held)) return;
            CurrentHolder = holder;
            rb.isKinematic = true;
            heldLocalOffset = Vector3.zero;
        }

        public void Release(BallState releaseState, Vector3 velocity)
        {
            if (stateMachine.CurrentState != BallState.Held) return;
            stateMachine.TryTransition(releaseState);
            lastReleasedBy = CurrentHolder;
            lastReleaseTime = Time.time;
            CurrentHolder = null;
            rb.isKinematic = false;
            rb.linearVelocity = velocity;
            stateMachine.TryTransition(BallState.Free);
        }

        public void SetHeldLocalOffset(Vector3 offset)
        {
            heldLocalOffset = offset;
        }

        public void NotifyScored()
        {
            OnScored?.Invoke(CurrentHolder);
        }

        private void LateUpdate()
        {
            if (stateMachine.CurrentState == BallState.Held && CurrentHolder != null)
            {
                transform.position = CurrentHolder.position + Vector3.up * config.handHeightOffset + heldLocalOffset;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (stateMachine.CurrentState != BallState.Free) return;
            if (collision.transform == lastReleasedBy && Time.time - lastReleaseTime < SelfCatchGraceSeconds) return;
            if (collision.transform.TryGetComponent<PlayerMarker>(out _))
            {
                Catch(collision.transform);
            }
        }

        internal void SetConfigForTest(BallConfig testConfig)
        {
            config = testConfig;
            if (rb != null) ApplyConfigToRigidbody();
        }
    }
}
```

- [ ] **Step 4: Run PLAYMODE_TESTS to verify it passes**

Expected: 3 tests, `Passed`.

- [ ] **Step 5: Commit**

```bash
cd "/d/Projetos/basket"
git add Assets/_Project/Scripts/Gameplay/PlayerMarker.cs Assets/_Project/Scripts/Gameplay/BallController.cs Assets/_Project/Tests/PlayMode/BallControllerTests.cs
git commit -m "feat(gameplay): add BallController (Free/Held/Passing/Shooting state machine + Rigidbody wiring)"
```

---

### Task 9: `TrajectoryMath` + `ShotMath`

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/TrajectoryMath.cs`
- Create: `Assets/_Project/Scripts/Gameplay/ShotMath.cs`
- Test: `Assets/_Project/Tests/EditMode/TrajectoryMathTests.cs`
- Test: `Assets/_Project/Tests/EditMode/ShotMathTests.cs`

**Interfaces:**
- Consumes: none.
- Produces: `TrajectoryMath.ComputeArcVelocity(Vector3 origin, Vector3 target, float apexHeight, float gravity) : Vector3`; `ShotMath.ComputeMissOffset(float baseRadius, float rating, System.Random rng) : Vector3` — consumed by `PassSystem`/`ShootingSystem` (Task 10).

- [ ] **Step 1: Write the failing tests**

`Assets/_Project/Tests/EditMode/TrajectoryMathTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;
using Basket.Gameplay;

public class TrajectoryMathTests
{
    [Test]
    public void ComputeArcVelocity_LevelTargets_LandsAtTargetXZ()
    {
        Vector3 origin = Vector3.zero;
        Vector3 target = new Vector3(5f, 0f, 0f);
        float gravity = -9.81f;

        Vector3 velocity = TrajectoryMath.ComputeArcVelocity(origin, target, apexHeight: 2f, gravity: gravity);

        // Simulate the resulting parabola and confirm it lands near the target.
        Vector3 pos = origin;
        Vector3 vel = velocity;
        float dt = 0.001f;
        float landingX = 0f;
        for (float t = 0f; t < 5f; t += dt)
        {
            vel += Vector3.up * gravity * dt;
            pos += vel * dt;
            if (pos.y <= 0f && t > 0.01f)
            {
                landingX = pos.x;
                break;
            }
        }

        Assert.AreEqual(target.x, landingX, 0.1f);
    }

    [Test]
    public void ComputeArcVelocity_HasPositiveUpwardComponent()
    {
        Vector3 velocity = TrajectoryMath.ComputeArcVelocity(Vector3.zero, new Vector3(3f, 0f, 0f), apexHeight: 1.5f, gravity: -9.81f);
        Assert.Greater(velocity.y, 0f);
    }
}
```

`Assets/_Project/Tests/EditMode/ShotMathTests.cs`:
```csharp
using System;
using NUnit.Framework;
using UnityEngine;
using Basket.Gameplay;

public class ShotMathTests
{
    [Test]
    public void ComputeMissOffset_PerfectRating_ReturnsZero()
    {
        var rng = new Random(42);
        Vector3 offset = ShotMath.ComputeMissOffset(baseRadius: 0.5f, rating: 1f, rng: rng);
        Assert.AreEqual(Vector3.zero, offset);
    }

    [Test]
    public void ComputeMissOffset_ZeroRating_StaysWithinBaseRadius()
    {
        var rng = new Random(42);
        Vector3 offset = ShotMath.ComputeMissOffset(baseRadius: 0.5f, rating: 0f, rng: rng);
        Assert.LessOrEqual(new Vector3(offset.x, 0f, offset.z).magnitude, 0.5f + 0.0001f);
    }
}
```

- [ ] **Step 2: Run EDITMODE_TESTS to verify it fails**

Expected: compile error (`TrajectoryMath`, `ShotMath` do not exist).

- [ ] **Step 3: Write the minimal implementation**

`Assets/_Project/Scripts/Gameplay/TrajectoryMath.cs`:
```csharp
using UnityEngine;

namespace Basket.Gameplay
{
    public static class TrajectoryMath
    {
        public static Vector3 ComputeArcVelocity(Vector3 origin, Vector3 target, float apexHeight, float gravity)
        {
            float g = Mathf.Abs(gravity);
            Vector3 displacement = target - origin;
            Vector3 displacementXZ = new Vector3(displacement.x, 0f, displacement.z);
            float peakHeight = Mathf.Max(apexHeight, 0.01f);

            // Apex sits peakHeight above whichever of origin/target is higher, so the
            // trajectory is guaranteed to still be rising until it clears both endpoints.
            // (An earlier version measured peakHeight from origin only, which silently
            // produced a below-target apex — and a physically broken landing — whenever
            // the target sat more than apexHeight above the origin, e.g. a shot arc to a
            // rim well above the shooter's release height.)
            float apexAboveOrigin = peakHeight + Mathf.Max(0f, displacement.y);
            float apexAboveTarget = apexAboveOrigin - displacement.y;

            float timeUp = Mathf.Sqrt(2f * apexAboveOrigin / g);
            float timeDown = Mathf.Sqrt(2f * apexAboveTarget / g);
            float totalTime = timeUp + timeDown;

            Vector3 velocityXZ = displacementXZ / totalTime;
            float velocityY = g * timeUp;

            return velocityXZ + Vector3.up * velocityY;
        }
    }
}
```

`Assets/_Project/Scripts/Gameplay/ShotMath.cs`:
```csharp
using System;
using UnityEngine;

namespace Basket.Gameplay
{
    public static class ShotMath
    {
        public static Vector3 ComputeMissOffset(float baseRadius, float rating, Random rng)
        {
            float effectiveRadius = baseRadius * (1f - Mathf.Clamp01(rating));
            if (effectiveRadius <= 0f) return Vector3.zero;

            double angle = rng.NextDouble() * Mathf.PI * 2;
            double dist = rng.NextDouble() * effectiveRadius;
            return new Vector3((float)(Math.Cos(angle) * dist), 0f, (float)(Math.Sin(angle) * dist));
        }
    }
}
```

- [ ] **Step 4: Run EDITMODE_TESTS to verify it passes**

Expected: 4 new tests, `Passed`.

- [ ] **Step 5: Commit**

```bash
cd "/d/Projetos/basket"
git add Assets/_Project/Scripts/Gameplay/TrajectoryMath.cs Assets/_Project/Scripts/Gameplay/ShotMath.cs Assets/_Project/Tests/EditMode/TrajectoryMathTests.cs Assets/_Project/Tests/EditMode/ShotMathTests.cs
git commit -m "feat(gameplay): add TrajectoryMath and ShotMath"
```

---

### Task 10: `PassSystem` + `ShootingSystem`

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/PassSystem.cs`
- Create: `Assets/_Project/Scripts/Gameplay/ShootingSystem.cs`
- Test: `Assets/_Project/Tests/PlayMode/PassAndShootSystemTests.cs`

**Interfaces:**
- Consumes: `BallController` (Task 8); `TrajectoryMath`, `ShotMath`, `BallConfig`, `ShotConfig` (Tasks 7, 9).
- Produces: `PassSystem : MonoBehaviour { void Configure(BallController ball, BallConfig config); bool TryPass(Transform passer, Transform target); }`; `ShootingSystem : MonoBehaviour { void Configure(BallController ball, ShotConfig config, Transform rimTarget); bool TryShoot(Transform shooter); }` — consumed by `GameBootstrap` (Task 18).

- [ ] **Step 1: Write the failing test**

`Assets/_Project/Tests/PlayMode/PassAndShootSystemTests.cs`:
```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Basket.Core;
using Basket.Gameplay;

public class PassAndShootSystemTests
{
    private GameObject ballGo, shooterGo, targetGo;
    private BallController ball;

    [SetUp]
    public void SetUp()
    {
        ballGo = new GameObject("TestBall");
        ballGo.AddComponent<Rigidbody>();
        ball = ballGo.AddComponent<BallController>();
        ball.SetConfigForTest(ScriptableObject.CreateInstance<BallConfig>());

        shooterGo = new GameObject("Shooter");
        targetGo = new GameObject("Target");
        targetGo.transform.position = new Vector3(4f, 0f, 0f);
    }

    [TearDown]
    public void TearDown()
    {
        Object.Destroy(ballGo);
        Object.Destroy(shooterGo);
        Object.Destroy(targetGo);
    }

    [UnityTest]
    public IEnumerator TryPass_WhileHolding_ReleasesBallTowardTarget()
    {
        yield return null;
        ball.Catch(shooterGo.transform);

        var passSystemGo = new GameObject("PassSystem");
        var passSystem = passSystemGo.AddComponent<PassSystem>();
        passSystem.Configure(ball, ScriptableObject.CreateInstance<BallConfig>());

        bool result = passSystem.TryPass(shooterGo.transform, targetGo.transform);

        Assert.IsTrue(result);
        Assert.AreEqual(BallState.Free, ball.CurrentState);
        Assert.Greater(ballGo.GetComponent<Rigidbody>().linearVelocity.magnitude, 0f);
        Object.Destroy(passSystemGo);
    }

    [UnityTest]
    public IEnumerator TryShoot_WhileHolding_ReleasesBallTowardRim()
    {
        yield return null;
        ball.Catch(shooterGo.transform);

        var shootGo = new GameObject("ShootingSystem");
        var shootSystem = shootGo.AddComponent<ShootingSystem>();
        shootSystem.Configure(ball, ScriptableObject.CreateInstance<ShotConfig>(), targetGo.transform);

        bool result = shootSystem.TryShoot(shooterGo.transform);

        Assert.IsTrue(result);
        Assert.AreEqual(BallState.Free, ball.CurrentState);
        Object.Destroy(shootGo);
    }

    [UnityTest]
    public IEnumerator TryPass_WhileNotHolding_ReturnsFalse()
    {
        yield return null;
        var passSystemGo = new GameObject("PassSystem");
        var passSystem = passSystemGo.AddComponent<PassSystem>();
        passSystem.Configure(ball, ScriptableObject.CreateInstance<BallConfig>());

        bool result = passSystem.TryPass(shooterGo.transform, targetGo.transform);

        Assert.IsFalse(result);
        Object.Destroy(passSystemGo);
    }
}
```

- [ ] **Step 2: Run PLAYMODE_TESTS to verify it fails**

Expected: compile error (`PassSystem`, `ShootingSystem` do not exist).

- [ ] **Step 3: Write the minimal implementation**

`Assets/_Project/Scripts/Gameplay/PassSystem.cs`:
```csharp
using UnityEngine;
using Basket.Core;

namespace Basket.Gameplay
{
    public class PassSystem : MonoBehaviour
    {
        private BallController ball;
        private BallConfig config;

        public void Configure(BallController ballController, BallConfig ballConfig)
        {
            ball = ballController;
            config = ballConfig;
        }

        public bool TryPass(Transform passer, Transform target)
        {
            if (ball.CurrentState != BallState.Held || ball.CurrentHolder != passer) return false;

            Vector3 origin = passer.position + Vector3.up * config.handHeightOffset;
            Vector3 destination = target.position + Vector3.up * config.handHeightOffset;
            Vector3 velocity = TrajectoryMath.ComputeArcVelocity(origin, destination, apexHeight: 1.2f, gravity: Physics.gravity.y);

            ball.Release(BallState.Passing, velocity);
            return true;
        }
    }
}
```

`Assets/_Project/Scripts/Gameplay/ShootingSystem.cs`:
```csharp
using System;
using UnityEngine;
using Basket.Core;

namespace Basket.Gameplay
{
    public class ShootingSystem : MonoBehaviour
    {
        private BallController ball;
        private ShotConfig config;
        private Transform rimTarget;
        // Fully qualified: with both `using System;` and `using UnityEngine;` in
        // scope, bare `Random` is ambiguous between System.Random and
        // UnityEngine.Random and fails to compile.
        private readonly System.Random rng = new();

        public void Configure(BallController ballController, ShotConfig shotConfig, Transform rim)
        {
            ball = ballController;
            config = shotConfig;
            rimTarget = rim;
        }

        public bool TryShoot(Transform shooter)
        {
            if (ball.CurrentState != BallState.Held || ball.CurrentHolder != shooter) return false;

            Vector3 origin = shooter.position + Vector3.up * 1.6f;
            Vector3 velocity = TrajectoryMath.ComputeArcVelocity(origin, rimTarget.position, config.arcHeight, Physics.gravity.y);
            velocity += ShotMath.ComputeMissOffset(config.baseAccuracyRadius, config.defaultShooterRating, rng);

            ball.Release(BallState.Shooting, velocity);
            return true;
        }
    }
}
```

- [ ] **Step 4: Run PLAYMODE_TESTS to verify it passes**

Expected: 3 tests, `Passed`.

- [ ] **Step 5: Commit**

```bash
cd "/d/Projetos/basket"
git add Assets/_Project/Scripts/Gameplay/PassSystem.cs Assets/_Project/Scripts/Gameplay/ShootingSystem.cs Assets/_Project/Tests/PlayMode/PassAndShootSystemTests.cs
git commit -m "feat(gameplay): add PassSystem and ShootingSystem"
```

---

### Task 11: `DribbleMath` + `DribbleSystem`

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/DribbleMath.cs`
- Create: `Assets/_Project/Scripts/Gameplay/DribbleSystem.cs`
- Test: `Assets/_Project/Tests/EditMode/DribbleMathTests.cs`
- Test: `Assets/_Project/Tests/PlayMode/DribbleSystemTests.cs`

**Interfaces:**
- Consumes: `BallController` (Task 8).
- Produces: `DribbleMath.ComputeBounceOffsetY(float phase, float bounceHeight) : float`; `DribbleSystem : MonoBehaviour { void Configure(BallController ball); void Tick(bool isMoving, float dt); }` — consumed by `GameBootstrap` (Task 18).

- [ ] **Step 1: Write the failing tests**

`Assets/_Project/Tests/EditMode/DribbleMathTests.cs`:
```csharp
using NUnit.Framework;
using Basket.Gameplay;

public class DribbleMathTests
{
    [Test]
    public void ComputeBounceOffsetY_AtPhaseZero_ReturnsMaxNegativeOffset()
    {
        float offset = DribbleMath.ComputeBounceOffsetY(phase: 0f, bounceHeight: 0.35f);
        Assert.AreEqual(-0.35f, offset, 0.001f);
    }

    [Test]
    public void ComputeBounceOffsetY_StaysWithinBounceHeightRange()
    {
        for (float phase = 0f; phase < 10f; phase += 0.3f)
        {
            float offset = DribbleMath.ComputeBounceOffsetY(phase, bounceHeight: 0.35f);
            Assert.LessOrEqual(offset, 0f + 0.001f);
            Assert.GreaterOrEqual(offset, -0.35f - 0.001f);
        }
    }
}
```

`Assets/_Project/Tests/PlayMode/DribbleSystemTests.cs`:
```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Basket.Gameplay;

public class DribbleSystemTests
{
    [UnityTest]
    public IEnumerator Tick_WhileHeldAndMoving_OffsetsBallDownward()
    {
        var ballGo = new GameObject("Ball");
        ballGo.AddComponent<Rigidbody>();
        var ball = ballGo.AddComponent<BallController>();
        ball.SetConfigForTest(ScriptableObject.CreateInstance<BallConfig>());
        yield return null;

        var holderGo = new GameObject("Holder");
        ball.Catch(holderGo.transform);

        var dribbleGo = new GameObject("Dribble");
        var dribble = dribbleGo.AddComponent<DribbleSystem>();
        dribble.Configure(ball);

        dribble.Tick(isMoving: true, dt: 0.1f);
        yield return null;

        Assert.LessOrEqual(ball.transform.position.y, holderGo.transform.position.y + 1.1f);

        Object.Destroy(ballGo);
        Object.Destroy(holderGo);
        Object.Destroy(dribbleGo);
    }
}
```

- [ ] **Step 2: Run EDITMODE_TESTS and PLAYMODE_TESTS to verify they fail**

Expected: compile error (`DribbleMath`, `DribbleSystem` do not exist).

- [ ] **Step 3: Write the minimal implementation**

`Assets/_Project/Scripts/Gameplay/DribbleMath.cs`:
```csharp
using UnityEngine;

namespace Basket.Gameplay
{
    public static class DribbleMath
    {
        public static float ComputeBounceOffsetY(float phase, float bounceHeight)
        {
            float bob = Mathf.Abs(Mathf.Sin(phase)) * bounceHeight;
            return -(bounceHeight - bob);
        }
    }
}
```

`Assets/_Project/Scripts/Gameplay/DribbleSystem.cs`:
```csharp
using UnityEngine;
using Basket.Core;

namespace Basket.Gameplay
{
    public class DribbleSystem : MonoBehaviour
    {
        [SerializeField] private float bounceHeight = 0.35f;
        [SerializeField] private float bounceFrequency = 2.2f;

        private BallController ball;
        private float phase;

        public void Configure(BallController ballController)
        {
            ball = ballController;
        }

        public void Tick(bool isMoving, float dt)
        {
            if (ball.CurrentState != BallState.Held)
            {
                phase = 0f;
                return;
            }
            if (!isMoving)
            {
                ball.SetHeldLocalOffset(Vector3.zero);
                return;
            }
            phase += dt * bounceFrequency * Mathf.PI * 2f;
            float offsetY = DribbleMath.ComputeBounceOffsetY(phase, bounceHeight);
            ball.SetHeldLocalOffset(Vector3.up * offsetY);
        }
    }
}
```

- [ ] **Step 4: Run EDITMODE_TESTS and PLAYMODE_TESTS to verify they pass**

Expected: 2 new EditMode tests + 1 new PlayMode test, all `Passed`.

- [ ] **Step 5: Commit**

```bash
cd "/d/Projetos/basket"
git add Assets/_Project/Scripts/Gameplay/DribbleMath.cs Assets/_Project/Scripts/Gameplay/DribbleSystem.cs Assets/_Project/Tests/EditMode/DribbleMathTests.cs Assets/_Project/Tests/PlayMode/DribbleSystemTests.cs
git commit -m "feat(gameplay): add DribbleMath and DribbleSystem"
```

---

### Task 12: `OpponentAIStateMachine`

**Files:**
- Create: `Assets/_Project/Scripts/AI/OpponentAIStateMachine.cs`
- Test: `Assets/_Project/Tests/EditMode/OpponentAIStateMachineTests.cs`

**Interfaces:**
- Consumes: `AIState`, `AIPerception` (Task 3).
- Produces: `OpponentAIStateMachine { AIState CurrentState; event Action<AIState,AIState> OnStateChanged; void Evaluate(AIPerception perception); }` — consumed by `AIOpponentController` (Task 13).

- [ ] **Step 1: Write the failing test**

`Assets/_Project/Tests/EditMode/OpponentAIStateMachineTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;
using Basket.Core;
using Basket.AI;

public class OpponentAIStateMachineTests
{
    [Test]
    public void Evaluate_OpponentHasBallFar_TransitionsToGuard()
    {
        var fsm = new OpponentAIStateMachine();
        var perception = new AIPerception(Vector3.zero, new Vector3(10f, 0f, 0f), Vector3.zero, opponentHasBall: true, selfHasBall: false);

        fsm.Evaluate(perception);

        Assert.AreEqual(AIState.Guard, fsm.CurrentState);
    }

    [Test]
    public void Evaluate_OpponentHasBallClose_TransitionsToContestShot()
    {
        var fsm = new OpponentAIStateMachine();
        var perception = new AIPerception(Vector3.zero, new Vector3(1f, 0f, 0f), Vector3.zero, opponentHasBall: true, selfHasBall: false);

        fsm.Evaluate(perception);

        Assert.AreEqual(AIState.ContestShot, fsm.CurrentState);
    }

    [Test]
    public void Evaluate_NoOneHasBall_TransitionsToChase()
    {
        var fsm = new OpponentAIStateMachine();
        var perception = new AIPerception(Vector3.zero, Vector3.zero, new Vector3(3f, 0f, 3f), opponentHasBall: false, selfHasBall: false);

        fsm.Evaluate(perception);

        Assert.AreEqual(AIState.Chase, fsm.CurrentState);
    }

    [Test]
    public void Evaluate_SelfHasBall_TransitionsToIdle()
    {
        var fsm = new OpponentAIStateMachine();
        var perception = new AIPerception(Vector3.zero, Vector3.zero, Vector3.zero, opponentHasBall: false, selfHasBall: true);

        fsm.Evaluate(perception);

        Assert.AreEqual(AIState.Idle, fsm.CurrentState);
    }

    [Test]
    public void Evaluate_StateChange_FiresOnStateChanged()
    {
        var fsm = new OpponentAIStateMachine();
        bool fired = false;
        fsm.OnStateChanged += (from, to) => fired = true;

        fsm.Evaluate(new AIPerception(Vector3.zero, Vector3.zero, Vector3.zero, false, false));

        Assert.IsTrue(fired);
    }
}
```

- [ ] **Step 2: Run EDITMODE_TESTS to verify it fails**

Expected: compile error (`OpponentAIStateMachine` does not exist).

- [ ] **Step 3: Write the minimal implementation**

`Assets/_Project/Scripts/AI/OpponentAIStateMachine.cs`:
```csharp
using System;
using UnityEngine;
using Basket.Core;

namespace Basket.AI
{
    public sealed class OpponentAIStateMachine
    {
        public AIState CurrentState { get; private set; } = AIState.Idle;
        public event Action<AIState, AIState> OnStateChanged;

        public void Evaluate(AIPerception perception)
        {
            AIState next = DecideNextState(perception);
            if (next == CurrentState) return;

            var prev = CurrentState;
            CurrentState = next;
            OnStateChanged?.Invoke(prev, next);
        }

        private static AIState DecideNextState(AIPerception p)
        {
            if (p.OpponentHasBall)
            {
                float distToOpponent = Vector3.Distance(p.SelfPosition, p.OpponentPosition);
                return distToOpponent < 2.5f ? AIState.ContestShot : AIState.Guard;
            }
            if (p.SelfHasBall) return AIState.Idle;
            return AIState.Chase;
        }
    }
}
```

- [ ] **Step 4: Run EDITMODE_TESTS to verify it passes**

Expected: 5 tests, `Passed`.

- [ ] **Step 5: Commit**

```bash
cd "/d/Projetos/basket"
git add Assets/_Project/Scripts/AI/OpponentAIStateMachine.cs Assets/_Project/Tests/EditMode/OpponentAIStateMachineTests.cs
git commit -m "feat(ai): add OpponentAIStateMachine (Idle/Guard/ContestShot/Chase)"
```

---

### Task 13: `AIOpponentController` MonoBehaviour

**Files:**
- Create: `Assets/_Project/Scripts/AI/AIOpponentController.cs`
- Test: `Assets/_Project/Tests/PlayMode/AIOpponentControllerTests.cs`

**Interfaces:**
- Consumes: `OpponentAIStateMachine` (Task 12); `IAIController`, `AIPerception`, `AIState` (Task 3).
- Produces: `AIOpponentController : MonoBehaviour, IAIController` — consumed by `GameBootstrap` (Task 18).

- [ ] **Step 1: Write the failing test**

`Assets/_Project/Tests/PlayMode/AIOpponentControllerTests.cs`:
```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Basket.Core;
using Basket.AI;

public class AIOpponentControllerTests
{
    [UnityTest]
    public IEnumerator Tick_LooseBall_MovesTowardBall()
    {
        var go = new GameObject("AI");
        var ai = go.AddComponent<AIOpponentController>();
        yield return null;

        var perception = new AIPerception(
            selfPosition: Vector3.zero,
            opponentPosition: new Vector3(-5f, 0f, 0f),
            ballPosition: new Vector3(3f, 0f, 0f),
            opponentHasBall: false,
            selfHasBall: false);

        ai.Tick(perception);

        Assert.AreEqual(AIState.Chase, ai.CurrentState);
        Assert.Greater(ai.GetMoveInput().x, 0f);
        Object.Destroy(go);
    }

    [UnityTest]
    public IEnumerator Tick_SelfHasBall_WantsShootIsTrue()
    {
        var go = new GameObject("AI");
        var ai = go.AddComponent<AIOpponentController>();
        yield return null;

        var perception = new AIPerception(Vector3.zero, Vector3.zero, Vector3.zero, opponentHasBall: false, selfHasBall: true);
        ai.Tick(perception);

        Assert.IsTrue(ai.WantsShoot());
        Object.Destroy(go);
    }
}
```

- [ ] **Step 2: Run PLAYMODE_TESTS to verify it fails**

Expected: compile error (`AIOpponentController` does not exist).

- [ ] **Step 3: Write the minimal implementation**

`Assets/_Project/Scripts/AI/AIOpponentController.cs`:
```csharp
using UnityEngine;
using Basket.Core;

namespace Basket.AI
{
    public class AIOpponentController : MonoBehaviour, IAIController
    {
        private readonly OpponentAIStateMachine fsm = new();
        private Vector2 currentMoveInput;
        private bool selfHasBall;

        public AIState CurrentState => fsm.CurrentState;

        public void Tick(AIPerception perception)
        {
            fsm.Evaluate(perception);
            selfHasBall = perception.SelfHasBall;
            currentMoveInput = ComputeMoveInput(fsm.CurrentState, perception);
        }

        private static Vector2 ComputeMoveInput(AIState state, AIPerception p)
        {
            Vector3 targetPos = state switch
            {
                AIState.Chase => p.BallPosition,
                AIState.Guard => Vector3.Lerp(p.OpponentPosition, p.SelfPosition, 0.3f),
                AIState.ContestShot => p.OpponentPosition,
                _ => p.SelfPosition
            };
            Vector3 toTarget = targetPos - p.SelfPosition;
            toTarget.y = 0f;
            return toTarget.sqrMagnitude < 0.04f ? Vector2.zero : new Vector2(toTarget.x, toTarget.z).normalized;
        }

        public Vector2 GetMoveInput() => currentMoveInput;
        public bool WantsSprint() => fsm.CurrentState == AIState.Chase;
        public bool WantsDribbleAction() => false;
        public bool WantsPass() => false;
        public bool WantsShoot() => selfHasBall;
    }
}
```

- [ ] **Step 4: Run PLAYMODE_TESTS to verify it passes**

Expected: 2 tests, `Passed`.

- [ ] **Step 5: Commit**

```bash
cd "/d/Projetos/basket"
git add Assets/_Project/Scripts/AI/AIOpponentController.cs Assets/_Project/Tests/PlayMode/AIOpponentControllerTests.cs
git commit -m "feat(ai): add AIOpponentController (implements IAIController via OpponentAIStateMachine)"
```

**Note:** the AI shoots immediately upon gaining possession — there is no positioning/dribble-to-basket logic yet. That is explicitly deferred to the future "advanced AI" subproject (see spec §9); it is not a bug in this slice.

---

### Task 14: `MatchState` + `MatchManager`

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/MatchState.cs`
- Create: `Assets/_Project/Scripts/Gameplay/MatchManager.cs`
- Test: `Assets/_Project/Tests/EditMode/MatchStateTests.cs`
- Test: `Assets/_Project/Tests/PlayMode/MatchManagerTests.cs`

**Interfaces:**
- Consumes: `MatchPhase`, `IMatchState` (Task 3); `BallController` (Task 8).
- Produces: `MatchState : IMatchState { void StartLivePlay(); void RegisterScore(bool homeScored, int points); void ResetForNextPossession(); }`; `MatchManager : MonoBehaviour { IMatchState State { get; } void Configure(BallController ball); }` — consumed by `GameBootstrap`, `DebugHud` (Tasks 17, 18).

- [ ] **Step 1: Write the failing tests**

`Assets/_Project/Tests/EditMode/MatchStateTests.cs`:
```csharp
using NUnit.Framework;
using Basket.Core;
using Basket.Gameplay;

public class MatchStateTests
{
    [Test]
    public void RegisterScore_WhileWaitingForInbound_IsIgnored()
    {
        var state = new MatchState();
        state.RegisterScore(homeScored: true, points: 2);
        Assert.AreEqual(0, state.ScoreHome);
    }

    [Test]
    public void RegisterScore_WhileLive_AddsPointsAndTransitionsToScored()
    {
        var state = new MatchState();
        state.StartLivePlay();
        state.RegisterScore(homeScored: true, points: 2);

        Assert.AreEqual(2, state.ScoreHome);
        Assert.AreEqual(MatchPhase.Scored, state.Phase);
    }

    [Test]
    public void ResetForNextPossession_ReturnsToWaitingForInbound()
    {
        var state = new MatchState();
        state.StartLivePlay();
        state.RegisterScore(true, 2);
        state.ResetForNextPossession();

        Assert.AreEqual(MatchPhase.WaitingForInbound, state.Phase);
    }
}
```

`Assets/_Project/Tests/PlayMode/MatchManagerTests.cs`:
```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Basket.Core;
using Basket.Gameplay;

public class MatchManagerTests
{
    [UnityTest]
    public IEnumerator BallScored_IncreasesScoreOnMatchManager()
    {
        var ballGo = new GameObject("Ball");
        ballGo.AddComponent<Rigidbody>();
        var ball = ballGo.AddComponent<BallController>();
        ball.SetConfigForTest(ScriptableObject.CreateInstance<BallConfig>());
        yield return null;

        var matchGo = new GameObject("Match");
        var match = matchGo.AddComponent<MatchManager>();
        match.Configure(ball);
        match.State.StartLivePlayForTest();

        var holderGo = new GameObject("Holder");
        ball.Catch(holderGo.transform);
        ball.Release(BallState.Shooting, Vector3.up);
        ball.NotifyScored();

        Assert.AreEqual(2, match.State.ScoreHome + match.State.ScoreAway);

        Object.Destroy(ballGo);
        Object.Destroy(matchGo);
        Object.Destroy(holderGo);
    }
}
```

- [ ] **Step 2: Run EDITMODE_TESTS and PLAYMODE_TESTS to verify they fail**

Expected: compile error (`MatchState`, `MatchManager` do not exist).

- [ ] **Step 3: Write the minimal implementation**

`Assets/_Project/Scripts/Gameplay/MatchState.cs`:
```csharp
using System;
using Basket.Core;

namespace Basket.Gameplay
{
    public sealed class MatchState : IMatchState
    {
        public int ScoreHome { get; private set; }
        public int ScoreAway { get; private set; }
        public MatchPhase Phase { get; private set; } = MatchPhase.WaitingForInbound;

        public event Action OnScoreChanged;
        public event Action<MatchPhase, MatchPhase> OnPhaseChanged;

        public void StartLivePlay() => SetPhase(MatchPhase.Live);

        public void RegisterScore(bool homeScored, int points)
        {
            if (Phase != MatchPhase.Live) return;
            if (homeScored) ScoreHome += points; else ScoreAway += points;
            OnScoreChanged?.Invoke();
            SetPhase(MatchPhase.Scored);
        }

        public void ResetForNextPossession() => SetPhase(MatchPhase.WaitingForInbound);

        private void SetPhase(MatchPhase next)
        {
            if (next == Phase) return;
            var prev = Phase;
            Phase = next;
            OnPhaseChanged?.Invoke(prev, next);
        }

        internal void StartLivePlayForTest() => StartLivePlay();
    }
}
```

`Assets/_Project/Scripts/Gameplay/MatchManager.cs`:
```csharp
using UnityEngine;
using Basket.Core;

namespace Basket.Gameplay
{
    public class MatchManager : MonoBehaviour
    {
        private readonly MatchState matchState = new();
        private BallController ball;

        public MatchState State => matchState;

        public void Configure(BallController ballController)
        {
            ball = ballController;
            ball.OnScored += OnBallScored;
            matchState.StartLivePlay();
        }

        private void OnBallScored(Transform scorer)
        {
            // Vertical slice: single hoop, always credit "home" — real home/away attribution
            // belongs to the future 3v3/5v5 subproject once there are two hoops.
            matchState.RegisterScore(homeScored: true, points: 2);
            matchState.ResetForNextPossession();
            matchState.StartLivePlay();
        }

        private void OnDestroy()
        {
            if (ball != null) ball.OnScored -= OnBallScored;
        }
    }
}
```

- [ ] **Step 4: Run EDITMODE_TESTS and PLAYMODE_TESTS to verify they pass**

Expected: 3 new EditMode tests + 1 new PlayMode test, all `Passed`.

- [ ] **Step 5: Commit**

```bash
cd "/d/Projetos/basket"
git add Assets/_Project/Scripts/Gameplay/MatchState.cs Assets/_Project/Scripts/Gameplay/MatchManager.cs Assets/_Project/Tests/EditMode/MatchStateTests.cs Assets/_Project/Tests/PlayMode/MatchManagerTests.cs
git commit -m "feat(gameplay): add MatchState and MatchManager"
```

---

### Task 15: `ScoreTrigger`

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/ScoreTrigger.cs`
- Test: `Assets/_Project/Tests/PlayMode/ScoreTriggerTests.cs`

**Interfaces:**
- Consumes: `BallController` (Task 8).
- Produces: `ScoreTrigger : MonoBehaviour { void Configure(BallController ball); }` — placed on the rim trigger collider by `GameBootstrap`'s scene assembly (Task 18).

- [ ] **Step 1: Write the failing test**

`Assets/_Project/Tests/PlayMode/ScoreTriggerTests.cs`:
```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Basket.Core;
using Basket.Gameplay;

public class ScoreTriggerTests
{
    [UnityTest]
    public IEnumerator BallEnteringTrigger_CallsNotifyScoredOnBall()
    {
        var ballGo = new GameObject("Ball");
        var ballCollider = ballGo.AddComponent<SphereCollider>();
        ballGo.AddComponent<Rigidbody>();
        var ball = ballGo.AddComponent<BallController>();
        ball.SetConfigForTest(ScriptableObject.CreateInstance<BallConfig>());
        yield return null;

        bool scored = false;
        ball.OnScored += _ => scored = true;

        var rimGo = new GameObject("Rim");
        var rimCollider = rimGo.AddComponent<SphereCollider>();
        rimCollider.isTrigger = true;
        rimCollider.radius = 5f;
        var trigger = rimGo.AddComponent<ScoreTrigger>();
        trigger.Configure(ball);

        ballGo.transform.position = rimGo.transform.position;
        yield return new WaitForFixedUpdate();
        yield return null;

        Assert.IsTrue(scored);

        Object.Destroy(ballGo);
        Object.Destroy(rimGo);
    }
}
```

- [ ] **Step 2: Run PLAYMODE_TESTS to verify it fails**

Expected: compile error (`ScoreTrigger` does not exist).

- [ ] **Step 3: Write the minimal implementation**

`Assets/_Project/Scripts/Gameplay/ScoreTrigger.cs`:
```csharp
using UnityEngine;

namespace Basket.Gameplay
{
    [RequireComponent(typeof(Collider))]
    public class ScoreTrigger : MonoBehaviour
    {
        private BallController ball;

        public void Configure(BallController ballController)
        {
            ball = ballController;
        }

        private void OnTriggerEnter(Collider other)
        {
            var otherBall = other.GetComponentInParent<BallController>();
            if (otherBall != null && otherBall == ball)
            {
                ball.NotifyScored();
            }
        }
    }
}
```

- [ ] **Step 4: Run PLAYMODE_TESTS to verify it passes**

Expected: 1 test, `Passed`.

- [ ] **Step 5: Commit**

```bash
cd "/d/Projetos/basket"
git add Assets/_Project/Scripts/Gameplay/ScoreTrigger.cs Assets/_Project/Tests/PlayMode/ScoreTriggerTests.cs
git commit -m "feat(gameplay): add ScoreTrigger (rim collider -> BallController.NotifyScored)"
```

---

### Task 16: `CameraConfig` + `CameraController`

**Files:**
- Create: `Assets/_Project/Scripts/Gameplay/CameraConfig.cs`
- Create: `Assets/_Project/Scripts/Gameplay/CameraController.cs`
- Test: `Assets/_Project/Tests/PlayMode/CameraControllerTests.cs`

**Interfaces:**
- Consumes: none beyond a `Transform` target.
- Produces: `CameraController : MonoBehaviour { void Configure(Transform target); }` — consumed by `GameBootstrap` (Task 18).

- [ ] **Step 1: Write the failing test**

`Assets/_Project/Tests/PlayMode/CameraControllerTests.cs`:
```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Basket.Gameplay;

public class CameraControllerTests
{
    [UnityTest]
    public IEnumerator Configure_WithTarget_CameraMovesTowardOffsetPosition()
    {
        var camGo = new GameObject("Cam");
        var controller = camGo.AddComponent<CameraController>();
        controller.SetConfigForTest(ScriptableObject.CreateInstance<CameraConfig>());

        var targetGo = new GameObject("Target");
        targetGo.transform.position = new Vector3(10f, 0f, 10f);

        controller.Configure(targetGo.transform);

        Vector3 startPos = camGo.transform.position;
        for (int i = 0; i < 10; i++) yield return null;

        Assert.Less(Vector3.Distance(camGo.transform.position, targetGo.transform.position),
                    Vector3.Distance(startPos, targetGo.transform.position));

        Object.Destroy(camGo);
        Object.Destroy(targetGo);
    }
}
```

- [ ] **Step 2: Run PLAYMODE_TESTS to verify it fails**

Expected: compile error (`CameraController`, `CameraConfig` do not exist).

- [ ] **Step 3: Write the minimal implementation**

`Assets/_Project/Scripts/Gameplay/CameraConfig.cs`:
```csharp
using UnityEngine;

namespace Basket.Gameplay
{
    [CreateAssetMenu(fileName = "CameraConfig", menuName = "Basket/Camera Config")]
    public class CameraConfig : ScriptableObject
    {
        public Vector3 offset = new Vector3(0f, 6f, -8f);
        public float followSmoothing = 8f;
    }
}
```

`Assets/_Project/Scripts/Gameplay/CameraController.cs`:
```csharp
using UnityEngine;

namespace Basket.Gameplay
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private CameraConfig config;
        private Transform target;

        public void Configure(Transform followTarget)
        {
            target = followTarget;
        }

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 desiredPos = target.position + config.offset;
            transform.position = Vector3.Lerp(transform.position, desiredPos, 1f - Mathf.Exp(-config.followSmoothing * Time.deltaTime));
            transform.LookAt(target.position + Vector3.up * 1.2f);
        }

        internal void SetConfigForTest(CameraConfig testConfig)
        {
            config = testConfig;
        }
    }
}
```

- [ ] **Step 4: Run PLAYMODE_TESTS to verify it passes**

Expected: 1 test, `Passed`.

- [ ] **Step 5: Commit**

```bash
cd "/d/Projetos/basket"
git add Assets/_Project/Scripts/Gameplay/CameraConfig.cs Assets/_Project/Scripts/Gameplay/CameraController.cs Assets/_Project/Tests/PlayMode/CameraControllerTests.cs
git commit -m "feat(gameplay): add CameraConfig and CameraController"
```

---

### Task 17: `DebugHud`

**Files:**
- Create: `Assets/_Project/Scripts/UI/DebugHud.cs`

**Interfaces:**
- Consumes: `IMatchState`, `IBallStateReadOnly`, `IAIController` (Task 3).
- Produces: `DebugHud : MonoBehaviour { void Configure(IMatchState match, IBallStateReadOnly ball, IAIController ai); }` — consumed by `GameBootstrap` (Task 18).

This component is a debug-only visual overlay (`OnGUI`, IMGUI) — it has no automated test; verify it manually in Task 18's playtest checklist instead.

- [ ] **Step 1: Write the implementation**

`Assets/_Project/Scripts/UI/DebugHud.cs`:
```csharp
using UnityEngine;
using Basket.Core;

namespace Basket.UI
{
    public class DebugHud : MonoBehaviour
    {
        private IMatchState match;
        private IBallStateReadOnly ball;
        private IAIController ai;

        public void Configure(IMatchState matchState, IBallStateReadOnly ballState, IAIController aiController)
        {
            match = matchState;
            ball = ballState;
            ai = aiController;
        }

        private void OnGUI()
        {
            if (match == null || ball == null || ai == null) return;

            GUI.Label(new Rect(10, 10, 300, 20), $"Score: {match.ScoreHome} - {match.ScoreAway}  ({match.Phase})");
            GUI.Label(new Rect(10, 30, 300, 20), $"Ball: {ball.CurrentState}");
            GUI.Label(new Rect(10, 50, 300, 20), $"AI: {ai.CurrentState}");
        }
    }
}
```

- [ ] **Step 2: Run COMPILE_CHECK**

Expected: no `error CS` output.

- [ ] **Step 3: Commit**

```bash
cd "/d/Projetos/basket"
git add Assets/_Project/Scripts/UI/DebugHud.cs
git commit -m "feat(ui): add DebugHud (debug-only IMGUI overlay for score/ball state/AI state)"
```

---

### Task 18: `GameBootstrap` + scene assembly + integration test + acceptance playtest

**Files:**
- Create: `Assets/_Project/Scripts/Bootstrap/GameBootstrap.cs`
- Create: `Assets/_Project/Editor/SceneAssembly.cs` (editor-only, not shipped — assembles the scene once via `-executeMethod`)
- Create: `Assets/_Project/Scenes/01_VerticalSlice_HalfCourt.unity` (produced by running `SceneAssembly`)
- Test: `Assets/_Project/Tests/PlayMode/VerticalSliceIntegrationTests.cs`

**Interfaces:**
- Consumes: every public type produced by Tasks 3–17.
- Produces: a playable scene and the wired `GameBootstrap` component. Nothing downstream consumes this — it is the top of the dependency graph.

- [ ] **Step 1: Write `GameBootstrap`**

`Assets/_Project/Scripts/Bootstrap/GameBootstrap.cs`:
```csharp
using UnityEngine;
using Basket.Core;
using Basket.Gameplay;
using Basket.AI;
using Basket.Input;
using Basket.UI;

namespace Basket.Bootstrap
{
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private PlayerMotor humanMotor;
        [SerializeField] private HumanInputProvider humanInput;
        [SerializeField] private PlayerMotor aiMotor;
        [SerializeField] private AIOpponentController aiController;
        [SerializeField] private BallController ball;
        [SerializeField] private PassSystem passSystem;
        [SerializeField] private ShootingSystem shootingSystem;
        [SerializeField] private DribbleSystem dribbleSystem;
        [SerializeField] private MatchManager matchManager;
        [SerializeField] private ScoreTrigger scoreTrigger;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private DebugHud debugHud;
        [SerializeField] private BallConfig ballConfig;
        [SerializeField] private ShotConfig shotConfig;
        [SerializeField] private Transform rimTarget;

        private void Awake()
        {
            matchManager.Configure(ball);
            scoreTrigger.Configure(ball);
            passSystem.Configure(ball, ballConfig);
            shootingSystem.Configure(ball, shotConfig, rimTarget);
            dribbleSystem.Configure(ball);
            cameraController.Configure(humanMotor.transform);
            debugHud.Configure(matchManager.State, ball, aiController);
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            var perception = new AIPerception(
                selfPosition: aiMotor.transform.position,
                opponentPosition: humanMotor.transform.position,
                ballPosition: ball.Position,
                opponentHasBall: ball.CurrentHolder == humanMotor.transform,
                selfHasBall: ball.CurrentHolder == aiMotor.transform);
            aiController.Tick(perception);

            TickAgent(humanInput, humanMotor, humanMotor.transform, aiMotor.transform, dt);
            TickAgent(aiController, aiMotor, aiMotor.transform, humanMotor.transform, dt);
        }

        private void TickAgent(IPlayerAgent agent, PlayerMotor motor, Transform self, Transform other, float dt)
        {
            motor.Tick(agent.GetMoveInput(), agent.WantsSprint(), dt);
            dribbleSystem.Tick(agent.GetMoveInput().sqrMagnitude > 0.01f, dt);

            if (agent.WantsPass() && ball.CurrentHolder == self)
            {
                passSystem.TryPass(self, other);
            }
            if (agent.WantsShoot() && ball.CurrentHolder == self)
            {
                shootingSystem.TryShoot(self);
            }
        }
    }
}
```

- [ ] **Step 2: Run COMPILE_CHECK**

Expected: no `error CS` output.

- [ ] **Step 3: Write the scene-assembly editor script**

`Assets/_Project/Editor/SceneAssembly.cs` — builds the half-court scene entirely from primitives and wires every component via code (no Inspector drag-drop). Court dimensions: half of a regulation 15m-wide court, 14m from baseline to half-court line (matching the 3v3 half-court decision).

```csharp
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

        private static void BuildCourt()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "CourtFloor";
            floor.transform.localScale = new Vector3(15f, 0.2f, 14f);
            floor.transform.position = new Vector3(0f, -0.1f, 7f);
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
    }
}
```

- [ ] **Step 4: Run the scene assembly**

```bash
"/c/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe" -batchmode -nographics -projectPath "D:\Projetos\basket\.worktrees\vertical-slice" -executeMethod Basket.EditorTools.SceneAssembly.Build -logFile "D:\Projetos\basket\.worktrees\vertical-slice\Logs\scene-assembly.log" -quit
grep -i "error" "D:\Projetos\basket\.worktrees\vertical-slice\Logs\scene-assembly.log"
```
Expected: `grep` finds no matches; `Assets/_Project/Scenes/01_VerticalSlice_HalfCourt.unity` exists; `Assets/_Project/Data/DefaultBallConfig.asset` and `DefaultShotConfig.asset` exist.

- [ ] **Step 5: Write the integration test**

`Assets/_Project/Tests/PlayMode/VerticalSliceIntegrationTests.cs`:
```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Basket.Gameplay;

public class VerticalSliceIntegrationTests
{
    [UnityTest]
    public IEnumerator VerticalSliceScene_RunsForFiveSecondsWithoutExceptions()
    {
        yield return SceneManager.LoadSceneAsync("01_VerticalSlice_HalfCourt", LoadSceneMode.Single);
        yield return null;

        var matchManager = Object.FindFirstObjectByType<MatchManager>();
        Assert.IsNotNull(matchManager, "MatchManager should exist in the assembled scene.");
        Assert.AreEqual(MatchPhase.Live, matchManager.State.Phase);

        float elapsed = 0f;
        while (elapsed < 5f)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }

        Assert.Pass("Scene ran for 5 seconds without an unhandled exception.");
    }
}
```

Add the scene to Build Settings so `SceneManager.LoadSceneAsync` can find it by name:
```bash
"/c/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe" -batchmode -nographics -projectPath "D:\Projetos\basket\.worktrees\vertical-slice" -executeMethod Basket.EditorTools.SceneAssembly.AddSceneToBuildSettings -logFile "D:\Projetos\basket\.worktrees\vertical-slice\Logs\build-settings.log" -quit
```

Add this method to `SceneAssembly.cs` (append inside the `SceneAssembly` class from Step 3, then re-run Step 4 once to pick it up):
```csharp
        [MenuItem("Basket/Add Vertical Slice Scene To Build Settings")]
        public static void AddSceneToBuildSettings()
        {
            const string scenePath = "Assets/_Project/Scenes/01_VerticalSlice_HalfCourt.unity";
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == scenePath)) return;
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
```

- [ ] **Step 6: Run PLAYMODE_TESTS to verify the integration test passes**

Expected: all PlayMode tests from every task, including this one, `Passed`. Zero `Failed`.

- [ ] **Step 7: Manual acceptance-criteria playtest**

Open the Editor, open `01_VerticalSlice_HalfCourt`, press Play, and manually confirm every criterion from spec §7:
- [ ] Player moves with WASD / left stick, and turns to face the movement direction (not instantly snapping).
- [ ] Ball visibly bobs while held and the human player is moving (dribble).
- [ ] Pressing E (or gamepad West) passes the ball toward the AI opponent with a visible arc.
- [ ] Pressing Space (or gamepad South) shoots toward the rim with a visible arc.
- [ ] A make triggers `ScoreTrigger` → score increments on the `DebugHud` overlay.
- [ ] The AI opponent chases a loose ball, guards the human when the human holds the ball, and shoots immediately if it gains possession.
- [ ] After a score, possession resets and play continues (`MatchPhase` cycles `Live → Scored → WaitingForInbound → Live` visibly via the HUD).
- [ ] No console errors/exceptions appear during a 2–3 minute play session.

Record any deviation as a follow-up note in this plan file (do not silently skip a failing criterion).

- [ ] **Step 8: Commit**

```bash
cd "/d/Projetos/basket"
git add Assets/_Project/Scripts/Bootstrap/GameBootstrap.cs Assets/_Project/Editor/SceneAssembly.cs Assets/_Project/Scenes/01_VerticalSlice_HalfCourt.unity Assets/_Project/Scenes/01_VerticalSlice_HalfCourt.unity.meta Assets/_Project/Data Assets/_Project/Tests/PlayMode/VerticalSliceIntegrationTests.cs ProjectSettings/EditorBuildSettings.asset
git commit -m "feat: assemble Vertical Slice scene, wire GameBootstrap, add integration test"
```

---

## Self-Review Notes

- **Spec coverage:** every component named in the spec's §4 ("Componentes e responsabilidades") has a task: `GameBootstrap` (18), `PlayerMotor` (5), `PlayerController`/`AIOpponentController` — realized as `HumanInputProvider`+`PlayerMotor` (6, 5) and `AIOpponentController` (13), `BallController`/`BallPhysics` (8), `DribbleSystem`/`PassSystem`/`ShootingSystem` (11, 10), IA FSM (12, 13), `MatchManager` (14), `CameraController` (16). Acceptance criteria (§7) are walked in Task 18 Step 7.
- **Placeholder scan:** no TBD/TODO remain; every step has real, complete code.
- **Type consistency:** `BallController.Release(BallState releaseState, Vector3 velocity)` signature is identical across Tasks 8, 10, 14. `AIPerception` constructor order (`self, opponent, ball, opponentHasBall, selfHasBall`) is identical across Tasks 3, 12, 13, 18. `Configure(...)` method names/signatures match between each producer task and `GameBootstrap`'s usage in Task 18.
- **Assembly rule spot-check:** `Basket.AI` (Tasks 12, 13) never references `Basket.Gameplay` — `AIOpponentController` only touches `Core` types (`AIPerception`, `AIState`, `IAIController`). `Basket.UI` (Task 17) only references `Core` (`IMatchState`, `IBallStateReadOnly`, `IAIController`), never `Basket.Gameplay` directly — `GameBootstrap` (which does reference everything) is the one that hands `DebugHud` its concrete instances through those interfaces.
- **Preflight fixes applied before execution (see SDD ledger for the ruling record):** (1) every `SetConfigForTest`/`StartLivePlayForTest` test hook added in Tasks 5, 8, 10, 11, 14, 15, 16 is `internal` to `Basket.Gameplay`, which `Basket.Tests.EditMode`/`Basket.Tests.PlayMode` cannot see without an explicit grant — Task 2 now creates `AssemblyInfo.cs` with `[assembly: InternalsVisibleTo(...)]` for both test assemblies so every later task compiles as written. (2) `BallController.OnCollisionEnter` (Task 8) would otherwise re-catch a just-released ball to the same player before it clears their own collider, silently nullifying every pass/shot — a `lastReleasedBy`/`SelfCatchGraceSeconds` guard was added to `Release`/`OnCollisionEnter`.
