# Balloon Balance VR

Balloon Balance VR is an SSRP Unity project for real-time Center of Pressure signal processing using a Wii Balance Board. A participant leans on the board to move a pointer through a forward-facing VR balloon challenge.

## Approved game

- A calibration lobby ends at a glowing **Start Balloon Challenge** door.
- A moderate forward lean confirms the start after calibration.
- Each scored round lasts 90 seconds.
- Each participant completes four measured rounds, providing six minutes of measured gameplay data in total.
- The VR camera remains stationary except for natural headset tracking.
- CoP ML controls horizontal pointer movement.
- CoP AP controls vertical pointer movement.
- Balloons spawn only ahead, at controlled width, height, and depth.
- Balloons move toward an interaction plane and are burst by pointer collision.
- Practice rounds are random. Experimental rounds use hidden matched sequences and counterbalanced conditions.

## Documentation

- [Complete PDF plan](Devlopment%20Plan.pdf)
- [Project context](Docs/PROJECT_CONTEXT.md)
- [Architecture](Docs/ARCHITECTURE.md)
- [Development plan](Docs/DEVELOPMENT_PLAN.md)
- [Unity setup](Docs/UNITY_SETUP.md)
- [Signal processing](Docs/SIGNAL_PROCESSING.md)
- [Decisions](Docs/DECISIONS.md)
- [Task tracker](Docs/TASKS.md)
- [New chat handoff](Docs/NEW_CHAT_HANDOFF.md)

Future Codex sessions must start by reading [AGENTS.md](AGENTS.md).

## Copying this package into the Unity project

Copy the following into the future Unity repository root:

```text
AGENTS.md
README.md
Devlopment Plan.pdf
Docs/
```

The destination should become:

```text
BalloonBalanceVR/
|-- AGENTS.md
|-- README.md
|-- Devlopment Plan.pdf
|-- Docs/
|-- Assets/
|-- Packages/
`-- ProjectSettings/
```

Do not place the documentation inside `Assets/`; Unity does not need to import it.

## Planned technology

- Unity LTS through Unity Hub
- Unity VR template
- OpenXR
- XR Interaction Toolkit
- Input System
- TextMeshPro
- Unity Test Framework
- C# gameplay and processing code
- Python or compatible acquisition bridge for the Wii Balance Board
- UDP communication between acquisition and Unity
- A dedicated external board bridge that logs every acquired raw packet before sending versioned four-load UDP packets
- GitHub private repository with Git LFS

## Status

Planning is approved and includes the dedicated live bridge and four-round participant protocol. The Unity `6000.6.6f1` VR-template project, OpenXR/XR Simulation baseline, Git LFS rules, and repository context are present. The board library, sensor orientation, physical sensor spacing, sampling rate, Bluetooth behaviour, and physical headset runtime remain deferred until hardware details are available. Project-specific gameplay implementation is beginning with the board-independent vertical slice.

Continue from the first unfinished item in [Tasks](Docs/TASKS.md).

