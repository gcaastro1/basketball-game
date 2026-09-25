# Auditoria do projeto + Avaliação do briefing

> **Atualização 2026-09-25:** Etapa 1.5 aprovada e implementada (C1–C5, I1, I2, I4, I6–I10). Tripo3D mantido no manifest local (D-008); CI configurado (D-007). Ver `docs/decisoes.md` e `docs/roadmap-mestre.md`. Pendentes: I3 e I5 (Etapa 2), P-001 e P-002.

**Data:** 2026-09-25
**Branch auditada:** `claude/hopeful-ptolemy-5nfm22` (idêntica a `main`, commit `14a74bd`)
**Briefing:** `docs/briefing-original.md`
**Limitação importante:** o container onde esta auditoria rodou **não tem Unity instalado**. Nada foi compilado nem executado aqui. Toda conclusão abaixo vem de leitura de código, assets serializados e configurações. O estado "testes passam" **não foi verificado** nesta auditoria.

---

## 1. Objetivo

1. Registrar o estado real do projeto em relação ao briefing (Etapa 0 — Auditoria).
2. Avaliar o briefing (nota 0–10 por categoria, lacunas, correções), como o próprio briefing pede.
3. Identificar problemas arquiteturais críticos que devem ser resolvidos **antes** de expandir (Fase 2 completa, 3v3, personagens).
4. Propor o próximo passo e aguardar confirmação.

---

## 2. Estado atual (inventário)

| Item | Estado |
|---|---|
| Unity | **6000.6.2f1** (a spec do vertical slice ainda diz 6000.6.0f1 — desatualizada) |
| Render pipeline | URP 17.7.0 (assets PC + Mobile do template) |
| Input | Input System 1.20.0; `activeInputHandler: 2` (legacy + novo ativos) |
| Packages relevantes | ai.navigation 2.0.14, test-framework 1.8.0, timeline 6.6.0, ugui 2.6.0 |
| Package problemático | `com.tripo3d.unitybridge` → `file:D:/Downloads/...` (**caminho absoluto local**) |
| Cenas | `Assets/Scenes/SampleScene.unity` (template, índice 0 no Build) + `Assets/_Project/Scenes/01_VerticalSlice_HalfCourt.unity` |
| Geração de cena | `Assets/_Project/Editor/SceneAssembly.cs` (menu *Basket/Build Vertical Slice Scene*) — cena reprodutível por código |
| Scripts de jogo | ~720 linhas em 6 assemblies: Core, Gameplay, AI, Input, UI, Bootstrap (+ EditorTools) |
| Testes | 9 arquivos EditMode + 9 PlayMode (~1 teste de integração: "a cena roda 5s sem exceção") |
| Dados (SO) | `DefaultBallConfig`, `DefaultShotConfig`, `DefaultPlayerMovementConfig`, `DefaultCameraConfig` |
| Arte | Primitivas (cápsulas, cubos, esfera). Um modelo Tripo3D (`Assets/TripoModels/...`) importado como rig **Generic** (não Humanoid), ainda não integrado |
| Layers/Tags | Nenhuma customizada |
| Documentação | Spec + plano do vertical slice em `docs/superpowers/`. `docs/roadmap-mestre.md` é referenciado mas **não existe** |
| Processo | O histórico usou o fluxo *superpowers* (spec → plano → subagentes), não o `claude-code-game-studios` citado no briefing |
| CI | Nenhum |

### Arquitetura existente (resumo)

```
Core        enums (BallState, AIState, MatchPhase), AIPerception (struct), IPlayerAgent,
            IAIController, IMatchState, IBallStateReadOnly
Gameplay    PlayerMotor (CharacterController), BallController (+ BallPossessionStateMachine),
            Pass/Shooting/DribbleSystem, TrajectoryMath, ShotMath, MatchManager/MatchState,
            ScoreTrigger, CameraController, configs SO
AI          OpponentAIStateMachine (Idle/Guard/ContestShot/Chase) + AIOpponentController
Input       HumanInputProvider (polling direto de teclas/gamepad)
UI          DebugHud (IMGUI)
Bootstrap   GameBootstrap — composition root E loop por frame (tick de agentes, IA, pickup)
```

