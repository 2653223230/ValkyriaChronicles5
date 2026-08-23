"""Convert a .docx file to Markdown, preserving paragraph text verbatim."""

import re
import sys
from pathlib import Path

from docx import Document
from docx.oxml.table import CT_Tbl
from docx.oxml.text.paragraph import CT_P
from docx.table import Table
from docx.text.paragraph import Paragraph


def iter_block_items(parent):
    parent_elm = parent.element.body
    for child in parent_elm.iterchildren():
        if isinstance(child, CT_P):
            yield Paragraph(child, parent)
        elif isinstance(child, CT_Tbl):
            yield Table(child, parent)


def paragraph_text(p: Paragraph) -> str:
    return p.text


def heading_prefix(style_name: str) -> str | None:
    if not style_name:
        return None
    name = style_name.lower()
    if name.startswith("heading"):
        # Heading 1, Heading 2, ...
        parts = style_name.split()
        if len(parts) >= 2 and parts[1].isdigit():
            level = min(int(parts[1]), 6)
            return "#" * level + " "
    # Chinese Word templates sometimes use 标题 1 / 标题 2
    if "标题" in style_name:
        m = re.search(r"(\d+)", style_name)
        if m:
            level = min(int(m.group(1)), 6)
            return "#" * level + " "
    return None


def is_list_paragraph(p: Paragraph) -> tuple[str | None, str]:
    """Return (bullet_prefix, text) if list item."""
    text = paragraph_text(p)
    ppr = p._p.pPr
    if ppr is not None and ppr.numPr is not None:
        return "- ", text
    return None, text


def table_to_md(table: Table) -> str:
    rows = []
    for row in table.rows:
        cells = [cell.text.replace("\n", " ").strip() for cell in row.cells]
        rows.append(cells)
    if not rows:
        return ""

    width = max(len(r) for r in rows)
    for r in rows:
        while len(r) < width:
            r.append("")

    def esc(cell: str) -> str:
        return cell.replace("|", "\\|")

    lines = []
    header = rows[0]
    lines.append("| " + " | ".join(esc(c) for c in header) + " |")
    lines.append("| " + " | ".join("---" for _ in header) + " |")
    for r in rows[1:]:
        lines.append("| " + " | ".join(esc(c) for c in r) + " |")
    return "\n".join(lines)


def convert(docx_path: Path, md_path: Path) -> None:
    doc = Document(str(docx_path))
    out_lines: list[str] = []

    for block in iter_block_items(doc):
        if isinstance(block, Paragraph):
            text = paragraph_text(block)
            if text == "":
                out_lines.append("")
                continue

            hp = heading_prefix(block.style.name if block.style else "")
            if hp:
                out_lines.append(hp + text)
                continue

            bullet, list_text = is_list_paragraph(block)
            if bullet:
                out_lines.append(bullet + list_text)
                continue

            out_lines.append(text)
        else:
            md_table = table_to_md(block)
            if md_table:
                if out_lines and out_lines[-1] != "":
                    out_lines.append("")
                out_lines.append(md_table)
                out_lines.append("")

    # Trim trailing blank lines
    while out_lines and out_lines[-1] == "":
        out_lines.pop()

    content = "\n".join(out_lines) + "\n"
    md_path.write_text(content, encoding="utf-8")
    print(f"Wrote {md_path} ({len(out_lines)} lines)")


if __name__ == "__main__":
    src = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(
        r"E:\unity\git_newest\ValkyriaChronicles5\战场女武神5最新规则与卡牌-20260621.docx"
    )
    dst = Path(sys.argv[2]) if len(sys.argv) > 2 else src.with_suffix(".md")
    convert(src, dst)
