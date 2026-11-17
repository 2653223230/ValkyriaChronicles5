using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VC5PvE;
using VC5PvE.SharedAP;

namespace VC5PvE.SharedAP.Tests
{
    public sealed class SharedVisualAdapterTests
    {
        GameObject boardObject;
        GameObject cameraObject;

        [Test]
        public void SharedRangesClearWithoutRemovingArtilleryWarning()
        {
            boardObject = new GameObject("shared range contract");
            var board = boardObject.AddComponent<BoardView>();
            board.Initialize(new BattleState { Width = 7, Height = 7 }, null);
            board.ShowCellOutlines(new[] { new GridPos(1, 5) }, Color.red);
            var method = typeof(BoardView).GetMethod("ShowSharedActionRanges");
            Assert.IsNotNull(method, "shared move/attack/future ranges must have a separate visual API");
            method.Invoke(board, new object[] { new[] { new GridPos(1, 4) }, new[] { new GridPos(2, 4) }, new[] { new GridPos(3, 4) }, null });
            Assert.IsTrue(board.GetComponentsInChildren<Transform>().Any(t => t.name == "Shared future range segment"));
            board.ClearHighlights();
            Assert.IsFalse(board.GetComponentsInChildren<Transform>().Any(t => t.gameObject.activeInHierarchy && t.name == "Shared future range segment"));
            Assert.IsTrue(board.GetComponentsInChildren<Transform>().Any(t => t.gameObject.activeInHierarchy && t.name == "Shared AP cell outline"));
        }

