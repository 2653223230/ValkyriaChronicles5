using System;
using System.Linq;
using UnityEngine;

namespace VC5PvE.SharedAP
{
    public sealed partial class SharedBattleController
    {
        public FocusTutorialStep FocusStep { get; private set; }
        public bool FocusTutorialActive => FocusStep != FocusTutorialStep.Off && !tutorialSkipped && State != null && State.Phase != SPhase.Won && State.Phase != SPhase.Lost;
        public string FocusTitle { get; private set; }
        public string FocusBody { get; private set; }
        public string FocusChapter { get; private set; }
        public bool FocusExitOpen { get; private set; }
        public string FocusRecoveryReason { get; private set; }
        public bool FocusRestartConfirm { get; private set; }
        public float FocusRejectedAt { get; private set; }
        public string FocusEnemyActorId { get; private set; }

        string focusMoveCardId, focusSkillCardId, focusShieldCardId;
        SAction focusPendingAction;
        SPlan focusPendingRound;
        int focusPendingRevision;
        bool focusActionPending;
        string focusLastRejected;
        int focusRetainedCount;

        bool FocusInputLocked => Busy || FocusExitOpen || FocusRestartConfirm || FocusRecoveryReason != null;
        bool FocusRequiresCardDrag => FocusStep == FocusTutorialStep.DragMove || FocusStep == FocusTutorialStep.DragSkill || FocusStep == FocusTutorialStep.DragShield;
        bool FocusAllowsEndTurn => !FocusTutorialActive || !FocusInputLocked && (FocusStep == FocusTutorialStep.EndTurn || FocusStep == FocusTutorialStep.Retain);
        bool FocusAllowsConfirmEndTurn => !FocusTutorialActive || !FocusInputLocked && FocusStep == FocusTutorialStep.Retain;

        void InitializeFocusTutorial(bool enabled)
        {
            FocusStep = enabled ? FocusTutorialStep.SelectMage : FocusTutorialStep.Off;
            focusMoveCardId = "card-7";
            focusSkillCardId = "card-12";
            focusShieldCardId = "card-15";
            focusPendingAction = null;
            focusPendingRound = null;
            focusPendingRevision = 0;
            focusActionPending = false;
            focusLastRejected = null;
            focusRetainedCount = 0;
            FocusExitOpen = FocusRestartConfirm = false;
            FocusRecoveryReason = null;
            FocusEnemyActorId = null;
            FocusRejectedAt = -10f;
            RefreshFocusCopy();
        }

