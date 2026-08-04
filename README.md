# Stickman Rollerblader

**Stickman Rollerblader** is a 2D physics-based arcade skateboarding and rollerblading game built in Unity. It features smooth slope-aligned movement, responsive jump physics, dynamic ramp launching, rail grinding, and a ScriptableObject-driven trick execution system.

---

## Technical Stack & Overview

- **Engine**: Unity 2D
- **Language**: C# (.NET / Mono)
- **Input**: Unity Input System (New Input System)
- **Physics**: 2D Physics Rigidbody2D & Custom Slope Raycasting

---

## Documentation Index

Detailed technical documentation is available in the [`docs/`](./docs) folder:

- 🏗️ **[Architecture Documentation](./docs/Architecture.md)**: High-level overview of project structure, component relations, and data flow.
- 📐 **[Design Patterns](./docs/DesignPatterns.md)**: In-depth guide to design patterns used (FSM, Strategy, Interfaces, Observer).
- 🎮 **[Gameplay Mechanics](./docs/Mechanics.md)**: Technical breakdown of slope alignment, jump/fall physics, ramp boost, camera follow, and trick execution.

---

## Getting Started

1. **Unity Setup**: Open the project folder in Unity (2022.3 LTS or newer recommended).
2. **Main Scene**: Open `Assets/Scenes/SampleScene.unity`.
3. **Controls**:
   - **Move Left / Right**: `A` / `D` or Left / Right Arrow keys.
   - **Jump / Ramp Boost**: `Space` key.
