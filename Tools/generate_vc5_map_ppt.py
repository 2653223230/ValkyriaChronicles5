"""
Generate VC5 map requirement PPT for AI / program consumption.
Source spec: 战场女武神5最新规则与卡牌-20260621.md § 游戏地图与地图上的得分机制
"""

from __future__ import annotations

import re
from datetime import date
from pathlib import Path

from pptx import Presentation
from pptx.dml.color import RGBColor
from pptx.enum.shapes import MSO_SHAPE
from pptx.enum.text import MSO_ANCHOR, PP_ALIGN
from pptx.util import Inches, Pt

ROOT = Path(__file__).resolve().parents[1]
SPEC_MD = ROOT / "战场女武神5最新规则与卡牌-20260621.md"
UNITY_SCENE = ROOT / "Assets/TcgEngine/Scenes/Game/Game.unity"
OUTPUT = ROOT / "Docs" / "VC5_策划目标地图_需求说明.pptx"

# --- colors (high contrast, semantic) ---
C_BG_TITLE = RGBColor(0x1E, 0x29, 0x3B)
C_TEXT_LIGHT = RGBColor(0xF8, 0xFA, 0xFC)
C_TEXT_DARK = RGBColor(0x0F, 0x17, 0x2A)
C_TEXT_MUTED = RGBColor(0x64, 0x74, 0x8B)
C_SCORE = RGBColor(0x1F, 0x29, 0x37)  # 得分区 — 文档「黑色得分区」
C_SPAWN_SELF = RGBColor(0x25, 0x63, 0xEB)
C_SPAWN_OPP = RGBColor(0xDC, 0x26, 0x26)
C_NORMAL = RGBColor(0xE2, 0xE8, 0xF0)
C_STROKE = RGBColor(0x94, 0xA3, 0xB8)
C_ACCENT = RGBColor(0x0E, 0xA5, 0xE9)
C_WHITE = RGBColor(0xFF, 0xFF, 0xFF)

SCORE_SET = frozenset({(5, 3), (4, 3), (6, 3), (5, 2), (5, 4), (4, 4), (6, 2)})
# 规格文档 spawn_self；对方在玩家视野中为围绕棋盘中心的镜像显示
SPAWN_SELF = frozenset({(3, 2), (2, 3), (2, 4)})
SPAWN_OPP = frozenset({(7, 4), (8, 3), (8, 2)})  # x' = 10 - x, y' = 6 - y

SCORE_CELLS_ORDERED = [
    ((5, 3), "正中心"),
    ((4, 3), "中心左"),
    ((6, 3), "中心右"),
    ((5, 2), "中心上（偏对方方向）"),
    ((5, 4), "中心下（偏己方方向）"),
    ((4, 4), "左下"),
    ((6, 2), "右上"),
]


def parse_unity_slots() -> list[dict]:
    text = UNITY_SCENE.read_text(encoding="utf-8", errors="ignore")
    blocks = re.split(r"--- !u!1001 &\d+\n", text)
    slots: list[dict] = []
    for b in blocks:
        if "BoardSlot" not in b or "propertyPath: x" not in b:
            continue
        x = re.search(r"propertyPath: x\n\s+value: (\d+)", b)
        y = re.search(r"propertyPath: y\n\s+value: (\d+)", b)
        lx = re.search(r"propertyPath: m_LocalPosition\.x\n\s+value: ([^\n]+)", b)
        ly = re.search(r"propertyPath: m_LocalPosition\.y\n\s+value: ([^\n]+)", b)
        if x and y and lx and ly:
            slots.append(
                {
                    "x": int(x.group(1)),
                    "y": int(y.group(1)),
                    "lx": float(lx.group(1)),
                    "ly": float(ly.group(1)),
                }
            )
    return sorted(slots, key=lambda s: (s["y"], s["x"]))


def cell_kind(x: int, y: int) -> str:
    if (x, y) in SCORE_SET:
        return "score"
    if (x, y) in SPAWN_SELF:
        return "spawn_self"
    if (x, y) in SPAWN_OPP:
        return "spawn_opp"
    return "normal"


def kind_fill(kind: str) -> RGBColor:
    return {
        "score": C_SCORE,
        "spawn_self": C_SPAWN_SELF,
        "spawn_opp": C_SPAWN_OPP,
        "normal": C_NORMAL,
    }[kind]


def kind_label_cn(kind: str) -> str:
    return {
        "score": "得分区",
        "spawn_self": "己方出生",
        "spawn_opp": "对方出生",
        "normal": "普通格",
    }[kind]


