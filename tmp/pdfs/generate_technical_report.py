from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER, TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.units import mm
from reportlab.platypus import (
    SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle, PageBreak,
    KeepTogether, HRFlowable
)
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfgen import canvas
from pathlib import Path


OUTPUT = Path(r"C:\ARHAM\College Downloads\Sem 3\SSRP\Unity\My project\output\pdf\Balloon_Balance_Technical_Creation_and_Integration.pdf")
OUTPUT.parent.mkdir(parents=True, exist_ok=True)

NAVY = colors.HexColor("#102A43")
BLUE = colors.HexColor("#176B87")
CYAN = colors.HexColor("#33B5C8")
PALE = colors.HexColor("#EAF7FA")
GOLD = colors.HexColor("#F2B84B")
INK = colors.HexColor("#243B53")
MUTED = colors.HexColor("#627D98")
RED = colors.HexColor("#B42318")
GREEN = colors.HexColor("#137A5B")
LIGHT = colors.HexColor("#F5F8FA")


class NumberedCanvas(canvas.Canvas):
    def __init__(self, *args, **kwargs):
        canvas.Canvas.__init__(self, *args, **kwargs)
        self._saved_page_states = []

    def showPage(self):
        self._saved_page_states.append(dict(self.__dict__))
        self._startPage()

    def save(self):
        page_count = len(self._saved_page_states)
        for state in self._saved_page_states:
            self.__dict__.update(state)
            self.draw_header_footer(page_count)
            canvas.Canvas.showPage(self)
        canvas.Canvas.save(self)

    def draw_header_footer(self, page_count):
        width, height = A4
        if self._pageNumber > 1:
            self.setStrokeColor(colors.HexColor("#D9E2EC"))
            self.line(18 * mm, height - 14 * mm, width - 18 * mm, height - 14 * mm)
            self.setFillColor(MUTED)
            self.setFont("Helvetica", 8)
            self.drawString(18 * mm, height - 11 * mm, "BALLOON BALANCE - TECHNICAL CREATION AND INTEGRATION")
        self.setFillColor(MUTED)
        self.setFont("Helvetica", 8)
        self.drawString(18 * mm, 10 * mm, "SSRP Project 3 | Prepared from repository and acquired data")
        self.drawRightString(width - 18 * mm, 10 * mm, f"{self._pageNumber} / {page_count}")


styles = getSampleStyleSheet()
styles.add(ParagraphStyle(
    name="TitleHero", parent=styles["Title"], fontName="Helvetica-Bold",
    fontSize=28, leading=32, textColor=colors.white, alignment=TA_LEFT,
    spaceAfter=8
))
styles.add(ParagraphStyle(
    name="SubtitleHero", parent=styles["Normal"], fontName="Helvetica",
    fontSize=12, leading=17, textColor=colors.HexColor("#D9F2F5")
))
styles.add(ParagraphStyle(
    name="H1x", parent=styles["Heading1"], fontName="Helvetica-Bold",
    fontSize=18, leading=22, textColor=NAVY, spaceBefore=8, spaceAfter=9
))
styles.add(ParagraphStyle(
    name="H2x", parent=styles["Heading2"], fontName="Helvetica-Bold",
    fontSize=12.5, leading=16, textColor=BLUE, spaceBefore=8, spaceAfter=5
))
styles.add(ParagraphStyle(
    name="Bodyx", parent=styles["BodyText"], fontName="Helvetica",
    fontSize=9.3, leading=13.4, textColor=INK, spaceAfter=6
))
styles.add(ParagraphStyle(
    name="Smallx", parent=styles["BodyText"], fontName="Helvetica",
    fontSize=7.8, leading=10.5, textColor=INK, spaceAfter=3
))
styles.add(ParagraphStyle(
    name="Callout", parent=styles["BodyText"], fontName="Helvetica-Bold",
    fontSize=9.2, leading=13, textColor=NAVY, leftIndent=8, rightIndent=8,
    spaceBefore=4, spaceAfter=4
))
styles.add(ParagraphStyle(
    name="CodeBlock", parent=styles["Code"], fontName="Courier",
    fontSize=7.4, leading=10, textColor=colors.HexColor("#102A43"),
    backColor=colors.HexColor("#EDF2F7"), borderPadding=6, spaceBefore=4, spaceAfter=7
))
styles.add(ParagraphStyle(
    name="Bulletx", parent=styles["BodyText"], fontName="Helvetica",
    fontSize=9, leading=13, textColor=INK, leftIndent=13, firstLineIndent=-7,
    bulletIndent=4, spaceAfter=3
))
styles.add(ParagraphStyle(
    name="Caption", parent=styles["BodyText"], fontName="Helvetica-Oblique",
    fontSize=7.5, leading=10, textColor=MUTED, alignment=TA_CENTER, spaceAfter=6
))


def P(text, style="Bodyx"):
    return Paragraph(text, styles[style])


def bullet(text):
    return Paragraph("- " + text, styles["Bulletx"])


def callout(text, color=PALE, border=CYAN):
    t = Table([[P(text, "Callout")]], colWidths=[170 * mm])
    t.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, -1), color),
        ("BOX", (0, 0), (-1, -1), 0.8, border),
        ("LEFTPADDING", (0, 0), (-1, -1), 7),
        ("RIGHTPADDING", (0, 0), (-1, -1), 7),
        ("TOPPADDING", (0, 0), (-1, -1), 5),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 5),
    ]))
    return t


