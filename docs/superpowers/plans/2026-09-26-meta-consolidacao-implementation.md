# Etapa 8.5 — Meta Consolidação Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ligar `Basket.Meta` (Etapa 8, já testado) ao fluxo real do jogo: fim de partida observável, recompensas aplicadas, elenco vindo do perfil salvo (com fallback), save nos pontos certos, telas placeholder.

**Architecture:** Um POCO novo (`ProfileRuntimeService`, em `Basket.Meta`, mesmo molde de `SaveService`/`EconomyService`) coordena tudo; `GameBootstrap` só instancia e chama nos pontos certos. A UI continua só conhecendo `Core` — `ProfileRuntimeService` expõe tipos de resumo definidos em `Core` (`MatchRewardSummary`, `GachaPullSummary`), nunca os tipos concretos de `Basket.Meta` (`ObtainResult`, `PullOutcome`), então não existe exceção à regra de dependência.

**Tech Stack:** Unity 6000.6.2f1, Unity Test Framework (EditMode para lógica pura, PlayMode para o ciclo completo).

**Spec:** [docs/etapas/etapa-8.5-meta-consolidacao.md](../../etapas/etapa-8.5-meta-consolidacao.md) (decisões registradas em [docs/decisoes.md](../../decisoes.md), entrada D-021)

## Global Constraints

- `Basket.UI` continua referenciando só `Basket.Core` — nenhum tipo de `Basket.Meta` cruza essa fronteira, nem como parâmetro/retorno.
- `Basket.Meta` continua referenciando só `Basket.Core` e `Basket.Characters` — nunca `Gameplay`/`AI`/`Input`/`UI`.
- `ProfileRuntimeService` é POCO (sem `MonoBehaviour`), testável sem cena, seguindo o molde de `SaveService`/`EconomyService`.
- `GameBootstrap` continua sendo só composition root — a lógica nova mora em `ProfileRuntimeService`, não em `GameBootstrap`.
- Comandos de verificação (reusados em toda task):
  - **EDITMODE_TESTS**: `"/c/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe" -batchmode -runTests -nographics -projectPath "D:\Projetos\basket" -testPlatform EditMode -testResults "D:\Projetos\basket\Logs\editmode-results.xml" -logFile "D:\Projetos\basket\Logs\editmode.log"` — **sem `-quit`** (já confirmado neste projeto: `-quit` corre com `-runTests` e derruba o arquivo de resultado silenciosamente).
  - **PLAYMODE_TESTS**: mesmo comando com `-testPlatform PlayMode` / `playmode-results.xml` / `playmode.log`, também sem `-quit`.
  - **COMPILE_CHECK**: `"/c/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe" -batchmode -quit -nographics -projectPath "D:\Projetos\basket" -logFile "D:\Projetos\basket\Logs\compile.log"` (este usa `-quit` normalmente, não envolve `-runTests`).
  - Confirme antes de cada rodada que o Editor não está aberto (`tasklist | grep Unity.exe` no Bash) — batchmode não abre o mesmo projeto duas vezes.
- Estado atual antes desta etapa: EditMode 245/245, PlayMode 52/52 (confirme os números atuais rodando as suites antes da Task 1, já que outras mudanças podem ter alterado a contagem).

---

### Task 1: Tipos novos em `Core`

**Files:**
- Create: `Assets/_Project/Scripts/Core/IPlayerProfileReadOnly.cs`
- Create: `Assets/_Project/Scripts/Core/MatchRewardSummary.cs`
- Create: `Assets/_Project/Scripts/Core/GachaPullSummary.cs`
- Test: `Assets/_Project/Tests/EditMode/CoreProfileTypesTests.cs`

**Interfaces:**
- Consumes: nada (tipos novos, sem dependência).
- Produces: `IPlayerProfileReadOnly` (implementada por `PlayerProfile` na Task 2); `MatchRewardSummary`/`GachaPullSummary` (retornados por `ProfileRuntimeService` nas Tasks 7/8, lidos pela UI na Task 10).

- [ ] **Step 1: Escrever o teste que falha**

`Assets/_Project/Tests/EditMode/CoreProfileTypesTests.cs`:
```csharp
using System.Collections.Generic;
using NUnit.Framework;
using Basket.Core;

public class CoreProfileTypesTests
{
    [Test]
    public void MatchRewardSummary_StoresAllFieldsExactly()
    {
        var items = new List<string> { "currency_gems x150" };
        var characters = new List<string> { "ace (New)" };

        var summary = new MatchRewardSummary(won: true, itemsGranted: items, charactersObtained: characters);

        Assert.IsTrue(summary.Won);
        Assert.AreEqual(items, summary.ItemsGranted);
        Assert.AreEqual(characters, summary.CharactersObtained);
    }

    [Test]
    public void GachaPullSummary_StoresAllFieldsExactly()
    {
        var results = new List<string> { "ace (Dupe)" };

        var summary = new GachaPullSummary(success: true, failureReason: null, resultDescriptions: results);

        Assert.IsTrue(summary.Success);
        Assert.IsNull(summary.FailureReason);
        Assert.AreEqual(results, summary.ResultDescriptions);
    }
}
```

- [ ] **Step 2: Rodar EDITMODE_TESTS pra confirmar que falha**

Esperado: erro de compilação (`MatchRewardSummary`/`GachaPullSummary` não existem ainda).

- [ ] **Step 3: Implementação mínima**

`Assets/_Project/Scripts/Core/IPlayerProfileReadOnly.cs`:
```csharp
using System.Collections.Generic;

namespace Basket.Core
{
    // Superfície somente-leitura do perfil do jogador, pra UI (que só conhece Core) ler sem
    // referenciar Basket.Meta. Implementada por PlayerProfile.
    public interface IPlayerProfileReadOnly
    {
        IEnumerable<KeyValuePair<string, long>> Items { get; }
        long Amount(string itemId);
        IEnumerable<string> OwnedCharacterIds { get; }
        bool OwnsCharacter(string characterId);
        int CharacterLevel(string characterId);
    }
}
```

`Assets/_Project/Scripts/Core/MatchRewardSummary.cs`:
```csharp
using System.Collections.Generic;

namespace Basket.Core
{
    // Resumo pronto-para-exibir do que uma partida deu. ProfileRuntimeService (Basket.Meta)
    // monta isso a partir dos tipos concretos dele (Reward, ObtainResult) antes de devolver --
    // a UI nunca vê os tipos de Meta.
    public readonly struct MatchRewardSummary
    {
        public readonly bool Won;
        public readonly IReadOnlyList<string> ItemsGranted;
        public readonly IReadOnlyList<string> CharactersObtained;

        public MatchRewardSummary(bool won, IReadOnlyList<string> itemsGranted, IReadOnlyList<string> charactersObtained)
        {
            Won = won;
            ItemsGranted = itemsGranted;
            CharactersObtained = charactersObtained;
        }
    }
}
```

