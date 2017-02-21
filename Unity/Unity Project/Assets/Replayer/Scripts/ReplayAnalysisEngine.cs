#if REPLAY_ENGINE 
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System;
using Newtonsoft.Json.Linq;

namespace RAE {

    [RequireComponent(typeof(ReplayGUI))]
    [DisallowMultipleComponent]
    public class ReplayAnalysisEngine : MonoBehaviour {

        #region Flags
        private bool __flag_ret = true;
            
        private bool done = true;
        /// <summary>
        /// True if the RAE is done processing the last action and is ready to 
        /// begin the next one. This is only contingent on the interals of RAE 
        /// itself. It is set to false when an action is started and set to 
        /// true at the end of the process.
        /// </summary>
        public bool Done {
            get {
                return this.done;
            }
        }

        private bool initialized = false;
        /// <summary>
        /// True if the RAE has completed first time intialization. This is 
        /// contingent on the intialization of all other replay related 
        /// resources and can be taken as a flag to represent the entire 
        /// system's status.
        /// </summary>
        public bool Initialized {
            get {
                __flag_ret = initialized && agent.Initialized && extender.Initialized && interpreter.Initialized;
                foreach (RAEComponent comp in extraRAEComponents) {
                    __flag_ret &= comp.Initialized;
                }
                return __flag_ret;
            }
        }

        private bool ready = false;
        /// <summary>
        /// True if the RAE is ready to continue with replay. The primary use 
        /// case for this flag is for controlling replay while transitioning 
        /// between scenes. This is contingent on the readiness of all other 
        /// replay related resources and can be taken as a flag to represent 
        /// the entire system's status.
        /// 
        /// As opposed to Done this flag is a way for any RAEComponent to 
        /// essentially request more time before moving on to the next action
        /// or next substep of an action process.
        /// </summary>
        public bool Ready {
            get {
                __flag_ret = ready && agent.Ready && extender.Ready && interpreter.Ready;
                foreach (RAEComponent comp in extraRAEComponents) {
                    __flag_ret &= comp.Ready;
                }
                return __flag_ret;
            }
        }

        private bool running;
        /// <summary>
        /// True if the RAE is currently in running mode.
        /// This is like the top level kill switch, the other flags are used to
        /// advance on in the running process this one will kill it at the end 
        /// of the current action.
        /// </summary>
        public bool Running {
            get {
                return running;
            }
        }

        public bool heavyDebug = false;

        #endregion

        #region Screenshot Settings

        public enum ScreenShotTimingOption {
            Disabled,
            OnCreate,
            OnStop,
            OnWrite
        }

        public enum ScreenShotModeOption {
            PNG,
            JPG
        }

        [SerializeField]
        public ScreenShotTimingOption screenshotTiming = ScreenShotTimingOption.Disabled;

        [SerializeField]
        public ScreenShotModeOption screenShotMode = ScreenShotModeOption.PNG;
        public string screenshotDirectory = string.Empty;

        private bool takingScreenshot = false;

        public bool TakingScreenShot {
            get {
                return takingScreenshot;
            }
        }

        public ScreenShotModeOption ScreenShotMode {
            get {
                return screenShotMode;
            }
            set {
                screenShotMode = value;
            }
        }

        public ScreenShotTimingOption ScreenShotTiming {
            get {
                return screenshotTiming;
            }
            set {
                screenshotTiming = value;
            }
        }

        #endregion

        #region Replay Settings

        //public enum IterationMode {
        //    FinalStates = 1,
        //    ActionByAction = 2,
        //    ProjectiveReplay = 3
        //}

        //[SerializeField]
        //private IterationMode replayMode = IterationMode.ActionByAction;

        //public IterationMode ReplayMode {
        //    get {
        //        return replayMode;
        //    }
        //    set {
        //        if(value == IterationMode.ProjectiveReplay && projectiveAgent == null) {
        //            return;
        //        }
        //        else {
        //            replayMode = value;
        //        }
        //    }
        //}


        private long itersSinceRun = 0;
        [SerializeField]
        private long pauseEvery = -1;
        public long PauseAfter {
            get {
                return pauseEvery;
            }
            set {
                if (value <= 0)
                    pauseEvery = -1;
                else
                    pauseEvery = value;
            }
        }

        [SerializeField]
        private bool exitPlayOnDone = true;
        public bool ExitOnDone {
            get {
                return exitPlayOnDone;
            }
            set {
                exitPlayOnDone = value;
            }
        }

