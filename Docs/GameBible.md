# Game Bible

## Identidade Básica

- **Título provisório:** O Tribunal das Almas (Título de trabalho alternativo: *Setor de Triagem 7*)
- **Gênero:** Simulador Burocrático / Investigação Moral / Document Thriller
- **Perspectiva:** 2D Diegética (Visão em primeira pessoa de bancada de trabalho)
- **Engine:** Unity 6 LTS (ou Godot 4.x)
- **Plataforma alvo:** Windows / WebGL (foco em entrega rápida e execução direta)
- **Estilo visual:** Pixel art estilizada de alto contraste, estética burocrática anos 1960
- **Equipe:**
  - João Miguel Barros de Lacerda — Desenvolvimento Individual (Game Design, Roteiro, Arte e Programação)

## Visão Criativa e High Concept

*O Tribunal das Almas* explora a frieza dos sistemas administrativos quando confrontados com a ambiguidade da moralidade humana. O jogador assume o controle do Auditor 404, um funcionário amnésico de uma repartição pós-vida encarregado de julgar recém-falecidos com base em dossiês oficiais, depoimentos e relíquias pessoais.

O cerne do jogo não é punir ou absolver mecanicamente, mas experimentar o peso desconfortável de reduzir uma existência inteira a selos de carimbo sob a pressão do relógio e as ordens severas de uma hierarquia cósmica impessoal.

## Pilares de Design

1. **Burocracia Diegética e Tátil:** Todos os elementos de interface pertencem ao mundo do jogo. Não há menus flutuantes invasivos: papéis têm peso, folhas são arrastadas, carimbos batem contra o tampo da mesa e o relógio de ponto dita o ritmo.
2. **Ambiguidade Moral Autêntica:** Nenhum réu é puramente herói ou vilão. As fichas apresentam nuances éticas (atos de desespero por amor, crimes cometidos para salvar outros, virtudes manchadas por negligência), forçando escolhas genuinamente difíceis.
3. **Pressão Sistêmica vs. Compaixão:** O jogador opera sob um regulamento rígido, cotas diárias e a vigilância do Supervisor. Oferecer misericórdia consome tempo e recursos, arriscando a estabilidade do próprio auditor.
4. **Escopo Contido e Progresso Acadêmico:** O projeto rejeita sistemas gigantescos de movimentação ou física em prol de uma estrutura narrativa e mecânica modular, refinada e plenamente executável. Trata-se de um projeto acadêmico desenvolvido passo a passo, seguindo a orientação em sala de aula do Professor Tiago França Melo de Lima.

## Universo e Lore (Worldbuilding)

### O Setor de Triagem 7
Uma repartição administrativa suspensa em um plano cinzento e atemporal, construída sob os preceitos da arquitetura brutalista dos anos 1960. Paredes de concreto aparente descascadas, lâmpadas fluorescentes tubulares que zumbem de forma intermitente e estantes monumentais repletas de caixas de arquivo que se perdem na escuridão do teto.

Do lado de fora do vidro aramado do guichê, há apenas um abismo de névoa leitosa cortado por trilhos suspensos, por onde passam composições ferroviárias desprovidas de condutores. Esses trens chegam vazios e partem apinhados de almas carimbadas para seus destinos finais.

### A Estrutura do Pós-Vida
O além é administrado pela *Diretoria Geral de Destinações*, uma burocracia metafísica que cataloga a mortalidade humana com a mesma indiferença com que uma repartição de trânsito emite licenças. Existem três destinos oficiais:

1. **Repouso Eterno (Descanso):** Destinado àqueles cuja balança moral registra ausência de débitos graves e resolução pacífica de conflitos. É o selo mais cobiçado e o mais rigidamente fiscalizado.
2. **Reencarnação Imediata (Ciclo):** Para almas neutras, vidas inacabadas ou indivíduos com pendências mundanas leves que exigem reinício biológico para depuração.
3. **Punição Correcional (Expurgo/Purgatório):** Encaminhamento para as câmaras de retificação e peso espiritual severo para almas que cometeram transgressões deliberadas e deixaram rastro de dor.

