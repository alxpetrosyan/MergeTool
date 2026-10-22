# POL-07 Testing Report

**Task:** Verify portable Windows publish and user documentation  
**Date:** 2026-07-02  
**Verdict:** **PASS**

## Baseline checks

| Check | Result |
|-------|--------|
| `dotnet restore` | OK (via bounded runner) |
| `dotnet build -c Release` | OK |
| `dotnet test -c Release` (bounded) | **378/378** pass |
| `docs/DEPENDENCIES.md` | Unchanged packages (MSTest, AvalonEdit only) |

## Publish verification

| Step | Command | Result |
|------|---------|--------|
| Full publish pipeline | `.\build\publish_verify.ps1 -Configuration Release` | **PASS** |
| Publish output | `artifacts/MergeTool-portable-win-x64/` | Present |
| Required artifacts | exe, AvalonEdit.dll, THIRD_PARTY_NOTICES.txt, README.txt | Verified |
| Smoke on **published** exe | `phase1_checkpoint.ps1 -ExePath <published exe>` | **ALL CHECKS PASSED** |

### Published workflow exercised (UI automation)

- File compare, navigation, merge (copy/delete/modified), identical + not-text paths
- Safe save to disk
- Folder compare, Differences filter, paired-open gating

## Documentation

| Document | Location | Matches UI? |
|----------|----------|-------------|
| Quick start | `docs/user/QUICK_START.md` | Yes (workspaces, shortcuts) |
| User guide | `docs/user/USER_GUIDE.md` | Yes |
| Troubleshooting | `docs/user/TROUBLESHOOTING.md` | Yes (diagnostic codes) |
| Known limitations | `docs/user/KNOWN_LIMITATIONS.md` | Yes |
| Portable README | `artifacts/.../README.txt` | Yes |

## Proprietary / unexpected assets

- Publish folder contains only MergeTool assemblies + AvalonEdit + notices — no unexpected third-party binaries.

## Defects

None.

## Recommendation

**Proceed** — publish artifact is verified for controlled distribution.
