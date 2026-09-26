# Roadmap mestre

Roadmap único do projeto (substitui as "Fases 1–10" e as "Etapas 0–10" do briefing, que
conflitavam). Origem: `docs/auditoria/2026-09-25-auditoria-e-avaliacao.md`, seção 7.

| Etapa | Conteúdo | Saída verificável | Status |
|---|---|---|---|
| 0 Auditoria | Estado do projeto + avaliação do briefing | `docs/auditoria/` | ✅ |
| 1 Vertical slice 1v1 | Movimento, bola, drible visual, passe, arremesso, IA FSM, placar | Spec/plano em `docs/superpowers/` | ✅ |
| **1.5 Consolidação** | Arquitetura N jogadores, bola com voo real, aro físico, placar por time/zona, reinício de posse, fim de partida, layers, CI | Loop cesta→placar→reinício testado | ✅ (CI verde: 90/90) |
| 2 Fundamentos | Arremesso por distância/contest/timing, pulo, block, steal, rebote/box-out, bandeja/enterrada básicas, Input Actions + buffer | Testes por fundamento (`FundamentalsTests`) | ✅ — plano: `docs/etapas/etapa-2-fundamentos.md` |
| 2.5 Medidor de arremesso | Barra estilo NBA 2K: janela verde em volta do topo do pulo (verde = perfeito), tamanho pelo atributo, marcação, distância e movimento | `ShotMeterTests` (EditMode), `LiveShotTests.GreenReleases_AlwaysGoIn` (PlayMode) | 🔄 — decisão D-028 |
| 3 Regras 3v3 | Preset FIBA 3x3 (1/2 pts), check ball, clear the ball, tempo, shot clock, faltas básicas | Partida 3v3 completa | ✅ (CI: 137 EditMode + 31 PlayMode) — plano: `docs/etapas/etapa-3-regras.md` |
| 4 IA 3v3 | Utility AI, blackboard de time, companheiros, marcação, ajuda | Simulação IA×IA com estatísticas (`AISimulationTests`) | ✅ (CI verde; calibrado com log por arremesso/passe) — plano: `docs/etapas/etapa-4-ia.md` |
| 4.5 IA posicional | Defesa individual com ajuda (nega perto da bola, ajuda no lado fraco) e zona (2-3/1-2) escolhida por posse; ataque espaçado longe da bola na linha de 3 da quadra; IA lê o canto de 3 | `TeamAITests` (ajuda, zona, espaçamento, canto), `AISimulationTests` (3v3 individual e zona) | 🔄 — plano: `docs/etapas/etapa-4.5-ia-posicional.md`, decisão D-027 |
| 5 Personagens | Definição/instância, atributos 0–99 + curvas, habilidades data-driven, níveis/LB/dupes | Arquétipos jogam diferente (medido) | ✅ (CI verde) — plano: `docs/etapas/etapa-5-personagens.md` |
| 6 Animação | Rig Humanoid, Animator/blend trees, sockets/IK de mão e bola | Placeholders trocados sem mudar gameplay (`CharacterVisualTests`) | ✅ (CI verde: modelo 1,68 m no chão, mãos na bola, gameplay idêntico) — plano: `docs/etapas/etapa-6-animacao.md` |
| 6.5 Clipes reais + câmera | Clipes Mixamo/UAL/captura de basquete (locomoção por velocidade, drible na parte de cima, arremessos em sincronia com o jogo, deslize de defesa), procedural para o resto; câmera de transmissão virada para o ataque | `AnimationClipLogicTests`, `CameraRigMathTests`, `CharacterVisualTests`, `CameraControllerTests` | ✅ (CI verde: 55 PlayMode + 269 EditMode; clipes ligados no CI, drible correndo com a mão na bola) — decisões D-022, D-023 |
| 6.6 Ginásio e bola | Ginásio (MarpaStudio) montado em volta da quadra, bola real (TierrasDeRol), quadra e linha de 3 NBA (cantos retos), cestas com rede/vidro/poste; quadra de treino (`03_Practice_Solo`) com câmera lenta, câmeras e painel da animação | `ArenaVisualTests` (piso NBA alinhado à quadra, bola de 0,24 m, sem colisores novos), `CourtDataTests`, `ScoringMathTests` | 🔄 — plano: `docs/etapas/etapa-6.6-ginasio-e-bola.md`, decisão D-026 |
| 7 5v5 | Quadra inteira, transição, inbound, backcourt, IA de 5 | Partida 5v5 completa (`FullCourtTests`, cena `02_FullCourt_5v5`) | ✅ (CI verde) — plano: `docs/etapas/etapa-7-5v5.md` |
| 8 Meta | Inventário, save versionado, economia, Gacha atrás de `IEconomyService` | Testes de pity/taxas com seed (`GachaTests`, `SaveSystemTests`) | ✅ (CI verde: 243 EditMode + 52 PlayMode) — plano: `docs/etapas/etapa-8-meta.md` |
| **8.5 Meta consolidação** | Liga o meta (Etapa 8) ao fluxo real: fim de partida, recompensas, elenco do perfil, save nos pontos certos, telas placeholder | Ciclo completo testado (partida → recompensa → save sobrevive fechar/reabrir → gacha muda o elenco) | ✅ — plano: `docs/etapas/etapa-8.5-meta-consolidacao.md` |
| 9 História | Capítulos/diálogos/partidas-objetivo data-driven | Capítulo de teste | — |
| 10 Polish | VFX anime, câmera cinemática, UI final, áudio, otimização | — | — |

Decisões: `docs/decisoes.md`. Arquitetura: `docs/arquitetura.md`.
Próximas tarefas: `docs/proximos-passos.md`. Guia para sessões do Claude: `CLAUDE.md`.
