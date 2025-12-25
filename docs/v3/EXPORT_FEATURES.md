# v3: Data Export Features

Last updated: 2025-12-25
Version: 3.0 (Data Export)

## Vision

Enable users to export their financial data and visualizations for external use. Users can download their financial plans, projections, transaction history, and charts in standard formats (CSV, Excel, PDF) for backup, sharing, or further analysis in tools like Google Sheets.

## Scope

**In Scope (v3):**
- CSV export for tabular data
- Excel (.xlsx) export for tabular data
- PDF export for chart visualizations
- Custom date range selection
- Export from projections, accounts, transactions, recurring contributions

**Out of Scope (Future v4):**
- Direct Google Sheets API integration (OAuth)
- Automatic cloud sync/backup
- Scheduled/automated exports
- Import from external sources

---

## Sub-Version Breakdown

### v3.1: Core Spreadsheet Exports
**Focus:** CSV and Excel exports for projections and transactions

**Features:**
- Export projection data (date, account balances, net worth) to CSV/Excel
- Export transaction/event history to CSV/Excel
- Date range picker for filtering export data
- Download button in Projections and Transactions pages

### v3.2: PDF Chart Export
**Focus:** Export visualizations as PDF documents

**Features:**
- Export projection chart as PDF with title and description
- Basic report layout (chart image + metadata)
- User-provided title and optional notes

### v3.3: Extended Data Exports
**Focus:** Complete data export coverage

**Features:**
- Export account summaries (current balances, types, status)
- Export recurring contribution schedules
- Export budget data (if v2 complete)
- Combined "full export" option (all data in one Excel workbook with multiple sheets)

---

## Technical Approach

### Backend

**CSV Generation:**
- Use `CsvHelper` NuGet package for robust CSV generation
- Endpoint returns `FileContentResult` with `text/csv` content type

**Excel Generation:**
- Use `ClosedXML` NuGet package (MIT licensed, no Office dependency)
- Support multiple sheets per workbook for combined exports

**PDF Generation:**
- Use `QuestPDF` NuGet package (MIT licensed, modern fluent API)
- Accept chart image (base64 or blob) from frontend
- Compose PDF with title, chart, metadata

### Frontend

**Export UI:**
- Export button/menu in relevant pages (Projections, Transactions, Accounts)
- Date range picker component (reusable)
- Format selector (CSV, Excel, PDF where applicable)
- Loading state during export generation

**Chart Capture:**
- Use `html2canvas` or Chart.js built-in `toBase64Image()` for chart capture
- Send chart image to backend for PDF composition

### API Endpoints

```
GET /api/export/projections?format={csv|xlsx}&startDate={date}&endDate={date}
GET /api/export/transactions?format={csv|xlsx}&startDate={date}&endDate={date}
GET /api/export/accounts?format={csv|xlsx}
GET /api/export/recurring?format={csv|xlsx}
POST /api/export/chart-pdf  (body: { title, description, chartImage })
GET /api/export/full?format=xlsx  (all data in multi-sheet workbook)
```

---

## Detailed Work Items

### Phase v3.1: Core Spreadsheet Exports

#### WI-V31-001: Export Infrastructure
- **Status:** [ ]
- **Parallelizable:** No (foundation)
- **Files:**
  - `FinanceEngine.Api/Endpoints/ExportEndpoints.cs` (NEW)
  - Add `CsvHelper` and `ClosedXML` NuGet packages
- **Task:** Set up export endpoint structure and dependencies
- **Acceptance:** Packages installed, base endpoint file created

#### WI-V31-002: Projection CSV Export
- **Status:** [ ]
- **Parallelizable:** No
- **Depends on:** WI-V31-001
- **Files:**
  - `FinanceEngine.Api/Endpoints/ExportEndpoints.cs`
  - `FinanceEngine.Api/Services/ExportService.cs` (NEW)
  - `FinanceEngine.Tests/Endpoints/ExportEndpointsTests.cs` (NEW)
- **Task:** Export projection data to CSV
- **Details:**
  - Columns: Date, AccountName, AccountType, Balance, TotalNetWorth
  - Support date range filtering
  - Use existing ForwardSimulationEngine for data
- **Verification:**
  ```bash
  dotnet test --filter "FullyQualifiedName~ExportEndpoints"
  ```
- **Acceptance:** CSV downloads with correct projection data

#### WI-V31-003: Projection Excel Export
- **Status:** [ ]
- **Parallelizable:** Yes (after WI-V31-002)
- **Depends on:** WI-V31-002
- **Files:**
  - `FinanceEngine.Api/Endpoints/ExportEndpoints.cs`
  - `FinanceEngine.Api/Services/ExportService.cs`
