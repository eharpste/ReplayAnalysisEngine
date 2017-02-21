#if REPLAY_ENGINE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RAE {

    public abstract class AnalysisWriter {

        public bool writingEnabled = true;

        public abstract void Open(string header);

        public abstract void Close();

        public abstract void Close(string footer);

        public abstract void Write(string line);

        public abstract void Write(string[] line);

    }

}
#endif