# Arquitetura (estado após a Etapa 1.5)

## Assemblies e dependências

```
Core ◄── Gameplay ◄──┐
  ▲  ◄── AI          ├── Bootstrap (composition root)
  ▲  ◄── Input       │
  ▲  ◄── UI     ◄────┘
Editor (EditorTools) → todos          Tests.EditMode / Tests.PlayMode → ver .asmdef
```
Regra: `AI`, `Input` e `UI` só conhecem `Core`. Nada depende de `Bootstrap`.

## Fluxo por frame

```
GameBootstrap.Update
  └─ MatchSimulation.Tick(dt)
       1. RefreshSnapshot()           jogadores, bola, fase → MatchSnapshot
       2. para cada jogador i:
            cmd = controller[i].Decide(snapshot, i)      (humano | IA | teste)
            motor.Tick(cmd.Move, cmd.Sprint)
            ShotSystem: segurar arremesso = pular; soltar = arremessar (layup/enterrada soltam no ápice)
            com a bola: drible visual, passe (PassTargeting)
            sem a bola: pulo (bloqueio/rebote), roubo (DefenseSystem)
       3. se Live: bloqueios (defensor no ar com os braços na bola logo após a soltura);
          bola solta → jogador mais próximo com alcance vertical pega
       4. MatchManager.Tick(dt)       atraso de reinício → OnPossessionRestart
Física (FixedUpdate)
  BallController   colisões: pega passe/bola solta; voo → Free; chão encerra a release
  HoopController   bola cruzou o aro de cima p/ baixo? → ball.NotifyScored()
Eventos
  BallController.OnScored(ScoreEvent) → MatchManager.HandleScore → MatchState (pontos por time/zona)
  MatchManager.OnPossessionRestart(time) → MatchSimulation posiciona jogadores e dá check ball
```

## Onde mexer para…

| Quero… | Mexa em |
|---|---|
| Mudar 1v1 → 2v2/3v3 | `Data/DefaultMatchSetup.asset` (slots) |
| Mudar pontos/limite/atraso | `Data/DefaultMatchRules.asset` |
| Mudar geometria da quadra/aro | `Data/DefaultCourtConfig.asset` |
| Ajustar precisão/tipos de arremesso | `Data/DefaultShotConfig.asset` (modelo em `Gameplay/ShotAccuracyModel.cs`) |
| Ajustar roubo/bloqueio | `Data/DefaultDefenseConfig.asset` |
| Ajustar pulo/alcance | `Data/DefaultPlayerMovementConfig.asset` |
| Ajustar IA | `Data/DefaultAIConfig.asset`; lógica em `AI/AIAgentController.cs` |
| Novo tipo de controle (Modo B, replay, rede) | nova implementação de `Core/IAgentController` |
| Nova regra | `Gameplay/MatchManager` + campo em `MatchRules` (nunca nos controllers) |

## Dados

Configs são ScriptableObjects em `Assets/_Project/Data`. Todo campo tem default no código, então
um asset ausente não quebra a cena. Menu **Basket → Build Vertical Slice Scene** recria a cena e
cria assets faltantes.

## Verificação

- `tools/typecheck/check.sh` — compila tudo sem Unity (também no CI).
- Unity Test Runner — EditMode (lógica pura) e PlayMode (física, aro, loop completo de partida).
