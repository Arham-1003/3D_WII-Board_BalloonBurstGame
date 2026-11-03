# Wii Balance Board Recorder to Unity bridge

This Python 3 program tails the official Wii Balance Board Recorder TXT file,
writes an audit CSV containing every observed source row, and sends valid
four-load packets to Unity at `127.0.0.1:5005`.

It intentionally does not filter, calibrate the participant, or move the
pointer. Unity owns those responsibilities and applies the single Raw, Moving
Average, EMA, Butterworth, or Median condition selected for the participant.

## Required input format

The default is the Recorder's documented 12-column, whitespace-delimited file:

```text
Time CpX CpY BL BR TL TR Weight RAWBL RAWBR RAWTL RAWTR
```

`TL`/`TR` are transmitted as front-left/front-right. `BL`/`BR` are transmitted
as rear-left/rear-right. Confirm this physical orientation with the real board
before formal trials.

The supplied legacy `WiiMote1_All.txt` has ten undocumented columns. The bridge
rejects that format by default because guessing its sensor order could reverse
game controls and corrupt research data. It can be used only after the TA
confirms the four column indexes, through `--format custom` and the explicit
column options shown by `--help`.

## Start it

1. Pair the board with WiiBalanceWalker.
2. Open Wii Balance Board Recorder and complete its zero and known-weight
   calibration.
3. Choose the Recorder output folder and filename.
4. Open Unity's `Gameplay` scene and press Play.
5. Press Y to open `Input & View Settings`, choose `Live Wii Board (UDP)`, select the participant's assigned control filter, choose `Fixed Matched Sequences` for measured trials or `Random Sequence` for practice, and confirm the status changes from `WAITING / STALE` to `CONNECTED` after packets arrive. Press Y again to close the paused settings window.
6. In PowerShell, change to this folder and run:

```powershell
python .\balance_board_bridge.py --input "C:\path\to\RecorderOutput.txt"
```

7. Start recording in the Recorder. New complete rows will be logged and sent
   to Unity. The default behavior ignores old rows already in the file.
8. Stop the bridge with Ctrl+C after the trial.

For a safe playback test of an existing Recorder file:

```powershell
python .\balance_board_bridge.py --input "C:\path\to\RecorderOutput.txt" --from-start
```

Each run creates `BridgeLogs/bridge_raw_YYYYMMDD_HHMMSS.csv`. This is the
bridge-side authoritative audit log and must be retained with the Unity trial
files.

When a practice round completes, Unity creates a separate `.samples.csv` under
`Application.persistentDataPath/TrialLogs`. It contains the source and receive
timestamps, all four raw loads, total load, validity/connection state, the
raw CoP, selected filter output, control output, active filter, round-sequence
mode, and hidden seed. The stable five-condition columns use `NaN` for inactive
filtered conditions while raw loads and raw CoP are always retained. An unfinished run keeps the `.inprogress` suffix so it is
never mistaken for a cleanly completed trial.

## Validate the bridge

```powershell
python -m unittest -v
python .\balance_board_bridge.py --help
```

Hardware-dependent checks still required are the physical corner order,
forward/right signs, actual acquisition rate, timestamp jitter, disconnect
behavior, and whether Recorder output is flushed quickly enough for live play.
