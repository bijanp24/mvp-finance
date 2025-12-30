# ROADMAP-v4.md

Last updated: 2025-12-30
Version: 4.0 (Cloud Integrations)

## Purpose
Structured work items for v4 features. Optimized for parallel agent execution.

See `CLOUD_INTEGRATIONS.md` for the full v4 vision.

## How to Use This File
1. Pick an unclaimed work item from the current phase
2. Mark it `[IN PROGRESS]` with your agent identifier
3. Complete the work item
4. Run the verification command
5. Mark it `[DONE]` and update WORKLOG-v4.md
6. Move to next item or hand off

## Parallel Execution Rules
- Items marked `Parallelizable: Yes` can run simultaneously
- Items with `Depends on:` must wait for dependencies
- Backend and frontend items in same feature should run sequentially
- Google and Microsoft integrations can run in parallel

---

## Prerequisites
- v3 complete (local file import/export working)
- Google Cloud Console project with OAuth credentials
- Microsoft Azure AD app registration
- Environment variables configured for OAuth secrets

---

## Phase v4.1: Google Workspace Integration
**Status:** Not Started
**Estimated work items:** 10

### WI-V41-001: Google OAuth Infrastructure
- **Status:** [ ]
- **Parallelizable:** No (foundation)
- **Depends on:** None
- **Files:**
  - Add `Google.Apis.Auth` NuGet package
  - `FinanceEngine.Api/Services/GoogleAuthService.cs` (NEW)
  - `FinanceEngine.Api/Endpoints/GoogleAuthEndpoints.cs` (NEW)
  - `FinanceEngine.Api/Models/OAuthModels.cs` (NEW)
- **Task:** Set up Google OAuth 2.0 flow
- **Details:**
  - Login endpoint initiates OAuth redirect
  - Callback endpoint exchanges code for tokens
  - Store refresh token encrypted in database
  - Status endpoint checks if user is connected
- **Verification:**
  ```bash
  dotnet build
  ```
- **Acceptance:** OAuth flow completes, tokens stored

### WI-V41-002: Google Sheets Service
- **Status:** [ ]
- **Parallelizable:** No
- **Depends on:** WI-V41-001
- **Files:**
  - Add `Google.Apis.Sheets.v4` NuGet package
  - `FinanceEngine.Api/Services/GoogleSheetsService.cs` (NEW)
  - `FinanceEngine.Tests/Services/GoogleSheetsServiceTests.cs` (NEW)
- **Task:** Service to create and populate Google Sheets
- **Details:**
  - Create new spreadsheet with title
  - Add data to sheet (headers + rows)
  - Format columns (dates, currency)
  - Return spreadsheet URL
- **Verification:**
  ```bash
  dotnet test --filter "FullyQualifiedName~GoogleSheetsService"
  ```
- **Acceptance:** Spreadsheets created with correct data

### WI-V41-003: Export Projections to Google Sheets
- **Status:** [ ]
- **Parallelizable:** No
- **Depends on:** WI-V41-002
- **Files:**
  - `FinanceEngine.Api/Endpoints/CloudExportEndpoints.cs` (NEW)
  - `FinanceEngine.Tests/Endpoints/CloudExportEndpointsTests.cs` (NEW)
- **Task:** Endpoint to export projection data to Google Sheets
- **Details:**
  - Reuse projection data from ForwardSimulationEngine
  - Create sheet with Date, Account, Balance, NetWorth columns
  - Support date range filtering
- **Verification:**
  ```bash
  dotnet test --filter "FullyQualifiedName~CloudExportEndpoints"
  ```
- **Acceptance:** Projections export to Google Sheets

### WI-V41-004: Export Transactions to Google Sheets
- **Status:** [ ]
- **Parallelizable:** Yes (after WI-V41-002)
- **Depends on:** WI-V41-002
- **Files:**
  - `FinanceEngine.Api/Endpoints/CloudExportEndpoints.cs`
- **Task:** Endpoint to export transaction history to Google Sheets
- **Details:**
  - Export events with Date, Type, Description, Amount, Account
  - Support date range filtering
