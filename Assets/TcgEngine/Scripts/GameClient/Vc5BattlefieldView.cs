using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine.Client
{
    /// <summary>Approved R4/B-AI1 client art only; never moves slots or adds colliders.</summary>
    public class Vc5BattlefieldView : MonoBehaviour
    {
        private readonly List<Mesh> ownedMeshes = new List<Mesh>();
        private readonly List<LineRenderer> scoringRims = new List<LineRenderer>();
        private readonly List<BoardSlot> scoringSlots = new List<BoardSlot>();
        private Camera gameCamera;
        private Camera backdropCamera;
        private const int BackdropLayer = 31;
        private SpriteRenderer background;
        private MeshRenderer lamp;
        private Mesh dust;
        private Material material;
        private readonly Vector3[] dustVertices = new Vector3[56];
        private readonly Color[] dustColors = new Color[56];
        private int viewportWidth, viewportHeight;
        private float halfWidth, halfHeight;
        private Vector3 viewCenter;
        private MaterialPropertyBlock lightProperties;

        public static bool ShouldApply(GameType type, string playerDeck, string aiDeck)
        {
            return type == GameType.Solo && playerDeck == Vc5DemoBootstrap.CommandR4DeckId
                && aiDeck == Vc5DemoBootstrap.SteadyAssaultDeckId;
        }

        public static bool AppliesToCurrentMatch()
        {
            return ShouldApply(GameClient.game_settings.game_type,
                GameClient.player_settings?.deck?.tid, GameClient.ai_settings?.deck?.tid);
        }

        private void Start()
        {
            gameCamera = Camera.main;
            lightProperties = new MaterialPropertyBlock();
            // Keep the original 80% board viewport and all interaction coordinates unchanged.
            backdropCamera = new GameObject("VC5 backdrop camera", typeof(Camera)).GetComponent<Camera>();
            backdropCamera.transform.SetParent(transform, false);
            backdropCamera.transform.SetPositionAndRotation(gameCamera.transform.position, gameCamera.transform.rotation);
            backdropCamera.orthographic = true;
            backdropCamera.orthographicSize = gameCamera.orthographicSize;
            backdropCamera.depth = gameCamera.depth - 1f;
            backdropCamera.cullingMask = 1 << BackdropLayer;
            backdropCamera.clearFlags = CameraClearFlags.SolidColor;
            backdropCamera.backgroundColor = new Color(0.04f, 0.07f, 0.09f);
            // URP base cameras clear their own viewport: also draw this background in
            // the original camera, without changing its projection or culling settings.
            BoardSlot[] slots = FindObjectsOfType<BoardSlot>();
            material = slots[0].GetComponent<SpriteRenderer>().sharedMaterial;
            GameObject.Find("Scene/BG").GetComponent<SpriteRenderer>().enabled = false;
            background = new GameObject("VC5 command tabletop", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            background.transform.SetParent(transform, false);
            background.gameObject.layer = BackdropLayer;
            background.sprite = Resources.Load<Sprite>("VC5/Battlefield/command-table");
            background.sortingOrder = -50;
            FitBackground();

            Bounds boardBounds = new Bounds(slots[0].transform.position, Vector3.zero);
            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            foreach (BoardSlot slot in slots)
            {
                // Existing child is the plain green fill. The root remains the interaction overlay.
                slot.transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = false;
                slot.UseBattlefieldArt();
                Vector3[] hex = new Vector3[6];
                float radius = slot.radius * slot.transform.lossyScale.x;
                for (int i = 0; i < 6; i++)
                {
                    float angle = i * Mathf.PI / 3f;
                    hex[i] = slot.transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius * 0.975f;
                    boardBounds.Encapsulate(hex[i]);
                }
                Vector3[] shadow = (Vector3[])hex.Clone();
                for (int i = 0; i < 6; i++) shadow[i].y -= 0.035f;
                AddPolygon(vertices, colors, triangles, shadow, new Color(0.04f, 0.085f, 0.105f));
                bool scoring = Vc5ScoringZone.IsScoringCell(slot.x, slot.y);
                AddPolygon(vertices, colors, triangles, hex,
                    scoring ? new Color(0.20f, 0.275f, 0.294f) : new Color(0.16f, 0.24f, 0.26f));
                // Fine contour-style surface marks stay inside the hex, with no terrain semantics.
                for (int row = 0; row < 3; row++)
                {
                    Vector3 center = slot.transform.position + Vector3.up * (row - 1) * radius * 0.17f;
                    Vector3[] strip = { center + new Vector3(-radius * 0.63f, -0.003f, 0),
                        center + new Vector3(radius * 0.63f, radius * 0.045f - 0.003f, 0),
                        center + new Vector3(radius * 0.63f, radius * 0.045f + 0.003f, 0),
                        center + new Vector3(-radius * 0.63f, 0.003f, 0) };
                    AddPolygon(vertices, colors, triangles, strip, new Color(0.24f, 0.31f, 0.31f));
                }
                LineRenderer rim = CreateLine("VC5 hex rim", hex, scoring ? 0.018f : 0.006f, -5);
                SetLineColor(rim, scoring ? new Color(0.74f, 0.65f, 0.47f, 0.7f) : new Color(0.33f, 0.45f, 0.46f));
                if (scoring) { scoringSlots.Add(slot); scoringRims.Add(rim); }
            }
            CreateMeshObject("VC5 decorated tile surfaces", BuildMesh(vertices, colors, triangles), -10);
            CreatePlate(boardBounds);
            CreateAtmosphere();
        }

        private void FitBackground()
        {
            viewportWidth = Screen.width;
            viewportHeight = Screen.height;
            backdropCamera.aspect = (float)Screen.width / Screen.height;
            halfHeight = backdropCamera.orthographicSize;
            halfWidth = halfHeight * backdropCamera.aspect;
            viewCenter = new Vector3(gameCamera.transform.position.x, gameCamera.transform.position.y, 0f);
            float scale = Mathf.Max(halfWidth * 2f / background.sprite.bounds.size.x,
                halfHeight * 2f / background.sprite.bounds.size.y) * 1.02f;
            background.transform.localScale = Vector3.one * scale;
        }

        private void LateUpdate()
        {
            if (background == null) return;
            if (Screen.width != viewportWidth || Screen.height != viewportHeight) FitBackground();
            float t = Time.unscaledTime;
            background.transform.position = viewCenter + Vector3.right * (Mathf.Sin(t * Mathf.PI / 12f) * halfWidth * 6f / 1469f);
            lamp.transform.position = viewCenter + new Vector3(-halfWidth * 0.9f, halfHeight * 0.68f, 0f);
            lamp.transform.localScale = new Vector3(halfWidth * 0.34f, halfHeight * 0.64f, 1f);
            lightProperties.SetColor("_Color", new Color(1f, 0.79f, 0.45f, 0.035f + 0.015f * Mathf.Sin(t * Mathf.PI / 3f)));
            lamp.SetPropertyBlock(lightProperties);
            for (int i = 0; i < scoringRims.Count; i++)
            {
                // The original overlay is the authoritative legal-target feedback, drawn above us.
                float overlay = scoringSlots[i].GetComponent<SpriteRenderer>().color.a;
                float alpha = overlay > 0.04f ? 0.2f : 0.70f + 0.15f * Mathf.Sin(t * Mathf.PI / 2f);
                SetLineColor(scoringRims[i], new Color(0.90f, 0.78f, 0.50f, alpha));
            }
            UpdateDust(t);
        }

        private void CreatePlate(Bounds bounds)
        {
            Vector3 min = bounds.min - new Vector3(0.27f, 0.25f, 0);
            Vector3 max = bounds.max + new Vector3(0.27f, 0.25f, 0);
            const float bevel = 0.14f;
            Vector3[] corners = { new Vector3(min.x + bevel, min.y), new Vector3(max.x - bevel, min.y),
                new Vector3(max.x, min.y + bevel), new Vector3(max.x, max.y - bevel),
                new Vector3(max.x - bevel, max.y), new Vector3(min.x + bevel, max.y),
                new Vector3(min.x, max.y - bevel), new Vector3(min.x, min.y + bevel) };
            var vertices = new List<Vector3>(); var colors = new List<Color>(); var triangles = new List<int>();
            AddPolygon(vertices, colors, triangles, corners, new Color(0.08f, 0.13f, 0.16f));
            CreateMeshObject("VC5 tactical board plate", BuildMesh(vertices, colors, triangles), -25);
            SetLineColor(CreateLine("VC5 thin brass frame", corners, 0.018f, -24), new Color(0.62f, 0.52f, 0.36f));
        }

        private void CreateAtmosphere()
        {
            var vertices = new List<Vector3>(); var colors = new List<Color>(); var triangles = new List<int>();
            vertices.Add(Vector3.zero); colors.Add(Color.white);
            for (int i = 0; i < 32; i++)
            {
                float a = i * Mathf.PI * 2f / 32f;
                vertices.Add(new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f)); colors.Add(new Color(1f, 1f, 1f, 0f));
                triangles.Add(0); triangles.Add(i + 1); triangles.Add((i + 1) % 32 + 1);
            }
            lamp = CreateMeshObject("VC5 soft warm light", BuildMesh(vertices, colors, triangles), -45);
            lamp.gameObject.layer = BackdropLayer;
            dust = new Mesh { name = "VC5 14 drifting dust motes" };
            ownedMeshes.Add(dust); dust.MarkDynamic();
            int[] indices = new int[84];
            for (int i = 0; i < 14; i++)
            {
                int p = i * 4, j = i * 6;
                indices[j] = p; indices[j + 1] = p + 1; indices[j + 2] = p + 2;
                indices[j + 3] = p; indices[j + 4] = p + 2; indices[j + 5] = p + 3;
            }
            dust.vertices = dustVertices; dust.colors = dustColors; dust.uv = new Vector2[56]; dust.triangles = indices;
            CreateMeshObject("VC5 edge dust only", dust, -40).gameObject.layer = BackdropLayer;
        }

        private void UpdateDust(float time)
        {
            for (int i = 0; i < 14; i++)
            {
                float phase = Mathf.Repeat(time / (18f + i % 4) + i * 0.137f, 1f);
                float side = i % 2 == 0 ? -1f : 1f;
                Vector3 center = viewCenter + new Vector3(side * halfWidth * (0.53f + i % 3 * 0.06f),
                    (phase * 1.3f - 0.65f) * halfHeight, 0);
                float r = halfHeight * 0.002f;
                dustVertices[i * 4] = center + new Vector3(-r, -r);
                dustVertices[i * 4 + 1] = center + new Vector3(r, -r);
                dustVertices[i * 4 + 2] = center + new Vector3(r, r);
                dustVertices[i * 4 + 3] = center + new Vector3(-r, r);
                Color color = new Color(0.94f, 0.85f, 0.66f, Mathf.Sin(phase * Mathf.PI) * 0.12f);
                for (int k = 0; k < 4; k++) dustColors[i * 4 + k] = color;
            }
            dust.vertices = dustVertices; dust.colors = dustColors; dust.RecalculateBounds();
        }

        private static void AddPolygon(List<Vector3> vertices, List<Color> colors, List<int> triangles, Vector3[] points, Color color)
        {
            int first = vertices.Count;
            foreach (Vector3 point in points) { vertices.Add(point); colors.Add(color); }
            for (int i = 1; i < points.Length - 1; i++) { triangles.Add(first); triangles.Add(first + i); triangles.Add(first + i + 1); }
        }

        private Mesh BuildMesh(List<Vector3> vertices, List<Color> colors, List<int> triangles)
        {
            Mesh mesh = new Mesh { name = "VC5 battlefield visual mesh" };
            mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetTriangles(triangles, 0);
            mesh.uv = new Vector2[vertices.Count]; mesh.RecalculateBounds(); ownedMeshes.Add(mesh);
            return mesh;
        }

        private MeshRenderer CreateMeshObject(string name, Mesh mesh, int order)
        {
            GameObject obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            obj.transform.SetParent(transform, false);
            obj.GetComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = obj.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material; renderer.sortingOrder = order;
            return renderer;
        }

        private LineRenderer CreateLine(string name, Vector3[] points, float width, int order)
        {
            GameObject obj = new GameObject(name, typeof(LineRenderer)); obj.transform.SetParent(transform, false);
            LineRenderer line = obj.GetComponent<LineRenderer>(); line.useWorldSpace = true;
            line.sharedMaterial = material; line.loop = true; line.widthMultiplier = width;
            line.positionCount = points.Length; line.SetPositions(points); line.sortingOrder = order;
            return line;
        }

        private static void SetLineColor(LineRenderer line, Color color) { line.startColor = line.endColor = color; }

        private void OnDestroy() { foreach (Mesh mesh in ownedMeshes) Destroy(mesh); }
    }
}
