# Etapa 5 — Personagens, atributos, progressão e habilidades

## Objetivo
Cada jogador em quadra passa a ser um **personagem**: atributos 0–99 que mudam o gameplay de
verdade, progressão por nível / Limit Break / dupes, habilidades (passivas e ativas) e
tendências de IA. Tudo data-driven (ScriptableObjects); nada de valores finais de design.

## Arquitetura
- **Core**: `AttributeId` (26 atributos do briefing, enum com valores fixos — só se acrescenta),
  `IPlayerAttributes`, `AttributeSet`, `AttributeModifier`, `AITendencies`. A IA e o gameplay
  só conhecem isso.
- **Basket.Characters** (novo assembly, depende só de Core): `CharacterDefinition`,
  `ProgressionConfig` (nível máx., XP, estágios de Limit Break, 6 dupes), `CharacterInstance`
  (dado de save), `CharacterProgression` (XP, teto, LB, dupes), `CharacterStatsCalculator`,
  `AbilityDefinition` + `PlayerAbilities` (runtime: cooldown, duração, condições, nível da
  habilidade, efeitos em atributos e no arremesso).
- **Gameplay** lê atributos pelo `PlayerEntity` e os converte em efeito por `AttributeTuning`
  (SO com as faixas). Sem personagem = comportamento neutro atual (testes antigos continuam valendo).

## Onde cada atributo age (nenhum cosmético)
| Grupo | Atributo → efeito |
|---|---|
| Arremesso | Close Shot (< 4,5 m), Mid Range (até o arco), 3PT, Layup, Dunk (mínimo p/ enterrar), Free Throw → *rating* do modelo de precisão |
| Contato | Strength e Post Scoring → reduzem o efeito da marcação em finalizações perto da cesta |
| Bola | Ball Handling → resistência a roubo e velocidade driblando; Passing → passe mais rápido/reto |
| Defesa | Perimeter/Interior Defense → força do contest (longe/perto da cesta); Steal → chance de roubo; Block → alcance do toco; Defensive Rebound → raio de rebote |
| Físico | Speed, Acceleration, Agility (giro), Vertical (altura do salto), Stamina (fôlego do sprint; cansaço reduz velocidade e precisão) |
| Inteligência (IA) | Offensive IQ (timing de soltura), Shot Selection (exigência para arremessar), Passing IQ (disposição a passar), Defensive IQ (reação no toco), Help Defense (raio de ajuda), Rebound Positioning (antecipação do rebote) |

## Progressão (valores do `DefaultProgressionConfig` são provisórios)
- Nível 1–60, XP por curva configurável; o teto depende do Limit Break.
- **P-002 provisório**: teto inicial 20; LB1 (nv 20→teto 40), LB2 (40→50), LB3 (50→60),
  LB4 "Awakening" (60, sem teto novo). Cada LB: custo em materiais, bônus de atributos,
  nível de habilidade, desbloqueio de habilidades. Tudo em dados.
- Dupes: base + 6; cada uma com bônus de atributos, nível de habilidade e redução de cooldown.

## Habilidades (framework + 3 exemplos provisórios)
Passiva (sempre ou condicional: *Clutch*, *On Fire*) ou ativa (botão, cooldown, duração).
Efeitos: modificadores de atributo e efeito de arremesso (erro, trajetória). Exemplos
genéricos para teste: "Zone" (ativa), "Clutch" (passiva), "Rainbow Shot" (ativa, trajetória alta).
Botão: **Q** / **RB**.

## Personagens de exemplo (placeholders, sem nomes/lore)
6 arquétipos para testar identidade: Sharpshooter, Slasher, Playmaker, Rim Protector,
Lockdown, Rebounder. A cena 3v3 usa esses 6.

## IA
A IA lê atributos (seus e dos companheiros) no snapshot: estima arremessos pelo atributo da
zona, prefere passar para quem arremessa melhor dali; `AITendencies` do personagem ajustam
preferência por 3, infiltração, passe, agressividade no roubo e uso de habilidade; os atributos
de IQ ajustam a execução.

## Testes
EditMode: cálculo de atributos (nível, LB, dupes, teto), XP e tetos, LB com custo e
requisitos, dupes, desbloqueio/nível de habilidades, cooldown/duração/condições, mapeamentos
atributo→gameplay, IA usando atributos. PlayMode: personagem com Vertical alto salta mais;
arremessador com 3PT alto erra menos que um com 3PT baixo (mesmo arremesso).
