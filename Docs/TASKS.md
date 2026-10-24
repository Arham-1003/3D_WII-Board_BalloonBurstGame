# Tasks

This is the implementation tracker for future chats and team work. Check an item only after its acceptance condition has been verified. Add a short note or commit reference for material completions.

## Planning and access

- [x] Approve the forward-only stationary-camera game concept.
- [x] Create the full PDF development plan.
- [x] Create portable repository context documents.
- [ ] Select and record the exact Unity LTS version.
- [ ] Record the headset model/runtime and availability.
- [ ] Record the Wii Balance Board access date.
- [ ] Assign four member roles and scene ownership.

## Repository and Unity foundation

- [ ] Create a private GitHub repository.
- [ ] Install/configure Git LFS.
- [ ] Add Unity `.gitignore` and `.gitattributes`.
- [ ] Create the Unity VR template project.
- [ ] Copy this documentation package to the repository root.
- [ ] Enable Visible Meta Files.
- [ ] Enable Force Text serialization.
- [ ] Confirm required packages and versions.
- [ ] Configure OpenXR for the target runtime.
- [ ] Verify a teammate can clone and open the project.

Acceptance: the same clean project opens on two team computers without compiler errors.

## Vertical slice

- [ ] Create Bootstrap scene.
- [ ] Create CalibrationLobby scene.
- [ ] Create Gameplay scene.
- [ ] Create Results scene.
- [ ] Add stationary XR Origin.
- [ ] Import/configure XR simulator.
- [ ] Create Pointer prefab.
- [ ] Create Balloon prefab.
- [ ] Implement keyboard input source.
- [ ] Move pointer on a front plane.
- [ ] Detect balloon hit.
- [ ] Play burst feedback.
- [ ] Increment score.
- [ ] Log spawn and hit events.

Acceptance: one keyboard-simulated lean bursts one balloon in Game view and XR simulation.

## Complete round

- [ ] Implement 3D forward spawn volume.
- [ ] Enforce bounds and minimum spacing.
- [ ] Enforce consecutive target-distance limits.
- [ ] Implement random hidden seed mode.
- [ ] Implement matched sequence mode.
- [ ] Add balloon movement toward the interaction plane.
- [ ] Add miss boundary.
- [ ] Add 90-second timer.
- [ ] Add three difficulty phases.
- [ ] Add countdown, pause, score, and results UI.
- [ ] Add balloon and effect pooling.

Acceptance: a 90-second round produces complete spawn/hit/miss/result data and never spawns behind.

## Input and CoP

- [ ] Define immutable/read-only `CopSample`.
- [ ] Define `ICopInputSource`.
- [ ] Implement `KeyboardCopInputSource`.
- [ ] Implement `SyntheticBoardInputSource`.
- [ ] Add noise, drift, spike, loss, and timing controls.
- [ ] Implement CoP calculation.
- [ ] Add low-total-load and invalid-data guards.
- [ ] Implement `CsvPlaybackInputSource`.
- [ ] Test CoP signs and centre mathematically.

Acceptance: the production CoP calculator converts simulated four-corner loads into pointer movement.

## Calibration and mapping

- [ ] Detect board occupancy.
- [ ] Record neutral stance and stability.
- [ ] Record comfortable directional ranges.
- [ ] Validate calibration quality.
- [ ] Implement asymmetric normalization.
- [ ] Implement dead zone with continuous remapping.
- [ ] Add X/Y inversion settings.
- [ ] Clamp output.
- [ ] Implement start-door forward-hold gesture.

Acceptance: two different users can safely reach the game bounds without changing code.

## Filters

- [ ] Define `ICopFilter`.
- [ ] Implement raw/pass-through filter.
- [ ] Implement moving average or EMA.
- [ ] Implement second-order Butterworth low-pass.
- [ ] Optionally implement median spike prefilter.
- [ ] Reset filters between calibration and trials.
- [ ] Record all parameters.
- [ ] Add filter step-response tests.

Acceptance: identical CSV input can be replayed through every condition with traceable parameter metadata.

## UDP and board bridge

- [ ] Freeze version 1 packet schema.
- [ ] Build an external synthetic UDP sender.
- [ ] Implement background UDP receive.
- [ ] Publish samples safely on Unity main thread.
- [ ] Detect stale, malformed, duplicate, and out-of-order packets.
- [ ] Add connection-state UI.
- [ ] Add timeout-safe pointer behaviour.
- [ ] Select/test board acquisition software.
- [ ] Verify all four physical sensors.
- [ ] Verify ML/AP signs.
- [ ] Measure actual sample rate.
- [ ] Connect real board loads to UDP bridge.
- [ ] Test power-off and step-off states.

Acceptance: live board lean controls the same calibrated pointer pipeline; disconnection is safe.

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

- [ ] Build attractive high-contrast scenery.
- [ ] Add lobby instructions and calibration visuals.
- [ ] Add the glowing start door.
- [ ] Add restrained audio and burst effects.
- [ ] Add 30/60-second environment transitions.
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
- [ ] Verify all required recording durations.
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

Select the Unity LTS version and headset target, create the private GitHub repository, and generate the Unity VR template project. Then start a new Codex chat using [NEW_CHAT_HANDOFF.md](NEW_CHAT_HANDOFF.md).

