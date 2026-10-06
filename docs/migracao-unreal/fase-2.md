# Fase 2 — Lógica pura em C++ (`BasketCore`)

Repositório: `gcaastro1/basketball-unreal`. Pré-requisito: Fase 1 concluída (`fase-0-1.md`).

## Objetivo

Portar para o módulo `BasketCore` (só depende de `Core`; sem Actors, sem UObject) a lógica pura do
Unity que o jogo precisa no vertical slice, **com os testes portados junto**. Cada peça é conferida
contra o Unity (os mesmos casos de teste) e, quando houver, contra `referencia-unity.md`.

Essa fase não depende da GPU: cabe antes do upgrade.

## Regras do port

- **Comportamento, não tradução literal.** Mesma matemática; forma idiomática de Unreal C++.
- **Unidades nativas**: cm, Z para cima, X ao longo da quadra (rumo à cesta atacada), Y para o lado.
  Distâncias dos configs viram cm; taxas por metro viram por cm (`/100`); razões sem dimensão
  (ex.: erro por distância, 0,012) não mudam; tempos continuam em segundos.
- **Configs** viram structs C++ simples com os defaults do Unity (`FBasket...Settings`). O Data Asset
  editável (equivalente ao ScriptableObject) fica no módulo `Basket`, embrulhando a struct.
- **Testes**: Automation Spec (`Basket.Core.<Peça>`), casos do Unity escritos em metros/Y-up e
  convertidos com `BasketUnits::FromUnity`, para ler igual ao original e testar a troca de eixos.
- **Aleatório** com `FRandomStream` (semente fixa nos testes; o servidor sorteia no jogo).

## Peças

| Ordem | Unity | Unreal (`BasketCore`) | Testes | Status |
|---|---|---|---|---|
| 1 | `TrajectoryMath`, `ShotArc` | `Shot/BasketTrajectory`, `Shot/BasketShotArc` | 8 (`Basket.Core.Trajectory`) | ✅ |
| 2 | `ThreePointLine`, `ScoringMath`, `HoopMath` | `Scoring/BasketScoring` | 10 (`Basket.Core.Scoring`) | ✅ |
| 3 | `ShotAccuracyModel`, `ShotMath`, janela verde | `Shot/BasketShotAccuracy` | 23 (`Basket.Core.ShotAccuracy`) | ✅ |
| 4 | `ContestMath`, `BlockMath` | `Defense/BasketDefense` | 13 (`Basket.Core.Defense`) | ✅ |
| 5 | `GameClock`, `FoulMath`, `FoulRules`, `MatchState`, `MatchRules` | `Rules/BasketMatchTypes`, `BasketMatchRules`, `BasketClocks`, `BasketFouls`, `BasketMatchState` | 18 (`Basket.Core.Rules`) | ✅ |
| 6 | `MatchManager` (árbitro) | `Rules/BasketReferee` | 36 (`Basket.Core.Referee`) | ✅ |
| 7 | `PossessionLayout`, `PassTargeting`, `DribbleMath`, `PlayerMotorMath` | `Play/…` | ~12 | ⬜ |
| 8 | `Attributes`, `CharacterStatsCalculator`, `CharacterProgression` | `Characters/…` | ~15 | ⬜ |

Fora desta fase: IA de time (`TeamBrain` e cia., 33 testes) e o meta (inventário, gacha, save, ~40
testes). São lógica pura, mas a IA só faz sentido com o jogo rodando (Fase 6) e o meta online depende
do backend (P-006, Fase 11).

## Diferenças deliberadas em relação ao Unity

- **`TrajectoryMath.ComputeCompensatedArcVelocity` não foi portado.** Ele corrigia o lançamento contra o
  integrador do PhysX (50 Hz, amortecimento linear). No Unreal, o plano de rede replica o lançamento e
  cada máquina avalia a parábola (`BasketTrajectory::PositionAt`), que não precisa de compensação.
  Decisão do modelo de voo da bola: Fase 4.
- **`PositionAt` / `VelocityAt`** são novos: o voo analítico que os clientes vão reproduzir.

## Saída verificável

Todos os testes `Basket.Core.*` passando na linha de comando, e cada número de `referencia-unity.md`
que é lógica pura (seção 4 e a trajetória da seção 5) coberto por um teste.

## Progresso

- 2026-10-06: peças 1–4 portadas; `Basket.*` 57/57 na linha de comando (Units 3, Trajectory 8,
  Scoring 10, ShotAccuracy 23, Defense 13). Cobertos por teste: tabela de acerto e larguras da janela
  verde (seção 4 da referência), arcos de 1,21/1,81 m e os 12 arremessos mirando no centro do aro
  (seção 5).
- Dica de ambiente: com o editor aberto sobram ~2,9 GB de RAM e o build cai para 1 compilação por vez
  (~10 min); com o editor fechado, ~1,5 min.
- 2026-10-06: peça 5 (regras, relógios, faltas, estado da partida); `Basket.*` 75/75. `FoulMath.ShootingContact`
  recebe uma lista leve de jogadores (`FBasketPlayerSample`); o snapshot completo vem com o árbitro (peça 6).
- 2026-10-06: peça 6 (árbitro `FBasketReferee`); `Basket.*` 111/111. O teste de fim de partida agora usa o caminho
  real (atingir a pontuação) em vez da chamada interna `EndWith` que o Unity usava.
