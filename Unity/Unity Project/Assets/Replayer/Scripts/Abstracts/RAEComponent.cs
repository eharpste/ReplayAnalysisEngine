#if REPLAY_ENGINE
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RAE {

    [RequireComponent(typeof(ReplayAnalysisEngine))]
    public abstract class RAEComponent : MonoBehaviour {

        /// <summary>
        /// This function will be called by the RAE to trigger inialization of a run.
        /// Once the component is initialized its Initialized property should be true.
        /// If some part of the process fails then it should log an Exception.
        /// </summary>
        public virtual IEnumerator Initialize() { yield break; }

        /// <summary>
        /// This is a flag to inform the RAE that this component is initialized to 
        /// begin replay. Usually this is meant to gate accessing external resources.
        /// </summary>
        public virtual bool Initialized { get { return true; } }

        /// <summary>
        /// This is a flag to inform the RAE that this component is ready to move 
        /// on to the next action. Most steps in the replay process are contingent 
        /// on all subcomponets of the replay process being ready to proceed.
        /// </summary>
        public virtual bool Ready { get { return true; } }

        /// <summary>
        /// This is the name for the component that will appear in the GUI and a 
        /// button to access the component's OptionsPane.
        /// </summary>
        public abstract string GUIName { get; }

        /// <summary>
        /// This function is used to draw the in-engine options menu for the 
        /// component. It will be called within the context of a GUILayout.Window 
        /// after a GUILayout.BeginVertical() call. The GUILayout.EndVertical() 
        /// will be managed in the calling context after returning from this 
        /// functions.
        /// </summary>
        public virtual void OptionsPane() {
            ReplayGUI.Label(string.Format("No options provided for {0} component", GUIName));
        }


        /// <summary>
        /// This function should return a string that will be appended to a log describing the
        /// current run of the system. This will be called after intialization to report
        /// the particular settings of the current run of the system.
        /// </summary>
        /// <returns></returns>
        public abstract string RunReport();


        /// <summary>
        /// This should be the name used by the RAEComponent in the log report.
        /// </summary>
        public abstract string RunReportName { get; }
        
        /// <summary>
        /// The ReplayGUI will call this on each RAEComponent so they can show 
        /// some debug output during running replay without having to compete 
        /// for screen real estate.
        /// </summary>
        public virtual void DebugGUI() { return; }

        /// <summary>
        /// A flag for setting the order that elements should display in the gui
        /// </summary>
        /// <returns></returns>
        public virtual int GUIOrder { get { return _gui_order; } set { _gui_order = value; } }
        protected int _gui_order = 0;

        protected Dictionary<string, List<System.Action>> eventListeners = new Dictionary<string, List<System.Action>>();

        public virtual void RegisterEventListener(string eventype, System.Action callback) {
            if(!eventListeners.ContainsKey(eventype)) {
                eventListeners[eventype] = new List<System.Action>();
            }
            eventListeners[eventype].Add(callback);
        }

        public virtual void RemoveEventListener(string evenType, System.Action callback) {
            if (eventListeners.ContainsKey(evenType)) {
                eventListeners[evenType].Remove(callback);
            }
        }

        protected virtual void FireEvent(string eventType) {
            if (eventListeners.ContainsKey(eventType)) {
                foreach (System.Action c in eventListeners[eventType]) {
                    c();
                }
            }
        }

    }
}
#endif                     