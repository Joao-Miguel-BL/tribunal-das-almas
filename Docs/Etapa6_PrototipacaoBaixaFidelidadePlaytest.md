# UNIVERSIDADE FEDERAL DE OURO PRETO (UFOP)
## INSTITUTO DE CIÊNCIAS EXATAS E APLICADAS (ICEA) | DECSI
### CSI507 — DESIGN E DESENVOLVIMENTO DE JOGOS | SEMESTRE 2026.2
**Aluno:** João Miguel Barros de Lacerda (Desenvolvimento Individual)  
**Projeto de Curso: Etapa 6 — Prototipação de Baixa Fidelidade (Fluxo, Mecânica, Dinâmica e Playtest em Papel)**  
**Título do Jogo:** O Tribunal das Almas (*Setor de Triagem 7*)  
**Docente Responsável:** Prof. Tiago França Melo de Lima | **Data de Submissão:** Outubro/Novembro de 2026

---

# 1. RECAPITULAÇÃO DO CONCEITO, NARRATIVA E ENQUADRAMENTO MDA / DPE

*O Tribunal das Almas* é um simulador investigativo e burocrático de julgamento moral em perspectiva 2D diegética de bancada (1ª pessoa estática), desenvolvido individualmente para PC (Windows e WebGL via Unity 6 LTS). Ambientado em uma repartição administrativa do pós-vida congelada na estética brutalista dos anos 1960, o jogo coloca o jogador na pele do Auditor 404.

Esta Etapa 6 especifica formalmente o funcionamento sistêmico do jogo (regras, ações, condições de vitória/derrota, curva de progressão, fase da demo e fluxograma completo de telas) e valida essa especificação por meio de prototipação física de baixa fidelidade em papel com dois jogadores externos antes da codificação da Vertical Slice (Etapa 7). A modelagem fundamenta-se nos frameworks MDA (Mechanics, Dynamics, Aesthetics) e DPE (Design, Play, Experience).

---

# 2. ESPECIFICAÇÃO COMPLETA DAS MECÂNICAS DE JOGO (PASSOS 1 E 2)