**Pontos fortes reais (reaproveitáveis):**
- Separação em assemblies com regra de dependência respeitada (AI e UI só enxergam Core).
- Lógica pura extraída e testável em EditMode (`TrajectoryMath`, `PlayerMotorMath`, `ShotMath`, FSMs).
- `IPlayerAgent` comum a humano e IA: o motor não sabe quem controla — base correta para Modo A/B.
- Configs em ScriptableObject; cena gerada por script.
- Bugs de playtest documentados nos comentários com causa raiz (pickup via CharacterController, cooldown da IA etc.).

---

## 3. Análise — o que funciona vs. o que o item 37 do briefing pede

| Pergunta do item 37 | Situação no código | Veredito |
|---|---|---|
| Movimenta naturalmente? | Aceleração/desaceleração/giro configuráveis; sempre gira para a direção da velocidade (sem shuffle/backpedal), sem animação | Parcial (placeholder) |
| Consegue driblar? | Oscilação visual da bola; bola continua `Held`; sem risco/roubo | Visual apenas |
| Bola com física convincente? | Rigidbody quando livre; **não existe colisor de aro** — só uma esfera trigger. Não há quique no aro | Parcial |
| Consegue passar? | Passe em arco — mas o único alvo é o **adversário** | Mecânica existe, sem uso real em 1v1 |
| Consegue arremessar? | Arco calculado para o **centro do aro** + erro aleatório minúsculo (~0,09 m/s). Distância, contest e timing não influem | Quase sempre acerta |
| A cesta funciona? | Trigger esférico de raio 0,3 m sem checagem de direção (bola subindo também conta) | Frágil |
| O defensor defende? | IA se aproxima (ContestShot) mas **o contest não afeta o arremesso**; não há steal/block/pulo | Não |
| O rebote funciona? | Quem estiver a ≤1 m da bola livre pega primeiro | Mínimo |
| A IA consegue jogar? | Pega a bola e arremessa **de onde estiver**; cooldown de 4 s evita loop | Mínimo |
| Marca pontos? | Sim, mas **toda cesta é creditada ao "home"** (inclusive as da IA), sempre 2 pontos | Bug |
| Começa e termina / reinicia posse? | Fase volta para `Live` imediatamente; bola não é resetada, não há check-ball, não há fim de partida | Não |

**Conclusão:** o slice é uma boa *fundação técnica* (assemblies, testes, configs), mas ainda **não responde "sim"** à maioria das perguntas do item 37. Isso é esperado para uma primeira iteração — não é motivo para refazer, e sim para consolidar antes de expandir.

---

## 4. Problemas encontrados

### 4.1 Críticos (bloqueiam expansão sem retrabalho)

| # | Problema | Onde | Por que é crítico |
|---|---|---|---|
| C1 | **Arquitetura rigidamente 1v1.** `GameBootstrap` tem campos `humanMotor`/`aiMotor`; `AIPerception` tem um único `OpponentPosition`; passe tem um único alvo possível | `GameBootstrap.cs`, `AIPerception.cs` | 3v3/5v5, companheiros, Modo B e troca de jogador exigem N agentes em 2 times. Cada sistema novo escrito sobre 1v1 vira retrabalho |
| C2 | **Estados de voo da bola duram zero frames.** `Release()` faz `Held → Passing/Shooting → Free` na mesma chamada | `BallController.Release` | Regras (2/3 pts, goaltending, shot clock, bloqueio, "passe não pontua", atribuição de rebote ofensivo/defensivo) precisam saber que "há um arremesso em voo, de quem, de onde". Hoje qualquer bola livre que entra no trigger pontua |
| C3 | **Cesta sem geometria física e sem detecção direcional** | `SceneAssembly.BuildRimAndBackboard`, `ScoreTrigger` | Física de bola é prioridade #3 do briefing; sem aro real não há quique no aro, rebote crível, nem "quase entrou" |
| C4 | **Package com caminho absoluto `D:/Downloads/...`** | `Packages/manifest.json` | Qualquer outro clone (outra máquina, CI, nuvem) falha ao resolver packages. Impede CI e colaboração |
| C5 | **`GameBootstrap` acumula papel de loop de simulação** (tick de IA, motores, drible, passe, arremesso, pickup) | `GameBootstrap.Update` | Tende a virar o "GameManager gigante" que o briefing proíbe assim que houver 6–10 agentes. Precisa de um `MatchSimulation`/`AgentRunner` separado do composition root |