`Assets/_Project/Scripts/Core/GachaPullSummary.cs`:
```csharp
using System.Collections.Generic;

namespace Basket.Core
{
    // Mesma ideia de MatchRewardSummary, pro resultado de um pull de gacha (PullOutcome é
    // Basket.Meta, não cruza pra UI).
    public readonly struct GachaPullSummary
    {
        public readonly bool Success;
        public readonly string FailureReason;
        public readonly IReadOnlyList<string> ResultDescriptions;

        public GachaPullSummary(bool success, string failureReason, IReadOnlyList<string> resultDescriptions)
        {
            Success = success;
            FailureReason = failureReason;
            ResultDescriptions = resultDescriptions;
        }
    }
}
```

- [ ] **Step 4: Rodar EDITMODE_TESTS pra confirmar que passa**

Esperado: 2 testes novos, `Passed`.

- [ ] **Step 5: Commit**

```bash
cd "/d/Projetos/basket"
git add Assets/_Project/Scripts/Core/IPlayerProfileReadOnly.cs Assets/_Project/Scripts/Core/MatchRewardSummary.cs Assets/_Project/Scripts/Core/GachaPullSummary.cs Assets/_Project/Tests/EditMode/CoreProfileTypesTests.cs
git commit -m "feat(core): add IPlayerProfileReadOnly, MatchRewardSummary, GachaPullSummary"
```

---

### Task 2: `PlayerProfile` implementa `IPlayerProfileReadOnly`

**Files:**
- Modify: `Assets/_Project/Scripts/Meta/PlayerProfile.cs`
- Test: `Assets/_Project/Tests/EditMode/PlayerProfileReadOnlyTests.cs`

**Interfaces:**
- Consumes: `IPlayerProfileReadOnly` (Task 1); `Inventory` (já existe: `Items`, `Amount(itemId)`, `Owns(characterId)`, `GetCharacter(characterId)`, `Characters`).
- Produces: `PlayerProfile : IPlayerProfileReadOnly` — consumido pela UI (Task 10) através da interface.

**Antes de implementar:** leia `Assets/_Project/Scripts/Meta/PlayerProfile.cs` e `Assets/_Project/Scripts/Meta/Inventory.cs` inteiros — confirme os nomes exatos usados abaixo (`Inventory.Items`, `Inventory.Amount`, `Inventory.Owns`, `Inventory.GetCharacter`, `Inventory.Characters`, e se `CharacterInstance` tem mesmo um campo `characterId` e `level` acessíveis) antes de escrever o código; se algum nome divergir do que está aqui, use o nome real e ajuste os passos seguintes.

- [ ] **Step 1: Escrever o teste que falha**

`Assets/_Project/Tests/EditMode/PlayerProfileReadOnlyTests.cs`:
```csharp
using System.Linq;
using NUnit.Framework;
using Basket.Core;
using Basket.Meta;

public class PlayerProfileReadOnlyTests
{
    [Test]
    public void PlayerProfile_ImplementsIPlayerProfileReadOnly_ReflectingRealState()
    {
        var profile = new PlayerProfile();
        profile.Inventory.Add("currency_gems", 100);
        var instance = profile.Inventory.AddCharacter(new Basket.Characters.CharacterInstance("ace", level: 5));

        IPlayerProfileReadOnly readOnly = profile;

        Assert.AreEqual(100, readOnly.Amount("currency_gems"));
        Assert.IsTrue(readOnly.OwnsCharacter("ace"));
        Assert.IsFalse(readOnly.OwnsCharacter("nobody"));
        Assert.AreEqual(5, readOnly.CharacterLevel("ace"));
        Assert.AreEqual(0, readOnly.CharacterLevel("nobody"));
        CollectionAssert.Contains(readOnly.OwnedCharacterIds.ToList(), "ace");
    }
}
```

- [ ] **Step 2: Rodar EDITMODE_TESTS pra confirmar que falha**

Esperado: erro de compilação (`PlayerProfile` ainda não implementa `IPlayerProfileReadOnly`).

- [ ] **Step 3: Implementação mínima**

Em `Assets/_Project/Scripts/Meta/PlayerProfile.cs`, adicione `using Basket.Core;` no topo, mude a declaração da classe para `public sealed class PlayerProfile : IPlayerProfileReadOnly`, e adicione estes membros (delegando pro `Inventory` que já existe como propriedade):
```csharp
public IEnumerable<KeyValuePair<string, long>> Items => Inventory.Items;

public long Amount(string itemId) => Inventory.Amount(itemId);

public bool OwnsCharacter(string characterId) => Inventory.Owns(characterId);

public IEnumerable<string> OwnedCharacterIds
{
    get
    {
        foreach (var character in Inventory.Characters) yield return character.characterId;
    }
}

public int CharacterLevel(string characterId)
{
    var character = Inventory.GetCharacter(characterId);
    return character?.level ?? 0;
}
```
(Se `Inventory.Items` não devolver `IEnumerable<KeyValuePair<string, long>>` exatamente, ajuste a assinatura de `Items` pra bater com o tipo real, e ajuste `IPlayerProfileReadOnly.Items` na Task 1 do mesmo jeito antes de prosseguir.)

- [ ] **Step 4: Rodar EDITMODE_TESTS pra confirmar que passa**

Esperado: 1 teste novo, `Passed`; nenhum teste existente de `Basket.Meta` quebrou (rode a suite completa, não só o arquivo novo).

- [ ] **Step 5: Commit**

```bash
cd "/d/Projetos/basket"
git add Assets/_Project/Scripts/Meta/PlayerProfile.cs Assets/_Project/Tests/EditMode/PlayerProfileReadOnlyTests.cs
git commit -m "feat(meta): PlayerProfile implements IPlayerProfileReadOnly"
```

---

### Task 3: `CharacterCatalog`

**Files:**
- Create: `Assets/_Project/Scripts/Meta/CharacterCatalog.cs`
- Test: `Assets/_Project/Tests/EditMode/CharacterCatalogTests.cs`

**Interfaces:**
- Consumes: `CharacterDefinition` (já existe em `Basket.Characters`, tem campo `characterId`).
- Produces: `CharacterCatalog.Find(string characterId) : CharacterDefinition` — consumido por `ProfileRuntimeService.BuildRosterOrNull` (Task 6).

**Antes de implementar:** confirme em `Assets/_Project/Scripts/Characters/CharacterDefinition.cs` que o campo/propriedade se chama mesmo `characterId` (é usado assim em `GameBootstrap.ApplyCharacter`, mas confirme antes de copiar).

- [ ] **Step 1: Escrever o teste que falha**

