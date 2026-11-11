# UNIVERSIDADE FEDERAL DE OURO PRETO (UFOP)
## INSTITUTO DE CIÊNCIAS EXATAS E APLICADAS (ICEA) | DECSI
### CSI507 — DESIGN E DESENVOLVIMENTO DE JOGOS | SEMESTRE 2026.2
**Aluno:** João Miguel Barros de Lacerda (Desenvolvimento Individual)  
**Projeto de Curso: Etapa 5 — Identidade Visual, Mecânicas e Protótipos Preliminares**  
**Título do Jogo:** O Tribunal das Almas (*Setor de Triagem 7*)  
**Docente Responsável:** Prof. Tiago França Melo de Lima | **Data de Submissão:** Outubro de 2026

---

# 1. RECAPITULAÇÃO DO CONCEITO E COERÊNCIA ESTÉTICO-NARRATIVA

*O Tribunal das Almas* é um simulador investigativo e burocrático de julgamento moral em perspectiva 2D diegética de bancada (1ª pessoa estática), desenvolvido para PC (Windows e WebGL via Unity 6 LTS). Ambientado em uma repartição administrativa do pós-vida congelada na estética brutalista dos anos 1960, o jogo coloca o jogador na pele do Auditor 404 (cuja identidade terrena suprimida é a do ex-arquivista municipal Gabriel Lacerda). Sob a supervisão implacável do Inspetor Kael, o assédio clandestino do contrabandista Valter Sombra e a pressão constante do relógio de ponto, o jogador examina dossiês, pesa relíquias pessoais na Balança do Pesar, aplica a Tinta Reveladora para queimar mentiras documentais e carimba o destino eterno de cada alma — Repouso Eterno, Reencarnação Imediata ou Punição Correcional (Expurgo) — até confrontar, no último expediente, a jornalista Helena Vasconcellos, vítima direta de sua omissão em vida.

Esta Etapa 5 consolida a ponte entre a concepção narrativa (Etapas 3 e 4) e a futura validação física e digital (Etapas 6 e 7), alinhando direção de arte, paleta cromática, tipografia, especificação formal de mecânicas e protótipos de interface aos quatro pilares do projeto: (1) Burocracia Diegética e Tátil, (2) Ambiguidade Moral Autêntica, (3) Pressão Sistêmica vs. Compaixão e (4) Viabilidade de Escopo Individual.

---

# 2. DIREÇÃO DE ARTE E IDENTIDADE VISUAL

