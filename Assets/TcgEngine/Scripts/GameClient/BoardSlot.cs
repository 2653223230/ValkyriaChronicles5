using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Client;
using TcgEngine.UI;

namespace TcgEngine.Client
{
    /// <summary>
    /// Visual representation of a Slot.cs
    /// Will highlight when can be interacted with
    /// slot的可视化表示
    /// 当可以交互时将高亮显示
    /// </summary>
    
    [RequireComponent(typeof(MeshCollider))]
    public class BoardSlot : BSlot
    {
        public BoardSlotType type;
        public int x;
        public int y;

        public float radius = 0.5f; // 六边形半径
        public float height = 0.01f; // 柱体高度

        private bool is_scoring_zone = false;

        private static List<BoardSlot> slot_list = new List<BoardSlot>();

        protected override void Awake()
        {
            CreateHexagonCollider();
            base.Awake();
            slot_list.Add(this);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            slot_list.Remove(this);
        }

        private void Start()
        {
            if (x < Slot.x_min || x > Slot.x_max || y < Slot.y_min || y > Slot.y_max)
                Debug.LogError("Board Slot X and Y value must be within the min and max set for those values, check Slot.cs script to change those min/max.");

            is_scoring_zone = Vc5ScoringZone.IsScoringCell(x, y);
            if (is_scoring_zone)
                EnsureScoringZoneLabel();
        }

        protected override void Update()
        {
            base.Update();

            if (!GameClient.Get().IsReady())
                return;

            BoardCard bcard_selected = PlayerControls.Get().GetSelected();
            HandCard drag_card = HandCard.GetDrag();

            Game gdata = GameClient.Get().GetGameData();
            Player player = GameClient.Get().GetPlayer();
            Slot slot = GetSlot();
            Card dcard = drag_card?.GetCard();
            Card slot_card = gdata.GetSlotCard(GetSlot());
            bool your_turn = GameClient.Get().IsYourTurn();
            collide.enabled = slot_card == null; //Disable collider when a card is here有卡片时禁用对撞机

            color_r = 255;
            color_g = 255;
            color_b = 255;
            //Find target opacity value查找目标不透明度值
            target_alpha = 0f;

            if (is_scoring_zone)
            {
                target_alpha = 0.78f;
                color_r = 255f;
                color_g = 48f;
                color_b = 48f;
            }

            BoardCard focus_card = BoardCard.GetFocus();
            Card hover_card = focus_card != null ? focus_card.GetCard() : null;
            if (Vc5C3Rules.IsCard(dcard) || (gdata.selector != SelectorType.None
                && Vc5C3Rules.IsCard(gdata.GetCard(gdata.selector_caster_uid))))
                hover_card = null;
            if (hover_card != null && hover_card.CardData.IsCharacter() && slot.p == hover_card.slot.p)
            {
                int dx = slot.x - hover_card.slot.x;
                int dy = slot.y - hover_card.slot.y;
                int dz = (hover_card.slot.x + hover_card.slot.y) - (slot.x + slot.y);
                int hexDistance = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy), Mathf.Abs(dz));
                if (hexDistance > 0 && hexDistance <= Vc5DemoGrid.AttackRange(hover_card))
                {
                    target_alpha = 1f;
                    color_r = 255;
                    color_g = 0;
                    color_b = 0;
                }
            }

            // --- 调试随从不能打出到棋盘的问题 ---
            if (your_turn && dcard != null && dcard.CardData.IsBoardCard())
            {
                bool canPlay = gdata.CanPlayCard(dcard, slot);
                if (canPlay)
                {
                    // 可以在该格子打出随从，高亮
                    target_alpha = 1f; //hightlight when dragging a character or artifact拖动角色或工件时高亮显示
                }
                else
                {
                    // 不能在该格子打出随从，输出详细调试信息（仅在拖拽随从时触发）
                    string cardId = dcard != null ? dcard.card_id : "null";
                    string log = "[BoardSlot] Cannot play minion here. "
                                 + "player_id=" + GameClient.Get().GetPlayerID()
                                 + ", slot=(" + slot.x + "," + slot.y + "," + slot.p + ")"
                                 + ", card=" + cardId
                                 + ", your_turn=" + your_turn
                                 + ", slot_has_card=" + (slot_card != null);
                    Debug.Log(log);
                }
            }

            if (your_turn && dcard != null && dcard.CardData.IsRequireTarget() && gdata.CanPlayCard(dcard, slot))
            {
                target_alpha = 1f; //Highlight when dragin a spell with target用目标拖动法术时突出显示
            }

            if (gdata.selector == SelectorType.SelectTarget && player.player_id == gdata.selector_player_id)
            {
                Card caster = gdata.GetCard(gdata.selector_caster_uid);
                AbilityData ability = AbilityData.Get(gdata.selector_ability_id);
                if(ability != null && slot_card == null && ability.CanTarget(gdata, caster, slot))
                    target_alpha = 1f; //Highlight when selecting a target and slot are valid选择目标时突出显示，插槽有效
                if (ability != null && slot_card != null && ability.CanTarget(gdata, caster, slot_card))
                    target_alpha = 1f; //Highlight when selecting a target and cards are valid选择目标时突出显示，卡片有效
            }

            Card select_card = bcard_selected?.GetCard();
            bool can_do_move = your_turn && select_card != null && slot_card == null && gdata.CanManualMoveCard(select_card, slot);
            bool can_do_attack = your_turn && select_card != null && slot_card != null && gdata.CanAttackTarget(select_card, slot_card);

