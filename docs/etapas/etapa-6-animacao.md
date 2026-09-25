# Etapa 6 — Modelos e animação

## Objetivo
Trocar as cápsulas pelo personagem do Tripo3D, animado a partir do estado de jogo, **sem mudar o
gameplay** (regra da etapa, testada).

## O que foi feito
| Parte | Onde |
|---|---|
| Import Humanoid do modelo do Tripo (e de qualquer modelo novo em `Assets/TripoModels`) | `Editor/TripoHumanoidImporter.cs`, `.fbx.meta` |
| Dados visuais do personagem: modelo, altura, rotação, textura, clipes por pose | `Characters/CharacterVisualDefinition.cs`, `Data/Characters/DefaultCharacterVisual.asset` |
| Estado de jogo → pose (corrida, drible, posse, defesa, arremesso, bandeja, enterrada, passe, toco, no ar, comemoração) | `Presentation/AnimationStateMapper.cs` |
| Pose → corpo (canais independentes de rig) | `Presentation/ProceduralPoseMath.cs` |
| Canais → músculos humanoides | `Presentation/ProceduralHumanoidAnimator.cs` |
| Clipes reais (quando existirem) via Playables | `Presentation/ClipAnimationBackend.cs` |
| Mãos na bola (IK de dois ossos) | `Presentation/TwoBoneIK.cs`, `Presentation/HandIK.cs` |
| Modelo no jogador: instanciar, ajustar altura/pés, material URP, esconder cápsula, anel do time | `Presentation/CharacterVisual.cs` |
| Driver por jogador (lê gameplay, anima, IK) | `Presentation/PlayerAnimationDriver.cs` |

## Testes
- EditMode: import Humanoid válido com os ossos mapeados (no Unity do CI); pose por estado; efeito das
  poses no corpo; IK (alcance, comprimento dos ossos, cotovelo para o polo); ajuste de altura.
- PlayMode: modelo substitui a cápsula, altura e pés no chão; **gameplay idêntico com e sem modelo**;
  mãos na bola ao segurar; pernas alternam ao correr; cenas 3v3/5v5 com modelos em todos.

## Como ver
Abra a cena 3v3 ou 5v5 e dê Play. Se o modelo estiver de costas, ajuste `yawOffsetDegrees` (180) no
`DefaultCharacterVisual`. Para animações de verdade, arraste clipes Humanoid (Mixamo, Tripo…) nos slots
`clips` — com idle + run o personagem passa a usar os clipes; poses sem clipe ficam na locomoção.

## Limitações conhecidas (placeholders)
- A animação procedural é propositalmente simples; os valores de músculo são provisórios e ficam numa
  única tabela para ajuste visual.
- Todos os personagens usam o mesmo modelo até existirem modelos por personagem.
- Não há clipes ainda; o caminho por clipes está pronto mas só será exercitado quando houver clipes.
