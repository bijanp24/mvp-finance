# Project Context: Personal Finance MVP

## 1. Core Data Concepts (High Level)

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
* **DebtPayment:** Transfer from Cash → Debt (reduces liability).
* **Interest/Fee:** Cost of borrowing or account fees (Generated or Imported).
* **Savings/InvestmentContribution:** Transfer from Cash → Investment.

### Optional / Post-MVP
* **State Management:** Pending vs. Cleared transactions.
* **Organization:** Categories and Tags.
* **Timeframes:** Statement cycles.

---

## 2. Definition of Done (MVP)

The MVP is considered complete when the user can perform the following actions:

1.  **Fast Entry:** Add a spending record manually in seconds.
2.  **Immediate Feedback:** Instantly view "Spendable Balance" remaining until the next paycheck.
3.  **Debt Clarity:** View debt balances trending downward, with a visual split between **Principal** reduction and **Interest** cost.
4.  **Forecasting:** See a projected "Debt-Free Date" that updates dynamically based on payment changes.
5.  **Compounding Visualization:** View comparative curves for Debt, Investments, and Net Worth.

---

## 3. Key Feature Specifications

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