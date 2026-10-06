# Fase 4 — Bola, aro físico e cesta

Repositório: `gcaastro1/basketball-unreal`. Pré-requisito: Fase 3 concluída (166 testes).

## Objetivo

A bola existe e o aro é físico: a bola fica na mão de quem tem a posse, voa quando lançada, bate no
aro e na tabela, cai, quica e é pega; quando passa pelo aro de cima para baixo, o árbitro conta os
pontos. **O aro no Chaos tem que reproduzir a tabela de acertos da referência do Unity** (a maior
incerteza técnica da migração). O lançamento pelo jogador (arremesso com medidor, passe) é da Fase 5;
aqui os lançamentos vêm de código (testes e um comando de depuração).

Cabe na GTX 1050: é física na CPU com formas simples.

## Referência no Unity

| Item | Valor (`referencia-unity.md`) |
|---|---|
| Bola | Ø 24 cm, 0,62 kg, quique 0,75 (combine *Maximum*), drag 0,05, angular drag 0,3, CCD em voo |
| Aro | anel de 16 cápsulas, raio 22,86 cm, tubo de 1 cm; tabela 183 × 107 × 5 cm |
| Física | gravidade −9,81, passo fixo 50 Hz, solver 6 / 1 |
| Estados | `Free`, `Held`, `Passing`, `Shooting`; soltura "viva" até tocar o chão ou ser pega |
| Pegar | por **proximidade**, não por colisão (CharacterController × Rigidbody não dispara colisão confiável): até 1,0 m na horizontal, abaixo do alcance + 0,15 m; quem soltou não pega por 0,25 s |
| Na mão | 1,0 m acima dos pés, 0,45 m à frente, 0,2 m para o lado da mão |
| Calibração | mirando no centro entra 12/12; taxa × desvio da mira (seção 5): 4,5 m 8/8/5/3/3/0/0, 6,75 m 8/8/8/5/0/0/0, bandeja 8/8/8/8/1/0–1/0 |

## Decisão central: modelo de voo (D-031, aceita)

**Voo analítico até o primeiro contato, física do Chaos depois.**

- **Na mão** (`Held`): a bola acompanha quem tem a posse; replica só *quem* é o dono.
- **Em voo** (`Shooting`/`Passing`): o servidor replica **ponto de lançamento, velocidade e instante**
  (tempo do servidor); servidor e clientes calculam a posição com `BasketTrajectory::PositionAt`.
  Liso nos clientes, barato de replicar e idêntico em todas as máquinas. O servidor varre (*sweep*) o
  trecho de cada quadro contra o cenário e os jogadores.
- **No primeiro contato** (aro, tabela, chão, jogador): o servidor liga a física do Chaos com a
  velocidade analítica daquele instante. A partir daí (`Free`) a bola é corpo rígido no servidor e os
  clientes recebem a posição replicada com suavização.

Por que não física do começo ao fim, como no Unity: replicar um corpo rígido em voo dá trajetórias que
"pulam" no cliente com latência, e o arremesso é o momento que mais precisa ser liso e igual para os
seis jogadores. Por que não analítico até o fim: o aro, a tabela e o rebote precisam de física de
verdade. O efeito no acerto: o arco analítico não tem o drag de 0,05 do Unity, mas a mira já é feita
contra o arco analítico (`ComputeArcVelocity`), então a bola cruza o plano do aro exatamente no ponto
mirado — o que a calibração do Unity media era o aro decidindo a partir desse ponto.

## Peças

| Ordem | Peça | Onde | Testes |
|---|---|---|---|
| 1 | `ABasketBall`: estados, na mão, voo analítico, passagem para física no contato, replicação | `Basket` | mundo de teste: voo segue `PositionAt`; contato liga a física com a velocidade certa; chão encerra a soltura viva |
| 2 | Cesta física: anel de cápsulas + tabela com colisão e material físico; detecção da cesta (`BasketScoring::IsScoringCrossing`) → árbitro | `Basket` | bola solta no centro entra e o árbitro conta; 12 arremessos mirando no centro entram (referência 12/12) |
| 3 | **Calibração** (opção B, D-031): o aro do Chaos fica como está; mede-se o raio efetivo por distância e o modelo escala a mira para manter as chances do Unity | `Basket` + `BasketCore` (dados) | raio medido = dados (±0,5 cm); 200 arremessos com o erro do modelo a até 8 pontos da chance prevista |
| 4 | Posse: pegar por proximidade (alcance, 0,25 s de graça), bola solta, reinício entrega a bola; status da bola → árbitro (shot clock, buzzer) | `Basket` + `BasketCore` | mundo de teste: pega dentro do alcance, não pega fora/acima; shot clock só corre com posse |
| 5 | Rede: cliente vê o mesmo voo; `net-smoke` estendido; checagem com janela | `Basket` | `net-smoke` (cliente vê a bola mudar de dono); PIE com emulação |

