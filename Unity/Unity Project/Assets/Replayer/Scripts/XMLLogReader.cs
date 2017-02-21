#if REPLAY_ENGINE

/*
 * 1/7/17 - Erik
 * This implementation is not being actively maintained but is updated enough to remove compile errors
 * I will take another look at it some time.
 * 
 * 
 */ 

using UnityEngine;
using System.Collections;
using System.Xml;
using System;

namespace RAE {

    public class XMLLogReader : Agent {
        private AgentAction curr = AgentAction.NullAction;
        private AgentAction prev = AgentAction.NullAction;
        private AgentAction next = AgentAction.NullAction;

        public string xmlFilePath = string.Empty;
        private string errorMessage = null;
        private XmlNodeEnumerator nodes = null;
        private bool hasNext = true;

        // Use this for initialization
        void Start() {

        }

        // Update is called once per frame
        void Update() {

        }

        void OnDestroy() {
            nodes.Dispose();
        }

        public override AgentAction CurrentAction {
            get { return curr; }
        }

        public override AgentAction Previous {
            get { return prev; }
        }

        public override AgentAction Next {
            get { return next; }
        }

        public override bool Initialized {
            get { return nodes != null; }
        }

        public override bool HasNext {
            get { return hasNext; }
        }

        public override bool HasNextInSession {
            get { return hasNext; }
        }

        public override IEnumerator Initialize() {
            Debug.Log("Load()");
            nodes = new XmlNodeEnumerator(xmlFilePath);
            yield break;
        }

        public override IEnumerator RequestNextAction() {
            Debug.Log("GetNextStudentAction()");
            prev = curr;
            curr = next;
            XmlNode node = GetNextEvent();
            Debug.Log(node.OuterXml);
            if (node != null) {
                string selection = node[RAEConstants.SELECTION].InnerText;
                string action = node[RAEConstants.ACTION].InnerText;
                string input = node[RAEConstants.INPUT].InnerText;
                string levelName = node[RAEConstants.SELECTION].InnerText;
                string state = node[RAEConstants.STATE].InnerText;
                string time = node[RAEConstants.TIME].InnerText;
                string user = node[RAEConstants.USER_ID].InnerText;
                string attemptNum = node[RAEConstants.ATTEMPT_NUMBER].InnerText;
                string sessionID = node[RAEConstants.SESSION_ID].InnerText;
                string transactionID = node[RAEConstants.TRANSACTION_ID].InnerText;
                next = new AgentAction(selection, action, input, state, time, user, levelName, attemptNum, sessionID, transactionID,"attemptID");
            }
            else {
                next = AgentAction.NullAction;
            }
            yield break;
        }

        private XmlNode GetNextEvent() {
            if (nodes.MoveNext()) {
                XmlNode ret = nodes.Current as XmlNode;
                if (ret == null) {
                    hasNext = false;
                    return null;
                }
                return ret;
            }
            else {
                Debug.Log("nodes.MoveNext() == false");
                hasNext = false;
                return null;
            }
        }

        private Vector2 scrollPos = Vector2.zero;
        public override void OptionsPane() {
            // GUILayout.BeginArea(optionArea);
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            GUILayout.BeginVertical();

            xmlFilePath = FixPath(ReplayGUI.TextField(xmlFilePath, "XML File Path"));

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            //GUILayout.EndArea();
        }

        public override string GUIName {
            get { return "XML Reader Options"; }
        }

        void OnGUI() {
            if (!string.IsNullOrEmpty(errorMessage)) {
                if (ReplayGUI.ErrorMessage(errorMessage)) {
                    errorMessage = string.Empty;
                }
            }
        }

        private string FixPath(string path) {
            return path.Trim('"');
        }

        class XmlNodeEnumerator : IEnumerator {
            private XmlReader reader;
            public object Current {
                get {
                    return currentNode;
                }
            }

            private XmlNode currentNode;
            private string filePath;

            public XmlNodeEnumerator(string filePath) {
                this.filePath = filePath;
                Reset();
            }

            public bool MoveNext() {
                XmlDocument xmlDoc = new XmlDocument();
                XmlNode testNode;

                try {
                    testNode = xmlDoc.ReadNode(reader);
                }
                catch (System.Exception ex) {
                    Debug.LogException(ex);
                    return false;
                }
                if (testNode != null) {
                    currentNode = testNode;
                    return true;
                }
                return false;
            }

            public void Reset() {
                reader = new XmlTextReader(filePath);
                reader.ReadToDescendant("Event");
            }

            public void Dispose() {
                reader.Close();
                currentNode = null;
            }
        }

        public override string RunReportName {
            get { return "XML LOG READER"; }
        }

        public override ActionRequestStatus ActionStatus {
            get {
                return ActionRequestStatus.Idle;
            }
        }

        public override string RunReport() {
            return "Nothing to Report, This reader is probably broken!";
        }

    }
}

#endif