def table(data, widths, header=True, font=7.2):
    formatted = []
    for r, row in enumerate(data):
        formatted.append([P(str(v), "Smallx") for v in row])
    t = Table(formatted, colWidths=widths, repeatRows=1 if header else 0, hAlign="LEFT")
    commands = [
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("GRID", (0, 0), (-1, -1), 0.35, colors.HexColor("#BCCCDC")),
        ("LEFTPADDING", (0, 0), (-1, -1), 4),
        ("RIGHTPADDING", (0, 0), (-1, -1), 4),
        ("TOPPADDING", (0, 0), (-1, -1), 4),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 4),
        ("ROWBACKGROUNDS", (0, 1), (-1, -1), [colors.white, LIGHT]),
    ]
    if header:
        commands += [
            ("BACKGROUND", (0, 0), (-1, 0), NAVY),
            ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
            ("FONTNAME", (0, 0), (-1, 0), "Helvetica-Bold"),
        ]
        for cell in t._cellvalues[0]:
            cell.textColor = colors.white
    t.setStyle(TableStyle(commands))
    return t


story = []

# Cover
hero = Table([[
    P("BALLOON BALANCE", "TitleHero"),
], [
    P("How the Wii Balance Board, Python UDP bridge, Unity signal processing, calibration, gameplay and logging were built into one real-time system", "SubtitleHero")
]], colWidths=[170 * mm], rowHeights=[38 * mm, 35 * mm])
hero.setStyle(TableStyle([
    ("BACKGROUND", (0, 0), (-1, -1), NAVY),
    ("LEFTPADDING", (0, 0), (-1, -1), 14),
    ("RIGHTPADDING", (0, 0), (-1, -1), 14),
    ("TOPPADDING", (0, 0), (-1, -1), 12),
    ("BOTTOMPADDING", (0, 0), (-1, -1), 12),
    ("LINEBELOW", (0, 0), (-1, 0), 3, CYAN),
]))
story += [Spacer(1, 20 * mm), hero, Spacer(1, 10 * mm)]
story.append(callout(
    "Purpose: provide report-ready, implementation-specific details for SSRP Project 3. This document is based on the current source code and the four labelled participant datasets collected on 5 November 2026.",
    colors.HexColor("#FFF7E6"), GOLD
))
story += [Spacer(1, 8 * mm)]
story.append(table([
    ["System", "Implementation"],
    ["Game engine", "Unity 6.6, editor 6000.6.6f1"],
    ["Physical input", "Nintendo Wii Balance Board through WiiBalanceWalker and Wii Balance Board Recorder"],
    ["Transport", "Python 3 file-tail bridge to versioned UDP on 127.0.0.1:5005"],
    ["Processing", "C# CoP calculation, seven causal live filter choices, calibration and mapping"],
    ["Measured protocol", "4 participants x 4 rounds x 90 seconds = 24 measured minutes total"],
], [40 * mm, 130 * mm]))
story += [Spacer(1, 8 * mm), P("Prepared 6 November 2026", "Caption"), PageBreak()]

# Executive summary
story += [P("1. What was created", "H1x")]
story.append(P(
    "Balloon Balance is a stationary body-controlled balloon-bursting game. A participant stands on a Wii Balance Board and shifts pressure left/right or forward/backward. Four corner loads are converted to a two-axis Center of Pressure (CoP), filtered in real time, calibrated to the participant's comfortable range, and mapped to a blue pointer. Balloons approach a fixed interaction plane; pointer contact records a hit and produces a same-color burst. The camera remains fixed for safety and experimental consistency."
))
story.append(P(
    "The implementation was deliberately split into acquisition, transport, signal processing, calibration, gameplay and logging. This allowed keyboard, synthetic-board, CSV playback and live UDP input to use the same downstream game code. It also preserved raw measurements independently of the selected gameplay filter."
))
story.append(P("System flow", "H2x"))
flow_data = [[
    P("Wii board", "Callout"), P("Recorder", "Callout"), P("Python bridge", "Callout"),
    P("Unity processing", "Callout"), P("Pointer + game", "Callout")
]]
flow = Table(flow_data, colWidths=[29*mm, 31*mm, 34*mm, 39*mm, 37*mm])
flow.setStyle(TableStyle([
    ("BACKGROUND", (0, 0), (0, 0), colors.HexColor("#E3F2FD")),
    ("BACKGROUND", (1, 0), (1, 0), colors.HexColor("#E8F5E9")),
    ("BACKGROUND", (2, 0), (2, 0), colors.HexColor("#FFF3E0")),
    ("BACKGROUND", (3, 0), (3, 0), colors.HexColor("#F3E5F5")),
    ("BACKGROUND", (4, 0), (4, 0), colors.HexColor("#E0F7FA")),
    ("BOX", (0, 0), (-1, -1), 0.6, BLUE),
    ("INNERGRID", (0, 0), (-1, -1), 0.5, colors.white),
    ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
    ("ALIGN", (0, 0), (-1, -1), "CENTER"),
]))
story += [flow, P("Four loads -> calibrated text rows -> JSON/UDP -> CoP/filter/calibration -> normalized pointer and events", "Caption")]
story.append(callout(
    "Critical clarification for the report: the Python code did not implement the gameplay filters. Python safely read, audited and transported samples. The seven live filters were implemented in Unity C# after CoP calculation.",
    colors.HexColor("#FFF4E5"), GOLD
))

story += [P("2. Unity game construction", "H1x")]
story.append(P(
    "The project started from Unity's VR template but retained desktop and XR-simulator operation. The assessed scene uses one stationary XR Origin, a fixed camera, a bounded pointer plane, forward-only balloon spawning, HUD, input settings and a simple high-contrast outdoor environment. CoP changes only pointer position; it never translates or yaws the camera."
))
story.append(P("Major gameplay components", "H2x"))
for item in [
    "BalloonGameManager owns the 90-second state machine, countdown, three difficulty phases, seed selection, score, misses and object pools.",
    "PointerController reads only the normalized ControlPosition from CopInputManager and moves the pointer inside fixed screen-space/world bounds.",
    "BalloonTarget advances toward the participant at a configured approach speed and reports hit or miss events.",
    "Eighteen balloons and eight burst effects are pooled to avoid repeated Instantiate/Destroy work during measured rounds.",
    "The paused Y settings menu selects the input source, one filter condition, fixed/random sequence mode, calibration and optional desktop head-look.",
]: story.append(bullet(item))

