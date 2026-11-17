import json,copy
from pathlib import Path
OUT=Path('Docs/ArtReviews/2026-11-03-SharedAP-Figma')
HASH='576b9905e40d75fe874c5bf68ea4a016daf69525'
def paint(hex):return {'type':'SOLID','color':{k:int(hex.lstrip('#')[i:i+2],16)/255 for k,i in [('r',0),('g',2),('b',4)]}}
def rect(n,x,y,w,h,c='#15343B',stroke=None):
 d=dict(type='RECTANGLE',name=n,x=x,y=y,width=w,height=h,fills=[paint(c)],cornerRadius=8)
 if stroke:d.update(strokes=[paint(stroke)],strokeWeight=2)
 return d
def text(n,s,x,y,w=300,size=22,c='#E9F0E9'):
 return dict(type='TEXT',name=n,characters=s,x=x,y=y,width=w,height=1,fontFamily='Microsoft YaHei, Noto Sans CJK SC, Inter',fontSize=size,lineHeight={'unit':'PERCENT','value':138},fills=[paint(c)])
def frame(n,x,y,w,h,children,c=None):return dict(type='FRAME',name=n,x=x,y=y,width=w,height=h,children=children,fills=[] if c is None else [paint(c)],clipsContent=True)
def image(n,x,y,w=1920,h=1080):return dict(type='RECTANGLE',name=n,x=x,y=y,width=w,height=h,fills=[dict(type='IMAGE',scaleMode='FILL',imageHash=HASH)],locked=True)
def pill(n,s,x,y,w,c='#244A53',fg='#D8ECDD'):return [rect(n+'底',x,y,w,38,c),text(n,s,x+12,y+4,w-24,19,fg)]
def svg(n,s,x,y,w,h):return dict(type='SVG',name=n,x=x,y=y,width=w,height=h,svg=s)
def ring(n,x,y,w,h,color='#63E9CF'):
 return svg(n,f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}"><ellipse cx="{w/2}" cy="{h/2}" rx="{w/2-3}" ry="{h/2-3}" fill="{color}" fill-opacity=".12" stroke="{color}" stroke-width="4"/></svg>',x,y,w,h)
def arrow(n,x,y,w,h):return svg(n,f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}"><path d="M 4 {h-4} L {w-12} 12 L {w-31} 14 M {w-12} 12 L {w-16} 33" fill="none" stroke="#63E9CF" stroke-width="5" stroke-linecap="round"/></svg>',x,y,w,h)
names=['先锋','射手','术师'];weapons=['3伤 · 邻格单体','2伤 · 射程3 · 单体','1伤 · 射程2 · 范围']
cards=[('攻击',1,'按接牌队员的武器\n攻击一次。'),('机动',1,'移动1–2格。'),('突击',2,'移动1–2格，\n再武器攻击。'),('护盾',1,'护盾补到3。\n下轮开始时清除。'),('治疗',1,'接牌队员恢复3HP，\n不超过生命上限。')]

