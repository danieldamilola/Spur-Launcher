# Deep Research: Software Engineering — How to Code Like a Real Engineer
*General practices, any stack — compiled for AI ingestion*

---

## Part 1 — The Mindset Shift

Most beginners sit down to write code and think the goal is to make it work. That is half the job.

A real engineer asks a different question: **"Will this code be easy to change in 6 months?"**

Code that works today but can't be safely modified tomorrow is not a success — it's a liability. The difference between a codebase someone enjoys working in and one people dread is not the language or the framework. It is whether the engineers who built it applied a coherent set of principles about how software should be structured.

The transition from "code that works" to "code that endures" is not accidental. It is the result of deliberate practice guided by a set of principles that professional engineers apply — often without naming them out loud.

### The Three Shifts

**From: "Does it work?" → To: "Can someone else understand it in 6 months?" (including future you)**

**From: "I'll fix it later" → To: "What does 'later' actually cost?"** (Technical debt accrues interest. Every shortcut today means slower, riskier work tomorrow.)

**From: "I wrote it, so I understand it" → To: "Can this be understood without my explanation?"** Clean code explains itself. If you need to walk someone through what your code does, the code is not yet clean.

---

## Part 2 — Plan Before You Code

This is the most consistently violated rule in software development — and the one with the highest cost.

### The 40-20-40 Rule
Professional software engineering allocates:
- **40%** of time to planning and design
- **20%** of time to actual coding
- **40%** of time to testing and debugging

Most developers invert this — they spend 80% in code and 20% on everything else. The result is code written in the wrong direction, which then requires rewriting.

### What Planning Looks Like

**Step 1: Understand the problem completely.**
Before writing a single line, be able to articulate in plain language:
- What problem is this solving?
- Who experiences this problem?
- What does success look like?
- What are the edge cases?

If you can't explain the problem simply, you don't understand it well enough to code it.

**Step 2: Sketch the solution structure.**
Not code — structure. What are the major components? What data does each one own? What are the interfaces between them? A 15-minute sketch on paper prevents hours of refactoring later.

Questions to answer:
- What kind of thing am I building? (library? tool? service? UI?)
- What are the major concerns that need to be separated?
- What changes frequently? What stays stable?
- What are the inputs and outputs at each boundary?

**Step 3: Identify what you don't know.**
Every project has unknowns. Name them explicitly before coding. "I don't know how the file watching API behaves on Windows" is information. Discovering that at 3am after building the wrong abstraction is expensive.

**Step 4: Start with the simplest thing that could possibly work.**
Not the most elegant. Not the most scalable. The simplest. You can refine it once it works. You cannot refine something that doesn't exist yet.

**Step 5: Write tests before features.**
Not later — before. See TDD section below.

### The Senior Developer's First Question
When presented with a feature request or problem, a senior engineer's first question is almost never "how do I code this?" It's "what exactly needs to happen, and what's the simplest structure that allows that to happen reliably?" The coding is the last part of the thought process, not the first.

---

## Part 3 — Clean Code Principles

### Names Are Everything

The single most impactful improvement you can make to any codebase is better naming. Names are not cosmetic — they are documentation. A function named `calculateMonthlyInterestRate()` communicates its contract. A function named `calc()` does not.

**Rules for naming:**
- Name things by what they *are*, not how they're *implemented*
- Use full words, not abbreviations (`customerName` not `custNm`)
- Functions and methods should be verbs: `fetchUser()`, `validateConfig()`, `calculateScore()`
- Booleans should read as questions: `isActive`, `hasPermission`, `shouldRefresh`
- Classes and types should be nouns: `User`, `SearchResult`, `ConfigValidator`
- Avoid generic names: `data`, `info`, `temp`, `result`, `value`, `stuff`, `doThing()`
- If naming a function is hard, the function is probably doing too much

**The test:** Read your function/variable name out loud. Does it tell you what it does without looking at the implementation? If not, rename it.

### Functions Should Do One Thing

