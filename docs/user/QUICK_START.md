# MergeTool Quick Start

MergeTool is a Windows desktop app for **text file** and **folder** comparison, with optional 3-way workflows.

## Requirements

- Windows 10/11 x64
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download) (for framework-dependent portable builds)

## Run the portable build

1. Unzip or copy the contents of `artifacts/MergeTool-portable-win-x64/`.
2. Double-click `MergeTool.App.Wpf.exe`.

## First comparison

1. Click **New File Compare**.
2. Choose left and right text files (or type paths).
3. Click **Compare**.
4. Use **Next ▶** / **◀ Previous** (or **F7** / **Shift+F7**) to move between differences.
5. Use **Copy to Right** / **Copy to Left** to merge selected changes.
6. Press **Ctrl+Shift+S** or **Save All** when ready.

## Folder compare

1. Click **Folder Compare**.
2. Pick two folder roots and click **Compare**.
3. Filter rows, then **Open paired text comparison** on a selected row.

## Settings and sessions

- **Recents** and **Pinned** appear on the shell bar.
- **Reset settings** restores defaults (recents/pinned cleared).

See `USER_GUIDE.md` for full coverage and `TROUBLESHOOTING.md` if something fails.
