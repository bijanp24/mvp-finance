# MVP Finance - Project-Wide Development Guidelines

## Git Workflow & Branching Strategy

### Branch Structure
This project follows a structured branching model aligned with Plane.so project management:

**Hierarchy:** Projects → Epics → Modules → Work Items

**Branch Naming Conventions:**
- **Main branch:** `master` (production/deployable code)
- **Module branches:** `module/<module-name>` (created from `master`)
- **Work Item branches:** `work-item/<unique-key>-<short-description>` (created from parent Module branch)
  - Example: `work-item/FIN-123-spendable-calculator-ui`

### Development Workflow

**Working on a Module:**
1. Create Module branch from `master`: `git checkout -b module/<module-name>`
2. For each Work Item in the Module:
   - Create Work Item branch from Module: `git checkout -b work-item/<key>-<desc>`
   - Complete development work
   - Verify dev complete
   - Consolidate documentation (see Documentation Rules below)
   - Merge into Module: `git checkout module/<module-name> && git merge --no-ff work-item/<key>-<desc>`
   - Delete Work Item branch: `git branch -d work-item/<key>-<desc>`

**Completing a Module:**
1. Merge latest `master` into Module: `git checkout module/<module-name> && git merge --no-ff master`
2. Resolve any conflicts
3. Test thoroughly
4. Merge Module into `master`: `git checkout master && git merge --no-ff module/<module-name>`
5. Delete Module branch: `git branch -d module/<module-name>`
6. Deploy from `master`

### Merge Strategy
- **Always use `--no-ff` (no fast-forward)** to preserve branch history and create merge commits
- This maintains a clear visual history of feature branches in the Git tree
- **Squashing:** May be used in the future; not implemented yet

### Branch Cleanup
- Delete Work Item branches after merging into Module
- Delete Module branches after merging into `master`
- Use `git branch -d <branch-name>` (will warn if not merged)

### Pull Request Process
- **Current approach:** Local merges only (no remote repository yet)
- When a remote is added (GitHub, Azure DevOps, etc.), the PR process will be formalized
- All Git history will be preserved when pushing to remote

## Documentation & Commit Rules

### Markdown Consolidation
**Before ANY commit**, consolidate all `.md` files in each folder into a single canonical file:

- **Root folder:** All `.md` files → `mvp-finance.md`
- **Dashboard folder:** All `.md` files → `dashboard.md`
- **Other folders:** All `.md` files → `{folder-name}.md`

**Exceptions (never consolidate):**
- `CLAUDE.md` files (this file and `dashboard/CLAUDE.md`)
- `README.md` files

**Process:**
1. Merge all content from working `.md` files into the canonical file
2. Remove any image references from consolidated markdown
3. Delete redundant source `.md` files
4. Stage and commit changes

**Note:** Work Item markdown files are managed by the developer during the work item process. Claude only consolidates before commits.

### Images
- Development images (mockups, screenshots, diagrams) are excluded from commits
- Images are stored in Google Workspace and referenced in external docs
- `.gitignore` excludes: `*.png`, `*.jpg`, `*.jpeg`, `*.gif` (except public assets and favicon)
- Remove image references when consolidating markdown files

## Commit Message Format

Follow conventional commit format:

```
<type>: <short description>

<optional body>

<optional footer>
```

**Types:**
- `feat:` New feature
- `fix:` Bug fix
- `docs:` Documentation changes
- `chore:` Maintenance tasks
- `refactor:` Code refactoring
- `test:` Test additions/changes
- `style:` Code style changes (formatting, etc.)

**Footer:**
Always include Claude Code attribution:
```
🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
```

## Project Structure

```
mvp-finance/
├── CLAUDE.md                    # This file - project-wide rules
├── mvp-finance.md              # Consolidated project documentation
├── .gitignore                  # Git exclusions
├── mvp-finance.sln             # .NET solution
├── FinanceEngine/              # Core calculation library
├── FinanceEngine.Data/         # EF Core data layer
├── FinanceEngine.Api/          # ASP.NET Core Web API
├── FinanceEngine.Tests/        # xUnit test suite
└── dashboard/                  # Angular 21 frontend
    ├── CLAUDE.md               # Angular/TypeScript-specific rules
    ├── README.md               # Angular CLI documentation
    └── dashboard.md            # Consolidated frontend documentation
```

## Technology Stack

**Backend:**
- .NET 10.0
- Entity Framework Core 10.0.1
- SQLite
- xUnit 2.9.3
- ASP.NET Core Minimal APIs

**Frontend:**
- Angular 21.0.5
- Angular Material 21.0.3
- TypeScript 5.9.2
- SCSS
- RxJS 7.8.0

## Development Commands

**Backend:**
```bash
dotnet build                    # Build solution
dotnet test                     # Run tests
dotnet run --project FinanceEngine.Api  # Run API
```

**Frontend:**
```bash
cd dashboard
npm install                     # Install dependencies
npm start                       # Run dev server (http://localhost:4200)
npm run build                   # Production build
npm test                        # Run tests
```

## Notes for Claude Code

- When working on **frontend** code, follow both this file AND `dashboard/CLAUDE.md`
- When working on **backend** code, follow only this file
- Always consolidate documentation before commits
- Always use `--no-ff` when merging
- Delete branches after successful merge
- Verify dev complete before merging Work Items
