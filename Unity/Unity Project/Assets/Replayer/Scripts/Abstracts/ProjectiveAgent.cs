#if REPLAY_ENGINE

using UnityEngine;
using System.Collections;
using System;

namespace RAE {
    public abstract class ProjectiveAgent : RAEComponent {
        /// <summary>
        /// The different orientations that the ProjectiveAgent can be in:
        /// 
        /// Observing   - The agent is acting based on the historical record of 
        ///               logged actions
        /// Autonomous  - The agent is acting upon its own reasoning and will 
        ///               not defer to the logged trace.
        /// FallingBack - The agent is ahead of its observation set and is 
        ///               using a fall back resource to get an idea of 
        ///               something to do next.
        /// Stuck       - The agent is out of ideas and either cannot use the 
        ///               log record or does not have an idea of what to do in
        ///               the current state.
        /// 
        /// 
        /// </summary>
        public enum AgentOrientation {
            Observing,
            Autonomous,
            FallingBack,
            Stuck
        }

        public abstract AgentOrientation Orientation { get; }
        public abstract AgentAction CurrentAction { get; }
        public abstract IEnumerator RequestAction();
        public abstract IEnumerator TrainAction(AgentAction action);
        public override string GUIName { get { return "Projective Agent"; } }
    }
}

#endif