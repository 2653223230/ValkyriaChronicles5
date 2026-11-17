"""Editable review only. No Unity assets or gameplay code are changed."""
import copy
import importlib.util
import json
from pathlib import Path

OUT = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('prior', OUT.parent/'2026-11-13-晴岚首战教学/build_review.py')
old = importlib.util.module_from_spec(spec)
spec.loader.exec_module(old)
old.CARDS=[(a,{'攻击':'攻','机动':'动','突击':'突'}.get(a,b),c,d,e) for a,b,c,d,e in old.CARDS]
PAPER, INK, GREEN, GOLD = '#FBF8EF', '#183A32', '#356A54', '#DAA337'
rect, text, frame = old.rect, old.text, old.frame
W, H = 1920, 1080
START=['攻击','机动','突击','护盾','战技']
MOVED=['攻击','突击','护盾','战技']
BURST=['攻击','突击','护盾']
KEPT=['攻击','突击']
ROSTER_M=(21,413,308,99)
ROSTER_A=(21,308,308,99)
AP=(350,770,580,70)
END=(1540,735,354,70)
SKIP=(24,1008,260,48)
POS_E6=(879,606,88,86)
POS_E4=(879,454,88,86)
TARGET_D4=(753,454,88,86)
TARGET_C4=(627,454,88,86)
def cr(i):return (350+i*226,861,220,170)
def ground(c):
    x,y=old.xy(c);return (x-61,y-37,122,74)

def border(name,box,color=GOLD,weight=5):
    x,y,w,h=box;n=rect(name,x,y,w,h,PAPER,color,10,weight);n['fills']=[];return n

def mask(holes):
    # Native rectangles form a true union of rectangular spotlight openings.
    xs=sorted(set([0,W]+[max(0,min(W,x)) for b in holes for x in (b[0],b[0]+b[2])]))
    ys=sorted(set([0,H]+[max(0,min(H,y)) for b in holes for y in (b[1],b[1]+b[3])]))
    result=[]
    for x0,x1 in zip(xs,xs[1:]):
        for y0,y1 in zip(ys,ys[1:]):
            cx,cy=(x0+x1)/2,(y0+y1)/2
            if any(x<=cx<=x+w and y<=cy<=y+h for x,y,w,h in holes):continue
            n=rect('非本步操作区域 · 半透明且拦截输入',x0,y0,x1-x0,y1-y0,INK,r=0)
            n['opacity']=.29;result.append(n)
    return result

def note(title,body,x=1302,y=430,button=None):
    h=190 if not button else 248
    n=[rect('就近教学提示',x,y,586,h,PAPER,GOLD,14,3),text('当前操作',title,x+23,y+20,540,27),text('操作说明',body,x+23,y+67,530,21,'#4F6559')]
    if button:
        n += [rect('阅读或退出按钮',x+23,y+h-65,540,47,GREEN,r=9),text('按钮文案',button,x+40,y+h-57,500,22,PAPER)]
    return n

def role_info(archer=False):
    title='射手 · 远程单体' if archer else '术师 · 远程范围'
    lines=['武器：2 伤害 · 射程 3','普通攻击：消耗 2 AP','每轮一次，不用打牌','战技：架弩 · 消耗 1 AP'] if archer else ['武器：1 伤害 · 射程 2','命中目标与相邻敌人','战技：星火爆裂 · 消耗 1 AP','中心与四邻敌人各 2 伤']
    n=[rect('左侧角色详情',25,530,300,425,PAPER,old.HAIR),text('角色定位',title,40,545,275,21)]
    for i,line in enumerate(lines):n.append(text('角色信息 '+str(i),line,40,583+i*30,277,16))
    n.append(text('技能图示标题','战技示意 · '+('下一击更远、更重' if archer else '中心与四邻，友军安全'),40,714,275,14))
    if archer:
        n += [text('架弩前后','伤害 2 → 4\n射程 3 → 4\n强化下一次武器攻击',45,755,270,19)]
        return n
    for r in range(3):
        for c in range(3):
            hit=r==1 or c==1
            n.append(rect('技能图示格',48+c*42,750+r*42,38,38,'#F4D9A2' if hit else '#E0E7D7',r=3))
            if hit:n.append(text('示例伤害','+2' if archer else '2',56+c*42,756+r*42,30,18))
    n += [text('图示说明','示意图\n实际范围\n以战场为准',195,760,120,16,'#64746A')]
    return n

