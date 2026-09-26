# Etapa 6.6 — Ginásio, bola real e quadra NBA

## Pedido

O usuário adicionou dois pacotes: um ginásio de basquete (`Assets/MarpaStudio`, "Basket Ball Stadium")
e uma bola real (`Assets/TierrasDeRol/Basketball`). Objetivo: usar os dois no jogo. Escolha do usuário:
**medidas NBA**, as da quadra desenhada no piso do ginásio (D-026).

## O que mudou

| Parte | Onde | O quê |
|---|---|---|
| Medidas da quadra | `Data/Court5v5Config.asset`, `Data/DefaultCourtConfig.asset` | 15,24 × 28,65 m (meia quadra: 14,325 m), aro a 1,6 m do fundo, tabela 1,83 × 1,07 m, lance livre a 4,19 m |
| Linha de 3 | `MatchRules.threePointCornerDistance`, `ScoringMath.IsBeyondArc` | arco de 7,24 m + cantos retos a 6,71 m (placar e posse); o placeholder desenha os cantos |
| Ginásio | `Presentation/StadiumLayout`, `Data/Arena/MarpaStadiumLayout.asset`, `tools/stadium/extract_layout.py` | 1307 peças da cena de demonstração, montadas ao iniciar, centradas na quadra; sem colisores |
| Materiais | `Presentation/LitMaterials` | Standard antigo → Lit do URP (textura, cor, normal, metal/suavidade, emissão), uma cópia por material |
| Bola | `Data/Arena/DefaultArenaVisual.asset` → `URPOrange` | modelo escalado para 0,24 m, centrado na bola física; esfera escondida |
| Ligação | `ArenaDresser.Dress` no `GameBootstrap`; campo `arenaVisual` nas duas cenas e no `SceneAssembly` | tudo opcional: sem o asset, o placeholder continua |

A física e as simulações não mudam: colisores de piso, paredes, tabela e aro são os do placeholder.

## Saída verificável

- `ArenaVisualTests` (PlayMode):
  - todas as peças do layout apontam para um modelo;
  - o piso do ginásio (PlayField) tem 15,24 × 28,65 m e está centrado na quadra do jogo, em y = 0;
  - o ginásio não tem colisores e nenhum material fica no shader antigo;
  - a bola mede 0,24 m e fica centrada na bola física.
- `CourtDataTests` (EditMode): medidas NBA nos dados; a posição de check ball fica atrás da linha de 3.
- `ScoringMathTests` (EditMode): cesta do canto conta 3 a partir de 6,71 m; fora da zona do canto vale o arco.

## Pendente

- **Tabela, aro e rede do ginásio.** Ainda não entram: o aro físico precisa coincidir com o aro do
  modelo, e o `StadiumHoopModel_Measurements` imprime no CI as medidas do `Ring.fbx`, da rede e do
  poste para encaixá-los. Até lá, a tabela e o aro são os do placeholder.
- **Luzes.** As luzes da cena de demonstração (spots e probes) não entram; o jogo usa uma luz direcional.
- **IA.** A IA ainda avalia a linha de 3 só pela distância (ver D-026).
