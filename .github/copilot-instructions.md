# Copilot Instructions (Auto-Loaded)

This file is intentionally minimal to avoid duplication.

Source of truth: `AGENTS.md`
- Conventions, workflow, and handoff rules live there.
- Use `mvp-finance.md` for architecture and feature specs.
- Use `PROGRESS.md` for current status and file references.
- Use `ROADMAP.md` for work items and parallel execution.

## File-Scoped Copilot Rules
- Make atomic, surgical edits focused on the current file only.
- If multiple changes are needed in one file, batch them into one edit.
- Use `multi_replace_string_in_file` for multiple fixes in one file.
- Avoid scope creep; do not touch other files unless explicitly asked.
- Preserve existing formatting, patterns, and style in the file.
- Avoid speculative refactors; change only what the request requires.
- Add or change imports only when necessary for the edit.
- Watch for common mistakes:
  - Missing imports/dependencies
  - Type mismatches (especially accidental `any`)
  - Angular pattern violations (decorators vs functions, signals rules)
  - Dead code or unused variables
  - Accessibility issues (labels, contrast, ARIA)

## Handoff for Small Fixes
- Run verification (build/test) before committing.
- Trivial fixes: allow a direct commit without WORKLOG updates.
- Non-trivial fixes: add a brief WORKLOG entry (fix + reason).
- Update `TODO_NEXT.md` only if it changes the next actions.
