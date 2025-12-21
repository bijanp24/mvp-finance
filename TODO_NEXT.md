# TODO_NEXT.md

Read this first when resuming work.

## Current focus
- Get Git workflow back on track: feature branch, work-item branches, and task/commit mapping.
- Clean and consolidate documentation for human + agent handoff.

## Next actions (do now)
1. Decide the current initiative name and create/check out the feature branch.
2. List work items for the initiative and create `wi/<ticket>-<slug>` branches.
3. Map each task to a checkpoint commit and note doc consolidation needs.

## Commands to run
- `git status`
- `git branch --show-current`
- `git switch -c feature/<name>` (if not already on a feature branch)
- `git switch -c wi/<ticket>-<slug>` (from the feature branch)
- `rg --files -g '*.md'` (locate docs to consolidate before commits)

## Blockers / questions
- What is the current feature name?
- What are the initial work items (tickets + short slugs)?
