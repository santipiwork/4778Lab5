using AvoiderPlugin;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace AvoiderShowcase
{
    public sealed class ShowcaseController : MonoBehaviour
    {
        public Avoider avoider;
        public NavMeshAgent player;
        public Camera viewCamera;
        public Material overlayMaterial;
        public bool autoTour;
        public bool showSamples = true;
        private Vector3 playerStart, avoiderStart;
        private float tourClock;
        private Material lineMaterial;
        private Mesh lineMesh;
        private MeshRenderer lineRenderer;
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Color> colors = new List<Color>();
        private readonly List<int> indices = new List<int>();
        private GUIStyle title, subtitle, body, small, badge;
        private static readonly Color Mint = new Color(0.38f, 1f, 0.76f);

        private void Start()
        {
            playerStart = player.transform.position; avoiderStart = avoider.transform.position;
            lineMaterial = new Material(overlayMaterial);
            var lines = new GameObject("Sample visualization"); lines.transform.SetParent(transform, false);
            lineMesh = new Mesh { name = "Avoider sample lines" }; lineMesh.MarkDynamic();
            lines.AddComponent<MeshFilter>().sharedMesh = lineMesh;
            lineRenderer = lines.AddComponent<MeshRenderer>(); lineRenderer.sharedMaterial = lineMaterial;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
        }
        private void OnDestroy() { if (lineMaterial != null) Destroy(lineMaterial); if (lineMesh != null) Destroy(lineMesh); }
        private void Update()
        {
            Keyboard k = Keyboard.current;
            if (k != null)
            {
                if (k.gKey.wasPressedThisFrame) { showSamples = !showSamples; avoider.showGizmos = showSamples; }
                if (k.tKey.wasPressedThisFrame) ToggleTour();
                if (k.rKey.wasPressedThisFrame) ResetActors();
                Vector3 move = new Vector3((k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0), 0,
                    (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0));
                if (move.sqrMagnitude > 0 && player.isOnNavMesh)
                {
                    autoTour = false; player.ResetPath(); player.Move(move.normalized * 5f * Time.deltaTime);
                    player.transform.rotation = Quaternion.LookRotation(move);
                }
            }
            if (autoTour && player.isOnNavMesh)
            {
                tourClock += Time.deltaTime;
                if (!player.hasPath || player.remainingDistance < 0.6f)
                {
                    Vector3[] tour = { new Vector3(7, 0, -5), new Vector3(8, 0, 6), new Vector3(-7, 0, 6), new Vector3(-8, 0, -6) };
                    player.SetDestination(tour[Mathf.FloorToInt(tourClock / 5f) % tour.Length]);
                }
            }
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && player.isOnNavMesh)
            {
                Vector2 mouse = Mouse.current.position.ReadValue();
                if (mouse.x > 350 * Screen.width / 1440f && mouse.y > 80 * Screen.height / 900f &&
                    Physics.Raycast(viewCamera.ScreenPointToRay(mouse), out RaycastHit hit, 200, 1 << 8) &&
                    NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 1.5f, NavMesh.AllAreas))
                { autoTour = false; player.SetDestination(navHit.position); }
            }
        }
        public void ResetActors()
        {
            autoTour = false; tourClock = 0;
            avoider.enabled = false; player.ResetPath(); player.Warp(playerStart);
            avoider.GetComponent<NavMeshAgent>().Warp(avoiderStart); avoider.enabled = true;
        }
        private void ToggleTour() { autoTour = !autoTour; tourClock = 0; player.ResetPath(); }
        private void SetupStyles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = 38, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            subtitle = new GUIStyle(GUI.skin.label) { fontSize = 12, normal = { textColor = Mint } };
            body = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true, normal = { textColor = new Color(0.85f, 0.9f, 0.94f) } };
            small = new GUIStyle(body) { fontSize = 12, normal = { textColor = new Color(0.55f, 0.66f, 0.74f) } };
            badge = new GUIStyle(body) { fontSize = 21, fontStyle = FontStyle.Bold, normal = { textColor = Mint } };
        }
        private static void Panel(Rect rect, Color color) { GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = Color.white; }
        private void OnGUI()
        {
            if (avoider == null || player == null) return;
            SetupStyles(); GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1440f, Screen.height / 900f, 1));
            Panel(new Rect(22, 24, 302, 780), new Color(0.025f, 0.045f, 0.075f, 0.96f));
            Panel(new Rect(22, 24, 3, 780), Mint);
            GUI.Label(new Rect(46, 45, 265, 24), "LAB 05  /  MANAGED UNITY PLUG-IN", subtitle);
            GUI.Label(new Rect(44, 79, 270, 56), "AVOIDER", title);
            GUI.Label(new Rect(46, 144, 250, 64), "Find cover.\nKeep the target in sight.", body);
            Panel(new Rect(46, 228, 252, 1), new Color(0.2f, 0.3f, 0.36f));
            GUI.Label(new Rect(46, 248, 250, 20), "AGENT STATUS", small);
            GUI.Label(new Rect(46, 277, 250, 38), avoider.Status.ToUpperInvariant(), badge);
            GUI.Label(new Rect(46, 328, 250, 28), $"{avoider.LastSamples.Count:000} samples   /   {avoider.HiddenCount:000} safe", body);
            GUI.Label(new Rect(46, 365, 245, 40), "Nearest reachable cover, measured along the NavMesh path.", small);
            GUI.Label(new Rect(46, 430, 250, 28), $"Detection range     {avoider.range:0.0} m", body);
            avoider.range = GUI.HorizontalSlider(new Rect(46, 468, 250, 20), avoider.range, 3, 20);
            GUI.Label(new Rect(46, 505, 250, 28), $"Escape speed        {avoider.speed:0.0} m/s", body);
            avoider.speed = GUI.HorizontalSlider(new Rect(46, 543, 250, 20), avoider.speed, 0, 8);
            bool draw = GUI.Toggle(new Rect(46, 587, 250, 28), showSamples, "  Show sampling overlay [G]");
            if (draw != showSamples) { showSamples = draw; avoider.showGizmos = draw; }
            if (GUI.Button(new Rect(46, 643, 250, 42), autoTour ? "STOP AUTO TOUR  [T]" : "START AUTO TOUR  [T]")) ToggleTour();
            if (GUI.Button(new Rect(46, 702, 250, 42), "RESET POSITIONS  [R]")) ResetActors();
            GUI.Label(new Rect(46, 765, 250, 24), "C# DLL  /  BRIDSON SAMPLING", small);
            Panel(new Rect(350, 826, 1068, 52), new Color(0.025f, 0.045f, 0.075f, 0.94f));
            GUI.Label(new Rect(370, 842, 670, 28), "WASD / ARROWS  Move player     •     CLICK FLOOR  Set destination", body);
            GUI.Label(new Rect(1100, 842, 310, 28), "RED  visible     GREEN  cover", small);
            LabelActor(player.transform.position, "PLAYER / AVOIDEE", new Color(1, 0.8f, 0.24f));
            LabelActor(avoider.transform.position, "AVOIDER", new Color(1, 0.4f, 0.35f));
            GUI.matrix = Matrix4x4.identity;
        }
        private void LabelActor(Vector3 position, string label, Color color)
        {
            Vector3 p = viewCamera.WorldToScreenPoint(position + Vector3.up * 2.4f);
            Color previous = small.normal.textColor; small.normal.textColor = color;
            GUI.Label(new Rect(p.x * 1440 / Screen.width - 60, (Screen.height - p.y) * 900 / Screen.height - 20, 210, 24), label, small);
            small.normal.textColor = previous;
        }
        // A mesh with a URP shader also renders with Unity 6's Render Graph enabled.
        private void LateUpdate()
        {
            if (lineRenderer == null) return;
            lineRenderer.enabled = showSamples;
            if (!showSamples) return;
            vertices.Clear(); colors.Clear(); indices.Clear();
            Vector3 start = avoider.transform.position + Vector3.up * 0.09f;
            foreach (Avoider.Sample sample in avoider.LastSamples)
            {
                Color color = sample.reachable ? new Color(0.3f, 1, 0.65f, 0.65f) : new Color(1, 0.25f, 0.3f, 0.32f);
                Line(start, sample.position + Vector3.up * 0.09f, color);
            }
            for (int i = 0; i < 96; i++)
            {
                float a = i * Mathf.PI * 2 / 96, b = (i + 1) * Mathf.PI * 2 / 96;
                Line(start + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * avoider.range,
                    start + new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b)) * avoider.range, new Color(1, 0.8f, 0.3f, 0.7f));
            }
            if (avoider.Destination.HasValue)
                Line(start, avoider.Destination.Value + Vector3.up * 0.15f, Color.cyan);
            lineMesh.Clear(); lineMesh.SetVertices(vertices); lineMesh.SetColors(colors); lineMesh.SetIndices(indices, MeshTopology.Lines, 0);
            lineMesh.RecalculateBounds();
        }
        private void Line(Vector3 a, Vector3 b, Color color)
        {
            indices.Add(vertices.Count); vertices.Add(a); colors.Add(color);
            indices.Add(vertices.Count); vertices.Add(b); colors.Add(color);
        }
    }
}
