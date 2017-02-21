#if REPLAY_ENGINE

using UnityEditor;
using UnityEngine;
using System.Collections;

namespace RAE {

    [CustomEditor(typeof(ReplayAnalysisEngine))]
    public class ReplayAnalysisEngineEditor : Editor {

        private int lastPause = 10;

        public override void OnInspectorGUI() {
            ReplayAnalysisEngine rae = target as ReplayAnalysisEngine;

            EditorGUILayout.BeginVertical();

            Application.targetFrameRate = EditorGUILayout.IntField("Target FrameRate", Application.targetFrameRate);
            Application.runInBackground = EditorGUILayout.Toggle("Run In Background", Application.runInBackground);

            //rae.ReplayMode = (ReplayAnalysisEngine.IterationMode)EditorGUILayout.EnumPopup("Iteration Mode", rae.ReplayMode);

            rae.stopCondition = (ReplayAnalysisEngine.StoppingCondition)EditorGUILayout.EnumPopup("Stopping Condition", rae.stopCondition);

            switch (rae.stopCondition) {
                case ReplayAnalysisEngine.StoppingCondition.Instant:
                    break;

                case ReplayAnalysisEngine.StoppingCondition.WaitForStop:
                case ReplayAnalysisEngine.StoppingCondition.TimeOut:
                case ReplayAnalysisEngine.StoppingCondition.Custom:
                case ReplayAnalysisEngine.StoppingCondition.Simulate:
                    rae.TimeAcceleration = EditorGUILayout.FloatField("Time Acceleration", rae.TimeAcceleration);
                    rae.TimeOut = EditorGUILayout.FloatField("Time Out", rae.TimeOut);
                    break;
            }

            rae.ScreenShotTiming = (ReplayAnalysisEngine.ScreenShotTimingOption)EditorGUILayout.EnumPopup("Screenshot Timing", rae.screenshotTiming);

            if (rae.screenshotTiming != ReplayAnalysisEngine.ScreenShotTimingOption.Disabled) {
                rae.screenShotMode = (ReplayAnalysisEngine.ScreenShotModeOption)EditorGUILayout.EnumPopup("Screenshot Mode", rae.screenShotMode);
                rae.screenshotDirectory = EditorGUILayout.TextField("Screenshot Directory",rae.screenshotDirectory);
            }

            bool before = rae.PauseAfter > 0;

            bool check = EditorGUILayout.Toggle("Pause", rae.PauseAfter > 0);

            if (before != check) {
                if (before) {
                    rae.PauseAfter = -1;
                }
                else {
                    rae.PauseAfter = lastPause;
                }
            }

            if (rae.PauseAfter > 0) {
                lastPause = EditorGUILayout.IntField("Pause After Every", (int)rae.PauseAfter);
                if (lastPause < 1) {
                    lastPause = 1;
                }
                rae.PauseAfter = lastPause;
            }

            rae.runReportPath = EditorGUILayout.TextField("Report Path", rae.runReportPath);

            rae.heavyDebug = EditorGUILayout.Toggle("Heavy Debug",rae.heavyDebug);

            EditorGUILayout.EndVertical();

            if (GUI.changed) {
                EditorUtility.SetDirty(rae);
            }
        }
    }
}

#endif