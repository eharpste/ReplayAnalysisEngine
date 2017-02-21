#if REPLAY_ENGINE


using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Text;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MySql.Data.MySqlClient;

namespace RAE {

    public class TrestleApprenticeLearerAgent : Agent {

        #region Constants

        public class EventTypes {
            public const string User_Changed = "UserChanged";
        }

        #endregion

        #region =================|    Public Variables / Inspector Properties     |========================

        public string ConnString {
            get {
                return String.Format("Server={0};Port={1};Database={2};Uid={3};password={4}",
                                                db_url,
                                                db_port,
                                                db_name,
                                                db_user,
                                                db_pass);
            }
        }

        #region Database Connection Settings
        [Header("Database Settings")]
        public string db_url = "127.0.0.1";
        public string db_port = "3306";
        public string db_name = "rae";
        public string db_user = "root";
        public string db_pass = "password";
        public string db_table = "penntrafF_steps";
        public int db_timeout = 60 * 60 * 24 * 3;
        [TextArea(3,7)]
        public string db_where_clause = string.Empty;
        [TextArea(3, 7)]
        public string db_other_clause = string.Empty;
        
        #endregion

        #region Apprentice API Connection Settings
        [Header("API Settings")]
        public string api_url = "http://127.0.0.1:8000";
        public string api_train_url = "/train_action/";
        public string api_request_url = "/request_action/";
        public string api_create_url = "/create_tree/";
        public string api_report_url = "/report/";
        public float api_timeout = 10;
        #endregion

        #region Agent Settings        
        [Header("Agent Settings")]
        public ApprenticeAgentType apprentice_agent_type = ApprenticeAgentType.LogicalWhenHow;
        public AgentCorrectnessType agent_correctness_type = AgentCorrectnessType.GameGoal;
        public UserAgentStyle user_agent_style = UserAgentStyle.NewPerUser;
        public string omnibusAgentName = "Omnibus";
        public string agent_actionset = "";
        private Dictionary<string, string> user_agent_map = new Dictionary<string, string>();
        private List<AgentAction> back_prop_actions = new List<AgentAction>();

        private List<AgentAction> last_observed_actions = new List<AgentAction>();
        private int last_observed_dex = -1;
        #endregion

        // Action Planner Settings
        //public float explanationEpsilon = .15f;
        //public int explanationDepthLimit = 4;
        //public int numberOfExplanations = 3;
        //public float explanationTimeLimit = -1;

        public bool verboseLogs = false;


#endregion ========================================================================================


#region ============================|     Component Pointers     |=================================

        private ReplayExtender extender = null;
        //private Agent reader = null;

#endregion


#region ============================|          Agent API         |=================================

        private bool ready = false;
        public override bool Ready { get { return this.ready && !waitingOnAPI; } }

        private bool initialized = false;
        public override bool Initialized { get { return this.initialized; } }

        private List<AgentAction> cached_actions = new List<AgentAction>();
        private Dictionary<string, List<int>> cached_actions_by_level;
        private List<string> cached_users = new List<string>();
        private int current_user_dex = -1;
        private int current_action_dex = -1;

        public string CurrentUserId {
            get {
                if (!this.initialized) {
                    return "None";
                }
                return current_user_dex < cached_users.Count ? cached_users[current_user_dex] : "None";
            }
        }

        private string CurrentAgentName {
            get {
                if (!this.initialized) { return null; }
                switch (user_agent_style) {
                    case UserAgentStyle.NewPerUser:
                    case UserAgentStyle.OldPerUser:
                        return CurrentUserId;
                    case UserAgentStyle.NewOmnibus:
                    case UserAgentStyle.OldOmnibus:
                        return omnibusAgentName;
                    default:
                        return null;
                }
            }
        }

        private string CurrentAgentID {
            get {
                if (!this.initialized) { return null; }
                switch (user_agent_style) {
                    case UserAgentStyle.NewPerUser:
                    case UserAgentStyle.OldPerUser:
                        if (user_agent_map.ContainsKey(CurrentUserId)) {
                            return user_agent_map[CurrentUserId];
                        }
                        else {
                            return null;
                        }
                    case UserAgentStyle.NewOmnibus:
                    case UserAgentStyle.OldOmnibus:
                        if (user_agent_map.ContainsKey(omnibusAgentName)) {
                            return user_agent_map[omnibusAgentName];
                        }
                        else {
                            return null;
                        }
                    default:
                        return null;
                }
            }
        }