- **Verification:**
  ```bash
  dotnet test --filter "FullyQualifiedName~CloudExportEndpoints"
  ```
- **Acceptance:** Transactions export to Google Sheets

### WI-V41-005: Google Docs Service
- **Status:** [ ]
- **Parallelizable:** Yes (after WI-V41-001)
- **Depends on:** WI-V41-001
- **Files:**
  - Add `Google.Apis.Docs.v1` NuGet package
  - `FinanceEngine.Api/Services/GoogleDocsService.cs` (NEW)
- **Task:** Service to create Google Docs with formatted content
- **Details:**
  - Create new document with title
  - Add headings, paragraphs, tables
  - Insert images (charts as base64)
  - Return document URL
- **Verification:**
  ```bash
  dotnet build
  ```
- **Acceptance:** Documents created with formatted content

### WI-V41-006: Export Summary Report to Google Docs
- **Status:** [ ]
- **Parallelizable:** No
- **Depends on:** WI-V41-005
- **Files:**
  - `FinanceEngine.Api/Endpoints/CloudExportEndpoints.cs`
  - `FinanceEngine.Api/Services/ReportGeneratorService.cs` (NEW)
- **Task:** Generate monthly summary report as Google Doc
- **Details:**
  - Title, date range, account summary table
  - Budget vs actual section (if v2 budgets exist)
  - Goal progress section (if v2 goals exist)
  - Embed projection chart image
- **Verification:**
  ```bash
  dotnet build
  ```
- **Acceptance:** Summary reports export to Google Docs

### WI-V41-007: Google Drive Upload
- **Status:** [ ]
- **Parallelizable:** Yes (after WI-V41-001)
- **Depends on:** WI-V41-001
- **Files:**
  - Add `Google.Apis.Drive.v3` NuGet package
  - `FinanceEngine.Api/Services/GoogleDriveService.cs` (NEW)
- **Task:** Service to upload files to Google Drive
- **Details:**
  - Upload PDF files
  - Create in root or specified folder
  - Return file URL
- **Verification:**
  ```bash
  dotnet build
  ```
- **Acceptance:** Files upload to Drive

### WI-V41-008: Settings UI - Google Connection
- **Status:** [ ]
- **Parallelizable:** Yes (frontend, independent)
- **Depends on:** None
- **Files:**
  - `dashboard/src/app/pages/settings/settings.ts`
  - `dashboard/src/app/pages/settings/settings.html`
  - `dashboard/src/app/core/services/google-auth.service.ts` (NEW)
- **Task:** UI to connect/disconnect Google account
- **Details:**
  - "Connect Google Account" button
  - Show connected email when linked
  - "Disconnect" option
  - Status indicator
- **Verification:**
  ```bash
  cd dashboard && npm run build
  ```
- **Acceptance:** Users can manage Google connection

### WI-V41-009: Export Dialog - Google Options
- **Status:** [ ]
- **Parallelizable:** No
- **Depends on:** WI-V41-008
- **Files:**
  - `dashboard/src/app/shared/components/export-dialog/` (NEW or extend existing)
- **Task:** Add Google export options to export dialog
- **Details:**
  - "Export to Google Sheets" option
  - "Export to Google Docs" option (for reports)
  - Show only if Google connected
  - Success message with link to created file
- **Verification:**
  ```bash
  cd dashboard && npm run build
  ```
- **Acceptance:** Users can export to Google from dialog

### WI-V41-010: Projections Page - Google Export
- **Status:** [ ]
- **Parallelizable:** No
- **Depends on:** WI-V41-009, WI-V41-003
- **Files:**
  - `dashboard/src/app/pages/projections/projections.ts`
  - `dashboard/src/app/pages/projections/projections.html`
- **Task:** Add Google export button to Projections page
- **Verification:**
  ```bash
  cd dashboard && npm run build
  ```
- **Acceptance:** Users can export projections to Google Sheets

---