## 2.1. Tratamento Gráfico, Resolução Base e Técnica de Dithering
- **Resolução Nativa e Escalonamento:** Pixel art estilizada na resolução base de 640×360 pixels (16:9), escalonada por multiplicador inteiro exato (integer scaling 3×) para 1920×1080 pixels (Full HD) com amostragem *Point (No Filter)* na Unity 6 LTS.
- **Sombras Tramadas (Ordered Bayer Dithering 2×2 e 4×4):** As transições de iluminação fluorescente, a névoa da estação ferroviária e as manchas de papel utilizam padrões de pontilhado (*dithering*), evocando impressos mimeografados e fotografias de arquivo policial dos anos 1960.
- **Contornos Secos Seletivos (1px Outline):** Objetos manipuláveis sobre a mesa possuem contorno sólido escuro (#1C2026) de 1px para destaque imediato, enquanto as silhuetas das almas atrás do vidro aramado utilizam bordas difusas sem contorno preto.

## 2.2. Moodboard Conceitual e Referências Visuais Justificadas
1. **Papers, Please (Lucas Pope, 2013):** Divisão espacial entre o guichê superior e a bancada inferior de inspeção, uso de pixel fonts de leitura rápida e peso mecânico dos carimbos.
2. **Return of the Obra Dinn (Lucas Pope, 2018) & Impressos Anos 1960:** Uso expressivo de tramas de dithering nas sombras do escritório, retratos 3×4 das Certidões de Passamento e recortes do Jornal do Além.
3. **Death and Taxes (Placeholder Gameworks, 2020) & Beholder (Warm Lamp Games, 2016):** Representação das almas e do Inspetor Kael por meio de silhuetas sombrias de forte leitura geométrica com pontos focais luminosos (óculos, olhos, cinzas).
4. **Arquitetura Brutalista Anos 1960 & Cinema Noir (O Processo, 1962; Brazil, 1985):** Concreto armado aparente, divisórias de vidro aramado (*wired glass*), tubulações pneumáticas de latão oxidado e arquivos de aço verde-oliva.

## 2.3. Paleta de Cores Oficial e Regra de Proporção 70-20-10

| Código Hex | Valor RGB | Nome Semântico | Categoria (Regra 70-20-10) | Aplicação Visual na Bancada e Cenário |
| :---: | :---: | :---: | :---: | :--- |
| **#1C2026** | RGB(28, 32, 38) | Chumbo Setor 7 | 70% Base Brutalista | Sombras profundas, contornos de 1px e sobretudo do Inspetor Kael |
| **#343C47** | RGB(52, 60, 71) | Cinza Ardósia | 70% Base Brutalista | Paredes de concreto, esquadrias metálicas do guichê e vidro aramado |
| **#4A5448** | RGB(74, 84, 72) | Oliva Decadente | 70% Base Brutalista | Tapete de linóleo da bancada, gavetas e fichários metálicos |
| **#2E231E** | RGB(46, 35, 30) | Mogno Desgastado | 70% Base Brutalista | Moldura estrutural da mesa de trabalho e cabos torneados dos carimbos |
| **#DFD5C0** | RGB(223, 213, 192) | Creme Certidão | 20% Papelaria | Papel oficial das Certidões de Passamento e laudos da Diretoria |
| **#C8B99E** | RGB(200, 185, 158) | Mimeógrafo Antigo | 20% Papelaria | Cartas pessoais, diários íntimos, jornais e prontuários antigos |
| **#D4AF37** | RGB(212, 175, 55) | Dourado Repouso | 10% Acento Funcional | Carimbo de Repouso Eterno e partículas de levitação da Balança |
| **#2B5B84** | RGB(43, 91, 132) | Azul Reencarnação | 10% Acento Funcional | Carimbo de Reencarnação Imediata e manchas nos dedos do Auditor 404 |
| **#9E2A2B** | RGB(158, 42, 43) | Carmim Expurgo | 10% Acento Funcional | Carimbo de Punição Correcional, autos de infração de Kael e alertas |
| **#6C3483** | RGB(108, 52, 131) | Púrpura Revelador | 10% Acento Místico | Fluido da Tinta Reveladora (#BB8FCE ativo) e fuligem densa da Balança |

## 2.4. Tipografia Diegética e Hierarquia de Leitura
- **Nível 1 — Institucional e Selos de Carimbo:** Fonte Grotesca Condensada / Stencil (*Chakra Petch Bold* / *Special Elite*), caixa-alta, 12–16px nativos.
- **Nível 2 — Corpo Documental Datilografado:** Fonte Monoespaçada de Máquina de Escrever (*Courier Prime* / *IBM Plex Mono*), 8–10px nativos (#23211E, contraste WCAG AAA).
- **Nível 3 — Manuscritos, Cartas e Rasuras Reveladas:** Fonte Cursiva / Caligráfica Estilizada, aplicada em cartas pessoais, bilhetes clandestinos de Valter Sombra e textos ocultos revelados em púrpura (#BB8FCE).

---

# 3. ESPECIFICAÇÃO ESTRUTURADA DE MECÂNICAS (TEMPLATE OFICIAL CSI507)

## 3.1. Tabela-Resumo das Mecânicas
- **M1 (Core — Alta):** Manipulação Tátil e Inspeção Documental na Bancada.
- **M2 (Core / USP — Alta):** Pesagem Moral na Balança do Pesar.
- **M3 (Core / USP — Alta):** Aplicação Químico-Mística da Tinta Reveladora.
- **M4 (Core — Alta):** Selagem por Carimbos de Veredito e Despacho Pneumático.
- **M5 (Secundária — Média):** Interrogatório Curto de Contradições via Interfone.
- **M6 (Secundária — Média):** Pressão Temporal Diegética, Inquietação e Subornos Clandestinos.

*(Consulte o documento completo para as fichas detalhadas com Input, Resposta do Sistema, Condições de Uso, Feedback e Falha Graciosa).*

---

# 4. PROTÓTIPOS PRELIMINARES DE INTERFACE, WIREFRAMES E FLUXO DE TELAS

## 4.1. Mapeamento das 5 Zonas da Bancada (16:9)
- **Zona A (Superior — Guichê e Pressão Temporal | 00_Background_Booth):** Janela de vidro aramado com a silhueta da alma, Quadro de Avisos à esquerda (Cota do Turno e até 3 Autos de Infração de Kael), Relógio Analógico à direita e Interfone [M5].
- **Zona B (Esquerda — Triagem Metafísica | 20_DeskProps_Fixed):** Balança do Pesar [M2] com dois pratos (Relíquia vs. Código de Triagem) e bandeja de entrada de objetos.
- **Zona C (Centro — Inspeção Documental | 30_DraggableDocs):** Tapete de linóleo verde-oliva para manipulação livre e sobreposição de Certidões, Laudos, Cartas e Manual do Auditor [M1].
- **Zona D (Direita — Reagente, Carimbos e Despacho | 40_ActiveTool_VFX):** Frasco de Tinta Reveladora [M3] (3 gotas/dia), suporte com os 3 Carimbos de Veredito e Alavanca do Tubo Pneumático [M4].
- **Zona E (Inferior — Submundo e Gaveta | 10_DeskSurface):** Fresta inferior por onde deslizam os bilhetes de suborno de Valter Sombra [M6] e gaveta de itens retidos.

## 4.2. Fluxo de Navegação de Telas (UI State Machine)
1. **Tela 1 — Arquivo Central (Título):** Gavetas metálicas interativas ("Iniciar Expediente", "Configurações", "Sair").
2. **Tela 2 — Abertura de Turno:** Leitura do *Jornal do Além*, diretriz diária de Kael e batida do cartão no relógio de ponto.
3. **Tela 3 — Bancada de Triagem (Core Loop):** Processamento dos dossiês e relíquias do turno (M1 a M6).
4. **Tela 4 — Auditoria de Fim de Turno:** Avaliação presencial do Inspetor Kael, emissão de advertências e desbloqueio de memórias de Gabriel Lacerda.
5. **Tela 5 — Desfecho (3 Finais):** Clímax com Helena Vasconcellos no Turno 5 (Fim Burocrático, Fim Oportunista ou Fim Humanista/Salto Livre).
