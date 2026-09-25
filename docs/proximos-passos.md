# Próximos passos

Estado em 25/09/2026: etapas 0–8 prontas, CI verde (EditMode 243/243, PlayMode 52/52).
As tarefas abaixo são independentes; a ordem sugerida é A → B → C. D depende do usuário.

Para cada uma: escreva o plano em `docs/etapas/`, adicione/atualize a linha no
`docs/roadmap-mestre.md`, cubra com testes e registre decisões em `docs/decisoes.md`.

---

## A. Ligar o meta (Etapa 8) ao fluxo do jogo

Hoje `Basket.Meta` existe e está testado, mas **nada no jogo o usa**.

1. **Fim de partida.** Não existe evento de fim: `MatchManager` só muda `State.Phase` para
   `MatchPhase.Ended` (`Scripts/Core/MatchPhase.cs`). Acrescente
   `event Action<MatchState> OnMatchEnded` em `Gameplay/MatchManager.cs`, disparado uma vez
   quando a fase vira `Ended`, e teste em EditMode.
2. **Serviço de perfil em runtime.** Um componente (novo assembly ou dentro de `Bootstrap`,
   que pode passar a referenciar `Basket.Meta`) que:
   - carrega o save com `SaveService` + `FileSaveStorage`
     (`Application.persistentDataPath`) + `JsonSaveSerializer` → `PlayerProfile.FromSave`;
   - trata `SaveLoadStatus` (`NewerVersion` = não sobrescrever; avisar);
   - salva em pontos seguros (fim de partida, depois do gacha, ao sair).
3. **Recompensas.** No `OnMatchEnded`: `MatchRewardRules.For(placarDoTime, placarAdversário)`
   → `RewardGranter.Grant(...)`, com XP só para os personagens que jogaram.
   Dados: `Data/Meta/MatchRewardRules.asset`.
4. **Time vindo do perfil.** Hoje o elenco vem do asset `MatchSetup` no `GameBootstrap`
   (`Bootstrap/GameBootstrap.cs`, loop em `matchSetup.slots`). Crie o `MatchSetup` em runtime
   a partir dos personagens do `Inventory` (nível, LB e dupes da instância), mantendo o asset
   como fallback.
5. **Telas mínimas (placeholder).** Inventário, gacha (usa `GachaService.Pull` e só lê o
   `PullOutcome`) e resultado da partida. Visual provisório — a UI final é da Etapa 10.
   A UI só conhece `Core` hoje; se precisar ler o meta, decida (e registre) se a UI passa a
   referenciar `Basket.Meta` ou se recebe dados por interface em `Core`.

**Pronto quando:** jogar uma partida dá moedas/XP, o save sobrevive a fechar e abrir o jogo,
uma tiragem no gacha adiciona o personagem ao time. Testes PlayMode para o ciclo completo.

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

- Arena e jogadores placeholder montados em runtime (D-003, provisória).
- Animação é procedural até existirem clipes (D-019): basta preencher os `clips` do
  `CharacterVisualDefinition`.
- Valores provisórios: gacha (P-003), Limit Breaks (P-002), faltas (D-012), precisão (D-010).
