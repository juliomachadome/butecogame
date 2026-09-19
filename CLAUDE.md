# Buteco dos Devs: A Guerra Púnica

Documento central do projeto. Game Jam de **48 horas**. Leia antes de qualquer tarefa.

> **Fase atual: 3 concluída** (inimigo Hacker Rival com ataque telegrafado). Arte via PixelLab em andamento. Cena de trabalho: `Assets/_Game/Scenes/Buteco.unity`.
> Não avance de fase sem instrução explícita do usuário.

---

## 1. Contexto técnico

| Item | Valor |
|---|---|
| Unity | 6000.6.2f1 (Apple Silicon) |
| Template | Universal 2D (URP 17.6, Renderer 2D) |
| Input | **Input System 1.20** (`activeInputHandler: 1` = só o novo sistema; não usar `Input.GetAxis`) |
| UI | uGUI 2.6 |
| Pacotes 2D já presentes | 2d.animation, aseprite, psdimporter, sprite, spriteshape, tilemap, tilemap.extras, timeline |
| Plataformas | Windows, macOS, Linux |
| Raiz do projeto | `/Users/juliomachado/Documents/game-jam/ButecoDosDevs/ButecoDosDevs` |
| Unity MCP | servidor `unity-editor-mcp` (já configurado — **não reinstalar**) |
| Plugin | Unity Agent para Claude Code (skills `unity:*`) — **não reinstalar** |
| Repo | https://github.com/juliomachadome/butecogame (**PÚBLICO**) |

Cena de trabalho: `Assets/_Game/Scenes/Buteco.unity` (greybox do bar). `Assets/Scenes/SampleScene.unity` e `Assets/Welcome` são do template — não usar.

---

## 2. Visão do jogo

- **Título:** Buteco dos Devs: A Guerra Púnica
- **Gênero:** 2D top-down action / beat'em-up leve
- **Duração:** ~10–20 min
- **Protagonista:** DEV NOVATO — membro recém-chegado ao Buteco. O jogador aprende o mundo e a história junto com o personagem, porque acabou de chegar na comunidade.
- **Tom:** conflito fictício e cartunizado. **Sem gore.** Derrota = knockout cartunizado.

### História (fluxo)
1. Dev Novato entra no Buteco dos Devs.
2. Explora o bar.
3. Conhece os personagens.
4. Pedro vai ao bar rival divulgar/convidar pessoas para o Buteco.
5. Pedro volta indignado: foi acusado de divulgar um link com vírus.
6. A comunidade decide ajudar Pedro.
7. Fase de preparação.
8. Jogador escolhe uma arma.
9. Aliados se equipam.
10. Pedro chama todos.
11. Todos saem do Buteco.
12. Cutscene curta atravessando a rua.
13. Título: **A GUERRA PÚNICA**
14. Batalha na rua.
15. Entrada no bar rival.
16. Batalha final.
17. Retorno ao Buteco.
18. Epílogo.
19. Buteco volta ao modo sandbox/exploração.

### Temas da jam
| Tema | Como aparece |
|---|---|
| Independência | identidade do Buteco |
| Revolução | a comunidade se organiza |
| Sacrifício | aliados podem cair / segurar inimigos |
| Vida/Morte | apenas knockouts cartunizados |
| Coragem | sistema simples de moral/coragem |

### Desafio de áudio
- **Todos os sons são gravados pela equipe/Buteco dos Devs. NÃO gerar sons por IA.**
- O áudio é parte do gameplay: o Buteco tem uma **soundboard** (RUA!!!, aplausos, vaias, risadas, burp, copos, garrafas, cadeiras, passos, ataques, espadas, gritos).
- A soundboard pode influenciar **levemente** moral/caos.
- Até os sons reais chegarem, usar silêncio ou placeholders claramente marcados (`PH_` no nome).

---

## 3. Personagens