- **Task:** Export projection data to Excel
- **Details:**
  - Same data as CSV but in .xlsx format
  - Proper column formatting (dates, currency)
  - Sheet name: "Projections"
- **Verification:**
  ```bash
  dotnet test --filter "FullyQualifiedName~ExportEndpoints"
  ```
- **Acceptance:** Excel file downloads with formatted projection data

#### WI-V31-004: Transaction CSV/Excel Export
- **Status:** [ ]
- **Parallelizable:** Yes (after WI-V31-001)
- **Depends on:** WI-V31-001
- **Files:**
  - `FinanceEngine.Api/Endpoints/ExportEndpoints.cs`
  - `FinanceEngine.Api/Services/ExportService.cs`
- **Task:** Export transaction history to CSV and Excel
- **Details:**
  - Columns: Date, Type, Description, Amount, Account, Status
  - Support date range filtering
- **Verification:**
  ```bash
  dotnet test --filter "FullyQualifiedName~ExportEndpoints"
  ```
- **Acceptance:** Transactions export in both formats

#### WI-V31-005: Date Range Picker Component
- **Status:** [ ]
- **Parallelizable:** Yes (frontend, independent)
- **Files:**
  - `dashboard/src/app/shared/components/date-range-picker/` (NEW)
- **Task:** Create reusable date range picker component
- **Details:**
  - Start date / End date inputs
  - Quick presets: "This Month", "Last 3 Months", "This Year", "Custom"
  - Emit selected range
- **Verification:**
  ```bash
  cd dashboard && npm run build
  ```
- **Acceptance:** Component renders and emits date range

#### WI-V31-006: Projections Page Export UI
- **Status:** [ ]
- **Parallelizable:** No
- **Depends on:** WI-V31-003, WI-V31-005
- **Files:**
  - `dashboard/src/app/pages/projections/projections.ts`
  - `dashboard/src/app/pages/projections/projections.html`
  - `dashboard/src/app/core/services/api.service.ts`
- **Task:** Add export button and dialog to Projections page
- **Details:**
  - Export button in toolbar
  - Dialog with date range picker and format selector
  - Download file on submit
- **Verification:**
  ```bash
  cd dashboard && npm run build
  ```
- **Acceptance:** Users can export projection data

#### WI-V31-007: Transactions Page Export UI
- **Status:** [ ]
- **Parallelizable:** Yes (after WI-V31-005)
- **Depends on:** WI-V31-004, WI-V31-005
- **Files:**
  - `dashboard/src/app/pages/transactions/transactions.ts`
  - `dashboard/src/app/pages/transactions/transactions.html`
- **Task:** Add export button to Transactions page
- **Verification:**
  ```bash
  cd dashboard && npm run build
  ```
- **Acceptance:** Users can export transaction history

---

### Phase v3.2: PDF Chart Export

#### WI-V32-001: PDF Generation Service
- **Status:** [ ]
- **Parallelizable:** No (foundation)
- **Files:**
  - Add `QuestPDF` NuGet package
  - `FinanceEngine.Api/Services/PdfExportService.cs` (NEW)
- **Task:** Set up PDF generation infrastructure
- **Acceptance:** QuestPDF integrated, service structure in place

#### WI-V32-002: Chart PDF Endpoint
- **Status:** [ ]
- **Parallelizable:** No
- **Depends on:** WI-V32-001
- **Files:**
  - `FinanceEngine.Api/Endpoints/ExportEndpoints.cs`
  - `FinanceEngine.Api/Services/PdfExportService.cs`
  - `FinanceEngine.Tests/Endpoints/ExportEndpointsTests.cs`
- **Task:** POST endpoint to generate PDF from chart image
- **Details:**
  - Accept: title, description, chartImage (base64)
  - Generate PDF with header, chart, footer with date
- **Verification:**
  ```bash
  dotnet test --filter "FullyQualifiedName~ExportEndpoints"
  ```
- **Acceptance:** PDF generated with chart image

#### WI-V32-003: Chart Capture Utility
- **Status:** [ ]
- **Parallelizable:** Yes (frontend, independent)
- **Files:**
  - `dashboard/src/app/shared/utils/chart-capture.ts` (NEW)
  - Add `html2canvas` npm package (if needed)
- **Task:** Utility to capture chart as base64 image
- **Details:**
  - Use Chart.js `toBase64Image()` if available
  - Fallback to html2canvas for other elements
