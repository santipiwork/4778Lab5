using AvoiderPlugin;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

[CustomEditor(typeof(Avoider))]
public sealed class AvoiderInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var avoider = (Avoider)target;
        if (avoider.GetComponent<NavMeshAgent>() == null)
        {
            EditorGUILayout.HelpBox("Add a NavMeshAgent to this GameObject and bake a NavMesh before playing.", MessageType.Warning);
            if (GUILayout.Button("Add NavMeshAgent")) Undo.AddComponent<NavMeshAgent>(avoider.gameObject);
        }
        if (avoider.avoidee == null) EditorGUILayout.HelpBox("Drag the Player into the Avoidee field.", MessageType.Warning);
        if (Application.isPlaying) EditorGUILayout.HelpBox(avoider.Status + " | Reachable hidden samples: " + avoider.HiddenCount, MessageType.Info);
        else EditorGUILayout.HelpBox("Bake a NavMeshSurface for your level. Only solid cover should block sight; triggers and both actors are ignored.", MessageType.Info);
    }
}
