using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VC5PvE.Editor
{
    /// <summary>Creates a standalone visual review scene without touching the production scenes.</summary>
    public static class CampaignReviewBuilder
    {
        private const string TitleScenePath="Assets/VC5PvE/Scenes/Title.unity";
        private const string ReviewScenePath="Assets/VC5PvE/Scenes/CampaignReview.unity";

        [MenuItem("VC5 PvE/Review Campaign Camp")]
        public static void BuildReviewScene()
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var title=EditorSceneManager.OpenScene(TitleScenePath,OpenSceneMode.Single);
            var flow=UnityEngine.Object.FindObjectOfType<PrototypeFlow>();
            if(flow==null)throw new InvalidOperationException("Title scene does not contain PrototypeFlow.");
            Font font=flow.UiFont!=null?flow.UiFont:Resources.GetBuiltinResource<Font>("Arial.ttf");
            Sprite[] portraits=flow.UnitSprites;

            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            scene.name="CampaignReview";
            BuildCanvas(font,portraits);
            EditorSceneManager.SaveScene(scene,ReviewScenePath);
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(ReviewScenePath,OpenSceneMode.Single);
            Debug.Log("Campaign camp review scene created at "+ReviewScenePath+". Press Play to switch between the three review states.");
        }

        private static void BuildCanvas(Font font,Sprite[] portraits)
        {
            var canvasObject=new GameObject("Campaign review canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var eventSystem=new GameObject("Event System",typeof(EventSystem),typeof(StandaloneInputModule));
            var viewObject=new GameObject("Campaign Camp Review",typeof(RectTransform),typeof(CampaignCampView));
            viewObject.transform.SetParent(canvasObject.transform,false);
            var view=viewObject.GetComponent<CampaignCampView>();
            view.EditorReviewOnPlay=true;
            view.EditorReviewSeed=20261021;
            view.EditorReviewFont=font;
            view.EditorReviewPortraits=portraits;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }
}
