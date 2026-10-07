# Mapa de migração Unity → Unreal

Status: ⬜ não iniciado · 🔄 em andamento · 🧪 testando · ✅ concluído.
Prioridade: 🔴 crítico · 🟠 importante · 🟡 secundário · 🟢 polimento.

## Visão geral

| Sistema (Unity) | Unreal | Tecnologia | Fase | Prior. | Complex. | Status |
|---|---|---|---|---|---|---|
| Assemblies (`Basket.*.asmdef`) | Módulos (`BasketCore`, `Basket`) | C++ / `.Build.cs` | 1 | 🔴 | Baixa | ✅ |
| Testes EditMode | Automation Spec em `BasketCore` | C++ | 1–2 | 🔴 | Média | ✅ 131 testes |
| `*Math`, `ShotAccuracyModel`, `FoulRules`, `GameClock` | Funções/structs C++ puros | C++ | 2 | 🔴 | Média | ✅ |
| ScriptableObjects de config | `UPrimaryDataAsset` (+ `UCurveFloat`) | C++ + assets | 2 | 🔴 | Baixa | ⬜ |
| `MatchManager` + `MatchRules` | Lógica pura + GameMode (servidor) + GameState (replicado) | C++ | 2–3 | 🔴 | Alta | ✅ árbitro no GameMode, estado replicado pelo GameState |
| `MatchSimulation.Tick` | GameMode/World Subsystem no servidor | C++ | 3 | 🔴 | Alta | 🔄 árbitro ticado no GameMode; simulação completa com bola na Fase 4–5 |
| `IAgentController` + `PlayerCommand` | Interface C++; PlayerController e AIController produzem comandos | C++ | 3 | 🔴 | Média | 🔄 humano via PlayerController + `FBasketContextInput`; IA na Fase 6 |
| `GameBootstrap` | GameMode + GameInstance + Subsystems | C++ | 3 | 🔴 | Média | ✅ GameMode + GameState + PlayerController |
| Input System (`.inputactions`) | Enhanced Input (Input Actions + Mapping Contexts) | Assets + C++ | 3 | 🔴 | Baixa | ✅ assets gerados por script |
| `PlayerMotor` (CharacterController) | `ACharacter` + `UCharacterMovementComponent` (custom: sprint/guarda) | C++ | 3 | 🔴 | Média | ✅ medido contra o Unity |
| `CameraController` | Actor/componente de câmera próprio (local, não replicado) | C++ | 3 | 🔴 | Média | ✅ |
| `BallController` (Rigidbody) | Actor de bola, servidor autoritativo, voo analítico | C++ | 4 | 🔴 | **Alta** | ✅ `ABasketBall` + `BasketPossession` (D-031) |
| `HoopController` / `RimSurface` | Actor do aro: colisão + Physical Material | C++ | 4 | 🔴 | **Alta** | ✅ cápsulas `BasketRim` + `WatchHoop` no GameMode; recalibrado (D-031) |
| Shot/Dribble/Pass/DefenseSystem | Actor Components no Character | C++ | 5 | 🔴 | Média | ⬜ |
| `ShotMeterView` | Widget UMG lendo o componente de arremesso | UMG + C++ | 5 | 🔴 | Baixa | ⬜ |
| `AIAgentController`, `OpponentAIStateMachine` | AIController (servidor) + utility em C++ | C++ | 5–6 | 🟠 | Média | ⬜ |
| `TeamBrain`, formações, espaçamento | Lógica pura C++ chamada pelo GameMode/AI | C++ | 6 | 🟠 | Alta | ⬜ |
| Personagens, atributos, `AttributeTuning` | Data Assets + curvas; avaliar GAS | C++ | 7 | 🟠 | Média | ⬜ |
| Habilidades (`AbilityDefinition`) | Gameplay Ability System **ou** sistema próprio (decidir na fase 7) | C++ | 7 | 🟠 | Alta | ⬜ |
| Animação (Playables + procedural + `HandIK`) | Animation Blueprint, Blend Space, Montages, Two Bone IK / Control Rig | AnimBP + C++ | 8 | 🟠 | **Alta** | ⬜ |
| Clipes FBX (Kimodo, Mixamo, UAL) | Reimport + IK Retargeter para o esqueleto escolhido | Editor | 8 | 🟠 | Média | ⬜ |
| HUDs IMGUI (`DebugHud`, `ProfileHud`) | UMG; depuração com `stat`/Gameplay Debugger | UMG | 9 | 🟠 | Baixa | 🔄 HUD de depuração (Canvas); UMG na Fase 9 |
| 5v5 (`CourtConfig.fullCourt`) | Mesmos dados, nível de quadra inteira | C++ | 10 | 🟡 | Média | ⬜ |
| Save versionado (`PlayerSave`, `SaveMigrator`) | `USaveGame` ou JSON próprio (offline) | C++ | 11 | 🟡 | Média | ⬜ |
| Inventário, economia, gacha | GameInstance Subsystem; autoridade no backend (P-006) | C++ | 11 | 🟡 | Alta | ⬜ |
| Ginásio MarpaStudio, bola TierrasDeRol | Reimport (se a licença permitir) ou substitutos; luz pré-calculada | Editor | 12 | 🟢 | Média | ⬜ |
| Materiais Standard/URP | Material mestre toon + Material Instances | Editor | 12 | 🟢 | Média | ⬜ |
| Importadores do Editor (Tripo, clipes, FPS) | Presets de import / Python de editor, só se necessário | Editor | 8–12 | 🟢 | Baixa | ⬜ |
| CI (GameCI) | Testes por linha de comando local; CI só com runner próprio | Scripts | depois | 🟡 | Alta | ⬜ |

