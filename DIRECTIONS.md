ightweight Multi-Agent Unity Project Setup

You are setting up a practical, low-cost Claude Code workflow for an existing Unity game project.

I use the approximately €20 Claude Pro subscription. Usage is limited and shared between Claude and Claude Code. Optimize this setup aggressively for low token consumption. Do not build an enterprise orchestration platform.

Project information
Workspace parent folder: D:\Projects\Personal\Dungeon
Existing Unity project folder: D:\Projects\Personal\Dungeon\Dungeon
Submodules: D:\Projects\Personal\Dungeon\Submodules
GitHub repository: git@github.com:zory/Dungeon.git
Readme: README.md
Project direction: PROJECT.md
Intended architecture: ARCHITECTURE.md
Gamedesign: GAME_DESIGN.md
Style guide: STYLE_GUIDE.md

Inspect the existing project before deciding how to configure anything.

Objective

Create a lightweight workflow where I communicate primarily with one Manager agent.

The Manager should:

Understand the project direction and current code.
Maintain a small roadmap and task backlog.
Break my requests into implementation tasks.
Delegate implementation to one worker agent when useful.
Delegate independent review and testing to one reviewer agent.
Track work requiring me in a separate human-task directory.
Preserve state in files so work can resume in a new Claude session.
Minimize Claude usage and avoid unnecessary agents or repeated repository analysis.
Hard resource limits

Design around these limits:

Maximum two Claude Code instances active at once.
Normally one Manager and one worker.
No permanent background processes.
No continuous autonomous development loop.
No automatic retries beyond one repair attempt.
No Agent SDK service unless absolutely necessary.
No Docker, Kubernetes, databases, queues, dashboards, or distributed infrastructure.
No automatic GitHub issue creation.
No automatic pushes or merges.
I will commit, push, and merge unless I explicitly authorize otherwise.
Prefer scripts and repository files over additional AI calls.
Prefer deterministic tests over AI reviewers.
Reuse prior reports instead of repeatedly rereading the entire repository.
Required agents

Create only these project-level Claude Code agents:

1. Manager

Responsibilities:

Receive high-level requests from me.
Read project direction, architecture, roadmap, current status, and relevant code.
Determine whether the request needs clarification.
Create one or more bounded task files.
Select either the Programmer or Reviewer.
Track dependencies.
Maintain status documentation.
Create human tasks when necessary.
Avoid implementing large features directly.
Avoid launching workers when the task is small enough to complete efficiently in the current session.
Never launch more than one worker concurrently.
Stop when usage limits are approaching.

The Manager must not reread the entire repository for every request. Maintain concise architecture and status files.

2. Programmer

Responsibilities:

Implement one bounded task.
Read only the relevant project documentation and code.
Work in an isolated Git worktree when the change is substantial.
Follow existing architecture.
Run relevant compilation and tests.
Write a concise completion report.
Never push or merge.
Never broaden the task without Manager approval.

The Programmer should handle gameplay, world systems, rendering, UI, editor tooling, and ordinary Unity work. Do not create permanent specialist agents for each subsystem.

3. Reviewer

Responsibilities:

Independently review one completed task.
Examine changed files and relevant surrounding code.
Run or inspect relevant tests.
Identify correctness, Unity lifecycle, performance, serialization, and architectural problems.
Return either APPROVED or CHANGES_REQUESTED.
Avoid rewriting code unless assigned a separate repair task.
Keep review concise and focused on meaningful issues.

A temporary specialist agent may be created only when a task clearly requires expertise not handled adequately by these three roles. Remove or archive unnecessary temporary agents afterward.

Required repository files

Create or update:

CLAUDE.md

docs/
  PROJECT_DIRECTION.md
  ARCHITECTURE.md
  ROADMAP.md
  CURRENT_STATUS.md
  TESTING.md
  OWNER_WORKFLOW.md

tasks/
  backlog/
  active/
  review/
  completed/
  human/

.claude/
  agents/
    manager.md
    programmer.md
    reviewer.md

Use the exact supported Claude Code project configuration locations currently available.

Do not create dozens of documents. Keep each document concise and useful.

CLAUDE.md requirements

Create a project constitution containing:

Unity version and main technology choices.
Project purpose and game pillars.
Existing architecture summary.
Coding conventions.
Directory and subsystem boundaries.
Unity serialization and .meta file safety.
Testing expectations.
Performance expectations.
Git and worktree rules.
Definition of done.
Prohibited operations.
Requirement to preserve compiling behavior.
Requirement not to invent facts about unfinished systems.
Requirement to minimize token usage.

Include instructions to read only documents and code relevant to the current task.

Task format

Use simple Markdown task files rather than a complex database or JSON system.

Each task should contain:

ID
Title
Status
Priority
Owner
Objective
Relevant files
Requirements
Acceptance criteria
Tests
Dependencies
Human dependencies
Result

Use statuses:

