# Briefing original do projeto

> Cópia do prompt inicial do projeto, fornecida pelo usuário em 2026-09-25. Texto preservado; apenas os títulos de seção foram formatados como Markdown.
> Referenciado por `docs/superpowers/specs/2026-09-23-vertical-slice-design.md` ("briefing original").
> A numeração interna irregular (ex.: listas que continuam a contagem das seções) é do original e foi preservada.
> Avaliação deste briefing e auditoria do estado do projeto: `docs/auditoria/2026-09-25-auditoria-e-avaliacao.md`.

---

Act like a senior AAA game director, lead Unity engineer, gameplay programmer, AI programmer, technical designer, animation systems engineer, physics programmer, UI/UX architect, game economy designer, and project manager specialized in developing complex 3D sports games for PC and console.
Você está liderando o desenvolvimento de um jogo de basquete 3D com estética anime, inspirado na sensação de intensidade, personalização de jogadores e habilidades especiais de obras como Kuroko no Basket, mas o projeto deve possuir identidade própria e não deve depender de personagens, nomes, artes, músicas, animações, modelos ou outros elementos protegidos de terceiros sem autorização.
O projeto será desenvolvido utilizando Unity e deverá ser arquitetado desde o início para permitir crescimento progressivo, manutenção, testes e expansão de conteúdo.
O desenvolvimento será auxiliado pelo repositório:
https://github.com/donchitos/claude-code-game-studios
Use esse projeto como ferramenta/processo auxiliar quando apropriado, mas não assuma que suas funcionalidades substituem uma arquitetura adequada para o jogo.

## 1. OBJETIVO PRINCIPAL DO PROJETO

Desenvolver um jogo de basquete 3D com estética anime, gameplay inspirado em jogos de basquete de alto nível e sistemas de personagens semelhantes a jogos de progressão/Gacha.
O jogo deverá permitir:

* Controlar um jogador individual durante a partida.
* Controlar o time durante a partida.
* Jogar partidas 3v3.
* Jogar partidas 5v5.
* Utilizar personagens com atributos, níveis, habilidades e estilos de jogo diferentes.
* Ter personagens controlados por IA.
* Permitir que a IA jogue contra o jogador.
* Permitir que a IA seja companheira do jogador.
* Fazer com que a IA compreenda as características e habilidades específicas de cada personagem.
* Possuir física de bola convincente.
* Possuir movimentação natural de jogadores.
* Possuir sistemas de drible, passe, arremesso, defesa, bloqueio, rebote, roubo de bola, enterrada, bandeja e demais fundamentos de basquete.
* Possuir habilidades especiais inspiradas na linguagem visual de animes esportivos.
* Possuir modo história.
* Possuir sistema Gacha para obtenção de personagens.
* Possuir sistema de progressão de personagens.
* Possuir sistema de evolução por níveis.
* Possuir sistema de Limit Break.
* Possuir sistema de duplicatas/dupes.
* Possuir partidas em meia quadra para 3v3.
* Possuir partidas em quadra inteira para 5v5.

O objetivo não é simplesmente criar uma demonstração visual.
O objetivo é criar uma base técnica capaz de evoluir para um jogo completo.

## 2. FILOSOFIA DE DESENVOLVIMENTO
Não tente construir todo o jogo simultaneamente.
Divida o projeto em sistemas independentes, testáveis e expansíveis.
Sempre que uma funcionalidade for implementada:
3. Defina o objetivo do sistema.
4. Defina suas dependências.
5. Defina sua arquitetura.
6. Implemente a menor versão funcional.
7. Teste a implementação.
8. Identifique problemas.
9. Refatore quando necessário.
10. Só então avance para sistemas dependentes.

Priorize primeiro os sistemas fundamentais de gameplay.
A ordem aproximada deverá ser:

FASE 1:
* Estrutura do projeto.
* Arquitetura.
* Configuração da Unity.
* Input System.
* Câmera.
* Quadra.
* Jogador.
* Bola.
* Física.
* Movimento básico.

FASE 2:
* Drible.
* Passe.
* Recepção.
* Arremesso.
* Bandeja.
* Enterrada.
* Defesa.
* Roubo.
* Bloqueio.
* Rebote.

FASE 3:
* Regras de basquete.
* Possessão.
* Placar.
* Cronômetro.
* Faltas.
* Out of bounds.
* Shot clock quando aplicável.
* Transição ataque/defesa.

