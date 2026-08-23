# VC5 Compile and Turn Flow Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Restore compilation for the VC5 scoring/end-discard work and align opening mana growth with the 2026-06-21 design.

**Architecture:** Keep the existing VC5 phase implementation. Add the missing network message type used by end-discard, and make opening-turn mana growth apply per round instead of mutating the global opening flag inside the player loop.

**Tech Stack:** Unity 2021.3.33f1c1, Unity Netcode, NUnit EditMode tests.

---

### Task 1: Message Serialization Coverage

**Files:**
- Modify: `Assets/TcgEngine/Scripts/Network/NetworkMsg.cs`
- Create: `Assets/TcgEngine/Scripts/Testing/Editor/Vc5NetworkMsgTests.cs`

- [x] Add an EditMode test that serializes and deserializes `MsgString`.
- [x] Confirm the project compilation error is caused by missing `MsgString`.
- [x] Add `MsgString : INetworkSerializable` with a single `text` field.
- [x] Re-run the targeted batch verification and confirm it passes.

### Task 2: Opening Mana Coverage

**Files:**
- Modify: `Assets/TcgEngine/Scripts/GameLogic/GameLogic.cs`
- Create: `Assets/TcgEngine/Scripts/Testing/Editor/Vc5TurnFlowTests.cs`

- [x] Add an EditMode test proving both players start at `GameplayData.mana_start`.
- [x] Add an EditMode test proving both players gain exactly `mana_per_turn` when the next round begins.
- [x] Confirm the old implementation mutated the global `opening_turn` flag inside the player loop.
- [x] Change `StartTurn()` so opening round detection is captured before the player mana loop and `opening_turn` is cleared after all players are processed.
- [x] Re-run the targeted batch verification.

### Task 3: Verification and Docs

**Files:**
- Modify only if behavior changes: `战场女武神5最新规则与卡牌-20260621.md`

- [x] Run Unity batch verification for VC5 compile/runtime checks.
- [x] Run a Unity compile/test command sufficient to prove the project no longer hits `MsgString`.
- [x] Confirm implementation behavior matches the design document, so no design-rule update is needed.