story.append(P("Game pacing and fairness", "H2x"))
story.append(table([
    ["Setting", "Implemented value", "Reason"],
    ["Round duration", "90 s", "Enough data without excessive fatigue"],
    ["Difficulty phases", "0-30, 30-60, 60-90 s", "Controlled progression rather than abrupt random difficulty"],
    ["Spawn intervals", "2.4, 2.1, 1.8 s", "Gradually increases target demand"],
    ["Speed multipliers", "0.85, 1.00, 1.10", "Board-friendly approach speed progression"],
    ["Base speed", "0.70-1.15 units/s", "Avoids unplayably rapid movement"],
    ["Pointer target range", "x: -1.55 to 1.55; y: 0.75 to 2.55", "Keeps all targets in the forward visible play area"],
    ["Matched seeds", "104729, 130363, 155921, 196613", "All participants receive the same four target sequences"],
], [36*mm, 48*mm, 86*mm]))
story += [PageBreak()]

# Acquisition and bridge
story += [P("3. Physical acquisition and Python bridge", "H1x")]
story.append(P(
    "WiiBalanceWalker was used to pair the board through Bluetooth. Wii Balance Board Recorder performed board-level calibration and wrote a continuously growing text file. The bridge then tailed only complete new lines from this file. This avoided putting Bluetooth/device-library code inside Unity and made the hardware boundary replaceable."
))
story.append(P("Recorder row format", "H2x"))
story.append(P("Time  CpX  CpY  BottomLeft  BottomRight  TopLeft  TopRight  Weight  RAWBottomLeft  RAWBottomRight  RAWTopLeft  RAWTopRight", "CodeBlock"))
story.append(P(
    "The bridge mapped TopLeft/TopRight to Unity front-left/front-right and BottomLeft/BottomRight to rear-left/rear-right. It preserved both calibrated loads and the recorder's raw sensor counts in a bridge audit CSV. Header rows, missing columns, non-numeric values, non-finite values and backward timestamps were rejected rather than guessed."
))
story.append(P("What the bridge did for every line", "H2x"))
for item in [
    "Parse the 12 documented columns and normalize decimal formatting.",
    "Assign a strictly increasing sequence number.",
    "Write an audit row and flush it before network transmission.",
    "Serialize protocol version, sequence, source time, four calibrated loads, total load and connection state to JSON.",
    "Send the UTF-8 packet over UDP to loopback address 127.0.0.1, port 5005.",
    "Print cumulative sent/rejected counts and stop safely with Ctrl+C.",
]: story.append(bullet(item))
story.append(P("UDP packet", "H2x"))
story.append(P('{"version":1,"sequence":205,"sourceTime":4.216,"frontLeft":16.8,"frontRight":17.1,"rearLeft":16.5,"rearRight":16.9,"totalLoad":67.3,"connected":true}', "CodeBlock"))
story.append(P(
    "UDP was chosen because this is continuous control: the newest state is more useful than retransmitting old samples. Reliability was added at the application level through sequence numbers, duplicate/out-of-order detection, missing-sequence counts, a 0.25-second stale timeout and independent disk logs."
))
story.append(P("Correct operating command", "H2x"))
story.append(P('python .\\balance_board_bridge.py --input "C:\\path\\Participant0.txt" --time-unit seconds', "CodeBlock"))
story.append(callout(
    "Use --time-unit seconds with the supplied Recorder files. Their Time column advances by about 0.05 per sample, meaning seconds at approximately 20 Hz. The bridge default is milliseconds and is not correct for these acquired files.",
    colors.HexColor("#FDECEC"), RED
))

# CoP and processing
story += [P("4. CoP calculation and validation", "H1x")]
story.append(P(
    "Unity received packets on a background thread, queued them, and processed them on the main thread. A sample was rejected when a corner value was NaN/infinite/negative or when total load was below 10. Valid samples used normalized CoP equations:"
))
story.append(P("total = FL + FR + RL + RR", "CodeBlock"))
story.append(P("CoP_ML = ((FR + RR) - (FL + RL)) / total<br/>CoP_AP = ((FL + FR) - (RL + RR)) / total", "CodeBlock"))
story.append(P(
    "These outputs are dimensionless balance ratios, not centimetres. Positive ML means relatively more load on the right; positive AP means relatively more load on the front. The raw four loads remain in every Unity sample row so signs, orientation and alternative processing can be audited later."
))
story.append(P("Common input architecture", "H2x"))
story.append(P(
    "KeyboardCopInputSource, SyntheticBoardInputSource, CsvPlaybackInputSource and UdpCopInputSource all implement ICopInputSource. CopInputManager therefore performs the same validation, filtering, calibration and logging regardless of where the sample originated. This design enabled development without hardware and reproducible playback after collection."
))

