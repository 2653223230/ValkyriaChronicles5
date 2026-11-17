using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VC5PvE;

namespace VC5PvE.SharedAP
{
    /// <summary>Procedural, presentation-only cannon warning and three-cell salvo effects.</summary>
    public sealed class SharedArtilleryView : MonoBehaviour
    {
        const float CellHalf = .43f;
        const float WarningPeriod = 2.6f;
        const float FlightTime = .62f;

        BoardView board;
        Transform warningRoot, salvoRoot;
        readonly List<GameObject> warningShells = new List<GameObject>();
        readonly List<TrailRenderer> warningTrails = new List<TrailRenderer>();
        Material warningMaterial, shellMaterial, trailMaterial, impactMaterial;
        SPos[] warningCells = new SPos[0];
        Vector3 launchPoint;
        float warningClock;
        bool warningEnabled;
        int salvoVersion;

        public void Configure(BoardView boardView)
        {
            if (board == boardView) return;
            ClearAllEffects();
            board = boardView;
        }

        /// <summary>Shows or clears a visual-only warning. The supplied state is never modified.</summary>
        public void SetWarning(SState state, bool enabled)
        {
            ++salvoVersion;
            ClearSalvo();
            ClearWarning();
            warningEnabled = enabled && board != null && state != null && state.Telegraph != null;
            if (!warningEnabled)
            {
                return;
            }

            var cells = new List<SPos>(state.Telegraph.Count);
            foreach (SPos cell in state.Telegraph)
                if (!cells.Contains(cell)) cells.Add(cell);
            warningCells = cells.ToArray();
            SUnit cannon = FindCannon(state);
            if (cannon == null || warningCells.Length == 0)
            {
                warningEnabled = false;
                return;
            }

            launchPoint = board.World(new GridPos(cannon.Position.X, cannon.Position.Y)) + Vector3.up * .96f;
            if (warningMaterial == null) warningMaterial = MakeMaterial(new Color(1f, .17f, .11f, .92f));
            if (shellMaterial == null) shellMaterial = MakeMaterial(new Color(1f, .64f, .25f, 1f));
            if (trailMaterial == null) trailMaterial = MakeMaterial(new Color(1f, .29f, .10f, .88f));
            warningRoot = new GameObject("Cannon warning arcs").transform;
            warningRoot.SetParent(transform, false);

            for (int i = 0; i < warningCells.Length; i++)
            {
                Vector3 landing = CellCenter(warningCells[i]);
                AddRedFrame(landing);
                AddDottedArc(launchPoint, landing + Vector3.up * .04f);
                GameObject shell = MakeShell(warningRoot, trailMaterial, shellMaterial, "Warning shell " + (i + 1), .095f);
                warningShells.Add(shell);
                warningTrails.Add(shell.GetComponent<TrailRenderer>());
            }
        }

        /// <summary>Animates one shell to each cell in the same telegraph snapshot used by the rules.</summary>
        public IEnumerator PlaySalvo(SState before,List<SPos> firedCells)
        {
            int version = ++salvoVersion;
            ClearSalvo();
            if (board == null || before == null || before.Telegraph == null || before.Telegraph.Count == 0)
                yield break;

            var cells = firedCells ?? before.Telegraph;
            SUnit cannon = FindCannon(before);
            if (cannon == null || cells.Count == 0) yield break;

            Vector3 start = board.World(new GridPos(cannon.Position.X, cannon.Position.Y)) + Vector3.up * .96f;
            salvoRoot = new GameObject("Cannon salvo effects").transform;
            salvoRoot.SetParent(transform, false);
            if (shellMaterial == null) shellMaterial = MakeMaterial(new Color(1f, .64f, .25f, 1f));
            if (trailMaterial == null) trailMaterial = MakeMaterial(new Color(1f, .29f, .10f, .88f));
            if (impactMaterial == null) impactMaterial = MakeMaterial(new Color(1f, .34f, .13f, .92f));

            try
            {
                for (int i = 0; i < cells.Count; i++)
                {
                    if (version != salvoVersion || board == null) yield break;
                    Vector3 landing = CellCenter(cells[i]);
                    GameObject shell = MakeShell(salvoRoot, trailMaterial, shellMaterial, "Salvo shell " + (i + 1), .13f);
                    TrailRenderer trail = shell.GetComponent<TrailRenderer>();
                    shell.transform.position = start;
                    float elapsed = 0f;
                    while (elapsed < FlightTime)
                    {
                        if (version != salvoVersion || shell == null || board == null) yield break;
                        elapsed += Time.deltaTime;
                        shell.transform.position = ArcPoint(start, landing + Vector3.up * .04f, Mathf.Clamp01(elapsed / FlightTime));
                        yield return null;
                    }

                    shell.transform.position = landing + Vector3.up * .09f;
                    if (trail != null) trail.emitting = false;
                    Destroy(shell);
                    ShowImpact(landing);
                    yield return new WaitForSeconds(.22f);
                }
            }
            finally
            {
                if (version == salvoVersion) ClearSalvo();
            }
        }

        void Update()
        {
            if (!warningEnabled || warningShells.Count == 0 || warningCells.Length == 0) return;
            warningClock += Time.deltaTime;
            float cycle = warningClock % WarningPeriod;
            int flightCount = warningCells.Length;
            float launchOffset = cycle - .75f;
            int active = launchOffset < 0f ? -1 : Mathf.FloorToInt(launchOffset / .48f);
            float progress = active < 0 || active >= flightCount ? 0f : Mathf.Clamp01((launchOffset - active * .48f) / .42f);

            for (int i = 0; i < warningShells.Count; i++)
            {
                GameObject shell = warningShells[i];
                if (shell == null) continue;
                bool visible = i == active && progress < 1f;
                shell.SetActive(visible);
                if (visible)
                {
                    Vector3 landing = CellCenter(warningCells[i]) + Vector3.up * .04f;
                    shell.transform.position = ArcPoint(launchPoint, landing, progress);
                }
                else if (warningTrails[i] != null) warningTrails[i].Clear();
            }
        }

