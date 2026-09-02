using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

namespace TcgEngine.UI
{
    /// <summary>
    /// Main UI script for all the game scene UI
    /// 所有游戏场景UI的主UI脚本
    /// </summary>

    public class GameUI : MonoBehaviour
    {
        public Canvas game_canvas;
        public Canvas panel_canvas;
        public Canvas top_canvas;
        public UIPanel menu_panel;
        public Text quit_btn;

        [Header("Turn Area")]
        public Text turn_count;
        public Text turn_timer;
        public Button end_turn_button;
        public Animator timeout_animator;
        public AudioClip timeout_audio;

        private float selector_timer = 0f;
        private float end_turn_timer = 0f;
        private int prev_time_val = 0;
        private Text end_turn_button_text;

        private static GameUI instance;

        void Awake()
        {
            instance = this;

            if (game_canvas.worldCamera == null)
                game_canvas.worldCamera = Camera.main;
            if (panel_canvas.worldCamera == null)
                panel_canvas.worldCamera = Camera.main;
            if (top_canvas.worldCamera == null)
                top_canvas.worldCamera = Camera.main;
        }

        private void Start()
        {
            GameClient.Get().onGameStart += OnGameStart;
            GameClient.Get().onNewTurn += OnNewTurn;
            LoadPanel.Get().Show(true);
            BlackPanel.Get().Show(true);
            BlackPanel.Get().Hide();

            if (quit_btn != null)
                quit_btn.text = GameClient.game_settings.IsOnlinePlayer() ? "Resign" : "Quit";
        }

        void Update()
        {
            Game data = GameClient.Get().GetGameData();
			bool is_connecting = data == null || data.state == GameState.Connecting;
            bool connection_lost = !is_connecting && !GameClient.Get().IsReady();
            ConnectionPanel.Get().SetVisible(connection_lost);

            //Menu
            if (Input.GetKeyDown(KeyCode.Escape))
                menu_panel.Toggle();

            if (!GameClient.Get().IsReady())
                return;

            bool yourturn = GameClient.Get().IsYourTurn();
            bool endDiscardPhase = data.phase == GamePhase.EndDiscard;
            Player localPlayer = GameClient.Get().GetPlayer();
            bool canEndPhase = endDiscardPhase
                ? (localPlayer != null && !localPlayer.end_discard_passed)
                : yourturn;
            if (data != null && data.selector != SelectorType.None && data.selector_player_id == GameClient.Get().GetPlayerID()
                && Input.GetMouseButtonDown(1) && !IsOverUI())
            {
                GameClient.Get().CancelSelection();
            }

            LoadPanel.Get().SetVisible(is_connecting && !data.HasStarted());
            end_turn_button.interactable = canEndPhase && end_turn_timer > 1f;
            if (end_turn_button_text == null && end_turn_button != null)
                end_turn_button_text = end_turn_button.GetComponentInChildren<Text>(true);
            if (end_turn_button_text != null)
                end_turn_button_text.text = GetVc5EndButtonText(data.phase);
            end_turn_timer += Time.deltaTime;
            selector_timer += Time.deltaTime;

            // VC5: no countdown timer; use the turn area to show whose turn it is.
            turn_count.text = GetVc5TurnStatusText(
                data.phase, yourturn, localPlayer != null && localPlayer.end_discard_passed);
            if (turn_timer != null)
            {
                turn_timer.enabled = true;
                turn_timer.text = GetVc5TurnDetailText(
                    data.phase, localPlayer != null && localPlayer.end_discard_passed, data.turn_count);
            }

            //Show selector panels
            foreach (SelectorPanel panel in SelectorPanel.GetAll())
            {
                bool should_show = panel.ShouldShow();
                if (should_show != panel.IsVisible() && selector_timer > 1f)
                {
                    selector_timer = 0f;
                    panel.SetVisible(should_show);

                    if (should_show)
                    {
                        AbilityData ability = AbilityData.Get(data.selector_ability_id);
                        Card caster = data.GetCard(data.selector_caster_uid);
                        panel.Show(ability, caster);
                    }
                }
            }

            //Hide
            if (!canEndPhase && !yourturn)
            {
                SelectorPanel.HideAll();
            }

        }

        private void PulseFX()
        {
            timeout_animator?.SetTrigger("pulse");
            AudioTool.Get().PlaySFX("time", timeout_audio, 1f);
        }

        private void OnGameStart()
        {
            if (top_canvas != null)
                Vc5C3PreviewOverlay.Show(top_canvas.transform);
            if (top_canvas != null && Vc5DemoTutorialOverlay.IsCurrentDemoSoloMatch())
            {
                Vc5DemoBattleFeedback.Show(top_canvas.transform);
            }
            if (top_canvas != null && Vc5DemoTutorialOverlay.ShouldShowCurrentMatch())
            {
                Vc5DemoTutorialOverlay.Show(top_canvas.transform);
            }
        }

