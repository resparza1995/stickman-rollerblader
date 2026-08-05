# Gameplay Mechanics

This document provides a technical explanation of the core physics, movement, camera, and trick mechanics in **Stickman Rollerblader**.

---

## 1. Slope Movement & Normal Alignment

### Slope Surface Speed
When the player is grounded on an inclined surface (`isGrounded && isOnSlope`), horizontal movement velocity is projected along the slope tangent vector using `Vector2.Perpendicular(slopeNormal)`.

### Smooth Rotation (`UpdateSlopeRotation`)
To align the skater's body perpendicularly with curved or angled ramps:
1. Raycast collision returns the surface normal vector `slopeNormal`.
2. Angle calculation: `targetAngle = Mathf.Atan2(slopeNormal.y, slopeNormal.x) * Mathf.Rad2Deg - 90f`.
3. Rotation interpolation: `transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * slopeRotationSpeed)`.
4. When airborne, the rotation smoothly interpolates back to `0°` (`Quaternion.identity`).

---

## 2. Jump & Fall Physics Dynamics

### Dynamic Fall Gravity (`fallMultiplier`)
To prevent "floaty" jumping physics in 2D space:
- When the player is ascending (`rb.linearVelocity.y > 0`), standard gravity applies.
- When descending (`rb.linearVelocity.y < 0`), extra downward acceleration is added:
  ```csharp
  rb.linearVelocity += Vector2.up * (Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime);
  ```

### Slope Jump Preservation
To prevent slope-aligned velocity from crushing upward jump velocity during takeoff:
- Slope movement is applied in `FixedUpdate` only when `rb.linearVelocity.y <= 0.1f`.
- `Jump()` sets `isGrounded = false` immediately upon invocation.

---

## 3. Ramp Boost Mechanic

### Overview
Allows the player to gain extra height and speed by pressing `Space` right at the lip of a ramp.

### Mechanics Workflow
1. Player enters `RampBoostZone` trigger collider at the top of a ramp.
2. `RampBoostZone` calls `EnableRampBoost(boostImpulse, boostWindowDuration)` on `PlayerMovement`.
3. A timer window (e.g. `0.25s`) is activated.
4. If `Jump()` is called while `canRampBoost` is `true`, directional impulse `(X * facingDirection, Y)` is applied immediately to `rb.linearVelocity`.

---

## 4. Camera Follow System

### Smooth Damping (`CameraFollow.cs`)
- Tracks the player's horizontal position (X-axis) using `Vector3.SmoothDamp`.
- Locks vertical positioning (Y-axis) to a fixed height (`fixedY = -0.47f` by default) to keep side-scrolling level layout visible and stable.

---

## 5. Controls & Trick System

### Control Scheme
- **Movement (`WASD`)**: `A` and `D` keys control horizontal skater movement (`Move` action); `W` or `Space` triggers jumps (`Jump` action). Arrow keys are isolated from horizontal movement.
- **Air Rotation Tricks (`Arrow Keys`)**:
  - **Left / Right Arrow**: Air 360 Spin (360° horizontal body rotation around Y-axis).
  - **Up / Down Arrow**: Backflip (`UpArrow`) and Frontflip (`DownArrow`) (360° somersault flips around Z-axis).

### Air Tricks & Scoring Integration
- **Execution Trigger**: Activated when an Arrow key is pressed while airborne (`!isGrounded`).
- **Animation & Physics**: Triggers the `AirRotate` animator state (`AirRotate.anim`) and runs dynamic rotation coroutines (`PerformYSpin` for Y-axis spins, `PerformZFlip` for Z-axis flips) over `spinDuration`.
- **Scoring**: Processed via `TrickController.TryExecuteTrick`, granting score points (e.g., `+150 pts` for `Air360`) upon successful airborne execution.


