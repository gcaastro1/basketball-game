# Vertical Slice — Basquete 3D Anime (Subprojeto 1)

**Status:** Aprovado para implementação
**Data:** 2026-09-23
**Escopo:** Primeiro subprojeto de um jogo maior (ver `docs/roadmap-mestre.md`, a ser extraído do briefing original). Este documento cobre **apenas** a base jogável mínima: 1 jogador humano vs 1 adversário controlado por IA, meia quadra, fundamentos básicos de basquete.

## 1. Objetivo

Produzir a menor versão jogável do basquete que responda "sim" às perguntas do item 37 do briefing original:
- O personagem se movimenta naturalmente?
- O jogador consegue driblar, passar, arremessar?
- A cesta funciona e o placar atualiza?
- O defensor consegue defender e disputar rebote?
- A IA consegue jogar (nível básico)?
- A partida começa e reinicia posse corretamente?

**Fora de escopo neste subprojeto** (specs futuras, nesta ordem): IA avançada (Behavior Tree/Utility, companheiros), sistema de personagens/atributos/progressão, modos 3v3/5v5 completos, regras avançadas (shot clock, faltas, out of bounds completo), Gacha/economia, modo história, UI/VFX finais.

## 2. Decisões travadas (com justificativa)

| Decisão | Escolha | Por quê |
|---|---|---|
| Unity | 6000.6.0f1 (única versão instalada na máquina) | Evita gerenciar múltiplas versões antes de haver motivo. |
| Render Pipeline | URP | Melhor controle de shaders estilizados (toon/cel-shading) para estética anime + melhor performance em console futuro, vs HDRP mais pesado e menos direcionado a estilização. |
| Multiplayer | Nenhum ainda — 1 humano vs 1 IA, local, mesma máquina | Valida o core de gameplay antes de qualquer decisão de rede/autoridade de estado, que é cara de mudar depois. |
| Plataforma de desenvolvimento | PC (Windows) | Console vira meta de otimização em fase de polish, não trava decisões de input/render agora. |
| Input | Novo Input System (Unity), suportando Teclado+Mouse e Gamepad desde já | Evita retrabalho de remapear ações depois; o Input System já abstrai o dispositivo. |
| Tamanho da quadra | Meia quadra (dimensões de 3v3) | Consistente com a ordem do roadmap mestre (3v3 antes de 5v5) e reduz a área/complexidade de posicionamento de IA nesta primeira etapa. |
| Padrão de comunicação entre sistemas | Composition Root leve (`GameBootstrap`) + interfaces + eventos C# — **sem** framework de DI externo (VContainer/Zenject) | Zero dependência externa, testável via interface, sem curva de aprendizado extra antes de haver algo jogável. Migração para um DI framework fica isolada em `GameBootstrap` se necessário no futuro. |
| Movimento do jogador | `CharacterController` (kinemático) + motor custom de aceleração/turn, não Rigidbody dinâmico | Prioriza responsividade de controle (2ª prioridade absoluta do briefing, logo após gameplay), que é mais previsível com CharacterController do que com física dinâmica plena. |
| Física da bola | Híbrida por estado: `Free` (Rigidbody real), `Held` (cinemático, anexado à mão), `Passing`/`Shooting` (trajetória calculada e depois física real) | Requisito explícito do briefing original (item 11): separar física visual de física de gameplay, sem a bola "teleportar" entre jogadores. |
| IA nesta fase | Finite State Machine simples (`Idle → Guard → ContestShot → Chase`) atrás de uma interface `IAIController` | Suficiente para 1 adversário; troca futura para Behavior Tree/Utility AI fica isolada atrás da interface, sem reescrever o resto do sistema. |
| Regras de partida nesta fase | Placar + posse + reinício após cesta/rebote. Sem shot clock, sem faltas, sem out-of-bounds completo | Esses são "Fase 3 completa" no roadmap mestre — item 37 só exige que a partida comece/termine e o placar funcione, não o conjunto completo de regras. |

## 3. Estrutura de projeto

```
Assets/
  _Project/
    Scripts/
      Core/       (Basket.Core.asmdef)     — interfaces, eventos, GameBootstrap
      Gameplay/   (Basket.Gameplay.asmdef) — motor do jogador, bola, drible/passe/arremesso, MatchManager
      AI/         (Basket.AI.asmdef)       — FSM do adversário
      Input/      (Basket.Input.asmdef)    — wrappers do Input System
      UI/         (Basket.UI.asmdef)       — HUD mínimo (placar, estado, overlay de debug)
    Data/                                  — ScriptableObjects de configuração
    Prefabs/
    Scenes/
      00_Bootstrap
      01_VerticalSlice_HalfCourt
    Art/                                   — placeholders geométricos (cápsulas, primitivas), sem assets de terceiros
  Tests/
    EditMode/     (Basket.Tests.EditMode.asmdef)
    PlayMode/     (Basket.Tests.PlayMode.asmdef)
```

Regra de dependência entre assemblies: `Gameplay` e `AI` dependem de `Core`; `AI` **não** referencia `Gameplay` diretamente (só via interfaces em `Core`); `UI` depende de `Core` (eventos), nunca de `Gameplay` diretamente.

