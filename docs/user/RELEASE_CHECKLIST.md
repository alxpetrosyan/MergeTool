# Release checklist

- [ ] `.\build\run_tests_bounded.ps1` (Debug) — all pass
- [ ] `.\build\run_tests_bounded.ps1 -Configuration Release` — all pass
- [ ] `.\build\phase1_checkpoint.ps1` — all pass
- [ ] `.\build\pol03_performance_smoke.ps1 -Configuration Release` — within budgets
- [ ] `.\build\publish_verify.ps1 -Configuration Release` — publish + UI smoke on published exe
- [ ] `docs/DEPENDENCIES.md` matches `Directory.Packages.props`
- [ ] `THIRD_PARTY_NOTICES.txt` present in publish output
- [ ] No open **release blocker** items in `docs/release/RC_ACCEPTANCE.md`
