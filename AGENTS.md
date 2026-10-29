# Balloon Balance VR Agent Instructions

## Project objective

Build a Unity VR game for SSRP Project 3: real-time Center of Pressure processing for body-controlled balloon bursting using a Wii Balance Board.

The assessed game is a 90-second forward-only balloon challenge. The participant stands on the board while a stationary VR camera tracks only natural head movement. Mediolateral CoP controls pointer movement left and right. Anterior-posterior CoP controls pointer movement up and down. Balloons spawn ahead at different heights, widths, and depths and approach a fixed interaction plane.

Read the following before making architectural changes:

- [Project context](Docs/PROJECT_CONTEXT.md)
- [Architecture](Docs/ARCHITECTURE.md)
- [Development plan](Docs/DEVELOPMENT_PLAN.md)
- [Unity setup](Docs/UNITY_SETUP.md)
- [Signal processing](Docs/SIGNAL_PROCESSING.md)
- [Decisions](Docs/DECISIONS.md)
- [Tasks](Docs/TASKS.md)

The detailed human-readable plan is [Devlopment Plan.pdf](Devlopment%20Plan.pdf).

## Non-negotiable design rules

1. Keep the XR Origin stationary in the assessed mode. Do not map CoP to camera position or camera yaw.
2. Never spawn required targets behind the participant.
3. Preserve keyboard, synthetic four-load, and CSV playback input even after live board input works.
4. Gameplay must consume a common input interface and must not access Bluetooth or UDP directly.
5. Save raw corner loads and timestamps alongside derived CoP and gameplay events.
6. Use only causal filters for live control. Offline zero-phase filtering may be used only as a labelled analysis reference.
7. Guard against low total load, invalid numbers, stale packets, and disconnection.
8. Do not change filter settings, mapping, spawn rules, or scoring during a measured trial.
9. Record the active filter, parameters, sequence ID, hidden seed, calibration values, input source, and build identifier.
10. Treat VR comfort and participant stability as release-blocking requirements.
11. A dedicated external bridge must read the physical board and transmit versioned four-load packets to Unity; the recorder application alone is not the live transport.
12. The confirmed protocol requirement is at least five measured minutes per participant. Four 90-second measured rounds provide six minutes total.

## Required architecture

Keep these concerns separate:

```text
Acquisition -> ICopInputSource -> CoP calculation/filtering
            -> calibration/mapping -> pointer control
            -> gameplay -> logging/analysis
```

Required input implementations:

- `KeyboardCopInputSource`
- `SyntheticBoardInputSource`
- `CsvPlaybackInputSource`
- `UdpCopInputSource`

Required processing abstractions:

- `ICopInputSource`
- `ICopFilter`
- immutable or read-only `CopSample`
- calibration profile/data object
- trial logger separated from gameplay

Do not place socket code, filter state, calibration logic, scoring, and movement inside one MonoBehaviour.

The physical path is `Wii Balance Board -> external bridge -> UDP -> UdpCopInputSource`. The bridge and Unity must share sequence numbers and source timestamps. The bridge preserves every acquired raw packet; gameplay may consume the newest valid state without silently becoming the authoritative raw recorder.

## Unity working rules

- Use the Unity editor and package versions recorded in [Unity setup](Docs/UNITY_SETUP.md). Do not update packages casually.
- Keep visible `.meta` files and Force Text asset serialization enabled.
- Commit `Assets/`, `Packages/`, and `ProjectSettings/`.
- Never commit `Library/`, `Temp/`, `Logs/`, `Obj/`, or ordinary local builds.
- Prefer prefabs and small additive scenes over simultaneous edits to one large scene.
- Avoid manually editing scene or prefab YAML unless Unity cannot safely make the required change.
- Avoid allocations and repeated `Instantiate`/`Destroy` during measured rounds; use pooling for balloons and effects.
- Use monotonic time for response and latency measurements.

## Validation expectations

Before declaring a feature complete:

- Run relevant EditMode tests.
- Enter Play mode without compiler errors.
- Test with keyboard or synthetic input.
- Test stale/invalid/disconnected input paths.
- Confirm logs contain the expected samples and events.
- Reopen the project or scene when serialization/reference risk is involved.
- Update [Tasks](Docs/TASKS.md) and [Decisions](Docs/DECISIONS.md) when status or architecture changes.

Hardware-dependent work must remain explicitly marked unverified until tested with a physical Wii Balance Board and, where applicable, a VR headset.

## Scope discipline

Prioritize the tested vertical slice, data integrity, calibration, filters, and experimental fairness. Keep the four-scene structure, a small balloon/effect pool, matched hidden sequences, and core CoP/filter tests. Use a simple high-contrast environment for the assessed build; defer elaborate scenery, timed scenery changes, multiplayer, online accounts, embedded OpenAI APIs, lean-controlled camera rotation, and complex procedural worlds.

## New-session procedure

At the start of a new Codex session:

1. Read this file and the relevant documents under `Docs/`.
2. Inspect repository status and current Unity structure.
3. Read [Tasks](Docs/TASKS.md) for the first unfinished milestone.
4. State assumptions and identify hardware-dependent validation.
5. Make one reviewable change at a time and verify it before expanding scope.

