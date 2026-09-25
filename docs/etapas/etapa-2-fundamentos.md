# Etapa 2 — Fundamentos

## Objetivo
Transformar arremesso, defesa e rebote em mecânicas de **timing e posicionamento**, não em
sorteio: o resultado de cada jogada vem de onde os jogadores estão, quando apertam/soltam o
botão e da física da bola.

## Escopo (menor versão funcional de cada fundamento)

| Fundamento | Versão mínima | Fora do escopo (etapa) |
|---|---|---|
| Pulo | `PlayerMotor` com velocidade vertical real, altura configurável, pouco controle no ar | Animação de salto/aterrissagem (6) |
| Arremesso | Segurar = saltar; soltar = arremessar. Erro de mira (em metros, no plano do aro) cresce com distância, timing fora do ápice, contest e velocidade | Tipos pull-up/fadeaway/stepback (5/6) |
| Bandeja | Perto da cesta: sobe e solta sozinho no ápice, arco baixo, erro base menor | Euro step / reverse (5/6) |
| Enterrada | Perto + sprint + alcance suficiente: salto em direção ao aro, bola enterrada fisicamente | Estilos por personagem (5) |
| Bloqueio | Defensor no ar com a mão perto da bola logo após o arremesso desvia a bola | Goaltending (3) |
| Roubo | Defensor perto e de frente para o portador tenta o roubo; chance-base + cooldown de "reach" ao errar | Falta por contato (3) |
| Rebote | Pegar a bola exige alcance vertical (em pé ou saltando); quem pula antes pega mais alto | Box-out ativo (4) |
| Input | Ações do Input System definidas em código (remapeáveis), botões contextuais, buffer de entrada | Tela de remapeamento (9) |
| IA | Segura/solta arremesso perto do ápice com erro configurável, bloqueia, rouba, pula para rebote | Tomada de decisão tática (4) |

## Controles
| Ação | Teclado | Gamepad |
|---|---|---|
| Mover | WASD | Analógico esquerdo |
| Sprint | Shift | RT / clique no analógico esquerdo |
| Com a bola: arremessar (segurar e soltar) · Sem a bola: pular/bloquear/rebote | Espaço | X (Xbox) / □ (PS) |
| Com a bola: passar · Sem a bola: roubar | E | A (Xbox) / ✕ (PS) |

## Arquitetura
- `PlayerCommand`: `Move, Sprint, Pass, ShootHeld, Jump, Steal` (dados, iguais para humano/IA).
- `ShotSystem` (substitui `ShootingSystem`): estado de arremesso por jogador (subida → soltura → pouso), classificação (JumpShot/Layup/Dunk), `ShotAccuracyModel` (puro), relatório `ShotReport` (Core) para HUD/telemetria.
- `DefenseSystem`: roubo e bloqueio. Matemática pura em `ContestMath` / `BlockMath`.
- Configs: `ShotConfig`, `DefenseConfig`, `PlayerMovementConfig` (pulo, alcance).
- Atributos: cada fórmula recebe um `rating` 0–1 que hoje vem do config — ponto de troca para o sistema de personagens (Etapa 5).

## Testes
- EditMode (lógica pura): modelo de precisão, contest, bloqueio, buffer de input, decisões da IA.
- PlayMode (física): pulo, arremesso com soltura no ápice, bandeja, enterrada que pontua, bloqueio desviando a bola, roubo, rebote exigindo alcance, loop completo de partida.

## Critérios de aceitação
- Soltar no ápice, sem marcação e perto → erro de mira menor que soltar cedo/tarde, marcado e longe (testado).
- Enterrada com alcance suficiente pontua; bloqueio bem cronometrado impede a cesta (testado).
- Bola no aro não pode ser pega por quem está no chão sem alcance (testado).
- IA usa pulo, bloqueio, roubo e soltura por timing (testado em EditMode).
- CI verde (typecheck + EditMode + PlayMode).

## Riscos
- Calibração: os números iniciais (erros em metros, chances) são estimativas; ficam em SO e o HUD mostra o relatório de cada arremesso para ajuste.
- CharacterController + pulo: aterrissagem e colisão entre jogadores no ar podem exigir ajuste.
