# -*- coding: utf-8 -*-
"""
Repository-wide mojibake fixer.

What it does:
- Scans text files in the repo.
- Repairs lines that look like UTF-8 text decoded as Windows-1252 and then saved.
- Preserves BOM and original line endings.
- Backs up every changed file before writing.

Usage:
1. Run once in dry-run mode to preview the files that would change.
2. Set DRY_RUN = False to apply changes.
3. Review git diff after the run.
"""

from __future__ import annotations

import shutil
from pathlib import Path

ROOT_DIR = Path(__file__).resolve().parent
BACKUP_DIR_NAME = "_mojibake_backup"
DRY_RUN = True
MAX_PREVIEW_LINES = 5

TEXT_EXTENSIONS = {
    ".cs",
    ".cshtml",
    ".cshtml.cs",
    ".css",
    ".drawio",
    ".html",
    ".htm",
    ".js",
    ".json",
    ".md",
    ".sln",
    ".slnx",
    ".razor",
    ".txt",
    ".xml",
}

SKIP_DIR_NAMES = {".git", "bin", "node_modules", "obj", BACKUP_DIR_NAME}
SKIP_SUBPATHS = ("wwwroot\\lib", "wwwroot\\uploads")


def is_text_candidate(path: Path) -> bool:
    rel = path.relative_to(ROOT_DIR)
    if rel.name.endswith(".cshtml.cs"):
        return True
    if path.suffix.lower() not in TEXT_EXTENSIONS:
        return False
    if any(part.lower() in SKIP_DIR_NAMES for part in rel.parts):
        return False
    rel_norm = str(rel).replace("/", "\\").lower()
    return not any(skip in rel_norm for skip in SKIP_SUBPATHS)


def protect_unicode(line: str) -> tuple[str, dict[str, str]]:
    protected: dict[str, str] = {}
    pieces: list[str] = []
    counter = 0
    for ch in line:
        try:
            ch.encode("cp1252")
        except UnicodeEncodeError:
            token = f"__U{counter}__"
            protected[token] = ch
            pieces.append(token)
            counter += 1
            continue
        pieces.append(ch)
    return "".join(pieces), protected


def restore_unicode(line: str, protected: dict[str, str]) -> str:
    restored = line
    for token, ch in protected.items():
        restored = restored.replace(token, ch)
    return restored


def fix_line(line: str) -> tuple[str, bool]:
    safe_line, protected = protect_unicode(line)
    try:
        fixed = safe_line.encode("cp1252").decode("utf-8")
    except (UnicodeEncodeError, UnicodeDecodeError):
        return line, False
    fixed = restore_unicode(fixed, protected)
    return fixed, fixed != line


def process_file(path: Path, preview_left: list[int]) -> bool:
    raw = path.read_bytes()
    has_bom = raw.startswith(b"\xef\xbb\xbf")
    body = raw[3:] if has_bom else raw

    try:
        text = body.decode("utf-8")
    except UnicodeDecodeError:
        print(f"[SKIP unreadable UTF-8] {path}")
        return False

    use_crlf = "\r\n" in text
    lines = text.splitlines(keepends=False)

    changed_any = False
    new_lines: list[str] = []
    for line in lines:
        fixed, changed = fix_line(line)
        new_lines.append(fixed)
        if changed:
            changed_any = True
            if preview_left[0] > 0:
                print(f"  before: {line}")
                print(f"  after : {fixed}\n")
                preview_left[0] -= 1

    if not changed_any:
        return False

    sep = "\r\n" if use_crlf else "\n"
    new_text = sep.join(new_lines)
    if text.endswith(("\r\n", "\n")):
        new_text += sep

    print(f"[FIX] {path}")
    if DRY_RUN:
        return True

    backup_root = ROOT_DIR / BACKUP_DIR_NAME
    backup_path = backup_root / path.relative_to(ROOT_DIR)
    backup_path.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(path, backup_path)

    new_bytes = new_text.encode("utf-8")
    if has_bom:
        new_bytes = b"\xef\xbb\xbf" + new_bytes
    path.write_bytes(new_bytes)
    return True


def main() -> None:
    preview_left = [MAX_PREVIEW_LINES]
    fixed_files = 0

    for path in ROOT_DIR.rglob("*"):
        if not path.is_file():
            continue
        if not is_text_candidate(path):
            continue
        if process_file(path, preview_left):
            fixed_files += 1

    mode = "would be fixed" if DRY_RUN else "were fixed"
    print(f"\n===> Total files that {mode}: {fixed_files}")
    if DRY_RUN:
        print("Dry run only. Set DRY_RUN = False to apply changes.")
    else:
        print(f"Backups saved under: {ROOT_DIR / BACKUP_DIR_NAME}")


if __name__ == "__main__":
    main()
