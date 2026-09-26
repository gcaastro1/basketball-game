# Próximos passos

Estado em 26/09/2026: etapas 0–8.5 prontas (EditMode 258/258, PlayMode 52/53 — a única falha é
pré-existente e sensível à seed em `AISimulationTests.AIvsAI_3v3_PlaysBasketball`, ver
"Pendências menores conhecidas" abaixo). Item A concluído (Etapa 8.5); seguem B → C. D depende
do usuário.

Para cada uma: escreva o plano em `docs/etapas/`, adicione/atualize a linha no
`docs/roadmap-mestre.md`, cubra com testes e registre decisões em `docs/decisoes.md`.

---

## A. Ligar o meta (Etapa 8) ao fluxo do jogo — ✅ feito (Etapa 8.5)

`Basket.Meta` existia e estava testado, mas nada no jogo o usava. Os 5 sub-itens abaixo foram
feitos na Etapa 8.5 (`docs/etapas/etapa-8.5-meta-consolidacao.md`, `docs/decisoes.md` D-021):

1. **Fim de partida.** ✅ `event Action<MatchState> OnMatchEnded` em `Gameplay/MatchManager.cs`,
   disparado uma vez quando a fase vira `Ended` (`MatchManagerOnMatchEndedTests`).
2. **Serviço de perfil em runtime.** ✅ `Meta/ProfileRuntimeService.cs` (POCO): carrega o save
   com `SaveService` + `FileSaveStorage` (`Application.persistentDataPath`, com override para
   testes — `GameBootstrap.SaveDirectoryOverride`) + `JsonSaveSerializer` → `PlayerProfile.FromSave`;
   trata `SaveLoadStatus.NewerVersion` (não sobrescreve; `GameBootstrap.Awake` avisa com
   `Debug.LogWarning`); salva no fim de partida e em `OnDestroy`.
3. **Recompensas.** ✅ `GameBootstrap` chama `ProfileRuntimeService.ApplyMatchReward` no
   `OnMatchEnded`, que usa `MatchRewardRules.For(...)` → `RewardGranter.Grant(...)` com XP só
   para quem jogou. Dados: `Data/Meta/MatchRewardRules.asset`. A cadeia de I/O de disco dentro do
   handler tem `try/catch` no composition root (achado I6 da revisão final).
4. **Time vindo do perfil.** ✅ `GameBootstrap.Awake` monta um `MatchSetup` em runtime a partir de
   `ProfileRuntimeService.BuildRosterOrNull`, com o asset do Inspector como fallback. Catálogo
   novo `CharacterCatalog` (`Data/Meta/CharacterCatalog.asset`) dá o `characterId → CharacterDefinition`
   que faltava.
5. **Telas mínimas (placeholder).** ✅ `ProfileHud` (IMGUI, estilo `DebugHud`) mostra saldo,
   elenco e o último resultado de partida/gacha. Decisão registrada (D-021): UI continua só
   conhecendo `Core`, via `IPlayerProfileReadOnly`.

**Pronto quando:** jogar uma partida dá moedas/XP, o save sobrevive a fechar e abrir o jogo, uma
tiragem no gacha adiciona o personagem ao time — coberto por `MetaIntegrationTests` (PlayMode).

**Ressalva (revisão final da Etapa 8.5, achados PARKED, fora desta rodada):** nenhum teste ainda
cobre a integração real `GameBootstrap` → `OnMatchEnded` → recompensa de ponta a ponta (I2;
`MetaIntegrationTests` chama `ApplyMatchReward` direto, não via `MatchManager`/cena), e
`ProfileRuntimeService.Pull`/`ProfileHud.ShowGachaResult` não têm nenhum chamador de produção — a
tela de gacha nunca é de fato acionável em jogo (I3). Candidatos a uma etapa-8.6 curta.

## B. Etapa 9 — framework de história

Briefing, seção 25: **não escrever história**, só a arquitetura (capítulos, diálogos,
personagens, cenas/cutscenes, escolhas opcionais, partidas com objetivos, recompensas,
progressão, desbloqueios). Saída do roadmap: "capítulo de teste" data-driven.

Sugestão:
- assembly `Basket.Story` (Core + Characters + Meta);
- SOs `StoryChapter` → lista de nós (`DialogueNode`, `MatchNode` com `MatchSetup` +
  `MatchRules` + objetivos, `RewardNode` com `RewardDefinition`, `UnlockNode`);
- objetivos como interface (`IMatchObjective`: vencer, marcar X pontos, vencer com personagem Y…);
- progresso já tem lugar no save: `PlayerSave.storyChapter` e `storyFlags`;
- um capítulo de teste com textos placeholder claramente marcados como tal.

## C. Polish (Etapa 10) — só depois de A e B

VFX, câmera cinemática para habilidades, UI final, áudio, otimização. Precisa de decisões
de arte do usuário.

## D. Modo B — controle tático do time (P-001, pendente)

Briefing, seção 22. **Não implemente sem o usuário decidir** o que o Modo B é (chamar
jogadas? trocar o jogador controlado? pausar e dar ordens?). Pontos de encaixe já prontos:
`ITeamStrategy` (camada acima do `TeamBrain`) e `IAgentController`.

---

## Pendências menores conhecidas

- **IA 3x3 depois de uma cesta.** O jogador que repõe a bola sai reto para limpar além do
  arco, e o defensor é posicionado exatamente ali (`MatchSimulation`, reinício
  `UnderBasket`); correndo contra ele, posses travavam até roubo ou violação de shot clock
  (até 3 por jogo na `Timeline` do `AISimulationTests`). Agora ele contorna quem está no
  caminho (mesma esquiva do jogo em equipe). Tentativa mais agressiva (limpar para o lado
  mais vazio + defensor da bola sempre em sprint) virou "limpar e arremessar livre na
  hora", derrubou os passes e piorou o 5v5; foi revertida. Refinar jogando no editor,
  olhando passes/arremessos/violações por minuto no 3v3 e no 5v5 juntos.
- O teste `AIvsAI_3v3_PlaysBasketball` fica perto do limite (16 arremessos em 120 s; já
  deu 12, 16 e 28 no mesmo código). Faltas de arremesso com 2–3 lances livres consomem
  ~10 s cada e pesam muito na contagem.

- Arena e jogadores placeholder montados em runtime (D-003, provisória).
- Animação é procedural até existirem clipes (D-019): basta preencher os `clips` do
  `CharacterVisualDefinition`.
- Valores provisórios: gacha (P-003), Limit Breaks (P-002), faltas (D-012), precisão (D-010).
