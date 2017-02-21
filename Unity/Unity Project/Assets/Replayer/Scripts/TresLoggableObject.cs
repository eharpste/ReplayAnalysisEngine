using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RAE {

    /// <summary>
    /// A very simple behavior script that can be applied to any objects that you 
    /// wish to show up in State snaphots of the game throughout their existence in
    /// the scene. Use of this script is entirely optional.
    /// </summary>
    public class TresLoggableObject : MonoBehaviour {
        /// <summary>
        /// The name of the prefab that this GameObject was a copy of.
        /// It is possible to capture this information from the Awake() method before other
        /// Behaviors get the chance to re-name the Object. Altrernatively this can be set
        /// through its public access in the Interactions pane of the editor.
        /// </summary>/
        public string prefabName = string.Empty;

        /// <summary>
        /// A list of additional tags that you want to annotate this object with.
        /// </summary>
        public string[] tags;

        public float appox_epsilon = .0001f;
        public float angleBinSize = 15;

        private new Collider collider = null;
        private CheckPointTrigger checkpoint = null;
        private BlockCollision block = null;
        //private Hashtable sai_bak = null;
        private bool acting = false;
        private string tresName = string.Empty;
        private Hashtable bak = new Hashtable();


        #region ============================|   Specific Properties     |==================================

        private NewInventory newInv;

        #endregion ========================================================================================

        void Awake() {
            // Debug.Log("LoggableObject Awake "+this.gameObject.name);
            if (string.IsNullOrEmpty(prefabName)) {
                if (this.gameObject.name.Contains("(Clone)")) {
                    prefabName = this.gameObject.name.Substring(0, this.gameObject.name.LastIndexOf("(Clone)"));
                }
                else {
                    prefabName = this.gameObject.name;
                }
            }
            this.collider = GetComponent<Collider>();
            this.checkpoint = GetComponent<CheckPointTrigger>();
            this.block = GetComponent<BlockCollision>();
        }

        // Use this for initialization
        void Start() {
            //Debug.Log("LoggableObject Start " + this.gameObject.name);
            //GameObject tb = GameObject.FindGameObjectWithTag("TrestleBridge");
            //if (tb != null) {
            //    tresName = tb.GetComponent<TrestleApprenticeLearerAgent>().AddLoggableObject(this);
            //}
            //GameObject inv = GameObject.Find("NewInventory");
            //if (inv != null) {
            //    newInv = GameObject.Find("NewInventory").GetComponent<NewInventory>();
            //}
            //else {
            //    newInv = null;
            //}
        }

        public string TresName {
            get {
                return this.tresName;
            }
        }

        //void OnDestroy() {
        //    GameObject.FindGameObjectWithTag("TrestleBridge").GetComponent<TresBridgeBehavior>().RemoveLoggableObject(this);
        //}

        public Hashtable StateData(Vector3 origin) {
            Hashtable dict = new Hashtable();
            if (this.gameObject == null) {
                return dict;
            }
            Vector3 pos = this.transform.position - origin;
            dict["x"] = pos.x;
            dict["y"] = pos.y;
            if (!this.checkpoint && this.name != "GoalTrigger") {
                dict["rot"] = FormRotation(this.transform.rotation.eulerAngles.z);
            }
            if (this.prefabName.Length > 4) {
                dict["type"] = this.prefabName.Substring(0, 4);
            }
            else {
                dict["type"] = this.prefabName;
            }
            dict["_name"] = this.gameObject.name;
            if (this.collider) {
                dict["width"] = this.collider.bounds.size.x;
                dict["height"] = this.collider.bounds.size.y;
            }
            if (this.checkpoint) {
                dict["active"] = this.checkpoint.charged;
            }
            return dict;
        }

        public Hashtable SAIData(Vector3 origin) {
            Vector3 pos = this.transform.position - origin;
            Hashtable dict = new Hashtable();
            dict["selection"] = "?" + this.tresName;
            dict["action"] = "place";
            dict["input"] = new object[] { pos.x, pos.y, FormRotation(this.transform.rotation.eulerAngles.z) };
            return dict;
        }

        private string FormRotation(float z) {
            return (Mathf.Round((z % 360) / angleBinSize) * angleBinSize % 360).ToString();
        }

        public bool PerformSAI(string action, Newtonsoft.Json.Linq.JArray input, Vector3 origin) {
            if (action != "place") {
                Debug.Log("Don't Recognize Action: " + action);
                return false;
            }

            if (this.checkpoint) {
                Debug.Log("Can't act on a checkpount!");
                return false;
            }

            if (this.name == "GoalTrigger") {
                Debug.Log("Can't act on the GoalTrigger");
                return false;
            }

            float new_x = float.NaN;
            float new_y = float.NaN;
            float new_rot = float.NaN;

            new_x = origin.x + input.Value<float>(0);
            new_y = origin.y + input.Value<float>(1);
            new_rot = input.Value<float>(2);

            float curr_rot = this.transform.rotation.eulerAngles.z;

            if (Approx(curr_rot, new_rot)) {
                BackUp();
                this.transform.position = new Vector3(new_x, new_y, 0);
                this.acting = true;
                if (this.block) {
                    PerformBlockStuff();
                }
                return true;
            }
            else {
                BackUp();
                Vector3 currEuler = this.transform.rotation.eulerAngles;
                currEuler.z = new_rot;
                this.transform.rotation = Quaternion.Euler(currEuler);
                this.transform.position = new Vector3(new_x, new_y, 0);
                this.acting = true;
                if (this.block) {
                    PerformBlockStuff();
                }
                return true;
            }
        }

        private void PerformBlockStuff() {
            if (this.newInv) {
                this.transform.GetChild(0).GetComponent<Renderer>().enabled = true;
                newInv.itemsOnMenu.Remove(this.gameObject);
                this.GetComponent<Rigidbody>().isKinematic = false;
                if (this.tag == "Blocks") {
                    newInv.ChangeNumber(this.gameObject, true, this.tag);
                }
                this.tag = "Tower";
            }
        }

        private void UndoBlockStuff(bool revert) {
            if (this.newInv) {
                this.transform.GetChild(0).GetComponent<Renderer>().enabled = false;
                if (revert && bak["tag"].ToString() == "Blocks" && this.tag == "Tower") {
                    newInv.ChangeNumber(this.gameObject, false, this.tag);
                }
            }
        }

        public void BackUp() {
            bak["position"] = this.transform.position;
            bak["rotation"] = this.transform.rotation;
            bak["tag"] = this.tag;
        }

        public void Reset() {
            this.transform.position = (Vector3)bak["position"];
            this.transform.rotation = (Quaternion)bak["rotation"];
            if (this.block) {
                UndoBlockStuff(true);
            }
            this.tag = bak["tag"].ToString();
        }

        private bool Approx(float x1, float x2) {
            return Mathf.Abs(x1 - x2) < appox_epsilon;
        }

        public void CommitToAction() {
            if (this.acting) {
                this.acting = false;
                if (this.block) {
                    UndoBlockStuff(false);
                }
            }
        }
    }

}