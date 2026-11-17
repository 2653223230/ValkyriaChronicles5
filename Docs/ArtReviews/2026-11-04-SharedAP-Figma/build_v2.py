import json
from pathlib import Path
exec(Path('Docs/ArtReviews/2026-11-03-SharedAP-Figma/build_review_layers.py').read_text(encoding='utf-8').split('specs=')[0])
OUT=Path('Docs/ArtReviews/2026-11-04-SharedAP-Figma')
def boardxy(x,y):return (400+(x-350)*1.24,80+(y-160)*1.24)
def marker(n,x,y,w,h,c='#63E9CF'):
 x,y=boardxy(x,y);return ring(n,x,y,w*1.24,h*1.24,c)
def tiles(n,coords,color,dash=False):
 polygons=''
 for x,y in coords:
  px,py=boardxy(x,y-38)
  polygons+=f'<polygon points="{px},{py-40} {px+74},{py} {px},{py+40} {px-74},{py}" fill="{color}" fill-opacity=".22" stroke="{color}" stroke-width="3" '+('stroke-dasharray="9 6"' if dash else '')+'/>'
 return svg(n,'<svg xmlns="http://www.w3.org/2000/svg" width="1920" height="1080">'+polygons+'</svg>',0,0,1920,1080)
def line(n,points,c='#FFBD81',dash=False):
 p=[boardxy(*v) for v in points];coords=' '.join(f'{x},{y}' for x,y in p)
 return svg(n,f'<svg xmlns="http://www.w3.org/2000/svg" width="1920" height="1080"><polyline points="{coords}" fill="none" stroke="{c}" stroke-width="4" '+('stroke-dasharray="9 8"' if dash else '')+'/></svg>',0,0,1920,1080)