# Filters
story += [P("5. How the filters were selected and implemented", "H1x")]
story.append(P(
    "The filter list was chosen to cover distinct real-time strategies while obeying the requirement that live filtering must be causal. Raw gives a delay-free baseline; moving average gives transparent finite-window smoothing; EMA gives timestamp-aware first-order low-pass behavior; Butterworth gives a conventional second-order IIR response; median suppresses isolated spikes; adaptive low-pass trades smoothing for responsiveness based on movement speed; alpha-beta estimates position and velocity. Forward-backward zero-phase filtering was excluded because it uses future samples and cannot run live."
))
filter_rows = [
    ["Filter", "Implemented rule", "Default parameters", "Main trade-off"],
    ["Raw", "y[n] = x[n]", "None", "No algorithmic delay; maximum jitter"],
    ["Moving average", "Mean of newest N vectors", "N = 5", "Simple smoothing; window delay"],
    ["EMA", "a=1-exp(-2*pi*fc*dt); y+=a(x-y)", "fc = 2.5 Hz", "Time-aware; delayed if cutoff too low"],
    ["Butterworth", "2nd-order bilinear-transform IIR", "fc = 2.5 Hz; nominal fs = 50 Hz", "Flat passband; assumes design sample rate"],
    ["Median", "Independent median of ML and AP", "Odd window N = 3", "Rejects isolated spikes; nonlinear"],
    ["Adaptive low-pass", "One-Euro-like speed-dependent cutoff", "min fc=1.5 Hz; beta=3; derivative fc=1 Hz", "Smooth at rest; responsive in motion"],
    ["Alpha-beta", "Predict position; correct position and velocity", "alpha=0.85; beta=0.05", "Responsive prediction; may overshoot"],
]
story.append(table(filter_rows, [27*mm, 57*mm, 47*mm, 39*mm]))
story.append(P(
    "All filters operated separately on ML and AP and were reset when the source/filter changed, calibration began, or a measured condition restarted. Only the selected filter drove the pointer. The sample log still stored raw loads, raw CoP and the selected filter output, using NaN for inactive filter columns so the condition could not be confused during analysis."
))
story.append(callout(
    "The collected experiment used four of the seven options: Akshat - Median (3 samples), Alka - Moving Average (5 samples), Arham - Butterworth (2.5 Hz designed at 50 Hz), and Dharti - Alpha-Beta (alpha 0.85, beta 0.05).",
    PALE, CYAN
))

# Calibration and integration
story += [PageBreak(), P("6. Participant calibration and pointer mapping", "H1x")]
story.append(P(
    "The Y settings window guided a five-point calibration: centre, comfortable left, comfortable right, comfortable forward and comfortable backward. Each capture used a valid raw CoP sample. The system stored a neutral point and four asymmetric ranges because a participant normally cannot lean equally far in opposing directions."
))
story.append(P("For each axis:", "H2x"))
story.append(P(
    "relative = filtered_CoP - neutral<br/>positive side = relative / positive comfortable range<br/>negative side = relative / negative comfortable range<br/>apply dead zone and continuously remap remainder<br/>clamp result to [-1, +1]",
    "CodeBlock"
))
story.append(P(
    "This normalized control vector moved the pointer across its allowed rectangle. At a limit, further leaning remained clamped; it did not move the camera or extend the world. Calibration and filter parameters remained unchanged during each measured 90-second round."
))
story.append(P("End-to-end live sequence", "H2x"))
steps = [
    "Pair board in WiiBalanceWalker and confirm live corner readings.",
    "Zero/calibrate the empty board in Recorder, then select a participant-specific output file.",
    "Start the Python bridge with --time-unit seconds and start Recorder acquisition.",
    "Open Unity Gameplay, select Live Wii Board (UDP), assigned filter and Fixed Matched Sequences.",
    "Confirm CONNECTED and inspect valid standing load; calibrate centre and four comfortable directions.",
    "Run four seeds in order, allowing rest between 90-second rounds.",
    "After round completion, retain Recorder TXT, bridge audit CSV, Unity samples CSV and Unity events CSV.",
    "Copy all files into one participant-labelled dataset and preserve the unmodified originals.",
]
for i, s in enumerate(steps, 1): story.append(P(f"<b>{i}.</b> {s}"))

# Logging
story += [P("7. What was recorded and where", "H1x")]
story.append(table([
    ["Layer", "File", "Information retained", "Purpose"],
    ["Recorder", "Participant0.txt", "Time, CpX/CpY, calibrated BL/BR/TL/TR, weight, four raw counts", "Independent hardware record"],
    ["Python bridge", "bridge_raw_*.csv", "Record/sequence, bridge time, source time, four loads, total, raw counts, status, original line", "Authoritative transport audit"],
    ["Unity samples", "*.samples.csv", "Sequence, source/receive time, four loads, validity, CoP, filter output, control, condition, dt", "Signal and control analysis"],
    ["Unity events", "*.events.csv", "Round start/end, spawn/hit/miss, balloon position/speed, score, misses, seed", "Gameplay performance analysis"],
], [27*mm, 31*mm, 76*mm, 36*mm]))
story.append(P(
    "Unity first wrote .inprogress files and renamed them only after a clean round completion. Completed logs were saved under Application.persistentDataPath/TrialLogs; on Windows this resolves under the user's AppData/LocalLow company/product hierarchy. Sequence number and source timestamp provide the join keys between bridge and Unity data; event time is a Unity monotonic round-relative clock."
))

# Real data inventory
story += [P("8. Acquired dataset inventory", "H1x")]
story.append(P(
    "The supplied folder contains four labelled participant datasets. Each participant has one Recorder raw text file, one main bridge audit file, and four paired Unity sample/event logs. Therefore the supplementary submission can be organized as four top-level datasets - one per team member - while each dataset contains four measured rounds."
))
story.append(table([
    ["Participant", "Assigned filter", "Rounds", "Seeds", "Unity sample rows", "Approx. rate"],
    ["Akshat", "Median", "4 x 90 s", "104729, 130363, 155921, 196613", "7,221", "20 Hz"],
    ["Alka", "Moving Average", "4 x 90 s", "same matched set", "7,200", "20 Hz"],
    ["Arham", "Butterworth", "4 x 90 s", "same matched set", "7,196", "19.98-20 Hz"],
    ["Dharti", "Alpha-Beta", "4 x 90 s", "same matched set", "7,198", "19.99-20 Hz"],
], [28*mm, 35*mm, 24*mm, 50*mm, 24*mm, 19*mm]))
story.append(P(
    "All 16 event logs reached round_complete. Each matched sequence spawned 44 balloons. Across all rounds, the logs contain 704 spawns, 400 hits and 240 misses. Almost every sample was marked valid; one Akshat sample was invalid, and minor row-count differences reflect packet timing/loss rather than a change in round duration."
))

