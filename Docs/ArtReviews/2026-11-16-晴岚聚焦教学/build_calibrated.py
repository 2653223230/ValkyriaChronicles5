"""Build editable focus-layer review frames on top of timestamped GameView screenshots."""
import importlib.util
import json
from pathlib import Path

OUT = Path(__file__).resolve().parent
RAW = OUT / 'raw'
CALIBRATED = OUT / 'calibrated'
spec = importlib.util.spec_from_file_location(
    'prior_review', OUT.parent / '2026-11-13-晴岚首战教学' / 'build_review.py')
old = importlib.util.module_from_spec(spec)
spec.loader.exec_module(old)

W, H = 1920, 1200
VIEW_H, FOOTER_H = 1080, 120
PAPER, INK, GREEN, GOLD = '#FBF8EF', '#183A32', '#356A54', '#DAA337'
MUTED, MASK = '#64746A', '#183A32'
rect, text, frame = old.rect, old.text, old.frame

SNAPSHOTS = {s['key']: s for s in json.loads((RAW / 'snapshots.json').read_text(encoding='utf-8'))}
IMAGE_HASHES_PATH = RAW / 'image-hashes.json'
IMAGE_HASHES = json.loads(IMAGE_HASHES_PATH.read_text(encoding='utf-8')) if IMAGE_HASHES_PATH.exists() else {}


def control_rect(snap, name):
    for control in snap.get('controls', []):
        if control['name'] == name:
            r = control['rect']
            return (r['x'], r['y'], r['w'], r['h'])
    raise KeyError(f"Control {name!r} missing from {snap['key']}")


def unit_rect(snap, unit_id):
    for unit in snap['units']:
        if unit['id'] == unit_id:
            r = unit['rect']
            return (r['x'], r['y'], r['w'], r['h'])
    raise KeyError(f"Unit {unit_id!r} missing from {snap['key']}")


def hand_rect(snap, entity_id):
    for card in snap['hand']:
        if card['id'] == entity_id:
            r = card['rect']
            return (r['x'], r['y'], r['w'], r['h'])
    raise KeyError(f"Hand entity {entity_id!r} missing from {snap['key']}")


def cell_polygon(snap, coord):
    for cell in snap['cells']:
        if cell['coord'] == coord:
            return [(p['x'], p['y']) for p in cell['polygon']]
    raise KeyError(f"Cell {coord!r} missing from {snap['key']}")


def polygon_bounds(points):
    xs, ys = [p[0] for p in points], [p[1] for p in points]
    return (min(xs), min(ys), max(xs) - min(xs), max(ys) - min(ys))


def border(name, box, color=GOLD, weight=5):
    x, y, w, h = box
    n = rect(name, x, y, w, h, PAPER, color, 7, weight)
    n['fills'] = []
    return n


def polygon_border(name, points, color=GOLD, weight=5):
    x, y, w, h = polygon_bounds(points)
    path = ' '.join(('M' if i == 0 else 'L') + f'{px-x:.3f} {py-y:.3f}' for i, (px, py) in enumerate(points)) + ' Z'
    return {'type': 'SVG', 'name': name, 'x': x, 'y': y, 'width': w, 'height': h,
            'svg': f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}"><path d="{path}" fill="none" stroke="{color}" stroke-width="{weight}"/></svg>'}


def mask(holes):
    """Create editable 28% native-rectangle dimming across the 1080p screenshot."""
    boxes = [polygon_bounds(h) if isinstance(h, list) else h for h in holes]
    xs = sorted(set([0, W] + [max(0, min(W, v)) for b in boxes for v in (b[0], b[0] + b[2])]))
    ys = sorted(set([0, VIEW_H] + [max(0, min(VIEW_H, v)) for b in boxes for v in (b[1], b[1] + b[3])]))
    result = []
    for x0, x1 in zip(xs, xs[1:]):
        for y0, y1 in zip(ys, ys[1:]):
            cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
            if any(x <= cx <= x + w and y <= cy <= y + h for x, y, w, h in boxes):
                continue
            n = rect('非本步操作区域 · 审核蒙层', x0, y0, x1 - x0, y1 - y0, MASK, r=0)
            n['opacity'] = 0.28
            result.append(n)
    return result