### 4.2 Importantes (resolver na próxima iteração)

| # | Problema | Onde |
|---|---|---|
| I1 | Placar sempre credita "home" e sempre 2 pontos | `MatchManager.OnBallScored` |
| I2 | Reinício de posse não reposiciona bola/jogadores; não há fim de partida (score alvo / tempo) | `MatchManager`, `MatchState` |
| I3 | Arremesso ignora distância, contest, timing e atributos (`defaultShooterRating` único) | `ShootingSystem`, `ShotMath`, `ShotConfig` |
| I4 | Qualquer contato de jogador com bola `Free` = recepção (inclusive arremesso em voo tocando defensor → "roubo" acidental) | `BallController.OnCollisionEnter` |
| I5 | Input com teclas fixas no código; `InputSystem_Actions.inputactions` existe mas não é usado → sem rebinding, sem buffer | `HumanInputProvider` |
| I6 | `IPlayerAgent` é uma coleção de perguntas `Wants*()`; `AIOpponentController.WantsShoot()` tem efeito colateral (grava cooldown ao ser consultado) | `IPlayerAgent`, `AIOpponentController` |
| I7 | Números mágicos na IA (2,5 m, 1,3 m, 0,2 m, 4 s) e no passe (apex 1,2) fora de SO | `OpponentAIStateMachine`, `AIOpponentController`, `PassSystem` |
| I8 | Bola "na mão" fica a ~2,1 m do chão, no centro do corpo (offset relativo ao centro da cápsula, não a um socket de mão) | `BallController.LateUpdate`, `BallConfig.handHeightOffset` |
| I9 | Nenhuma layer/tag (Ball, Player, Court, Hoop) — matriz de colisão e raycasts futuros dependem disso | `TagManager.asset` |
| I10 | Teste de integração só verifica "5 s sem exceção"; nenhum teste do loop "arremesso → cesta → placar → reinício" | `VerticalSliceIntegrationTests` |

### 4.3 Menores / higiene

- `SampleScene` e `TutorialInfo` (template) ainda no projeto e em Build Settings índice 0.
- Spec do vertical slice com versão de Unity desatualizada e referência a `roadmap-mestre.md` inexistente.
- Modelo Tripo3D importado como **Generic**: para retarget de animações humanoides (Mixamo, mocap) precisa ser **Humanoid**. Verificar também os termos de licença do asset gerado (o briefing exige não depender de material de terceiros sem autorização).
- Sem CI: os testes só rodam na máquina do desenvolvedor.

---

## 5. Avaliação do briefing

