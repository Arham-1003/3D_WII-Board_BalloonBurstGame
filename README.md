# Balloon Balance VR

Balloon Balance VR is an SSRP Unity project for real-time Center of Pressure signal processing using a Wii Balance Board. A participant leans on the board to move a pointer through a forward-facing VR balloon challenge.

## Approved game

- A calibration lobby ends at a glowing **Start Balloon Challenge** door.
- A moderate forward lean confirms the start after calibration.
- Each scored round lasts 90 seconds.
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
- GitHub private repository with Git LFS

## Status

Planning is approved. The PDF and portable context package are complete. Unity project creation and implementation have not started. Hardware integration remains unverified until a physical Wii Balance Board is available.

Continue from the first unfinished item in [Tasks](Docs/TASKS.md).