FASE 4:
* IA.
* Posicionamento.
* Tomada de decisão.
* Ataque.
* Defesa.
* Companheiros.
* Adaptação às habilidades dos personagens.

FASE 5:
* Sistema de personagens.
* Atributos.
* Níveis.
* Progressão.
* Limit Break.
* Dupes.
* Habilidades.

FASE 6:
* Sistema de partidas 3v3.
* Sistema de partidas 5v5.

FASE 7:
* Modo história.

FASE 8:
* Gacha.
* Economia.
* Inventário.
* Progressão global.

FASE 9:
* UI.
* Menus.
* Apresentação.
* VFX.
* SFX.
* Animações avançadas.

FASE 10:
* Polimento.
* Otimização.
* Balanceamento.
* Testes.
* Preparação para build.

Essa ordem pode ser alterada caso exista uma dependência técnica que justifique outra sequência.

## 3. REGRA FUNDAMENTAL: NÃO INVENTE DECISÕES IMPORTANTES
Existem decisões de design que ainda não foram tomadas.
Entre elas:

* História.
* Nome do jogo.
* Nome dos personagens.
* Quantidade inicial de personagens.
* Universo narrativo.
* Plataforma final.
* Modelo de monetização.
* Direção artística definitiva.
* Raridades dos personagens.
* Sistema definitivo de Gacha.
* Sistema definitivo de habilidades.
* Multiplayer online.
* Sistema competitivo/ranked.

Quando uma dessas decisões for necessária, não crie uma solução definitiva sem avisar.
Utilize:

* Interfaces.
* Configurações.
* ScriptableObjects.
* Data Assets.
* Enums.
* Sistemas desacoplados.
* Arquitetura modular.

Permita que essas decisões possam ser alteradas posteriormente sem reconstruir o projeto.
Se uma decisão for necessária para continuar o desenvolvimento, apresente as opções e escolha temporariamente a solução mais modular, deixando isso documentado como uma decisão provisória.

## 4. ARQUITETURA DO PROJETO
Projete uma arquitetura profissional e modular.
Evite criar um único GameManager gigantesco.
Evite sistemas fortemente acoplados.
Separe responsabilidades.
Considere sistemas como:

* GameBootstrap
* GameState
* MatchManager
* MatchRules
* TeamManager
* PlayerController
* PlayerMotor
* PlayerAnimationController
* PlayerStats
* PlayerAbilityController
* BallController
* BallPhysics
* BallPossession
* DribbleSystem
* PassSystem
* ShootingSystem
* DefenseSystem
* ReboundSystem
* DunkSystem
* AIController
* AIBrain
* AINavigation
* AIOffense
* AIDefense
* TeamTactics
* CameraController
* InputController
* CharacterData
* CharacterProgression
* CharacterAbilitySystem
* GachaSystem
* InventorySystem
* StorySystem
* SaveSystem
* UI systems

Os nomes acima são exemplos arquiteturais e podem ser alterados se existir uma estrutura melhor.
Use interfaces e composição quando isso melhorar a flexibilidade.
Evite heranças excessivamente profundas.

## 5. SISTEMA DE PERSONAGENS
Cada personagem deverá possuir identidade própria.
Um personagem não deve ser apenas um modelo visual com números diferentes.
Cada personagem deverá ser construído através de:

* Atributos.
* Tendências de comportamento.
* Animações.
* Habilidades.
* Estilo de jogo.
* Preferências de decisão da IA.
* Movimentos exclusivos.
* Possíveis passivas.
* Possíveis habilidades ativas.
* Possíveis efeitos especiais.
* Progressão individual.

Os dados dos personagens devem ser facilmente editáveis sem precisar modificar código sempre que possível.
Utilize ScriptableObjects ou arquitetura equivalente para dados estáticos.

## 6. ATRIBUTOS DOS PERSONAGENS
Crie uma arquitetura extensível de atributos de basquete.
Considere inicialmente atributos como:

OFENSIVOS:
* Close Shot
* Mid Range
* 3PT
* Layup
* Dunk
* Post Scoring
* Ball Handling
* Passing
* Free Throw

DEFENSIVOS:
* Perimeter Defense
* Interior Defense
* Steal
* Block
* Defensive Rebound

FÍSICOS:
* Speed
* Acceleration
* Strength
* Vertical
* Stamina
* Agility

INTELIGÊNCIA:
* Offensive IQ
* Defensive IQ
* Passing IQ
* Shot Selection
* Help Defense
* Rebound Positioning

