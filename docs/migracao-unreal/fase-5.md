# Fase 5 — Arremesso com medidor, passe, drible e defesa

Repositório: `gcaastro1/basketball-unreal`. Pré-requisito: Fase 4 com o código completo (186 testes,
`net-smoke` 12/12; a checagem com janela da Fase 4 fica para a peça 7 desta fase, já com o Espaço
arremessando).

## Objetivo

O humano joga basquete de verdade com o próprio jogador, online: **Espaço / X (Quadrado)** arremessa
com o medidor (soltar no verde = sem erro de mira), bandeja e enterrada saem sozinhas no topo do pulo;
**E / A (X)** passa para o companheiro na direção do analógico; sem a bola, Espaço pula e E tenta o
roubo. O defensor que pula na frente de um arremesso pode dar o toco; contato no arremesso pode virar
falta e lance livre. A bola quica na mão de quem está driblando.

Tudo no servidor (autoritativo); o cliente só manda os botões. A IA continua fora (Fase 6): os testes
usam jogadores comandados por código.

## Referência no Unity

| Sistema | Unity | Valores (`referencia-unity.md`) |
|---|---|---|
| Arremesso | `ShotSystem` | apertar no chão com a bola → pula com a bola erguida; jump shot e lance livre soltam ao largar o botão (melhor no topo); bandeja e enterrada soltam sozinhas no topo; cair com a bola força a soltura (bem atrasada). Soltura do jump shot 2,1 m acima dos pés + pulo, 0,15 m à frente (D-030) |
| Medidor | `ShotAccuracyModel` (já em `BasketShotAccuracy`) | janela verde ±11–15 ms livre, ±3,6–4,4 ms marcado (atributo 0,75); fora do verde o erro cresce com o atraso |
| Marcação | `ContestMath` (já em `BasketDefense::Contest`) | o maior contest entre os adversários, no instante da soltura |
| Toco | `DefenseSystem.CheckBlocks` | adversário no ar, até 0,35 s após a soltura, bola dentro dos braços (cabeça 1,8 m até a ponta dos dedos + 0,12 m, 0,45 m para os lados) → bola desviada |
| Passe | `PassSystem` + `PassTargeting` (já em `BasketPassing::SelectTarget`) | alvo = companheiro mais alinhado com o analógico (o mais perto sem analógico); mira à frente de quem corre (tempo de voo × velocidade, limitado); arco do passe; quem não é o receptor só pega dentro do raio de interceptação |
| Drible | `DribbleSystem` + `DribbleMath` | a bola começa a quicar quando o portador anda e continua parado; volta às mãos no arremesso |
| Roubo | `DefenseSystem.TrySteal` | alcance, de frente para o portador, recarga entre tentativas, chance base + bônus se o portador corre; falha pode virar falta (reach-in) |
| Faltas | `MatchSimulation` + `FoulMath` (já em `BasketFouls`) | contato no arremesso (`ShootingContact`) com chance → falta com lances livres; o árbitro já resolve o resto |

## Decisão central: o verde com latência (D-032, aceita: opção A)

A janela verde inteira tem 25–29 ms livre e 7–9 ms marcada: menos de um quadro a 60 FPS, e bem menos
que a latência de uma partida online (50–100 ms ida). Se o servidor julgar pelo instante em que o
comando **chega**, todo arremesso online sai atrasado e o verde fica impossível (é a pendência de P-005).

**Opção A, escolhida pelo usuário: o cliente mede, o servidor confere.**

- O pulo já é predito no cliente (CMC), então o cliente sabe o instante do próprio topo do pulo.
  Ao largar o botão ele mede `TimingError = soltura − topo` **no tempo dele** e manda junto com o
  comando (`ServerReleaseShot(TimingError)`).
- O servidor mede o mesmo erro pelo relógio dele e aceita o valor do cliente se a diferença couber
  na latência daquele jogador (meia ida e volta + 50 ms de folga, no máximo 150 ms); fora disso usa o
  próprio valor (e registra no log: é o sinal de trapaça ou de rede muito ruim).
- Daí em diante tudo é do servidor: sorteio do erro, lançamento, cesta.

Alternativas: **(B)** o servidor julga pela chegada do comando — simples e à prova de trapaça, mas o
verde some online; **(C)** alargar a janela no online — muda o design do medidor (D-028).
Com (A), quem trapaceia pode no máximo transformar uma soltura "um pouco atrasada" em verde dentro da
folga; o ganho é limitado e aparece no log.

## Peças

