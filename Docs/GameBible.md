# Game Bible

## Identidade Básica

- **Título oficial:** O Tribunal das Almas (Título de trabalho alternativo: *Setor de Triagem 7*)
- **Gênero:** Simulador Burocrático / Investigação Moral / Document Thriller
- **Perspectiva:** 2D Diegética (Visão em primeira pessoa estática de bancada de trabalho)
- **Engine:** Unity 6 LTS
- **Plataforma alvo:** Windows / WebGL (foco em entrega rápida e execução direta)
- **Resolução e Estilo Visual:** Pixel art estilizada em grade nativa 640×360 px (integer scaling 3× para 1920×1080 px), alto contraste com sombras tramadas (Ordered Bayer Dithering 2×2 e 4×4), estética burocrática brutalista dos anos 1960.
- **Equipe:**
  - João Miguel Barros de Lacerda — Desenvolvimento Individual (Game Design, Roteiro, Arte Técnica e Programação).

## Visão Criativa e High Concept

*O Tribunal das Almas* explora a frieza dos sistemas administrativos quando confrontados com a ambiguidade da moralidade humana. O jogador assume o controle do Auditor 404 (Gabriel Lacerda), um funcionário amnésico de uma repartição pós-vida encarregado de julgar recém-falecidos com base em dossiês oficiais, depoimentos e relíquias pessoais.

O cerne do jogo não é punir ou absolver mecanicamente, mas experimentar o peso desconfortável de reduzir uma existência inteira a selos de carimbo sob a pressão do relógio e as ordens severas de uma hierarquia cósmica impessoal.

## Pilares de Design

1. **Burocracia Diegética e Tátil (Zero-HUD):** Todos os elementos de interface pertencem fisicamente ao mundo do jogo. Não há menus flutuantes invasivos: papéis têm gramatura e peso, folhas são arrastadas, carimbos batem contra o tampo da mesa e o relógio analógico de parede dita o ritmo.
2. **Ambiguidade Moral Autêntica:** Nenhum réu é puramente herói ou vilão. As fichas apresentam nuances éticas (atos de desespero por amor, crimes cometidos para salvar outros, virtudes manchadas por omissão), forçando escolhas genuinamente difíceis.
3. **Pressão Sistêmica vs. Compaixão:** O jogador opera sob um regulamento rígido, cotas diárias e a vigilância do Inspetor Kael. Oferecer misericórdia consome tempo e recursos escassos (como as 3 gotas diárias de Tinta Reveladora), arriscando a estabilidade do próprio auditor.
4. **Escopo Contido e Progresso Acadêmico:** O projeto rejeita sistemas gigantescos de movimentação ou física aberta em prol de uma estrutura narrativa e mecânica modular, refinada e plenamente executável individualmente na disciplina CSI507.

## Universo e Lore (Worldbuilding)

### O Setor de Triagem 7
Uma repartição administrativa suspensa em um plano cinzento e atemporal, construída sob os preceitos da arquitetura brutalista dos anos 1960. Paredes de concreto aparente descascadas, lâmpadas fluorescentes tubulares que zumbem de forma intermitente e estantes monumentais repletas de caixas de arquivo que se perdem na escuridão do teto.

Do lado de fora do vidro aramado do guichê, há apenas um abismo de névoa leitosa cortado por trilhos suspensos, por onde passam composições ferroviárias sem condutor que partem apinhadas de almas carimbadas para seus destinos finais.