## Fichas dos sistemas críticos

### Núcleo de lógica pura
- **Unity**: classes estáticas e POCOs em `Gameplay/*Math.cs`, `ShotAccuracyModel`, `FoulRules`,
  `GameClock`, `AI/TeamMath`, `SpacingLayout`, `Meta/GachaEngine`; 269 testes EditMode.
- **Unreal**: módulo `BasketCore` sem Actors; structs `USTRUCT` só onde o editor precisa ver; resto
  C++ simples. Testes em Automation Spec (`BEGIN_DEFINE_SPEC`).
- **Dependências**: nenhuma (é a base).
- **Riscos**: eixos (Y↑ → Z↑) e unidades (m → cm) em cada vetor; `Random` com seed precisa dar a
  mesma sequência nos testes (usar `FRandomStream`).
- **Testes**: cada teste EditMode relevante ganha um equivalente; números conferidos contra a Unity.
- **Performance**: desprezível.

### Árbitro e estado da partida
- **Unity**: `MatchManager` (regras, relógio, faltas, reinícios) + `MatchState` + `MatchRules` (SO).
- **Unreal**: regras puras em `BasketCore`; `ABasketGameMode` (só servidor) as executa;
  `ABasketGameState` replica placar, relógio, fase e posse; `MatchRules` vira Data Asset.
- **Dependências**: núcleo puro; bola (eventos de cesta, toque no aro).
- **Riscos**: GameMode não existe no cliente — UI lê sempre do GameState.
- **Testes**: partida 1v1 até o fim com controladores de teste; placar igual nos dois clientes.

### Jogador (movimento)
- **Unity**: `PlayerMotor` + `PlayerMotorMath` sobre CharacterController; sprint, guarda, pulo,
  controle aéreo zero (`airTurnMultiplier`).
- **Unreal**: `ABasketCharacter` com `UCharacterMovementComponent`; sprint e guarda como flags
  comprimidas do CMC (para a predição do cliente funcionar); valores do `PlayerMovementConfig`.
- **Riscos**: modificar velocidade fora do CMC causa correções de rede (personagem "puxando").
- **Testes**: andar/correr/pular igual ao Unity (velocidade 450/650 cm/s); sem correções em PIE com
  100 ms.

### Bola e aro
- **Unity**: `BallController` (Rigidbody, estados de voo D-005, `TryCatchNearby` por distância),
  `HoopController`/`RimSurface` (aro físico), precisão calibrada no aro (D-010).
- **Unreal**: ver "Multiplayer" no README (posse presa, voo analítico, aro com física no servidor).
- **Riscos**: os mais altos da migração — rebote no aro do Chaos ≠ PhysX; recalibrar contra a
  referência.
- **Testes**: arremessos "verdes" sempre entram; taxa por zona ±5 p.p. da referência; mesma
  trajetória nos dois clientes.
- **Performance**: 1 corpo físico; CCD ligado na bola.

### Animação
- **Unity**: rig Humanoid, clipes por Playables, camada superior de drible com máscara,
  procedural por músculos onde falta clipe, `HandIK`/`TwoBoneIK`, bola visual seguindo a mão.
- **Unreal**: Animation Blueprint por esqueleto; Blend Space de locomoção; Layered Blend per Bone
  para o drible; Montages para arremesso/passe com Anim Notifies na soltura; Two Bone IK das mãos.
  O procedural por músculos **não tem equivalente** — some em favor de clipes ou Control Rig.
- **Riscos**: retargeting entre Banana Man, Tripo e clipes Kimodo/Mixamo (IK Retargeter).
- **Testes**: gameplay idêntico com e sem animação (a animação só lê, como D-019).
