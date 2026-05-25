#!/usr/bin/env python3
"""Generate VC5 slime hero CardData / Ability / Effect / Condition .asset files."""
from __future__ import annotations

import json
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RES = ROOT / "Assets" / "TcgEngine" / "Resources"

# Existing resource GUIDs
G = {
    "card_script": "843a21f7f6f205741a0a7341aec8e84d",
    "ability_script": "8ce5f81e5cc37f547af6923758602c8c",
    "condition_owner": "004c422eee4e9ee41a7603d981a2fff7",
    "condition_target": "06df69f70efef364b8eab89605ed0d70",
    "condition_status": "7a3c78f482b96ed47896e9c6ad583d1e",
    "condition_any_enemy_status": "093ea5b1209d7264398c8045efc7f0ae",
    "condition_slot_dist": "016e59b7a7666f44e8ef9f834a4eba29",
    "condition_turn_interval": "c4e8a1b23d5f6789012345678abcdef0",
    "effect_dmg_status": "0a9a8ac7a70a1694794897264cb96c1b",
    "effect_apply_hp": "9001b065cdfc44cca5c15af93946bebf",
    "status_slime": "463634bf8fb6484089408fa5a96f01c3",
    "trait_slime": "510a78cb12497a646b6e2a8ba36b4dcf",  # wrong - need asset guid not script
    "team_try": "153b61109f8e25a40a61ca06e18ab072",
    "rarity_common": "13b592f40b7cd424c966f5d7eef425aa",
    "is_card": "902733d12f434e94b904de5ebf4112de",
    "is_enemy": "41c58513071a53b4e9066a58c439760f",
    "is_ally": "7463bf87d3794e249b7610c9de9c5327",
    "trait_slime_asset": "2be68ae9f58344abbea18f7130abbd99",
    "trait_slime_blood": "8dc28e32dda04e8fa5db147d2d272408",
    "trait_slime_corrosive": "15640fdadbed4058abc76178b858ad40",
    "trait_slime_hard": "8044c9926c9a426795f2ccc2b0f9064a",
    "field_slime": "9cfdba30c7af48888c5cf200fe9393dc",
}

# Fix trait_slime - read actual meta
for name in ["slime", "field_slime"]:
    meta = RES / "Traits" / f"{name}.asset.meta"
    if meta.exists():
        for line in meta.read_text(encoding="utf-8").splitlines():
            if line.startswith("guid:"):
                G[f"trait_{name}_asset" if name == "slime" else "field_slime"] = line.split(":")[1].strip()

REGISTRY: dict[str, str] = {}


def new_guid() -> str:
    return uuid.uuid4().hex


def ref(guid: str) -> str:
    return f"{{fileID: 11400000, guid: {guid}, type: 2}}"


def write_meta(path: Path, guid: str):
    path.write_text(
        f"""fileFormatVersion: 2
guid: {guid}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""",
        encoding="utf-8",
    )


def write_asset(path: Path, guid: str, body: str):
    path.parent.mkdir(parents=True, exist_ok=True)
    yaml = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {body.split('script_guid:')[1].split()[0] if False else ''}, type: 3}}
