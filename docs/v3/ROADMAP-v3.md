# ROADMAP-v3.md

Last updated: 2025-12-30
Version: 3.0 (Data Import/Export)

## Purpose
Structured work items for v3 features. Optimized for parallel agent execution.

See `EXPORT_FEATURES.md` for the full v3 vision (includes import and export).

## How to Use This File
1. Pick an unclaimed work item from the current phase
2. Mark it `[IN PROGRESS]` with your agent identifier
3. Complete the work item
4. Run the verification command
5. Mark it `[DONE]` and update WORKLOG-v3.md
6. Move to next item or hand off

## Parallel Execution Rules
- Items marked `Parallelizable: Yes` can run simultaneously
- Items with `Depends on:` must wait for dependencies
- Backend and frontend items in same feature should run sequentially
- Test items can run in parallel with each other

---

## v2 Summary
Goal-Driven Budgeting features are tracked in `docs/v2/`.
v3 can proceed independently as export features don't depend on v2.

---

## Phase v3.1: Core Spreadsheet Exports
**Status:** Not Started
**Estimated work items:** 7

### WI-V31-001: Export Infrastructure
- **Status:** [ ]
- **Parallelizable:** No (foundation)
- **Depends on:** None
- **Files:**
  - `FinanceEngine.Api/Endpoints/ExportEndpoints.cs` (NEW)
  - Add `CsvHelper` and `ClosedXML` NuGet packages
- **Task:** Set up export endpoint structure and dependencies
- **Verification:**
  ```bash
  dotnet build
  ```
- **Acceptance:** Packages installed, base endpoint file created

### WI-V31-002: Projection CSV Export
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

### WI-V31-003: Projection Excel Export
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

### WI-V31-004: Transaction CSV/Excel Export
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

### WI-V31-005: Date Range Picker Component
- **Status:** [ ]
- **Parallelizable:** Yes (frontend, independent)
- **Depends on:** None
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

### WI-V31-006: Projections Page Export UI
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

### WI-V31-007: Transactions Page Export UI
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

## Phase v3.2: PDF Chart Export
**Status:** Not Started
**Depends on:** Can start after WI-V31-001
**Estimated work items:** 4

### WI-V32-001: PDF Generation Service
- **Status:** [ ]
- **Parallelizable:** No (foundation)
- **Depends on:** None
- **Files:**
  - Add `QuestPDF` NuGet package
  - `FinanceEngine.Api/Services/PdfExportService.cs` (NEW)
- **Task:** Set up PDF generation infrastructure
- **Verification:**
  ```bash
  dotnet build
  ```
- **Acceptance:** QuestPDF integrated, service structure in place

### WI-V32-002: Chart PDF Endpoint
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

### WI-V32-003: Chart Capture Utility
- **Status:** [ ]
- **Parallelizable:** Yes (frontend, independent)
- **Depends on:** None
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

### WI-V32-004: PDF Export UI
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

## Phase v3.3: Extended Data Exports
**Status:** Not Started
**Depends on:** Phase v3.1 complete
**Estimated work items:** 5

### WI-V33-001: Account Summary Export
- **Status:** [ ]
- **Parallelizable:** Yes
- **Depends on:** WI-V31-001
- **Files:**
  - `FinanceEngine.Api/Endpoints/ExportEndpoints.cs`
  - `FinanceEngine.Api/Services/ExportService.cs`
- **Task:** Export account list with current balances
- **Details:**
  - Columns: Name, Type, CurrentBalance, LastUpdated, IsActive
- **Verification:**
  ```bash
  dotnet test --filter "FullyQualifiedName~ExportEndpoints"
  ```
- **Acceptance:** Accounts export in CSV/Excel

### WI-V33-002: Recurring Contributions Export
- **Status:** [ ]
- **Parallelizable:** Yes
- **Depends on:** WI-V31-001
- **Files:**
  - `FinanceEngine.Api/Endpoints/ExportEndpoints.cs`
  - `FinanceEngine.Api/Services/ExportService.cs`
- **Task:** Export recurring contribution schedules
- **Details:**
  - Columns: Name, FromAccount, ToAccount, Amount, Frequency, NextDate
