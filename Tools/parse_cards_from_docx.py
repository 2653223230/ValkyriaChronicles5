#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
解析docx文档中的卡牌和英雄信息，生成结构化的Excel文件
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

def parse_heroes_and_cards(text):
    """解析文本中的英雄和卡牌信息"""
    heroes = []
    cards = []
    
    # 分割文本为英雄和卡牌部分
    hero_section = ""
    card_section = ""
    
    if "英雄：" in text:
        parts = text.split("卡牌：", 1)
        if len(parts) == 2:
            hero_section = parts[0]
            card_section = parts[1]
        else:
            hero_section = text
    
    # 解析英雄
    hero_pattern = r'([^：]+)：(?:攻击力|攻击力：)(\d+)(?:生命值|生命值：)(\d+)(?:移动力|移动力：)(\d+)(?:攻击距离|攻击距离：)(\d+)(.*?)(?=(?:[^：]+)：(?:攻击力|攻击力：)|卡牌：|$)'
    hero_matches = re.finditer(hero_pattern, hero_section, re.DOTALL)
    
    for match in hero_matches:
        name = match.group(1).strip()
        attack = int(match.group(2))
        hp = int(match.group(3))
        move = int(match.group(4))
        attack_range = int(match.group(5))
        skills_text = match.group(6).strip()
        
        # 解析技能和觉醒
        skills = []
        awakenings = []
        
        # 查找技能
        skill_match = re.search(r'技能[：:](.*?)(?=觉醒[：:]|$)', skills_text, re.DOTALL)
        if skill_match:
            skill_text = skill_match.group(1).strip()
            skills.append(skill_text)
        
        # 查找觉醒
        awakening_match = re.search(r'觉醒[：:](.*?)(?=技能[：:]|$)', skills_text, re.DOTALL)
        if awakening_match:
            awakening_text = awakening_match.group(1).strip()
            awakenings.append(awakening_text)
        
        heroes.append({
            'name': name,
            'attack': attack,
            'hp': hp,
            'move_range': move,
            'attack_range': attack_range,
            'skills': '; '.join(skills),
            'awakenings': '; '.join(awakenings),
            'full_text': skills_text
        })
    
    # 解析卡牌
    card_pattern = r'([^：]+)[：:]([^：]+)类卡牌[，,].*?cost[：:]([^效果]+)效果[：:](.*?)(?=特殊效果[：:]|$)'
    card_matches = re.finditer(card_pattern, card_section, re.DOTALL)
    
    for match in card_matches:
        name = match.group(1).strip()
        card_type = match.group(2).strip()
        cost = match.group(3).strip()
        effect = match.group(4).strip()
        
        # 查找特殊效果
        special_effect = ""
        special_match = re.search(r'特殊效果[：:](.*?)(?=[^：]+[：:]|$)', card_section[card_section.find(match.group(0)):], re.DOTALL)
        if special_match:
            special_effect = special_match.group(1).strip()
        
        cards.append({
            'name': name,
            'type': card_type,
            'cost': cost,
            'effect': effect,
            'special_effect': special_effect
        })
    
    return heroes, cards

def create_excel(heroes, cards, output_path):
    """创建Excel文件，包含英雄和卡牌两个工作表"""
    with pd.ExcelWriter(output_path, engine='openpyxl') as writer:
        # 创建英雄工作表
        if heroes:
            heroes_df = pd.DataFrame(heroes)
            heroes_df.to_excel(writer, sheet_name='英雄', index=False)
        
        # 创建卡牌工作表
        if cards:
            cards_df = pd.DataFrame(cards)
            cards_df.to_excel(writer, sheet_name='卡牌', index=False)
    
    print(f"Excel文件已生成: {output_path}")

def main():
    """主函数"""
    # 查找docx文件
    root = Path(__file__).parent.parent
    docx_files = list(root.glob('*.docx'))
    
    if not docx_files:
        print("未找到docx文件")
        return
    
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
    
    # 保存文本到文件（用于调试）
    text_file = root / 'doc_text_parsed.txt'
    text_file.write_text(text, encoding='utf-8')
    print(f"文本已保存到: {text_file}")
    
    # 解析英雄和卡牌
    heroes, cards = parse_heroes_and_cards(text)
    
    print(f"解析到 {len(heroes)} 个英雄")
    print(f"解析到 {len(cards)} 张卡牌")
    
    # 生成Excel
    output_excel = root / '卡牌数据.xlsx'
    create_excel(heroes, cards, output_excel)
    
    # 同时生成JSON格式（用于调试）
    output_json = root / '卡牌数据.json'
    with open(output_json, 'w', encoding='utf-8') as f:
        json.dump({'heroes': heroes, 'cards': cards}, f, ensure_ascii=False, indent=2)
    print(f"JSON文件已生成: {output_json}")

if __name__ == '__main__':
    main()




