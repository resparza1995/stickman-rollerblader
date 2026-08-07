# Stickman Rollerblader

A retro-style rollerblading game inspired by classic flash stickman animations.

---

## Technical Stack & Overview

- **Engine**: Unity 2D
- **Language**: C# (.NET / Mono)
- **Input**: Unity Input System (New Input System)
- **Physics**: 2D Physics Rigidbody2D, Contact Point Analysis & Halfpipe Launch Mechanics
- **Graphics**: Screen-space Custom Image Effect Shaders (`VintageEffect.shader`)

---

## Documentation Index

Detailed technical documentation is available in the [`docs/`](./docs) folder:

- 🏗️ **[Architecture Documentation](./docs/Architecture.md)**: High-level overview of project structure, component relations, and data flow.
- 📐 **[Design Patterns](./docs/DesignPatterns.md)**: In-depth guide to design patterns used (FSM, Strategy, Interfaces, Observer).
- 🎮 **[Gameplay Mechanics](./docs/Mechanics.md)**: Technical breakdown of slope alignment, halfpipe vertical air, camera follow & bounds, vintage filter, and trick execution.

---

## Getting Started

1. **Unity Setup**: Open the project folder in Unity (2022.3 LTS or newer recommended).
2. **Main Scene**: Open `Assets/Scenes/SampleScene.unity`.
3. **Controls**:
   - **Move Left / Right**: `A` / `D` keys.
   - **Jump / Ramp Boost**: `Space` key.
   - **Air Rotation Tricks**: `Arrow Keys` (Up/Down for flips, Left/Right for spins).

---

## License

This project is licensed under the [MIT License](./LICENSE).
