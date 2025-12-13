# Project Context: Personal Finance "Compounding Dashboard" MVP

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

## 6. Immediate Instructions
When asked to write code, please:
1.  Start by defining the **Domain Entities** based on the Event-based Ledger description.
2.  Create the interfaces for the **Calculation Engines**.
3.  Ensure all financial calculations use `decimal` type for precision.