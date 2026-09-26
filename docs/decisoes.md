# Registro de decisões

Formato: contexto → opções → decisão → consequências. **Provisória** = pode (e deve) ser
revista quando a decisão de design correspondente for tomada; a arquitetura isola o ponto
de troca.

| ID | Decisão | Status |
|---|---|---|
| D-001 | Composition root (`GameBootstrap`) + interfaces + eventos C#, sem framework de DI | Aceita (spec do slice) |
| D-002 | Arquitetura de N jogadores: `MatchSnapshot` + `PlayerCommand` + `IAgentController` | Aceita |
| D-003 | Arena e jogadores placeholder construídos em runtime; cena contém só o bootstrap | **Provisória** |
| D-004 | Regras padrão: 2/3 pontos, 21 pontos, posse alternada, check ball simplificado | **Provisória** |
| D-005 | Estados de voo reais da bola + "release" que pode pontuar | Aceita |
| D-006 | Layers físicas `Player`, `Ball`, `Court`, `Hoop` (8–11) | Aceita |
| D-007 | CI em dois níveis: type-check sem Unity + GameCI com Unity | Aceita |
| D-008 | Tripo3D Bridge fica no manifest local; só o CI o remove | Aceita |
| D-009 | Botões contextuais (com bola: arremesso/passe; sem bola: pulo/roubo) + buffer de 0,15 s | Aceita |
| D-010 | Precisão = raio de erro de mira (m) no plano do aro, com multiplicadores independentes | **Provisória** (valores) |
| D-011 | Regras como dados (`MatchRules`) + árbitro (`MatchManager`) com peças puras; presets 1v1 genérico e FIBA 3x3 | Aceita |
| D-012 | Faltas por chance em situações de contato (reach-in, contato na soltura) | **Provisória** |
| D-013 | Marcação homem-a-homem fixa por reinício; só o mais próximo persegue bola solta | **Provisória** (até a IA tática, Etapa 4) |
| D-014 | IA em três camadas: estratégia do time → TeamBrain (ordens) → controlador por jogador (utility com a bola) | Aceita |
| D-015 | Atributos: enum fixo (só acrescenta), efeito "centrado" no neutro 70, faixas em `AttributeTuning` | Aceita |
| D-016 | Personagens em assembly próprio (`Basket.Characters`), dados em SO, instância = dado de save | Aceita |
| D-017 | Quadra inteira = mesma simulação: `CourtConfig.fullCourt` espelha a cesta; cada `HoopController` sabe quem o ataca; regras de meia-quadra/linhas/bola ao alto são flags de `MatchRules` | Aceita |
| D-018 | Passe "passa" pelo defensor colado ao passador (todo o voo) e só o recebedor pega com o raio cheio; interceptar exige estar na linha do passe | **Provisória** (valores) |
| D-019 | Visual em assembly próprio (`Presentation`) que só lê o gameplay; modelo Humanoid; animação procedural por músculos até existirem clipes; clipes por Playables (sem Animator Controller) | Aceita (valores do procedural: **provisórios**) |
| D-020 | Meta em assembly próprio (`Basket.Meta`, só Core + Characters): dados (SO), regras puras e serviços separados; apresentação fora; save versionado com checksum, escrita atômica e backup | Aceita |
| D-021 | Ligação do meta ao jogo (Etapa 8.5): `IPlayerProfileReadOnly` em Core (UI continua só conhecendo Core, mesmo padrão do gameplay); `CharacterCatalog` novo para `characterId → CharacterDefinition`; coordenador `ProfileRuntimeService` é POCO em `Basket.Meta`, não lógica no `GameBootstrap` | Aceita |
| D-022 | Clipes reais + procedural híbridos (Etapa 6.5): locomoção por velocidade (parado/andar/correr/de costas) com playback na velocidade real; drible/segurar bola numa camada só da parte de cima (máscara), pernas continuam; procedural por cima só nas poses sem clipe; clipes ligados por script de editor (IDs internos do FBX) | Aceita (mapeamento pose → clipe: **provisório**, trocável no Inspector) |
| D-023 | Câmera de transmissão: atrás e acima do jogador seguido, sempre virada para a cesta atacada pelo time com a bola; vira suavemente (só no ângulo horizontal) quando o ataque troca de lado | Aceita (valores em `DefaultCameraConfig`: **provisórios**) |
| D-024 | Guarda defensiva por botão (Ctrl / LT, segurado, sem a bola): mais devagar (×0,75), sem sprint, de frente para a bola; IA entra em guarda ao marcar a bola a ≤ 3 m. Arremessador vira para a cesta. Velocidade 4,5 m/s (sprint ×1,45 = 6,5) | Aceita (valores: **provisórios**) |
| D-025 | Personagem padrão Banana Man (1,8 m, provisório); materiais convertidos para URP um a um. Starter Assets (controles 1ª/3ª pessoa) e Cinemachine ficam como referência: o jogo mantém motor/input/câmera próprios | Aceita |
| D-026 | Quadra NBA (a desenhada no piso do ginásio MarpaStudio): 28,65 × 15,24 m, aro a 1,6 m do fundo, linha de 3 a 7,24 m com cantos retos a 6,71 m. Ginásio e bola são só visuais (`ArenaDresser`), por cima dos colisores do placeholder | Aceita (escolha do usuário; valores em dados: **provisórios**) |
| D-027 | IA posicional: defesa individual com ajuda (nega a um passe da bola, recua para a linha de ajuda no lado fraco, presa ao seu homem) e zona (2-3 / 2-2 / 1-2 deslizando com a bola, um só defensor na bola), escolhida por posse (`zoneDefenseChance`); ataque espaçado longe da bola, na linha de 3 da quadra; a IA lê a linha de 3 com os cantos | Aceita (valores em `DefaultAIConfig`: **provisórios**) |
| D-028 | Medidor de arremesso (estilo NBA 2K): janela verde em volta do topo do pulo; soltar no verde = arremesso perfeito (sem erro de mira); o verde cresce com o atributo do arremesso e encolhe com marcação, distância e movimento | Aceita (pedido do usuário; tamanhos em `ShotConfig`: **provisórios**) |
| P-001 | Modo B (controle do time) | **Pendente** — ponto de encaixe pronto: `ITeamStrategy` (e `IAgentController`) |
| P-003 | Gacha definitivo (raridades, taxas, pity, custos, moedas) | **Provisória**: 3 níveis genéricos, 3/17/80%, pity 80 (soft 65, +6%), 50/50 com garantia, multi de 10 com garantia de nível 2 — tudo em `Data/Meta/StandardBanner.asset` |
| P-002 | Semântica dos Limit Breaks | **Provisória**: 4 LBs (20→40, 40→50, 50→60, "Awakening" no 60 sem novo teto), tudo em `DefaultProgressionConfig` |
| P-004 | `GameBootstrap` assume que o time do jogador é Home ao calcular a recompensa de partida | **Provisória** |

