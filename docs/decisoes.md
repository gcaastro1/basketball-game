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
| P-001 | Modo B (controle do time) | **Pendente** (antes da Etapa 4) |
| P-002 | Semântica dos Limit Breaks (o que o "4º LB" no nível 60 destrava) | **Pendente** (antes da Etapa 5) |

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
