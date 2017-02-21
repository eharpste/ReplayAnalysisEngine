#if REPLAY_ENGINE
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace RAE {

    public abstract class Interpreter : RAEComponent {
        //protected string currentLine = string.Empty;
        protected Agent agent;

        //public abstract string Header {
        //    get;
        //}

        //public abstract string Footer {
        //    get;
        //}

        //public abstract string Current {
        //    get;
        //}

        public virtual bool TakeScreenShots { get { return false; } }

        public abstract string ScreenShotName(AgentAction action);

        public abstract IEnumerator ResetInterpretation();

        public abstract IEnumerator InterpretAction(AgentAction action);

        public abstract IEnumerator CommitInterpretation();

    }

}
#endif