| Categoria | Nota | Justificativa curta |
|---|---|---|
| Clareza | **7** | Intenção e prioridades muito claras. Perde pontos por: dois roadmaps que conflitam (Fases 1–10 põem personagens antes de 3v3; Etapas 0–10 põem 3v3 antes de personagens), numeração quebrada (listas numeradas continuando a contagem das seções) e muitas listas "considere X" sem dizer o que é obrigatório |
| Especificidade | **6** | Excelente em *o quê*, fraco em *quanto*: não há metas mensuráveis (latência de input, FPS alvo, % de acerto esperado por zona/atributo, duração de partida, escala de atributos 0–99?) |
| Arquitetura | **7** | Bons princípios (composição, data-driven, sem God Object, sem singleton). Faltam decisões estruturais que definem tudo: separação **simulação × apresentação**, modelo de tick (fixo? determinístico?), **definição × instância** de personagem, pipeline de modificadores de atributo, modelo formal de habilidade |
| Modularidade | **8** | Pede explicitamente interfaces, SO, sistemas desacoplados e separação das camadas do Gacha. Bem coberto |
| Testabilidade | **6** | Pede testes e cenas de teste, mas não pede CI, execução headless, seeds determinísticas, nem **simulação IA×IA em massa** para balanceamento — a ferramenta mais útil para um jogo com atributos/habilidades |
| Escalabilidade | **5** | Multiplayer online "indefinido" + Gacha + economia é o maior risco do projeto: gacha e inventário confiáveis exigem servidor autoritativo; gameplay online exige simulação determinística/autoritativa. Decidir tarde custa reescrever. Também faltam: pipeline de conteúdo (Addressables), versionamento/migração de save, localização |
| Cobertura de gameplay | **8** | Muito completa. Faltam: jump ball/tip-off, lance livre (mecânica), modelo de contato/falta (como detectar), screens como mecânica do jogador (só aparece na IA), post-up, input buffering e janelas de cancelamento de animação, stamina/fadiga e substituições no 5v5 |
| Cobertura de IA | **7** | Boa lista de comportamentos e arquiteturas. Faltam: quem coordena o time (blackboard de equipe / "coach"), como a IA avalia *custo × benefício* de habilidades, formato de dados das táticas, orçamento de CPU por agente, o que a IA faz no Modo B, e como medir "dificuldade por decisão" |
| Cobertura de progressão | **7** | Nível 60, Limit Breaks e dupes bem pedidos como framework. Ambiguidades: "1→20, 20→40, 40→50, 50→60" são 4 *faixas* — o LB acontece ao **atingir** 20/40/50 para destravar a próxima faixa? Então o "4º LB" em 60 destrava o quê? Faltam curva de XP, teto de atributos, interação dupes × LB, e como progressão afeta PvE/PvP (power creep) |
| Capacidade de expansão | **8** | O briefing inteiro é orientado a crescer sem reescrever. Fica 8 e não 9 pelas decisões de rede/servidor não tomadas (ver Escalabilidade) |

**Média ≈ 6,9.** É um briefing forte em visão e princípios; as lacunas são de *mensuração* e de *decisões estruturais caras de mudar depois*.

### 5.1 Lacunas e correções propostas

