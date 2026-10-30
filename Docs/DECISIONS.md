# Decisions

This file records approved architectural and experimental decisions. Add a dated entry whenever a material decision changes. Do not rewrite history; mark an old decision superseded and link the replacement.

## D001: Forward-only target volume

**Status:** Accepted

**Decision:** Balloons spawn only in a bounded three-dimensional region ahead of the participant.

**Reason:** The participant must not turn physically on the Wii Balance Board or search behind while wearing a headset.

**Consequence:** The environment may look large or infinite, but required targets remain inside controlled forward bounds.

## D002: Stationary assessed camera

**Status:** Accepted

**Decision:** CoP does not rotate or translate the VR camera. The headset supplies natural head tracking only.

**Reason:** Lean-controlled yaw gives ML two meanings, complicates analysis, increases discomfort, and raises physical risk.

**Consequence:** Depth and motion are created by balloons approaching the player and by scenery, not player locomotion.

## D003: CoP mapping

**Status:** Accepted

**Decision:** ML maps to horizontal pointer position. AP maps to vertical pointer position.

**Reason:** This provides intuitive two-axis control without camera locomotion or ambiguous depth control.

## D004: Start door

**Status:** Accepted

**Decision:** Calibration occurs in a lobby. A glowing door labelled **Start Balloon Challenge** is confirmed by a moderate forward lean held for approximately one second, followed by a fade transition.

**Reason:** The door makes the start understandable and visually engaging while avoiding virtual walking.

## D005: Round duration and difficulty

**Status:** Accepted

**Decision:** Each scored round lasts 90 seconds with three 30-second difficulty phases.

**Reason:** The duration is long enough for performance data but short enough to reduce fatigue and discomfort.

## D006: Randomness and reproducibility

**Status:** Accepted

**Decision:** Practice and showcase rounds may use new random hidden seeds. Filter comparisons use several matched hidden sequences, randomized assignments, and counterbalanced order where practical.

**Reason:** Repeating one obvious sequence can cause learning, while uncontrolled random trials can create unfair difficulty differences.

**Consequence:** Every seed and sequence ID is logged but not shown to the participant.

## D007: GitHub collaboration

**Status:** Accepted

**Decision:** Use a private GitHub repository with Git LFS instead of Unity Version Control.

**Reason:** The project is code-heavy, the team needs standard collaboration, and GitHub works naturally with VS Code and Codex.

**Consequence:** Scene ownership and prefab separation are required to reduce Unity YAML conflicts.

## D008: Direct VR development with fallbacks

**Status:** Accepted

**Decision:** Start from the Unity VR template, while retaining Game view and XR simulator workflows.

**Reason:** The schedule is short, but headset access may be intermittent.

## D009: Input abstraction

**Status:** Accepted

**Decision:** Keyboard, synthetic four-load, CSV playback, and UDP sources implement one common input contract.

**Reason:** Most development must continue without the physical board, and recorded trials must be reproducible.

## D010: UDP data path

**Status:** Accepted

**Decision:** Use a versioned UDP packet from the external acquisition bridge to Unity, including sequence numbers and source timestamps.

**Reason:** For continuous control, the latest sample matters more than retransmission of stale packets.

## D011: Raw-data preservation

**Status:** Accepted

**Decision:** Save four raw/calibrated corner loads and timestamps alongside all derived signals and events.

**Reason:** CoP, sensor orientation, alternative filters, sample timing, and total load cannot be verified from filtered CoP alone.

## D012: Live filter causality

**Status:** Accepted

**Decision:** Live control uses causal filtering only.

**Reason:** Zero-phase forward-backward filtering requires future samples and cannot represent real-time control.

## D013: Feature exclusions

**Status:** Accepted

**Decision:** Exclude lean-controlled camera rotation, multiplayer, online accounts, embedded OpenAI APIs, and complex procedural world systems from the assessed build.

**Reason:** They increase risk without improving the central signal-processing investigation.

## D014: Dedicated live acquisition bridge

**Date:** 2026-10-24

**Status:** Accepted

**Decision:** The physical path is Wii Balance Board to a dedicated external bridge to versioned UDP to `UdpCopInputSource`. The bridge logs every acquired four-load packet with a monotonic timestamp and sequence number before transmission. The Wii Balance Board Recorder may support calibration or independent recording but is not assumed to provide live Unity streaming.

**Reason:** Live transport was previously unspecified, and Unity must remain isolated from Bluetooth/library details while preserving complete raw acquisition data.

## D015: Four measured rounds per participant

**Date:** 2026-10-24

**Status:** Accepted

**Decision:** Each participant completes four 90-second measured rounds, providing six minutes of measured gameplay data, with rests as required.

**Reason:** The confirmed requirement is at least five measured minutes per participant; four 90-second rounds provide six minutes total.