        [UnityTest]
        public IEnumerator ArtilleryWarningRetainsVisibleWidthAcrossPulseFrames()
        {
            boardObject = new GameObject("shared warning pulse");
            var board = boardObject.AddComponent<BoardView>();
            board.ShowPersonalAp = false;
            board.Initialize(new BattleState { Width = 7, Height = 7 }, null);
            board.ShowCellOutlines(new[] { new GridPos(1, 5) }, Color.red);
            var line = board.GetComponentsInChildren<LineRenderer>().Single(l => l.name == "Shared AP cell outline");
            for (int i = 0; i < 4; i++)
            {
                yield return null;
                Assert.That(line.startWidth, Is.InRange(.04f, .08f), "pulse must preserve a readable warning stroke");
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (boardObject != null) Object.DestroyImmediate(boardObject);
            if (cameraObject != null) Object.DestroyImmediate(cameraObject);
        }

        [Test]
        public void VisualProjectionUsesSevenBySevenAndLegacyPortraitRolesWithoutPersonalAp()
        {
            var state = new SState { Round = 2, Ap = 4, Phase = SPhase.Player };
            state.Units.Add(Unit("vanguard", SRole.Vanguard, true, 9, 9, 3, 6, 3));
            state.Units.Add(Unit("archer", SRole.Archer, true, 5, 6, 2, 6, 3));
            state.Units.Add(Unit("mage", SRole.Mage, true, 4, 6, 1, 6, 2));
            state.Units.Add(Unit("patrol", SRole.Patrol, false, 4, 4, 2, 3, 3));
            state.Units.Add(Unit("crossbow", SRole.Crossbow, false, 3, 3, 2, 3, 3));
            state.Units.Add(Unit("guard", SRole.Guard, false, 5, 5, 2, 2, 1));
            state.Units.Add(Unit("cannon", SRole.Cannon, false, 10, 10, 2, 1, 1));
            state.Find("archer").Braced = true;
            state.Find("cannon").Shield = 2;
            state.Obstacles.Add(new SPos(2, 3));

            BattleState visual = SharedVisualAdapter.ToBattleState(state);

            Assert.AreEqual(7, visual.Width);
            Assert.AreEqual(7, visual.Height);
            Assert.AreEqual(BattlePhase.Player, visual.Phase);
            Assert.AreEqual(0, visual.FindUnit("archer").Ap);
            Assert.AreEqual(0, visual.FindUnit("archer").MaxAp);
            Assert.AreEqual(UnitRole.Warrior, visual.FindUnit("vanguard").Role);
            Assert.AreEqual(UnitRole.Support, visual.FindUnit("archer").Role);
            Assert.AreEqual(UnitRole.Mage, visual.FindUnit("mage").Role);
            Assert.AreEqual(UnitRole.Guard, visual.FindUnit("patrol").Role);
            Assert.AreEqual(UnitRole.Shooter, visual.FindUnit("crossbow").Role);
            Assert.AreEqual(UnitRole.Guard, visual.FindUnit("guard").Role);
            Assert.AreEqual(UnitRole.ShieldPillar, visual.FindUnit("cannon").Role);
            Assert.IsTrue(visual.FindUnit("cannon").IsFacility);
            Assert.AreEqual(2, visual.FindUnit("cannon").Shield);
            Assert.AreEqual(4, visual.FindUnit("archer").AttackRange);
            Assert.AreEqual(0, visual.FindUnit("archer").InspireExpiresRound);
            Assert.IsTrue(visual.Obstacles.Contains(new GridPos(2, 3)));
        }

        [Test]
        public void ToPlanOnlyMovesTheActorAndSkipsTheStartingPathCell()
        {
            var before = new SState { Phase = SPhase.Player };
            before.Units.Add(Unit("mage", SRole.Mage, true, 6, 6, 1, 6, 2));
            before.Find("mage").Position = new SPos(2, 2);
            var after = before.Clone();
            after.Find("mage").Position = new SPos(3, 2);
            var request = new SAction { Kind = SActionKind.Card, ActorId = "mage", CardId = "skill", TargetId = "patrol" };
            var plan = new SPlan { Valid = true, After = after };
            plan.Path.AddRange(new[] { new SPos(2, 2), new SPos(3, 2) });

            ActionPlan adapted = SharedVisualAdapter.ToPlan(before, plan, request);

            Assert.AreEqual("mage", adapted.Request.ActorId);
            Assert.AreEqual(ActionKind.Card, adapted.Request.Kind);
            Assert.AreEqual("skill", adapted.Request.CardId);
            Assert.AreEqual(new GridPos(3, 2), adapted.Request.Destination.Value);
            Assert.AreEqual(1, adapted.Path.Count);
            Assert.AreEqual(new GridPos(3, 2), adapted.Path[0]);
        }

        [Test]
        public void ToPlanDoesNotMakeTheCasterFollowATargetDisplacementPath()
        {
            var before = new SState { Phase = SPhase.Player };
            before.Units.Add(Unit("mage", SRole.Mage, true, 6, 6, 1, 6, 2));
            before.Units.Add(Unit("patrol", SRole.Patrol, false, 4, 4, 2, 3, 3));
            before.Find("patrol").Position = new SPos(3, 2);
            var after = before.Clone();
            after.Find("patrol").Position = new SPos(4, 2);
            var request = new SAction { Kind = SActionKind.Card, ActorId = "mage", CardId = "skill", TargetId = "patrol", Destination = new SPos(4, 2) };
            var plan = new SPlan { Valid = true, After = after };
            plan.Path.AddRange(new[] { new SPos(3, 2), new SPos(4, 2) });

            ActionPlan adapted = SharedVisualAdapter.ToPlan(before, plan, request);

            Assert.IsNull(adapted.Request.Destination);
            Assert.IsEmpty(adapted.Path);
        }

        [Test]
        public void ToResultBuildsPrimaryAndSplashSummariesFromStateDiff()
        {
            var before = new SState { Phase = SPhase.Player };
            before.Units.Add(Unit("mage", SRole.Mage, true, 6, 6, 1, 6, 2));
            before.Units.Add(Unit("patrol", SRole.Patrol, false, 4, 4, 1, 3, 2));
            before.Units.Add(Unit("guard", SRole.Guard, false, 5, 5, 0, 4, 2));
            before.Find("patrol").Shield = 1;
            var after = before.Clone();
            after.Find("patrol").Shield = 0;
            after.Find("patrol").Hp = 2;
            after.Find("guard").Hp = 4;
            var request = new SAction { Kind = SActionKind.Card, ActorId = "mage", CardId = "attack", TargetId = "patrol" };
            var plan = new SPlan { Valid = true, After = after };
            plan.Affected.AddRange(new[] { new SPos(3, 2), new SPos(4, 2) });

            ActionResult result = SharedVisualAdapter.ToResult(before, plan, request);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(2, result.Damage);
            Assert.AreEqual("patrol", result.DamageSummary.TargetId);
            Assert.AreEqual(2, result.DamageSummary.HpLost);
            Assert.AreEqual(1, result.DamageSummary.ShieldAbsorbed);
            Assert.AreEqual("guard", result.AreaDamageSummaries.Single().TargetId);
            Assert.AreEqual(1, result.AreaDamageSummaries.Single().HpLost);
        }

        [Test]
        public void ToResultFindsSelfHealingWhenTheRequestHasNoTargetId()
        {
            var before = new SState { Phase = SPhase.Player };
            before.Units.Add(Unit("mage", SRole.Mage, true, 2, 6, 0, 3, 3));
            var after = before.Clone();
            after.Find("mage").Hp = 5;
            var request = new SAction { Kind = SActionKind.Card, ActorId = "mage", CardId = "heal" };
            var plan = new SPlan { Valid = true, After = after };

            ActionResult result = SharedVisualAdapter.ToResult(before, plan, request);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(3, result.Healing);
            Assert.IsNull(result.DamageSummary);
        }

        [UnityTest]
        public IEnumerator SevenBySevenBoardUsesCenteredWorldCoordinatesAndRejectsOutsideCells()
        {
            boardObject = new GameObject("7x7 board view test");
            var board = boardObject.AddComponent<BoardView>();
            cameraObject = new GameObject("board camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0, 12, 0);
            camera.transform.rotation = Quaternion.Euler(90, 0, 0);
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.aspect = 16f / 9f;
            camera.pixelRect = new Rect(0, 0, 1920, 1080);
            board.Initialize(new BattleState { Width = 7, Height = 7 }, camera);
            yield return null;

            Assert.AreEqual(new Vector3(-3, 0, 3), board.World(new GridPos(0, 0)));
            Assert.AreEqual(new Vector3(3, 0, -3), board.World(new GridPos(6, 6)));
            Assert.IsTrue(board.ScreenToGrid(camera.WorldToScreenPoint(new Vector3(3.49f, 0, -3)), out GridPos lastCell));
            Assert.AreEqual(new GridPos(6, 6), lastCell);
            GridPos outside;
            Assert.IsFalse(board.ScreenToGrid(camera.WorldToScreenPoint(new Vector3(3.51f, 0, -3)), out outside));
        }

        [Test]
        public void BoardViewKeepsTheLegacyEightByEightDefault()
        {
            var host = new GameObject("default board view test");
            try
            {
                var board = host.AddComponent<BoardView>();
                Assert.AreEqual(new Vector3(-3.5f, 0, 3.5f), board.World(new GridPos(0, 0)));
                Assert.AreEqual(new Vector3(3.5f, 0, -3.5f), board.World(new GridPos(7, 7)));
            }
            finally { Object.DestroyImmediate(host); }
        }

        static SUnit Unit(string id, SRole role, bool player, int hp, int maxHp, int shield, int x, int y)
        {
            return new SUnit
            {
                Id = id, Role = role, Name = id, Player = player,
                Hp = hp, MaxHp = maxHp, Shield = shield, Position = new SPos(x, y)
            };
        }
    }
}
