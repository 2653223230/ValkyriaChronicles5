using System.Collections;
using System.Text;
using TcgEngine.Client;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>Read-only presentation for VC5 Demo battle events and scoring state.</summary>
    public class Vc5DemoBattleFeedback : MonoBehaviour
    {
        public const string RootName = "VC5 Demo Battle Feedback";
        private static Vc5DemoBattleFeedback instance;

        public static void SetCardPreviewVisible(bool visible)
        {
            if (instance == null) return;
            if (instance.scoreText != null) instance.scoreText.transform.parent.gameObject.SetActive(!visible);
            if (instance.logText != null) instance.logText.transform.parent.gameObject.SetActive(!visible);
        }

        private static readonly Color PanelColor = new Color(0.035f, 0.055f, 0.07f, 0.91f);
        private static readonly Color CyanColor = new Color(0.12f, 0.92f, 0.88f, 1f);
        private static readonly Color YellowColor = new Color(1f, 0.77f, 0.16f, 1f);
        private static readonly Color MutedColor = new Color(0.78f, 0.86f, 0.89f, 1f);

        private readonly Vc5DemoBattleFeedbackModel model = new Vc5DemoBattleFeedbackModel(3);
        private Font uiFont;
        private Text scoreText;
        private Text bannerText;
        private Text logText;
        private Text toastText;
        private Image bannerPanel;
        private Image toastPanel;
        private RectTransform actorHighlight;
        private RectTransform targetHighlight;
        private Coroutine bannerRoutine;
        private Coroutine toastRoutine;
        private int lastLocalScore;
        private int lastOpponentScore;
        private int previousActionPlayerId = -1;
        private GamePhase lastPhase = GamePhase.None;
        private bool initializedScores;

        public static Vc5DemoBattleFeedback Show(Transform parent)
        {
            Transform existing = parent.Find(RootName);
            if (existing != null)
                return existing.GetComponent<Vc5DemoBattleFeedback>();

            GameObject root = new GameObject(RootName, typeof(RectTransform));
            root.layer = parent.gameObject.layer;
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Vc5DemoBattleFeedback feedback = root.AddComponent<Vc5DemoBattleFeedback>();
            instance = feedback;
            feedback.Build();
            feedback.Subscribe();
            feedback.RefreshScore(true);
            return feedback;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
            Unsubscribe();
        }

        private void Update()
        {
            GameClient client = GameClient.Get();
            if (client == null || !client.IsReady())
                return;

            RefreshScore(false);
            Game data = client.GetGameData();
            if (data != null && data.phase != lastPhase)
            {
                if (data.phase == GamePhase.Scoring)
                    ShowScoringToast(data);
                lastPhase = data.phase;
            }
        }

        private void Build()
        {
            uiFont = FindFont();
            Transform scorePanel = CreatePanel("ScorePanel", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(72f, -72f), new Vector2(430f, 122f), CyanColor);
            scoreText = CreateText("ScoreText", scorePanel, "", 22, FontStyle.Bold,
                new Vector2(14f, -8f), new Vector2(402f, 106f), Color.white, TextAnchor.UpperLeft);

            bannerPanel = CreatePanel("ActionBanner", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -48f), new Vector2(700f, 88f), YellowColor).GetComponent<Image>();
            bannerText = CreateText("ActionBannerText", bannerPanel.transform, "", 22, FontStyle.Bold,
                Vector2.zero, new Vector2(670f, 80f), Color.white, TextAnchor.MiddleCenter);
            bannerPanel.gameObject.SetActive(false);

            Transform logPanel = CreatePanel("ActionLog", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(72f, 10f), new Vector2(430f, 170f), new Color(0.28f, 0.42f, 0.48f, 0.7f));
            CreateText("ActionLogTitle", logPanel, "最近行动", 19, FontStyle.Bold,
                new Vector2(0f, 57f), new Vector2(402f, 30f), CyanColor, TextAnchor.MiddleLeft);
            logText = CreateText("ActionLogText", logPanel, "等待行动...", 18, FontStyle.Normal,
                new Vector2(0f, -18f), new Vector2(402f, 104f), MutedColor, TextAnchor.UpperLeft);

            toastPanel = CreatePanel("ScoreToast", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(680f, 104f), YellowColor).GetComponent<Image>();
            toastText = CreateText("ScoreToastText", toastPanel.transform, "", 28, FontStyle.Bold,
                Vector2.zero, new Vector2(650f, 94f), Color.white, TextAnchor.MiddleCenter);
            toastPanel.gameObject.SetActive(false);

            actorHighlight = CreateHighlight("ActorHighlight", CyanColor);
            targetHighlight = CreateHighlight("TargetHighlight", YellowColor);
        }

        private void Subscribe()
        {
            GameClient client = GameClient.Get();
            if (client == null)
                return;
            client.onNewTurn += OnNewTurn;
            client.onCardPlayed += OnCardPlayed;
            client.onCardMoved += OnCardMoved;
            client.onAbilityStart += OnAbilityStart;
            client.onAbilityTargetCard += OnAbilityTargetCard;
            client.onAbilityTargetSlot += OnAbilityTargetSlot;
            client.onAttackStart += OnAttackStart;
            client.onCardDamaged += OnCardDamaged;
            client.onCardHealed += OnCardHealed;
            client.onCardDiscarded += OnCardDiscarded;
            client.onGameEnd += OnGameEnd;
            client.onRefreshAll += OnRefreshAll;
        }

        private void Unsubscribe()
        {
            GameClient client = GameClient.Get();
            if (client == null)
                return;
            client.onNewTurn -= OnNewTurn;
            client.onCardPlayed -= OnCardPlayed;
            client.onCardMoved -= OnCardMoved;
            client.onAbilityStart -= OnAbilityStart;
            client.onAbilityTargetCard -= OnAbilityTargetCard;
            client.onAbilityTargetSlot -= OnAbilityTargetSlot;
            client.onAttackStart -= OnAttackStart;
            client.onCardDamaged -= OnCardDamaged;
            client.onCardHealed -= OnCardHealed;
            client.onCardDiscarded -= OnCardDiscarded;
            client.onGameEnd -= OnGameEnd;
            client.onRefreshAll -= OnRefreshAll;
        }

        private void OnNewTurn(int playerId)
        {
            int localId = GameClient.Get().GetPlayerID();
            bool aiTurn = playerId != localId;
            bool aiJustPassed = previousActionPlayerId >= 0
                && previousActionPlayerId != localId && playerId == localId;
            string message = aiJustPassed
                ? "AI 放弃行动  ·  轮到我方"
                : aiTurn ? "AI 行动中" : "轮到我方行动";
            ShowBanner(message, aiTurn ? YellowColor : CyanColor, 1.5f);
            AddAction(aiJustPassed ? "AI 放弃行动" : aiTurn ? "AI 开始行动" : "我方开始行动");
            previousActionPlayerId = playerId;
        }

        private void OnCardPlayed(Card card, Slot slot)
        {
            if (!IsAiCard(card))
                return;
            CardData data = card.CardData;
            string message = Vc5DemoBattleFeedbackModel.BuildAiCardMessage(
                data != null ? data.GetTitle() : "未知卡牌", card.GetMana());
            string summary = data != null
                ? Vc5DemoBattleFeedbackModel.BuildShortDescription(data.text)
                : "";
            if (!string.IsNullOrEmpty(summary))
                message += "\n" + summary;
            ShowBanner(message, YellowColor, 2.2f);
            AddAction(message);
            Highlight(card, actorHighlight);
        }

        private void OnCardMoved(Card card, Slot slot)
        {
            if (!IsAiCard(card))
                return;
            string message = "AI 移动【" + CardTitle(card) + "】";
            ShowBanner(message, YellowColor, 1.8f);
            AddAction(message);
            Highlight(card, actorHighlight);
        }

        private void OnAbilityStart(AbilityData ability, Card caster)
        {
            if (!IsAiCard(caster) || ability == null || ability.trigger != AbilityTrigger.Activate)
                return;
            string message = "AI 使用技能【" + ability.GetTitle() + "】";
            ShowBanner(message, YellowColor, 2f);
            AddAction(message);
            Highlight(caster, actorHighlight);
        }

        private void OnAbilityTargetCard(AbilityData ability, Card caster, Card target)
        {
            if (!IsAiCard(caster) || target == null)
                return;
            AddAction("目标：【" + CardTitle(target) + "】");
            Highlight(caster, actorHighlight);
            Highlight(target, targetHighlight);
        }

        private void OnAbilityTargetSlot(AbilityData ability, Card caster, Slot target)
        {
            if (!IsAiCard(caster))
                return;
            AddAction("目标：棋盘格（" + target.x + "," + target.y + "）");
            Highlight(caster, actorHighlight);
            HighlightSlot(target, targetHighlight);
        }

        private void OnAttackStart(Card attacker, Card target)
        {
            if (!IsAiCard(attacker) || target == null)
                return;
            string message = "AI：【" + CardTitle(attacker) + "】攻击【" + CardTitle(target) + "】";
            ShowBanner(message, YellowColor, 2f);
            AddAction(message);
            Highlight(attacker, actorHighlight);
            Highlight(target, targetHighlight);
        }

        private void OnCardDamaged(Card card, int value)
        {
            if (card == null || value <= 0)
                return;
            AddAction("【" + CardTitle(card) + "】受到 " + value + " 点伤害");
        }

        private void OnCardHealed(Card card, int value)
        {
            if (card == null || value <= 0)
                return;
            AddAction("【" + CardTitle(card) + "】恢复 " + value + " 点生命");
        }

        private void OnCardDiscarded(Card card)
        {
            Game data = GameClient.Get().GetGameData();
            bool wasBoardCharacter = card != null && card.CardData != null
                && card.CardData.IsCharacter() && data != null && data.IsOnBoard(card);
            if (!wasBoardCharacter)
                return;
            string side = card.player_id == GameClient.Get().GetPlayerID() ? "我方" : "AI";
            string scorer = side == "我方" ? "AI" : "我方";
            AddAction(side + "【" + CardTitle(card) + "】被消灭，" + scorer + " +3 分");
            ShowToast(scorer + " 击杀得分  +3", 2.2f);
        }

        private void OnRefreshAll()
        {
            RefreshScore(false);
        }

        private void OnGameEnd(int winnerId)
        {
            StartCoroutine(ShowEndAfterRefresh(winnerId));
        }

        private IEnumerator ShowEndAfterRefresh(int winnerId)
        {
            yield return new WaitForSeconds(0.15f);
            Game data = GameClient.Get().GetGameData();
            if (data == null)
                yield break;
            int localId = GameClient.Get().GetPlayerID();
            Player local = data.GetPlayer(localId);
            Player opponent = data.GetPlayer((localId + 1) % data.players.Length);
            if (local == null || opponent == null)
                yield break;
            string reason = Vc5DemoBattleFeedbackModel.BuildEndReason(
                winnerId == localId, local.kill_count, opponent.kill_count,
                local.cards_deck.Count, opponent.cards_deck.Count);
            ShowToast(reason + "\n最终比分  " + local.kill_count + " : " + opponent.kill_count, 5f);
            AddAction(reason);
        }

        private void RefreshScore(bool force)
        {
            GameClient client = GameClient.Get();
            Game data = client != null ? client.GetGameData() : null;
            if (data == null || data.players == null || data.players.Length < 2)
                return;
            int localId = client.GetPlayerID();
            Player local = data.GetPlayer(localId);
            Player opponent = data.GetPlayer((localId + 1) % data.players.Length);
            if (local == null || opponent == null)
                return;

            int localCount = Vc5ScoringZone.CountCharactersInZone(local);
            int opponentCount = Vc5ScoringZone.CountCharactersInZone(opponent);
            scoreText.text = Vc5DemoBattleFeedbackModel.BuildScoreSummary(
                local.kill_count, opponent.kill_count, localCount, opponentCount);

            if (initializedScores && !force
                && (local.kill_count != lastLocalScore || opponent.kill_count != lastOpponentScore))
            {
                int localDelta = local.kill_count - lastLocalScore;
                int opponentDelta = opponent.kill_count - lastOpponentScore;
                string message = Vc5DemoBattleFeedbackModel.BuildScoreChangeMessage(
                    localDelta, opponentDelta);
                ShowToast(message + "\n当前比分  " + local.kill_count + " : " + opponent.kill_count, 2.4f);
                AddAction(message);
            }
            lastLocalScore = local.kill_count;
            lastOpponentScore = opponent.kill_count;
            initializedScores = true;
        }

        private void ShowScoringToast(Game data)
        {
            int localId = GameClient.Get().GetPlayerID();
            Player local = data.GetPlayer(localId);
            Player opponent = data.GetPlayer((localId + 1) % data.players.Length);
            int localCount = Vc5ScoringZone.CountCharactersInZone(local);
            int opponentCount = Vc5ScoringZone.CountCharactersInZone(opponent);
            Vc5DemoBattleFeedbackModel.GetScoreAwards(localCount, opponentCount,
                out int localAward, out int opponentAward);
            ShowToast("得分阶段\n得分区人数 " + localCount + " : " + opponentCount
                + "   本次 +" + localAward + " : +" + opponentAward, 3f);
        }

        private void AddAction(string message)
        {
            model.AddAction(message);
            StringBuilder builder = new StringBuilder();
            foreach (string action in model.GetActions())
                builder.Append("• ").Append(action).Append('\n');
            logText.text = builder.ToString().TrimEnd();
        }

        private void ShowBanner(string message, Color borderColor, float duration)
        {
            bannerText.text = message;
            Outline outline = bannerPanel.GetComponent<Outline>();
            if (outline != null)
                outline.effectColor = borderColor;
            bannerPanel.gameObject.SetActive(true);
            if (bannerRoutine != null)
                StopCoroutine(bannerRoutine);
            bannerRoutine = StartCoroutine(HideAfter(bannerPanel.gameObject, duration));
        }

        private void ShowToast(string message, float duration)
        {
            toastText.text = message;
            toastPanel.gameObject.SetActive(true);
            if (toastRoutine != null)
                StopCoroutine(toastRoutine);
            toastRoutine = StartCoroutine(HideAfter(toastPanel.gameObject, duration));
        }

        private IEnumerator HideAfter(GameObject target, float duration)
        {
            yield return new WaitForSeconds(duration);
            target.SetActive(false);
        }

        private void Highlight(Card card, RectTransform marker)
        {
            if (card == null || marker == null)
                return;
            BoardCard boardCard = BoardCard.Get(card.uid);
            Camera camera = GameCamera.Get() != null ? GameCamera.GetCamera() : Camera.main;
            if (boardCard == null || camera == null)
                return;
            Vector2 screenPoint = camera.WorldToScreenPoint(boardCard.transform.position);
            ShowHighlightAtScreenPoint(screenPoint, marker);
        }

        private void HighlightSlot(Slot slot, RectTransform marker)
        {
            BSlot boardSlot = BSlot.Get(slot);
            Vector3 worldPosition;
            if (boardSlot != null)
            {
                worldPosition = boardSlot.GetPosition(slot);
            }
            else
            {
                int localId = GameClient.Get().GetPlayerID();
                Slot localEquivalent = new Slot(slot.x, slot.y, Slot.GetP(localId));
                BSlot localSlot = BSlot.Get(localEquivalent);
                if (localSlot == null)
                    return;
                Vector3 selfPosition = localSlot.GetPosition(localEquivalent);
                Vector3 center = GameBoard.Get() != null ? GameBoard.Get().transform.position : Vector3.zero;
                worldPosition = new Vector3(
                    center.x * 2f - selfPosition.x,
                    center.y * 2f - selfPosition.y,
                    selfPosition.z);
            }

            Camera camera = GameCamera.Get() != null ? GameCamera.GetCamera() : Camera.main;
            if (camera != null)
                ShowHighlightAtScreenPoint(camera.WorldToScreenPoint(worldPosition), marker);
        }

        private void ShowHighlightAtScreenPoint(Vector2 screenPoint, RectTransform marker)
        {
            RectTransform root = transform as RectTransform;
            Canvas canvas = GetComponentInParent<Canvas>();
            Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                root, screenPoint, uiCamera, out Vector2 localPoint);
            marker.anchoredPosition = localPoint;
            marker.gameObject.SetActive(true);
            StartCoroutine(HideAfter(marker.gameObject, 1.8f));
        }

        private bool IsAiCard(Card card)
        {
            return card != null && card.player_id != GameClient.Get().GetPlayerID();
        }

        private static string CardTitle(Card card)
        {
            return card != null && card.CardData != null ? card.CardData.GetTitle() : "未知单位";
        }

        private Transform CreatePanel(string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 position, Vector2 size, Color borderColor)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline));
            obj.layer = gameObject.layer;
            obj.transform.SetParent(transform, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(anchorMin.x, anchorMin.y);
            if (anchorMin.x == 0.5f)
                rect.pivot = new Vector2(0.5f, anchorMin.y);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = obj.GetComponent<Image>();
            image.color = PanelColor;
            image.raycastTarget = false;
            Outline outline = obj.GetComponent<Outline>();
            outline.effectColor = borderColor;
            outline.effectDistance = new Vector2(2f, -2f);
            return obj.transform;
        }

        private Text CreateText(string name, Transform parent, string value, int size, FontStyle style,
            Vector2 position, Vector2 dimensions, Color color, TextAnchor alignment)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
            obj.layer = gameObject.layer;
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            Text text = obj.GetComponent<Text>();
            text.font = uiFont;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private RectTransform CreateHighlight(string name, Color color)
        {
            Transform panel = CreatePanel(name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(118f, 154f), color);
            Image image = panel.GetComponent<Image>();
            image.color = new Color(color.r, color.g, color.b, 0.12f);
            panel.gameObject.SetActive(false);
            return panel as RectTransform;
        }

        private Font FindFont()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            Text source = canvas != null ? canvas.GetComponentInChildren<Text>(true) : null;
            return source != null && source.font != null
                ? source.font
                : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
