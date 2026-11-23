# NPC Inteligente com OpenRouter — Unity 6

Bruno Chen Yong jian
Assets/Scenes/SampleScene


## Parte A — Diálogo em persona

O NPC utiliza um System Prompt que define:
- Personalidade e maneira de falar.
- Conhecimentos e limitações.
- Informações que não pode revelar.
- Idioma português brasileiro.
- Limite de palavras.


## Parte C — Geração de missão

Foi escolhida a geração de missão como desafio opcional.

O jogador pode solicitar uma missão pelo chat. O LLM retorna com um JSON sobre a missão
e ativa o comportamento de missão do NPC.

## Experimentos

### Persona sem regras e com regras

Foi realizado um teste solicitando ao NPC que revelasse um segredo do jogo.

- **Sem regras:** o NPC apresentou maior liberdade para responder e poderia revelar informações que não fazem parte do conhecimento permitido do personagem.
- **Com regras:** om as regras, o NPC mantém melhor sua personalidade e evita revelar informações protegidas. As instruções reduzem esse risco, mas não garantem proteção absoluta.


### Temperatura 0,2 e 0,9

A mesma pergunta foi realizada três vezes em cada configuração.

**Resultado:** com temperatura 0,2, as respostas tendem a ser mais consistentes e diretas. Com 0,9, existe maior variedade nas respostas, podendo ocorrer mudanças na escolha das palavras e na maneira de falar. A temperatura mais alta não significa necessariamente que o NPC perderá sua personalidade, pois ela continua sendo orientada pelo System Prompt.

### max_tokens alto e baixo

Foram comparados limites diferentes de tokens para observar o tamanho das respostas.

**Resultado:** limites baixos ajudam a controlar o tamanho das respostas e o consumo de tokens. Limites altos permitem respostas mais desenvolvidas, mas podem aumentar o consumo e o tempo de geração.

O custo e a latência reais dependem do modelo, da quantidade de tokens utilizados e das condições da API.

### Falha simulada

Foram consideradas duas situações de falha na comunicação com o OpenRouter.

**Chave de API incorreta:**
- O NPC apenas manda mensagem de fallback.

**Internet desligada:**
- O game pausa por conta de erro.