def base(stage,selected=None,details=True,discard=False):
    # stage 0 opening, 1 E6, 2 E4, 3 burst, 4 basic, 5 shield, 6 round2.
    state={}
    if stage>=1:state['术师']='E6'
    if stage>=2:state['术师']='E4'
    if stage>=3:state.update(巡兵HP='2/4 HP',弩手HP='1/3 HP',护卫HP='3/5 HP')
    if stage>=6:state.update(术师HP='4/6 HP',弩手='D4',护卫='E3')
    cards=START if stage<2 else MOVED if stage<3 else BURST if stage<5 else KEPT
    if stage>=6:cards=['攻击','突击','猛攻','护盾','机动']
    ap=[6,6,5,4,2,1,6][stage]
    n=old.roster(selected)+old.board(state,dead=['巡兵'] if stage>=4 else [])
    if stage>=6:
        for row in n:
            if row.get('name')=='小队成员 术师':
                for child in row.get('children',[]):
                    if child.get('name')=='生命 术师':child['characters']='4/6 HP'
    if details:n+=role_info(selected=='射手')
    n+=old.hand(cards,discard=discard)
    # The old helper has no Heavy template; add it explicitly for round2.
    if stage>=6:
        n += [rect('新增猛攻牌',808,867,208,158,PAPER,old.HAIR),text('猛攻牌名','猛攻',826,889,160,22),text('猛攻费用','消耗 2 AP',826,929,160,17),text('猛攻效果','主目标额外 2 伤',826,965,170,16)]
    n += [rect('共享AP常驻',356,775,565,58,PAPER,old.HAIR),text('共享AP文案',f'全队共享 AP    {ap} / 6',375,789,500,25),rect('回合按钮',1546,739,343,61,GREEN,r=12),text('回合按钮文案','不弃牌，结束回合' if discard else '结束回合',1565,756,310,23,PAPER)]
    n += [text('关卡名','晴岚炮台',25,27,270,26),text('胜利目标','击毁炮座 或 击败三名敌兵',25,68,305,17)]
    if stage==5:n.append(text('术师护盾标记','盾 3',970,505,110,18,GREEN))
    return n

def build(key,title,body,chapter,stage,focus,selected=None,details=True,button=None,note_xy=(1302,430),extra=None,event='',resources='',discard=False,read_holes=None):
    n=base(stage,selected,details,discard)
    if extra:n+=extra
    holes=list(focus)+list(read_holes or [])+[SKIP]
    n+=mask(holes)
    for i,box in enumerate(focus):
        n.append(border('当前唯一操作对象 '+str(i+1),box))
        x,y,w,h=box;n+= [rect('目标序号',x-4,y-17,36,32,GOLD,r=12),text('操作顺序',str(i+1),x+6,y-14,24,20,PAPER)]
    n+=note(title,body,*note_xy,button)
    if key.startswith(('05-','08-')):
        is_move=key.startswith('05-')
        n += [rect('跟手效果小卡 · 不是第二张牌',1102,622,174,134,PAPER,GOLD,10,2),text('跟手预览标题','机动' if is_move else '星火爆裂',1113,633,158,22),text('跟手预览费用','预计消耗 1 AP',1113,668,158,17),text('跟手预览内容','移动最多 2 格\n松手选定术师' if is_move else '射程 2 · 十字范围\n松手选定术师',1113,699,158,16)]
        if not is_move:
            for c in ['C4','D3','D4','E2','E3','E5','E6','F5','G4']:
                n.append(border('战技射程示意 '+c,ground(c),'#78AC8C',2))
        px,py=(952,647) if is_move else (952,495)
        n.append({'type':'SVG','name':'按住中的指针示意','x':px,'y':py,'width':27,'height':38,'svg':'<svg xmlns="http://www.w3.org/2000/svg" width="27" height="38"><path d="M3 2 L3 29 L10 23 L17 36 L23 32 L16 21 L25 20 Z" fill="white" stroke="#183A32" stroke-width="2"/></svg>'})
    chapters=['认识队员','移动与出牌','普通攻击','回合交接']
    n += [rect('轻量章节导航',360,25,920,64,PAPER,r=12)]
    for i,c in enumerate(chapters):n.append(text('章节 '+c,('● ' if i==chapter else '○ ')+c,380+225*i,44,220,19,GREEN if i==chapter else '#91998C'))
    n += [rect('教学退出入口',*SKIP,PAPER,old.HAIR,8),text('跳过教学','跳过教学',42,1018,200,20),text('设计状态','流程审核示意 · 未实现',1510,34,380,18)]
    # Review-only contract outside the game viewport; never shown in Unity.
    n += [rect('审核注记底',0,1080,1920,120,'#EAE5D8',r=0),text('审核状态',key+'  |  '+resources,25,1095,1860,20),text('推进合同','推进：'+event,25,1133,1860,19)]
    return frame(key+' '+title,0,0,1920,1200,n,PAPER)

