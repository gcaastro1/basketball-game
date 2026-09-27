# Etapa 4.5 — IA posicional (defesa com ajuda, zona, espaçamento)

## Pedido

"Deixar a IA mais inteligente nas jogadas: hoje parece um monte de formiga indo atrás de um pedaço de
açúcar, e não jogadores de basquete se posicionando adequadamente, marcando por zona e coisas do tipo."
E a simulação 3x3 ainda falhava às vezes (poucos arremessos).

## Diagnóstico (log do CI, 3x3 IA×IA que falhou)

- 14 arremessos em 120 s, quase todos com o relógio de posse a ~2 s (arremesso forçado), contestação
  0,6–0,8 e vários tocos; 50 passes sem criar vantagem.
- Causa: todo defensor sem a bola ficava a 1,5 m do seu homem, estivesse a bola onde estivesse; e os
  atacantes sem a bola iam para pontos fixos, às vezes colados em quem tinha a bola.

## O que mudou (D-027)

| Parte | Onde | O quê |
|---|---|---|
| Defesa individual | `AI/DefenseFormation.OffBallSpot`, `TeamBrain.PlanDefense` | a um passe da bola: nega (entre o homem e a cesta, um passo na linha do passe); longe da bola: recua para a linha de ajuda, preso ao homem |
| Zona | `DefenseFormation.ZoneSpots`, `TeamBrain.PlanZone` | 2-3 (5), 2-2 (4), 1-2 (3), deslizando com a bola; um só na bola; cada um marca quem entra na sua área |
| Escolha da defesa | `ITeamStrategy.ChooseDefense`, `AIConfig.zoneDefenseChance` | por posse do adversário; código 0 (sempre individual), dados 0,3 (provisório) |
| Espaçamento | `OffensePlanner.SpacingSlots(maxSide)`, `OffensePlanner.AwayFrom` | além da linha de 3 da quadra, cantos dentro da quadra, longe de quem tem a bola |
| Linha de 3 | `Core/ThreePointLine`, `MatchSnapshot.IsBeyondArc` | a IA lê o canto de 3 como o placar |
| 3x3 limpar a bola | `AIConfig.clearTowardTop` | limpa em direção ao topo do arco |

Valores novos em `AIConfig` (todos provisórios): `denyDistance`, `denyLaneStep`, `helpFullDistance`,
`helpLineFraction`, `maxSag`, `maxSagFromMan`, `zoneDefenseChance`, `zoneShift`, `zoneShiftMax`,
`zoneMarkRadius`, `zoneMarkDistance`, `spacingBeyondArc`, `spacingMinFromHandler`, `clearTowardTop`.

## Papéis táticos e espaçamento editável (pedido seguinte)

- Papéis (`Core.TacticalRole`): Idle, OffenseWithBall, OffenseOffBall, DefenseOnBall, DefenseHelp — no
  painel de debug, e ponto de encaixe para animações de papel (postura de defesa, pedir a bola).
- O defensor da bola fica entre o atacante e a cesta, a 1,1 m, e nunca vai na bola.
- `SpacingLayout` (Inspector): pontos de espaçamento por tamanho de time, em `AIConfig.spacingLayout`.
- Testes: `TeamAITests.Roles_*`, `OnBallDefender_*`, `SpacingLayout_*`.

## Saída verificável

- `TeamAITests` (EditMode):
  - nega a um passe e ajuda no lado fraco;
  - na zona, um só defensor na bola e os outros na sua área;
  - os pontos da zona deslizam com a bola;
  - o espaçamento fica longe de quem tem a bola;
  - o canto conta como 3 para a IA.
- `AISimulationTests.AIvsAI_3v3_PlaysBasketball(0)` (individual) e `(1)` (zona): as mesmas metas por
  minuto (8 arremessos, 4 passes, 2 rebotes). O log mostra a contestação média dos arremessos; o
  esperado é cair.