### A Estrutura do Pós-Vida (Os Três Destinos)
1. **Repouso Eterno (Carimbo Dourado #D4AF37):** Destinado àqueles cuja balança moral registra ausência de débitos graves ou atos de sacrifício altruísta.
2. **Reencarnação Imediata (Carimbo Azul #2B5B84):** Para almas neutras, vidas inacabadas ou indivíduos com pendências mundanas comuns que exigem reinício biológico.
3. **Punição Correcional / Expurgo (Carimbo Carmim #9E2A2B):** Encaminhamento para as câmaras de retificação para almas que cometeram transgressões egoístas deliberadas.

### Personagens Centrais
- **Auditor 404 / Gabriel Lacerda (Protagonista):** Ex-arquivista municipal nos anos 1960 que ocultou provas de um desvio de verbas hospitalares por covardia e morreu no incêndio do arquivo municipal. Visualizado em 1ª pessoa pelas mãos pálidas com dedos manchados de tinta azul (#2B5B84) e pelo reflexo sutil no vidro aramado.
- **Inspetor Kael — Conformidade 07-K (Antagonista Sistêmico):** Silhueta monolítica vestindo sobretudo pesado em Chumbo (#1C2026) e chapéu fedora que projeta sombra perpétua sobre os olhos, destacando apenas o reflexo branco de seus óculos redondos.
- **Valter Sombra — Dr. Valter Brandão (Tentador Clandestino):** Ex-advogado criminalista que opera uma rede de tráfico de almas nos dutos sob o Setor 7. Seus dedos em luvas de couro sem dedos, abotoaduras de ouro oxidado e mechas de fumaça violácea emergem da fresta inferior da bancada (Zona E).
- **Helena Vasconcellos (Catalisador Dramático):** Jovem jornalista investigativa que tentou denunciar a fraude hospitalar nos anos 1960 e morreu após Gabriel Lacerda negar-lhe os documentos comprobatórios. Surge no último expediente como silhueta translúcida em capa de chuva encharcada, segurando um caderno de anotações queimado do qual cinzas e brasas levitam.

## Especificação Estruturada das Mecânicas (M1 a M6)

| ID | Nome da Mecânica | Categoria / Prioridade | Input Principal | Síntese de Regra e Condição de Uso |
| :---: | :--- | :---: | :--- | :--- |
| **M1** | Inspeção e Manipulação Documental | Core / Alta | Mouse0 Drag / Scroll Zoom | Uso livre na Zona C; ordena sortingOrder na camada 30_DraggableDocs para cruzar Certidão, Extrato e Manual. |
| **M2** | Pesagem Moral na Balança do Pesar | Core (USP) / Alta | Mouse0 Drag & Drop da relíquia | 1 uso por alma na Zona B. Compara karmicWeight: Equilíbrio (0), Culpa (+1 com fuligem) ou Sacrifício (-1 com brilho dourado). |
| **M3** | Aplicação da Tinta Reveladora | Core (USP) / Alta | Conta-gotas na Zona D + Clique sobre tarja | Recurso escasso: 3 gotas por turno diário. Dissolve tarjas pretas (com halo violeta DD-011) revelando a verdade oculta. |
| **M4** | Selagem por Carimbos e Despacho | Core / Alta | Selecionar carimbo + Carimbar + Alavanca | Exige 1 selo na Certidão (Dourado, Azul ou Carmim) para destravar a alavanca pneumática. Decisão irreversível. |
| **M5** | Interrogatório via Interfone | Secundária / Média | Botão metálico do Interfone (Zona A) | Até 2 perguntas por caso. Exibe testemunho em fita datilografada sobre o vidro e consome 15s do relógio. |
| **M6** | Pressão Temporal e Subornos | Secundária / Média | Gestão de relógio / Fresta de Valter (Zona E) | Ultrapassar 90s ativa Inquietação. 3 multas geram demissão. Aceitar subornos dá tempo/abono mas eleva corrupção. |

## Paleta de Cores Oficial (Regra 70-20-10)

| Código Hex | Valor RGB | Nome Semântico | Categoria | Aplicação Visual |
| :---: | :---: | :---: | :---: | :--- |
| **#1C2026** | RGB(28, 32, 38) | Chumbo Setor 7 | 70% Base Brutalista | Sombras profundas, contornos de 1px e sobretudo de Kael |
| **#343C47** | RGB(52, 60, 71) | Cinza Ardósia | 70% Base Brutalista | Paredes de concreto, esquadrias e vidro aramado |
| **#4A5448** | RGB(74, 84, 72) | Oliva Decadente | 70% Base Brutalista | Tapete de linóleo da bancada, gavetas e fichários |
| **#2E231E** | RGB(46, 35, 30) | Mogno Desgastado | 70% Base Brutalista | Moldura da mesa de trabalho e cabos dos carimbos |
| **#DFD5C0** | RGB(223, 213, 192) | Creme Certidão | 20% Papelaria | Papel oficial das Certidões de Passamento e laudos |
| **#C8B99E** | RGB(200, 185, 158) | Mimeógrafo Antigo | 20% Papelaria | Cartas pessoais, diários íntimos e jornais |
| **#D4AF37** | RGB(212, 175, 55) | Dourado Repouso | 10% Acento Funcional | Carimbo de Repouso Eterno e partículas da Balança |
| **#2B5B84** | RGB(43, 91, 132) | Azul Reencarnação | 10% Acento Funcional | Carimbo de Reencarnação e manchas nos dedos do 404 |
| **#9E2A2B** | RGB(158, 42, 43) | Carmim Expurgo | 10% Acento Funcional | Carimbo de Punição Correcional e alertas de infração |
| **#6C3483** | RGB(108, 52, 131) | Púrpura Revelador | 10% Acento Místico | Fluido da Tinta Reveladora e fuligem densa da Balança |

## Hierarquia Tipográfica Diegética
- **Nível 1 (Institucional e Selos):** Grotesca Condensada / Stencil (*Chakra Petch Bold* / *Special Elite*), 12–16px nativos.
- **Nível 2 (Corpo Documental):** Monoespaçada de Máquina (*Courier Prime* / *IBM Plex Mono*), 8–10px nativos (#23211E, contraste WCAG AAA).
- **Nível 3 (Manuscritos e Rasuras):** Caligráfica Cursiva estenográfica em cartas pessoais e revelações em púrpura (#BB8FCE).