def prompt(title, body, x, y, button=None):
    w, h = 660, 200 if button is None else 252
    nodes = [rect('就近教学提示 · 设计图层', x, y, w, h, PAPER, GOLD, 12, 3),
             text('提示标题', title, x + 22, y + 18, w - 44, 24),
             text('提示内容', body, x + 22, y + 58, w - 44, 18, MUTED)]
    if button:
        bw = (w - 60) // 2 if isinstance(button, tuple) else w - 44
        if isinstance(button, tuple):
            for i, label in enumerate(button):
                bx = x + 22 + i * (bw + 12)
                nodes.extend([rect('设计按钮 · ' + label, bx, y + h - 58, bw, 40, GREEN if i else PAPER, GREEN, 8, 2),
                              text('按钮文案 · ' + label, label, bx + 8, y + h - 49, bw - 16, 16, PAPER if i else GREEN)])
        else:
            nodes.extend([rect('信息按钮 · 设计图层', x + 22, y + h - 54, w - 44, 38, GREEN, r=8),
                           text('信息按钮文案', button, x + 34, y + h - 46, w - 68, 17, PAPER)])
    return nodes


def frame_nodes(frame_key, source_key, snap_key, title, body, event, cancel, resources,
                focus_rects=(), focus_polygons=(), extra_holes=(), prompt_xy=(1220, 300),
                button=None, enemy_phase=False, chapter='移动与出牌', safe_holes=()):
    snap = SNAPSHOTS[snap_key]
    image_hash = IMAGE_HASHES.get(source_key, 'raw:' + source_key)
    nodes = [{'type': 'RECTANGLE', 'name': '真实 GameView 底图 · ' + source_key,
              'x': 0, 'y': 0, 'width': W, 'height': VIEW_H, 'locked': True,
              'fills': [{'type': 'IMAGE', 'imageHash': image_hash, 'scaleMode': 'FILL'}]}]

    # The only screenshot alteration is the old long instruction area at its measured bounds.
    nodes += [rect('覆盖旧顶部教学长文字', 586, 24, 850, 76, PAPER, r=0),
              text('轻量章节状态', chapter + ' · 实机底图＋待审聚焦层', 608, 47, 806, 22, GREEN)]

    holes = list(focus_rects) + list(focus_polygons) + list(extra_holes) + list(safe_holes)
    # Preserve screenshot details, weapon ranges, skill art, real cards, AP bar and existing scene content.
    holes.extend([(24, 410, 330, 520), (356, 779, 640, 43)])
    if not enemy_phase: holes.append((24,950,225,48))
    nodes += mask(holes)
    for i, box in enumerate(focus_rects):
        nodes.append(border('实际锚点金框 ' + str(i + 1), box))
    if frame_key in {'04-move-hover', '07-skill-hover', '13-shield-drag', '18-error-example'} and len(focus_rects) >= 2:
        for label, box in zip(('按住', '拖到这里'), focus_rects[:2]):
            x, y, w, _ = box
            tx = max(0, min(W - 130, x))
            ty = max(0, y - 32)
            nodes += [rect('拖动步骤标记 · ' + label, tx, ty, 124, 26, GREEN, r=7),
                      text('拖动步骤文案 · ' + label, label, tx + 9, ty + 3, 108, 15, PAPER)]
    for i, points in enumerate(focus_polygons):
        nodes.append(polygon_border('实际地面格斜四边形金框 ' + str(i + 1), points))

    px, py = prompt_xy
    nodes += prompt(title, body, px, py, button)
    nodes += [text('审核稿标识', '实机底图＋待审聚焦层', 1510, 37, 380, 18, PAPER),
              rect('底部审核合同', 0, VIEW_H, W, FOOTER_H, '#EAE5D8', r=0),
              text('真实资源状态', f"{frame_key}  |  实际 {snap['ap']} AP  |  手牌实体 {len(snap['hand'])} 张  |  {snap['round']} 轮", 24, 1090, 1872, 17),
              text('推进取消资源合同', f'推进事件：{event}    取消：{cancel}    资源：以实机底图和锚点为准；限制规则仍是提案未实现。{resources}', 24, 1128, 1872, 15, MUTED)]
    if enemy_phase:
        nodes.append(text('敌方阶段提示约束', '敌方行动截图：不显示跳过可用提示。', 24, 1157, 1872, 14, MUTED))
    return old.importer_order(frame(frame_key + ' · ' + title, 0, 0, W, H, nodes, PAPER))


