# System Architecture

This document details the high-level software architecture, component organization, and data flow of **Stickman Rollerblader**.

---

## 1. System Overview

The application architecture is structured into decoupled modules to promote maintainability, scalability, and adherence to SOLID principles:

```
                  ┌────────────────────────┐
                  │    Player Controller   │
                  │ (PlayerStateMachine)   │
                  └───────────┬────────────┘
                              │
         ┌────────────────────┼────────────────────┐
         ▼                    ▼                    ▼
┌─────────────────┐  ┌──────────────────┐  ┌─────────────────┐
│ Player States   │  │ Obstacle System  │  │  Trick System   │
│ (Grounded/Air)  │  │ (Ramp/Rail Interfaces) │ (ScriptableObj)│
└─────────────────┘  └──────────────────┘  └─────────────────┘
```

---

## 2. Core Modules

### 2.1 Player Module (`Assets/Scripts/Player/`)
- **`PlayerStateMachine`**: Manages state transitions and delegates `Update` / `FixedUpdate` calls to the active state.
- **`IPlayerState`**: State interface requiring lifecycle methods (`Enter`, `Exit`, `LogicUpdate`, `PhysicsUpdate`).
- **Concrete States**:
  - `GroundedState`: Handles surface movement, slope normal detection, and jump initiation.
  - `AirborneState`: Handles airborne movement, extra fall gravity, and trick input listening.
  - `GrindingState`: Handles rail attachment, rail directional velocity, and balance mechanics.

### 2.2 Movement & Camera (`Assets/Scripts/`)
- **`PlayerMovement.cs`**: Handles physics interactions, Raycast slope detection, slope rotation Slerp, and ramp boost timing.
- **`CameraFollow.cs`**: Implements 2D camera tracking using `Vector3.SmoothDamp` with locked Y-axis positioning for side-scrolling levels.

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

1. **Input Stage**: `PlayerInput` triggers input callbacks (`Move`, `Jump`) on `PlayerMovement`.
2. **Detection Stage**: `Update()` performs Raycasts (`CheckSlope`) and overlap box checks (`UpdateGroundedState`).
3. **Physics Stage**: `FixedUpdate()` applies slope-aligned velocity or airborne gravity.
4. **State Transition Stage**: `PlayerStateMachine` switches active states based on ground contact or rail detection.
5. **Render & Camera Stage**: `LateUpdate()` executes `CameraFollow.cs` to smoothly position the camera after player physics updates.
