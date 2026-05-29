---
name: opus
description: A systematic coding methodology that enforces deep thinking before writing code. Covers problem understanding, codebase reconnaissance, architectural design, execution ordering, real-time coding discipline, debugging, and verification. Invoke this skill to approach any coding task with maximum rigor.
---
# Opus — Systematic Coding Methodology
This skill defines a **mandatory thought process** to follow before, during, and after writing any non-trivial code. It applies to new features, UI rebuilds, refactors, bug fixes, and any task that touches more than a few lines.
> **Core Principle:** 80% of the work is NOT typing code. It's thinking about what to type and verifying what you typed. Premature coding is the #1 source of wasted effort.
---
## When to Activate This Skill
Use this skill when:
- Building a new feature or component
- Rebuilding or significantly modifying a UI
- Debugging a non-trivial bug
- Refactoring existing code
- Making architectural decisions
- Any task where "just start coding" feels tempting but risky
Do NOT use this skill for:
- One-line fixes, typo corrections, or trivial formatting changes
- Tasks the user explicitly says to "just do quickly"
---
## Phase 1: Interrogation (Before Touching Anything)
**Goal:** Deeply understand what is being asked. Do NOT start coding.
### For Feature Requests, ask:
- What is the end goal — not just the feature, but the *why* behind it?
- Who are the users? This shapes API design, error messages, performance targets, and UX.
- What are the constraints? (Tech stack, performance budgets, backwards compatibility, deployment environment, team conventions)
- What are the edge cases? Mentally walk through failure modes early.
### For "Rebuild the UI" requests, ask:
- What's wrong with the current UI? Is it ugly, slow, or functionally broken?
- What should it look like? Is there a reference? (Spotlight, PowerToys Run, Raycast, Alfred, a Figma mockup)
- Are we keeping the same features and just reskinning, or rearchitecting the component structure?
- What tech stack is the current UI?
### For "Add X" requests, ask:
- What exactly is X? (e.g., "add search" — search *what*? Files? Apps? Everything?)
- How should it behave? (Instant keystroke-by-keystroke, or press Enter?)
- Is there an existing data source, or do we need to build one?
- What's the performance expectation?
### Why this matters:
If you skip this and just start coding, you might build a beautiful search bar that searches the wrong thing, or rebuild a UI in React when the user wanted native WPF. **The cost of asking 5 questions is 2 minutes. The cost of building the wrong thing is hours.**
### Action Items:
1. Write down the answers to these questions (mentally or in a scratch file)
2. If answers are unclear, ASK the user before proceeding
3. Only move to Phase 2 once you have clarity on the goal, constraints, and scope
---
## Phase 2: Reconnaissance (Reading Before Writing)
**Goal:** Build a complete mental model of the existing codebase before making changes.
### Mandatory Exploration Sequence:
```
Step 1: List the root directory
  → What's the project shape? Monorepo? Single app? Library?
Step 2: Read the manifest file (package.json, Cargo.toml, .csproj, etc.)
  → What are the dependencies? What's already available?
Step 3: Find the entry point (main.js, App.tsx, Program.cs, etc.)
  → Where does execution start? How does data flow?
Step 4: Trace the component/module tree
  → How is the code organized? What depends on what?
Step 5: Search for existing patterns (grep/search)
  → How does the codebase handle:
    - Error handling (try/catch? Result types? Error codes?)
    - State management (Redux? Context? Signals? ViewModel?)
    - Styling approach (CSS modules? Tailwind? Inline? XAML?)
    - Data fetching (REST? GraphQL? Direct DB?)
    - Logging and observability
Step 6: Read the style/design files
  → What's the design system? Colors, typography, spacing?
Step 7: Check for tests
  → What's the testing strategy? What's the safety net?
```
### Key Principle: Match Existing Patterns
Do NOT introduce new patterns unless the existing ones are fundamentally broken. If the codebase uses Result types for errors, use Result types. If it uses CSS modules, use CSS modules. Consistency > personal preference.
### Action Items:
1. Complete all 7 steps above for any non-trivial change
2. Note any patterns you must follow
3. Note any technical debt or landmines you spotted
4. Only move to Phase 3 once you understand the codebase architecture
---
## Phase 3: Architectural Design (The Crucial Thinking)
**Goal:** Decompose the problem into components and make explicit design decisions before writing code.
### Step 3a: Identify Core Abstractions
Ask yourself:
- What are the **nouns** in the system? (User, App, SearchResult, Filter) → these become types/classes/tables
- What are the **verbs**? (search, filter, rank, launch, validate) → these become functions/methods
- What are the **boundaries**? Where does one module end and another begin?
### Step 3b: Define Interfaces First
Think about **contracts between components** before implementations:
```
Example: "The SearchEngine needs to expose a method that takes a query string
and returns a ranked list of results. The caller shouldn't need to know 
if we're using fuzzy matching, regex, or an index internally."
```
### Step 3c: Data Flow Analysis
Trace how data moves through the system:
- **Input** → validation → transformation → **processing** → formatting → **output**
- Where are the bottlenecks?
- Where could data be corrupted or lost?
### Step 3d: State Management
Decide explicitly:
- What state exists? Where does it live?
- What's the source of truth?
- How do you handle stale state, concurrent mutations, and consistency?
### Step 3e: Error Strategy
Decide upfront:
- What errors are **expected** (user typo) vs **unexpected** (disk failure)?
- How do errors propagate?
- What does the user see when something fails?
### Step 3f: Document Trade-offs
For every significant decision, document the trade-off in a table:
```
| Decision        | Choice              | Why                                          |
|-----------------|----------------------|----------------------------------------------|
| Search algorithm| Fuzzy scoring        | Users expect partial/out-of-order matches    |
| Debounce input  | Yes, 100ms           | Avoid thrashing but stay responsive           |
| Result limit    | Top 8                | More is visual noise; keyboard nav suffers    |
```
### Action Items:
1. Identify all core abstractions
2. Define interfaces/contracts between components
3. Map the data flow
4. Decide state management strategy
5. Decide error handling strategy
6. Document trade-offs for all significant decisions
7. Only move to Phase 4 once the design is solid
---
## Phase 4: Execution Ordering (What to Build First)
**Goal:** Sequence the work based on dependency order and risk. Build from the inside out.
### Standard Build Order:
```
Priority 1: Foundation (Data Layer / "Engine")
  → Data models, types, schemas
  → Core algorithms (search, filter, sort, validate)
  → WHY FIRST: If this doesn't work, nothing else matters
Priority 2: Core Components (The "Skeleton")
  → Main functional components with event handling
  → Business logic integration
  → WHY SECOND: Get it functional before making it pretty
Priority 3: Integration (The "Wiring")
  → Connect components to each other and to the host app
  → Handle lifecycle, focus, routing, navigation
  → WHY THIRD: Must work in context, not just isolation
Priority 4: Styling & Polish (The "Skin")
  → Visual design, animations, transitions, typography
  → Responsive layout, accessibility
  → WHY LAST: Polish on top of working code, never instead of it
```
### Risk-First Principle:
> Tackle the **riskiest and most uncertain parts first**. If something is going to break the design, you want to know early — not after building everything else on top of it.
### Action Items:
1. List all work items
2. Order them by dependency (what blocks what?)
3. Move risky/uncertain items earlier in the sequence
4. Create a task checklist tracking progress
---
## Phase 5: Writing Code (Real-Time Discipline)
**Goal:** Write code with intentionality, not on autopilot.
### Naming Discipline
- Names are the most important documentation. Spend real time on them.
- `processData()` tells you nothing. `validateAndNormalizeUserInput()` tells you everything.
- Match the naming conventions of the existing codebase.
### Function Design
- **Single responsibility** — each function does one thing well
- **Small surface area** — minimize parameters; 8+ arguments means restructure
- **Pure functions where possible** — same inputs → same outputs, no side effects
### Managing Complexity
- **Keep the "working set" small** — at any point, the reader shouldn't need to hold more than ~5 things in their head
- **Early returns** to avoid deep nesting
- **Extract complex conditionals** into well-named boolean variables or helper functions
- **Composition over inheritance** — build behavior by combining small pieces
### Defensive Programming
- Validate inputs at system boundaries (API endpoints, user input, file parsing)
- Use type systems aggressively — let the compiler catch bugs
- Handle the null/undefined/empty case explicitly, never ignore it
### Performance Awareness
- Don't optimize prematurely, but don't write obviously slow code either
- Be aware of algorithmic complexity — avoid O(n²) when O(n) is straightforward
- Watch for: N+1 query problems, memory allocation in hot loops, unnecessary re-renders
### Real-Time Inner Monologue
For every few lines of code, ask:
1. **"What could go wrong here?"** — null input, empty arrays, special characters, concurrency
2. **"Will the next developer understand this?"** — if not, rename or add a comment
3. **"Am I repeating something that already exists?"** — search before you write
4. **"Is there a simpler way?"** — less code = fewer bugs
---
## Phase 6: Verification (Proving It Works)
**Goal:** Systematically verify the implementation against requirements and edge cases.
### Testing Checklist:
- ✅ **Happy path** — does it work when everything goes right?
- ✅ **Boundary conditions** — empty lists, zero values, max values, single item
- ✅ **Error cases** — invalid input, network failure, permission denied, timeout
- ✅ **Concurrency** — race conditions, deadlocks (if applicable)
- ✅ **Regression** — did you break anything that was working before?
- ✅ **Performance** — does it meet the performance targets from Phase 1?
- ✅ **Accessibility** — can it be used with keyboard only? Screen reader?
### Verification Actions:
1. Run the full test suite
2. Build the project — catch compilation/type errors
3. Test manually against the original requirements
4. Check logs and error output for warnings or deprecation notices
5. Review your own code as if you're seeing it for the first time
### Self-Review Questions:
- **Can I delete code?** Less code = fewer bugs. Look for dead code, over-abstractions, YAGNI violations.
- **Is the code readable?** Imagine a developer seeing this for the first time in 6 months.
- **Are there implicit assumptions?** Make them explicit with assertions, types, or comments.
- **Did I repeat myself?** Look for structural duplication that could be unified.
---
## Phase 7: Debugging Framework (When Things Go Wrong)
**Goal:** Systematically diagnose and fix bugs using structured reasoning, not random changes.
### The Debugging Loop:
```
REPRODUCE → CHARACTERIZE → HYPOTHESIZE → LOCATE → UNDERSTAND → DESIGN → FIX → VERIFY
```
### Step-by-step:
#### 1. REPRODUCE
- Can you see the bug happen consistently?
- What are the exact steps to trigger it?
- Does it happen every time, or intermittently?
#### 2. CHARACTERIZE (Pattern Hunt)
- What works? What doesn't?
- What do the failing cases have in common?
- What do the passing cases have in common?
- Create two columns: ✅ Working vs ❌ Broken — and look for the pattern.
#### 3. HYPOTHESIZE
- Generate multiple hypotheses that could explain the pattern
- Rank them by likelihood
- For each hypothesis, predict: "If this hypothesis is correct, then I should also see X"
- Test the predictions to narrow down
#### 4. LOCATE
- Search the code **targeted**, not randomly — grep for the decision point
- Find the exact line(s) where the bug's behavior is determined
- Keywords to search: filter, exclude, include, whitelist, blacklist, isValid, shouldShow, etc.
#### 5. UNDERSTAND (Most Critical — Do NOT Skip)
- WHY is this code wrong? Not just WHAT is wrong.
- Is the bug a symptom of a deeper design flaw?
- Would patching this line just create another bug elsewhere?
> **This is where most people skip ahead.** They find the broken line, patch it, and move on. But if you don't understand WHY the approach was flawed, you'll write the same bug in a different form.
#### 6. DESIGN the Fix
- Consider the **right fix** vs the **quick fix**
- The quick fix patches the symptom. The right fix addresses the root cause.
- If the root cause is a fundamentally flawed approach, redesign it — don't add more band-aids.
**Example of Bad vs Good fix:**
```
BAD (Whack-a-Mole):
  "The filter misses apps in AppData, so add AppData to the whitelist"
  → Every new install location = another bug report
GOOD (Address Root Cause):
  "The filter uses install path as a proxy for importance — that's wrong.
   Replace with multi-signal scoring (Start Menu presence, desktop shortcut, 
   recent usage, registered app status)"
  → Robust against any install location
```
#### 7. FIX
- Implement the designed fix
- Keep the change minimal and focused — don't sneak in unrelated changes
#### 8. VERIFY
- Build a test matrix: what was broken before, what should work now
- Verify all previously-broken cases are fixed
- Verify all previously-working cases still work (no regressions)
- Add tests to prevent this class of bug from recurring
---
## Mental Models & Principles
These principles guide every decision throughout all phases:
| Principle | What It Means in Practice |
|-----------|--------------------------|
| **YAGNI** (You Aren't Gonna Need It) | Build for now, design for extensibility. Don't build for hypothetical futures. |
| **Separation of Concerns** | Each module/layer handles one responsibility. |
| **Fail Fast** | Detect errors early and loudly, not silently and late. |
| **Principle of Least Surprise** | Code should behave the way a reasonable developer would expect. |
| **Make it work, make it right, make it fast** | In that order. Never optimize before it works correctly. |
| **Locality of Behavior** | Related code should be close together. Don't force jumping across 10 files to understand one feature. |
| **Composition over Inheritance** | Build behavior by combining small, focused pieces. |
| **Invert the Problem** | If whitelisting is fragile, try blacklisting. If filtering is too aggressive, try scoring. |
---
## Effort Distribution
The expected ratio of thinking to coding:
```
████████████████████░░░░░  Understanding & Interrogation  (30%)
████████████████░░░░░░░░░  Design & Architecture          (25%)
████████████░░░░░░░░░░░░░  Writing Code                   (20%)
████████░░░░░░░░░░░░░░░░░  Testing & Debugging            (15%)
████░░░░░░░░░░░░░░░░░░░░░  Polishing & Refinement         (10%)
```
If you find yourself spending 80% of the time typing code, you are likely skipping phases and will pay for it later in bugs and rework.
---
## Quick Reference Checklist
Before writing ANY code, confirm:
- [ ] I understand the goal, constraints, and edge cases (Phase 1)
- [ ] I've explored the codebase and identified existing patterns (Phase 2)
- [ ] I've designed the architecture and documented trade-offs (Phase 3)
- [ ] I've ordered the work by dependency and risk (Phase 4)
While writing code, continuously check:
- [ ] Am I naming things clearly? (Phase 5)
- [ ] Am I handling edge cases? (Phase 5)
- [ ] Am I matching existing patterns? (Phase 5)
- [ ] Could this be simpler? (Phase 5)
After writing code, verify:
- [ ] Happy path works (Phase 6)
- [ ] Edge cases handled (Phase 6)
- [ ] No regressions (Phase 6)
- [ ] Code is self-reviewable (Phase 6)
When debugging:
- [ ] I can reproduce the bug (Phase 7)
- [ ] I've characterized the pattern (Phase 7)
- [ ] I understand the ROOT CAUSE, not just the symptom (Phase 7)
- [ ] My fix addresses the root cause, not just the symptom (Phase 7)
