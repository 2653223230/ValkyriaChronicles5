# VC5 规格对齐 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 以《战场女武神5最新规则与卡牌-20260621.md》为准，补齐工程与规格差异（得分区、结束弃牌、卡组/法力配置）。

**Architecture:** 在 `GameLogic` 回合流中插入 `Scoring` 与 `EndDiscard` 阶段；得分区坐标集中在 `Vc5ScoringZone`；`Player.kill_count` 作为累计胜利分（击杀+3、占点+1/+2）。

**Tech Stack:** Unity / TcgEngine C# / NUnit EditMode

**Backup:** `git tag backup/pre-spec-align-20260621` on `main` before branch `feature/vc5-spec-align-20260621`

---

## 差异核对摘要

| 规格项 | 实现前 | 实现后 |
|--------|--------|--------|
| 9 分制胜 + 击杀 +3 | ✅ kill_count | ✅ 不变 |
| 得分区 7 格占点 | ❌ 未实现 | ✅ ResolveScoringZone |
| 得分阶段 / 结束弃牌 | ❌ 直接下一回合 | ✅ Scoring → EndDiscard |
| 初始 3 法力 / 每回合 +1 | ⚠️ mana_start=2 靠首回合+1 | ✅ mana_start=3 + opening_turn |
| 手牌补满 5 | ✅ cards_per_turn=5 | ✅ |
| 卡组 20–30 / 同名 3 | ⚠️ 固定 20 / 同名 2 | ✅ deck_size_max=30, duplicate=3 |
| 三角出生点 3 英雄 | ✅ DeployInitialHeroes | ✅ |

---

## 已完成任务

- [x] `Vc5ScoringZone.cs` — 7 格坐标与计数
- [x] `GameLogic` — Scoring / EndDiscard 阶段
- [x] `GameplayData.asset` — mana/deck 参数
- [x] UI — 结束弃牌提示、手牌拖下弃牌
- [x] `Vc5ScoringZoneTests.cs`
- [x] `BoardSlot` 得分区淡金色标记
