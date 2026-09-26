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