## Phase v4.2: Microsoft 365 Integration
**Status:** Not Started
**Depends on:** Can run in parallel with v4.1
**Estimated work items:** 10

### WI-V42-001: Microsoft OAuth Infrastructure
- **Status:** [ ]
- **Parallelizable:** Yes (independent of Google)
- **Depends on:** None
- **Files:**
  - Add `Microsoft.Identity.Client` NuGet package
  - `FinanceEngine.Api/Services/MicrosoftAuthService.cs` (NEW)
  - `FinanceEngine.Api/Endpoints/MicrosoftAuthEndpoints.cs` (NEW)
- **Task:** Set up Microsoft OAuth 2.0 / MSAL flow
- **Details:**
  - Support personal and work/school accounts
  - Login endpoint initiates OAuth redirect
  - Callback endpoint exchanges code for tokens
  - Store refresh token encrypted
- **Verification:**
  ```bash
  dotnet build
  ```
- **Acceptance:** OAuth flow completes, tokens stored

### WI-V42-002: Microsoft Graph Service
- **Status:** [ ]
- **Parallelizable:** No
- **Depends on:** WI-V42-001
- **Files:**
  - Add `Microsoft.Graph` NuGet package
  - `FinanceEngine.Api/Services/MicrosoftGraphService.cs` (NEW)
- **Task:** Base service for Microsoft Graph API calls
- **Details:**
  - Initialize Graph client with user token
  - Helper methods for common operations
- **Verification:**
  ```bash
  dotnet build
  ```
- **Acceptance:** Graph client initialized and working

### WI-V42-003: Excel Online Service
- **Status:** [ ]
- **Parallelizable:** No
- **Depends on:** WI-V42-002
- **Files:**
  - `FinanceEngine.Api/Services/ExcelOnlineService.cs` (NEW)
  - `FinanceEngine.Tests/Services/ExcelOnlineServiceTests.cs` (NEW)
- **Task:** Service to create Excel files in OneDrive
- **Details:**
  - Create new workbook in OneDrive
  - Add worksheet with headers and data
  - Format columns
  - Return file URL
- **Verification:**
  ```bash
  dotnet test --filter "FullyQualifiedName~ExcelOnlineService"
  ```
- **Acceptance:** Excel files created in OneDrive

### WI-V42-004: Export Projections to Excel Online
- **Status:** [ ]
- **Parallelizable:** No
- **Depends on:** WI-V42-003
- **Files:**
  - `FinanceEngine.Api/Endpoints/CloudExportEndpoints.cs`
- **Task:** Endpoint to export projection data to Excel Online
- **Details:**
  - Same data as Google Sheets export
  - Create in user's OneDrive root
- **Verification:**
  ```bash
  dotnet test --filter "FullyQualifiedName~CloudExportEndpoints"
  ```
- **Acceptance:** Projections export to Excel Online

### WI-V42-005: Export Transactions to Excel Online
- **Status:** [ ]
- **Parallelizable:** Yes (after WI-V42-003)
- **Depends on:** WI-V42-003
- **Files:**
  - `FinanceEngine.Api/Endpoints/CloudExportEndpoints.cs`
- **Task:** Endpoint to export transactions to Excel Online
- **Verification:**
  ```bash
  dotnet test --filter "FullyQualifiedName~CloudExportEndpoints"
  ```
- **Acceptance:** Transactions export to Excel Online

### WI-V42-006: Word Online Service
- **Status:** [ ]
- **Parallelizable:** Yes (after WI-V42-002)
- **Depends on:** WI-V42-002
- **Files:**
  - `FinanceEngine.Api/Services/WordOnlineService.cs` (NEW)
- **Task:** Service to create Word documents in OneDrive
- **Details:**
  - Create new document
  - Add formatted content (headings, paragraphs, tables)
  - Insert images
  - Return file URL
- **Verification:**
  ```bash
  dotnet build
  ```
- **Acceptance:** Word documents created in OneDrive

