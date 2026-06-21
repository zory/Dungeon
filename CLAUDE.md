# Dungeon — Project Constitution

This file is the authoritative reference for all Claude Code agents working in this repository.
Read this first. Read `docs/` files only when relevant to the current task.

## Project

- **Engine:** Unity 6 (6000.4.7f1), C#, URP, Input System
- **Genre:** 2D top-down dungeon management game
- **Concept:** Two brothers — one living (shopkeeper/villager), one dead (dungeon builder)
- **Platform:** PC (Windows primary)

## Repository Structure

```
Dungeon/          # Unity project (the only Unity project folder)
docs/             # Project direction, architecture, design docs (owner-maintained)
tasks/            # Agent task tracking: backlog/ → active/ → review/ → completed/
worktrees/        # Git worktrees for agent branches (gitignored)
scripts/          # PowerShell helper scripts
Submodules/       # Shared code packages (FactorialFunShared)
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

- Tests go in `Dungeon.Tests.EditMode` (references Logic) or `Dungeon.Tests.PlayMode` (references Logic). If tests are testing submodule features, then there is tests asmef in submodule esxisting project.
- Prioritize: pure Logic tests, grid behaviour, serialization, determinism
- Use `scripts/run-unity-tests.ps1` for batch test execution
- CI runs on GitHub Actions via game-ci/unity-test-runner

## Agent System — You Are the Manager

**Every session in this project operates as the Manager agent by default.**
Follow the full workflow in `.claude/agents/manager.md`.

The owner talks only to you (the Manager). You delegate automatically using the **Agent tool**:

| Sub-agent | When to spawn | Instructions source |
|-----------|---------------|---------------------|
| **Programmer** | Implementation needed | `.claude/agents/programmer.md` |
| **Reviewer** | Code ready for review | `.claude/agents/reviewer.md` |
| **Tester** | Tests need to be written/run | `.claude/agents/tester.md` |

**How to delegate:** Use the `Agent` tool. In the prompt, tell the sub-agent to read its role file (e.g. `.claude/agents/programmer.md`) and `CLAUDE.md` for project rules, then describe the specific task. Sub-agents have full tool access and work autonomously.

**Sub-agents:** If you were spawned via the Agent tool, ignore this section. Follow the instructions in your launch prompt instead.

Never launch more than one worker sub-agent concurrently.

## Documentation Reference

| File | Purpose | Maintained by |
|------|---------|---------------|
| `docs/PROJECT_DIRECTION.md` | Vision, goals, priorities | Owner |
| `docs/ARCHITECTURE.md` | Technical architecture details | Owner + Manager |
| `docs/GAME_DESIGN.md` | Game mechanics, loops, content design | Owner |
| `docs/STYLE_GUIDE.md` | Code and art style rules | Owner |
| `docs/CURRENT_STATUS.md` | What is done, what is next, blockers | Manager |
| `docs/TESTING.md` | Test strategy and coverage | Manager + Tester |
| `docs/OWNER_WORKFLOW.md` | How to use this system | Owner reference |
