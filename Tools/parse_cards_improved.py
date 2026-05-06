#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
改进版：解析docx文档中的卡牌和英雄信息，生成结构化的Excel文件
使用更精确的正则表达式和文本分析
"""
import re
from pathlib import Path
from zipfile import ZipFile
import xml.etree.ElementTree as ET
import pandas as pd
import json

def extract_text_from_docx(docx_path):
    """从docx文件中提取纯文本"""
    root_xml = ET.fromstring(ZipFile(docx_path).read('word/document.xml'))
    ns = {'w': 'http://schemas.openxmlformats.org/wordprocessingml/2006/main'}
    texts = [t.text or '' for t in root_xml.findall('.//w:t', ns)]
    return ''.join(texts)

def parse_heroes(text):
    """解析英雄信息"""
    heroes = []
    
    # 查找所有英雄部分
    # 英雄格式：名称：攻击力X生命值X移动力X攻击距离X技能：...觉醒：...
    hero_sections = re.split(r'(?=[^：]+：攻击力)', text)
    
    for section in hero_sections:
        if '攻击力' not in section or '生命值' not in section:
            continue
        
        # 提取英雄名称
        name_match = re.match(r'([^：]+)：', section)
        if not name_match:
            continue
        name = name_match.group(1).strip()
        
        # 跳过不是英雄的部分
        if '卡牌' in name or '卡包' in name or '示例' in name:
            continue
        
        # 提取属性
        attack_match = re.search(r'攻击力[：:]?(\d+)', section)
        hp_match = re.search(r'生命值[：:]?(\d+)', section)
        move_match = re.search(r'移动力[：:]?(\d+)', section)
        attack_range_match = re.search(r'攻击距离[：:]?(\d+)', section)
        
        if not all([attack_match, hp_match, move_match, attack_range_match]):
            continue
        
        attack = int(attack_match.group(1))
        hp = int(hp_match.group(1))
        move = int(move_match.group(1))
        attack_range = int(attack_range_match.group(1))
        
        # 提取技能和觉醒
        skills = []
        awakenings = []
        
        # 查找技能部分
        skill_pattern = r'技能[：:](.*?)(?=觉醒[：:]|$)'
        skill_match = re.search(skill_pattern, section, re.DOTALL)
        if skill_match:
            skill_text = skill_match.group(1).strip()
            # 清理技能文本
            skill_text = re.sub(r'注[：:].*', '', skill_text)
            skills.append(skill_text)
        
        # 查找觉醒部分
        awakening_pattern = r'觉醒[：:](.*?)(?=技能[：:]|$)'
        awakening_match = re.search(awakening_pattern, section, re.DOTALL)
        if awakening_match:
            awakening_text = awakening_match.group(1).strip()
            awakenings.append(awakening_text)
        
        # 查找被动
        passive_match = re.search(r'被动[：:](.*?)(?=技能[：:]|觉醒[：:]|$)', section, re.DOTALL)
        if passive_match:
            passive_text = passive_match.group(1).strip()
            skills.append(f"被动：{passive_text}")
        
        heroes.append({
            '名称': name,
            '攻击力': attack,
            '生命值': hp,
            '移动力': move,
            '攻击距离': attack_range,
            '技能描述': '; '.join(skills),
            '觉醒描述': '; '.join(awakenings),
            '完整文本': section[:200]  # 只保存前200字符用于参考
        })
    
    return heroes

def parse_cards(text):
    """解析卡牌信息"""
    cards = []
    
    # 找到卡牌部分
    card_start = text.find('卡牌：')
    if card_start == -1:
        return cards
    
    card_text = text[card_start:]
    
    # 卡牌格式：名称：类型类卡牌，包含字段...cost：...效果：...特殊效果：...
    # 使用更精确的匹配
    card_pattern = r'([^：\n]+)[：:]([^类]+)类卡牌[，,].*?包含字段([^c]*?)cost[：:]([^效果]+)效果[：:](.*?)(?=特殊效果[：:]|$)'
    
    matches = list(re.finditer(card_pattern, card_text, re.DOTALL))
    
    for i, match in enumerate(matches):
        name = match.group(1).strip()
        card_type = match.group(2).strip()
        fields = match.group(3).strip()
        cost = match.group(4).strip()
        effect = match.group(5).strip()
        
        # 查找特殊效果（在下一个卡牌之前）
        special_effect = ""
        if i + 1 < len(matches):
            next_match_start = matches[i + 1].start()
            current_match_end = match.end()
            special_text = card_text[current_match_end:next_match_start]
            special_match = re.search(r'特殊效果[：:](.*?)(?=[^：]+[：:][^类]+类卡牌|$)', special_text, re.DOTALL)
            if special_match:
                special_effect = special_match.group(1).strip()
        else:
            # 最后一个卡牌，查找到文本末尾
            current_match_end = match.end()
            special_text = card_text[current_match_end:]
            special_match = re.search(r'特殊效果[：:](.*?)(?=示例|$)', special_text, re.DOTALL)
            if special_match:
                special_effect = special_match.group(1).strip()
        
        # 清理文本
        effect = re.sub(r'\s+', ' ', effect).strip()
        special_effect = re.sub(r'\s+', ' ', special_effect).strip()
        
        cards.append({
            '名称': name,
            '类型': card_type,
            '字段': fields,
            '费用': cost,
            '效果': effect,
            '特殊效果': special_effect
        })
    
    return cards

def create_excel(heroes, cards, output_path):
    """创建Excel文件"""
    with pd.ExcelWriter(output_path, engine='openpyxl') as writer:
        # 创建英雄工作表
        if heroes:
            heroes_df = pd.DataFrame(heroes)
            heroes_df.to_excel(writer, sheet_name='英雄', index=False)
            print(f"英雄工作表：{len(heroes)} 条记录")
        
        # 创建卡牌工作表
        if cards:
            cards_df = pd.DataFrame(cards)
            cards_df.to_excel(writer, sheet_name='卡牌', index=False)
            print(f"卡牌工作表：{len(cards)} 条记录")
    
    print(f"\nExcel文件已生成: {output_path}")

def main():
    """主函数"""
    root = Path(__file__).parent.parent
    docx_files = list(root.glob('*.docx'))
    
    # 查找包含"先行测试"的docx文件
    target_docx = None
    for docx in docx_files:
        if "先行测试" in docx.name:
            target_docx = docx
            break
    
    if not target_docx:
        print("未找到包含'先行测试'的docx文件")
        return
    
    print(f"正在解析: {target_docx}")
    
    # 提取文本
    text = extract_text_from_docx(target_docx)
    
    # 解析英雄和卡牌
    heroes = parse_heroes(text)
    cards = parse_cards(text)
    
    print(f"\n解析结果：")
    print(f"英雄：{len(heroes)} 个")
    print(f"卡牌：{len(cards)} 张")
    
    # 生成Excel
    output_excel = root / '卡牌数据_改进版.xlsx'
    create_excel(heroes, cards, output_excel)
    
    # 生成JSON（用于调试）
    output_json = root / '卡牌数据_改进版.json'
    with open(output_json, 'w', encoding='utf-8') as f:
        json.dump({'heroes': heroes, 'cards': cards}, f, ensure_ascii=False, indent=2)
    print(f"JSON文件已生成: {output_json}")

if __name__ == '__main__':
    main()




