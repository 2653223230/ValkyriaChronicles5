using UnityEngine;
using UnityEngine.UI;
using TcgEngine.UI;

namespace TcgEngine.Client
{
    /// <summary>Client-only presentation for the reviewed VC5 board standees.</summary>
    public class Vc5BoardStandeeView : MonoBehaviour
    {
        private const float FeetY = -0.9f;
        private const float CharacterHeight = 3f;
        private static Mesh disk;
        private BoardCard board;
        private CardUI cardUI;
        private GameClient client;
        private MeshRenderer pedestal, shadow;
        private LineRenderer rim, selection, hitSpark;
        private Image originalShield;
        private Text shieldText;
        private Vector3 previousPosition;
        private bool wasMoving;
        private int previousProtection;
        private float hitTime, protectionTime, landingTime, recoilTime;
        private Color factionColor;

        public void Initialize(BoardCard owner)
        {
            board = owner;
            cardUI = owner.GetComponent<CardUI>();
            client = GameClient.Get();
            Card card = owner.GetCard();
            factionColor = card.player_id == client.GetPlayerID()
                ? new Color(0.28f, 0.9f, 0.84f) : new Color(1f, 0.53f, 0.48f);
            previousProtection = card.r4_shield + card.GetStatusValue(StatusType.Armor);
            previousPosition = transform.position;

            pedestal = CreateDisk("Standee thin base", new Vector3(2f, 0.5f, 1f), FeetY,
                new Color(0.06f, 0.12f, 0.17f));
            shadow = CreateDisk("Standee ground shadow", new Vector3(2.3f, 0.57f, 1f), FeetY - 0.1f,
                new Color(0f, 0f, 0f, 0.3f));
            rim = CreateRing("Standee faction rim", 1f, 0.25f, 0.035f);
            selection = CreateRing("Standee selection ring", 1.15f, 0.35f, 0.07f);
            hitSpark = CreateLine("Standee hit spark", 0.055f);
            hitSpark.positionCount = 8;
            hitSpark.SetPositions(new[] { Vector3.zero, Vector3.left * 0.35f, Vector3.zero,
                Vector3.right * 0.35f, Vector3.zero, Vector3.up * 0.35f,
                Vector3.zero, Vector3.down * 0.35f });
            hitSpark.transform.localPosition = new Vector3(0.7f, 0.35f, 0f);
            hitSpark.enabled = false;

            // Keep the familiar blue attack/red life art and existing delayed life updates.
            ArrangeBadge(cardUI.attack_icon, cardUI.attack, new Vector2(-90f, -95f), new Vector2(64f, 58f));
            ArrangeBadge(cardUI.hp_icon, cardUI.hp, new Vector2(90f, -95f), new Vector2(64f, 58f));
            ArrangeBadge(board.armor_icon, board.armor, new Vector2(45f, -38f), new Vector2(60f, 68f));
            board.armor_icon.color = new Color(0.76f, 0.82f, 0.9f);
            Transform oldShield = board.armor_icon.transform.parent.Find("R4ShieldIcon");
            originalShield = oldShield.GetComponent<Image>();
            shieldText = originalShield.GetComponentInChildren<Text>();
            ArrangeBadge(originalShield, shieldText, new Vector2(45f, -38f), new Vector2(60f, 68f));
            originalShield.color = new Color(1f, 0.83f, 0.42f);

            RectTransform statusRect = (RectTransform)board.status_group.transform;
            statusRect.anchoredPosition = new Vector2(0f, 235f);
            statusRect.sizeDelta = new Vector2(250f, 38f);
            BoxCollider collider = GetComponent<BoxCollider>();
            collider.center = new Vector3(0f, FeetY + CharacterHeight * 0.5f, 0f);
            collider.size = new Vector3(2.25f, CharacterHeight, 0.2f);

            client.onCardDamaged += OnDamaged;
            client.onAttackStart += OnAttack;
        }

        private void OnDestroy()
        {
            if (client != null)
            {
                client.onCardDamaged -= OnDamaged;
                client.onAttackStart -= OnAttack;
            }
        }