### Personagens Centrais
- **Auditor 404 (Protagonista):** Um funcionário eficiente, silencioso e destituído de memórias sobre sua própria vida mortal. Acredita piamente que seu trabalho é uma honra necessária, até que detalhes em prontuários começam a despertar faíscas de sua identidade pregressa.
- **O Supervisor (Inspetor Kael):** Uma entidade alta, de ombros largos, vestindo sobretudo pesado e chapéu fedora que obscurece seu rosto em sombras perpétuas. Ele surge aleatoriamente no final de cada expediente para auditar decisões limítrofes, conferir cotas e emitir advertências com uma frieza burocrática cortante.
- **O Contato Clandestino (Voz do Esgoto/Bilhetes Ocultos):** Entidades do submundo que deslizam bilhetes dobrados sob a fresta inferior da mesa, oferecendo propinas (tempo extra, favores) em troca do desvio de certas almas influentes para seus setores.

## Mecânicas Principais (Core Mechanics)

### 1. Manipulação da Mesa e Análise Documental
A área de jogo é a bancada de trabalho do Auditor 404. O jogador pode segurar, arrastar, sobrepor e soltar documentos livremente com o cursor do mouse:
- **Certidão de Passamento:** Dados biográficos, causa da morte, data e hora.
- **Relatório de Ações Notáveis:** Registro objetivo dos principais atos em vida (doações, agressões, traições, sacrifícios).
- **Cartas Pessoais & Diários:** Textos manuscritos que revelam as motivações íntimas e a verdade emocional do falecido.
- **Contratos & Termos:** Cláusulas que podem ocultar pactos mundanos ou pecados legais.

### 2. A Balança do Pesar (Mecânica Tátil Única)
Localizada no canto esquerdo da mesa. Cada alma traz consigo um único objeto físico residual (um anel quebrado, uma chave enferrujada, uma boneca de pano, uma adaga cerimonial).
- O jogador posiciona o objeto em um dos pratos da balança.
- No outro prato, aplica o peso do Código de Triagem.
- **Comportamento Mágico/Físico:** Se o objeto afunda violentamente, ele emite fuligem e fumaça escura, indicando culpa reprimida ou atrocidades não declaradas. Se o prato levita suavemente com brilho fosco, a alma agiu por sacrifício ou pureza de espírito.

### 3. Reagente Químico Revelador (Tinta de Luminescência)
Um frasco com conta-gotas contendo uma substância que, ao ser pingada sobre borrões, assinaturas ou trechos suspeitos de cartas, queima a camada superficial e revela rasuras, censuras ou mentiras que o réu tentou omitir no dossiê oficial.

### 4. O Sistema de Interrogatório Curto
O jogador possui uma campainha e um comunicador de vidro. Ao notar uma contradição gritante entre o depoimento verbal da alma e os registros escritos, é possível disparar até 2 perguntas investigativas predefinidas por caso. As respostas da alma alteram seu semblante e suas microexpressões.

### 5. O Nível de Inquietação da Alma
A alma atrás do vidro não é estática. Um medidor sutil de paciência diminui conforme o exame documental se arrasta. Almas impacientes batem no vidro, provocam interferência nas luzes fluorescentes e emitem sussurros desconcertantes, gerando pressão psicológica no jogador para selar o despacho.

### 6. Os Três Carimbos de Veredito
Para finalizar o expediente de uma alma, o jogador pega um dos carimbos metálicos no suporte direito, pressiona na almofada de tinta correspondente e carimba o cabeçalho do dossiê:
- **Verde / Dourado:** Repouso Eterno.
- **Azul Escuro:** Reencarnação Imediata.
- **Vermelho Carmim:** Punição Correcional. Ao carimbar, a alavanca pneumática é puxada e a pasta é sugada pelo tubo de correio a vácuo, despachando a alma para a plataforma do trem.

## Ciclo Principal de Jogo (Core Loop)
1. **Início do Expediente:** O relógio de ponto é batido e o som da sirene da repartição ecoa.
2. **Chegada do Dossiê:** Uma pasta de arquivo é ejetada pelo tubo pneumático sobre a mesa.
3. **Apresentação da Alma:** A silhueta de uma alma se senta no banco do outro lado do vidro aramado.
4. **Investigação & Pesagem:** O jogador abre o dossiê, lê os papéis, coloca o objeto na Balança do Pesar e aplica a Tinta Reveladora onde houver suspeita.
5. **Confronto:** Caso haja divergência entre laudos e depoimento, o jogador interroga a alma pelo comunicador.
6. **Veredito:** O jogador seleciona o carimbo, sela o despacho e aciona a alavanca pneumática.
7. **Balanço Diário:** Ao fim do turno, o Supervisor Kael audita os vereditos, aplica advertências ou valida a cota, e o Auditor 404 descobre um novo fragmento de sua própria memória.