---

## D-002 — Arquitetura de N jogadores

**Contexto.** O primeiro slice era 1v1 no código: `GameBootstrap` tinha `humanMotor`/`aiMotor`,
a percepção da IA tinha um único oponente e o passe só podia ir para o adversário.
3v3, 5v5, companheiros e Modo B exigiriam reescrever tudo que fosse construído em cima.

**Opções.** (a) manter 1v1 e generalizar depois; (b) ECS/DOTS; (c) roster de `PlayerEntity` +
snapshot compartilhado + comandos como dados.

**Decisão.** (c).
- `PlayerEntity` (Gameplay): índice no roster, `TeamId`, motor, corpo.
- `MatchSnapshot` (Core): visão de todos os jogadores/bola/fase, atualizada in-place a cada tick, sem alocação.
- `IAgentController.Decide(snapshot, selfIndex) → PlayerCommand` (Core): humano, IA e testes roteirizados produzem o mesmo tipo de comando.
- `MatchSimulation` (Gameplay): loop snapshot → decidir → aplicar → pickup → regras. `GameBootstrap` voltou a ser só composition root.

**Consequências.** Modo A/B = qual controller cada slot usa. 1v1/3v3/5v5 = assets de `MatchSetup`.
Comandos como dados deixam a porta aberta para replay e rede sem decidir multiplayer agora.

## D-003 — Arena placeholder em runtime (provisória)

**Contexto.** A cena anterior era gerada por um menu do Editor e versionada. Mudar a estrutura
exigia regenerar a cena no Unity; sem Unity no ambiente de nuvem isso não é verificável.

**Decisão.** A cena `01_VerticalSlice_HalfCourt` contém só um `GameBootstrap` com referências
aos assets de config (e usa defaults de código se algum faltar). `PlaceholderArenaBuilder` e
`PlaceholderPlayerFactory` constroem quadra, aro físico, bola, luz, câmera e jogadores a partir
de `CourtConfig`/`MatchSetup`.

**Quando rever.** Quando existir arte de quadra/personagem: a arena vira cena/prefab autoral e o
bootstrap passa a receber `BallController`/`HoopController` já existentes. Jogadores continuam
sendo instanciados a partir do roster (isso é correto a longo prazo).

## D-004 — Regras padrão (provisória)

`DefaultMatchRules`: 2 pontos dentro / 3 fora do arco (6,75 m, FIBA), vitória com 21, posse
alterna após cesta, reinício após 1,5 s com check ball no topo do garrafão. Um preset FIBA 3x3
(1/2 pontos, 10 min, shot clock 12 s, "clear the ball") é **outro asset**, a criar na Etapa 3 —
não exige código novo para os pontos.

Segurança: se a bola sai da área de jogo (ex.: por cima das paredes placeholder), o outro time
do último a tocar recebe check ball. Linhas reais de out of bounds e inbound são Etapa 3.

## D-005 — Estados de voo da bola

**Contexto.** `Release()` fazia `Held → Passing/Shooting → Free` na mesma chamada: regras não
sabiam que havia um arremesso no ar; qualquer bola livre que entrasse no trigger pontuava; um
defensor encostando num arremesso o "pegava".

**Decisão.**
- `Passing`/`Shooting` duram até o primeiro contato (→ `Free`). Passe pode ser pego no ar (interceptação); arremesso não.
- A bola guarda a "release" (quem, time, posição dos pés, tipo) até tocar o chão, ser pega ou ser resetada. Só uma release viva pontua — então bola que bate no aro e cai conta; bola que quicou no chão não.
- Aro físico (anel de 16 cápsulas de 1 cm) + `HoopController` que detecta a bola cruzando o plano do aro **de cima para baixo por dentro** (`HoopMath`).
- A velocidade de lançamento é corrigida para o integrador de passo fixo + amortecimento da física (`TrajectoryMath.ComputeCompensatedArcVelocity`); sem isso arremessos de 3 caíam ~20–35 cm curtos e batiam no aro da frente sempre.

## D-007 — CI

- **`typecheck`** (sempre): compila todos os assemblies com Roslyn/Mono contra DLLs de referência da Unity 2021.3 + stubs (`tools/typecheck`). Pega erros de sintaxe/tipo/referência entre assemblies. Não roda testes.
- **`unity-tests`** (GameCI): EditMode + PlayMode reais. Precisa dos secrets `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD` (ver `docs/ci.md`); sem eles, é pulado com aviso.

## D-008 — Tripo3D Bridge

O package aponta para `file:D:/Downloads/...` (máquina do desenvolvedor) e é usado para importar
modelos. Continua no `manifest.json`. O job de CI remove essa dependência **apenas na cópia do
runner**; nenhum código do projeto depende dela.

## D-009 — Input contextual

Dois botões de ação cobrem o que o jogador precisa em cada situação (padrão do gênero):
com a bola, *Primary* = arremesso (segurar/soltar) e *Secondary* = passe; sem a bola,
*Primary* = pulo (bloqueio/rebote) e *Secondary* = roubo. Ações definidas em código
(`BasketInputActions`), remapeáveis em runtime. Um aperto fica "guardado" 0,15 s
(`InputBuffer`), e o buffer é descartado quando o contexto muda (um roubo não vira passe).

## D-010 — Modelo de precisão (provisório nos valores)