`Assets/_Project/Tests/EditMode/CharacterCatalogTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;
using Basket.Characters;
using Basket.Meta;

public class CharacterCatalogTests
{
    [Test]
    public void Find_KnownId_ReturnsDefinition()
    {
        var ace = ScriptableObject.CreateInstance<CharacterDefinition>();
        ace.characterId = "ace";
        var catalog = ScriptableObject.CreateInstance<CharacterCatalog>();
        catalog.characters.Add(ace);

        CharacterDefinition found = catalog.Find("ace");

        Assert.AreEqual(ace, found);
    }

    [Test]
    public void Find_UnknownId_ReturnsNull()
    {
        var catalog = ScriptableObject.CreateInstance<CharacterCatalog>();

        Assert.IsNull(catalog.Find("nobody"));
    }
}
```

- [ ] **Step 2: Rodar EDITMODE_TESTS pra confirmar que falha**

Esperado: erro de compilação (`CharacterCatalog` não existe).

- [ ] **Step 3: Implementação mínima**

`Assets/_Project/Scripts/Meta/CharacterCatalog.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;
using Basket.Characters;

namespace Basket.Meta
{
    // characterId -> CharacterDefinition em runtime. Não existia antes: BannerDefinition.pool
    // referencia personagens, mas só cobre quem está em algum banner ativo, não é fonte completa
    // (D-021).
    [CreateAssetMenu(fileName = "CharacterCatalog", menuName = "Basket/Meta/Character Catalog")]
    public class CharacterCatalog : ScriptableObject
    {
        public List<CharacterDefinition> characters = new List<CharacterDefinition>();

        public CharacterDefinition Find(string characterId)
        {
            foreach (var character in characters)
            {
                if (character != null && character.characterId == characterId) return character;
            }
            return null;
        }
    }
}
```

- [ ] **Step 4: Rodar EDITMODE_TESTS pra confirmar que passa**

Esperado: 2 testes novos, `Passed`.

- [ ] **Step 5: Commit**

```bash
cd "/d/Projetos/basket"
git add Assets/_Project/Scripts/Meta/CharacterCatalog.cs Assets/_Project/Tests/EditMode/CharacterCatalogTests.cs
git commit -m "feat(meta): add CharacterCatalog (characterId -> CharacterDefinition lookup)"
```

---

### Task 4: `MatchManager.OnMatchEnded`

**Files:**
- Modify: `Assets/_Project/Scripts/Gameplay/MatchManager.cs`
- Test: `Assets/_Project/Tests/EditMode/MatchManagerOnMatchEndedTests.cs`

**Interfaces:**
- Consumes: `MatchState.OnPhaseChanged` (já existe: `event Action<MatchPhase, MatchPhase>`), `MatchState.EndWith(TeamId winner)` (já existe, `internal`).
- Produces: `MatchManager.OnMatchEnded : event Action<MatchState>` — consumido por `GameBootstrap` (Task 9).

**Antes de implementar:** leia `Assets/_Project/Scripts/Gameplay/MatchManager.cs` e `Assets/_Project/Scripts/Gameplay/MatchState.cs` inteiros. Confirme: o construtor de `MatchManager` é mesmo `public MatchManager(MatchRules matchRules, Vector3 rimCenter)`? `State` é `public MatchState State { get; }` (ou equivalente publicamente legível)? `MatchState.EndWith` é chamado de dentro do próprio `MatchState` (então `OnPhaseChanged` dispara com `next == MatchPhase.Ended` sozinho) ou precisa de outro gatilho? Ajuste os passos abaixo se algo divergir.

- [ ] **Step 1: Escrever o teste que falha**

`Assets/_Project/Tests/EditMode/MatchManagerOnMatchEndedTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;
using Basket.Core;
using Basket.Gameplay;

public class MatchManagerOnMatchEndedTests
{
    [Test]
    public void OnMatchEnded_FiresOnceWhenPhaseBecomesEnded_WithFinalState()
    {
        var rules = ScriptableObject.CreateInstance<MatchRules>();
        rules.winningScore = 5;
        var manager = new MatchManager(rules, Vector3.zero);

        int firedCount = 0;
        MatchState received = null;
        manager.OnMatchEnded += state => { firedCount++; received = state; };

        manager.State.EndWith(TeamId.Home);

        Assert.AreEqual(1, firedCount);
        Assert.AreEqual(manager.State, received);
        Assert.AreEqual(MatchPhase.Ended, received.Phase);
    }

    [Test]
    public void OnMatchEnded_DoesNotFireForOtherPhaseChanges()
    {
        var rules = ScriptableObject.CreateInstance<MatchRules>();
        var manager = new MatchManager(rules, Vector3.zero);

        int firedCount = 0;
        manager.OnMatchEnded += _ => firedCount++;

        manager.State.RegisterScore(TeamId.Home, 2, ShotType.Layup); // adjust to whatever real scoring method MatchState exposes if this name is wrong

        Assert.AreEqual(0, firedCount);
    }
}
```
(Se `MatchState.EndWith` for `internal` e o teste não compilar por causa disso, confirme se `Assets/_Project/Tests/EditMode/Basket.Tests.EditMode.asmdef` já tem `InternalsVisibleTo` pra `Basket.Gameplay` — se sim, o teste já devia funcionar; se não, ou se `EndWith` não existir com esse nome exato, ajuste o teste pra usar o caminho real que leva `MatchPhase` a `Ended` — por exemplo simulando uma pontuação que estoura `winningScore`, se for esse o gatilho real.)

- [ ] **Step 2: Rodar EDITMODE_TESTS pra confirmar que falha**

Esperado: erro de compilação (`OnMatchEnded` não existe em `MatchManager`).

- [ ] **Step 3: Implementação mínima**

Em `Assets/_Project/Scripts/Gameplay/MatchManager.cs`, adicione o evento e a inscrição no construtor:
```csharp
public event Action<MatchState> OnMatchEnded;
```
No construtor, logo depois de `State = new MatchState(matchRules.winningScore);` (ou equivalente):
```csharp
State.OnPhaseChanged += (previousPhase, nextPhase) =>
{
    if (nextPhase == MatchPhase.Ended) OnMatchEnded?.Invoke(State);
};
```

- [ ] **Step 4: Rodar EDITMODE_TESTS pra confirmar que passa**

Esperado: 2 testes novos, `Passed`; suite completa de `MatchManager`/`MatchState` ainda passa.

- [ ] **Step 5: Commit**

```bash
cd "/d/Projetos/basket"
git add Assets/_Project/Scripts/Gameplay/MatchManager.cs Assets/_Project/Tests/EditMode/MatchManagerOnMatchEndedTests.cs
git commit -m "feat(gameplay): add MatchManager.OnMatchEnded"
```

---

### Task 5: `ProfileRuntimeService` — carregar/criar/salvar

**Files:**
- Create: `Assets/_Project/Scripts/Meta/ProfileRuntimeService.cs`
- Test: `Assets/_Project/Tests/EditMode/ProfileRuntimeServiceTests.cs`

