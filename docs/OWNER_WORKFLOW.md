# Owner Workflow

## Quick Start

1. Open Claude Code in `D:\Projects\Personal\Dungeon`
2. Address the Manager: `/agents/manager`
3. Describe what you want in plain language
4. The Manager will plan, delegate, and report back

## Example Requests

```
"Add the Health feature to WorldObject with a CombatSystem that processes damage"
"Review the last programmer task"
"What's the current project status?"
"Create a task for adding elevation layer switching"
```

## What You Manage

- **docs/** — Fill and update direction files. Agents read these for context.
- **todo/** — Your personal task list. Create `.md` files for each task. Move to `todo/done/` when complete.
- **Git** — You commit, push, and merge. Agents never do this automatically.
- **Assets** — Art, audio, and visual design decisions are always yours.

## Task Flow

```
You → Manager → creates task in tasks/backlog/
Manager → moves to tasks/active/ → delegates to Programmer
Programmer → completes → Manager moves to tasks/review/
Manager → delegates to Reviewer and/or Tester
Reviewer/Tester → approves or requests changes
Manager → moves to tasks/completed/ → reports to you
```

## Stopping and Resuming

- All state is in files (tasks/, docs/CURRENT_STATUS.md)
- You can close Claude Code at any time
- Next session: address the Manager, it reads CURRENT_STATUS.md and task files to resume
- If usage limit is hit: agents preserve state before stopping

## Scripts

| Script | Purpose |
|--------|---------|
| `scripts/create-worktree.ps1` | Create a git worktree for an agent branch |
| `scripts/remove-worktree.ps1` | Clean up a worktree after merge |
| `scripts/run-unity-tests.ps1` | Run Unity EditMode/PlayMode tests |
| `scripts/project-status.ps1` | Show project status summary |

## Cost Safety

- Claude Pro subscription only — no API billing
- Agents stop when usage limit approaches
- No pay-as-you-go is ever enabled automatically
