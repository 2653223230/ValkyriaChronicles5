using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TcgEngine;
using TcgEngine.Client;

namespace TcgEngine.UI
{
    public class HeroUI : MonoBehaviour
    {
        public bool opponent;
        public GameObject power_area;
        public Button power_button;
        public Image power_image;
        public GameObject power_mana_slot;
        public Text power_mana;

        public Material active_mat;
        public Material inactive_mat;

        private bool focus = false;
        private int heroPreviewHoverDepth;
        private Coroutine heroPreviewHideCo;

        private static List<HeroUI> ui_list = new List<HeroUI>();

        private void Awake()
        {
            ui_list.Add(this);
        }

        private void OnDestroy()
        {
            if (heroPreviewHideCo != null)
                StopCoroutine(heroPreviewHideCo);
            heroPreviewHideCo = null;
            ui_list.Remove(this);
        }

        private void Start()
        {
            if (power_area != null)
                power_area.SetActive(false);
            if (power_button != null)
                power_button.onClick.AddListener(OnClickPower);

            RegisterHeroPreviewTriggers();
        }

        private void RegisterHeroPreviewTriggers()
        {
            AddPointerHoverTarget(power_area);
            AddPointerHoverTarget(power_button != null ? power_button.gameObject : null);
            AddPointerHoverTarget(power_image != null ? power_image.gameObject : null);
            AddPointerHoverTarget(power_mana_slot);
        }

        private static void AddPointerHoverTarget(GameObject go)
        {
            if (go == null)
                return;

            EventTrigger trigger = go.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = go.AddComponent<EventTrigger>();

            EventTrigger.Entry enter = new EventTrigger.Entry();
            enter.eventID = EventTriggerType.PointerEnter;
            enter.callback.AddListener(_ => OnHeroPointerEnterStatic(go));
            trigger.triggers.Add(enter);

            EventTrigger.Entry exit = new EventTrigger.Entry();
            exit.eventID = EventTriggerType.PointerExit;
            exit.callback.AddListener(_ => OnHeroPointerExitStatic(go));
            trigger.triggers.Add(exit);
        }

        private static void OnHeroPointerEnterStatic(GameObject _)
        {
            HeroUI self = FindHeroUIForEvent(_);
            self?.OnHeroPointerEnter();
        }

        private static void OnHeroPointerExitStatic(GameObject _)
        {
            HeroUI self = FindHeroUIForEvent(_);
            self?.OnHeroPointerExit();
        }

        private static HeroUI FindHeroUIForEvent(GameObject go)
        {
            if (go == null)
                return null;
            Transform t = go.transform;
            while (t != null)
            {
                HeroUI h = t.GetComponent<HeroUI>();
                if (h != null)
                    return h;
                t = t.parent;
            }
            return null;
        }

        private void OnHeroPointerEnter()
        {
            if (GameUI.IsUIOpened())
                return;
            if (GameTool.IsMobile())
                return;

            heroPreviewHoverDepth++;
            focus = heroPreviewHoverDepth > 0;

            if (heroPreviewHideCo != null)
            {
                StopCoroutine(heroPreviewHideCo);
                heroPreviewHideCo = null;
            }

            if (heroPreviewHoverDepth == 1)
                TryShowHeroDetailPreview();
        }

        private void OnHeroPointerExit()
        {
            if (GameTool.IsMobile())
                return;

            heroPreviewHoverDepth = Mathf.Max(0, heroPreviewHoverDepth - 1);
            focus = heroPreviewHoverDepth > 0;

            if (heroPreviewHoverDepth == 0 && isActiveAndEnabled)
            {
                if (heroPreviewHideCo != null)
                    StopCoroutine(heroPreviewHideCo);
                heroPreviewHideCo = StartCoroutine(HideHeroPreviewAfterDelay());
            }
        }

        private IEnumerator HideHeroPreviewAfterDelay()
        {
            yield return new WaitForSecondsRealtime(0.06f);
            heroPreviewHideCo = null;
            if (heroPreviewHoverDepth < 1)
                CardDetailPreview.Hide();
        }

        private void TryShowHeroDetailPreview()
        {
            if (GameUI.IsUIOpened())
                return;
            if (!GameClient.Get().IsReady())
                return;

            Card c = GetCard();
            if (c != null && c.CardData != null)
                CardDetailPreview.ShowCard(c);
        }

        private void Update()
        {
            if (!GameClient.Get().IsReady())
                return;

            Game gdata = GameClient.Get().GetGameData();
            Player player = GetPlayer();
            Card hero = player.hero;
            if (hero == null)
                return;

            AbilityData ability = hero.GetAbility(AbilityTrigger.Activate);
            if (ability != null && power_image != null)
            {
                power_image.sprite = hero.CardData.GetBoardArt(hero.VariantData);
                power_image.material = !hero.exhausted ? active_mat : inactive_mat;
                power_mana_slot?.SetActive(gdata.IsPlayerTurn(player) && !hero.exhausted);
                if (power_mana != null)
                    power_mana.text = ability.mana_cost.ToString();
            }

            if (power_button != null)
                power_button.interactable = ability != null && !hero.exhausted && gdata.IsPlayerTurn(player);

            if (hero != null && power_area != null && !power_area.activeSelf)
                power_area.SetActive(true);
        }

        public void OnClickPower()
        {
            if (!GameUI.IsUIOpened())
            {
                Card h = GetCard();
                if (h != null && h.CardData != null)
                    CardDetailPreview.ShowCard(h);
            }

            Game gdata = GameClient.Get().GetGameData();
            Player player = GameClient.Get().GetPlayer();
            Card hero = player.hero;
            AbilityData ability = hero?.GetAbility(AbilityTrigger.Activate);
            if (ability != null && !opponent)
            {
                if (!hero.exhausted && !player.CanPayAbility(hero, ability))
                {
                    WarningText.ShowNoMana();
                    return;
                }

                bool valid = gdata.IsPlayerActionTurn(player) && gdata.CanCastAbility(hero, ability);
                if (valid)
                {
                    GameClient.Get().CastAbility(hero, ability);
                }
            }
        }

        private void OnDisable()
        {
            heroPreviewHoverDepth = 0;
            focus = false;
            if (heroPreviewHideCo != null)
            {
                StopCoroutine(heroPreviewHideCo);
                heroPreviewHideCo = null;
            }
            CardDetailPreview.Hide();
        }

        public bool IsFocus()
        {
            return focus;
        }

        public int GetPlayerID()
        {
            return opponent ? GameClient.Get().GetOpponentPlayerID() : GameClient.Get().GetPlayerID();
        }

        public Player GetPlayer()
        {
            Game gdata = GameClient.Get().GetGameData();
            return gdata.GetPlayer(GetPlayerID());
        }

        public Card GetCard()
        {
            Player player = GetPlayer();
            return player.hero;
        }

        public static HeroUI GetFocus()
        {
            foreach (HeroUI ui in ui_list)
            {
                if (ui.IsFocus())
                    return ui;
            }
            return null;
        }

        public static HeroUI Get(bool opponent)
        {
            foreach (HeroUI ui in ui_list)
            {
                if (ui.opponent == opponent)
                    return ui;
            }
            return null;
        }

        public static HeroUI Get(int player_id)
        {
            bool opp = player_id != GameClient.Get().GetPlayerID();
            return Get(opp);
        }
    }
}
