using System;
using System.Collections.Generic;
using System.IO;
using AvoiderPlugin;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.AI;

namespace AvoiderShowcase.Editor
{
    /// <summary>Small, project-local test runner. Requests are consumed once by the open editor.</summary>
    [InitializeOnLoad]
    public static class ShowcaseValidation
    {
        private const string Request = "Assets/AvoiderShowcase/Editor/automation-request.txt";
        private static bool running;
        private static double start;
        private static int phase;
        private static Vector3 initial;
        private static readonly List<string> results = new List<string>();
        static ShowcaseValidation() { EditorApplication.update += Update; }

        private static void Update()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (File.Exists(Request))
            {
                string command;
                try { command = File.ReadAllText(Request).Trim(); File.Delete(Request); }
                catch (IOException) { return; } // A writer may still hold the request for this frame.
                Directory.CreateDirectory("Artifacts");
                try
                {
                    if (command == "create") { ShowcaseBuilder.CreateScene(); File.WriteAllText("Artifacts/create.txt", "PASS: scene created and navigation baked."); }
                    if (command == "test") RunTests();
                    if (command == "play") EditorApplication.isPlaying = true;
                    if (command == "stop") EditorApplication.isPlaying = false;
                    if (command == "capture") { ScreenCapture.CaptureScreenshot(Path.GetFullPath("Artifacts/showcase.png")); }
                    if (command == "build") BuildPlayer();
                }
                catch (Exception e) { File.WriteAllText("Artifacts/error.txt", e.ToString()); Debug.LogException(e); }
            }
            if (SessionState.GetBool("AvoiderTests", false) && EditorApplication.isPlaying && !running)
            {
                running = true; phase = 0; start = EditorApplication.timeSinceStartup;
                results.Clear(); results.Add("DLL: " + typeof(Avoider).Assembly.GetName().Name);
            }
            if (!running || !EditorApplication.isPlaying) return;
            try { AdvanceTests(); }
            catch (Exception e) { results.Add("FAIL: " + e); Finish(); }
        }
        [MenuItem("Avoider/Run Validation")]
        public static void RunTests()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Exit Play mode before running validation."); return; }
            TestSampler(); SessionState.SetBool("AvoiderTests", true); EditorApplication.isPlaying = true;
        }
        private static void TestSampler()
        {
            for (int seed = 0; seed < 12; seed++)
            {
                var points = new List<Vector2>(new PoissonDiscSampler(24, 20, 1.5f, seed).Samples());
                Require(points.Count > 60, "Sampler generated enough points");
                for (int i = 0; i < points.Count; i++)
                {
                    Require(points[i].x >= 0 && points[i].x < 24 && points[i].y >= 0 && points[i].y < 20, "Sample in bounds");
                    for (int j = 0; j < i; j++) Require(Vector2.Distance(points[i], points[j]) >= 1.4999f, "Minimum sample separation");
                }
            }
            bool rejected = false;
            try { new PoissonDiscSampler(10, 10, 0); } catch (ArgumentOutOfRangeException) { rejected = true; }
            Require(rejected, "Invalid sampler spacing rejected");
            Directory.CreateDirectory("Artifacts");
            File.WriteAllText("Artifacts/sampler-tests.txt", "PASS: 12 seeds, bounds, pairwise minimum distance, sample density, invalid spacing.");
        }
        private static void AdvanceTests()
        {
            var control = UnityEngine.Object.FindFirstObjectByType<ShowcaseController>();
            Require(control != null, "Showcase loaded");
            var a = control.avoider; var agent = a.GetComponent<NavMeshAgent>();
            double elapsed = EditorApplication.timeSinceStartup - start;
            if (phase == 0 && elapsed > 0.15)
            {
                control.ResetActors(); initial = a.transform.position;
                Require(agent.isOnNavMesh && control.player.isOnNavMesh, "Both agents on baked NavMesh");
                Require(a.IsVisible(new Vector3(1, 0, -3)), "Unobstructed point visible");
                Require(!a.IsVisible(new Vector3(-2, 0, 2)), "Wall occludes hiding point");
                a.FindHidingSpot(); Require(a.Destination.HasValue && a.HiddenCount > 0, "Reachable hidden destination found");
                Require(!a.IsVisible(a.Destination.Value), "Chosen destination outside player sight");
                float chosen = RouteLength(agent, a.Destination.Value);
                foreach (var sample in a.LastSamples)
                    if (sample.reachable) Require(chosen <= RouteLength(agent, sample.position) + 0.01f, "Nearest candidate by path length selected");
                results.Add("PASS: baked mesh, visibility, hidden destination, nearest reachable route.");
                phase++;
            }
            if (phase == 1 && elapsed > 5)
            {
                Require(Vector3.Distance(initial, a.transform.position) > 0.5f, "Agent moved toward cover");
                Require(!a.IsVisible(a.transform.position), "Agent reached concealment");
                Vector3 toPlayer = control.player.transform.position - a.transform.position; toPlayer.y = 0;
                Require(Vector3.Dot(a.transform.forward, toPlayer.normalized) > 0.95f, "Agent maintains eye contact");
                results.Add("PASS: actual movement, concealment, eye contact.");
                control.player.Warp(new Vector3(-2, 0, 5));
                initial = a.transform.position; phase++;
            }
            if (phase == 2 && elapsed > 10)
            {
                Require(Vector3.Distance(initial, a.transform.position) > 0.3f, "Agent reacts to player relocation");
                results.Add("PASS: agent replans when player exposes cover.");
                a.range = 0.5f; control.player.Warp(new Vector3(11, 0, -9)); phase++;
            }
            if (phase == 3 && elapsed > 11)
            {
                Require(a.Status == "Out of range" && !agent.hasPath, "Out of range agent stops");
                a.range = 30; a.occluderMask = 0; phase++;
            }
            if (phase == 4 && elapsed > 12)
            {
                Require(a.Status == "No reachable cover" && !a.Destination.HasValue, "No cover safely handled");
                var missing = new GameObject("Validation missing agent");
                var invalid = missing.AddComponent<Avoider>(); Require(!invalid.ValidateSetup(), "Missing agent rejected");
                UnityEngine.Object.Destroy(missing);
                a.avoidee = null; Require(!a.ValidateSetup(), "Missing avoidee rejected");
                a.avoidee = control.player.transform;
                a.enabled = false; agent.Warp(new Vector3(100, 0, 100));
                // Warp can reject off-mesh points; explicitly move the disabled agent instead.
                agent.enabled = false; agent.transform.position = new Vector3(100, 0, 100); agent.enabled = true;
                Require(!a.ValidateSetup(), "Off-mesh agent rejected");
                results.Add("PASS: out of range, no cover, missing agent, missing target, off-mesh warnings.");
                Finish();
            }
        }
        private static float RouteLength(NavMeshAgent agent, Vector3 point)
        {
            var path = new NavMeshPath(); Require(agent.CalculatePath(point, path), "Path exists");
            float length = 0; Vector3 previous = agent.transform.position;
            foreach (Vector3 corner in path.corners) { length += Vector3.Distance(previous, corner); previous = corner; }
            return length;
        }
        private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
        private static void Finish()
        {
            File.WriteAllLines("Artifacts/play-tests.txt", results);
            SessionState.SetBool("AvoiderTests", false); running = false; EditorApplication.isPlaying = false;
        }
        [MenuItem("Avoider/Build Windows Showcase")]
        public static void BuildPlayer()
        {
            Directory.CreateDirectory("Builds/Windows");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { "Assets/AvoiderShowcase/Scenes/AvoiderShowcase.unity" },
                locationPathName = "Builds/Windows/AvoiderShowcase.exe", target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None });
            Directory.CreateDirectory("Artifacts");
            File.WriteAllText("Artifacts/build.txt", report.summary.result + " | errors: " + report.summary.totalErrors);
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Showcase build failed.");
        }
    }
}
