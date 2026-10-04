using UnityEngine;
using UnityEngine.UI;

namespace VC5PvE
{
    /// <summary>Draws a light UI arrow from a hand slot to the current drag pointer.</summary>
    public sealed class CardTargetingArrow : MonoBehaviour
    {
        private RectTransform canvasRect;
        private RectTransform[] segments;
        private Image[] images;
        private Camera eventCamera;
        private Vector2 startScreen;

        public void Begin(Vector2 start, Vector2 pointer, bool valid)
        {
            canvasRect = transform as RectTransform;
            startScreen = start;
            segments = new RectTransform[3];
            images = new Image[3];
            for (int i = 0; i < segments.Length; i++)
            {
                var line = new GameObject("Card targeting arrow segment", typeof(RectTransform), typeof(Image));
                line.transform.SetParent(transform, false);
                segments[i] = line.GetComponent<RectTransform>();
                segments[i].anchorMin = segments[i].anchorMax = new Vector2(.5f,.5f);
                segments[i].pivot = new Vector2(.5f,.5f);
                images[i] = line.GetComponent<Image>();
                images[i].raycastTarget = false;
            }
            UpdatePointer(start, pointer, valid);
        }

        public void UpdatePointer(Vector2 start, Vector2 pointer, bool valid)
        {
            if (canvasRect == null || segments == null) return;
            startScreen = start;
            Color color = valid ? new Color(.36f,1f,.76f,.95f) : new Color(.63f,.70f,.70f,.48f);
            for (int i = 0; i < images.Length; i++) images[i].color = color;
            Vector2 from, tip;
            if (!ScreenToLocal(startScreen, out from) || !ScreenToLocal(pointer, out tip)) return;
            Vector2 direction = (tip - from).normalized;
            Vector2 side = new Vector2(-direction.y, direction.x);
            float headLength = Mathf.Min(16f, Vector2.Distance(from, tip) * .35f);
            Vector2 back = tip - direction * headLength;
            DrawSegment(0, from, tip, 4.5f);
            DrawSegment(1, back + side * headLength * .55f, tip, 4.5f);
            DrawSegment(2, back - side * headLength * .55f, tip, 4.5f);
        }

        private bool ScreenToLocal(Vector2 screen, out Vector2 local)
        {
            Canvas canvas = GetComponent<Canvas>();
            eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, eventCamera, out local);
        }

        private void DrawSegment(int index, Vector2 from, Vector2 to, float width)
        {
            Vector2 delta = to - from;
            var rect = segments[index];
            rect.anchoredPosition = (from + to) * .5f;
            rect.sizeDelta = new Vector2(delta.magnitude, width);
            rect.localRotation = Quaternion.Euler(0f,0f,Mathf.Atan2(delta.y,delta.x) * Mathf.Rad2Deg);
        }

        private void OnDestroy()
        {
            if (segments == null) return;
            foreach (var segment in segments) if (segment != null) Destroy(segment.gameObject);
        }
    }
}
