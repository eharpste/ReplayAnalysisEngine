#if REPLAY_ENGINE

using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Collections.Specialized;
using Newtonsoft.Json.Linq;

namespace RAE {

    public class ReplayExtender : RAEComponent {
        public enum GoalStatus {
            Success,
            Failure,
            Pending,
            Skip
        }

        protected ReplayGUI mainGUI;

        void Start() {
            mainGUI = GetComponent<ReplayGUI>();
        }

        public virtual IEnumerator OnIterationStart() { yield break; }

        public virtual IEnumerator OnActionPre(AgentAction action) { yield break; }

        public virtual IEnumerator AssumeState(JObject state) { return null; }

        public virtual IEnumerator PerformAction(AgentAction action) { yield break; }

        public virtual IEnumerator OnActionPost(AgentAction action) { yield break; }

        public virtual IEnumerator StoppingCoroutine(AgentAction action) { yield break; }

        public virtual IEnumerator OnStop(AgentAction action) { yield break; }

        public virtual IEnumerator OnScreenshotPre() { yield break; }

        public virtual IEnumerator OnScreenshotPost() { yield break; }

        public virtual IEnumerator OnIterationEnd() { yield break; }

        public virtual IEnumerator OnPause() { yield break; }

        public virtual IEnumerator OnUnpause() { yield break; }

        public virtual JObject DescribeState() { return null; }

        public virtual JObject DescribeTrainingState() { return null; }

        public virtual JObject DescribeActionInput() { return null; }

        public virtual GoalStatus GoalState() { return GoalStatus.Skip; }

        // public virtual GameObject RetrieveGameObject(JObject logobj, AgentAction action) { return null; }

        // public virtual ReplayBehavior AssumeState(JObject stateObj) { return AssumeState(stateObj, null); }

        // public virtual JObject RecordState() { return null;}


        #region Built in Action Skipping System

        //protected List<string> seeActionTypes = new List<string>();
        protected List<string> skipActionTypes = new List<string>();
        protected OrderedDictionary seeActionTypes = new OrderedDictionary();

        public virtual bool SkipAction(AgentAction action) {
            if (action != null && action.Action != null) {
                if (seeActionTypes.Contains(action.Action)) {
                    int holder = ((int)seeActionTypes[action.Action]);
                    holder += 1;
                    seeActionTypes[action.Action] = holder;
                }
                else {
                    seeActionTypes.Add(action.Action, 1);
                }
            }
            return skipActionTypes.Contains(action.Action);

        }

        private Vector2 scrollPos = Vector2.zero;
        private float charButtonWidth = 24;

        public override void OptionsPane() {
            if (mainGUI == null) {
                mainGUI = GetComponent<ReplayGUI>();
            }


            // GUILayout.BeginArea(layoutRect);
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            GUILayout.BeginVertical();
            GUILayout.Label("Action Filtering");

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            int removeDex = -1;
            for (int i = 0; i < skipActionTypes.Count; i++) {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("-", GUILayout.Width(charButtonWidth), GUILayout.Height(ReplayGUI.standardButtonHeight))) {
                    removeDex = i;
                }
                skipActionTypes[i] = GUILayout.TextField(skipActionTypes[i], GUILayout.Width(ReplayGUI.standardButtonWidth * 2), GUILayout.Height(ReplayGUI.standardButtonHeight));
                GUILayout.EndHorizontal();
            }
            if (removeDex >= 0) {
                skipActionTypes.RemoveAt(removeDex);
            }

            GUILayout.Space(ReplayGUI.standardButtonHeight / 2);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+", GUILayout.Width(charButtonWidth), GUILayout.Height(ReplayGUI.standardButtonHeight))) {
                AddSkipActionType(NEW_TYPE);
            }
            bool bak = GUI.enabled;
            GUI.enabled = false;
            GUILayout.TextField(NEW_TYPE, GUILayout.Width(ReplayGUI.standardButtonWidth * 3), GUILayout.Height(ReplayGUI.standardButtonHeight));
            GUI.enabled = bak;
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.Space(ReplayGUI.standardButtonWidth / 2);

            GUILayout.BeginVertical();
            if (seeActionTypes.Count == 0) {
                GUILayout.Label("No Actions Seen Yet.", GUILayout.Width(ReplayGUI.standardButtonWidth * 3), GUILayout.Height(ReplayGUI.standardButtonHeight));
            }
            else {
                foreach (string s in seeActionTypes.Keys) {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("<", GUILayout.Width(charButtonWidth), GUILayout.Height(ReplayGUI.standardButtonHeight))) {
                        AddSkipActionType(s);
                    }
                    GUILayout.Label(s + " : " + seeActionTypes[s], GUILayout.Width(ReplayGUI.standardButtonWidth * 3), GUILayout.Height(ReplayGUI.standardButtonHeight));
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            //GUILayout.EndArea();
        }

        public override string GUIName {
            get { return "Filter Options"; }
        }

        public override string RunReport() {
            return "Nothing to Report";
        }

        public override string RunReportName {
            get {
                return "BASE REPLAY EXTENDER";
            }
        }

        private const string NEW_TYPE = "**Add New Type**";

        private void AddSkipActionType(string type) {
            foreach (string s in skipActionTypes) {
                if (s == type && type != NEW_TYPE) {
                    return;
                }
            }
            skipActionTypes.Add(type);
        }

#endregion

    }

}

#endif