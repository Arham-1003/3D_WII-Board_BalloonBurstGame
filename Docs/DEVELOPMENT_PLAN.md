# Development Plan

This Markdown plan is the operational companion to [the complete PDF](../Devlopment%20Plan.pdf). The PDF contains the full rationale, tables, equations, testing detail, schedule, and references.

## Build principle

Create one end-to-end vertical slice before expanding scope:

```text
XR simulator/headset
-> stationary scene
-> simulated CoP
-> pointer movement
-> one balloon collision
-> burst feedback
-> score and log event
```

Every later milestone must preserve this working path.

## Milestone 0: decisions and access

- Select one Unity LTS version for all computers.
- Identify headset model/runtime and availability.
- Identify Wii Balance Board access date and Bluetooth computer.
- Agree on GitHub and Git LFS. Formal team-role and scene-ownership assignments are not required for the current workflow.
- Read [Project context](PROJECT_CONTEXT.md), [Architecture](ARCHITECTURE.md), and [Decisions](DECISIONS.md).

Exit gate: dependencies and responsibilities are known.

## Milestone 1: collaborative VR repository

1. Create a private GitHub repository.
2. Install/enable Git LFS.
3. Add the official Unity `.gitignore` and a suitable `.gitattributes`.
4. Create the Unity VR template project inside the repository.
5. Set visible metadata and Force Text serialization.
6. Verify required packages and OpenXR.
7. Commit documentation, settings, and a clean opening scene.
8. Confirm another member can clone and open the project.

Exit gate: every member can open the same editor version without errors.

## Milestone 2: one-balloon VR vertical slice

1. Create Bootstrap, CalibrationLobby, Gameplay, and Results scenes.
2. Add a stationary XR Origin to lobby and gameplay.
3. Add a keyboard-driven pointer on a front interaction plane.
4. Create one balloon prefab with trigger collision.
5. Burst the balloon, play feedback, and increment score.
6. Verify Game view and XR simulator operation.
7. Log spawn and hit events.

Exit gate: one simulated lean reaches and bursts one balloon.

## Milestone 3: complete 90-second round

1. Define a forward-only spawn volume.
2. Implement random and hidden-sequence spawn modes.
3. Add difficulty progression across three 30-second phases.
4. Add miss boundary and hit/miss definitions.
5. Add score, timer, countdown, pause, and results.
6. Pool balloons and effects.
7. Add scenery only after gameplay is reliable.

Exit gate: the full round completes and produces complete event data.

## Milestone 4: board-independent signal pipeline

1. Define `CopSample` and `ICopInputSource`.
2. Implement keyboard input.
3. Implement a four-corner load simulator.
4. Add configurable noise, drift, spikes, packet loss, and timing jitter.
5. Implement CoP calculation and low-load guards.
6. Implement CSV recording and playback.
7. Display optional raw and filtered diagnostic traces.

Exit gate: gameplay uses calculated simulated CoP rather than direct keyboard position.

## Milestone 5: calibration and mapping

1. Detect stable occupancy.
2. Record 3-5 seconds of neutral stance.
3. Record comfortable left/right/forward/backward ranges.
4. Validate minimum range and neutral stability.
5. Implement asymmetric normalization.
6. Add dead zones, remapping, inversion, and clamping.
7. Implement the start-door forward-hold gesture.

Exit gate: different users can reach the full safe play area without extreme lean.

## Milestone 6: filters and logging

1. Add raw/pass-through condition.
2. Add moving average or EMA.
3. Add second-order Butterworth low-pass.
4. Optionally add a short median prefilter.
5. Reset state before each trial.
6. Record every parameter in metadata.
7. Log raw loads, raw CoP, filtered CoP, normalized control, pointer, packet age, and events.

Exit gate: a recorded input can be replayed through each condition and compared.

## Milestone 7: UDP integration

1. Implement the versioned packet contract.
2. Create a synthetic external sender.
3. Receive on a background task/thread.
4. Publish the latest valid sample on Unity's main thread.
5. Detect stale, malformed, duplicate, and out-of-order packets.
6. Add visible connection status and safe timeout behaviour.
7. Queue and process every received packet for sample logging while using the newest valid state for rendering.
8. Detect and count sequence gaps, duplicates, reordering, malformed packets, and stale packets.

Exit gate: the external synthetic sender controls the same pointer path.

## Milestone 8: physical board

1. Install fresh batteries and test on a hard, level surface.
2. Pair or connect through compatible acquisition software and select a board-reading library only after a hardware test.
3. Verify each corner sensor independently.
4. Record zero-load offsets and units.
5. Verify right/left and forward/backward signs.
6. Measure actual rate from timestamps.
7. Build the dedicated external bridge: read four calibrated loads, assign source timestamp and sequence number, log every raw packet, and transmit the versioned UDP schema.
8. Connect raw calibrated loads to the UDP bridge.
9. Test power-off and participant-step-off handling.

Exit gate: live body lean reliably controls the pointer and produces complete logs.

## Milestone 9: VR comfort and visual polish

- Validate stationary camera and standing height.
- Keep targets within comfortable field and calibrated control limits.
- Use a simple, attractive, static environment with high target contrast and clear depth cues.
- Add audio and restrained burst effects.
- Defer environment/lighting transitions until all measured-session requirements are reliable.
- Conduct short comfort pilots with a spotter.

Exit gate: a headset pilot completes the round comfortably and safely.

## Milestone 10: pilot experiment

- Finalize filter parameters from non-formal pilot data.
- Create several matched hidden sequences.
- Counterbalance filter and sequence order.
- Run a complete session including practice, four 90-second measured rounds per participant, rest, and conditions.
- Verify every sample, event, and metadata field.
- Produce preliminary plots.

Exit gate: the pilot yields a complete analyzable dataset without manual repair.

## Milestone 11: formal acquisition and analysis

- Acquire four 90-second measured rounds per participant, yielding six minutes total and satisfying the confirmed minimum of five measured minutes per participant.
- Preserve raw data before analysis.
- Calculate neutral jitter, CoP path length, latency, settling, hit rate, response time, misses, and path efficiency.
- Produce load, raw/filtered CoP, trajectory, spectrum/PSD, and performance plots.
- Record exclusions and technical failures.

Exit gate: results support the claimed filter comparison.

## Milestone 12: release and presentation

- Build a Windows release and desktop fallback.
- Verify a fresh clone/build on a teammate's computer.
- Save a backup build and recorded demonstration.
- Capture hardware, calibration, and gameplay footage.
- Prepare a maximum 15-minute presentation.
- Rehearse hardware and fallback demonstrations.

Exit gate: the project can be presented even if live hardware fails on presentation day.

## Feature priorities

### Must

Stationary XR, four-scene flow, calibration, pointer, forward spawning, 90-second round, four measured rounds per participant, hit/miss, simulator, CoP, filters, dedicated live bridge, UDP, raw-preserving logging, safety timeout, core CoP/filter tests, pooling, and matched sequences.

### Should

Diagnostic plots, results screen, simple static scenery, restrained audio, and broader gameplay tests.

### Optional

Scenery transitions, slow constant-forward showcase mode, multiple elaborate environments, and advanced adaptive difficulty.

### Excluded

Lean-controlled camera yaw, multiplayer, online accounts, embedded OpenAI APIs, and complex procedural systems that threaten the schedule.