E6=ground('E6');E4=ground('E4')
def tag(label,box):
    x,y,w,h=box;return [rect('动作标签',x,y-45,max(w,140),36,GREEN,r=8),text('动作标签文案',label,x+9,y-40,max(w,140)-12,19,PAPER)]

SPECS=[
dict(key='01-A1-点击术师',title='点击术师，看看她能做什么',body='点左侧框中的头像。\n角色信息会在同一侧展开。',chapter=0,stage=0,focus=[ROSTER_M],details=False,note_xy=(365,398),event='指定头像点击成功，术师详情真实展开。',resources='6 AP · 5 张牌'),
dict(key='02-A2-认识信息',title='生命、射程和战技都在这里',body='左侧有技能图示。全队共用 6 AP。\n先试一次不花 AP 的移动。',chapter=0,stage=0,focus=[(20,525,310,434),AP],selected='术师',button='试试免费短步',note_xy=(365,480),event='点击阅读按钮；不产生游戏行动。',resources='6 AP · 5 张牌'),
dict(key='03-B1-免费短步',title='点这个足迹，免费走一格',body='每名队员每轮都有一次免费短步。\n点地面的标记，不用拖牌。',chapter=1,stage=0,focus=[E6],selected='术师',extra=tag('足迹 · 免费',E6),event='mage 短步至 E6 成功，移动演出结束。',resources='仍为 6 AP · 5 张牌',read_holes=[AP]),
dict(key='04-B2-拿起机动',title='按住「机动」，拖到术师身上',body='从框中的牌开始拖。\n拖到术师身上时，会出现移动范围。',chapter=1,stage=1,focus=[cr(1),POS_E6],selected='术师',extra=tag('按住拖动',cr(1))+tag('拖到这里',POS_E6),event='拖入 mage 只进入预览子状态；尚不结算。',resources='6 AP · 5 张牌'),
dict(key='05-B2P-机动预览',title='现在松手，选定术师',body='亮起的格子是这张牌能到的位置。\n松手后，再点一个落点。',chapter=1,stage=1,focus=[POS_E6],selected='术师',extra=old.board({'术师':'E6'},highlights=['E5','E4','F6','G6','D6','F5'])+tag('正在预览 · 未扣 AP',POS_E6),event='指定牌松手且 mage 锁定成功 → B3；移开或取消 → B2。',resources='预览费用 1 AP · 实际仍为 6 AP',read_holes=[(738,446,516,319),AP]),
dict(key='06-B3-机动落点',title='点亮起的空格，走到这里',body='术师会沿路径走两格。\n这次移动消耗 1 AP。',chapter=1,stage=1,focus=[E4],selected='术师',extra=tag('点击落点',E4),event='card-7 / mage / E4 成功提交并完成演出 → B4。',resources='完成后 5 AP · 4 张牌',read_holes=[POS_E6,AP]),
dict(key='07-B4-拿起战技',title='把「战技」拖给术师',body='她的战技是范围爆裂。\n先拖给队员，再选要攻击的敌人。',chapter=1,stage=2,focus=[cr(3),POS_E4],selected='术师',extra=tag('按住拖动',cr(3))+tag('术师',POS_E4),event='战技拖入 mage 进入预览子状态，尚不扣费。',resources='5 AP · 4 张牌'),
dict(key='08-B4P-战技预览',title='松手选定术师，再选敌人',body='当前可在两格内选敌人。\n星火爆裂会伤到目标和四邻的敌人。',chapter=1,stage=2,focus=[POS_E4],selected='术师',extra=tag('星火爆裂 · 消耗 1 AP',POS_E4),event='card-12 松手且 mage 锁定成功 → B5；悬停本身不推进。',resources='预览状态 · 仍为 5 AP',read_holes=[(738,370,390,242),AP]),
dict(key='09-B5-选择弩手',title='点框中的弩手，一次命中三敌',body='十字内的敌人各受 2 点伤害。\n术师在范围内，也不会伤到自己。',chapter=1,stage=2,focus=[TARGET_D4],selected='术师',extra=old.board({'术师':'E4'},affected=['D4','C4','D3','E4']),event='card-12 / mage / crossbow 成功，伤害与演出结束 → C1。',resources='完成后 4 AP · 3 张牌',read_holes=[(610,370,400,245),AP]),
dict(key='10-C1-选择射手',title='点击射手，找她的普通攻击',body='不出牌时，也能消耗 AP 普攻。\n先选中射手。',chapter=2,stage=3,focus=[ROSTER_A],details=False,note_xy=(365,306),event='射手选中且合法普攻目标生成 → C2。',resources='4 AP · 3 张牌'),
dict(key='11-C2-普通攻击',title='点击剑标，不用出牌也能攻击',body='普通攻击消耗 2 AP，每人每轮一次。\n这个巡兵只剩 2 点生命。',chapter=2,stage=3,focus=[TARGET_C4],selected='射手',extra=tag('剑标 · 普攻',TARGET_C4),event='archer 对 patrol 普攻成功且演出结束 → D1。',resources='完成后 2 AP · 手牌仍是 3 张',read_holes=[AP,(350,860,680,170)]),
dict(key='12-D1-护盾保护',title='把「护盾」拖给术师',body='她站在前面，需要挡住敌人的反击。\n松手就获得 3 点护盾，消耗 1 AP。',chapter=3,stage=4,focus=[cr(2),POS_E4],selected='术师',extra=tag('按住拖动',cr(2))+tag('保护术师',POS_E4),event='card-15 交给 mage 成功结算 → D2；这张牌不再选目标。',resources='完成后 1 AP · 2 张牌 · 术师 3 盾'),
dict(key='13-D2-结束按钮',title='点击「结束回合」，看看怎样留牌',body='行动已经完成，下一步决定留哪些牌。\n未用完的 AP 不会带到下一轮。',chapter=3,stage=5,focus=[END],selected='术师',note_xy=(1302,490),event='弃牌选择界面实际打开 → D3；尚不执行敌方行动。',resources='1 AP · 2 张牌'),
dict(key='14-D3-留牌确认',title='想留下的牌不用点',body='点牌会选中弃掉，再点可取消。\n当前弃 0 张，保留 2 张，下轮补 3 张。',chapter=3,stage=5,focus=[(350,860,452,170),END],selected='术师',discard=True,note_xy=(1302,480),event='确认实际选弃后进入敌方回合；数量与按钮实时更新。',resources='零弃牌示例 · 选弃可撤销 · 允许返回'),
dict(key='15-D4-观察敌方',title='敌人行动中，注意炮击落点',body='红框里的格子将受到炮击。\n等待敌方行动完成，自动继续。',chapter=3,stage=5,focus=[(482,596,385,96)],selected='术师',extra=tag('炮击预告', (482,596,385,96)),event='示意敌方阶段开始；逐个跟随真实行动者，结束演出后 → D5。',resources='等待实际敌方阶段结束；不靠计时推进'),
dict(key='16-D5-自由战斗',title='AP 恢复了，留下的牌还在',body='现在由你决定谁来移动、出牌与攻击。\n击败剩下的敌兵，或摧毁炮座。',chapter=3,stage=6,focus=[AP,(350,860,1130,170)],selected='术师',button='开始自由战斗',note_xy=(1302,430),event='点击按钮，关闭聚焦层与所有教学限制，继续当前第 2 轮。',resources='6 AP · 5 张牌 · 术师 4 HP；敌人位置待逐步实机校准'),
dict(key='17-错误投放',title='先把这张牌拖给框中的术师',body='拖错不会消耗 AP，也不会丢掉卡牌。\n松手后可重新拖动。',chapter=1,stage=1,focus=[cr(1),POS_E6],selected='术师',event='错误执行者被拒绝，停在 B2；绝不先执行再切自由提示。',resources='错误状态样例 · 仍为 6 AP · 5 张牌'),
dict(key='18-退出确认',title='退出引导，继续当前战局？',body='已经完成的行动会保留。\n退出后，可以自由行动。',chapter=1,stage=1,focus=[],selected='术师',button='退出引导，继续本局',note_xy=(667,400),extra=[rect('确认取消按钮',690,680,540,50,PAPER,GOLD),text('继续教学按钮','继续教学',720,692,470,22)],event='确认退出清除未结算预览并解锁；取消则回原步骤。',resources='退出确认样例 · 不重置 AP 或棋盘'),
]

