using System.Collections.Generic;
using UnityEngine;

namespace VC5PvE.SharedAP
{
    /// <summary>Visual-only 7x7 board for the shared AP encounter.</summary>
    public sealed class SharedBoardView : MonoBehaviour
    {
        public Sprite[] UnitSprites;
        public GameObject[] TreePrefabs, RockPrefabs, ShrubPrefabs, RuinPrefabs;
        public Texture2D GroundAtlas;
        public Font UiFont;

        private const int Size = 7;
        private readonly Dictionary<string, Transform> units = new Dictionary<string, Transform>();
        private readonly List<GameObject> generated = new List<GameObject>();
        private readonly List<Material> materials = new List<Material>();
        private Transform boardRoot, dynamicRoot;
        private Camera viewCamera;
        private SState state;
        private int persistentMaterialCount;
        private readonly Dictionary<Color,Material> materialCache=new Dictionary<Color,Material>(); private Material spriteMaterial;

        public void Build(SState initialState, Camera camera)
        {
            Clear();
            state = initialState;
            viewCamera = camera != null ? camera : Camera.main;
            boardRoot = new GameObject("Shared AP Board").transform;
            boardRoot.SetParent(transform, false);
            BuildTiles();
            BuildObstacles(initialState);
            persistentMaterialCount = materials.Count;
            Render(initialState, null, null);
        }

        public void Render(SState nextState, string selectedId, SPlan preview)
        {
            if (nextState == null) return;
            if (boardRoot == null) { Build(nextState, viewCamera); return; }
            state = nextState;

            ClearDynamic();
            dynamicRoot = new GameObject("Dynamic").transform; dynamicRoot.SetParent(boardRoot, false);
            units.Clear();
            foreach (SUnit unit in state.Units)
            {
                if (unit == null || !unit.Alive) continue;
                BuildUnit(unit, selectedId == unit.Id);
            }
            foreach (SUnit guard in state.Units)
            {
                if (guard == null || !guard.Alive || guard.Role != SRole.Guard) continue;
                SUnit cannon = FindRole(SRole.Cannon);
                if (cannon != null && Manhattan(guard.Position, cannon.Position) == 1)
                    AddLine("Guard protection", World(guard.Position) + Vector3.up * .24f, World(cannon.Position) + Vector3.up * .24f, new Color(.38f, .86f, .98f), .045f);
            }
            if (state.Telegraph != null)
                foreach (SPos p in state.Telegraph) AddCellMark("Cannon telegraph", p, new Color(1f, .16f, .12f, .34f), .035f);
            if (preview != null && preview.Valid)
            {
                if (preview.Path != null)
                    for (int i = 0; i < preview.Path.Count; i++) AddCellMark("Preview path", preview.Path[i], new Color(.15f, .9f, .82f, .42f), .045f);
                if (preview.Affected != null)
                    foreach (SPos p in preview.Affected) AddCellMark("Preview affected", p, new Color(1f, .42f, .12f, .48f), .055f);
                if (preview.After != null)
                    foreach (SUnit u in preview.After.Units)
                        if (u != null && u.Alive && state.Find(u.Id) != null && !SamePos(u.Position, state.Find(u.Id).Position))
                            AddCellMark("Preview landing", u.Position, new Color(1f, .83f, .2f, .75f), .065f);
            }
        }

        public bool TryCell(Vector2 screen, out SPos cell)
        {
            cell = default(SPos);
            if (viewCamera == null) viewCamera = Camera.main;
            if (viewCamera == null) return false;
            Ray ray = viewCamera.ScreenPointToRay(screen);
            float distance;
            if (!new Plane(Vector3.up, Vector3.zero).Raycast(ray, out distance) || distance < 0f) return false;
            Vector3 hit = ray.GetPoint(distance);
            int x = Mathf.FloorToInt(hit.x + 3.5f), y = Mathf.FloorToInt(3.5f - hit.z);
            if (x < 0 || x >= Size || y < 0 || y >= Size) return false;
            cell = new SPos(x, y);
            return true;
        }

        private void LateUpdate()
        {
            if (viewCamera == null) return;
            foreach (Transform root in units.Values)
            {
                if (root == null) continue;
                Transform portrait = root.Find("Portrait");
                if (portrait == null) continue;
                Vector3 forward = viewCamera.transform.position - portrait.position;
                forward.y = 0f;
                if (forward.sqrMagnitude > .001f) portrait.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            }
        }

