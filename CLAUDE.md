# Buteco dos Devs: A Guerra Púnica

Documento central do projeto. Game Jam de **48 horas**. Leia antes de qualquer tarefa.

> **Bar com arte real montado** (entrada na parede do fundo, Moe atrás do balcão, Julio e Funnie "sentados" atrás das mesas). **Fase atual: 4c-1 concluída** (aliados Pedro/Rei Luiz/Funnie seguem em formação e lutam; inimigos atacam player ou aliados; cena `Test_Combat`). Próximo: 4c-2 (poder azul/verde do suporte + painel do grupo). ⚠️ Playtest manual pendente: parry completo e regressão ao vivo (diálogo/KO/dash) após crash do Unity. Cena de trabalho: `Assets/_Game/Scenes/Buteco.unity`.
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
**Tom:** começa como **um dia normal** no Buteco — o jogador é um membro novo chegando, explorando e conhecendo a galera (tutorial disfarçado: andar, `[E]` conversar, bater nos bonecos). No meio desse dia comum, a Guerra Púnica estoura: Pedro sai para o bar da frente, volta indignado (acusado de link com vírus) e pede ajuda.

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
- **Lista de sons a gravar (prioridade):** 1) "RUA!!!" (Moe) 2) aplausos/vaias 3) risadas 4) copo/garrafa 5) golpe + "ai" 6) **isqueiro do Pedro ("tsc tsc" falhando + acendendo)** — toca no loop de atividade dele, perto do player (som posicional) 7) dardo de Nerf "pew" 8) caneca batendo na mesa (Julio bebendo).

---

## 3. Personagens

- **DEV NOVATO** — controlado pelo jogador.
- **PEDRO** — veterano ("o mais velho do mundo, 500 anos" — brincadeira) e **moderador do Buteco**: quem fala besteira toma **TIMEOUT** (estilo moderação de Discord). Ironia da história: justo ele é acusado de mandar link com vírus no bar rival. Habilidade **TIMEOUT**: o NPC alvo sai do combate por **60 s** e depois retorna por um **waypoint seguro**.
- **BARTENDER** — visual do **Moe** (decisão do usuário, ver seção Arte). Serve cerveja, pode expulsar personagens, grita **"RUA!!!"**.
- **JULIO (criador do jogo, nick "Naldo")** — NPC: fica **no canto do balcão tomando cerveja com o Moe** (piada do criador dentro do jogo). Na guerra, **Julio + Funnie são SUPORTE na retaguarda — "dão vida" E fazem DEFESA** (curam e protegem; não entram no corpo a corpo). **Visual pedido pelo usuário: eles jogam um "poderzinho" — orbe/projétil AZUL = defesa (escudo temporário no aliado: reduz dano) e VERDE = vida (cura)**, com brilho da cor no aliado atingido. Detalhes por personagem: **Funnie** sobe uma estação de cura no chão ("HealthCheck as a Service", cura em área aos poucos); **Julio** joga uma **cerveja** no aliado com menos HP de tempos em tempos ("a rodada é por conta do criador"). Ambos com ícone de cura no painel do grupo. Sprite: aguardando foto de referência do usuário.
- **Grupo na guerra:** linha de frente = Pedro, Rei Luiz e 1–3 **membros do Buteco genéricos** (camiseta laranja); retaguarda de cura = Julio + Funnie. O protagonista continua sendo o **Dev Novato** genérico (não é o Julio).
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
| Interagir | E | Botão South (A/✕) — Fase 4b |
| Defender | **segurar mouse direito** / K | LB / L1 |
| Dash (gamepad) | — | Botão East (B/○) |

**Defesa (implementada — `PlayerBlock` via `Health.DamageFilter`, `EnemyController.Stagger`):** segurar = guarda (move 50%, não ataca); golpe **frontal** (±90° da direção) leva ~20% do dano e knockback mínimo; pelas costas entra inteiro. **Parry:** iniciar a guarda até ~0.15 s antes do hit → inimigo atordoado (~0.6 s) + dano extra no contra-ataque; +Coragem. Entra junto do HUD, depois da 4b.

### Animação de combate e itens na mão (pedido do usuário) — por CÓDIGO, sem gerar frames
- **Itens na mão = sprites sobrepostos** (filho do personagem, posição/ordem por direção; W espelha E): arma na mão principal (Fase 7), **cigarro sempre na mão do Pedro** (branco + ponta laranja brilhando, fumacinha periódica) — **inclusive na batalha: espada numa mão, cigarro na outra**.
- **Golpe:** arco/rastro curvo da arma (sprite de "slash" por arma) + avanço curto (lunge) + squash & stretch no personagem + faíscas no impacto + screen shake leve em golpes fortes. Vale para player, aliados e rivais.
- **Defesa:** escudinho azul na frente (substitui o quadrado placeholder).
- Isqueiro do Pedro com som gravado pela equipe (ver Desafio de áudio).

