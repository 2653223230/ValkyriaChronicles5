namespace VC5PvE.SharedAP
{
    /// <summary>Rule-derived display copy for comparing the three shared-AP roles.</summary>
    public static class SharedRoleInfo
    {
        public static string Weapon(SUnit unit)
        {
            if (unit == null) return "无执行者";

            var damage = SharedRules.WeaponDamage(unit);
            var range = SharedRules.WeaponRange(unit);
            if (unit.Role == SRole.Archer)
            {
                var brace = unit.Braced ? "；当前架弩：伤害 " + (damage + 2) + "，射程 " + range : "；架弩后伤害 +2、射程 +1，移动解除";
                return "武器伤害 " + damage + "；射程 " + range + "；单体" + brace;
            }
            if (unit.Role == SRole.Mage)
                return "武器伤害 " + damage + "；射程 " + range + "；主目标外，对相邻且可见的敌人各造成 1 点范围伤害";
            if (unit.Role == SRole.Cannon)
                return "炮击伤害 " + damage + "；攻击红框内单位";
            return "武器伤害 " + damage + "；射程 " + range + "；单体";
        }

        public static string Skill(SUnit unit)
        {
            if (unit == null) return "无执行者";
            switch (unit.Role)
            {
                case SRole.Vanguard: return "推开相邻敌人 1 格（后一格须为空；炮座不移动），自身获得至少 2 护盾";
                case SRole.Archer: return "架弩：下一击伤害 +2、射程 +1；移动会解除，未使用时于下次己方回合结束时解除";
                case SRole.Mage: return "星火爆裂：消耗 1 AP，点选 2 格内可见敌人；目标及其四邻可见敌人各受 2 点伤害";
                default: return "无角色战技";
            }
        }

        static string AttackCopy(SUnit unit,int bonus)
        {
            int damage=SharedRules.WeaponDamage(unit)+(unit.Role==SRole.Archer&&unit.Braced?2:0)+bonus;
            string text="主目标 "+damage+" 伤害 · 射程 "+SharedRules.WeaponRange(unit);
            if(unit.Role==SRole.Mage)text+="\n相邻可见敌人各受 1 点范围伤害";
            else text+="\n单体攻击";
            if(unit.Role==SRole.Archer&&unit.Braced)text+="；消耗架弩";
            return text;
        }

        public static string CardEffect(SUnit unit, SCard card)
        {
            if (unit == null) return "无执行者";
            switch (card)
            {
                case SCard.Attack:
                    return AttackCopy(unit,0);
                case SCard.Heavy:
                    return AttackCopy(unit,2);
                case SCard.Move:
                    return unit.Role == SRole.Archer && unit.Braced
                        ? "移动 1–2 格；移动会解除架弩"
                        : "移动 1–2 格";
                case SCard.Charge:
                    var chargeActor = unit.Role == SRole.Archer && unit.Braced ? unit.Clone() : unit;
                    if (chargeActor != unit) chargeActor.Braced = false;
                    return "移动 1–2 格后攻击。\n" + AttackCopy(chargeActor,0) +
                           (unit.Role == SRole.Archer ? "；突击移动会解除架弩" : "");
                case SCard.Skill:
                    return Skill(unit);
                case SCard.Shield:
                    return "护盾补到 3，下轮开始失效；已有3盾不可用。无职业加成";
                case SCard.Heal:
                    return "接牌棋子恢复 3 点生命，不超过上限；无职业加成";
                default:
                    return "未知卡牌";
            }
        }
    }
}
