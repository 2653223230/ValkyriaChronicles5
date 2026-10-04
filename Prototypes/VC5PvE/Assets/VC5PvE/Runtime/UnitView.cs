using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VC5PvE
{
    /// <summary>HD-2D style billboard, combat feedback, and world-space status for one rules unit.</summary>
    public sealed class UnitView : MonoBehaviour
    {
        private const float CharacterHeight = 1.5f;
        private const float CharacterMaxWidth = 1f;
        private const float SelectionRadius = .53f;
        private static readonly Dictionary<string, Sprite> PlaceholderSprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Material> SharedMaterials = new Dictionary<string, Material>();
        private static Mesh shadowMesh, pedestalMesh;
        private const float FootHeight = .105f;

        private BoardView board;
        private Camera viewCamera;
        private UnitState unit;
        private Sprite[] sprites;
        private SpriteRenderer spriteRenderer;
        private Transform billboard;
        private Transform indicatorRoot;
        private Transform groundShadow;
        private Transform pedestal;
        private Transform factionRing;
        private LineRenderer selectionRing;
        private LineRenderer statusRing, statusRingOuter;
        private Transform hpFill, hpBack, apRoot;
        private readonly Transform[] apDiamonds = new Transform[8];
        private Transform statusMark, statusInspire, statusShield;
        private readonly Renderer[][] apParts = new Renderer[8][];
        private readonly Color[][] apColors = new Color[8][];
        private MaterialPropertyBlock indicatorProperties;
        public int PreviewApCost { get; private set; }
        private float apPreviewStarted;
        private Color teamColor;
        private Color spriteTint = Color.white;
        private Color statusColor;
        private float statusTime;
        private float landingTime;
        private bool spriteInMotion;
        private bool selected;
        private float selectionPulse;

        public string UnitId { get { return unit != null ? unit.Id : null; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSharedResources()
        {
            foreach (Material material in SharedMaterials.Values) DestroyGenerated(material);
            SharedMaterials.Clear();
            foreach (Sprite sprite in PlaceholderSprites.Values)
            {
                if (sprite == null) continue;
                Texture2D texture = sprite.texture;
                DestroyGenerated(sprite);
                DestroyGenerated(texture);
            }
            PlaceholderSprites.Clear();
            if (shadowMesh != null) DestroyGenerated(shadowMesh);
            shadowMesh = null;
            if (pedestalMesh != null) DestroyGenerated(pedestalMesh);
            pedestalMesh = null;
        }

        public void Initialize(BoardView owner, Camera camera, Sprite[] unitSprites)
        {
            indicatorProperties = new MaterialPropertyBlock();
            board = owner;
            viewCamera = camera;
            sprites = unitSprites;
            CreateVisuals();
        }

        public void SetUnit(UnitState state)
        {
            unit = state;
            transform.position = board.World(state.Position);
            teamColor = state.Team == Team.Enemy
                ? new Color(.95f, .33f, .30f)
                : new Color(.30f, .78f, .63f);
            spriteTint = state.Role == UnitRole.Guard ? new Color(.80f, .86f, .95f) : Color.white;
            spriteRenderer.color = spriteTint;
            if (factionRing != null)
                SetLineColor(factionRing.GetComponent<LineRenderer>(), state.Team == Team.Enemy
                    ? new Color(.72f, .32f, .24f, .8f)
                    : new Color(.26f, .61f, .57f, .8f));
            Sprite sprite = SpriteFor(state.Role);
            if (sprite != null) spriteRenderer.sprite = sprite;
            else spriteRenderer.sprite = PlaceholderSprite(state.Role, teamColor);
            FitSprite();
            UpdateHeadIndicators();

            float health = state.MaxHp > 0 ? Mathf.Clamp01((float)state.Hp / state.MaxHp) : 0f;
            hpFill.localScale = new Vector3(health * .78f, .065f, .02f);
            hpFill.localPosition = new Vector3(-.39f + health * .39f, .09f, .025f);
            SetIndicatorColor(hpFill.GetComponent<Renderer>(),HealthBarColor.Evaluate(health));
            hpBack.localPosition = new Vector3(0f, .09f, 0f);
            int ap = Mathf.Clamp(state.Ap, 0, apDiamonds.Length);
            apRoot.localPosition = new Vector3(0f, .24f, .015f);
            for (int i = 0; i < apDiamonds.Length; i++)
            {
                bool active = i < ap;
                apDiamonds[i].gameObject.SetActive(active);
                if (active)
                    apDiamonds[i].localPosition = new Vector3((i - (ap - 1) * .5f) * .18f, 0f, 0f);
            }
            statusShield.gameObject.SetActive(state.Shield > 0);
            statusMark.gameObject.SetActive(state.MarkExpiresRound > 0);
            statusInspire.gameObject.SetActive(state.InspireExpiresRound > 0);
        }

        public void SetPreviewApCost(int cost)
        {
            int next=unit==null?0:Mathf.Clamp(cost,0,unit.Ap);
            if(next!=PreviewApCost)apPreviewStarted=Time.unscaledTime;
            PreviewApCost=next;
            UpdateApPreview();
        }
        private void UpdateApPreview()
        {
            if(unit==null)return;
            float fade=.16f+.84f*(.5f+.5f*Mathf.Cos((Time.unscaledTime-apPreviewStarted)*Mathf.PI*2f/.95f));
            for(int i=0;i<apDiamonds.Length;i++)
            {
                if(apParts[i]==null)continue;
                bool spending=PreviewApCost>0 && i>=unit.Ap-PreviewApCost && i<unit.Ap;
                for(int j=0;j<apParts[i].Length;j++)
                {
                    Color c=apColors[i][j];c.a=spending?fade:1f;
                    SetIndicatorColor(apParts[i][j],c);
                }
            }
        }
        private void SetIndicatorColor(Renderer target,Color color)
        {
            indicatorProperties.Clear();indicatorProperties.SetColor("_BaseColor",color);indicatorProperties.SetColor("_Color",color);
            target.SetPropertyBlock(indicatorProperties);
        }

        public void SetSelected(bool value)
        {
            selected = value;
            if (selectionRing != null) selectionRing.enabled = selected;
        }

        public bool ContainsScreenPoint(Vector2 screen)
        {
            if (viewCamera == null) viewCamera = Camera.main;
            if (viewCamera == null || spriteRenderer == null || spriteRenderer.sprite == null) return false;

            Bounds local = spriteRenderer.sprite.bounds;
            Vector3 min = local.min;
            Vector3 max = local.max;
            Vector3[] corners =
            {
                spriteRenderer.transform.TransformPoint(new Vector3(min.x, min.y, 0f)),
                spriteRenderer.transform.TransformPoint(new Vector3(min.x, max.y, 0f)),
                spriteRenderer.transform.TransformPoint(new Vector3(max.x, min.y, 0f)),
                spriteRenderer.transform.TransformPoint(new Vector3(max.x, max.y, 0f))
            };

            float minX = float.PositiveInfinity;
            float minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float maxY = float.NegativeInfinity;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 projected = viewCamera.WorldToScreenPoint(corners[i]);
                if (projected.z <= 0f) return false;
                minX = Mathf.Min(minX, projected.x);
                minY = Mathf.Min(minY, projected.y);
                maxX = Mathf.Max(maxX, projected.x);
                maxY = Mathf.Max(maxY, projected.y);
            }
            return screen.x >= minX && screen.x <= maxX && screen.y >= minY && screen.y <= maxY;
        }

        public IEnumerator AnimateMove(Vector3 destination)
        {
            Vector3 start = transform.position;
            float elapsed = 0f;
            const float duration = .22f;
            spriteInMotion = true;
            try
            {
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    float eased = t * t * (3f - 2f * t);
                    transform.position = Vector3.Lerp(start, destination, eased);
                    spriteRenderer.transform.localPosition = Vector3.up * (FootHeight + Mathf.Sin(t * Mathf.PI) * .19f);
                    spriteRenderer.transform.localScale = SpriteScale() * (1f + Mathf.Sin(t * Mathf.PI) * .035f);
                    yield return null;
                }
            }
            finally
            {
                spriteRenderer.transform.localPosition = Vector3.up * FootHeight;
                FitSprite();
                spriteInMotion = false;
            }
            transform.position = destination;
            landingTime = .2f;
        }

        public IEnumerator AnimateAction(UnitView recipient, bool offensive, bool healing)
        {
            if (recipient == null) yield break;
            Vector2 direction = ScreenDirection(recipient);
            if (healing)
            {
                recipient.PlayStatusEffect(new Color(.42f, 1f, .72f));
                PlayStatusEffect(new Color(.72f, 1f, .77f));
                yield return new WaitForSeconds(.32f);
                yield break;
            }

            if (offensive)
            {
                recipient.PlayStatusEffect(new Color(1f, .48f, .30f));
                bool ranged = unit != null && (unit.AttackRange > 1 || unit.Role == UnitRole.Mage || unit.Role == UnitRole.Shooter);
                if (ranged)
                    yield return AnimateProjectile(recipient, unit != null && unit.Role == UnitRole.Mage
                        ? new Color(.52f, .88f, 1f) : new Color(1f, .76f, .40f));
                else
                    yield return AnimateMeleeLunge(direction);
            }
        }

        public void PlayStatusEffect(Color color)
        {
            statusColor = color;
            statusTime = .48f;
        }

        public void ShowFloatingText(string text, Color color)
        {
            var go = new GameObject("Floating combat text");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, CharacterHeight + .42f, -.02f);
            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.fontSize = 46;
            mesh.characterSize = .035f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;
            var floating = go.AddComponent<FloatingTextView>();
            floating.Initialize(viewCamera);
        }

        private void LateUpdate()
        {
            if (viewCamera == null) viewCamera = Camera.main;
            if (viewCamera != null && billboard != null)
            {
                billboard.rotation = Quaternion.LookRotation(-viewCamera.transform.forward,viewCamera.transform.up);
            }

            float time = Time.time;
            float breath = Mathf.Sin(time * 2.2f + GetInstanceID() * .17f);
            if (spriteRenderer != null && !spriteInMotion)
            {
                spriteRenderer.transform.localPosition = Vector3.up * (FootHeight + breath * .012f);
                spriteRenderer.transform.localScale = SpriteScale() * (1f + breath * .004f);
            }
            UpdateHeadIndicators();

            landingTime = Mathf.Max(0f, landingTime - Time.deltaTime);
            statusTime = Mathf.Max(0f, statusTime - Time.deltaTime);
            selectionPulse += Time.deltaTime * 3.2f;
            float landing = landingTime / .2f;
            if (groundShadow != null) groundShadow.localScale = new Vector3(.96f + landing * .12f, 1f, .62f + landing * .08f);
            if (pedestal != null) pedestal.localScale = new Vector3(1f + landing * .04f, 1f, 1f + landing * .04f);
            if (selectionRing != null)
            {
                float pulse = .5f + Mathf.Sin(selectionPulse) * .5f;
                selectionRing.startWidth = selectionRing.endWidth = selected ? .045f + pulse * .025f : .035f;
                Color c = teamColor;
                c.a = selected ? .65f + pulse * .3f : .5f;
                selectionRing.startColor = selectionRing.endColor = c;
            }
            if (spriteRenderer != null)
            {
                float flash = statusTime > 0f ? Mathf.Sin((.48f - statusTime) * Mathf.PI * 5f) * (statusTime / .48f) : 0f;
                spriteRenderer.color = statusTime > 0f
                    ? Color.Lerp(spriteTint, statusColor, Mathf.Clamp01(flash * .42f))
                    : spriteTint;
            }
            UpdateStatusRings();
            UpdateApPreview();
        }

        private void CreateVisuals()
        {
            GameObject shadowObject = new GameObject("Soft ground shadow", typeof(MeshFilter), typeof(MeshRenderer));
            groundShadow = shadowObject.transform;
            groundShadow.SetParent(transform, false);
            groundShadow.localPosition = new Vector3(0f, .012f, 0f);
            groundShadow.localScale = new Vector3(.96f, 1f, .62f);
            shadowObject.GetComponent<MeshFilter>().sharedMesh = SoftShadowMesh();
            MeshRenderer shadowRenderer = shadowObject.GetComponent<MeshRenderer>();
            shadowRenderer.sharedMaterial = TransparentMaterial();
            shadowRenderer.shadowCastingMode = ShadowCastingMode.Off;
            shadowRenderer.receiveShadows = false;

            var pedestalObject = new GameObject("Carved limestone miniature base", typeof(MeshFilter), typeof(MeshRenderer));
            pedestal = pedestalObject.transform;
            pedestal.SetParent(transform, false);
            pedestalObject.GetComponent<MeshFilter>().sharedMesh = PedestalMesh();
            var pedestalRenderer = pedestalObject.GetComponent<MeshRenderer>();
            Material stone = GetMaterial(Shader.Find("Universal Render Pipeline/Lit"), "limestone-base");
            SetMaterialColor(stone, new Color(.87f, .91f, .77f));
            if (board.GroundAtlas != null)
            {
                stone.SetTexture("_BaseMap", board.GroundAtlas);
                stone.SetTextureScale("_BaseMap", new Vector2(.48f, .96f));
                stone.SetTextureOffset("_BaseMap", new Vector2(.51f, .02f));
            }
            stone.SetFloat("_Smoothness", .15f);
            pedestalRenderer.sharedMaterial = stone;
            pedestalRenderer.shadowCastingMode = ShadowCastingMode.Off;
            factionRing = CreateGroundRing("Inset faction enamel", .345f, .345f, .012f);
            factionRing.localPosition = Vector3.up * .108f;
            selectionRing = CreateGroundRing("Selection breathing ring", SelectionRadius, .39f, .045f).GetComponent<LineRenderer>();
            selectionRing.sharedMaterial = TransparentMaterial();
            selectionRing.enabled = false;

            var spriteObject = new GameObject("HD-2D unit billboard");
            billboard = spriteObject.transform;
            billboard.SetParent(transform, false);
            spriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
            var pixelShader = Shader.Find("VC5PvE/PixelCharacter");
            if (pixelShader != null) spriteRenderer.sharedMaterial = GetMaterial(pixelShader, "pixel-character");
            spriteRenderer.sortingOrder = 10;
            spriteRenderer.sprite = PlaceholderSprite(UnitRole.Warrior, new Color(.3f, .8f, .7f));

            var indicators = new GameObject("World status indicators");
            indicatorRoot = indicators.transform;
            indicatorRoot.SetParent(transform, false);
            hpBack = MakeBar("HP bar frame", new Vector3(0f, .09f, 0f), new Vector3(.86f, .10f, .03f), new Color(.035f, .08f, .075f, .95f));
            hpFill = MakeBar("HP bar", new Vector3(-.39f, .09f, .025f), new Vector3(.78f, .065f, .02f), new Color(.34f, .9f, .57f));
            var apParent = new GameObject("AP diamonds");
            apRoot = apParent.transform;
            apRoot.SetParent(indicatorRoot, false);
            apRoot.localPosition = new Vector3(0f, .24f, .015f);
            for (int i = 0; i < apDiamonds.Length; i++)
            {
                var slotObject = new GameObject("AP diamond slot");
                Transform slot = slotObject.transform;
                slot.SetParent(apRoot, false);
                slot.localRotation = Quaternion.Euler(0f, 0f, 45f);
                MakeDiamondPart(slot, "AP diamond outline", new Vector3(.12f, .12f, .026f), .0f,
                    new Color(.027f, .216f, .329f));
                MakeDiamondPart(slot, "AP diamond glow", new Vector3(.082f, .082f, .018f), .023f,
                    new Color(.208f, .812f, 1f), true);
                apParts[i]=slot.GetComponentsInChildren<Renderer>();
                apColors[i]=new Color[apParts[i].Length];
                for(int j=0;j<apParts[i].Length;j++)apColors[i][j]=apParts[i][j].sharedMaterial.GetColor("_BaseColor");
                apDiamonds[i] = slot;
                slot.gameObject.SetActive(false);
            }
            statusShield = MakeStatus("Shield", new Vector3(-.24f, .44f, .04f), new Color(.48f, .80f, 1f));
            statusMark = MakeStatus("Mark", new Vector3(0f, .44f, .04f), new Color(1f, .46f, .33f));
            statusInspire = MakeStatus("Inspire", new Vector3(.24f, .44f, .04f), new Color(1f, .82f, .35f));

            statusRing = CreateBillboardRing("Status pulse inner", .30f, .035f);
            statusRingOuter = CreateBillboardRing("Status pulse outer", .36f, .022f);
            statusRing.enabled = false;
            statusRingOuter.enabled = false;
        }

        private GameObject MakeDiamondPart(Transform parent, string label, Vector3 scale, float depth, Color color, bool emissive = false)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = label;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = new Vector3(0f, 0f, depth);
            part.transform.localScale = scale;
            Renderer renderer = part.GetComponent<Renderer>();
            color.a=.998f; // Use transparent AP materials so preview opacity can fade without changing shared assets.
            renderer.sharedMaterial = SimpleMaterial(color, emissive);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            Destroy(part.GetComponent<Collider>());
            return part;
        }

        private void UpdateHeadIndicators()
        {
            if (indicatorRoot == null || spriteRenderer == null || spriteRenderer.sprite == null) return;
            if (viewCamera == null) viewCamera = Camera.main;
            if (viewCamera == null) return;

            indicatorRoot.rotation = billboard != null ? billboard.rotation : Quaternion.LookRotation(-viewCamera.transform.forward, viewCamera.transform.up);
            Vector3 spriteHead = spriteRenderer.transform.TransformPoint(new Vector3(0f, spriteRenderer.sprite.bounds.max.y, 0f));
            indicatorRoot.position = spriteHead + viewCamera.transform.up * .07f - viewCamera.transform.forward * .04f;
        }

        private void FitSprite()
        {
            spriteRenderer.transform.localScale = SpriteScale();
            spriteRenderer.transform.localPosition = Vector3.up * FootHeight;
        }

        private Vector3 SpriteScale()
        {
            if (spriteRenderer == null || spriteRenderer.sprite == null) return Vector3.one;
            Vector2 size = spriteRenderer.sprite.bounds.size;
            if (size.x <= .001f || size.y <= .001f) return Vector3.one;
            float scale = Mathf.Min(CharacterHeight / size.y, CharacterMaxWidth / size.x);
            return new Vector3(scale, scale, 1f);
        }

        private IEnumerator AnimateMeleeLunge(Vector2 direction)
        {
            Vector3 originalPosition = spriteRenderer.transform.localPosition;
            Quaternion originalRotation = spriteRenderer.transform.localRotation;
            float horizontal = Mathf.Clamp(direction.x, -1f, 1f);
            float vertical = Mathf.Clamp(direction.y, -1f, 1f);
            const float duration = .22f;
            float elapsed = 0f;
            spriteInMotion = true;
            try
            {
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float phase = Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI);
                    spriteRenderer.transform.localPosition = originalPosition + new Vector3(horizontal * .13f, vertical * .06f, -.015f) * phase;
                    spriteRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, -horizontal * 13f * phase);
                    yield return null;
                }
            }
            finally
            {
                spriteRenderer.transform.localPosition = Vector3.up * FootHeight;
                spriteRenderer.transform.localRotation = originalRotation;
                spriteInMotion = false;
            }
        }

        private IEnumerator AnimateProjectile(UnitView recipient, Color color)
        {
            Vector3 start = transform.position + Vector3.up * .92f;
            Vector3 end = recipient.transform.position + Vector3.up * .92f;
            GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = "Combat light projectile";
            projectile.transform.position = start;
            projectile.transform.localScale = Vector3.one * .13f;
            Renderer renderer = projectile.GetComponent<Renderer>();
            renderer.sharedMaterial = SimpleMaterial(color, true);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            Destroy(projectile.GetComponent<Collider>());

            GameObject trailObject = new GameObject("Combat light trail", typeof(LineRenderer));
            LineRenderer trail = trailObject.GetComponent<LineRenderer>();
            trail.useWorldSpace = true;
            trail.positionCount = 2;
            trail.startWidth = .075f;
            trail.endWidth = .012f;
            trail.sharedMaterial = TransparentMaterial();
            trail.startColor = new Color(color.r, color.g, color.b, .75f);
            trail.endColor = new Color(color.r, color.g, color.b, .08f);
            trail.numCapVertices = 2;
            trail.shadowCastingMode = ShadowCastingMode.Off;

            try
            {
                float elapsed = 0f;
                const float duration = .24f;
                Vector3 previous = start;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    Vector3 position = Vector3.Lerp(start, end, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * .16f;
                    projectile.transform.position = position;
                    trail.SetPosition(0, previous);
                    trail.SetPosition(1, position);
                    previous = position;
                    yield return null;
                }
            }
            finally
            {
                if (projectile != null) Destroy(projectile);
                if (trailObject != null) Destroy(trailObject);
            }
        }

        private Vector2 ScreenDirection(UnitView other)
        {
            if (viewCamera == null) viewCamera = Camera.main;
            if (viewCamera == null || other == null) return Vector2.right;
            Vector3 from = viewCamera.WorldToScreenPoint(transform.position);
            Vector3 to = viewCamera.WorldToScreenPoint(other.transform.position);
            Vector2 direction = new Vector2(to.x - from.x, to.y - from.y);
            return direction.sqrMagnitude > .001f ? direction.normalized : Vector2.right;
        }

        private void UpdateStatusRings()
        {
            bool visible = statusTime > 0f;
            if (statusRing == null || statusRingOuter == null) return;
            statusRing.enabled = statusRingOuter.enabled = visible;
            if (!visible) return;
            float progress = 1f - statusTime / .48f;
            float alpha = 1f - progress;
            SetLineColor(statusRing, new Color(statusColor.r, statusColor.g, statusColor.b, alpha));
            SetLineColor(statusRingOuter, new Color(statusColor.r, statusColor.g, statusColor.b, alpha * .65f));
            statusRing.transform.localScale = Vector3.one * (.72f + progress * .45f);
            statusRingOuter.transform.localScale = Vector3.one * (.8f + progress * .58f);
        }

        private Transform CreateGroundRing(string name, float radiusX, float radiusZ, float width)
        {
            var child = new GameObject(name, typeof(LineRenderer));
            child.transform.SetParent(transform, false);
            child.transform.localPosition = new Vector3(0f, .055f, 0f);
            LineRenderer line = child.GetComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 48;
            line.startWidth = line.endWidth = width;
            line.numCapVertices = 2;
            line.sharedMaterial = TransparentMaterial();
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / line.positionCount;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radiusX, 0f, Mathf.Sin(angle) * radiusZ));
            }
            return child.transform;
        }

        private LineRenderer CreateBillboardRing(string name, float radius, float width)
        {
            var child = new GameObject(name, typeof(LineRenderer));
            child.transform.SetParent(billboard, false);
            child.transform.localPosition = new Vector3(0f, .74f, -.02f);
            LineRenderer line = child.GetComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 40;
            line.startWidth = line.endWidth = width;
            line.numCapVertices = 2;
            line.sharedMaterial = TransparentMaterial();
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / line.positionCount;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }
            return line;
        }

        private Transform MakeBar(string label, Vector3 position, Vector3 scale, Color color)
        {
            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = label;
            bar.transform.SetParent(indicatorRoot, false);
            bar.transform.localPosition = position;
            bar.transform.localScale = scale;
            Renderer renderer = bar.GetComponent<Renderer>();
            renderer.sharedMaterial = SimpleMaterial(color, false);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            Destroy(bar.GetComponent<Collider>());
            return bar.transform;
        }

        private Transform MakeStatus(string label, Vector3 position, Color color)
        {
            GameObject badge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            badge.name = label + " status pip";
            badge.transform.SetParent(indicatorRoot, false);
            badge.transform.localPosition = position;
            badge.transform.localScale = new Vector3(.105f, .105f, .03f);
            badge.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Renderer renderer = badge.GetComponent<Renderer>();
            renderer.sharedMaterial = SimpleMaterial(color, true);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            Destroy(badge.GetComponent<Collider>());
            return badge.transform;
        }

        private Sprite SpriteFor(UnitRole role)
        {
            if (sprites == null || sprites.Length == 0) return null;
            int index = (int)role;
            return index >= 0 && index < sprites.Length ? sprites[index] : null;
        }

        private static Sprite PlaceholderSprite(UnitRole role, Color team)
        {
            string key = role.ToString() + ":" + ColorUtility.ToHtmlStringRGB(team);
            Sprite cached;
            if (PlaceholderSprites.TryGetValue(key, out cached)) return cached;
            const int w = 24, h = 32;
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color dark = new Color(.08f, .12f, .12f, 1f);
            Color cloth = role == UnitRole.Mage ? new Color(.48f, .48f, .88f) : role == UnitRole.Support ? new Color(.38f, .76f, .59f) : team;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color c = clear;
                bool head = y >= 23 && y <= 28 && x >= 8 && x <= 15;
                bool body = y >= 9 && y < 23 && x >= 6 && x <= 17;
                bool legs = y < 10 && x >= 7 && x <= 16;
                bool trim = body && (y == 10 || y == 21 || x == 6 || x == 17);
                if (head) c = y >= 27 ? new Color(.95f, .76f, .53f) : dark;
                if (body || legs) c = trim ? dark : cloth;
                if ((int)role == (int)UnitRole.Mage && y >= 11 && y < 27 && x == 19) c = new Color(.95f, .82f, .4f);
                if ((int)role == (int)UnitRole.Shooter && y >= 11 && y < 25 && x == 4) c = dark;
                texture.SetPixel(x, y, c);
            }
            texture.Apply(false, true);
            Sprite created = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(.5f, .12f), 24f, 0, SpriteMeshType.FullRect);
            PlaceholderSprites.Add(key, created);
            return created;
        }

        private static Mesh PedestalMesh()
        {
            if (pedestalMesh != null) return pedestalMesh;
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            // Eight chamfered stone sides, with separate face vertices for crisp bevel lighting.
            float[] radii = { .37f, .42f, .42f, .36f };
            float[] heights = { .012f, .035f, .065f, .105f };
            for (int ring = 0; ring < 3; ring++)
            for (int side = 0; side < 8; side++)
            {
                float a = (side + .5f) * Mathf.PI / 4f, b = (side + 1.5f) * Mathf.PI / 4f;
                int start = vertices.Count;
                vertices.Add(new Vector3(Mathf.Cos(a)*radii[ring], heights[ring], Mathf.Sin(a)*radii[ring]));
                vertices.Add(new Vector3(Mathf.Cos(b)*radii[ring], heights[ring], Mathf.Sin(b)*radii[ring]));
                vertices.Add(new Vector3(Mathf.Cos(b)*radii[ring+1], heights[ring+1], Mathf.Sin(b)*radii[ring+1]));
                vertices.Add(new Vector3(Mathf.Cos(a)*radii[ring+1], heights[ring+1], Mathf.Sin(a)*radii[ring+1]));
                triangles.AddRange(new[] {start,start+2,start+1,start,start+3,start+2});
            }
            int top = vertices.Count;
            vertices.Add(Vector3.up * .105f);
            for (int side = 0; side < 8; side++)
            {
                float a = (side + .5f) * Mathf.PI / 4f;
                vertices.Add(new Vector3(Mathf.Cos(a)*.36f,.105f,Mathf.Sin(a)*.36f));
                triangles.AddRange(new[] {top,top+1+(side+1)%8,top+1+side});
            }
            foreach (Vector3 v in vertices) uv.Add(new Vector2(v.x + .5f, v.z + .5f));
            pedestalMesh = new Mesh { name = "Bevelled octagonal limestone base" };
            pedestalMesh.SetVertices(vertices); pedestalMesh.SetUVs(0,uv); pedestalMesh.SetTriangles(triangles,0);
            pedestalMesh.RecalculateNormals(); pedestalMesh.RecalculateBounds();
            return pedestalMesh;
        }

        private static Mesh SoftShadowMesh()
        {
            if (shadowMesh != null) return shadowMesh;
            const int segments = 32;
            var vertices = new Vector3[segments + 1];
            var colors = new Color[segments + 1];
            var triangles = new int[segments * 3];
            vertices[0] = Vector3.zero;
            colors[0] = new Color(.025f, .045f, .04f, .26f);
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * .5f, 0f, Mathf.Sin(angle) * .5f);
                colors[i + 1] = new Color(.025f, .045f, .04f, 0f);
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = (i + 1) % segments + 1;
                triangles[i * 3 + 2] = i + 1;
            }
            shadowMesh = new Mesh { name = "VC5 PvE radial soft shadow" };
            shadowMesh.vertices = vertices;
            shadowMesh.colors = colors;
            shadowMesh.triangles = triangles;
            shadowMesh.RecalculateNormals();
            shadowMesh.RecalculateBounds();
            return shadowMesh;
        }

        private static Material TransparentMaterial()
        {
            // Sprite/Default multiplies Mesh and LineRenderer vertex colors, including alpha.
            // URP/Unlit does not reliably consume those colors in this project shader setup.
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            return GetMaterial(shader, "soft-shadow-transparent", true);
        }

        private static Material SimpleMaterial(Color color, bool emissive)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            string key = ColorUtility.ToHtmlStringRGBA(color) + (emissive ? "-e" : "-n");
            bool transparent = color.a < .999f;
            if (transparent) key += "-t";
            Material cached;
            if (SharedMaterials.TryGetValue(key, out cached) && cached != null) return cached;
            Material material = GetMaterial(shader, key, transparent);
            SetMaterialColor(material, color);
            if (emissive && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color);
            }
            return material;
        }

        private static Material GetMaterial(Shader shader, string key, bool transparent = false)
        {
            if (shader == null) return null;
            string cacheKey = key + (transparent ? "-transparent" : "");
            Material cached;
            if (SharedMaterials.TryGetValue(cacheKey, out cached) && cached != null) return cached;
            var material = new Material(shader);
            if (transparent)
            {
                if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)RenderQueue.Transparent;
            }
            SharedMaterials[cacheKey] = material;
            return material;
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material == null) return;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        }

        private static void DestroyGenerated(Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }

        private static void SetLineColor(LineRenderer line, Color color)
        {
            if (line != null) line.startColor = line.endColor = color;
        }
    }

    internal sealed class FloatingTextView : MonoBehaviour
    {
        private Camera viewCamera;
        private float age;
        public void Initialize(Camera camera) { viewCamera = camera; }
        private void Update()
        {
            age += Time.deltaTime;
            transform.localPosition += Vector3.up * Time.deltaTime * .72f;
            if (viewCamera == null) viewCamera = Camera.main;
            if (viewCamera != null) transform.rotation = Quaternion.LookRotation(viewCamera.transform.position - transform.position, Vector3.up);
            TextMesh text = GetComponent<TextMesh>();
            if (text != null) { Color c = text.color; c.a = Mathf.Clamp01(1f - age / .8f); text.color = c; }
            if (age >= .8f) Destroy(gameObject);
        }
    }
}