O arremesso não sorteia "cesta/erro". Ele sorteia **para onde a bola é mirada**: um ponto
num disco no plano do aro, com raio = base(tipo, distância) × timing × contest × movimento ×
rating. A física decide o resto (aro, tabela, rebote). Vantagens: resultados visualmente
coerentes, cada fator testável isoladamente, e o `rating` vira atributo de personagem na
Etapa 5 sem mudar a fórmula. Os números em `DefaultShotConfig` são estimativas iniciais; o
HUD mostra o relatório de cada arremesso para calibrar.

## D-011 — Regras como dados

`MatchRules` descreve pontos, relógios, prorrogação, shot clock, "limpar a bola", tipo de
reinício após cesta e faltas/lances livres. `MatchManager` é o árbitro: recebe **fatos** da
simulação (posse, toque no aro, arremesso solto, cesta, falta, estado da bola) e responde com
eventos (reiniciar posse de um tipo, preparar lance livre). Relógio de jogo, shot clock e a
tabela de penalidades (`FoulRules`) são peças puras testadas isoladamente.
Presets: `DefaultMatchRules` (1v1 genérico, sem relógios) e `FIBA3x3MatchRules`. 5v5 será
outro asset; o que 5v5 exigir além disso (backcourt, 8 s, reposição lateral) entra como campo
novo, não como `if (modo == 5v5)` espalhado.

## D-012 — Faltas (provisória)

Sem animação/colisão de corpo confiável ainda, falta é **chance configurável em situações de
contato**: roubo errado (reach-in) e defensor colado no arremessador no momento da soltura
(no ar ou chegando rápido). Quando houver animação e hitboxes (Etapa 6), a detecção passa a
ser por contato real; o `MatchManager` não muda, porque recebe só o `FoulEvent`.

## D-014 — IA de time

1. `ITeamStrategy` escolhe a jogada da posse (Spacing / PickAndRoll / Isolation); `AutoStrategy`
   usa pesos do `AIConfig`.
2. `TeamBrain` (um por time, 10 Hz) dá uma ordem por jogador: Handle, Space, Cut, Screen, Roll,
   Crash, Guard, Help, BoxOut. Mantém o próprio mapa de marcação para trocar em bloqueios.
3. `AIAgentController` executa a ordem pelos mesmos comandos do humano; com a bola decide por
   utility (`BallHandlerDecision`): arremessar / passar / infiltrar / segurar, a partir da
   **leitura da IA** (`TeamMath.ShotValue`, `PassSafety`, `DriveLaneOpen`) — não da fórmula real
   de acerto.

Sem `TeamBrain` (1v1) o controlador joga sozinho como antes. O time do humano também tem um
`TeamBrain`: os companheiros de IA espaçam, cortam e bloqueiam para o jogador.

**Modo B (P-001) continua pendente**, mas não bloqueia: qualquer das opções entra como
`ITeamStrategy` (técnico que chama jogadas) e/ou `IAgentController` (trocar o jogador
controlado), sem mexer no restante.

## D-015 — Atributos

26 atributos do briefing num `enum` com números fixos (novo atributo = novo valor no fim;
nunca reordenar). Cada atributo tem efeito real no gameplay ou na IA (tabela em
`docs/etapas/etapa-5-personagens.md`). Os efeitos são **centrados no valor neutro (70)**: um
jogador com 70 em tudo — ou sem personagem — joga exatamente como os valores base dos configs;
acima/abaixo, multiplicadores vão até os extremos definidos em `AttributeTuning` (0 e 99).
Isso mantém os configs de gameplay como "o jogador médio" e deixa o balanceamento de
personagens independente.

## D-016 — Personagens

`Basket.Characters` depende só de Core: `CharacterDefinition` (dados estáticos: atributos base,
crescimento, habilidades com LB de desbloqueio, tendências de IA), `ProgressionConfig` (XP,
tetos, Limit Breaks, 6 dupes), `CharacterInstance` (o que o jogador possui: nível, XP, LB, dupes
— é o dado que o save guardará na Etapa 8). Habilidades: framework data-driven
(passiva sempre/condicional, ativa com cooldown/duração, efeitos em atributos e no arremesso,
nível pela progressão). Personagens e habilidades de exemplo são **placeholders** — nomes,
raridades e o conjunto definitivo de habilidades continuam decisões em aberto (briefing, seção 3).

## D-017 — Quadra inteira (Etapa 7)

Nada de "modo 5v5" separado: `CourtConfig.fullCourt` faz o `PlaceholderArenaBuilder` espelhar
a cesta no meio-campo (z = depth/2), e `MatchSimulation` atribui a cada `HoopController` o time
que o ataca (troca após `switchSidesAfterPeriod`). A cesta credita **o time que a ataca** e
pontua pela distância até **ela**. Reposições (fundo, lateral, meio, bola ao alto), 8 s,
backcourt e linhas são flags/campos de `MatchRules` com o árbitro (`MatchManager`) decidindo.
Presets: `Court5v5Config`, `FIBA5v5MatchRules`, `MatchSetup5v5`, cena `02_FullCourt_5v5`.
Fora do escopo por ora: pressão em quadra inteira, substituições/exclusão por 5 faltas,
tempos técnicos, seta de posse alternada.

## D-018 — Passe e defensor colado (provisória nos valores)

A simulação IA×IA mostrou quase todos os passes virando turnover: o defensor da bola fica colado
no passador e a bola, ao sair da mão, já estava no raio de pegada (1 m) dele. Jogadores a até
`BallConfig.passProtectRadius` (1,8 m) da soltura (o marcador do passador) **não tocam naquele
passe durante todo o voo** — ele vai por cima/ao lado deles. Uma janela de só 0,2 s não bastou: o
log por passe mostrou 8 de 10 passes perdidos pegos por um defensor a 0,7–1,1 m do passador, na
linha do passe. Durante o voo, quem não é o recebedor só pega a bola com `interceptRadius`
(0,5 m) — interceptar exige estar na linha do passe longe do passador (ex.: o marcador do recebedor).

Também: o passe sai da mão (não do ponto do quique do drible, que o fazia bater no chão na hora), o
recebedor **vai ao encontro** da bola em voo em vez de seguir a ordem do time (passes com antecipação
para quem mudou de direção caíam sozinhos), e a antecipação máxima caiu de 4 m para 2 m.