## 4. Componentes e responsabilidades

### `GameBootstrap` (Core)
Composition root da cena `01_VerticalSlice_HalfCourt`. Instancia e conecta, no `Awake`: `MatchManager`, `BallController`, os dois `IPlayerAgent` (humano e IA), `CameraController`. Nenhum outro script deve usar `FindObjectOfType` ou singletons estáticos para se localizar — tudo é injetado por referência a partir daqui.

### `PlayerMotor` (Gameplay)
Baseado em `CharacterController`. Responsável por aceleração, desaceleração, giro corporal e sprint, configurado via `PlayerMovementConfig` (ScriptableObject: velocidade máxima, aceleração, desaceleração, velocidade de giro, multiplicador de sprint). Colisão jogador-jogador resolvida por *push-apart* simples (não física dinâmica).

### `IPlayerAgent` + `PlayerController` / `AIOpponentController` (Gameplay / AI)
Interface comum consumida pelo `PlayerMotor` e pelos sistemas de bola. `PlayerController` traduz input humano (via `Basket.Input`) em comandos; `AIOpponentController` traduz decisões da FSM em comandos idênticos — o motor não sabe se está sendo controlado por humano ou IA.

### `BallController` + `BallPhysics` (Gameplay)
Máquina de estados: `Free → Held → Passing/Shooting → Free`.
- `Free`: Rigidbody real — quique, atrito, colisão física de verdade contra quadra/tabela/aro/jogadores.
- `Held`: cinemático, anexado a um socket na mão do jogador com posse; sem simulação de física.
- `Passing` / `Shooting`: velocidade inicial calculada para atingir o alvo/arco desejado, aplicada ao Rigidbody, que então volta a se comportar como `Free` em voo.

### `DribbleSystem`, `PassSystem`, `ShootingSystem` (Gameplay)
Lógica mínima funcional. Cada um lê um único valor de configuração padrão a partir de um SO (`BallHandlingDefault`, `ShotConfig`) em vez de valores fixos no código — ponto de extensão explícito para quando o sistema de atributos de personagem (subprojeto futuro) existir.

### `IAIController` + FSM do adversário (AI)
Estados: `Idle → Guard → ContestShot → Chase`. Toda decisão de IA passa por essa interface, isolando a implementação atual (FSM simples) de uma futura substituição por Behavior Tree/Utility AI sem alterar quem a consome.

### `MatchManager` (Gameplay)
Não conhece física da bola — só reage a eventos (`OnScored`, `OnPossessionChanged`) publicados por `BallController`. Mantém placar e estado de posse, e dispara o reinício de jogada após cesta.

### `CameraController` (Gameplay)
Câmera de acompanhamento em terceira pessoa, configurável via `CameraConfig` (SO). Sem elementos cinemáticos nesta fase.

## 5. Fluxo de eventos

Sistemas não se chamam diretamente. `BallController` publica eventos C# (`OnPossessionChanged`, `OnShotReleased`, `OnScored`) que `MatchManager` e `UI` assinam. Isso permite testar `MatchManager` isoladamente, sem instanciar nenhuma física.

## 6. Estratégia de testes

- **EditMode** (`Basket.Tests.EditMode`): matemática pura — cálculo de arco de arremesso, transições de estado de posse e de partida — sem rodar o Player Loop.
- **PlayMode** (`Basket.Tests.PlayMode`): integração — bola sai da mão → viaja → detecta cesta → posse reinicia.
- **Debug overlay**: tecla de debug exibe em tempo real o estado da FSM da IA e da máquina de estados da bola.

## 7. Critérios de aceitação

Idênticos ao item 37 do briefing original: jogador se move naturalmente; consegue driblar; a bola tem física convincente; consegue passar; consegue arremessar; a cesta funciona; o defensor consegue defender; o rebote funciona; a IA consegue jogar; o jogador consegue marcar pontos; a partida consegue começar e terminar (reiniciar posse).

## 8. Riscos conhecidos

- **Feel do CharacterController**: pode exigir iteração de tuning (aceleração/turn speed) para não parecer "escorregadio" — mitigado por manter todos os valores em `PlayerMovementConfig`, editáveis sem recompilar lógica.
- **Física de arremesso**: acertar a curva de "parece justo, mas físico" é o maior risco de gameplay feel deste subprojeto; será validado por PlayMode tests de trajetória + teste manual.
- **Escopo do MatchManager**: risco de o mínimo necessário (posse + placar) crescer organicamente para incluir regras da Fase 3 completa (shot clock, faltas). Mitigado mantendo `MatchManager` deliberadamente burro nesta fase — qualquer regra nova vira uma decisão explícita de escopo, não um acréscimo silencioso.

## 9. Próximos subprojetos (fora de escopo aqui, ordem sugerida)

1. IA avançada (Behavior Tree/Utility, companheiros)
2. Sistema de personagens (atributos, progressão, Limit Break, dupes, habilidades)
3. Modos de partida completos (3v3 / 5v5, regras completas)
4. Meta-game (Gacha, inventário, economia)
5. Modo história
6. Polish (UI final, VFX, áudio, balanceamento)

Cada um recebe seu próprio ciclo spec → plano → implementação.
