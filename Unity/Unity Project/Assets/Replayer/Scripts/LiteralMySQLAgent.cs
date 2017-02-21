
#if REPLAY_ENGINE

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;
using System;
using System.Collections;

namespace RAE {

    public class LiteralMySQLAgent : Agent {
        private MySqlConnection conn;
        private MySqlDataReader reader;


        void Start() {

        }

        private string sqlErrorString = null;

        void OnGUI() {
            if (!string.IsNullOrEmpty(sqlErrorString)) {
                GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");
                GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));
                GUILayout.BeginVertical();

                GUILayout.Label("There was an SQL error:\n" + sqlErrorString);
                if (ReplayGUI.Button("OK")) {
                    sqlErrorString = null;
                }

                GUILayout.EndVertical();
                GUILayout.EndArea();

            }
        }

        void OnDestroy() {
            if (reader != null) {
                reader.Close();
            }
            if (conn != null) {
                conn.Close();
            }
        }



#region ==============================|       Agent API       |==============================

        private AgentAction curr = AgentAction.NullAction;
        private AgentAction prev = AgentAction.NullAction;
        private AgentAction next = AgentAction.NullAction;
        private ActionRequestStatus state = ActionRequestStatus.Idle;
        private bool loaded = false;
        private int count = 0;

        public override IEnumerator Initialize() {    
            ConnectionString = System.String.Format("Server={0};Port={1};Database={2};Uid={3};password={4}",
                                                server,
                                                portNum,
                                                database,
                                                userID,
                                                password);
                conn = new MySqlConnection(ConnectionString);

                MySqlCommand command = conn.CreateCommand();

                command.CommandText = CommandString;
                command.CommandTimeout = 999999;

                try {
                    conn.Open();
                }
                catch (System.Exception ex) {
                    Debug.LogException(ex);
                    ReplayGUI.ErrorMessage(string.Format("{0}\n{1}", ex.Message, ex.StackTrace));
                }

                reader = command.ExecuteReader();
                loaded = true;
                RequestNextAction();
            yield break;
        }

        public override AgentAction CurrentAction {
            get { return curr; }
        }

        public override AgentAction Next {
            get { return next; }
        }

        public override AgentAction Previous {
            get { return prev; }
        }

        public override bool Initialized {
            get { return loaded; }
        }

        public override bool HasNext {
            get { return next != AgentAction.NullAction; }
        }

        public override bool HasNextInSession {
            get { return next != AgentAction.NullAction; }
        }

        public override ActionRequestStatus ActionStatus {
            get { return this.state; }
        }

        public override IEnumerator RequestNextAction() {
            state = ActionRequestStatus.Requesting;
            prev = curr;
            curr = next;
            count++;
            try {
                if (reader.Read()) {
                    //Debug.Log("Selection: " + reader["selection"]);
                    next = new AgentAction(
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
                        reader["attemptID"].ToString()
                        );
                }
                else {
                    next = AgentAction.NullAction;
                    state = ActionRequestStatus.NoAction;
                    yield break;
                }
            }
            catch (System.Exception e) {
                Debug.LogError(string.Format("Error on line {0}: {1}", count, reader.ToString()));
                Debug.LogException(e);
                next = AgentAction.NullAction;
                state = ActionRequestStatus.NoAction;
                yield break;
            }

            //Debug.Log("GetNextAction: " + curr);
            state = ActionRequestStatus.ActionFound;
            yield break;
        }

        private string GetField(string field) {
            string ret = null;
            try {
                ret = reader[field].ToString();
            }
            catch (System.Exception ex) {
                Debug.LogException(ex);
                ret = null;
            }
            return ret;
        }

        private Vector2 scrollPos = Vector2.zero;
        public override void OptionsPane() {
            // GUILayout.BeginArea(optionArea);
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            GUILayout.BeginVertical();

            //   float bWidth = ReplayGUI.standardButtonWidth;
            //  float bHeight = ReplayGUI.standardButtonHeight;

            connOptionsOpen = ReplayGUI.ExpandButton(connOptionsOpen, "Connection Settings");
            if (connOptionsOpen) {
                ConnectionGUI();
            }

            ReplayGUI.Label("Command Settings");
            CommandGUI();

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            //  GUILayout.EndArea();
        }

        public override string GUIName {
            get { return "Database"; }
        }

        public override string RunReportName {
            get {
                return "MySQL LOG READER";
            }
        }

        public override string RunReport() {
            return string.Format("Connection:{0}\nCommand:{1}", ConnectionString, CommandString);
        }

#endregion ============================|------------------------------|==============================