        void RefreshFocusCopy()
        {
            FocusTitle = null;
            FocusBody = null;
            switch (FocusStep)
            {
                case FocusTutorialStep.SelectMage:
                    FocusChapter = "认识队员"; FocusTitle = "点击术师，看看她能做什么"; FocusBody = "点左侧框中的头像。\n角色信息会在同一侧展开。"; break;
                case FocusTutorialStep.ReadInfo:
                    FocusChapter = "认识队员"; FocusTitle = "生命、射程和战技都在这里"; FocusBody = "左侧有技能图示。全队共用 6 AP。\n先试一次不花 AP 的移动。"; break;
                case FocusTutorialStep.ShortStep:
                    FocusChapter = "移动与出牌"; FocusTitle = "点这个足迹，免费走一格"; FocusBody = "每名队员每轮都有一次免费短步。\n点地面的标记，不用拖牌。"; break;
                case FocusTutorialStep.DragMove:
                    FocusChapter = "移动与出牌";
                    if (IsDragging && PreviewExecutorId == "mage") { FocusTitle = "现在松手，选定术师"; FocusBody = "亮起的格子是这张牌能到的位置。\n松手后，再点一个落点。"; }
                    else { FocusTitle = "按住「机动」，拖到术师身上"; FocusBody = "从框中的牌开始拖。\n拖到术师身上时，会出现移动范围。"; } break;
                case FocusTutorialStep.MoveTarget:
                    FocusChapter = "移动与出牌"; FocusTitle = "点亮起的空格，走到这里"; FocusBody = "术师会沿路径走两格。\n这次移动消耗 1 AP。"; break;
                case FocusTutorialStep.DragSkill:
                    FocusChapter = "移动与出牌";
                    if (IsDragging && PreviewExecutorId == "mage") { FocusTitle = "松手选定术师，再选敌人"; FocusBody = "当前可在两格内选敌人。\n星火爆裂会伤到目标和四邻的敌人。"; }
                    else { FocusTitle = "把「战技」拖给术师"; FocusBody = "她的战技是范围爆裂。\n先拖给队员，再选要攻击的敌人。"; } break;
                case FocusTutorialStep.SkillTarget:
                    FocusChapter = "移动与出牌"; FocusTitle = "点框中的弩手，一次命中三敌"; FocusBody = "十字内的敌人各受 2 点伤害。\n术师在范围内，也不会伤到自己。"; break;
                case FocusTutorialStep.SelectArcher:
                    FocusChapter = "普通攻击"; FocusTitle = "点击射手棋子"; FocusBody = "点击左侧头像也可选中。"; break;
                case FocusTutorialStep.BasicAttack:
                    FocusChapter = "普通攻击"; FocusTitle = "点击剑标，不用出牌也能攻击"; FocusBody = "普通攻击消耗 2 AP，每人每轮一次。\n这个巡兵只剩 2 点生命。"; break;
                case FocusTutorialStep.DragShield:
                    FocusChapter = "回合交接"; FocusTitle = "把「护盾」拖给术师"; FocusBody = "她站在前面，需要挡住敌人的反击。\n松手就获得 3 点护盾，消耗 1 AP。"; break;
                case FocusTutorialStep.EndTurn:
                    FocusChapter = "回合交接"; FocusTitle = "点击「结束回合」，看看怎样留牌"; FocusBody = "行动已经完成，下一步决定留哪些牌。\n未用完的 AP 不会带到下一轮。"; break;
                case FocusTutorialStep.Retain:
                    FocusChapter = "回合交接"; FocusTitle = "想留下的牌不用点";
                    int hand = State == null ? 0 : State.Hand.Count, discard = DiscardIds.Count, retained = Math.Max(0, hand - discard), draw = Math.Max(0, 5 - retained);
                    FocusBody = "点牌会选中弃掉，再点可取消。\n当前弃 " + discard + " 张，保留 " + retained + " 张，下轮补 " + draw + " 张。"; break;
                case FocusTutorialStep.Enemy:
                    FocusChapter = "回合交接"; FocusTitle = "敌人行动中，注意炮击落点"; FocusBody = "红框里的格子将受到炮击。\n等待敌方行动完成，自动继续。"; break;
                case FocusTutorialStep.Ready:
                    FocusChapter = "回合交接"; FocusTitle = focusRetainedCount > 0 ? "AP 恢复了，留下的牌还在" : "AP 恢复了，手牌补满了";
                    FocusBody = "点击后重新开始本关，不再显示教学。\n由你自由决定移动、出牌与攻击。"; break;
                default: FocusChapter = null; break;
            }
        }

        public bool FocusAllowsCard(string id)
        {
            if (!FocusTutorialActive) return true;
            if (FocusInputLocked) return false;
            if (FocusRecoveryReason != null) return false;
            if (FocusStep == FocusTutorialStep.Retain) return State != null && State.Hand.Any(c => c.Id == id);
            if (string.IsNullOrEmpty(id)) return false;
            return FocusStep == FocusTutorialStep.DragMove && id == focusMoveCardId
                || FocusStep == FocusTutorialStep.DragSkill && id == focusSkillCardId
                || FocusStep == FocusTutorialStep.DragShield && id == focusShieldCardId;
        }

        public bool FocusAllowsUnit(string id)
        {
            if (!FocusTutorialActive) return true;
            if (FocusInputLocked) return false;
            if (FocusRecoveryReason != null) return false;
            switch (FocusStep)
            {
                case FocusTutorialStep.SelectMage:
                case FocusTutorialStep.ShortStep:
                case FocusTutorialStep.DragMove:
                case FocusTutorialStep.MoveTarget:
                case FocusTutorialStep.DragSkill:
                case FocusTutorialStep.SkillTarget:
                case FocusTutorialStep.DragShield:
                    return id == "mage";
                case FocusTutorialStep.SelectArcher:
                case FocusTutorialStep.BasicAttack:
                    return id == "archer";
                default: return false;
            }
        }

