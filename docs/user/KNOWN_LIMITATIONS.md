# Known limitations

- **Text only** — binary, image, Office, and PDF files are not compared as text.
- **No automatic merge/sync** — all merges are explicit user actions.
- **No VCS integration** — paths are chosen manually; no Git/SVN/TFS hooks.
- **No printing or report export** — results stay in the workspace UI.
- **No installer/updater** — portable folder publish only in this release.
- **No vertical blank-line padding** — panes are not padded to equal line counts; scroll sync keeps selections aligned.
- **Large files** — entire files are loaded into memory; very large inputs may be slow (see `docs/performance/POL-03_budgets.md`).
- **Windows only** — WPF desktop target; no macOS/Linux UI.
