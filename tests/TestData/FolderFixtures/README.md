# Folder comparison fixtures

Used by Phase-1 checkpoint verification (`build/phase1_checkpoint.ps1`).

| Path | Expected state |
|------|----------------|
| `same.txt` (both sides) | Metadata match (same size + UTC time when created together) |
| `different.txt` (both sides) | Metadata different |
| `left-only.txt` | Left only |
| `right-only.txt` | Right only |
| `changed.txt` (both sides) | Metadata different; opens paired file compare |
