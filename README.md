# The traitor's lullaby

An action-packed 2D hack-and-slash platformer built with **Unity 2D**. Take control of **Kairi**, a skilled warrior utilizing specialized Amber elemental abilities to take down a formidable, multi-staged dark entity. Deflect, strike, and trigger chain reactions to survive the onslaught.

---

## 🎮 Gameplay Features

* **Fluid Combat Mechanics:** Engage in fast-paced combo strings, dashes, and heavy attacks to break through enemy defenses.
* **Amber Explosion System:** Plant elemental resonance on targets and trigger massive explosive damage (`AmberExplosion`) to push the boss into its next phase.
* **Advanced 2D Dynamic Boss AI:** Face an intelligent boss character (`BossAI`) that adapts to the player's positioning, distances, and remaining health pools across multiple tactical stages.

---

## ⚔️ Boss AI Behavior & Stages

The core encounter challenges players through **two distinct, high-intensity phases**:

### Phase 1: The Pull of Darkness

* **Behavior:** The boss keeps its distance while constantly channeling a gravity vortex (`HandlePhase1Pull`), dragging Kairi toward hazardous zones.
* **Combat:** It initiates standard slash combinations if Kairi steps within its close-quarter detection sphere (`attackRadius`).
* **Transition:** Upon dropping below **400 HP**, the vortex dissipates, pushing the encounter into the second phase.

### Phase 2: Shadow Clones & Airborne Assault

* **Behavior:** The boss activates "Phân Thân Chi Thuật," spawning active shadow duplicates (`allClones`) across the arena to confuse the player.
* **Airborne Assault (`Boss_jump`):** If Kairi attempts to stay out of range, the boss breaks its grounded state and launches a powerful forward leap attack (`ExecuteJump`) to close the gap instantly.
* **Stun Lock Protection:** Includes an absolute freeze loop state (`BossState.GetAttack`) with dynamic frame interruption, allowing the boss to visually react to Kairi's explosions without getting locked infinitely by light spam.

---

## 🛠️ Technical Implementation Details

* **State Machine Architecture:** The AI runs on an optimized State Machine (`Idle`, `Chasing`, `Attacking`, `Jumping`, `GetAttack`) handling transitions seamlessly via physics parameters.
* **Animator Interruption Overhaul:** Leverages Unity's `Any State` architecture coupled with precise `Interruption Source` overrides and frame duration blending ($0.1s$) to eliminate animation canceling/flickering bugs.
* **Physics-Grounded Logic:** Uses a 2D circle overlap (`Physics2D.OverlapCircle`) attached to a customized `GroundCheck` transform array, preventing the AI from miscalculating jump states or walking mid-air.

---

## ⌨️ Controls

| Action | Input / Control |
| --- | --- |
| **Move Left / Right** | `A` / `D` or `Arrow Keys` |
| **Jump** | `W` or `Up Arrow` |
| **Dash / Dodge** | `Left Shift` |
| **Attack Combo** | `Left Mouse Click` |
| **Trigger Amber Detonation** | `E` |

---

## 🚀 Getting Started & Installation

### Prerequisites

* **Unity Editor:** Version `2022.3 LTS` or higher recommended.
* **Physics Engine:** Unity 2D Physics module.

### Setup Instructions

1. Clone this repository to your local machine:
```bash
git clone https://github.com/yourusername/kairis-fate.git

```


2. Open **Unity Hub**, click **Add**, and select the project folder.
3. Once loaded, navigate to `Assets/Scenes/` and open `MainLevel.unity`.
4. Click **Play** at the top of the editor to test the project live!

> [!IMPORTANT]
> **Inspector Configuration Note:** Ensure that the `Ground Check` slot under the `BossAI` component on the Boss GameObject is properly linked to the child transform object, and the `Ground Layer` is assigned to your level's specific platform layer (e.g., `Midground`).

---

## 👥 Contributors & Credits

* **Game Programmers:** Developed as part of the Major Assignment/Cap Project framework.
* **Engine & Tools:** Powered by Unity Technologies, C# scripting, and Sprite rigging tools.