| Lacuna | Correção proposta |
|---|---|
| Dois roadmaps conflitantes | Adotar **um** roadmap mestre (seção 7 abaixo) e marcar o outro como obsoleto |
| "Controle estratégico do time" (Modo B) indefinido | Registrar como **decisão pendente** com opções: (a) troca de jogador controlado estilo NBA 2K + chamadas de jogada; (b) comando tático em tempo real (pausa/ordens); (c) "manager" sem controle direto. Arquitetura provisória: todo agente é controlado por um `IAgentController`; Modo A/B só trocam *qual* controller cada agente usa + uma camada `TeamCommandSource` |
| Multiplayer online indefinido | Não decidir agora, mas adotar **desde já** o que é barato e mantém a porta aberta: simulação de gameplay separada da apresentação, tick fixo, comandos como dados (`PlayerCommand`), RNG injetável com seed. Gacha/economia projetados atrás de interface `IEconomyService` (implementação local agora, servidor depois) |
| Sem métricas de "feel" | Definir alvos: ex. input→resposta ≤ 100 ms, 60 FPS PC, % de acerto de 3PT para atributo 50/75/99, tempo de posse médio 3v3 |
| Escala de atributos indefinida | Decisão provisória: inteiro 0–99 por atributo, mapeado para gameplay via curvas (`AnimationCurve`) em SO — curvas, não fórmulas fixas no código |
| Definição × instância de personagem | `CharacterDefinition` (SO, estático) + `CharacterInstance` (save: nível, XP, LB, dupes) + `StatBlock` calculado (base + nível + LB + dupes + modificadores de habilidade/estado) |
| Modelo de habilidade | Habilidade = gatilho/condição + custo/cooldown + lista de efeitos (modificador de atributo, override de trajetória, animação, VFX, tag de estado). Data-driven, testável sem cena |
| Testabilidade de balanceamento | Adicionar requisito de **simulação headless IA×IA** com N partidas e relatório estatístico |
| Legal/compliance do Gacha | Adicionar requisito: exibição de taxas, pity transparente, regras por região (loot boxes), classificação etária. Não é decisão de código agora, mas afeta arquitetura (log de pulls auditável) |
| Faltantes gerais | Localização, acessibilidade, áudio (arquitetura), analytics/telemetria, versionamento de save |
| Ambiguidade dos Limit Breaks | Confirmar: LB em 20, 40, 50 destravam as faixas seguintes; o 4º "marco" em 60 é um *Awakening* (ou não existe)? Arquitetura provisória: lista configurável de `LimitBreakStage { levelCap, requisitos, recompensas }` — o número de estágios é dado, não código |

---

## 6. Arquitetura proposta (evolução do que existe — sem reescrita)

Princípio: manter o que funciona (assemblies, math puro, configs SO, `IPlayerAgent`) e corrigir os pontos C1–C5.

```
Core (contratos, sem dependência de Gameplay)
  TeamId, PlayerId, PlayerCommand (struct: move, sprint, action flags)
  IAgentController  -> produz PlayerCommand a partir de uma MatchSnapshot
  MatchSnapshot     -> visão read-only de N jogadores + bola + placar (substitui AIPerception 1v1)
  IStatSource       -> "qual o valor do atributo X deste jogador" (default: valores de config)
  Eventos: ShotReleased(ShotContext), BallScored(ScoreEvent), PossessionChanged, ...

Gameplay (simulação)
  PlayerEntity      -> agrega PlayerMotor + TeamId + IStatSource + socket de mão
  MatchSimulation   -> loop: coleta comandos -> aplica em motores/ações -> bola -> regras
  Ball: estados Free / Held / InFlight(Pass|Shot com ShotContext) / Loose
  HoopController    -> aro físico + detecção direcional (passou de cima para baixo pelo plano do aro)
  MatchRules (SO por modo: pontos por zona, alvo de pontos, check-ball, shot clock opcional)

AI
  AIController: IAgentController (FSM atual adaptada para MatchSnapshot)
  -> depois: Utility AI por decisão + blackboard de time

Bootstrap
  GameBootstrap volta a ser SÓ composition root (monta times/rosters a partir de um MatchSetup SO)
```

Decisões provisórias registradas (substituíveis):
- **Tick:** gameplay continua em `Update` com `dt` explícito agora; `MatchSimulation` recebe `dt`, o que permite migrar para tick fixo sem mexer nos sistemas.
- **Sem framework de DI** (mantém decisão da spec).
- **Atributos 0–99 + curvas em SO**, lidos via `IStatSource` — os sistemas passam a pedir o atributo em vez de ler `defaultShooterRating`. O sistema de personagens completo (Fase 5) só troca a implementação de `IStatSource`.

---

## 7. Roadmap técnico (único, substitui os dois do briefing)