# Preliminary results
story += [PageBreak(), P("9. Preliminary results from the gameplay logs", "H1x")]
story.append(table([
    ["Member / condition", "Scores by seed", "Mean score", "Total hits", "Mean misses"],
    ["Akshat / Median", "28, 30, 29, 23", "27.50", "110", "12.75"],
    ["Alka / Moving Average", "20, 23, 25, 22", "22.50", "90", "17.50"],
    ["Arham / Butterworth", "19, 26, 21, 17", "20.75", "83", "19.00"],
    ["Dharti / Alpha-Beta", "30, 27, 30, 30", "29.25", "117", "10.75"],
], [45*mm, 43*mm, 27*mm, 25*mm, 30*mm]))
story.append(callout(
    "Do not describe this table as proof that Alpha-Beta is the best filter. Each participant used only one filter, so participant skill, calibration and learning are confounded with filter condition. These are individual outcomes, not a controlled within-subject filter comparison.",
    colors.HexColor("#FFF4E5"), GOLD
))
story.append(P("Suggested member-specific result wording", "H2x"))
member_text = [
    ("Akshat", "Using the 3-sample median condition, Akshat achieved 110 hits across four matched rounds, with a mean of 27.50 hits and 12.75 misses per round. Performance was highest on seed 130363 (30 hits) and lowest on seed 196613 (23 hits)."),
    ("Alka", "Using the 5-sample moving-average condition, Alka achieved 90 total hits, averaging 22.50 hits and 17.50 misses. Scores varied from 20 to 25 across matched sequences."),
    ("Arham", "Using the second-order Butterworth condition, Arham achieved 83 total hits, averaging 20.75 hits and 19.00 misses. The best round was seed 130363 with 26 hits."),
    ("Dharti", "Using the alpha-beta condition, Dharti achieved 117 total hits, averaging 29.25 hits and 10.75 misses. Three of four rounds reached 30 hits."),
]
for name, text in member_text:
    story.append(P(f"<b>{name}:</b> {text}"))

# Data quality caveat
story += [PageBreak(), P("10. Data-quality finding that must be reported", "H1x")]
story.append(callout(
    "The actual Recorder Time column is in seconds, but the bridge was run using its default milliseconds interpretation. A real interval of about 0.05 s was stored as about 0.00005 s. Unity then clamped this to 0.0001 s generally and to 0.001 s inside the alpha-beta filter.",
    colors.HexColor("#FDECEC"), RED
))
story.append(P("Evidence", "H2x"))
for item in [
    "Recorder rows advance by about 0.050 seconds.",
    "Unity receive timestamps show approximately 89.95 seconds for about 1,800 rows: an actual acquisition rate of approximately 20 Hz.",
    "Unity source timestamps cover only approximately 0.08995 seconds for the same 90-second round.",
    "The Unity delta_seconds column is commonly 0.0001 rather than approximately 0.05.",
]: story.append(bullet(item))
story.append(P("Impact by collected condition", "H2x"))
story.append(table([
    ["Condition", "Likely impact"],
    ["Median (Akshat)", "Window-based and does not use dt; timing conversion does not change median values."],
    ["Moving Average (Alka)", "Window-based and does not use dt; at 20 Hz, five samples span 0.25 s with about 0.10 s nominal group delay."],
    ["Butterworth (Arham)", "Does not use packet dt, but coefficients were designed for 50 Hz while actual data arrived at about 20 Hz. The realized response was therefore slower/lower-cutoff than the stated 2.5 Hz design."],
    ["Alpha-Beta (Dharti)", "Uses dt. The clamped 0.001 s value was far below the actual approximately 0.05 s interval, changing velocity estimation and prediction dynamics."],
], [40*mm, 130*mm]))
story.append(P(
    "Recommended handling: preserve these files unchanged; disclose the issue in Methods/Limitations; use receive_seconds for real timing; avoid claiming a valid controlled comparison of intended filter dynamics; and, if possible, recollect after launching the bridge with --time-unit seconds and setting Unity's nominal sample rate to the measured approximately 20 Hz. Offline analysis can still calculate raw/filtered traces, scores and descriptive metrics, but it cannot retroactively make the live Alpha-Beta or Butterworth conditions equal to their intended designs."
))

