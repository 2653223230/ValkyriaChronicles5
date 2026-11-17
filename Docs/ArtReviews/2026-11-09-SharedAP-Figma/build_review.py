"""Build five editable native Figma layer trees for the 2026-11-09 review."""

import copy
import json
from pathlib import Path


OUT = Path(__file__).resolve().parent
HASH = '7b70b69ea70a9f5fca6267726f8782df5a6fb3f2'

IVORY = '#F4F0E4'
PAPER = '#FBF8EF'
INK = '#183A32'
GREEN = '#356A54'
MINT = '#A8C9AC'
GOLD = '#BE9C55'
MUTED = '#64746A'
HAIRLINE = '#C9C1AA'
RED = '#B75D49'


def paint(hex_color):
    return {
        'type': 'SOLID',
        'color': {
            key: int(hex_color.lstrip('#')[index:index + 2], 16) / 255
            for key, index in [('r', 0), ('g', 2), ('b', 4)]
        },
    }


def rect(name, x, y, width, height, fill=IVORY, stroke=None, radius=10, stroke_width=1):
    node = {
        'type': 'RECTANGLE', 'name': name,
        'x': x, 'y': y, 'width': width, 'height': height,
        'fills': [paint(fill)], 'cornerRadius': radius,
    }
    if stroke:
        node.update(strokes=[paint(stroke)], strokeWeight=stroke_width)
    return node


def text(name, value, x, y, width=280, size=20, color=INK, weight=None):
    node = {
        'type': 'TEXT', 'name': name, 'characters': value,
        'x': x, 'y': y, 'width': width, 'height': 1,
        'fontFamily': 'Microsoft YaHei, Noto Sans CJK SC, Inter',
        'fontSize': size,
        'lineHeight': {'unit': 'PERCENT', 'value': 132},
        'fills': [paint(color)],
    }
    if weight:
        node['fontWeight'] = weight
    return node


def frame(name, x, y, width, height, children, fill=None, stroke=None, radius=12):
    node = {
        'type': 'FRAME', 'name': name,
        'x': x, 'y': y, 'width': width, 'height': height,
        'children': children, 'fills': [] if fill is None else [paint(fill)],
        'clipsContent': True,
    }
    if stroke:
        node.update(strokes=[paint(stroke)], strokeWeight=1)
    return node


def image(name):
    return {
        'type': 'RECTANGLE', 'name': name, 'x': 0, 'y': 0,
        'width': 1920, 'height': 1080,
        'fills': [{'type': 'IMAGE', 'scaleMode': 'FILL', 'imageHash': HASH}],
        'locked': True,
    }


def svg(name, markup, x, y, width, height):
    return {'type': 'SVG', 'name': name, 'x': x, 'y': y, 'width': width, 'height': height, 'svg': markup}


