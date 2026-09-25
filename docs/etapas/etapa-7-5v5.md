# Etapa 7 — 5v5 em quadra inteira

## Objetivo
Partida 5v5 FIBA em quadra inteira (28 × 15 m): duas cestas, troca de lado no intervalo,
transição ataque/defesa, reposições de fundo e de lateral, regras de meia-quadra e de tempo.
Tudo como **dados** (`MatchRules`, `CourtConfig`, `MatchSetup`) sobre a mesma simulação do 3v3.

## Escopo
| Área | Versão mínima |
|---|---|
| Quadra | `CourtConfig.fullCourt`: arena espelhada com 2 cestas, linhas de 3, meio-campo e círculo central |
| Cestas | Cada `HoopController` sabe qual time a ataca; cesta credita esse time e pontua pela distância até **aquela** cesta; troca no intervalo |
| Início | Bola ao alto (jump ball) no círculo central no 1º período e na prorrogação; demais períodos: reposição lateral no meio |
| Após cesta | Reposição de fundo pelo time que sofreu, sem teleportar os outros (a transição acontece de verdade) |
| Violações / faltas comuns | Reposição lateral no ponto mais próximo |
| Linhas | Bola ou portador fora das linhas → posse adversária |
| Meia-quadra | 8 s para passar do meio; volta à quadra de defesa (backcourt) → violação |
| Shot clock | 24 s; 14 s após toque no aro |
| Faltas | Penalidade a partir da 5ª falta coletiva por período (2 LL); LL 2/3 no arremesso |
| Tempo | 4 × 10 min, prorrogação de 5 min |
| IA | Portador sobe a bola até o topo do garrafão (ou ataca se o contra-ataque estiver aberto); companheiros correm para as vagas; defensores voltam correndo; "4 abertos + 1 no poste" em meia-quadra; marcação por proximidade nas reposições |

Fora do escopo agora: pressão em quadra inteira, substituições/exclusão por 5 faltas, tempos
técnicos, posse alternada (seta) — ficam registrados como próximos passos.

## Presets
`Court5v5Config`, `FIBA5v5MatchRules`, `MatchSetup5v5` (Home: humano + 4 IA; Away: 5 IA, com os
personagens de exemplo) e a cena `02_FullCourt_5v5`. A cena 3v3 continua existindo.

## Testes
- EditMode: 8 s, backcourt (inclusive "defesa tocou por último = legal"), troca de lado no
  intervalo, 4 períodos, penalidade na 5ª falta, tipos de reposição.
- PlayMode: arena de quadra inteira (bola na cesta de cada lado credita o time certo);
  simulação IA×IA 5v5 acelerada: os dois times cruzam o meio-campo, arremessam e pontuam.

## Implementação
- `CourtConfig`: `fullCourt`, `centerCircleRadius`, `CourtCenter`, `Mirror()`; `PlaceholderArenaBuilder`
  constrói a segunda cesta (`Arena.SecondHoop`), meio-campo, círculo central e as duas linhas de 3.
- `HoopController.AttackingTeam` → `ScoreEvent.HoopTeam/HoopCenter` → `MatchManager.HandleScore`.
- `MatchRules`: `RestartKind` ganhou `BaselineInbound`, `SidelineInbound`, `JumpBall`, `MidcourtInbound`;
  campos `fullCourt`, `useBoundaryLines`, `frontcourtSeconds`, `useBackcourtRule`, `startWithJumpBall`,
  `switchSidesAfterPeriod`.
- `MatchManager`: 8 s, backcourt (legal se o adversário tocou por último), reposição lateral após
  violação/falta comum, troca de lado (`OnSidesSwitched`), prorrogação com bola ao alto.
- `MatchSimulation`: cestas por time, `SetUpJumpBall` / `SetUpInbound` / `SetUpHalfCourtLineup`,
  marcação por proximidade nas reposições, linhas laterais/fundo.
- IA: `BallHandlerDecision` sobe a bola (ou ataca no contra-ataque aberto), companheiros correm para as
  vagas (sprint quando longe), "4 abertos + 1 no poste" (`OffensePlanner.PostSpot`).
- Passe: proteção na soltura e raio de interceptação (D-018) — corrigiu passes "roubados" pelo
  defensor colado, visto na simulação IA×IA 3v3.

## Como jogar
Abra `Assets/_Project/Scenes/02_FullCourt_5v5.unity` (ou recrie pelo menu *Basket → Build Full Court
5v5 Scene*) e dê Play. Você controla o primeiro jogador do Home; os outros 9 são IA.