        private void BuildTiles()
        {
            Material a = MakeMaterial(new Color(.40f, .50f, .39f), true);
            Material b = MakeMaterial(new Color(.47f, .55f, .42f), true);
            Material stone = MakeMaterial(new Color(.57f, .57f, .51f), true);
            for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
            {
                var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.name = "Tile " + x + "," + y; tile.transform.SetParent(boardRoot, false);
                tile.transform.position = World(new SPos(x, y)) + Vector3.down * .08f;
                tile.transform.localScale = new Vector3(.98f, .16f, .98f);
                tile.GetComponent<Renderer>().sharedMaterial = ((x + y) % 2 == 0) ? a : b;
                Collider c = tile.GetComponent<Collider>(); if (c != null) Destroy(c);
                if (x == 3 && y == 4) tile.GetComponent<Renderer>().sharedMaterial = stone;
                generated.Add(tile);
            }
            if (GroundAtlas != null)
            {
                foreach(var mat in new[]{a,b,stone})
                {
                    mat.SetTexture("_BaseMap",GroundAtlas);mat.SetTextureScale("_BaseMap",new Vector2(.495f,.99f));
                    mat.SetTextureOffset("_BaseMap",new Vector2(mat==stone?.5025f:.0025f,.005f));mat.color=mat==b?new Color(.78f,.88f,.78f):Color.white;
                }
            }
            for(int side=-1;side<=1;side+=2)
                for(int z=-3;z<=3;z+=3)
                {
                    var prefab=Pick(TreePrefabs,(z+3)/3);if(prefab==null)continue;
                    var tree=Instantiate(prefab,new Vector3(side*4.6f,0,z),Quaternion.identity,boardRoot);tree.name="Peripheral Obstacle Tree";
                    FitProp(tree,1.8f,2.3f);
                }
        }

        private void BuildObstacles(SState s)
        {
            if (s == null || s.Obstacles == null) return;
            foreach (SPos p in s.Obstacles)
            {
                var wallBase=Part(boardRoot,PrimitiveType.Cube,"Wall Obstacle",World(p)+Vector3.up*.22f,new Vector3(.86f,.44f,.86f),MakeMaterial(new Color(.40f,.44f,.39f),false));
                GameObject prop = Pick(RuinPrefabs, p.X == 3 ? 3 : 1);
                if (prop == null) prop = Pick(RockPrefabs, 0);
                if (prop != null)
                {
                    prop = Instantiate(prop, World(p), Quaternion.identity, boardRoot);
                    prop.name = "Shared AP Ruin Obstacle"; FitProp(prop,.84f,.8f); generated.Add(prop);
                }
                else
                {
                    var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall.name = "Stone Obstacle"; wall.transform.SetParent(boardRoot, false); wall.transform.position = World(p) + Vector3.up * .28f;
                    wall.transform.localScale = new Vector3(.78f, .56f, .78f); wall.GetComponent<Renderer>().sharedMaterial = MakeMaterial(new Color(.37f, .39f, .36f), false); generated.Add(wall);
                }
            }
        }

