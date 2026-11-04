# Signal Processing

## Measurements

The Wii Balance Board exposes four corner loads through compatible acquisition software:

- front-left: `FL`
- front-right: `FR`
- rear-left: `RL`
- rear-right: `RR`

Exact labels and physical orientation must be verified experimentally because acquisition libraries may use top/bottom or reversed conventions.

## Center of Pressure

For sensors at coordinates `(xi, yi)` with vertical forces `Fi`:

```text
CoP_x = sum(Fi * xi) / sum(Fi)
CoP_y = sum(Fi * yi) / sum(Fi)
```

For a symmetric board with effective left/right spacing `Dx` and front/rear spacing `Dy`:

```text
total = FL + FR + RL + RR

CoP_ML = ((FR + RR) - (FL + RL)) / total * (Dx / 2)
CoP_AP = ((FL + FR) - (RL + RR)) / total * (Dy / 2)
```

Use documented or physically measured sensor-centre coordinates when physical units matter. Do not assume that the board's outer dimensions equal sensor spacing. Normalized coordinates are acceptable for initial control development until the supplied board can be measured and verified.

## Direction validation

1. Stand centrally: ML and AP should be near the calibrated neutral point.
2. Lean right: configured ML should increase.
3. Lean left: configured ML should decrease.
4. Lean forward: configured AP should increase.
5. Lean backward: configured AP should decrease.

Implement `InvertX` and `InvertY` configuration rather than rewriting formulas for different libraries.

## Guard conditions

- Do not divide when total load is below a validated occupancy threshold.
- Reject NaN, infinity, impossible loads, and malformed timestamps.
- Flag negative calibrated loads beyond a small tolerated sensor error.
- Keep board sensor offset correction separate from participant neutral subtraction.
- Use source timestamps instead of assuming a fixed rate.

## Processing order

```text
raw corner loads
-> board offset correction
-> validity and outlier checks
-> total load and CoP calculation
-> optional spike rejection
-> causal low-pass filter
-> participant neutral subtraction
-> dead zone and continuous remapping
-> asymmetric range normalization
-> clamp to [-1, +1]
-> pointer target mapping
```

Document and test any change to this order.

## Filters

### Raw/pass-through

Purpose: baseline with the lowest algorithmic delay.

Expected result: most visible jitter and spikes.

### Moving average

```text
y[n] = mean(x[n], x[n-1], ..., x[n-N+1])
```

Advantages: simple and easy to explain.

Risks: window length adds delay and depends on sampling rate.

### Exponential moving average

For a time-based cutoff:

```text
alpha = 1 - exp(-2 * pi * cutoffHz * deltaTime)
y = y + alpha * (x - y)
```

Advantages: inexpensive, responsive, and naturally supports timestamp-derived `deltaTime`.

Risks: fixed alpha is misleading when sample intervals vary; prefer timestamp-aware `deltaTime`. Do not describe `1 / (2*pi*cutoffHz)` as a fixed input-to-output delay: it is the analogue time constant, while phase/group delay depends on signal frequency and sampling.

### Second-order Butterworth low-pass

Use a causal digital IIR implementation with coefficients derived for the measured nominal sample rate. First measure the board rate and jitter from source timestamps. If jitter is material, resample to a uniform timeline or use the timestamp-aware EMA; do not silently redesign Butterworth coefficients for every packet interval. Record cutoff frequency, nominal sample rate, coefficient design method, and reset behaviour.

Do not use forward-backward zero-phase filtering for live control. It may be used only as an offline reference and must be labelled accordingly.

Treat theoretical filter phase/group delay, transport/processing latency, and observed gameplay response delay as separate quantities. Measure and report them separately rather than assigning one fixed delay value to a filter.

### Median spike prefilter

A short odd-length median filter can suppress isolated spikes. Keep the window small and measure its effect on transition timing. Treat it as an optional prefilter, not an automatic default.

### Adaptive low-pass

Use a causal speed-sensitive low-pass condition based on the One Euro principle. Smooth strongly near quiet stance and raise the cutoff independently on each axis as the filtered CoP derivative increases. Record the minimum cutoff, speed coefficient, derivative cutoff, and timestamp-derived sample interval. This is a non-linear condition, so evaluate it from measured jitter, response delay, and settling behaviour rather than describing it with one fixed frequency response.

### Alpha-beta predictive filter

Maintain causal position and velocity estimates. Predict the next position from the source timestamp interval, then correct position and velocity using fixed alpha and beta gains. Clamp implausible time intervals and reset both estimates between trials. Record both gains and assess overshoot when a participant stops or reverses direction.

The initial adaptive and alpha-beta parameters are provisional gameplay defaults. They must remain unchanged within a measured trial and must be finalized from pilot recordings before filter comparisons begin.

## Filter state

Reset every filter:

- before calibration;
- before each measured trial;
- when switching source recordings;
- after prolonged disconnection when stale state would create a jump.

Do not change filter or parameter values inside a measured trial.

## Calibration

### Neutral

Record 3-5 seconds of quiet stance:

```text
neutralX = mean(CoP_ML)
neutralY = mean(CoP_AP)
```

Also record standard deviation as a calibration-quality measure.

### Directional ranges

Record comfortable, not maximum, lean in all four directions:

```text
leftRange
rightRange
forwardRange
backwardRange
```

Reject calibration when a range is too small, unstable, saturated, or in the wrong direction.

### Asymmetric normalization

```text
centeredX = CoP_ML - neutralX

if centeredX >= 0:
    normalizedX = centeredX / rightRange
else:
    normalizedX = centeredX / abs(leftRange)

normalizedX = clamp(normalizedX, -1, +1)
```

Apply the same principle to AP. Preserve the agreed game mapping:

- ML -> horizontal pointer position
- AP -> vertical pointer position

### Dead zone

Apply a small dead zone after centring. Remap the remaining range continuously so the output does not jump at the dead-zone boundary.

Do not add excessive Unity-side smoothing after signal filtering; it can hide filter differences and add unreported delay.

## Synthetic board model

Given normalized target `(x, y)` and total weight `W`:

```text
FL = W * (1 - x) * (1 + y) / 4
FR = W * (1 + x) * (1 + y) / 4
RL = W * (1 - x) * (1 - y) / 4
RR = W * (1 + x) * (1 - y) / 4
```

The simulator may add:

- independent sensor noise;
- slow drift;
- isolated spikes;
- packet loss;
- timing jitter;
- configurable sample rate;
- unoccupied/step-off states.

Feed these loads through the production CoP calculator.

## Latency

Record:

- source timestamp;
- Unity receive timestamp;
- processing timestamp or duration;
- frame/pointer update time;
- balloon eligibility and hit timestamps.

Use monotonic clocks. Do not subtract timestamps from different unsynchronized clock domains without estimating the offset.

## Required signal outputs

- four corner loads over time;
- raw and filtered ML/AP traces;
- two-dimensional CoP trajectory;
- FFT or PSD of representative segments;
- quiet-stance SD/RMS;
- CoP path length;
- response delay and settling time;
- filter parameter table.

Natural postural sway is not automatically noise. Interpret frequency and task context before labelling variation as artifact.

