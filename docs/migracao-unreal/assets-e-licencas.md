# Assets e licenças (Fase 0)

Levantado em 2026-10-05. O usuário confirmou que os pacotes de modelos são gratuitos e podem ser
usados fora da Unity. A coluna "A confirmar" lista só o que essa resposta não cobre.

| Pacote | Conteúdo | Tamanho | Origem / licença | Vai para o Unreal? | A confirmar |
|---|---|---|---|---|---|
| `MarpaStudio` | Ginásio: 37 FBX, 163 texturas (138 de 2K, 10 de 4K), materiais Standard, `ArenaHDRP.unitypackage` | 541 MB | Asset Store (gratuito); usuário confirmou o uso | Sim: FBX + texturas, materiais refeitos | — |
| `TierrasDeRol/Basketball` | Bola (3 variações de albedo), Built-In e HDRP | 56 MB | Asset Store (gratuito); usuário confirmou | Sim | — |
| `Plugins/Banana Yellow Games/Banana Man` | Personagem padrão (1 FBX) | 1,4 MB | Asset Store (gratuito); usuário confirmou | Sim | — |
| `TripoModels/anime_character_3d_model` | Modelo Tripo (1,68 m) | — | Gerado no Tripo3D | Sim | Plano do Tripo usado permite uso comercial? (no plano gratuito, a licença dos modelos pode ser diferente) |
| `TripoModels/basketball_player_character` | Segundo modelo Tripo + 5 clipes retargetados do Mixamo | — | Tripo3D + Mixamo | Sim | Mesmo ponto do Tripo |
| `Animations/Basquete` (69 FBX) | Captura de movimento de basquete (esqueleto `CharacterArmature`) | — | A numeração (`06_02`, `102_25`, `124_05`) parece a do banco de captura da CMU | Sim, via IK Retargeter | Origem exata do pacote e licença |
| `Animations/Kimodo` (10 FBX) | Locomoção gerada no Kimodo e retargetada por `tools/kimodo/` | — | Gerado por IA (Kimodo) | Sim | Termos de uso das saídas do Kimodo |
| `Animations/*.fbx` (Mixamo) | Idle, Walking, Running, Dribble… | — | Mixamo (Adobe): uso em jogos permitido, não redistribuir os arquivos soltos | Sim | — |
| `Universal Animation Library[Standard]` | UAL1 Standard (com e sem root motion) | — | CC0 (D-022) | Sim | — |
| `Starter Assets` | Controles 1ª/3ª pessoa da Unity | 93 MB | Unity, só referência (D-025) | **Não**: o Unreal tem os próprios templates | — |
| `TutorialInfo`, `Readme.asset` | Template da Unity | — | — | Não | — |

## O que vai e o que é refeito

| Tipo | Destino no Unreal |
|---|---|
| FBX de malha (ginásio, bola, personagens) | Reimportados. Escala: a Unity exporta em metros, o Unreal importa em cm (conferir "Import Uniform Scale" no primeiro import) |
| FBX de animação | Reimportados para o esqueleto de cada fonte; IK Retargeter para o personagem final (o Unreal não tem o "Humanoid" automático da Unity) |
| Texturas PNG | Reimportadas. Normais com compressão de normal map; máscaras sem sRGB |
| Materiais Standard/URP/HDRP | **Refeitos**: um material mestre toon + Material Instances por superfície |
| Prefabs e cenas Unity | Não migram. O layout do ginásio (`Data/Arena/MarpaStadiumLayout.asset`, gerado por `tools/stadium/extract_layout.py`) pode virar um script Python de editor que posiciona os Actors |
| `ArenaHDRP.unitypackage` | Não usado |
| Scripts do Editor (`TripoHumanoidImporter`, `CharacterAnimationImporter`, `FbxFrameRateFixer`) | Não migram. O problema dos 30 fps drop-frame precisa ser reavaliado no import do Unreal |

## Orçamento de VRAM do ginásio

Com todas as texturas na resolução original, o ginásio passa de ~700 MB de VRAM (comprimido, com
mipmaps). Isso não cabe na GTX 1050 (2 GB, com o editor aberto).

- **Agora**: limitar o ginásio a 1K (LOD bias do grupo de texturas ou "Maximum Texture Size") e
  só importá-lo depois do vertical slice.
- **Depois do upgrade**: 2K normal; 4K só onde a câmera chega perto.