        private AgentAction CurrentCachedAction {
            get {
                return cached_actions[current_action_dex];
            }
        }

        private bool waitingOnAPI = false;

        private AgentOrientation orientation;
        public AgentOrientation Orientation { get { return orientation; } }

        private ActionRequestStatus requestStatus = ActionRequestStatus.Idle;
        public override ActionRequestStatus ActionStatus { get { return requestStatus; } }

        private MySqlConnection conn;

        public override IEnumerator Initialize() {
            string connString = ConnString;

            conn = new MySqlConnection(connString);
            CacheUserList();
            CacheUserLogs();

            this.orientation = AgentOrientation.Watching;

            this.initialized = true;

            //TODO - I think...?
            ready = true;
            yield break;
        }

        private void CacheUserList() {
            MySqlCommand command = conn.CreateCommand();

            if (string.IsNullOrEmpty(db_where_clause)) {
                command.CommandText = String.Format("SELECT DISTINCT(userID) FROM {0};", db_table);
            }
            else {
                command.CommandText = String.Format("SELECT DISTINCT(userID) FROM {0} WHERE {1};", db_table, db_where_clause);
            }

            try {
                conn.Open();
            }
            catch (Exception ex) {
                Debug.LogException(ex);
                ReplayGUI.ErrorMessage(string.Format("{0}\n{1}", ex.Message, ex.StackTrace));
            }
            Debug.LogFormat("<color=purple>MySQL Query:</color> {0}", command.CommandText);
            MySqlDataReader reader = command.ExecuteReader();

            cached_users = new List<string>();
            while (reader.Read()) {
                cached_users.Add(reader.GetString(0));
            }

            conn.Close();

            current_user_dex = 0;
        }

        private void CacheUserLogs() {
            MySqlCommand command = conn.CreateCommand();

            if (string.IsNullOrEmpty(db_where_clause)) {
                command.CommandText = String.Format("SELECT selection, action, input, state, eventTime, userID, levelName, attemptNumber, sessionID, transactionID, attemptID FROM {0} WHERE userID=\"{1}\" ORDER BY eventTime;", db_table, cached_users[current_user_dex]);
            }
            else {
                string where = string.Format("userID = \"{0}\" AND {1}", cached_users[current_user_dex], db_where_clause);
                command.CommandText = String.Format("SELECT selection, action, input, state, eventTime, userID, levelName, attemptNumber, sessionID, transactionID, attemptID FROM {0} WHERE {1} ORDER BY eventTime;", db_table, where);
            }

            try {
                conn.Open();
            }
            catch (Exception ex) {
                Debug.LogException(ex);
                ReplayGUI.ErrorMessage(string.Format("{0}\n{1}", ex.Message, ex.StackTrace));
            }
            Debug.LogFormat("<color=purple>MySQL Query:</color> {0}", command.CommandText);
            MySqlDataReader reader = command.ExecuteReader();

            cached_actions = new List<AgentAction>();
            while (reader.Read()) {
                AgentAction act = new AgentAction(
                        reader["selection"].ToString(),
                        reader["action"].ToString(),
                        reader["input"].ToString(),
                        reader["state"].ToString(),
                        reader["eventTime"].ToString(),
                        reader["userID"].ToString(),
                        reader["levelName"].ToString(),
                        reader["attemptNumber"].ToString(),
                        reader["sessionID"].ToString(),
                        reader["transactionID"].ToString(),
                        reader["attemptID"].ToString());
                cached_actions.Add(act);
            }

            conn.Close();

            Debug.LogFormat("Cached {0} actions for user {1}", cached_actions.Count, cached_users[current_user_dex]);

            cached_actions_by_level = new Dictionary<string, List<int>>();
            for(int i = 0; i < cached_actions.Count; i ++) {
                AgentAction act = cached_actions[i];
                if(act.Action == "Level_Start") {
                    continue;
                }
                if (!cached_actions_by_level.ContainsKey(act.LevelName)) {
                    cached_actions_by_level.Add(act.LevelName, new List<int>());
                }
                cached_actions_by_level[act.LevelName].Add(i);
            }
            current_action_dex = -1;
            FireEvent(EventTypes.User_Changed);
        }

