#if REPLAY_ENGINE

using UnityEngine;
using System.Collections;
using Newtonsoft.Json.Linq;

namespace RAE {

    public abstract class Agent : RAEComponent {
        public enum ActionRequestStatus {
            Idle,
            Requesting,
            ActionFound,
            NoAction,
            OutOfActions
        }

        public abstract ActionRequestStatus ActionStatus { get; }
        public abstract AgentAction CurrentAction { get; }
        public abstract AgentAction Previous { get; }
        public abstract AgentAction Next { get; }

        public abstract bool HasNext { get; }
        public abstract bool HasNextInSession { get; }

//        public abstract void Load();
//        public abstract void Close();
        
        // I need some kind of flag solution to know when an agent is done finding an action
        public abstract IEnumerator RequestNextAction();
        public virtual IEnumerator EvaluateActionResult() { yield break; }
    }

}

#endif