        private void LateUpdate()
        {
            if (!client.IsReady()) return;
            Card card = board.GetCard();
            bool alive = !board.IsDead();
            int order = board.card_sprite.sortingOrder;
            shadow.sortingOrder = order - 4;
            pedestal.sortingOrder = order - 3;
            rim.sortingOrder = order - 2;
            selection.sortingOrder = order - 1;
            hitSpark.sortingOrder = order + 2;
            shadow.enabled = pedestal.enabled = rim.enabled = alive;

            // BoardCard continues to compute legal target/focus colors, but only the base lights up.
            board.card_glow.enabled = false;
            board.card_shadow.enabled = false;
            cardUI.frame_image.enabled = false;
            cardUI.attack_icon.enabled = cardUI.hp_icon.enabled = alive;
            cardUI.attack.enabled = cardUI.hp.enabled = alive;
            int armor = card.GetStatusValue(StatusType.Armor);
            int shield = card.r4_shield;
            int totalProtection = armor + shield;
            if (totalProtection != previousProtection)
            {
                protectionTime = 0.35f;
                if (totalProtection < previousProtection) hitTime = 0.22f;
                previousProtection = totalProtection;
            }
            board.armor.enabled = board.armor_icon.enabled = alive && armor > 0;
            shieldText.enabled = originalShield.enabled = alive && shield > 0;
            originalShield.rectTransform.anchoredPosition = new Vector2(35f, -38f);
            board.armor_icon.rectTransform.anchoredPosition = new Vector2(shield > 0 ? 100f : 35f, -38f);
            Vector3 badgeScale = Vector3.one * (1f + protectionTime * 0.35f);
            originalShield.rectTransform.localScale = badgeScale;
            board.armor_icon.rectTransform.localScale = badgeScale;

            bool moving = (transform.position - previousPosition).sqrMagnitude > 0.000001f;
            if (wasMoving && !moving) landingTime = 0.18f;
            previousPosition = transform.position;
            wasMoving = moving;
            hitTime = Mathf.Max(0f, hitTime - Time.deltaTime);
            protectionTime = Mathf.Max(0f, protectionTime - Time.deltaTime);
            landingTime = Mathf.Max(0f, landingTime - Time.deltaTime);
            recoilTime = Mathf.Max(0f, recoilTime - Time.deltaTime);
            float lift = alive ? (moving ? Mathf.Abs(Mathf.Sin(Time.time * 15f)) * 0.1f
                : Mathf.Sin(Time.time * 2f + GetInstanceID()) * 0.015f) : 0f;
            float shake = Mathf.Sin(hitTime * 100f) * hitTime * 0.45f;
            Transform art = board.card_sprite.transform;
            float scale = CharacterHeight / board.card_sprite.sprite.bounds.size.y;
            art.localScale = Vector3.one * scale;
            art.localPosition = new Vector3(shake - recoilTime * 0.3f,
                FeetY + CharacterHeight * 0.5f + lift - landingTime * 0.15f, 0f);
            board.card_sprite.color = card.HasStatus(StatusType.Stealth) ? Color.gray
                : Color.white * (1f + hitTime * 4f);
            Color artColor = board.card_sprite.color;
            artColor.a = 1f;
            board.card_sprite.color = artColor;
            float highlightAlpha = board.card_glow.color.a;
            selection.enabled = alive && (highlightAlpha > 0.03f || protectionTime > 0f);
            SetLineColor(selection, new Color(factionColor.r, factionColor.g, factionColor.b,
                Mathf.Max(highlightAlpha, protectionTime * 2f)));
            selection.transform.localScale = Vector3.one * (1f + landingTime * 0.3f);
            hitSpark.enabled = alive && hitTime > 0f;
            hitSpark.transform.localScale = Vector3.one * (1.3f - hitTime);
            SetLineColor(hitSpark, new Color(1f, 0.84f, 0.5f, Mathf.Min(1f, hitTime * 8f)));
        }

        private void OnDamaged(Card target, int value)
        {
            if (target.uid == board.GetCardUID() && value > 0) hitTime = 0.22f;
        }

        private void OnAttack(Card actor, Card target)
        {
            if (actor.uid == board.GetCardUID()) recoilTime = 0.16f;
        }

        private static void ArrangeBadge(Image icon, Text label, Vector2 position, Vector2 size)
        {
            icon.rectTransform.anchoredPosition = position;
            icon.rectTransform.sizeDelta = size;
            icon.raycastTarget = false;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            label.fontSize = 40;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 22;
            label.resizeTextMaxSize = 40;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
        }

        private LineRenderer CreateLine(string name, float width)
        {
            GameObject child = new GameObject(name, typeof(LineRenderer));
            child.transform.SetParent(transform, false);
            LineRenderer line = child.GetComponent<LineRenderer>();
            line.sharedMaterial = board.card_sprite.sharedMaterial;
            line.useWorldSpace = false;
            line.widthMultiplier = width;
            line.numCapVertices = 2;
            line.numCornerVertices = 2;
            line.sortingLayerID = board.card_sprite.sortingLayerID;
            return line;
        }

        private LineRenderer CreateRing(string name, float radiusX, float radiusY, float width)
        {
            LineRenderer line = CreateLine(name, width);
            line.loop = true;
            line.positionCount = 32;
            for (int i = 0; i < 32; i++)
            {
                float angle = i * Mathf.PI * 2f / 32f;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY, 0f));
            }
            line.transform.localPosition = new Vector3(0f, FeetY, 0f);
            SetLineColor(line, factionColor);
            return line;
        }

        private static void SetLineColor(LineRenderer line, Color color)
        {
            line.startColor = line.endColor = color;
        }

        private MeshRenderer CreateDisk(string name, Vector3 size, float y, Color color)
        {
            if (disk == null)
            {
                disk = new Mesh { name = "VC5 standee shared disk" };
                Vector3[] vertices = new Vector3[33];
                Color[] colors = new Color[33];
                int[] triangles = new int[96];
                for (int i = 0; i < 33; i++) colors[i] = Color.white;
                for (int i = 0; i < 32; i++)
                {
                    float angle = i * Mathf.PI * 2f / 32f;
                    vertices[i + 1] = new Vector3(Mathf.Cos(angle) * 0.5f, Mathf.Sin(angle) * 0.5f, 0f);
                    triangles[i * 3] = 0;
                    triangles[i * 3 + 1] = i + 1;
                    triangles[i * 3 + 2] = (i + 1) % 32 + 1;
                }
                disk.vertices = vertices;
                disk.colors = colors;
                disk.uv = new Vector2[33];
                disk.triangles = triangles;
                disk.RecalculateBounds();
            }
            GameObject child = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            child.transform.SetParent(transform, false);
            child.transform.localPosition = new Vector3(0f, y, 0f);
            child.transform.localScale = size;
            child.GetComponent<MeshFilter>().sharedMesh = disk;
            MeshRenderer render = child.GetComponent<MeshRenderer>();
            render.sharedMaterial = board.card_sprite.sharedMaterial;
            render.sortingLayerID = board.card_sprite.sortingLayerID;
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            properties.SetColor("_Color", color);
            render.SetPropertyBlock(properties);
            return render;
        }
    }
}
