# Fase 3 — Input, personagem, câmera e árbitro em rede

Repositório: `gcaastro1/basketball-unreal`. Pré-requisitos: Fase 1 (projeto, rede) e Fase 2 (lógica pura,
131 testes) concluídas.

## Objetivo

Primeira fase que junta a lógica com o jogo rodando, **já em rede**: os jogadores se movem com teclado
ou controle como no Unity (andar, correr, guarda, pular), com a câmera de transmissão, e o árbitro roda
no servidor, reinicia as posses posicionando os jogadores e o placar/relógios chegam a todos.

Ainda **sem bola** (Fase 4), sem arremesso/passe (Fase 5), sem IA (Fase 6), sem animação (Fase 8):
os jogadores continuam cilindros. Cabe na GTX 1050.

## Referência no Unity

| Sistema | Unity | Valores |
|---|---|---|
| Input contextual | D-009, `BasketInputActions`, `InputBuffer` | buffer 0,15 s |
| Teclas | `BasketInputActions` | Mover WASD · Sprint Shift · Primário Espaço · Secundário E · Guarda Ctrl · Habilidade Q |
| Controle | idem | Mover analógico esq. · Sprint RT ou L3 · Primário X/□ · Secundário A/✕ · Guarda LT · Habilidade RB |
| Movimento | `DefaultPlayerMovementConfig`, D-024 | 4,5 m/s; sprint ×1,45 (6,5 m/s); guarda ×0,75 e sem sprint; acel. 30 / desacel. 40 m/s²; giro 720 °/s; pulo 0,8 m; controle aéreo 0,15; sem girar no ar |
| Movimento relativo à câmera | D-023 adendo, `CameraRelativeMove` | "para cima" = para onde a câmera olha |
| Câmera de transmissão | D-023, `CameraRigMath`, `DefaultCameraConfig` | distância 7 m, altura 3,6 m, olhar a 1,4 m, lookAhead 0,35 (máx. 4 m), aimAtHoop 0,3, FOV 55°, suavização 8 (posição) e 3 (giro) |
| Árbitro → reinícios | `MatchSimulation` + `PossessionLayout` | posições do `BasketPlayLayout` (Fase 2) |

## Decisões de arquitetura

1. **Movimento predito pelo Character Movement Component (CMC).** Sprint e guarda são **flags
   comprimidas** de um `UBasketMovementComponent` (`FSavedMove` com `FLAG_Custom_0/1`), não variáveis
   mexidas fora dele — senão o servidor corrige o cliente ("puxões"). Pulo = `Jump()` do CMC
   (`JumpZVelocity` = √(2·981·80) ≈ 396 cm/s). Sem girar no ar: orientação para o movimento desligada
   enquanto `IsFalling()`.
2. **Input em assets do Enhanced Input** (`IA_Move`, `IA_Sprint`, `IA_Primary`, `IA_Secondary`,
   `IA_Guard`, `IA_Ability`, `IMC_Basket` com teclado e controle). Para continuarem reproduzíveis e
   versionados, são **gerados por um script Python de editor** (`tools/editor/create_input_assets.py`,
   rodado com `UnrealEditor-Cmd -run=pythonscript`), não clicados à mão. O WASD feito em código da
   Fase 1 sai.
