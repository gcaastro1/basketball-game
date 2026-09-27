# Lista de animações para gerar no Kimodo

Todas as movimentações de que o jogo precisa, com o prompt de cada uma e o aviso de
**segmentação** quando o clipe continua (ou desemboca em) outro. Marque `[x]` na coluna
**Feito** conforme for gerando.

- **Prioridade A**: entra direto numa vaga que já existe em `CharacterAnimationClips`
  (`Assets/_Project/Scripts/Characters/CharacterVisualDefinition.cs`). Basta ligar o clipe
  no `Data/Characters/DefaultCharacterVisual.asset`.
- **Prioridade B**: situação que o jogo já tem (hoje com pose procedural); precisa de uma
  vaga nova pequena no código.
- **Prioridade C**: mecânica que ainda não existe; só gere quando a mecânica for feita.

## Regras gerais (valem para todos os clipes)

1. **No lugar (in place).** O motor do jogo move o corpo (D-025); o clipe não pode andar
   pelo chão. Os prompts de locomoção já dizem "in place"; se o resultado ainda andar, remova
   o deslocamento horizontal da raiz ao exportar.
2. **Loops fecham o ciclo:** a última pose = a primeira. Gere um pouco mais longo e recorte
   um ciclo limpo se precisar (o jogo também aceita uma janela `from..to` do clipe).
3. **Sem bola.** O Kimodo gera só o corpo; o jogo coloca a bola na mão. As mãos precisam estar
   onde a bola estaria: quadril (drible), peito (segurar), acima da testa (arremesso).
4. **Mão dominante = direita** em todos os clipes (as variações de mão esquerda estão listadas
   à parte).
5. **30 fps, um clipe por FBX, mesmo esqueleto** em todos.
6. **Onde salvar:** `Assets/TripoModels/<personagem>/Animations/` — o
   `CharacterAnimationImporter` configura como Humanoid sozinho.
7. **Nome do arquivo:** a coluna *Arquivo* de cada tabela (`bb_<grupo>_<nome>.fbx`).
8. **Prompts em inglês** (é o idioma em que o modelo foi treinado). Todos começam com o mesmo
   sujeito — "A basketball player" — para manter o mesmo estilo de corpo entre os clipes.

## O que é o aviso de segmentação

> ⛓ **SEGMENTAÇÃO** — o clipe **começa na pose final** de outro clipe e/ou **termina na pose
> inicial** de outro. Se forem gerados separados, a emenda aparece como um "pulo" do corpo.

Como gerar um clipe com esse aviso (na ordem de preferência):