**Clarification (2026-10-24):** The requirement is confirmed as at least five measured minutes per participant. Four 90-second rounds provide six minutes and satisfy it; no separate five-minute requirement per filter condition is currently required.

## D016: Retained scientific and runtime scope

**Date:** 2026-10-24

**Status:** Accepted

**Decision:** Retain the four-scene structure, small balloon/effect pool, matched hidden sequences, and core CoP/filter tests. Use a simple static high-contrast environment and defer elaborate scenery and timed scenery transformations.

**Reason:** Pooling, reproducible trials, and mathematical tests protect VR stability and experimental validity; scenery is the safer schedule reduction.

## D017: Timing-aware filter reporting

**Date:** 2026-10-24

**Status:** Accepted

**Decision:** EMA uses source-timestamp `deltaTime`. Butterworth coefficients use a measured nominal rate; material jitter requires resampling or selection of EMA rather than per-packet coefficient redesign. Filter phase/group delay, transport latency, and gameplay response delay are reported separately.

**Reason:** A time constant is not a universal fixed delay, and fixed-rate IIR assumptions must be explicit.

## D018: No formal role or scene ownership assignments

**Date:** 2026-10-24

**Status:** Accepted

**Decision:** Do not maintain formal member-role or scene-ownership assignments. Keep changes small, prefab-oriented, and reviewable to control Unity merge risk.

**Reason:** The project owner explicitly removed role and scene-ownership assignment as a planning blocker.

**Consequences:** Contributors must still coordinate before simultaneous scene edits and prefer isolated prefabs and additive scenes.

## D019: Four-scene baseline and build order

**Date:** 2026-10-24

**Status:** Accepted

**Decision:** Use four independently serialized scenes enabled in build order: `Bootstrap`, `CalibrationLobby`, `Gameplay`, and `Results`. Initial scene creation is performed through Unity editor APIs, with a repeatable editor menu command, rather than by hand-editing scene YAML.

**Reason:** The fixed scene boundaries match the approved architecture, keep future changes reviewable, and reduce the risk of invalid Unity serialization.

**Consequences:** `Bootstrap` remains build index zero. Scene-specific systems are added in later reviewable milestones; this baseline does not yet claim runtime scene transitions or Play Mode gameplay behavior.

## D020: Minimal stationary XR Origin

**Date:** 2026-10-24

**Status:** Accepted

**Decision:** Use one reusable stationary XR Origin prefab in `CalibrationLobby` and `Gameplay`. Keep the origin at world zero, request floor tracking, apply headset pose only to its child camera, and include no locomotion provider. Retain a 1.65 m editor fallback camera height for non-XR inspection.

**Reason:** Assessed movement must come from the body-controlled pointer while natural headset movement remains available; moving the rig would confound control and increase comfort risk.

**Consequences:** XR Simulation and physical-headset checks must confirm head tracking without origin displacement. Physical standing height and headset-runtime behavior remain unverified until hardware is available.

## D021: Editor-only XR Interaction Simulator

**Date:** 2026-10-24

**Status:** Accepted

**Decision:** Import the official XR Interaction Simulator sample matching XR Interaction Toolkit 3.6.1 and automatically instantiate it only in the Unity Editor. Do not include the simulator in standalone experimental builds.

**Reason:** Keyboard-and-mouse headset simulation supports development and regression testing without hardware while keeping measured builds free of simulator input and UI.

**Consequences:** Automated PlayMode validation may inject deterministic simulated HMD device state. Manual simulator controls remain an Editor workflow, and physical headset tracking, standing height, and comfort still require hardware validation.

## D022: Visible integrated milestone scenes

**Date:** 2026-10-24

**Status:** Accepted

**Decision:** Integrate each vertical-slice prefab into `Gameplay` when its milestone is completed and keep a simple high-contrast stage visible during development. Remove the obsolete VR-template `SampleScene` and `BasicScene` so Unity no longer reopens an unrelated demonstration environment.

**Reason:** A standalone prefab or empty scene can be technically correct but gives the project owner no visible way to review progress. Immediate scene integration makes each milestone observable and testable.

**Consequences:** Gameplay remains the review scene throughout the vertical slice. Reusable template assets may remain available, but obsolete template scenes are not retained.

## D023: Desktop-playable practice loop before research integration

**Date:** 2026-10-24

**Status:** Accepted

**Decision:** Keep a complete desktop-testable practice loop in `Gameplay`: explicit Start and Restart buttons, a 90-second round, keyboard-controlled pointer, randomized forward balloon spawning, pooled balloons and burst effects, scoring, misses, and a bright static environment. Treat its current random seed and score display as practice functionality, not as a measured-trial implementation.

**Reason:** The core interaction must be visible, playable, and regression-testable before adding calibration, filtering, networking, and experiment logging.

