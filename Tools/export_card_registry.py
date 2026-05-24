#!/usr/bin/env python3
"""从 Resources 下 CardData .asset 导出 vc5_card_registry CSV（无需打开 Unity）。"""
from __future__ import annotations

import csv
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CARDS_ROOT = ROOT / "Assets" / "TcgEngine" / "Resources" / "Cards"
OUT_DIR = ROOT / "Docs" / "vc5_card_registry"

HEADERS = [
    "enabled", "id", "title", "series", "type",
    "mana", "hp_cost", "discard_cost", "attack", "hp", "move_Range", "attack_Range",
    "fast_action", "deckbuilding", "text", "source", "asset_path", "notes",
]

TYPE_MAP = {
    0: "None", 5: "Hero", 10: "Character", 20: "Spell",
    30: "Artifact", 40: "Secret", 50: "Equipment",
}

# Vc5SlimeBootstrap 运行时注册、无 .asset 的条目（与 Register 后 id 一致）
RUNTIME_VC5 = [
    ("vc5_slime_spawn", "黏黏幼崽", "Character", 0, 0, 0, 1, 1, 1, 1, 0, 1, ""),
    ("vc5_slime_pool", "粘液池", "Artifact", 0, 0, 0, 0, 1, 0, 0, 0, 0, ""),
    ("vc5_hero_slime_blood", "血腥黏黏", "Character", 0, 0, 0, 1, 12, 3, 1, 0, 1, ""),
    ("vc5_hero_slime_corrosive", "腐蚀黏黏", "Character", 0, 0, 0, 1, 14, 3, 1, 0, 1, ""),
    ("vc5_hero_slime_giant", "巨臂黏黏", "Character", 0, 0, 0, 1, 11, 3, 2, 0, 1, ""),
    ("vc5_hero_slime_hard", "坚硬黏黏", "Character", 0, 0, 0, 0, 15, 2, 1, 0, 1, ""),
    ("vc5_hero_slime_hard2", "坚硬黏黏2", "Character", 0, 0, 0, 1, 13, 3, 2, 0, 1, ""),
    ("vc5_slime_heavy_strike", "黏黏重击", "Spell", 0, 2, 0, 0, 0, 0, 0, 0, 1, ""),
    ("vc5_slime_combo_strike", "黏黏连打", "Spell", 1, 0, 0, 0, 0, 0, 0, 0, 1, ""),
    ("vc5_slime_pool_spell", "黏液池", "Spell", 0, 0, 0, 0, 0, 0, 0, 0, 1, ""),
    ("vc5_slime_spray", "黏液喷射", "Spell", 0, 0, 0, 0, 0, 0, 0, 0, 1, ""),
    ("vc5_slime_attach", "黏液附着", "Spell", 0, 0, 0, 0, 0, 0, 0, 0, 1, ""),
    ("vc5_slime_split_card", "分裂黏液", "Spell", 0, 0, 0, 0, 0, 0, 0, 0, 1, ""),
    ("vc5_slime_merge_card", "黏液融合", "Spell", 0, 0, 0, 0, 0, 0, 0, 0, 1, ""),
    ("vc5_slime_boom_card", "黏液爆破", "Spell", 0, 0, 0, 0, 0, 0, 0, 0, 1, ""),
    ("vc5_slime_sharpen", "黏液锐化", "Spell", 0, 0, 0, 0, 0, 0, 0, 0, 1, ""),
    ("vc5_slime_run", "黏液奔行", "Spell", 0, 0, 0, 0, 0, 0, 0, 0, 1, ""),
    ("vc5_slime_detonate_card", "黏液引爆", "Spell", 0, 0, 0, 0, 0, 0, 0, 0, 1, ""),
]


def parse_asset(path: Path) -> dict | None:
    text = path.read_text(encoding="utf-8", errors="replace")
    if "m_Script:" not in text or "id:" not in text:
        return None

    def field(name: str, default: str = "") -> str:
        m = re.search(rf"^\s*{re.escape(name)}:\s*(.*)$", text, re.MULTILINE)
        return m.group(1).strip() if m else default

    cid = field("id")
    if not cid:
        return None

    type_num = int(field("type", "0") or "0")
    rel = path.as_posix().replace(ROOT.as_posix() + "/", "")

    return {
        "enabled": "1",
        "id": cid,
        "title": field("title"),
        "series": field("series", ""),
        "type": TYPE_MAP.get(type_num, "Spell"),
        "mana": field("mana", "0"),
        "hp_cost": field("hp_cost", "0"),
        "discard_cost": field("discard_cost", "0"),
        "attack": field("attack", "0"),
        "hp": field("hp", "0"),
        "move_Range": field("move_Range", "2"),
        "attack_Range": field("attack_Range", "2"),
        "fast_action": "1" if field("fast_action", "0") in ("1", "true") else "0",
        "deckbuilding": "1" if field("deckbuilding", "0") in ("1", "true") else "0",
        "text": field("text").replace("\n", "\\n"),
        "source": "asset",
        "asset_path": rel,
        "notes": "",
    }


def is_hero(row: dict) -> bool:
    if row["type"] == "Hero":
        return True
    return row["id"].startswith("vc5_hero_")


def runtime_row(t: tuple) -> dict:
    id_, title, typ, mana, hp_cost, discard, atk, hp, move, atk_rng, fast, deck, text = t
    return {
        "enabled": "1",
        "id": id_,
        "title": title,
        "series": "",
        "type": typ,
        "mana": str(mana),
        "hp_cost": str(hp_cost),
        "discard_cost": str(discard),
        "attack": str(atk),
        "hp": str(hp),
        "move_Range": str(move),
        "attack_Range": str(atk_rng),
        "fast_action": str(fast),
        "deckbuilding": str(deck),
        "text": text,
        "source": "runtime_vc5",
        "asset_path": "",
        "notes": "",
    }


def main() -> None:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    by_id: dict[str, dict] = {}

    for path in sorted(CARDS_ROOT.rglob("*.asset")):
        row = parse_asset(path)
        if row:
            by_id[row["id"]] = row

    for t in RUNTIME_VC5:
        row = runtime_row(t)
        by_id.setdefault(row["id"], row)

    heroes = sorted((r for r in by_id.values() if is_hero(r)), key=lambda r: r["id"])
    cards = sorted((r for r in by_id.values() if not is_hero(r)), key=lambda r: r["id"])

    for name, rows in (("heroes_registry.csv", heroes), ("cards_registry.csv", cards)):
        out = OUT_DIR / name
        with out.open("w", encoding="utf-8-sig", newline="") as f:
            w = csv.DictWriter(f, fieldnames=HEADERS, extrasaction="ignore")
            w.writeheader()
            w.writerows(rows)
        print(f"Wrote {out} ({len(rows)} rows)")


if __name__ == "__main__":
    main()
