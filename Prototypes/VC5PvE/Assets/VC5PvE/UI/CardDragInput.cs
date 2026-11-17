using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VC5PvE
{
    public sealed class CardDragInput : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private const float HoverDuration = .15f;
        private const float HoverLift = 40f;
        private const float HoverScale = 1.06f;
        private const float ExitPadding = 8f;
        private static CardDragInput activeDrag;

        public string CardId;
        public bool Enabled;
        public bool AllowUnavailableDrag;
        public bool SharedCardStyle;
        public Func<string> SharedDragSummary;
        private RectTransform sharedGhost;
        private Text ghostSummary;
        public Action<string,Vector2> Dropped;
        public Action<string> DragStarted;
        public Action<string> DragCancelled;
        public Func<Vector2,bool> CanDrop;
        public Func<Vector2?> AimOrigin;
        public Action Hovered;
        public Action HoverExited;

        private RectTransform rect;
        private Vector2 originalPosition;
        private Vector3 originalScale;
        private int sibling;
        private CanvasGroup group;
        private bool savedGroupState;
        private float originalAlpha;
        private bool originalBlocksRaycasts;
        private bool originalInteractable;
        private bool pointerInside;
        private bool dragging;
        private bool selected;
        private bool usable;
        private float emphasisProgress;
        private float emphasisFrom;
        private float emphasisTarget;
        private float emphasisElapsed;
        private Vector2 lastPointerPosition;
        private Camera eventCamera;
        private Image[] borderImages;
        private CardTargetingArrow arrow;

        private void Awake()
        {
            rect = transform as RectTransform;
            if (rect == null) rect = gameObject.AddComponent<RectTransform>();
            originalPosition = rect.anchoredPosition;
            originalScale = rect.localScale;
            CacheBorderImages();
            UpdateBorder();
        }

        private void Update()
        {
            if (activeDrag != null && activeDrag != this)
            {
                // Other cards retract silently so they cannot replace the dragged card's details.
                pointerInside = false;
                UpdateEmphasisTarget();
            }
            else if (!Enabled)
            {
                pointerInside = false;
                UpdateEmphasisTarget();
            }
            else if (activeDrag == null && !pointerInside && !dragging && rect != null &&
                RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, eventCamera))
            {
                // Pointer-enter events can be consumed while another card is being dragged.
                pointerInside = true;
                UpdateEmphasisTarget();
                Hovered?.Invoke();
            }
            else if (pointerInside && !dragging && !ContainsPointer(Input.mousePosition, ExitPadding))
            {
                pointerInside = false;
                UpdateEmphasisTarget();
                HoverExited?.Invoke();
            }

            if (dragging) return;
            if (Mathf.Approximately(emphasisProgress, emphasisTarget)) return;

            emphasisElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(emphasisElapsed / HoverDuration);
            float eased = t * t * (3f - 2f * t);
            emphasisProgress = Mathf.Lerp(emphasisFrom, emphasisTarget, eased);
            ApplyHoverTransform();
            if (t >= 1f) emphasisProgress = emphasisTarget;
            UpdateBorder();
        }

        public void OnPointerEnter(PointerEventData e)
        {
            if (!Enabled || dragging || (activeDrag != null && activeDrag != this)) return;
            eventCamera = e.enterEventCamera;
            lastPointerPosition = e.position;
            pointerInside = true;
            UpdateEmphasisTarget();
            UpdateBorder();
            Hovered?.Invoke();
        }

        public void OnPointerExit(PointerEventData e)
        {
            if (!Enabled || dragging || (activeDrag != null && activeDrag != this)) return;
            eventCamera = e.enterEventCamera;
            lastPointerPosition = e.position;
            // The card grows while hovered; allow a small margin so its changing hitbox
            // cannot repeatedly toggle the hover state near an edge.
            if (!ContainsPointer(lastPointerPosition, ExitPadding))
            {
                bool wasInside=pointerInside;
                pointerInside = false;
                UpdateEmphasisTarget();
                UpdateBorder();
                if(wasInside) HoverExited?.Invoke();
            }
        }

        public void SetSelected(bool value)
        {
            selected = value;
            UpdateEmphasisTarget();
            UpdateBorder();
        }

        public void SetUsable(bool value)
        {
            usable = value;
            UpdateEmphasisTarget();
            UpdateBorder();
        }

        /// <summary>Opt-in interaction gate used while the shared AP hand is in discard selection.</summary>
        public void SetInteractionEnabled(bool value)
        {
            Enabled=value;
            if(!value&&dragging)RestoreAfterDrag();
            if(!value)pointerInside=false;
            UpdateEmphasisTarget();
        }

        public void OnBeginDrag(PointerEventData e)
        {
            if (!Enabled || (!usable&&!AllowUnavailableDrag) || dragging || rect == null || (activeDrag != null && activeDrag != this)) return;

            dragging = true;
            activeDrag = this;
            pointerInside = false;
            emphasisProgress = 0f;
            UpdateEmphasisTarget();
            rect.anchoredPosition = originalPosition;
            rect.localScale = originalScale;

            sibling = rect.GetSiblingIndex();
            originalPosition = rect.anchoredPosition;
            originalScale = rect.localScale;
            rect.SetAsLastSibling();

            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            originalAlpha = group.alpha;
            originalBlocksRaycasts = group.blocksRaycasts;
            originalInteractable = group.interactable;
            savedGroupState = true;
            group.blocksRaycasts = false;
            group.alpha = .28f;
            DragStarted?.Invoke(CardId);
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null && SharedCardStyle)
            {
                sharedGhost=BattleHud.Panel(canvas.transform,"Shared drag card",0,0,205,104,SharedPaper.Paper,SharedPaper.Gold);
                sharedGhost.pivot=new Vector2(0,1);
                var blocker=sharedGhost.gameObject.AddComponent<CanvasGroup>();blocker.blocksRaycasts=false;blocker.interactable=false;
                var source=transform.Find("Card name").GetComponent<Text>();
                var cost=transform.Find("Shared card cost/Cost").GetComponent<Text>();
                BattleHud.Label(sharedGhost,"Command",10,6,185,29,18,SharedPaper.Ink,source.font).text=source.text+" · "+cost.text;
                ghostSummary=BattleHud.Label(sharedGhost,"Effect",10,36,185,60,16,SharedPaper.Green,source.font);
                UpdateSharedDrag(e);
            }
            else if (canvas != null)
            {
                arrow = canvas.gameObject.AddComponent<CardTargetingArrow>();
                var origin = AimOrigin == null ? (Vector2?)null : AimOrigin();
                arrow.Begin(origin ?? (Vector2)rect.position, e.position, CanDrop != null && CanDrop(e.position));
            }
        }

        void UpdateSharedDrag(PointerEventData e)
        {
            bool legal=CanDrop!=null&&CanDrop(e.position);
            if(sharedGhost==null)return;
            var canvas=(RectTransform)sharedGhost.parent;
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,e.position,e.pressEventCamera,out var local))
            {
                float x=Mathf.Clamp(local.x+16,canvas.rect.xMin,canvas.rect.xMax-sharedGhost.rect.width);
                float y=Mathf.Clamp(local.y+28,canvas.rect.yMin+sharedGhost.rect.height,canvas.rect.yMax);
                sharedGhost.localPosition=new Vector3(x,y,0);
            }
            ghostSummary.text=SharedDragSummary?.Invoke()??(legal?"松手交给队员":"拖到队员查看效果");
        }

        public void OnDrag(PointerEventData e)
        {
            if (!Enabled || !dragging || rect == null) return;
            lastPointerPosition = e.position;
            if(SharedCardStyle){UpdateSharedDrag(e);return;}
            var origin = AimOrigin == null ? (Vector2?)null : AimOrigin();
            arrow?.UpdatePointer(origin ?? (Vector2)rect.position, e.position, CanDrop != null && CanDrop(e.position));
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (!dragging) return;
            RestoreAfterDrag();
            if (Enabled) Dropped?.Invoke(CardId,e.position);
            else DragCancelled?.Invoke(CardId);
        }

        public void CancelVisualDrag()
        {
            if(dragging)RestoreAfterDrag();
            pointerInside=false;selected=false;UpdateEmphasisTarget();UpdateBorder();
        }
        private void OnDisable()
        {
            if (activeDrag == this) activeDrag = null;
            pointerInside = false;
            UpdateEmphasisTarget();
            bool cancelCardSelection = dragging || selected;
            if (dragging)
            {
                RestoreAfterDrag(false);
            }
            else
            {
                emphasisProgress = 0f;
                emphasisFrom = 0f;
                emphasisTarget = selected ? 1f : usable && !SharedCardStyle ? .5f : 0f;
                ApplyHoverTransform();
                UpdateBorder();
            }
            if (cancelCardSelection)
            {
                selected = false;
                UpdateEmphasisTarget();
                ApplyHoverTransform();
                UpdateBorder();
                DragCancelled?.Invoke(CardId);
            }
        }

        private void OnDestroy()
        {
            if (activeDrag == this) activeDrag = null;
            bool cancelCardSelection = dragging || selected;
            if (dragging)
            {
                RestoreAfterDrag(false);
            }
            if (cancelCardSelection) DragCancelled?.Invoke(CardId);
        }

        private void RestoreAfterDrag(bool restoreSibling = true)
        {
            if (activeDrag == this) activeDrag = null;
            if(sharedGhost!=null){sharedGhost.gameObject.SetActive(false);Destroy(sharedGhost.gameObject);sharedGhost=null;ghostSummary=null;}
            if (arrow != null) Destroy(arrow);
            arrow = null;
            if (rect != null)
            {
                rect.anchoredPosition = originalPosition;
                rect.localScale = originalScale;
                int maxSibling = rect.parent == null ? 0 : rect.parent.childCount - 1;
                if (restoreSibling) rect.SetSiblingIndex(Mathf.Clamp(sibling, 0, Mathf.Max(0, maxSibling)));
            }

            if (group != null && savedGroupState)
            {
                group.alpha = originalAlpha;
                group.blocksRaycasts = originalBlocksRaycasts;
                group.interactable = originalInteractable;
            }

            savedGroupState = false;
            dragging = false;
            emphasisProgress = 0f;
            emphasisFrom = 0f;
            emphasisElapsed = 0f;
            UpdateEmphasisTarget();
            ApplyHoverTransform();
            UpdateBorder();
        }

        private void UpdateEmphasisTarget()
        {
            if (dragging) return;
            float target = selected || pointerInside ? 1f : usable && !SharedCardStyle ? .5f : 0f;
            if (Mathf.Approximately(emphasisTarget, target)) return;
            emphasisFrom = emphasisProgress;
            emphasisTarget = target;
            emphasisElapsed = 0f;
            UpdateBorder();
        }

        private void ApplyHoverTransform()
        {
            if (rect == null || dragging) return;
            rect.anchoredPosition = originalPosition + Vector2.up * (HoverLift * emphasisProgress);
            rect.localScale = originalScale * Mathf.Lerp(1f, HoverScale, emphasisProgress);
        }

        private bool ContainsPointer(Vector2 screenPosition, float padding)
        {
            if (rect == null) return false;
            var parentRect = rect.parent as RectTransform;
            if (parentRect == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPosition, eventCamera, out var local)) return false;
            var bounds = rect.rect;
            bounds = new Rect(
                originalPosition.x + bounds.xMin - padding - bounds.width * (HoverScale - 1f),
                originalPosition.y + bounds.yMin - padding,
                bounds.width * (2f * HoverScale - 1f) + padding * 2f,
                bounds.height * HoverScale + HoverLift + padding * 2f);
            return bounds.Contains(local);
        }

        private void CacheBorderImages()
        {
            borderImages = new Image[4];
            string[] names = { "Top border", "Bottom border", "Left border", "Right border" };
            for (int i = 0; i < names.Length; i++)
            {
                var border = transform.Find(names[i]);
                if (border == null) continue;
                borderImages[i] = border.GetComponent<Image>();
                if (borderImages[i] != null) borderImages[i].raycastTarget = false;
            }
        }

        private void UpdateBorder()
        {
            if (borderImages == null) return;
            Color color = selected ? new Color(1f,.88f,.52f,1f) :
                pointerInside ? BattleHud.Gold : usable ? new Color(.28f,.88f,1f,1f) : BattleHud.Jade;
            if(SharedCardStyle)color=selected||pointerInside?SharedPaper.Gold:SharedPaper.Edge;
            for (int i = 0; i < borderImages.Length; i++)
                if (borderImages[i] != null) borderImages[i].color = color;
        }
    }
}
