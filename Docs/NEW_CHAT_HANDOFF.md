# New Chat Handoff

## Short prompt

Copy and paste this after opening the future Unity repository root in VS Code:

```text
This is the Balloon Balance VR SSRP project. Before changing anything, read AGENTS.md and the relevant files under Docs, especially PROJECT_CONTEXT.md, ARCHITECTURE.md, DECISIONS.md, UNITY_SETUP.md, SIGNAL_PROCESSING.md, and TASKS.md. Inspect the repository and Unity project state, then identify the first unfinished task and propose the smallest verifiable next step. Preserve keyboard, synthetic four-load, CSV playback, and UDP input paths. Preserve the dedicated external board bridge boundary, bridge-side raw logging, four-scene flow, pooling, matched hidden sequences, and core signal-processing tests. Do not add CoP-controlled camera movement. Clearly mark anything that cannot be verified without the Wii Balance Board or VR headset.
```

## First implementation prompt

Use this if the Unity VR template has just been created:

```text
Read AGENTS.md and all relevant Docs files. This is a newly created Unity VR template project. First inspect the exact Unity and package versions, existing scenes, repository status, and Console/build configuration. Update Docs/UNITY_SETUP.md with the verified versions. Then implement only the first vertical slice: a stationary XR scene, keyboard-simulated CoP input, a visible pointer on a forward plane, one balloon with trigger collision, burst feedback, score increment, and spawn/hit logging. Keep input behind ICopInputSource. Do not implement random spawning, real board integration, or visual polish until the vertical slice is verified.
```

## Continuation prompt

Use this when development has already started:

```text
Read AGENTS.md, Docs/DECISIONS.md, and Docs/TASKS.md. Inspect current changes, Unity structure, tests, and recent errors. Summarize what is complete versus unverified, including hardware-dependent items. Continue from the first unfinished acceptance gate in TASKS.md. Make a focused change, verify it in proportion to risk, and update TASKS.md and DECISIONS.md if the implementation changes project status or architecture.
```

## Debugging prompt

```text
Read AGENTS.md and the architecture/signal-processing documentation before diagnosing this problem. Preserve separation between acquisition, input source, filtering, calibration, mapping, gameplay, and logging. Reproduce with keyboard, synthetic, or CSV input when possible before depending on hardware. Explain the root cause, implement the smallest scoped fix I authorize, run relevant checks, and state what still requires Unity Play mode, headset, or board verification.
```

## Information to include with the new chat

If known, add:

- Unity editor version;
- target headset and OpenXR runtime;
- operating system;
- current Git branch;
- current milestone;
- Unity Console errors copied as text;
- whether the board and headset are physically available;
- whether the bridge is running with the synthetic sender or physical board;
- screenshots only when visual state matters;
- exact reproduction steps for a bug.

## Expected opening response

The new agent should:

1. confirm it read the repository instructions;
2. summarize the current architecture and next task;
3. identify missing version or hardware facts;
4. avoid broad rewrites before inspecting existing work;
5. distinguish code-complete from Unity-, headset-, and board-verified status.

