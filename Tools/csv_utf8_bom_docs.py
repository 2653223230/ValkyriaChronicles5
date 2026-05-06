# -*- coding: utf-8 -*-
"""Rewrite CSV files under Docs as UTF-8 with BOM for Excel (zh-CN)."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def to_utf8_bom(path: Path) -> None:
    raw = path.read_bytes()
    if raw.startswith(b"\xef\xbb\xbf"):
        text = raw.decode("utf-8-sig")
    else:
        text = None
        for enc in ("utf-8", "gbk", "cp936"):
            try:
                text = raw.decode(enc)
                break
            except UnicodeDecodeError:
                continue
        if text is None:
            text = raw.decode("utf-8", errors="replace")
    with path.open("w", encoding="utf-8-sig", newline="") as f:
        f.write(text)


def main() -> None:
    roots = [
        ROOT / "Docs" / "vc5_excel_seed_v1",
        ROOT / "Docs" / "vc5_excel_template",
    ]
    for root in roots:
        if not root.is_dir():
            continue
        for p in sorted(root.glob("*.csv")):
            to_utf8_bom(p)
            print("UTF-8 BOM:", p.relative_to(ROOT))


if __name__ == "__main__":
    main()
