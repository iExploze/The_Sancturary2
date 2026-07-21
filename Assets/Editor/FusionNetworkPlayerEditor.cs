using TheSancturary.FusionPrototype;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FusionNetworkPlayer))]
[CanEditMultipleObjects]
public sealed class FusionNetworkPlayerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
        {
            if (GUILayout.Button("Subtract Inspector Damage"))
            {
                foreach (Object inspectedTarget in targets)
                    ((FusionNetworkPlayer)inspectedTarget).TriggerInspectorDamage();
            }
        }

        if (!EditorApplication.isPlaying)
            EditorGUILayout.HelpBox("Enter Play Mode to apply the configured integer damage to this player.", MessageType.Info);
    }
}