- **DEV NOVATO** — controlado pelo jogador.
- **PEDRO** — veterano. Habilidade **TIMEOUT**: o NPC alvo sai do combate por **60 s** e depois retorna por um **waypoint seguro**.
- **BARTENDER** — visual do **Moe** (decisão do usuário, ver seção Arte). Serve cerveja, pode expulsar personagens, grita **"RUA!!!"**.
- **COMUNIDADE** — personagens caricatos; humor da cultura da comunidade. **Não transformar características pessoais sensíveis em mecânicas.**
- **RIVAIS** — estética exagerada neon/RGB/cyberpunk, computadores, monitores, Linux/root como piada visual, controle de videogame visível. O controle pode virar minigame **somente se sobrar tempo**.

---

## 4. Gameplay — valores iniciais (ajustar por playtest)

**Player:** movimento top-down (WASD + setas), diagonal normalizada, colisão 2D, não atravessa paredes, não sai da área jogável, dash não atravessa paredes, câmera segue o jogador e fica limitada ao mapa.

| Parâmetro | Valor inicial |
|---|---|
| HP do player | 100 |
| Dano inimigo | 10–15 |
| Dano do player | 25–35 |
| I-frames | 0.25–0.5 s |
| Hit-stop | 0.04–0.08 s |
| Dash cooldown | ~0.8 s |
| Inimigo comum | 50–80 HP |
| Inimigo forte | 120–160 HP |
| Boss | 300–450 HP |

Dificuldade cresce por **variedade, posicionamento, quantidade controlada e ataques telegrafados** — não só inflando HP.

### Combate
- Separar **Hitbox** (causa dano) e **Hurtbox** (recebe dano).
- Ataques têm **janela ativa**. Um ataque **não acerta o mesmo alvo mais de uma vez** na mesma janela.
- Usar: dano, knockback, i-frames, hit-stop, feedback visual (flash/shake).
- Player com 0 HP → estado **DOWN/KO** → reinício pelo **checkpoint**. Sem morte instantânea injusta.

### Controles atuais
| Ação | Teclado | Gamepad |
|---|---|---|
| Mover | WASD / setas | Left stick |
| Dash | Espaço / Shift esq. | — |
| Atacar | J / mouse esquerdo | Botão West (X/□) |

### Convenções de combate (Fase 2)
- `Health` (HP, i-frames, eventos `Damaged`/`Died`) + `Hurtbox` (collider trigger filho, com `Team`) + `Hitbox` (`OverlapBox` só na janela ativa, `HashSet` por janela).
- `Team`: Player, Ally, Enemy, Neutral — Hitbox nunca acerta o próprio time.
- Player é movido por posição: `PlayerMovement` zera `linearVelocity` todo FixedUpdate; empurrões externos usam `ApplyKnockback` (nunca setar velocidade direto no player).
- `HitStop` é a única classe estática com runner (sempre restaura `timeScale`).

### NPCs
- State machine simples: `Idle, Wander, Talk, Prepare, Follow, Combat, Flee, Timeout, Knocked`.
- Movimento por **waypoints**. Sem pathfinding complexo sem necessidade real.
- Máximo ~**4–6 aliados** ativos em combate.

### Preparação (antes da Guerra Púnica)
- Objetivo sempre visível na tela.
- Escolha de **1 arma** entre 3–4: espada balanceada · espada grande lenta e forte · espada curta rápida · objeto absurdo de bar (ex.: garrafa).
- **Sem inventário complexo.**
- Aliados equipam armas → Pedro reúne todos → mensagem **"Bora."** → cutscene Buteco → grupo → rua → rival → título **A GUERRA PÚNICA** → devolve o controle.

---

## 5. Anti-soft-lock (checklist obrigatório)

- [ ] Spawn seguro (nada nasce dentro de collider).
- [ ] Nenhum NPC dentro de parede.
- [ ] Nenhuma porta bloqueada permanentemente.
- [ ] Sempre existe caminho de saída.
- [ ] Objetivos sempre podem ser concluídos.
- [ ] NPCs/aliados não bloqueiam progresso nem prendem o jogador.
- [ ] Coroutines/esperas têm **timeout** — nada espera indefinidamente.
- [ ] Transições de cena/cutscene têm **fallback**.
- [ ] Dash não atravessa paredes (usar cast antes de mover).
- [ ] Câmera não sai dos limites.
- [ ] Player nunca fica preso permanentemente.