FLOW = json.loads((OUT / 'flow.json').read_text(encoding='utf-8'))


def make_specs():
    a = SNAPSHOTS
    # Review frame -> screenshot state, interaction geometry, and flow.json contract index.
    # Some teaching interactions intentionally reuse the same captured state before/after an event.
    rows = [
        ('01-opening', '01-opening', '01-opening', 0, [control_rect(a['01-opening'], 'Party row mage')], {'prompt_xy': (350, 310), 'chapter': '认识队员'}),
        ('02-mage-selected', '02-mage-selected', '02-mage-selected', 1, [], {'prompt_xy': (350, 310), 'button': '试试免费短步', 'chapter': '认识队员', 'safe_holes': [(24, 126, 292, 268)]}),
        ('03-shortstep-done', '02-mage-selected', '02-mage-selected', 2, [], {'focus_polygons': [cell_polygon(a['02-mage-selected'], 'E6')], 'safe_holes': [unit_rect(a['02-mage-selected'], 'mage')], 'chapter': '免费短步'}),
        ('04-move-hover', '03-shortstep-done', '03-shortstep-done', 3, [hand_rect(a['03-shortstep-done'], 'card-7'), unit_rect(a['03-shortstep-done'], 'mage')], {'chapter': '移动与出牌', 'extra_holes': [(913, 535, 205, 104)]}),
        ('05-move-landing', '04-move-hover', '04-move-hover', 4, [hand_rect(a['04-move-hover'], 'card-7')], {'focus_polygons': [cell_polygon(a['04-move-hover'], 'E4')], 'safe_holes': [unit_rect(a['04-move-hover'], 'mage')], 'chapter': '移动预览'}),
        ('06-move-done', '05-move-landing', '05-move-landing', 5, [], {'chapter': '移动与出牌'}),
        ('07-skill-hover', '06-move-done', '06-move-done', 6, [hand_rect(a['06-move-done'], 'card-12'), unit_rect(a['06-move-done'], 'mage')], {'chapter': '战技', 'extra_holes': [(1033, 461, 205, 104)]}),
        ('08-skill-target-preview', '07-skill-hover', '07-skill-hover', 7, [], {'safe_holes': [unit_rect(a['07-skill-hover'], 'mage')], 'chapter': '战技预览'}),
        ('09-skill-target', '18-skill-target-preview', '18-skill-target-preview', 8, [unit_rect(a['18-skill-target-preview'], 'crossbow')], {'safe_holes': [unit_rect(a['18-skill-target-preview'], 'mage')], 'chapter': '战技'}),
        ('10-burst-done', '09-burst-done', '09-burst-done', 9, [], {'chapter': '普通攻击'}),
        ('11-archer-selected', '10-archer-selected', '10-archer-selected', 10, [control_rect(a['10-archer-selected'], 'Party row archer')], {'safe_holes': [(24, 126, 292, 268)], 'chapter': '普通攻击'}),
        ('12-basic-attack', '10-archer-selected', '10-archer-selected', 10, [], {'focus_polygons': [cell_polygon(a['10-archer-selected'], 'C4')], 'safe_holes': [unit_rect(a['10-archer-selected'], 'archer')], 'chapter': '普通攻击'}),
        ('13-shield-drag', '11-basic-done', '11-basic-done', 11, [hand_rect(a['11-basic-done'], 'card-15'), unit_rect(a['11-basic-done'], 'mage')], {'chapter': '回合交接'}),
        ('14-end-turn', '13-shield-done', '13-shield-done', 12, [control_rect(a['13-shield-done'], '确认回合')], {'prompt_xy': (1260, 550), 'chapter': '回合交接'}),
        ('15-retain-none', '14-retain-none', '14-retain-none', 13, [control_rect(a['14-retain-none'], '确认回合')], {'prompt_xy': (1260, 550), 'chapter': '留牌'}),
        ('14B-retain-selected', '15-retain-selected', '15-retain-selected', 13, [hand_rect(a['15-retain-selected'], 'card-1')], {'prompt_xy': (1260, 550), 'safe_holes': [control_rect(a['15-retain-selected'], '确认回合')], 'chapter': '留牌'}),
        ('16-enemy-start', '16-enemy-start', '13-shield-done', 14, [], {'chapter': '敌方行动', 'enemy_phase': True}),
        ('17-round2', '17-round2', '17-round2', 15, [], {'chapter': '自由战斗', 'button': '开始自由战斗'}),
        ('18-error-example', '03-shortstep-done', '03-shortstep-done', 16, [hand_rect(a['03-shortstep-done'], 'card-7'), unit_rect(a['03-shortstep-done'], 'mage')], {'chapter': '移动与出牌', 'extra_holes': [(913, 535, 205, 104)]}),
        ('19-exit-confirm', '03-shortstep-done', '03-shortstep-done', 17, [], {'prompt_xy': (667, 400), 'button': ('继续教学', '退出引导，继续本局'), 'chapter': '退出确认'}),
    ]
    specs = []
    for key, source, snap, flow_index, focus, opts in rows:
        flow = FLOW[flow_index]
        title, body = flow['title'], flow['body']
        # The original 02 instruction has an explicitly designed info button; its label is fixed by the review contract.
        if key == '02-mage-selected':
            opts['button'] = '看看免费短步位置'
        specs.append(dict(frame_key=key, source_key=source, snap_key=snap,
                          title=title, body=body,
                          event=flow.get('event', ''), cancel='不扣费、不提交行动；保持本步。',
                          resources=flow.get('resources', ''), focus_rects=focus, **opts))
    # Each frame shows the pre-action state and highlights only its next action.
    specs=[s for s in specs if s['frame_key']!='11-archer-selected']
    b={s['frame_key']:s for s in specs}
    b['02-mage-selected'].update(focus_rects=[(24,410,330,520),(356,779,640,43)],prompt_xy=(370,320))
    b['05-move-landing'].update(focus_rects=[unit_rect(a['04-move-hover'],'mage')],focus_polygons=[],extra_holes=[(780,470,460,250),(913,535,205,104)])
    b['06-move-done'].update(focus_polygons=[cell_polygon(a['05-move-landing'],'E4')],safe_holes=[unit_rect(a['05-move-landing'],'mage')])
    b['08-skill-target-preview'].update(focus_rects=[unit_rect(a['07-skill-hover'],'mage')],extra_holes=[(840,300,400,305),(1033,461,205,104)])
    b['09-skill-target'].update(safe_holes=[(830,290,365,300)])
    b['10-burst-done'].update(focus_rects=[control_rect(a['09-burst-done'],'Party row archer')],prompt_xy=(350,240))
    b['12-basic-attack'].update(focus_rects=[unit_rect(a['10-archer-selected'],'patrol')])
    b['15-retain-none'].update(focus_rects=[(350,832,458,196),control_rect(a['14-retain-none'],'确认回合')],prompt_xy=(1240,550))
    b['14B-retain-selected'].update(title='再点一次，取消弃牌选择',body='上浮的攻击牌将被弃掉。\n当前弃 1 张，保留 1 张，下轮补 4 张。',event='再次点攻击牌回到零弃牌；也可按实际选择确认，进入敌方阶段。',resources='可选分支，非必做步骤；数量实时更新。')
    b['16-enemy-start'].update(focus_polygons=[cell_polygon(a['13-shield-done'],co) for co in ['B6','C6','D6']])
    b['17-round2'].update(focus_rects=[(356,779,640,43),(350,832,1148,196)])
    for key, target in [('05-move-landing','B2'),('06-move-done','B2'),('08-skill-target-preview','B4'),('09-skill-target','B4')]:
        b[key]['cancel']='清除未结算预览，回到 '+target+'；不扣费。'
    for key in ['15-retain-none','14B-retain-selected']:
        b[key]['cancel']='返回己方行动界面 D2，不结算敌方回合。'
    b['16-enemy-start']['cancel']='演出期间等待；不受理退出或重复确认。'
    b['17-round2']['resources']='零弃牌路线实拍：6 AP、5 张牌、术师 4 HP；选弃分支按实际手牌显示。'
    b['19-exit-confirm']['cancel']='关闭弹窗，回到打开前步骤。'
    return specs


