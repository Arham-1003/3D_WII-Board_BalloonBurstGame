# Architecture

## Design goals

The architecture must allow the complete Unity game and processing pipeline to be developed without a physical Wii Balance Board. Replacing simulated input with live input must not require gameplay changes.

```text
Wii board -> external bridge -> UDP
keyboard / simulator / CSV ------+
                  |
                  v
           ICopInputSource
                  |
                  v
    validation and CoP calculation
                  |
                  v
        causal signal filtering
                  |
                  v
    calibration and normalization
                  |
                  v
          pointer controller
                  |
          +-------+-------+
          |               |
          v               v
       gameplay        logging
```

## Assembly boundaries

When time allows, use assembly definitions to separate:

- `BalloonBalance.Core`
- `BalloonBalance.Input`
- `BalloonBalance.SignalProcessing`
- `BalloonBalance.Gameplay`
- `BalloonBalance.Logging`
- corresponding EditMode and PlayMode test assemblies

Avoid introducing assembly definitions before the first vertical slice if they slow initial learning. Add them once folders and dependencies stabilize.

## Data model

### `CopSample`

Use an immutable struct or a read-only data object containing:

```text
sequenceNumber
sourceTimestampSeconds
receiveTimestampSeconds
frontLeft
frontRight
rearLeft
rearRight
totalLoad
rawCopX
rawCopY
filteredCopX
filteredCopY
sourceType
validityFlags
connectionState
```

Do not use frame rate as the acquisition timestamp.

### Trial data

Separate high-rate sample records from lower-rate gameplay events.

`TrialSample` includes load, CoP, normalized control, pointer position, packet age, and validity.

`TrialEvent` includes event type, balloon ID, sequence index, target transform, size, speed, response time, and score.

`TrialMetadata` includes participant code, build/commit, condition, filter parameters, input source, calibration, hidden seed, sequence ID, and hardware/runtime information.

## Input sources

```csharp
public interface ICopInputSource
{
    bool IsConnected { get; }
    bool TryGetLatestSample(out CopSample sample);
}
```

Implementations:

- `KeyboardCopInputSource`: smooth keyboard-driven normalized movement.
- `SyntheticBoardInputSource`: generates four corner loads plus noise, drift, spikes, loss, and timing jitter.
- `CsvPlaybackInputSource`: replays recorded source timestamps in real time, paused, stepped, or accelerated modes.
- `UdpCopInputSource`: receives live or synthetic packets from an external bridge.

`CopInputManager` selects one source from configuration and publishes the latest valid sample. Gameplay must not know the selected source type.

## Processing

```csharp
public interface ICopFilter
{
    string Name { get; }
    void Reset();
    Vector2 Process(Vector2 rawCop, float deltaTime);
}
```

Planned implementations:

- pass-through/raw baseline;
- moving average;
- exponential moving average;
- second-order Butterworth low-pass;
- optional median spike prefilter.

Reset filter state when calibration starts and before every measured trial.

## Calibration

Keep calibration values in a serializable `CalibrationProfile`:

```text
neutralX, neutralY
leftRange, rightRange
forwardRange, backwardRange
deadZoneX, deadZoneY
invertX, invertY
createdAt
quality metrics
```

Use asymmetric normalization because opposing lean ranges are rarely equal.

## Unity scenes

### Bootstrap

Persistent configuration, input selection, processing, logging, and session state. Loads the calibration lobby.

### CalibrationLobby

XR Origin, instructions, neutral/directional targets, diagnostics, and glowing start door.

### Gameplay

Stationary XR Origin, environment, forward spawn volume, interaction plane, pointer, balloon pool, miss boundary, HUD, and diagnostics.

### Results

Score and metrics summary, saved-file confirmation, retry, and exit.

## Prefabs

### Pointer

- visible high-contrast mesh;
- trigger collider;
- `PointerController`;
- optional trail used only if it does not obscure targets;
- no direct input-source dependency.

### Balloon

- `BalloonController`;
- visible mesh/material;
- trigger collider;
- optional kinematic Rigidbody when required by collision configuration;
- identity and trial metadata;
- pooled burst and sound requests.

### Spawn volume

- explicit horizontal, vertical, and depth bounds;
- gizmos for editor visualization;
- minimum spacing and consecutive-distance rules;
- seeded sequence mode and random mode.

## UDP bridge

The Wii Balance Board Recorder is a recording/calibration tool, not the assumed live Unity transport. A dedicated external bridge is a required component. Its responsibilities are deliberately narrow:

1. connect to the board through the selected and hardware-verified library or acquisition method;
2. read calibrated four-corner loads and a monotonic source timestamp;
3. assign a strictly increasing sequence number;
4. append every acquired raw packet to a bridge-side CSV before transmission;
5. transmit the versioned packet to `127.0.0.1:5005` at the measured board rate;
6. expose connection, calibration, and fault status without performing gameplay filtering.

The acquisition library is intentionally undecided until the supplied board is tested. A Windows C# bridge is the preferred first investigation, but no library is considered approved merely because it compiles.

Preferred live packet:

```json
{
  "version": 1,
  "sequence": 205,
  "sourceTime": 4.216,
  "frontLeft": 16.8,
  "frontRight": 17.1,
  "rearLeft": 16.5,
  "rearRight": 16.9,
  "totalLoad": 67.3,
  "connected": true
}
```

Receive on a background task/thread and enqueue immutable packets. Never call Unity object APIs from the receive thread. On the main thread, process/log all queued packets in sequence while exposing the newest valid processed state to pointer rendering. Detect sequence gaps, duplicates, reordering, malformed values, stale data, and clock discontinuities. Dispose the socket on quit and assembly reload.

## Logging

Use separate sample, event, and metadata files. The bridge raw CSV is the authoritative record of every acquired packet. Unity's sample log records every packet it receives plus derived CoP/filter/mapping values; its event log records gameplay events. Merge them using sequence number and source timestamp, never frame index alone. Create unique names from participant code, condition, repetition, and timestamp. Write to an in-progress name and finalize only after a clean close. Report saved paths, row counts, missing-sequence counts, and rejected-packet counts to the operator.

## Performance rules

- Pool balloons and burst effects.
- Avoid per-frame LINQ and avoidable allocations during measured trials.
- Do not log synchronously every frame if it creates frame spikes; buffer and flush safely.
- Measure packet age and frame timing.
- Keep diagnostic rendering optional and disable expensive overlays during formal trials if validated.

## Failure handling

- Stale packet: freeze or smoothly return pointer to centre and show warning.
- Low total load: mark unoccupied and do not calculate CoP.
- NaN/infinity/impossible load: reject and log.
- Headset pause/focus loss: pause trial and timer.
- Sustained saturation: warn operator; do not silently recalibrate mid-trial.

