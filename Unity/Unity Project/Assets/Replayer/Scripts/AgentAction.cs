using UnityEngine;
using System.Collections;
using System.Text;
using System.Xml;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace RAE {

    /// <summary>
    /// Student action.
    /// </summary>
    /// <remarks>
    /// Change History:
    /// 2012/11/08: Added commnents header. Removed lingering build warning.
    /// </remarks>
    public class AgentAction {
        private static AgentAction nullAction = new AgentAction();

        public static AgentAction NullAction {
            get {
                return nullAction;
            }
        }

        public int Attempt {
            get {
                return attemptNum;
            }
        }

        public string LevelName {
            get {
                return levelName;
            }
        }

        public string User {
            get {
                return userID;
            }
        }

        public string SessionID {
            get {
                return sessionID;
            }
        }

        public string TransactionID {
            get {
                return transactionID;
            }
        }

        public string AttemptID {
            get {
                return attemptID;
            }
        }

        public string Selection {
            get {
                return selection;
            }
        }

        public string Action {
            get {
                return action;
            }
        }

        public string Input {
            get {
                return inputString;
            }
        }

        public JObject InputObject {
            get {
                return inputJSON;
            }
        }

        public System.DateTime Time {
            get {
                return time;
            }
        }

        public JObject StateObject {
            get {
                return this.stateJSON;
            }
        }

        public JObject Extra {
            get {
                return extra;
            }
        }

        public bool HasObjectInput { get { return inputJSON != null;/* && inputJSON[RAEConstants.OBJECT] != null;*/ } }


        private string selection = null;
        private string action = null;
        private string inputString = null;
        private JObject inputJSON = null;
        private JObject stateJSON = null;
        private JObject extra = null;

        private System.DateTime time = System.DateTime.MaxValue;

        private string userID = null;
        private string levelName = null;
        private int attemptNum = -1;
        private string sessionID = null;
        private string transactionID = null;
        private string attemptID = null;
        
        private AgentAction() { }

        public AgentAction(string selection, string action, string input) : this(selection, action, input, "", "", "", "", "", "","", "") { }

        public AgentAction(string selection, string action, string input, string state, string time,
            string user, string levelName, string attemptNum, string sessionID, string transactionID, string attemptID) : this(selection, action, input, state, time, user, levelName, attemptNum, sessionID, transactionID,attemptID , null) { }

        public AgentAction(string selection, string action, string input, string state, string time,
            string user, string levelName, string attemptNum, string sessionID, string transactionID, string attemptID,
            string extra) {

            this.selection = selection;
            this.action = action;
            this.userID = user;
            this.levelName = levelName;
            this.sessionID = sessionID;
            this.transactionID = transactionID;
            this.attemptID = attemptID;

            try {
                this.time = System.DateTime.ParseExact(time, "yyyy-MM-dd HH:mm:ss.fff", System.Globalization.CultureInfo.CurrentCulture);
            }
            catch {
                this.time = System.DateTime.MaxValue;
            }

            try {
                this.attemptNum = int.Parse(attemptNum);
            }
            catch {
                this.attemptNum = -1;
            }

            this.inputString = input;
            if (!string.IsNullOrEmpty(input)) {
                
                try {
                    this.inputJSON = JsonConvert.DeserializeObject<JObject>(input);
                }
                catch {
                    this.inputJSON = null;
                }
            }
            else {
                this.inputJSON = null;
            }
            
            if (!string.IsNullOrEmpty(state)) {
                try {
                    this.stateJSON = JsonConvert.DeserializeObject<JObject>(state);
                }
                catch {
                    this.stateJSON = null;
                }
            }

            if (!string.IsNullOrEmpty(extra)) {
                try {
                    this.extra = JsonConvert.DeserializeObject<JObject>(extra);
                }
                catch {
                    this.extra = null;
                }
            }
            else {
                this.extra = null;
            }
        }

        public AgentAction(string selection, string action, string input, JObject state, DateTime time,
            string user, string levelName, int attemptNum, string sessionID, string transactionID, string attemptID) {
            this.selection = selection;
            this.action = action;
            this.userID = user;
            this.levelName = levelName;
            this.sessionID = sessionID;
            this.transactionID = transactionID;
            this.attemptID = attemptID;
            this.time = time;
            this.attemptNum = attemptNum;
            this.inputString = input;
            if (!string.IsNullOrEmpty(input)) {

                try {
                    this.inputJSON = JsonConvert.DeserializeObject<JObject>(input);
                }
                catch {
                    this.inputJSON = null;
                }
            }
            else {
                this.inputJSON = null;
            }
            this.stateJSON = state;
        }

        public Vector3 GetInputObjectPosition() {
            if (this.HasObjectInput) {
                //Debug.Log(inputJSON);
                float posX = inputJSON[RAEConstants.OBJECT][RAEConstants.TRANSFORM][RAEConstants.POSITION].Value<float>("X");
                float posY = inputJSON[RAEConstants.OBJECT][RAEConstants.TRANSFORM][RAEConstants.POSITION].Value<float>("Y");
                float posZ = inputJSON[RAEConstants.OBJECT][RAEConstants.TRANSFORM][RAEConstants.POSITION].Value<float>("Z");
                return new Vector3(posX, posY, posZ);
            }
            else
                return Vector3.zero;
        }

        public Quaternion GetInputObjectRotation() {
            if (this.HasObjectInput) {
                float rotX = inputJSON[RAEConstants.OBJECT][RAEConstants.TRANSFORM][RAEConstants.ROTATION].Value<float>("X");
                float rotY = inputJSON[RAEConstants.OBJECT][RAEConstants.TRANSFORM][RAEConstants.ROTATION].Value<float>("Y");
                float rotZ = inputJSON[RAEConstants.OBJECT][RAEConstants.TRANSFORM][RAEConstants.ROTATION].Value<float>("Z");
                return Quaternion.Euler(rotX, rotY, rotZ);
            }
            else
                return Quaternion.identity;
        }

        public Vector3 GetInputObjectVelocity() {
            if (this.HasObjectInput) {
                float posX = inputJSON[RAEConstants.OBJECT][RAEConstants.TRANSFORM][RAEConstants.VELOCITY].Value<float>("X");
                float posY = inputJSON[RAEConstants.OBJECT][RAEConstants.TRANSFORM][RAEConstants.VELOCITY].Value<float>("Y");
                float posZ = inputJSON[RAEConstants.OBJECT][RAEConstants.TRANSFORM][RAEConstants.VELOCITY].Value<float>("Z");
                return new Vector3(posX, posY, posZ);
            }
            else
                return Vector3.zero;
        }

        public Quaternion GetInputObjectAngularVelocity() {
            if (this.HasObjectInput) {
                float rotX = inputJSON[RAEConstants.OBJECT][RAEConstants.TRANSFORM][RAEConstants.ANGULAR_VELOCITY].Value<float>("X");
                float rotY = inputJSON[RAEConstants.OBJECT][RAEConstants.TRANSFORM][RAEConstants.ANGULAR_VELOCITY].Value<float>("Y");
                float rotZ = inputJSON[RAEConstants.OBJECT][RAEConstants.TRANSFORM][RAEConstants.ANGULAR_VELOCITY].Value<float>("Z"); 
                return Quaternion.Euler(rotX, rotY, rotZ);
            }
            else
                return Quaternion.identity;
        }

        public override string ToString() {
            return string.Format("SELECTION:{0} ACTION:{1} INPUT:{2} STATE:{3}", selection, action, inputString, stateJSON != null ? JsonConvert.SerializeObject(stateJSON) : "No State");
        }

        public string ToLongString() {
            return string.Format("USERID:{0} SELECTION:{1} ACTION:{2} INPUT:{3} LEVEL:{4} STATE:{5}", 
                userID, selection, action, inputString, levelName, stateJSON != null ? JsonConvert.SerializeObject(stateJSON) : "No State");
        }

        public bool IsSameAttempt(AgentAction other) {
            if (this == NullAction || other == NullAction)
                return false;
            return this.User == other.User
                && this.SessionID == other.SessionID
                && this.LevelName == other.LevelName
                && this.Attempt == other.Attempt;
        }
    }
}