## D-010 (revisão) — Precisão calibrada no aro físico

`ShotCalibrationTests` mediu no Unity real: sem desvio de trajetória (mirando no centro, a bola
cruza o centro do aro, < 1 mm, de 4,2 a 7,5 m); a curva física acerta 100% até ~9 cm de desvio,
~63% a 12 cm, ~13% a 15 cm e 0% a partir de 20 cm — equivale a um raio efetivo de 0,124 m
(`ShotConfig.calibratedMakeRadius`), com chance ≈ (0,124 / raio de erro)².

Antes, um três livre e parado de um arremessador médio tinha raio 0,136 m (~83%), mas contestação
(×2) e movimento (×1,5) empilhavam até ~0,27 m (~21%) — a IA errava quase tudo. Novos valores
(provisórios): base 0,165 + 0,012/m, contestação +50% no máximo, movimento +30%, lance livre 0,18,
bandeja 0,10. Metas (média, soltura perfeita), fixadas em `ShotAccuracyModelTests`: três livre ~40%,
meia distância ~50%, lance livre ~74%, três totalmente contestado ~18%, três em movimento ~23%;
o melhor arremessador (nota 1,0) ~70% livre, o pior ~13%.

**Rodada 2 (dados do CI com o log por arremesso):** os lances livres bateram exatamente com o
modelo (0,144 m, 3/3), mas um três livre e com soltura perfeita saía com 0,26–0,27 m: ×1,3, a
penalidade máxima de movimento, porque a IA pulava direto da corrida. A IA agora **firma os pés**
antes do jump shot (`AIConfig.setFeetSpeed` = 1 m/s; bandejas e enterradas mantêm o embalo).
Bandejas: o arco baixo tem curva própria no aro (100% até 12 cm, 13% a 15 cm → raio efetivo
0,141 m, `calibratedLayupMakeRadius`); `layupBaseError` 0,10 → 0,19 para bandeja livre ~86%,
meio contestada ~55%, totalmente contestada ~38% (antes entrava sempre).

## D-019 — Modelos e animação (Etapa 6)

**Contexto.** O modelo do Tripo3D veio como rig Generic, sem animações, com material do shader
Standard (Built-in; rosa no URP). A regra da etapa: trocar os placeholders sem mudar o gameplay.

**Decisão.**
- `Basket.Presentation` (depende de Core/Characters/Gameplay) **só lê** o estado de jogo. O corpo de
  gameplay (CharacterController, motor) continua o mesmo; o modelo é um filho sem colisores.
  `CharacterVisualTests` prova que um arremesso sai idêntico com e sem modelo.
- Modelos em `Assets/TripoModels` são importados como **Humanoid** (`TripoHumanoidImporter`); os nomes de
  osso do Tripo mapeiam automaticamente. Teste de importação no CI.
- Sem clipes: animação **procedural** no espaço de músculos humanoides (`HumanPoseHandler`) — funciona em
  qualquer humanoide. Poses por estado de jogo (corrida, drible, posse, defesa, arremesso até a soltura,
  bandeja, enterrada, passe, toco, comemoração). Valores provisórios, numa única tabela.
- Com clipes (`CharacterVisualDefinition.clips`, idle + run no mínimo): **Playables** (mistura idle/run
  pela velocidade + clipe da pose por cima, crossfade). Sem Animator Controller: o mapeamento é dado.
- **IK de mão** (dois ossos) depois da pose: as mãos vão até a bola, que continua onde o gameplay a põe.
- Material: o padrão do pipeline ativo (o mesmo dos primitivos) com a textura do modelo.
- Todos os personagens de exemplo usam o mesmo modelo por ora; um anel na cor do time (amarelo para o
  humano) sob os pés mantém os times legíveis.

**Rodada 3 (rastro por arremesso):** um arremessador sozinho, sem defesa, bate o modelo (8/25 vs 9,1
esperado; a bola sai exatamente de onde a mira foi calculada e os erros são aro, como na curva). A diferença
nos jogos IA×IA vinha das **paredes** do arena placeholder, que ficam sobre as linhas: arremessadores nos
cantos/alas (8–8,5 m) seguravam a bola parcialmente dentro da parede e o arremesso morria nela (4 de 11 bolas
longas no 3v3). Arremessos e passes agora saem de um ponto livre de cenário (`BallController.ClearOfScenery`).
`LiveShotTests` fica como regressão de "arremesso ao vivo = modelo".

## D-020 — Meta game (Etapa 8)

O briefing pede separar sistema técnico do gacha, dados de banner, economia, apresentação e regras de
obtenção. `Basket.Meta` depende só de Core e Characters (não conhece gameplay nem UI):
- **Dados** em ScriptableObjects (itens, catálogo, banners, regras de obtenção, recompensas).
- **Regras puras** testáveis sem Unity (`GachaEngine` com RNG injetável, `CharacterObtainer`,
  `CharacterUpgrades`, `SaveMigrator`).
- **Serviços** que juntam as partes (`EconomyService`, `GachaService`, `SaveService`, `PlayerProfile`).
- O **inventário** implementa `IItemWallet`, então os Limit Breaks da Etapa 5 gastam materiais reais.
- **Save**: dados do jogador separados da configuração (o save guarda ids); envelope com versão e
  checksum; escrita atômica com backup; migração por versão; save de build mais nova nunca é sobrescrito.

## P-003 — Gacha (provisória)

Raridades, taxas, pity, moedas e custos são decisões em aberto (briefing, seção 3). Os valores atuais
são placeholders coerentes com o gênero e estão **só em dados**; mudar qualquer um não exige código.

## D-021 — Ligação do meta ao jogo (Etapa 8.5)

`docs/proximos-passos.md` (item A) pedia ligar `Basket.Meta` (Etapa 8, já testado) ao fluxo real de
partida. Três pontos exigiam decisão:

