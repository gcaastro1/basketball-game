# Referência Unity (oráculo da migração)

Fase 0 (D-029). Números que o port para Unreal precisa reproduzir. Extraídos em 2026-10-05 do
commit `74568df` (+ mudanças locais só visuais) com Unity 6000.6.2f1.

**Conversão para o Unreal**: metros × 100 = cm; Unity (x, y, z) com Y para cima →
Unreal (X = z, Y = x, Z = y) com Z para cima. Ex.: aro Unity (0; 3,05; 12,725) → Unreal (1272,5; 0; 305).
Confira o lado (esquerda/direita) no primeiro teste de quadra: ambos são sistemas de mão esquerda,
então a troca acima preserva a orientação.

## 1. Física global (`ProjectSettings`)

| Item | Valor Unity | No Unreal |
|---|---|---|
| Gravidade | −9,81 m/s² | −981 cm/s² (padrão do projeto: −980; ajustar para −981) |
| Passo fixo da física | 0,02 s (50 Hz) | Chaos com *substepping* ou passo fixo; ver nota abaixo |
| Iterações do solver | 6 posição / 1 velocidade | Recalibrar pelos testes, não copiar |
| Bounce threshold | 2 m/s | 200 cm/s |
| Contact offset | 0,01 m | 1 cm |

Nota: a precisão foi calibrada com a bola integrada a 50 Hz. Os valores de solver não são
equivalentes entre PhysX e Chaos; o que deve bater são as **saídas** das seções 4–6.

## 2. Quadra e aro (`DefaultCourtConfig` 3x3, `Court5v5Config`)

| Item | Valor (m) |
|---|---|
| Largura | 15,24 |
| Profundidade (meia quadra / inteira) | 14,325 / 28,65 |
| Centro do aro (meia quadra) | (0; 3,05; 12,725) |
| Centro do aro (inteira, cesta +Z) | (0; 3,05; 27,05) |
| Raio do aro / raio do tubo | 0,2286 / 0,01 (16 segmentos) |
| Tabela: centro / tamanho | (0; 3,435; 13,13) / 1,83 × 1,07 × 0,05 |
| Linha de 3 / no canto (D-026) | 7,24 / 6,71 |
| Lance livre | 4,19 |
| Ponto do check ball (meia quadra) | (0; 0; 5,125) |
| Círculo central | 1,83 |

## 3. Jogador e bola

| Item | Valor |
|---|---|
| Cápsula de gameplay | 1,9 m de altura |
| Velocidade / sprint | 4,5 m/s / ×1,45 (6,5 m/s) |
| Aceleração / desaceleração | 30 / 40 m/s² |
| Giro | 720 °/s |
| Altura do pulo | 0,8 m |
| Controle aéreo / giro no ar | 0,15 / 0 |
| Alcance parado | 2,45 m |
| Bola: diâmetro / massa | 0,24 m / 0,62 kg |
| Bola: drag / angular drag | 0,05 / 0,3 |
| Bola: quique (bounciness, combine Maximum) | 0,75 |
| Bola: CCD | Contínuo em voo, discreto quando presa |
| Raio de pegar a bola | 1,0 m (+0,15 m acima do alcance) |

## 4. Modelo de precisão (lógica pura, `ShotAccuracyModel` + `DefaultShotConfig`)

Erro de mira = raio no plano do aro (m). Chance esperada = (raio de acerto / erro)², com raio de
acerto **calibrado no aro físico**: 0,124 m (jump shot) e 0,141 m (bandeja). Estes dois números
são o principal alvo da recalibração no Chaos.

Arremessador com atributo padrão (0,75 → escala de erro 0,8), soltura perfeita fora da janela verde:

| Arremesso | Erro (m) | Livre | Marcado (contest 1) | Em movimento (máx.) | Marcado + movimento |
|---|---|---|---|---|---|
| Jump shot 3 m | 0,161 | 59,5% | 26,4% | 35,2% | 15,6% |
| Jump shot 4,5 m | 0,175 | 50,1% | 22,3% | 29,6% | 13,2% |
| Jump shot 5,5 m | 0,185 | 45,0% | 20,0% | 26,6% | 11,8% |
| Jump shot 6,71 m (canto) | 0,196 | 39,9% | 17,7% | 23,6% | 10,5% |
| Jump shot 7,24 m (3 pts) | 0,202 | 37,9% | 16,8% | 22,4% | 10,0% |
| Jump shot 8,5 m | 0,214 | 33,7% | 15,0% | 19,9% | 8,9% |
| Lance livre | 0,144 | 74,2% | — | — | — |
| Bandeja | 0,152 | 86,1% | 38,2% | — | — |
| Enterrada | 0 | 100% | — | — | — |

