# Project Context

## Course project

This repository supports SSRP Project 3: **Real-Time CoP Signal Processing for Body-Controlled Balloon-Bursting Using Wii Balance Board and Unity**.

The project requires the team to:

- acquire real-time Center of Pressure signals in anterior-posterior and mediolateral directions;
- identify signal noise and unwanted fluctuations;
- implement and compare appropriate causal digital filters;
- control a Unity virtual object using the processed signals;
- evaluate movement smoothness, response delay, accuracy, and game performance;
- record at least the required duration of real signals for each assigned activity or condition;
- present figures, plots, tables, analysis, a game demonstration, and member contributions.

The original planning sources are the course project DOCX, the sensor-systems guide workbook, and the Wii Balance Board reference listed in the final section of this file.

## Game concept

Working title: **Balloon Balance VR**.

The participant starts in a calibration lobby. After neutral and directional calibration, a glowing door shows **Start Balloon Challenge**. A moderate forward lean held for approximately one second confirms the start. The scene fades into a visually rich environment such as a magical garden, floating-island landscape, or moonlit forest.

The challenge lasts 90 seconds. The participant remains stationary on the Wii Balance Board. A pointer or magical bursting ring moves across a plane in front of the participant:

- left/right lean controls horizontal position;
- forward/backward lean controls vertical position;
- natural head movement is tracked by the headset;
- CoP never rotates or translates the VR camera in the assessed mode.

Balloons spawn inside a bounded three-dimensional volume ahead of the player, move toward an interaction plane, and burst when they collide with the pointer. A missed balloon is one that crosses the interaction plane or miss boundary without a valid hit.

## Why the camera is stationary

Lean-controlled camera turning was rejected because it would give the ML signal two meanings, introduce threshold/mode-switching problems, complicate filter evaluation, and increase VR discomfort and physical risk while standing on the board.

The environment may look infinite, but the gameplay volume remains controlled. Depth comes from approaching balloons, perspective, scenery, lighting, and natural head parallax.

## Round structure

1. Safety and equipment check.
2. Board zeroing and occupancy detection.
3. Neutral stance recording.
4. Comfortable left, right, forward, and backward calibration.
5. Random practice round not used in filter comparison.
6. Four 90-second measured rounds per participant, with rest between rounds, providing six minutes of measured gameplay data.
7. Results and optional random showcase round.

Difficulty progression:

- **0-30 seconds:** large balloons, central bias, slow approach.
- **30-60 seconds:** wider distribution, varied size, moderate approach.
- **60-90 seconds:** full safe bounds, smaller balloons, faster movement, limited multi-target events.

## Randomness policy

A seed is hidden and is never disclosed to the player. It exists to reproduce a trial.

- Practice rounds use new random seeds.
- Filter-comparison trials use several matched sequences with comparable position, size, speed, and difficulty distributions.
- Sequence assignment and filter order are counterbalanced when practical.
- Showcase rounds can be fully random, but their seed is still logged.

## Primary success criteria

- The game works in desktop, XR simulator, and headset modes.
- Keyboard, synthetic four-load, CSV playback, and live UDP inputs share one downstream pipeline.
- The participant can calibrate without developer intervention.
- Every balloon is ahead and inside safe documented bounds.
- Raw loads, timestamps, CoP, filtered CoP, pointer state, and events are logged.
- Raw/minimal, simple smoothing, and Butterworth conditions can be compared.
- Signal loss and low-load states are safe and visible.
- A fresh clone can be built from documented instructions.

## Constraints

- The team is short on time, so the vertical slice and data path take priority over decorative features.
- The assessed environment should be simple, comfortable, and high-contrast. Elaborate scenery and timed scenery transformations are deferred until the full data path is reliable.
- A physical board is eventually required for final validation and real data collection.
- A headset may not always be available; XR and input simulation must remain functional.
- Natural postural sway is not automatically noise and must be interpreted carefully.
- The confirmed requirement is at least five measured minutes per participant, satisfied by four 90-second measured rounds for six minutes total.

## Sources

- Course project: `SPRP_Project_2024.docx`
- Sensor guide: `ES244_Sensor_Systems_Student_Guide.xlsx`
- Wii Balance Board: <https://en.wikipedia.org/wiki/Wii_Balance_Board>
- Suggested recorder: <https://github.com/tomvredeveld/wii-balance-board-recorder>
- Unity Essentials: <https://learn.unity.com/pathway/unity-essentials>
- XR Interaction Toolkit: <https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.1/manual/index.html>