### Convenções de combate (Fase 2)
- `Health` (HP, i-frames, eventos `Damaged`/`Died`) + `Hurtbox` (collider trigger filho, com `Team`) + `Hitbox` (`OverlapBox` só na janela ativa, `HashSet` por janela).
- `Team`: Player, Ally, Enemy, Neutral — Hitbox nunca acerta o próprio time.
- Player é movido por posição: `PlayerMovement` zera `linearVelocity` todo FixedUpdate; empurrões externos usam `ApplyKnockback` (nunca setar velocidade direto no player).
- `HitStop` e `CombatantRegistry` são as únicas classes estáticas (exceções documentadas). Todo combatente precisa do componente `Combatant` (registra o `Health` por `Team`) — sem ele, aliados/inimigos não se enxergam.
- ⚠️ Observar em playtest: `timeScale` ficou preso em 0 uma vez no teste com vários hits simultâneos (possível artefato do teste via eval). `HitStop.ForceReset()` destrava.
- Uma classe MonoBehaviour por arquivo, com o **mesmo nome do arquivo** (senão "missing script" no prefab).

### Sandbox do Buteco (Fase 5 e pós-epílogo) — pedido do usuário
O jogador pode **mexer em tudo** no bar, sempre pelo mesmo `[E]`/`Interactable`:
- **Soundboard** (painel com botões): RUA!!!, aplausos, vaias, risadas, burp, gritos — sons gravados pela comunidade; influencia levemente moral/caos.
- **Jukebox** (troca música), **chopeira/balcão** (Moe serve cerveja, +Coragem leve), **cadeiras/banquetas** (sentar), **dardos/sinuca** (um lance + som, sem minigame), **copos/garrafas** (bater = som; bater demais → Moe grita "RUA!!!"), bonecos de treino.
- Sem minigames completos (escopo). Cada item = objeto + som + fala curta.

### Clima de chegada: bagunça no Buteco (pedido do usuário)
- Quando o jogador entra, o bar já está **uma bagunça**: 2–3 membros genéricos numa **guerra de Nerf** (dardos de espuma; quem leva faz "ai!", pulinho cômico e revida — sem HP, sem dano, cartunizado). Resto da galera nas atividades (Pedro isqueiro/baseado, Funnie notebook, Rei Luiz cerveja, Julio no balcão com Moe).
- **Tutorial disfarçado de dash:** dardos perdidos vêm na direção do player (0 dano, só "pew"); desviar com dash → galera comemora ("boa, novato!") + Coragem leve.
- **Nível de caos = "startup/hacker house" (referência do usuário: cena de filme sobre o Spotify em que alguém entra e o escritório é uma bagunça):** dardos cruzando a sala, gente escondida atrás de mesa/balcão/sofá, alguém de **patinete/skate** cortando o bar (NPC em rota fixa), **música alta** da jukebox com leve shake no grave, chão com caixas de pizza/cabos/latinhas/notebook, gritos em balões ("DEPLOY NA SEXTA!", "QUEM MEXEU NA MAIN?", "É FEATURE!").
- **O caos PARA de uma vez** quando o Pedro grita "OPA!" e vai até o jogador com o "Tem 18? Mostra o alistamento" (o contraste é a piada); depois a bagunça volta.

### Piada recorrente: "Tem 18? Mostra o alistamento" (pedido do usuário)
- **Intro do jogo:** Dev Novato entra pela porta → Pedro vem até ele: "Opa, opa. Quem é você?" / "Tem 18? Mostra o alistamento." / Dev: "...sério?" / Pedro: "Regra da casa. Sem alistamento, sem cerveja." → objetivo "Converse com o Pedro" segue daí.
- **Evento do sandbox:** de tempos em tempos um **desconhecido aleatório** (membro genérico) entra; Pedro anda até ele e faz a triagem em **balões de fala** ("Quem é você?", "Tem 18?", "Cadê o alistamento?"). Resposta aleatória ("perdi", "tá no gov.br", "tenho 17 e meio"...). Pedro libera **ou** dá **TIMEOUT** → Moe "RUA!!!" → sai voando (mesmo efeito da Fase 6).
- Implementação: balões de fala world-space (mais leves que o DialogueUI) + Pedro andando por waypoint até a porta; timeout/fallback se alguém travar no caminho.

