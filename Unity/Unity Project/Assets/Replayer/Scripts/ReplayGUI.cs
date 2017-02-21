#if REPLAY_ENGINE

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System;
using System.IO;

namespace RAE {

    public class ReplayGUI : MonoBehaviour {

        #region Flags

        bool menuUp = false;
        public bool ShowingMenu {
            get {
                return menuUp;
            }
            set {
                menuUp = value;
            }
        }

        [SerializeField]
        bool debugGUI = false;
        public bool DisplayDebugGUI {
            get {
                return debugGUI;
            }
            set {
                debugGUI = value;
            }
        }
        
        // This time stamp is used to prevent multiple input events coliding 
        // when brining the menu up
        private float lastTime = 0; 

#endregion

#region Size and Layout Properties

        public static float standardButtonWidth = 100;
        public static float standardLabelWidth = 150;
        public static float standardButtonHeight = 25;
        public static float standardTabSize = 50;
        public KeyCombo displayCombo = new KeyCombo(KeyCode.LeftControl, KeyCode.R, KeyCode.A, KeyCode.E);
        public KeyCombo playPauseCombo = new KeyCombo(KeyCode.LeftControl, KeyCode.P);
        public KeyCombo initCombo = new KeyCombo(KeyCode.BackQuote, KeyCode.I);

        private float originX;
        private float gameWidth = float.NaN;
        private float gameHeight = float.NaN;
        public float windowWidth = float.NaN;
        public float windowHeight = float.NaN;


        private Rect mainWindowRect;
        private Rect debugGUIRect;

#endregion

#region FieldValues

        private string[] optionNames;
        private int currentOption = 0;
        private Vector2 scrollPosition = Vector2.zero;

#endregion

#region Component Pointers

        ReplayAnalysisEngine rae;
        //AnalysisWriter writer;
        List<RAEComponent> raeComponents;
        Agent reader;

        public static ReplayGUI mainGUI = null;

#endregion

#region Unity Methods

        void Awake() {
            if (mainGUI == null) {
                mainGUI = this;
            }
            else {
                Debug.LogError("More than 1 ReplayGUI!");
                Destroy(this);
            }
        }

        void Start() {
            raeComponents = new List<RAEComponent>();
            raeComponents.Add(null);
            raeComponents.AddRange(GetComponents<RAEComponent>().OrderBy(comp => comp.GUIOrder));
            rae = GetComponent<ReplayAnalysisEngine>();
            //writer = GetComponent<AnalysisWriter>();
            optionNames = (from comp in raeComponents select comp == null ? "Replay" : comp.GUIName).ToArray<string>();
            reader = GetComponent<Agent>();

            gameWidth = float.IsNaN(gameWidth) ? Screen.height * Camera.main.aspect : gameWidth;
            originX = (Screen.width - gameWidth) / 2f;
            gameHeight = float.IsNaN(gameHeight) ? Screen.height : gameHeight;

            if (float.IsNaN(windowWidth) || float.IsNaN(windowHeight)) {
                windowWidth = gameWidth * 3 / 4;
                windowHeight = gameWidth * 3 / 4;
            }
            mainWindowRect = new Rect(gameWidth / 8, gameHeight / 8, windowWidth, windowHeight);
            debugGUIRect = new Rect(0,0, windowWidth, gameHeight);
        }


        void Update() {
            if (Time.time > lastTime + 1) {
                if (displayCombo.GetKeyCombo()) {
                    lastTime = Time.time;
                    menuUp = !menuUp;
                }
                if (playPauseCombo.GetKeyCombo()) {
                    lastTime = Time.time;
                    if (rae.Running)
                        rae.Pause();
                    else
                        rae.Run();
                }
                if (initCombo.GetKeyCombo()) {
                    lastTime = Time.time;
                    if (!rae.Initialized) {
                        rae.Initialize();
                    }
                }
            }
            if (windowWidth != mainWindowRect.width || windowHeight != mainWindowRect.height) {
                mainWindowRect = new Rect(mainWindowRect.x, mainWindowRect.y, windowWidth, windowHeight);
                debugGUIRect = new Rect(0, 0, windowWidth, gameHeight);
            }
        }