        bool FocusAllowsBasic(SActionKind kind)
        {
            if (!FocusTutorialActive) return true;
            if (FocusInputLocked) return false;
            return FocusRecoveryReason == null && FocusStep == FocusTutorialStep.ShortStep && kind == SActionKind.ShortStep && SelectedId == "mage"
                || FocusRecoveryReason == null && FocusStep == FocusTutorialStep.BasicAttack && kind == SActionKind.BasicAttack && SelectedId == "archer";
        }

        bool FocusAllowsCell(SPos cell)
        {
            if (!FocusTutorialActive) return true;
            if (FocusInputLocked) return false;
            if (FocusRecoveryReason != null) return false;
            var unit = State == null ? null : State.UnitAt(cell);
            switch (FocusStep)
            {
                case FocusTutorialStep.SelectMage: return unit != null && unit.Id == "mage";
                case FocusTutorialStep.ShortStep: return unit != null && unit.Id == "mage" || cell == new SPos(4, 5);
                case FocusTutorialStep.DragMove: return unit != null && unit.Id == "mage";
                case FocusTutorialStep.MoveTarget: return cell == new SPos(4, 3);
                case FocusTutorialStep.DragSkill: return unit != null && unit.Id == "mage";
                case FocusTutorialStep.SkillTarget: return unit != null && unit.Id == "crossbow";
                case FocusTutorialStep.SelectArcher: return unit != null && unit.Id == "archer";
                case FocusTutorialStep.BasicAttack: return unit != null && unit.Id == "patrol";
                case FocusTutorialStep.DragShield: return unit != null && unit.Id == "mage";
                default: return false;
            }
        }

        bool FocusAllowsAction(SAction action)
        {
            if (!FocusTutorialActive) return true;
            if (FocusInputLocked) return false;
            if (FocusRecoveryReason != null || action == null) return false;
            var mage = State == null ? null : State.Find("mage");
            var archer = State == null ? null : State.Find("archer");
            switch (FocusStep)
            {
                case FocusTutorialStep.ShortStep:
                    return mage != null && mage.Alive && mage.Position == new SPos(4, 6) && !mage.StepUsed && action.Kind == SActionKind.ShortStep && action.ActorId == "mage" && action.Destination == new SPos(4, 5);
                case FocusTutorialStep.MoveTarget:
                    return mage != null && mage.Alive && mage.Position == new SPos(4, 5) && State.Hand.Any(c => c.Id == focusMoveCardId && c.Kind == SCard.Move) && action.Kind == SActionKind.Card && action.CardId == focusMoveCardId && action.ActorId == "mage" && action.Destination == new SPos(4, 3);
                case FocusTutorialStep.SkillTarget:
                    return mage != null && mage.Alive && mage.Position == new SPos(4, 3) && State.Hand.Any(c => c.Id == focusSkillCardId && c.Kind == SCard.Skill) && action.Kind == SActionKind.Card && action.CardId == focusSkillCardId && action.ActorId == "mage" && action.TargetId == "crossbow";
                case FocusTutorialStep.BasicAttack:
                    return archer != null && archer.Alive && State.Find("patrol")?.Alive == true && action.Kind == SActionKind.BasicAttack && action.ActorId == "archer" && action.TargetId == "patrol";
                case FocusTutorialStep.DragShield:
                    return mage != null && mage.Alive && mage.Position == new SPos(4, 3) && State.Hand.Any(c => c.Id == focusShieldCardId && c.Kind == SCard.Shield) && action.Kind == SActionKind.Card && action.CardId == focusShieldCardId && action.ActorId == "mage";
                default: return false;
            }
        }

        string FocusActionKey(SAction action) => action == null ? "" : action.Kind + "/" + (action.CardId ?? action.ActorId ?? "") + "/" + (action.TargetId ?? (action.Destination.HasValue ? SharedRules.Coord(action.Destination.Value) : ""));