def set_run_font(run, size_pt: float, bold: bool = False, color: RGBColor | None = None):
    run.font.name = "Microsoft YaHei"
    run.font.size = Pt(size_pt)
    run.font.bold = bold
    if color:
        run.font.color.rgb = color


def add_textbox(
    slide,
    left,
    top,
    width,
    height,
    text: str,
    *,
    size: float = 14,
    bold: bool = False,
    color: RGBColor | None = None,
    align=PP_ALIGN.LEFT,
):
    box = slide.shapes.add_textbox(left, top, width, height)
    tf = box.text_frame
    tf.word_wrap = True
    p = tf.paragraphs[0]
    p.alignment = align
    run = p.add_run()
    run.text = text
    set_run_font(run, size, bold=bold, color=color or C_TEXT_DARK)
    return box


def add_bullet_slide(prs: Presentation, title: str, bullets: list[str], *, subtitle: str = ""):
    layout = prs.slide_layouts[6]  # blank
    slide = prs.slides.add_slide(layout)
    add_textbox(slide, Inches(0.5), Inches(0.35), Inches(9), Inches(0.6), title, size=24, bold=True)
    if subtitle:
        add_textbox(
            slide,
            Inches(0.5),
            Inches(0.95),
            Inches(9),
            Inches(0.4),
            subtitle,
            size=11,
            color=C_TEXT_MUTED,
        )
    top = Inches(1.45) if subtitle else Inches(1.1)
    box = slide.shapes.add_textbox(Inches(0.55), top, Inches(8.9), Inches(5.5))
    tf = box.text_frame
    tf.word_wrap = True
    for i, line in enumerate(bullets):
        p = tf.paragraphs[0] if i == 0 else tf.add_paragraph()
        p.level = 0
        p.space_after = Pt(6)
        run = p.add_run()
        run.text = line
        set_run_font(run, 13, color=C_TEXT_DARK)
    return slide


def add_table_slide(
    prs: Presentation,
    title: str,
    headers: list[str],
    rows: list[list[str]],
    *,
    col_widths: list[float] | None = None,
):
    layout = prs.slide_layouts[6]
    slide = prs.slides.add_slide(layout)
    add_textbox(slide, Inches(0.5), Inches(0.35), Inches(9), Inches(0.55), title, size=22, bold=True)
    n_rows = len(rows) + 1
    n_cols = len(headers)
    table_shape = slide.shapes.add_table(
        n_rows, n_cols, Inches(0.5), Inches(1.05), Inches(9.0), Inches(0.35 * n_rows)
    ).table
    if col_widths:
        for ci, w in enumerate(col_widths):
            table_shape.columns[ci].width = Inches(w)
    for ci, h in enumerate(headers):
        cell = table_shape.cell(0, ci)
        cell.text = h
        for p in cell.text_frame.paragraphs:
            for run in p.runs:
                set_run_font(run, 11, bold=True, color=C_TEXT_DARK)
        cell.fill.solid()
        cell.fill.fore_color.rgb = RGBColor(0xF1, 0xF5, 0xF9)
    for ri, row in enumerate(rows, start=1):
        for ci, val in enumerate(row):
            cell = table_shape.cell(ri, ci)
            cell.text = val
            for p in cell.text_frame.paragraphs:
                for run in p.runs:
                    set_run_font(run, 10, color=C_TEXT_DARK)
    return slide


def world_to_slide(lx: float, ly: float, origin_x: float, origin_y: float, scale: float):
    """Map Unity local position to slide EMU. ly positive = toward self (bottom)."""
    x = origin_x + lx * scale
    y = origin_y - ly * scale
    return x, y


