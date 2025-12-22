# Orchestration.md

Last updated: 2025-12-22

## Purpose
Formal multi-agent execution process for parallel work in Cursor. This file is the source of truth for orchestration steps, worker contracts, and conflict rules.

## Quick Start
1. Read `TODO_NEXT.md` and `ROADMAP.md` to pick parallelizable work items.
2. Ask the Orchestrator to generate worker contracts from this SOP.
3. Create one new Cursor agent per contract and paste each contract into a separate agent.
4. Ensure each agent works on its own `wi/<ticket>-<slug>` branch.
5. Collect agent reports and integrate changes.

## Current Multi-Agent Panel Usage
Until multi-agent creation is automated, the user manually creates one agent per contract.
To make copy/paste easy, the Orchestrator must output contracts in a labeled, block format:

```
CONTRACT 1 - <WI-ID>
Goal: ...
Allowed paths: ...
Forbidden paths: ...
Definition of done:
- ...
Output:
- ...
```

## Roles
- Orchestrator: owns planning, task partitioning, constraints, integration, and final polish.
- Worker agents: execute exactly one bounded work item each.

## Golden Rules
- One branch per work item.
- No two workers modify the same file.
- Shared or central files are owned only by the Orchestrator.
- Workers must not expand scope.

## Orchestrator Workflow
1. Read, in order: `AGENTS.md`, `TODO_NEXT.md`, `ROADMAP.md`, `WORKLOG.md`, `PROGRESS.md`.
2. Create a task map with:
   - Exact files to change
   - Verification command(s)
   - Clear acceptance criteria
3. Update the Agent Assignment Log in `ROADMAP.md`.
4. Issue worker contracts.
5. Collect worker reports.
6. Integrate, review for conflicts, and polish.
7. Update handoff docs as required by `AGENTS.md`.

## Worker Workflow
1. Create branch: `wi/<ticket>-<slug>`.
2. Make changes only in allowed paths.
3. Run verification if feasible; otherwise note "not run".
4. Report results in the required format.

## Worker Contract Template
Goal: <one sentence>
Allowed paths: <folders/files>
Forbidden paths: <folders/files>
Definition of done:
- <tests or build command>
- <expected behavior change>
Output:
- Short summary
- Files changed
- Commands run + results
- Risks/follow-ups

## Agent Status Protocol
- "Starting <WI-...>"
- "Blocked: <reason>"
- "PR ready: <branch>"
- "Needs orchestrator: <shared file>"

## Conflict Avoidance
- Core routing, shared configs, and central models are Orchestrator-only.
- If a task touches a shared file, the worker must stop and notify the Orchestrator.
- If two tasks must change the same file, merge them into a single work item.

## Branching
- Worker branches: `wi/<ticket>-<slug>`
- Optional integration branch: `feature/<initiative>`

## Required Worker Output
- Summary of changes
- List of files changed
- Commands run and results
- Follow-ups or risks

## Orchestrator Checklist
- [ ] Agent Assignment Log updated
- [ ] Worker contracts issued
- [ ] Changes integrated and reviewed
- [ ] `WORKLOG.md` updated
- [ ] `TODO_NEXT.md` updated
- [ ] `ROADMAP.md` statuses updated
- [ ] `PROGRESS.md` updated if scope or issues changed
- [ ] Working state snapshot recorded