- **Verification:**
  ```bash
  cd dashboard && npm run build
  ```
- **Acceptance:** Charts can be captured as images

#### WI-V32-004: PDF Export UI
- **Status:** [ ]
- **Parallelizable:** No
- **Depends on:** WI-V32-002, WI-V32-003
- **Files:**
  - `dashboard/src/app/pages/projections/projections.ts`
  - `dashboard/src/app/pages/projections/projections.html`
- **Task:** Add "Export as PDF" option to projections
- **Details:**
  - Dialog for title and optional description
  - Capture current chart view
  - Download generated PDF
- **Verification:**
  ```bash
  cd dashboard && npm run build
  ```
- **Acceptance:** Users can export projection chart as PDF

---

### Phase v3.3: Extended Data Exports

#### WI-V33-001: Account Summary Export
- **Status:** [ ]
- **Parallelizable:** Yes
- **Files:**
  - `FinanceEngine.Api/Endpoints/ExportEndpoints.cs`
  - `FinanceEngine.Api/Services/ExportService.cs`
- **Task:** Export account list with current balances
- **Details:**
  - Columns: Name, Type, CurrentBalance, LastUpdated, IsActive
- **Acceptance:** Accounts export in CSV/Excel

#### WI-V33-002: Recurring Contributions Export
- **Status:** [ ]
- **Parallelizable:** Yes
- **Files:**
  - `FinanceEngine.Api/Endpoints/ExportEndpoints.cs`
  - `FinanceEngine.Api/Services/ExportService.cs`
- **Task:** Export recurring contribution schedules
- **Details:**
  - Columns: Name, FromAccount, ToAccount, Amount, Frequency, NextDate
- **Acceptance:** Recurring schedules export in CSV/Excel

#### WI-V33-003: Full Data Export (Multi-Sheet Excel)
- **Status:** [ ]
- **Parallelizable:** No
- **Depends on:** WI-V33-001, WI-V33-002
- **Files:**
  - `FinanceEngine.Api/Endpoints/ExportEndpoints.cs`
  - `FinanceEngine.Api/Services/ExportService.cs`
- **Task:** Combined export with all data
- **Details:**
  - Excel workbook with sheets: Accounts, Transactions, Projections, Recurring
  - Summary sheet with totals and export metadata
- **Acceptance:** Single Excel file with all financial data

#### WI-V33-004: Accounts Page Export UI
- **Status:** [ ]
- **Parallelizable:** Yes (after WI-V33-001)
- **Depends on:** WI-V33-001
- **Files:**
  - `dashboard/src/app/pages/accounts/accounts.ts`
  - `dashboard/src/app/pages/accounts/accounts.html`
- **Task:** Add export button to Accounts page
- **Acceptance:** Users can export account list

#### WI-V33-005: Settings Page Full Export
- **Status:** [ ]
- **Parallelizable:** No
- **Depends on:** WI-V33-003
- **Files:**
  - `dashboard/src/app/pages/settings/settings.ts`
  - `dashboard/src/app/pages/settings/settings.html`
- **Task:** Add "Export All Data" option in Settings
- **Details:**
  - Button to download full data export
  - Could double as backup functionality
- **Acceptance:** Users can export all data from settings

---

## User Stories

1. **As a user**, I want to export my projected balances to Excel so I can create custom charts or share with my financial advisor.

2. **As a user**, I want to export my transaction history to CSV so I can import it into Google Sheets for additional analysis.

3. **As a user**, I want to export my projection chart as a PDF so I can save it as a visual record or share it.

4. **As a user**, I want to select a custom date range for my exports so I only get the data I need.

5. **As a user**, I want to export all my financial data at once so I have a complete backup.

---

## Success Metrics

- Users can export data in < 3 clicks
- Exported files open correctly in Excel, Google Sheets, and PDF readers
- Export completes in < 5 seconds for typical data sizes
- Date range filtering works correctly

---

## Dependencies and Packages

### Backend (NuGet)
- `CsvHelper` - CSV generation
- `ClosedXML` - Excel generation (no Office dependency)
- `QuestPDF` - PDF generation

### Frontend (npm)
- `html2canvas` (optional, if Chart.js capture insufficient)

---

## Future Considerations (v4+)

- **Google Sheets Integration:** Direct OAuth flow to export to user's Google account
- **Scheduled Exports:** Automatic weekly/monthly exports to email or cloud storage
- **Import:** Import transactions from bank CSV exports
- **Templates:** Customizable export templates