def draw_hex_board(slide, slots: list[dict], *, left_offset=0.0, top_offset=0.0, hex_size=0.52):
    """Draw all 34 hex cells with coordinates."""
    scale = Inches(hex_size * 1.15)
    origin_x = Inches(4.9 + left_offset)
    origin_y = Inches(3.55 + top_offset)
    hex_w = Inches(hex_size)
    hex_h = Inches(hex_size * 1.08)

    for cell in slots:
        x, y, lx, ly = cell["x"], cell["y"], cell["lx"], cell["ly"]
        kind = cell_kind(x, y)
        cx, cy = world_to_slide(lx, ly, origin_x, origin_y, scale)
        shape = slide.shapes.add_shape(
            MSO_SHAPE.HEXAGON,
            cx - hex_w / 2,
            cy - hex_h / 2,
            hex_w,
            hex_h,
        )
        shape.rotation = 90.0  # pointy-top (尖顶朝向)
        shape.fill.solid()
        shape.fill.fore_color.rgb = kind_fill(kind)
        shape.line.color.rgb = C_STROKE
        shape.line.width = Pt(1.0 if kind == "normal" else 1.5)

        tf = shape.text_frame
        tf.clear()
        tf.vertical_anchor = MSO_ANCHOR.MIDDLE
        p1 = tf.paragraphs[0]
        p1.alignment = PP_ALIGN.CENTER
        r1 = p1.add_run()
        r1.text = f"({x},{y})"
        text_color = C_WHITE if kind != "normal" else C_TEXT_DARK
        set_run_font(r1, 8 if kind == "normal" else 9, bold=True, color=text_color)

        if kind != "normal":
            p2 = tf.add_paragraph()
            p2.alignment = PP_ALIGN.CENTER
            r2 = p2.add_run()
            short = {"score": "得分", "spawn_self": "己方", "spawn_opp": "对方"}[kind]
            r2.text = short
            set_run_font(r2, 7, color=C_WHITE if kind != "normal" else C_TEXT_MUTED)

    # center marker
    center = next(s for s in slots if s["x"] == 5 and s["y"] == 3)
    cx, cy = world_to_slide(center["lx"], center["ly"], origin_x, origin_y, scale)
    add_textbox(
        slide,
        cx + Inches(0.35),
        cy - Inches(0.12),
        Inches(1.2),
        Inches(0.25),
        "几何中心",
        size=8,
        color=C_ACCENT,
    )

    # direction labels
    add_textbox(slide, Inches(4.2), Inches(0.95), Inches(2), Inches(0.3), "↑ 对方方向（y 减小）", size=10, color=C_SPAWN_OPP, align=PP_ALIGN.CENTER)
    add_textbox(slide, Inches(4.2), Inches(6.15), Inches(2), Inches(0.3), "↓ 己方方向（y 增大）", size=10, color=C_SPAWN_SELF, align=PP_ALIGN.CENTER)


def add_legend(slide, left, top):
    items = [
        (C_SCORE, "得分区（7 格）"),
        (C_SPAWN_SELF, "己方出生点（3 格，下方短边三角区）"),
        (C_SPAWN_OPP, "对方出生点（3 格，上方短边 · 镜像显示）"),
        (C_NORMAL, "普通 battlefield 格（24 格）"),
    ]
    y = top
    add_textbox(slide, left, y, Inches(2.5), Inches(0.3), "图例", size=12, bold=True)
    y += Inches(0.32)
    for fill, label in items:
        rect = slide.shapes.add_shape(MSO_SHAPE.RECTANGLE, left, y, Inches(0.22), Inches(0.18))
        rect.fill.solid()
        rect.fill.fore_color.rgb = fill
        rect.line.color.rgb = C_STROKE
        add_textbox(slide, left + Inches(0.28), y - Inches(0.02), Inches(2.2), Inches(0.22), label, size=9)
        y += Inches(0.24)


