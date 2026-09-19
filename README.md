# 🍺 Buteco dos Devs: A Guerra Púnica

> *Dois bares. Uma rua. Uma comunidade.*

Um beat'em-up top-down em pixel art sobre a comunidade **Buteco dos Devs**, feito em **48 horas** para uma **Game Jam**.

Você é o **Dev Novato**, recém-chegado no Buteco. Mal deu tempo de pedir a primeira cerveja e o Pedro volta do bar rival indignado: acusaram ele de espalhar um link com vírus. Aí a comunidade se organiza. Cada um escolhe uma arma, atravessa a rua e começa **A Guerra Púnica**.

> 🚧 **Em desenvolvimento durante a jam.** Este README acompanha o progresso.

---

## 🎮 Sobre o jogo

| | |
|---|---|
| **Gênero** | Ação top-down / beat'em-up leve |
| **Duração** | ~10–20 minutos |
| **Plataformas** | Windows · macOS · Linux |
| **Engine** | Unity 6 (URP 2D) |
| **Controles** | WASD / setas para mover (combate em definição) |

### O que te espera
- 🍻 **O Buteco**: explore o bar, conheça a galera e aperte os botões da **soundboard**.
- ⚔️ **Escolha sua arma**: espada equilibrada, espada grande, espada curta ou… uma garrafa.
- ⏱️ **TIMEOUT do Pedro**: o veterano tira um inimigo de combate por 60 segundos.
- 🗣️ **"RUA!!!"**: o bartender resolve as coisas do jeito dele.
- 🌃 **O bar rival**: neon, RGB, monitores e muito `root`.
- 👾 **Batalha final** contra o chefão do bar rival.

Tudo é cartunizado: ninguém morre, todo mundo só leva um *knockout*.

---

## 🎙️ Desafio de áudio

**Todo som do jogo foi gravado pela própria galera do Buteco dos Devs**: gritos, risadas, vaias, arrotos, copos, garrafas, cadeiras e o lendário **"RUA!!!"**. Nada de som gerado por IA.

O áudio também é mecânica: a **soundboard** do Buteco mexe com a moral (e com o caos) do bar.

---

## 🏆 Temas da jam

| Tema | No jogo |
|---|---|
| **Independência** | a identidade do Buteco |
| **Revolução** | a comunidade se organiza |
| **Sacrifício** | aliados seguram os inimigos para você passar |
| **Vida / Morte** | só knockouts cartunizados |
| **Coragem** | sistema de moral da galera |

---

## 🛠️ Rodando o projeto

1. Instale o **Unity 6000.6.2f1** pelo Unity Hub.
2. Clone o repositório:
   ```bash
   git clone https://github.com/juliomachadome/butecogame.git
   ```
3. No Unity Hub: **Add → Add project from disk** → selecione a pasta clonada.
4. Abra a cena principal em `Assets/_Game/Scenes/` e aperte ▶️.

### Estrutura
```
Assets/_Game/
  Scripts/   Player · Combat · NPC · UI · Systems
  Scenes/    cenas do jogo
  Prefabs/   personagens, props, armas
  Art/       Characters · Environment · UI
  Audio/     SFX · Music (gravados pela comunidade)
```

---

## 🗺️ Roadmap da jam

- [x] Fase 0: setup, documentação e pipeline
- [ ] Fase 1: player, movimento, colisão e câmera
- [ ] Fase 2: combate (hitbox/hurtbox, HP, knockback)
- [ ] Fase 3: primeiro inimigo
- [ ] Fase 4: NPCs e aliados
- [ ] Fase 5: o Buteco, interação e soundboard
- [ ] Fase 6: Pedro e o TIMEOUT
- [ ] Fase 7: preparação e escolha de armas
- [ ] Fase 8: a travessia e a batalha de rua
- [ ] Fase 9: bar rival, chefão e epílogo
- [ ] Fase 10: arte final, áudio, polimento e builds

---

## 👥 Créditos

Feito com 🍺 e muito `git push --force` (brincadeira) pela comunidade **Buteco dos Devs**.

- **Game design / dev:** Julio Machado e galera do Buteco
- **Vozes e efeitos sonoros:** a comunidade do Buteco dos Devs
- **Desenvolvimento assistido por:** [Claude Code](https://claude.com/claude-code)

*Qualquer semelhança com bares, servidores ou admins reais é mera coincidência. Ou não.*