        private AgentAction lastAction = AgentAction.NullAction;
        private AgentAction currAction = AgentAction.NullAction;
        public override AgentAction CurrentAction { get { return currAction; }}

        public override AgentAction Next {
            get {
                if(orientation == AgentOrientation.Watching) {
                    return cached_actions[current_action_dex + 1];
                }
                else {
                    return AgentAction.NullAction;
                }
            }
        }

        public override AgentAction Previous {
            get {
                return lastAction;
            }
        }

        public override bool HasNext {
            get {
                return current_user_dex < cached_users.Count && current_action_dex < cached_actions.Count-1;
            }
        }

        public override bool HasNextInSession {
            get {
                return current_action_dex < cached_actions.Count-1;
            }
        }

        //in initialize pull down a list of users from the DB
        //pick the first one
        //pull down their entire history ~1000 actions at most
        //while observing feed the next action in the set
        //once autonomous then run with it
        //if we fall out of sync then will need some kind of fallback.
        //
        private AgentAction NextCachedAttempt() {
            Debug.LogFormat("NextCatchedAttempt()");
            while (Previous.IsSameAttempt(CurrentAction)) {
                NextCachedAction();
            }
            return CurrentAction;
        }

        private AgentAction NextCachedAttemptOnLevel(string levelName) {
            Debug.LogFormat("NextCatchedAttemptOnLevel()");
            if (!this.Initialized) return AgentAction.NullAction;
            
            if (!cached_actions_by_level.ContainsKey(levelName)) {
                Debug.LogWarningFormat("No Actions for this level:{0}", levelName);
                return AgentAction.NullAction;
            }
            //Debug.LogFormat("<color=green>CachedActions:</color>{0}", JsonConvert.SerializeObject(cached_actions_by_level));
            List<int> lvl = cached_actions_by_level[levelName];
            int curr_dex = current_action_dex;

            if(lvl.Contains(current_action_dex)) {
                //Debug.LogFormat("<color=red>DEX IN LEVEL - current_action_dex:{0},level_dexs:{1}</color>", current_action_dex, JsonConvert.SerializeObject(lvl));
                // If the current_action_dex is in this level 
                // Then search through the dexs in this level until there is a new attempt.
                foreach (int dex in lvl) {
                    if (dex < curr_dex) {
                        continue;
                    }
                    if (!cached_actions[curr_dex].IsSameAttempt(cached_actions[dex])){
                        curr_dex = dex;
                        break;
                    }
                }
            }
            else {
                //Debug.LogFormat("<color=red>DEX NOT IN LEVEL - current_action_dex:{0},level_dexs:{1}</color>", current_action_dex, JsonConvert.SerializeObject(lvl));
                // If the current_action_dex is not in this level
                // Then find the first action_dex in this level that is greater than current
                foreach (int dex in lvl) {
                    if (dex > curr_dex) {
                        curr_dex = dex;
                        break;
                    }
                }
            }
            if (current_action_dex == curr_dex) {
                //Debug.LogFormat("<color=red>DEX UNCHANGED</color>");
                //if after all of that level_dex hasn't changed then we're out of actions on this user.
                current_user_dex++;
                CacheUserLogs();
            }
            else {
                current_action_dex = curr_dex;
                currAction = cached_actions[current_action_dex];
            }
            return currAction;
        }
      
        private AgentAction NextCachedAction() {
            //do {
                
            //}
            //while (extender.SkipAction(cached_actions[current_action_dex]));
            AgentAction resp = cached_actions[current_action_dex];
            current_action_dex++;
            if (current_action_dex >= cached_actions.Count) {
                Debug.Log("Out of actions on this user moving to next");
                current_user_dex++;
                if (current_user_dex == cached_users.Count) {
                    Debug.Log("Out of actions.");
                    requestStatus = ActionRequestStatus.OutOfActions;
                    return AgentAction.NullAction;
                }
                else {
                    CacheUserLogs();
                }
            }
            return resp;
        }