- **UI ↔ Meta.** Opção descartada: `Basket.UI` passar a referenciar `Basket.Meta` direto — seria a
  primeira exceção à regra "AI/Input/UI só conhecem Core". Decisão: estender o padrão já usado pro
  gameplay (`IMatchState`, `IBallStateReadOnly`) — nova interface `IPlayerProfileReadOnly` em `Core`,
  implementada por `PlayerProfile`. A UI continua só conhecendo Core, sem exceção.
- **Catálogo de personagens.** Não existia `characterId → CharacterDefinition` confiável em runtime
  (`BannerDefinition.pool` só cobre quem está em algum banner ativo). `CharacterCatalog` novo (SO),
  espelhando `ItemCatalog`.
- **Onde mora o coordenador novo.** `ProfileRuntimeService` é POCO dentro de `Basket.Meta` (construtor
  com injeção, sem `MonoBehaviour`, testável sem cena — mesmo molde de `SaveService`/`EconomyService`),
  não lógica nova dentro do `GameBootstrap`. O Bootstrap ganha uma referência sancionada a `Basket.Meta`
  (antes ausente de propósito) e só instancia/chama nos pontos certos (início, fim de partida, saída).

Plano completo: `docs/etapas/etapa-8.5-meta-consolidacao.md`.

## P-004 — GameBootstrap assume que o time do jogador é Home (provisória)

**Contexto.** `GameBootstrap.cs`, no lambda de `Simulation.Match.OnMatchEnded`, calcula
`ownScore`/`opponentScore` para `ProfileRuntimeService.ApplyMatchReward` usando
`finalState.ScoreHome` como "meu placar" e `finalState.ScoreAway` como "do adversário" — sem
checar de qual time é, de fato, o slot humano. Hoje isso é inofensivo porque todo `MatchSetup`
usado em jogo real (`MatchSetup3v3`, `MatchSetup5v5`) põe o humano no time Home, mas a suposição
não estava registrada em lugar nenhum (achado M1 da revisão final da Etapa 8.5).

**Decisão.** Fica como está por ora — `ScoreHome` = "own", `ScoreAway` = "opponent" —, documentado
aqui como provisório. Quando existir a possibilidade real do humano jogar pelo time Away
(`MatchSetup` customizado, Modo B — P-001, multiplayer), `GameBootstrap` precisa calcular
`ownScore`/`opponentScore` a partir do time de fato controlado pelo jogador (ex.: o `TeamId` do
primeiro slot humano em `matchSetup.slots`), não de um lado fixo.

**Consequências.** Nenhuma mudança de comportamento agora. Quem alterar `MatchSetup` para pôr o
humano no time Away sem ajustar essa suposição em `GameBootstrap` vai inverter silenciosamente
vitória/derrota nas recompensas de partida — ponto de atenção para quem tocar isso antes desta
decisão ser revisitada.

## D-022 — Clipes reais + procedural híbridos (Etapa 6.5)

**Contexto.** Chegaram clipes reais (Mixamo: Idle, Offensive Idle, Walking, Running, Running
Backward, Dribble; Universal Animation Library, CC0) mas não os de arremesso/passe/bandeja/enterrada
(pacote a comprar). O backend de clipes da Etapa 6 era tudo-ou-nada e trocava o corpo inteiro por
ação (um drible pararia as pernas).

**Decisão.**
- Camada 0 (corpo inteiro): locomoção misturada por velocidade (`LocomotionBlend`, puro): parado →
  andar → correr, e "de costas" conforme o movimento se afasta da frente do corpo; playback escalado
  para os pés andarem na velocidade real (0,6–1,6×). Clipes de ação de corpo inteiro por cima.
- Camada 1 (máscara: tronco, braços, cabeça): drible e segurar a bola sobre a locomoção.
- Poses sem clipe continuam procedurais, misturadas por cima dos clipes (`ProceduralHumanoidAnimator.Apply(..., weight)`).
- Importação (`CharacterAnimationImporter`): loops para idle/andar/correr/drible/`*_Loop`; rotação e
  altura na pose; movimento horizontal fica na raiz e nunca é aplicado (no lugar).
- Ligação dos clipes (`CharacterClipBinder`): os clipes dentro do FBX têm IDs gerados pelo Unity, então
  um script de editor preenche os slots vazios do `DefaultCharacterVisual` (ao importar, ao abrir o
  editor e pelo menu **Basket → Bind Character Animations**); o que for posto no Inspector prevalece.

**Consequências.** O pacote comprado entra só preenchendo slots (`jumpShot`, `layup`, `dunk`, `pass`,
`block`...), sem código. Gameplay continua sem ler nada da apresentação.

### D-022 (adendo) — Animações de basquete (captura de movimento)

**Contexto.** Chegaram 69 clipes de basquete em `Animations/Basquete` (captura de movimento, um só
esqueleto `CharacterArmature`, mapeado como Humanoid): arremessos, bandeja, lance livre, dribles em
várias direções, crossovers/giros/fintas, deslizes de defesa, corridas e sinais de árbitro. São longos
(o jump shot tem 6,5 s, com segundos de parado e drible antes do arremesso).

**Decisão.**
- Pose própria para lance livre (`AnimPose.FreeThrow`; proceduralmente igual ao jump shot).
- Arremessos (jump shot, bandeja, enterrada, lance livre) tocam **em sincronia com o arremesso do
  jogo**: o progresso do arremesso (0 = início, 1 = soltura no ápice) leva o clipe do agachamento
  (`start`) até a soltura (`release`), numa janela por clipe (`ClipWindow`, `ActionClipTiming`); depois
  da soltura o clipe segue sozinho. Janelas medidas na altura do quadril de cada clipe (lendo as curvas
  do FBX): jump shot 0,464–0,526; bandeja 0,503–0,595; lance livre 0,824–0,905.
- Defesa: `defenseMove` (deslize lateral) quando o defensor se move, `defense` (postura) parado.
- Drible padrão: `basketball_forward_dribble_06_02` (drible em movimento, na camada de cima do corpo).
- Importação: clipe de um só take recebe o nome do arquivo; arremessos, fintas, giros, paradas e curvas
  não repetem.
