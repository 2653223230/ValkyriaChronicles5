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
        public Action<string,Vector2> Dropped;
        public Action<string> DragStarted;
        public Action<string> DragCancelled;
        public Func<Vector2,bool> CanDrop;
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
        private float hoverProgress;
        private float hoverFrom;
        private float hoverTarget;
        private float hoverElapsed;
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
                SetHoverTarget(0f);
            }
            else if (!Enabled)
            {
                pointerInside = false;
                SetHoverTarget(0f);
            }
            else if (activeDrag == null && !pointerInside && !dragging && rect != null &&
                RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, eventCamera))
            {
                // Pointer-enter events can be consumed while another card is being dragged.
                pointerInside = true;
                SetHoverTarget(1f);
                Hovered?.Invoke();
            }
            else if (pointerInside && !dragging && !ContainsPointer(Input.mousePosition, ExitPadding))
            {
                pointerInside = false;
                SetHoverTarget(0f);
                HoverExited?.Invoke();
            }

            if (dragging) return;
            if (Mathf.Approximately(hoverProgress, hoverTarget)) return;

            hoverElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(hoverElapsed / HoverDuration);
            float eased = t * t * (3f - 2f * t);
            hoverProgress = Mathf.Lerp(hoverFrom, hoverTarget, eased);
            ApplyHoverTransform();
            if (t >= 1f) hoverProgress = hoverTarget;
            UpdateBorder();
        }

        public void OnPointerEnter(PointerEventData e)
        {
            if (!Enabled || dragging || (activeDrag != null && activeDrag != this)) return;
            eventCamera = e.enterEventCamera;
            lastPointerPosition = e.position;
            pointerInside = true;
            SetHoverTarget(1f);
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
                SetHoverTarget(0f);
                UpdateBorder();
                if(wasInside) HoverExited?.Invoke();
            }
        }

        public void SetSelected(bool value)
        {
            selected = value;
            UpdateBorder();
        }

        public void OnBeginDrag(PointerEventData e)
        {
            if (!Enabled || dragging || rect == null || (activeDrag != null && activeDrag != this)) return;

            dragging = true;
            activeDrag = this;
            pointerInside = false;
            hoverProgress = 0f;
            SetHoverTarget(0f);
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
            if (canvas != null)
            {
                arrow = canvas.gameObject.AddComponent<CardTargetingArrow>();
                arrow.Begin(rect.position, e.position, CanDrop != null && CanDrop(e.position));
            }
        }

        public void OnDrag(PointerEventData e)
        {
            if (!Enabled || !dragging || rect == null) return;
            lastPointerPosition = e.position;
            arrow?.UpdatePointer(rect.position, e.position, CanDrop != null && CanDrop(e.position));
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (!dragging) return;
            RestoreAfterDrag();
            if (Enabled) Dropped?.Invoke(CardId,e.position);
            else DragCancelled?.Invoke(CardId);
        }

        private void OnDisable()
        {
            if (activeDrag == this) activeDrag = null;
            pointerInside = false;
            SetHoverTarget(0f);
            bool cancelCardSelection = dragging || selected;
            if (dragging)
            {
                RestoreAfterDrag(false);
            }
            else
            {
                hoverProgress = 0f;
                hoverFrom = 0f;
                hoverTarget = 0f;
                ApplyHoverTransform();
                UpdateBorder();
            }
            if (cancelCardSelection)
            {
                selected = false;
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
            hoverProgress = 0f;
            hoverFrom = 0f;
            hoverTarget = 0f;
            hoverElapsed = 0f;
            ApplyHoverTransform();
            UpdateBorder();
        }

        private void SetHoverTarget(float target)
        {
            if (dragging) return;
            if (Mathf.Approximately(hoverTarget, target)) return;
            hoverFrom = hoverProgress;
            hoverTarget = target;
            hoverElapsed = 0f;
            UpdateBorder();
        }

        private void ApplyHoverTransform()
        {
            if (rect == null || dragging) return;
            rect.anchoredPosition = originalPosition + Vector2.up * (HoverLift * hoverProgress);
            rect.localScale = originalScale * Mathf.Lerp(1f, HoverScale, hoverProgress);
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
                hoverProgress > .01f || hoverTarget > 0f ? BattleHud.Gold : BattleHud.Jade;
            for (int i = 0; i < borderImages.Length; i++)
                if (borderImages[i] != null) borderImages[i].color = color;
        }
    }
}