        private IEnumerator NextAPIAction() {
            if (string.IsNullOrEmpty(CurrentAgentID)) { 
                yield return StartCoroutine(CreateOrFindAgent(CurrentAgentName));
                while (waitingOnAPI) { yield return 0; }
            }
            yield return StartCoroutine(APIRequest(user_agent_map[CurrentUserId]));

            while(waitingOnAPI) { yield return 0; }

            yield break;
        }

        public override IEnumerator RequestNextAction() {
            ready = false;
            requestStatus = ActionRequestStatus.Requesting;
            switch (orientation) {
                case AgentOrientation.Watching:
                    yield return StartCoroutine(NextAPIAction());
                    while(waitingOnAPI) { yield return 0; }
                    if (orientation == AgentOrientation.Watching) {
                        lastAction = currAction;
                        currAction = NextCachedAction();
                    }
                    break;
                case AgentOrientation.Trying:
                    yield return StartCoroutine(NextAPIAction());
                    while (waitingOnAPI) { yield return 0; }
                    break;
                case AgentOrientation.Listening:
                    lastAction = currAction;            
                    //TODO - Might need to cache a state here?
                    currAction = NextCachedAction();
                    break;
                default:
                    Debug.LogErrorFormat("Not sure what to do with Agent Orientation:{0}", orientation);
                    break;
            }
            requestStatus = ActionRequestStatus.ActionFound;
            //Debug.LogFormat("Returning Action - {0}", currAction.ToLongString());
            BackUpState();
            ready = true;
        }

        public float epsilon = 0.9f;
        public bool IsEpsilonEqual(JObject oldState, JObject newState) {
            foreach (JProperty oldProp in oldState.Properties()) {
                
                JProperty newProp = newState.Property(oldProp.Name);
                //Debug.LogFormat("Comparing oldProp (name:{0} type:{1}) to newProp (name:{2} type:{3})",
                    //oldProp.Name,oldProp.Value.Type,newProp.Name,newProp.Value.Type);
                if (newProp == null) {
                    //Debug.Log("<color=red>Property doesn't exist in new</color>");
                    return false;
                }
                if(oldProp.Value.Type != newProp.Value.Type) {
                    //Debug.Log("<color=red>Types are not equal</color>");
                    return false;
                }
                switch (oldProp.Value.Type) {
                    case JTokenType.Float:
                        float oldFloat = (float)oldProp.Value;
                        float newFloat = (float)newProp.Value;
                        //Debug.LogFormat("<color=red>Comparing Floats</color>: {0} to {1}",oldFloat,newFloat);
                        if (Mathf.Abs(oldFloat - newFloat) > epsilon) {
                            return false;                            
                        }
                        break;
                    case JTokenType.Object:
                        //Debug.LogFormat("<color=red>Comparing Floats</color>");
                        if (!IsEpsilonEqual( (JObject)oldProp.Value, (JObject)newProp.Value)) {
                            return false;
                        }
                        break;
                    default:
                        //Debug.LogFormat("<color=red>Comparing Other Types</color>: {0} to {1}",oldProp.Value, newProp.Value);
                        if (!oldProp.Value.Equals(newProp.Value)) {
                            return false;
                        }
                        break;
                }
                
            }
            return true;
        }