| Ordem | Peça | Onde | Testes |
|---|---|---|---|
| 1 | **Arremesso no servidor**: `UBasketShotComponent` no personagem (começar no chão com a bola → classificar → pular com a bola erguida → soltar: botão / topo / pouso), erro de mira (`ErrorRadius` + verde + `AimScale` + `SampleDiscOffset`), lançamento; enterrada perto do aro; soltura de jump shot acima da cabeça (D-030). Corrige o alcance parado para 2,45 m (estava 2,40 na peça 4 da Fase 4) | `Basket` + `BasketCore` | mundo de teste: soltar no topo = verde = 12/12 cestas; soltar tarde erra mais; bandeja sai sozinha no topo; pousar com a bola força a soltura; enterrada só com alcance |
| 2 | **Rede e medidor** (D-032): botão → servidor com o erro de tempo medido no cliente e validado; medidor no HUD (enche até o topo, faixa verde, resultado: perfeito / um pouco cedo / tarde…); a bola erguida acompanha o pulo em todas as máquinas | `Basket` + `BasketCore` | Core: validação do tempo (dentro da folga aceita, fora usa o do servidor); `net-smoke` com o cliente arremessando sozinho no topo, com latência emulada, e o servidor registrando "verde" |
| 3 | **Marcação, toco e falta no arremesso**: contest do adversário mais forte na soltura; toco automático (adversário no ar, 0,35 s, braços); contato → falta com chance → árbitro → **lance livre** (posições na linha e no garrafão, só o cobrador arremessa, tipo `FreeThrow`) | `Basket` | mundo de teste: marcado erra mais (erro maior); defensor pulando na frente bloqueia, parado não; falta no arremesso leva aos lances livres e eles contam 1 ponto |
| 4 | **Passe**: E com a bola → alvo por `SelectTarget` com o analógico, mira à frente de quem corre, arco; raio de interceptação para quem não é o receptor; a bola passa pelos jogadores perto do passador | `Basket` + `BasketCore` | mundo de teste: passe chega ao companheiro parado e ao que corre; defensor no meio do caminho só intercepta perto da linha; o passador não pega de volta |
| 5 | **Roubo e reach-in**: E sem a bola → alcance, de frente, recarga, chance (semente fixa nos testes); bola solta na direção do defensor; falha pode virar falta | `Basket` | mundo de teste: fora do alcance / de costas não tenta; com a semente fixa, rouba e a posse troca; recarga impede tentativas seguidas |
| 6 | **Drible** (visual, igual em todas as máquinas): a bola quica na mão de quem anda (frequência e altura do Unity), volta às mãos no arremesso; porte do `DribbleMath` | `BasketCore` + `Basket` | Core: curva do quique; mundo de teste: parado antes de andar não quica, depois de andar quica e continua parado |
| 7 | **Checagem com janela** (usuário) — também fecha a da Fase 4 | — | PIE com 2 jogadores e emulação de rede: arremessar, passar, roubar, toco; o medidor acerta o verde mesmo com latência |

## Saída verificável

| Teste | Critério |
|---|---|
| Verde | soltar no topo = erro zero = cesta (12/12 de 3, 5,5 e 7,24 m) |
| Fora do verde | erro cresce com o atraso (Core já cobre; mundo: soltar 0,2 s tarde erra mais que no topo) |
| Bandeja / enterrada | saem sozinhas no topo; enterrada só com sprint, perto e alcance; sem alcance vira bandeja |
| Rede | com 100 ms de latência emulada, o arremesso do cliente no topo sai verde no servidor (`net-smoke`) |
| Toco | defensor no ar na frente bloqueia; no chão não; só nos 0,35 s após a soltura |
| Lance livre | falta no arremesso → lances livres da linha; cada acerto vale 1 |
| Passe / roubo | passe chega ao alvo; interceptação só perto da linha; roubo com semente fixa troca a posse |
| Testes | `Basket.*` todos verdes; `net-smoke` verde |

## Riscos

- **Verde com latência** (D-032): se o usuário escolher (B), a peça 2 fica mais simples, mas o verde
  online deixa de ser alcançável; se (C), o medidor muda de tamanho só no online.
- **Pulo do arremesso em rede**: o pulo é predito no cliente e corrigido pelo servidor; se a correção
  chegar no meio do pulo, o topo do cliente e o do servidor diferem. A peça 2 mede essa diferença com
  latência emulada antes de confiar nela.
- **Alvos pequenos no tempo**: o servidor roda a 60 Hz e a janela marcada é menor que um quadro. O
  erro de tempo é calculado a partir de instantes (não de quadros), então o quadro não arredonda a
  janela; os testes usam erros de tempo exatos.
- **RAM/compilação**: cada peça mexe no `ABasketCharacter` (cabeçalho muito incluído) → builds maiores.
  Agrupar mudanças de cabeçalho por peça.

## Progresso

- 2026-10-07: **peça 1 pronta** — `UBasketShotComponent` (servidor) e `BasketShotMotion` (Core). Soltar no
  topo do pulo = verde = 12/12 cestas de 3, 5,5 e 7,24 m, com as meias-larguras do verde iguais às do
  Unity (±14,5 / 13,6 / 12,1 ms); soltura atrasada = erro do modelo; cair com a bola força a soltura;
  bandeja sai no topo; enterrada correndo perto do aro entra. Alcance parado corrigido para 2,45 m. A bola
  passa a ser atualizada depois do movimento de quem a segura (sem um quadro de atraso). O anfitrião do
  listen server já arremessa com o Espaço; os clientes remotos chegam na peça 2. `Basket.*` 196/196.