**Consequences:** Gameplay can be reviewed without a headset or Wii Balance Board. Measured trials remain blocked until deterministic/matched sequences, complete metadata and event logging, calibration, live input, and hardware validation are implemented.

## D024: Provisional practice event log

**Date:** 2026-10-25

**Status:** Accepted

**Decision:** Keep gameplay event persistence in a separate `TrialEventLogger`. Practice rounds write uniquely named CSV files using a monotonic clock and record the active hidden practice seed, balloon identity, target position, approach speed, score, and miss count. Files retain an `.inprogress` suffix until a clean round completion.

**Reason:** The vertical slice needs observable, testable event data now without coupling gameplay rules to file I/O or prematurely claiming the complete measured-trial schema.

**Consequences:** Practice spawn/hit/miss data is recoverable and regression-tested. This provisional file is not a substitute for the later sample log and metadata schema, raw bridge log, input/filter/calibration parameters, build identifier, or sequence ID required for measured trials.

## D025: Separate fixed desktop view, simulated head look, and pointer control

**Date:** 2026-10-25

**Status:** Accepted

**Decision:** Keep the default desktop gameplay camera fixed at the interaction-plane centre height and facing directly forward. WASD/arrows control only the pointer. Provide an Editor-only HUD toggle that permits simulated head rotation using right-mouse dragging while keeping camera position and the XR Origin fixed. Do not couple pointer position to camera pose, and do not suppress natural tracking on a physical headset.

**Reason:** A stale simulated pose placed the viewpoint below and behind the interaction rectangle, making balloon positions difficult to judge. Separating the three controls restores a predictable centred desktop view without introducing visually induced motion or confounding CoP control with head motion.

**Consequences:** Desktop practice has a stable default viewpoint and an explicit optional look mode. Assessed VR retains a stationary origin, natural headset tracking, and independent CoP pointer control. Physical-headset presentation and comfort still require hardware validation.

## D026: Game-owned desktop input settings window

**Date:** 2026-10-25

**Status:** Accepted

**Decision:** Replace reliance on the XR Interaction Simulator's small input-selection overlay during keyboard practice with a large game-owned `Input & View Settings` window. Y on the keyboard or the north/Y gamepad button toggles it. The window contains the optional desktop head-look setting and pauses the timer, targets, and pointer while open.

**Reason:** The simulator overlay describes simulated XR devices rather than the game's controls, is difficult to read, and remains confusing after simulator movement is deliberately disabled to protect the fixed camera.

**Consequences:** Desktop users get one readable settings surface and cannot lose round time while changing the view option. This Editor-only convenience does not alter assessed VR input or physical headset tracking.

## D027: Fixed, balance-board-friendly difficulty phases

**Date:** 2026-10-26

**Status:** Accepted

**Decision:** Use a three-second countdown followed by three fixed 30-second phases. Increase target frequency and speed modestly at phase boundaries while retaining forward bounds, minimum active-target spacing, and a maximum consecutive target-position jump. Burst colour matches the balloon colour.

**Reason:** A participant controlling the pointer through body lean needs predictable, reachable challenges rather than rapid or haphazard target changes. Fixed phases also preserve experimental comparability.

**Consequences:** Difficulty never adapts silently during a measured round. Final values still require a physical-board comfort pilot before release.

## D028: Normalized board-independent CoP foundation

**Date:** 2026-10-26

**Status:** Accepted

**Decision:** Implement normalized four-corner CoP calculation, source-timestamp-aware causal filtering, asymmetric participant mapping, and the four required input sources before selecting a physical acquisition library. Keep physical sensor spacing and final filter parameters provisional until hardware measurement.

**Reason:** This allows deterministic development and mathematical validation without pretending that unmeasured board geometry, rate, or Bluetooth behavior is known.

**Consequences:** Synthetic, CSV, and UDP development can proceed now. Physical units, final Butterworth sample rate, live bridge library, and hardware signs remain unverified.

## D029: Laptop-only assessed build (VR deferred)

**Date:** 2026-10-30

**Status:** Accepted

**Decision:** Continue the assessed build as a laptop-displayed Unity game without requiring an HMD. Keep the same CoP gameplay mapping and forward-only spawning rules, and keep the existing input/filter/logging architecture unchanged.

**Reason:** The project team confirmed that the near-term deliverable is a laptop-playable game while still collecting live Wii-board raw data and real-time filtered control data.

**Consequences:** VR-specific comfort and headset-runtime validation are deferred. Desktop view and input behavior become the primary execution path for testing and data collection readiness. D008 is superseded for the assessed build scope while D009-D012 and D028 remain in force.

## Decision template

```markdown
## DXXX: Title

**Date:** YYYY-MM-DD

**Status:** Proposed | Accepted | Superseded

**Decision:**

**Reason:**

**Consequences:**

**Supersedes/Superseded by:**
```