**Interfaces:**
- Consumes: `SaveService` (já existe: `Save(slot, save)`, `Load(slot) : SaveLoadResult`), `MemorySaveStorage`/`ISaveStorage`, `JsonSaveSerializer`, `PlayerProfile.FromSave`/`.ToSave()`, `ItemCatalog`.
- Produces: `ProfileRuntimeService.LoadOrCreate() : PlayerProfile`, `.Save()`, `.Profile { get; }`, `.LastLoadStatus { get; }` — consumidos por `GameBootstrap` (Task 9) e pelas próprias Tasks 6/7/8 (mesmo arquivo, métodos adicionados depois).

**Antes de implementar:** confirme em `Assets/_Project/Scripts/Meta/SaveService.cs` a assinatura exata de `SaveLoadResult` (campos `Save`/`Status`) e de `ISaveStorage`/`MemorySaveStorage` (usado no teste).

- [ ] **Step 1: Escrever o teste que falha**

`Assets/_Project/Tests/EditMode/ProfileRuntimeServiceTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;
using Basket.Meta;

public class ProfileRuntimeServiceTests
{
    private ProfileRuntimeService BuildService(MemorySaveStorage storage)
    {
        var saveService = new SaveService(storage, new JsonSaveSerializer());
        var itemCatalog = ScriptableObject.CreateInstance<ItemCatalog>();
        var characterCatalog = ScriptableObject.CreateInstance<CharacterCatalog>();
        var progression = ScriptableObject.CreateInstance<Basket.Characters.ProgressionConfig>();
        var obtainRules = ScriptableObject.CreateInstance<CharacterObtainRules>();
        var rewardRules = ScriptableObject.CreateInstance<MatchRewardRules>();
        return new ProfileRuntimeService(saveService, itemCatalog, characterCatalog, progression, obtainRules, rewardRules);
    }

    [Test]
    public void LoadOrCreate_NoExistingSave_CreatesNewProfile()
    {
        var service = BuildService(new MemorySaveStorage());

        PlayerProfile profile = service.LoadOrCreate();

        Assert.IsNotNull(profile);
        Assert.AreEqual(SaveLoadStatus.NewPlayer, service.LastLoadStatus);
    }

    [Test]
    public void Save_ThenLoadOrCreate_RoundTripsCharacters()
    {
        var storage = new MemorySaveStorage();
        var service = BuildService(storage);
        PlayerProfile profile = service.LoadOrCreate();
        profile.Inventory.AddCharacter(new Basket.Characters.CharacterInstance("ace", level: 3));

        service.Save();

        var reloadService = BuildService(storage); // same backing storage, fresh service (simulates reopening the game)
        PlayerProfile reloaded = reloadService.LoadOrCreate();
        Assert.IsTrue(reloaded.Inventory.Owns("ace"));
        Assert.AreEqual(3, reloaded.Inventory.GetCharacter("ace").level);
    }
}
```

- [ ] **Step 2: Rodar EDITMODE_TESTS pra confirmar que falha**

Esperado: erro de compilação (`ProfileRuntimeService` não existe).

- [ ] **Step 3: Implementação mínima**

`Assets/_Project/Scripts/Meta/ProfileRuntimeService.cs`:
```csharp
using System;
using Basket.Characters;

namespace Basket.Meta
{
    // Coordenador de perfil em runtime -- POCO, sem MonoBehaviour, mesmo molde de SaveService/
    // EconomyService (D-021). GameBootstrap só instancia e chama nos pontos certos.
    public sealed class ProfileRuntimeService
    {
        private const string SaveSlot = "profile";

        private readonly SaveService saveService;
        private readonly ItemCatalog itemCatalog;
        private readonly CharacterCatalog characterCatalog;
        private readonly ProgressionConfig progressionConfig;
        private readonly CharacterObtainRules obtainRules;
        private readonly MatchRewardRules rewardRules;
        private readonly Random rng;

        public PlayerProfile Profile { get; private set; }
        public SaveLoadStatus LastLoadStatus { get; private set; }

        public ProfileRuntimeService(SaveService saveService, ItemCatalog itemCatalog, CharacterCatalog characterCatalog,
            ProgressionConfig progressionConfig, CharacterObtainRules obtainRules, MatchRewardRules rewardRules, Random rng = null)
        {
            this.saveService = saveService;
            this.itemCatalog = itemCatalog;
            this.characterCatalog = characterCatalog;
            this.progressionConfig = progressionConfig;
            this.obtainRules = obtainRules;
            this.rewardRules = rewardRules;
            this.rng = rng ?? new Random();
        }

        public PlayerProfile LoadOrCreate()
        {
            SaveLoadResult result = saveService.Load(SaveSlot);
            LastLoadStatus = result.Status;
            // NewerVersion: não sobrescreve o save mais novo. Joga com um perfil novo em memória
            // esta sessão em vez de arriscar perder dados de uma build futura (Save() abaixo já
            // recusa gravar nesse caso).
            Profile = result.Status == SaveLoadStatus.NewerVersion
                ? new PlayerProfile(itemCatalog)
                : PlayerProfile.FromSave(result.Save, itemCatalog);
            return Profile;
        }

        public void Save()
        {
            if (Profile == null || LastLoadStatus == SaveLoadStatus.NewerVersion) return;
            saveService.Save(SaveSlot, Profile.ToSave());
        }
    }
}
```

- [ ] **Step 4: Rodar EDITMODE_TESTS pra confirmar que passa**

Esperado: 2 testes novos, `Passed`.

- [ ] **Step 5: Commit**

```bash
cd "/d/Projetos/basket"
git add Assets/_Project/Scripts/Meta/ProfileRuntimeService.cs Assets/_Project/Tests/EditMode/ProfileRuntimeServiceTests.cs
git commit -m "feat(meta): add ProfileRuntimeService (load/create/save)"
```

---

### Task 6: `ProfileRuntimeService` — montar elenco do perfil

**Files:**
- Modify: `Assets/_Project/Scripts/Meta/ProfileRuntimeService.cs`
- Test: `Assets/_Project/Tests/EditMode/ProfileRuntimeServiceTests.cs` (mesmo arquivo da Task 5, novos casos)

**Interfaces:**
- Consumes: `CharacterCatalog.Find` (Task 3), `Inventory.Characters` (já existe).
- Produces: `ProfileRuntimeService.BuildRosterOrNull(int requiredCount) : List<CharacterDefinition>` — consumido por `GameBootstrap` (Task 9).

- [ ] **Step 1: Escrever o teste que falha**