## Estrutura Narrativa e Finais
O jogo se divide em 5 Turnos de Expediente (cada turno equivale a um dia de trabalho e traz almas com complexidade moral crescente):
- **Turno 1: O Procedimento Padrão.** Almas simples, casos fáceis para tutorial diegético.
- **Turno 2: As Zonas Cinzentas.** Casos em que a lei fria condena um ato de amor puro (ex: um pai que furtou alimentos hospitalares para a filha moribunda).
- **Turno 3: A Primeira Interferência.** O surgimento de bilhetes de suborno e a primeira auditoria presencial do Supervisor Kael.
- **Turno 4: O Espelho Trincado.** O Auditor encontra um relatório de óbito de uma cidadezinha familiar e uma foto antiga onde ele próprio aparece ao fundo.
- **Turno 5: O Julgamento Final do Auditor.** A última alma do expediente é a pessoa diretamente responsável pela morte do Auditor 404 na vida terrena. O jogador deve decidir se usa seu poder burocrático para vingança pessoal ou para julgamento estrito da verdade.

### Os Três Finais do Jogo
1. **A Engrenagem Perfeita (Fim Burocrático):** O jogador cumpre todas as metas com 100% de obediência, ignora propinas e não demonstra misericórdia. O Auditor 404 é promovido a novo Supervisor, tendo sua memória permanentemente apagada.
2. **A Corrupção do Abismo (Fim Oportunista):** O jogador aceita as propinas do submundo para comprar regalias e sabotar o Setor 7. É desmascarado ou absorvido pelas entidades clandestinas, tornando-se um contrabandista de almas.
3. **A Revelação e o Salto no Vazio (Fim Humano / Canônico):** O jogador desafia as regras nos momentos de injustiça, descobre sua verdadeira identidade e perdoa seu executor no Turno 5. Antes de ser condenado pelo Supervisor, o Auditor 404 embarca clandestinamente no trem das almas rumo ao desconhecido.

## Direção de Arte e Identidade Visual
- **Paleta de Cores:** Dominância de tons frios e desbotados — cinza ardósia, verde-oliva decadente, creme envelhecido e chumbo.
- **Pontos de Contraste Satural:** Apenas elementos de interação crucial possuem cores puras (o carimbo vermelho sangue, a cera do selo dourado, a fumaça roxa da balança e os olhos bioluminescentes das almas no escuro).
- **Tratamento de Linhas e Texturas:** Pixel art estilizada com contornos firmes e sombras tramadas (*dithering*), simulando fotografias antigas e documentos mimeografados.

## Paisagem Sonora e Trilha
- **Design de Som Diegético (O Som do Papel):**
  - Folhear papéis ásperos e gramaturas diferentes.
  - O estalo metálico pesado do carimbo batendo na mesa de madeira maciça.
  - O zumbido elétrico grave e constante (60Hz) da lâmpada fluorescente.
  - O clique rítmico do relógio de ponto mecânico marcando o final do expediente.
  - O rangido pesado das ferragens do trem chegando na estação exterior.
- **Trilha Sonora:** Minimalista e introspectiva. Drones lentos de violoncelo, notas esparsas de piano fosco e sintetizadores analógicos melancólicos que criam um clima reflexivo e solene.

## Inspirações e Referências Oficiais
- **Jogos:** *Papers, Please* (Lucas Pope), *Death and Taxes* (Placeholder Gameworks), *Grim Fandango* (LucasArts).
- **Literatura & Filosofia:** Franz Kafka (*O Processo* e *A Metamorfose*), Dante Alighieri (*A Divina Comédia*), Livro dos Mortos do Egito Antigo (O Mito da Pesagem do Coração de Osíris e Maat).
- **Cinema & Estética:** *Brazil* (Terry Gilliam), filmes noir dos anos 1940/50 e arquitetura soviética brutalista.