        void OnGUI() {
            bakSkin = GUI.skin;
            GUI.skin = null;
            if (!rae.TakingScreenShot) {
                if (menuUp) {
                    mainWindowRect = GUILayout.Window(0, mainWindowRect, MainGUI, "Replay Analysis Engine");
                }
                else if (rae.Initialized && debugGUI) {
                    DebugGUI();
                }
            }
            else if (rae.TakingScreenShot) {
                InfoBox();
            }
            GUI.skin = bakSkin;
        }

#endregion

#region Core GUI Methods

        private GUISkin bakSkin;
        private bool bakEnabled;

        /// <summary>
        /// Draws the additional options GUI.
        /// </summary>
        void MainGUI(int windowID) {
            //draw the toolbar
            GUILayout.BeginVertical();

            //put the running buttons here
            GUILayout.BeginHorizontal();
            if (ReplayGUI.Button(rae.Initialized ? "Next Action" : "Initialize", rae.Initialized ? "Step to the next action in the logs" : "Initialize all of the RAE's components. This much be done before running.")) {
                if (!rae.Initialized) {
                    rae.Initialize();
                }
                else {
                    rae.StepNextAction();
                }
            }

            bakEnabled = GUI.enabled;
            GUI.enabled = rae.Initialized;
            if (ReplayGUI.Button(rae.Running ? "Pause" : "Run", rae.Running ? "Pause the run after the next action is complete." : "Begin running the simulation where here.")) {
                if (rae.Running)
                    rae.Pause();
                else
                    rae.Run();
            }

            GUI.enabled = bakEnabled;

            if (ReplayGUI.Button("Close", "Close the RAE Options. You can bring it back up with:" + displayCombo.InputString)) {
                this.menuUp = false;
            }

            GUILayout.Space(standardButtonWidth / 8);
            GUILayout.Label(string.Format("Current Scene: {0}", SceneManager.GetActiveScene().name));

            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Options");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            currentOption = GUILayout.Toolbar(currentOption, this.optionNames, GUILayout.Width(standardButtonWidth * optionNames.Length), GUILayout.Height(standardButtonHeight));

            if (currentOption == 0) {
                StandardOptions();
            }
            else {
                scrollPosition = GUILayout.BeginScrollView(scrollPosition);
                GUILayout.BeginVertical();
                raeComponents[currentOption].OptionsPane();
                GUILayout.EndVertical();
                GUILayout.EndScrollView();
            }

            GUILayout.Label(GUI.tooltip);

            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        void DebugGUI() {
            GUILayout.BeginArea(debugGUIRect);
            GUILayout.BeginVertical();
            foreach(RAEComponent comp in this.raeComponents) {
                if (comp != null) {
                    //GUILayout.Label("======");
                    //GUILayout.Label(comp.GUIName);
                    comp.DebugGUI();
                    //GUILayout.Label("======");
                }
            }
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private Regex floatRegEx = new Regex(@"\d*\.?\d*?");
        private Regex intRegEx = new Regex(@"\d*");
        private float FormatFloat(string s) {
            return float.Parse(floatRegEx.IsMatch(s) ? s : "0");
        }

        private int FormatInt(string s) {
            if (string.IsNullOrEmpty(s))
                return 0;
            return int.Parse(intRegEx.IsMatch(s) ? s : "0");
        }

        void StandardOptions() {
            //   GUILayout.BeginArea(mainOptionsRect);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            GUILayout.BeginVertical();

            Application.targetFrameRate = ReplayGUI.IntField(Application.targetFrameRate, "Target Frame Rate", "Set the Application.targetFrameRate, may help improve parity between the replay result and the original recorded data. A value of -1 runs at full speed.");
            Application.runInBackground = ReplayGUI.ToggleField(Application.runInBackground, "Run Unity in Background", "If checked Unity will continue to run if the editor loses focus.", 2);
            rae.ExitOnDone = ToggleField(rae.ExitOnDone, "Exit play mode when done", "The RAE will automatically exit playmode when the log reader has run out of logs.", 2);
            rae.stopCondition = EnumField<ReplayAnalysisEngine.StoppingCondition>(rae.stopCondition, "Stopping Mode", "Controls the conditions that RAE will use to decide when to stop action simulation before running the interpreter.");

            switch (rae.stopCondition) {
                case ReplayAnalysisEngine.StoppingCondition.Instant:
                    break;

                case ReplayAnalysisEngine.StoppingCondition.WaitForStop:
                case ReplayAnalysisEngine.StoppingCondition.TimeOut:
                case ReplayAnalysisEngine.StoppingCondition.Simulate:
                case ReplayAnalysisEngine.StoppingCondition.Custom:
                    rae.TimeAcceleration = ReplayGUI.FloatField(rae.TimeAcceleration, "Time Acceleration", "The multiplier applied to Unity's Time.timescale when running an action.", 1);
                    rae.TimeOut = ReplayGUI.FloatField(rae.TimeOut, "Time Out", "The number of scaled seconds to wait before forcing the RAE to advance to the next action.", 1);
                    break;
            }
            //rae.ReplayMode = ReplayGUI.EnumField<ReplayAnalysisEngine.IterationMode>(rae.ReplayMode, "Iteration Mode:", "Whether to iterate action-by-action or only the final action in any attempt.");
            rae.PauseAfter = ReplayGUI.IntField((int)rae.PauseAfter, "Pause After N Actions", "The RAE will pause itself after every N actions. A value of <= 0 will never pause.");
            rae.runReportPath = ReplayGUI.TextField(rae.runReportPath, "Report Directory", "The path where the run report of this replay run will be saved.");

            rae.ScreenShotTiming = EnumField<ReplayAnalysisEngine.ScreenShotTimingOption>(rae.ScreenShotTiming, "Screenshot Setting:", "Change if and when the RAE should take screenshots while processing.", .8f, 0);
            switch(rae.ScreenShotTiming) {
                case ReplayAnalysisEngine.ScreenShotTimingOption.Disabled:
                    break;
                default:
                    rae.ScreenShotMode = ReplayGUI.EnumField<ReplayAnalysisEngine.ScreenShotModeOption>(rae.ScreenShotMode, "Screenshot Mode:", "Which screenshot mode you want to use, JPG will not include a watermark.",1,1);
                    rae.screenshotDirectory = ReplayGUI.TextField(rae.screenshotDirectory, "Screenshot Directory", 4, 1);
                    //if (this.writer is TabDelimitedTextFileWriter) {
                    //    switch (ReplayGUI.YesNo("Would you like to copy the log path from the AnalysisWriter?",.5f,1)) {
                    //        case YesNoResponse.Yes:
                    //            rae.screenshotDirectory = (this.writer as TabDelimitedTextFileWriter).logPath;
                    //            this.writer = null;
                    //            break;
                    //        case YesNoResponse.No:
                    //            this.writer = null;
                    //            break;
                    //        default:
                    //            break;
                    //    }
                    //}
                    if (!string.IsNullOrEmpty(rae.screenshotDirectory) && !Directory.Exists(rae.screenshotDirectory)) {
                        switch (ReplayGUI.YesNo("The screenshot path does not exist would you like to create it?",.5f,1)) {
                            case YesNoResponse.Yes:
                                Directory.CreateDirectory(rae.screenshotDirectory);
                                break;
                            case YesNoResponse.No:
                                rae.screenshotDirectory = string.Empty;
                                break;
                            default:
                                break;
                        }
                    }
                    break;
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            // GUILayout.EndArea();

        }



        #endregion

        #region Convenience Functions

        public static string FixPastedPath(string path) {
            return path.Trim('"');
        }

        //using static holder fields to avoid continuous memory allocation
        private static string __input_back = string.Empty;
        private static string __input_test = string.Empty;
        private static bool __input_bool = false;
        private static float __input_float = float.NaN;
        private static int __input_int = int.MinValue;

        private static Dictionary<string, GUIContent> __label_cache = new Dictionary<string, GUIContent>();

        private static GUIContent MakeOrRetrieveLabel(string key, string tooltip) {
            if (!__label_cache.ContainsKey(key)) {
                __label_cache[key] = new GUIContent(key, tooltip);
                return __label_cache[key];
            }
            return __label_cache[key];
        }

        private static void AddTabs(int tabs) {
            for (int i = 0; i < tabs; i++) {
                GUILayout.Space(standardTabSize);
            }
        }

        public static float FloatField(float value, string label) {
            return FloatField(value, MakeOrRetrieveLabel(label, string.Empty), 0);
        }

        public static float FloatField(float value, string label, string tooltip) {
            return FloatField(value, MakeOrRetrieveLabel(label, tooltip), 0);
        }

        public static float FloatField(float value, string label, int tabLevel) {
            return FloatField(value, MakeOrRetrieveLabel(label, string.Empty), tabLevel);
        }

        public static float FloatField(float value, string label, string tooltip, int tabLevel) {
            return FloatField(value, MakeOrRetrieveLabel(label, tooltip), tabLevel);
        }

        public static float FloatField(float value, GUIContent label, int tabLevel) {
            __input_back = value.ToString();
            __input_test = string.Empty;
            __input_float = float.NaN;

            GUILayout.BeginHorizontal();
            AddTabs(tabLevel);
            GUILayout.Label(label, GUILayout.Width(standardLabelWidth), GUILayout.Height(standardButtonHeight));
            __input_test = GUILayout.TextField(__input_back, GUILayout.Width(standardButtonWidth), GUILayout.Height(standardButtonHeight));
            GUILayout.EndHorizontal();

            if (__input_test != __input_back) {
                if (float.TryParse(__input_test, out __input_float)) {
                    return __input_float;
                }
            }
            return value;
        }

        public static int IntField(int value, string label) {
            return IntField(value, MakeOrRetrieveLabel(label, string.Empty), 0);
        }

        public static int IntField(int value, string label, string tooltip) {
            return IntField(value, MakeOrRetrieveLabel(label, tooltip), 0);
        }

        public static int IntField(int value, string label, int tabLevel) {
            return IntField(value, MakeOrRetrieveLabel(label, string.Empty), tabLevel);
        }

        public static int IntField(int value, string label, string tooltip, int tabLevel) {
            return IntField(value, MakeOrRetrieveLabel(label, tooltip), tabLevel);
        }

        public static int IntField(int value, GUIContent label, int tabLevel) {
            __input_back = value.ToString();
            __input_test = string.Empty;
            __input_int = 0;

            GUILayout.BeginHorizontal();
            AddTabs(tabLevel);
            GUILayout.Label(label, GUILayout.Width(standardLabelWidth), GUILayout.Height(standardButtonHeight));
            __input_test = GUILayout.TextField(__input_back, GUILayout.Width(standardButtonWidth), GUILayout.Height(standardButtonHeight));
            GUILayout.EndHorizontal();

            if (__input_test != __input_back) {
                if (int.TryParse(__input_test, out __input_int)) {
                    return __input_int;
                }
            }
            return value;
        }

        public static bool ToggleField(bool value, string label) {
            return ToggleField(value, MakeOrRetrieveLabel(label, string.Empty), 1, 0);
        }

        public static bool ToggleField(bool value, string label, float scale) {
            return ToggleField(value, MakeOrRetrieveLabel(label, string.Empty), scale, 0);
        }

        public static bool ToggleField(bool value, string label, string tooltip) {
            return ToggleField(value, MakeOrRetrieveLabel(label, tooltip), 1, 0);
        }

        public static bool ToggleField(bool value, string label, string tooltip, float scale) {
            return ToggleField(value, MakeOrRetrieveLabel(label, tooltip), scale, 0);
        }

        public static bool ToggleField(bool value, string label, float scale, int tabLevel) {
            return ToggleField(value, MakeOrRetrieveLabel(label, string.Empty), scale, tabLevel);
        }

        public static bool ToggleField(bool value, string label, string tooltip, float scale, int tabLevel) {
            return ToggleField(value, MakeOrRetrieveLabel(label, tooltip), scale, tabLevel);
        }

        public static bool ToggleField(bool value, GUIContent label, float scale, int tabLevel) {
            GUILayout.BeginHorizontal();
            AddTabs(tabLevel);
            value = GUILayout.Toggle(value, label, GUILayout.Width(standardLabelWidth * scale), GUILayout.Height(standardButtonHeight));
            GUILayout.EndHorizontal();
            return value;
        }

        public static bool Button(string label) {
            return Button(MakeOrRetrieveLabel(label, string.Empty), 0);
        }

        public static bool Button(string label, string tooltip) {
            return Button(MakeOrRetrieveLabel(label, tooltip), 0);
        }

        public static bool Button(string label, int tabLevel) {
            return Button(MakeOrRetrieveLabel(label, string.Empty), tabLevel);
        }

        public static bool Button(string label, string tooltip, int tabLevel) {
            return Button(MakeOrRetrieveLabel(label, tooltip), tabLevel);
        }

        public static bool Button(GUIContent label, int tabLevel) {
            GUILayout.BeginHorizontal();
            AddTabs(tabLevel);
            __input_bool = GUILayout.Button(label, GUILayout.Width(standardButtonWidth), GUILayout.Height(standardButtonHeight));
            GUILayout.EndHorizontal();
            return __input_bool;
        }

        public static string TextField(string content, string label) {
            return TextField(content, MakeOrRetrieveLabel(label, string.Empty), 4, 0);
        }

        public static string TextField(string content, string label, string tooltip) {
            return TextField(content, MakeOrRetrieveLabel(label, tooltip), 4, 0);
        }

        public static string TextField(string content, string label, float scaleWidth) {
            return TextField(content, label, scaleWidth, 0);
        }

        public static string TextField(string content, string label, float scaleWidth, int tabLevel) {
            return TextField(content, MakeOrRetrieveLabel(label, string.Empty), scaleWidth, tabLevel);
        }

        public static string TextField(string content, string label, string tooltip, float scaleWidth, int tabLevel) {
            return TextField(content, MakeOrRetrieveLabel(label, tooltip), scaleWidth, tabLevel);
        }

        public static string TextField(string content, GUIContent label, float scaleWidth, int tabLevel) {
            GUILayout.BeginHorizontal();
            AddTabs(tabLevel);
            GUILayout.Label(label, GUILayout.Width(standardLabelWidth), GUILayout.Height(standardButtonHeight));
            content = GUILayout.TextField(content, GUILayout.Width(standardButtonWidth * scaleWidth), GUILayout.Height(standardButtonHeight));
            GUILayout.EndHorizontal();
            return content;
        }

        public static string BigTextField(string content, string label) {
            return BigTextField(content, label, 4, 0);
        }

        public static string BigTextField(string content, string label, float minScale) {
            return BigTextField(content, label, minScale, 0);
        }

        public static string BigTextField(string content, string label, float minScale, int tabLevel) {
            return BigTextField(content, MakeOrRetrieveLabel(label, string.Empty), minScale, tabLevel);
        }

        public static string BigTextField(string content, string label, string tooltip, float minScale, int tabLevel) {
            return BigTextField(content, MakeOrRetrieveLabel(label, tooltip), minScale, tabLevel);
        }

        public static string BigTextField(string content, GUIContent label, float minScale, int tabLevel) {
            GUILayout.BeginHorizontal();
            AddTabs(tabLevel);
            GUILayout.Label(label, GUILayout.Width(standardLabelWidth / 2), GUILayout.Height(standardButtonHeight));
            content = GUILayout.TextField(content, GUILayout.MinWidth(standardButtonWidth * minScale), GUILayout.Height(standardButtonHeight));
            GUILayout.EndHorizontal();
            return content;
        }

        public static string PasswordField(string content, string label) {
            return PasswordField(content, label, 4, 0);
        }

        public static string PasswordField(string content, string label, float scaleWidth) {
            return PasswordField(content, label, scaleWidth, 0);
        }

        public static string PasswordField(string content, string label, float scaleWidth, int tabLevel) {
            return PasswordField(content, MakeOrRetrieveLabel(label, string.Empty), scaleWidth, tabLevel);
        }

        public static string PasswordField(string content, string label, string tooltip, float scaleWidth, int tabLevel) {
            return PasswordField(content, MakeOrRetrieveLabel(label, tooltip), scaleWidth, tabLevel);
        }

        public static string PasswordField(string content, GUIContent label, float scaleWidth, int tabLevel) {
            GUILayout.BeginHorizontal();
            AddTabs(tabLevel);
            GUILayout.Label(label, GUILayout.Width(standardLabelWidth), GUILayout.Height(standardButtonHeight));
            content = GUILayout.PasswordField(content, '*', GUILayout.Width(standardButtonWidth * scaleWidth), GUILayout.Height(standardButtonHeight));
            GUILayout.EndHorizontal();
            return content;
        }

        public static bool ExpandButton(bool expanded, string label) {
            return ExpandButton(expanded, MakeOrRetrieveLabel(label, string.Empty));
        }

        public static bool ExpandButton(bool expanded, string label, string tooltip) {
            return ExpandButton(expanded, MakeOrRetrieveLabel(label, tooltip));
        }

        public static bool ExpandButton(bool expanded, GUIContent label) {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(standardLabelWidth), GUILayout.Height(standardButtonHeight));
            if (GUILayout.Button(expanded ? "-" : "+", GUILayout.Width(standardButtonHeight), GUILayout.Height(standardButtonHeight))) {
                expanded = !expanded;
            }
            GUILayout.EndHorizontal();
            return expanded;
        }

        public static void Label(string content) {
            Label(content, 4, 0);
        }

        public static void Label(string content, float scale) {
            Label(content, scale, 0);
        }

        public static void Label(string content, float scale, int tabLevel) {
            GUILayout.BeginHorizontal();
            AddTabs(tabLevel);
            GUILayout.Label(content, GUILayout.Width(standardButtonWidth * scale), GUILayout.Height(standardButtonHeight));
            GUILayout.EndHorizontal();
        }

        public enum YesNoResponse {
            Yes,
            No,
            Pending
        }

        public static YesNoResponse YesNo(string prompt) {
            return YesNo(prompt, 1.0f, 0);
        }

        public static YesNoResponse YesNo(string prompt, float scale) {
            return YesNo(prompt, scale, 0);
        }

        public static YesNoResponse YesNo(string prompt, float scale, int tabLevel) {
            YesNoResponse response = YesNoResponse.Pending;
            GUILayout.BeginHorizontal();
            AddTabs(tabLevel);
            GUILayout.BeginVertical();
            GUILayout.Label(prompt, GUILayout.Height(standardButtonHeight));
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if(GUILayout.Button("Yes", GUILayout.Width(standardButtonWidth * scale), GUILayout.Height(standardButtonHeight))){
                response = YesNoResponse.Yes;
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("No", GUILayout.Width(standardButtonWidth * scale), GUILayout.Height(standardButtonHeight))){
                response = YesNoResponse.No;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();            
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            return response;
        }

        public static T EnumField<T>(T current, string label) where T : struct, IConvertible {
            return EnumField<T>(current, MakeOrRetrieveLabel(label, string.Empty), 1, 0);
        }

        public static T EnumField<T>(T current, string label, float scale) where T : struct, IConvertible {
            return EnumField<T>(current, MakeOrRetrieveLabel(label, string.Empty), scale, 0);
        }

        public static T EnumField<T>(T current, string label, string tooltip) where T : struct, IConvertible {
            return EnumField<T>(current, MakeOrRetrieveLabel(label, tooltip), 1, 0);
        }

        public static T EnumField<T>(T current, string label, string tooltip, float scale) where T : struct, IConvertible {
            return EnumField<T>(current, MakeOrRetrieveLabel(label, tooltip), scale, 0);
        }

        public static T EnumField<T>(T current, string label, string tooltip, float scale, int tabLevel) where T : struct, IConvertible {
            return EnumField<T>(current, MakeOrRetrieveLabel(label, tooltip), scale, tabLevel);
        }

        public static T EnumField<T>(T current, GUIContent label, float scale, int tabLevel) where T : struct, IConvertible {
            string[] names = Enum.GetNames(typeof(T));
            for (__input_int = 0; __input_int < names.Length; __input_int++) {
                if (current.ToString() == names[__input_int]) {
                    break;
                }
            }
            GUILayout.BeginHorizontal();
            AddTabs(tabLevel);
            if (!string.IsNullOrEmpty(label.text)) {
                GUILayout.Label(label, GUILayout.Width(standardLabelWidth), GUILayout.Height(standardButtonHeight));
            }
            __input_int = GUILayout.Toolbar(__input_int, names, GUILayout.Width(standardButtonWidth * scale * names.Length), GUILayout.Height(standardButtonHeight));
            GUILayout.EndHorizontal();

            return (T)Enum.Parse(typeof(T), names[__input_int]);
        }

        public static bool ErrorMessage(string content) {
            __input_bool = false;
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");
            GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("There was an error:\n" + content);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("OK", GUILayout.Width(standardButtonWidth), GUILayout.Height(standardButtonHeight))) {
                __input_bool = true;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            GUILayout.EndArea();
            return __input_bool;
        }

        public void InfoBox() {
            GUILayout.BeginArea(new Rect(originX, 0, Screen.width, standardButtonHeight));
            GUILayout.Box(string.Format("User:{0}, Level:{1}, Attempt:{2}, Transaction ID:{3}",
                reader.CurrentAction.User,
                reader.CurrentAction.LevelName,
                reader.CurrentAction.Attempt,
                reader.CurrentAction.TransactionID));
            GUILayout.EndArea();
        }
#endregion

    }

}
#endif
