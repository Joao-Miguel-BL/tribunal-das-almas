# Backlog

Este documento organiza o plano de desenvolvimento do projeto individual *O Tribunal das Almas* para a disciplina CSI507 (UFOP 2026.2), sob autoria de João Miguel Barros de Lacerda. As entregas e marcos acompanham a progressão acadêmica (Concepção, Design, Prototipação, Vertical Slice, Demo e Mostra de Jogos).

**Status possíveis:** TODO, IN PROGRESS, BLOCKED, DONE.

---

## Módulo 0 — Concepção, Narrativa, Identidade Visual e Especificação (Etapas 1 a 6)

### TA-000A — High Concept & Worldbuilding Narrativo (Etapas 1 a 4)
- **Responsável:** João Miguel
- **Status:** DONE
- **Prioridade:** Alta
- **Objetivo:** Consolidar o High Concept Document, Story Spine, regras do Setor de Triagem 7, fichas completas dos 4 personagens (Auditor 404, Inspetor Kael, Valter Sombra e Helena Vasconcellos) e estrutura dos 3 finais.
- **Critério de conclusão:** Relatórios das Etapas 1 a 4 aprovados e documentados no repositório.

### TA-000B — Direção de Arte, Paleta 70-20-10, Fichas de Mecânicas e Wireframes UI (Etapa 5)
- **Responsável:** João Miguel
- **Status:** DONE
- **Prioridade:** Alta
- **Objetivo:** Definir a resolução nativa 640×360 (upscale 3×), paleta de 10 cores Hex/RGB, hierarquia tipográfica, fichas formais das mecânicas M1 a M6 e wireframes das Zonas A a E + Sorting Layers.
- **Critério de conclusão:** Relatório em PDF da Etapa 5 gerado e documentação técnica sincronizada.

### TA-000C — Prototipação Física em Baixa Fidelidade / Papel e Playtest (Etapa 6)
- **Responsável:** João Miguel
- **Status:** DONE
- **Prioridade:** Alta
- **Objetivo:** Confeccionar o kit em papel da bancada do Auditor 404 (dossiês físicos, tarjas removíveis para a Tinta Reveladora e tabuleiro da Balança do Pesar) e conduzir sessões de playtest com voluntários.
- **Critério de conclusão:** 2 sessões de playtest realizadas, Formulários de Playtest preenchidos (Template 7) e relatório da Etapa 6 entregue com os 3 ajustes de design (DD-011, DD-012, DD-013) concluídos.

---

## Módulo 1 — Fundação & Bancada Diegética (Playable Core — Etapa 7)

### TA-001 — Core Architecture & Game State Machine
- **Responsável:** João Miguel
- **Status:** IN PROGRESS
- **Prioridade:** Alta
- **Objetivo:** Estabelecer o gerenciador do fluxo das 5 telas do jogo (GameStateMachine: Arquivo Central -> Abertura/Jornal -> Bancada -> Auditoria Kael -> Desfecho) e ciclo de vida do turno.
- **Critério de conclusão:** Turno inicia, processa fila de almas e finaliza com relatório de desempenho funcional.

### TA-002 — Diegetic Desk & Document Interaction Framework (M1)
- **Responsável:** João Miguel
- **Status:** TODO
- **Prioridade:** Alta
- **Objetivo:** Implementar o sistema de interação do mouse na Zona C: clicar, arrastar, soltar, zoom focal (Mouse1), sortingOrder dinâmico na camada 30_DraggableDocs e soft clamp de retorno automático.
- **Critério de conclusão:** Múltiplos documentos interativos com sensação tátil de papel e sem sobreposição corrompida.

### TA-003 — Soul Data Model & ScriptableObject Architecture
- **Responsável:** João Miguel
- **Status:** TODO
- **Prioridade:** Alta
- **Objetivo:** Projetar a arquitetura orientada a dados (SoulData) que alimenta cada alma, seus documentos, peso kármico da relíquia (karmicWeight), trechos ocultos para reagente e diálogos de interfone.
- **Critério de conclusão:** Carregamento dinâmico de novos casos sem necessidade de alterar código da engine.