        static void FitProp(GameObject prop,float width,float height)
        {
            var renderers=prop.GetComponentsInChildren<Renderer>();if(renderers.Length==0)return;
            var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            float factor=Mathf.Min(width/Mathf.Max(bounds.size.x,bounds.size.z),height/Mathf.Max(.01f,bounds.size.y));
            prop.transform.localScale*=factor;
            bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            prop.transform.position+=new Vector3(prop.transform.position.x-bounds.center.x,-bounds.min.y,prop.transform.position.z-bounds.center.z);
        }
        void OnGUI()
        {
            if(state==null||viewCamera==null||state.Phase==SPhase.Won||state.Phase==SPhase.Lost)return;
            float scale=Screen.height/1080f;
            var label=new GUIStyle(GUI.skin.label){font=UiFont,fontSize=Mathf.Max(12,Mathf.RoundToInt(21*scale)),alignment=TextAnchor.MiddleCenter};
            foreach(var u in state.Units)
            {
                if(!u.Alive)continue;var p=viewCamera.WorldToScreenPoint(World(u.Position)+Vector3.up*1.28f);
                string text=u.Name+" "+u.Hp+(u.Shield>0?" +"+u.Shield+"盾":"");
                var rect=new Rect(p.x-66*scale,Screen.height-p.y-24*scale,132*scale,28*scale);
                GUI.color=new Color(.025f,.06f,.065f,.9f);GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=Color.white;
                label.normal.textColor=u.Player?new Color(.78f,1,.85f):new Color(1,.8f,.65f);GUI.Label(rect,text,label);
            }
            label.normal.textColor=new Color(.9f,.87f,.65f);
            for(int i=0;i<7;i++)
            {
                var a=viewCamera.WorldToScreenPoint(new Vector3(i-3,.02f,3.65f));
                GUI.Label(new Rect(a.x-15,Screen.height-a.y-15,30,30),((char)('A'+i)).ToString(),label);
                var b=viewCamera.WorldToScreenPoint(new Vector3(-3.65f,.02f,3-i));
                GUI.Label(new Rect(b.x-15,Screen.height-b.y-15,30,30),(i+1).ToString(),label);
            }
        }
        private void BuildUnit(SUnit unit, bool selected)
        {
            Transform root = new GameObject("Shared unit " + unit.Id).transform; root.SetParent(dynamicRoot, false); root.position = World(unit.Position); units[unit.Id] = root; generated.Add(root.gameObject);
            bool cannon = unit.Role == SRole.Cannon;
            if (cannon) BuildCannon(root);
            else
            {
                var go = new GameObject("Portrait"); go.transform.SetParent(root, false); go.transform.localPosition = Vector3.up * .04f;
                var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = SpriteFor(unit.Role); sr.color = unit.Player ? Color.white : new Color(1f, .76f, .70f);
                sr.sharedMaterial = SpriteMaterial();
                float size=sr.sprite!=null?Mathf.Min(1.35f/sr.sprite.bounds.size.y,.85f/sr.sprite.bounds.size.x):1f; go.transform.localScale=Vector3.one*size; if(sr.sprite!=null)go.transform.localPosition=Vector3.up*(.04f-sr.sprite.bounds.min.y*size);
            }
            AddRing(root, selected ? new Color(1f, .82f, .22f) : unit.Player ? new Color(.30f, .84f, .70f) : new Color(.92f, .34f, .27f), selected ? .62f : .48f);
            BuildHealth(root, unit);
        }

        private void BuildCannon(Transform root)
        {
            Material baseMat = MakeMaterial(new Color(.32f, .37f, .37f), false), glow = MakeMaterial(new Color(.95f, .35f, .20f), true);
            Part(root, PrimitiveType.Cylinder, "炮座基座", new Vector3(0, .18f, 0), new Vector3(.68f, .18f, .68f), baseMat);
            Part(root, PrimitiveType.Cube, "炮座主体", new Vector3(0, .64f, 0), new Vector3(.60f, .70f, .60f), baseMat);
            Part(root, PrimitiveType.Cylinder, "炮口", new Vector3(0, .86f, -.38f), new Vector3(.23f, .32f, .23f), glow).transform.localRotation = Quaternion.Euler(90, 0, 0);
        }

        private void BuildHealth(Transform root, SUnit u)
        {
            float ratio = u.MaxHp > 0 ? Mathf.Clamp01((float)u.Hp / u.MaxHp) : 0;
            Part(root, PrimitiveType.Cube, "Health backing", new Vector3(0, .17f, 0), new Vector3(.68f, .07f, .07f), MakeMaterial(new Color(.10f, .12f, .11f), false));
            Part(root, PrimitiveType.Cube, "Health", new Vector3(-.34f + .34f * ratio, .18f, -.01f), new Vector3(.68f * ratio, .055f, .08f), MakeMaterial(new Color(.32f, .82f, .48f), true));
            if (u.Shield > 0) Part(root, PrimitiveType.Sphere, "Shield", new Vector3(.32f, .30f, 0), Vector3.one * .13f, MakeMaterial(new Color(.28f, .72f, 1f), true));
        }

