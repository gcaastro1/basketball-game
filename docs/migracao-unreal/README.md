# Migração para Unreal Engine 5

Decisão D-029 (`docs/decisoes.md`). Este diretório é a fonte da verdade da migração até o
repositório Unreal existir; depois ele passa para lá.

- `README.md` — por que, como, versão, hardware, multiplayer, fases (este arquivo).
- `mapa.md` — mapa Unity → Unreal, sistema a sistema, com status.
- `fase-0-1.md` — plano concreto das fases 0 (referência Unity) e 1 (projeto Unreal).
- `referencia-unity.md` — números do Unity que o Unreal precisa reproduzir (Fase 0).
- `assets-e-licencas.md` — inventário de assets, licenças e o que é reimportado ou refeito (Fase 0).

## Por que migrar

Respostas do usuário em 2026-10-05:

| Pergunta | Resposta | Efeito na decisão |
|---|---|---|
| Motivo | Aprender e trabalhar com Unreal | A migração é o objetivo em si, não um meio para gráfico melhor |
| Plataforma | PC/console; talvez mobile depois | Console favorece Unreal; mobile exige disciplina de materiais desde já |
| Multiplayer online | Sim | Arquitetura cliente-servidor desde o primeiro dia (replicação do Unreal) |
| Hardware | 16 GB RAM, 200 GB livres em HD (não SSD) | Dá para começar; HD deixa compilação de shaders e carregamento lentos |
| C++ | Estudou na faculdade, quer C++ | Lógica em C++; Blueprint só para visual, UI e cola |
| Upgrade | Até dezembro: placa-mãe, CPU, GPU ≥ 8 GB | Trabalho pesado de GPU fica para depois do upgrade |

## Princípios

1. **Comportamento, não tradução.** Porta-se o que o jogo faz; a forma segue o Unreal.
2. **O projeto Unity é o oráculo.** Fica congelado (sem features novas) e serve de referência
   numérica: estatísticas das simulações IA×IA, taxa de acerto por zona, tempos de voo.
3. **Servidor manda.** Toda regra, placar, posse, resultado de arremesso e IA rodam no servidor.
   Cliente prevê o próprio movimento e mostra o resto.
4. **Lógica pura continua pura.** O que hoje é testável sem cena (`*Math`, `ShotAccuracyModel`,
   regras, `TeamBrain`, gacha) vira C++ sem Actor, com testes de automação portados junto.
5. **Unidades nativas do Unreal.** Centímetros, Z para cima, X para frente. Valores dos
   ScriptableObjects são convertidos no port (metros × 100, eixos trocados), nunca na execução.

## Versão

**UE5, a versão 5.x estável mais recente com pelo menos um hotfix (.1/.2)**, instalada pelo Epic
Launcher. UE4 descartado: sem atualizações, e migrar duas vezes custa mais que desligar recursos.
Trocar de versão menor (5.x → 5.y) só entre fases, nunca no meio de uma.

## Hardware

### Até o upgrade (GTX 1050 2 GB, quad-core, 16 GB, HD)

O que fazer nesse período: fases 0–2, que são quase só C++ e testes (CPU), e aprender o Gameplay
Framework com níveis vazios ou de cubos. Evite importar o ginásio ou texturas grandes.

- Launcher: instale só a plataforma Windows; desmarque Android/iOS/Linux, Starter Content e os
  símbolos de debug do editor (ocupam dezenas de GB).
- Editor em escalabilidade Média; viewport sem "Realtime" quando parado.
- Visual Studio 2022 (workload "Desenvolvimento de jogos com C++") ou Rider (gratuito para uso
  não comercial). Confira na documentação da versão escolhida qual VS/SDK ela exige.

### Depois do upgrade (GPU ≥ 8 GB)

- **Compre um SSD NVMe junto** (≥ 1 TB) — mais impacto no dia a dia do Unreal que a própria GPU:
  motor, projeto e Derived Data Cache no SSD.
- **32 GB de RAM** se o orçamento permitir (editor + IDE + compilação + 2 clientes de teste).
- Engine compilada a partir do código-fonte só neste ponto (necessária para servidor dedicado).

### Recursos pesados

| Recurso | Agora (GTX 1050) | Depois do upgrade | Motivo |
|---|---|---|---|
| Lumen | Evitar | Opcional | Ginásio fechado e estático fica melhor e mais barato com luz pré-calculada |
| Nanite | Evitar | Não precisa | Personagens estilizados e low-poly não ganham nada |
| Virtual Shadow Maps | Evitar | Usar com cautela | Shadow maps clássicos bastam para uma quadra |
| Luz pré-calculada (Lightmass) | Recomendado | Recomendado | Arena estática; só jogadores e bola se movem |
| Distance Fields | Evitar | Opcional | Só servem para Lumen/AO de distância |
| TSR | Usar com cautela | Recomendado | No hardware atual, TAA/FXAA |
| Cel-shading | No material | No material | Toon no próprio material funciona também no mobile; pós-processo depende do GBuffer |
| Outline | Usar com cautela | Recomendado | Casco invertido ou pós-processo; medir com `stat gpu` |
| Texturas | 1K–2K | 2K (4K só em close-ups) | VRAM |