Adicione a `ProfileRuntimeServiceTests.cs`:
```csharp
    [Test]
    public void BuildRosterOrNull_EnoughOwnedCharacters_ReturnsThem()
    {
        var storage = new MemorySaveStorage();
        var service = BuildService(storage);
        PlayerProfile profile = service.LoadOrCreate();
        var ace = ScriptableObject.CreateInstance<Basket.Characters.CharacterDefinition>();
        ace.characterId = "ace";
        // service's CharacterCatalog is private -- rebuild with a catalog containing "ace" for this test
        var catalogWithAce = ScriptableObject.CreateInstance<CharacterCatalog>();
        catalogWithAce.characters.Add(ace);
        var serviceWithCatalog = new ProfileRuntimeService(
            new SaveService(storage, new JsonSaveSerializer()),
            ScriptableObject.CreateInstance<ItemCatalog>(), catalogWithAce,
            ScriptableObject.CreateInstance<Basket.Characters.ProgressionConfig>(),
            ScriptableObject.CreateInstance<CharacterObtainRules>(),
            ScriptableObject.CreateInstance<MatchRewardRules>());
        serviceWithCatalog.LoadOrCreate();
        serviceWithCatalog.Profile.Inventory.AddCharacter(new Basket.Characters.CharacterInstance("ace"));

        var roster = serviceWithCatalog.BuildRosterOrNull(requiredCount: 1);

        Assert.IsNotNull(roster);
        Assert.AreEqual(1, roster.Count);
        Assert.AreEqual(ace, roster[0]);
    }

    [Test]
    public void BuildRosterOrNull_NotEnoughOwnedCharacters_ReturnsNull()
    {
        var service = BuildService(new MemorySaveStorage());
        service.LoadOrCreate();

        var roster = service.BuildRosterOrNull(requiredCount: 1);

        Assert.IsNull(roster);
    }
```

- [ ] **Step 2: Rodar EDITMODE_TESTS pra confirmar que falha**

Esperado: erro de compilação (`BuildRosterOrNull` não existe).

- [ ] **Step 3: Implementação mínima**

Adicione em `ProfileRuntimeService.cs`:
```csharp
        using System.Collections.Generic; // adicione ao topo do arquivo se ainda não estiver lá

        // Elenco possível a partir do que o jogador possui de verdade. Devolve null (o chamador
        // cai para o MatchSetup configurado) se o perfil não tiver personagens suficientes ainda.
        public List<CharacterDefinition> BuildRosterOrNull(int requiredCount)
        {
            var owned = new List<CharacterDefinition>();
            foreach (CharacterInstance instance in Profile.Inventory.Characters)
            {
                CharacterDefinition definition = characterCatalog.Find(instance.characterId);
                if (definition != null) owned.Add(definition);
            }
            return owned.Count >= requiredCount ? owned : null;
        }
```

- [ ] **Step 4: Rodar EDITMODE_TESTS pra confirmar que passa**

Esperado: 2 testes novos, `Passed`.

- [ ] **Step 5: Commit**

```bash
cd "/d/Projetos/basket"
git add Assets/_Project/Scripts/Meta/ProfileRuntimeService.cs Assets/_Project/Tests/EditMode/ProfileRuntimeServiceTests.cs
git commit -m "feat(meta): ProfileRuntimeService.BuildRosterOrNull"
```

---

### Task 7: `ProfileRuntimeService` — aplicar recompensa de partida

**Files:**
- Modify: `Assets/_Project/Scripts/Meta/ProfileRuntimeService.cs`
- Test: `Assets/_Project/Tests/EditMode/ProfileRuntimeServiceTests.cs` (mesmo arquivo, novo caso)

**Interfaces:**
- Consumes: `MatchRewardRules.For(ownScore, opponentScore) : Reward` (já existe), `RewardGranter.Grant(reward, inventory, economy, progression, rules, reason, xpTo) : List<ObtainResult>` (já existe), `MatchRewardSummary` (Task 1).
- Produces: `ProfileRuntimeService.ApplyMatchReward(int ownScore, int opponentScore, IEnumerable<string> playedCharacterIds) : MatchRewardSummary` — consumido por `GameBootstrap` (Task 9) e `ProfileHud` (Task 10, via `GameBootstrap`).

**Antes de implementar:** confirme em `Assets/_Project/Scripts/Meta/Reward.cs` os nomes exatos dos campos de `ItemCost` (usado como `reward.items`) -- o relatório de exploração viu o uso `new ItemCost("currency_gems", 160)` mas não o código-fonte do struct/classe; leia `Assets/_Project/Scripts/Characters/ProgressionConfig.cs` (onde `ItemCost` mora) antes de escrever `ItemsGranted`. Confirme também o shape de `ObtainResult` (`Outcome`, `Instance`) em `Assets/_Project/Scripts/Meta/CharacterUpgrades.cs` ou onde `CharacterObtainer` estiver.

- [ ] **Step 1: Escrever o teste que falha**

Adicione a `ProfileRuntimeServiceTests.cs`:
```csharp
    [Test]
    public void ApplyMatchReward_Win_GrantsXpOnlyToPlayedCharacters()
    {
        var storage = new MemorySaveStorage();
        var service = BuildService(storage);
        service.LoadOrCreate();
        service.Profile.Inventory.AddCharacter(new Basket.Characters.CharacterInstance("ace"));
        service.Profile.Inventory.AddCharacter(new Basket.Characters.CharacterInstance("bench"));

        MatchRewardSummary summary = service.ApplyMatchReward(ownScore: 21, opponentScore: 15, playedCharacterIds: new[] { "ace" });

        Assert.IsTrue(summary.Won);
        Assert.Greater(service.Profile.Inventory.GetCharacter("ace").xp, 0);
        Assert.AreEqual(0, service.Profile.Inventory.GetCharacter("bench").xp);
    }
```

- [ ] **Step 2: Rodar EDITMODE_TESTS pra confirmar que falha**

Esperado: erro de compilação (`ApplyMatchReward` não existe).

- [ ] **Step 3: Implementação mínima**

Adicione em `ProfileRuntimeService.cs`:
```csharp
        using System.Linq; // adicione ao topo se ainda não estiver lá
        using Basket.Core; // adicione ao topo se ainda não estiver lá

        public MatchRewardSummary ApplyMatchReward(int ownScore, int opponentScore, IEnumerable<string> playedCharacterIds)
        {
            Reward reward = rewardRules.For(ownScore, opponentScore);
            List<ObtainResult> obtainResults = RewardGranter.Grant(reward, Profile.Inventory, Profile.Economy,
                progressionConfig, obtainRules, reason: "match_result", xpTo: playedCharacterIds);
            Save();

            var itemDescriptions = reward.items?.Select(cost => $"{cost.itemId} x{cost.amount}").ToList()
                ?? new List<string>();
            var characterDescriptions = obtainResults.Select(r => $"{r.Instance.characterId} ({r.Outcome})").ToList();

            return new MatchRewardSummary(won: ownScore > opponentScore, itemsGranted: itemDescriptions, charactersObtained: characterDescriptions);
        }
```

- [ ] **Step 4: Rodar EDITMODE_TESTS pra confirmar que passa**

Esperado: 1 teste novo, `Passed`.

- [ ] **Step 5: Commit**

