# ADV-03 Development Report

**Task:** Add data-driven file-type profiles and syntax highlighting  
**Date:** 2026-07-02  
**Status:** Complete (core + UI)

## Delivered

- `MergeTool.Text.Profiles`: 12 built-in profiles (Plain Text, Delphi/Pascal, C#, SQL, XML, JSON, HTML, CSS, JavaScript, TypeScript, Markdown, INI/config)
- Extension-based `FileTypeProfileMatcher`
- Tab size + optional default comparison options per profile
- `SyntaxHighlightingAdapter` (AvalonEdit, WPF only)
- File compare UI: profile combo, auto/manual badge, editor presentation on session open

## Tests

`FileTypeProfileMatcherTests` — 3 tests

## Build/test

236/236 bounded tests pass (Debug + Release).

## Limitations

- Delphi, SQL, Markdown, INI have no AvalonEdit highlighting definition (plain rendering)
- Per-pane profile override not implemented; workspace uses left-path auto-detect with manual override