def build_presentation(slots: list[dict]) -> Presentation:
    prs = Presentation()
    prs.slide_width = Inches(10)
    prs.slide_height = Inches(7.5)
    today = date.today().isoformat()

    # --- Slide 1: Cover ---
    slide = prs.slides.add_slide(prs.slide_layouts[6])
    bg = slide.shapes.add_shape(MSO_SHAPE.RECTANGLE, 0, 0, prs.slide_width, prs.slide_height)
    bg.fill.solid()
    bg.fill.fore_color.rgb = C_BG_TITLE
    bg.line.fill.background()
    add_textbox(
        slide,
        Inches(0.8),
        Inches(2.0),
        Inches(8.5),
        Inches(1.0),
        "战场女武神5 — 策划目标地图需求说明",
        size=32,
        bold=True,
        color=C_TEXT_LIGHT,
        align=PP_ALIGN.CENTER,
    )
    add_textbox(
        slide,
        Inches(0.8),
        Inches(3.1),
        Inches(8.5),
        Inches(0.5),
        "供 AI / 程序读取 · 描述游戏地图与得分机制",
        size=16,
        color=C_TEXT_MUTED,
        align=PP_ALIGN.CENTER,
    )
    meta = [
        f"规格来源：战场女武神5最新规则与卡牌-20260621.md",
        f"生成日期：{today}",
        f"棋盘验证：Unity Game.unity · {len(slots)} 个 BoardSlot（与规格一致）",
        "坐标系：逻辑 (x,y,p)；y 越大越靠近己方底边；p 为玩家侧 0/1",
    ]
    box = slide.shapes.add_textbox(Inches(1.2), Inches(4.0), Inches(7.6), Inches(2.0))
    tf = box.text_frame
    for i, line in enumerate(meta):
        p = tf.paragraphs[0] if i == 0 else tf.add_paragraph()
        run = p.add_run()
        run.text = line
        set_run_font(run, 12, color=C_TEXT_LIGHT)

    # --- Slide 2: Design intent ---
    add_bullet_slide(
        prs,
        "1. 设计意图（策划目标）",
        [
            "竖向六边形战棋盘，共 34 个可行走格子；5 条纵向列（6/7/8/7/6），整体呈上下较长、左右较窄的纺锤形。",
            "己方位于下方短边、对方位于上方短边；推进方向为自下向上，而非左右长边。",
            "上下短边各有一处三角出生区（各 3 格）：己方 (3,2)(2,3)(2,4)，对方逻辑同坐标、客户端镜像显示。",
            "正中央有一处连续得分区（7 格），呈菱形团块，是每回合得分阶段争夺的核心区域。",
            "左右最外列为侧翼通道，用于绕边、包夹或回避正面冲突（demo 阶段无固有地形效果）。",
            "设计目标：对局围绕「从出生点推进 → 占据中央得分区 → 防止对方占区得分」展开；",
            "走位与相对位置成为与打牌同等重要的决策维度。",
        ],
        subtitle="本节为策划权威描述；程序实现以本节为准，见末页「实现差异」。",
    )

    # --- Slide 3: Board spec table ---
    add_table_slide(
        prs,
        "2. 棋盘规格与坐标约定",
        ["项目", "规格值", "说明"],
        [
            ["格子总数", "34", "与 Unity 场景 Game.unity 中 BoardSlot 数量一致"],
            ["网格类型", "六边形（尖顶朝向）", "场景内 BoardSlot 碰撞体"],
            ["逻辑坐标", "(x, y, p)", "p 为玩家侧（0 或 1）"],
            ["几何中心", "(5, 3)", "棋盘世界坐标原点附近"],
            [
                "六边形距离",
                "max(|dx|, |dy|, |dz|)",
                "dz = (x1+y1) - (x2+y2)；用于移动力与攻击范围计算",
            ],
            ["y 轴方向", "y↑ = 己方底边", "y 越小越靠近对方方向"],
        ],
        col_widths=[1.6, 2.2, 5.2],
    )

    # --- Slide 4: Full map visual ---
    slide = prs.slides.add_slide(prs.slide_layouts[6])
    add_textbox(slide, Inches(0.5), Inches(0.25), Inches(9), Inches(0.5), "3. 策划目标地图 — 全图（34 格）", size=22, bold=True)
    add_textbox(
        slide,
        Inches(0.5),
        Inches(0.72),
        Inches(6),
        Inches(0.35),
        "每格标注逻辑坐标 (x,y)；颜色区分区域类型。与 Slot.cs / Game.unity 坐标一致。",
        size=10,
        color=C_TEXT_MUTED,
    )
    draw_hex_board(slide, slots)
    add_legend(slide, Inches(0.45), Inches(1.05))

    # --- Slide 5: ASCII grid reference ---
    ascii_grid = "\n".join(
        [
            "列→  2   3   4   5   6   7   8   9",
            "y=1  .   O   O   O   O   O   O   O    ← 远端（对方出生带）",
            "y=2  .   O   O   O   O   O   O   O",
            "y=3  O   O   O   O   O   O   O   .    ★ (5,3) 几何中心",
            "y=4  O   O   O   O   O   O   O   .",
            "y=5  O   O   O   O   O   O   .   .    ← 近端（己方出生带）",
            "",
            "O = 存在格子；. = 该 (x,y) 不存在",
        ]
    )
    slide = prs.slides.add_slide(prs.slide_layouts[6])
    add_textbox(slide, Inches(0.5), Inches(0.35), Inches(9), Inches(0.5), "4. 逻辑坐标矩阵（34 格存在性）", size=22, bold=True)
    box = slide.shapes.add_textbox(Inches(0.55), Inches(1.0), Inches(5.5), Inches(3.5))
    tf = box.text_frame
    p = tf.paragraphs[0]
    run = p.add_run()
    run.text = ascii_grid
    run.font.name = "Consolas"
    run.font.size = Pt(12)
    run.font.color.rgb = C_TEXT_DARK

    # compact annotated ASCII from spec
    annotated = "\n".join(
        [
            "      2  3  4  5  6  7  8  9",
            "y=1   .  .  .  .  .  .  .  .",
            "y=2   B  .  .  S  S  .  .  T",
            "y=3   B  .  S  S  S  .  .  T",
            "y=4   B  .  .  S  .  T  .  .",
            "y=5   .  .  .  .  .  .  .  .",
            "",
            "T=对方出生  B=己方出生  S=得分区（7 格菱形）",
        ]
    )
    box2 = slide.shapes.add_textbox(Inches(5.8), Inches(1.0), Inches(3.5), Inches(3.0))
    tf2 = box2.text_frame
    p2 = tf2.paragraphs[0]
    r2 = p2.add_run()
    r2.text = annotated
    r2.font.name = "Consolas"
    r2.font.size = Pt(12)
    r2.font.color.rgb = C_TEXT_DARK
    add_textbox(slide, Inches(0.55), Inches(4.6), Inches(8.8), Inches(0.8), "全部 34 格 (x,y) 列表见下一页表格；世界坐标取自 Game.unity BoardSlot。", size=10, color=C_TEXT_MUTED)

    # --- Slide 6: All cells table ---
    rows = [[str(i + 1), f"({s['x']}, {s['y']})", kind_label_cn(cell_kind(s["x"], s["y"])), f"({s['lx']}, {s['ly']})"] for i, s in enumerate(slots)]
    add_table_slide(
        prs,
        "5. 全部格子枚举（34 格 · 逻辑坐标 + 区域类型 + Unity 世界坐标）",
        ["#", "逻辑 (x,y)", "区域类型", "世界坐标 (lx, ly)"],
        rows,
        col_widths=[0.5, 1.4, 1.6, 2.2],
    )

    # --- Slide 7: Spawn points ---
    add_table_slide(
        prs,
        "6. 出生点（每方 3 英雄 · 三角排布）",
        ["阵营", "逻辑坐标 (x,y)", "部署角色", "备注"],
        [
            ["己方（下方短边三角区）", "(3, 2)", "三角区一格", "逻辑坐标见规格文档"],
            ["己方（下方短边三角区）", "(2, 3)", "三角区一格", ""],
            ["己方（下方短边三角区）", "(2, 4)", "三角区一格", ""],
            ["对方（上方短边 · 镜像）", "(7, 4)", "镜像显示格", "与 (3,2) 中心对称"],
            ["对方（上方短边 · 镜像）", "(8, 3)", "镜像显示格", "与 (2,3) 中心对称"],
            ["对方（上方短边 · 镜像）", "(8, 2)", "镜像显示格", "与 (2,4) 中心对称"],
        ],
        col_widths=[1.8, 1.6, 1.8, 2.4],
    )
    slide = prs.slides[-1]
    add_textbox(
        slide,
        Inches(0.5),
        Inches(5.8),
        Inches(9),
        Inches(0.8),
        "己方三角出生区 (3,2)(2,3)(2,4) 至中央 (5,3) 的六边形距离约 2～3；"
        "移动力 3 的英雄可在数回合内推进至得分区争夺。",
        size=11,
        color=C_TEXT_MUTED,
    )

    # --- Slide 8: Scoring zone table + map ---
    score_rows = [[str(i + 1), f"({xy[0]}, {xy[1]})", desc] for i, (xy, desc) in enumerate(SCORE_CELLS_ORDERED)]
    add_table_slide(
        prs,
        "7. 得分区（中央 7 格 · zone_id: center）",
        ["#", "逻辑坐标 (x,y)", "位置说明"],
        score_rows,
        col_widths=[0.5, 1.8, 5.2],
    )

    slide = prs.slides.add_slide(prs.slide_layouts[6])
    add_textbox(slide, Inches(0.5), Inches(0.25), Inches(9), Inches(0.5), "7b. 得分区 + 出生点 相对位置", size=22, bold=True)
    add_textbox(
        slide,
        Inches(0.5),
        Inches(0.72),
        Inches(9),
        Inches(0.35),
        "全图 34 格；深色 = 得分区，蓝 = 己方出生，红 = 对方出生。",
        size=10,
        color=C_TEXT_MUTED,
    )
    draw_hex_board(slide, slots, hex_size=0.46)
    add_legend(slide, Inches(0.45), Inches(1.05))

    # --- Slide 9: Scoring rules ---
    add_table_slide(
        prs,
        "8. 得分区结算规则（得分阶段）",
        ["触发条件", "统计对象", "计分规则", "结果"],
        [
            [
                "主要阶段双方均放弃继续行动后",
                "得分区任一格子内存活角色数（按 player_id）",
                "己方人数 > 对方",
                "己方 +2 分",
            ],
            ["（同上）", "只统计存活且在板上的角色；按棋子个数非占格数", "双方人数相同", "双方各 +1 分"],
            ["（同上）", "同一玩家占多格仍按英雄个数计", "己方人数 < 对方", "己方 0 分（对方 +2 分）"],
        ],
        col_widths=[2.0, 2.8, 1.6, 1.4],
    )
    slide = prs.slides[-1]
    add_textbox(
        slide,
        Inches(0.5),
        Inches(5.5),
        Inches(9),
        Inches(1.2),
        "与击杀得分（+3 分/次）叠加累计；先达到 9 分者获胜。"
        "当前设计为单一中央得分区；若将来拆分多区，每区独立结算一次。",
        size=11,
        color=C_TEXT_MUTED,
    )

    # --- Slide 10: YAML config ---
    yaml_text = """# 策划目标 — 中央得分区（zone_id: center）
scoring_zone_center:
  cells:
    - { x: 5, y: 3 }
    - { x: 4, y: 3 }
    - { x: 6, y: 3 }
    - { x: 5, y: 2 }
    - { x: 5, y: 4 }
    - { x: 4, y: 4 }
    - { x: 6, y: 2 }
  scoring:
    win_more: 2      # 区内人数多者
    tie_each: 1      # 人数相同双方各得

# 策划目标 — 出生点（每方 3 英雄）
spawn_self:
  cells:
    - { x: 3, y: 2 }
    - { x: 2, y: 3 }
    - { x: 2, y: 4 }
# 对方逻辑同 spawn_self；客户端镜像显示在上方短边（PPT 标注为 7,4 / 8,3 / 8,2）"""
    slide = prs.slides.add_slide(prs.slide_layouts[6])
    add_textbox(slide, Inches(0.5), Inches(0.35), Inches(9), Inches(0.5), "9. 配置速查（供程序 / AI 直接引用）", size=22, bold=True)
    box = slide.shapes.add_textbox(Inches(0.55), Inches(1.0), Inches(8.9), Inches(5.8))
    tf = box.text_frame
    p = tf.paragraphs[0]
    run = p.add_run()
    run.text = yaml_text
    run.font.name = "Consolas"
    run.font.size = Pt(11)
    run.font.color.rgb = C_TEXT_DARK

    # --- Slide 11: Implementation gap ---
    add_table_slide(
        prs,
        "10. 与当前程序实现的差异（AI 协作必读）",
        ["项目", "策划目标（权威）", "当前工程状态"],
        [
            ["出生点坐标", "(3,2)(2,3)(2,4) 三角区", "DeployInitialHeroes() 已用该组坐标 ✅"],
            ["得分区标记", "上表 7 格", "未实现标记与结算"],
            ["得分阶段结算", "每回合双方弃权后按人数计分", "未实现"],
            ["棋盘可视", "34 格六边形", "Game.unity 已具备 ✅"],
        ],
        col_widths=[1.8, 3.2, 3.5],
    )
    slide = prs.slides[-1]
    add_textbox(
        slide,
        Inches(0.5),
        Inches(5.6),
        Inches(9),
        Inches(0.9),
        "实现时以本 PPT 及 战场女武神5最新规则与卡牌-20260621.md 为准；"
        "出生点、得分区、得分阶段需按策划目标补齐。",
        size=11,
        bold=True,
        color=C_SPAWN_OPP,
    )

    return prs


def main():
    if not UNITY_SCENE.is_file():
        raise SystemExit(f"Unity scene not found: {UNITY_SCENE}")
    slots = parse_unity_slots()
    if len(slots) != 34:
        raise SystemExit(f"Expected 34 BoardSlots, got {len(slots)}")
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    prs = build_presentation(slots)
    prs.save(OUTPUT)
    print(f"Wrote {OUTPUT} ({len(prs.slides)} slides, {len(slots)} cells)")


if __name__ == "__main__":
    main()