BACKLOG
READY
ACTIVE
WAITING_FOR_HUMAN
REVIEW
CHANGES_REQUESTED
DONE

Do not create tasks for trivial actions unless tracking them is useful.

Human tasks

Anything I must personally do must be placed in:

tasks/human/

Examples:

Create or modify artwork.
Choose between visual designs.
Supply credentials.
Install software.
Configure GitHub permissions.
Perform subjective playtesting.
Purchase or license assets.
Record audio.
Make product decisions.

Each human task must independently contain:

Why I am needed.
Exact required output.
Step-by-step instructions.
Relevant references.
File format, dimensions, naming, and destination.
Verification checklist.
What work is blocked by it.

Do not write vague instructions such as “create assets.”

Git workflow

For substantial implementation tasks:

Ensure the main working tree is clean.
Create a branch named agent/<task-id>-<slug>.
Create a worktree under the workspace-level worktrees/ folder.
Let the Programmer modify only that worktree.
Run validation.
Let the Reviewer inspect the diff.
Report results to me.
Do not merge, push, or delete unmerged work.

For small documentation or configuration changes, avoid worktree overhead when safe.

Create only a few small helper scripts:

scripts/create-worktree
scripts/remove-worktree
scripts/run-unity-tests
scripts/project-status

Use PowerShell if this is a Windows workspace. Add shell equivalents only when genuinely useful.

Unity automation

Create minimal, reliable Unity automation.

Detect the Unity version from:

ProjectSettings/ProjectVersion.txt

Provide configurable scripts for:

Opening or locating the correct Unity Editor.
Running EditMode tests in batch mode.
Running PlayMode tests in batch mode.
Producing test-result XML and log files.
Verifying that the project compiles.
Optionally creating a development build when a build method already exists or can be added safely.

Do not build a custom MCP server during initial setup.

Do not add extensive Unity editor tooling unless the current project needs it.

Do not edit Unity YAML assets blindly when Unity Editor APIs are safer.

Testing

Audit existing tests first.

Create only foundational tests that provide immediate value.

Prioritize:

Core pure C# logic.
Procedural generation determinism.
Grid and chunk behavior.
Save/load behavior when present.
Previously observed bugs.
Scene-loading smoke tests where practical.

Do not attempt to establish complete visual regression, performance farms, or hundreds of generated tests during bootstrap.

Every implementation task should specify the smallest relevant validation set.

Token-saving rules

Apply these rules throughout the system:

Keep task context narrow.
Do not paste entire files into reports.
Refer to paths and symbols.
Keep completion reports below approximately 500 words.
Keep review reports below approximately 500 words unless severe problems require more.
Do not regenerate unchanged architectural summaries.
Update CURRENT_STATUS.md rather than reconstructing project history.
Use deterministic scripts for status and tests.
Do not ask multiple agents to solve the same task.
Use the Reviewer only for substantial or risky changes.
Skip independent AI review for trivial documentation changes.
Stop after one failed implementation and one repair attempt.
Convert unresolved work into a blocked or human task.
Do not continue automatically after the Claude usage limit is reached.
Preserve all state before stopping.
Cost safety

Do not configure API billing or usage credits automatically.

Do not switch from subscription usage to paid API usage.

Do not enable pay-as-you-go usage.

Document how I can ensure that work stops when subscription usage is exhausted.

If Claude Code offers usage-credit settings, leave additional paid usage disabled unless I explicitly request it.

Initial setup procedure

Perform these stages:

Stage 1: Audit

Inspect:

Repository status.
Unity version.
Project structure.
Assembly definitions.
Packages.
Existing tests.
Existing documentation.
Current architecture.
Recent Git history.
Known compilation state where practical.

Do not modify code during this stage.

Stage 2: Concise setup proposal

Report:

Current project state.
Proposed three-agent workflow.
Files to create or update.
Any permissions or installations required.
Any blocking questions.

Avoid a long enterprise architecture proposal.

Stage 3: Implement setup

After resolving true blockers:

Create the documentation.
Create the three agents.
Create task directories and one task template.
Create minimal worktree scripts.
Create minimal Unity test scripts.
Create owner instructions.
Create an initial roadmap and backlog based on existing work and my supplied direction.

Do not refactor unrelated game code.

Stage 4: Validate

Run:

Script syntax checks.
Repository status checks.
Unity compilation or tests if Unity is available.
One harmless sample task through the Manager → Programmer → Reviewer workflow.

Do not push or merge.

Final output

When setup is complete, report only:

Created

Files and systems created.

Validation

What was tested and the results.

Manual actions

Only actions I must perform.

Start command

Exact way to start or address the Manager.

Normal workflow

A brief example of giving the Manager a feature request.

Stop and recovery

How to stop safely and resume after usage resets.

Limitations

Important remaining limitations.

Begin by auditing the existing Unity project. Ask questions only when the answer cannot reasonably be obtained from the repository or supplied project information.