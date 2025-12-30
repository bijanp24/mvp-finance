# v4: Cloud Integrations

Last updated: 2025-12-30
Version: 4.0 (Cloud Integrations)

## Vision

Enable users to seamlessly integrate MVP Finance with their Google Workspace and Microsoft 365 accounts. Users can:
- **Sign in** with Google or Microsoft accounts (OAuth)
- **Export directly** to Google Sheets, Google Docs, OneDrive, Excel Online
- **Generate reports** as Google Docs or Microsoft Word documents
- **Backup data** automatically to cloud storage

This builds on v3's local file import/export by adding cloud connectivity.

## Scope

**In Scope (v4):**
- OAuth authentication with Google and Microsoft
- Export to Google Sheets (projection data, transaction history)
- Export to Google Docs (summary reports with charts)
- Export to Microsoft Excel Online / OneDrive
- Export to Microsoft Word (summary reports)
- PDF report generation for Google Drive / OneDrive upload

**Out of Scope (Future):**
- Real-time sync (Plaid/bank connections)
- Automatic scheduled backups
- Multi-user collaboration
- Import from Google Sheets/Excel Online

---

## Sub-Version Breakdown

### v4.1: Google Workspace Integration
**Focus:** OAuth + Google Sheets/Docs/Drive export

**Features:**
- Sign in with Google (OAuth 2.0)
- Export projection data to Google Sheets
- Export transaction history to Google Sheets
- Export summary report to Google Docs (with embedded charts)
- Upload PDF reports to Google Drive

### v4.2: Microsoft 365 Integration
**Focus:** OAuth + OneDrive/Excel/Word export

**Features:**
- Sign in with Microsoft (OAuth 2.0 / MSAL)
- Export projection data to Excel Online
- Export transaction history to Excel Online
- Export summary report to Word Online
- Upload PDF reports to OneDrive

### v4.3: Report Templates
**Focus:** Customizable report generation

**Features:**
- Pre-built report templates (Monthly Summary, Goal Progress, Annual Review)
- Template customization (select sections, date ranges)
- Branded exports (user logo, colors)
- Scheduled report generation (future)

---

## Technical Approach

### Authentication

**Google OAuth 2.0:**
- Use Google Identity Services
- Scopes: `https://www.googleapis.com/auth/spreadsheets`, `https://www.googleapis.com/auth/documents`, `https://www.googleapis.com/auth/drive.file`
- Store refresh tokens securely (encrypted in database)

**Microsoft OAuth 2.0 (MSAL):**
- Use Microsoft Authentication Library
- Scopes: `Files.ReadWrite`, `User.Read`
- Support both personal Microsoft accounts and work/school accounts

### Backend Architecture

```
FinanceEngine.Api/
  Services/
    GoogleAuthService.cs      - OAuth flow handling
    GoogleSheetsService.cs    - Sheets API integration
    GoogleDocsService.cs      - Docs API integration
    GoogleDriveService.cs     - Drive API integration
    MicrosoftAuthService.cs   - MSAL flow handling
    OneDriveService.cs        - OneDrive/Graph API integration
    ExcelOnlineService.cs     - Excel Online via Graph API
    WordOnlineService.cs      - Word Online via Graph API
  Endpoints/
    GoogleAuthEndpoints.cs    - OAuth callback endpoints
    MicrosoftAuthEndpoints.cs - MSAL callback endpoints
    CloudExportEndpoints.cs   - Export to cloud endpoints
```

### Frontend Architecture

```
dashboard/src/app/
  core/services/
    google-auth.service.ts
    microsoft-auth.service.ts
    cloud-export.service.ts
  pages/
    settings/
      cloud-connections/      - Manage connected accounts
    export/
      cloud-export-dialog/    - Export to cloud UI
```

### API Endpoints

```
# Google OAuth
GET  /api/auth/google/login          - Initiate OAuth flow
GET  /api/auth/google/callback       - OAuth callback
POST /api/auth/google/revoke         - Disconnect account
GET  /api/auth/google/status         - Check connection status

# Microsoft OAuth
GET  /api/auth/microsoft/login       - Initiate MSAL flow
GET  /api/auth/microsoft/callback    - MSAL callback
POST /api/auth/microsoft/revoke      - Disconnect account
GET  /api/auth/microsoft/status      - Check connection status

# Cloud Export
POST /api/export/google/sheets       - Export to new Google Sheet
POST /api/export/google/docs         - Export report to Google Doc
POST /api/export/google/drive        - Upload file to Drive
POST /api/export/microsoft/excel     - Export to Excel Online
POST /api/export/microsoft/word      - Export report to Word
POST /api/export/microsoft/onedrive  - Upload file to OneDrive
```

---

## User Stories

### Google Integration
1. **As a user**, I can connect my Google account so I can export to Google Workspace.
2. **As a user**, I can export my projections to a new Google Sheet with one click.
3. **As a user**, I can generate a monthly summary report as a Google Doc.
4. **As a user**, I can save PDF reports directly to my Google Drive.
5. **As a user**, I can disconnect my Google account from Settings.

### Microsoft Integration
6. **As a user**, I can connect my Microsoft account so I can export to Microsoft 365.
7. **As a user**, I can export my transaction history to Excel Online.
8. **As a user**, I can generate a goal progress report as a Word document.
9. **As a user**, I can save backup exports to OneDrive.
10. **As a user**, I can see which cloud accounts are connected in Settings.

---

## Security Considerations

### Token Storage
- Refresh tokens encrypted at rest
- Access tokens short-lived, refreshed as needed
- Tokens scoped to minimum required permissions

### User Consent
- Clear explanation of what data is shared
- Users can revoke access at any time
- No automatic background access without user action

### Data Privacy
- Only export what user explicitly requests
- No reading from user's cloud storage
- Audit log of export actions (future)

---

## Dependencies and Packages

### Backend (NuGet)
- `Google.Apis.Sheets.v4` - Google Sheets API
- `Google.Apis.Docs.v1` - Google Docs API
- `Google.Apis.Drive.v3` - Google Drive API
- `Google.Apis.Auth` - Google OAuth
- `Microsoft.Identity.Client` - MSAL for Microsoft OAuth
- `Microsoft.Graph` - Microsoft Graph API (Excel, Word, OneDrive)

### Frontend (npm)
- `@angular/oauth2-oidc` (optional, if handling OAuth on frontend)
- Or handle OAuth entirely via backend redirects

---

## Environment Configuration

```json
// appsettings.json (secrets in user-secrets or env vars)
{
  "Google": {
    "ClientId": "xxx.apps.googleusercontent.com",
    "ClientSecret": "xxx",
    "RedirectUri": "https://localhost:5001/api/auth/google/callback"
  },
  "Microsoft": {
    "ClientId": "xxx-xxx-xxx",
    "ClientSecret": "xxx",
    "TenantId": "common",
    "RedirectUri": "https://localhost:5001/api/auth/microsoft/callback"
  }
}
```

---

## Success Metrics

- Users can connect cloud accounts in < 30 seconds
- Exports to cloud complete in < 10 seconds
- Zero token leaks or security incidents
- 95%+ success rate on export operations

---

## Future Considerations (v5+)

- **Real-time bank sync** via Plaid or similar
- **Scheduled exports** (weekly summary to Drive)
- **Import from cloud** (read from Google Sheets)
- **Multi-device sync** via cloud storage
- **Collaboration** features (share projections)