### Janela verde (D-028)

Meia-largura da janela (soltura perfeita = erro zero), atributo 0,75:

| Distância | Livre | Marcado |
|---|---|---|
| 3 m | ±14,5 ms | ±4,4 ms |
| 5,5 m | ±13,6 ms | ±4,1 ms |
| 7,24 m | ±12,1 ms | ±3,6 ms |
| 8,5 m | ±11,0 ms | ±3,6 ms |

**Alerta para a migração (rede e quadros):** a janela inteira livre (~25–29 ms) tem menos de 2
quadros a 60 FPS, e a marcada (~7–9 ms) é menor que 1 quadro. Online, com latência de 50–100 ms,
o servidor não consegue julgar isso pelo instante em que o comando chega. Será preciso usar o
instante da soltura **medido no cliente** (timestamp do evento de input, não do quadro), com
validação no servidor. Isso alimenta P-005.

## 5. Calibração no aro físico (PlayMode, `ShotCalibrationTests`)

**Trajetória sem viés**: mirando no centro do aro, de 4,2 / 5,5 / 6,75 / 7,5 m e de 0°, 40° e 70°,
a bola cruza o plano do aro a 0,000 m do centro e entra (12/12). O Unreal precisa disso antes de
qualquer outro número: se o arco analítico não acerta o centro, o resto não tem significado.

**Taxa de acerto × desvio da mira** (8 direções por linha, entrada a 47°):

| Desvio | Jump 4,5 m (arco 1,21 m) | Jump 6,75 m (arco 1,81 m) | Bandeja 1,5 m (arco 0,9 m) |
|---|---|---|---|
| 0,03 m | 8/8 | 8/8 | 8/8 |
| 0,06 m | 8/8 | 8/8 | 8/8 |
| 0,09 m | 5/8 | 8/8 | 8/8 |
| 0,12 m | 3/8 | 5/8 | 8/8 |
| 0,15 m | 3/8 | 0/8 | 1/8 |
| 0,20 m | 0/8 | 0/8 | 0/8 |
| 0,25 m | 0/8 | 0/8 | 0/8 |

Daí vêm os raios de acerto da seção 4 (≈0,124 m no jump, ≈0,141 m na bandeja). **Este é o teste
de aceitação da física do aro no Unreal**: mesma tabela, ±1 em cada célula.

**Arremesso real (`LiveShotTests`)**: 6/6 soltas no verde entraram; 25/25 jump shots livres
entraram contra 25,0 esperados pelo modelo.

## 6. Simulações IA×IA (PlayMode)

Uma execução local (2026-10-05). Os números variam entre execuções (ver `docs/proximos-passos.md`,
"Pendências menores conhecidas"); servem como **faixa**, não valor exato.

| Simulação | Placar | Arremessos (FG) | Bolas de 3 | Passes | Roubos | Tocos | Erros (TO) |
|---|---|---|---|---|---|---|---|
| 3v3 individual, 120 s | 5–2 | 5/23 | 0/12 | 28/28 | 3 | **11** | 3 |
| 3v3 zona, 120 s | 8–2 | 6/15 | 2/6 | 33/35 | 8 | 5 | 10 |
| 5v5 quadra inteira, 90 s | 5–8 | 5/11 | 1/3 | 26/27 | 2 | 2 | 3 |

Por tipo de arremesso (3v3 individual): jump 3/18 (média 6,9 m, marcação 0,44); bandeja 0/2;
enterrada 2/3; lance livre 2/4.

**Pontos a investigar antes de usar como gabarito** (não levar defeito para o Unreal):
- **Tocos demais no 3v3 individual**: 11 em 23 arremessos. No basquete real fica perto de 5–10%.
  Pode ser a janela de bloqueio (`blockWindowSeconds` 0,35 s, `blockRadius` 0,45 m) ou a IA
  pulando em todo arremesso.
- **3v3 zona falhou** em `AIvsAI_3v3_PlaysBasketball(1.0)`: 15 arremessos contra o mínimo de 16
  (falha conhecida, sensível à seed). O individual (0.0) passou.

## 7. Saída bruta dos testes

`dados/unity-playmode-2026-10-05.txt` (linhas de estatística filtradas do log). Resultado da
execução: 10 testes, 9 passaram, 1 falhou (o 3v3 zona citado acima).
Comando usado (Unity fechado):

```bash
"C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe" -batchmode -nographics -projectPath "D:/Projetos/basket" -runTests -testPlatform PlayMode -testFilter "AISimulationTests|FullCourtTests|LiveShotTests|ShotCalibrationTests" -testResults playmode-results.xml -logFile playmode.log
```