### HUD (pedido do usuário)
- **Sup. esquerdo:** retrato do Dev + **barra de HP** + **barra de Coragem** (tema; sobe ao acertar/soundboard, cai ao apanhar).
- **Inferior:** ações com ícone da tecla (Kenney Input Prompts): **[J] Atacar**, **[Espaço] Dash** com cooldown radial, **[E] Interagir**; slot da arma escolhida (Fase 7).
- **Lateral esquerda (combate):** painel do grupo — retratos dos aliados com HP; indicador de **TIMEOUT pronto** do Pedro; retrato cinza quando o aliado cai. O player luta **junto** com os aliados.
- **Sup. direito:** caixa OBJETIVO.
- Feedback: números de dano flutuando, flash vermelho na borda ao tomar dano.
- Ordem: 4b (NPCs/diálogo/objetivo) → HUD do player → 4c (aliados + painel do grupo).

### NPCs
- State machine simples: `Idle, Wander, Talk, Prepare, Follow, Combat, Flee, Timeout, Knocked`.
- Movimento por **waypoints**. Sem pathfinding complexo sem necessidade real.
- Máximo ~**4–6 aliados** ativos em combate.

### Preparação (antes da Guerra Púnica)
- Objetivo sempre visível na tela.
- Escolha de **1 arma** entre 3–4: espada balanceada · espada grande lenta e forte · espada curta rápida · objeto absurdo de bar (ex.: garrafa).
- **Sem inventário complexo.**
- Aliados equipam armas → Pedro reúne todos → mensagem **"Bora."** → cutscene Buteco → grupo → rua → rival → título **A GUERRA PÚNICA** → devolve o controle.

### Mapa das próximas áreas (proposta aprovada em conversa, 2026-09-19)
- **Arsenal (Fase 7) — sem sala nova:** Moe abre um arsenal escondido debaixo do balcão; as 4 armas aparecem na **mesa de sinuca** (vira "mesa de armas"): espada equilibrada, espadona lenta, espada curta rápida, **garrafa**. `[E]` escolhe. Aliados pegam as deles → Pedro: "Bora." → saem pela porta.
- **Rua (Fase 8) — cena `Rua`:** os **dois bares lado a lado** na mesma calçada (Buteco à esquerda, letreiro laranja; RIVAL BAR à direita, neon azul, "HACK THE PLANET"), faixa de pedestre, postes, noite. Cutscene de travessia → título **A GUERRA PÚNICA** → batalha na rua com ondas saindo do bar rival.
- **Bar rival (Fase 9) — cena `BarRival`:** "anti-Buteco" com a mesma planta (porta embaixo, balcão no canto), estética cyberpunk/gamer: PCs, monitores, rack de servidores, cadeiras gamer, LED RGB, neon de caveira, piadas Linux/root, controle de videogame gigante (minigame só se sobrar tempo). Bartender = Moe jovem descartado com neon. Rivais = genéricos com tint neon (custo 0). **Admin Rival** num "trono de servidores" com monitor gigante → luta final.
- **Orçamento (6 gerações):** Admin Rival 1 · folha props do bar rival 1 · folha da rua 1 · paredes/piso rival = Buteco tingido (0) · rivais = tint (0) · **reserva 3**.

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
- **Genéricos (feitos por RECOLORAÇÃO da folha do Dev, 0 gerações, já com idle + walk 4 direções — mesmo layout 76x76 5x4 do `DevNovato_Sheet`):**
  - **Membro do Buteco** = camiseta **AMARELA**, calça marrom, cabelo preto → `Characters/Generic/Generic_Buteco_Sheet.png`.
  - **Rival** = **todos iguais**, camiseta **VERDE** + cabelo verde ("hacker punk"), pele mais escura, calça preta → `Characters/Generic/Generic_Rival_Sheet.png`.
  - Leitura em combate: **amarelo = Buteco, verde = rival**. Variação extra por tint em código se precisar.
- **Rivais:** UM "maluco do outro lado" repetido (variação só de cor) + **Admin Rival** (boss) único.
- Ataque/impacto: efeito em código (arco de golpe, avanço, shake) — não gerar frames de ataque.
- NPCs que não lutam: só rotações (1 geração); "respirar"/balanço em código.
- Referências do usuário vão em `refs/` (gitignored).

