# Etapa 4 — IA de time (3v3)

## Objetivo
A IA passa de "cinco indivíduos" para **um time**: ataque com espaçamento, cortes,
pick-and-roll e passes escolhidos por qualidade de arremesso; defesa com marcação, ajuda,
troca em bloqueio e box-out no rebote. Companheiros da IA ajudam o jogador humano do mesmo jeito.

## Arquitetura (três camadas)
1. **`ITeamStrategy`** — escolhe o tipo de jogada da posse (Spacing, PickAndRoll, Isolation).
   `AutoStrategy` escolhe por pesos no `AIConfig`. **Ponto de encaixe do Modo B (P-001):** um
   jogador humano "técnico" será outra implementação de `ITeamStrategy` (e/ou de
   `IAgentController` para troca de jogador), sem mudar o resto.
2. **`TeamBrain`** (um por time, replaneja a 10 Hz) — transforma a situação em uma **ordem
   por jogador** (`TeamOrder`: Handle, Space, Cut, Screen, Roll, Crash, Guard, Help, BoxOut).
   Planejadores puros: `OffensePlanner`, `DefensePlanner` (e rebote).
3. **`AIAgentController`** (um por jogador) — executa a ordem com as mesmas mecânicas do
   humano. O portador decide por **utility**: arremessar / passar / infiltrar / segurar, com
   `ShotQuality` (distância, marcação, valor do arco) e segurança da linha de passe.
   Sem `TeamBrain` (1v1) mantém o comportamento individual da Etapa 3.

A IA **não lê a fórmula real de acerto** (`ShotAccuracyModel`): ela estima com a própria
percepção (`ShotQuality`), como um jogador faria.

## Escopo
| Comportamento | Versão mínima |
|---|---|
| Espaçamento | Vagas no perímetro (alas/cantos), atribuídas por proximidade |
| Corte | Jogador cujo marcador se afasta demais corta para a cesta e volta |
| Pick-and-roll | Companheiro mais próximo bloqueia o marcador do portador; portador ataca pelo lado do bloqueio; bloqueador "rola" para a cesta |
| Passe | Portador passa quando um companheiro tem arremesso claramente melhor e a linha está livre |
| Ajuda | Portador batido perto da cesta → defensor mais próximo fecha o caminho |
| Troca | Bloqueio pega o marcador do portador → troca de marcação |
| Rebote | Defensores fazem box-out no seu homem; um atacante ataca o rebote |
| Movimento | Desvio simples de outros jogadores (evita amontoar) |
| Estatísticas | `MatchStats` por time (arremessos, 2/3, LL, passes, roubos, tocos, faltas, rebotes of/def, erros) |

## Testes
- EditMode: planejadores (vagas, corte, bloqueio, ajuda, troca, box-out), utility do portador, qualidade de arremesso, segurança de passe, desvio.
- PlayMode: **simulação IA×IA 3v3** acelerada com estatísticas — tem que passar, arremessar, pegar rebote e pontuar, sem travar.

## Riscos
- Utility mal calibrado → passes demais ou arremessos forçados. Mitigação: pesos no `AIConfig` + estatísticas da simulação.
- Bloqueios dependem da colisão entre `CharacterController`s (funciona, mas é "duro").