        [SerializeField]
        public string runReportPath = string.Empty;

        private string runReportID = string.Empty;
        public string RunReportID {
            get {
                return runReportID;
            }
        }

        public enum StoppingCondition {
            Instant = 0,
            WaitForStop = 1,
            TimeOut = 2,
            Simulate = 3,
            Custom = 4
        }

        [SerializeField]
        public StoppingCondition stopCondition = StoppingCondition.Instant;

        [SerializeField]
        private float timeAcceleraton = 1.0f;

        public float TimeAcceleration {
            get {
                return timeAcceleraton;
            }
            set {
                if (value > 1f)
                    timeAcceleraton = value;
                else
                    timeAcceleraton = 1f;
            }
        }

        private const float NormalSpeed = 1.0f;
        private const float PauseSpeed = 0.0f;

        [SerializeField]
        private float timeOut = 20.0f;

        public float TimeOut {
            get {
                return timeOut;
            }
            set {
                if (value > 0)
                    timeOut = value;
                else
                    timeOut = float.NaN;
            }
        }

        private AgentAction lastAction = AgentAction.NullAction;

        #endregion

        #region Component Pointers

        private Agent agent;
        //private AnalysisWriter writer;
        private Interpreter interpreter;
        private ReplayExtender extender;
        //private ProjectiveAgent projectiveAgent = null;
        private List<RAEComponent> extraRAEComponents = new List<RAEComponent>();
        public static ReplayAnalysisEngine mainRAE = null;

        public AgentAction CurrentAction {
            get {
                //if(replayMode == IterationMode.ProjectiveReplay) {
                //    return projectiveAgent.CurrentAction;
                //}
                //else {
                    return agent.CurrentAction;
                //}
            }
        }

        #endregion

        #region Unity Methods

        void Awake() {
            if (mainRAE == null) {
                mainRAE = this;
            }
            else {
                Debug.LogError("More than 1 RAE!");
                Destroy(this);
            }
            Logger.Instance.Enabled = false;
            DontDestroyOnLoad(this);
            DontDestroyOnLoad(this.gameObject);
        }

        // Use this for initialization
        void Start() {
            agent = GetComponent<Agent>();
            if (agent == null)
                Debug.LogError("No LogReader attached to the Replay Analysis Engine.");

            interpreter = GetComponent<Interpreter>();
            if (interpreter == null)
                Debug.LogError("No Calculator attached to the Replay Analysis Engine.");

            //writer = GetComponent<AnalysisWriter>();
            //if (writer == null)
            //    Debug.LogError("No LogWriter attached to the Replay Analysis Engine.");

            extender = GetComponent<ReplayExtender>();
            if (extender == null) {
                Debug.LogWarning("No ReplayExtender attached to the Replay Analysis Engine. Adding a DummyExtender.");
                this.gameObject.AddComponent<ReplayExtender>();
            }

            //projectiveAgent = GetComponent<ProjectiveAgent>();
            //if (projectiveAgent != null) {
            //    ReplayMode = IterationMode.ProjectiveReplay;
            //}

            RAEComponent[] raeComps = this.GetComponents<RAEComponent>();

            foreach (RAEComponent comp in raeComps) {
                if (comp != agent && comp != interpreter && comp != extender) {
                    extraRAEComponents.Add(comp);
                }
            }
        }

        void Update() {
            
        }

        void OnLevelWasLoaded(int level) {
            if (!this.Initialized) return;
            Debug.LogFormat("<color=cyan>Loaded Level: {0}</color>", SceneManager.GetActiveScene().name);
            StartCoroutine(WaitLevelLoad());
        }

        #endregion

        #region Replay Logic

        /// <summary>
        /// Initialized the RAE system by performing FirstTimePrep.
        /// This is the function called by clicking Initialize in the GUI.
        /// </summary>
        public void Initialize() {
            Debug.Log("<color=green>Begin Initalization</color>");
            StartCoroutine(InitCoroutine());
        }

