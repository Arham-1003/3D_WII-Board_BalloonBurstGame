# Tasks

This is the implementation tracker for future chats and team work. Check an item only after its acceptance condition has been verified. Add a short note or commit reference for material completions.

## Planning and access

- [x] Approve the forward-only stationary-camera game concept.
- [x] Create the full PDF development plan.
- [x] Create portable repository context documents.
- [x] Resolve the live bridge, measured-round, retained-scope, and filter-timing planning decisions.
- [x] Select and record Unity `6000.6.6f1` as the team editor version.
- [ ] Record the physical headset model/runtime and availability when hardware details become available (deferred).
- [ ] Record the Wii Balance Board access date when hardware details become available (deferred).
- [x] Remove formal member-role and scene-ownership assignment as a planning blocker; retain small reviewable changes and coordination for shared scenes (D018).

## Repository and Unity foundation

- [ ] Create a private GitHub repository.
- [x] Install/configure Git LFS.
- [x] Add Unity `.gitignore` and `.gitattributes`.
- [x] Create the Unity VR template project.
- [x] Copy this documentation package to the repository root.
- [x] Enable Visible Meta Files.
- [x] Enable Force Text serialization.
- [x] Confirm and record the current required package versions.
- [ ] Configure OpenXR for the target runtime.
- [ ] Verify a teammate can clone and open the project.

Acceptance: the same clean project opens on two team computers without compiler errors.

## Vertical slice

- [x] Create Bootstrap scene.
- [x] Create CalibrationLobby scene.
- [x] Create Gameplay scene.
- [x] Create Results scene.
- [x] Add stationary XR Origin.
- [x] Import/configure XR simulator.
- [x] Create Pointer prefab.
- [x] Create Balloon prefab.
- [x] Implement keyboard input source.
- [x] Move pointer on a front plane.
- [x] Detect balloon hit.
- [x] Play burst feedback.
- [x] Increment score.
- [x] Log spawn and hit events.
- [x] Add a desktop-only `Simulator Head Look` setting: off by default so pointer keys cannot move or rotate the view; when enabled, allow deliberate simulated head rotation through controls separate from pointer input. Ignore this setting on physical headsets so natural head tracking remains active.

Acceptance: one keyboard-simulated lean bursts one balloon in Game view and XR simulation.

Progress note (2026-10-24): the read-only `CopSample`, `ICopInputSource`, keyboard source, input manager, and pointer controller have been added and pass an isolated C# compilation check. Keep their task boxes open until Unity imports them and the keyboard-to-pointer path passes Play Mode validation.

Progress note (2026-10-24): Unity created and serialized the four required scenes through editor APIs, assigned one named root object per scene, and enabled them in the required Bootstrap, CalibrationLobby, Gameplay, Results build order. A Unity batch import/compile completed without C# compiler errors. The EditMode runner exited successfully but reported zero authored tests; Play Mode behavior is not claimed by this scene-structure milestone.

Progress note (2026-10-24): Unity created a reusable stationary XR Origin prefab and instantiated it in CalibrationLobby and Gameplay. Head pose tracking is enabled, floor tracking is requested, the origin is fixed at world zero, and no locomotion provider is present. A clean headless Unity reopen validated both serialized scenes. Physical headset behavior and standing height remain explicitly unverified.

Progress note (2026-10-24): the official XR Interaction Simulator 3.6.1 sample is imported and configured for automatic Editor-only instantiation, so it is excluded from standalone experimental builds. A focused PlayMode test loaded Gameplay, instantiated the configured simulator, injected a tracked HMD pose, verified camera movement, and verified that the XR Origin position and rotation remained unchanged (1 passed, 0 failed). Physical headset validation remains deferred.

Progress note (2026-10-24): removed the obsolete VR-template `SampleScene` and `BasicScene`, then replaced the previously empty Gameplay view with a high-contrast stage, interaction frame, title, keyboard instructions, and a glowing trigger-based Pointer prefab. Keyboard CoP input and the pointer controller are wired in Gameplay. The PlayMode suite verifies simulated head motion with a stationary origin and rightward keyboard pointer movement constrained to the front plane (2 passed, 0 failed).

Progress note (2026-10-24): Gameplay now contains a complete desktop-testable practice loop: Start/Restart buttons, a 90-second timer, randomized forward balloon spawning, pooled balloon and burst-effect prefabs, approach movement, trigger hits, misses, score/status UI, and bright static scenery. The PlayMode suite clicks the real Start button, verifies an approaching balloon, forces a pointer collision, verifies score/burst/deactivation, finishes the round, and restarts it (3 passed, 0 failed across the full suite). Spawn/hit/miss trial logging is not implemented, so the vertical-slice acceptance statement is not yet fully satisfied.

