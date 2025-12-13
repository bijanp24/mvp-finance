# MVP Finance Dashboard - Design & Structural Specifications

## Main Layout Structure

### Layout (Flex Column, 100vh height)

#### 1. Top Bar (MatToolbar - Primary color, sticky top)
- Menu Button (MatIconButton, Icon: 'menu') -> Toggles the Sidenav signal
- Title Text: "MVP Finance"
- Spacer (CSS flex-grow)

#### 2. Sidenav Container (MatSidenavContainer - flex-grow to fill remaining space)

**A. The Sidebar (MatSidenav)**
- **Settings:** Mode="side", Opened="true" (bound to signal)
- **Content:**
    - MatNavList
        - Link: Home (Icon: home)
        - Link: Settings (Icon: settings)

**B. The Main Content Area (MatSidenavContent - flex column)**
- **Body wrapper:** (flex-grow, padding: 20px)
    - `<router-outlet></router-outlet>` (Placeholder for page content)

---

# MVP Finance - Design & Structural Specifications

## Core (MVP) Pages

### A. Home (DashboardComponent)
* **Layout:** CSS Grid (Responsive: 1 col mobile, 3 col desktop).
* **Section 1: Hero Card (MatCard - Primary Color)**
    * **Header:** "Safe to Spend"
    * **Content:** Large Typography displaying calculated amount (e.g., "$450.00").
    * **Footer:** Subtext "Until next paycheck (14 days)".
* **Section 2: Pace Tiles (Grid of small MatCards)**
    * **Card 1:** "Monthly Budget" (Progress Bar: Spend vs Limit).
    * **Card 2:** "Savings Goal" (Progress Bar: Current vs Target).
    * **Card 3:** "Bills Due" (Text count of unpaid bills).
* **Section 3: Recent Activity (MatList)**
    * Header: "Recent Transactions"
    * List Items: Icon (category), Title (merchant), Amount (right-aligned).
    * Action: "View All" button (Links to Transactions).

### B. Quick Add (TransactionFormComponent)
* **Layout:** Centered Card (`max-width: 600px`).
* **Container:** `mat-card`.
* **Form Controls (Vertical Flex):**
    * **Amount:** `mat-form-field` (Input type="number", prefix="$").
    * **Type:** `mat-button-toggle-group` (Expense | Income | Transfer).
    * **Date:** `mat-form-field` with `mat-datepicker`.
    * **Category:** `mat-select` (Groups: Housing, Food, etc.).
    * **Merchant/Note:** `mat-form-field` (Input).
* **Actions:**
    * `mat-button` (stroked): "Cancel"
    * `mat-button` (flat, color="primary"): "Save Transaction"

### C. Transactions (LedgerComponent)
* **Layout:** Full width container.
* **Toolbar (Filter Area):**
    * `mat-form-field`: Search (Text).
    * `mat-chip-listbox`: Filters (This Month, Last Month, High Value).
* **Data Table (`mat-table` with Sticky Header):**
    * **Columns:** Date, Merchant, Category (Chip), Amount (Red/Green text styling), Actions.
    * **Features:** `mat-sort`, `mat-paginator`.
    * **Menu:** Action column contains `mat-menu` (Edit, Delete).

### D. Debts (DebtDashboardComponent)
* **Layout:** CSS Grid (Auto-fill cards).
* **Visuals:**
    * **Summary Header:** Total Debt Load (Text).
* **Debt Cards (Iterated list):**
    * **Header:** Debt Name (e.g., "Visa Sapphire").
    * **Subtitle:** APR % | Min Payment.
    * **Content:**
        * `mat-progress-bar`: Balance vs Limit.
        * Text: "Payoff Date: [Date]" (Calculated).
    * **Actions:** "Record Payment" button.

### E. Plan (PaycheckAllocatorComponent)
* **Layout:** `mat-accordion` (Expansion Panels).
* **Panel 1: Income Source:**
    * Input: Paycheck Amount.
    * Date: Next Payday.
* **Panel 2: Obligations (Non-negotiable):**
    * `mat-selection-list`: List of bills due in this period.
    * Summary Text: "Total Obligations".
* **Panel 3: Allocations (Discretionary):**
    * List of input fields (`mat-form-field`) for envelopes/savings.
    * *Validation:* Ensure Total Allocations + Obligations <= Income.
* **Footer (Sticky):**
    * Summary Bar: "Left to Budget: $0.00" (Green if 0, Red if negative).

---

## 3. Next Phase Pages

### F. Scenarios (SimulatorComponent)
* **Layout:** Split View (Inputs Left, Results Right).
* **Inputs:**
    * `mat-slider`: "Extra Payment Amount ($0 - $1000)".
    * `mat-radio-group`: Strategy (Avalanche vs Snowball).
* **Results:**
    * `mat-card`: "Debt Free Date" (Changes dynamically).
    * `mat-card`: "Total Interest Saved".

### G. Investments (PortfolioComponent)
* **Layout:** `mat-tab-group`.
* **Tab 1: Contributions:**
    * Form to log 401k/IRA contributions.
* **Tab 2: Projections:**
    * Visual placeholder for Growth Chart.
    * Assumptions: `mat-expansion-panel` (Rate of Return inputs).

### H. Reports (AnalyticsComponent)
* **Layout:** Dashboard Grid.
* **Metrics:**
    * Burn Rate: `mat-card` (Average monthly spend).
    * Net Worth: `mat-card` (Assets - Debts).
* **Visuals:**
    * Charts containers (provide `div` with specific IDs for Chart.js/Ngx-Charts).
    * `mat-button-toggle`: Timeframe (3M, 6M, 1Y, YTD).

---

## 4. Admin Pages

### I. Settings (SettingsComponent)
* **Layout:** `mat-tab-group` (Vertical or Horizontal).
* **Tab: Accounts:**
    * List of Accounts (`mat-list`) + "Add New" FAB.
* **Tab: Preferences:**
    * `mat-slide-toggle`: Dark Mode.
    * `mat-form-field`: Payday Frequency (Weekly, Bi-weekly, Monthly).

### J. Import/Export (DataManagerComponent)
* **Import:**
    * File Input (Hidden) triggered by `mat-button` (raised): "Upload CSV".
    * `mat-progress-bar` (mode="determinate") for upload status.
* **Export:**
    * List of download options: "Full Backup (JSON)", "Transactions (CSV)".

### K. About (AboutComponent)
* **Container:** `mat-card` (centered).
* **Content:** App Version, Developer Credits, Link to Repo.