        //1 time prep
        IEnumerator InitCoroutine() {
            if (Initialized) yield break;
            runReportID = string.Format("{0:yyyy-dd-MM HH-mm-ss}", DateTime.Now);

            foreach (RAEComponent comp in this.GetComponents<RAEComponent>()) {
                StartCoroutine(comp.Initialize());
            }

            //writer.Open(interpreter.Header);
            //reader.Load();

            bool continueWaiting = true;
            while (continueWaiting) {
                continueWaiting = false;
                foreach (RAEComponent comp in this.GetComponents<RAEComponent>()) {
                    if (!comp.Initialized) {
                        continueWaiting = false;
                    }
                }
                running = false;
                yield return 0;
            }
            GenRunReport();
            Debug.LogFormat("<color=green>Finished Initalization:{0}</color>", runReportID);
            this.initialized = true;
            ready = true;
            yield break;
        }
        
        /// <summary>
        /// Generates a RunReport containing all of the current settings for documentation purposes.
        /// </summary>
        private void GenRunReport() {
            if (string.IsNullOrEmpty(runReportPath)) {
                return;
            }

            string reportName = string.Format("RAE Run - {0}.txt", runReportID);

            if (!Directory.Exists(runReportPath)) {
                Directory.CreateDirectory(runReportPath);
            }

            reportName = Path.Combine(runReportPath, reportName);

            using (TextWriter report = new StreamWriter(reportName)) {
                string delim = "===================================================================";
                report.WriteLine(delim);
                report.WriteLine("REPLAY ANALYSIS ENGINE");
                report.WriteLine(delim);
                report.WriteLine(RunReport());
                report.WriteLine();

                report.WriteLine(delim);
                report.WriteLine(agent.RunReportName);
                report.WriteLine(delim);
                report.WriteLine(agent.RunReport());
                report.WriteLine();

                //report.WriteLine(delim);
                //report.WriteLine(writer.RunReportName);
                //report.WriteLine(delim);
                //report.WriteLine(writer.RunReport());
                //report.WriteLine();

                report.WriteLine(delim);
                report.WriteLine(interpreter.RunReportName);
                report.WriteLine(delim);
                report.WriteLine(interpreter.RunReport());
                report.WriteLine();

                report.WriteLine(delim);
                report.WriteLine(extender.RunReportName);
                report.WriteLine(delim);
                report.WriteLine(extender.RunReport());
                report.WriteLine();

                foreach (RAEComponent comp in extraRAEComponents) {
                    report.WriteLine(delim);
                    report.WriteLine(comp.RunReportName);
                    report.WriteLine(delim);
                    report.WriteLine(comp.RunReport());
                    report.WriteLine();
                }

                report.Flush();
                report.Close();
            }

        }

        private string RunReport() {
            //string ret = string.Format("Iteration Mode:{0}\nStopping Condition:{1}\n", ReplayMode, stopCondition);
            string ret = string.Format("Stopping Condition:{0}\n", stopCondition);
            if (stopCondition != StoppingCondition.Instant) {
                ret += string.Format("\tTime Acceleration:{0}\n\tTime Out:{1}\n", timeAcceleraton, timeOut);
            }
            ret += string.Format("Pause After:{0}\nScreenshot Timing:{1}\n", pauseEvery, this.screenshotTiming);
            if (this.screenshotTiming != ScreenShotTimingOption.Disabled) {
                ret += string.Format("\tScreenShot Mode:{0}\n\tScreenShot Directory:{1}", this.screenShotMode, this.screenshotDirectory);
            }
            return ret;
        }

        /// <summary>
        /// Begins the replay process.
        /// This is the function called by Run in the GUI.
        /// This is the pre-coroutine start function like Initialize.
        /// </summary>
        public void Run() {
            if (Running) return;

            Debug.LogFormat("<color=red>Running:{0} Initialized:{1} Ready:{2}</color>",Running,Initialized,Ready);

            List<RAEComponent> comps = new List<RAEComponent>() { agent, extender, interpreter };
            comps.AddRange(extraRAEComponents);

            foreach (RAEComponent comp in comps) {
                Debug.LogFormat("<color=red>Comp:{0} Ready:{1}",comp.GUIName,comp.Ready);
            }

            if (Initialized && Ready) {
                running = true;
                StartCoroutine(RunNextIterationCoroutine(PauseAfter));

                //done = true;
                //StartCoroutine(GetNextAction());
                //running = agent.HasNext;
                //itersSinceRun = 0;
            }
            else {
                Debug.LogWarning("Replayer is not ready to be run yet");
            }
        }

        /// <summary>
        /// Pauses the replay process.
        /// This is the function called by the Pause button in the GUI.
        /// </summary>
        public IEnumerator Pause() {
            yield return StartCoroutine(extender.OnPause());
            running = false;
            yield break;
        }