1. **Um take só com a sequência inteira**: gere a cadeia toda de uma vez (ex.: "correndo com
   a bola → bandeja → aterrissa") usando os prompts encadeados na linha do tempo do Kimodo, e
   exporte o take inteiro. No jogo, a janela `start..release` recorta só a parte da ação.
2. **Pose fixada**: gere o clipe anterior primeiro e use a pose final dele como restrição de
   pose inicial (keyframe) do clipe seguinte.
3. **Separado**: se nenhuma das duas der, gere separado e tente deixar a primeira e a última
   pose iguais à do clipe vizinho (descrevendo-as no prompt). O jogo faz uma transição curta
   entre clipes, que esconde diferenças pequenas — não grandes.

Os loops de locomoção **não** têm aviso: o jogo mistura (blend) entre eles continuamente.

## Substituição das animações atuais

A meta é o jogo ter **só animações próprias**, feitas do zero. O bloco **A** cobre todas as
vagas de `CharacterAnimationClips`; nenhuma vaga fica de fora. Hoje o
`DefaultCharacterVisual.asset` usa clipes do pacote Basquete (`Assets/TripoModels`) nestas
vagas, e cada uma tem o substituto abaixo:

| Vaga | Hoje | Substituto |
|---|---|---|
| `free` (8 vagas) | clipes do pacote Basquete | A1.1–A1.8 |
| `withBall` (8 vagas) | clipes do pacote Basquete | A2.1–A2.8 |
| `guard` (8 vagas) | clipes do pacote Basquete | A3.1–A3.8 |
| `dribbleUpperBody` | clipe do pacote (janela de 2 quiques) | A4.1 |
| `jumpShots` (2) | clipes do pacote | A5.1–A5.3 |
| `jumpShotsMoving` (2) | clipes do pacote | A5.4–A5.6 |
| `layup` | clipe do pacote | A5.7 |
| `dunk` | mesmo clipe da bandeja | A5.8 (clipe próprio) |
| `freeThrow` | clipe do pacote | A5.9 |
| `block` | clipe do pacote | A6.2 |
| `airborne` | mesmo clipe do bloqueio | A6.3 (clipe próprio) |
| `holdBall` | **vazio** (pose procedural) | A4.2 |
| `pass` | **vazio** (pose procedural) | A6.1 |
| `celebrate` | **vazio** (pose procedural) | A6.4–A6.6 |
| — | pegar a bola do chão: pose procedural `PickUp` | B1 |

Quando o bloco A estiver completo e ligado, os clipes do pacote Basquete deixam de ser usados
e podem sair do projeto.

---

## A1. Locomoção sem bola (`free`) — 8 loops

| Feito | ID | Arquivo | Vaga | Tipo | Prompt |
|---|---|---|---|---|---|
| [ ] | A1.1 | `bb_free_idle` | `free.idle` | loop 2–3 s | A basketball player standing idle in place, relaxed athletic posture, arms hanging loosely, slight weight shift from one foot to the other, breathing. |
| [ ] | A1.2 | `bb_free_walk_fwd` | `free.forward` | loop, 1 ciclo | A basketball player walking forward in place at a relaxed pace, arms swinging naturally. |
| [ ] | A1.3 | `bb_free_walk_back` | `free.backward` | loop, 1 ciclo | A basketball player walking backward in place, looking forward, arms relaxed. |
| [ ] | A1.4 | `bb_free_walk_left` | `free.left` | loop, 1 ciclo | A basketball player side-stepping to the left in place, facing forward, arms relaxed. |
| [ ] | A1.5 | `bb_free_walk_right` | `free.right` | loop, 1 ciclo | A basketball player side-stepping to the right in place, facing forward, arms relaxed. |
| [ ] | A1.6 | `bb_free_run` | `free.run` | loop, 1 ciclo | A basketball player running in place at full speed, athletic stride, arms pumping. |
| [ ] | A1.7 | `bb_free_run_turn_l` | `free.runTurnLeft` | loop, 1 ciclo | A basketball player running in place while leaning the body into a left turn, arms pumping. |
| [ ] | A1.8 | `bb_free_run_turn_r` | `free.runTurnRight` | loop, 1 ciclo | A basketball player running in place while leaning the body into a right turn, arms pumping. |

## A2. Locomoção com bola (`withBall`) — 8 loops

Mão direita quicando a bola na altura do quadril em todos. Mantenha o **mesmo ritmo de
quique** nos oito (o jogo sincroniza o quique da bola com o clipe).

| Feito | ID | Arquivo | Vaga | Tipo | Prompt |
|---|---|---|---|---|---|
| [ ] | A2.1 | `bb_ball_idle` | `withBall.idle` | loop, 2 quiques | A basketball player standing in place dribbling a basketball with the right hand at hip height, low steady bounces, knees slightly bent, looking forward. |
| [ ] | A2.2 | `bb_ball_walk_fwd` | `withBall.forward` | loop, 1 ciclo | A basketball player walking forward in place while dribbling a basketball with the right hand at hip height, steady rhythm. |
| [ ] | A2.3 | `bb_ball_walk_back` | `withBall.backward` | loop, 1 ciclo | A basketball player backpedaling in place while dribbling a basketball with the right hand, protecting the ball with the left arm. |
| [ ] | A2.4 | `bb_ball_walk_left` | `withBall.left` | loop, 1 ciclo | A basketball player side-stepping to the left in place while dribbling a basketball with the right hand, left arm protecting the ball. |
| [ ] | A2.5 | `bb_ball_walk_right` | `withBall.right` | loop, 1 ciclo | A basketball player side-stepping to the right in place while dribbling a basketball with the right hand, left arm protecting the ball. |
| [ ] | A2.6 | `bb_ball_run` | `withBall.run` | loop, 1 ciclo | A basketball player running in place at full speed while dribbling a basketball with the right hand, ball pushed ahead, one bounce per stride. |
| [ ] | A2.7 | `bb_ball_run_turn_l` | `withBall.runTurnLeft` | loop, 1 ciclo | A basketball player running in place while dribbling with the right hand and leaning into a left turn. |
| [ ] | A2.8 | `bb_ball_run_turn_r` | `withBall.runTurnRight` | loop, 1 ciclo | A basketball player running in place while dribbling with the right hand and leaning into a right turn. |

## A3. Locomoção em marcação (`guard`) — 8 loops

| Feito | ID | Arquivo | Vaga | Tipo | Prompt |
|---|---|---|---|---|---|
| [ ] | A3.1 | `bb_guard_idle` | `guard.idle` | loop 2 s | A basketball defender in a low defensive stance in place, knees bent, back straight, arms wide with hands up, bouncing lightly on the balls of the feet. |
| [ ] | A3.2 | `bb_guard_fwd` | `guard.forward` | loop, 1 ciclo | A basketball defender in a low defensive stance shuffling forward in place with short steps, arms wide, hands up. |
| [ ] | A3.3 | `bb_guard_back` | `guard.backward` | loop, 1 ciclo | A basketball defender in a low defensive stance backpedaling in place with short quick steps, arms wide, hands up. |
| [ ] | A3.4 | `bb_guard_slide_l` | `guard.left` | loop, 1 ciclo | A basketball defender doing a defensive slide to the left in place, feet never crossing, low stance, arms wide. |
| [ ] | A3.5 | `bb_guard_slide_r` | `guard.right` | loop, 1 ciclo | A basketball defender doing a defensive slide to the right in place, feet never crossing, low stance, arms wide. |
| [ ] | A3.6 | `bb_guard_run` | `guard.run` | loop, 1 ciclo | A basketball defender running in place in a semi-crouched stance, arms up and wide, chasing an opponent. |
| [ ] | A3.7 | `bb_guard_run_turn_l` | `guard.runTurnLeft` | loop, 1 ciclo | A basketball defender running in place in a semi-crouched stance, arms up, leaning into a left turn. |
| [ ] | A3.8 | `bb_guard_run_turn_r` | `guard.runTurnRight` | loop, 1 ciclo | A basketball defender running in place in a semi-crouched stance, arms up, leaning into a right turn. |

## A4. Com a bola (parado)

| Feito | ID | Arquivo | Vaga | Tipo | Prompt | Segmentação |
|---|---|---|---|---|---|---|
| [ ] | A4.1 | `bb_ball_dribble_upper` | `dribbleUpperBody` | loop, exatamente 2 quiques | A basketball player standing in place dribbling a basketball with the right hand at hip height, exactly two low steady bounces, torso upright. | — (é camada só do tronco; ritmo igual ao A2) |
| [ ] | A4.2 | `bb_ball_hold` | `holdBall` | loop 2–3 s | A basketball player holding a basketball with both hands at chest height in a triple-threat stance, knees bent, looking around the court. | — |

## A5. Arremessos

Em todos, anote dois instantes do clipe: **gather** (junta a bola para arremessar) e
**release** (a bola sai da mão). Eles viram a janela `start..release`.

| Feito | ID | Arquivo | Vaga | Tipo | Prompt | Segmentação |
|---|---|---|---|---|---|---|
| [ ] | A5.1 | `bb_shot_jump_1` | `jumpShots[0]` | 1× ~1,6 s | A basketball player in triple-threat stance holding the ball at the chest, bends the knees, jumps straight up, shoots a jump shot with the right hand releasing at the top of the jump, holds the follow-through with the wrist flicked down, and lands in place. | ⛓ Começa na pose de **A4.2** (`bb_ball_hold`). Termina em pé → volta a **A1.1**. |
| [ ] | A5.2 | `bb_shot_jump_2` | `jumpShots[1]` | 1× ~1,6 s | Same as a jump shot from triple-threat, with a quicker, lower set point and a slight forward lean, releasing at the top, landing in place. | ⛓ Igual a A5.1. |
| [ ] | A5.3 | `bb_shot_jump_3` | `jumpShots[2]` | 1× ~1,6 s | A basketball player shoots a high-arc jump shot from standing, ball raised above the forehead, release at the top of the jump, long follow-through, lands in place. | ⛓ Igual a A5.1. |
| [ ] | A5.4 | `bb_shot_pullup_1` | `jumpShotsMoving[0]` | 1× ~1,8 s | A basketball player dribbling forward stops abruptly with a one-two step, gathers the ball to the chest, jumps and shoots a pull-up jump shot with the right hand, lands in place. | ⛓ Começa no meio de **A2.6** (`bb_ball_run`). **Gere num take só**: "running while dribbling" → "pull-up jump shot". Termina em pé → A1.1. |
| [ ] | A5.5 | `bb_shot_pullup_2` | `jumpShotsMoving[1]` | 1× ~1,8 s | A basketball player running without the ball receives it, plants both feet in a hop step, and shoots a catch-and-shoot jump shot with the right hand, landing in place. | ⛓ Começa em **A1.6** (`bb_free_run`) e passa pela recepção (**B2**). Gere num take só. |
| [ ] | A5.6 | `bb_shot_stepback` | `jumpShotsMoving[2]` | 1× ~1,8 s | A basketball player dribbling takes a hard step back, gathers the ball and shoots a step-back jump shot with the right hand, landing in place. | ⛓ Começa em **A2.1** (`bb_ball_idle`). Gere num take só. |
| [ ] | A5.7 | `bb_shot_layup_r` | `layup` | 1× ~1,6 s | A basketball player running while dribbling picks up the ball, takes two steps, jumps off the left foot and lays the ball up with the right hand reaching toward the rim, then lands. | ⛓ Começa no meio de **A2.6** (`bb_ball_run`). **Gere num take só**: corrida com bola → dois passos → bandeja → aterrissagem. |
| [ ] | A5.8 | `bb_shot_dunk` | `dunk` | 1× ~2 s | A basketball player running while dribbling gathers the ball, jumps high off both feet and slams a one-handed dunk with the right hand, hangs on the rim briefly, drops and lands. | ⛓ Começa no meio de **A2.6**. Gere num take só (corrida → enterrada → aterrissagem). |
| [ ] | A5.9 | `bb_shot_freethrow` | `freeThrow` | 1× ~2 s | A basketball player at the free throw line holding the ball at the chest, sets the feet, bends the knees and shoots a free throw with the right hand without jumping, holding the follow-through. | ⛓ Começa na pose final de **C6.1** (`bb_ft_routine`) se ele existir; senão, de **A4.2**. |

## A6. Outras ações

| Feito | ID | Arquivo | Vaga | Tipo | Prompt | Segmentação |
|---|---|---|---|---|---|---|
| [ ] | A6.1 | `bb_pass_chest` | `pass` | 1× ~0,8 s | A basketball player holding the ball at the chest steps forward and throws a two-handed chest pass, arms extending with thumbs pointing down, then recovers. | ⛓ Começa em **A4.2** (`bb_ball_hold`), termina em **A1.1**. |
| [ ] | A6.2 | `bb_block` | `block` | 1× ~1,2 s | A basketball defender in a defensive stance jumps straight up to block a shot, both arms fully extended above the head, then lands back in a defensive stance. | ⛓ Começa e termina em **A3.1** (`bb_guard_idle`). |
| [ ] | A6.3 | `bb_airborne` | `airborne` | 1× ~1,2 s | A basketball player jumps straight up for a rebound with both arms reaching high, then lands on both feet absorbing the impact. | ⛓ Começa e termina em **A1.1**. |
| [ ] | A6.4 | `bb_celebrate_1` | `celebrate` | 1× ~2 s | A basketball player celebrates a made basket with a strong fist pump and a shout, then jogs back. | — (a variação pode começar de qualquer lugar) |
| [ ] | A6.5 | `bb_celebrate_2` | `celebrate` (variação) | 1× ~2 s | A basketball player celebrates by pointing to the sky with both hands, then claps. | — |
| [ ] | A6.6 | `bb_celebrate_3` | `celebrate` (variação) | 1× ~2 s | A basketball player celebrates a three-pointer by holding up three fingers and walking backward. | — |

---

## B. Situações que o jogo já tem (precisam de vaga nova)

| Feito | ID | Arquivo | Situação | Tipo | Prompt | Segmentação |
|---|---|---|---|---|---|---|
| [ ] | B1 | `bb_pickup` | bola solta no chão (hoje pose procedural `PickUp`) | 1× ~1 s | A basketball player running slows down, bends the knees and crouches to scoop a basketball off the floor with both hands, then rises to a triple-threat stance holding the ball at the chest. | ⛓ Começa em **A1.6** (`bb_free_run`), termina em **A4.2** (`bb_ball_hold`). Gere num take só. |
| [ ] | B2 | `bb_catch` | recepção de passe | 1× ~0,6 s | A basketball player standing with hands up as a target catches a chest pass with both hands and brings the ball to the chest in triple-threat. | ⛓ Começa em **A1.1**, termina em **A4.2**. |
| [ ] | B3 | `bb_catch_run` | recepção correndo | 1× ~0,8 s | A basketball player running catches a pass with both hands at chest height without breaking stride, then starts dribbling. | ⛓ Começa em **A1.6**, termina em **A2.6** (`bb_ball_run`). Gere num take só. |
| [ ] | B4 | `bb_pass_bounce` | passe quicado | 1× ~0,8 s | A basketball player holding the ball at the chest steps forward and throws a two-handed bounce pass aimed at the floor. | ⛓ Começa em **A4.2**, termina em **A1.1**. |
| [ ] | B5 | `bb_pass_overhead` | passe por cima | 1× ~0,9 s | A basketball player raises the ball above the head with both hands and throws an overhead pass forward. | ⛓ Começa em **A4.2**, termina em **A1.1**. |
| [ ] | B6 | `bb_pass_dribble` | passe saindo do drible | 1× ~0,7 s | A basketball player dribbling with the right hand picks up the ball and throws a quick one-handed push pass. | ⛓ Começa em **A2.1** (`bb_ball_idle`), termina em **A1.1**. |
| [ ] | B7 | `bb_shot_layup_l` | bandeja pela esquerda | 1× ~1,6 s | A basketball player running while dribbling picks up the ball, takes two steps, jumps off the right foot and lays the ball up with the left hand, then lands. | ⛓ Começa no meio de **A2.6**. Gere num take só. |
| [ ] | B8 | `bb_shot_hook` | gancho perto da cesta | 1× ~1,4 s | A basketball player near the basket with the back to the rim turns sideways and shoots a right-handed hook shot, arm sweeping over the head, then lands. | ⛓ Começa em **C2.1** (`bb_post_idle`) se existir; senão, de **A4.2**. |
| [ ] | B9 | `bb_shot_fadeaway` | arremesso caindo para trás | 1× ~1,6 s | A basketball player holding the ball jumps backward away from the defender and shoots a fadeaway jump shot, leaning back, landing behind the starting spot. | ⛓ Começa em **A4.2**. |
| [ ] | B10 | `bb_contest` | contestar arremesso | 1× ~0,9 s | A basketball defender closes out quickly and raises one hand high to contest a shot without jumping much, then settles back into a defensive stance. | ⛓ Começa em **A3.6** (`bb_guard_run`), termina em **A3.1**. Gere num take só. |
| [ ] | B11 | `bb_boxout_rebound` | disputa de rebote | 1× ~1,8 s | A basketball player boxes out an opponent with the back and elbows wide, then jumps and grabs a rebound with both hands, landing and securing the ball at the chest. | ⛓ Termina em **A4.2** (`bb_ball_hold`). |
| [ ] | B12 | `bb_land_hard` | aterrissagem pesada | 1× ~0,6 s | A basketball player lands from a high jump, absorbing the impact with deeply bent knees, then stands up. | ⛓ Começa no **fim** de A5.8 / A6.2 / A6.3 (no ar), termina em **A1.1**. |
| [ ] | B13 | `bb_stumble` | desequilíbrio após contato | 1× ~1 s | A basketball player is bumped from the side, stumbles two steps and regains balance. | — |
| [ ] | B14 | `bb_shot_miss_react` | reação a erro | 1× ~1,5 s | A basketball player misses a shot and reacts with frustration, hands on the head, then jogs back. | ⛓ Começa na aterrissagem de um arremesso (fim de **A5.1**). |

---

## C. Mecânicas que ainda não existem

Gere só quando a mecânica for implementada (o jogo ainda não tem onde tocar).

### C1. Movimentos de drible

Todos começam e terminam dribblando: o início e o fim precisam bater com o loop de drible
(**A2.1** parado ou **A2.6** correndo).

| Feito | ID | Arquivo | Tipo | Prompt | Segmentação |
|---|---|---|---|---|---|
| [ ] | C1.1 | `bb_move_cross_rl` | 1× ~0,8 s | A basketball player dribbling with the right hand does a quick low crossover, bouncing the ball across the body to the left hand, and keeps dribbling with the left hand. | ⛓ Começa em **A2.1**, termina no **drible com a mão esquerda** (gere também uma versão espelhada de A2.1, `bb_ball_idle_left`). |
| [ ] | C1.2 | `bb_move_cross_lr` | 1× ~0,8 s | A basketball player dribbling with the left hand does a quick low crossover to the right hand and keeps dribbling with the right hand. | ⛓ Começa no drible esquerdo, termina em **A2.1**. |
| [ ] | C1.3 | `bb_move_between_legs` | 1× ~0,9 s | A basketball player dribbling with the right hand bounces the ball between the legs to the left hand, stepping forward with the left foot. | ⛓ Começa em **A2.1**, termina no drible esquerdo. |
| [ ] | C1.4 | `bb_move_behind_back` | 1× ~0,9 s | A basketball player dribbling with the right hand wraps the ball behind the back to the left hand and continues dribbling. | ⛓ Começa em **A2.1**, termina no drible esquerdo. |
| [ ] | C1.5 | `bb_move_spin` | 1× ~1,1 s | A basketball player dribbling with the right hand plants the left foot and does a full reverse spin move, switching the ball to the left hand, and continues dribbling forward. | ⛓ Começa em **A2.6** (correndo), termina correndo com drible esquerdo. Gere num take só. |
| [ ] | C1.6 | `bb_move_hesitation` | 1× ~1 s | A basketball player dribbling forward slows down, raises the head and shoulders as if to shoot, then explodes forward again dribbling. | ⛓ Começa e termina em **A2.6**. Gere num take só. |
| [ ] | C1.7 | `bb_move_stepback` | 1× ~0,9 s | A basketball player dribbling takes a hard step back to create space and keeps holding the dribble. | ⛓ Começa e termina em **A2.1**. (O A5.6 é esta mesma entrada terminando em arremesso.) |

### C2. Jogo de costas para a cesta (post)

| Feito | ID | Arquivo | Tipo | Prompt | Segmentação |
|---|---|---|---|---|---|
| [ ] | C2.1 | `bb_post_idle` | loop 2 s | A basketball player in the low post with the back to the basket, holding the ball with both hands, elbows out, knees bent, looking over the shoulder. | — |
| [ ] | C2.2 | `bb_post_backdown` | loop, 1 ciclo | A basketball player with the back to the basket dribbling with the right hand, backing down a defender with short backward pushes of the body. | ⛓ Começa e termina em **C2.1**. |
| [ ] | C2.3 | `bb_post_dropstep` | 1× ~1 s | A basketball player with the back to the basket does a drop step, swinging the left leg around the defender toward the rim, holding the ball high. | ⛓ Começa em **C2.1**, termina na preparação da bandeja (**A5.7**). |
| [ ] | C2.4 | `bb_post_fade` | 1× ~1,6 s | A basketball player with the back to the basket turns over the shoulder and shoots a fadeaway jump shot. | ⛓ Começa em **C2.1**. |

### C3. Defesa

| Feito | ID | Arquivo | Tipo | Prompt | Segmentação |
|---|---|---|---|---|---|
| [ ] | C3.1 | `bb_steal_reach` | 1× ~0,7 s | A basketball defender in a low stance quickly reaches forward with the right hand to poke the ball away from a dribbler, then recovers. | ⛓ Começa e termina em **A3.1**. |
| [ ] | C3.2 | `bb_deflect_pass` | 1× ~0,7 s | A basketball defender jumps sideways into a passing lane and deflects the ball with an outstretched hand. | ⛓ Começa em **A3.1**. |
| [ ] | C3.3 | `bb_take_charge` | 1× ~1,5 s | A basketball defender plants the feet and takes a charge, falling backward onto the floor. | ⛓ Começa em **A3.1**. Gere junto o levantar (**C3.4**) num take só. |
| [ ] | C3.4 | `bb_get_up` | 1× ~1,5 s | A basketball player lying on the floor gets up quickly. | ⛓ Começa no fim de **C3.3**, termina em **A1.1**. |

### C4. Ataque sem bola

| Feito | ID | Arquivo | Tipo | Prompt | Segmentação |
|---|---|---|---|---|---|
| [ ] | C4.1 | `bb_screen` | loop 2 s | A basketball player sets a screen, standing firm with feet shoulder-width apart and arms crossed over the chest. | — |
| [ ] | C4.2 | `bb_call_ball` | 1× ~1,2 s | A basketball player claps the hands and raises one hand high calling for the ball. | — |
| [ ] | C4.3 | `bb_cut_vcut` | 1× ~1,2 s | A basketball player jogs toward the basket, plants the foot and sharply changes direction outward to get open. | ⛓ Começa e termina em **A1.6** (`bb_free_run`). |

### C5. Reações e comunicação

| Feito | ID | Arquivo | Tipo | Prompt | Segmentação |
|---|---|---|---|---|---|
| [ ] | C5.1 | `bb_point_play` | 1× ~1,5 s | A basketball point guard raises a fist and points to call a play. | ⛓ Começa em **A2.1** (dribblando), termina em **A2.1**. |
| [ ] | C5.2 | `bb_high_five` | 1× ~1 s | A basketball player gives a teammate a high five. | — |
| [ ] | C5.3 | `bb_tired` | loop 2 s | A tired basketball player bent over with hands on the knees, breathing heavily. | — |

### C6. Bola parada e pré-jogo

| Feito | ID | Arquivo | Tipo | Prompt | Segmentação |
|---|---|---|---|---|---|
| [ ] | C6.1 | `bb_ft_routine` | 1× ~2 s | A basketball player at the free throw line bounces the ball three times with the right hand, spins it in the hands, and sets it at the chest. | ⛓ Termina exatamente na pose inicial de **A5.9** (`bb_shot_freethrow`). **Gere os dois num take só.** |
| [ ] | C6.2 | `bb_ft_lane_wait` | loop 2 s | A basketball player standing on the free throw lane, hands on knees, waiting for a rebound. | — |
| [ ] | C6.3 | `bb_jumpball` | 1× ~1,2 s | A basketball player at center court jumps for a jump ball tip, reaching up with the right hand. | ⛓ Começa e termina em **A1.1**. |
| [ ] | C6.4 | `bb_inbound` | 1× ~1,2 s | A basketball player out of bounds holding the ball above the head, looking for a teammate, then throws an overhead inbound pass. | ⛓ Termina em **A1.1**. |

---

## Resumo das cadeias (gere num take só)

- `bb_ball_run` → bandeja (**A5.7**, **B7**), enterrada (**A5.8**), pull-up (**A5.4**)
- `bb_free_run` → recepção (**B3**) → `bb_ball_run`; → catch-and-shoot (**A5.5**)
- `bb_free_run` → pegar do chão (**B1**) → `bb_ball_hold`
- `bb_ball_hold` → arremesso parado (**A5.1–A5.3**), passes (**A6.1**, **B4**, **B5**)
- `bb_ft_routine` (**C6.1**) → lance livre (**A5.9**)
- `bb_guard_run` → contestar (**B10**) → `bb_guard_idle`
- `bb_take_charge` (**C3.3**) → levantar (**C3.4**)
- Drible direito ↔ drible esquerdo nos movimentos **C1.x**

## Ordem sugerida

1. **A1, A2, A4**: é o que aparece na tela o tempo todo.
2. **A5.1, A5.7, A5.9**: arremesso parado, bandeja e lance livre.
3. **A3, A6** e o restante de **A5**.
4. **B**, depois **C** conforme as mecânicas forem entrando.