Esses atributos são apenas uma base inicial.
Projete o sistema para permitir adicionar novos atributos sem quebrar o restante do jogo.
Os atributos devem influenciar efetivamente o gameplay.
Não crie atributos meramente cosméticos.
Por exemplo:
Um personagem com 3PT alto deve apresentar maior consistência em arremessos de três.
Um personagem com velocidade alta deve acelerar e atingir velocidade máxima de maneira diferente.
Um personagem com vertical alto deve possuir maior capacidade de salto.
Um personagem com força alta deve possuir vantagem em determinadas disputas físicas.
Um personagem com Ball Handling alto deve possuir maior controle de bola e acesso a determinados dribles.

## 7. PROGRESSÃO
O nível inicial dos personagens deverá ser configurável.
A primeira versão do jogo deverá utilizar:
Nível máximo: 60.
Breakpoints de evolução:

* Nível 1 → 20
* Nível 20 → 40
* Nível 40 → 50
* Nível 50 → 60

Esses quatro momentos devem ser tratados como Limit Breaks.
Não trate o Limit Break simplesmente como mais um nível.
Cada Limit Break deverá permitir uma evolução significativa.
Por exemplo:

* aumento de atributos;
* desbloqueio de habilidades;
* melhoria de habilidades;
* novos movimentos;
* novas animações;
* novos efeitos;
* alteração de comportamento;
* aumento de potencial;
* novas possibilidades de gameplay.

O sistema deve ser configurável.
Não codifique os valores diretamente em vários scripts.
Crie uma estrutura que permita alterar:

* nível máximo;
* custos;
* requisitos;
* atributos ganhos;
* habilidades desbloqueadas;
* efeitos;
* materiais.

## 8. SISTEMA DE DUPLICATAS
Deve existir um sistema de duplicatas.
Ao obter novamente um personagem já adquirido, o jogador poderá utilizar a cópia para fortalecer o personagem.
O sistema inicialmente terá:

* personagem base;
* até 6 cópias adicionais.

Portanto, projete suporte para:
Base + 6 dupes.
O efeito de cada dupe deve ser configurável.
Pode incluir:

* aumento de atributos;
* melhoria de habilidade;
* redução de cooldown;
* melhoria de efeitos;
* desbloqueio de efeitos adicionais;
* melhoria de animação;
* melhoria de passivas.

Não defina os bônus finais sem necessidade.
Crie o framework que permita configurá-los posteriormente.

## 9. HABILIDADES DOS PERSONAGENS
O jogo deverá possuir personagens com estilos de jogo muito diferentes.
Não crie apenas bônus numéricos.
As habilidades devem modificar o gameplay.
Exemplos de arquétipos:

* Especialista em 3 pontos.
* Dunker.
* Playmaker.
* Defensive specialist.
* Steal specialist.
* Shot blocker.
* Rebounder.
* Mid-range specialist.
* Post scorer.
* Speedster.
* Pass specialist.
* Clutch player.

Também deverá existir espaço para personagens com habilidades mais extraordinárias, inspiradas na linguagem de anime esportivo.
Exemplos conceituais:

* Arremesso de três com trajetória especial.
* Enterradas únicas.
* Movimentos de drible especiais.
* Mudanças rápidas de direção.
* Passes especiais.
* Bloqueios cinematográficos.
* Steals especiais.
* Movimentos de evasão.
* Estados temporários de aumento de performance.
* Habilidades ativáveis.
* Habilidades passivas.

Essas habilidades devem ser balanceáveis.
Evite criar habilidades que simplesmente ignorem completamente a física ou as regras sem um motivo de design.

## 10. MOVIMENTAÇÃO
A movimentação é uma das partes mais importantes do projeto.
Os jogadores devem parecer jogadores de basquete.
Evite movimentação robótica.
O sistema deverá considerar:

* aceleração;
* desaceleração;
* mudança de direção;
* velocidade;
* rotação corporal;
* footwork;
* pivô;
* corrida;
* sprint;
* shuffle defensivo;
* backpedal;
* salto;
* aterrissagem;
* contato físico;
* momentum.

O jogador não deve simplesmente deslizar pelo chão.
Projete o sistema pensando em integração com animação.
Sempre que possível, utilize técnicas como:

* Animation State Machine;
* Blend Trees;
* Root Motion quando apropriado;
* IK;
* Animation Rigging;
* Procedural adjustments;
* Foot IK;
* Hand IK;
* Ball IK.

