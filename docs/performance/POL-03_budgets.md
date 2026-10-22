# POL-03 performance budgets

**Purpose:** Practical smoke budgets for large inputs. These are **not** universal timing guarantees.

## Measurement environment

Record the machine used when running `build/pol03_performance_smoke.ps1`:

| Field | Example |
|-------|---------|
| OS | Windows 10/11 x64 |
| CPU | 8+ logical cores |
| RAM | 16 GB+ |
| Storage | SSD |
| .NET | 10.0 (see `global.json`) |
| Configuration | Release for manual benchmarks; Debug for CI smoke tests |

## Automated smoke budgets (CI)

Enforced by `PerformanceSmokeTests` with generous ceilings:

| Scenario | Budget |
|----------|--------|
| 1 MB text open + compare | 30 s |
| 1,000 folder entries (quick metadata) | 8 s |
| 10,000 folder entries (quick metadata) | 45 s |
| 200 files content-hash mode | 30 s |

## Manual benchmark script

```powershell
.\build\pol03_performance_smoke.ps1
.\build\pol03_performance_smoke.ps1 -Configuration Release
```

The script runs filtered performance tests and prints elapsed time from the test host.

## Optimizations applied (POL-03)

| Area | Change |
|------|--------|
| Folder grid | `FolderCompareView` DataGrid on `Height="*"` row so row virtualization is effective |
| Folder scan UI | `ThrottledProgress` limits scan status updates (~200 ms) |
| Folder filter apply | Throttled progress reduces UI thread churn during scans |
| Text load | Decode + line split on thread pool in `TextDocumentLoader` |

## Out of scope

- Native rewrites, GPU, binary compare
- Micro-optimizations without measured bottleneck