        public override IEnumerator EvaluateActionResult() {
            AgentAction t_action = AgentAction.NullAction;
            switch (orientation) {
                case AgentOrientation.Trying:
                    t_action = new AgentAction(
                        CurrentAction.Selection,
                        CurrentAction.Action,
                        CurrentAction.Input,
                        lastRequestState,
                        CurrentAction.Time,
                        CurrentAction.User,
                        SceneManager.GetActiveScene().name,
                        CurrentAction.Attempt,
                        CurrentAction.SessionID,
                        Guid.NewGuid().ToString(),
                        CurrentAction.AttemptID);

                    if (IsEpsilonEqual(lastRequestState, extender.DescribeState())) {
                        Debug.LogFormat("<color=red>State Unchagned</color>");
                        yield return StartCoroutine(APITrain(CurrentAgentID, t_action, false));
                        yield break;
                    }
                    break;
                case AgentOrientation.Watching:
                case AgentOrientation.Listening:
                    t_action = new AgentAction(
                                CurrentAction.Selection,
                                CurrentAction.Action,
                                CurrentAction.Input,
                                extender.DescribeTrainingState(),
                                CurrentAction.Time,
                                CurrentAction.User,
                                SceneManager.GetActiveScene().name,
                                CurrentAction.Attempt,
                                CurrentAction.SessionID,
                                Guid.NewGuid().ToString(),
                                CurrentAction.AttemptID);       
                    break;
                default:
                    Debug.LogErrorFormat("Don't know what to do with AgentOrientation in EvaluationActionResult: {0}", orientation);
                    break;
            }
            switch (extender.GoalState()) {
                case ReplayExtender.GoalStatus.Pending:
                    back_prop_actions.Add(t_action);
                    break;
                case ReplayExtender.GoalStatus.Success:
                case ReplayExtender.GoalStatus.Failure:
                    back_prop_actions.Add(t_action);
                    yield return StartCoroutine(APITrainBatch(user_agent_map[CurrentUserId], extender.GoalState()));
                    break;
                default:
                    break;
            }
            yield break;
        }



        public override string RunReportName {
            get {
                return "APPRENTICE LEARNER AGENT";
            }
        }

        public override string RunReport() {
            return string.Format("TargetURL:{0}\nTrainingURL:{1}\nRequestURL:{2}\nCreateURL:{3}\nDataURL:{4}", api_url, api_train_url, api_request_url, api_create_url, api_train_url);
        }

        public override string GUIName {
            get {
                return "Apprentice Agent";
            }
        }

        #endregion

        #region ============================|          Enums             |=================================

        public enum Correctness {
            Correct,
            Incorrect,
            Missing
        }

        public enum ApprenticeAgentType {
            Dummy,
            WhereWhenHow,
            LogicalWhenHow,
            LogicalWhereWhenHow
        }

        public string AgentTypeName(ApprenticeAgentType type) {
            switch (type) {
                case ApprenticeAgentType.Dummy:
                    return "Dummy";
                case ApprenticeAgentType.LogicalWhenHow:
                    return "LogicalWhenHow";
                case ApprenticeAgentType.LogicalWhereWhenHow:
                    return "LogicalWhereWhenHow";
                case ApprenticeAgentType.WhereWhenHow:
                    return "WhereWhenHow";
                default:
                    return "None";
            }
        }

        public enum UserAgentStyle {
            NewPerUser,
            OldPerUser,
            NewOmnibus,
            OldOmnibus
        }

        public enum AgentCorrectnessType {
            GameGoal,
            Emulation
        }

        public enum AgentOrientation {
            Idle,
            Watching,
            Trying,
            Listening,
            Stuck
        }

        #endregion ========================================================================================


        #region =============================|      Unity Methods        |=================================

        // Use this for initialization
        void Start() {
            extender = GetComponent<ReplayExtender>();
        }

        // Update is called once per frame
        void Update() {
           
        }

        void OnDestroy() {

        }

        void OnLevelWasLoaded(int level) {
            if (!this.Initialized) return;
            back_prop_actions = new List<AgentAction>();
            last_observed_actions = new List<AgentAction>();
            last_observed_dex = -1;
            if (orientation == AgentOrientation.Listening) {
                orientation = AgentOrientation.Watching;
            }
            NextCachedAttemptOnLevel(SceneManager.GetActiveScene().name);
            ready = true;
        }

        public override void OptionsPane() {
            ReplayGUI.Label("THIS MENU IS PROBABLY OUT OF DATE USE THE INSPECTOR FOR NOW.");
        }

        public override void DebugGUI() {
            GUILayout.Label(string.Format("Orientation:{0}", this.orientation));
            GUILayout.Label(string.Format("CachedActions:{0}", cached_actions.Count));
            GUILayout.Label(string.Format("BackPropActions:{0}", back_prop_actions.Count));
            GUILayout.Label(string.Format("CurrentUser:{0}", CurrentUserId));
            GUILayout.Label(string.Format("CurrActionDex:{0}", current_action_dex));
        }


#endregion ========================================================================================  

#region =============================|     State Management      |=================================

