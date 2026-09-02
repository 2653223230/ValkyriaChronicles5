using System.Collections.Generic;
using TcgEngine.Client;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    public class Vc5C3PreviewOverlay : MonoBehaviour
    {
        public const string RootName = "VC5 C3 Effect Preview";
        public Vc5C3Preview Preview { get; private set; }
        public Card Actor { get; private set; }
        public Card Spell { get; private set; }
        public Game Data { get; private set; }
        Vc5C3PreviewGraphic graphic;
        Text summary;
        Image ghost;
        Text ghostTitle;
        Text actorNote;
        Font font;
        readonly List<Text> labels = new List<Text>();
        float nextRefresh;

        public static Vc5C3PreviewOverlay Show(Transform parent)
        {
            Transform existing = parent.Find(RootName);
            if (existing != null) return existing.GetComponent<Vc5C3PreviewOverlay>();
            GameObject root = new GameObject(RootName, typeof(RectTransform));
            root.layer = parent.gameObject.layer;
            root.transform.SetParent(parent, false);
            Stretch(root.GetComponent<RectTransform>());
            var overlay = root.AddComponent<Vc5C3PreviewOverlay>();
            overlay.BuildUI();
            return overlay;
        }

        void BuildUI()
        {
            Text source = GetComponentInParent<Canvas>().GetComponentInChildren<Text>(true);
            font = source != null && source.font != null ? source.font : Resources.GetBuiltinResource<Font>("Arial.ttf");
            GameObject lines = new GameObject("Ranges and routes", typeof(RectTransform));
            lines.layer = gameObject.layer;
            lines.transform.SetParent(transform, false);
            Stretch(lines.GetComponent<RectTransform>());
            graphic = lines.AddComponent<Vc5C3PreviewGraphic>();
            graphic.overlay = this;
            graphic.raycastTarget = false;
            summary = MakeText("Effect summary", 23);
            summary.alignment = TextAnchor.UpperLeft;
            RectTransform rect = summary.rectTransform;
            rect.anchorMin = new Vector2(0.055f, 0.12f);
            rect.anchorMax = new Vector2(0.29f, 0.36f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            GameObject image = new GameObject("Destination ghost", typeof(RectTransform), typeof(Image));
            image.layer = gameObject.layer;
            image.transform.SetParent(transform, false);
            ghost = image.GetComponent<Image>();
            ghost.raycastTarget = false;
            ghost.color = new Color(0.2f, 1f, 0.85f, 0.45f);
            ghost.preserveAspect = true;
            ghostTitle = MakeText("Destination name", 17);
            actorNote = MakeText("Actor effect", 18);
            Hide();
        }

        void Update()
        {
            GameClient client = GameClient.Get();
            if (client == null || !client.IsReady() || !client.IsYourTurn() || GameUI.IsUIOpened()) { Hide(); return; }
            Game game = client.GetGameData();
            if (game == null || game.HasEnded() || game.phase != GamePhase.Main) { Hide(); return; }
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.08f;
            Vector3 mouse = GameBoard.Get().RaycastMouseBoard();
            BSlot slot = BSlot.GetNearest(mouse);
            HandCard drag = HandCard.GetDrag();
            Card spell = drag != null ? drag.GetCard() : null;
            Card actor = slot != null ? Vc5DemoGrid.GetDisplayedSlotCard(game, slot.GetSlot(mouse)) : null;
            Slot? destination = null;
            if (game.selector == SelectorType.SelectTarget && game.selector_player_id == client.GetPlayerID())
            {
                spell = game.GetCard(game.selector_caster_uid);
                actor = game.GetCard(game.ability_triggerer);
                if (!Vc5C3Rules.IsPreciseMove(spell)) { Hide(); return; }
                if (slot != null) destination = slot.GetSlot(mouse);
            }
            if (!Vc5C3Rules.IsCard(spell)) { Hide(); return; }
            CardDetailPreview.Hide();
            Refresh(game, spell, actor, destination);
        }

        public void Refresh(Game game, Card spell, Card actor, Slot? destination = null)
        {
            Data = game; Spell = spell; Actor = actor;
            Preview = Vc5C3Preview.Build(game, spell, actor, destination);
            graphic.enabled = true;
            graphic.SetVerticesDirty();
            summary.gameObject.SetActive(true);
            summary.text = spell.CardData.title + "\n" + Preview.plan.reason;
            if (Preview.sequence.Count > 0) summary.text += "\n" + string.Join("\n", Preview.sequence);
            summary.color = Preview.plan.valid ? new Color(0.5f, 1f, 0.9f) : Color.white;
            foreach (Text label in labels) label.gameObject.SetActive(false);
            int index = 0;
            foreach (var hit in Preview.damage)
            {
                BoardCard board = BoardCard.Get(hit.Key);
                if (board == null) continue;
                while (labels.Count <= index) labels.Add(MakeText("Predicted damage " + index, 22));
                Text label = labels[index++];
                label.gameObject.SetActive(true);
                Card target = game.GetCard(hit.Key);
                label.text = "预计 -" + hit.Value + (target != null && hit.Value >= target.GetHP() ? " 击破" : "");
                label.color = new Color(1f, 0.4f, 0.4f);
                Position(label.rectTransform, Project(board.transform.position) + new Vector2(0f, 52f), new Vector2(150f, 32f));
            }
            bool moving = Preview.plan.valid && Preview.plan.path.Count > 1 && actor != null;
            bool rangeBuff = Preview.plan.valid && (spell.card_id == Vc5C3Rules.Prefix + "temp_calibration"
                || spell.card_id == Vc5C3Rules.Prefix + "scope_upgrade");
            actorNote.gameObject.SetActive(rangeBuff);
            if (rangeBuff)
            {
                actorNote.text = spell.card_id == Vc5C3Rules.Prefix + "temp_calibration" ? "射程 +1（本回合）" : "射程 +1（本局）";
                Position(actorNote.rectTransform, Point(actor.slot) + new Vector2(0f, 58f), new Vector2(200f, 30f));
            }
            ghost.gameObject.SetActive(moving);
            ghostTitle.gameObject.SetActive(moving);
            if (moving)
            {
                ghost.sprite = actor.VariantData.frame_board;
                Vector2 point = Point(Preview.plan.destination);
                Position(ghost.rectTransform, point, new Vector2(62f, 82f));
                Position(ghostTitle.rectTransform, point + new Vector2(0f, -48f), new Vector2(140f, 28f));
                ghostTitle.text = actor.CardData.title + " 落点";
                if (spell.card_id == Vc5C3Rules.Prefix + "forced_march" && Preview.plan.path.Count - 1 > actor.move_Range)
                    ghostTitle.text = "强行军落点";
            }
        }

        void Hide()
        {
            Preview = null;
            if (graphic != null) graphic.enabled = false;
            if (summary != null) summary.gameObject.SetActive(false);
            if (ghost != null) ghost.gameObject.SetActive(false);
            if (ghostTitle != null) ghostTitle.gameObject.SetActive(false);
            if (actorNote != null) actorNote.gameObject.SetActive(false);
            foreach (Text label in labels) label.gameObject.SetActive(false);
        }

        public Vector2 Point(Slot slot)
        {
            slot = Vc5DemoGrid.ToPerspective(slot, Slot.GetP(GameClient.Get().GetPlayerID()));
            BSlot board = BSlot.Get(slot);
            return board != null ? Project(board.GetPosition(slot)) : Vector2.zero;
        }

        public Vector2 Project(Vector3 world)
        {
            Canvas canvas = GetComponentInParent<Canvas>().rootCanvas;
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector3 screen = Camera.main.WorldToScreenPoint(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, screen, uiCamera, out Vector2 point);
            return point;
        }

        Text MakeText(string name, int size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            Text text = go.GetComponent<Text>();
            text.font = font; text.fontSize = size; text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            go.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.95f);
            return text;
        }

        void Position(RectTransform rect, Vector2 point, Vector2 size)
        {
            Rect bounds = ((RectTransform)transform).rect;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = new Vector2(Mathf.Clamp(point.x, bounds.xMin + size.x / 2, bounds.xMax - size.x / 2),
                Mathf.Clamp(point.y, bounds.yMin + size.y / 2, bounds.yMax - size.y / 2));
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