| Etapa | Conteúdo | Saída verificável |
|---|---|---|
| **1.5 Consolidação** *(próximo)* | C1–C5 + I1, I2, I4, I6, I9, I10 | Slice 1v1 rodando sobre arquitetura N-agentes; loop cesta→placar→reinício testado |
| 2 Fundamentos | Arremesso por distância/contest/timing (I3), pulo, block, steal, rebote com box-out simples, bandeja/enterrada básicas, input via Actions (I5) | Cena de teste por fundamento + testes |
| 3 Regras 3v3 | Pontos 1/2, check-ball, clear-the-ball, alvo de pontos/tempo, shot clock opcional, faltas básicas | Partida 3v3 completa começa e termina |
| 4 IA 3v3 | Utility AI, blackboard de time, companheiros, marcação individual, ajuda | IA×IA roda N partidas headless com estatísticas plausíveis |
| 5 Personagens | Definição/instância, atributos com curvas, habilidades data-driven, níveis/LB/dupes | Dois personagens de arquétipos diferentes jogam *diferente* (medido em simulação) |
| 6 Animação | Humanoid, Animator com blend trees, sockets/IK de mão e bola | Placeholders cápsula substituídos sem mudar gameplay |
| 7 5v5 | Quadra inteira, transição, inbound, backcourt, IA de 5 | Partida 5v5 completa |
| 8 Meta | Inventário, save versionado, economia, Gacha atrás de `IEconomyService` | Testes de pity/taxas com seed |
| 9 História | Capítulos/diálogos/partidas-objetivo data-driven | Capítulo de teste com placeholders |
| 10 Polish | VFX anime, câmera cinemática de habilidade, UI final, áudio, otimização | — |

Personagens (5) vem antes de 5v5 (7) porque o comportamento da IA depende de atributos; 3v3 (3–4) vem antes de personagens porque é o menor contexto onde a IA de time pode ser validada.

---

## 8. Próximo passo proposto — Etapa 1.5 "Consolidação do Vertical Slice"

**Objetivo:** deixar o slice 1v1 respondendo "sim" ao item 37 sobre uma arquitetura que já aceita N jogadores em 2 times, sem adicionar features novas de gameplay.

**Escopo:**
1. Resolver o package Tripo (C4) — ver decisão pendente abaixo.
2. `MatchSnapshot` + `PlayerCommand` + `IAgentController`; adaptar `HumanInputProvider` e a IA (C1, I6).
3. `PlayerEntity` com `TeamId` e socket de mão; roster montado a partir de um `MatchSetup` SO (C1, I8).
4. `MatchSimulation` separado do `GameBootstrap` (C5).
5. Bola com estado `InFlight` + `ShotContext` (quem, de onde, tipo) (C2, I4).
6. Aro físico (anel de colisores) + detecção direcional de cesta (C3).
7. Placar por time e pontos por zona via `MatchRules` SO; reinício real de posse (check-ball simples) e fim de partida por pontuação alvo (I1, I2).
8. Layers/tags: Player, Ball, Hoop, Court (I9).
9. Teste PlayMode: arremesso → cesta → placar do time certo → posse reinicia (I10).
10. Atualizar spec (versão Unity) e criar `docs/roadmap-mestre.md` a partir da seção 7.

**Fora de escopo:** novos fundamentos (block/steal/pulo), personagens, animação, 3v3.

**Riscos:** mudança de `IPlayerAgent` → `IAgentController` toca todos os testes existentes; mitigado fazendo em passos pequenos, cada um com os testes verdes. Sem Unity no ambiente de nuvem, a validação de compilação/testes precisa rodar na sua máquina (ou em CI com licença Unity — ver decisões).

### Decisões pendentes (preciso da sua confirmação)

1. **Package Tripo3D:** (a) remover do manifest e manter só o modelo já importado *(recomendado)*; (b) copiar o package para `Packages/` como embedded; (c) manter como está (quebra outros clones).
2. **Aprovar a Etapa 1.5** como descrita, antes de avançar para Fase 2?
3. **CI:** configurar GitHub Actions com GameCI (exige secret de licença Unity) para rodar EditMode/PlayMode a cada push?
4. **Modo B e Limit Break** — não bloqueiam a 1.5, mas precisam de resposta antes das Etapas 4 e 5 respectivamente.
