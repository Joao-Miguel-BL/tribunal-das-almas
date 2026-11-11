# Design Decisions

Este documento registra as decisões formais de Game Design de *O Tribunal das Almas*. Toda mudança conceitual ou técnica relevante deve ser documentada aqui com justificativa e consequências no projeto.

---

## 2026-09-16 — DD-001: Nome Provisório e Conceito Raiz
- **Status:** Aprovado
- **Decisão:** Adotar *O Tribunal das Almas* (com subtítulo de trabalho *Setor de Triagem 7*) como título oficial do projeto.
- **Motivo:** Origina-se da Ideia 19 ("O Tribunal do Além") na lista de 100 ideias da Etapa 1a de CSI507.
- **Consequências:** Toda a documentação e os registros acadêmicos passam a referenciar este título.

## 2026-09-24 — DD-002: Perspectiva de Câmera e Interface de Bancada
- **Status:** Aprovado
- **Decisão:** O jogo utilizará perspectiva em primeira pessoa estática de bancada de trabalho (estilo mesa de escritório diegética 2D), sem movimentação livre de avatar.
- **Motivo:** Reduz drasticamente o risco de produção artística e física de plataformas, concentrando o esforço no polimento de UX, interação tátil com papéis e profundidade de roteiro.
- **Consequências:** Elimina a necessidade de spritesheets de caminhada, pulo, colisão com cenário e mapa aberto.

## 2026-09-25 — DD-003: A Balança do Pesar como Diferencial Mecânico
- **Status:** Aprovado
- **Decisão:** Integrar a mecânica da "Balança do Pesar" na mesa de trabalho, onde cada alma traz um objeto pessoal que reage fisicamente ao peso de sua culpa.
- **Motivo:** Fruto da dinâmica de brainstorming da Árvore de Ideias da Etapa 1b, evitando que o jogo seja apenas um "leitor de texto" e adicionando ludicidade física/tátil.
- **Consequências:** Requer o desenvolvimento de um minissistema de pesagem e feedback visual (fumaça negra vs. brilho etéreo).

## 2026-09-26 — DD-004: Pressão Temporal Diegética (Inquietação da Alma)
- **Status:** Aprovado
- **Decisão:** Adicionar um relógio mecânico audível na parede e o comportamento de inquietação da alma atrás do vidro do guichê caso a investigação demore.
- **Motivo:** Proposto no feedback crítico dos avaliadores da Etapa 1b para evitar que o jogador demore indefinidamente em cada caso sem senso de urgência.
- **Consequências:** Cria tensão dramática autêntica e reforça o sentimento de pressão burocrática.

## 2026-09-27 — DD-005: Ponto de Virada Narrativo do Auditor 404
- **Status:** Aprovado
- **Decisão:** O Auditor 404 descobrirá gradualmente laudos de sua própria morte no passado, culminando na chegada de seu executor no 5º turno.
- **Motivo:** Sugestão unânime dos avaliadores para dar motivação pessoal e emocional ao protagonista, evitando que o auditor seja apenas uma casca vazia.
- **Consequências:** O roteiro do Turno 5 torna-se o clímax da campanha, exigindo múltiplos finais baseados na escolha do jogador.

## 2026-09-30 — DD-006: Escolha como Projeto de Curso Oficial
- **Status:** Aprovado
- **Decisão:** Eleger *O Tribunal das Almas* como o projeto definitivo a ser desenvolvido até o fim da disciplina CSI507.
- **Motivo:** Apresenta a melhor relação custo-benefício de desenvolvimento: arte e programação altamente viáveis em paralelo ao estágio e matérias da faculdade, sem abrir mão de qualidade e originalidade.
- **Consequências:** Descarte formal das outras 4 opções refinadas da Etapa 1b e início imediato da pré-produção.

## 2026-10-01 — DD-007: Plataforma de Execução e Engine
- **Status:** Aprovado
- **Decisão:** Desenvolver na Unity 6 LTS com foco em build executável Windows e opção de export WebGL para testes no navegador.
- **Motivo:** Facilidade de submissão acadêmica na Mostra de Jogos e teste rápido por terceiros e pelo professor.
- **Consequências:** Evitar plugins pesados de terceiros que quebrem compilação WebGL.

