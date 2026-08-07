# System Architecture

This document details the high-level software architecture, component organization, and data flow of **Stickman Rollerblader**.

---

## 1. System Overview

The application architecture is structured into modular components to promote maintainability, performance, and clear separation of concerns:

```
                  ┌────────────────────────┐
                  │    Player Controller   │
                  │   (PlayerMovement.cs)  │
                  └───────────┬────────────┘
                              │
         ┌────────────────────┼────────────────────┐
         ▼                    ▼                    ▼
┌─────────────────┐  ┌──────────────────┐  ┌─────────────────┐
┌ Internal States ┐  │ Obstacle System  │  │  Trick System   │
│(Ground/Air/Grind│  │ (Ramp/Rail Interfaces) │ (ScriptableObj)│
└─────────────────┘  └──────────────────┘  └─────────────────┘
```

---

## 2. Core Modules

### 2.1 Player Module (`Assets/Scripts/`)
- **`PlayerMovement.cs`**: Consolidated player controller managing input handling, Physics2D raycasting & contact detection, slope vector alignment, halfpipe vertical air trajectory, map boundary clamping, and grind stance pre-selection. Dead state machine classes (`PlayerStateMachine`, `IPlayerState`, etc.) were purged under Option B optimization.
- **Cached Hashes & Component References**: Caches all `Animator.StringToHash` IDs and `PlayerAudio` references in `Start()` to eliminate runtime string lookup overhead and allocations.

### 2.2 Movement, Camera, UI & Post-Processing (`Assets/Scripts/` & `Assets/Shaders/`)
- **`CameraFollow.cs`**: Implements 2D camera tracking using `Vector3.SmoothDamp`, background bounds clamping, and selective upward Y-tracking (`followYAboveFixed`).
- **`VintageFilter.cs` & `VintageEffect.shader`**: Full-screen post-processing filter providing Sepia tinting, Vignette, Desaturation, and real-time Film Grain using cached shader property IDs (`Shader.PropertyToID`).
- **`ScoreUI.cs`**: Dynamic HUD and initial pre-match menu controller. Includes procedural rounded UI texture generation with a static `Dictionary<string, Sprite>` cache to prevent GPU VRAM memory leaks. Set to `sortingOrder = 120` to render above countdown transitions.
- **`CountdownManager.cs`**: Iris transition and match countdown manager. Controls pre-match freeze state and triggers game start upon clicking "READY!".

### 2.3 Obstacle System (`Assets/Scripts/Obstacles/`)
- **`IRampObstacle`**: Interface defining launch impulse vectors and boost trigger timing windows.
- **`IRailObstacle`**: Interface providing rail geometry vectors (`GetClosestPointOnRail`, `GetRailDirection`) and friction values.
- **`RampBoostZone.cs`**: MonoBehaviour trigger component placed on ramp lips to enable ramp boost jumping.
- **`GrindRail.cs`**: MonoBehaviour component defining rail start/end points for grinding physics.

### 2.4 Trick System (`Assets/Scripts/Tricks/`)
- **`TrickData`**: Base ScriptableObject defining trick metadata (name, points, input binding, animation trigger).
- **`AirTrickData`**: Specialized ScriptableObject for aerial tricks (spins, grabs, required air time).
- **`GrindTrickData`**: Specialized ScriptableObject for grind tricks (stances, balance multipliers).
- **`TrickController`**: Evaluates player inputs against active trick strategies and fires completion events.

---

## 3. Data Flow & Execution Order

1. **Pre-Match Stage**: `ScoreUI` presents the initial "HOW TO PLAY" instruction panel (`ShowInitialMenu`). `CountdownManager` overlay remains hidden until the player clicks "READY!".
2. **Input Stage**: `PlayerInput` triggers input callbacks (`Move`, `Jump`, `Trick`) on `PlayerMovement`.
3. **Detection Stage**: `Update()` performs contact detection (`UpdateGroundedState`) and updates animation parameters.
4. **Physics Stage**: `FixedUpdate()` applies slope-aligned velocity, airborne fall multiplier, and projected slope speed capping (`maxSlopeSpeed`).
5. **Boundary Stage**: `LateUpdate()` clamps player position within background bounds (`ClampPositionToBounds`) and updates camera position (`CameraFollow.cs`).