```bash
cd "/d/Projetos/basket"
git add Assets/_Project/Scripts/Meta/ProfileRuntimeService.cs Assets/_Project/Tests/EditMode/ProfileRuntimeServiceTests.cs
git commit -m "feat(meta): ProfileRuntimeService.ApplyMatchReward"
```

---

### Task 8: `ProfileRuntimeService` — puxar gacha

**Files:**
- Modify: `Assets/_Project/Scripts/Meta/ProfileRuntimeService.cs`
- Test: `Assets/_Project/Tests/EditMode/ProfileRuntimeServiceTests.cs` (mesmo arquivo, novo caso)

**Interfaces:**
- Consumes: `PlayerProfile.CreateGacha(progression, rules, rng) : GachaService` (já existe), `GachaService.Pull(banner, count) : PullOutcome` (já existe), `GachaPullSummary` (Task 1).
- Produces: `ProfileRuntimeService.Pull(BannerDefinition banner, int count) : GachaPullSummary` — consumido por `GameBootstrap` (Task 9).

**Antes de implementar:** confirme que `PlayerProfile.CreateGacha` de fato passa `this.Gacha` (o `GachaSaveData` da própria instância) pro `GachaService` que ele constrói -- se cada chamada perder o pity acumulado entre pulls, é bug pra reportar, não pra contornar aqui. Confirme também `PullResult`'s campos (`CharacterId`, `Outcome`) pra montar a descrição.

- [ ] **Step 1: Escrever o teste que falha**

Adicione a `ProfileRuntimeServiceTests.cs`:
```csharp
    [Test]
    public void Pull_SuccessfulPull_SavesAndReturnsDescriptiveSummary()
    {
        var storage = new MemorySaveStorage();
        var service = BuildService(storage);
        service.LoadOrCreate();
        service.Profile.Economy.Grant("currency_gems", 1000, "test_setup");
        var banner = ScriptableObject.CreateInstance<BannerDefinition>();
        banner.singleCost = new Basket.Characters.ItemCost("currency_gems", 10);
        banner.rarities.Add(new GachaRarity { rarityId = "r1", rate = 1f });
        var character = ScriptableObject.CreateInstance<Basket.Characters.CharacterDefinition>();
        character.characterId = "ace";
        banner.pool.Add(new GachaEntry { character = character, rarityId = "r1", weight = 1f });

        GachaPullSummary summary = service.Pull(banner, count: 1);

        Assert.IsTrue(summary.Success);
        Assert.AreEqual(1, summary.ResultDescriptions.Count);
    }
```

- [ ] **Step 2: Rodar EDITMODE_TESTS pra confirmar que falha**

Esperado: erro de compilação (`Pull` não existe em `ProfileRuntimeService`).

- [ ] **Step 3: Implementação mínima**

Adicione em `ProfileRuntimeService.cs`:
```csharp
        public GachaPullSummary Pull(BannerDefinition banner, int count)
        {
            GachaService gacha = Profile.CreateGacha(progressionConfig, obtainRules, rng);
            PullOutcome outcome = gacha.Pull(banner, count);
            if (outcome.Success) Save();

            var descriptions = outcome.Results.Select(r => $"{r.CharacterId} ({(r.Featured ? "featured " : "")}{r.Outcome})").ToList();
            return new GachaPullSummary(success: outcome.Success,
                failureReason: outcome.Success ? null : outcome.Failure.ToString(),
                resultDescriptions: descriptions);
        }
```

- [ ] **Step 4: Rodar EDITMODE_TESTS pra confirmar que passa**

Esperado: 1 teste novo, `Passed`.

- [ ] **Step 5: Commit**

```bash
cd "/d/Projetos/basket"
git add Assets/_Project/Scripts/Meta/ProfileRuntimeService.cs Assets/_Project/Tests/EditMode/ProfileRuntimeServiceTests.cs
git commit -m "feat(meta): ProfileRuntimeService.Pull"
```

---

### Task 9: Ligar no `GameBootstrap`

**Files:**
- Modify: `Assets/_Project/Scripts/Bootstrap/Basket.Bootstrap.asmdef` (adicionar referência a `Basket.Meta`)
- Modify: `Assets/_Project/Scripts/Bootstrap/GameBootstrap.cs`

**Interfaces:**
- Consumes: `ProfileRuntimeService` (Tasks 5-8), `MatchManager.OnMatchEnded` (Task 4), `MatchSetup`/`MatchSetup.PlayerSlot` (já existe).
- Produces: nada de novo pro resto do código -- este é o ponto de ligação final.

**Antes de implementar:** leia `Assets/_Project/Scripts/Bootstrap/GameBootstrap.cs` inteiro de novo (o código deste projeto, não o da fatia vertical antiga) pra confirmar a ordem exata de `Awake()` e onde `Simulation` fica disponível, já que a assinatura de `OnMatchEnded` precisa de `Simulation.Match` já construído antes de assinar.

- [ ] **Step 1: Adicionar a referência de assembly**

Em `Assets/_Project/Scripts/Bootstrap/Basket.Bootstrap.asmdef`, adicione `"Basket.Meta"` à lista `"references"`.

- [ ] **Step 2: Rodar COMPILE_CHECK pra confirmar que ainda compila**

Esperado: zero `error CS` (mudança de asmdef isolada não deveria quebrar nada ainda).

- [ ] **Step 3: Adicionar os campos e a inicialização do perfil**

Em `GameBootstrap.cs`, adicione `using Basket.Meta;` e estes `[SerializeField]` (mesmo estilo dos existentes):
```csharp
        [SerializeField] private Basket.Meta.ItemCatalog itemCatalog;
        [SerializeField] private CharacterCatalog characterCatalog;
        [SerializeField] private CharacterObtainRules obtainRules;
        [SerializeField] private MatchRewardRules rewardRules;
```
No início de `Awake()` (antes de `EnsureConfigs()` ou logo depois, mantendo a ordem que já existe pros outros configs), monte o serviço e tente o elenco do perfil:
```csharp
            var profileService = new ProfileRuntimeService(
                new SaveService(new FileSaveStorage(Application.persistentDataPath), new JsonSaveSerializer()),
                itemCatalog, characterCatalog, progressionConfig, obtainRules, rewardRules);
            profileService.LoadOrCreate();

            var ownedRoster = profileService.BuildRosterOrNull(matchSetup.slots.Count);
            if (ownedRoster != null)
            {
                var liveSetup = ScriptableObject.CreateInstance<MatchSetup>();
                liveSetup.slots = new List<MatchSetup.PlayerSlot>();
                for (int i = 0; i < matchSetup.slots.Count; i++)
                {
                    MatchSetup.PlayerSlot original = matchSetup.slots[i];
                    var instance = profileService.Profile.Inventory.GetCharacter(ownedRoster[i].characterId);
                    liveSetup.slots.Add(new MatchSetup.PlayerSlot(original.team, original.control, ownedRoster[i],
                        instance?.level ?? 1, instance?.limitBreak ?? 0, instance?.dupes ?? 0));
                }
                matchSetup = liveSetup;
            }
            // senão, matchSetup continua sendo o asset configurado no Inspector (fallback já documentado
            // no comentário de MatchSetup.PlayerSlot).
```