#region  ==============================|       Command Settings       |==============================

        public string CommandString {
            get {
                return System.String.Format("SELECT {0} FROM {1} WHERE {2} {3};", selectClause, fromClause, whereClause, otherClauses);
            }
        }


        // List<string[]> args = new List<string[]>();

        private bool directEditCommString = true;
        private bool lockSelect = true;
        private const string DEFAULT_SELECT = "selection, action, input, state, eventTime, userID, levelName, attemptNumber, sessionID, transactionID, attemptID";
        private string selectClause = DEFAULT_SELECT;
        public string fromClause = "";
        [TextArea(3, 7)]
        public string whereClause = "";
        [TextArea(3, 7)]
        public string otherClauses = string.Empty;

        enum Field {
            UserID = 0,
            SessionID = 1,
            TransactionID = 2,
            School = 3,
            Age = 4,
            Attempt = 5,
            Gender = 6,
            Time = 7,
            Selection = 8,
            Action = 9,
            Input = 10,
            Custom = 11
        }

        void CommandGUI() {

            directEditCommString = ReplayGUI.ToggleField(directEditCommString, "Direct Edit Command String", 2, 1);
            if (directEditCommString) {

                bool bak = GUI.enabled;
                GUI.enabled &= !lockSelect;
                selectClause = ReplayGUI.BigTextField(selectClause, "SELECT", 4, 1);
                GUI.enabled = bak;
                lockSelect = ReplayGUI.ToggleField(lockSelect, lockSelect ? "unlock" : "lock", 1, 2);
                fromClause = ReplayGUI.BigTextField(fromClause, "FROM", 4, 1);
                whereClause = ReplayGUI.BigTextField(whereClause, "WHERE", 4, 1);
                otherClauses = ReplayGUI.BigTextField(otherClauses, "misc", 4, 1);
            }
            else {
                ReplayGUI.Label("I haven't setup indirect command editing yet", 3, 1);
            }

        }

#endregion ============================|--------------------------------|==============================

#region  ==============================|       Connection Settings       |==============================

        public string ConnectionString {
            get {
                return connString;
            }
            set {
                connString = value;
            }
        }

        private string connString = "Server=127.0.0.1;Port=3306;Database=rae;Uid=root;password=password";

        private string server = "127.0.0.1";
        private string portNum = "3306";
        private string database = "rae";
        private string userID = "root";
        public string password = "password";



        private bool connOptionsOpen = false;
        private bool directEditConnString = false;
        private bool showPassword = false;

        private void ParseConnString(string conn) {
            server = string.Empty;
            portNum = string.Empty;
            database = string.Empty;
            userID = string.Empty;
            password = string.Empty;

            foreach (string ent in conn.Split(';')) {
                string[] sp = ent.Split('=');

                switch (sp[0].ToLower()) {
                    case "server":
                        server = sp[1];
                        break;
                    case "port":
                        portNum = sp[1];
                        break;
                    case "database":
                        database = sp[1];
                        break;
                    case "uid":
                        userID = sp[1];
                        break;
                    case "password":
                        password = sp[1];
                        break;
                    default:
                        Debug.Log("Parsing connection string don't understand field: " + sp[0]);
                        break;
                }
            }
        }


        private void ConnectionGUI() {
            bool before = directEditConnString;
            directEditConnString = ReplayGUI.ToggleField(directEditConnString, "Direct Edit Connection String", 2, 1);

            if (before && !directEditConnString) {
                ParseConnString(ConnectionString);
            }

            if (directEditConnString) {
                ConnectionString = ReplayGUI.BigTextField(ConnectionString, "Connection", 4, 1);
            }
            else {
                server = ReplayGUI.TextField(server, "Server Name", 2, 1);
                portNum = ReplayGUI.TextField(portNum, "Port Number", 2, 1);
                database = ReplayGUI.TextField(database, "Database Name", 2, 1);
                userID = ReplayGUI.TextField(userID, "User ID", 2, 1);

                //The password for the DB
                if (!showPassword)
                    password = ReplayGUI.PasswordField(password, "Password", 2, 1);
                else
                    password = ReplayGUI.TextField(password, "Password", 2, 1);

                showPassword = ReplayGUI.ToggleField(showPassword, showPassword ? "hide password" : "show password", 1, 4);

                ConnectionString = System.String.Format("Server={0};Port={1};Database={2};Uid={3};password={4}",
                                            server,
                                            portNum,
                                            database,
                                            userID,
                                            password);
            }

        }

#endregion ============================|------------------------------|==============================
    }
}
#endif