        private void AddRing(Transform root, Color color, float radius)
        {
            var go = new GameObject("Unit ground ring"); go.transform.SetParent(root, false);
            var lr = go.AddComponent<LineRenderer>(); lr.useWorldSpace = false; lr.loop = true; lr.positionCount = 40; lr.startWidth = .035f; lr.endWidth = .035f; lr.sharedMaterial = LineMaterial(color);
            for (int i = 0; i < 40; i++) { float a = i * Mathf.PI * 2 / 40; lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, .025f, Mathf.Sin(a) * radius)); }
        }

        private void AddCellMark(string label, SPos p, Color color, float y)
        {
            var q=new GameObject(label);q.transform.SetParent(dynamicRoot,false);var line=q.AddComponent<LineRenderer>();
            line.loop=true;line.positionCount=4;line.startWidth=.045f;line.endWidth=.045f;line.sharedMaterial=LineMaterial(new Color(color.r,color.g,color.b));
            var center=World(p)+Vector3.up*y;
            line.SetPositions(new[]{center+new Vector3(-.43f,0,-.43f),center+new Vector3(.43f,0,-.43f),center+new Vector3(.43f,0,.43f),center+new Vector3(-.43f,0,.43f)});
        }
        private void AddLine(string label, Vector3 a, Vector3 b, Color color, float width)
        {
            var go = new GameObject(label); go.transform.SetParent(dynamicRoot, false); var line = go.AddComponent<LineRenderer>(); line.positionCount = 2; line.SetPosition(0, a); line.SetPosition(1, b); line.startWidth = width; line.endWidth = width; line.sharedMaterial = LineMaterial(color); generated.Add(go);
        }
        private GameObject Part(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false); go.transform.localPosition = pos; go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = mat; Collider c = go.GetComponent<Collider>(); if (c != null) Destroy(c); return go;
        }
        private Sprite SpriteFor(SRole role)
        {
            if (UnitSprites == null || UnitSprites.Length == 0) return null;
            int i = role == SRole.Vanguard ? 0 : role == SRole.Archer ? 2 : role == SRole.Mage ? 1 : role == SRole.Patrol ? 3 : 4;
            return i < UnitSprites.Length ? UnitSprites[i] : null;
        }
        private Material SpriteMaterial()
        {
            if(spriteMaterial!=null)return spriteMaterial;
            Shader s = Shader.Find("VC5PvE/PixelCharacter"); if (s == null) s = Shader.Find("Sprites/Default");
            var material = new Material(s); materials.Add(material); spriteMaterial=material; return material;
        }
        private Material MakeMaterial(Color c, bool transparent)
        {
            Material cached;if(materialCache.TryGetValue(c,out cached))return cached;
            Shader shader = Shader.Find(transparent?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit"); if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            var m = new Material(shader); m.color = c; materials.Add(m); materialCache[c]=m; return m;
        }
        private Material LineMaterial(Color c) { var m = MakeMaterial(c, true); return m; }
        private void ClearDynamic()
        { if(dynamicRoot!=null){dynamicRoot.gameObject.SetActive(false);Destroy(dynamicRoot.gameObject);} generated.Clear(); }
        public bool TryUnit(Vector2 screen,out string id)
        {
            id=null;if(viewCamera==null||state==null)return false;
            float nearest=float.MaxValue;
            foreach(var u in state.Units)
            {
                if(!u.Alive)continue;
                var bottom=viewCamera.WorldToScreenPoint(World(u.Position));
                var top=viewCamera.WorldToScreenPoint(World(u.Position)+Vector3.up*1.4f);
                float width=Mathf.Abs(viewCamera.WorldToScreenPoint(World(u.Position)+viewCamera.transform.right*.43f).x-bottom.x);
                if(screen.x<bottom.x-width||screen.x>bottom.x+width||screen.y<bottom.y-8||screen.y>top.y+8)continue;
                float dist=Vector2.Distance(screen,new Vector2(bottom.x,(bottom.y+top.y)*.5f));
                if(dist<nearest){nearest=dist;id=u.Id;}
            }
            return id!=null;
        }        private void Clear()
        {
            if (boardRoot != null) Destroy(boardRoot.gameObject); boardRoot = null; units.Clear(); generated.Clear();
            foreach (Material m in materials) if (m != null) Destroy(m); materials.Clear();
            persistentMaterialCount = 0; materialCache.Clear();spriteMaterial=null;
        }
        private void ClearRenderMaterials()
        {
            for (int i = materials.Count - 1; i >= persistentMaterialCount; i--) { if (materials[i] != null) Destroy(materials[i]); materials.RemoveAt(i); }
        }
        private void OnDestroy() { Clear(); }
        private Vector3 World(SPos p) { return new Vector3(p.X - 3, 0, 3 - p.Y); }
        private SUnit FindRole(SRole role) { foreach (SUnit u in state.Units) if (u != null && u.Alive && u.Role == role) return u; return null; }
        private static int Manhattan(SPos a, SPos b) { return Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Y - b.Y); }
        private static bool SamePos(SPos a, SPos b) { return a.X == b.X && a.Y == b.Y; }
        private static GameObject Pick(GameObject[] prefabs, int index) { return prefabs != null && prefabs.Length > 0 ? prefabs[Mathf.Clamp(index, 0, prefabs.Length - 1)] : null; }
    }
}