        private void EndRun() {
            running = false;
            Debug.Log("Run has Ended, Following Exit behavior.");
            if (ExitOnDone) {
                // reader.Close();
                //writer.Close(interpreter.Footer);
#if UNITY_EDITOR
                EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            }
        }

        private void HeavyDebug(string message) {
            if (heavyDebug) {
                Debug.LogFormat("<color=red>HD:</color> {0}", message);
            }
        }
        
        /// <summary>
        /// This is the top level of the replay coroutine. 
        /// Conventionally it is called by the Run button in the GUI and at the end of the last replay iteration.
        /// </summary>
        /// <returns></returns>
        IEnumerator RunNextIterationCoroutine(long steps) { 
            while(!Initialized || !Ready) {yield return 0;}

            itersSinceRun = 0;

            while (Running) {

                // We need a consensus on readyness before next iteration
                while (!Ready) { yield return 0; }

                HeavyDebug("extender.OnIterationState()");
                yield return StartCoroutine(extender.OnIterationStart());

                // loop on the agent until we have a next action
                do {
                    HeavyDebug("GetNextAction()");
                    yield return StartCoroutine(GetNextAction());
                    if(!agent.HasNext) {
                        EndRun();
                        yield break;
                    }
                } while (agent.HasNext && extender.SkipAction(agent.CurrentAction));

                // let the extender know about pre-action
                HeavyDebug("extender.OnActionPre()");
                yield return StartCoroutine(extender.OnActionPre(agent.CurrentAction));

                HeavyDebug("interpreter.ResetInterpretation()");
                yield return StartCoroutine(interpreter.ResetInterpretation());

                HeavyDebug("extender.AssumeState()");
                yield return StartCoroutine(extender.AssumeState(agent.CurrentAction.StateObject));

                Time.timeScale = NormalSpeed;

                HeavyDebug("extender.PerformAction()");
                yield return StartCoroutine(extender.PerformAction(agent.CurrentAction));

                if (screenshotTiming == ScreenShotTimingOption.OnCreate) {
                    HeavyDebug("CaptureScreenShot()");
                    yield return StartCoroutine(CaptureScreenShot(interpreter.ScreenShotName(agent.CurrentAction)));
                }

                Time.timeScale = TimeAcceleration;

                HeavyDebug("WaitForStop()");
                yield return StartCoroutine(WaitForStop(agent.CurrentAction));

                HeavyDebug("extender.OnStop()");
                yield return StartCoroutine(extender.OnStop(agent.CurrentAction));

                //Time.timeScale = 0f;

                //screenshot
                if (screenshotTiming == ScreenShotTimingOption.OnStop) {
                    HeavyDebug("CaptureScreenShot()");
                    yield return StartCoroutine(CaptureScreenShot(interpreter.ScreenShotName(agent.CurrentAction)));
                }

                //calculate
                HeavyDebug("interpreter.InterpretAction()");
                yield return StartCoroutine(interpreter.InterpretAction(agent.CurrentAction));

                // let an agent learn is necessary
                HeavyDebug("agent.EvaluateActionResult()");
                yield return StartCoroutine(agent.EvaluateActionResult());

                HeavyDebug("interpreter.CommitInterpretation()");
                yield return StartCoroutine(interpreter.CommitInterpretation());

                if (screenshotTiming == ScreenShotTimingOption.OnWrite) {
                    HeavyDebug("CaptureScreenShot()");
                    yield return StartCoroutine(CaptureScreenShot(interpreter.ScreenShotName(agent.CurrentAction)));
                }

                //yield return StartCoroutine(interpreter.InterpretAction(agent.CurrentAction));

                //yield return StartCoroutine(interpreter.CommitInterpretation());

                HeavyDebug("extender.OnIterationEnd()");
                yield return StartCoroutine(extender.OnIterationEnd());

                itersSinceRun++;

                if (steps > 0 && itersSinceRun == steps) {
                    Pause();
                }
            }
            yield break;
        }

        //Every new level Prep
        IEnumerator WaitLevelLoad() {
            while (!extender.Ready && !interpreter.Ready && !agent.Ready) {
                ready = false;
                yield return 0;
            }
            
            yield break;
        }