- Os 69 FBX declaram "30 fps drop-frame" (TimeMode 7), que o Unity não suporta ("Framerate was set to
  0.00, it's been reset to 1.0"). `Editor/FbxFrameRateFixer.cs` troca para 30 fps (TimeMode 6, um byte,
  dados intactos) ao abrir o editor e reimporta; os arquivos corrigidos são commitados de uma máquina
  local (o ambiente do Claude na nuvem não consegue enviar arquivos novos ao Git LFS).

**Consequências.** Crossovers, giros, fintas e dribles laterais/de costas ficam disponíveis para uma
etapa de movimentos com a bola (hoje o jogo não tem esses comandos). Os valores das janelas são
ajustáveis no Inspector do `DefaultCharacterVisual`.

## D-023 — Câmera de transmissão

**Contexto.** Pedido do usuário: câmera "PRO" como nos jogos de basquete, acompanhando o jogador e
sempre virada para a direção do ataque.

**Decisão.** `CameraRigMath` (puro): direção = eixo da quadra rumo à cesta atacada, girada em direção ao
aro por `aimAtHoop` (0 = transmissão, 1 = atrás da linha jogador→aro); câmera `distance` atrás e
`height` acima do jogador, olhando um pouco à frente dele rumo à cesta (nunca além dela).
`CameraController` suaviza posição e rotação e vira só no ângulo horizontal. A cesta vem do
`GameBootstrap`: a atacada pelo time com a bola (a do time do jogador quando ninguém tem a bola).

**Consequências.** Em meia quadra (3x3) a câmera sempre olha para a mesma cesta; em quadra inteira ela
dá a volta quando a posse muda. Ajustes em `Data/DefaultCameraConfig.asset`.

## D-010 (revisão 2) — Arco do arremesso pelo ângulo de entrada

**Contexto.** Teste do usuário: "os arremessos estão indo muito alto". O arco era fixo: topo 3,5 m acima
do aro em qualquer distância (~6,5 m de altura, entrada a ~63°).

**Decisão.** `ShotArc`: o topo fica H = D·tan(θ)/4 acima do aro (D = distância horizontal), com θ =
`entryAngleDegrees` 47° (arremessadores reais: ~45°), entre `minArcHeight` 0,9 m e `maxArcHeight` 2,6 m:
~1,1 m no lance livre, ~1,9 m na bola de 3. Bandeja mantém `layupArcHeight`. Habilidades continuam
multiplicando o arco.

**Consequências.** Entrada mais rasa deixa o aro "menor" para a bola: a curva de acerto física
(`ShotCalibrationTests`) muda e os raios de erro/`calibratedMakeRadius` podem precisar de reajuste
com os números do CI (**provisório**).

## D-023 (adendo) — Movimento relativo à câmera

**Contexto.** No 5v5, quando a câmera vira para a outra cesta, "para frente" continuava sendo +z do
mundo: o jogador ia para o lado errado.

**Decisão.** `HumanInputProvider.SetView`: o direcional é girado pela direção da câmera no chão
(`CameraRelativeMove`); o `GameBootstrap` liga a câmera principal. "Para cima" é sempre para onde a
câmera olha (o passe mirado pelo direcional segue junto).

## D-022 (adendo 2) — Ritmo das animações

**Contexto.** "As animações parecem em 2x." O playback da locomoção acompanhava a velocidade do jogo
até 1,6× (jogador a 6 m/s, sprint a 9,6 m/s).

**Decisão.** Playback entre 0,8× e 1,15× da velocidade do clipe (pés podem deslizar um pouco em alta
velocidade, em vez de parecer acelerado) e `playbackSpeed` global no `CharacterAnimationClips` para
ajuste no Inspector. A velocidade de movimento do jogo (`maxSpeed` 6, `sprintMultiplier` 1,6) fica
como está até decisão do usuário.

## D-022 (adendo 3) — Só animações de basquete, em conjuntos

**Contexto.** Teste do usuário: "ainda está muito acelerado"; usar todas as animações da pasta
`Basquete` e apenas elas; andar em defesa estranho. Análise dos FBX: as takes declaram durações muito
maiores que o movimento (corrida: take de 88,5 s, curvas até 0,93 s; jump shot 11 s vs 6,6 s) e o
Unity dimensiona o clipe pela take (a corrida mexia 1 s e ficava parada 87); todo take começa com um
quadro de calibração.

**Decisão.**
- `FbxCurveSpan` (leitor de FBX binário) + importador v3: cada take de captura (`*_remap`) vira um
  clipe do primeiro ao último quadro-chave, sem o quadro de calibração, mais uma cópia espelhada
  (`<nome>_Mirror`) para o outro lado. Taxa "30 fps drop-frame" corrigida antes da importação.
- Clipes em três conjuntos de locomoção (sem bola / com bola / em guarda), cada um com parado,
  frente, costas, esquerda, direita, corrida e curvas de corrida (`LocomotionBlend` por velocidade,
  direção relativa ao corpo e giro); tempo de cada clipe controlado pelo código, com janelas de loop
  para usar trechos de tomadas longas (`LoopClip`).
- Arremessos com variações: parado (jump shot, shoot) e em movimento (drible+arremesso, crossover+
  arremesso), um por arremesso, em rodízio; bandeja; enterrada = bandeja; lance livre; pulo sem bola e
  toco = o pulo do jump shot. Janelas em segundos da gravação medidas nas curvas.
- Sem clipe na pasta: passe e comemoração (procedurais). Não usados ainda: fintas, giros, crossovers,
  drives, dribles com curva de 90°, pivô, sinais de árbitro, dança, calibração — pedem comandos novos
  (etapa de movimentos com a bola).
- Clipes do Mixamo e da UAL deixam de ser usados (continuam no projeto).

## D-025 — Banana Man e Starter Assets

**Contexto.** O usuário trouxe o Banana Man (modelo humanoide, materiais Body/Joints no shader
Standard antigo) e os Starter Assets da Unity (ThirdPersonController/FirstPersonController,
StarterAssetsInputs, Cinemachine).

**Decisão.**
- Banana Man vira o modelo do `DefaultCharacterVisual` (altura 1,8 m, provisória); os clipes de
  basquete continuam os mesmos (retargeting humanoide). `CharacterVisual` converte cada material para o
  Lit do URP mantendo textura e cor (antes: uma textura só para todos os materiais).
