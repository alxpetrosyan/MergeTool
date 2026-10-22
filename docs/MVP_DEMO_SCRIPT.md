# MergeTool MVP Demo Script (< 10 minutes)

For product owners or QA. Requires a Release build.

## Before you start

```powershell
cd C:\_Softworks\MergeTool
dotnet build -c Release
```

Launch: `src\MergeTool.App.Wpf\bin\Release\net10.0-windows\MergeTool.App.Wpf.exe`

Fixtures live under `tests\TestData\`.

---

## Part A — File compare (5 minutes)

1. **Open the app.** A File Compare tab is already open.

2. **Compare modified files**
   - Left: `tests\TestData\CompareFixtures\mods_left.txt`
   - Right: `tests\TestData\CompareFixtures\mods_right.txt`
   - Click **Compare**.
   - Confirm summary shows **3 difference block(s)** and colored rows in both panes.

3. **Navigate**
   - Click **Next ▶** three times. Status shows `Difference 3 of 3`.
   - **Next ▶** should disable (no wrap).

4. **Merge an insertion (right → left)**
   - Set Left: `insert_left.txt`, Right: `insert_right.txt` (same folder).
   - **Compare** → **Next ▶** → **⬅ Copy to Left** → **Recompare**.
   - Confirm **No differences found**.

5. **Save**
   - Copy `mods_right.txt` to your Desktop as `demo_save.txt`.
   - Compare `mods_left.txt` vs your `demo_save.txt`.
   - **Next ▶** → **Copy to Right ➜** → **Save Right**.
   - Re-open `demo_save.txt` in Notepad; line 5 should no longer say `CHANGED`.

6. **Safety checks**
   - Compare identical_a.txt vs identical_b.txt → **No differences**.
   - Set right path to `notext_nul.bin` → Compare → safe “not text” message, no crash.

---

## Part B — Folder compare (3 minutes)

1. Click **Folder Compare** in the toolbar.

2. Set folders:
   - Left: `tests\TestData\FolderFixtures\left`
   - Right: `tests\TestData\FolderFixtures\right`
   - **Compare**.

3. Confirm grid shows **Left only**, **Right only**, **Metadata different**, and (if timestamps aligned) **Metadata match**.

4. Read legend: **Metadata match = size + UTC modification time only.**

5. Click **Differences** filter → `same.txt` row should disappear.

6. Select `changed.txt` → **Open paired text comparison** → new File Compare tab with differences visible.

---

## Part C — Cancel (1 minute)

1. On Folder Compare, pick two large folders you own (optional).
2. Click **Compare**, then **Cancel** in the status bar while scanning.
3. Confirm UI returns to idle/cancelled without crashing; prior results stay visible if a scan had completed earlier.

---

## Done

You have exercised the Phase-1 MVP: file compare, navigation, manual merge both directions, safe save, folder metadata compare, filters, and paired file open.

Automated equivalent: `build/phase1_checkpoint.ps1`