        IEnumerator GetNextAction() {
            this.ready = false;
            if (agent.ActionStatus == Agent.ActionRequestStatus.OutOfActions) {
                EndRun();
            }
            lastAction = agent.CurrentAction;
            yield return agent.RequestNextAction();
            if (agent.ActionStatus == Agent.ActionRequestStatus.ActionFound) {
                CheckLevelName(agent.CurrentAction);
                this.ready = true;
                yield break;
            }
            else if (agent.ActionStatus == Agent.ActionRequestStatus.NoAction) {
                Debug.LogError("No Action Found!");
                EndRun();
            }
        }

        private void CheckLevelName(AgentAction action) {
            this.ready = false;
            if (action == AgentAction.NullAction) {
                return;
            }
            if (action.LevelName != SceneManager.GetActiveScene().name
                || action.Action == RAEConstants.LEVEL_START
                || !action.IsSameAttempt(lastAction)) {
                //Debug.LogFormat("<color=red>RAE CHECK LEVEL NAME IS LOADING {0}</color>", action.LevelName);
                SceneManager.LoadScene(action.LevelName);
            }
        }





        /// <summary>
        /// Advances the reader and executes the resulting action, basically 
        /// stepping through replay rather than actively replaying.
        /// This is the function called by the Next Action button in the GUI.
        /// </summary>
        public void StepNextAction() {
            if (Initialized && Ready && !Running) {
                StartCoroutine(RunNextIterationCoroutine(1));
                //yield return StartCoroutine(GetNextAction());
                //yield return StartCoroutine(RunAction(agent.CurrentAction));
            }
        }

        /// <summary>
        /// The call to actually perform replay of an action.
        /// </summary>
        /// <param name="action"></param>
        //public IEnumerator RunAction(AgentAction action) {
        //    while (!Ready) { yield return 0; }

        //    Debug.LogFormat("Running:{0}",action);
        //    //Debug.Log("runstate " +action);
        //    Time.timeScale = 1.0f;
            
        //    //interpreter.ResetInterpretation();

        //    if (!InstantiateState(action)) {
        //        Debug.LogWarning("Instatiate State returned false!");
        //        if (Running) {
        //            yield return StartCoroutine(GetNextAction());
        //        }
        //        done = true;
        //        yield break;
        //    }
        //    PerformAction(action);

        //    extender.OnActionPost(action);

        //    //Time.timeScale = timeAcceleraton;
        //    StartCoroutine(WaitForStop(action));
        //}

        //TODO instantiate state is going to need to failure more gracefully in 
        //projective replay. Currently it returns a failure if it can't find an 
        //object in the state and that won't work if that object just doesn't exist anymore.

        /// <summary>
        /// Instantiates the state described by the action.
        /// Returns true if the state was successfully instantiated.
        /// Otherwise returns false if the state could not be instantiated normally
        /// because one or more of the elements could not be found in the scene.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        //private bool InstantiateState(AgentAction action) {
        //    if (action.Equals(AgentAction.NullAction))
        //        return false;

        //    if (action.StateObject != null) {
        //        foreach (JObject entry in action.StateObject["Objects"]) {
        //            ReplayBehavior rb = extender.AssumeState(entry, action);
        //            if (rb != null) {  
        //                rb.AddTag(RAEConstants.RAE_STATE);
        //            }
        //        }
        //    }
        //    return true;
        //}

        /// <summary>
        /// Retrieves a particular object by name to be altered in assuming state.
        /// The function will first ask the replay extender for the object in case
        /// special processing is necessary before trying to look for an object with
        /// a given name.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        //private GameObject RetrieveGameObject(JObject node, AgentAction action) {
        //    GameObject ret = extender.RetrieveGameObject(node, action);
        //    if (ret == null) {
        //        ret = GameObject.Find(node[RAEConstants.NAME].ToString());
        //    }
        //    return ret;
        //}

        //private void PerformAction(AgentAction action) {
        //    if (action.Equals(AgentAction.NullAction))
        //        return;

        //    extender.PerformAction(action);
        //}

