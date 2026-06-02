# Debug Examples Reference

Three worked examples applying the full methodology — including pre-code triage, confidence updating, directional reasoning, and adversarial fix review.

---

## Example 1: The Over-Aggressive Filter

**Reported:** *"The filter is hiding Claude, Codex, Firefox, and Notepad."*

### Pre-Code Triage

Symptom: works for some apps, fails for others → **Classification/filter logic, ~80% confidence.**

Secondary hypothesis: maybe it's a name-matching bug (case sensitivity, partial match) → **~15% confidence.**

### Step 0 — Translate

> "A classifier is producing false negatives. Items that should pass an 'is important' check are failing it. The criteria used are wrong, incomplete, or based on a flawed signal."

### Step 1 — Characterize + Update Confidence

| ✅ Showing | ❌ Hidden |
|---|---|
| Chrome | Claude |
| VS Code | Codex |
| Microsoft Edge | Firefox |
| Word, Excel | Notepad |

Pattern: different publishers, different categories, different install locations. The name-matching hypothesis (case/partial) doesn't explain why all four fail while similarly-named apps pass. Classification logic: **confidence now ~95%.** Name matching: **dropped to ~5%.**

### Step 2 — Layer Decomposition

Output layer: the app list UI is rendering whatever it receives — no bug there.
Logic layer: there's clearly a filter function deciding what makes the list — **this is where the bug lives.**
Data layer: the app enumeration itself looks fine since the apps exist and are installed.

Focus: logic layer only.

### Step 3 — Read the Code

Search: `IsImportant`, `filter`, `ShouldShow`, `InstallPath`

Found:
```csharp
private bool IsImportantApp(AppInfo app)
{
    if (!app.InstallPath.StartsWith(@"C:\Program Files"))
        return false;

    var startMenuPath = Environment.GetFolderPath(
        Environment.SpecialFolder.CommonStartMenu);
    return Directory.GetFiles(startMenuPath, "*.lnk", SearchOption.AllDirectories)
        .Any(f => f.Contains(app.Name));
}
```

### Step 4 — Three Directions

**Forward:** Walk Claude through this code. Claude installs to `AppData\Local\Programs\`. First check: `"C:\Users\...\AppData\Local\Programs\".StartsWith("C:\Program Files")` → false. Returns false immediately. Never reaches the shortcut check. ❌

Walk Notepad: lives in `C:\Windows\System32\notepad.exe`. Same failure on first check. ❌

