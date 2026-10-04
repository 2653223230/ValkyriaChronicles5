using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VC5PvE
{
    /// <summary>Owns the visual-only 8x8 forest-ruin board and its piece views.</summary>
    public sealed class BoardView : MonoBehaviour
    {
        public const int BoardSize = 8;
        public GameObject[] TreePrefabs;
        public GameObject[] RockPrefabs;
        public GameObject[] ShrubPrefabs;
        public GameObject[] RuinPrefabs;
        public Sprite[] UnitSprites;
        public Texture2D GroundAtlas;
        public Sprite[] ScenerySprites;

        [Header("Board palette")]
        [SerializeField] private Color forestA = new Color(0.67f, 0.79f, 0.52f);
        [SerializeField] private Color forestB = new Color(0.73f, 0.83f, 0.57f);
        [SerializeField] private Color stone = new Color(0.76f, 0.75f, 0.65f);
        [SerializeField] private Color cliff = new Color(0.46f, 0.40f, 0.33f);

        private readonly Dictionary<string, UnitView> unitViews = new Dictionary<string, UnitView>();
        private readonly List<GameObject> highlightObjects = new List<GameObject>();
        private readonly List<GameObject> intentObjects = new List<GameObject>();
        private readonly List<LineRenderer> intentLines = new List<LineRenderer>();
        private readonly List<Transform> driftingMotes = new List<Transform>();
        private readonly List<Vector3> moteOrigins = new List<Vector3>();
        private readonly List<float> motePhases = new List<float>();
        private Transform boardRoot;
        private Transform unitRoot;
        private Camera viewCamera;
        private Material tileMaterialA, tileMaterialB, stoneMaterial, cliffMaterial;
        private Material highlightMaterial, intentMaterial, fireflyMaterial, shadowMaterial;
        private Texture2D surfaceTexture, stoneTexture;
        private Material sceneryMaterial;
        private BattleState currentState;

        public void Initialize(BattleState state, Camera camera)
        {
            viewCamera = camera != null ? camera : Camera.main;
            currentState = state;
            ClearGenerated();
            DestroyMaterials();
            CreateMaterials();
            boardRoot = new GameObject("Forest Ruins Board").transform;
            boardRoot.SetParent(transform, false);
            unitRoot = new GameObject("Units").transform;
            unitRoot.SetParent(transform, false);
            BuildBoard(state);
            BuildCoordinates();
            BuildObstacleProps(state);
            Refresh(state);
        }

        public void Refresh(BattleState state)
        {
            if (state == null) return;
            currentState = state;
            if (boardRoot == null) Initialize(state, viewCamera);

            var alive = new HashSet<string>();
            foreach (var unit in state.Units)
            {
                if (unit == null || !unit.IsAlive) continue;
                alive.Add(unit.Id);
                UnitView view;
                if (!unitViews.TryGetValue(unit.Id, out view) || view == null)
                {
                    var go = new GameObject("Unit " + unit.Id);
                    go.transform.SetParent(unitRoot, false);
                    view = go.AddComponent<UnitView>();
                    view.Initialize(this, viewCamera, UnitSprites);
                    unitViews[unit.Id] = view;
                }
                view.SetUnit(unit);
            }

            var removed = new List<string>();
            foreach (var pair in unitViews)
            {
                if (!alive.Contains(pair.Key))
                {
                    if (pair.Value != null) Destroy(pair.Value.gameObject);
                    removed.Add(pair.Key);
                }
            }
            foreach (string id in removed) unitViews.Remove(id);
        }

        public void PreviewAp(string actorId, int cost)
        {
            foreach(var pair in unitViews)
                if(pair.Value!=null)pair.Value.SetPreviewApCost(pair.Key==actorId?cost:0);
        }

        public Vector3 World(GridPos pos)
        {
            return new Vector3(pos.X - 3.5f, 0f, 3.5f - pos.Y);
        }

        private void Update()
        {
            float time = Time.time;
            for (int i = 0; i < driftingMotes.Count; i++)
            {
                Transform mote = driftingMotes[i];
                if (mote == null) continue;
                Vector3 origin = moteOrigins[i];
                float phase = motePhases[i];
                mote.position = origin + new Vector3(Mathf.Sin(time * .24f + phase) * .055f,
                    Mathf.Sin(time * .8f + phase) * .035f, Mathf.Cos(time * .19f + phase) * .04f);
                mote.Rotate(Vector3.up, Mathf.Sin(time * .35f + phase) * 4f * Time.deltaTime, Space.World);
            }
        }

        private void BuildCoordinates()
        {
            for (int i = 0; i < BoardSize; i++)
            {
                Coordinate(((char)('A' + i)).ToString(), new Vector3(i - 3.5f, .035f, 4.08f));
                Coordinate((i + 1).ToString(), new Vector3(-4.12f, .035f, 3.5f - i));
            }
        }

        private void Coordinate(string label, Vector3 position)
        {
            var go = new GameObject("Coordinate " + label);
            go.transform.SetParent(boardRoot, false); go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(90, 0, 0);
            var text = go.AddComponent<TextMesh>(); text.text = label;
            text.fontSize = 48; text.characterSize = .09f;
            text.anchor = TextAnchor.MiddleCenter; text.color = new Color(.84f, .78f, .5f);
        }

        public bool ScreenToGrid(Vector2 screen, out GridPos pos)
        {
            pos = default(GridPos);
            if (viewCamera == null) viewCamera = Camera.main;
            if (viewCamera == null) return false;
            Ray ray = viewCamera.ScreenPointToRay(screen);
            Plane boardPlane = new Plane(Vector3.up, Vector3.zero);
            float distance;
            if (!boardPlane.Raycast(ray, out distance) || distance < 0f) return false;
            Vector3 hit = ray.GetPoint(distance);
            int x = Mathf.FloorToInt(hit.x + 4f);
            int y = Mathf.FloorToInt(4f - hit.z);
            if (x < 0 || x >= BoardSize || y < 0 || y >= BoardSize) return false;
            pos = new GridPos(x, y);
            return true;
        }

        public void ShowHighlights(IEnumerable<GridPos> reachable, GridPos? target = null)
        {
            ClearHighlights();
            var ringTargets = new HashSet<GridPos>();
            var reachableColor = new Color(.28f, 1f, .96f, 1f);
            var targetColor = new Color(1f, .70f, .20f, 1f);
            if (reachable != null)
                foreach (GridPos pos in reachable)
                {
                    if (currentState != null && currentState.UnitAt(pos) != null)
                    {
                        AddDoubleRingHighlight(pos, "Unit target highlight", target.HasValue && target.Value == pos ? targetColor : reachableColor);
                        ringTargets.Add(pos);
                    }
                    else AddHighlight(pos, new Color(0.25f, 0.92f, 0.78f, 0.48f), 0.025f);
                }
            if (target.HasValue)
            {
                if (currentState != null && currentState.UnitAt(target.Value) != null)
                {
                    if (!ringTargets.Contains(target.Value)) AddDoubleRingHighlight(target.Value, "Unit target highlight", targetColor);
                }
                else AddHighlight(target.Value, new Color(1f, 0.70f, 0.20f, 0.62f), 0.04f);
            }
        }

        public void ShowActorHighlights(IEnumerable<UnitState> actors)
        {
            ClearHighlights();
            if (actors == null) return;
            foreach (var actor in actors)
            {
                if (actor == null || !actor.IsAlive) continue;
                AddDoubleRingHighlight(actor.Position, "Eligible card executor", new Color(.28f, 1f, .96f, 1f));
            }
        }

        private void AddDoubleRingHighlight(GridPos pos, string label, Color innerColor)
        {
            var center = World(pos) + Vector3.up * .075f;
            var outer = new GameObject(label + " white ring", typeof(LineRenderer));
            outer.transform.SetParent(boardRoot, false);
            outer.transform.position = center;
            var outerLine = outer.GetComponent<LineRenderer>();
            outerLine.useWorldSpace = false;
            outerLine.loop = true;
            outerLine.positionCount = 64;
            outerLine.startWidth = outerLine.endWidth = .065f;
            outerLine.numCapVertices = 3;
            outerLine.sharedMaterial = highlightMaterial;
            outerLine.startColor = outerLine.endColor = Color.white;
            var whiteBlock = new MaterialPropertyBlock();
            whiteBlock.SetColor("_BaseColor", Color.white);
            whiteBlock.SetColor("_Color", Color.white);
            outerLine.SetPropertyBlock(whiteBlock);
            for (int i = 0; i < outerLine.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / outerLine.positionCount;
                outerLine.SetPosition(i, new Vector3(Mathf.Cos(angle) * .62f, 0f, Mathf.Sin(angle) * .62f));
            }
            highlightObjects.Add(outer);

            var inner = new GameObject(label + " colored ring", typeof(LineRenderer));
            inner.transform.SetParent(boardRoot, false);
            inner.transform.position = center + Vector3.up * .006f;
            var innerLine = inner.GetComponent<LineRenderer>();
            innerLine.useWorldSpace = false;
            innerLine.loop = true;
            innerLine.positionCount = 64;
            innerLine.startWidth = innerLine.endWidth = .038f;
            innerLine.numCapVertices = 3;
            innerLine.sharedMaterial = highlightMaterial;
            innerLine.startColor = innerLine.endColor = innerColor;
            var colorBlock = new MaterialPropertyBlock();
            colorBlock.SetColor("_BaseColor", innerColor);
            colorBlock.SetColor("_Color", innerColor);
            innerLine.SetPropertyBlock(colorBlock);
            for (int i = 0; i < innerLine.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / innerLine.positionCount;
                innerLine.SetPosition(i, new Vector3(Mathf.Cos(angle) * .55f, 0f, Mathf.Sin(angle) * .55f));
            }
            highlightObjects.Add(inner);
        }

        public void ShowIntent(EnemyTurnPlan plan)
        {
            ClearIntent();
            if (plan == null || plan.Actions == null) return;
            foreach (ActionPlan action in plan.Actions)
            {
                if (action == null || action.Request == null) continue;
                var points = new List<Vector3>();
                UnitView actor;
                if (unitViews.TryGetValue(action.Request.ActorId, out actor) && actor != null)
                    points.Add(actor.transform.position + Vector3.up * 0.12f);
                if (action.Path != null)
                    foreach (GridPos step in action.Path) points.Add(World(step) + Vector3.up * 0.12f);
                if (action.Request.Destination.HasValue) points.Add(World(action.Request.Destination.Value) + Vector3.up * 0.12f);
                UnitView target;
                if (!string.IsNullOrEmpty(action.Request.TargetId) && unitViews.TryGetValue(action.Request.TargetId, out target) && target != null)
                    points.Add(target.transform.position + Vector3.up * 0.16f);
                if (points.Count < 2) continue;
                var lineObject = new GameObject("Enemy intent path");
                lineObject.transform.SetParent(boardRoot, false);
                var line = lineObject.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = points.Count;
                line.SetPositions(points.ToArray());
                line.startWidth = 0.045f;
                line.endWidth = 0.07f;
                line.material = intentMaterial;
                line.numCapVertices = 3;
                line.numCornerVertices = 2;
                intentLines.Add(line);
                AddIntentArrow(points);
                if (action.Request.TargetId != null && points.Count > 0)
                    AddWorldMarker(points[points.Count - 1], new Color(1f, 0.62f, 0.18f, 0.75f), 0.18f);
            }
        }

        public void ClearHighlights()
        {
            foreach (GameObject obj in highlightObjects) if (obj != null) Destroy(obj);
            highlightObjects.Clear();
        }

        public IEnumerator Animate(ActionPlan plan, ActionResult result)
        {
            if (plan == null || plan.Request == null) yield break;
            UnitView actor;
            if (unitViews.TryGetValue(plan.Request.ActorId, out actor) && actor != null)
            {
                Vector3 start = actor.transform.position;
                Vector3 destination = plan.Request.Destination.HasValue ? World(plan.Request.Destination.Value) : start;
                if (plan.Path != null && plan.Path.Count > 0)
                {
                    foreach (GridPos step in plan.Path)
                        yield return actor.AnimateMove(World(step));
                }
                else if (plan.Request.Destination.HasValue)
                    yield return actor.AnimateMove(destination);

                UnitView recipient = actor;
                UnitView target;
                if (!string.IsNullOrEmpty(plan.Request.TargetId) && unitViews.TryGetValue(plan.Request.TargetId, out target) && target != null)
                    recipient = target;

                if (result != null && result.Success)
                {
                    if (plan.DamageSummary != null || result.Healing > 0)
                        yield return actor.AnimateAction(recipient, plan.DamageSummary != null, result.Healing > 0);
                    else if (plan.Request.Kind == ActionKind.Card)
                        recipient.PlayStatusEffect(SupportEffectColor(plan.Request.CardId));
                }

                if (result != null && (result.Damage > 0 || result.Healing > 0))
                {
                    recipient.ShowFloatingText(result.Damage > 0 ? "-" + result.Damage : "+" + result.Healing,
                        result.Damage > 0 ? new Color(1f, 0.48f, 0.36f) : new Color(0.44f, 1f, 0.72f));
                    yield return new WaitForSeconds(0.55f);
                }
                UnitView pushedView;
                var pushed = currentState.FindUnit(plan.Request.TargetId);
                if (pushed != null && pushed.IsAlive && unitViews.TryGetValue(pushed.Id, out pushedView))
                {
                    Vector3 finalPosition = World(pushed.Position);
                    if ((pushedView.transform.position - finalPosition).sqrMagnitude > .001f)
                        yield return pushedView.AnimateMove(finalPosition);
                }
            }
        }

        private static Color SupportEffectColor(string cardId)
        {
            string id = cardId ?? string.Empty;
            if (id.IndexOf("cover", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return new Color(.42f, .72f, 1f);
            if (id.IndexOf("inspire", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return new Color(1f, .78f, .32f);
            return new Color(.55f, .86f, .88f);
        }

        public void SetVisible(bool visible)
        {
            if (boardRoot != null) boardRoot.gameObject.SetActive(visible);
            if (unitRoot != null) unitRoot.gameObject.SetActive(visible);
        }

        public void Select(string unitId)
        {
            foreach (var pair in unitViews)
                if (pair.Value != null) pair.Value.SetSelected(pair.Key == unitId);
        }

        private void BuildBoard(BattleState state)
        {
            BuildIslandFoundation();
            // Eight raised stone/earth cells with exposed sides form one readable floating island.
            for (int y = 0; y < BoardSize; y++)
            for (int x = 0; x < BoardSize; x++)
            {
                bool central = x >= 2 && x <= 5 && y >= 2 && y <= 5;
                GameObject tile = CreateTile(x, y, central ? stoneMaterial : ((x + y) % 2 == 0 ? tileMaterialA : tileMaterialB));
                tile.transform.SetParent(boardRoot, false);
                AddTopDetail(x, y, central);
            }
            BuildCliffWalls();
            BuildEdgeFoliage();
            BuildFireflies();
        }

        private void BuildIslandFoundation()
        {
            const int count = 20;
            var vertices = new Vector3[count * 3 + 1];
            vertices[count * 3] = new Vector3(0, -.025f, 0);
            for(int i=0;i<count;i++)
            {
                float angle=i*Mathf.PI*2/count;
                float cx=Mathf.Cos(angle),sz=Mathf.Sin(angle);
                float radius=5.15f / Mathf.Max(Mathf.Abs(cx),Mathf.Abs(sz));
                radius *= .98f + .035f*Mathf.Sin(i*13.7f);
                vertices[i]=new Vector3(cx*radius,-.025f,sz*radius);
                vertices[count+i]=new Vector3(cx*radius*.92f,-1.15f-.15f*Mathf.Sin(i*2.7f),sz*radius*.92f);
                vertices[count*2+i]=new Vector3(cx*radius*.65f,-2.25f-.22f*Mathf.Sin(i*3.7f),sz*radius*.65f);
            }
            var top=new List<int>();var sides=new List<int>();
            for(int i=0;i<count;i++)
            {
                int next=(i+1)%count;
                top.Add(count*3);top.Add(next);top.Add(i);
                for(int ring=0;ring<2;ring++)
                {
                    int a=ring*count+i,b=ring*count+next,c=(ring+1)*count+i,d=(ring+1)*count+next;
                    sides.Add(a);sides.Add(b);sides.Add(c);sides.Add(b);sides.Add(d);sides.Add(c);
                }
            }
            var mesh=new Mesh();mesh.vertices=vertices;mesh.subMeshCount=2;
            var uv=new Vector2[vertices.Length];for(int i=0;i<uv.Length;i++)uv[i]=new Vector2(.5f+vertices[i].x/10.6f,.5f+vertices[i].z/10.6f);mesh.uv=uv;
            mesh.SetTriangles(top,0);mesh.SetTriangles(sides,1);mesh.RecalculateNormals();
            var go=new GameObject("Floating island foundation");go.transform.SetParent(boardRoot,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials=new[] {tileMaterialA,cliffMaterial};
        }

        private void BuildObstacleProps(BattleState state)
        {
            if (state.Obstacles == null) return;
            foreach (GridPos obstacle in state.Obstacles)
            {
                var ruin = new GameObject("Impassable ruin " + obstacle);
                ruin.transform.SetParent(transform, false);
                ruin.transform.position = World(obstacle);
                // A full-cell masonry silhouette separates blocking terrain from edge foliage.
                ObstacleStone(ruin.transform, "Stone foundation", new Vector3(0,.06f,0), new Vector3(.94f,.12f,.94f), stoneMaterial);
                var lower = ObstacleStone(ruin.transform, "Lower masonry", new Vector3(0,.29f,0), new Vector3(.84f,.34f,.84f), stoneMaterial);
                var shade = new MaterialPropertyBlock();
                shade.SetColor("_BaseColor", new Color(.67f,.77f,.75f));
                lower.GetComponent<Renderer>().SetPropertyBlock(shade);
                ObstacleStone(ruin.transform, "Upper masonry", new Vector3(.012f,.61f,-.012f), new Vector3(.80f,.29f,.80f), stoneMaterial);
                ObstacleStone(ruin.transform, "Chamfer cap", new Vector3(0,.775f,0), new Vector3(.86f,.07f,.86f), stoneMaterial);
                ObstacleStone(ruin.transform, "Moss on crown", new Vector3(-.27f,.815f,.25f), new Vector3(.27f,.015f,.26f), tileMaterialB);
                // Recess-like crossed bands on the crown repeat the same blocked-cell motif.
                var bandA = ObstacleStone(ruin.transform, "Blocked seal A", new Vector3(.07f,.818f,-.06f), new Vector3(.42f,.009f,.035f), cliffMaterial);
                var bandB = ObstacleStone(ruin.transform, "Blocked seal B", new Vector3(.07f,.819f,-.06f), new Vector3(.42f,.009f,.035f), cliffMaterial);
                bandA.localRotation = Quaternion.Euler(0,45,0);
                bandB.localRotation = Quaternion.Euler(0,-45,0);
            }
        }

        private static Transform ObstacleStone(Transform parent, string label, Vector3 position, Vector3 size, Material material)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = label;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = size;
            part.GetComponent<Renderer>().sharedMaterial = material;
            // Rules own occupancy and pointer picking; visual colliders must not intercept it.
            Destroy(part.GetComponent<Collider>());
            return part.transform;
        }

        private GameObject CreateTile(int x, int y, Material topMaterial)
        {
            var go = new GameObject("Tile " + (char)('A' + x) + (y + 1));
            go.transform.position = new Vector3(x - 3.5f, 0f, 3.5f - y);
            var mesh = new Mesh { name = "Raised forest floor tile" };
            var vertices = new[] {
                new Vector3(-.5f, 0, -.5f), new Vector3(.5f, 0, -.5f), new Vector3(.5f, 0, .5f), new Vector3(-.5f, 0, .5f),
                new Vector3(-.5f, -.20f, -.5f), new Vector3(.5f, -.20f, -.5f), new Vector3(.5f, -.20f, .5f), new Vector3(-.5f, -.20f, .5f)
            };
            mesh.vertices = vertices;
            float u=x/8f, v=(7-y)/8f, step=1f/8f;
            mesh.uv = new[] {new Vector2(u,v),new Vector2(u+step,v),new Vector2(u+step,v+step),new Vector2(u,v+step),Vector2.zero,Vector2.zero,Vector2.zero,Vector2.zero};
            mesh.subMeshCount = 2;
            mesh.SetTriangles(new[] { 0,2,1, 0,3,2 }, 0);
            mesh.SetTriangles(new[] { 0,1,5, 0,5,4, 1,2,6, 1,6,5, 2,3,7, 2,7,6, 3,0,4, 3,4,7, 4,5,6, 4,6,7 }, 1);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new[] { topMaterial, cliffMaterial };
            return go;
        }

        private void BuildCliffWalls()
        {
            // Shallow warm strata keep the floating-island silhouette distinct without fencing in the board.
            for (int i = 0; i < 8; i++)
            {
                AddCliffBlock(new Vector3(-4f + i + .5f, -.39f, -4.03f), new Vector3(1.03f, .38f, .18f));
                AddCliffBlock(new Vector3(-4f + i + .5f, -.39f, 4.03f), new Vector3(1.03f, .38f, .18f));
                AddCliffBlock(new Vector3(-4.03f, -.39f, -3.5f + i), new Vector3(.18f, .38f, 1.03f));
                AddCliffBlock(new Vector3(4.03f, -.39f, -3.5f + i), new Vector3(.18f, .38f, 1.03f));
            }
        }

        private void AddCliffBlock(Vector3 position, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Layered cliff face"; wall.transform.SetParent(boardRoot, false);
            wall.transform.position = position; wall.transform.localScale = scale;
            wall.GetComponent<Renderer>().sharedMaterial = cliffMaterial;
            Destroy(wall.GetComponent<Collider>());
        }

        private void AddTopDetail(int x, int y, bool central)
        {
            Vector3 center = new Vector3(x - 3.5f, 0.012f, 3.5f - y);
            if (central)
            {
                // Fine seams make the 4x4 ancient plaza legible without adding obstacles.
                AddThinStoneSeam(center + new Vector3(-.49f, 0f, .47f), new Vector3(.98f, .012f, .025f));
                if ((x + y) % 2 == 0) AddThinStoneSeam(center + new Vector3(-.47f, 0f, -.48f), new Vector3(.026f, .012f, .96f));
            }
            else
            {
                // Fine warm seams preserve the 8x8 grid while keeping grass cells distinct from the plaza.
                AddThinStoneSeam(center + new Vector3(-.49f, 0f, .48f), new Vector3(.98f, .008f, .012f));
                if ((x + y) % 2 == 0)
                    AddThinStoneSeam(center + new Vector3(-.48f, 0f, -.49f), new Vector3(.012f, .008f, .98f));
                if ((x * 11 + y * 7) % 13 == 0) AddMossPatch(center + new Vector3(.24f, 0f, -.23f));
            }
        }

        private void AddThinStoneSeam(Vector3 position, Vector3 scale)
        {
            var seam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seam.name = "Plaza stone seam"; seam.transform.SetParent(boardRoot, false);
            seam.transform.position = position; seam.transform.localScale = scale;
            seam.GetComponent<Renderer>().sharedMaterial = cliffMaterial;
            Destroy(seam.GetComponent<Collider>());
        }

        private void AddMossPatch(Vector3 position)
        {
            GameObject patch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            patch.name = "Moss inlay"; patch.transform.SetParent(boardRoot, false);
            patch.transform.position = position; patch.transform.localScale = new Vector3(.18f, .008f, .12f);
            patch.GetComponent<Renderer>().sharedMaterial = tileMaterialB;
            Destroy(patch.GetComponent<Collider>());
        }

        private void BuildEdgeFoliage()
        {
            // Short canopies and ruins sit beyond the board edge so sprites and intent markers stay visible.
            for (int i = 0; i < 5; i++)
            {
                Vector3 treePosition = new Vector3(-3.5f + i * 1.75f, -.025f, -4.82f);
                SpawnOrPrimitive(TreePrefabs, PrimitiveType.Cylinder, treePosition, .52f, "Canopy tree");
                AddSoftShadow(treePosition + Vector3.forward * .08f, new Vector2(.34f, .24f));
                if (i == 0 || i == 4)
                    SpawnOrPrimitive(TreePrefabs, PrimitiveType.Cylinder, new Vector3(-4.82f, -.025f, -2.6f + i * 1.3f), 1.15f, "Canopy tree");
            }
            for (int i = 0; i < 4; i++)
            {
                SpawnOrPrimitive(ShrubPrefabs, PrimitiveType.Sphere, new Vector3(4.8f, -.04f, -2.7f + i * 1.65f), .52f, "Forest shrub");
            }
            for (int i = 0; i < 4; i++)
            {
                SpawnOrPrimitive(RockPrefabs, PrimitiveType.Cube, new Vector3(-3.3f + i * 2.2f, -.16f, 4.22f), .50f, "Ruin rubble");
                SpawnOrPrimitive(TreePrefabs, PrimitiveType.Cylinder, new Vector3(-3.5f+i*2.2f, -.05f,4.85f), 1.15f,"Canopy tree");
            }
            AddBrokenColumn(new Vector3(-4.23f, -.12f, -.65f));
            AddBrokenColumn(new Vector3(4.23f, -.12f, 1.15f));
            BuildDecorativeSteps();
        }

        private void BuildDecorativeSteps()
        {
            // Three shallow entry stones imply an old processional path without occupying a cell.
            for (int i = 0; i < 3; i++)
            {
                GameObject step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                step.name = "Outer ruin step";
                step.transform.SetParent(boardRoot, false);
                step.transform.position = new Vector3(-4.36f, -.06f + i * .035f, -.55f + i * .34f);
                step.transform.localScale = new Vector3(.34f, .10f, .30f);
                step.GetComponent<Renderer>().sharedMaterial = stoneMaterial;
                Destroy(step.GetComponent<Collider>());
            }
        }

        private void SpawnOrPrimitive(GameObject[] prefabs, PrimitiveType fallback, Vector3 position, float scale, string label)
        {
            if (ScenerySprites!=null && ScenerySprites.Length==4)
            {
                int slot=label=="Canopy tree" ? (Mathf.RoundToInt(Mathf.Abs(position.x*3+position.z))%2) : label=="Forest shrub" ? 3 : 2;
                Sprite sprite=ScenerySprites[slot];
                if(sprite!=null)
                {
                    var prop=new GameObject(label+" painted");prop.transform.SetParent(boardRoot,false);prop.transform.position=position;
                    prop.transform.rotation=viewCamera.transform.rotation;
                    var renderer=prop.AddComponent<SpriteRenderer>();renderer.sprite=sprite;
                    if(sceneryMaterial==null) sceneryMaterial=new Material(Shader.Find("VC5PvE/PixelCharacter"));
                    renderer.sharedMaterial=sceneryMaterial;
                    float height=slot<2 ? scale*3.1f : slot==3 ? scale*2.5f : scale*3.1f;
                    prop.transform.localScale=Vector3.one*(height/sprite.bounds.size.y);
                    renderer.flipX=position.x<0;return;
                }
            }
            if (label == "Canopy tree" && !HasPrefab(prefabs))
            {
                CreateFallbackTree(position, scale);
                return;
            }
            GameObject item = null;
            if (prefabs != null && prefabs.Length > 0)
            {
                int index = Mathf.Abs((label.GetHashCode() + Mathf.RoundToInt(position.x * 37f) + Mathf.RoundToInt(position.z * 19f)) % prefabs.Length);
                if (prefabs[index] != null) item = Instantiate(prefabs[index]);
            }
            if (item == null) item = GameObject.CreatePrimitive(fallback);
            item.name = label; item.transform.SetParent(boardRoot, false); item.transform.position = position;
            float variation = 0.76f + Mathf.Abs(Mathf.Sin(position.x * 7.13f + position.z * 3.41f)) * .18f;
            item.transform.localScale *= scale * variation;
            foreach (Renderer renderer in item.GetComponentsInChildren<Renderer>())
            {
                if (renderer.sharedMaterial == null) renderer.sharedMaterial = tileMaterialB;
                var original = renderer.sharedMaterial;
                Color originalColor = original.HasProperty("_BaseColor") ? original.GetColor("_BaseColor") : original.color;
                Color palette = label.Contains("stone") || label.StartsWith("Ruin") ? new Color(.78f,.75f,.65f) : new Color(.64f,.78f,.53f);
                renderer.sharedMaterial = TintMaterial(original, Color.Lerp(originalColor, palette, .72f));
            }
            foreach (Collider collider in item.GetComponentsInChildren<Collider>()) Destroy(collider);
        }

        private static bool HasPrefab(GameObject[] prefabs)
        {
            if (prefabs == null) return false;
            foreach (GameObject prefab in prefabs) if (prefab != null) return true;
            return false;
        }

        private void CreateFallbackTree(Vector3 position, float scale)
        {
            var tree = new GameObject("Forest canopy tree");
            tree.transform.SetParent(boardRoot, false); tree.transform.position = position;
            tree.transform.localScale = Vector3.one * Mathf.Max(scale, .55f);
            AddTreePart(tree.transform, PrimitiveType.Cylinder, new Vector3(0f,.38f,0f), new Vector3(.16f,.42f,.16f), new Color(.48f,.34f,.20f));
            AddTreePart(tree.transform, PrimitiveType.Sphere, new Vector3(0f,.84f,0f), new Vector3(.72f,.55f,.70f), new Color(.40f,.65f,.36f));
            AddTreePart(tree.transform, PrimitiveType.Sphere, new Vector3(-.22f,.78f,-.12f), new Vector3(.44f,.37f,.46f), new Color(.50f,.72f,.40f));
            AddTreePart(tree.transform, PrimitiveType.Sphere, new Vector3(.23f,.96f,.12f), new Vector3(.46f,.40f,.45f), new Color(.60f,.76f,.43f));
        }

        private void AddTreePart(Transform parent, PrimitiveType type, Vector3 localPosition, Vector3 scale, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(type); part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition; part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = MakeMaterial(color);
            Destroy(part.GetComponent<Collider>());
        }

        private void AddBrokenColumn(Vector3 position)
        {
            GameObject basePart = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            basePart.name = "Broken stone column"; basePart.transform.SetParent(boardRoot, false);
            basePart.transform.position = position; basePart.transform.localScale = new Vector3(.27f, .23f, .27f);
            basePart.transform.rotation = Quaternion.Euler(0f, 13f, 11f);
            basePart.GetComponent<Renderer>().sharedMaterial = stoneMaterial;
            Destroy(basePart.GetComponent<Collider>());
        }

        private void AddSoftShadow(Vector3 position, Vector2 size)
        {
            GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shadow.name = "Soft foliage ground shadow";
            shadow.transform.SetParent(boardRoot, false);
            shadow.transform.position = new Vector3(position.x, .006f, position.z);
            shadow.transform.localScale = new Vector3(size.x, .003f, size.y);
            shadow.GetComponent<Renderer>().sharedMaterial = shadowMaterial;
            Destroy(shadow.GetComponent<Collider>());
        }

        private void BuildFireflies()
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 2.399f;
                Vector3 p = new Vector3(Mathf.Cos(angle) * (4.55f + (i % 3) * .12f), .34f + (i % 3) * .12f,
                    Mathf.Sin(angle) * (4.48f + (i % 2) * .14f));
                var fly = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                fly.name = i % 2 == 0 ? "Floating pollen mote" : "Drifting leaf mote"; fly.transform.SetParent(boardRoot, false); fly.transform.position = p;
                fly.transform.localScale = Vector3.one * (i % 2 == 0 ? .028f : .04f);
                fly.GetComponent<Renderer>().sharedMaterial = fireflyMaterial;
                Destroy(fly.GetComponent<Collider>());
                driftingMotes.Add(fly.transform);
                moteOrigins.Add(p);
                motePhases.Add(i * 1.37f);
            }
        }

        private void AddHighlight(GridPos pos, Color color, float height)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "Grid highlight"; marker.transform.SetParent(boardRoot, false);
            marker.transform.position = World(pos) + Vector3.up * height;
            marker.transform.localScale = new Vector3(.87f, .008f, .87f);
            Material material = new Material(highlightMaterial); SetColor(material, color); marker.GetComponent<Renderer>().sharedMaterial = material;
            Destroy(marker.GetComponent<Collider>()); highlightObjects.Add(marker);
        }

        private void AddWorldMarker(Vector3 position, Color color, float size)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "Intent target marker"; marker.transform.SetParent(boardRoot, false);
            marker.transform.position = position; marker.transform.localScale = Vector3.one * size;
            Material material = new Material(highlightMaterial); SetColor(material, color); marker.GetComponent<Renderer>().sharedMaterial = material;
            Destroy(marker.GetComponent<Collider>()); intentObjects.Add(marker);
        }

        private void AddIntentArrow(List<Vector3> points)
        {
            Vector3 direction = points[points.Count - 1] - points[points.Count - 2];
            if (direction.sqrMagnitude < .0001f) return;
            GameObject arrow = new GameObject("Enemy intent arrowhead");
            arrow.name = "Enemy intent arrowhead"; arrow.transform.SetParent(boardRoot, false);
            arrow.transform.position = points[points.Count - 1];
            arrow.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
            arrow.transform.localScale = new Vector3(.14f, .22f, .14f);
            var mesh = new Mesh();
            mesh.vertices = new[] { new Vector3(-.5f,0f,-.5f), new Vector3(.5f,0f,-.5f), new Vector3(.5f,0f,.5f), new Vector3(-.5f,0f,.5f), new Vector3(0f,1f,0f) };
            mesh.triangles = new[] { 0,4,1, 1,4,2, 2,4,3, 3,4,0, 0,1,2, 0,2,3 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            arrow.AddComponent<MeshFilter>().sharedMesh = mesh;
            arrow.AddComponent<MeshRenderer>().sharedMaterial = intentMaterial;
            intentObjects.Add(arrow);
        }

        private void ClearIntent()
        {
            foreach (LineRenderer line in intentLines) if (line != null) Destroy(line.gameObject);
            intentLines.Clear();
            foreach (GameObject obj in intentObjects) if (obj != null) Destroy(obj);
            intentObjects.Clear();
        }

        private void CreateMaterials()
        {
            tileMaterialA = MakeMaterial(forestA);
            tileMaterialB = MakeMaterial(forestB);
            stoneMaterial = MakeMaterial(stone);
            cliffMaterial = MakeMaterial(cliff);
            highlightMaterial = MakeMaterial(new Color(.2f, .9f, .7f, .55f));
            intentMaterial = MakeMaterial(new Color(1f, .65f, .18f, .9f));
            fireflyMaterial = MakeMaterial(new Color(1f, .66f, .22f, 1f));
            shadowMaterial = MakeMaterial(new Color(.27f, .24f, .19f, .13f));
            surfaceTexture = CreateSurfaceTexture(false);
            stoneTexture = CreateSurfaceTexture(true);
            ApplySurface(tileMaterialA, surfaceTexture, .14f);
            ApplySurface(tileMaterialB, surfaceTexture, .14f);
            ApplySurface(stoneMaterial, stoneTexture, .18f);
            ApplySurface(cliffMaterial, stoneTexture, .12f);
            if (GroundAtlas != null)
            {
                ApplyAtlas(tileMaterialA, 0f, new Color(.93f,1f,.94f));
                ApplyAtlas(tileMaterialB, 0f, new Color(.87f,.96f,.88f));
                ApplyAtlas(stoneMaterial, .5f, Color.white);
            }
            SetEmission(fireflyMaterial, new Color(1f, .77f, .42f) * .45f);
        }

        private void ApplyAtlas(Material material, float offset, Color tint)
        {
            ApplySurface(material, GroundAtlas, .08f);
            material.SetTextureScale("_BaseMap", new Vector2(.495f, .99f));
            material.SetTextureOffset("_BaseMap", new Vector2(offset+.0025f,.005f));
            SetColor(material,tint);
        }

        private Texture2D CreateSurfaceTexture(bool stoneSurface)
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            texture.name = stoneSurface ? "Soft limestone grain" : "Soft meadow grain";
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;
            var random = new System.Random(stoneSurface ? 771 : 443);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float shade = .955f + (float)random.NextDouble() * .045f;
                if (random.NextDouble() < (stoneSurface ? .045 : .035)) shade -= .08f;
                pixels[y * size + x] = new Color(shade, shade, shade, 1f);
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private void ApplySurface(Material material, Texture2D texture, float smoothness)
        {
            if (material == null) return;
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        }

        private Material MakeMaterial(Color color)
        {
            Shader shader = Shader.Find(color.a < .99f ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var material = new Material(shader);
            SetColor(material, color);
            if (color.a < .99f)
            {
                material.SetFloat("_Surface", 1); material.SetFloat("_ZWrite", 0);
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            return material;
        }

        private Material TintMaterial(Material source, Color color)
        {
            Material tinted = new Material(source);
            SetColor(tinted, color);
            return tinted;
        }

        private static void SetColor(Material material, Color color)
        {
            if (material == null) return;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        }

        private static void SetEmission(Material material, Color color)
        {
            if (material == null) return;
            material.EnableKeyword("_EMISSION");
            if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", color);
        }

        private void ClearGenerated()
        {
            if (boardRoot != null) Destroy(boardRoot.gameObject);
            if (unitRoot != null) Destroy(unitRoot.gameObject);
            boardRoot = unitRoot = null;
            unitViews.Clear(); highlightObjects.Clear(); intentObjects.Clear(); intentLines.Clear();
            driftingMotes.Clear(); moteOrigins.Clear(); motePhases.Clear();
        }

        private void OnDestroy()
        {
            DestroyMaterials();
        }

        private void DestroyMaterials()
        {
            DestroyMaterial(sceneryMaterial);sceneryMaterial=null;
            DestroyMaterial(tileMaterialA); DestroyMaterial(tileMaterialB); DestroyMaterial(stoneMaterial);
            DestroyMaterial(cliffMaterial); DestroyMaterial(highlightMaterial); DestroyMaterial(intentMaterial); DestroyMaterial(fireflyMaterial); DestroyMaterial(shadowMaterial);
            if (surfaceTexture != null) Destroy(surfaceTexture);
            if (stoneTexture != null) Destroy(stoneTexture);
            tileMaterialA = tileMaterialB = stoneMaterial = cliffMaterial = null;
            highlightMaterial = intentMaterial = fireflyMaterial = shadowMaterial = null;
            surfaceTexture = stoneTexture = null;
        }

        private static void DestroyMaterial(Material material) { if (material != null) Destroy(material); }
    }
}