3. **Botões contextuais (D-009) decididos no servidor.** O cliente manda a intenção ("primário
   apertado/solto", com o instante); quem decide o que ela significa (pulo, arremesso, passe, roubo)
   é o servidor, pelo estado da posse. Nesta fase: sem bola, primário = pulo (predito pelo CMC). O
   buffer de 0,15 s (`InputBuffer`) vai para `BasketCore`.
4. **Câmera só local.** `UBasketCameraComponent` no `PlayerController` (não replicada), usando a
   matemática pura portada de `CameraRigMath` e o time com a posse lido do GameState. Movimento
   relativo à câmera portado de `CameraRelativeMove`.
5. **Árbitro no GameMode, estado no GameState.** `ABasketGameMode` (só servidor) cria o
   `FBasketReferee` com as regras FIBA 3x3, dá `Tick` com a bola "solta" (até a Fase 4) e, no
   `OnPossessionRestart`, posiciona os jogadores com `BasketPlayLayout`. `ABasketGameState` replica
   placar, fase, relógio de jogo, shot clock, período, posse e "limpar a bola" (propriedades
   replicadas com `RepNotify`). A UI (HUD de depuração) **só lê o GameState**.
6. **Regras e quadra como Data Assets.** `UBasketMatchRulesAsset` e `UBasketCourtAsset` embrulham as
   structs do `BasketCore` (`FBasketMatchRules`, medidas da quadra) — o equivalente aos
   ScriptableObjects. Criados pelo mesmo script Python, com os valores da referência.
7. **Quadra provisória com cesta visual.** `ABasketSandboxArena` passa a desenhar a meia quadra 3x3
   com linha de 3, garrafão e uma cesta só visual (tabela, aro, poste, sem colisão), para a câmera ter
   para onde olhar. A cesta física é da Fase 4.

## Peças

| Ordem | Peça | Onde | Testes |
|---|---|---|---|
| 1 | `InputBuffer`, `CameraRelativeMove`, `CameraRigMath` portados | `BasketCore` | Automation Spec (portados: 2 + 5 + 5) |
| 2 | Assets de input + script Python que os gera | `Content/Input`, `tools/editor` | script roda sem erro; assets carregam |
| 3 | `UBasketMovementComponent`: sprint, guarda, pulo, sem giro no ar | `Basket` | Functional Test (ver abaixo) |
| 4 | Câmera de transmissão + movimento relativo à câmera | `Basket` | Spec da matemática + checagem no PIE |
| 5 | Árbitro no GameMode, GameState replicado, reinícios posicionando jogadores | `Basket` | `net-smoke` estendido + Functional Test |
| 6 | Quadra 3x3 desenhada + cesta visual + HUD de depuração | `Basket` | checagem no PIE + `stat unit` |

**Testes com mundo (Functional Tests):** os de movimento e de reinício precisam de um mundo rodando.
Uso o plugin *Functional Testing* do Unreal (atores `AFunctionalTest` num mapa de teste gerado pelo
script Python), executável pela linha de comando como os testes da Fase 2. Se esse caminho der
problema no 5.8 sem GPU, o plano B é um `-game -nullrhi` com bots roteirizados, como o `net-smoke`.

## Saída verificável

| Teste | Critério |
|---|---|
| Aceleração | parado → 450 cm/s em 0,15 s ± 1 quadro (4,5 ÷ 30) |
| Freio | 450 cm/s → 0 em 0,11 s ± 1 quadro (4,5 ÷ 40) |
| Sprint / guarda | 652 cm/s com sprint; 337 cm/s em guarda; guarda ignora o sprint |
| Pulo | ápice 80 cm (± 2), tempo no ar ~0,81 s, sem girar no ar |
| Rede | com 100 ms / 1% de perda, sprint e guarda do cliente sem correções visíveis (`p.NetShowCorrections 1` sem marcas) |
| Câmera | atrás e acima do jogador, virada para a cesta; "para cima" no direcional anda para onde a câmera olha |
| Árbitro | partida começa Live com posse do Home; relógio e shot clock descem no HUD do cliente; reinício posiciona os jogadores nos pontos do `BasketPlayLayout` |
| Desempenho | `stat unit` na GTX 1050 perto da linha de base da Fase 1 (frame ~12 ms) |
| Testes | `Basket.*` todos verdes (131 + os novos) |

## Fora desta fase

Bola, aro físico, arremesso, passe, roubo e toco (Fases 4–5); IA (6); habilidades (7); animação e
modelo 3D (8); UI final (9). Vagas sem humano numa partida 3v3 continuam pendentes (P-005): nesta
fase só existem os personagens dos humanos conectados.

## Riscos

- **Flags do CMC**: é o ponto mais delicado de rede da fase; errar aqui gera correções constantes.
- **Functional Tests sem GPU** podem precisar de ajuste (plano B acima).
- **Assets binários gerados por script**: se o script quebrar numa versão futura da engine, os assets
  continuam no LFS e podem ser editados no editor; o script é a receita, não a única cópia.
- **Build com o editor aberto** fica lento (~10 min): fechar o editor nas compilações.

## Progresso

- 2026-10-06: **peças 1–3 prontas** (`Basket.*` 151/151, `net-smoke` 4/4).
  - Peça 1: `FBasketInputBuffer`, `FBasketContextInput`, `BasketCameraMove`, `BasketCameraRig` no
    `BasketCore` (12 testes). Sem direção de câmera, o Unity devolvia o direcional cru (que lá era
    "olhando +Z"); aqui isso trocaria os eixos, então vale "olhando +X".
  - Peça 2: assets em `/Game/Input` gerados por `tools/editor/create_input_assets.py` (reaproveita os
    existentes ao rodar de novo); o PlayerController guarda só os caminhos (referências "soft"), senão
    o gerador sai com erro na primeira execução.
  - Peça 3: `UBasketMovementComponent` com sprint/guarda em flags preditas; medido num mundo de teste
    (`FTestWorldWrapper`): 0,15 s até 450 cm/s, ~0,11 s para parar, 652 / 337 cm/s, pulo de 80 cm,
    ~0,81 s no ar, sem girar no ar. Gravidade do projeto −981 cm/s².
  - Aprendizados de teste: sem controlador o `ACharacter` só escolhe o modo de movimento inicial se
    `bRunPhysicsWithNoController` já estiver ligado ao inicializar os componentes; o CMC mantém a
    cápsula 1,9–2,4 cm acima do chão de propósito.
  - Falta: checar no PIE com emulação de rede que sprint/guarda não geram correções (junto com a
    câmera, peça 4).

