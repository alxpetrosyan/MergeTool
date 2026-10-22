# MergeTool User Guide

## Workspaces

| Workspace | Purpose |
|-----------|---------|
| File Compare | Two-way text diff, merge, save |
| Folder Compare | Quick metadata or content-hash folder scan |
| 3-Way Compare | Base/left/right text with manual output merge |
| 3-Way Folder Compare | Three-root folder scan; open 3-way text from a row |

## File compare

### Comparison rules

Open **Comparison rules** before **Recompare** to change whitespace mode, ignore patterns, moved-block detection, or JSON/XML formatted view. Formatted view is display-only; merge requires raw text.

### Navigation and merge

- **F7** / **Shift+F7** — next/previous difference
- **F5** — recompare
- Merge buttons copy the **selected** difference block to the other side.
- **Save** actions are separated from merge buttons; save never runs automatically.

### Find and go-to

Open **Find & go** (or press **Ctrl+F**). Search does not change comparison results. **F3** / **Shift+F3** find next/previous match.

### States

A banner appears when the comparison is **stale**, a file is **dirty** or **read-only**, or formatted mode blocks merge.

## Folder compare

- **Quick metadata** compares size and UTC timestamp (optional tolerance).
- **Content hash** reads file bytes (SHA-256) for equality.
- Destructive **Delete** actions require confirmation and use red styling.
- **Find rename candidates** suggests pairings only; nothing is renamed automatically.

## 3-way output merge

Resolve each region with **Use base/left/right**, then **Save output…**. Unresolved regions block save.

## Keyboard reference (file compare)

| Action | Shortcut |
|--------|----------|
| Next difference | F7 |
| Previous difference | Shift+F7 |
| Recompare | F5 |
| Save all | Ctrl+Shift+S |
| Find next | F3 |
| Find previous | Shift+F3 |
| Open find panel | Ctrl+F |

## Known limitations

See `KNOWN_LIMITATIONS.md`.