def build2(mode,role):
 discard=mode=='discard';attack=mode=='attack';move=mode=='move';charge=mode=='charge';shield=mode=='shield';preview=attack or move or charge or shield
 L=[rect('主背景',0,0,1920,1080,'#0B2027'),frame('战场 / 晴岚截图锁定参考',400,80,1500,690,[image('实际截图',-350*1.24,-160*1.24,1920*1.24,1080*1.24)]),rect('左侧全部角色信息',0,0,380,1080,'#112D35'),text('关卡','晴岚炮台',24,20,340,29,'#EAD39B'),text('目标','击毁炮座',24,62,330,19,'#ACC6C3'),text('回合','第 1 回合 · '+('弃牌阶段' if discard else '我方行动'),1645,22,260,21,'#EAD39B')]
 hints={'unit':'选择队员查看技能；直接拖牌给队员即可。','attack':'攻击 → 射手：亮格为可攻击范围，松手后选敌人。','move':'机动 → 术师：亮格为可移动位置，松手后选落点。','charge':'突击 → 射手：选择落点，再选择落点射程内的敌人。','shield':'护盾 → 术师：松手获得护盾，移出取消。','discard':'点选要弃的牌；未选保留。再次点击取消选择。'}
 L.append(text('当前提示',hints[mode],420,22,1200,23))
 for i in range(3):
  y=110+i*78
  L.extend([rect('队员 '+names[i],16,y,348,68,'#1B4049','#EAD39B' if i==role else '#31545C'),frame('头像 '+names[i],23,y+5,48,58,[image('锁定立绘',-46*.65,-(222+i*134)*.65,1920*.65,1080*.65)]),text('名字 '+names[i],names[i],83,y+5,100,21),text('武器 '+names[i],weapons[i],178,y+8,180,16,'#98DCCB'),text('HP '+names[i],('9/9' if i==0 else '6/6')+' HP   盾0   短步可用',83,y+36,270,16,'#AEC9C4')])
 L.extend([text('详情名',names[role]+' · 武器与技能',24,363,330,25,'#EAD39B'),text('武器数据',weapons[role]+(' · 主目标及四邻' if role==2 else ''),24,405,330,22),text('范围图标题','武器范围示意',24,451,200,18,'#9DDCCF')])
 radius=[1,3,2][role];n=7;sz=27
 for y in range(n):
  for x in range(n):
   d=abs(x-3)+abs(y-3);L.append(rect('角色射程图',25+x*sz,486+y*sz,22,22,'#EAD39B' if d==0 else '#60D8C1' if d<=radius else '#24464D'))
 L += [text('图例','金：自己\n绿：射程\n\n实际以战场\n亮格为准',228,491,140,18,'#AEC9C4'),text('战技名称',['冲撞','架弩','移位'][role],24,690,100,24,'#EAD39B'),text('战技需要','战技牌 ＋ 1 AP',160,696,200,18,'#9DDCCF')]
 details=['推开邻敌1格，自身至少2盾。\n推不开仍得盾；炮座不能移动。','下一击主目标＋2伤、射程＋1。\n移动解除；下个己方回合结束失效。','将2格内可见敌人移到其四邻空格。\n不能移炮座，不能穿墙。'][role]
 L.extend([text('战技图解',['己 → 敌 → 空格','2伤 / 3格 → 4伤 / 4格','敌：原位 → 四邻空格'][role],24,742,335,23,'#EAD39B'),text('技能说明',details,24,787,335,18),text('基础行动','短步：免费 · 四邻1格\n普攻：2 AP · 每轮一次',24,859,335,20,'#B6D0C9'),rect('结束按钮',20,968,340,65,'#D6BC7F'),text('结束文字','弃牌并结束（2）' if discard else '结束回合',54,986,290,24,'#15343B')])
 # Intent is represented on the battlefield, never a sidebar list.
 L.append(line('敌方瞄准线 / 动画关键帧',[(898,405),(716,520)],'#F3B080',True))
 L.append(marker('锁定目标 / 呼吸圈',677,500,76,40,'#F3B080'))
 L.append(tiles('炮击落点 / 脉冲警戒',[(716,552),(782,590),(848,628)],'#EE916F',True))
 L.append(marker('炮座蓄力环',1040,354,64,34,'#EE916F'))
 L.append(line('巡兵移动终点方向',[(895,437),(850,477)],'#E6B183',True))
 L.append(marker('移动终点虚影',825,464,50,24,'#E6B183'))
 L.append(text('战场简洁图例','◇ 炮击区     ⊙ 锁定目标',1510,724,360,18,'#FFE4C8'))
 # Hand-adjacent shared AP and discard status.
 L.append(rect('共享AP与手牌统一底板',400,778,1500,302,'#0B2027'))
 L.extend([text('AP标签','全队共享 AP',422,791,200,22),text('AP数值','6 → '+('4' if charge else '5') if preview else '6 / 6',1005,784,200,31,'#68D7F6'),text('资源辅助',('确认前不扣费' if preview else '本轮剩余'),1210,796,250,18,'#ADC7C1')])
 for i in range(6):L.append(rect('AP '+str(i+1),650+55*i,792,44,30,'#244A53' if preview and i>=6-(2 if charge else 1) else '#50CDF4','#50CDF4'))
 picked={'attack':0,'move':1,'charge':2,'shield':3}.get(mode,-1)
 for i,(name,cost,desc) in enumerate(cards):
  selected=discard and i in [1,4];y=858-(20 if selected else 0)
  L.append(frame('卡牌 '+name,422+i*238,y,222,211,[rect('牌面',0,0,222,211,'#193C44','#E8B278' if selected else '#EAD39B' if i==picked else '#47646B'),text('费用',str(cost)+' AP',15,12,70,20,'#68D7F6'),text('名称',name,100,9,110,25,'#EAD39B'),text('效果',desc,15,60,195,20),text('状态','待弃' if selected else '保留' if discard else '预览中' if i==picked else '拖给队员',15,173,190,17,'#EAD39B' if selected or i==picked else '#AEC8C3')]))
 if discard:
  L.extend([rect('弃牌说明',1230,642,640,95,'#15343B'),text('弃牌统计','弃2张 · 留3张 · 下轮补2张',1250,658,590,25,'#EAD39B'),text('零选中说明','不选牌＝全部保留；下轮补至5张。',1250,697,590,19),rect('返回战斗',1650,867,225,56,'#244A53'),text('返回文字','返回战斗',1690,881,180,22)])
 elif mode=='unit':
  L.append(marker('选中队员',*[[(730,568),(665,520),(795,607)][role]][0],104,58,'#EAD39B'))
  L.append(tiles('免费短步格示意',[(650,590)] if role==1 else [(715,628)],'#63E9CF'))
 elif preview:
  points={'attack':(710,544),'move':(842,624),'charge':(710,544),'shield':(842,624)};px,py=boardxy(*points[mode])
  if attack:
   L.append(tiles('射手攻击范围 / 合法格表现示意',[(716,552),(782,515),(848,478),(914,441),(650,515),(716,478),(782,441),(848,404),(650,589),(584,552)],'#F4C17D'))
   L.append(marker('可命中敌人',854,420,90,50,'#F4C17D'))
  if move or charge:
   coords=[(782,665),(716,628),(650,590)] if move else [(650,590),(584,628),(716,628)]
   L.append(tiles('可移动落点 / 实线',coords,'#63E9CF'))
  if charge:
   L.append(tiles('指向落点后的攻击范围 / 虚线',[(716,552),(782,515),(848,478),(650,515),(584,552)],'#F4C17D',True))
   L.append(line('落点路径',[(716,552),(650,590)],'#63E9CF'))
  L.append(ring('接牌队员高亮',px-48,py-14,96,45,'#EAD39B'))
  L.append(frame('拖动中卡牌',px+85,py-85,196,104,[rect('拖牌底',0,0,196,104,'#143B44','#EAD39B'),text('拖牌文字',cards[picked][0]+' → '+names[role],12,12,180,23,'#EAD39B'),text('拖牌提示','锁定后选择目标' if not shield else '护盾 0 → 3',12,58,180,18)]))
  captions={'attack':'橙格：射手当前攻击范围\n松手锁定射手 → 点亮起的敌人','move':'青格：术师可移动落点\n松手锁定术师 → 指向落点看路径','charge':'青格：可移动位置\n橙色虚线：指向落点后的攻击范围','shield':'护盾 0 → 3\n松手生效 · 移出取消'}
  L.extend([rect('预览图例底',1390,570,480,125,'#12313A'),text('预览图例',captions[mode],1410,585,440,23,'#E3ECD9')])
 return frame('V2 游戏审核画面',0,0,1920,1080,L,'#0B2027')
def reverse(n):
 for c in n.get('children',[]):reverse(c)
 if 'children' in n:n['children'].reverse()
 return n
specs=[('01-左侧角色信息','unit',0),('02-拖攻击看射程','attack',1),('03-拖机动看移动范围','move',2),('04-突击落点与射程','charge',1),('05-护盾预览','shield',2),('06-弃牌选择','discard',0),('07-术师信息','unit',2),('08-射手信息','unit',1)]
for name,mode,role in specs:(OUT/(name+'.json')).write_text(json.dumps(reverse(build2(mode,role)),ensure_ascii=False),encoding='utf-8')
print('Generated 8 native V2 frames')

