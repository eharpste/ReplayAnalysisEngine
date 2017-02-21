#if REPLAY_ENGINE

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;
using System;

namespace RAE {

    [System.Serializable]
    public class TabDelimitedTextFileWriter : AnalysisWriter {
        

        private TextWriter writer = null;

        public bool applyRunID = true;
        public bool allowEmptyString = true;
                               
        public string logPath = "";
        public string logFileName = "";

        //private ReplayGUI mainGUI;
        //private Interpreter interpreter;
        //private ReplayAnalysisEngine rae;

        public TabDelimitedTextFileWriter(string fileName, string filePath, bool applyRunID, bool allowEmptyString) {
            this.logPath = filePath;
            this.logFileName = fileName;
            this.applyRunID = applyRunID;
            this.allowEmptyString = allowEmptyString;
        }

        public TabDelimitedTextFileWriter() : this("", ".", false, true) { }

        //public override bool Initialized {
        //    get { return writer != null || !writingEnabled; }
        //}

        //void Start() {
        //    mainGUI = GetComponent<ReplayGUI>();
        //    interpreter = GetComponent<Interpreter>();
        //    rae = GetComponent<ReplayAnalysisEngine>();
        //}

        //void OnDestroy() {
        //    this.Close(interpreter.Footer);
        //}

        public override void Open(string header) {
            if (!writingEnabled) return;
            if (string.IsNullOrEmpty(logPath)) {
                logPath = Application.dataPath;
            }
            try {
                if (applyRunID) {
                    this.writer = new StreamWriter(Path.Combine(logPath, logFileName.Replace(".", string.Format(" - {0}.", ReplayAnalysisEngine.mainRAE.RunReportID))));
                }
                else {
                    this.writer = new StreamWriter(Path.Combine(logPath, logFileName));
                }
            }
            catch (System.Exception e) {
                Debug.LogException(e);
                this.writer = null;
            }

            this.WriteLine(header);
        }

        public override void Close() {
            this.Close(string.Empty);
        }

        public override void Close(string footer) {
            if (this.writer != null && writingEnabled) {
                this.WriteLine(footer);
                this.writer.Close();
                this.writer = null;
            }
        }

        public override void Write(string line) {
            if (writingEnabled)
                this.WriteLine(line);
        }

        public override void Write(string[] line) {
            if (writingEnabled)
                this.WriteLine(String.Join("\t", line));
        }

        private void WriteLine(string line) {
            if (writingEnabled && this.writer != null) {
                if (this.allowEmptyString) {
                    this.writer.WriteLine(line);
                    this.writer.Flush();
                }
                else if (!string.IsNullOrEmpty(line)) {
                    this.writer.WriteLine(line);
                    this.writer.Flush();
                }
            }
        }

        public bool PathExists() {
            return this.logPath != string.Empty && !Directory.Exists(logPath);
        }

        public void CreatePath() {
            Directory.CreateDirectory(logPath);
        }
        //private bool screenShotTouched = false;


        //public override void OptionsPane() {
        //    writingEnabled = ReplayGUI.ToggleField(writingEnabled, "Enable Writing");
        //    bool bak = GUI.enabled;
        //    GUI.enabled &= writingEnabled;

        //    applyRunID = ReplayGUI.ToggleField(applyRunID, "Apply Run ID");

        //    logFileName = ReplayGUI.TextField(logFileName, "Output File Name", 4, 1);

        //    logPath = ReplayGUI.TextField(logPath, "Output Directory", 4, 1).Trim('"');

        //    if (logPath != string.Empty && !Directory.Exists(logPath)) {
        //        ReplayGUI.Label("The log path does not exist would you like to create it?");
        //        GUILayout.BeginHorizontal();
        //        if (ReplayGUI.Button("Yes", 1)) {
        //            Directory.CreateDirectory(logPath);
        //        }
        //        if (ReplayGUI.Button("No")) {
        //            logPath = string.Empty;
        //        }
        //        GUILayout.EndHorizontal();
        //    }
        //    GUI.enabled = bak;
        //}

        //public override string GUIName {
        //    get { return "Text Writer"; }
        //}

        //public override string RunReportName {
        //    get { return "TEXT FILE WRITER"; }
        //}

        //public override string RunReport() {
        //    return string.Format("Writing Enabled:{0}\nLog Path:{1}\nLog File Name:{2}", writingEnabled, logPath, logFileName);
        //}

        
    }
}
#endif