        private JObject lastRequestState = null;
        
        private void BackUpState() {
            lastRequestState = extender.DescribeState();
        }
        
        private void RevertState() {
            extender.AssumeState(lastRequestState);
        }

        #endregion ========================================================================================

        #region ========================|     Apprentice API Connection        |===========================

        public IEnumerator CreateOrFindAgent(string agent_name) {
            switch (user_agent_style) {
                case UserAgentStyle.NewPerUser:
                    yield return StartCoroutine(APICreate(agent_name));
                    break;
                case UserAgentStyle.OldPerUser:
                    yield return StartCoroutine(APIReport(agent_name));
                    break;
                case UserAgentStyle.NewOmnibus:
                    yield return StartCoroutine(APICreate(omnibusAgentName));
                    break;
                case UserAgentStyle.OldOmnibus:
                    yield return StartCoroutine(APIReport(omnibusAgentName));
                    break;
                default:
                    Debug.LogErrorFormat("Don't know what to do with agent_style:{0}", user_agent_style);
                    break;
            }
            yield break;
        }

        public IEnumerator APICreate(string agent_name) {
            waitingOnAPI = true;
            JObject data = new JObject();
            data["name"] = agent_name;
            data["agent_type"] = AgentTypeName(apprentice_agent_type);
            data["action_set"] = agent_actionset;
            yield return StartCoroutine(SendWWW(data, api_create_url, ProcessCreateResponse));
        }

        public void ProcessCreateResponse(WWW www) {
            Debug.Log("Agent created");
            JObject response = JsonConvert.DeserializeObject<JObject>(www.text);
            user_agent_map[CurrentUserId] = response.Value<string>("agent_id");
            waitingOnAPI = false;
        }

        public IEnumerator APIReport(string agent_name) {
            waitingOnAPI = true;
            yield return StartCoroutine(SendWWW(api_report_url, CurrentAgentName, ProcessReportResponse));
        }

        public void ProcessReportResponse(WWW www) { 

            if(!string.IsNullOrEmpty( www.error) ){
                Debug.Log("No Agent Found");
                StartCoroutine(APICreate(CurrentAgentName));
            }
            else {
                Debug.Log("Found Agent");
                JObject response = JsonConvert.DeserializeObject<JObject>(www.text);
                user_agent_map[response.Value<string>("name")] = response.Value<string>("id");
                waitingOnAPI = false;
            }
        }

        public IEnumerator APIRequest(string agent_id) {
            waitingOnAPI = true;
            JObject data = new JObject();
            data["state"] = extender.DescribeState();
            BackUpState();
            yield return StartCoroutine(SendWWW(data, api_request_url, agent_id, ProcessRequestResponse));
        }

        private void ProcessRequestResponse(WWW www) {
            if (string.IsNullOrEmpty(www.error)) {
                // backup the state
                
                JObject response = JsonConvert.DeserializeObject<JObject>(www.text);

                //If no action was returned
                if (response.Count == 0) {
                    switch(orientation) {
                        case AgentOrientation.Watching:
                            waitingOnAPI = false;
                            return;
                        case AgentOrientation.Trying:
                            current_action_dex = last_observed_dex;
                            back_prop_actions = last_observed_actions;
                            orientation = AgentOrientation.Listening;
                            waitingOnAPI = false;
                            return;
                        case AgentOrientation.Stuck:
                        case AgentOrientation.Idle:
                        case AgentOrientation.Listening:
                            Debug.LogErrorFormat("No Action Returned in {0} Orientation; this should be impossible!",orientation);
                            return;
                    }
                }
                // IF an action was returned
                else {
                    //Debug.LogFormat("RESPONSE: {0}",response);
                    AgentAction act = new AgentAction(response.Value<string>("selection"),
                                            response.Value<string>("action"),
                                            JsonConvert.SerializeObject(response["inputs"]),
                                            null,
                                            DateTime.Now,
                                            CurrentUserId,
                                            SceneManager.GetActiveScene().name,
                                            CurrentCachedAction.Attempt,
                                            CurrentCachedAction.SessionID,
                                            Guid.NewGuid().ToString(),
                                            CurrentCachedAction.AttemptID);
                    
                    switch (orientation) {
                        case AgentOrientation.Watching:
                            last_observed_actions = new List<AgentAction>(back_prop_actions);
                            last_observed_dex = current_action_dex;
                            this.orientation = AgentOrientation.Trying;
                            break;
                        case AgentOrientation.Trying:
                            break;
                        default:
                            Debug.LogErrorFormat("Don't know what to do with orientation {0}", orientation);
                            break;
                    }

                    this.lastAction = currAction;
                    this.currAction = act;
                    waitingOnAPI = false;
                }
            }
        }