---

## 6. Arte

- Pixel art caricatural, estilo consistente entre personagens.
- Personagens importantes: idle, walk, attack, hit, knockout; idealmente 4 direções quando necessário.
- Arte final vem depois (ferramentas de arte/assets). **Nunca bloquear gameplay esperando arte — usar placeholders** (quadrados coloridos / sprites simples).

### Referências visuais (conceitos gerados no ChatGPT — direção, não spec)

**Referência PRINCIPAL — "Buteco estilo bar de desenho" (preferida pelo usuário):**
- Interior top-down 3/4: balcão em L com banquetas vermelhas, prateleira de garrafas, mesas redondas, sinuca, jukebox, dardos, sofá roxo, porta dos fundos.
- UI: prompt de interação **`[E] Conversar`** sobre o NPC; caixa **OBJETIVO** no canto superior direito; logo "Buteco dos Devs — A Guerra Púnica" no canto inferior esquerdo.
- Sprite sheets por personagem: **4 direções (Baixo/Cima/Esquerda/Direita) + Idle + Walk (4 frames) + Attack**; bartender também `Interact` e `Serve`; Pedro também `Timeout` com silhueta pontilhada de **60s** (fantasma enquanto fora do combate).
- **Pedro (Java)**: barba, óculos, camiseta preta. **Player (Dev)**: cabelo bagunçado, óculos, camiseta `</>`.
- Rua: os **dois bares lado a lado** na mesma calçada; bar rival = **"RIVAL BAR"** neon azul, "HACK THE PLANET", caveiras/monitores. Tagline: *"...dois bares. Uma comunidade."*
- Inimigos: **Hacker Rival** (capuz, óculos RGB) e **Admin Rival (Boss)** (cabelo branco, óculos escuros, jaqueta).
- Tileset do bar: balcão, banqueta, mesa, cadeira, sofá, jukebox, dardos, espelho, garrafas, caneca, sinuca, porta, vitral, flâmula.

**Referência secundária — "cozy pixel RPG":** fachada noturna com neon laranja "BUTECO DOS DEVS", neon "GOOD CODE GREAT BEER", retratos grandes nos diálogos, HP em corações, hotbar com as 4 armas, boss **"General Buggius"**, tagline *"Código, cerveja e glória"*.

**Fora do escopo da jam** (aparece nos conceitos, NÃO fazer sem aprovação): cidade explorável aberta, inventário em grade com atributos (+Carisma etc.), menu de opções elaborado.

### Bartender — DECISÃO DO USUÁRIO (2026-09-19)
- **Visual do bartender = Moe (Os Simpsons)**, como na referência principal. Decisão tomada pelo usuário ciente de que o repo é público e o jogo vai para o itch.io (risco de IP aceito). Não reabrir o assunto.
- **Tamanhos (decidido):** personagens em canvas **48×48** (personagem ~30 px de altura); tiles e props em grade de **32 px**; **PPU 32** em tudo (1 unidade = 1 tile). Filter Point, sem compressão, pivot nos pés.
- **Geração de arte:** `pixellab-cli` (PixelLab, v0.3.1) instalado via `uv tool`. Token só em `~/.pixellab.json` (fora do repo). Saídas brutas em `pixellab-out/` (gitignored) → copiar só os PNGs aprovados para `Assets/_Game/Art/`. Sempre `--dry-run` antes de gerar; gerar 1 personagem, aprovar estilo com o usuário, depois o resto.