        void RejectFocusInput(string target)
        {
            if (!FocusTutorialActive || FocusInputLocked) return;
            focusLastRejected = target;
            FocusRejectedAt = Time.unscaledTime;
            if (FocusStep == FocusTutorialStep.Retain) RefreshFocusCopy();
            else Hint = "先完成框中的操作。";
            if (!Busy) Render();
        }

        void OnFocusUnitSelected(SUnit unit)
        {
            if (!FocusTutorialActive || unit == null) return;
            if (FocusStep == FocusTutorialStep.SelectMage && unit.Id == "mage") FocusStep = FocusTutorialStep.ReadInfo;
            else if (FocusStep == FocusTutorialStep.SelectArcher && unit.Id == "archer") FocusStep = FocusTutorialStep.BasicAttack;
            RefreshFocusCopy();
        }

        void OnFocusCardAssigned(SCardInstance card, SUnit receiver)
        {
            if (!FocusTutorialActive || card == null || receiver == null || receiver.Id != "mage") return;
            if (FocusStep == FocusTutorialStep.DragMove && card.Id == focusMoveCardId) FocusStep = FocusTutorialStep.MoveTarget;
            else if (FocusStep == FocusTutorialStep.DragSkill && card.Id == focusSkillCardId) FocusStep = FocusTutorialStep.SkillTarget;
            RefreshFocusCopy();
        }

        void FilterFocusActionTargets()
        {
            if (!FocusTutorialActive || Board == null) return;
            if (FocusStep == FocusTutorialStep.MoveTarget)
            { options.Clear(); options.Add(Grid(new SPos(4, 3))); Board.ShowHighlights(options); }
            else if (FocusStep == FocusTutorialStep.SkillTarget)
            { options.Clear(); options.Add(Grid(new SPos(3, 3))); Board.ShowHighlights(options); }
        }

        void OnFocusActionCommitted(SAction action, SCard? cardKind, SPlan plan)
        {
            if (!FocusTutorialActive || action == null || plan == null) return;
            focusPendingAction = action;
            focusPendingRevision = plan.Revision;
            focusActionPending = true;
        }

        void OnFocusActionAnimationCompleted(SAction action, SPlan plan)
        {
            if (!FocusTutorialActive || !focusActionPending || action == null || plan == null || focusPendingAction == null) return;
            if (focusPendingAction.Kind != action.Kind || focusPendingAction.ActorId != action.ActorId || focusPendingAction.CardId != action.CardId
                || focusPendingAction.TargetId != action.TargetId || focusPendingAction.Destination != action.Destination
                || State == null || State.Revision <= focusPendingRevision || State.Revision != plan.After.Revision)
            { focusActionPending = false; focusPendingAction = null; return; }
            focusActionPending = false;
            focusPendingAction = null;
            if (State.Phase == SPhase.Won || State.Phase == SPhase.Lost) { FocusStep = FocusTutorialStep.Off; RefreshFocusCopy(); return; }
            switch (FocusStep)
            {
                case FocusTutorialStep.ShortStep: FocusStep = FocusTutorialStep.DragMove; break;
                case FocusTutorialStep.MoveTarget: FocusStep = FocusTutorialStep.DragSkill; break;
                case FocusTutorialStep.SkillTarget: FocusStep = FocusTutorialStep.SelectArcher; break;
                case FocusTutorialStep.BasicAttack: FocusStep = FocusTutorialStep.DragShield; break;
                case FocusTutorialStep.DragShield: FocusStep = FocusTutorialStep.EndTurn; break;
            }
            RefreshFocusCopy();
        }

        void OnFocusRetentionOpened()
        {
            if (!FocusTutorialActive) return;
            if (FocusStep == FocusTutorialStep.EndTurn) FocusStep = FocusTutorialStep.Retain;
            RefreshFocusCopy();
        }

        void OnFocusEnemyStarted(SPlan plan)
        {
            if (!FocusTutorialActive) return;
            focusPendingRound = plan;
            FocusStep = FocusTutorialStep.Enemy;
            FocusEnemyActorId = plan == null || plan.Steps.Count == 0 ? null : plan.Steps[0].Action?.ActorId;
            RefreshFocusCopy();
        }