        public IEnumerator APITrainBatch(string agent, ReplayExtender.GoalStatus status) {
            foreach(AgentAction act in this.back_prop_actions) {
                bool correct;
                switch (this.agent_correctness_type) {
                    case AgentCorrectnessType.GameGoal:
                        correct = status == ReplayExtender.GoalStatus.Success ? true : false;
                        break;
                    case AgentCorrectnessType.Emulation:
                        correct = true;
                        break;
                    default:
                        Debug.LogErrorFormat("Don't know what to do with correctnessType:{0}", this.agent_correctness_type);
                        yield break;
                }
                yield return StartCoroutine(APITrain(agent, act, correct));
                while(waitingOnAPI) { yield return 0; }
            }
        }

        public IEnumerator APITrain(string agentId, AgentAction action, bool correct) {
            waitingOnAPI = true;
            JObject data = new JObject();
            //data["state"] = JsonConvert.SerializeObject(extender.DescribeState());
            data["state"] = action.StateObject;
            data["correct"] = correct;
            data["selection"] = action.Selection;
            data["action"] = action.Action;
            data["inputs"] = action.InputObject;
            yield return StartCoroutine(SendWWW(data, api_train_url, agentId, APITrainResponse));
        }

        private void APITrainResponse(WWW www) {
            waitingOnAPI = false;
        }

        public IEnumerator SendWWW(string target_view, System.Action<WWW> onResponse) {
            yield return SendWWW(null, target_view, string.Empty, onResponse);
        }

        public IEnumerator SendWWW(string target_view, string target_id, System.Action<WWW> onResponse) {
            yield return SendWWW(null, target_view, target_id, onResponse);
        }

        public IEnumerator SendWWW(JObject data, string target_view, System.Action<WWW> onResponse) {
            yield return SendWWW(data, target_view, string.Empty, onResponse);
        }

        public IEnumerator SendWWW(JObject data, string target_view, string target_id, System.Action<WWW> onResponse) {
            WWW www;
            if (string.IsNullOrEmpty(target_id)) {
                target_view = api_url + target_view;
            }
            else {
                target_view = api_url + target_view + WWW.EscapeURL(target_id) + "/";
            }

            if (data == null) {
                Debug.LogFormat("<color=teal>API Call</color> to <color=blue>{0}</color>", target_view);
                www = new WWW(target_view);
            }
            else {
                string jsstring = JsonConvert.SerializeObject(data);
                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Content-Type", "text/json");
                byte[] body = Encoding.UTF8.GetBytes(jsstring);
                Debug.LogFormat("<color=teal>API Call</color> to <color=blue>{1}</color> data: {0}", jsstring, target_view);
                www = new WWW(target_view, body, headers);
            }

            yield return www;

            if (verboseLogs) {
                Debug.LogFormat("<color=teal>HTTP Repsonse Headers:</color>{0}", JsonConvert.SerializeObject(www.responseHeaders));
            }

            if (!www.responseHeaders.ContainsKey("STATUS")) {
                Debug.LogError("<color=red>HTTP No server Status, server is probably dead?</color>");
                yield break;
            }

            if (!string.IsNullOrEmpty(www.error)) {
                Debug.LogErrorFormat("<color=red>HTTP Error:</color>{0}\nBody:{1}", www.error,www.text);
            }
            else {
                Debug.LogFormat("<color=green>HTTP Response:</color>{0}", www.text);
            }

            onResponse(www);
            yield break;
        }

        

#endregion ========================================================================================
    }

}
#endif