- Starter Assets **não** substituem o controle do jogo: o `ThirdPersonController` move o
  CharacterController direto a partir do próprio input e anima um Animator Controller próprio,
  enquanto aqui o `MatchSimulation` comanda o `PlayerMotor` com `PlayerCommand` de qualquer controlador
  (humano, IA, teste, futuro Modo B/rede) e a apresentação só lê o jogo. Usá-lo quebraria esse
  contrato. Ideias aproveitáveis: suavização de giro (SmoothDampAngle) e aceleração no motor;
  Cinemachine para a câmera (colisão, amortecimento) numa etapa de polimento.

## D-022 (adendo 4) — Ritmo do drible

**Contexto.** "Os personagens batem a bola numa velocidade muito rápida." O quique usava |sen| com
`dribbleFrequency` 2,2 "ciclos" por segundo — dois quiques por ciclo: 4,4 quiques/s. Nas gravações o
braço do drible faz ~1,0–1,4 ciclos/s (medido na rotação do antebraço). Além disso, o drible para
frente escolhido (`06_04`) é com a mão esquerda, e a bola fica na direita.

**Decisão.** `dribbleFrequency` passa a ser quiques por segundo (padrão 1,2, o das gravações); drible
para frente e da parte de cima do corpo = `06_05` (mão direita); drible para a esquerda = `06_08` de trás
para frente (espelhado, driblaria com a esquerda); o binder troca os clipes antigos também em assets já
ligados. `playbackSpeed` é lido a cada quadro (ajuste no Inspector durante o Play).

## D-026 — Quadra NBA, ginásio e bola reais (Etapa 6.6)

**Contexto.** O usuário trouxe dois pacotes: um ginásio (MarpaStudio "Basket Ball Stadium") e uma
bola (TierrasDeRol "Basketball", com materiais URP). O piso do ginásio é uma quadra NBA desenhada na
textura (15,24 × 28,65 m, linha de 3 a 7,24 m com cantos retos a 6,71 m); o jogo usava medidas FIBA
(15 × 28 m, linha de 6,75 m) e as linhas não bateriam. O usuário escolheu as medidas NBA.

**Decisão.**
- Dados (`Court5v5Config`, `DefaultCourtConfig` = meia quadra da mesma quadra, e os três
  `MatchRules`): quadra 15,24 × 28,65 m, aro a 1,6 m do fundo e a 3,05 m de altura, tabela
  1,83 × 1,07 m a 1,22 m do fundo, lance livre a 4,19 m do aro, linha de 3 a 7,24 m com cantos a
  6,71 m (`MatchRules.threePointCornerDistance`, 0 = só o arco). Os nomes dos assets de regras
  (FIBA...) não mudaram, para não quebrar referências. Os padrões **no código** continuam os FIBA
  (os testes com `CreateInstance` não mudam).
- Pontuação: `ScoringMath.IsBeyondArc` (arco + cantos retos) no placar e na posse. A IA e a escolha
  do atributo de arremesso continuam usando só a distância (aproximação: um arremesso de canto entre
  6,71 e 7,24 m é "de 3" no placar, mas a IA o avalia como meia distância).
- Visual (`Basket.Presentation`): `ArenaVisualDefinition` (`Data/Arena/DefaultArenaVisual.asset`) =
  ginásio + modelo da bola. O ginásio é um `StadiumLayout` (1307 peças: posição/rotação/escala e
  materiais de cada uma, relativas ao centro da quadra) extraído da cena de demonstração do pacote
  por `tools/stadium/extract_layout.py`; o jogo o monta ao iniciar, centrado em
  `CourtConfig.FullCourtCenter` (meia quadra: linha do meio em z = 0). As peças não ganham colisores,
  os materiais do shader Standard antigo viram Lit do URP (`LitMaterials`, compartilhado com o
  personagem) e o piso/linhas do placeholder somem (o colisor do piso continua).
- A bola do jogo ganha o modelo (`URPOrange`) escalado para o diâmetro físico (0,24 m); a esfera do
  placeholder some; física igual.
- Cestas: o `Ring.fbx` do ginásio tem aro de ~0,85 m de diâmetro a 2,89 m de altura (medido no CI),
  fora da regra; não dá para alinhar com o aro físico (0,457 m a 3,05 m). Usamos só as peças que
  servem, em volta do aro do jogo: a rede (`Net`) escalada para o diâmetro do aro e pendurada nele,
  o vidro da tabela (`RingGlass`, transparente no URP) com as linhas (borda e quadrado de
  0,61 × 0,457 m), um suporte do aro, e o poste acolchoado (`FoamFinal` + `FoamPoleFinal`) atrás da
  tabela com um braço até ela. O aro visível continua o do placeholder (laranja).

## D-027 — IA posicional (Etapa 4.5)

**Contexto.** "A IA parece um monte de formiga indo atrás de um pedaço de açúcar." Na defesa, todo
defensor sem a bola ficava a 1,5 m do seu homem, entre ele e a cesta, onde quer que estivesse a bola:
ninguém fechava o garrafão, e os arremessos saíam sempre contestados (log do 3x3 que falhou: 14
arremessos em 120 s, quase todos com o relógio de posse a ~2 s, contestação 0,6–0,8, muitos tocos).
No ataque, os pontos de espaçamento ignoravam onde estava a bola (companheiros colados no armador).

**Decisão.**
- Defesa individual "bola – você – homem" (`DefenseFormation.OffBallSpot`): a um passe da bola (até
  `denyDistance`), entre o homem e a cesta, um passo na linha do passe; mais longe, recua para a linha de
  ajuda (um ponto entre a cesta e a bola), no máximo `maxSag` do caminho e a `maxSagFromMan` do homem.
  Ordem nova `Position`; o defensor da bola continua `Guard`; a ajuda no drible continua.
- Zona (`DefenseScheme.Zone`, ordem `Zone`): formação por número de defensores (5: 2-3, 4: 2-2, 3: 1-2,
  2: 1-1) em volta da cesta, deslizando `zoneShift` em direção à bola; cada defensor fica com um ponto na
  posse; quem está mais perto da bola a marca, os outros marcam quem entra na sua área ou guardam o ponto.
  Escolhida pela estratégia (`ITeamStrategy.ChooseDefense`) a cada posse do adversário, com
  `zoneDefenseChance` (código: 0 = sempre individual; dados: 0,3, provisório). No rebote, a zona bloqueia
  o mais próximo.