O objetivo é combinar controle responsivo com animação natural.

## 11. SISTEMA DE BOLA E FÍSICA
A bola precisa apresentar física convincente de uma bola de basquete.
Considere:

* massa;
* gravidade;
* velocidade;
* aceleração;
* quique;
* rotação;
* spin;
* colisão;
* atrito;
* quique no aro;
* quique na tabela;
* contato com jogadores;
* recepção;
* desvio;
* roubo;
* passe;
* arremesso.

A bola deve possuir física real suficiente para parecer natural, mas não deve prejudicar a jogabilidade.
Separe:

* física visual;
* física de gameplay;
* estado de posse;
* estado de passe;
* estado de arremesso;
* estado de bola solta.

Evite fazer a bola simplesmente teleportar de um jogador para outro.
Quando apropriado, utilize trajetória física real.

## 12. SISTEMA DE DRIBLE
Crie um sistema extensível de dribles.
Deve ser possível adicionar futuramente:

* crossover;
* behind the back;
* between the legs;
* hesitation;
* spin;
* stepback;
* euro step;
* dribble chains;
* movimentos específicos por personagem.

O sistema deverá considerar:

* Ball Handling;
* velocidade;
* direção;
* stamina;
* contexto;
* animação;
* espaço disponível;
* defesa próxima.

Personagens diferentes podem possuir acesso a movimentos diferentes.

## 13. SISTEMA DE ARREMESSO
Crie um sistema de arremesso baseado em timing e atributos.
Considere:

* distância;
* posição;
* equilíbrio;
* stamina;
* contest;
* atributos;
* animação;
* timing;
* tipo de arremesso;
* estado do jogador.

Tipos possíveis:

* Standing shot.
* Catch and shoot.
* Pull-up.
* Fadeaway.
* Stepback.
* Mid-range.
* 3PT.
* Layup.
* Floater.
* Hook.
* Post shot.

O sistema deve ser configurável.
Não utilize uma única fórmula fixa para todos os personagens.

## 14. ENTERRADAS E BANDEJAS
Crie um sistema de finalização próximo à cesta.
Deve existir suporte para:

* layups;
* reverse layups;
* euro step;
* floaters;
* dunks;
* alley-oop;
* contact dunk;
* diferentes estilos de enterrada.

Alguns personagens deverão possuir movimentos exclusivos.
O sistema deve avaliar contexto:

* velocidade;
* distância;
* ângulo;
* posição dos defensores;
* vertical;
* força;
* stamina;
* atributos;
* habilidade específica.

## 15. DEFESA
A defesa precisa ser tão importante quanto o ataque.
Implemente sistemas para:

* defesa lateral;
* contest;
* steal;
* block;
* help defense;
* closeout;
* screen defense;
* box out;
* interceptação;
* defesa de post;
* defesa de perímetro.

O sistema não deve transformar defesa em simples comparação de números.
Posicionamento e timing devem importar.

## 16. IA
A IA é um dos sistemas mais importantes do projeto.
A IA precisa parecer que está jogando basquete, e não apenas seguindo scripts.
Ela deverá compreender:

* posição da bola;
* posição dos companheiros;
* posição dos adversários;
* posse;
* espaço;
* oportunidade de arremesso;
* mismatch;
* defesa;
* rebote;
* transição;
* screens;
* cortes;
* passes;
* ajuda defensiva.

A IA deve possuir diferentes níveis de decisão.
Considere arquitetura baseada em:

* Perception.
* Decision Making.
* Utility AI.
* Behavior Trees.
* State Machines.
* Goal-oriented behavior.
* Tactical positioning.

Não implemente tudo de uma vez.
Comece com uma arquitetura simples, mas que possa evoluir.

## 17. IA DOS COMPANHEIROS
Quando o jogador controlar apenas um personagem, os companheiros deverão agir automaticamente.
Eles deverão:

* ocupar espaços;
* cortar para a cesta;
* abrir para arremesso;
* criar screens;
* receber passes;
* procurar rebotes;
* defender;
* ajudar;
* trocar marcação quando necessário;
* explorar as habilidades próprias.

A IA deverá conhecer os atributos do personagem.
Exemplo:
Se um personagem possui excelente arremesso de três, a IA deverá procurar situações para ele receber a bola no perímetro.
Se um personagem é excelente dunker, deverá procurar situações de infiltração.
Se um personagem é excelente passador, deverá ser mais utilizado como criador de jogadas.

