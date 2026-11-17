using System.Linq;
using NUnit.Framework;

namespace VC5PvE.SharedAP.Tests
{
    // No altered units, resources, hand contents, or draw order: legal actions only.
    public class SharedNaturalClearTests
    {
        SState s;
        void Apply(SAction a){var p=SharedRules.Apply(s,a);Assert.IsTrue(p.Valid,p.Reason+" actor="+a.ActorId+" card="+a.CardId+" target="+a.TargetId+" destination="+a.Destination);}
        void Step(string actor,int x,int y){Apply(new SAction{Kind=SActionKind.ShortStep,ActorId=actor,Destination=new SPos(x,y)});}
        void Card(string actor,SCard kind,string target=null,SPos? destination=null)
        {
            var card=s.Hand.FirstOrDefault(c=>c.Kind==kind);Assert.IsNotNull(card,"Expected actual hand card "+kind);
            Apply(new SAction{Kind=SActionKind.Card,ActorId=actor,CardId=card.Id,TargetId=target,Destination=destination});
        }
        void End(){Assert.IsTrue(SharedRules.EndTurn(s,new string[0]).Valid);}
        [Test]
        public void Seed17FixedOpeningClearsNaturallyWithNewFifthCard()
        {
            s=SharedRules.Create(17,true);
            Step("vanguard",3,5);Card("vanguard",SCard.Charge,"patrol",new SPos(2,4));
            Step("mage",4,5);Card("mage",SCard.Move,destination:new SPos(4,3));Card("mage",SCard.Attack,"crossbow");
            Step("archer",2,5);Card("archer",SCard.Shield);End();
            Step("vanguard",2,3);Card("vanguard",SCard.Move,destination:new SPos(3,2));
            Card("vanguard",SCard.Heavy,"guard");
            Step("archer",2,4);Card("archer",SCard.Attack,"crossbow");
            Assert.AreEqual(SPhase.Won,s.Phase);Assert.AreEqual(2,s.Round);Assert.AreEqual(2,s.Ap);
            Assert.IsFalse(s.Find("guard").Alive);Assert.IsTrue(s.Units.Where(u=>u.Player).All(u=>u.Alive));
        }

        [Test] public void Seed17ArcherKillsPatrolOnRoundOneAndGuardEventuallyLandsThreeDamageHit()
        {
            s=SharedRules.Create(17,true);
            Card("archer",SCard.Attack,"patrol");
            Apply(new SAction{Kind=SActionKind.BasicAttack,ActorId="archer",TargetId="patrol"});
            Assert.IsFalse(s.Find("patrol").Alive,"Attack card plus basic attack must deal four damage to the C4 patrol.");

            var first=SharedRules.EndTurn(s,new string[0]);Assert.IsTrue(first.Valid,first.Reason);
            var firstGuard=first.Steps.First(x=>x.Action.ActorId=="guard");
            CollectionAssert.AreEqual(new[]{new SPos(3,2),new SPos(4,2),new SPos(4,3)},firstGuard.Path,
                "The first-round guard intent must expose each traversed cell from D3 through E3 to E4.");
            Assert.IsNull(firstGuard.Action.TargetId,"The guard should still be approaching after the first two cells.");

            var second=SharedRules.EndTurn(s,new string[0]);Assert.IsTrue(second.Valid,second.Reason);
            var secondGuard=second.Steps.First(x=>x.Action.ActorId=="guard");
            CollectionAssert.AreEqual(new[]{new SPos(4,3),new SPos(4,4),new SPos(4,5)},secondGuard.Path,
                "The second-round guard intent must expose each traversed cell through E6.");
            Assert.AreEqual("mage",secondGuard.Action.TargetId,"The fixed opening should leave the mage as the guard's hit target.");
            Assert.AreEqual(3,secondGuard.Before.Find("mage").Hp-secondGuard.After.Find("mage").Hp,
                "The second-round guard hit must deal three damage.");
        }

        [Test] public void Seed17PatrolAliveAtC4HasIndependentThreeDamageAttackRegression()
        {
            s=SharedRules.Create(17,true);
            Card("archer",SCard.Attack,"patrol");
            Assert.AreEqual(2,s.Find("patrol").Hp,"The patrol remains alive after the archer uses only the attack card.");

            var patrolHit=false;
            for(var enemyPhase=0;enemyPhase<4&&!patrolHit;enemyPhase++)
            {
                var plan=SharedRules.EndTurn(s,new string[0]);Assert.IsTrue(plan.Valid,plan.Reason);
                var patrolStep=plan.Steps.FirstOrDefault(x=>x.Action.ActorId=="patrol");
                Assert.IsNotNull(patrolStep,"The surviving patrol should act independently of the guard regression.");
                if(patrolStep.Action.TargetId==null)continue;
                var targetBefore=patrolStep.Before.Find(patrolStep.Action.TargetId);
                var targetAfter=patrolStep.After.Find(patrolStep.Action.TargetId);
                Assert.AreEqual(3,targetBefore.Hp-targetAfter.Hp,"A patrol hit must deal three damage.");
                patrolHit=true;
            }
            Assert.IsTrue(patrolHit,"The fixed opening with a living patrol should produce a real patrol hit within four enemy phases.");
        }

        [Test] public void RandomOpeningsAreDeterministicAndDoNotChangeBaselineActions()
        {
            for(int seed=0;seed<24;seed++)
            {
                s=SharedRules.Create(seed,false);
                CollectionAssert.AreEqual(s.Hand.Select(c=>c.Id),SharedRules.Create(seed,false).Hand.Select(c=>c.Id));
                Assert.AreEqual(5,s.Hand.Count);Assert.AreEqual(18,s.Hand.Concat(s.Deck).Select(c=>c.Id).Distinct().Count());
                // All three can advance for free whatever the five actual opening cards are.
                Step("vanguard",3,5);Step("archer",2,5);Step("mage",4,5);Assert.AreEqual(6,s.Ap);
                End();Assert.AreEqual(2,s.Round);Assert.AreEqual(6,s.Ap);Assert.AreEqual(5,s.Hand.Count);
            }
        }
    }
}
