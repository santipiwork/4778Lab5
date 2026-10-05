using System;
using System.IO;
using AvoiderPlugin;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace AvoiderShowcase.Editor
{
    public static class ShowcaseBuilder
    {
        private const string Root = "Assets/AvoiderShowcase";
        [MenuItem("Avoider/Create Showcase Scene")]
        public static void CreateScene()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Root + "/Materials"); Directory.CreateDirectory(Root + "/Scenes");
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tags.FindProperty("layers");
            layers.GetArrayElementAtIndex(8).stringValue = "Walkable";
            layers.GetArrayElementAtIndex(9).stringValue = "Cover";
            tags.ApplyModifiedPropertiesWithoutUndo();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Material floor = Material("Floor", new Color(0.055f, 0.11f, 0.16f));
            Material cover = Material("Cover", new Color(0.22f, 0.37f, 0.43f));
            Material trim = Material("Trim", new Color(0.36f, 0.85f, 0.72f));
            Material coral = Material("Avoider", new Color(1f, 0.22f, 0.16f));
            Material gold = Material("Player", new Color(1f, 0.72f, 0.09f));
            Material white = Material("Eyes", new Color(0.92f, 1f, 1f));
            Material grid = Material("Grid", new Color(0.12f, 0.23f, 0.28f));
            GameObject level = new GameObject("Arena - baked navigation");
            Box("Floor", new Vector3(0, -0.25f, 0), new Vector3(26, 0.5f, 22), floor, level.transform, 8);
            Box("Central cover A", new Vector3(-1, 1.35f, 0), new Vector3(6, 2.7f, 0.7f), cover, level.transform, 9);
            Box("Central cover B", new Vector3(-3.65f, 1.35f, 1.8f), new Vector3(0.7f, 2.7f, 3.2f), cover, level.transform, 9);
            Box("East cover", new Vector3(6, 1.35f, 2), new Vector3(0.8f, 2.7f, 5), cover, level.transform, 9);
            Box("West cover", new Vector3(-8, 1.35f, -3), new Vector3(3, 2.7f, 1), cover, level.transform, 9);
            Box("North cover", new Vector3(0.5f, 1.35f, 7), new Vector3(4, 2.7f, 0.8f), cover, level.transform, 9);
            foreach (Transform wall in level.transform)
                if (wall.gameObject.layer == 9)
                    Box("Cover light strip", wall.position + Vector3.up * 1.36f, new Vector3(wall.localScale.x, 0.04f, wall.localScale.z), trim, null, 0, false);
            for (int x = -13; x <= 13; x++) Box("Floor grid X", new Vector3(x, 0.008f, 0), new Vector3(0.018f, 0.01f, 22), grid, null, 0, false);
            for (int z = -11; z <= 11; z++) Box("Floor grid Z", new Vector3(0, 0.008f, z), new Vector3(26, 0.01f, 0.018f), grid, null, 0, false);
            Box("Edge N", new Vector3(0, 0.04f, 11), new Vector3(26, 0.08f, 0.08f), trim, null, 0, false);
            Box("Edge S", new Vector3(0, 0.04f, -11), new Vector3(26, 0.08f, 0.08f), trim, null, 0, false);
            Box("Edge E", new Vector3(13, 0.04f, 0), new Vector3(0.08f, 0.08f, 22), trim, null, 0, false);
            Box("Edge W", new Vector3(-13, 0.04f, 0), new Vector3(0.08f, 0.08f, 22), trim, null, 0, false);
            NavMeshSurface surface = level.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children; surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = (1 << 8) | (1 << 9); surface.BuildNavMesh();
            string dataPath = Root + "/Scenes/ArenaNavMesh.asset";
            // Preserve the existing asset GUID when regenerating the showcase.
            var oldData = AssetDatabase.LoadAssetAtPath<NavMeshData>(dataPath);
            if (oldData == null) AssetDatabase.CreateAsset(surface.navMeshData, dataPath);
            else { EditorUtility.CopySerialized(surface.navMeshData, oldData); surface.RemoveData(); surface.navMeshData = oldData; surface.AddData(); }
            NavMeshAgent player = Actor("Player - Avoidee", new Vector3(5, 0, -4), gold, white);
            player.speed = 5; player.angularSpeed = 600;
            NavMeshAgent agent = Actor("Avoider - DLL component", new Vector3(1, 0, -3), coral, white);
            Avoider avoider = agent.gameObject.AddComponent<Avoider>();
            avoider.avoidee = player.transform; avoider.occluderMask = 1 << 9; avoider.range = 10; avoider.speed = 4;
            Camera camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera"; camera.transform.position = new Vector3(22, 28, -30);
            camera.transform.LookAt(new Vector3(-3, 0, 1)); camera.orthographic = true; camera.orthographicSize = 15f;
            camera.transform.position -= camera.transform.right * 3;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.025f, 0.045f, 0.07f);
            camera.nearClipPlane = 0.1f; camera.farClipPlane = 150;
            Light light = new GameObject("Sun", typeof(Light)).GetComponent<Light>(); light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(48, -28, 0); light.intensity = 1.6f; light.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.55f, 0.65f, 0.76f);
            ShowcaseController showcase = new GameObject("Showcase controls").AddComponent<ShowcaseController>();
            showcase.avoider = avoider; showcase.player = player; showcase.viewCamera = camera;
            string overlayPath = Root + "/Materials/SampleOverlay.mat";
            Material overlay = AssetDatabase.LoadAssetAtPath<Material>(overlayPath);
            if (overlay == null) { overlay = new Material(Shader.Find("Avoider/SampleLines")); AssetDatabase.CreateAsset(overlay, overlayPath); }
            overlay.shader = Shader.Find("Avoider/SampleLines");
            showcase.overlayMaterial = overlay;
            string scenePath = Root + "/Scenes/AvoiderShowcase.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
            PlayerSettings.productName = "Avoider - Lab 5";
            PlayerSettings.defaultScreenWidth = 1440; PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = agent.gameObject;
            if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.LookAt(Vector3.zero, Quaternion.Euler(55, 0, 0), 23);
            Debug.Log("Avoider showcase created with baked NavMesh and DLL component.");
        }
        private static Material Material(string name, Color color)
        {
            string path = Root + "/Materials/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            material.color = color; material.SetFloat("_Smoothness", 0.32f); return material;
        }
        private static GameObject Box(string name, Vector3 position, Vector3 scale, Material material, Transform parent, int layer, bool collider = true)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name; obj.layer = layer;
            obj.transform.position = position; obj.transform.localScale = scale; obj.transform.SetParent(parent, true);
            obj.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
            return obj;
        }
        private static NavMeshAgent Actor(string name, Vector3 position, Material body, Material eyes)
        {
            var root = new GameObject(name); root.transform.position = position;
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule); capsule.name = "Body"; capsule.transform.SetParent(root.transform, false);
            capsule.transform.localPosition = Vector3.up; capsule.GetComponent<Renderer>().sharedMaterial = body;
            for (int side = -1; side <= 1; side += 2)
            {
                var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere); eye.name = "Eye"; eye.transform.SetParent(root.transform, false);
                eye.transform.localPosition = new Vector3(side * 0.18f, 1.5f, 0.43f); eye.transform.localScale = Vector3.one * 0.14f;
                eye.GetComponent<Renderer>().sharedMaterial = eyes; UnityEngine.Object.DestroyImmediate(eye.GetComponent<Collider>());
            }
            NavMeshAgent agent = root.AddComponent<NavMeshAgent>(); agent.radius = 0.5f; agent.height = 2; agent.acceleration = 16; agent.stoppingDistance = 0.12f;
            return agent;
        }
    }
}