## 2026-10-19 — DD-008: Resolução Nativa 640×360 (Upscale 3×) e Dithering Anos 1960
- **Status:** Aprovado
- **Decisão:** Adotar grade nativa de 640×360 px (16:9) com escalonamento inteiro 3× para 1920×1080 px (Point / No Filter), utilizando tramas de Ordered Bayer Dithering (2×2 e 4×4) e contornos seletivos de 1px (#1C2026) apenas nos objetos manipuláveis da bancada.
- **Motivo:** Garante legibilidade nítida dos documentos em Full HD, evoca a textura de arquivos mimeografados dos anos 1960 e mantém o volume de produção de sprites viável para desenvolvimento individual.
- **Consequências:** Todos os assets de interface e bancada devem ser exportados na proporção 1:1 para o canvas 640×360 sem anti-aliasing de borda.

## 2026-10-19 — DD-009: Paleta Semântica 70-20-10 e Acessibilidade nos Carimbos
- **Status:** Aprovado
- **Decisão:** Restringir a paleta oficial a 10 cores sob a proporção espacial 70-20-10 (70% arquitetura brutalista fria, 20% papelaria creme de alto contraste WCAG AAA >10.5:1 e 10% acentos saturados exclusivos para carimbos e reações místicas), diferenciando também a silhueta dos cabos dos 3 carimbos.
- **Motivo:** Dirigir imediatamente o olhar do jogador para as ferramentas de decisão e garantir acessibilidade plena para jogadores daltônicos.
- **Consequências:** Nenhum elemento decorativo de cenário pode utilizar as cores funcionais Dourado (#D4AF37), Azul (#2B5B84) ou Carmim (#9E2A2B).

## 2026-10-19 — DD-010: Arquitetura Espacial das 5 Zonas da Bancada Diegética
- **Status:** Aprovado
- **Decisão:** Dividir o canvas 16:9 em 5 Zonas fixas: Zona A (Guichê, Relógio e Interfone), Zona B (Balança do Pesar), Zona C (Tapete central de documentos), Zona D (Tinta Reveladora, Carimbos e Alavanca) e Zona E (Fresta de Valter Sombra e Gaveta).
- **Motivo:** Criação de layout intuitivo e ergonômico, evitando sobreposição de ferramentas sobre os documentos de leitura.
- **Consequências:** Cada zona mapeia para uma Sorting Layer específica na Unity (00_Background_Booth a 40_ActiveTool_VFX).

## 2026-10-31 — DD-011: Feedback Visual da Tinta Reveladora e Ordem do Interfone
- **Status:** Aprovado (Pós-Playtest Etapa 6)
- **Decisão:** Adicionar um sutil halo violeta (#BB8FCE) pulsante ao redor das tarjas pretas quando o conta-gotas for selecionado e permitir a 1ª pergunta do interfone antes de aplicar a tinta.
- **Motivo:** No playtest da Etapa 6, jogadores hesitaram sobre onde a tinta funcionava e desejaram usar o interfone previamente para poupar gotas escassas de reagente.
- **Consequências:** Reduz frustração de tentativa-e-erro e reforça o valor tático de conservação de recursos.

## 2026-10-31 — DD-012: Onboarding Sem Pressão Temporal no Caso 1 da Demo
- **Status:** Aprovado (Pós-Playtest Etapa 6)
- **Decisão:** Pausar o relógio mecânico e desativar o temporizador de Inquietação da Alma durante o primeiro caso da Demo (Dona Zulmira Tavares) até que o jogador aplique o primeiro carimbo.
- **Motivo:** Jogadores relataram ansiedade excessiva ao ler o manual pela primeira vez enquanto viam o relógio correndo.
- **Consequências:** Garante curva de aprendizado suave (scaffolding diegético) sem punir o jogador na primeira experiência.

## 2026-10-31 — DD-013: Regra Explícita no Contrapeso da Balança e Interação de Valter
- **Status:** Aprovado (Pós-Playtest Etapa 6)
- **Decisão:** Gravar em baixo-relevo no contrapeso da Balança a diretriz: "Brilho Dourado + Prova Revelada = Isenção de Multa", e fazer a balança fumegar em fumaça púrpura caso o jogador coloque os bilhetes de Valter Sombra no prato.
- **Motivo:** Responde a duas reações espontâneas observadas nas sessões de teste com Lucas e Mariana.
- **Consequências:** Transparência de regras sem quebrar a diegese e adição de easter egg tátil recompensador.
