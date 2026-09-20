using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    // A cheap UI mesh: only the dashed edge pulses, keeping cost and text fully opaque.
    [RequireComponent(typeof(CanvasRenderer))]
    public class Vc5TemporaryCardGraphic : MaskableGraphic
    {
        float nextRefresh;
        void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.05f;
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect r = rectTransform.rect;
            Color tint = new Color(0.3f, 1f, 0.95f, 0.55f + Mathf.Sin(Time.unscaledTime * 2.5f) * 0.25f);
            Edge(mesh, new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), tint);
            Edge(mesh, new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax), tint);
            Edge(mesh, new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax), tint);
            Edge(mesh, new Vector2(r.xMin, r.yMax), new Vector2(r.xMin, r.yMin), tint);
        }
        static void Edge(VertexHelper mesh, Vector2 a, Vector2 b, Color tint)
        {
            Vector2 direction = (b - a).normalized;
            Vector2 width = new Vector2(-direction.y, direction.x) * 2.5f;
            float length = Vector2.Distance(a, b);
            for (float step = 0; step < length; step += 22f)
            {
                Vector2 start = a + direction * step;
                Vector2 end = a + direction * Mathf.Min(step + 12f, length);
                int n = mesh.currentVertCount;
                mesh.AddVert(start - width, tint, Vector2.zero);
                mesh.AddVert(start + width, tint, Vector2.zero);
                mesh.AddVert(end + width, tint, Vector2.zero);
                mesh.AddVert(end - width, tint, Vector2.zero);
                mesh.AddTriangle(n, n + 1, n + 2);
                mesh.AddTriangle(n, n + 2, n + 3);
            }
        }
    }
}