## Saída verificável

| Teste | Critério |
|---|---|
| Voo | posição do voo = `PositionAt` (± 0,5 cm) em servidor e cliente |
| Mirando no centro | 12/12 cestas de 4,2 / 5,5 / 6,75 / 7,5 m, a 0°, 40° e 70° |
| Calibração (D-031) | raio efetivo do aro = curva nos dados (±0,5 cm); taxa real a ±8 pontos da chance do modelo (4,5 e 7,24 m) |
| Cesta | conta 2 ou 3 (1 ou 2 no 3x3) pelo ponto de soltura; não conta de baixo para cima |
| Posse | pega até 1 m e até o alcance + 15 cm; quem soltou espera 0,25 s; shot clock corre só com posse |
| Rede | o cliente vê o voo liso e a mesma cesta; `net-smoke` verde |
| Testes | `Basket.*` todos verdes |

## Riscos

- **O Chaos não vai bater de primeira com o PhysX** (quique, atrito, contato com cápsulas finas). A
  peça 3 existe para isso; se a tabela não fechar ajustando material e geometria, a alternativa é um
  aro mais simples com resposta de contato própria — decisão a levar ao usuário antes.
- **Passo da física**: o Chaos com passo variável dá resultados diferentes a cada execução. Usar
  *substepping* com passo fixo (como os 50 Hz do Unity) para a calibração ser reprodutível.
- **Tubo de 1 cm e bola rápida**: CCD ligado na bola em `Free`.

## Progresso

- 2026-10-06: **peça 1 pronta** — `ABasketBall` (na mão, voo analítico replicado pelo lançamento,
  Chaos depois do primeiro contato, soltura viva até o chão). Os testes acharam dois erros: a bola nova
  aplicava o estado "solta" padrão e ia para a origem do mundo; a varredura do primeiro quadro partia de
  onde a bola estava antes do lançamento.
- 2026-10-06: **peça 2 pronta** — aro com 16 cápsulas (`BasketRim`), tabela com colisão, material físico
  da bola (quique 0,75 combinando pelo máximo, atrito 0,6), cesta detectada no GameMode e contada pelo
  árbitro, toque no aro zera o shot clock. **Aro do Chaos: 12/12 mirando no centro, como no Unity.**
  `Basket.*` 172/172. Faltou a dependência `PhysicsCore` (erro de link no fim de um build de 40 min).
- Substepping do Chaos não ligado: no 5.8 ele é marcado como experimental. Os testes avançam o mundo com
  passo fixo de 50 Hz (o do Unity), então a calibração é reprodutível sem ele.
- Ambiente: builds de 18–40 min nesta fase com a RAM comprometida acima da física (19 de 15,9 GB) e o HD
  paginando; fechar programas pesados antes de compilar.
- 2026-10-06: **peça 3 pronta (opção B, D-031)** — a tabela do Chaos não fecha com a do PhysX (4,5 m:
  8 8 8 6 2 0 0 contra 8 8 5 3 3 0 0; passar a bola à física no ponto do quadro anterior ajudou pouco).
  O usuário escolheu manter o aro e recalibrar o modelo. Raio efetivo medido (r² = 2·∫ taxa·desvio):
  16,4 / 14,1 / 14,0 / 15,6 / 15,9 cm de 3 / 4,5 / 6 / 7,24 / 8,5 m, bandeja 18,2 cm (Unity: 12,4 e 14,1).
  A curva foi para `FBasketShotAccuracySettings` e o desvio da mira é escalado por `AimScale`.
  Aceitação: 200 arremessos livres — 4,5 m 56,0% (modelo 50,1%; antes da correção 69,5%), 7,24 m 31,5%
  (modelo 37,9%; antes 55,5%). O Chaos varia ~0,15 cm de uma execução para outra (14,14 → 14,04 a
  4,5 m), dentro da tolerância. `Basket.*` 177/177; build de 4,5 min com a RAM livre.
