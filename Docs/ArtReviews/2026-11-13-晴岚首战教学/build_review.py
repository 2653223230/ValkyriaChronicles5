"""Build seven editable native layer trees for the 2026-11-13 Qinglan tutorial review."""
import json
from pathlib import Path

OUT = Path(__file__).resolve().parent
AVATAR_HASH = '91d3a53ea8965d6c0249fec4904396a036f88486'
IVORY='#F4F0E4'; PAPER='#FBF8EF'; INK='#183A32'; GREEN='#356A54'; MINT='#A8C9AC'; GOLD='#BE9C55'; MUTED='#64746A'; HAIR='#C9C1AA'; RED='#B75D49'; BLUE='#627A9A'

def paint(c):
    c=c.lstrip('#'); return {'type':'SOLID','color':{k:int(c[i:i+2],16)/255 for k,i in [('r',0),('g',2),('b',4)]}}
def rect(name,x,y,w,h,fill=PAPER,stroke=None,r=10,sw=1):
    n={'type':'RECTANGLE','name':name,'x':x,'y':y,'width':w,'height':h,'fills':[paint(fill)],'cornerRadius':r}
    if stroke:n.update(strokes=[paint(stroke)],strokeWeight=sw)
    return n
def text(name,value,x,y,w=260,size=18,color=INK):
    return {'type':'TEXT','name':name,'characters':value,'x':x,'y':y,'width':w,'height':1,'fontFamily':'Microsoft YaHei, Noto Sans CJK SC, Inter','fontSize':size,'lineHeight':{'unit':'PERCENT','value':132},'fills':[paint(color)]}
def frame(name,x,y,w,h,children,fill=None,stroke=None,r=12):
    n={'type':'FRAME','name':name,'x':x,'y':y,'width':w,'height':h,'children':children,'fills':[] if fill is None else [paint(fill)],'clipsContent':True}
    if stroke:n.update(strokes=[paint(stroke)],strokeWeight=1)
    return n
def line(name,x1,y1,x2,y2,color=HAIR,width=1):
    # Editable vector line in the imported SVG node.
    return {'type':'SVG','name':name,'x':min(x1,x2),'y':min(y1,y2),'width':max(1,abs(x2-x1)),'height':max(1,abs(y2-y1)),'svg':f'<svg xmlns="http://www.w3.org/2000/svg" width="{max(1,abs(x2-x1))}" height="{max(1,abs(y2-y1))}"><path d="M0 0 L{max(1,abs(x2-x1))} {max(1,abs(y2-y1))}" stroke="{color}" stroke-width="{width}"/></svg>'}