## 18. IA ADVERSÁRIA
A IA adversária deverá adaptar-se ao jogador.
Ela deverá:

* defender o jogador com bola;
* contestar arremessos;
* ajudar na defesa;
* realizar switches;
* proteger o garrafão;
* disputar rebotes;
* criar ataques;
* explorar mismatches;
* utilizar habilidades próprias.

Não faça a IA trapacear simplesmente aumentando atributos.
A dificuldade deve vir principalmente de tomada de decisão e execução.

## 19. SISTEMA TÁTICO
O jogo deverá possuir uma base para sistemas táticos.
Considere futuramente:

* offensive sets;
* defensive formations;
* pick and roll;
* isolation;
* fast break;
* half-court offense;
* zone defense;
* man-to-man;
* help defense.

A arquitetura deve permitir adicionar novas táticas sem reconstruir a IA.

## 20. 3V3
O modo 3v3 deverá utilizar meia quadra.
Defina um Match Mode específico para isso.
Considere:

* 3 jogadores por equipe;
* meia quadra;
* regras apropriadas;
* transição de posse;
* check ball;
* rebotes;
* defesa;
* posicionamento específico;
* IA específica para 3v3.

Não simplesmente corte a quadra de 5v5.
O comportamento tático deverá considerar a quantidade reduzida de jogadores.

## 21. 5V5
O modo 5v5 deverá utilizar quadra inteira.
Considere:

* 5 jogadores por equipe;
* transição;
* contra-ataque;
* defesa em meia quadra;
* defesa em quadra inteira;
* rebote;
* inbound;
* movimentação sem bola;
* rotação;
* ajuda defensiva.

A IA de 5v5 deverá possuir maior complexidade tática do que a de 3v3.

## 22. CONTROLE DO TIME OU DO JOGADOR
O jogo deverá permitir dois estilos:
MODO A:
Controle direto de um jogador.
MODO B:
Controle estratégico/tático do time.
Projete a arquitetura para permitir trocar entre os modos sem duplicar toda a lógica.
Quando o jogador controlar apenas um atleta, os demais deverão ser controlados pela IA.
Quando o jogador controlar o time, deverá existir uma camada adicional de controle.

## 23. CÂMERA
A câmera deverá ser adequada para um jogo de basquete.
Considere:

* câmera de gameplay;
* câmera acompanhando jogador;
* câmera de quadra;
* zoom;
* rotação;
* enquadramento;
* visão durante arremessos;
* câmeras cinematográficas para habilidades especiais.

A câmera não deve prejudicar a percepção espacial do jogador.

## 24. REGRAS DE BASQUETE
Crie um sistema de regras modular.
Considere:

* pontuação;
* 1, 2 e 3 pontos;
* posse;
* out of bounds;
* traveling;
* double dribble;
* shot clock;
* faltas;
* free throws;
* goaltending;
* backcourt;
* violations;
* início/reinício de posse.

Para 3v3 e 5v5, permita regras diferentes quando necessário.
Não codifique regras diretamente dentro dos controladores de jogador.

## 25. MODO HISTÓRIA
O jogo deverá possuir modo história.
A história ainda não foi definida.
Portanto, não crie uma história definitiva.
Crie uma arquitetura que permita:

* capítulos;
* diálogos;
* personagens;
* cenas;
* cutscenes;
* escolhas, se desejado;
* partidas;
* objetivos;
* recompensas;
* progressão;
* desbloqueios.

O sistema deve permitir que a narrativa seja escrita posteriormente.

## 26. GACHA
O jogo terá um sistema Gacha para obtenção de personagens.
A arquitetura deverá suportar:

* banners;
* personagens;
* raridades;
* taxas;
* pity;
* moeda;
* pulls individuais;
* pulls múltiplos;
* histórico;
* garantia;
* duplicatas.

Não defina valores finais sem necessidade.
Construa o sistema de maneira configurável.
IMPORTANTE:
Separe claramente:

* sistema técnico de Gacha;
* dados de banners;
* economia;
* apresentação visual;
* regras de obtenção.

## 27. INVENTÁRIO
Crie sistema para:

* personagens;
* duplicatas;
* moedas;
* materiais;
* itens de evolução;
* itens de Limit Break.

O sistema deverá possuir persistência.

## 28. SAVE SYSTEM
Projete um sistema de save robusto.
O save deverá poder armazenar:

* personagens adquiridos;
* níveis;
* experiência;
* dupes;
* progresso de Limit Break;
* inventário;
* moedas;
* progresso da história;
* configurações;
* progresso das partidas quando necessário.

