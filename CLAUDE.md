# Basketball anime — guia para sessões do Claude

Jogo de basquete anime em Unity, modular e data-driven. Responda em **português**. O usuário
está em **UTC-3**.

## Leia antes de mudar algo

1. `docs/briefing-original.md` — o pedido original (fonte da verdade do escopo).
2. `docs/roadmap-mestre.md` — etapas, status e o que vem depois.
3. `docs/arquitetura.md` — assemblies, fluxo por frame e a tabela "Onde mexer para…".
4. `docs/decisoes.md` — decisões aceitas (D-xxx) e provisórias/pendentes (P-xxx).
5. `docs/etapas/etapa-N-*.md` — o plano e o resultado de cada etapa já feita.
6. `docs/proximos-passos.md` — as próximas tarefas, com os pontos de encaixe no código.

## Regras do projeto

- **Não invente decisões finais de design** (raridades, gacha, monetização, nomes, história,
  Modo B…). Use valores provisórios **em dados** (ScriptableObjects) e registre como P-xxx em
  `docs/decisoes.md`. Decisão pendente: **P-001 (Modo B)** — pergunte ao usuário.
- Toda etapa nova ganha um plano em `docs/etapas/`, uma linha no roadmap e testes que provam
  a "saída verificável" da etapa.
- Configs e conteúdo são ScriptableObjects em `Assets/_Project/Data`; todo campo tem default
  no código. Lógica pura (testável sem cena) separada dos MonoBehaviours.
- Um arquivo de ScriptableObject/MonoBehaviour precisa ter uma classe **com o mesmo nome do
  arquivo** (senão o asset quebra no Unity; o typecheck acusa).
- **Nunca** exiba nem faça commit do arquivo de licença Unity (`.ulf`) nem de senhas.

## Assemblies (dependências só nesta direção)

```
Core ◄── Characters ◄── Gameplay ◄── Presentation (só lê o gameplay)
Core ◄── Characters ◄── Meta        (inventário, economia, gacha, save — sem gameplay)
Core ◄── AI / Input / UI            (só conhecem Core)
Bootstrap = composition root (GameBootstrap); nada depende dele
```

Controle de jogador = `Core/IAgentController` (humano, IA, teste). Regras = `MatchRules` (dados)
+ `MatchManager` (árbitro). Nunca coloque regra dentro de um controller.

## Ambiente

- Unity **6000.6.2f1**, URP, Input System. Cenas: `Assets/_Project/Scenes/01_VerticalSlice_HalfCourt`
  (meia quadra), `02_FullCourt_5v5` e `03_Practice_Solo` (treino sozinho: Z velocidade, X pausa, C câmera,
  painel da animação). Menu **Basket → Build Vertical Slice Scene** recria a cena.
- Modelos 3D vêm do **Tripo3D** (`Assets/TripoModels`); o pacote `com.tripo3d.unitybridge` aponta
  para um caminho local em `D:/` no `Packages/manifest.json` — **não remova** (só o CI o tira).
  `Editor/TripoHumanoidImporter.cs` importa novos FBX como Humanoid.
- Modelos, clipes e texturas (`*.fbx`, `*.png`, `*.jpg`...) estão no **Git LFS** (`.gitattributes`):
  instale o Git LFS; o CI baixa com `lfs: true`. Clipes em `Assets/TripoModels/**/Animations/` são
  configurados por `Editor/CharacterAnimationImporter.cs` e ligados ao personagem por
  `Editor/CharacterClipBinder.cs` (menu **Basket → Bind Character Animations**).
  FBX com "30 fps drop-frame" são corrigidos por `Editor/FbxFrameRateFixer.cs` (menu **Basket → Fix
  FBX Frame Rates**). Sessões na nuvem não conseguem enviar arquivos novos ao LFS: mudanças em
  `.fbx`/texturas precisam ser commitadas de uma máquina local.
- Personagem padrão: **Banana Man** (`Assets/Plugins/Banana Yellow Games`), 1,8 m em
  `Data/Characters/DefaultCharacterVisual.asset` (só visual; o corpo de gameplay tem 1,9 m). O modelo
  Tripo (1,68 m) continua em `Assets/TripoModels`. Materiais do Standard antigo são convertidos para o
  Lit do URP em runtime, um a um (`CharacterVisual`).
- Ginásio: `Assets/MarpaStudio` (Basket Ball Stadium, materiais do Standard antigo, convertidos em
  runtime) e bola `Assets/TierrasDeRol/Basketball` (URP). O jogo monta o ginásio a partir de
  `Data/Arena/MarpaStadiumLayout.asset` (gerado por `tools/stadium/extract_layout.py` a partir da cena de
  demonstração) via `Data/Arena/DefaultArenaVisual.asset`; é só visual. Quadra com medidas NBA (D-026).
- `Assets/Starter Assets` (controles 1ª/3ª pessoa da Unity) + Cinemachine: referência, não usados pelo
  jogo (o jogo tem motor, input e câmera próprios — D-025).

## Como verificar

- **Unity Test Runner** (Window → General → Test Runner): EditMode (lógica pura) e PlayMode
  (física, aro, partidas IA×IA). Estado atual: EditMode 269/269, PlayMode 55/55 (a simulação 3v3 ainda pode falhar às vezes:
  `AISimulationTests.AIvsAI_3v3_PlaysBasketball`, ver
  `docs/proximos-passos.md`, "Pendências menores conhecidas").
- `tools/typecheck/check.sh` — compila tudo sem Unity (bash + mono; no Windows use WSL ou deixe
  para o CI). Se usar API nova de Unity/Editor/Input System, acrescente em `tools/typecheck/stubs/`.
- CI (`.github/workflows/ci.yml`, detalhes em `docs/ci.md`): `typecheck` + GameCI (EditMode e
  PlayMode) em todo push. Os testes de simulação imprimem estatísticas no log
  ("AI vs AI 120s", "SHOT TRACE", "PASS LOST") — use-as para calibrar, não só o verde/vermelho.
- Falha de infraestrutura antes de qualquer teste rodar (ex.: 403 no download do GameCI): rode
  de novo uma vez. Teste que falha não é "flaky": investigue a causa.

## Git

- Commits descritivos (o que mudou e por quê). Não reescreva histórico de `main`.
- Atualize o roadmap e os docs da etapa no mesmo commit da mudança.