Progress note (2026-10-25): completed Milestone 2 by adding a logger component separate from gameplay. Each practice round now records a hidden practice seed plus monotonic `round_start`, `spawn`, `hit`, `miss`, and `round_complete` events to a uniquely named in-progress CSV, then finalizes the file on clean round completion. The scene generator also corrected the near-ground rendering glitch and moved the HUD inward for narrow Game views. The full PlayMode suite passed (3 passed, 0 failed), including verification of a finalized event file containing start, spawn, hit, and completion rows. Physical headset presentation and Wii Balance Board input remain unverified.

Progress note (2026-10-25): corrected the Editor XR simulator's zero-height starting pose with an Editor-only 1.65 m standing-height fallback on the camera. This keeps the interaction rectangle centred in the Game view without translating or rotating the XR Origin and without overriding valid tracked headset heights. Physical-headset standing height remains unverified.

Progress note (2026-10-25): changed the desktop keyboard fallback to persistent incremental control. WASD/arrow input now moves the pointer from its current normalized position and releasing the key holds that position instead of returning to centre. This behavior is specific to the keyboard input source; future Wii Balance Board control remains an absolute calibrated CoP mapping through the common input interface.

Progress note (2026-10-25): isolated desktop pointer controls from the Editor XR Interaction Simulator because both were consuming WASD/arrow input. In keyboard gameplay the simulator component is disabled and the camera is reset once to the fixed centred standing pose; pointer input remains clamped and cannot translate or rotate the view. This Editor-only isolation does not disable later physical-headset tracking.

Planning note (2026-10-25): approved a future desktop-only `Simulator Head Look` toggle. The default fixed-view mode keeps WASD/arrows exclusive to pointer movement; the enabled mode will use separate look controls. Pointer position will not drive camera pose, and physical VR will continue to use natural headset tracking because coupling or suppressing it would confound CoP measurement and create a comfort risk.

Progress note (2026-10-25): implemented the desktop head-look setting and corrected the remaining below/behind viewpoint bug. Fixed-view mode now continuously holds the camera at `(0, 1.65, 0)` with identity rotation, exactly level with the interaction centre at `(0, 1.65, 3)`, and disables both simulator and tracked-pose updates that could restore a stale pose. The optional HUD toggle enables rotation only through right-mouse dragging, never through pointer keys, and never translates the XR Origin. The full PlayMode suite passed (3 passed, 0 failed), including stale-pose correction, optional mouse look, camera/pointer isolation, gameplay, pooling, and event logging. Physical-headset presentation remains unverified.

Progress note (2026-10-25): moved the desktop head-look option into a large game-owned `Input & View Settings` window. Keyboard Y or a gamepad's north/Y button opens and closes the window; opening it freezes the timer, balloons, and pointer, and closing it restores the previous time scale. The window explains the desktop pointer and camera controls. The full PlayMode suite passed (4 passed, 0 failed), including the new pause/restore regression test. Physical-headset tracking remains unaffected and hardware validation remains deferred.

Progress note (2026-10-26): completed the software portion of Milestone 3. Bursts inherit balloon colour; a three-second countdown leads into three fixed 30-second difficulty phases with deliberately modest interval/speed changes; forward bounds, active-target spacing, and consecutive-distance limits prevent haphazard target jumps. The static environment now uses layered blue-green hills, cloud clusters, a warm sun, and pathside flowers. PlayMode validation passed (5 passed, 0 failed).

Progress note (2026-10-26): replaced the stretched-sphere hill background after visual review. The Gameplay scene now uses purpose-built faceted mountain meshes with brighter blue distance layers, separate white snow caps, and green foothills positioned to the sides of the forward play corridor. No old `Far Hill` or `Near Hill` oval objects remain. The scene regenerated successfully and PlayMode validation passed (5 passed, 0 failed).

Progress note (2026-10-26): added the board-independent processing foundation: four-load CoP calculation with low-load/invalid/negative guards, synthetic four-load input with noise/drift/spikes/loss, CSV playback, asymmetric calibration mapping, continuous dead zones, inversion/clamping, raw/moving-average/EMA/Butterworth filters, and processing reset. Mathematical EditMode validation passed (3 passed, 0 failed), and the gameplay regression suite remained green (5 passed, 0 failed).

Progress note (2026-10-26): added the version 1 UDP receiver and external .NET synthetic sender. The sender logs raw packets before transmission and builds with 0 errors; Unity receives on a background thread, parses on the main thread, drains every queued packet, exposes the newest valid sample, and counts missing, duplicate, reordered, malformed, and stale traffic. End-to-end live UDP control and sample logging remain to be integrated and tested.

## Complete round

- [x] Implement 3D forward spawn volume.
- [x] Enforce bounds and minimum spacing.
- [x] Enforce consecutive target-distance limits.
- [x] Implement random hidden seed mode.
- [ ] Implement matched sequence mode.
- [x] Add balloon movement toward the interaction plane.
- [x] Add miss boundary.
- [x] Add 90-second timer.
- [x] Add three difficulty phases.
- [x] Add countdown, pause, score, and results UI.
- [x] Add balloon and effect pooling.

Acceptance: a 90-second round produces complete spawn/hit/miss/result data and never spawns behind.

## Input and CoP