- [ ] **Step 4: Rodar COMPILE_CHECK**

Esperado: zero `error CS`. Se `MatchSetup.PlayerSlot`'s construtor ou `List<CharacterDefinition>` indexado por posição não bater com o roster real (times/controles podem não se alinhar 1-a-1 com uma lista simples de personagens), ajuste a lógica — o objetivo é manter time/controle de cada slot original, só trocando o personagem/progresso pelo que o perfil realmente tem.

- [ ] **Step 5: Ligar `OnMatchEnded` e salvar ao sair**

Depois que `Simulation` é criado (`Simulation = new MatchSimulation(...)`), assine o evento:
```csharp
            Simulation.Match.OnMatchEnded += finalState =>
            {
                var playedIds = matchSetup.slots
                    .Where(s => s.character != null)
                    .Select(s => s.character.characterId);
                profileService.ApplyMatchReward(finalState.ScoreHome, finalState.ScoreAway, playedIds);
            };
```
(Ajuste `finalState.ScoreHome`/`ScoreAway` pros nomes reais de `MatchState` se forem outros -- `IMatchState` já expõe `GetScore(TeamId)`, pode ser mais seguro usar isso: `finalState.GetScore(TeamId.Home)`/`GetScore(TeamId.Away)`.)

No `OnDestroy()` (que já existe, faz `Simulation?.Dispose()`), adicione a gravação de segurança antes do dispose:
```csharp
        private void OnDestroy()
        {
            profileService?.Save();
            Simulation?.Dispose();
            foreach (var d in disposables) d.Dispose();
            disposables.Clear();
        }
```
(Isso exige que `profileService` vire um campo da classe, não uma variável local de `Awake()` -- mova a declaração pra um `private ProfileRuntimeService profileService;` no topo da classe e atribua dentro de `Awake()`.)

- [ ] **Step 6: Rodar COMPILE_CHECK**

Esperado: zero `error CS`.

- [ ] **Step 7: Commit**

```bash
cd "/d/Projetos/basket"
git add Assets/_Project/Scripts/Bootstrap/Basket.Bootstrap.asmdef Assets/_Project/Scripts/Bootstrap/GameBootstrap.cs
git commit -m "feat(bootstrap): wire ProfileRuntimeService (roster from profile, match-end reward, save on quit)"
```

---

### Task 10: `ProfileHud` (tela placeholder)

**Files:**
- Create: `Assets/_Project/Scripts/UI/ProfileHud.cs`
- Modify: `Assets/_Project/Scripts/Bootstrap/GameBootstrap.cs` (instanciar e alimentar o HUD)

**Interfaces:**
- Consumes: `IPlayerProfileReadOnly` (Task 1/2), `MatchRewardSummary`/`GachaPullSummary` (Task 1) -- todos tipos de `Core`, nenhum de `Basket.Meta`.
- Produces: `ProfileHud.Configure(IPlayerProfileReadOnly profile)`, `.ShowMatchReward(MatchRewardSummary summary)`, `.ShowGachaResult(GachaPullSummary summary)`.

- [ ] **Step 1: Implementação (sem teste automatizado -- overlay de debug visual, igual `DebugHud`; ver Task 18 do plano da fatia vertical original pra o precedente)**

`Assets/_Project/Scripts/UI/ProfileHud.cs`:
```csharp
using UnityEngine;
using Basket.Core;

namespace Basket.UI
{
    // Placeholder (Etapa 10 faz a UI final) -- overlay IMGUI mostrando saldo/elenco e o último
    // resultado de partida/gacha. Só conhece Core (D-021): MatchRewardSummary/GachaPullSummary
    // já vêm prontos pra exibir, sem tipo nenhum de Basket.Meta cruzando essa fronteira.
    public class ProfileHud : MonoBehaviour
    {
        private IPlayerProfileReadOnly profile;
        private MatchRewardSummary? lastReward;
        private GachaPullSummary? lastPull;

        public void Configure(IPlayerProfileReadOnly playerProfile)
        {
            profile = playerProfile;
        }

        public void ShowMatchReward(MatchRewardSummary summary) => lastReward = summary;
        public void ShowGachaResult(GachaPullSummary summary) => lastPull = summary;

        private void OnGUI()
        {
            if (profile == null) return;

            GUI.Label(new Rect(10, 70, 400, 20), $"Gemas: {profile.Amount("currency_gems")}");
            GUI.Label(new Rect(10, 90, 400, 20), $"Personagens: {System.Linq.Enumerable.Count(profile.OwnedCharacterIds)}");

            int y = 110;
            if (lastReward.HasValue)
            {
                var r = lastReward.Value;
                GUI.Label(new Rect(10, y, 500, 20), $"Última partida: {(r.Won ? "Vitória" : "Derrota")} — {string.Join(", ", r.ItemsGranted)}");
                y += 20;
            }
            if (lastPull.HasValue)
            {
                var p = lastPull.Value;
                GUI.Label(new Rect(10, y, 500, 20), p.Success
                    ? $"Gacha: {string.Join(", ", p.ResultDescriptions)}"
                    : $"Gacha falhou: {p.FailureReason}");
            }
        }
    }
}
```

- [ ] **Step 2: Ligar no `GameBootstrap`**

Depois de criar o `DebugHud` (que já existe em `Awake()`), adicione:
```csharp
            var profileHud = new GameObject("ProfileHud").AddComponent<ProfileHud>();
            profileHud.Configure(profileService.Profile);
```
E dentro da assinatura de `Simulation.Match.OnMatchEnded` (Task 9, Step 5), adicione a chamada de exibição junto da de recompensa:
```csharp
            Simulation.Match.OnMatchEnded += finalState =>
            {
                var playedIds = matchSetup.slots.Where(s => s.character != null).Select(s => s.character.characterId);
                MatchRewardSummary summary = profileService.ApplyMatchReward(finalState.GetScore(TeamId.Home), finalState.GetScore(TeamId.Away), playedIds);
                profileHud.ShowMatchReward(summary);
            };
```

- [ ] **Step 3: Rodar COMPILE_CHECK**

Esperado: zero `error CS`. Confirme também que `Assets/_Project/Scripts/UI/Basket.UI.asmdef` continua só referenciando `Basket.Core` (não precisa mudar nesta task -- se o compilador pedir uma referência nova em UI, algo no `ProfileHud.cs` vazou um tipo de `Basket.Meta` por engano; revise antes de adicionar a referência).

- [ ] **Step 4: Commit**

```bash
cd "/d/Projetos/basket"
git add Assets/_Project/Scripts/UI/ProfileHud.cs Assets/_Project/Scripts/Bootstrap/GameBootstrap.cs
git commit -m "feat(ui): add ProfileHud placeholder (profile balance, match reward, gacha result)"
```

