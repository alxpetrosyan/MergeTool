# Troubleshooting

## The file could not be read (MT-IO-001)

- Confirm the path exists and is not locked by another program.
- Check read permissions.

## Save conflict (MT-IO-002)

The file changed on disk after you loaded it. Use **Reload** or save to a different path; MergeTool will not silently overwrite external changes.

## Comparison is stale

After editing, saving, or merging, run **Recompare (F5)** before navigating or merging again.

## Folder scan is slow

Large trees take time. Use **Cancel** on the status bar; the last completed scan remains visible. For very large folders, prefer **Quick metadata** over **Content hash**.

## Invalid regex (MT-CMP-004 / MT-SRC-001)

Fix the pattern in comparison rules or search options, or disable regex mode.

## Settings could not be loaded (MT-SET-001)

Use **Reset settings** or delete the settings file, then restart MergeTool.

## Portable build will not start

Install the **.NET 10 Desktop Runtime** for Windows x64, or republish with `-SelfContained`.

## Copy diagnostic details

When an error panel appears, use **Copy details** and include the code (e.g. `MT-IO-002`) in bug reports.