        void AddRedFrame(Vector3 center)
        {
            var go = new GameObject("Red impact frame");
            go.transform.SetParent(warningRoot, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = 4;
            line.startWidth = .055f;
            line.endWidth = .055f;
            line.sharedMaterial = warningMaterial;
            line.SetPositions(new[]
            {
                center + new Vector3(-CellHalf, .075f, -CellHalf),
                center + new Vector3(CellHalf, .075f, -CellHalf),
                center + new Vector3(CellHalf, .075f, CellHalf),
                center + new Vector3(-CellHalf, .075f, CellHalf)
            });
        }

        void AddDottedArc(Vector3 start, Vector3 end)
        {
            const int dots = 9;
            const float halfLength = .026f;
            for (int i = 0; i < dots; i++)
            {
                float t = (i + .5f) / dots;
                Vector3 center = ArcPoint(start, end, t);
                Vector3 tangent = (ArcPoint(start, end, Mathf.Min(1f, t + .02f)) -
                                   ArcPoint(start, end, Mathf.Max(0f, t - .02f))).normalized;
                var go = new GameObject("Arc dash " + (i + 1));
                go.transform.SetParent(warningRoot, false);
                var line = go.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.startWidth = .035f;
                line.endWidth = .035f;
                line.sharedMaterial = warningMaterial;
                line.SetPosition(0, center - tangent * halfLength);
                line.SetPosition(1, center + tangent * halfLength);
            }
        }

        void ShowImpact(Vector3 landing)
        {
            if (salvoRoot == null) return;
            const int points = 24;
            var ringObject = new GameObject("Ground impact ring");
            ringObject.transform.SetParent(salvoRoot, false);
            var ring = ringObject.AddComponent<LineRenderer>();
            ring.useWorldSpace = true;
            ring.loop = true;
            ring.positionCount = points;
            ring.startWidth = .065f;
            ring.endWidth = .065f;
            ring.sharedMaterial = impactMaterial;
            for (int i = 0; i < points; i++)
            {
                float angle = i * Mathf.PI * 2f / points;
                ring.SetPosition(i, landing + new Vector3(Mathf.Cos(angle) * .08f, .09f, Mathf.Sin(angle) * .08f));
            }
            StartCoroutine(ExpandImpact(ring, landing));
        }

        IEnumerator ExpandImpact(LineRenderer ring, Vector3 landing)
        {
            float elapsed = 0f;
            while (elapsed < .22f && ring != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / .22f);
                float radius = Mathf.Lerp(.08f, .58f, t);
                for (int i = 0; i < ring.positionCount; i++)
                {
                    float angle = i * Mathf.PI * 2f / ring.positionCount;
                    ring.SetPosition(i, landing + new Vector3(Mathf.Cos(angle) * radius, .09f + .025f * t, Mathf.Sin(angle) * radius));
                }
                ring.startWidth = ring.endWidth = Mathf.Lerp(.065f, .012f, t);
                yield return null;
            }
        }

        GameObject MakeShell(Transform parent, Material trailMat, Material bodyMat, string name, float radius)
        {
            var shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shell.name = name;
            shell.transform.SetParent(parent, true);
            shell.transform.localScale = Vector3.one * radius;
            shell.GetComponent<Renderer>().sharedMaterial = bodyMat;
            Collider collider = shell.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            var trail = shell.AddComponent<TrailRenderer>();
            trail.time = .35f;
            trail.startWidth = radius * .72f;
            trail.endWidth = .008f;
            trail.minVertexDistance = .035f;
            trail.sharedMaterial = trailMat;
            trail.emitting = true;
            return shell;
        }

        void ClearWarning()
        {
            warningEnabled = false;
            warningCells = new SPos[0];
            warningClock = 0f;
            warningShells.Clear();
            warningTrails.Clear();
            if (warningRoot != null) Destroy(warningRoot.gameObject);
            warningRoot = null;
        }

        void ClearSalvo()
        {
            if (salvoRoot != null) Destroy(salvoRoot.gameObject);
            salvoRoot = null;
        }

        void ClearAllEffects()
        {
            ++salvoVersion;
            ClearWarning();
            ClearSalvo();
        }

        void OnDisable() => ClearAllEffects();
        void OnDestroy()
        {
            ClearAllEffects();
            DestroyMaterial(warningMaterial); DestroyMaterial(shellMaterial);
            DestroyMaterial(trailMaterial); DestroyMaterial(impactMaterial);
        }

        Vector3 CellCenter(SPos pos) => board == null
            ? new Vector3(pos.X - 3f, 0f, 3f - pos.Y)
            : board.World(new GridPos(pos.X, pos.Y));

        static Vector3 ArcPoint(Vector3 start, Vector3 end, float t)
        {
            Vector3 point = Vector3.Lerp(start, end, t);
            float distance = Vector2.Distance(new Vector2(start.x, start.z), new Vector2(end.x, end.z));
            point.y += Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * Mathf.Clamp(1.05f + distance * .12f, 1.05f, 1.72f);
            return point;
        }

        static SUnit FindCannon(SState state)
        {
            foreach (SUnit unit in state.Units)
                if (unit != null && unit.Alive && unit.Role == SRole.Cannon) return unit;
            return null;
        }

        static Material MakeMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var material = new Material(shader);
            material.color = color;
            return material;
        }

        static void DestroyMaterial(Material material)
        {
            if (material != null) Destroy(material);
        }
    }
}
