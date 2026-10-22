# POL-07 Development Report

**Verdict:** Complete

## Summary

Added reproducible portable Windows x64 publish script, third-party notices in output, and user documentation set.

## Key deliverables

- `build/publish-portable.ps1` → `artifacts/MergeTool-portable-win-x64/`
- `build/publish_verify.ps1` — tests + publish + checkpoint on published exe
- `docs/user/QUICK_START.md`, `USER_GUIDE.md`, `TROUBLESHOOTING.md`, `KNOWN_LIMITATIONS.md`, `RELEASE_CHECKLIST.md`

## Publish decision

Framework-dependent (`--self-contained false`) — requires .NET 10 Desktop Runtime; optional `-SelfContained` flag documented.
