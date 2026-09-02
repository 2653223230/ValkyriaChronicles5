using TcgEngine.Client;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class Vc5C3PreviewGraphic : MaskableGraphic
    {
        public Vc5C3PreviewOverlay overlay;
        static readonly Color Cyan = new Color(0.15f, 1f, 0.86f, 0.95f);
        static readonly Color Gold = new Color(1f, 0.85f, 0.15f, 0.95f);
        static readonly Color Red = new Color(1f, 0.2f, 0.25f, 1f);

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (overlay == null || overlay.Preview == null || overlay.Actor == null) return;
            Vc5C3Plan plan = overlay.Preview.plan;
            Card actor = overlay.Actor;
            foreach (BSlot slot in BSlot.GetAll())
            {
                if (!(slot is BoardSlot hex)) continue;
                Slot cell = slot.GetSlot();
                int distance = Vc5DemoGrid.HexDistance(plan.destination, cell);
                bool legal = plan.legalSlots.Contains(cell);
                if (!legal && (!plan.valid || distance > plan.range)) continue;
                bool extra = Vc5C3Rules.IsPreciseMove(overlay.Spell)
                    ? Vc5DemoGrid.HexDistance(actor.slot, cell) > actor.move_Range
                    : distance > Vc5DemoGrid.AttackRange(actor);
                Color color = extra ? Gold : Cyan;
                Vector2[] corners = new Vector2[6];
                for (int i = 0; i < 6; i++)
                {
                    float angle = i * Mathf.PI / 3f;
                    corners[i] = overlay.Project(hex.transform.TransformPoint(new Vector3(Mathf.Cos(angle) * hex.radius * 0.94f,
                        Mathf.Sin(angle) * hex.radius * 0.94f, 0f)));
                }
                Vector2 center = overlay.Project(hex.transform.position);
                Color fill = color; fill.a = legal ? 0.18f : 0.09f;
                for (int i = 0; i < 6; i++)
                {
                    Triangle(mesh, center, corners[i], corners[(i + 1) % 6], fill);
                    Line(mesh, corners[i], corners[(i + 1) % 6], extra ? 3f : 1.5f, color);
                }
            }
            BoardCard actorBoard = BoardCard.Get(actor.uid);
            if (actorBoard != null) Box(mesh, overlay.Project(actorBoard.transform.position), plan.valid ? Cyan : Color.gray, !plan.valid);
            if (plan.valid && overlay.Spell.card_id == Vc5C3Rules.Prefix + "heavy_break")
                foreach (Player player in overlay.Data.players)
                    if (player.player_id != actor.player_id)
                        foreach (Card enemy in player.cards_board)
                        {
                            if (overlay.Preview.damage.ContainsKey(enemy.uid) || Vc5DemoGrid.HexDistance(plan.destination, enemy.slot) > plan.range) continue;
                            BoardCard board = BoardCard.Get(enemy.uid);
                            if (board != null) Box(mesh, overlay.Project(board.transform.position), new Color(1f, 0.2f, 0.25f, 0.3f), false);
                        }
            foreach (var hit in overlay.Preview.damage)
            {
                BoardCard target = BoardCard.Get(hit.Key);
                if (target == null) continue;
                Vector2 point = overlay.Project(target.transform.position);
                Box(mesh, point, Red, false);
                if (overlay.Spell.card_id == Vc5C3Rules.Prefix + "weakpoint_snipe")
                {
                    Line(mesh, point + Vector2.left * 16f, point + Vector2.right * 16f, 2f, Red);
                    Line(mesh, point + Vector2.down * 16f, point + Vector2.up * 16f, 2f, Red);
                }
            }
            for (int i = 1; i < plan.path.Count; i++)
            {
                Vector2 from = overlay.Point(plan.path[i - 1]);
                Vector2 to = overlay.Point(plan.path[i]);
                Line(mesh, from, to, 3f, Gold);
                Vector2 direction = (to - from).normalized;
                Vector2 normal = new Vector2(-direction.y, direction.x);
                Triangle(mesh, to, to - direction * 13f + normal * 6f, to - direction * 13f - normal * 6f, Gold);
            }
        }

        static void Box(VertexHelper mesh, Vector2 point, Color color, bool filled)
        {
            Vector2 a = point + new Vector2(-32f, -42f), b = point + new Vector2(32f, -42f);
            Vector2 c = point + new Vector2(32f, 42f), d = point + new Vector2(-32f, 42f);
            Line(mesh, a, b, 3f, color); Line(mesh, b, c, 3f, color);
            Line(mesh, c, d, 3f, color); Line(mesh, d, a, 3f, color);
            if (filled) { color.a = 0.45f; Triangle(mesh, a, b, c, color); Triangle(mesh, a, c, d, color); }
        }

        static void Line(VertexHelper mesh, Vector2 a, Vector2 b, float width, Color color)
        {
            Vector2 delta = b - a;
            Vector2 offset = new Vector2(-delta.y, delta.x).normalized * width * 0.5f;
            Triangle(mesh, a - offset, a + offset, b + offset, color);
            Triangle(mesh, a - offset, b + offset, b - offset, color);
        }

        static void Triangle(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(a, color, Vector2.zero); mesh.AddVert(b, color, Vector2.zero); mesh.AddVert(c, color, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
        }
    }
}