def icon_circle(name,glyph,x,y,color=GREEN,size=40):
    return frame(name,x,y,size,size,[rect('徽章底',0,0,size,size,color,r=size//2),text('徽章字',glyph,0,5,size,22,IVORY)],None)
def badge(name,label,x,y,w=160,accent=GOLD):
    return frame(name,x,y,w,31,[rect('标记底',0,0,w,31,PAPER,accent,15,2),text('标记文字',label,9,6,w-18,15,INK)],None)

# Fixed, original 7x7 overhead training map. Coordinates match the rules grid.
COLS='ABCDEFG'; CELL_W=126; CELL_H=76; BOARD_X=356; BOARD_Y=227
UNITS={
 '先锋':('D7','先',GREEN,'9/9 HP'), '射手':('C7','射',BLUE,'6/6 HP'), '术师':('E7','术','#735B91','6/6 HP'),
 '巡兵':('C4','巡',RED,'4/4 HP'), '弩手':('D4','弩',RED,'3/3 HP'), '护卫':('D3','卫',RED,'5/5 HP'), '炮座':('D2','炮','#7A7467','10/10 HP')}

def xy(coord):
    c=COLS.index(coord[0]); r=int(coord[1])-1
    return BOARD_X+c*CELL_W+CELL_W//2, BOARD_Y+r*CELL_H+CELL_H//2

def board(state=None, highlights=(), path=(), affected=None, dead=(), sword=False):
    state=state or {}
    nodes=[frame('原创俯视战术棋盘 · 教学审核示意',BOARD_X-18,BOARD_Y-19,CELL_W*7+36,CELL_H*7+40,[],PAPER,HAIR,16)]
    for ci,c in enumerate(COLS):
        nodes.append(text('列坐标 '+c,c,BOARD_X+ci*CELL_W+48,BOARD_Y-24,30,14,MUTED))
    for ri in range(7): nodes.append(text('行坐标 '+str(ri+1),str(ri+1),BOARD_X-25,BOARD_Y+ri*CELL_H+27,18,14,MUTED))
    for ri in range(7):
        for ci,c in enumerate(COLS):
            coord=f'{c}{ri+1}'; col='#F6F2E8' if (ri+ci)%2==0 else '#EEE9DC'
            if coord in ('B3','B4','D5','F3','F4'): col='#AEB7AE'
            if coord in highlights: col='#E4EED7'
            if affected and coord in affected: col='#F5E4C6'
            nodes.append(rect('格 '+coord,BOARD_X+ci*CELL_W,BOARD_Y+ri*CELL_H,CELL_W-2,CELL_H-2,col,RED if coord in ('B6','C6','D6') else HAIR,4,3 if coord in ('B6','C6','D6') else 1))
            nodes.append(text('格坐标 '+coord,coord,BOARD_X+ci*CELL_W+6,BOARD_Y+ri*CELL_H+5,34,10,'#8A9189'))
    # Draw route behind pieces, using simple circles and segments, never directional arrows.
    if len(path)>1:
        for a,b in zip(path,path[1:]):
            x1,y1=xy(a);x2,y2=xy(b)
            w=max(1,abs(x2-x1)); h=max(1,abs(y2-y1))
            sx=0 if x1<=x2 else w; sy=0 if y1<=y2 else h
            ex=w if x1<=x2 else 0; ey=h if y1<=y2 else 0
            nodes.append({'type':'SVG','name':'移动路线 '+a+' 到 '+b,'x':min(x1,x2),'y':min(y1,y2),'width':w,'height':h,'svg':f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}"><path d="M{sx} {sy} L{ex} {ey}" stroke="{GOLD}" stroke-width="5" stroke-linecap="round"/></svg>'})
        for coord in path:nodes.append(badge('路径格 '+coord,coord,*xy(coord),56,GOLD))
    for name,(start,glyph,color,hp) in UNITS.items():
        if name in dead: continue
        coord=state.get(name,start); x,y=xy(coord)
        if affected and coord in affected:
            nodes.append(rect('受击格 '+coord,BOARD_X+(COLS.index(coord[0]))*CELL_W+2,BOARD_Y+(int(coord[1])-1)*CELL_H+2,CELL_W-6,CELL_H-6,'#F4DBB1',GOLD,7,3))
        nodes.append(icon_circle('棋子 '+name+' '+coord,glyph,x-22,y-23,color,44))
        hpval=state.get(name+'HP',hp)
        nodes.append(text('棋子名坐标 '+name,name+' · '+coord,x-55,y+22,110,13,INK))
        nodes.append(text('棋子生命 '+name,hpval,x+9,y+5,49,12,RED if name in ('巡兵','弩手','护卫') else GREEN))
    if 'E6' in highlights and not path:
        x,y=xy('E6');nodes.append(badge('免费足迹','足迹 · 免费',x-45,y-12,92,GREEN))
    if sword:
        x,y=xy('C4');nodes.append(frame('C4 普攻目标标记',x+23,y-39,30,30,[rect('剑标底',0,0,30,30,GOLD,r=15),text('剑标文字','剑',2,5,26,16,IVORY)],None))
    if affected:
        for coord in ('D4','C4','D3'):
            x,y=xy(coord);nodes.append(badge('爆裂伤害 '+coord,'2 伤',x+27,y-43,62,RED))
    return nodes

ROLE_DATA=[('先锋','前排肉盾 · 突进近战','邻格单体 · 3伤','冲撞：推敌1格，自得至少2盾','9/9 HP'),('射手','远程 · 单体射击','射程3 · 单体2伤','架弩：下一击+2伤、射程+1','6/6 HP'),('术师','远程 · 范围伤害','射程2 · 主目标外邻敌1伤','星火爆裂：中心与四邻敌人各受2伤','6/6 HP')]
def roster(selected=None, expanded=False):
    n=[text('小队标题','小队 · 点击头像查看详情',25,176,300,17,INK)]
    for i,(name,role,weap,skill,hp) in enumerate(ROLE_DATA):
        y=207+i*105;focus=name==selected
        children=[rect('队员底',0,0,300,91,'#F7F3E9',GOLD if focus else HAIR,12,2 if focus else 1),
          frame('头像图层',8,11,58,62,[{'type':'RECTANGLE','name':'头像哈希裁切 '+name,'x':-35,'y':-152-i*92,'width':1920,'height':1080,'fills':[{'type':'IMAGE','scaleMode':'FILL','imageHash':AVATAR_HASH}],'locked':True}],None),
          icon_circle('角色名徽章 '+name,name[0],73,10,GREEN if name=='先锋' else BLUE if name=='射手' else '#735B91',34),
          text('角色名 '+name,name,115,8,80,18,INK),text('定位 '+name,role,73,42,212,14,MUTED),text('生命 '+name,hp,218,10,75,13,GREEN),text('武器 '+name,weap,73,63,220,13,INK)]
        if expanded and focus: children.append(text('战技 '+name,'战技 · '+skill,73,77,220,12,BLUE))
        n.append(frame('小队成员 '+name,25,y,300,91,children,None))
    return n

def header(title,detail,step):
    return [frame('教学指引',351,20,1040,91,[rect('指引底',0,0,1040,91,PAPER,GOLD,14),text('教学步数',f'{step} / 9',18,10,100,14,GOLD),text('教学标题',title,18,30,1000,23,INK),text('教学说明',detail,18,62,1000,15,MUTED)],None),badge('审核示意标记','教学审核示意 · 原创坐标棋盘',1450,29,430,BLUE)]

CARDS=[('攻击','攻击','按执行者的武器攻击','1 AP',GREEN),('机动','机动','移动 1–2 格','1 AP',BLUE),('突击','突击','移动后武器攻击','2 AP',GOLD),('护盾','盾','获得 3 点护盾','1 AP',GREEN),('战技','技','按角色执行专属效果','1 AP','#735B91')]
def hand(cards,focus=None,used=(),discard=False,draw=False):
    n=[text('手牌标题','手牌 · '+('保留 / 补牌' if discard else f'{len(cards)} 张'),356,838,220,15,MUTED)]
    for i,name in enumerate(cards):
        data=next((c for c in CARDS if c[0]==name),None)
        if not data:continue
        _,glyph,effect,cost,color=data;x=356+i*226;y=867
        stroke=GOLD if name==focus else HAIR
        ch=[rect('卡面',0,0,208,158,PAPER,stroke,14,2 if name==focus else 1),icon_circle('卡牌徽章 '+name,glyph,12,12,color,38),text('卡名 '+name,name,58,18,105,19,INK),badge('费用 '+name,cost,139,13,58,color),text('卡牌说明 '+name,effect,14,76,180,13,MUTED)]
        if name in used:ch.append(text('卡牌已使用 '+name,'已使用',14,132,90,13,RED))
        if discard:ch.append(text('保留状态 '+name,'保留',14,132,90,13,GREEN))
        n.append(frame('手牌 '+name,x,y,208,158,ch,None))
    if draw:
        for j in range(2):
            x=25+(len(cards)+j)*226
            n.append(frame('新抽牌 '+str(j+1),x,867,208,158,[rect('牌背',0,0,208,158,'#EFE7D4',GOLD,14),text('补牌标记','新抽牌',20,42,170,18,INK),text('补牌数量','抽取 '+str(j+1),20,77,170,14,MUTED)],None))
    return n

def footer(ap,detail,button=None):
    n=[frame('共享AP状态',356,775,565,58,[rect('AP底',0,0,568,62,PAPER,HAIR,14),text('AP状态','全队共享 AP',17,9,172,15,MUTED),text('AP数值',ap,190,7,140,27,GREEN),text('回合状态',detail,335,12,216,15,INK)],None)]
    if button:n.append(frame('操作按钮',1546,739,343,61,[rect('按钮底',0,0,343,61,GREEN,GOLD,14,2),text('按钮文字',button,18,18,307,20,IVORY)],None))
    n.append(frame('跳过教学',25,759,300,54,[rect('跳过底',0,0,300,54,PAPER,HAIR,12),text('跳过文案','跳过教学',18,16,255,17,MUTED)],None))
    return n

def detail_panel(title,lines,focus=False):
    ch=[rect('说明底',0,0,568,260,PAPER,GOLD if focus else HAIR,14,2 if focus else 1),text('说明标题',title,17,14,525,18,INK)]
    for i,line in enumerate(lines):ch.append(text('说明行 '+str(i+1),line,17,53+i*38,530,15,MUTED if i else INK))
    return frame('战场说明卡',1321,231,568,260,ch,None)

def build(step,title,detail,units=None,highlights=(),path=(),affected=None,dead=(),roles=None,cards=None,focus=None,used=(),ap='6 / 6',phase='玩家回合',panel=None,button=None,draw=False,discard=False,sword=False):
    nodes=header(title,detail,step)+roster(roles,expanded=step==1)+board(units,highlights,path,affected,dead,sword)+footer(ap,phase,button)
    if panel:nodes.append(detail_panel(panel[0],panel[1],panel[2] if len(panel)>2 else False))
    if cards is not None:nodes.extend(hand(cards,focus,used,discard,draw))
    return frame(f'{step:02d} {title}',0,0,1920,1080,nodes,PAPER)

START=['攻击','机动','突击','护盾','战技']
MOVED=['攻击','突击','护盾','战技']
KEPT=['攻击','突击','护盾']
E6={'术师':'E6'}
E4={'术师':'E4'}
BURST=dict(E4,巡兵HP='2/4 HP',弩手HP='1/3 HP',护卫HP='3/5 HP')
FIRE=['D4','C4','D3','E4','D5']
SPECS=[
 ('01-认识战场.json',lambda:build(1,'点左侧术师头像','头像旁是生命。点一下，查看她的武器和战技。',cards=START,roles=None,panel=('晴岚炮台',['击毁 D2 炮座，或击败三名敌兵。','红框是下次炮击落点，灰格是障碍。','手牌上方的 6 AP 由全队共享。']))),
 ('02-免费短步.json',lambda:build(2,'点 E6 的足迹，免费走一格','每人每轮可免费短步一次。观察术师位置和共享 AP。',roles='术师',cards=START,highlights=['E6'],panel=('术师 · 远程范围伤害',['生命 6 / 6，武器射程 2。','普通武器命中主目标及相邻敌人，各 1 伤。','战技 · 星火爆裂：中心和四邻敌人各 2 伤。','本次短步：E7 到 E6，AP 仍为 6。']))),
 ('03-拖机动给术师.json',lambda:build(3,'把「机动」拖到术师身上','拖入查看效果，松手选定队员。此时仍有 6 AP。',units=E6,roles='术师',cards=START,focus='机动',panel=('机动 · 1 AP',['短步完成，术师站在 E6。','把手牌交给要执行它的队员。','移到空白处松手可取消。']))),
 ('04-选机动落点.json',lambda:build(4,'点 E4，让术师走到弩手身旁','按 E6、E5、E4 的顺序移动。点中落点才消耗机动牌和 1 AP。',units=E6,roles='术师',cards=START,focus='机动',path=['E6','E5','E4'],highlights=['E4'],ap='6 → 5',panel=('落点预览',['E4 空格可达。','从这里可以打到站在一起的三名敌兵。']))),
 ('05-拖战技看差别.json',lambda:build(5,'把「战技」拖给术师','同一张战技可以交给不同队员。拖到术师身上松手，准备范围攻击。',units=E4,cards=MOVED,roles='术师',focus='战技',ap='5 / 6',panel=('同一张战技，三种用法',['先锋 · 冲撞：推开邻敌，自己得盾。','射手 · 架弩：下一击更远、更重。','术师 · 星火爆裂：一次打到一群敌人。']))),
 ('06-点弩手引爆.json',lambda:build(6,'点 D4 弩手，让星火命中三敌','橙格是爆裂范围。术师自己在范围内也不会受伤。',units=E4,roles='术师',cards=MOVED,focus='战技',affected=FIRE,ap='5 → 4',panel=('命中预览 · 消耗 1 AP',['D4 弩手　3 → 1','C4 巡兵　4 → 2','D3 护卫　5 → 3','中心和四邻各打一次；友军安全。']))),
 ('07-选射手看普攻.json',lambda:build(7,'点射手头像，找他的普攻目标','巡兵只剩 2 生命，射手的普通攻击刚好能击败它。',units=BURST,cards=KEPT,roles=None,ap='4 / 6',panel=('射手 · 远程单体',['武器射程 3，单体伤害 2。','点己方棋子或头像可查看详情和基础行动。']))),
 ('08-点剑标普攻.json',lambda:build(8,'点 C4 巡兵脚下的剑标','这是普通攻击：消耗 2 AP，不用出牌，每名队员每轮一次。',units=BURST,cards=KEPT,roles='射手',sword=True,ap='4 → 2',panel=('普通攻击预览',['射手 C7 命中巡兵 C4，生命 2 → 0。','手中三张牌都可以留下。']))),
 ('09-结束与留牌.json',lambda:build(9,'想留的牌不用点，确认结束回合','还剩 2 AP，可以继续行动。点结束回合后，只有点选上浮的牌会弃掉。',units=BURST,dead=['巡兵'],cards=KEPT,roles=None,ap='2 / 6',phase='弃牌阶段',button='不弃牌，结束回合',discard=True,panel=('回合交接',['当前留 3 张，弃 0 张。','敌方行动后若战斗继续，全队恢复 6 AP。','按本次提案，补入猛攻和护盾，手牌共 5 张。']))),
]

def importer_order(node):
    if 'children' in node:
        for child in node['children']:importer_order(child)
        node['children'].reverse()
    return node

def main():
    for filename,builder in SPECS:
        (OUT/filename).write_text(json.dumps(importer_order(builder()),ensure_ascii=False),encoding='utf-8')
    print(f'Generated {len(SPECS)} editable native layer trees in {OUT}')
if __name__=='__main__':main()
