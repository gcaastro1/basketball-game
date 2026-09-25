# Roadmap mestre

Roadmap único do projeto (substitui as "Fases 1–10" e as "Etapas 0–10" do briefing, que
conflitavam). Origem: `docs/auditoria/2026-09-25-auditoria-e-avaliacao.md`, seção 7.

| Etapa | Conteúdo | Saída verificável | Status |
|---|---|---|---|
| 0 Auditoria | Estado do projeto + avaliação do briefing | `docs/auditoria/` | ✅ |
| 1 Vertical slice 1v1 | Movimento, bola, drible visual, passe, arremesso, IA FSM, placar | Spec/plano em `docs/superpowers/` | ✅ |
| **1.5 Consolidação** | Arquitetura N jogadores, bola com voo real, aro físico, placar por time/zona, reinício de posse, fim de partida, layers, CI | Loop cesta→placar→reinício testado | ✅ (CI verde: 90/90) |
| 2 Fundamentos | Arremesso por distância/contest/timing, pulo, block, steal, rebote/box-out, bandeja/enterrada básicas, Input Actions + buffer | Testes por fundamento (`FundamentalsTests`) | ✅ — plano: `docs/etapas/etapa-2-fundamentos.md` |
| 3 Regras 3v3 | Preset FIBA 3x3 (1/2 pts), check ball, clear the ball, tempo, shot clock, faltas básicas | Partida 3v3 completa | ✅ (CI: 137 EditMode + 31 PlayMode) — plano: `docs/etapas/etapa-3-regras.md` |
| 4 IA 3v3 | Utility AI, blackboard de time, companheiros, marcação, ajuda | Simulação IA×IA com estatísticas (`AISimulationTests`) | ✅ (CI verde; calibrado com log por arremesso/passe) — plano: `docs/etapas/etapa-4-ia.md` |
| 5 Personagens | Definição/instância, atributos 0–99 + curvas, habilidades data-driven, níveis/LB/dupes | Arquétipos jogam diferente (medido) | ✅ (CI verde) — plano: `docs/etapas/etapa-5-personagens.md` |
| 6 Animação | Rig Humanoid, Animator/blend trees, sockets/IK de mão e bola | Placeholders trocados sem mudar gameplay (`CharacterVisualTests`) | ✅ (CI verde: modelo 1,68 m no chão, mãos na bola, gameplay idêntico) — plano: `docs/etapas/etapa-6-animacao.md` |
| 7 5v5 | Quadra inteira, transição, inbound, backcourt, IA de 5 | Partida 5v5 completa (`FullCourtTests`, cena `02_FullCourt_5v5`) | ✅ (CI verde) — plano: `docs/etapas/etapa-7-5v5.md` |
| 8 Meta | Inventário, save versionado, economia, Gacha atrás de `IEconomyService` | Testes de pity/taxas com seed (`GachaTests`, `SaveSystemTests`) | ✅ código / ⏳ CI — plano: `docs/etapas/etapa-8-meta.md` |
| 9 História | Capítulos/diálogos/partidas-objetivo data-driven | Capítulo de teste | — |
| 10 Polish | VFX anime, câmera cinemática, UI final, áudio, otimização | — | — |

Decisões: `docs/decisoes.md`. Arquitetura: `docs/arquitetura.md`.
