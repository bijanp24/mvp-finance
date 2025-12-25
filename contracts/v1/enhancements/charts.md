## Enhancement Backlog: Improve Chart Readability (Net Worth + Debt)

### Problem
Current charts are technically accurate but visually “jerky” (sawtooth). This happens because financial events are discrete (paychecks, bills, payments), producing sharp spikes and drops that make long-term trends hard to read at a glance.

### User Story
As a user reviewing my financial trajectory,
I want net worth and debt charts that emphasize the long-term trend,
so I can quickly understand progress without being distracted by day-to-day volatility.

### Goals
- Make the default view feel stable and professional.
- Preserve accuracy while improving comprehension.
- Keep a way to view granular “reality” when desired.

### Non-Goals
- Do not fabricate or imply values that did not occur.
- Do not “over-smooth” in a way that creates phantom peaks/valleys.

### Proposed Approach (Implementation Options)

#### Option A (Preferred): Change data granularity for default view
Aggregate data before charting.

- Net worth: plot end-of-month (or end-of-week) balances instead of daily/event points.
- Debt payoff: plot balances at the same aggregation level so the trajectory reads as a clean curve over time.

Why this works: intra-month paycheck spikes and bill drops collapse into a stable periodic snapshot that communicates the actual trend.

#### Option B: Trend overlay (keep granular data, highlight the signal)
Plot two series on the same chart:

- Raw series: current daily/event series, rendered faintly (or as bars) to show “what really happened.”
- Trend series: a rolling moving average (e.g., 30-day) rendered as the primary line.

Why this works: users can see both volatility and direction without confusing the two.

#### Option C: Step interpolation for debt charts
Debt payments are discrete; connecting drops with diagonal lines makes the curve look noisy.

- Render debt payoff as a stepped/step-after line (staircase).

Why this works: the chart becomes visually intentional and communicates discrete payment events honestly.

#### Option D: Minimal curve smoothing (visual-only)
If the chart library supports interpolation/tension/bezier:

- Apply only low smoothing.

Warning: aggressive smoothing can introduce implied values between points. If used at all, keep it subtle and validate visually against raw data.

#### Option E: Toggle for “Daily vs Monthly” (optional UI)
Provide a view toggle:

- Monthly (default): aggregated trend view.
- Daily: current jagged view for inspecting specific paycheck/payment timing.

### Acceptance Criteria
- Default chart view uses aggregated points (monthly or weekly) and reads as a smooth trend at typical time ranges (e.g., 1–5 years).
- Users can switch to a granular view (if toggle is implemented) and see the original day/event volatility.
- Debt payoff chart supports a stepped rendering (staircase) so drops appear as discrete events.
- If any smoothing/interpolation is enabled, it is minimal and does not visually create peaks/valleys that contradict the raw series.
- Values shown in tooltips/labels match the underlying computed series for that view (aggregated view shows aggregated values; daily view shows daily values).

### Notes / Implementation Detail
- Aggregation definition should be explicit (e.g., “end-of-month balance” = last computed balance within the month).
- Prefer “data-first” smoothing (aggregation / trend series) over “render-only” smoothing.
- If trend overlay is added, the raw series should be visually de-emphasized (opacity/weight) so it reads as context, not the primary signal.