# Backlog

Este documento organiza o plano de desenvolvimento do projeto individual *O Tribunal das Almas* para a disciplina CSI507 (UFOP 2026.2), sob autoria de João Miguel. As entregas e marcos acompanham a progressão acadêmica e a introdução gradual dos tópicos em aula (Concepção, Design, Desenvolvimento, Testes e Mostra de Jogos).

**Status possíveis:** TODO, IN PROGRESS, BLOCKED, DONE.

---

## Módulo 1 — Fundação & Bancada Diegética (Playable Core)

### TA-001 — Core Architecture & Game State Machine
- **Responsável:** João Miguel
- **Status:** IN PROGRESS
- **Prioridade:** Alta
- **Objetivo:** Estabelecer o gerenciador do fluxo do jogo (GameStateMachine), controle de turnos de expediente e ciclo de vida do turno.
- **Critério de conclusão:** Turno inicia, processa ciclo de almas e finaliza com relatório de desempenho funcional.

### TA-002 — Diegetic Desk & Document Interaction Framework
- **Responsável:** João Miguel
- **Status:** TODO
- **Prioridade:** Alta
- **Objetivo:** Explorar e prototipar o sistema de interação do mouse na mesa: clicar, arrastar, soltar, ordem de camadas (*sorting order*) e limites de tela conforme o tópico de interface diegética for abordado em aula.
- **Critério de conclusão:** Múltiplos documentos interativos com sensação tátil de papel e sem sobreposição corrompida.

### TA-003 — Soul Data Model & ScriptableObject Architecture
- **Responsável:** João Miguel
- **Status:** PENDING
- **Prioridade:** Alta
- **Objetivo:** Projetar a arquitetura orientada a dados (SoulData) que alimenta cada alma, seus documentos, diálogos e objetos.
- **Critério de conclusão:** Carregamento dinâmico de novos casos sem necessidade de alterar código da engine.

### TA-004 — Stamp & Pneumatic Dispatch System
- **Responsável:** João Miguel
- **Status:** TODO
- **Prioridade:** Alta
- **Objetivo:** Criar a ferramenta de carimbos físicos (Repouso, Reencarnação, Punição) e a alavanca pneumática que despacha a pasta de dossiê.
- **Critério de conclusão:** O jogador carimba o documento, aciona o despacho e o sistema valida a decisão contra a folha de regras.

---

## Módulo 2 — Mecânicas de Investigação Especializadas

### TA-005 — Weighing Scale of Regret (Balança do Pesar)
- **Responsável:** João Miguel
- **Status:** TODO
- **Prioridade:** Média
- **Objetivo:** Desenhar conceitualmente e prototipar a balança física interativa para pesar objetos do falecido contra o código moral, refinando a mecânica com calma ao longo do módulo de design de regras.
- **Critério de conclusão:** Animação física de pesagem e disparo do evento de feedback moral (fumaça escura ou brilho etéreo).

### TA-006 — Lie Revelation Reagent (Tinta Reveladora)
- **Responsável:** João Miguel
- **Status:** TODO
- **Prioridade:** Média
- **Objetivo:** Elaborar o conceito e a implementação exploratória do reagente que revela textos ocultos e contradições nos documentos conforme a evolução do conteúdo programático.
- **Critério de conclusão:** Mecânica de aplicar a tinta sobre áreas demarcadas revelando a camada de texto secreta.

### TA-007 — Interrogation Dialogue & Soul Expression Controller
- **Responsável:** João Miguel
- **Status:** TODO
- **Prioridade:** Média
- **Objetivo:** Sistema de perguntas curtas através do interfone do guichê e reação facial/expressão da alma.
- **Critério de conclusão:** O jogador seleciona uma inconsistência e a alma responde pelo balão/áudio diegético.

### TA-008 — Soul Restlessness & Diegetic Pressure System
- **Responsável:** João Miguel
- **Status:** TODO
- **Prioridade:** Média
- **Objetivo:** Temporizador psicológico diegético (relógio mecânico de parede e alma inquieta que bate no vidro se a análise demorar).
- **Critério de conclusão:** Queda de paciência gera efeitos visuais/sonoros e afeta a avaliação final do dia.

---

## Módulo 3 — Sistemas de Mundo, Moralidade e Meta-Progresso

### TA-009 — Supervisor Evaluation & Bureaucratic Ruleset
- **Responsável:** João Miguel
- **Status:** TODO
- **Prioridade:** Média
- **Objetivo:** Lógica de auditoria ao final de cada dia conduzida pelo Inspetor Kael, emitindo advertências por infrações.
- **Critério de conclusão:** 3 advertências ativam o fim de jogo administrativo; cumprimento correto avança o turno e gera recompensas.

### TA-010 — Clandestine Bribes & Karma Tracker
- **Responsável:** João Miguel
- **Status:** TODO
- **Prioridade:** Baixa
- **Objetivo:** Sistema de bilhetes secretos do submundo oferecendo propinas para desvio de almas e acompanhamento da bússola moral do auditor.
- **Critério de conclusão:** Escolhas clandestinas influenciam as variáveis que determinam os finais do jogo.

---

## Módulo 4 — Conteúdo Narrativo e Apresentação

### TA-011 — Narrative Campaign (Turnos 1 a 5)
- **Responsável:** João Miguel
- **Status:** DONE (Fase de Roteiro e Fichas - Etapa 4)
- **Prioridade:** Alta
- **Objetivo:** Planejar e redigir conceitualmente os dossiês, certidões e dilemas narrativos da campanha, amadurecendo o conteúdo à medida que as diretrizes de design de narrativa forem apresentadas.
- **Critério de conclusão:** Dossiês e fichas estruturadas conforme os 5 turnos e 3 atos dramáticos.

### TA-012 — Visual Polish, Audio Foley & Final Build
- **Responsável:** João Miguel
- **Status:** TODO
- **Prioridade:** Alta
- **Objetivo:** Arte final de pixel art da mesa e personagens, sonoplastia pesada de escritório analógico (papel, carimbo, zumbido) e build final para Windows e WebGL.
- **Critério de conclusão:** Jogo polido, testado e empacotado para a Mostra de Jogos da disciplina ao final do semestre.
