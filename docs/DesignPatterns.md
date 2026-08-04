# Design Patterns & Practices

This document outlines the primary software design patterns implemented in **Stickman Rollerblader** and how they contribute to code quality and maintainability.

---

## 1. State Pattern (Finite State Machine)

### Purpose
To eliminate massive `switch-case` blocks and boolean flags inside player movement code by encapsulating state-specific logic into dedicated classes.

### Implementation
- **Context**: `PlayerStateMachine`
- **Interface**: `IPlayerState`
- **Concrete States**: `GroundedState`, `AirborneState`, `GrindingState`

### Benefits
- **Single Responsibility Principle (SRP)**: Each state class manages only its own physics and input behaviors.
- **Ease of Expansion**: Adding new player states (e.g., `RagdollState`, `CrouchState`) requires creating a new `IPlayerState` implementation without modifying existing states.

---

## 2. Strategy Pattern + ScriptableObjects (Trick System)

### Purpose
To separate trick definitions, scoring parameters, and animation triggers from C# execution code, fulfilling the **Open/Closed Principle (OCP)**.

### Implementation
- **Abstract Strategy**: `TrickData` (ScriptableObject)
- **Concrete Strategies**: `AirTrickData`, `GrindTrickData`
- **Strategy Executor**: `TrickController`

### Benefits
- Designers can create, balance, and configure unlimited tricks directly inside the Unity Inspector without recompiling C# scripts.

---

## 3. Interface-Based Polymorphism (Obstacle System)

### Purpose
To decouple player physics code from concrete environment game objects.

### Implementation
- **`IRampObstacle`**: Implemented by ramp lip trigger components (`RampBoostZone.cs`).
- **`IRailObstacle`**: Implemented by rail geometry components (`GrindRail.cs`).

### Benefits
- The player controller interacts exclusively with interface methods (`GetLaunchImpulse`, `GetRailDirection`) rather than referencing specific scene objects.

---

## 4. Observer Pattern (Event-Driven Architecture)

### Purpose
To decouple core gameplay physics from secondary systems such as User Interface (UI), Sound Effects (SFX), and Particle Effects (VFX).

### Implementation
- C# `event Action<TrickData>` defined on `TrickController.cs`.
- Game managers and UI scripts subscribe to trick events without the player controller needing direct references to UI text components or Audio Sources.
