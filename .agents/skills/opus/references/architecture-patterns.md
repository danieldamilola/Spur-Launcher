# Architecture Patterns Reference

Decision frameworks, templates, and trade-off tables for the feature development process.

---

## Confidence Weighting in Hypothesis Formation

Treat hypotheses as probability estimates, not a flat list. Assign an initial weight during triage, then update it as you gather evidence.

**Initial weights from symptom alone:**

| Symptom | Strongest hypothesis | Initial weight |
|---|---|---|
| Subset fails, others pass | Classification/filter error | 75–85% |
| Intermittent failure | Async timing or race condition | 70–80% |
| Off by constant amount | Index/boundary mismatch | 80–90% |
| Worked before, broke after change | Regression — check the diff | 80–90% |
| Works in dev, not prod | Environment/config assumption | 70–80% |
| Slow but correct | Performance, N+1, locking | 75% |

**Updating weights:**

- Evidence that matches the hypothesis: increase weight, decrease alternatives
- Evidence that contradicts it: decrease weight, promote alternatives
- If top hypothesis drops below ~40%, reconsider from scratch

The goal is to arrive at a single hypothesis above ~80% confidence before committing time to reading code. One strong lead beats five weak ones.

---

## Layer Decomposition Template

For any system, stack the layers and work inward:

```
Surface layer (what the user observes)
        ↓
Transform/logic layer (decisions, filters, computation)
        ↓
Data/input layer (source data, fetch, indexing)
        ↓
Infrastructure (DB, API, filesystem, OS)
```

**Process:**
1. Start at the surface. Is the output already wrong here, or does it arrive wrong from below?
2. If wrong at surface: is the input to this layer correct? If yes, bug is in this layer. If no, go deeper.
3. Repeat until you find the layer where data is still correct going in but wrong coming out.
4. The bug lives in that layer's logic.

**Why this beats reading everything:** A 5-layer system with 10 files per layer = 50 files. Layer decomposition narrows you to 10. A targeted read in 10 files beats a random read across 50.

---

## Three Diagnostic Directions

Choose based on where you have the most information:

| Direction | When to use | How |
|---|---|---|
| **Forward** (cause → symptom) | You can see the decision logic and trace it | Walk failing cases through the code step by step |
| **Backward** (symptom → cause) | The failure is downstream; source is unclear | Ask: what state must be true for the correct output? Trace back to where that state is lost |
| **Requirements check** | Code behaves consistently but "wrongly" | Ask: is the code doing exactly what it was asked to do? If yes, the spec drifted — fix the contract, not the code |

Most bugs yield to forward tracing. Reach for backward when the failure is far from its source. Reach for the requirements check when the code is internally consistent but produces the wrong thing every time — that pattern often means the original assumption has become invalid.

---

## When to Patch vs. Redesign

| Signal | Patch | Redesign |
|---|---|---|
| One edge case wasn't handled | ✅ | |
| Same type of bug has recurred before | | ✅ |
| Fix requires adding 3rd+ branch to existing condition | | ✅ |
| Root cause is a flawed proxy variable | | ✅ |
| Root cause is an incorrect assumption | | ✅ |
| The fix makes the code meaningfully harder to read | | ✅ |
| Bug is a typo, wrong constant, missing null check | ✅ | |

> Rule: if you're adding the third `||` to a condition that was already doing two things, replace the whole thing.

---

## Binary Filter → Scoring System

When a yes/no filter produces false negatives or false positives, replace it with weighted scoring.

**Pattern:**
```
score = Σ(positiveSignals × weight) - Σ(negativeSignals × weight)
include if score > threshold
sort by score descending
```

**Why it's more robust:**
- No single missing signal excludes a valid item
- Scores are inspectable — easy to debug classification errors
- Threshold is tunable without rewriting logic
- Adding new signals (e.g., usage frequency) requires no structural changes

**Typical signal weights:**

| Signal | Weight | Notes |
|---|---|---|
| User-facing shortcut exists | +20–30 | Strongest intent signal |
| Recently launched by user | +15–25 | Ground truth — user actually wanted it |
| Registered in system app registry | +15–25 | UWP/MSIX, macOS .app bundles |
| Has a visible icon | +5–10 | Weak, but adds up |
| Name matches "Uninstall *" | -80–100 | Almost always junk |
| Name matches "* Update" / "* Helper" | -80–100 | Background service, not user-facing |
| No icon at all | -15–25 | Likely not user-facing |
| Background-only process | -30–50 | Services, daemons |

---

## Adversarial Fix Review Checklist

Before writing any fix, ask these questions about your proposed solution:

- **Failure mode:** What input or state would cause my fix to produce the wrong result?
- **New assumption:** What does my fix assume that the old code didn't? Is that assumption always true?
- **False positives:** Does my fix let anything through that should have been excluded?
- **Performance:** Does this change the time or space complexity in a way that matters?
- **Regression surface:** What existing behavior could my fix inadvertently change?
- **Wrong root cause:** If my diagnosis is incorrect, does this fix cause harm, or is it merely neutral?

If you can't find a failure mode, that's evidence (not proof) the fix is sound. If you find one, either adjust the fix or document the known limitation explicitly so the next person knows.

---

## Build Order Template

For any feature:

```
Step 1: Constraints               — performance, compat, scope, dependencies
         ↓
Step 2: Architecture decisions    — document choices + rejections
         ↓
Step 3: Data layer                — the engine; if this fails, nothing else matters
         ↓
Step 4: Core skeleton             — functional, unstyled, wired to data layer
         ↓
Step 5: Integration               — connects to the rest of the app
         ↓
Step 6: Error states              — empty, loading, failure
         ↓
Step 7: Polish                    — animation, spacing, typography — always last
```

---

## Common Root Cause Categories

When the cause isn't obvious, scan this list:

**Proxy variable** — using X as a signal for Y when they're only loosely correlated. Breaks at the edges of the correlation. Fix: find a signal that directly measures what you actually want to know.

**Flawed assumption** — code assumes a precondition that's usually true but not always. Fix: either guarantee the precondition upstream, or remove the assumption and handle the general case.

**Indexing mismatch** — 0-indexed vs 1-indexed, off-by-one in slice/range, boundary not handled. Fix: pick one convention, enforce it, centralize offset calculation.

**Async timing** — state set on unmounted component, two fetches racing, effect with wrong dependencies. Fix: cleanup flags, correct dependency arrays, cancellation tokens.

**Overly narrow whitelist** — enumerating allowed values when the real world exceeds the list. Fix: enumerate disallowed values instead, or replace with scoring.

**Requirements drift** — code was correct when written; the spec changed but the code didn't. Fix: update the contract, not just the code.

**Wrong data source** — reading a cache when fresh data was needed, checking one location when data can be in multiple, using a deprecated field. Fix: read the right source, or unify sources behind a single abstraction.
