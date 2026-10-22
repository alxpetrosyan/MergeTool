# Text Fixtures

Deterministic byte fixtures for encoding and line-ending coverage
(`00_TEST_DATA_CONVENTIONS.txt`). Regenerate with the PowerShell snippet in the MVP-03
development report if a fixture is ever corrupted.

| File | Content |
| --- | --- |
| `utf8_bom_crlf.txt` | UTF-8 with BOM, CRLF endings, final newline |
| `utf8_nobom_lf.txt` | UTF-8 without BOM, LF endings, final newline |
| `utf16le_bom_crlf.txt` | UTF-16 LE with BOM, CRLF endings |
| `utf16be_bom_crlf.txt` | UTF-16 BE with BOM, CRLF endings |
| `ansi_windows1252.txt` | Invalid UTF-8; decodes via documented windows-1252 fallback (bytes 0x93/0x94 = curly quotes) |
| `cr_only.txt` | Classic Mac CR-only endings |
| `mixed_endings_no_final_newline.txt` | CRLF + LF + CR mixed, no final newline |
| `empty.txt` | Empty file |
| `nul_heavy_not_text.bin` | NUL-heavy bytes; must classify `NotTextForThisProduct` |