# Report structure
story += [PageBreak(), P("11. How to convert this into the required report", "H1x")]
story.append(table([
    ["Required section", "Specific content to write"],
    ["Title", "Real-Time Center of Pressure Signal Processing for Body-Controlled Balloon Bursting Using a Wii Balance Board and Unity"],
    ["Objective", "Build a real-time CoP-controlled game; compare causal smoothing strategies; quantify smoothness, response and gameplay performance while retaining auditable raw data."],
    ["Experimental Design", "Four participants; one labelled condition each; four 90-second matched seeded rounds; 20 Hz observed acquisition; five-point participant calibration; fixed forward game and identical difficulty schedule."],
    ["Results", "Show raw corner loads, ML/AP raw and filtered traces, CoP trajectory, score/miss table, path length/jitter and timing. Give a separate subsection for each member using the values above."],
    ["Analysis", "Interpret smoothing versus responsiveness, learning/participant confounding, matched-seed fairness, actual sample rate, source-time error and condition-specific implementation limitations."],
    ["Conclusion", "Provide a team conclusion about successful end-to-end integration, then separate member conclusions limited to each person's observed condition and performance."],
], [35*mm, 135*mm]))
story.append(P("Suggested analysis figures", "H2x"))
for item in [
    "Four calibrated corner loads versus receive time for a representative round.",
    "Raw CoP ML/AP overlaid with the selected live output.",
    "Two-dimensional CoP/control trajectory with the normalized play boundary.",
    "Per-round hit and miss bars grouped by participant and seed.",
    "Jitter/path length and response timing, calculated using receive_seconds.",
    "Optional PSD of quiet-stance/raw CoP, explicitly separating natural sway from measurement noise.",
]: story.append(bullet(item))
story.append(P("Separate conclusion guidance", "H2x"))
story.append(P(
    "Each member should state what was observed for their assigned condition, what was learned about smoothness/control, and what limitation prevents generalization. Avoid wording such as 'our filter was scientifically best' because filters were not crossed within every participant and two timing/design mismatches were detected. A defensible conclusion is that the integrated pipeline worked and generated complete labelled gameplay datasets, while comparative filter superiority requires corrected timing and a counterbalanced within-subject design."
))

# Video and supplementary
story += [P("12. Video and supplementary submission checklist", "H1x")]
story.append(P("Suggested 5-7 minute video division", "H2x"))
story.append(table([
    ["Time", "Speaker focus"],
    ["0:00-0:40", "Problem, objective and complete system flow"],
    ["0:40-1:40", "Board pairing, Recorder calibration and raw data"],
    ["1:40-2:40", "Python bridge, audit log and UDP protocol"],
    ["2:40-3:50", "Unity CoP, filters and participant calibration"],
    ["3:50-4:50", "Gameplay, fixed seeds, difficulty and logging"],
    ["4:50-6:00", "Each member's result and condition"],
    ["6:00-7:00", "Limitations, corrected timing command and conclusion"],
], [30*mm, 140*mm]))
story.append(P("Supplementary material organization", "H2x"))
story.append(P("Supplementary_Data/<br/>  Akshat_Median/{Recorder_Raw, Bridge_Audit, Unity_Samples, Unity_Events}<br/>  Alka_MovingAverage/{...}<br/>  Arham_Butterworth/{...}<br/>  Dharti_AlphaBeta/{...}<br/>  README_data_dictionary.pdf", "CodeBlock"))
for item in [
    "Share one top-level labelled dataset per team member, satisfying the stated one-dataset-per-member requirement.",
    "Keep all four rounds inside that participant's dataset and never overwrite the original files.",
    "Include filter parameters, four fixed seeds, sample-rate evidence, calibration procedure, software/build identifier and known timestamp issue in the data dictionary.",
    "Test the Drive link from an account without owner privileges and grant the required viewer access before submission.",
]: story.append(bullet(item))

# Final concise method
story += [P("13. Concise report-ready method", "H1x")]
story.append(P(
    "A Nintendo Wii Balance Board was paired to a Windows computer using WiiBalanceWalker and calibrated using Wii Balance Board Recorder. The Recorder produced time-stamped calibrated and raw readings from four corner sensors. A custom Python 3 bridge continuously tailed the Recorder text file, validated each complete 12-column row, assigned a monotonically increasing sequence number, wrote a durable audit CSV, and transmitted calibrated front-left, front-right, rear-left and rear-right loads to Unity as versioned JSON datagrams over UDP at 127.0.0.1:5005."
))
story.append(P(
    "Unity received UDP data on a background thread and processed queued samples on the main thread. Samples with non-finite/negative corner loads or total load below 10 were rejected. Normalized mediolateral and anterior-posterior CoP were calculated from load differences divided by total load. One of seven causal conditions was selected in a paused operator menu: raw, 5-sample moving average, 2.5-Hz EMA, second-order 2.5-Hz Butterworth, 3-sample median, adaptive speed-sensitive low-pass, or alpha-beta prediction. Filters were implemented in Unity C#, not Python."
))
story.append(P(
    "A five-point calibration captured neutral stance and comfortable left, right, forward and backward limits. Filtered CoP was neutral-subtracted, dead-zone remapped, asymmetrically normalized and clamped to [-1,+1] before controlling a fixed pointer. The 90-second game used three 30-second difficulty phases and four matched hidden seeds. Balloons spawned only ahead of the player, approached a fixed plane, and were pooled with color-matched burst effects. The stationary camera avoided coupling balance input to viewpoint movement."
))
story.append(P(
    "For every round, Unity wrote a high-rate sample CSV containing sequence/timestamps, four loads, raw CoP, selected filter output and normalized control, plus an event CSV containing target spawns, hits, misses, positions, speeds, score and hidden seed. Four participants completed four matched rounds each. The observed arrival rate was approximately 20 Hz. A post-collection audit found that Recorder seconds had been interpreted as milliseconds by the bridge; this must be disclosed and corrected in future acquisition by using --time-unit seconds and configuring filter design for the measured rate."
))
story.append(callout(
    "Bottom line: the project successfully demonstrated a complete live hardware-to-game pipeline and produced four labelled participant datasets. The current data support transparent descriptive reporting. Strong causal claims about which filter is superior require corrected timing and a counterbalanced within-participant comparison.",
    PALE, CYAN
))