- Ataque: pontos a `spacingBeyondArc` além da linha de 3 da quadra (7,24 m na NBA), os cantos dentro
  da quadra (no máximo `canto + spacingBeyondArc` para o lado), e só pontos a pelo menos
  `spacingMinFromHandler` de quem tem a bola. No 3x3, limpar a bola puxa para o topo do arco
  (`clearTowardTop`), não para fora do canto.
- A linha de 3 (arco + cantos) virou `Core.ThreePointLine`, usada pelo placar (`ScoringMath`) e pela IA
  (`MatchSnapshot.IsBeyondArc`): o arremesso do canto vale 3 para a IA também.

## D-022 (adendo 5) — Bola nas mãos, drible contínuo e no ritmo da bola

**Contexto (teste na quadra de treino).** "Segurando, a bola atravessa a mão e o personagem fica num
loop abaixando e subindo a cabeça"; "a bola quica numa velocidade real, mas o personagem está muito
rápido"; "quando está batendo a bola e para, ele não pode segurar de novo a bola"; "no arremesso a bola
deve estar na palma da mão, não na ponta dos dedos". Causas: a pose de segurar tocava o começo do lance
livre (os quiques de preparação); o IK puxava as mãos para a bola do jogo em todas as poses com bola,
inclusive no arremesso (o clipe quer as mãos acima da cabeça); o braço do drible corria no ritmo do
próprio clipe; e parar de andar virava "segurar a bola".

**Decisão.**
- Regra (`DribbleSystem`): depois do primeiro quique, o jogador continua quicando parado até arremessar
  (`PickUp` no início do arremesso) ou a bola sair dele. Segurar = só antes do primeiro quique.
- Drible: a camada de cima do corpo dribla sempre que há drible (parado, andando ou correndo) e segue a
  bola do jogo: a janela do clipe 06_05 passou a [0,88 s, 2,63 s], exatamente 2 quiques começando com a
  mão em cima da bola (picos de flexão do cotovelo direito em 0,90 / 1,73 / 2,63 s), e o tempo do clipe
  vem da contagem de quiques do jogo (`dribbleBouncesInWindow` = 2).
- Segurar: um quadro fixo do lance livre (3,8 s, pronto para arremessar), sem o balanço da cabeça.
- Bola nas mãos: com o modelo de bola (`BallVisualFollower`), segurando, a bola visual vai para entre as
  palmas e, no arremesso, para a palma da mão direita (virada para cima e para a cesta); os braços ficam
  como no clipe. Ao sair da mão, volta suavemente para a bola do jogo. A bola física não muda. Sem modelo
  de bola, o IK antigo continua.

## D-028 — Medidor de arremesso (janela verde)

**Contexto.** Pedido do usuário: "uma barra de força: quanto mais perto do verde, maior a chance de
acertar; no verde é um arremesso perfeito, 100%. O tamanho do verde depende de quão marcado está o
jogador e dos atributos relevantes para aquele arremesso (bandeja, 3 pontos, meia distância)."

**Decisão.**
- Arremessos cronometrados (arremesso e lance livre): a barra enche do salto até o topo do pulo; a janela
  verde é ±`GreenHalfWidth` em volta do topo. Soltar dentro dela = erro de mira 0 (a bola vai no centro
  do aro; no aro físico, até ~0,09 m do centro sempre entra). Fora dela, a penalidade de tempo conta a
  partir da borda do verde (antes: de uma janela fixa de ±0,05 s). Um toco ainda pode parar a bola.
- Tamanho (`ShotAccuracyModel.GreenHalfWidth`, dados em `ShotConfig`): interpola de
  `greenHalfWidthAtRatingZero` (4 ms) a `greenHalfWidthAtRatingOne` (18 ms) pelo atributo do arremesso
  (3PT além da linha, meia distância, arremesso curto, lance livre); vezes (1 − 0,7 × marcação), (1 − 0,4 ×
  velocidade), (1 − 6 %/m além de 4,5 m), nunca abaixo de 25 %. Bandeja e enterrada soltam sozinhas: sem
  medidor (pode vir depois).
- Tela: `ShotMeterView` ao lado do jogador humano. A barra sobe do salto até o topo no ápice do pulo, onde
  fica o verde; segurando além do ápice ela volta a descer (tarde). Depois de soltar, marca onde soltou e o
  resultado em 7 níveis (`ShotAccuracyModel.Grade`): muito cedo / cedo / pouco cedo / perfeito / pouco
  tarde / tarde / muito tarde — "pouco" até `slightTimingMargin` (0,03 s) além do verde, "muito" além de
  `timingMargin` (0,08 s). O evento do arremesso mostra o nível e o tamanho do verde.
- `useGreenWindow` = falso volta ao modelo antigo. A IA usa o mesmo verde (sua soltura tem uma variação
  que diminui com o QI ofensivo); isso aumenta um pouco o acerto dela nos arremessos livres.

## D-022 (adendo 6) — Parado sem bola e segurando a bola, escolhidos por cinemática

**Contexto.** "Após o arremesso o personagem fica com os braços pra cima." O parado sem bola era
`basketball_signals_32_07` [18 s, 20 s]: calculando a posição das mãos pela cadeia de ossos (script de
cinemática direta sobre os FBX), as duas mãos ficam acima dos ombros ali — um sinal de árbitro. E o
quadro de "segurar" (lance livre, 3,8 s) era o jogador já subindo para o arremesso.

**Decisão.** Varrer todos os clipes por quadros parados (quadril e mãos quase sem velocidade) com as
mãos mais baixas: parado sem bola = `basketball_signals_27_06` [3,45 s, 4,45 s] (braços soltos, mãos na
altura do quadril; o binder troca o 32_07 onde ainda estiver ligado). Segurar = lance livre em 0,2 s (em
pé, mãos a 0,23 m uma da outra — a largura da bola — na altura da cintura).

