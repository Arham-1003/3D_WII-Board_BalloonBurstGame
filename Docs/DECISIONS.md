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

