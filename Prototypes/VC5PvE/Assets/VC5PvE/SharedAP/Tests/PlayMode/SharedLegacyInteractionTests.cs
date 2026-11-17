using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace VC5PvE.SharedAP.Tests
{
    public class SharedLegacyInteractionTests
    {
        GameObject host; SharedBattleController flow;
        [UnitySetUp] public IEnumerator SetUp()
        {
            yield return EmptyScene();
            host=new GameObject("Legacy presentation fixture");flow=host.AddComponent<SharedBattleController>();flow.InitializeForTests(17);flow.SkipTutorial();flow.BuildPresentation();
        }
        internal static IEnumerator EmptyScene()
        {
            var previous=Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Select(UnityEngine.SceneManagement.SceneManager.GetSceneAt).ToArray();
            var scene=UnityEngine.SceneManagement.SceneManager.CreateScene("Shared isolated "+System.Guid.NewGuid().ToString("N"));
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
            // Keep the Unity Test Runner host alive while isolating game objects.
            foreach(var old in previous)
            {
                foreach(var root in old.GetRootGameObjects())
                {
                    bool runner=root.GetComponentsInChildren<MonoBehaviour>(true).Any(c=>c!=null&&c.GetType().Assembly.GetName().Name.Contains("TestRunner"));
                    if(runner)UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
                }
                yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(old);
            }
        }
        [TearDown] public void TearDown(){Object.DestroyImmediate(host);}
        [UnityTest] public IEnumerator UsesOriginalBoardAndLivePreview()
        {
            yield return null;
            Assert.IsNotNull(host.GetComponentInChildren<BoardView>());
            Assert.IsNotNull(host.GetComponentInChildren<ActionPreviewOverlay>());
            Assert.IsNull(host.GetComponentInChildren<SharedBoardView>());
        }
        [UnityTest] public IEnumerator EndTurnFirstOpensDiscardSelectionThenConfirmStartsResolution()
        {
            yield return null;flow.BeginEndTurn();
            Assert.IsFalse(flow.Busy,"First end-turn interaction only opens discard selection.");
            Assert.AreEqual(SharedInputStage.Retain,flow.Stage);
            flow.ConfirmEndTurn();
            Assert.IsTrue(flow.Busy,"End must start resolution, rather than silently enter retention selection");
            Assert.AreEqual(SPhase.Enemy,flow.State.Phase);
            float deadline=Time.realtimeSinceStartup+15f;
            while(flow.Busy&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsFalse(flow.Busy);Assert.AreEqual(2,flow.State.Round);Assert.AreEqual(6,flow.State.Ap);
        }
        [UnityTest] public IEnumerator SelectedChargeHasClickableLandingAndEnemyStage()
        {
            yield return null;flow.ChooseUnit("vanguard");flow.ChooseBasic(SActionKind.ShortStep);flow.ClickCell(new SPos(3,5));
            while(flow.Busy)yield return null;
            var select=typeof(SharedBattleController).GetMethod("SelectCard");Assert.IsNotNull(select);
            var charge=flow.State.Hand.First(c=>c.Kind==SCard.Charge).Id;
            select.Invoke(flow,new object[]{charge});
            flow.ChooseUnit("vanguard");
            Assert.AreEqual(SharedInputStage.Landing,flow.Stage);
            var click=typeof(SharedBattleController).GetMethod("ClickScreen");Assert.IsNotNull(click);
            var board=host.GetComponentInChildren<BoardView>();var landing=flow.ViewCamera.WorldToScreenPoint(board.World(new GridPos(2,4)));
            click.Invoke(flow,new object[]{(Vector2)landing});
            Assert.AreEqual(SharedInputStage.ChargeTarget,flow.Stage);Assert.AreEqual(new SPos(3,5),flow.State.Find("vanguard").Position);
            var target=flow.ViewCamera.WorldToScreenPoint(board.World(new GridPos(2,3)));
            click.Invoke(flow,new object[]{(Vector2)target});while(flow.Busy)yield return null;
            Assert.AreEqual(new SPos(2,4),flow.State.Find("vanguard").Position);Assert.AreEqual(4,flow.State.Ap);Assert.AreEqual(1,flow.State.Find("patrol").Hp);
        }
        [UnityTest] public IEnumerator RoleWeaponsAndCardDifferencesAreVisible()
        {
            yield return null;
            var text=string.Join("\n",host.GetComponentsInChildren<UnityEngine.UI.Text>().Select(t=>t.text));
            StringAssert.Contains("射程",text);StringAssert.Contains("溅射",text);
            flow.ChooseUnit("archer");
            text=string.Join("\n",host.GetComponentsInChildren<UnityEngine.UI.Text>().Select(t=>t.text));
            StringAssert.Contains("战技",text);
            var select=typeof(SharedBattleController).GetMethod("SelectCard");Assert.IsNotNull(select);
            select.Invoke(flow,new object[]{flow.State.Hand.First(c=>c.Kind==SCard.Attack).Id});
            text=string.Join("\n",host.GetComponentsInChildren<UnityEngine.UI.Text>().Select(t=>t.text));
            StringAssert.Contains("先锋",text);StringAssert.Contains("射手",text);StringAssert.Contains("术师",text);
            StringAssert.Contains("选择可执行队员",text);
        }
    }
}