        void OnFocusEnemyActionStarted(SAction action)
        {
            if (FocusTutorialActive && FocusStep == FocusTutorialStep.Enemy) FocusEnemyActorId = action?.ActorId;
        }

        void OnFocusRoundCompleted(SPlan plan)
        {
            if (!FocusTutorialActive || FocusStep != FocusTutorialStep.Enemy) return;
            if (State != null && (State.Phase == SPhase.Won || State.Phase == SPhase.Lost)) { FocusStep = FocusTutorialStep.Off; FocusRecoveryReason = null; tutorialFinished = true; }
            else if (plan == null || focusPendingRound != plan || State == null || State.Phase != SPhase.Player)
            { FocusStep = FocusTutorialStep.Off; FocusRecoveryReason = "敌方回合结束后，战局没有进入新的我方回合。"; }
            else FocusStep = FocusTutorialStep.Ready;
            focusPendingRound = null;
            FocusEnemyActorId = null;
            RefreshFocusCopy();
        }

        public void ContinueFocusTutorial()
        {
            if (!FocusTutorialActive || FocusInputLocked) return;
            if (FocusStep == FocusTutorialStep.ReadInfo) FocusStep = FocusTutorialStep.ShortStep;
            else if (FocusStep == FocusTutorialStep.Ready)
            {
                Restart(true);
                SkipTutorial();
                Hint = "自由战斗：击败三名敌兵，或摧毁炮座。";
                Render();
                return;
            }
            else return;
            RefreshFocusCopy();
            if (FocusStep == FocusTutorialStep.ShortStep) { var mage = State.Find("mage"); if (mage != null) { SelectedId = mage.Id; ShowRoleDetails = true; ShowUnitActionOptions(mage); } }
            Render();
        }

        public void RequestSkipTutorial()
        { if (!FocusTutorialActive || Busy) return; FocusExitOpen = true; FocusRestartConfirm = false; RefreshFocusCopy(); Render(); }

        public void ResumeFocusTutorial()
        { FocusExitOpen = false; FocusRestartConfirm = false; Render(); }

        public void ConfirmSkipTutorial()
        {
            if ((!FocusExitOpen && FocusRecoveryReason == null) || Busy) return;
            FocusExitOpen = false; FocusRestartConfirm = false; FocusRecoveryReason = null; focusActionPending = false; focusPendingAction = null;
            ResetInput(); DiscardIds.Clear(); FocusStep = FocusTutorialStep.Off; tutorialSkipped = true; RefreshFocusCopy();
            Hint = "已退出教学，继续当前战局。"; Render();
        }

        public void RequestRestartFocusTutorial()
        { if (Busy) return; FocusExitOpen = false; FocusRestartConfirm = true; RefreshFocusCopy(); Render(); }

        public void ConfirmRestartFocusTutorial()
        { if (!FocusRestartConfirm || Busy) return; FocusRestartConfirm = false; Restart(true); }

        public void ReportFocusAnchorMissing(string reason)
        {
            if (!FocusTutorialActive || string.IsNullOrEmpty(reason) || Busy) return;
            FocusRecoveryReason = reason;
            FocusExitOpen = false;
            FocusRestartConfirm = false;
            ResetInput();
            Hint = "教学暂停：" + reason;
            RefreshFocusCopy();
            Render();
        }