A function that does exactly one thing at one level of abstraction is easier to:
- Name (because it has a clear single purpose)
- Test (you can test exactly one behavior)
- Reuse (a focused function is more likely to be reusable)
- Change (a change to that one concern doesn't ripple unexpectedly)

**The 20-line guideline:** A function exceeding 20 lines of executable logic is a candidate for decomposition. This is not a rule — it's a signal that something might be doing too much.

**The one-level-of-abstraction rule:** Functions should operate at one level of abstraction. Don't mix high-level logic ("fetch and display the user's results") with low-level mechanics ("parse the JSON and map the fields") in the same function. Pull the low-level mechanics into their own function.

```
// Bad: mixed levels of abstraction
void ShowResults() {
    var json = File.ReadAllText("data.json");
    var parsed = JsonSerializer.Deserialize<List<Item>>(json);
    var filtered = parsed.Where(x => x.IsActive).ToList();
    foreach (var item in filtered) {
        Console.WriteLine($"{item.Name}: {item.Score}");
    }
}

// Better: each function has one job
void ShowResults() {
    var items = LoadActiveItems();
    DisplayItems(items);
}

List<Item> LoadActiveItems() {
    var all = LoadAllItems();
    return all.Where(x => x.IsActive).ToList();
}
```

### Comments: The Why, Not the What

Code should explain itself. Comments that explain *what* code does are a failure of the code, not documentation.

**Bad comment:**
```
// Loop through the list and add active items to result
foreach (var item in items) {
    if (item.IsActive) result.Add(item);
}
```

**Good comment:**
```
// Exclude archived items — archived state was added in v2 but never affects the display
foreach (var item in items) {
    if (item.IsActive) result.Add(item);
}
```

Comments should explain *why* a decision was made, especially when the code doesn't make it obvious. Document intent, constraints, trade-offs, and "don't change this or X will break."

### Don't Repeat Yourself (DRY)

Every piece of knowledge in a system should have a single, authoritative representation. If you change behavior that exists in one place, you shouldn't need to find and change it in three others.

DRY is not just about identical code blocks. It's about identical *decisions*. If the logic for what counts as an "active" item is defined in four different places — even if the code looks different in each — you have a DRY violation.

**How to apply it:** When you find yourself writing the same logic twice, extract it. When you find yourself updating the same value in multiple places, centralize it. When you find yourself explaining the same decision multiple times in comments, extract the decision into a named function.

**DRY's limits:** Don't over-DRY. Two pieces of code that happen to look similar but represent different concepts should not be merged. Merging them creates coupling where there shouldn't be any. The rule is "don't repeat the same *decision*," not "don't repeat the same code."

### KISS — Keep It Simple

The simplest solution that correctly solves the problem is the right solution. Not the most clever. Not the most extensible. The simplest.

Complexity has a cost that is paid continuously — every time someone reads the code, every time someone modifies it, every time something breaks and needs to be debugged. Simple code minimizes that ongoing cost.

**The simplicity test:** Can you explain what this code does to a junior developer in 30 seconds? If not, it's probably too complex.

### YAGNI — You Ain't Gonna Need It

Don't build features for requirements you don't have yet. Don't add abstractions for flexibility you might never need. Every piece of code that exists has to be maintained, understood, tested, and debugged. Code that doesn't exist has no maintenance cost.

The trap: "I'll add this abstraction now because we might need it later." But that future requirement may never come — or it may come with different constraints that make your abstraction wrong. Write the specific solution you need now. Generalize only when you have real evidence you need to.

---

## Part 4 — SOLID Principles

SOLID is a set of 5 principles for writing object-oriented code that is maintainable and extensible. They are named together but apply individually. Senior engineers use them constantly, often without labeling them.

### S — Single Responsibility Principle
**A class (or module, or function) should have one, and only one, reason to change.**

"One reason to change" means one concern. A class that handles both data persistence and business validation has two reasons to change — a new database, or a new validation rule — and changing one risks breaking the other.

In practice: if you can describe a class with "and" ("it handles X **and** Y"), it has more than one responsibility.

**Example:**
```
// Violation: one class doing two jobs
class User {
    public string Name { get; set; }
    public void SaveToDatabase() { ... }  // persistence responsibility
    public bool IsValid() { ... }          // validation responsibility
}

// Better: responsibilities separated
class User { public string Name { get; set; } }
class UserRepository { public void Save(User u) { ... } }
class UserValidator { public bool IsValid(User u) { ... } }
```

### O — Open-Closed Principle
**Code should be open for extension but closed for modification.**

When new requirements come in, you should be able to add new behavior by adding new code — not by modifying existing, tested code. Modifying existing code risks breaking existing behavior.

The mechanism: interfaces, abstract classes, and composition. New behavior is a new implementation of an existing interface, not a modification to an existing class.

### L — Liskov Substitution Principle
**Subtypes must be substitutable for their base types without altering the correctness of the program.**

If class B extends class A, anywhere you can use an A you should be able to use a B without anything breaking. If a subclass changes the fundamental contract of the parent — refuses to implement a method, or changes the meaning of an operation — it violates LSP and creates bugs that are hard to reason about.

In practice: before inheriting, ask "is this really an 'is-a' relationship?" Often composition is better than inheritance.

### I — Interface Segregation Principle
**Clients should not be forced to depend on methods they do not use.**

Large, general-purpose interfaces force implementations to provide methods they don't need. Split interfaces into smaller, more specific ones — each focused on one concern.

**Example:**
```
// Violation: large interface forces irrelevant implementations
interface IAction {
    void Execute();
    void Undo();        // not all actions support undo
    void Preview();     // not all actions have a preview
}

// Better: segregated interfaces
interface IAction { void Execute(); }
interface IUndoableAction : IAction { void Undo(); }
interface IPreviewableAction : IAction { void Preview(); }
```

### D — Dependency Inversion Principle
**High-level modules should not depend on low-level modules. Both should depend on abstractions.**

High-level code (your application logic) should not know about or depend directly on low-level code (your database, your file system, your HTTP client). If it does, changing the low-level implementation forces changes to high-level logic.

The solution: define an interface at the boundary. High-level code depends on the interface. Low-level code implements the interface. The two can evolve independently.

**Why it matters:** If your business logic directly creates a `SqlServerDatabase` instance, you can't switch to a different database, can't mock it in tests, and can't test the logic independently of the database. But if your logic depends on `IDataStore`, you can swap implementations freely.

---

## Part 5 — Architecture and Structure

### Think Before You Structure

The first question senior engineers ask when starting a new project is: **"What kind of thing am I building — and who is it for?"**

That answer determines the structure:
- A library → optimize for importability and clean public API
- A CLI tool → focus on entry points, argument parsing, testability of commands
- A desktop app → separate UI layer from business logic from data layer
- A service → separate concerns by domain, not by technical layer

Never structure by default. Structure by purpose.

### Separation of Concerns

Every system has multiple concerns: UI rendering, business logic, data access, configuration, external integrations. These concerns should be separated — placed in different layers, modules, or classes — so that a change to one does not require changes to the others.

In practice, this means:
- Your UI layer should not contain business logic
- Your business logic should not know about your database
- Your configuration should not be scattered throughout the codebase
- External service calls should be behind an abstraction layer

**Why it matters:** When concerns are mixed, a change to the UI requires touching business logic. A change to the database requires touching UI code. Changes become unpredictable and risky.

### Cohesion and Coupling

**High cohesion:** Related things are together. A module contains everything it needs to do its job, and only that.

**Low coupling:** Unrelated things are independent. Changing one module does not require changes to other modules.

These two properties define a well-structured codebase. High cohesion makes each piece easy to understand. Low coupling makes each piece easy to change.

**The danger of high coupling:** In tightly coupled systems, changes to one part have widespread impacts. Any modification requires extensive testing and potentially reworking other parts. The codebase becomes difficult to extend without rippling breakage.

### Project Structure

Senior engineers' first structural decision maps directly to the type of thing they're building. Some universal patterns:

**Naming by role, not by implementation:**
```
// Bad: named by technical type
Controllers/
Models/
Services/
Utils/

// Better: named by domain or concern
Search/
Clipboard/
AI/
Settings/
Shared/
```

The first approach groups by what something *is* (a model, a controller). The second groups by what something *does* (search, clipboard). Domain-organized code is easier to navigate because related things live together.

**Always have:**
- A clear entry point
- A clear separation between your code and third-party code
- A tests directory at the same level as your source
- A config layer that doesn't bleed into business logic

---

## Part 6 — How to Actually Approach a Problem

### The Sequence

1. **Understand the problem completely.** Not partially. Re-read the requirement. Ask "what are the edge cases?" Ask "what should this NOT do?" Write down the problem in your own words before touching code.

2. **Before trying to solve a problem, make sure it occurs.** Reproduce the bug. Confirm the requirement. Don't assume — verify.

3. **Think about the simplest possible solution.** Not the most elegant. Not the most extensible. The simplest. You can always generalize later. You can't simplify something that's already complex without risk.

4. **Map out the major pieces.** What are the inputs? What are the outputs? What transforms one into the other? Sketching this (even just in comments) before coding prevents writing in the wrong direction.

5. **Write a failing test first.** Before implementing, write a test that describes the expected behavior. If you can't write the test, you don't fully understand what "correct" looks like.

6. **Write the minimal code to make the test pass.** Not the final code. The minimal code. Then refactor.

7. **Refactor.** Now that it works, make it clean. Rename things. Extract functions. Eliminate repetition. This is not optional — it is the third step in a three-step cycle.

8. **Make sure the full scope of your change is understood.** Before committing, understand every line you changed and why. If you can't explain every line, you don't own the change.

### Debugging: Systematic, Not Random

The difference between developers who debug efficiently and those who spend hours thrashing is method.

**The method:**
1. **Reproduce the bug reliably.** If you can't reproduce it, you can't verify a fix.
2. **Isolate the smallest piece of code that demonstrates the problem.** Remove everything else.
3. **Form a hypothesis.** "I think X is happening because Y." Not "let me try changing things and see."
4. **Test the hypothesis.** Add a log, a breakpoint, an assertion. Confirm or deny it.
5. **If wrong, form a new hypothesis.** Don't keep changing things randomly.
6. **Fix the specific cause, not the symptom.** The symptom is "null reference exception." The cause is "the cache can return null but the caller doesn't handle it." Fix the cause.

**The mental model:** Debugging is a scientific process. You have observations (the bug), you form hypotheses (why it's happening), you design experiments (ways to verify), and you draw conclusions. Random changes are not experiments — they are luck.

---

## Part 7 — Test-Driven Development (TDD)

TDD is not primarily about having tests. It is a design technique that forces you to think about requirements and desired outcomes before writing implementation.

### The Cycle: Red → Green → Refactor

1. **Red:** Write a test for behavior that doesn't exist yet. Run it. It fails. This confirms your test setup is correct and the behavior doesn't exist.
2. **Green:** Write the minimal code to make that specific test pass. Do not add anything beyond what the test requires.
3. **Refactor:** Clean up the code. Improve names, extract functions, eliminate duplication. Run the tests to confirm nothing broke.

Repeat.

### What TDD Produces

Code developed with TDD is:
- **Inherently testable:** because you write the test first, the implementation is forced to be modular and injectable
- **Correctly scoped:** because you only write code to pass the current test, overengineering is structurally prevented
- **Self-documenting:** tests describe exactly what the system is supposed to do, in plain terms

Google, Microsoft, and Spotify use TDD for critical services. Not for ideological reasons — for practical ones: TDD produces fewer production bugs, makes refactoring safer, and produces cleaner architecture.

### When NOT to Use TDD

TDD is most valuable for business logic, core algorithms, and integration boundaries. It is less practical for pure UI code, exploratory prototypes, or one-off scripts. Know the difference. Apply TDD where it creates the most value.

---

## Part 8 — Refactoring: Making Code Continuously Cleaner

Refactoring is the process of improving the internal structure of code without changing its external behavior. It is not rewriting — it is incremental improvement.

**Why refactor:** Code written quickly accumulates technical debt. Debt is the gap between "how the code is" and "how it should be." Like financial debt, it accrues interest: the longer it goes unaddressed, the more costly it is to work with.

### The Code Smells (Signs Something Needs Refactoring)

**Long methods:** Functions doing too much. Break them into smaller, named functions.

**Duplicate code:** The same logic in multiple places. Extract it.

**Large classes:** Classes with too many responsibilities. Split them.

**Long parameter lists:** Functions with 5+ parameters. Introduce a parameter object or reconsider the function's design.

**Deep nesting:** Code with 3+ levels of indentation. Extract conditions and loops into named functions.

**Magic numbers/strings:** `if (status == 3)` — what is 3? Extract to a named constant: `if (status == STATUS_PENDING)`.

**Comments explaining what the code does:** If you need a comment to explain a line of code, the code should be clearer. Rename functions and variables until the code is self-explanatory.

**Inconsistent naming:** Variables named differently for the same concept across the codebase. Standardize.

### The Rules of Safe Refactoring

1. **Have tests before you refactor.** You need them to verify you haven't changed behavior. Refactoring without tests is gambling.
2. **Refactor in small steps.** One change at a time. Run tests after each change. Never make 10 changes and run tests at the end.
3. **Never add features while refactoring.** Keep them separate. A PR that "refactors and also adds X" is impossible to review safely.
4. **Rename first.** The cheapest, lowest-risk improvement. Better names compound over time — every future reader benefits.

---

## Part 9 — Technical Debt: Managing the Unavoidable

Technical debt is the accumulated cost of design and implementation decisions that prioritize short-term delivery over long-term quality. It is sometimes intentional (taking a deliberate shortcut to ship faster) and sometimes unintentional (not knowing better at the time).

Like financial debt, it is not inherently bad. Intentional technical debt — "we'll hardcode this now and generalize it in the next sprint" — is a tool. Unmanaged technical debt is a disease.

### The Cost Model

Every piece of technical debt has:
- **Principal:** the work needed to pay it off
- **Interest:** the slowdown it causes every day it exists

A messy module that everyone is afraid to touch is accruing interest. A missing abstraction that causes every new feature to require changes in five places is accruing interest. Each is making your team slower every day.

### When to Pay It Down

- Before adding a feature to a messy area — clean it up first so you're building on a solid foundation
- When the interest becomes higher than the principal (you're spending more time working around the debt than it would take to fix it)
- When it makes onboarding significantly harder

### Overengineering Is Also Debt

The opposite of technical debt is overengineering — building more complexity than you need. Overengineering creates:
- Code that's harder to understand
- More surface area for bugs
- More things to maintain
- Slower onboarding

**The 6 warning signs of overengineering:**
1. You built abstractions for requirements that don't exist yet
2. You added design patterns because they "feel right," not because they solve a problem
3. The solution is harder to explain than the problem
4. Junior developers are confused by simple changes
5. Adding a new feature requires understanding the entire system
6. Your PR reviews generate more questions about architecture than behavior

**The correction:** KISS and YAGNI. The most maintainable codebases are not the most architecturally elegant — they are the ones that match their complexity to the actual problem's complexity.

---

## Part 10 — Code Reviews: The Practice That Raises the Whole Team

Code reviews are not just quality gates — they are the most effective tool for transferring knowledge, maintaining standards, and catching problems before they become expensive.

### What a Good Code Review Looks Like

**As a reviewer:**
- Understand the intent of the change before commenting on the implementation
- Ask questions instead of issuing commands: "What do you think about handling the null case here?" not "Fix this"
- Focus on the *why*, not just the *what* — question logic and approach, not just syntax
- Distinguish between blockers (this must change before merging) and suggestions (this would be better if...)
- Notice what's absent: missing tests, missing error handling, missing edge cases

**As the author:**
- Keep PRs small and focused. Reviewing 200 lines is far more effective than reviewing 2,000 lines. Small PRs get reviewed faster and more thoroughly.
- One PR, one concern: a bug fix and a refactor should be separate PRs
- Write a description that explains *why* this change exists, not just what it does
- Ensure simple mistakes are fixed before requesting review — formatting, naming, obvious issues

**The rule of thumb:** If a reviewer needs to ask basic questions to understand what the PR is doing, the PR description (or the code itself) is insufficient.

---

## Part 11 — Version Control as a Discipline

Most developers use version control as a backup system. Senior developers use it as a communication tool.

### Commit Messages That Matter

A commit message should complete the sentence: "When applied, this commit will..."

**Bad:** `fix bug`, `update stuff`, `changes`, `WIP`
**Good:** `Fix null reference when clipboard is empty on startup`, `Extract SearchEngineService from MainViewModel to separate search routing`, `Add frequency boost for recently launched apps`

The subject line is the headline. Keep it under 72 characters. If more context is needed, add a body after a blank line explaining *why* the change was made.

### Branching

- Each feature, bug fix, or refactor is its own branch
- Branch names describe the work: `fix/clipboard-null-startup`, `feature/ai-action`, `refactor/search-service-extraction`
- Merge only passing, reviewed code to the main branch
- Keep branches short-lived — the longer a branch lives, the more painful the merge

### What Commits Should Not Contain

- Multiple unrelated changes in one commit
- "Fix previous commit" commits (fix it in the same commit, or amend)
- Binary files, build outputs, API keys, secrets, dependencies
- Half-finished features without a feature flag

---

## Part 12 — Performance vs. Readability: The Right Tradeoff

The default position for most code should be readability, not performance. Readable code is maintainable, debuggable, and improvable. Premature optimization creates complexity before there is evidence it's needed.

**Donald Knuth's rule:** Premature optimization is the root of all evil. Most of the time, you don't know which part of your code is slow until you measure it. Optimizing the wrong thing wastes effort and creates complexity for no gain.

**The correct sequence:**
1. Make it work
2. Make it readable
3. Measure — find the actual bottleneck
4. Optimize the actual bottleneck

Optimization without measurement is guesswork. Optimize where the profiler points, not where you assume is slow.

**The exception:** Performance-critical paths — cryptographic routines, real-time processing, inner loops in algorithms — may legitimately sacrifice naming clarity or structural purity for execution speed. The decision boundary: optimization is justified when profiling data identifies a specific bottleneck, not as a default posture.

---

## Part 13 — Naming Conventions and Standards

Code consistency is a form of respect — for future readers, for collaborators, for future you.

### The Universal Rules

- Follow the conventions of the language/platform/project you're working in. (C# → PascalCase for public, camelCase for private. TypeScript → camelCase for variables, PascalCase for types. etc.)
- When project conventions conflict with general best practices, follow the project conventions — consistency within a project matters more than abstract correctness
- Pick a convention, document it, and enforce it everywhere — inconsistency is always worse than any specific convention

### The Practical Checklist

Before considering code done:
- [ ] Every variable name describes what it contains
- [ ] Every function name describes what it does
- [ ] Every class name is a noun that describes its responsibility
- [ ] No single-letter variables except in tight loops (`i`, `j`) or math code
- [ ] No abbreviations unless universally known (`url`, `id`, `http`)
- [ ] Boolean names read as questions (`isActive`, not `active` or `activated`)
- [ ] Magic numbers are named constants
- [ ] Complex logic has a comment explaining *why*, not *what*

---

## Part 14 — Documentation

### What to Document

**README:** Every project should have one. Minimum: what the project does, how to set it up, how to run it, how to run tests. Maximum: everything a new contributor needs to get started without asking questions.

**Architecture documentation:** High-level decisions: why this structure, why this library, what the major components are and how they interact. Not every detail — just enough that someone can navigate the codebase without getting lost.

**Code comments:** The *why* of non-obvious decisions. Security considerations. Performance trade-offs. Known limitations. Gotchas. "Don't change this unless you understand X."

**What NOT to document:** What the code obviously does. Comments that just restate the code are clutter and lie when the code changes but the comment doesn't.

---

## Part 15 — The Senior Engineer's Daily Habits

These are the behaviors that separate engineers who get better every year from those who stagnate:

**Read code, not just write it.** Read open-source codebases. Read your colleagues' code. Reading good code is the fastest way to internalize good patterns. Reading bad code teaches you what to avoid.

**Understand your tools deeply.** Know your language's standard library. Know your IDE's debug tools. Know your version control deeply. Surface-level use of powerful tools leaves enormous productivity on the table.

**Make it work, make it right, make it fast.** In that order. Every time.

**Leave code better than you found it.** If you touch a file, fix the small thing that was bothering you. Rename the confusing variable. Add the missing test. Small improvements compound.

**Write code for the reader, not the compiler.** The compiler doesn't care about names. The reader does. Your code will be read far more times than it is written.

**Be skeptical of your own code.** The code you wrote 6 months ago should embarrass you slightly — that's a sign you're growing. The code you wrote yesterday should still be questioned.

**Measure before optimizing.** Never assume. Profile, then optimize what the profiler shows you.

**Automate the repetitive.** If you've done something manually twice, automate it before the third time. Automation is not laziness — it's engineering.

**Ask "why" more than "how."** Before asking "how do I implement X," ask "why do we need X, and is X the right solution?" Wrong direction, no matter how well implemented, produces waste.

---

## Summary: The 25 Rules

1. Code that works today but can't be changed tomorrow is a liability
2. Plan before you code — 40% planning, 20% coding, 40% testing
3. If you can't explain the problem simply, you can't code it well
4. The simplest solution that correctly solves the problem is the right solution
5. Names are documentation — they matter more than anything else
6. Functions should do one thing at one level of abstraction
7. Comments explain *why*, not *what*
8. DRY: every piece of knowledge has exactly one representation
9. KISS: complexity has a daily maintenance cost
10. YAGNI: don't build for requirements you don't have
11. SOLID: Single Responsibility, Open-Closed, Liskov, Interface Segregation, Dependency Inversion
12. High cohesion + low coupling = a codebase people enjoy working in
13. Before solving a problem, make sure it actually occurs
14. Reproduce the bug before trying to fix it
15. Debugging is a scientific process: hypothesis → experiment → conclusion
16. TDD: write the test before the implementation
17. Refactor continuously, in small steps, with tests
18. Code smells are signals, not rules — understand what they're signaling
19. Technical debt is a tool when managed; a disease when ignored
20. Overengineering is also debt
21. PRs should be small, focused, and described with *why*
22. Commit messages communicate intent to the next reader
23. Optimize where the profiler points, not where you assume
24. Leave code better than you found it
25. Write code for the reader, not the compiler