        private void OnNewTurn(int player_id)
        {
            CardSelector.Get().Hide();
            SelectTargetUI.Get().Hide();
        }

        public void OnClickNextStage()
        {
            GameClient.Get().EndStage();
            end_turn_timer = 0f; //立即禁用按钮（不要等待刷新）
        }
        public void OnClickNextTurn()
        {
            OnClickNextStage();
        }

        public static string GetVc5EndButtonText(GamePhase phase)
        {
            return phase == GamePhase.EndDiscard ? "完成弃牌" : "放弃行动";
        }

        public static string GetVc5TurnStatusText(GamePhase phase, bool yourTurn, bool localEndDiscardPassed)
        {
            if (phase == GamePhase.EndDiscard)
                return localEndDiscardPassed ? "等待对手" : "弃牌阶段";
            if (phase == GamePhase.Scoring)
                return "得分结算中";
            if (phase == GamePhase.EndTurn)
                return "回合结算中";
            return yourTurn ? "我方行动" : "对手行动";
        }

        public static string GetVc5TurnDetailText(GamePhase phase, bool localEndDiscardPassed, int turnCount)
        {
            if (phase == GamePhase.EndDiscard)
                return localEndDiscardPassed ? "弃牌已完成" : "拖动手牌后松开即可弃置";
            return "第 " + turnCount.ToString() + " 回合";
        }

        public void OnClickRestart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void OnClickMenu()
        {
            menu_panel.Show();
        }

        public void OnClickBack()
        {
            menu_panel.Hide();
        }

        public void OnClickQuit()
        {
            bool online = GameClient.game_settings.IsOnlinePlayer();
            bool ended = GameClient.Get().HasEnded();
            if (online && !ended)
                GameClient.Get().Resign();
            else
                StartCoroutine(QuitRoutine("Menu"));
            menu_panel.Hide();
        }

        private IEnumerator QuitRoutine(string scene)
        {
            BlackPanel.Get().Show();
            AudioTool.Get().FadeOutMusic("music");
            AudioTool.Get().FadeOutSFX("ambience");
            AudioTool.Get().FadeOutSFX("ending_sfx");

            yield return new WaitForSeconds(1f);

            GameClient.Get().Disconnect();
            SceneNav.GoTo(scene);
        }

        public void OnClickSwapObserve()
        {
            int other = GameClient.Get().GetPlayerID() == 0 ? 1 : 0;
            GameClient.Get().SetObserverMode(other);
        }

        public static bool IsUIOpened()
        {
            return CardSelector.Get().IsVisible() || EndGamePanel.Get().IsVisible();
        }

        public static bool IsOverUI()
        {
            //return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
            return results.Count > 0;
        }

        public static bool IsOverUILayer(string sorting_layer)
        {
            return IsOverUILayer(SortingLayer.NameToID(sorting_layer));
        }

        public static bool IsOverUILayer(int sorting_layer)
        {
            //return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
            int count = 0;
            foreach (RaycastResult result in results)
            {
                if (result.sortingLayer == sorting_layer)
                    count++;
            }
            return count > 0;
        }

        public static bool IsOverRectTransform(Canvas canvas, RectTransform rect)
        {
            PointerEventData pevent = new PointerEventData(EventSystem.current);
            pevent.position = Input.mousePosition;

            List<RaycastResult> results = new List<RaycastResult>();
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            raycaster.Raycast(pevent, results);

            foreach (RaycastResult result in results)
            {
                if (result.gameObject.transform == rect || result.gameObject.transform.IsChildOf(rect))
                    return true;
            }
            return false;
        }

        public static Vector2 MouseToRectPos(Canvas canvas, RectTransform rect, Vector2 screen_pos)
        {
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && canvas.worldCamera != null)
            {
                Vector2 anchor_pos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screen_pos, canvas.worldCamera, out anchor_pos);
                return anchor_pos;
            }
            else
            {
                Vector2 anchor_pos = screen_pos - new Vector2(rect.position.x, rect.position.y);
                anchor_pos = new Vector2(anchor_pos.x / rect.lossyScale.x, anchor_pos.y / rect.lossyScale.y);
                return anchor_pos;
            }
        }

        public static Vector3 MouseToWorld(Vector2 mouse_pos, float distance = 10f)
        {
            Camera cam = GameCamera.Get() != null ? GameCamera.GetCamera() : Camera.main;
            Vector3 wpos = cam.ScreenToWorldPoint(new Vector3(mouse_pos.x, mouse_pos.y, distance));
            return wpos;
        }

        public static string FormatNumber(int value)
        {
            return string.Format("{0:#,0}", value);
        }

        public static GameUI Get()
        {
            return instance;
        }
    }
}