        public void CheckFocusTutorialState()
        {
            if (!FocusTutorialActive || Busy || FocusRecoveryReason != null) return;
            if (FocusStep == FocusTutorialStep.Enemy || FocusStep == FocusTutorialStep.Ready) return;
            var mage = State.Find("mage"); var archer = State.Find("archer"); var crossbow = State.Find("crossbow");
            if (mage == null || !mage.Alive) { ReportFocusAnchorMissing("術師已不在可操作位置。退出教学或重新开始教学。"); return; }
            SPos expectedMage = FocusStep == FocusTutorialStep.SelectMage || FocusStep == FocusTutorialStep.ReadInfo || FocusStep == FocusTutorialStep.ShortStep ? new SPos(4, 6) :
                FocusStep == FocusTutorialStep.DragMove || FocusStep == FocusTutorialStep.MoveTarget ? new SPos(4, 5) : new SPos(4, 3);
            if (mage.Position != expectedMage) { ReportFocusAnchorMissing("术师当前不在本步预期的位置。退出教学或重新开始教学。"); return; }
            if ((FocusStep == FocusTutorialStep.SelectArcher || FocusStep == FocusTutorialStep.BasicAttack) && (archer == null || !archer.Alive)) { ReportFocusAnchorMissing("射手已不在可操作位置。退出教学或重新开始教学。"); return; }
            if (FocusStep == FocusTutorialStep.SkillTarget && (crossbow == null || !crossbow.Alive)) { ReportFocusAnchorMissing("指定的弩手目标已不存在。退出教学或重新开始教学。"); return; }
            if ((FocusStep == FocusTutorialStep.DragMove || FocusStep == FocusTutorialStep.MoveTarget) && !State.Hand.Any(c => c.Id == focusMoveCardId)
                || (FocusStep == FocusTutorialStep.DragSkill || FocusStep == FocusTutorialStep.SkillTarget) && !State.Hand.Any(c => c.Id == focusSkillCardId)
                || FocusStep == FocusTutorialStep.DragShield && !State.Hand.Any(c => c.Id == focusShieldCardId))
                ReportFocusAnchorMissing("本步指定的卡牌实体已不存在。退出教学或重新开始教学。");
        }

        public void SkipTutorial()
        {
            tutorialSkipped = true; tutorialStep = 0; fixedTutorial = false; FocusStep = FocusTutorialStep.Off;
            FocusExitOpen = FocusRestartConfirm = false; FocusRecoveryReason = null; focusActionPending = false; focusPendingAction = null;
            ResetInput(); Render();
        }

        public bool Teaching => FocusTutorialActive || !tutorialSkipped && !tutorialFinished && fixedTutorial;
        public int TutorialStep => FocusTutorialActive ? FocusStep == FocusTutorialStep.SelectMage ? 1 : FocusStep == FocusTutorialStep.ReadInfo ? 1 : FocusStep == FocusTutorialStep.ShortStep ? 2 : FocusStep == FocusTutorialStep.DragMove ? 3 : FocusStep == FocusTutorialStep.MoveTarget ? 4 : FocusStep == FocusTutorialStep.DragSkill ? 5 : FocusStep == FocusTutorialStep.SkillTarget ? 6 : FocusStep == FocusTutorialStep.SelectArcher ? 7 : FocusStep == FocusTutorialStep.BasicAttack ? 8 : FocusStep == FocusTutorialStep.DragShield || FocusStep == FocusTutorialStep.EndTurn || FocusStep == FocusTutorialStep.Retain ? 9 : 0 : Teaching && fixedTutorial ? Mathf.Clamp(tutorialStep, 0, 9) : 0;
        public string TutorialFocusUnitId => FocusTutorialActive ? FocusStep == FocusTutorialStep.SelectArcher || FocusStep == FocusTutorialStep.BasicAttack ? "archer" : FocusStep == FocusTutorialStep.SelectMage || FocusStep == FocusTutorialStep.ReadInfo || FocusStep == FocusTutorialStep.ShortStep || FocusStep == FocusTutorialStep.DragMove || FocusStep == FocusTutorialStep.MoveTarget || FocusStep == FocusTutorialStep.DragSkill || FocusStep == FocusTutorialStep.SkillTarget || FocusStep == FocusTutorialStep.DragShield ? "mage" : null : null;
        public string TutorialFocusCardId => FocusTutorialActive ? FocusStep == FocusTutorialStep.DragMove ? focusMoveCardId : FocusStep == FocusTutorialStep.DragSkill ? focusSkillCardId : FocusStep == FocusTutorialStep.DragShield ? focusShieldCardId : null : null;
        public string TutorialText => FocusTutorialActive ? FocusBody : null;
    }
}
