# Fases 0 e 1 — referência Unity e projeto Unreal

## Fase 0 — Referência Unity (o oráculo)

Objetivo: congelar o projeto Unity num estado conhecido e guardar os números que o Unreal precisa
reproduzir.

1. **Commitar o trabalho local pendente** (clipes Kimodo, `basketball_player_character`,
   `tools/kimodo/`) a partir desta máquina — o LFS exige envio local.
2. **Tag de congelamento**: `git tag unity-reference` no commit final. Daqui em diante, a Unity só
   recebe correção de bug que afete a referência.
3. **Números de referência** → `docs/migracao-unreal/referencia-unity.md`:
   - log do CI de `AISimulationTests` ("AI vs AI 120s", "SHOT TRACE", "PASS LOST"): arremessos,
     acertos, passes e violações por minuto no 3v3 e no 5v5;
   - taxa de acerto por zona/distância (aberto, marcado, verde) de `LiveShotTests`/`ShotAccuracyModel`;
   - tempo de voo e ápice dos arremessos típicos (`ShotArc`);
   - velocidades e alturas do motor (`DefaultPlayerMovementConfig`);
   - medidas da quadra e do aro (`DefaultCourtConfig`, D-026).
4. **Inventário de assets e licenças**: para cada pacote externo (MarpaStudio, TierrasDeRol,
   Banana Yellow Games, Starter Assets, clipes Mixamo/UAL/Kimodo, modelos Tripo), anotar origem,
   licença e se ela permite uso fora da Unity. O que não puder ir, ganha substituto.

**Saída verificável**: tag criada; `referencia-unity.md` com os números; tabela de licenças sem
"desconhecido".

## Fase 1 — Projeto Unreal

### Instalação
- Epic Launcher → UE 5.x (ver README, "Versão"). Opções: só Windows; sem Starter Content; sem
  símbolos de debug do editor.
- IDE: Visual Studio 2022 com o workload de jogos em C++, ou Rider.
- Git LFS já instalado (usado no projeto Unity).

### Criação
- Template **Blank**, **C++**, Desktop, qualidade **Scalable**, sem Starter Content, sem ray tracing.
  (O template Third Person é bom para estudar, num projeto descartável separado.)
- Nome do projeto/módulo: `Basket`.
- **Repositório novo** (privado), separado do Unity. O Unity continua como referência.

### Repositório
- `.gitignore`: `Binaries/`, `Intermediate/`, `Saved/`, `DerivedDataCache/`, `.vs/`, `.idea/`,
  `*.sln`, `*.VC.db` (os arquivos de solução são gerados).
- `.gitattributes` (LFS): `*.uasset`, `*.umap`, `*.fbx`, `*.png`, `*.jpg`, `*.tga`, `*.wav`, `*.ogg`.
- Branches: `main` sempre abre e compila; uma branch por fase. Confira a cota de LFS do GitHub.

### Configurações de projeto (hardware atual)
Nomes conforme o editor do UE5; confira na versão instalada.
- Rendering: Dynamic Global Illumination = None; Reflections = Screen Space; Shadow Map Method =
  Shadow Maps; Generate Mesh Distance Fields = off; Allow Static Lighting = on.
- Anti-aliasing: TAA (ou FXAA).
- Auto Exposure desligado (visual estilizado com exposição fixa).
- Editor: escalabilidade Média; Live Coding ligado (mudar cabeçalho com `UPROPERTY`/`UFUNCTION`
  exige fechar o editor e compilar pelo IDE).

### Módulos
```
BasketCore  (Runtime)  lógica pura: matemática, regras, dados; testes de automação
Basket      (Runtime)  Gameplay Framework: GameMode, GameState, Character, componentes, IA
```
Mesma regra das assemblies: `Basket` depende de `BasketCore`, nunca o contrário. Novos módulos
(UI, Meta, Editor) só quando houver código para eles.

### Esqueleto mínimo
- `ABasketGameMode`, `ABasketGameState`, `ABasketPlayerController`, `ABasketCharacter` (cápsula
  1,9 m de gameplay, sem malha), configurados como padrão no nível `L_Sandbox` (piso + luz).
- Um teste de fumaça em `BasketCore` (Automation Spec).

### Saída verificável (testes)
1. O projeto compila pelo IDE e abre no editor.
2. `Basket.Core.Smoke` passa na janela **Session Frontend → Automation** e na linha de comando
   (`UnrealEditor-Cmd ... -ExecCmds="Automation RunTests Basket; Quit"`).
3. PIE com **2 jogadores, Play As Listen Server**: cada um nasce com seu Character e anda; o
   outro vê o movimento.
4. Com Network Emulation (100 ms, 1% de perda), o movimento continua sem "puxões".
5. `stat unit` no `L_Sandbox`: anotar ms de frame/GPU na GTX 1050 (linha de base).
6. Clone limpo do repositório + gerar solução + compilar funciona (nada essencial ignorado).

### Resultado (2026-10-05)

Projeto em `D:\Projetos\basket-unreal` (repositório git local, ainda sem remoto), UE 5.8.3 em
`D:\UE_5.8`, VS 2022 Build Tools (MSVC 14.44.35229: a pasta se chama 14.44.35207, mas o compilador já
tem a correção que a 5.8 exige), Windows SDK 10.0.26100.

Diferenças em relação ao plano acima:
- Projeto montado à mão a partir do template `TP_Blank` da engine (mesmos `Target.cs`/`.uproject`),
  sem o assistente do editor.
- Sem nível `L_Sandbox` ainda: o mapa é o vazio `/Engine/Maps/Entry` e o `ABasketGameMode` monta uma
  quadra provisória replicada (`ABasketSandboxArena`), como o Unity fazia (D-003). Nível de verdade
  quando a quadra for portada.
- Input de movimento (WASD) criado em código; assets de Enhanced Input na Fase 3.

| Teste | Resultado |
|---|---|
| 1. Compila | ✅ `Build.bat BasketEditor`, sem avisos (~10 min por build no HD) |
| 2. Testes `Basket.*` na linha de comando | ✅ 3/3 (`Basket.Core.Units`) |
| 3. Dois jogadores, servidor listen | ✅ sem janela: `tools/net-smoke.ps1` 4/4 (cliente entra, ganha personagem, recebe a quadra). Com janela (PIE, os dois andando): **falta, precisa do usuário** |
| 4. Network Emulation 100 ms / 1% | **falta** (PIE) |
| 5. `stat unit` na GTX 1050 | **falta** (PIE) |
| 6. Clone limpo compila | **falta** (depois do repositório no GitHub) |

**Atenção, disco C:** o cache de dados derivados (Zen) da engine fica em
`%LOCALAPPDATA%\UnrealEngine\Common\Zen`, no C: (SSD de 240 GB com ~25 GB livres). Ele cresce com
shaders e assets; mover para outro disco antes de importar assets.

