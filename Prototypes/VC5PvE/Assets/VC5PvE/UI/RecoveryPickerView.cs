using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VC5PvE
{
    /// <summary>Explicit selection of one physical discard; never mutates a battle itself.</summary>
    public sealed class RecoveryPickerView : MonoBehaviour
    {
        private RectTransform root;
        private Font font;
        private string selection;
        private Button confirm;
        private readonly List<Image> frames = new List<Image>();
        private readonly List<string> ids = new List<string>();
        public bool Visible { get { return root != null && root.gameObject.activeSelf; } }

        public void Build(RectTransform parent, Font uiFont)
        {
            font = uiFont;
            root = BattleHud.Panel(parent, "Discard recovery selection", 0, 0, 1920, 1080, new Color(.012f,.04f,.055f,.96f));
            root.gameObject.SetActive(false);
        }

        public void Show(IList<CardInstance> cards, string title, string subtitle, Action<string> pick, Action cancel, bool optional)
        {
            foreach (Transform child in root) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            frames.Clear(); ids.Clear(); selection = null;
            root.gameObject.SetActive(true); root.SetAsLastSibling();
            var panel = BattleHud.Panel(root, "Recovery frame", 185, 148, 1550, 784, BattleHud.Ink, BattleHud.Gold);
            BattleHud.Label(panel, "Recovery heading", 44, 30, 1400, 58, 37, BattleHud.Gold, font).text = title;
            BattleHud.Label(panel, "Recovery description", 46, 100, 1450, 64, 23, BattleHud.Paper, font).text = subtitle;
            var viewport = BattleHud.Panel(panel, "Discard viewport", 44, 182, 1460, 410, new Color(.025f,.075f,.085f));
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = BattleHud.Rect(viewport, "Discard entries", 0, 0, 1440, Mathf.Max(410, ((cards.Count+2)/3)*204));
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 30;
            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i]; var def = Definitions.Card(card); string id = card.Id;
                var item = BattleHud.Panel(content, "Discard " + id, 8+(i%3)*480, 6+(i/3)*204, 462, 190, new Color(.055f,.15f,.17f), BattleHud.Jade);
                var image = item.GetComponent<Image>(); frames.Add(image); ids.Add(id);
                var button = item.gameObject.AddComponent<Button>(); button.targetGraphic = image;
                button.onClick.AddListener(() => Select(id));
                BattleHud.Label(item, "Recovered card name", 60, 14, 384, 38, 27, BattleHud.Gold, font).text = def.Name;
                BuildCost(item, def.Cost);
                BattleHud.Label(item, "Physical card", 19, 53, 420, 26, 16, BattleHud.Jade, font).text = def.UserLabel + (card.Upgraded ? "   · 已升级" : "   · 未升级");
                var description = BattleHud.Label(item, "Recovered effect", 19, 86, 420, 95, 20, BattleHud.Paper, font);
                description.text = def.Description.Replace("\n", " ");
            }
            if (cards.Count == 0) BattleHud.Label(panel, "No discard available", 60, 270, 1400, 70, 30, BattleHud.Paper, font).text = "弃牌堆中没有可回收的中立攻击牌。";
            BattleHud.Label(panel, "Recovery rule", 46, 616, 1450, 46, 21, BattleHud.Jade, font).text = "收回的是原牌，保留升级。再次打出仍需支付行动点；已用过的减费不会重置。";
            BattleHud.ButtonAt(panel, optional ? "跳过回收" : "取消回收", 800, 689, 260, 62, font, () => { Hide(); cancel?.Invoke(); });
            confirm = BattleHud.ButtonAt(panel, "确认收回", 1110, 689, 350, 62, font, () =>
            {
                if (selection == null) return;
                var id = selection; Hide(); pick?.Invoke(id);
            }, true);
            confirm.interactable = false;
            foreach(var label in root.GetComponentsInChildren<Text>())label.verticalOverflow=VerticalWrapMode.Overflow;
        }

        private void Select(string id)
        {
            selection = id;
            for (int i = 0; i < frames.Count; i++) frames[i].color = ids[i] == id ? new Color(.17f,.29f,.23f) : new Color(.055f,.15f,.17f);
            confirm.interactable = true;
        }

        private void BuildCost(RectTransform parent, int amount)
        {
            var badge = BattleHud.Panel(parent, "AP diamond", 17, 18, 27, 27, new Color(.10f,.73f,.95f));
            badge.pivot = new Vector2(.5f,.5f); badge.anchoredPosition = new Vector2(32,-33); badge.localRotation = Quaternion.Euler(0,0,45);
            var text = BattleHud.Label(parent, "AP cost", 14, 11, 37, 42, 26, Color.white, font);
            text.text = amount.ToString(); text.alignment = TextAnchor.MiddleCenter;
            badge.GetComponent<Image>().raycastTarget = false;
        }

        public void Hide() { if (root != null) root.gameObject.SetActive(false); selection = null; }
    }
}