### Plano de arte / orçamento PixelLab (decidido pelo usuário)
- Conta grátis: 40 gerações. Custo real observado: personagem 4 direções = 1; animação = ~1 por direção (4 frames). Oeste = leste espelhado (flipX) → nunca gerar oeste.
- **Únicos (poucos):** Dev Novato ✅ (idle + walk, character_id `1f84a9e7-94fe-4d83-aa7d-2d4d230ef1a8`), Pedro, Moe (bartender), **Rei Luiz** ✅ rotações (character_id `018aee4a-eeef-4bae-9872-b1b91090541c`, coroa dourada; papel a definir) e talvez 1 outro da comunidade.
- **Comunidade genérica = "Membro do Buteco":** 1 base com a **camiseta do Buteco** (laranja com logo da caneca, mesma cor do letreiro), variações por **tint/troca de cor em código** (cabelo, pele, camiseta). Leitura em combate: **laranja = Buteco, neon = rival**.
- **Rivais:** UM "maluco do outro lado" repetido (variação só de cor) + **Admin Rival** (boss) único.
- Ataque/impacto: efeito em código (arco de golpe, avanço, shake) — não gerar frames de ataque.
- NPCs que não lutam: só rotações (1 geração); "respirar"/balanço em código.
- Referências do usuário vão em `refs/` (gitignored).

### Personagens da comunidade (exemplos do conceito)
Comunista, Cristão, Satanista, Artista/Dev, Dev Cansado. São **cosméticos/humor** entre membros que topam a piada — identidade política ou religiosa **nunca** vira mecânica (sem bônus, dano, facção ou alvo por crença).

---

### Perspectiva — DECISÃO: top-down 3/4 (estilo Stardew/Pokémon, NÃO isométrico/Habbo)
- Câmera ortográfica reta (a que já existe). A profundidade vem da arte + ordenação.
- **Y-sort ligado**: `Assets/Settings/Renderer2D.asset` → Transparency Sort Mode = Custom Axis (0,1,0). Quem está mais embaixo na tela é desenhado na frente.
- Sprites de personagem/prop: **pivot nos pés** (Bottom Center) e `SpriteRenderer.spriteSortPoint = Pivot`; mesma sorting layer para personagens e props que se sobrepõem.
- Colliders de props (mesa, balcão, parede) **só na base/pés**, para o personagem poder passar "atrás" da parte de cima.

## 7. Arquitetura Unity

**Priorizar:** componentes pequenos · referências explícitas via `[SerializeField]` · ScriptableObjects só quando úteis (ex.: dados de arma) · state machines pequenas (enum + switch) · prefabs reutilizáveis · cenas pequenas · código fácil de debugar.

**Evitar:** arquitetura enterprise · sistemas genéricos · dependências/pacotes externos · abstrações prematuras · singletons globais sem necessidade · `GameObject.Find` / `FindObjectOfType` / `FindAnyObjectByType` em `Update` ou loops.

**Não instalar pacotes** sem necessidade real e aprovação do usuário.

### Estrutura de pastas
```
Assets/_Game/
  Scripts/{Player,Combat,NPC,UI,Systems}/
  Scenes/
  Prefabs/
  Art/{Characters,Environment,UI}/
  Audio/{SFX,Music}/
  UI/
```
Não criar pastas além dessas sem motivo. Não mexer em `Assets/Settings` (URP) nem em `Assets/Welcome` sem motivo.

---

## 8. Processo de desenvolvimento

```
PLANEJAR → IMPLEMENTAR PEQUENA FEATURE → COMPILAR → ABRIR/TESTAR NO UNITY
→ VERIFICAR CONSOLE → CORRIGIR → PLAYTEST → VALIDAR → só então avançar
```
Nunca implementar vários sistemas ao mesmo tempo.

### Verificação via Unity MCP (padrão)
1. `editor_status` → `compiling: false`.
2. `console_status` → `groundTruth.compilationFailed: false` e sem erros novos.
3. `editor_play` → testar → `console` (level `error`) → `editor_stop`.
4. `capture_game_view` quando o resultado for visual.

Observação: o Editor só avança frames em Play Mode com a janela **focada** — chame `editor_focus` antes de esperar frames (`wait_for` em `Time.frameCount`).

Observação: chamadas `eval` que disparam compilação estouram o timeout de 5 s do MCP e deixam um log "Main thread operation timed out" — é inofensivo, basta aguardar o reload.