### TA-004 — Stamp & Pneumatic Dispatch System (M4)
- **Responsável:** João Miguel
- **Status:** TODO
- **Prioridade:** Alta
- **Objetivo:** Criar os 3 carimbos físicos na Zona D (Repouso #D4AF37, Reencarnação #2B5B84, Expurgo #9E2A2B) e a alavanca pneumática com trava de falha graciosa caso a Certidão não esteja carimbada.
- **Critério de conclusão:** O jogador carimba o documento, aciona o despacho a vácuo e o sistema valida a decisão contra a folha de regras.

---

## Módulo 2 — Mecânicas de Investigação Especializadas

### TA-005 — Weighing Scale of Regret / Balança do Pesar (M2)
- **Responsável:** João Miguel
- **Status:** TODO
- **Prioridade:** Alta
- **Objetivo:** Implementar a balança interativa na Zona B com 3 estados físicos (Equilíbrio Neutro, Queda Brusca com fuligem #6C3483 e Levitação com brilho #D4AF37).
- **Critério de conclusão:** Animação física de pesagem, screen-shake de 2px e disparo do feedback visual/sonoro.

### TA-006 — Lie Revelation Reagent / Tinta Reveladora (M3)
- **Responsável:** João Miguel
- **Status:** TODO
- **Prioridade:** Alta
- **Objetivo:** Implementar o conta-gotas na Zona D (limite de 3 gotas/turno) que dissolve tarjas pretas via máscara 2D e revela textos ocultos em #BB8FCE com halo violeta (DD-011).
- **Critério de conclusão:** Aplicação precisa sobre áreas demarcadas atualizando evidenceUnlocked = true.

### TA-007 — Interrogation Dialogue & Soul Expression Controller (M5)
- **Responsável:** João Miguel
- **Status:** TODO
- **Prioridade:** Média
- **Objetivo:** Sistema de até 2 perguntas investigativas por caso através do interfone na Zona A, exibindo respostas em fita datilografada sobre o vidro aramado.
- **Critério de conclusão:** O jogador aciona o botão e recebe o testemunho diegético consumindo 15s de relógio.

### TA-008 — Soul Restlessness & Diegetic Pressure System (M6)
- **Responsável:** João Miguel
- **Status:** TODO
- **Prioridade:** Média
- **Objetivo:** Temporizador analógico diegético (relógio mecânico de parede e alma que bate no vidro e pisca as luzes se a análise ultrapassar 90s).
- **Critério de conclusão:** Efeitos visuais e sonoros de estresse ativados progressivamente.

---

## Módulo 3 — Sistemas de Mundo, Moralidade e Meta-Progresso

### TA-009 — Supervisor Evaluation & Bureaucratic Ruleset
- **Responsável:** João Miguel
- **Status:** TODO
- **Prioridade:** Média
- **Objetivo:** Lógica de auditoria ao final de cada dia conduzida pelo Inspetor Kael, emitindo advertências por infrações.
- **Critério de conclusão:** 3 advertências ativam demissão sumária; cumprimento correto avança o turno e gera fragmentos de memória.

### TA-010 — Clandestine Bribes & Karma Tracker
- **Responsável:** João Miguel
- **Status:** TODO
- **Prioridade:** Baixa
- **Objetivo:** Sistema de bilhetes secretos de Valter Sombra na Zona E oferecendo propinas e acompanhamento da bússola moral do auditor.
- **Critério de conclusão:** Escolhas clandestinas influenciam as variáveis que determinam os 3 finais da campanha.

---

## Módulo 4 — Conteúdo Narrativo, Polimento e Apresentação Final

### TA-011 — Narrative Campaign (Turnos 1 a 5)
- **Responsável:** João Miguel
- **Status:** DONE (Roteiro e Dossiês Completos — Etapa 4)
- **Prioridade:** Alta
- **Objetivo:** Planejar e redigir conceitualmente os dossiês, certidões e dilemas narrativos da campanha.
- **Critério de conclusão:** Dossiês e fichas estruturadas conforme os 5 turnos e 3 atos dramáticos.

### TA-012 — Visual Polish, Audio Foley & Final Build (Etapas 8 e 9)
- **Responsável:** João Miguel
- **Status:** TODO
- **Prioridade:** Alta
- **Objetivo:** Arte final de pixel art da mesa e personagens, sonoplastia analógica (papel, carimbo, zumbido) e build final para Windows e WebGL.
- **Critério de conclusão:** Jogo polido, testado e empacotado para a Mostra de Jogos da disciplina ao final do semestre.
