# Referência Unity (oráculo da migração)

Fase 0 (D-029). Números que o port para Unreal precisa reproduzir. **Referência congelada** na tag
`unity-reference` (2026-10-05, Unity 6000.6.2f1), já com a correção dos tocos (D-030).

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
| Bola segurada / driblando | 1,0 m acima dos pés, 0,45 m à frente, 0,2 m para o lado da mão |
| Soltura de jump shot e lance livre (D-030) | 2,1 m acima dos pés (+ pulo), **0,15 m à frente** |
| Soltura de bandeja e enterrada | 2,1 m acima dos pés, 0,45 m à frente |

### Bloqueio (`DefaultDefenseConfig`, `DefaultAIConfig`, D-030)

| Item | Valor |
|---|---|
| Alcance da mão | Eixo vertical do corpo, da cabeça (1,8 m) à ponta dos dedos (alcance parado + pulo); 0,45 m para os lados; topo = ponta dos dedos + 0,12 m (raio da bola) |
| Janela de bloqueio | 0,35 s após a soltura |
| IA: tentar o toco | Sorteio **uma vez por arremesso**: 0,3 com o atributo Block neutro (×0,5 a ×2) |
| IA: quando pular | Arremessador no ar, a ≤ 1,6 m, subindo a ≤ 2 m/s (antes do ápice, ajustado pelo QI defensivo) |

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
| 0,20 m | 0/8 | 0/8 | 0–1/8 |
| 0,25 m | 0/8 | 0/8 | 0/8 |

Daí vêm os raios de acerto da seção 4 (≈0,124 m no jump, ≈0,141 m na bandeja). **Este é o teste
de aceitação da física do aro no Unreal**: mesma tabela, ±1 em cada célula. (Igual antes e depois de
D-030: a mira é calculada a partir do ponto de soltura.)

**Arremesso real (`LiveShotTests`)**: 6/6 soltas no verde entraram; 25/25 jump shots livres
entraram contra 25,0 esperados pelo modelo.

## 6. Simulações IA×IA (PlayMode)

Uma execução com o código congelado. Os números variam bastante entre execuções (as partidas são
curtas); use como **faixa**, não valor exato.

| Simulação | Placar | Arremessos (FG) | Bolas de 3 | Passes | Roubos | Tocos | Erros (TO) |
|---|---|---|---|---|---|---|---|
| 3v3 individual, 120 s | 2–3 | 3/23 | 2/15 | 17/18 | 2 | 3 | 2 |
| 3v3 zona, 120 s | 1–8 | 7/19 | 2/9 | 26/31 | 7 | 4 | 12 |
| 5v5 quadra inteira, 90 s | 9–6 | 7/13 | 1/5 | 22/27 | 2 | 0 | 7 |

**Tocos em arremessos marcados** (contest ≥ 0,3; as três simulações juntas), alvo do usuário 5–10%
nos arremessos de média e longa distância (D-030):

| Jump shot | Enterrada | Bandeja | Total |
|---|---|---|---|
| 2/24 (8%) | 3/11 (27%) | 2/3 | 7/38 (18%) |

No aro a taxa é maior, como no basquete real (aceito pelo usuário).

**Ponto conhecido (não investigado)**: aproveitamento baixo nos arremessos de quadra do 3v3
individual nesta execução (3/23). O Unreal deve reproduzir a mesma faixa antes de qualquer ajuste de IA.

## 7. Saída bruta dos testes

`dados/unity-playmode-referencia.txt` (linhas de estatística e de arremessos/tocos filtradas do log).
Execução do código congelado: EditMode 310/310, PlayMode 64/64. Comando (Unity fechado):

```bash
"C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe" -batchmode -nographics -projectPath "D:/Projetos/basket" -runTests -testPlatform PlayMode -testResults playmode-results.xml -logFile playmode.log
```
