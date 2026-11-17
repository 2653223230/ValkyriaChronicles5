using System.Linq;
using NUnit.Framework;

namespace VC5PvE.SharedAP.Tests
{
    public class SharedRoundTests
    {
        static SAction Card(SState s,string actor,SCard kind,string target=null,SPos? dest=null)
        {
            var c=s.Hand.FirstOrDefault(x=>x.Kind==kind);
            if(c==null){c=s.Deck.First(x=>x.Kind==kind);s.Deck.Remove(c);s.Hand.Add(c);}
            return new SAction{Kind=SActionKind.Card,ActorId=actor,CardId=c.Id,TargetId=target,Destination=dest};
        }
        static void Do(SState s,SAction a){var p=SharedRules.Apply(s,a);Assert.IsTrue(p.Valid,p.Reason);}
        static void Step(SState s,string id,int x,int y){Do(s,new SAction{Kind=SActionKind.ShortStep,ActorId=id,Destination=new SPos(x,y)});}
        [Test] public void FullPaperOpeningAndRetainedSkillMatchLedger()
        {
            var s=SharedRules.Create(17,true);
            Step(s,"vanguard",3,5);Do(s,Card(s,"vanguard",SCard.Charge,"patrol",new SPos(2,4)));
            Step(s,"mage",4,5);Do(s,Card(s,"mage",SCard.Move,dest:new SPos(4,3)));
            Do(s,Card(s,"mage",SCard.Attack,"crossbow"));Step(s,"archer",2,5);Do(s,Card(s,"archer",SCard.Shield));
            Assert.AreEqual(1,s.Ap);Assert.AreEqual(0,s.Find("patrol").Hp);Assert.AreEqual(2,s.Find("crossbow").Hp);Assert.AreEqual(4,s.Find("guard").Hp);
            string retainedSkill=s.Hand.Single().Id;Assert.AreEqual(SCard.Skill,s.Hand.Single().Kind);var preview=SharedRules.EnemyPreview(s);
            Assert.AreEqual(1,preview.After.Find("mage").Hp);Assert.AreEqual(1,preview.After.Find("archer").Shield);
            Assert.IsTrue(SharedRules.EndTurn(s,s.Hand.Where(c=>c.Id!=retainedSkill).Select(c=>c.Id).ToArray()).Valid);
            Assert.AreEqual(2,s.Round);Assert.AreEqual(6,s.Ap);Assert.AreEqual(5,s.Hand.Count);Assert.AreEqual(0,s.Find("archer").Shield);
            Assert.AreEqual(6,s.Find("archer").Hp);Assert.AreEqual(1,s.Find("mage").Hp);
            Do(s,Card(s,"mage",SCard.Heal));Assert.AreEqual(4,s.Find("mage").Hp);Assert.AreEqual(5,s.Ap);
        }
        [Test] public void CannonDamagesBothTeamsAndPreviewMatchesResolution()
        {
            var s=SharedRules.Create(17,true);s.Find("patrol").Hp=0;s.Find("guard").Hp=0;
            s.Find("crossbow").Position=new SPos(2,5);s.Find("archer").Position=new SPos(3,5);
            var preview=SharedRules.EnemyPreview(s);Assert.AreEqual(1,preview.After.Find("crossbow").Hp);Assert.AreEqual(2,preview.After.Find("archer").Hp);
            Assert.AreEqual(6,s.Find("archer").Hp);Assert.IsTrue(SharedRules.EndTurn(s,new string[0]).Valid);
            CollectionAssert.AreEqual(preview.After.Units.Select(u=>u.Hp),s.Units.Select(u=>u.Hp));
            CollectionAssert.AreEqual(preview.After.Telegraph,s.Telegraph);
        }
        [Test] public void ForecastTieUsesLowestHpThenTopLeftAndStaysLocked()
        {
            var s=SharedRules.Create(17,true);s.Find("patrol").Hp=0;s.Find("crossbow").Hp=0;
            s.Find("mage").Position=new SPos(6,0);s.Find("mage").Hp=1;
            var next=SharedRules.EnemyPreview(s).After;
            // Two units on row 7 beat the single wounded unit on row 1.
            CollectionAssert.AreEqual(new[]{new SPos(1,6),new SPos(2,6),new SPos(3,6)},next.Telegraph);
            s.Find("archer").Position=new SPos(0,2);
            next=SharedRules.EnemyPreview(s).After;
            CollectionAssert.AreEqual(new[]{new SPos(4,0),new SPos(5,0),new SPos(6,0)},next.Telegraph);
            var locked=s.Telegraph.ToArray();Step(s,"vanguard",4,6);CollectionAssert.AreEqual(locked,s.Telegraph);
        }
        [Test] public void BraceExtendsRangeAndExpiresAtEndOfNextPlayerTurn()
        {
            var s=SharedRules.Create(17,true);s.Find("archer").Position=new SPos(6,6);s.Find("patrol").Position=new SPos(6,2);
            var attack=Card(s,"archer",SCard.Attack,"patrol");Assert.IsFalse(SharedRules.Plan(s,attack).Valid);
            Do(s,Card(s,"archer",SCard.Skill));Assert.IsTrue(SharedRules.Plan(s,attack).Valid);
            Assert.IsTrue(SharedRules.EndTurn(s,new string[0]).Valid);Assert.IsTrue(s.Find("archer").Braced);
            Assert.IsTrue(SharedRules.EndTurn(s,new string[0]).Valid);Assert.IsFalse(s.Find("archer").Braced);
        }
        [Test] public void PushMovesStraightAwayAndCannotMoveCannon()
        {
            var s=SharedRules.Create(17,true);s.Find("vanguard").Position=new SPos(4,2);
            Do(s,Card(s,"vanguard",SCard.Skill,"guard"));Assert.AreEqual(new SPos(2,2),s.Find("guard").Position);Assert.AreEqual(2,s.Find("vanguard").Shield);
            s.Find("vanguard").Position=new SPos(4,1);var pos=s.Find("cannon").Position;
            Do(s,Card(s,"vanguard",SCard.Skill,"cannon"));Assert.AreEqual(pos,s.Find("cannon").Position);
        }
        [Test] public void MageBurstCanTargetProtectedCannonWithoutMovingAnyUnit()
        {
            var s=SharedRules.Create(17,true);s.Obstacles.Clear();s.Find("mage").Position=new SPos(4,2);
            var before=s.Find("cannon").Hp;var guardBefore=s.Find("guard").Hp;
            var mageBefore=s.Find("mage").Position;
            Do(s,Card(s,"mage",SCard.Skill,"cannon"));
            Assert.AreEqual(before,s.Find("cannon").Hp,"An adjacent guard reduces the cannon's burst damage to zero.");
            Assert.AreEqual(guardBefore-2,s.Find("guard").Hp,"The visible guard in the cannon's four-neighborhood takes two damage.");
            Assert.AreEqual(mageBefore,s.Find("mage").Position,"Starfire does not move either caster or targets.");
        }
        [Test] public void CornerTouchDoesNotBlockButCrossingWallDoes()
        {
            var s=SharedRules.Create(17,true);
            Assert.IsTrue(SharedRules.HasSight(s,new SPos(3,5),new SPos(2,4)));
            Assert.IsFalse(SharedRules.HasSight(s,new SPos(3,5),new SPos(3,3)));
        }
        [Test] public void DeckReshuffleConservesAll18EntitiesAndDifferentSeedsVary()
        {
            var s=SharedRules.Create(17,true);s.Find("guard").Hp=100;s.Find("guard").MaxHp=100;
            foreach(var u in s.Units.Where(u=>u.Player)){u.Hp=100;u.MaxHp=100;}
            for(int i=0;i<8;i++)
            {
                var kept=s.Hand.Take(2).Select(c=>c.Id).ToArray();var discard=s.Hand.Where(c=>!kept.Contains(c.Id)).Select(c=>c.Id).ToArray();Assert.IsTrue(SharedRules.EndTurn(s,discard).Valid);
                var all=s.Hand.Concat(s.Deck).Concat(s.Discard).Select(c=>c.Id).ToArray();Assert.AreEqual(18,all.Length);Assert.AreEqual(18,all.Distinct().Count());
                Assert.IsTrue(kept.All(id=>s.Hand.Any(c=>c.Id==id)));
            }
            Assert.AreNotEqual(string.Join(",",SharedRules.Create(17,true).Deck.Select(c=>c.Id)),string.Join(",",SharedRules.Create(18,true).Deck.Select(c=>c.Id)));
        }
        [Test] public void DestroyingCannonPreventsAnyFurtherEnemyAction()
        {
            var s=SharedRules.Create(17,true);s.Find("guard").Hp=0;s.Find("cannon").Hp=1;s.Find("archer").Position=new SPos(4,1);
            Do(s,Card(s,"archer",SCard.Attack,"cannon"));Assert.AreEqual(SPhase.Won,s.Phase);
            var hp=s.Units.Select(u=>u.Hp).ToArray();Assert.IsFalse(SharedRules.EndTurn(s,new string[0]).Valid);
            CollectionAssert.AreEqual(hp,SharedRules.EnemyPreview(s).After.Units.Select(u=>u.Hp));
        }
        [Test] public void AllPlayersDownEndsInLossAndInvalidDiscardDoesNotAdvance()
        {
            var s=SharedRules.Create(17,true);Assert.IsFalse(SharedRules.EndTurn(s,new[]{"missing"}).Valid);Assert.AreEqual(1,s.Round);
            s.Find("vanguard").Hp=0;s.Find("mage").Hp=0;s.Find("archer").Hp=1;s.Find("archer").Position=new SPos(2,5);
            Assert.IsTrue(SharedRules.EndTurn(s,new string[0]).Valid);Assert.AreEqual(SPhase.Lost,s.Phase);Assert.AreEqual(1,s.Round);
        }
    }
}