# Direct answers requested for setup/method reporting
story += [PageBreak(), P("14. Direct answers to setup and method questions (B-I)", "H1x")]
story.append(P("B. Software and connection", "H2x"))
story.append(table([
    ["Question", "Verified answer"],
    ["Unity version", "Unity 6000.6.6f1, revision d20c9ac777da."],
    ["How board data reached Unity", "Board paired/connected with WiiBalanceWalker v0.5; Wii Balance Board Recorder v1.0.0 and wiiuse produced a text file; custom Python bridge tailed that file and sent UDP to Unity. vJoy was not part of the final Unity data path and direct Bluetooth code did not run inside Unity."],
    ["Tool/library versions", "WiiBalanceWalker README reports v0.5 (internally uses WiimoteLib, 32Feet.NET and optional vJoy). Recorder manual reports v1.0.0. Exact wiiuse DLL and installed vJoy driver versions are not exposed by the supplied files and require team confirmation."],
    ["Python", "Python 3.14.3 in the inspected environment. The bridge uses only standard-library modules: argparse, csv, json, math, socket, sys, time, dataclasses, datetime, pathlib and typing. No pip package is required to run the bridge."],
    ["Network", "UDP/JSON, IPv4 loopback 127.0.0.1, port 5005, protocol version 1."],
    ["Repository", "https://github.com/Arham-1003/COP-MAIN-PROJECT.git; branch main. Inspected GitHub HEAD: 1ffa7cd. The newest local calibration diagnostics are working-tree changes and must be committed/pushed before calling them part of the final remote version."],
], [46*mm, 124*mm]))

story.append(P("C. Signal acquisition", "H2x"))
story.append(table([
    ["Question", "Verified answer"],
    ["One sample", "Four calibrated corner loads (front-left, front-right, rear-left, rear-right) plus sequence/source time. Recorder and bridge audits also retain four raw ADC-like counts. Unity computes CoP; it does not receive only CoP x/y."],
    ["Units", "Recorder calibrated corner channels appear to be force-like values and its Weight column is kg; the project did not convert corner values to SI newtons. Unity CoP is a normalized dimensionless ratio. Raw sensor columns are device counts. Do not label the Unity CoP as cm."],
    ["Measured update rate", "Approximately 20.00 Hz from Unity receive timestamps: about 1,800 rows in 89.95 s."],
    ["CoP formula", "total=FL+FR+RL+RR; x=((FR+RR)-(FL+RL))/total; y=((FL+FR)-(RL+RR))/total."],
    ["Positive directions", "By code, positive x means right-loading and positive y means front-loading. invertX and invertY exist but default to false. The collected logs do not record inversion flags, so the team must confirm the physical sign test used during collection."],
    ["Board orientation", "Not recorded in source code or data. Team confirmation required: document which edge/power-button side faced the participant and retain that orientation for all trials."],
], [46*mm, 124*mm]))

story += [PageBreak(), P("D. Calibration and protocol", "H2x")]
story.append(table([
    ["Question", "Verified answer"],
    ["When calibrated", "Manual calibration is available before a participant/session and should be repeated after board repositioning or re-zeroing. The software does not force recalibration before every 90-second round."],
    ["Neutral duration", "No fixed averaging duration is implemented. The participant holds centre and the operator captures the newest valid raw CoP sample. Therefore it is a single-sample capture, not the 3-5 s mean proposed in the design document."],
    ["Directional calibration", "Comfortable, not maximum, left/right/forward/backward lean is captured once per direction. No timed hold is enforced. Each direction must differ from neutral by at least 0.02 normalized CoP units."],
    ["Use of ranges", "Neutral is subtracted; positive/negative directions are divided by separate comfortable ranges, then per-axis dead zones are remapped and values clamped."],
    ["Final signal", "Normalized independently to -1 to +1 on x and y."],
    ["Filter-analysis recordings", "Four 90-second fixed-seed gameplay rounds per participant, four repetitions total. Raw Recorder and bridge files also include surrounding acquisition, but neutral/left/right/forward/backward segments were not separately labelled as dedicated analysis trials."],
    ["Participant instructions", "Not logged. Minimum recommended wording for the report: keep both feet in contact; stand naturally at centre; lean comfortably and slowly without stepping; do not jump; stop if unstable; rest between rounds. Confirm this matches what was actually said."],
    ["Location and sessions", "File timestamps show all measured Unity rounds on 5 November 2026, consistent with one collection day. The physical location/laboratory is not encoded and must be supplied by the team."],
], [46*mm, 124*mm]))

story.append(P("E. Filter details", "H2x"))
story.append(table([
    ["Filter", "Exact implementation used"],
    ["Moving Average", "N=5 samples. Running vector sum/ring buffer; same operation independently on x and y."],
    ["Median spike", "N=3 samples (odd). x and y values sorted independently; middle value selected."],
    ["Alpha-Beta", "alpha=0.85, beta=0.05. Intended dt comes from consecutive source timestamps and is clamped to [0.001,0.25] s. In acquired files, source-time scaling caused dt to clamp to 0.001 s rather than the actual approximately 0.05 s."],
    ["Butterworth", "Second order, cutoff 2.5 Hz, designed at nominal 50 Hz. b=[0.02008337, 0.04016673, 0.02008337], denominator a=[1, -1.56101808, 0.64135154]. Difference equation: y=b0*x+b1*x1+b2*x2-a1*y1-a2*y2. Actual acquisition was approximately 20 Hz, so the realized response differed from the intended 50-Hz design."],
    ["When filters run", "CopInputManager drains every queued UDP sample during Unity Update and calls each filter once per new board sample, not merely once per rendered frame. Multiple samples can be processed within one frame. Observed effective sample rate: approximately 20 Hz."],
    ["Axes", "The same selected filter and parameters are applied to both x and y, with independent state components."],
    ["Reset", "All filter states reset when input/filter changes and when calibration begins. The trial flow should also reset before every measured condition; parameters cannot change mid-round."],
    ["Member condition", "Akshat=Median, Alka=Moving Average, Arham=Butterworth, Dharti=Alpha-Beta. This proves assigned experimental condition, not who designed the code. Actual individual code ownership/contribution must be confirmed by the team."],
    ["Selection", "Dropdown named Movement Filter in the paused Y settings menu. Changing it is disabled while a round is playing."],
], [42*mm, 128*mm]))