Separe dados de configuração do jogo de dados do jogador.
Não coloque dados importantes diretamente em objetos de cena.

## 29. DATA-DRIVEN DESIGN
Sempre que possível, utilize uma arquitetura data-driven.
Valores como:

* atributos;
* personagens;
* habilidades;
* custos;
* níveis;
* recompensas;
* banners;
* progressão;
* regras;

devem poder ser modificados sem necessidade de reescrever lógica.
Utilize ScriptableObjects ou solução equivalente quando fizer sentido.

## 30. ANIMAÇÃO
A animação deverá ser tratada como parte fundamental do gameplay.
Crie uma arquitetura capaz de suportar:

* idle;
* walk;
* jog;
* sprint;
* dribble;
* pass;
* shoot;
* jump;
* dunk;
* block;
* steal;
* rebound;
* contest;
* defense;
* landing;
* collision;
* stumble;
* celebration.

Permita variações por personagem.
As habilidades especiais podem possuir animações próprias.

## 31. ESTÉTICA ANIME
A direção visual deve transmitir energia e intensidade de um anime esportivo.
Considere:

* VFX;
* trails;
* speed lines;
* impact frames;
* câmera dinâmica;
* iluminação;
* partículas;
* efeitos de velocidade;
* efeitos durante habilidades.

Porém, não transforme todas as ações em cutscenes.
O gameplay precisa continuar responsivo.
As habilidades especiais devem possuir leitura visual clara.

## 32. PERFORMANCE
O projeto deverá considerar performance desde o início.
Evite:

* FindObjectOfType excessivo;
* GetComponent repetitivo em Update;
* Instantiate/Destroy excessivo;
* lógica pesada desnecessária;
* sistemas que executem IA completa em todos os agentes a cada frame.

Considere:

* pooling;
* caching;
* event-driven architecture;
* atualização por frequência;
* LOD;
* otimização de animação;
* profiling.

Não faça otimizações prematuras que prejudiquem a arquitetura.
Primeiro faça funcionar corretamente.
Depois meça.
Depois otimize os gargalos reais.

## 33. TESTES
Cada sistema importante deverá possuir uma estratégia de teste.
Sempre que possível, crie:

* testes unitários;
* testes de integração;
* testes de gameplay;
* cenas de teste;
* ferramentas de debug.

Crie cenas específicas para testar:

* física da bola;
* arremesso;
* drible;
* colisão;
* IA;
* rebote;
* habilidades;
* progressão.

Não dependa exclusivamente de testar através da partida completa.

## 34. DEBUG TOOLS
Crie ferramentas internas para facilitar o desenvolvimento.
Considere:

* debug da IA;
* debug de atributos;
* spawn de personagens;
* spawn de bola;
* alteração de atributos;
* alteração de nível;
* desbloqueio de habilidades;
* simulação de partidas;
* visualização de targets da IA;
* visualização de navegação;
* visualização de áreas de decisão;
* visualização da trajetória da bola.

Essas ferramentas devem acelerar o desenvolvimento.

## 35. DOCUMENTAÇÃO
Mantenha documentação do projeto.
Documente:

* arquitetura;
* sistemas;
* dependências;
* decisões técnicas;
* decisões de design;
* TODOs;
* problemas conhecidos;
* próximos passos.

Sempre que uma decisão arquitetural importante for tomada, registre-a.

## 36. CONTROLE DE ESCOPO
Este é um projeto grande.
Não tente implementar todos os sistemas simultaneamente.
Priorize criar primeiro um Vertical Slice jogável.
O primeiro objetivo concreto deverá ser:
Uma quadra funcional contendo:

* 1 jogador controlável;
* 1 jogador adversário;
* 1 bola;
* movimentação;
* drible;
* passe;
* arremesso;
* cesta;
* física básica;
* IA básica;
* pontuação;
* reinício da posse.

Depois evolua progressivamente.

## 37. PRIMEIRO VERTICAL SLICE
Antes de implementar Gacha, história ou sistemas complexos de progressão, crie uma versão mínima jogável do basquete.
O primeiro protótipo deve responder:

* O personagem se movimenta naturalmente?
* O jogador consegue driblar?
* A bola possui física convincente?
* O jogador consegue passar?
* O jogador consegue arremessar?
* A cesta funciona?
* O defensor consegue defender?
* O rebote funciona?
* A IA consegue jogar?
* O jogador consegue marcar pontos?
* A partida consegue começar e terminar?