### Elenco gerado (PixelLab, 48px, low top-down, 4 direções)
| Personagem | character_id | Visual | Piada/traço |
|---|---|---|---|
| Dev Novato | `1f84a9e7-94fe-4d83-aa7d-2d4d230ef1a8` | cabelo preto, óculos, camiseta `</>` | protagonista (idle+walk prontos) |
| Pedro (PedroPietro) | `89aa4338-9bd7-4c84-bf22-c1f8c539e161` | óculos escuros, barba, camiseta com xícara (Java) | moderador, TIMEOUT 60 s; **"o mais velho do mundo, 500 anos"** (brincadeira) |
| Rei Luiz | `018aee4a-eeef-4bae-9872-b1b91090541c` | coroa dourada, bigode/cavanhaque | **faz jogos e é o organizador da game jam** |
| Moe (bartender) | `67341daf-ebff-4319-9c54-95e611ab0fc4` | avental azul, camisa branca, cara fechada | "RUA!!!" — **ajustar depois**: saiu jovem, falta cabelo grisalho e pano no ombro |
| Funnie | `a7e72f73-35dc-4e0d-9694-03ead8a19ad1` | gordinho, cabelão, óculos, barba, moletom verde, notebook prateado | "coda muito, faz um SaaS em 1 segundo" |
**Limite do plano grátis:** a criação de PERSONAGEM (`character new`) está bloqueada (402 "Insufficient resources") mesmo com gerações sobrando e com 0 personagens salvos — o grátis parece limitar o nº de criações de personagem. **Todos os personagens foram apagados do servidor em 2026-09-19** (os sprites/walks usados no jogo estão no projeto). `pixellab-cli sprite` (imagem avulsa, 1 geração) e `rotate` (8 direções a partir de uma imagem, ~3) continuam funcionando.
- **Moe dos Simpsons (novo, decisão do usuário):** gerado via `sprite` sem referência de estilo → `pixellab-out/2026-09-19T1532-full-body-pixel-art-character-bartender-with-yel/` (pele amarela, cabelo grisalho p/ trás, sobrancelhona, camisa branca, avental azul). ⚠️ `--style` com o Dev contamina roupa/cabelo — não usar para personagens diferentes.
- Novas animações dos 4 principais exigem recriar a partir do sprite local (`character new --reference`, bloqueado no grátis) → alternativa: animação em código ou plano pago.
**Descartados como personagem único → reaproveitados como NPCs ALEATÓRIOS** (só rotações locais em `pixellab-out/`, sem walk gerada; animação em código). Apagados do servidor para liberar espaço:
- Funnie magro `cb44a0ac-…` → membro aleatório do Buteco (Nerf, "Tem 18?").
- Funnie notebook marrom `9bbc8d89-…` → dev aleatório, ou rival com tint neon.
- Moe jovem `67341daf-…` → bartender do bar rival.
- Moe duplicado `3d5f91b3-…` (backup em `pixellab-out/backup_moe_dup/`) → cliente aleatório.
- Rei Luiz prateado `e45540e3-…` → só como piada ("Rei impostor" do bar rival), senão não usar (confunde).

**Comportamento no Buteco (pedido do usuário):**
- **Nome aparece sobre o personagem quando o player chega perto** (proximidade), junto do prompt `[E] Conversar`.
- Cada NPC tem uma **atividade idle em loop**, de tempos em tempos: Pedro **acende o isqueiro e fuma um baseado** (piada interna, cartunizado); Funnie **no notebook**; outros **bebendo** no balcão; Moe **servindo/limpando copo**. Animação de atividade = 1 direção só (sul), ~1 geração cada no PixelLab.

### Arte do bar e novos personagens (2026-09-19, dia 2)
- **Truque de orçamento:** `pixellab-cli sprite --size 256x256` custa 1 geração → gerar **folhas de props** e recortar. `sprite` sai com fundo sólido: remover com flood-fill das bordas (Pillow via `uv run --with pillow`).
- `Assets/_Game/Art/Environment/Bar/`: `Bar_Props_Counter.png` (balcão, banquetas, chopeira, prateleiras, caixa, caneca, espelho), `Bar_Props_Fun.png` (jukebox, Love Tester, neon BEER, sinuca, fliperama, máquina de cigarro, cadeiras, mesa, sofá roxo), `Bar_Room_Tiles.png` (parede verde + lambri, portas, janela, piso xadrez vermelho/preto).
- **Moe dos Simpsons** (magro, topete azul-acinzentado, camisa cinza, gravata-borboleta, avental branco, braços cruzados) em `Characters/MoeSimpsons/` (South + East; oeste = East espelhado). Joga gente pra fora ("RUA!!!") → anda pelo bar com balanço em código.
- **Julio** (boné NY preto, puffer branca, correntinha) em `Characters/Julio/` (4 direções via `rotate`).
- Referências do usuário em `refs/` (ex.: `refs/moe_ref.png`). Imagem como referência no PixelLab custa ~30 gerações — não usar.
- **Saldo PixelLab: ~6 gerações** — reservadas para rival genérico + Admin Rival (boss). Não gastar à toa (pedido do usuário).