- [x] Define immutable/read-only `CopSample`.
- [x] Define `ICopInputSource`.
- [x] Implement `KeyboardCopInputSource`.
- [x] Implement `SyntheticBoardInputSource`.
- [ ] Add noise, drift, spike, loss, and timing controls.
- [x] Implement CoP calculation.
- [x] Add low-total-load and invalid-data guards.
- [x] Implement `CsvPlaybackInputSource`.
- [x] Test CoP signs and centre mathematically.

Acceptance: the production CoP calculator converts simulated four-corner loads into pointer movement.

## Calibration and mapping

- [ ] Detect board occupancy.
- [ ] Record neutral stance and stability.
- [ ] Record comfortable directional ranges.
- [ ] Validate calibration quality.
- [x] Implement asymmetric normalization.
- [x] Implement dead zone with continuous remapping.
- [x] Add X/Y inversion settings.
- [x] Clamp output.
- [ ] Implement start-door forward-hold gesture.

Acceptance: two different users can safely reach the game bounds without changing code.

## Filters

- [x] Define `ICopFilter`.
- [x] Implement raw/pass-through filter.
- [x] Implement moving average or EMA.
- [x] Implement second-order Butterworth low-pass.
- [ ] Optionally implement median spike prefilter.
- [x] Reset filters between calibration and trials.
- [ ] Record all parameters.
- [x] Add filter step-response tests.

Acceptance: identical CSV input can be replayed through every condition with traceable parameter metadata.

## UDP and board bridge

- [x] Freeze version 1 packet schema.
- [x] Build an external synthetic UDP sender.
- [x] Create bridge-side raw CSV logging before UDP transmission.
- [x] Implement background UDP receive.
- [x] Publish samples safely on Unity main thread.
- [ ] Process/log all queued packets while rendering from the newest valid processed state (queue processing implemented; sample logging pending).
- [x] Detect stale, malformed, duplicate, and out-of-order packets.
- [ ] Add connection-state UI.
- [ ] Add timeout-safe pointer behaviour.
- [ ] Select/test board acquisition software.
- [ ] Verify all four physical sensors.
- [ ] Verify ML/AP signs.
- [ ] Measure actual sample rate.
- [ ] Measure source-timestamp jitter and decide whether nominal-rate Butterworth is acceptable.
- [ ] Build the dedicated external board bridge.
- [ ] Connect real board loads to UDP bridge.
- [ ] Test power-off and step-off states.

Acceptance: the dedicated bridge logs and streams live four-load packets; Unity detects packet faults, controls the same calibrated pointer pipeline, and disconnects safely.

## Logging

- [ ] Define metadata file/schema.
- [ ] Define sample CSV schema.
- [ ] Define event CSV schema.
- [ ] Implement unique trial filenames.
- [ ] Use in-progress and clean-close file handling.
- [ ] Display saved path and counts.
- [ ] Record build or commit identifier.
- [ ] Validate source, receive, event, and response timestamps.

Acceptance: a full pilot produces raw-preserving, internally consistent, analyzable files.

## VR presentation and comfort

- [x] Build a simple static high-contrast environment with clear depth cues.
- [ ] Add lobby instructions and calibration visuals.
- [ ] Add the glowing start door.
- [ ] Add restrained audio and burst effects.
- [ ] Optionally add 30/60-second environment transitions after all measured-session requirements pass.
- [ ] Test headset standing height.
- [ ] Run a short comfort pilot with a spotter.
- [ ] Confirm desktop fallback.

Acceptance: the pilot participant completes the round comfortably without target visibility problems.

## Experiment

- [ ] Finalize filter parameters after pilot recordings.
- [ ] Generate multiple matched target sequences.
- [ ] Plan counterbalanced condition order.
- [ ] Create operator/session checklist.
- [ ] Run full pilot experiment.
- [x] Confirm at least five measured minutes per participant; four 90-second rounds provide six minutes total.
- [ ] Acquire formal trials.
- [ ] Preserve raw data unchanged.
- [ ] Record exclusions and technical failures.

Acceptance: the dataset supports the planned filter comparison.

## Analysis and presentation

- [ ] Plot four corner loads.
- [ ] Plot raw and filtered ML/AP.
- [ ] Plot CoP trajectories.
- [ ] Calculate spectrum/PSD where useful.
- [ ] Calculate jitter and path length.
- [ ] Calculate response delay and settling time.
- [ ] Calculate hit rate, misses, response time, and path efficiency.
- [ ] Compare filters with variability/error bars.
- [ ] Capture setup photographs and video.
- [ ] Build and verify release.
- [ ] Save backup build and recorded demonstration.
- [ ] Prepare and rehearse 15-minute presentation.

Acceptance: results, limitations, member contributions, and a reliable demonstration are presentation-ready.

## Current next action

Integrate the implemented synthetic/CSV/UDP sources into an operator-facing input selector and add high-rate sample/metadata logging. Then build the guided CalibrationLobby flow around the implemented calibration profile and mapping functions. Physical headset and Wii Balance Board validation remains deferred until hardware details are available.

