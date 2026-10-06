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