## Tabela de Mecânicas Validadas no Protótipo Físico:
- **M1 — Inspeção e Manipulação Documental (Core / Alta):** Arraste com Mouse0 e zoom focal (Mouse1/Scroll); ordenação de camadas na pilha e soft clamp de retorno nas bordas da mesa.
- **M2 — Pesagem Moral na Balança do Pesar (Core / USP / Alta):** Drag & Drop da relíquia na Balança (Zona B). Compara karmicWeight contra o contrapeso moral: Equilíbrio Neutro (0), Queda Brusca com fuligem roxa (+1) ou Levitação Suave com partículas douradas (-1).
- **M3 — Aplicação da Tinta Reveladora (Core / USP / Alta):** Conta-gotas na Zona D (máximo de 3 gotas por turno diário). Dissolve tarjas pretas de censura revelando texto oculto em caligrafia violeta luminescente (#BB8FCE).
- **M4 — Selagem por Carimbos e Despacho Pneumático (Core / Alta):** Aplicação de 1 dos 3 carimbos na Certidão de Passamento (Dourado: Repouso; Azul: Reencarnação; Carmim: Expurgo) destravando a alavanca pneumática.
- **M5 — Interrogatório de Contradições via Interfone (Secundária / Média):** Botão metálico no guichê (Zona A); até 2 perguntas investigativas por alma imprimindo respostas em fita datilografada sobre o vidro e avançando o relógio em 15s.
- **M6 — Pressão Temporal, Inquietação e Subornos Clandestinos (Secundária / Média):** Gestão do relógio analógico; ultrapassar 90s ativa batidas no vidro e piscar de luzes. Bilhetes de Valter Sombra na fresta inferior oferecem bônus de tempo em troca de corrupção.

## Ciclos do Loop Central:
- **Ciclo Micro (Por Alma — 90 a 120s):** Recepção diegética (10s) -> Inspeção documental M1 (30-40s) -> Triagem mística M2/M3/M5 (30-45s) -> Veredito e despacho M4 (10-15s) -> Resposta sistêmica M6 (5-10s).
- **Ciclo Macro (Por Turno — 8 a 12 min):** Jornal do Além + Diretriz de Kael -> Batida de Ponto -> Execução da Cota (3 a 5 almas) -> Auditoria de Kael, revelação de memória fotográfica de Gabriel Lacerda e Auto-Save.

---

# 3. CURVA DE DIFICULDADE E DESIGN DA FASE DA DEMO

## Progressão em 5 Turnos:
- **Turno 1 (Rotina das Engrenagens):** Cota de 3 almas. Introduz M1, M4 e a Balança M2. Caso 1 com relógio pausado (DD-012).
- **Turno 2 (Infiltração da Dúvida):** Cota de 3 almas. Introduz Tinta Reveladora M3 (3 gotas/dia) e Interfone M5. Dossiês com 3 folhas e tarjas pretas.
- **Turno 3 (O Preço da Compaixão):** Cota de 4 almas. Introduz bilhetes clandestinos de Valter Sombra M6. Escassez: 4 almas para 3 gotas de tinta.
- **Turno 4 (O Arquivo em Chamas):** Cota de 4 almas. Casos interligados ao incêndio de 1960. Múltiplas tarjas exigem leitura crítica.
- **Turno 5 (O Espelho Trincado):** Cota de 4 almas. Clímax com o julgamento supremo de Helena Vasconcellos sob os olhos de Kael.

## A Fase da Demo (Vertical Slice — 10 a 12 min):
- **Caso 1 (Dona Zulmira Tavares):** Tutorial implícito de manipulação e carimbo (Reencarnação). Relógio congelado.
- **Caso 2 (Valdir Nogueira, Maquinista):** Tutorial da Balança (o relógio de bolso levita em Dourado, refutando o laudo estatal de negligência).
- **Caso 3 (Artur Mendonça, Operário):** Tutorial da Tinta Reveladora e Interfone (revela compra de insulina e destrava interrogatório sob pressão do relógio).

---

# 4. CONDIÇÕES DE VITÓRIA, DERROTA E SALVAMENTO (SAVEDATA.JSON)

- **Vitória de Turno:** Cumprir a cota antes do relógio com menos de 3 advertências de Kael. Salva o progresso em `SaveData.json`.
- **Finais da Campanha:**
  - *Final 1 (A Engrenagem Perfeita — Burocrático):* Condenar Helena ao Expurgo; o auditor é promovido e perde a memória.
  - *Final 2 (Sócio do Abismo — Oportunista):* Acumular corrupção com Valter Sombra; o auditor vira contrabandista no submundo.
  - *Final 3 (O Salto Livre — Humanista/Canônico):* Absolver Helena, quebrar o vidro com a Balança e saltar no trem rumo à liberdade.
- **Condições de Derrota (Game Over):** Acumular 3 Autos de Infração de Kael ou esgotar o relógio (+15s de hora extra) com cota pendente (permite reiniciar o turno).

---

# 5. RELATÓRIO DO PLAYTEST EM PAPEL (SESSÕES COM JOGADORES EXTERNOS)

## O Kit Físico:
Bancada desenhada sobre folha A4 quadriculada (Zonas A–E), relógio analógico de clipe metálico, pratos de papelão para a Balança do Pesar, 3 fichas roxas de Tinta Reveladora e tiras de post-it preto cobrindo as rasuras dos dossiês.

## Sessões Realizadas:
- **Sessão 1 — Lucas M. (21 anos, Engenharia):** Compreendeu o loop em 2m40s. Tentou pesar o bilhete de suborno de Valter na Balança e aceitou conscientemente tomar 1 multa de Kael para salvar o operário Artur no Caso 3.
- **Sessão 2 — Mariana C. (23 anos, Arquitetura):** Sentiu ansiedade excessiva no Caso 1 por ler o manual com o relógio correndo. Tentou usar o Interfone antes da Tinta para poupar gotas no Caso 3.

## Três Ajustes Estruturais Incorporados (Pós-Playtest):
1. **DD-011:** Adição de halo violeta pulsante nas tarjas reagentes e liberação da 1ª pergunta do interfone antes de gastar a gota de tinta.
2. **DD-012:** Desativação do relógio e da inquietação durante o Caso 1 para onboarding sem pressão.
3. **DD-013:** Gravação da regra de isenção de multa no contrapeso da balança e fumaça púrpura especial ao pesar bilhetes de Valter Sombra.
