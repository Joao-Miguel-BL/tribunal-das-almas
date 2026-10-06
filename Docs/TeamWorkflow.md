# Workflow do Projeto (Desenvolvimento Individual)

## Objetivo

Definir como João Miguel Barros de Lacerda organiza o desenvolvimento, a documentação e os prazos de *O Tribunal das Almas*, garantindo qualidade e acompanhando o ritmo e o cronograma do professor Tiago França Melo de Lima na disciplina CSI507.

## Responsabilidades e Foco de Trabalho

### João Miguel Barros de Lacerda (Desenvolvimento Solo)
- Modelagem conceitual, decisões de Game Design e balanceamento ético;
- Redação dos dossiês narrativos, diálogos, casos do jogo e documentação oficial;
- Arquitetura de código, State Machines na Unity e UI diegética;
- Produção/seleção de assets em Pixel Art, áudio diegético e playtests individuais.

---

## Prioridade Conceitual e Ritmo Acadêmico

- As decisões conceituais e a estruturação do projeto têm prioridade sobre a codificação imediata.
- O avanço técnico deve respeitar o progresso da disciplina CSI507, evitando se precipitar em relação aos tópicos abordados em aula pelo professor Tiago França Melo de Lima.

## Git Workflow e Regras de Colaboração

- O Git será utilizado para controle de versão pessoal e para a futura sincronização com ferramentas como Antigravity e Unity assim que o repositório for criado.
- A branch main manterá as versões estáveis da documentação e do projeto, com tags para marcos da disciplina.
- Todo trabalho ocorre em branches temáticas com os seguintes prefixos padronizados:
  - `feature/TA-xxx-nome` — Novas mecânicas ou sistemas;
  - `art/TA-xxx-nome` — Novos sprites, paletas e animações;
  - `narrative/TA-xxx-nome` — Novos dossiês e textos;
  - `fix/TA-xxx-nome` — Correção de bugs encontrados;
  - `docs/TA-xxx-nome` — Atualização de documentações.
- Integração organizada por commits e merges diretos ou PRs pessoais conforme a complexidade das tarefas.

## Regras Específicas para Unity

- **Arquivos `.meta` são obrigatórios:** Nunca adicionar arquivos na pasta `Assets/` sem comitar imediatamente seus respectivos arquivos `.meta`.
- **Prevenção de conflito de Cenas:** Criar uma cena isolada para cada teste (`Sandbox_Desk`, `Sandbox_Stamps`). A cena principal (`Main_Game`) só deve ser editada com comunicação prévia.
- **Modularidade em Prefabs:** Todo elemento da mesa (documento, carimbo, balança, relógio) deve ser um Prefab independente.
- **Orientação a Dados:** As almas não devem ser hardcoded em scripts; devem ser criadas como instâncias de `ScriptableObject`.

## Critério de Conclusão de uma Tarefa (Definition of Done)

Uma tarefa do Backlog só é considerada DONE quando:
1. O código ou arte foi implementado e testado em Play Mode;
2. Não gera Warnings ou Errors no console da Unity;
3. A documentação foi atualizada (`CurrentGoals.md` e `DevLog.md`);
4. As alterações foram devidamente registradas no versionamento e validadas de acordo com os requisitos da disciplina.

## Protocolo de Manutenção da Documentação

- `CurrentGoals.md` é a bússola diária: reflete exclusivamente o que está sendo feito no momento.
- `Backlog.md` contém a visão panorâmica e os grandes pacotes até dezembro.
- `DevLog.md` é o diário histórico: atualizado a cada avanço significativo.
- `GameBible.md` é a bíblia sagrada: só muda com aprovação conjunta registrada em `DesignDecisions.md`.
