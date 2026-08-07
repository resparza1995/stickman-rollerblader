# Gameplay Mechanics

This document provides a technical explanation of the core physics, movement, camera, post-processing, and trick mechanics in **Stickman Rollerblader**.

---

## 1. Slope Movement & Normal Alignment

### Slope Surface Physics (`rb.GetContacts`)
When the player is grounded on an inclined surface (`isGrounded && isOnSlope`), surface detection uses `rb.GetContacts` for 100% accurate contact normals across steep quarterpipes and curved ramps.
- Normal inversion correction: Detects negative scale colliders (`slopeNormal.y < 0 => slopeNormal = -slopeNormal`).
- Slope movement velocity is calculated tangentially along `(slopeNormal.y, -slopeNormal.x)`.

### Smooth Rotation (`UpdateSlopeRotation`)
To align the skater's body perpendicularly with curved or angled ramps:
1. Target angle calculation: `targetAngle = Mathf.Atan2(-slopeNormal.x, slopeNormal.y) * Mathf.Rad2Deg`.
2. Rotation interpolation in `LateUpdate`: `transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * slopeRotationSpeed)`.
3. When airborne, rotation smoothly interpolates back to `0°` (`Quaternion.identity`).

---

## 2. Jump, Halfpipe & Fall Physics Dynamics

### Halfpipe Vertical Air Launch (`Tag: Halfpipe`)
When jumping on a slope tagged `"Halfpipe"`:
1. The character enters `isVerticalAir` state.
2. Direct vertical launch velocity is applied (`Vector2.up * rampJumpForce`, default `rampJumpForce = 12f`).
3. Lateral movement input (`A`/`D`) is suppressed for physics in the air to allow pure vertical parabolic flight without crashing into ramp walls.
4. A `jumpCooldownTimer = 0.35f` prevents immediate physics re-grounding overwrite during launch.

### Dynamic Fall Gravity (`fallMultiplier`)
To prevent "floaty" jumping physics in 2D space:
- When ascending (`rb.linearVelocity.y > 0`), standard gravity applies.
- When descending (`rb.linearVelocity.y < 0`), extra downward acceleration is added:
  ```csharp
  rb.linearVelocity += Vector2.up * (Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime);
  ```

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

## 4. Camera Systems (`CameraFollow.cs` & `VintageFilter.cs`)

### Background Bounds & Selective Upward Y Tracking (`CameraFollow.cs`)
- **Horizontal Clamping**: Automatically calculates `minX` and `maxX` boundaries using `SpriteRenderer` background bounds and camera orthographic aspect ratio to prevent showing out-of-map background edges.
- **Selective Y Tracking (`followYAboveFixed`)**: Keeps camera Y locked at `fixedY = -0.47f` on the ground, but smoothly follows the player upwards (`Mathf.Max(fixedY, targetY)`) during high vertical air launches.

### Vintage Camera Post-Processing Filter (`VintageFilter.cs` & `VintageEffect.shader`)
Custom full-screen camera Image Effect featuring:
- **Sepia & Color Tint**: Adjustable warm sepia tone tinting.
- **Desaturation**: Analog color saturation reduction.
- **Vignette**: Circular edge darkening.
- **Film Grain**: Animated real-time film noise grain.

---

## 5. Controls & Trick System

### Control Scheme
- **Movement (`WASD`)**: `A` and `D` keys control horizontal skater movement (`Move` action); `W` or `Space` triggers jumps (`Jump` action).
- **Air Rotation Tricks (`Arrow Keys`)**:
  - **Left / Right Arrow**: Air 360 Spin (360° horizontal body rotation around Y-axis).
  - **Up / Down Arrow**: Backflip (`UpArrow`) and Frontflip (`DownArrow`) (360° somersault flips around Z-axis).

### Air Tricks & Scoring Integration
- **Execution Trigger**: Activated when an Arrow key is pressed while airborne (`!isGrounded`).
- **Animation & Physics**: Triggers the `AirRotate` animator state (`AirRotate.anim`) and runs dynamic rotation coroutines (`PerformYSpin` for Y-axis spins, `PerformZFlip` for Z-axis flips) over `spinDuration`.
- **Scoring**: Processed via `TrickController.TryExecuteTrick`, granting score points (e.g., `+150 pts` for `Air360`) upon successful airborne execution.

---

## 6. Rail Grinding & Grind Stances

### Rail Surface Detection (`Tag: Rail` / `IRailObstacle`)
When the player collides with an object tagged `"Rail"` or implementing `IRailObstacle`:
1. `UpdateGroundedState` activates `isGrinding = true`.
2. Gravity scale is set to `0f` in `FixedUpdate` so the player locks smoothly onto the rail line.
3. Player horizontal velocity slides tangentially along the rail slope normal.

### Grind Stances & Controls
- **Keys `1` / `Numpad1`**: Switches stance to **Royal** (`Royal.anim`, `+200 pts`).
- **Keys `2` / `Numpad2`**: Switches stance to **Savannah** (`Savannah.anim`, `+300 pts`).
- **Keys `3` / `Numpad3`**: Switches stance to **Soul** (`Soul.anim`, `+250 pts`).

### Rail Exit Mechanics
- **Jump Exit**: Pressing `Space` / `W` while grinding sets `isGrinding = false` and `isGrounded = false`, triggering the `Jump` animation immediately.
- **Airborne Exit Guard**: Animator exit transitions from grind states require `IsGrounded == true` to enter `Skating` or `IdleReady`, preventing the player from entering ground animation states while falling through the air.




