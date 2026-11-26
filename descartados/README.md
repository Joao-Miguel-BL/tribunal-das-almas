# Descartados — O Tribunal das Almas (Setor de Triagem 7)

Esta pasta existe na raiz do repositório (**fora de `Assets/`**) para guardar rascunhos, protótipos rejeitados, experimentos, assets temporários e ideias não utilizadas durante o desenvolvimento do projeto.

## Finalidade

- **Análise Pós-Morte (Post-Mortem):** preservar histórico das decisões de design, mecânicas testadas e caminhos descartados para revisão ao final da Vertical Slice e do projeto completo.
- **Manter a Unity limpa:** por ficar fora de `Assets/`, os arquivos armazenados aqui não são importados pela Unity, não geram `.meta` e não afetam o tempo de compilação nem o tamanho das builds.

## Sugestão de Organização

Ao mover algo para esta pasta, prefira agrupar em subpastas descritivas (por exemplo, `AAAA-MM-DD_nome-do-experimento/`) e incluir uma nota curta explicando:

1. O que foi testado ou rascunhado.
2. Por que foi descartado ou substituído.
3. Possíveis aprendizados para revisitar no post-mortem.