### Montagem do cenário 3/4 — regras aprendidas (bar do Buteco)
- **Entrada principal fica na PAREDE DO FUNDO** (porta + tapete vermelho em x≈1.1): em 3/4 a parede de baixo não aparece. Player/Checkpoint nascem na frente dela, Pedro ao lado (intercepta).
- **MapBounds (limite da câmera) tem que incluir a parede do fundo** (hoje y -6.4..7.8), senão a câmera corta parede/porta/Moe.
- **"Sentado" sem sprite de sentado:** NPC um pouco ACIMA da mesa (y do NPC > y da mesa → desenhado atrás), com o tampo cobrindo da cintura pra baixo. Mesa `Prop_RoundTable` foi recortada para tirar o encosto de cadeira embutido.
- **Pivot nos pés sempre** — sprites vindos de `sprite`/`rotate` chegam com pivot no CENTRO: corrigir no import (medir a linha mais baixa opaca).
- Escala: props da folha 256px estão em PPU 32; mesas/cadeiras usam escala 0.55–0.6, chopeira 0.5, caixa 0.45, Julio 0.75, Moe 0.82 (os sprites deles ocupam o canvas inteiro).
- Paredes/decoração de parede: sortingOrder -20/-19 (sempre atrás); props e personagens no Y-sort.

### Assets de terceiros (regra do repo público)
- **Só entra no repo asset CC0 / domínio público**, ou licença que permita **redistribuição** (git público = redistribuição). Guardar o `License.txt` junto.
- "Free, redistribution not allowed" (comum no itch.io) **não entra** no repo.
- Em uso: **Kenney Input Prompts Pixel** (teclas/botões 16px) e **Kenney Pixel UI Pack** (painéis 9-slice) — CC0 — em `Assets/_Game/Art/UI/Kenney/`.
- Cenário/props do bar e da rua: gerar no PixelLab (`tiles`, `object`) no mesmo estilo dos personagens.

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

**Armadilha Unity:** nunca usar `?.` ou `??` em `UnityEngine.Object` (GameObject, Component) — não detecta objeto destruído ("fake null") e gera `MissingReferenceException`. Usar `if (x != null) x.Metodo();`.

**TextMeshPro:** TMP Essentials já estão em `Assets/TextMesh Pro/` (importados manualmente — o menu Window → TMP → Import travou o Editor). Não reimportar pelo menu.

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

**Não roubar o foco do usuário:** para avançar o Play Mode, preferir `menu` → `Edit/Play Mode/Step` (quadro a quadro, sem trazer o Unity para frente) ou testar a lógica chamando métodos via `eval`. `editor_focus` só em último caso e o mínimo de vezes — ele joga a janela do Unity na frente do usuário. Nunca usar `open` para mostrar arquivos sem necessidade.

Observação: o Editor só avança frames em Play Mode com a janela **focada** — chame `editor_focus` antes de esperar frames (`wait_for` em `Time.frameCount`).

Observação: prints do MCP em Play Mode (`source=screen`) saem na resolução da aba Game — se estiver pequena, TUDO fica borrado (parece texto quebrado, não é). Para checar UI nítida: fora do Play, trocar temporariamente o Canvas para ScreenSpaceCamera, `capture_game_view` 1920x1080, e voltar para Overlay sem salvar.

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
| 4 | NPCs + aliados | 🟡 4b ✅ NPCs/diálogo/objetivo (`Scripts/NPC`, `DialogueUI`, `ObjectiveUI`, prefabs `NPC_*`) · HUD+defesa ✅ (`PlayerHUD`, `PlayerBlock`, `CourageMeter`, `DamageNumbers`, `DamageVignette`) · 4c-1 ✅ aliados (`AllyController`, `CharacterSpriteAnimator`, `CombatantRegistry`+`Combatant`, prefabs `Ally_*`, cena `Test_Combat`) · 4c-2 ⏳ suporte + painel |
| 5 | Buteco + interação + soundboard | |
| 6 | Pedro + TIMEOUT (60 s + retorno por waypoint) — **efeito: Pedro carimba "TIMEOUT" no alvo → Moe grita "RUA!!!" → alvo sai voando cartunizado pela porta/tela (girando) → silhueta pontilhada com contador 60s no lugar → volta andando pela porta/waypoint seguro**. Fora do Buteco o "RUA!!!" do Moe toca como eco da soundboard. | |
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
