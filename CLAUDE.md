# Dungeon — Project Constitution

This file is the authoritative reference for all Claude Code agents working in this repository.
Read this first. Read `docs/` files only when relevant to the current task.

## Project

- **Engine:** Unity 6 (6000.4.7f1), C#, URP, Input System
- **Genre:** 2D top-down dungeon management game
- **Concept:** Two brothers — one living (shopkeeper/villager), one dead (dungeon explorer)
- **Platform:** PC (Windows primary)

## Repository Structure

```
Dungeon/          # Unity project (the only Unity project folder)
docs/             # Project direction, architecture, design docs (owner-maintained)
tasks/            # Agent task tracking: backlog/ → active/ → review/ → completed/
todo/             # Owner's personal tasks (assets, decisions) — owner-managed
worktrees/        # Git worktrees for agent branches (gitignored)
scripts/          # PowerShell helper scripts
Submodules/       # Shared code packages (FactorialFunShared)
Shaders/          # External shader code
.claude/agents/   # Agent definitions (manager, programmer, reviewer, tester)
```

## Assembly Architecture

**Dependency rule:** Logic depends on nothing. Visuals depends on Logic. UI depends on both.

| Assembly | Purpose | Dependencies |
|----------|---------|-------------|
| `Dungeon.Logic` | Pure C#. Game state, grid, entities, systems. No UnityEngine.Rendering. | None |
| `Dungeon.Visuals` | Unity rendering, input, camera, editor tools. | Dungeon.Logic, Unity.InputSystem |
| `Dungeon.UI` | UI layer (currently empty). | Dungeon.Logic, Dungeon.Visuals |

**Logic assembly purity is non-negotiable.** Dungeon.Logic must never reference Visuals, UI, or UnityEngine.Rendering.

## Coding Conventions

- Explicit types everywhere unless the line becomes unwieldy
- Explicit, understandable variable names
- Do not remove existing comments; add comments where logic is non-obvious
- Always use braces, even for single-line if/for bodies
- Prefer longer lines over unnecessary splits (ultrawide monitor workflow)
- Public members: `PascalCase`
- Private members: `_lowerCamelCaseWithLeadingUnderscore`
- Constants: `SCREAMING_SNAKE_CASE`

## Entity System

- Every entity is a `WorldObject` with composition-based features
- New behaviour = new Feature class + a System that processes it
- No inheritance for entity types — composition only
- Systems are stateless processors; objects are data containers

## Mandatory Rules

1. **Never break compilation.** Every change must compile in Unity.
2. **Logic assembly purity.** See above.
3. **No scene editing** unless performed through Unity Editor APIs or the Unity MCP server.
4. **Preserve .meta files.** Never delete, rename, or move Unity assets without handling .meta files.
5. **No automatic pushes or merges.** The owner commits, pushes, and merges.
6. **No broadening scope.** Complete the assigned task, nothing more.
7. **No inventing facts.** Do not describe unfinished systems as if they exist.
8. **Minimize token usage.** Read only relevant files. Keep reports under 500 words.
9. **No new packages** without owner approval.
10. **Stop on conflict.** When requirements conflict, stop and ask the owner.
11. **No API billing changes.** Never enable pay-as-you-go or usage credits.

## Git Workflow

- Substantial work: create branch `agent/<task-id>-<slug>`, use worktree under `worktrees/`
- Small docs/config changes: work directly on current branch, no worktree needed
- Never push, merge, or delete branches without explicit owner authorization
- One repair attempt on failure, then convert to blocked task or human task in `todo/`
- Maximum two Claude instances active at once

## Testing

- Tests go in `Dungeon.Tests.EditMode` (references Logic) or `Dungeon.Tests.PlayMode` (references Logic)
- Prioritize: pure Logic tests, grid behaviour, serialization, determinism
- Use `scripts/run-unity-tests.ps1` for batch test execution
- CI runs on GitHub Actions via game-ci/unity-test-runner

## Agent System

Four agents defined in `.claude/agents/`:

| Agent | Role |
|-------|------|
| **Manager** | Receives owner requests, plans work, delegates, tracks progress |
| **Programmer** | Implements one bounded task at a time |
| **Reviewer** | Reviews completed work for correctness and architecture |
| **Tester** | Writes and runs tests for completed features |

The owner communicates with the Manager. The Manager delegates to the others.
Never launch more than one worker concurrently.

## Documentation Reference

| File | Purpose | Maintained by |
|------|---------|---------------|
| `docs/PROJECT_DIRECTION.md` | Vision, goals, priorities | Owner |
| `docs/ARCHITECTURE.md` | Technical architecture details | Owner + Manager |
| `docs/GAME_DESIGN.md` | Game mechanics, loops, content design | Owner |
| `docs/STYLE_GUIDE.md` | Code and art style rules | Owner |
| `docs/ROADMAP.md` | Phase plan and milestones | Owner + Manager |
| `docs/CURRENT_STATUS.md` | What is done, what is next, blockers | Manager |
| `docs/TESTING.md` | Test strategy and coverage | Manager + Tester |
| `docs/OWNER_WORKFLOW.md` | How to use this system | Owner reference |