Walk Firefox: `C:\Program Files\Mozilla Firefox\` — passes the first check. But Firefox writes its Start Menu shortcut to the **user** Start Menu (`SpecialFolder.StartMenu`), not the **common** one (`CommonStartMenu`). Second check searches the wrong folder. ❌

Two bugs confirmed. No need for backward reasoning here — forward tracing was sufficient.

### Step 5 — Root Cause

The code uses **install location as a proxy for importance**. This assumption is only true for traditional MSI installers. Modern apps (Electron apps like Claude and Codex, user-scope installers like many open-source tools, OS built-ins) install everywhere. The proxy is wrong by design.

Second bug: `CommonStartMenu` only covers system-wide shortcuts. User-specific installs write to `SpecialFolder.StartMenu`. Searching only one location misses half the shortcuts.

### Step 6 — Design + Adversarial Review

**Quick fix (wrong):** Add `AppData` and `System32` to the path check. This is whack-a-mole — every app that installs somewhere new generates a new bug report. The fundamental signal is still wrong.

**Right fix:** Replace binary classification with a multi-signal scoring system. No single signal is reliable; combine them with weights so that missing one doesn't exclude a valid app.

```csharp
private int ScoreApp(AppInfo app)
{
    int score = 0;
    if (HasStartMenuShortcut(app))  score += 30;  // strongest intent signal
    if (HasDesktopShortcut(app))    score += 20;
    if (IsRegisteredApp(app))       score += 25;  // UWP/MSIX registry
    if (WasRecentlyLaunched(app))   score += 15;  // usage = ground truth

    if (IsUninstaller(app))         score -= 100;
    if (IsUpdaterOrHelper(app))     score -= 100;
    if (HasNoVisibleIcon(app))      score -= 20;
    return score;
}
```

**Adversarial review:**
- *"What if an app has no Start Menu shortcut AND no desktop shortcut?"* → It might score low. Is that right? Probably — apps without any user-facing entry point are usually background services. Acceptable.
- *"What if `WasRecentlyLaunched` has no data yet (first run)?"* → Score is 0 for that signal, but other signals compensate. Fine.
- *"Could a background service get a false positive via the registry signal?"* → Possible. Add: if `HasNoVisibleIcon`, apply -20 penalty. Partially mitigates.
- *"Does this change the behavior for apps that were already showing correctly?"* → Chrome: has Start Menu + desktop shortcut + registry → scores 75. Still shows. ✅

**Fix the Start Menu check too:**
```csharp
private bool HasStartMenuShortcut(AppInfo app)
{
    var paths = new[]
    {
        Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
        Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
        Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),
        Environment.GetFolderPath(Environment.SpecialFolder.Programs),
    };
    return paths.Any(path =>
        Directory.Exists(path) &&
        Directory.GetFiles(path, "*.lnk", SearchOption.AllDirectories)
            .Any(f => Path.GetFileNameWithoutExtension(f)
                .Contains(app.Name, StringComparison.OrdinalIgnoreCase)));
}
```

### Verify

| App | Before | After | Reason |
|---|---|---|---|
| Chrome | ✅ | ✅ | High score via all signals |
| Claude | ❌ | ✅ | Scores via Start Menu + desktop + recent use |
| Firefox | ❌ | ✅ | Start Menu now checks user path |
| Notepad | ❌ | ✅ | Scores via Start Menu shortcut |
| "Uninstall Chrome" | ✅ (wrong) | ❌ | `IsUninstaller` → -100 |
| ChromeUpdate.exe | ✅ (wrong) | ❌ | `IsUpdaterOrHelper` → -100 |

---

## Example 2: The Off-By-One (Backward Reasoning)

**Reported:** *"Clicking page 3 shows page 4's data. Pages 1 and 2 are fine."*

### Pre-Code Triage

Off by a constant amount → **Indexing/boundary condition, ~90% confidence.**

### Step 1 — Characterize

Pages 1 and 2: correct. Page 3+: always one page ahead.

Pattern: the error is proportional to the page number. At page 3, offset = 3 × 10 = 30 (skips pages 1 and 2). Page 1 "works" because page 1 at 0 or 1 offset both look similar enough — or the user saw page 2's data and assumed it was right.

### Step 4 — Backward Reasoning

*"For page 3 to show the right content, what slice does it need?"* Items 21–30 (0-indexed: items[20:30]). *"What offset would produce that?"* `offset = 20 = (3-1) * 10`. *"What does the current formula produce?"* `offset = currentPage * pageSize = 3 * 10 = 30`. That's page 4's start.

Conclusion: `currentPage` is 1-indexed in the UI but the formula treats it as 0-indexed. Found via backward reasoning in ~30 seconds without reading code.

### Step 5 — Root Cause

Indexing mismatch: 1-indexed variable used in a 0-indexed formula.

### Step 6 — Adversarial Review

Fix: `offset = (currentPage - 1) * pageSize`

*"Does this break page 1?"* `(1-1) * 10 = 0`. Items 0–9. ✅
*"Does this break the last page?"* If `currentPage = totalPages`, `(totalPages-1) * pageSize` = start of last page. ✅
*"Is `currentPage` used anywhere else with the same formula?"* Search for other uses. If yes, those need the same fix or a refactor to centralize the offset calculation.

---

## Example 3: Stale Data + Silent Crash (Async Bug)

**Reported:** *"The dashboard shows old data. Sometimes it shows nothing at all."*

### Pre-Code Triage

Two symptoms: stale data AND intermittent nothing. Stale → timing/cache. Intermittent nothing → async race or unmounted component. **Async timing, ~85% confidence.** Cache, ~15%.

### Step 2 — Layer Decomposition

Output: UI renders what it receives from state. Logic: state is set by a fetch effect. Data: fetch returns fresh data.

*"Is the data correct if I log it right after fetch?"* If yes, bug is in how state is set or re-rendered. If no, bug is upstream. Check the logic layer first.

### Step 3 — Read the Code

Search: `useEffect`, `fetch`, `setDashboard`, `componentDidMount`

Found:
```javascript
useEffect(() => {
    fetchDashboardData().then(result => {
        setDashboard(result);
    });
}, []); // empty dep array
```

### Step 4 — Forward + Backward

**Forward:** Empty dependency array means effect runs once on mount and never again. Navigate away and back — if React re-mounts the component, effect fires again (fresh data). If React *reuses* the instance (common with router caching), effect never re-fires → stale data.

**Backward:** *"For the dashboard to show fresh data, what must be true?"* Effect must run whenever `userId` (or whatever identity determines the data) changes. Currently it runs only once. That's the gap.

The "nothing" case: navigate away quickly → fetch resolves after unmount → `setDashboard` called on unmounted component → React warning or silent failure depending on version.

### Step 5 — Root Cause

Two bugs: (1) empty dependency array ignores identity changes, (2) no cleanup guard against stale async resolution.

### Step 6 — Adversarial Review

```javascript
useEffect(() => {
    let cancelled = false;

    fetchDashboardData()
        .then(result => {
            if (!cancelled) setDashboard(result);
        })
        .catch(err => {
            if (!cancelled) setError(err);
        });

    return () => { cancelled = true; };
}, [userId]); // re-run when identity changes
```

*"What if `userId` changes twice in quick succession?"* First effect's cleanup fires, sets `cancelled = true`. First fetch resolves, guard prevents stale set. Second fetch runs fresh. ✅

*"What if `userId` is undefined on first render?"* The effect fires with `undefined`. `fetchDashboardData()` may fail. Is that handled? Add a guard: `if (!userId) return;` at the top of the effect.

*"Does this fix the 'nothing' case?"* Yes — cleanup cancellation prevents the unmounted-component update. ✅