- **Verification:**
  ```bash
  dotnet test --filter "FullyQualifiedName~ExportEndpoints"
  ```
- **Acceptance:** Recurring schedules export in CSV/Excel

### WI-V33-003: Full Data Export (Multi-Sheet Excel)
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
- **Verification:**
  ```bash
  dotnet test --filter "FullyQualifiedName~ExportEndpoints"
  ```
- **Acceptance:** Single Excel file with all financial data

### WI-V33-004: Accounts Page Export UI
- **Status:** [ ]
- **Parallelizable:** Yes (after WI-V33-001)
- **Depends on:** WI-V33-001
- **Files:**
  - `dashboard/src/app/pages/accounts/accounts.ts`
  - `dashboard/src/app/pages/accounts/accounts.html`
- **Task:** Add export button to Accounts page
- **Verification:**
  ```bash
  cd dashboard && npm run build
  ```
- **Acceptance:** Users can export account list

### WI-V33-005: Settings Page Full Export
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
- **Verification:**
  ```bash
  cd dashboard && npm run build
  ```
- **Acceptance:** Users can export all data from settings

---

## Phase v3.4: Transaction Import
**Status:** Not Started
**Depends on:** Can start independently (uses same ClosedXML package)
**Estimated work items:** 10

### WI-V34-001: Import Infrastructure
- **Status:** [ ]
- **Parallelizable:** No (foundation)
- **Depends on:** None
- **Files:**
  - `FinanceEngine.Api/Endpoints/ImportEndpoints.cs` (NEW)
  - `FinanceEngine.Api/Services/ImportService.cs` (NEW)
- **Task:** Set up import endpoint structure and file upload handling
- **Verification:**
  ```bash
  dotnet build
  ```
- **Acceptance:** Base endpoint file created, file upload handling works

### WI-V34-002: CSV Parser Service
- **Status:** [ ]
- **Parallelizable:** No
- **Depends on:** WI-V34-001
- **Files:**
  - `FinanceEngine.Api/Services/CsvImportService.cs` (NEW)
  - `FinanceEngine.Tests/Services/CsvImportServiceTests.cs` (NEW)
- **Task:** Parse CSV files and extract transaction data
- **Details:**
  - Auto-detect delimiter (comma, semicolon, tab)
  - Handle quoted fields
  - Return column headers and preview rows
- **Verification:**
  ```bash
  dotnet test --filter "FullyQualifiedName~CsvImportService"
  ```
- **Acceptance:** CSV files parse correctly

### WI-V34-003: Excel Parser Service
- **Status:** [ ]
- **Parallelizable:** Yes (after WI-V34-001)
- **Depends on:** WI-V34-001
- **Files:**
  - `FinanceEngine.Api/Services/ExcelImportService.cs` (NEW)
  - `FinanceEngine.Tests/Services/ExcelImportServiceTests.cs` (NEW)
- **Task:** Parse Excel files and extract transaction data
- **Details:**
  - Support .xlsx format using ClosedXML
  - Handle multiple sheets (let user select)
  - Return column headers and preview rows
- **Verification:**
  ```bash
  dotnet test --filter "FullyQualifiedName~ExcelImportService"
  ```
- **Acceptance:** Excel files parse correctly

### WI-V34-004: Column Mapping Engine
- **Status:** [ ]
- **Parallelizable:** No
- **Depends on:** WI-V34-002, WI-V34-003
- **Files:**
  - `FinanceEngine.Api/Services/ColumnMappingService.cs` (NEW)
  - `FinanceEngine.Api/Models/ImportModels.cs` (NEW)
- **Task:** Map source columns to transaction fields
- **Details:**
  - Required mappings: Date, Amount
  - Optional mappings: Description, Category, Reference
  - Auto-detect common bank formats (Amex, Chase, BofA patterns)
  - Validate mapped data before import
- **Verification:**
  ```bash
  dotnet build
  ```
- **Acceptance:** Columns map to transaction fields correctly

### WI-V34-005: Duplicate Detection
- **Status:** [ ]
- **Parallelizable:** Yes (after WI-V34-004)
- **Depends on:** WI-V34-004
- **Files:**
  - `FinanceEngine.Api/Services/DuplicateDetectionService.cs` (NEW)
  - `FinanceEngine.Tests/Services/DuplicateDetectionTests.cs` (NEW)