### WI-V42-007: Export Summary Report to Word
- **Status:** [ ]
- **Parallelizable:** No
- **Depends on:** WI-V42-006
- **Files:**
  - `FinanceEngine.Api/Endpoints/CloudExportEndpoints.cs`
- **Task:** Generate summary report as Word document
- **Details:**
  - Same content as Google Docs report
  - Save to OneDrive
- **Verification:**
  ```bash
  dotnet build
  ```
- **Acceptance:** Summary reports export to Word

### WI-V42-008: Settings UI - Microsoft Connection
- **Status:** [ ]
- **Parallelizable:** Yes (frontend, independent)
- **Depends on:** None
- **Files:**
  - `dashboard/src/app/pages/settings/settings.ts`
  - `dashboard/src/app/pages/settings/settings.html`
  - `dashboard/src/app/core/services/microsoft-auth.service.ts` (NEW)
- **Task:** UI to connect/disconnect Microsoft account
- **Details:**
  - "Connect Microsoft Account" button
  - Show connected email when linked
  - "Disconnect" option
- **Verification:**
  ```bash
  cd dashboard && npm run build
  ```
- **Acceptance:** Users can manage Microsoft connection

### WI-V42-009: Export Dialog - Microsoft Options
- **Status:** [ ]
- **Parallelizable:** No
- **Depends on:** WI-V42-008
- **Files:**
  - `dashboard/src/app/shared/components/export-dialog/`
- **Task:** Add Microsoft export options to export dialog
- **Details:**
  - "Export to Excel Online" option
  - "Export to Word" option (for reports)
  - Show only if Microsoft connected
- **Verification:**
  ```bash
  cd dashboard && npm run build
  ```
- **Acceptance:** Users can export to Microsoft 365

### WI-V42-010: Transactions Page - Cloud Export
- **Status:** [ ]
- **Parallelizable:** No
- **Depends on:** WI-V42-009, WI-V42-005
- **Files:**
  - `dashboard/src/app/pages/transactions/transactions.ts`
- **Task:** Add cloud export options to Transactions page
- **Verification:**
  ```bash
  cd dashboard && npm run build
  ```
- **Acceptance:** Users can export transactions to cloud

---

## Phase v4.3: Report Templates (Optional Enhancement)
**Status:** Not Started
**Depends on:** v4.1 and v4.2 complete
**Estimated work items:** 5

### WI-V43-001: Report Template Entity
- **Status:** [ ]
- **Parallelizable:** No (foundation)
- **Files:**
  - `FinanceEngine.Data/Entities/ReportTemplateEntity.cs` (NEW)
  - New EF migration
- **Task:** Store customizable report templates
- **Details:**
  - Name, Sections (JSON), DateRangeType, IsDefault
- **Acceptance:** Entity created, migration applied

### WI-V43-002: Built-in Templates
- **Status:** [ ]
- **Depends on:** WI-V43-001
- **Files:**
  - `FinanceEngine.Api/Services/ReportGeneratorService.cs`
- **Task:** Create default report templates
- **Details:**
  - Monthly Summary
  - Goal Progress Report
  - Annual Review
- **Acceptance:** Default templates available

### WI-V43-003: Template Selection UI
- **Status:** [ ]
- **Depends on:** WI-V43-002
- **Files:**
  - `dashboard/src/app/shared/components/report-dialog/` (NEW)
- **Task:** UI to select and customize report template
- **Acceptance:** Users can choose report template

### WI-V43-004: Template Customization
- **Status:** [ ]
- **Depends on:** WI-V43-003
- **Files:**
  - Report dialog component
- **Task:** Allow users to customize report sections
- **Acceptance:** Users can toggle sections on/off

### WI-V43-005: Reports Page
- **Status:** [ ]
- **Depends on:** WI-V43-004
- **Files:**
  - `dashboard/src/app/pages/reports/` (NEW)
- **Task:** Dedicated page for generating reports
- **Details:**
  - Template selection
  - Date range picker
  - Export destination (local, Google, Microsoft)
  - Report preview (optional)
- **Acceptance:** Users can generate reports from dedicated page

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