        IEnumerator WaitForStop(AgentAction action) {
            Rigidbody[] state = MultiTag.FindAnyComponentsWithTags<Rigidbody>(RAEConstants.RAE_STATE, RAEConstants.RAE_ACTION);

            float callTime = Time.time; 

            switch (stopCondition) {
                case StoppingCondition.Instant:
                    Time.timeScale = NormalSpeed;
                    yield return new WaitForFixedUpdate();
                    break;
                case StoppingCondition.WaitForStop:
                    bool allSleeping = false;
                    while (!allSleeping) {
                        allSleeping = true;
                        if (Time.time - callTime > timeOut) {
                            break;
                        }

                        foreach (Rigidbody rigidbody in state) {
                            if (rigidbody != null & !rigidbody.IsSleeping()) {
                                allSleeping = false;
                                break;
                            }
                        }
                        yield return new WaitForFixedUpdate();
                    }
                    break;
                case StoppingCondition.TimeOut:
                    while (Time.time - callTime < timeOut) {
                        yield return null;
                    }
                    break;
                case StoppingCondition.Simulate:
                    float delay = timeOut;
                    if (agent.Next.IsSameAttempt(agent.CurrentAction)) {
                        TimeSpan timeToNext = agent.Next.Time - agent.CurrentAction.Time;
                        delay = Mathf.Min(timeOut, (float)timeToNext.TotalSeconds);
                    }
                    while (Time.time - callTime < delay) {
                        yield return new WaitForFixedUpdate();
                    }
                    break;
                case StoppingCondition.Custom:
                    yield return StartCoroutine(extender.StoppingCoroutine(action));
                    break;

            }
            Time.timeScale = PauseSpeed;
            //yield return StartCoroutine(Stop(action));
            yield break;
        }

        private IEnumerator Stop(AgentAction action) {

            yield return StartCoroutine(extender.OnStop(action));

            //Time.timeScale = 0f;

            //screenshot
            if (screenshotTiming == ScreenShotTimingOption.OnStop) {
                yield return StartCoroutine(CaptureScreenShot(interpreter.ScreenShotName(action)));
            }

            //calculate
            yield return StartCoroutine(interpreter.InterpretAction(action));

            //record
            // Debug.Log("Line to Write:" + interpreter.CurrentLine);
            //writer.Write(interpreter.Current);
            yield return StartCoroutine(interpreter.CommitInterpretation());

            if (screenshotTiming == ScreenShotTimingOption.OnWrite) {
                yield return StartCoroutine(CaptureScreenShot(interpreter.ScreenShotName(action)));
            }

            //yield return StartCoroutine(extender.OnIterationEnd());
            //done = true;
            //if (Running) {
            //    yield return StartCoroutine(GetNextAction());
            //}
            yield break;
        }

        #endregion

        #region Screenshot Logic 

        public IEnumerator CaptureScreenShot(string name) {
            if (!string.IsNullOrEmpty(name)) {
                if (this.screenShotMode == ScreenShotModeOption.PNG) {
                    if (!name.EndsWith(".png")) {
                        name += ".png";
                    }
                }
                else if (this.screenShotMode == ScreenShotModeOption.JPG) {
                    if (!name.EndsWith(".jpg")) {
                        name += ".jpg";
                    }
                }
                string filePath = Path.Combine(screenshotDirectory, name);
                if (!File.Exists(filePath))
                    yield return StartCoroutine(ScreenShotCoroutine(filePath));
            }
            yield break;
        }

        IEnumerator WaitForFrames(int frames) {
            for (int i =0; i < frames; i++) {
                yield return new WaitForEndOfFrame();
            }
            yield break;
        }

        IEnumerator ScreenShotCoroutine(string filePath) {
            yield return StartCoroutine(extender.OnScreenshotPre());
            this.takingScreenshot = true;
            yield return WaitForFrames(2);
            switch (ScreenShotMode) {
                case ScreenShotModeOption.JPG:
                    ScreenshotToJPG(filePath);
                    break;
                case ScreenShotModeOption.PNG:
                    ScreenshotToPNG(filePath);
                    break;
            }
            this.takingScreenshot = false;
            yield return StartCoroutine(extender.OnScreenshotPost());
            yield break;
        }

        void ScreenshotToPNG(string filePath) {
            Application.CaptureScreenshot(filePath);
        }

        void ScreenshotToJPG(string filePath) {
            Debug.Log("Taking Screenshot");
            int resWidth = 400;
            int resHeight = 300;
            RenderTexture rt = new RenderTexture(resWidth, resHeight, 24);
            Camera.main.targetTexture = rt;
            Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
            Camera.main.Render();
            Camera.main.Render();
            RenderTexture.active = rt;
            screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
            Camera.main.targetTexture = null;
            RenderTexture.active = null; // JC: added to avoid errors
            Destroy(rt);
            byte[] bytes = screenShot.EncodeToJPG();
            File.WriteAllBytes(filePath, bytes);
        }
                 
        #endregion
    }
}
#endif