---

### Task 11: Teste de integração PlayMode (ciclo completo)

**Files:**
- Create: `Assets/_Project/Tests/PlayMode/MetaIntegrationTests.cs`

**Interfaces:**
- Consumes: tudo das Tasks 1-8 (`ProfileRuntimeService` completo), mais `TestMatch` (helper já existente em `Assets/_Project/Tests/PlayMode/`, usado por `AISimulationTests.cs` -- leia esse arquivo primeiro pra confirmar a API exata de `TestMatch`, incluindo `.Start(...)`, `.RunUntil(...)`, `.Sim`, `.Rules`, `.ShotConfig`).
- Produces: nada consumido por outra task -- este é o critério de aceite final da etapa.

**Antes de implementar:** leia `Assets/_Project/Tests/PlayMode/AISimulationTests.cs` inteiro (já lido durante o design, mas confirme de novo antes de escrever) pra reusar exatamente o mesmo padrão de `TestMatch`/`using`/`Time.timeScale` -- não invente uma API nova pra montar a partida de teste.

- [ ] **Step 1: Escrever o teste que falha**

`Assets/_Project/Tests/PlayMode/MetaIntegrationTests.cs` (adapte a montagem de `TestMatch` pro padrão exato visto em `AISimulationTests.cs`; o esqueleto abaixo assume a mesma API):
```csharp
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Basket.Characters;
using Basket.Core;
using Basket.Meta;

public class MetaIntegrationTests
{
    [UnityTest]
    public IEnumerator FullCycle_MatchEndsRewardsGrantedSaveSurvivesReload_GachaAddsToRoster()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "meta_integration_" + System.Guid.NewGuid());
        try
        {
            var itemCatalog = ScriptableObject.CreateInstance<ItemCatalog>();
            var characterCatalog = ScriptableObject.CreateInstance<CharacterCatalog>();
            var progression = ScriptableObject.CreateInstance<ProgressionConfig>();
            var obtainRules = ScriptableObject.CreateInstance<CharacterObtainRules>();
            var rewardRules = ScriptableObject.CreateInstance<MatchRewardRules>();

            var service = new ProfileRuntimeService(
                new SaveService(new FileSaveStorage(tempDir), new JsonSaveSerializer()),
                itemCatalog, characterCatalog, progression, obtainRules, rewardRules);
            service.LoadOrCreate();
            service.Profile.Inventory.AddCharacter(new CharacterInstance("ace"));

            MatchRewardSummary rewardSummary = service.ApplyMatchReward(ownScore: 21, opponentScore: 10, playedCharacterIds: new[] { "ace" });
            Assert.IsTrue(rewardSummary.Won);

            // "fechar e abrir o jogo": novo serviço, mesmo diretório de disco.
            var reloadedService = new ProfileRuntimeService(
                new SaveService(new FileSaveStorage(tempDir), new JsonSaveSerializer()),
                itemCatalog, characterCatalog, progression, obtainRules, rewardRules);
            reloadedService.LoadOrCreate();
            Assert.IsTrue(reloadedService.Profile.Inventory.Owns("ace"), "save não sobreviveu ao fechar/reabrir simulado");
            Assert.Greater(reloadedService.Profile.Inventory.GetCharacter("ace").xp, 0, "XP da recompensa não persistiu");

            // gacha muda o elenco disponível
            var newCharacter = ScriptableObject.CreateInstance<CharacterDefinition>();
            newCharacter.characterId = "rookie";
            characterCatalog.characters.Add(newCharacter);
            var banner = ScriptableObject.CreateInstance<BannerDefinition>();
            banner.singleCost = new ItemCost("currency_gems", 0); // custo zero pra não depender de saldo aqui
            banner.rarities.Add(new GachaRarity { rarityId = "r1", rate = 1f });
            banner.pool.Add(new GachaEntry { character = newCharacter, rarityId = "r1", weight = 1f });

            Assert.IsNull(reloadedService.BuildRosterOrNull(requiredCount: 2), "não devia ter elenco suficiente ainda");
            GachaPullSummary pullSummary = reloadedService.Pull(banner, count: 1);
            Assert.IsTrue(pullSummary.Success);

            var roster = reloadedService.BuildRosterOrNull(requiredCount: 2);
            Assert.IsNotNull(roster, "gacha devia ter dado personagem suficiente pro elenco de 2");
            Assert.AreEqual(2, roster.Count);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
        yield return null;
    }
}
```

- [ ] **Step 2: Rodar PLAYMODE_TESTS pra confirmar que falha**

Esperado: falha real (não erro de compilação, já que tudo que este teste usa já existe das tasks anteriores) -- a menos que algo tenha divergido do assumido nas tasks anteriores, caso em que é erro de compilação e precisa voltar e ajustar a task correspondente antes de continuar.

- [ ] **Step 3: Se falhar, investigar e corrigir**

Este teste é o critério de aceite final ("pronto quando" do `docs/proximos-passos.md`) -- se falhar por causa real (não erro de digitação), é sinal de que uma das Tasks 5-8 tem um bug de integração que os testes isolados não pegaram. Não enfraqueça a asserção pra fazer passar; corrija a causa.

- [ ] **Step 4: Rodar PLAYMODE_TESTS pra confirmar que passa**

Esperado: 1 teste novo, `Passed`; suite completa continua verde.

- [ ] **Step 5: Commit**

```bash
cd "/d/Projetos/basket"
git add Assets/_Project/Tests/PlayMode/MetaIntegrationTests.cs
git commit -m "test: add MetaIntegrationTests covering the full match-end -> reward -> save -> gacha -> roster cycle"
```

---

## Self-Review Notes

- **Cobertura da spec:** os 5 itens de `docs/proximos-passos.md` item A estão cobertos: (1) Task 4, (2) Tasks 5+9, (3) Task 7, (4) Tasks 6+9, (5) Task 10. O "pronto quando" (recompensa, save persiste, gacha muda elenco) é testado diretamente na Task 11.
- **Risco conhecido, não escondido:** várias tasks dependem de assinaturas exatas que vieram de um relatório de exploração (não leitura direta minha de cada arquivo) -- cada task que usa algo assim tem uma nota explícita "antes de implementar, confirme X" em vez de fingir certeza. Isso é intencional, não uma lacuna do plano.
- **Consistência de tipos:** `MatchRewardSummary`/`GachaPullSummary` (Task 1) são usados com os mesmos campos em Tasks 7/8/10/11. `ProfileRuntimeService`'s construtor (Task 5) tem a mesma assinatura em todas as tasks que o instanciam (6, 7, 8, 9, 11).
- **Regra D-021 (UI só conhece Core):** verificada em cada task que toca UI (10) -- `ProfileHud` só importa `Basket.Core`, nunca `Basket.Meta`.
