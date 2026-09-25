# Etapa 8.5 — Ligar o meta ao fluxo do jogo

Consolidação, não etapa nova: os sistemas da Etapa 8 (`Basket.Meta`) já existem e estão testados, mas
nada no jogo os usa (`docs/proximos-passos.md`, item A). Esta etapa liga os pontos.

## Decisões novas (registradas em `docs/decisoes.md`)

- **UI só continua conhecendo Core.** Em vez de a UI passar a referenciar `Basket.Meta` (uma exceção à
  regra "AI/Input/UI só conhecem Core"), o padrão já usado para o gameplay (`IMatchState`,
  `IBallStateReadOnly` em Core, implementados do lado de baixo, lidos pela UI) se estende ao meta: nova
  interface `IPlayerProfileReadOnly` em Core (saldo, itens, personagens possuídos), implementada por
  `PlayerProfile`.
- **`CharacterCatalog` (SO novo, `Basket.Meta`).** Não existe hoje um `characterId → CharacterDefinition`
  confiável em runtime — `BannerDefinition.pool` referencia personagens, mas só cobre quem está em algum
  banner ativo, não é uma fonte completa. Catálogo dedicado, espelhando `ItemCatalog`.
- **Coordenador novo é POCO dentro de `Basket.Meta`, não dentro do `GameBootstrap`.** Seguindo o mesmo
  molde de `SaveService`/`EconomyService` (construtor com injeção, sem `MonoBehaviour`, testável sem
  cena). O Bootstrap continua só compondo — instancia e chama nos pontos certos, não ganha lógica nova.

## Separação

| Parte | Onde | Depende de |
|---|---|---|
| Leitura só-leitura do perfil para a UI | `IPlayerProfileReadOnly` (`Core`) | — |
| Catálogo de personagens (id → definição) | `CharacterCatalog` (asset, `Data/Meta`) | Characters |
| Coordenador de perfil em runtime (carrega/salva, monta `MatchSetup` do `Inventory`, aplica recompensa) | `ProfileRuntimeService` (`Meta`) | `SaveService`, `PlayerProfile`, `CharacterCatalog`, `MatchRewardRules`, `RewardGranter` |
| Fim de partida observável | `MatchManager.OnMatchEnded` (`Gameplay`) — wrapper fino sobre `MatchState.OnPhaseChanged` filtrando `Ended` | `MatchState` (já existe) |
| Ligação | `GameBootstrap` passa a referenciar `Basket.Meta` (mudança sancionada, antes ausente de propósito) | tudo acima |
| Telas placeholder (inventário, resultado de gacha, resultado de partida) | `Basket.UI`, estilo `DebugHud` (IMGUI, não é a UI final — Etapa 10) | `IPlayerProfileReadOnly` |

## Fluxo

1. `GameBootstrap.Awake`: `ProfileRuntimeService` carrega o save (ou cria perfil novo). Tenta montar um
   `MatchSetup` a partir de `Inventory.Characters` (via `CharacterCatalog`); se o perfil não tiver
   personagens suficientes para os slots exigidos, cai para o asset `MatchSetup` configurado no Bootstrap
   (fallback, como o comentário no próprio `MatchSetup.PlayerSlot` já antecipava).
2. Partida roda normal.
3. `MatchManager.OnMatchEnded` dispara uma vez → `ProfileRuntimeService.MatchEnded(...)`: chama
   `MatchRewardRules.For(placarPróprio, placarAdversário)` → `RewardGranter.Grant(...)` com XP só para
   quem jogou (ids das slots controladas, humano incluído) → salva o perfil.
4. Tela de resultado (placeholder) mostra o que foi ganho.
5. Tela de gacha (placeholder) chama `GachaService.Pull` via `ProfileRuntimeService`, mostra o
   `PullOutcome`, salva depois.
6. Ao sair (`OnApplicationQuit` ou equivalente), salva de novo por segurança.

## Testes

- EditMode: `ProfileRuntimeService` isolado (POCO, sem cena) — carregar/criar perfil, montar `MatchSetup`
  a partir do inventário (com e sem personagens suficientes → fallback), aplicar recompensa e checar XP só
  em quem jogou, `IPlayerProfileReadOnly` reflete o estado real do `PlayerProfile`.
- PlayMode novo (não existe nenhum hoje tocando `Basket.Meta`): ciclo completo — partida termina → recompensa
  aplicada → save sobrevive fechar/reabrir simulado (`FileSaveStorage` real, diretório de teste) → gacha
  adiciona personagem → próxima partida usa o elenco atualizado. "Pronto quando" do
  `docs/proximos-passos.md` é o critério de aceite direto.

## Fora de escopo (fica para depois)

UI final (Etapa 10), Etapa 9 (história) e Modo B (P-001, decisão pendente do usuário) não mudam aqui.
