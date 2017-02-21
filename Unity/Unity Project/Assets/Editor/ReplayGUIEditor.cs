#if REPLAY_ENGINE

using UnityEditor;
using UnityEngine;
using System.Collections;

namespace RAE {

    [CustomEditor(typeof(ReplayGUI))]
    public class ReplayGUIEditor : Editor {

        public override void OnInspectorGUI() {
            DrawDefaultInspector();
            ReplayGUI.standardButtonWidth = EditorGUILayout.FloatField("Button Width",ReplayGUI.standardButtonWidth);
            ReplayGUI.standardButtonHeight = EditorGUILayout.FloatField("Button Height",ReplayGUI.standardButtonHeight);
            ReplayGUI.standardLabelWidth = EditorGUILayout.FloatField("Label Width",ReplayGUI.standardLabelWidth);
            ReplayGUI.standardTabSize = EditorGUILayout.FloatField("Tab Size", ReplayGUI.standardTabSize);
            if (GUI.changed) {
                EditorUtility.SetDirty(target);
            }
        }

    }
}

#endif