def real_review(burst=False):
    # Same battle state as each timestamped GameView; only editable review UI is overlaid.
    image_hash='76fc68684c79c3790cd0a6fd939f9728fff9f8e3' if burst else '130afcb5274853805b88259caf9ccbc77f47d63a'
    n=[{'type':'RECTANGLE','name':'锁定实机底图 '+('2026-11-13 待选D4' if burst else '2026-11-09 初始站位'),'x':0,'y':0,'width':1920,'height':1080,'locked':True,'fills':[{'type':'IMAGE','imageHash':image_hash,'scaleMode':'FILL'}]}]
    n += [rect('以当前双胜利文案校准旧截图',25,56,300,35,PAPER,r=0),text('双胜利目标','击毁炮座 或 击败三名敌兵',40,65,280,17)]
    n += [rect('覆盖旧顶部长指令',586,24,850,76,PAPER,r=0),text('轻量章节状态','移动与出牌 · 选择敌人' if burst else '认识队员 · 查看角色信息',609,46,798,25)]
    if not burst:
        n += [rect('依当前牌序更正第5张卡',1277,839,214,183,PAPER,old.HAIR,0),text('战技名称','战技',1294,858,165,27),text('战技费用','消耗 1 AP',1294,900,166,18),text('战技描述','按执行者施展\n专属战技',1294,937,170,20)]
    else:
        n += [rect('补全现行战技卡正文',1048,861,225,95,PAPER,r=0),text('战技三角色','先锋：冲撞得盾\n射手：架弩\n术师：星火爆裂',1062,869,205,20)]
    target=(914,351,90,151) if burst else (20,306,300,92)
    keep=[(834,310,254,240),(355,774,641,51)] if burst else []
    actual_skip=(22,948,228,52)
    n+=mask([target,actual_skip]+keep)
    n.append(border('唯一可操作目标 · 真实截图位置',target))
    x,y,w,h=target
    n += [rect('点击目标标签',x,y-42,228,36,GOLD,r=9),text('目标标签','点击：D4 弩手' if burst else '点击：术师头像',x+10,y-37,215,19,INK)]
    n += note('点框中的弩手，引爆星火' if burst else '点击术师，看看她能做什么','目标和四邻的敌人各受 2 点伤害。\n点中并完成攻击后，教程才会继续。' if burst else '点头像后，左侧会展开她的详细信息。\n先完成框中的操作，教程才会继续。',1160 if burst else 350,325 if burst else 302)
    n += [text('审核稿标识','待审核 · 聚焦层尚未实现',1490,40,400,21,PAPER),rect('审核信息',0,1080,1920,120,'#EAE5D8',r=0),text('底图来源','真实 GameView '+('2026-11-13：术师E4，5 AP，战技待选D4。' if burst else '2026-11-09：初始站位，6 AP。牌面与目标文案按当前代码覆为可编辑层。'),26,1096,1850,20),text('实现约束','金框、提示、蒙层均为独立可编辑图层。操作允许区域随运行时目标更新；禁止其他行动，允许明确跳过。',26,1135,1850,19)]
    return frame('实机位置样例 '+('爆裂' if burst else '选人'),0,0,1920,1200,n,PAPER)

def main():
    for s in SPECS:
        n=old.importer_order(build(**s))
        (OUT/(s['key']+'.json')).write_text(json.dumps(n,ensure_ascii=False),encoding='utf-8')
    (OUT/'flow.json').write_text(json.dumps([{k:v for k,v in s.items() if k not in ('extra',)} for s in SPECS],ensure_ascii=False,indent=2),encoding='utf-8')
    for burst,name in [(False,'00A-实机聚焦选人'),(True,'00B-实机聚焦爆裂')]:
        (OUT/(name+'.json')).write_text(json.dumps(old.importer_order(real_review(burst)),ensure_ascii=False),encoding='utf-8')
    print('Generated',len(SPECS),'editable review frames')
if __name__=='__main__':main()
