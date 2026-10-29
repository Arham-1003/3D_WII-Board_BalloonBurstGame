# Unity Setup

## Version policy

Use Unity `6000.6.6f1` across all development computers. Do not change the editor or locked package versions without recording and validating the change.

Record the exact editor version and package-lock changes in this file once the project is created. Do not update the editor or packages during formal data collection unless a critical defect requires it.

## Installation

Install through Unity Hub:

- selected Unity LTS editor;
- Windows Build Support;
- Microsoft Visual Studio components only if the team uses them;
- Android build support only if a standalone Android-based headset build is actually required.

Install separately:

- Visual Studio Code;
- appropriate C# tooling;
- Git;
- Git LFS;
- Codex extension.

## Project creation

1. Create a private GitHub repository and clone it locally.
2. Create a Unity project using the VR template inside the repository.
3. Use the project name `BalloonBalanceVR`.
4. Avoid cloud-synchronized folders that can lock or duplicate Unity files.
5. Confirm the project enters Play mode before importing optional content.

## Required packages

Confirm compatible versions of:

- Input System;
- XR Plug-in Management;
- OpenXR Plugin;
- XR Interaction Toolkit;
- TextMeshPro;
- Unity Test Framework.

Import XR Interaction Toolkit Starter Assets and the XR simulator sample when needed by the installed package version.

Verified project baseline on 2026-10-24:

```text
Unity Editor: 6000.6.6f1
Input System: 1.20.0
XR Plug-in Management: 4.7.0
OpenXR Plugin: 1.18.0
XR Interaction Toolkit: 3.6.1
TextMeshPro: supplied by the Unity 6 project/UI stack; project essentials are imported
Test Framework: 1.8.0 (transitive dependency)
Development runtime: OpenXR with XR Simulation fallback
Physical headset/runtime: deferred until hardware details are available
```

## Project Settings

### Editor

- Version Control Mode: **Visible Meta Files**
- Asset Serialization Mode: **Force Text**

### Player

- Company name: team or university project label
- Product name: Balloon Balance VR
- Active Input Handling: use the Input System configuration required by the installed XR packages
- Run in background: decide deliberately; do not allow unnoticed measured trials when focus is lost

### XR Plug-in Management

- Enable the correct OpenXR provider for the target platform.
- Select only the interaction profiles required by the available headset/controllers.
- Resolve OpenXR validation warnings before formal headset testing.

### Quality

- Begin with moderate settings that maintain stable VR frame rate.
- Avoid heavy post-processing until performance is measured.
- Do not reduce target visibility for decorative effects.

## Repository files

Commit:

```text
Assets/
Packages/
ProjectSettings/
AGENTS.md
README.md
Docs/
```

Ignore:

```text
Library/
Temp/
Logs/
Obj/
Build/
Builds/
MemoryCaptures/
```

Use the official GitHub Unity `.gitignore` as the base.

## Git LFS

Track large binary source assets when they are introduced, for example:

```gitattributes
*.fbx filter=lfs diff=lfs merge=lfs -text
*.blend filter=lfs diff=lfs merge=lfs -text
*.psd filter=lfs diff=lfs merge=lfs -text
*.wav filter=lfs diff=lfs merge=lfs -text
*.mp3 filter=lfs diff=lfs merge=lfs -text
*.mp4 filter=lfs diff=lfs merge=lfs -text
```

Do not put C# scripts, scenes, prefabs, or ordinary YAML assets in LFS.

## Initial folders

```text
Assets/
|-- Art/
|-- Audio/
|-- Materials/
|-- Prefabs/
|-- Scenes/
|-- Scripts/
|   |-- Core/
|   |-- Input/
|   |-- SignalProcessing/
|   |-- Calibration/
|   |-- Gameplay/
|   |-- Logging/
|   `-- UI/
|-- Settings/
`-- Tests/
    |-- EditMode/
    `-- PlayMode/
```

## Initial scenes

Create and add to build settings in this order:

1. `Bootstrap`
2. `CalibrationLobby`
3. `Gameplay`
4. `Results`

## First vertical slice

1. Add a stationary XR Origin to `Gameplay`.
2. Add a simple environment and lighting.
3. Create an interaction plane approximately three virtual metres ahead.
4. Create a visible pointer with a trigger collider.
5. Create one balloon with a trigger collider.
6. Control the pointer through keyboard-simulated CoP.
7. Burst the balloon and update a TextMeshPro score label.
8. Verify Game view and XR simulator operation.
9. Commit before implementing random spawning.

## External script editor and Codex

Set Visual Studio Code as Unity's external script editor. Open the repository root, not only `Assets/`, so Codex can read `AGENTS.md`, `Docs/`, `Packages/`, and `ProjectSettings/`.

At the start of a new chat, use [the handoff prompt](NEW_CHAT_HANDOFF.md).

## Pre-commit checks

- Unity has finished compiling.
- No unexpected Console errors exist.
- Changed scenes and prefabs are saved.
- Required `.meta` files are present.
- Generated folders are not staged.
- The relevant Play mode path works.
- Scene and prefab edits are focused and reviewable.
- [Tasks](TASKS.md) reflects the result.