### Definition of Done
Uma feature só está pronta quando: compila · a cena abre · Play Mode funciona · Console sem erros · feature testada · comportamento principal funciona · sem soft-lock · não quebra o que já existia · arquivos organizados · escopo documentado aqui.

---

## 9. Fases

| Fase | Conteúdo | Status |
|---|---|---|
| 0 | Preparação, documentação, agentes | ✅ |
| 1 | Player + movimento + colisão + câmera | ✅ `PlayerMovement`, `CameraFollow2D`, greybox `Buteco.unity` |
| 2 | Combate + HP + hitbox/hurtbox + knockback | ✅ `Scripts/Combat/*`, `PlayerAttack`, `PlayerKO`, bonecos de treino |
| 3 | Inimigo simples | ✅ `EnemyController` (Idle/Chase/Windup/Attack/Recover/Hurt/KO), `Enemy_HackerRival.prefab` |
| 4 | NPCs + aliados | ⏳ aguardando ordem |
| 5 | Buteco + interação + soundboard | |
| 6 | Pedro + TIMEOUT (60 s + retorno por waypoint) | |
| 7 | Preparação + escolha de arma + equipar aliados | |
| 8 | Transição + Guerra Púnica + batalha de rua | |
| 9 | Bar rival + inimigos finais + boss + epílogo | |
| 10 | Arte final + áudio + polimento + builds | |

---

## 10. Agentes e delegação

Agentes ficam em `.claude/agents/`. O **agente principal** é o lead: planeja, integra e responde pelo resultado final.

| Agente | Modelo | Effort | Quando usar |
|---|---|---|---|
| `game-architect` | opus | high | arquitetura, escopo, integração difícil, riscos, bug difícil, problema MCP complexo |
| `unity-gameplay` | sonnet | medium | C#, player, combate, HP, inimigos, NPCs, interação, UI de gameplay, prefabs |
| `qa-playtest` | sonnet | medium | testar, reproduzir bug, Console, Play Mode, soft-lock, regressão (**não cria features**) |
| `art-integration` | sonnet | low | import PNG, slicing, pivots, Pixel Perfect, SpriteAtlas, Animator, sorting, Tilemap |
| `build-release` | sonnet | low | Build Settings, builds Win/Mac/Linux, erros de build, pacote itch.io |

### Política de modelo/custo
- **Opus** (`game-architect`): só decisões que podem causar retrabalho grande. Nunca para renomear, importar, ajustes pequenos.
- **Sonnet**: implementação, integração Unity, QA.
- **Haiku**: tarefas mecânicas (organizar, renomear, checagens simples) — passar `model: "haiku"` **na chamada** do Agent tool em vez de criar outro agente.
- Agentes em `.claude/agents/` só são carregados **no início da sessão**. Se criar/editar um agente, reinicie o Claude Code (ou, na sessão atual, use `general-purpose` com `model` e mande-o ler o `.md` do papel).
- `effort` é fixo por agente no frontmatter; a chamada do Agent tool só permite sobrescrever o **modelo**. Para mudar effort de um agente, editar o arquivo dele.

### Regras de delegação
- Delegar só quando a tarefa é independente, há ganho real de especialização e o resultado pode ser validado separadamente.
- **Nunca** dois agentes no mesmo script ou na mesma cena ao mesmo tempo.
- Não delegar tarefas de 30 segundos; não duplicar investigação.
- Todo brief de subagente deve dizer: objetivo, arquivos permitidos, o que já foi descartado, critério de pronto.
- O lead valida (Console + Play Mode) antes de aceitar o trabalho de um agente.

---

## 11. Git e segurança do projeto

- **Repo público** — nunca commitar segredos, tokens, `.env`, dados pessoais, nem arquivos de terceiros sem licença.
- **Sem commit ou push automático** — só com autorização explícita do usuário.
- Antes de mudanças grandes: listar os arquivos afetados.
- Não apagar arquivos existentes nem sobrescrever assets sem necessidade.
- Não alterar `ProjectSettings/` ou `Packages/` sem motivo explicado antes.
- Não instalar dependências automaticamente.
- Se uma mudança puder quebrar o projeto, explicar antes de fazer.
