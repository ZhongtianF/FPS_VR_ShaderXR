# TP5 – Pathfinding Avancé XR (Drone Companion)

This Unity project corresponds to **TP5 – Pathfinding Avancé**, in continuity with
**TP3 (Shaders)** and **TP4 (Animation)**.

The objective is to implement a **3D local pathfinding behaviour** for a flying
drone companion in an industrial FPS XR environment, without using NavMesh.
The drone follows the player while avoiding obstacles and maintaining smooth,
stable motion suitable for XR.

---

## Final Scene

The **final playable scene** used for the TP5 demonstration is located at: Assets/RPG_FPS_game_assets_industrial/Map_v2.unity


This scene contains the complete industrial environment, the player, and the
drone companion with AI behaviour, visual feedback, and animation.

---

## Project Structure

The repository contains the full Unity project source code:

- `Assets/`
  - Drone scripts (AI, state management, data configuration)
  - Shaders (TP3)
  - Animations (TP4)
  - Industrial environment assets
- `Packages/`
- `ProjectSettings/`

Build files (Windows / APK) are not included directly in the repository.

---

## Drone Architecture Overview

The drone behaviour is implemented using a **modular architecture** composed of
four main scripts:

### 1. DroneAI.cs (Core Pathfinding & Movement)
This script implements the main drone behaviour:
- Computation of the ideal direction towards the target (player follow point)
- Sampling of multiple candidate directions around this direction
- Obstacle detection using **SphereCast**
- Scoring and selection of the best valid direction
- Smooth movement and rotation using interpolation to avoid jitter

The approach is **local and reactive**, without any global navigation mesh.

---

### 2. DroneData.cs (Configuration & Parameters)
All navigation parameters are centralized in this script:
- Movement speed and rotation speed
- Follow distance and offset
- Obstacle detection distance and radius
- Weights used for direction scoring (stability, distance, safety)

This separation allows easy tuning of the drone behaviour without modifying
the AI logic.

---

### 3. DroneStateManager.cs (TP4 – Animation State Logic)
This script manages the logical state of the drone:
- Idle
- Moving
- Alert (obstacle proximity or constrained movement)

The current state is determined from the navigation context and is used to
drive the Animator transitions in a clean and readable way.

---

### 4. DroneVisualFeedback.cs (TP3 – Shader & Visual Feedback)
This script handles the visual representation of the drone state:
- Normal visual state
- Cautious / alert state when close to obstacles

Shader parameters are updated at runtime using
`MaterialPropertyBlock`, avoiding material instantiation and ensuring good
performance.

---

## Pathfinding Principle (TP5)

At each update:
1. The drone computes the ideal direction towards its follow target.
2. Several candidate directions are generated around this direction.
3. Each direction is tested using **SphereCast** to detect obstacles.
4. Valid directions are evaluated using a scoring system based on:
   - Distance to target
   - Movement stability
   - Safety margin from obstacles
5. The best direction is selected and applied with smoothing.

This ensures a **fluid, stable, and readable behaviour**, adapted to XR
constraints.

---

## Integration with Previous TPs

### TP3 – Shaders
- Visual feedback driven by drone state
- Shader parameters updated efficiently via `MaterialPropertyBlock`

### TP4 – Animation
- Animator states controlled by the logical state of the drone
- Clean separation between navigation logic and animation control


---

## 🛠️ Unity Version
- Unity 6000.0.62f1


A short presentation video demonstrating the final behaviour is available: `presentation_video.mp4`.

### PCVR Build (Windows)

The PCVR version of the project is available for download here:
https://drive.google.com/file/d/12QTg57EK202RXnuOJ6mAL2azGubor-U3/view?usp=sharing