def write_outputs():
    CALIBRATED.mkdir(exist_ok=True)
    specs = make_specs()
    expected = {s['frame_key'] for s in specs}
    prior_generated = {'01-opening', '02-mage-selected', '03-shortstep-done', '04-move-hover',
                       '05-move-landing', '06-move-done', '07-skill-hover',
                       '08-skill-target-preview', '09-burst-done', '10-archer-selected',
                       '11-basic-done', '12-shield-done', '13-retain-none',
                       '14B-retain-selected', '15-enemy-start', '16-round2',
                       '17-error-example', '18-exit-confirm','11-archer-selected'}
    for path in CALIBRATED.glob('*.json'):
        if path.stem in prior_generated and path.stem not in expected:
            path.unlink()
    for s in specs:
        data = frame_nodes(**s)
        (CALIBRATED / (s['frame_key'] + '.json')).write_text(json.dumps(data, ensure_ascii=False), encoding='utf-8')
    validate_outputs(specs)
    print(f'Generated and validated {len(specs)} frames in {CALIBRATED}')


def validate_outputs(specs):
    expected = {s['frame_key'] for s in specs}
    actual = {p.stem for p in CALIBRATED.glob('*.json')}
    if actual != expected:
        raise ValueError(f'Output set mismatch: expected {len(expected)} frames, got {len(actual)}')
    for path in sorted(CALIBRATED.glob('*.json')):
        data = json.loads(path.read_text(encoding='utf-8'))
        if data.get('type') != 'FRAME' or (data.get('width'), data.get('height')) != (W, H):
            raise ValueError(f'Invalid frame bounds or root type: {path.name}')
        children = data.get('children', [])
        image_nodes = [n for n in children if n.get('type') == 'RECTANGLE' and n.get('name', '').startswith('真实 GameView 底图')]
        image_hash = image_nodes[0].get('fills', [{}])[0].get('imageHash', '') if image_nodes else ''
        if len(image_nodes) != 1 or not (image_hash.startswith('raw:') or image_hash in IMAGE_HASHES.values()):
            raise ValueError(f'Missing source screenshot placeholder: {path.name}')
        if len(children) < 8:
            raise ValueError(f'Unexpectedly small layer tree: {path.name}')
        for node in children:
            if node.get('type') in {'RECTANGLE', 'TEXT', 'SVG'} and not node.get('name'):
                raise ValueError(f'Unnamed editable layer in {path.name}')


if __name__ == '__main__':
    write_outputs()