def build(mode,role=0):
 discard=mode.startswith('discard'); selected=[] if mode=='discard0' else [1,4] if mode=='discard2' else []
 L=[image('晴岚实际战场背景 / 锁定参考',0,0),rect('顶部常驻资源区',0,0,1920,100,'#0B2027'),rect('操作指引横栏',350,100,1210,60,'#123039'),rect('小队与敌方威胁',0,100,350,980,'#0D252D'),rect('角色图解侧栏',1560,100,360,980,'#0D252D'),rect('手牌操作区',350,730,1210,350,'#0B2027')]
 L += [text('关卡标题','晴岚炮台',28,15,310,29,'#EAD39B'),text('胜利目标','目标 / 击毁炮座',28,58,310,18,'#A7C0BD'),text('AP标题','全队共享 AP',620,20,210,23),text('AP数值','6 / 6',1150,16,170,31,'#68D7F6'),text('回合状态','第 1 回合 · '+('弃牌阶段' if discard else '我方行动'),1515,30,385,23,'#EAD39B')]
 for i in range(6):L.append(rect('AP槽 '+str(i+1),810+i*51,25,43,30,'#50CDF4' if not(mode=='drag' and i==5) else '#244A53','#50CDF4' if mode=='drag' and i==5 else None))
 if mode=='drag':L.append(text('AP预计变化','预览  −1 AP → 剩余5',810,64,370,18,'#EAD39B'))
 hints={'unit':'点脚印免费短步；将手牌拖给任意队员，无需先选人。','card':'已选护盾：亮起的队员都能执行。拖给队员即可生效。','drag':'正在交给术师：松手获得3盾；移出或取消不消耗。','charge':'突击 · 先选落点，再选敌人。完成前不消耗AP或手牌。','discard0':'点击要弃掉的牌；不选就全部保留。确认后进入敌方回合。','discard2':'选中牌上浮并标记“待弃”；再次点击取消选择。'}
 L.append(text('当前操作指引',hints[mode],382,113,1130,23))
 L.append(text('小队标题','出战小队',24,120,290,24,'#EAD39B'))
 for i in range(3):
  y=170+i*133;bright=mode in ['card','drag'];border='#63E9CF' if bright else '#EAD39B' if i==role and not discard else '#315660'
  L += [rect('队员行 '+names[i],20,y,310,119,'#173941',border),frame('原立绘 '+names[i],30,y+10,72,99,[image('头像原图',-46,-(222+i*134))]),text('队员名称 '+names[i],names[i],116,y+5,200,25),text('武器 '+names[i],weapons[i],116,y+43,200,18,'#8DDCCB'),text('状态 '+names[i],('可接护盾 · 松手执行' if bright else ('9/9' if i==0 else '6/6')+' HP · 短步可用'),116,y+78,202,17,'#BAEFDA' if bright else '#BACFCC')]
 L += [text('威胁标题','下一轮威胁',24,595,290,23,'#EAD39B'),text('威胁内容','巡兵 → C5\n弩手 → C4，攻击射手2伤\n护卫继续保护炮座',24,636,300,19),rect('炮击警示底',20,750,310,113,'#3C302A','#A96540'),text('炮击警示','炮击  B6 · C6 · D6\n红框内敌我都受2伤',34,766,282,21,'#FFCB9A')]
 button=('弃牌并结束（2）' if selected else '直接结束（不弃牌）') if discard else '结束回合 → 弃牌选择'
 L += [rect('回合按钮',20,925,310,68,'#D6BC7F'),text('回合按钮文字',button,35,944,285,22,'#142F35'),text('回合按钮辅助','确认后敌方行动' if discard else '先选择弃哪些，再执行敌方行动',24,1009,304,17,'#A9BFBB')]
 if discard:
  L += [rect('右侧弃牌说明',1578,122,324,650,'#12333B'),text('弃牌说明标题','整理手牌',1598,150,280,30,'#EAD39B'),text('弃牌统计',f'选择弃掉  {len(selected)} 张\n未选保留  {5-len(selected)} 张\n下轮补牌  {len(selected)} 张',1598,218,280,25),text('不选含义','没有默认弃牌。\n未选中的牌全部保留。\n\n手里已有5张且不弃牌，\n下轮不会抽到新牌。',1598,383,280,22),text('弃牌步骤','① 点牌选择 / 取消\n② 确认弃牌并结束\n③ 敌人行动\n④ 下轮手牌补至5张',1598,570,280,21),rect('返回战斗按钮',1590,825,300,56,'#244A53'),text('返回战斗文字','返回战斗',1665,839,220,23),text('返回说明','返回不弃牌，不消耗资源',1590,900,300,19,'#A9BFBB')]
 else:
  L += [text('角色标题',names[role]+' / 角色信息',1584,126,310,26,'#EAD39B'),text('生命武器',('生命9/9' if role==0 else '生命6/6')+'   护盾0\n'+weapons[role],1584,175,310,21),text('范围标题','武器射程 / 命中示意',1584,256,310,22,'#8DDCCB')]
  radius=[1,3,2][role];n=7 if role==1 else 5;size=29 if n==7 else 40
  for y in range(n):
   for x in range(n):
    d=abs(x-n//2)+abs(y-n//2);L.append(rect('射程示意 '+str(x)+','+str(y),1590+x*size,300+y*size,size-5,size-5,'#EAD39B' if d==0 else '#60D8C1' if d<=radius else '#23434A'))
  if role==2:
   for y in range(3):
    for x in range(3):L.append(rect('溅射示意 '+str(x)+','+str(y),1814+x*25,351+y*25,21,21,'#DFA56E' if abs(x-1)+abs(y-1)<=1 else '#23434A'))
   L.append(text('溅射标注','主目标\n及四邻',1810,440,95,17,'#FFCB9A'))
  L += [text('射程图注','金格：自己  绿格：射程\n示意不含障碍；战场显示合法目标',1584,522,310,17,'#AEC5BE'),text('技能名',['冲撞','架弩','移位'][role],1584,590,310,25,'#EAD39B')]+pill('战技条件','需战技牌 ＋ 1 AP',1584,632,310)
  symbols=[['己','敌','→','敌'],['弩','2伤','→','4伤'],['敌','原位','→','邻格']][role]
  for j,symbol in enumerate(symbols):
   L.append(rect('技能图格'+str(j),1588+j*76,691,66,50,'#244A53' if j!=2 else '#0D252D'))
   L.append(text('技能图文字'+str(j),symbol,1596+j*76,701,60,22,'#EAD39B'))
  details=['推开邻敌1格，自身获得至少2盾。推不开仍得盾；炮座不能移动。','下一击主目标＋2伤、射程＋1。移动解除；最迟下个己方回合结束失效。','将2格内可见敌人移到其四邻空格。不能移炮座，不能穿墙。'][role]
  L += [text('技能说明',details,1584,764,310,20),text('基础状态标题','基础行动',1584,887,310,22,'#EAD39B'),text('基础状态','短步  可用 · 免费 · 四邻1格\n普攻  可用 · 2 AP · 每轮一次\n使用卡牌不占基础行动次数',1584,928,310,19,'#BACFCC')]
 L += [text('手牌标题','选择要弃的牌' if discard else '手牌 5 / 5',378,745,290,23,'#EAD39B'),text('手牌状态',f'待弃{len(selected)}张 / 保留{5-len(selected)}张 / 下轮补{len(selected)}张' if discard else ('点击卡牌找执行者，或直接拖给队员' if mode in ['card','drag'] else names[role]+'可用牌已亮起 · 暗牌可查看原因'),695,748,840,20,'#AEC5BE')]
 for i,(name,cost,desc) in enumerate(cards):
  bright=(i in [0,1,2,3] if role==1 else i in [1,3]);picked=(i==3 and mode in ['card','drag']) or (i==2 and mode=='charge')
  state=('待弃' if i in selected else '保留') if discard else ('拖给任意队员' if mode in ['card','drag'] else '可执行' if bright else '生命已满' if i==4 else '暂无合法目标')
  y=815-(38 if i in selected else 14 if picked else 0)
  children=[rect('牌边框',0,0,218,235,'#19363D','#E9B985' if i in selected else '#EAD39B' if picked else '#63E9CF' if bright and not discard else '#476069')]+pill('费用',str(cost)+' AP',12,12,70,'#236680','#D9F7FF')+[text('牌名',name,96,14,110,27,'#EAD39B'),text('牌效果',desc,16,75,188,21),text('牌状态',state,16,182,188,18,'#FFCB9A' if i in selected else '#B6E9D9' if bright else '#A4B3B4')]
  L.append(frame('手牌 '+name,378+i*230,y,218,235,children))
  if i in selected:L+=pill('弃牌标记 '+name,'待弃',378+i*230+126,y-35,85,'#9D6243','#FFF3DD')
 if mode in ['card','drag']:
  for i,(x,y) in enumerate([(730,568),(665,520),(795,607)]):
   L.append(ring('可执行棋子 '+names[i],x,y,104,58));L+=pill('可执行标签 '+names[i],'可接牌',x-8,y-140,108,'#20574C','#C2FFF0')
 if mode=='unit':
  pos=[(730,568),(665,520),(795,607)][role];L.append(ring('当前棋子选中',*pos,104,58,'#EAD39B'))
  L+=pill('短步标记','↟ 免费',741 if role==0 else 678 if role==1 else 800,469 if role!=2 else 538,100,'#20574C','#C2FFF0')
  if role==1:L+=pill('普攻标记','剑 · 2AP',855,326,130,'#775034','#FFE2AF')
  else:L.append(text('无普攻提示','当前无普攻目标\n短步靠近后再进攻',1130,621,270,21,'#F3EDD4'))
 if mode=='drag':
  L.append(arrow('拖动路径',860,610,310,200));L.append(frame('拖动中的护盾牌',1015,525,210,130,[rect('拖牌底',0,0,210,130,'#193F48','#EAD39B'),text('拖牌名','护盾 · 1 AP',20,16,180,24,'#EAD39B'),text('拖牌说明','术师执行\n松手获得3盾',20,57,180,22)]))
  L += [rect('即时效果预览底',1032,350,390,143,'#102B32','#63E9CF'),text('即时效果预览','术师接牌\n护盾 0 → 3     全队AP 6 → 5\n松手执行 · 移出取消',1050,366,354,22)]
 if mode=='charge':
  # Separate native schematic avoids pretending the opening screenshot is a later battle snapshot.
  L += [rect('突击流程说明板',410,208,1090,472,'#102B32','#63E9CF'),text('流程标题','突击预览示意 / 先锋已先短步到D6',438,226,1020,27,'#EAD39B')]
  for y in range(3):
   for x in range(3):L.append(rect('突击局部格',470+x*102,310+y*102,92,92,'#24454A','#4B7176'))
  L += [text('敌人位置','巡兵\nC4',482,319,84,23,'#FFCBA1'),text('落点','落点\nC5',482,420,84,23,'#7FFFF0'),text('先锋位置','先锋\nD6',586,524,84,23,'#EAD39B'),arrow('突击路径',519,448,95,111),text('目标效果','① 拖突击给先锋\n② 点C5落点\n③ 指向C4巡兵：生命4 → 1\n④ 点击巡兵执行，消耗2AP',850,320,595,26),text('未确认不扣费','尚未执行：先锋仍在D6，全队AP仍为6。',850,526,600,21,'#8DDCCB'),text('取消方式','取消 / 右键：返回，不消耗手牌。',850,578,600,21,'#A9BFBB')]
 return frame('游戏审核画面',0,0,1920,1080,L,'#0B2027')

specs=[('01-角色选择-先锋','unit',0),('02-选牌找人','card',0),('03-拖牌预览','drag',2),('04-突击两阶段','charge',0),('05-弃牌零选中','discard0',0),('06-弃牌多选','discard2',0),('07-角色图解-术师','unit',2),('08-角色图解-射手','unit',1)]
def importer_order(node):
 for child in node.get('children',[]):importer_order(child)
 if 'children' in node:node['children'].reverse()
 return node
for name,mode,role in specs:(OUT/(name+'.json')).write_text(json.dumps(importer_order(build(mode,role)),ensure_ascii=False),encoding='utf-8')
print('Generated',len(specs),'editable layer trees')