"""
    # body is full mono content lines after script line handled separately
    raise NotImplementedError


def mono(script_guid: str, name: str, fields: list[str]) -> str:
    lines = [
        "%YAML 1.1",
        "%TAG !u! tag:unity3d.com,2011:",
        "--- !u!114 &11400000",
        "MonoBehaviour:",
        "  m_ObjectHideFlags: 0",
        "  m_CorrespondingSourceObject: {fileID: 0}",
        "  m_PrefabInstance: {fileID: 0}",
        "  m_PrefabAsset: {fileID: 0}",
        "  m_GameObject: {fileID: 0}",
        "  m_Enabled: 1",
        "  m_EditorHideFlags: 0",
        f"  m_Script: {{fileID: 11500000, guid: {script_guid}, type: 3}}",
        f"  m_Name: {name}",
        "  m_EditorClassIdentifier: ",
    ]
    lines.extend(fields)
    return "\n".join(lines) + "\n"


def save(path: Path, guid: str, content: str):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8")
    write_meta(path.with_suffix(path.suffix + ".meta"), guid)
    REGISTRY[path.as_posix()] = guid


def ability(id_: str, title: str, trigger: int, target: int, *,
            mana=0, hp=0, discard=0, uses=0, value=0, duration=0,
            effects=None, status=None, cond_trig=None, cond_tgt=None, chain=None, desc="",
            guid: str | None = None):
    g = guid or new_guid()
    p = RES / "Abilities" / "vc5" / "heroes" / f"{id_}.asset"
    fields = [
        f"  id: {id_}",
        f"  trigger: {trigger}",
        f"  conditions_trigger: [{', '.join(ref(x) for x in (cond_trig or []))}]",
        f"  target: {target}",
        f"  conditions_target: [{', '.join(ref(x) for x in (cond_tgt or []))}]",
        "  filters_target: []",
        f"  effects: [{', '.join(ref(x) for x in (effects or []))}]",
        f"  status: [{', '.join(ref(x) for x in (status or []))}]",
        f"  value: {value}",
        f"  duration: {duration}",
        f"  chain_abilities: [{', '.join(ref(x) for x in (chain or []))}]",
        f"  mana_cost: {mana}",
        "  hp_cost: 0",
        f"  discard_cost: {discard}",
        "  exhaust: 0",
        "  fast_action: 0",
        f"  uses_per_turn: {uses}",
        "  board_fx: {fileID: 0}",
        "  caster_fx: {fileID: 0}",
        "  target_fx: {fileID: 0}",
        "  projectile_fx: {fileID: 0}",
        "  cast_audio: {fileID: 0}",
        "  target_audio: {fileID: 0}",
        "  charge_target: 0",
        f"  title: {json.dumps(title, ensure_ascii=False)}",
        f"  desc: {json.dumps(desc, ensure_ascii=False)}",
    ]
    if hp:
        fields[fields.index("  hp_cost: 0")] = f"  hp_cost: {hp}"
    save(p, g, mono(G["ability_script"], id_, fields))
    return g


def condition_status(name: str, status_effect: int, value: int, oper: int = 0) -> str:
    g = new_guid()
    p = RES / "Conditions" / "vc5" / "heroes" / f"{name}.asset"
    fields = [
        f"  has_status: {status_effect}",
        f"  value: {value}",
        f"  oper: {oper}",
    ]
    save(p, g, mono(G["condition_status"], name, fields))
    return g


def condition_any_enemy(name: str, status_effect: int, value: int) -> str:
    g = new_guid()
    p = RES / "Conditions" / "vc5" / "heroes" / f"{name}.asset"
    fields = [
        f"  status: {status_effect}",
        f"  value: {value}",
    ]
    save(p, g, mono(G["condition_any_enemy_status"], name, fields))
    return g


def condition_slot_dist(name: str, offset: int = 0) -> str:
    g = new_guid()
    p = RES / "Conditions" / "vc5" / "heroes" / f"{name}.asset"
    fields = [f"  range_offset: {offset}"]
    save(p, g, mono(G["condition_slot_dist"], name, fields))
    return g


def condition_turn_interval(name: str, key: str, interval: int) -> str:
    g = new_guid()
    p = RES / "Conditions" / "vc5" / "heroes" / f"{name}.asset"
    fields = [
        f"  track_key: {key}",
        f"  min_turn_interval: {interval}",
    ]
    save(p, g, mono(G["condition_turn_interval"], name, fields))
    return g


def effect_dmg_from_status(name: str, status: int, divisor: int, clear: bool) -> str:
    g = new_guid()
    p = RES / "Effects" / "vc5" / "heroes" / f"{name}.asset"
    fields = [
        f"  status: {status}",
        f"  divisor: {divisor}",
        f"  clear_status: {1 if clear else 0}",
    ]
    save(p, g, mono(G["effect_dmg_status"], name, fields))
    return g


def effect_apply_hp(name: str, status: int, divisor: int) -> str:
    g = new_guid()
    p = RES / "Effects" / "vc5" / "heroes" / f"{name}.asset"
    fields = [
        f"  status: {status}",
        f"  hp_divisor: {divisor}",
    ]
    save(p, g, mono(G["effect_apply_hp"], name, fields))
    return g


def hero_card(id_, title, text, atk, hp, move, atk_rng, traits, fields, abilities, series="黏黏(栗子球)"):
    g = new_guid()
    p = RES / "Cards" / "vc5" / "heroes" / f"{id_}.asset"
    trait_refs = ", ".join(ref(G[t]) for t in traits)
    field_refs = ", ".join(ref(G[t]) for t in fields)
    ab_refs = ", ".join(ref(a) for a in abilities)
    fields_yaml = [
        f"  id: {id_}",
        f"  title: {json.dumps(title, ensure_ascii=False)}",
        f"  series: {json.dumps(series, ensure_ascii=False)}",
        "  art_full: {fileID: 0}",
        "  art_board: {fileID: 0}",
        "  type: 10",
        f"  team: {ref(G['team_try'])}",
        f"  rarity: {ref(G['rarity_common'])}",
        "  mana: 0",
        "  hp_cost: 0",
        "  discard_cost: 0",
        f"  attack: {atk}",
        f"  hp: {hp}",
        f"  move_Range: {move}",
        f"  attack_Range: {atk_rng}",
        f"  traits: [{trait_refs}]",
        "  stats: []",
        f"  fields: [{field_refs}]",
        "  fast_action: 0",
        f"  abilities: [{ab_refs}]",
        f"  text: {json.dumps(text, ensure_ascii=False)}",
        "  desc: ",
        "  spawn_fx: {fileID: 0}",
        "  death_fx: {fileID: 0}",
        "  attack_fx: {fileID: 0}",
        "  damage_fx: {fileID: 0}",
        "  idle_fx: {fileID: 0}",
        "  spawn_audio: {fileID: 0}",
        "  death_audio: {fileID: 0}",
        "  attack_audio: {fileID: 0}",
        "  damage_audio: {fileID: 0}",
        "  deckbuilding: 1",
        "  cost: 100",
        "  packs: []",
    ]
    save(p, g, mono(G["card_script"], id_, fields_yaml))
    return g


def main():
    # --- Effects ---
    eff_blood_skill = effect_dmg_from_status("eff_blood_skill_dmg", 50, 2, False)
    eff_blood_awake = effect_dmg_from_status("eff_blood_awake_dmg", 50, 2, True)
    eff_hard_awake = effect_apply_hp("eff_hard_awake_slime", 50, 2)

    # --- Conditions ---
    cond_dist0 = condition_slot_dist("cond_attack_range_from_self", 0)
    cond_awake_enemy = condition_any_enemy("cond_enemy_has_slime4", 50, 4)
    cond_tgt_slime4 = condition_status("cond_target_slime4", 50, 4, 0)
    cond_ally_slime1 = condition_status("cond_ally_has_slime", 50, 1, 0)
    cond_hard_cd = condition_turn_interval("cond_hard_awake_cd", "vc5_hard_awake_last", 2)

    # --- Corrosive: pick_4 -> pick_3 -> pick_2 -> pick_1，每次选目标叠 1 层粘液 ---
    pick_guids = {i: new_guid() for i in range(1, 5)}
    cor_entry = pick_guids[4]
    for i in range(4, 0, -1):
        chain = [pick_guids[i - 1]] if i > 1 else []
        ability(
            f"vc5_corrosive_slime_pick_{i}",
            "腐蚀喷洒" if i == 4 else f"腐蚀喷洒({5 - i}/4)",
            5 if i == 4 else 0,
            30,
            guid=pick_guids[i],
            mana=1 if i == 4 else 0,
            uses=1 if i == 4 else 0,
            value=1,
            status=[G["status_slime"]],
            cond_tgt=[G["is_card"]],
            chain=chain,
        )

    # --- Blood ---
    ab_blood_skill = ability(
        "vc5_blood_slime_skill", "血腥喷溅", 5, 30,
        mana=1, uses=1, value=1,
        effects=[eff_blood_skill],
        cond_tgt=[G["is_card"], G["is_enemy"], cond_dist0],
    )
    ab_blood_awake = ability(
        "vc5_blood_slime_awake", "粘液引爆", 5, 10,
        uses=1, value=1,
        effects=[eff_blood_awake],
        cond_trig=[cond_awake_enemy],
        cond_tgt=[G["is_card"], G["is_enemy"], cond_tgt_slime4],
    )

    # --- Hard ---
    ab_hard_awake = ability(
        "vc5_hard_slime_awake", "防护粘液", 5, 30,
        mana=1, uses=1,
        effects=[eff_hard_awake],
        cond_trig=[cond_hard_cd],
        cond_tgt=[G["is_card"], G["is_ally"], cond_ally_slime1],
    )

    texts = {
        "vc5_hero_slime_blood": (
            "【被动】我方的【黏黏】英雄对身上处于【粘液】状态下的敌方单位造成伤害时，恢复1点生命值。\n"
            "【技能】消耗1法力，每回合1次：对攻击距离内的一名敌人造成1+（其身上【粘液】层数/2，向下取整）的伤害。\n"
            "【觉醒】场上存在至少一名敌人带有4层及以上【粘液】时可发动：对所有满足条件的敌人造成1+（其【粘液】层数/2，向下取整）的伤害，并移除其全部【粘液】。"
        ),
        "vc5_hero_slime_corrosive": (
            "【被动】身上处于【粘液】状态下的敌方单位移动力-1（不叠加，无论多少层【粘液】都只-1）。\n"
            "【觉醒】消耗1法力，每回合1次：重复4次向任意单位施加1层【粘液】（每次可选不同目标）。"
        ),
        "vc5_hero_slime_hard": (
            "【被动】自己的（具有【粘液】标签的）卡牌可对友方单位释放【粘液】；友军受到伤害时，按已附着【粘液】层数减伤并移除对应层数。\n"
            "【觉醒】消耗1法力，每2回合1次：向一名已带有【粘液】的友军施放【粘液】，次数=当前生命值/2（向下取整）。"
        ),
    }

    hero_card(
        "vc5_hero_slime_blood", "血腥黏黏", texts["vc5_hero_slime_blood"],
        1, 12, 3, 1, ["trait_slime_asset", "trait_slime_blood"], ["field_slime"],
        [ab_blood_skill, ab_blood_awake],
    )
    hero_card(
        "vc5_hero_slime_corrosive", "腐蚀黏黏", texts["vc5_hero_slime_corrosive"],
        1, 14, 3, 1, ["trait_slime_asset", "trait_slime_corrosive"], ["field_slime"],
        [cor_entry],
    )
    hero_card(
        "vc5_hero_slime_hard", "坚硬黏黏", texts["vc5_hero_slime_hard"],
        0, 15, 2, 1, ["trait_slime_asset", "trait_slime_hard"], ["field_slime"],
        [ab_hard_awake],
    )

    # Script metas for new C# (fixed guids referenced above)
    script_metas = {
        "Assets/TcgEngine/Scripts/Conditions/vc5/ConditionVc5AbilityTurnInterval.cs": G["condition_turn_interval"],
        "Assets/TcgEngine/Scripts/Effects/vc5/EffectApplyStatusFromCasterHP.cs": G["effect_apply_hp"],
        "Assets/TcgEngine/Scripts/GameLogic/Vc5AbilityTurnTracker.cs": new_guid(),
    }
    for rel, sg in script_metas.items():
        mp = ROOT / (rel + ".meta")
        if not mp.exists():
            write_meta(mp, sg)

    manifest = ROOT / "Docs" / "vc5_card_registry" / "slime_hero_assets.json"
    manifest.write_text(json.dumps(REGISTRY, indent=2, ensure_ascii=False), encoding="utf-8")
    print(f"Generated {len(REGISTRY)} assets. Manifest -> {manifest}")


if __name__ == "__main__":
    main()