            if (can_do_attack || can_do_move)
            {
                target_alpha = 1f;
                if (can_do_attack)
                {
                    color_r = 255;
                    color_g = 0;
                    color_b = 0;
                }
                else
                {
                    color_r = 0;
                    color_g = 255;
                    color_b = 0;
                }
            }
        }

        //Find the actual slot coordinates of this board slot
        //找到该板槽的实际槽坐标
        public override Slot GetSlot()
        {
            int p = 0;

            if (type == BoardSlotType.FlipX)
            {
                int pid = GameClient.Get().GetPlayerID();
                int px = x;
                if ((pid % 2) == 1)
                    px = Slot.x_max - x + Slot.x_min; //Flip X coordinate if not the first player
                return new Slot(px, y, p);
            }

            if (type == BoardSlotType.FlipY)
            {
                int pid = GameClient.Get().GetPlayerID();
                int py = y;
                if ((pid % 2) == 1)
                    py = Slot.y_max - y + Slot.y_min; //Flip Y coordinate if not the first player
                return new Slot(x, py, p);
            }

            if (type == BoardSlotType.PlayerSelf)
                p = GameClient.Get().GetPlayerID();
            if(type == BoardSlotType.PlayerOpponent)
                p = GameClient.Get().GetOpponentPlayerID();
           
            return new Slot(x, y, p);
        }

        //When clicking on the slot
        //点击插槽时
        public void OnMouseDown()
        {
            if (GameUI.IsOverUI())
                return;

            Game gdata = GameClient.Get().GetGameData();
            int player_id = GameClient.Get().GetPlayerID();

            if (gdata.selector == SelectorType.SelectTarget && player_id == gdata.selector_player_id)
            {
                Slot slot = GetSlot();
                Card slot_card = gdata.GetSlotCard(slot);
                if (slot_card == null)
                {
                    Card caster = gdata.GetCard(gdata.selector_caster_uid);
                    AbilityData ability = AbilityData.Get(gdata.selector_ability_id);
                    if (ability != null && caster != null && ability.CanTarget(gdata, caster, slot))
                        GameClient.Get().SelectSlot(slot);
                    else
                        WarningText.ShowInvalidTarget();
                }
            }
        }

        public void CreateHexagonCollider()
        {
            Mesh mesh = new Mesh();
            // 顶点数组（14个顶点 = 底面6 + 顶面6 + 底面中心1 + 顶面中心1）
            Vector3[] vertices = new Vector3[14];
            int[] triangles = new int[72]; // 36(侧面) + 18(底面) + 18(顶面)
            // 生成底面和顶面的外圈顶点
            for (int i = 0; i < 6; i++)
            {
                float angle = 60f * i * Mathf.Deg2Rad;
                // 底面顶点（Z=0）
                vertices[i] = new Vector3(
                    radius * Mathf.Cos(angle),
                    radius * Mathf.Sin(angle),
                    0
                    );
                // 顶面顶点（Z=height）
                vertices[i + 6] = vertices[i] + new Vector3(0, 0, height);
            }
            
            // 添加底面和顶面的中心顶点
            vertices[12] = new Vector3(0, 0, 0);         // 底面中心
            vertices[13] = new Vector3(0, 0, height);    // 顶面中心

            // 生成侧面三角形（36个索引）
            for (int i = 0; i < 6; i++)
            {
                int a = i;
                int b = (i + 1) % 6;
                int c = a + 6;
                int d = b + 6;
                
                triangles[i * 6] = a;
                triangles[i * 6 + 1] = b;
                triangles[i * 6 + 2] = c;
                triangles[i * 6 + 3] = b;
                triangles[i * 6 + 4] = d;
                triangles[i * 6 + 5] = c;
            }
                
            // 生成底面三角形（18个索引）
            int triIndex = 36;
            for (int i = 0; i < 6; i++)
            {
                int current = i;
                int next = (i + 1) % 6;
                triangles[triIndex++] = 12;     // 底面中心
                triangles[triIndex++] = current;
                triangles[triIndex++] = next;
            }
                
            // 生成顶面三角形（18个索引，反向确保法线朝外）
            for (int i = 0; i < 6; i++)
            {
                int current = 6 + i;
                int next = 6 + ((i + 1) % 6);
                triangles[triIndex++] = 13;     // 顶面中心
                triangles[triIndex++] = next;    // 反向顶点顺序
                triangles[triIndex++] = current;
            }
                
            // 应用网格数据
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            GetComponent<MeshCollider>().sharedMesh = mesh;
            GetComponent<MeshCollider>().convex = true; // 启用凸面碰撞
        }

        private void EnsureScoringZoneLabel()
        {
            if (transform.Find("ScoringZoneLabel") != null)
                return;

            GameObject label = new GameObject("ScoringZoneLabel");
            label.transform.SetParent(transform, false);
            label.transform.localPosition = new Vector3(0f, 0f, -0.03f);
            label.transform.localRotation = Quaternion.identity;
            label.transform.localScale = Vector3.one;

            TextMesh text = label.AddComponent<TextMesh>();
            text.text = "得分区";
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 28;
            text.characterSize = 0.075f;
            text.color = Color.white;

            MeshRenderer mesh = label.GetComponent<MeshRenderer>();
            if (mesh != null)
                mesh.sortingOrder = 20;
        }

    }
}