- 2026-10-07: **peça 2 pronta** — o pulo do arremesso vai nos movimentos preditos (`FLAG_Custom_2`): o
  cliente pula na hora e conhece o próprio topo. Ao largar o botão ele mede soltura − topo e manda ao
  servidor, que compara com a própria medida (D-032: meia ida e volta + 50 ms, até 150 ms). Medidor no HUD
  (enche até o topo, faixa verde, nota por 1 s depois da soltura). `net-smoke` com o cliente a 100 ms de
  atraso (`-PktLag=100`) e os dois jogadores arremessando sozinhos no topo (`-BasketAutoShoot`): **13/13**;
  o arremesso do cliente: cliente +0,2 ms × servidor −0,5 ms, folga 109 ms, aceito, erro 0, cesta.
  `Basket.*` 201 (198 rodados + 3 de calibração sem mudança).
- 2026-10-07: **peça 3 pronta** — marcação na soltura (o adversário que mais atrapalha; também ao vivo no
  medidor: o verde encolhe quando o defensor chega), toco automático (adversário no ar, até 0,35 s após a
  soltura, bola entre a cabeça e a ponta dos dedos), falta no arremesso (contato na soltura, 35%) e lance
  livre (cobrador na linha, os outros no garrafão, ninguém se mexe, só o cobrador arremessa; vale 1 no 3x3).
  Números do teste: defensor a 1 m na frente → contest 0,50, verde de ±13,6 para ±8,9 ms. O primeiro teste
  do toco falhou porque o defensor já tinha pegado a bola desviada quando a checagem rodou — o toco em si
  aconteceu 4 quadros após a soltura. `Basket.*` 204 (201 + 3 de calibração), `net-smoke` 13/13. Fora:
  efeito dos atributos (Força, Defesa) no contest — Fase 7.
- 2026-10-07: **peça 4 pronta** — passe: E com a bola manda a direção do analógico; o servidor escolhe o
  companheiro mais alinhado (o mais perto sem analógico), joga nas mãos dele com antecipação para quem corre
  (no máximo 2 m, como no Unity) e protege o passe dos jogadores a até 1,8 m da soltura; os outros só
  interceptam a até 50 cm da linha. Os testes acharam um limite do próprio Unity: a toda velocidade, um
  passe de 6 m pede ~3,4 m de antecipação e cai atrás do recebedor (o teste usa meia velocidade). O teste
  da espera de 0,25 s (Fase 4) passou a derrubar a bola da mão: quem passa nunca toca o próprio passe no
  ar. `Basket.*` 211 (208 + 3 de calibração), `net-smoke` 13/13.
- 2026-10-07: **peça 5 pronta** — roubo: E sem a bola pede ao servidor; vale com o portador a até 1,3 m e
  de frente, sem estar arremessando, 1 s entre tentativas; chance 12% (20% se o portador anda); a bola sai
  na direção do defensor. Errar pode virar falta de reach-in (4%). Os testes acharam um defeito: um
  jogador conseguia ficar em pé sobre a bola solta, e aí o movimento dele esperava a bola e a bola na mão
  esperava o movimento (ciclo de tick); a bola agora não serve de chão. `Basket.*` 218 (215 + 3 de
  calibração), `net-smoke` 13/13.
- 2026-10-07: **peça 6 pronta (código da Fase 5 completo)** — drible visual, calculado em cada máquina a
  partir do movimento de quem tem a bola (nada replicado): começa quando anda, continua parado, 1,2
  quiques/s até 85 cm abaixo da mão, volta às mãos no arremesso. `Basket.*` 223 (220 + 3 de calibração),
  `net-smoke` 13/13. Build de 34 min nesta peça (cabeçalho do personagem, incluído por quase tudo).

## Checagem com janela (peça 7, usuário) — fecha também a da Fase 4

1. Abrir o editor; **Net Mode: Play As Listen Server**, **2 jogadores**; Play. (Opcional: emulação de
   rede **Average** para ver com latência.)
2. **Posse e drible:** a bola começa na mão do armador; andando, ela quica na mão (também na outra
   janela); parado, continua quicando.
3. **Arremesso (Espaço / X ou Quadrado):** segurar e soltar no topo do pulo. O medidor aparece à direita
   do centro; soltar na faixa verde mostra **PERFECT** e a bola não erra a mira. Conferir nas duas
   janelas: voo liso e igual, quique no aro, placar no HUD, bola para o outro time.
4. **Bandeja e enterrada:** perto do aro a bola sai sozinha no topo; correndo (Shift) bem perto, enterrada.
5. **Passe (E):** com o analógico/WASD apontando para o companheiro (com 2 jogadores, o outro time não
   recebe: sem companheiro o passe não sai — normal com 1×1).
6. **Defesa (outro jogador):** de frente para quem tem a bola, **E** tenta o roubo; pular (Espaço) na
   frente de um arremesso pode dar **toco**; falta no arremesso leva ao **lance livre** (todos parados,
   só o cobrador arremessa).
7. Avisar o que estiver estranho (tempo do verde com latência, bola atravessando alguém, câmera…).

