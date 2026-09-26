# Etapa 8 — Meta game (inventário, economia, gacha, progressão, recompensas, save)

## Separação (briefing, seções 26–28)
| Parte | Onde | Depende de |
|---|---|---|
| Dados de itens (moedas, materiais, itens de evolução e de Limit Break) | `ItemDefinition`, `ItemCatalog` (assets em `Data/Meta`) | — |
| Inventário (itens + personagens) | `Meta/Inventory.cs` — é também a carteira (`IItemWallet`) dos Limit Breaks | Characters |
| Economia (gastar tudo-ou-nada, conceder, eventos de transação) | `IEconomyService` / `EconomyService` | Inventário |
| Regras de obtenção (novo → dupe até 6 → conversão em item) | `CharacterObtainRules`, `CharacterObtainer` | Economia, ProgressionConfig |
| Dados de banner (raridades, taxas, pool, destaque, pity, garantia de multi, custos) | `BannerDefinition` | — |
| Sistema técnico do gacha (sorteio com pity, RNG injetável) | `GachaEngine` (puro) | Banner |
| Serviço de gacha (cobra, sorteia, concede, histórico) | `GachaService` | Economia, obtenção, engine |
| Progressão (itens de XP sem desperdício, Limit Break com materiais) | `CharacterUpgrades` | Inventário, CharacterProgression |
| Recompensas (itens, personagens, XP para quem jogou) | `Reward`, `RewardDefinition`, `MatchRewardRules`, `RewardGranter` | Economia, obtenção |
| Save (versionado, checksum, gravação atômica + backup, migração) | `PlayerSave`, `SaveService`, `FileSaveStorage`, `JsonSaveSerializer` | — |
| Jogador vivo (inventário + economia + gacha + configurações/história ↔ save) | `PlayerProfile` | tudo acima |
| Apresentação (tela de gacha, animação de pull) | **fora** — lê `PullOutcome` | — |

## Gacha (valores provisórios — P-003)
- 3 níveis de raridade genéricos (`rarity_3/2/1`), taxas 3% / 17% / 80%.
- Pity: garantido no 80º pull; *soft pity* a partir do 65º (+6% por pull). Contadores por **grupo de pity**
  (banners do mesmo grupo compartilham).
- Destaque 50/50: perdeu, o próximo do nível mais alto é o destacado.
- Multi (10): sempre ≥ 1 do nível 2 ou melhor; custa o preço de multi.
- Duplicatas pelas regras de obtenção; histórico dos últimos 1000 pulls com contador de pity.

## Save
Envelope `{versão, checksum FNV-1a, payload JSON}`; escreve em arquivo temporário e troca, mantendo o
anterior como `.bak`; save corrompido → backup → perfil novo; save de versão mais nova **não** é
sobrescrito (status `NewerVersion`). Migrações por versão em `SaveMigrator`. Guarda personagens (nível,
XP, dupes, Limit Break), inventário, moedas, gacha (pity + histórico), configurações, progresso da
história (Etapa 9) e partida em andamento.

## Testes
- EditMode puros (semente fixa): taxas convergem aos dados (200 mil pulls), hard pity nunca excedido,
  soft pity, garantia pós-50/50, garantia de multi, custo e "sem saldo não muda nada", dupes até 6 e
  conversão, histórico, mesma semente = mesmos resultados, grupos de pity; economia tudo-ou-nada; itens de
  XP só até o teto; Limit Break com materiais; recompensas de partida; save (round trip, backup em
  corrupção, perfil novo, versão mais nova, migração, checksum, arquivo com backup e sem temporário).
- EditMode no Unity: JSON real (JsonUtility) ida e volta; assets enviados consistentes (todo item
  referenciado existe no catálogo, banner válido, um multi real funciona).

## Ainda não ligado
Telas (Etapa 10), recompensas no fim da partida e personagens do perfil no `MatchSetup` (ligação do
fluxo de jogo) — o serviço está pronto para isso.

**Resolvido na Etapa 8.5** (`docs/etapas/etapa-8.5-meta-consolidacao.md`): recompensas no fim da
partida e personagens do perfil no `MatchSetup` já estão ligados via `GameBootstrap` +
`ProfileRuntimeService`. As telas placeholder (inventário, resultado de partida/gacha) também
existem (`ProfileHud`); a UI final continua na Etapa 10.