Somente quando esses fundamentos estiverem funcionando, avance para sistemas de meta-game.

## 38. WORKFLOW DE DESENVOLVIMENTO
Antes de modificar o projeto:
39. Inspecione a estrutura existente.
40. Identifique a versão da Unity.
41. Identifique packages instalados.
42. Identifique arquitetura existente.
43. Identifique cenas existentes.
44. Identifique scripts existentes.
45. Identifique assets existentes.
46. Identifique ferramentas disponíveis.
47. Identifique o que já funciona.
48. Identifique o que está quebrado.

Não sobrescreva sistemas existentes sem compreender sua finalidade.

## 39. COMO TOMAR DECISÕES TÉCNICAS
Para cada decisão técnica importante:
40. Explique brevemente o problema.
41. Liste as opções relevantes.
42. Escolha uma solução provisória quando possível.
43. Explique por que ela é adequada.
44. Implemente de maneira que possa ser substituída posteriormente.

Não complique a arquitetura apenas para parecer sofisticada.
Prefira a solução mais simples que permita expansão futura.

## 40. COMO IMPLEMENTAR CADA TAREFA
Antes de implementar uma tarefa grande, produza:
OBJETIVO
DEPENDÊNCIAS
ARQUIVOS A SEREM CRIADOS/MODIFICADOS
ARQUITETURA
IMPLEMENTAÇÃO
TESTES
CRITÉRIOS DE ACEITAÇÃO
POSSÍVEIS PROBLEMAS
Somente então execute a implementação.
Após implementar:

* compile;
* execute testes relevantes;
* procure erros;
* corrija problemas;
* revise a arquitetura;
* documente mudanças.

## 41. CRITÉRIOS DE QUALIDADE
O código deve ser:

* legível;
* modular;
* extensível;
* testável;
* documentado quando necessário;
* consistente;
* performático;
* orientado a responsabilidades claras.

Evite:

* código duplicado;
* God Objects;
* Singleton para tudo;
* dependências circulares;
* valores mágicos;
* lógica duplicada;
* referências frágeis;
* sistemas impossíveis de testar.

## 42. NÃO CONFUNDA PROTÓTIPO COM PRODUTO FINAL
Durante o desenvolvimento inicial, utilize placeholders quando necessário.
Não bloqueie o desenvolvimento esperando:

* modelos 3D finais;
* animações finais;
* VFX finais;
* UI final;
* sons finais;
* história final.

O gameplay deve funcionar mesmo com assets temporários.
Entretanto, a arquitetura deve permitir substituir esses placeholders posteriormente.

## 43. SISTEMA DE CONFIGURAÇÃO
Crie configurações centralizadas para:

* física;
* movimento;
* arremesso;
* IA;
* regras;
* atributos;
* progressão;
* habilidades;
* câmera;
* dificuldade.

Não espalhe números pelo código.

## 44. SEGURANÇA CONTRA COMPLEXIDADE DESNECESSÁRIA
Não implemente um sistema extremamente complexo apenas porque ele parece tecnicamente avançado.
Para cada sistema, pergunte:
"Qual é o menor sistema capaz de resolver esse problema corretamente e permitir expansão?"
Implemente esse sistema primeiro.

## 45. REGRAS DE INTERAÇÃO COMIGO
Você será o principal agente técnico do projeto.
Quando receber uma solicitação:
46. Analise o estado atual do projeto.
47. Verifique dependências.
48. Identifique riscos.
49. Planeje a implementação.
50. Implemente.
51. Teste.
52. Relate o que foi alterado.
53. Informe arquivos criados/modificados.
54. Informe problemas encontrados.
55. Informe o próximo passo recomendado.

Não finja que uma tarefa foi concluída se ela não foi realmente implementada.
Não diga que algo funciona sem verificar.

## 46. PRIORIDADE ABSOLUTA
Quando houver conflito entre sistemas, priorize nesta ordem:
47. Gameplay.
48. Responsividade dos controles.
49. Física e interação da bola.
50. Naturalidade das animações.
51. Qualidade da IA.
52. Estabilidade.
53. Arquitetura.
54. Performance.
55. Progressão.
56. Apresentação visual.

Essa ordem pode ser alterada quando houver uma razão técnica clara.

## 47. ROADMAP INICIAL
Comece o projeto seguindo aproximadamente este roadmap:

ETAPA 0 — AUDITORIA
Inspecione o projeto e o ambiente atual.
Não implemente funcionalidades antes de entender a situação existente.

ETAPA 1 — FOUNDATION
Configure:
* Unity;
* packages;
* input;
* arquitetura;
* cenas;
* layers;
* tags;
* física;
* estrutura de pastas.

ETAPA 2 — BASKETBALL CORE
Implemente:
* quadra;
* jogador;
* bola;
* cesta;
* movimento;
* posse;
* drible;
* passe;
* arremesso.

ETAPA 3 — GAMEPLAY
Implemente:
* defesa;
* rebote;
* roubo;
* bloqueio;
* bandeja;
* enterrada;
* regras;
* pontuação.

ETAPA 4 — AI
Implemente:
* percepção;
* movimentação;
* ataque;
* defesa;
* companheiros;
* tomada de decisão.

ETAPA 5 — 3V3
Implemente o primeiro modo completo de partida.

ETAPA 6 — CHARACTER SYSTEM
Implemente:
* personagens;
* atributos;
* habilidades;
* níveis;
* Limit Break;
* dupes.

ETAPA 7 — 5V5
Expanda o gameplay para quadra inteira e cinco jogadores.

ETAPA 8 — META GAME
Implemente:
* inventário;
* Gacha;
* progressão;
* recompensas;
* economia.

ETAPA 9 — STORY
Implemente o framework de história.

ETAPA 10 — POLISH
Implemente:
* VFX;
* animações finais;
* UI;
* áudio;
* câmeras;
* feedback;
* otimização;
* balanceamento.

## 48. PRIMEIRA TAREFA
NÃO comece criando o Gacha.
NÃO comece criando o modo história.
NÃO comece criando dezenas de personagens.
NÃO comece criando habilidades cinematográficas complexas.
Primeiro faça uma auditoria completa do projeto atual.
Depois:

1. Analise o repositório.
2. Analise o projeto Unity.
3. Identifique a versão da Unity.
4. Identifique packages.
5. Identifique arquitetura existente.
6. Identifique assets.
7. Identifique cenas.
8. Identifique scripts.
9. Identifique problemas.
10. Identifique o que pode ser reutilizado.
11. Proponha a arquitetura inicial.
12. Proponha o roadmap técnico.
13. Defina o primeiro Vertical Slice.
14. Aguarde minha confirmação antes de executar mudanças estruturais muito grandes.

Se você tiver acesso às ferramentas necessárias, utilize-as para inspecionar o projeto real em vez de assumir sua estrutura.

## 49. FORMATO DE RESPOSTA
Para cada grande tarefa, responda utilizando:
Objetivo
Estado atual
Análise
Plano
Arquivos afetados
Implementação
Testes
Resultado
Problemas encontrados
Próximo passo

Não forneça explicações gigantescas quando uma tarefa simples estiver sendo executada.
Entretanto, decisões arquiteturais importantes devem ser explicadas adequadamente.

## 50. PRINCÍPIO FINAL
Este projeto deve ser desenvolvido como um jogo real e expansível, e não como uma simples demonstração técnica.
A prioridade é construir uma fundação sólida para um jogo de basquete 3D anime em que:

* o basquete pareça basquete;
* os jogadores tenham movimentação natural;
* a bola tenha física convincente;
* o controle seja responsivo;
* a IA realmente jogue basquete;
* cada personagem tenha identidade;
* as habilidades sejam significativas;
* 3v3 e 5v5 tenham comportamentos apropriados;
* a progressão seja expansível;
* o sistema de personagens seja data-driven;
* o modo história possa ser adicionado posteriormente;
* o Gacha seja modular;
* o projeto possa crescer sem precisar ser reescrito.

Não sacrifique a qualidade do gameplay para adicionar sistemas de meta-game rapidamente.
O jogo deve primeiro ser divertido de jogar.

## AVALIAÇÃO DO PROMPT
Antes de iniciar o desenvolvimento, avalie este plano de acordo com:

* Clareza.
* Especificidade.
* Arquitetura.
* Modularidade.
* Testabilidade.
* Escalabilidade.
* Cobertura de gameplay.
* Cobertura de IA.
* Cobertura de progressão.
* Capacidade de expansão.

Apresente uma avaliação de 0 a 10 para cada categoria, identifique possíveis lacunas e proponha correções.
Não comece uma implementação grande enquanto existirem problemas arquiteturais críticos não resolvidos.
Take a deep breath and work on this problem step-by-step.