story += [PageBreak(), P("F. Game details", "H2x")]
story.append(table([
    ["Question", "Verified answer"],
    ["Dead zone", "0.08 normalized units on x and 0.08 on y. It is per-axis, not radial. The remaining range is continuously rescaled so output does not jump at the boundary."],
    ["Crosshair speed/mapping", "No maximum speed parameter exists for board input. Lean maps directly to absolute pointer position: x extent +/-1.6 world units and y extent +/-1.0 around the interaction-plane centre. Pointer position is assigned in LateUpdate. It is position control, not velocity control."],
    ["Balloon spawn", "Continuous random positions, not a grid. x=-1.55..1.55, y=0.75..2.55, z=7..12. Spawn intervals are 2.4/2.1/1.8 s across phases. Base speed 0.70..1.15 units/s multiplied by 0.85/1.00/1.10. Minimum active spacing 0.8; maximum consecutive x-y jump 0.95; 16 placement attempts; Default Unity layer 0."],
    ["Pop condition", "Unity trigger-collider intersection, not a manually coded distance threshold. Pointer sphere scale 0.22 with base radius 0.5 (effective radius about 0.11). Balloon sphere scale 0.48 x 0.62 x 0.48 with base radius 0.5 (effective radii about 0.24, 0.31, 0.24). Thus contact is approximately 0.35 x, 0.42 y and 0.35 z centre separation, subject to Unity collider scaling."],
    ["Miss condition", "Balloon centre reaches z <= 2.75 before trigger contact."],
    ["On-screen display", "Blue pointer/crosshair, approaching balloons, score, misses, 90-second timer, title/status prompts, countdown, scenery, start/restart controls and the Y settings/calibration overlay."],
    ["Final game", "Yes: the final version is the forward-only game with balloons approaching the stationary player. Refinements added fixed camera behavior, board-friendly pacing, fixed matched seeds, color-matched bursts, improved scenery, expanded settings/dropdowns, calibration, seven filter choices and detailed sample/event logs."],
], [46*mm, 124*mm]))

story.append(P("G. Evaluation", "H2x"))
story.append(table([
    ["Question", "Verified answer"],
    ["Metrics already measured", "Per-round score/hits, misses, target spawn position/speed, round duration, raw loads, raw CoP, selected filter output, normalized control, validity and timestamps."],
    ["How measured", "Each member completed four matched 90-second rounds. Events were counted directly from hit/miss rows; samples were recorded at the measured approximately 20-Hz arrival rate."],
    ["Planned signal metrics", "Rest noise/SD or RMS, path length/jitter, response delay, settling time, PSD and false movements per minute can be calculated from sample logs, but they have not yet been calculated in the supplied folder."],
    ["Feedback", "No structured participant-feedback questionnaire is present in the data. Do not claim quantified subjective ratings unless a separate record exists."],
    ["Existing results", "Yes: 16 completed event logs and corresponding sample logs. Descriptive score table: Akshat 27.50, Alka 22.50, Arham 20.75, Dharti 29.25 mean hits per round. No plots were supplied; plots still need to be generated."],
], [46*mm, 124*mm]))

story += [PageBreak(), P("H. Participants", "H2x")]
story.append(table([
    ["Question", "Verified answer"],
    ["Number/details", "Four participants/team members: Akshat, Alka, Arham and Dharti. The report can describe them minimally as four student team members unless consented demographic details are required."],
    ["Filter exposure", "Each participant was tested with one filter only, for four matched-seed rounds. They were not each tested with all four filters."],
    ["Order", "Within each participant, filenames/seeds show 104729, 130363, 155921, then 196613. Between-filter counterbalancing was therefore not performed because each person had only one assigned condition."],
], [46*mm, 124*mm]))

story.append(P("I. Report requirements", "H2x"))
story.append(table([
    ["Question", "Answer"],
    ["Required format", "PDF with Title, Objective, Experimental Design, Results, Analysis and Conclusion. Results and conclusion must be separate for all members. No page limit was supplied in the current instructions."],
    ["Audience/tone", "Course assessor/instructor. Use formal English, mainly past tense for completed work and cautious language for interpretations."],
    ["Visuals", "A block diagram and parameter table are not explicitly stated as mandatory, but they are strongly recommended and this guide provides both. Include representative signal plots and the member/seed performance table."],
    ["Rubric/template", "Only the submission list and reference to a previous classroom grading post were supplied. The exact rubric was not available here; the team should cross-check the final report against that post before submission."],
], [46*mm, 124*mm]))
story.append(callout(
    "Items requiring team confirmation before final submission: board power-button orientation; actual laboratory/location; exact participant instructions; whether calibration was repeated for each participant/session; installed vJoy and wiiuse versions; individual development ownership; page limit; and the complete grading rubric.",
    colors.HexColor("#FFF7E6"), GOLD
))

doc = SimpleDocTemplate(
    str(OUTPUT), pagesize=A4,
    rightMargin=20*mm, leftMargin=20*mm,
    topMargin=20*mm, bottomMargin=17*mm,
    title="Balloon Balance - Technical Creation and Integration",
    author="SSRP Project 3 Team",
    subject="Unity Wii Balance Board CoP game implementation and data pipeline",
)
doc.build(story, canvasmaker=NumberedCanvas)
print(OUTPUT)