Profiling desde a fase 1: `stat unit`, `stat fps`, `stat gpu`, `ProfileGPU`, Unreal Insights.

## Multiplayer (arquitetura base)

Formato (P-005): **3v3 online, cada humano controla um jogador** (até 6 humanos).
Modelo: **servidor autoritativo**. Começa com *listen server* (um jogador hospeda; funciona com o
engine do Launcher). Servidor dedicado exige engine compilado do código-fonte; fica para depois do
upgrade e do SSD.

| Peça | Onde roda | O que guarda / faz |
|---|---|---|
| GameMode | Só servidor | O árbitro (`MatchManager`): regras, reinícios, faltas, fim de partida |
| GameState | Replicado | Placar, relógio, shot clock, fase, posse — o que todos veem |
| PlayerState | Replicado | Personagem escolhido, time, estatísticas do jogador |
| PlayerController | Dono + servidor | Input do humano → comandos; UI local |
| AIController | Só servidor | IA dos jogadores não humanos |
| Character + CharacterMovementComponent | Todos | Movimento com predição do cliente (já vem pronto no CMC) |
| Bola | Servidor decide | Ver abaixo |

**Bola** (a parte mais difícil de rede num jogo de esporte):
- *Na mão*: presa ao jogador; replica só quem tem a posse. Drible é visual (já é no Unity).
- *Em voo* (passe, arremesso): o servidor replica os parâmetros do lançamento + instante; cada
  cliente calcula a mesma parábola (`TrajectoryMath`/`ShotArc` já são analíticos). Barato e liso.
- *No aro / solta*: física no servidor, posição replicada com suavização nos clientes.
- *Resultado do arremesso*: decidido no servidor. Como o medidor depende do tempo de soltura, o
  cliente envia o instante e o servidor valida com compensação de latência — risco a medir na fase
  de rede (P-005).

Testes de rede desde a fase 1: PIE com 2 clientes ("Play As Listen Server") e Network Emulation
(latência e perda de pacotes) ligados.

**Economia e gacha online**: num jogo online, moeda e tiragens não podem ficar num save local
editável. Fica pendente (P-006) qual backend usar; até lá o meta roda offline como hoje.

## Fases

Ordem por dependência; prioridade 🔴 crítico, 🟠 importante, 🟡 secundário, 🟢 polimento.

| Fase | Conteúdo | Prioridade | Quando |
|---|---|---|---|
| 0 | Referência Unity: congelar, exportar estatísticas, inventário e licenças dos assets — ✅ tag `unity-reference`; faltam só 3 licenças (Tripo, clipes de basquete, Kimodo) | 🔴 | Outubro |
| 1 | Projeto UE5 C++, repositório + LFS, módulos, configurações leves, teste de fumaça e PIE 2 clientes — ✅ `gcaastro1/basketball-unreal` (UE 5.8.3): build, testes, rede com e sem janela, emulação de rede e linha de base de desempenho | 🔴 | Outubro |
| 2 | Núcleo de lógica pura em C++ (`BasketCore`) com os testes portados — ✅ 131 testes (`fase-2.md`) | 🔴 | Out–Nov |
| 3 | Input (Enhanced Input), Character/CMC, câmera de transmissão — já em rede | 🔴 | Nov–Dez |
| 4 | Bola, aro físico (Chaos), pontuação; recalibração contra o oráculo | 🔴 | Após upgrade |
| 5 | Arremesso + medidor, drible, passe, defesa (componentes) | 🔴 | Após upgrade |
| **Vertical slice** | Fases 3–5 + IA 1v1 simples + HUD de placar, 1v1 online (2 clientes) | 🔴 | Avaliar antes de seguir |
| 6 | IA de time 3v3 (utility própria no servidor) | 🟠 | |
| 7 | Personagens, atributos, habilidades (avaliar Gameplay Ability System) | 🟠 | |
| 8 | Animação: AnimBP, Blend Spaces, Montages, IK, retargeting | 🟠 | |
| 9 | UI (UMG): HUD, medidor, menus | 🟠 | |
| 10 | 5v5 quadra inteira | 🟡 | |
| 11 | Meta (save, inventário, gacha) + backend online (P-006) | 🟡 | |
| 12 | Ginásio, materiais toon, VFX (Niagara), áudio, otimização final | 🟢 | |

**Vertical slice — critério de aprovação**: 1v1 com dois clientes em PIE com 100 ms de latência
emulada; movimento sem tremer; bola em voo igual nos dois; taxa de acerto por zona dentro de ±5
pontos percentuais da referência Unity; ≥ 60 FPS no hardware novo (≥ 30 no atual com nível
simples). Se não fechar, para e corrige antes da fase 6.

O que **não** entra: Navigation System/NavMesh, Behavior Trees, AI Perception, EQS (quadra aberta,
IA posicional própria — D-014/D-027), Lumen/Nanite.
