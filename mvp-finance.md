# Personal Finance "Compounding Dashboard" MVP

## 1. System Role & Objective
**Role:** Senior .NET Core Architect & Developer.
**Objective:** Build a local-first, UI-agnostic .NET Core library that serves as the engine for a personal finance instrument panel.
**Core Philosophy:** Real-time visibility into "Spendable Now" and "Compounding Velocity" (working for or against the user).

---

## 2. High-Level Goal
A real-time finance instrument panel that answers three critical questions:
1. "How much can I spend until my next paycheck?"
2. "Is compounding working for me (investments) or against me (debt)?"
3. "How fast am I paying things down?"

**Architecture Note:** We are building the **backend logic/library layer** first. It must be accessible by any future frontend (Angular, WPF, MAUI, etc.).

---

## 3. Core Features

### A. Real-time "Spendable Until Payday"
* **Primary Metric:** "Spendable Now" (Big Number).
* **Supporting Metrics:**
    * Projected remaining cash on payday.
    * Burn rate ($/day) with variability bands (typical vs. worst-case).
    * Upcoming obligations due before the next income event.

### B. Event-based Ledger (The Foundation)
The system relies on an event-sourcing model with four distinct event types:
1.  **Income Events:** Paychecks, bonuses.
2.  **Spending Events:** Daily life expenses.
3.  **Debt Events:** Charges/borrowing, payments, interest accrual, fee assessments.
4.  **Savings/Investment Events:** Contributions, projected growth.

* **Capabilities:**
    * Manual entry support.
    * Future import support (CSV/Aggregators).
    * Reconciliation (Pending vs. Cleared).

### C. Debt Paydown Velocity
Calculated per paycheck cycle or date range:
* **Delta Balance:** Change in total debt.
* **Efficiency:** Principal paid vs. Interest drag.
* **Projections:** Payoff pace and estimated debt-free date.
* **Scenarios:** Impact of " +$X extra per paycheck."

### D. Compounding Visualization
Data generation for charting:
* **Debt Curve:** Interest compounding against the user.
* **Investment Curve:** Returns compounding for the user (inflation-adjusted).
* **Net Worth Curve:** Assets minus Debts.
* **Crossover Milestone:** The date when Investment Growth Rate > Debt Interest Rate.

---

## 4. Key Algorithms & Logic

### A. Spendable Until Next Paycheck (Runway)
**Formula:**
$$Spendable = Available Cash - (Upcoming Obligations + Safety Buffer + Planned Contributions)$$

**Inputs:**
* Current cash balances (live or computed).
* Schedule of income events.
* Schedule of required bills/payments.
* Estimated spending pace.

**Outputs:**
* Spendable Now amount.
* Expected cash at next paycheck.
* Confidence range (Typical vs. Conservative).

### B. Rolling Burn Rate + Variability
* Calculate rolling spend/day over 7, 30, and 90-day windows.
* Calculate variability (Standard Deviation or Percentile-based).
* *Usage:* Determines the size of the "Safety Buffer" in Algorithm A.

### C. Forward Simulation Engine ("The Truth Machine")
Used for forecasting payoff dates and curves.
1.  Order all events by date.
2.  Accrue debt interest between events.
3.  Apply event effects (Payment, Charge, Income, Expense).
4.  Repeat forward until debt = 0 or target date reached.

### D. Extra Payment Allocation Strategies
Configurable logic for how extra cash is applied:
* **Avalanche:** Highest APR first.
* **Snowball:** Smallest balance first.
* **Hybrid:** Minimums enforced first, then allocation strategy applied.

### E. Investment Projection
* Inputs: Contribution schedule, expected nominal return, inflation rate.
* Output: Real return time series.

---

## 5. Technical Requirements
* **Framework:** .NET Core (Latest LTS).
* **Architecture:** Clean Architecture / Domain-Driven Design (DDD).
* **Persistence:** In-memory for MVP (interface-based repositories for future SQLite/SQL support).
* **Testing:** Unit tests required for all financial algorithms (rounding errors must be handled).

---

## 5.1 Implementation Notes

### Component Boundaries
- **FinanceEngine** - Pure calculation library (no I/O, no EF Core). Stateless calculators with record-based inputs/outputs.
- **FinanceEngine.Data** - EF Core layer with `FinanceDbContext`, entities, and SQLite persistence.
- **FinanceEngine.Api** - Minimal API using endpoint groups (`/api/accounts`, `/api/events`, `/api/calculators`).
- **dashboard/** - Angular app with standalone components, signals, and Material UI.

### Design Patterns and Gotchas
- **Event sourcing:** Account balances are computed from transaction history, not stored.
- **Calculator pattern:** Static methods in `FinanceEngine.Calculators` with record-based inputs/outputs.
- **APR storage:** Store as decimal (0.0499 = 4.99%), not percentage values.
- **EF Core queries:** Materialize with `.ToListAsync()` before mapping to domain models.

---

## 6. Core Data Concepts

### Accounts
Entities representing where money lives or is owed.
* **Cash Accounts:** Liquid assets (Checking, HYSA).
* **Debt Accounts:** Liabilities (Credit Cards, Student Loans, Parent PLUS).
* **Investment Accounts:** Growth assets (401k, Brokerage).

### Events (State Changes)
The system relies on an **Event Sourcing** model. Account balances are derived solely by aggregating these events. State does not change without an event.

* **Income:** Inflow to Cash (Paycheck, etc.).
* **Expense:** Outflow from Cash (General spending).
* **DebtCharge/Borrow:** Increase in Debt liability (e.g., Credit Card purchase).
* **DebtPayment:** Transfer from Cash  Debt (reduces liability).
* **Interest/Fee:** Cost of borrowing or account fees (Generated or Imported).
* **Savings/InvestmentContribution:** Transfer from Cash  Investment.

### Optional / Post-MVP
* **State Management:** Pending vs. Cleared transactions.
* **Organization:** Categories and Tags.
* **Timeframes:** Statement cycles.

---

## 7. Definition of Done (MVP)

The MVP is considered complete when the user can perform the following actions:

1.  **Fast Entry:** Add a spending record manually in seconds.
2.  **Immediate Feedback:** Instantly view "Spendable Balance" remaining until the next paycheck.
3.  **Debt Clarity:** View debt balances trending downward, with a visual split between **Principal** reduction and **Interest** cost.
4.  **Forecasting:** See a projected "Debt-Free Date" that updates dynamically based on payment changes.
5.  **Compounding Visualization:** View comparative curves for Debt, Investments, and Net Worth.

---

## 8. Key Feature Specifications

### A. Quick Add
* **Constraint:** Log spend, income, or payment in < 5 seconds.
* **UX Focus:** Minimal friction, mobile-first inputs.

### B. Spendable Until Payday
* **Primary Metric:** A single "Big Number" showing safe-to-spend cash.
* **Context:**
    * **Pace:** How fast money is leaving relative to time left.
    * **Variance Band:** Visual indicator of being "ahead" or "behind" schedule.

### C. Compounding View (The "Scenario Slider")
* **Visuals:**
    * Debt Amortization Curve.
    * Investment Growth Curve.
    * Net Worth Resultant Curve.
* **Interactivity:**
    * **Scenario Slider:** Allows user to toggle an extra amount (e.g., +$100/paycheck).
    * **Logic:** Instantly re-render curves to answer: *"Should I put this extra $100 toward Debt or Investments?"*