- **Task:** Detect and flag potential duplicate transactions
- **Details:**
  - Match on Date + Amount + Description (fuzzy)
  - Flag duplicates in preview, let user decide
  - Option to skip all duplicates
- **Verification:**
  ```bash
  dotnet test --filter "FullyQualifiedName~DuplicateDetection"
  ```
- **Acceptance:** Duplicates detected and flagged

### WI-V34-006: Import Preview Endpoint
- **Status:** [ ]
- **Parallelizable:** No
- **Depends on:** WI-V34-004, WI-V34-005
- **Files:**
  - `FinanceEngine.Api/Endpoints/ImportEndpoints.cs`
  - `FinanceEngine.Tests/Endpoints/ImportEndpointsTests.cs` (NEW)
- **Task:** POST endpoint to preview import before committing
- **Details:**
  - Accept: file, column mapping, target account
  - Return: parsed transactions with duplicate flags
  - No database writes yet
- **Verification:**
  ```bash
  dotnet test --filter "FullyQualifiedName~ImportEndpoints"
  ```
- **Acceptance:** Preview returns accurate transaction list

### WI-V34-007: Import Commit Endpoint
- **Status:** [ ]
- **Parallelizable:** No
- **Depends on:** WI-V34-006
- **Files:**
  - `FinanceEngine.Api/Endpoints/ImportEndpoints.cs`
- **Task:** POST endpoint to commit previewed import
- **Details:**
  - Accept: preview session ID, list of transactions to import
  - Create FinancialEvent records
  - Return: count of imported transactions
- **Verification:**
  ```bash
  dotnet test --filter "FullyQualifiedName~ImportEndpoints"
  ```
- **Acceptance:** Transactions saved to database

### WI-V34-008: Import UI - File Upload
- **Status:** [ ]
- **Parallelizable:** Yes (frontend, independent)
- **Depends on:** None
- **Files:**
  - `dashboard/src/app/pages/import/import.ts` (NEW)
  - `dashboard/src/app/pages/import/import.html` (NEW)
  - `dashboard/src/app/pages/import/import.scss` (NEW)
  - `dashboard/src/app/app.routes.ts`
- **Task:** Create import page with file upload
- **Details:**
  - Drag-and-drop file upload zone
  - Accept .csv and .xlsx files
  - Show file info after selection
- **Verification:**
  ```bash
  cd dashboard && npm run build
  ```
- **Acceptance:** Users can upload files

### WI-V34-009: Import UI - Column Mapping
- **Status:** [ ]
- **Parallelizable:** No
- **Depends on:** WI-V34-008
- **Files:**
  - `dashboard/src/app/pages/import/import.ts`
  - `dashboard/src/app/pages/import/import.html`
- **Task:** Column mapping interface
- **Details:**
  - Display detected columns from file
  - Dropdowns to map each column to transaction field
  - Show sample data for each column
  - Account selector for target account
- **Verification:**
  ```bash
  cd dashboard && npm run build
  ```
- **Acceptance:** Users can map columns

### WI-V34-010: Import UI - Preview and Confirm
- **Status:** [ ]
- **Parallelizable:** No
- **Depends on:** WI-V34-009
- **Files:**
  - `dashboard/src/app/pages/import/import.ts`
  - `dashboard/src/app/pages/import/import.html`
- **Task:** Preview and confirm import
- **Details:**
  - Table showing transactions to import
  - Highlight duplicates with warning icon
  - Checkbox to include/exclude each row
  - "Import Selected" button
  - Success summary after import
- **Verification:**
  ```bash
  cd dashboard && npm run build
  ```
- **Acceptance:** Users can review and confirm import

---

## Agent Assignment Log

| Work Item | Agent | Started | Completed |
|-----------|-------|---------|-----------|
| (none yet) | | | |

---

## Verification Commands Reference

```bash
# Full verification suite
dotnet build && dotnet test && cd dashboard && npm run build

# Backend only
dotnet build && dotnet test

# Frontend only
cd dashboard && npm run build && npm test

# Run servers
dotnet run --project FinanceEngine.Api  # Terminal 1
cd dashboard && npm start               # Terminal 2
```
