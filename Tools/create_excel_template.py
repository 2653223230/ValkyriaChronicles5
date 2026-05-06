#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
创建Excel模板，用于录入卡牌和英雄数据
"""
import pandas as pd
from pathlib import Path

def create_template():
    """创建Excel模板"""
    root = Path(__file__).parent.parent
    
    # 英雄模板
    heroes_template = pd.DataFrame(columns=[
        'ID',  # 唯一标识符，如：hero_slime_bloody
        '名称',  # 显示名称，如：血腥黏黏
        '类型',  # Hero
        '攻击力',
        '生命值',
        '移动力',
        '攻击距离',
        '费用',  # 通常为0
        '技能1_触发',  # None, Activate, Ongoing, StartOfTurn, EndOfTurn, OnPlay, OnBeforeAttack, OnAfterAttack, OnDeath等
        '技能1_目标',  # None, Self, SelectTarget, AllCardsBoard等
        '技能1_效果描述',  # 自然语言描述
        '技能1_效果类型',  # Damage, Heal, AddStatus, AddTrait, Move, Summon等
        '技能1_效果值',  # 数值
        '技能1_费用',  # 技能消耗的法力值
        '技能2_触发',
        '技能2_目标',
        '技能2_效果描述',
        '技能2_效果类型',
        '技能2_效果值',
        '技能2_费用',
        '觉醒_触发',
        '觉醒_目标',
        '觉醒_效果描述',
        '觉醒_效果类型',
        '觉醒_效果值',
        '觉醒_费用',
        '特性',  # 用分号分隔，如：黏黏;粘液
        '描述文本',
    ])
    
    # 添加示例数据
    heroes_template = pd.concat([heroes_template, pd.DataFrame([{
        'ID': 'hero_slime_bloody',
        '名称': '血腥黏黏',
        '类型': 'Hero',
        '攻击力': 1,
        '生命值': 12,
        '移动力': 3,
        '攻击距离': 1,
        '费用': 0,
        '技能1_触发': 'Ongoing',
        '技能1_目标': 'None',
        '技能1_效果描述': '己方的【黏黏】英雄对身上持有【粘液】的敌方单位造成伤害时恢复1生命值',
        '技能1_效果类型': 'Heal',
        '技能1_效果值': 1,
        '技能1_费用': 0,
        '技能2_触发': 'Activate',
        '技能2_目标': 'SelectTarget',
        '技能2_效果描述': '对一攻击距离内的一名敌人造成1+其身上【粘液】层数/2的伤害',
        '技能2_效果类型': 'Damage',
        '技能2_效果值': 1,
        '技能2_费用': 1,
        '觉醒_触发': 'Activate',
        '觉醒_目标': 'AllCardsBoard',
        '觉醒_效果描述': '当场上有敌人身上有至少4层【粘液】效果时激活，对所有满足条件的敌人造成1+【粘液】层数/2的伤害，并移除所有【粘液】',
        '觉醒_效果类型': 'Damage',
        '觉醒_效果值': 1,
        '觉醒_费用': 0,
        '特性': '黏黏',
        '描述文本': '血腥黏黏的技能描述',
    }])], ignore_index=True)
    
    # 卡牌模板
    cards_template = pd.DataFrame(columns=[
        'ID',  # 唯一标识符，如：card_slime_strike
        '名称',  # 显示名称
        '类型',  # Spell, Character, Artifact, Secret, Equipment
        '费用',  # 法力值或生命值
        '费用类型',  # Mana, HP, Discard
        '攻击力',  # 如果是Character类型
        '生命值',  # 如果是Character类型
        '移动力',  # 如果是Character类型
        '攻击距离',  # 如果是Character类型
        '字段',  # 用分号分隔，如：黏黏;重击
        '效果1_触发',  # OnPlay, None等
        '效果1_目标',  # SelectTarget, PlayTarget等
        '效果1_描述',
        '效果1_类型',  # Damage, Heal, AddStatus, Summon, Move等
        '效果1_值',
        '效果2_触发',
        '效果2_目标',
        '效果2_描述',
        '效果2_类型',
        '效果2_值',
        '特殊效果_条件',  # 如：如果本卡片由【黏黏】使用
        '特殊效果_描述',
        '特殊效果_类型',
        '特殊效果_值',
        '特性',
        '描述文本',
    ])
    
    # 添加示例数据
    cards_template = pd.concat([cards_template, pd.DataFrame([{
        'ID': 'card_slime_strike',
        '名称': '黏黏重击',
        '类型': 'Spell',
        '费用': 2,
        '费用类型': 'HP',
        '攻击力': 0,
        '生命值': 0,
        '移动力': 0,
        '攻击距离': 0,
        '字段': '黏黏;重击',
        '效果1_触发': 'OnPlay',
        '效果1_目标': 'SelectTarget',
        '效果1_描述': '对攻击距离内的一个任意单位造成2+攻击力的伤害',
        '效果1_类型': 'Damage',
        '效果1_值': 2,
        '效果2_触发': '',
        '效果2_目标': '',
        '效果2_描述': '',
        '效果2_类型': '',
        '效果2_值': '',
        '特殊效果_条件': '如果本卡片由【黏黏】使用',
        '特殊效果_描述': '额外附加一层【粘液】',
        '特殊效果_类型': 'AddStatus',
        '特殊效果_值': 1,
        '特性': '黏黏',
        '描述文本': '黏黏重击的卡牌描述',
    }])], ignore_index=True)
    
    # 保存到Excel
    output_path = root / '卡牌录入模板.xlsx'
    with pd.ExcelWriter(output_path, engine='openpyxl') as writer:
        heroes_template.to_excel(writer, sheet_name='英雄', index=False)
        cards_template.to_excel(writer, sheet_name='卡牌', index=False)
    
    print(f"Excel模板已创建: {output_path}")
    print("\n使用说明：")
    print("1. 在'英雄'工作表中填写英雄数据")
    print("2. 在'卡牌'工作表中填写卡牌数据")
    print("3. 保存后使用Unity编辑器工具导入")

if __name__ == '__main__':
    create_template()




