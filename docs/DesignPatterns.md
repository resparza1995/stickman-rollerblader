# Design Patterns & Practices

This document outlines the primary software design patterns implemented in **Stickman Rollerblader** and how they contribute to code quality and maintainability.

---

## 1. Consolidated State Management (Option B Architecture)

### Purpose
To avoid over-engineering and runtime allocations from polymorphic state objects in a self-contained game scope. Encapsulates state transitions (`isGrounded`, `isOnSlope`, `isGrinding`, `isVerticalAir`) cleanly inside `PlayerMovement.cs`.

### Implementation
- **Controller**: `PlayerMovement.cs`
- **Internal States**: Evaluated per frame in `UpdateGroundedState()` via Rigidbody2D contact normals and ground check overlaps.
- **Dead Code Purge**: Unused polymorphic state interfaces (`IPlayerState`, `PlayerStateMachine`, etc.) were removed under Option B refactoring.

### Benefits
- **Performance**: Zero heap allocations per state change.
- **Direct Access**: Direct cached access to Animator hashes and Rigidbody2D velocity vectors without indirection.

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

## 3. Flyweight & Object Caching Pattern (Performance Optimization)

### Purpose
To prevent GPU VRAM memory leaks and CPU garbage collection spikes caused by runtime string lookups or texture creation.

### Implementation
- **`ScoreUI.cs`**: Static `Dictionary<string, Sprite> spriteCache` reuses dynamically generated rounded UI textures across scene reloads.
- **`PlayerMovement.cs`**: `Animator.StringToHash` pre-computes static integer hashes for all animation parameters (`AnimHorizontal`, `AnimIsGrinding`, `AnimRoyal`, etc.).
- **`VintageFilter.cs`**: `Shader.PropertyToID` pre-computes static IDs for all shader uniforms (`_SepiaAmount`, `_Desaturation`, etc.).

---

## 4. Interface-Based Polymorphism (Obstacle System)

### Purpose
To decouple player physics code from concrete environment game objects.

### Implementation
- **`IRampObstacle`**: Implemented by ramp lip trigger components (`RampBoostZone.cs`).
- **`IRailObstacle`**: Implemented by rail geometry components (`GrindRail.cs`).

### Benefits
- The player controller interacts exclusively with interface methods (`GetLaunchImpulse`, `GetRailDirection`) rather than referencing specific scene objects.

---

## 5. Observer Pattern (Event-Driven Architecture)

### Purpose
To decouple core gameplay physics from secondary systems such as User Interface (UI), Sound Effects (SFX), and Particle Effects (VFX).

### Implementation
- C# `event Action<TrickData>` defined on `TrickController.cs`.
- Game managers and UI scripts subscribe to trick events without the player controller needing direct references to UI text components or Audio Sources.
