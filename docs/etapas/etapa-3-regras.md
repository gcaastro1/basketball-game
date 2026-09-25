# Etapa 3 — Regras (3v3 completo)

## Objetivo
Uma partida 3v3 em meia quadra que **começa, flui e termina** pelas regras: relógio de jogo,
relógio de arremesso, "limpar a bola", faltas com lances livres e prorrogação. Tudo em dados
(`MatchRules`), para que 5v5 (Etapa 7) seja outro asset e não outro código.

## Escopo

| Regra | Implementação mínima | Fica para depois |
|---|---|---|
| Relógio de jogo | Períodos configuráveis; corre só com bola viva; arremesso no ar no estouro ainda vale (buzzer beater) | Pausas de tempo técnico (5v5) |
| Prorrogação | Morte súbita por pontos (3x3: primeiro a marcar 2) ou período extra (5v5) | — |
| Shot clock | Corre com a bola em posse (segurada/passe); reseta em troca de posse, toque no aro e reinícios; estouro = posse adversária | Reset para 14 s específico de 5v5 (já é dado) |
| Limpar a bola (3x3) | Troca de posse com bola viva obriga levar a bola para fora do arco; cesta sem limpar não vale e a posse vira | — |
| Após cesta | Dado: check ball no topo, ou reposição embaixo da cesta com obrigação de limpar (3x3) | Reposição de fundo 5v5 (7) |
| Faltas | Faltas coletivas; falta no arremesso (contato de corpo na soltura) e "reach-in" em roubo errado, por chance configurável | Detecção por colisão física de corpo, faltas pessoais/exclusão |
| Lances livres | Fase própria: arremessador na linha, demais congelados; mesmo arremesso por timing; 1 ponto; último errado = rebote vivo | Mini-game próprio de lance livre (9) |
| Penalidade | Por dado: a partir da 7ª falta 2 LL; a partir da 10ª 2 LL + posse (FIBA 3x3) | — |
| Marcação | Pares homem-a-homem definidos a cada reinício; só o marcador do portador contesta, rouba e bloqueia; só o mais próximo de cada time persegue bola solta | Rotação/ajuda (4) |

## Presets
- `DefaultMatchRules` — slice 1v1 genérico (sem relógios), mantido para testes rápidos.
- `FIBA3x3MatchRules` — 1/2 pontos, 21 ou 10 min, shot clock 12 s, limpar a bola, reposição embaixo da cesta, faltas 7/10, LL 1/2 (+1 no and-one), prorrogação morte súbita a 2 pontos.
- `MatchSetup3v3` — Home: humano + 2 IA; Away: 3 IA. A cena passa a usar 3v3 + FIBA 3x3.

## Arquitetura
`MatchManager` continua sendo o árbitro, mas cada regra é uma peça pura e testável:
`GameClock`, `ShotClock`, `FoulRules` (decisão de penalidade), `FreeThrowSequence`. O
`MatchManager` recebe fatos da simulação (posse, toque no aro, arremesso solto, cesta, falta,
estado da bola) e responde com eventos (reinício de posse por tipo, preparar lance livre).

## Critérios de aceitação (testados)
- Relógio acaba → vence quem tem mais pontos; empate → prorrogação; arremesso no ar no estouro conta.
- Shot clock estoura → posse adversária; toque no aro reseta.
- Rebote defensivo sem limpar → cesta anulada e posse invertida (3x3); limpando → vale.
- Falta no arremesso: errou → LL pela zona; acertou → cesta + 1 LL; 10ª falta → 2 LL + posse.
- Lance livre: 1 ponto, fase congelada, último errado vira rebote vivo.
- Partida 3v3 completa na cena roda sem exceções.