def glyph_icon(name, glyph, x, y, size=44, fill=GREEN, color=IVORY):
    # A small native editable badge; the glyph remains ordinary Figma text.
    return frame(name, x, y, size, size, [
        rect(name + '底', 0, 0, size, size, fill, radius=size // 2),
        text(name + '字形', glyph, 0, 3, size, 23, color),
    ], fill=None)


def card_icon(kind, x, y, color=GREEN):
    paths = {
        'attack': '<path d="M7 34 L30 11 M23 10 L34 21 M5 29 L13 37 M19 8 L27 16"/>',
        'move': '<path d="M5 27 C11 21 15 34 21 27 C27 20 31 32 37 25 M7 16 L13 10 L19 16 M13 10 L13 21"/>',
        'charge': '<path d="M6 31 L18 19 L23 24 L35 11 M27 10 L36 10 L36 19 M7 10 L7 15 M5 12 L10 12"/>',
        'shield': '<path d="M21 6 L34 11 L32 25 C29 32 25 36 21 38 C17 36 13 32 10 25 L8 11 Z M15 22 L19 26 L27 17"/>',
        'heal': '<path d="M21 8 L21 34 M8 21 L34 21 M29 9 C34 12 37 17 37 22"/>',
    }
    path = paths[kind]
    markup = (
        f'<svg xmlns="http://www.w3.org/2000/svg" width="44" height="44" viewBox="0 0 44 44">'
        f'<circle cx="22" cy="22" r="20" fill="{IVORY}" stroke="{GOLD}" stroke-width="1.5"/>'
        f'<g fill="none" stroke="{color}" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">{path}</g>'
        '</svg>'
    )
    return svg('卡牌图标 ' + kind, markup, x, y, 44, 44)


CARDS = [
    ('攻击', 'attack', '1 AP', '按执行者武器造成伤害', 'weapon'),
    ('机动', 'move', '1 AP', '移动 1–2 格', 'move'),
    ('突击', 'charge', '2 AP', '移动后进行武器攻击', 'charge'),
    ('护盾', 'shield', '1 AP', '获得 3 点护盾', 'shield'),
    ('治疗', 'heal', '1 AP', '恢复 3 点生命', 'heal'),
]


def make_card(name, icon, cost, effect, kind, x, y, state=None, selected=False):
    stroke = GOLD if selected else GREEN if state == 'focus' else HAIRLINE
    children = [
        rect('卡牌纸面', 0, 0, 216, 184, PAPER, stroke=stroke, radius=13, stroke_width=2 if selected or state == 'focus' else 1),
        card_icon(icon, 13, 13),
        text('卡牌名称', name, 66, 18, 87, 24, INK),
        rect('费用徽标底', 151, 18, 53, 27, '#E6DDC6', radius=13),
        text('费用徽标', cost, 155, 21, 48, 16, INK),
        text('卡牌效果', effect, 14, 77, 188, 18, INK),
    ]
    if state:
        children.append(text('卡牌状态', state, 14, 157, 188, 14, GREEN))
    if selected:
        children.extend([
            rect('弃牌徽章底', 133, -20, 72, 25, '#E9D7AE', radius=12),
            text('弃牌徽章', '待弃', 151, -17, 44, 15, INK),
        ])
    return frame('手牌 ' + name, x, y, 216, 184, children)


UNIT_DATA = [
    {'name': '先锋', 'glyph': '先', 'weapon': '邻格 · 3伤', 'hp': '9/9 HP', 'x': 777, 'y': 607},
    {'name': '射手', 'glyph': '射', 'weapon': '射程3 · 2伤', 'hp': '6/6 HP', 'x': 716, 'y': 555},
    {'name': '术师', 'glyph': '术', 'weapon': '射程2 · 1伤', 'hp': '6/6 HP', 'x': 837, 'y': 642},
]


def mission_and_resource(nodes):
    nodes.extend([
        frame('胜利目标', 24, 22, 304, 70, [
            text('关卡名', '晴岚炮台', 17, 9, 170, 23, INK),
            text('目标', '目标 · 击毁炮座', 17, 40, 250, 16, MUTED),
        ], PAPER, stroke=HAIRLINE),
    ])


def instruction(nodes, title, detail, x=586, y=24, width=850):
    nodes.append(frame('教学提示', x, y, width, 76, [
        text('教学主句', title, 19, 12, width - 38, 23, INK),
        text('教学补充', detail, 19, 45, width - 38, 15, MUTED),
    ], PAPER, stroke=GOLD))


def roster(nodes, selected=None, compact_hint=None):
    # Small floating role badges keep the battlefield mostly uncovered.
    for index, unit in enumerate(UNIT_DATA):
        y = 126 + index * 82
        focus = unit['name'] == selected
        nodes.append(frame('队员 ' + unit['name'], 24, y, 292, 68, [
            rect('行底', 0, 0, 292, 68, '#F6F2E8', stroke=GOLD if focus else HAIRLINE, radius=11, stroke_width=2 if focus else 1),
            frame('队员原立绘', 7, 6, 50, 56, [dict(image('角色头像裁切'), x=-43, y=-152-index*92, fills=[{'type':'IMAGE','scaleMode':'FILL','imageHash':'91d3a53ea8965d6c0249fec4904396a036f88486'}])]),
            text('姓名', unit['name'], 66, 9, 94, 19, INK),
            text('武器摘要', unit['weapon'], 66, 36, 150, 14, MUTED),
            text('生命', unit['hp'], 215, 23, 68, 13, GREEN),
        ], fill=None))
        if compact_hint == unit['name']:
            nodes.append(frame('接牌标记', 191, y - 8, 118, 28, [
                rect('接牌标记底', 0, 0, 118, 28, '#E7D9AF', radius=13),
                text('接牌文案', '拖给射手', 12, 4, 96, 14, INK),
            ], fill=None))


def ground_marker(name, x, y, color=GREEN, label=None, label_x=None, label_y=None, label_width=170):
    markup = (
        f'<svg xmlns="http://www.w3.org/2000/svg" width="82" height="48" viewBox="0 0 82 48">'
        f'<ellipse cx="41" cy="24" rx="37" ry="19" fill="{color}" fill-opacity=".10" stroke="{color}" stroke-width="3"/>'
        f'<ellipse cx="41" cy="24" rx="4" ry="3" fill="{color}"/></svg>'
    )
    result = [svg(name, markup, x - 41, y - 24, 82, 48)]
    if label:
        px = label_x if label_x is not None else x + 47
        py = label_y if label_y is not None else y - 30
        result.append(frame(name + '标签', px, py, label_width, 34, [
            rect('标签纸面', 0, 0, label_width, 34, PAPER, stroke=GOLD, radius=17),
            text('标签文案', label, 10, 7, label_width - 20, 15, INK),
        ], fill=None))
    return result


def hand(nodes, focus_card=None, spent_attack=False, discard=False):
    if discard:
        header = '点选待弃 · 未选牌保留'
        selected_names = {'机动', '治疗'}
    else:
        header = '手牌 · 伤害按接牌队员武器结算'
        selected_names = set()
    nodes.append(frame('唯一共享AP计数', 356, 785, 150, 36, [
        rect('AP底', 0, 0, 150, 36, PAPER, stroke=HAIRLINE, radius=18),
        text('AP标题', '全队 AP', 11, 9, 65, 16, MUTED),
        text('AP值', '6 / 6', 91, 5, 50, 22, GREEN),
    ], fill=None))
    if discard:
        nodes.append(frame('手牌说明纸签', 520, 750, 650, 36, [
        rect('手牌说明底', 0, 0, 650, 36, PAPER, stroke=HAIRLINE, radius=18),
        text('手牌栏状态', header, 15, 9, 620, 15, MUTED),
    ], fill=None))
    for index, (name, icon, cost, effect, kind) in enumerate(CARDS):
        if spent_attack and name == '攻击':
            state = '待目标确认'
        elif discard:
            state = '已选待弃' if name in selected_names else '保留'
        else:
            state = '拖给射手' if focus_card == name else None
        y = 834 - (22 if name in selected_names else 0)
        nodes.append(make_card(name, icon, cost, effect, kind, 356 + index * 230, y, state,
                               selected=name in selected_names))
    turn_button(nodes, '弃 2 张并结束回合' if discard else '结束回合')


def turn_button(nodes, label):
    nodes.append(frame('常驻结束回合按钮', 1610, 842, 280, 58, [
        rect('结束回合底', 0, 0, 280, 58, GREEN, stroke=GOLD, radius=15, stroke_width=2),
        text('结束回合文字', label, 17, 17, 246, 19, IVORY),
    ], fill=None))


def build_opening():
    nodes = [image('晴岚开局底图 / 锁定无HUD截图')]
    mission_and_resource(nodes)
    instruction(nodes, '把「攻击」拖给射手', '拖入前不选目标；将按射手武器造成 2 伤。')
    roster(nodes, selected='射手', compact_hint='射手')
    nodes.extend(ground_marker('射手起始底座', 716, 555, GREEN))
    hand(nodes, focus_card='攻击')
    nodes.append(frame('操作跳过', 24, 864, 292, 54, [
        rect('跳过纸面', 0, 0, 292, 54, PAPER, stroke=HAIRLINE, radius=12),
        text('跳过文案', '跳过教学', 18, 15, 250, 17, MUTED),
    ], fill=None))
    return frame('01 开场教学', 0, 0, 1920, 1080, nodes)


def build_preview():
    nodes = [image('晴岚开局底图 / 锁定无HUD截图')]
    mission_and_resource(nodes)
    instruction(nodes, '拖给射手，先看本次攻击', '预览显示射程、伤害和 AP；松手锁定射手，移出则清除。')
    roster(nodes, selected='射手')
    nodes.extend(ground_marker('射手当前底座', 716, 555, GREEN, '接牌中 · 射手', 755, 507, 158))
    # Centers sampled from the opening rule state and its Unity camera.
    centers = [[896,461],[776,461],[836,498],[656,461],[716,498],[776,535],[836,572],[896,609],[596,498],[656,535],[896,683]]
    for cx, cy in centers:
        nodes.insert(1, svg('可攻击格', '<svg xmlns="http://www.w3.org/2000/svg" width="112" height="68"><path d="M56 1 L111 34 L56 67 L1 34 Z" fill="#F2C276" fill-opacity=".14" stroke="#EABE70" stroke-width="3"/></svg>', cx-56, cy-34, 112, 68))
    nodes.append(frame('手边拖动攻击卡', 764, 480, 148, 92, [
        rect('手边卡底', 0, 0, 148, 92, PAPER, stroke=GOLD, radius=13, stroke_width=2),
        card_icon('attack', 9, 10),
        text('手边卡名称', '攻击 · 1 AP', 59, 17, 82, 16, INK),
        text('手边卡效果', '射手造成 2 伤', 12, 61, 128, 14, MUTED),
    ], fill=None))
    hand(nodes, focus_card='攻击')
    return frame('02 拖给射手预览', 0, 0, 1920, 1080, nodes)


def build_target():
    nodes = [image('晴岚开局底图 / 锁定无HUD截图')]
    mission_and_resource(nodes)
    instruction(nodes, '射手已锁定 · 点击游戏实际亮起的敌人攻击', '确认后执行：射手造成 2 伤并消耗 1 AP。')
    roster(nodes, selected='射手')
    nodes.extend(ground_marker('射手执行者底座', 716, 555, GREEN, '执行者 · 射手', 753, 506, 151))
    nodes.extend(ground_marker('巡兵起始底座', 897, 462, GOLD, '巡兵 · 2 伤', 944, 416, 132))
    nodes.append(frame('目标提示', 1156, 413, 256, 74, [
        rect('目标提示底', 0, 0, 256, 74, PAPER, stroke=GOLD, radius=13),
        text('目标确认说明', '点击巡兵执行攻击', 15, 11, 226, 18, INK),
        text('目标确认费用', '执行后 AP 6 → 5', 15, 42, 226, 15, MUTED),
    ], fill=None))
    hand(nodes, spent_attack=True)
    return frame('03 锁定敌人', 0, 0, 1920, 1080, nodes)


def range_legend():
    # Abstract range cue only; no board squares or enemies are asserted legal.
    nodes = [
        rect('射程图底', 0, 0, 250, 72, PAPER, stroke=HAIRLINE, radius=12),
        text('射程图题', '射手武器', 14, 12, 200, 18, INK),
        text('射程图注', '单体 · 2 伤 · 射程 3', 14, 41, 220, 15, MUTED),
    ]
    return frame('射程小图示 / 仅说明距离', 1638, 128, 250, 72, nodes)


def base_action_badge(name, title, detail, x, y, glyph, accent):
    return frame(name, x, y, 270, 86, [
        rect('行动底', 0, 0, 270, 86, PAPER, stroke=HAIRLINE, radius=13),
        rect('行动徽标', 8, 8, 40, 40, PAPER, stroke=accent, radius=20),
        text('行动图标', glyph, 13, 14, 36, 21, accent),
        text('行动名称', title, 57, 11, 194, 18, INK),
        text('行动说明', detail, 57, 43, 198, 14, MUTED),
    ], fill=None)


def build_basics():
    nodes = [image('晴岚开局底图 / 锁定无HUD截图')]
    mission_and_resource(nodes)
    instruction(nodes, '基础行动 · 选中射手', '只点亮实际可执行对象；取消预览不会消耗行动。')
    roster(nodes, selected='射手')
    nodes.extend(ground_marker('已选射手底座', 716, 555, GREEN, '当前队员', 750, 508, 122))
    nodes.append(range_legend())
    nodes.append(frame('射手架弩技能前后示意', 24, 420, 292, 198, [
        rect('技能图底', 0, 0, 292, 198, PAPER, stroke=HAIRLINE, radius=14),
        text('技能标题', '架弩 · 打出战技牌，消耗 1 AP', 14, 12, 264, 18, INK),
        text('技能前标题', '使用前', 18, 52, 95, 15, MUTED),
        text('技能后标题', '使用后', 168, 52, 95, 15, GREEN),
        rect('技能前示意框', 16, 78, 112, 58, '#E7E2D4', stroke=HAIRLINE, radius=10),
        rect('技能后示意框', 164, 78, 112, 58, '#E4EBD9', stroke=MINT, radius=10),
        text('技能前数值', '2 伤 · 射程 3', 22, 98, 102, 15, INK),
        text('技能变化箭头', '→', 132, 96, 32, 20, GOLD),
        text('技能后数值', '4 伤 · 射程 4', 170, 98, 102, 15, INK),
        text('技能持续条件', '移动会解除；最迟下回合结束。', 14, 157, 264, 13, MUTED),
    ], fill=None))
    nodes.append(base_action_badge('免费短步说明', '短步 · 免费', '点击实际出现的足迹格', 356, 675, '足', GREEN))
    nodes.append(base_action_badge('基础普攻说明', '普攻 · 2 AP', '点击亮起敌人格下的小剑', 642, 675, '剑', GOLD))
    for cx, cy in ((776,535),(656,535)):
        nodes.append(svg('免费短步足迹', '<svg xmlns="http://www.w3.org/2000/svg" width="70" height="42"><path d="M35 1 L69 21 L35 41 L1 21 Z" fill="#DBEDC9" stroke="#37654E" stroke-width="2"/><g fill="#37654E"><ellipse cx="28" cy="18" rx="4" ry="7"/><circle cx="28" cy="27" r="3"/><ellipse cx="40" cy="14" rx="4" ry="7"/><circle cx="40" cy="23" r="3"/></g></svg>', cx-35,cy-21,70,42))
    nodes.extend(ground_marker('合法普攻 巡兵',896,461,GOLD,'普攻 · 2 AP',929,448,138))
    hand(nodes)
    return frame('04 基础短步与普攻', 0, 0, 1920, 1080, nodes)


def build_discard():
    nodes = [image('晴岚开局底图 / 锁定无HUD截图')]
    mission_and_resource(nodes)
    instruction(nodes, '想留下的牌不要点', '点牌加入待弃；再点可撤回。未选中的牌会保留。')
    roster(nodes)
    hand(nodes, discard=True)
    nodes.append(frame('弃牌计数提示', 24, 504, 292, 164, [
        rect('确认纸面', 0, 0, 292, 164, PAPER, stroke=GOLD, radius=14),
        text('弃牌计数', '已选 2 张 · 保留 3 张', 16, 18, 260, 18, INK),
        text('补牌结果', '下轮补牌 2 张', 16, 51, 260, 16, MUTED),
        text('确认提示', '确认后结束回合', 16, 84, 260, 15, MUTED),
    ], fill=None))
    return frame('05 弃牌', 0, 0, 1920, 1080, nodes)


def importer_order(node):
    """Match the 11-03 importer convention by reversing every child stack."""
    if 'children' in node:
        for child in node['children']:
            importer_order(child)
        node['children'].reverse()
    return node


SPECS = [
    ('01-开场教学.json', build_opening),
    ('02-拖给射手预览.json', build_preview),
    ('03-锁定敌人.json', build_target),
    ('04-基础短步与普攻.json', build_basics),
    ('05-弃牌.json', build_discard),
]


def main():
    for filename, builder in SPECS:
        tree = importer_order(builder())
        (OUT / filename).write_text(json.dumps(tree, ensure_ascii=False), encoding='utf-8')
    print(f'Generated {len(SPECS)} editable Figma layer trees in {OUT}')


if __name__ == '__main__':
    main()
