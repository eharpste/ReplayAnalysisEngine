using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace RAE {

    //[RequireComponent(typeof(MultiTag))]
    public class ReplayBehavior : MonoBehaviour {

        #region ==============================|   MultiTag System  |==============================
        /*
         * I'm putting anything that I don't want to exist in the final version in here but I just need stuff
         * to work right now.
         */
        private MultiTag mt;

        private void EnsureMT() {
            if (this.mt == null) {
                this.mt = this.gameObject.AddComponent<MultiTag>();
            }
        }

        public void AddTags(IList<string> tags) {
            EnsureMT();
            mt.AddMultiTags(tags.ToArray<string>());
        }

        public void AddTag(string tag) {
            EnsureMT();
            mt.AddMultiTag(tag);
        }

        public void RemoveTag(string tag) {
            EnsureMT();
            mt.RemoveMultiTag(tag);
        }

        public void RemoveTags(IList<string> tags) {
            EnsureMT();
            mt.RemoveMultiTags(tags.ToArray<string>());
        }

        public bool HasTag(string tag) {
            EnsureMT();
            return mt.HasMultiTag(tag);
        }

        #endregion

        #region ==============================|   Highlighting and Flashing   |==============================

        private Color bak;
        private bool highlighted = false;
        private bool flashing = false;
        private const float DEFAULT_FLASH_SPEED = .2f;
        private new Renderer renderer;
        private new Collider collider;

        private Color NegateColor(Color c) {
            return new Color(1 - c.r, 1 - c.g, 1 - c.b);
        }

        public void Highlight(Color highlight) {
            if (this.renderer == null || this.renderer.material == null)
                return;
            bak = this.renderer.material.color;
            this.renderer.material.color = highlight;
            highlighted = true;
        }

        public void Highlight(Color highlight, bool additive) {
            if (this.renderer == null || this.renderer.material == null)
                return;
            bak = this.renderer.material.color;
            if (additive)
                this.renderer.material.color += highlight;
            else
                this.renderer.material.color = highlight;
            highlighted = true;
        }

        public void Highlight() {
            if (this.renderer == null || this.renderer.material == null)
                return;
            bak = this.renderer.material.color;
            this.renderer.material.color = NegateColor(bak);
            highlighted = true;
        }

        public void Unhighlight() {
            if (this.renderer == null || this.renderer.material == null || !highlighted)
                return;
            this.renderer.material.color = bak;
            highlighted = false;
        }

        public void Flash() {
            if (this.renderer == null || this.renderer.material == null) return;
            Color c = NegateColor(this.renderer.material.color);
            Flash(c);
        }

        public void Flash(Color color) {
            if (this.renderer == null || this.renderer.material == null) return;
            StartCoroutine(FlashCoroutine(color, DEFAULT_FLASH_SPEED, -1, -1, false));
        }

        public void Flash(Color color, bool additive) {
            if (this.renderer == null || this.renderer.material == null) return;
            StartCoroutine(FlashCoroutine(color, DEFAULT_FLASH_SPEED, -1, -1, additive));
        }

        public void Flash(Color color, int numberOfTimes) {
            if (this.renderer == null || this.renderer.material == null) return;
            StartCoroutine(FlashCoroutine(color, DEFAULT_FLASH_SPEED, numberOfTimes, -1, false));
        }

        public void Flash(Color color, int numberOfTimes, bool additive) {
            if (this.renderer == null || this.renderer.material == null) return;
            StartCoroutine(FlashCoroutine(color, DEFAULT_FLASH_SPEED, numberOfTimes, -1, additive));
        }

        public void Flash(int numberOfTimes) {
            if (this.renderer == null || this.renderer.material == null) return;
            Color c = NegateColor(this.renderer.material.color);
            Flash(c, numberOfTimes);
        }

        public void Flash(Color color, float duration) {
            if (this.renderer == null || this.renderer.material == null) return;
            StartCoroutine(FlashCoroutine(color, DEFAULT_FLASH_SPEED, -1, duration, false));
        }

        public void Flash(Color color, float duration, bool additive) {
            if (this.renderer == null || this.renderer.material == null) return;
            StartCoroutine(FlashCoroutine(color, DEFAULT_FLASH_SPEED, -1, duration, additive));
        }

        public void Flash(float duration) {
            if (this.renderer == null || this.renderer.material == null) return;
            Color c = NegateColor(this.renderer.material.color);
            Flash(c, duration);
        }

        IEnumerator FlashCoroutine(Color c, float speed, int numTime, float duration, bool additive) {
            if (this.renderer == null || this.renderer.material == null)
                yield break;
            int count = 0;
            float dur = 0f;
            float timeSinceFlash = 0f;
            bak = this.renderer.material.color;
            while (flashing) {
                if (timeSinceFlash >= speed) {
                    if (!highlighted) {
                        Highlight(c, additive);
                    }
                    else {
                        Unhighlight();
                        count++;
                    }
                    timeSinceFlash = 0f;
                }
                timeSinceFlash += Time.deltaTime;
                dur += Time.deltaTime;
                if (numTime > 0 && count >= numTime)
                    break;
                if (duration > 0 && dur >= duration)
                    break;
                yield return new WaitForEndOfFrame();
            }
            StopFlash();
            yield break;
        }

        public void StopFlash() {
            this.flashing = false;
            Unhighlight();
        }

        #endregion ===========================|-------------------------------|==============================

        #region ==============================|   GetTouching Functionality   |==============================

        private bool recordTouching = true;

        public bool HasTouching {
            get {
                return this.collider != null && touching.Count > 0 && recordTouching;
            }
        }

        public bool RecordTouching {
            get {
                return recordTouching;
            }
            set {
                if (!value)
                    //     Debug.Log("Does this ever happen?");
                    touching.Clear();
                this.recordTouching = value;
            }
        }


        private HashSet<GameObject> touching = new HashSet<GameObject>();

        void OnCollisionEnter(Collision collision) {

            if (recordTouching) {
                //Debug.LogFormat("{0} hit {1}",this.gameObject.name, collision.gameObject.name);
                touching.Add(collision.gameObject);
            }
        }

        void OnCollisionExit(Collision collision) {

            if (recordTouching) {
                //Debug.LogFormat("{0} unhit {1}", this.gameObject.name, collision.gameObject.name);
                touching.Remove(collision.gameObject);
            }
        }

        void OnCollisionStay(Collision collision) {

            if (recordTouching) {
                //Debug.LogFormat("{0} still hitting {1}",this.gameObject.name, collision.gameObject.name);
                touching.Add(collision.gameObject);
            }
        }

        public IList<GameObject> GetTouchingObjects() {
            //Debug.LogFormat("{0} is touching {1} objects", this.gameObject.name, touching.Count);
            return touching.ToList<GameObject>();
        }

        #endregion ===========================|-------------------------------|==============================

        #region ==============================|          Unity Methods        |==============================

        void Start() {
            //LoggableObject lo = GetComponent<LoggableObject>();
            this.mt = GetComponent<MultiTag>();
            this.collider = GetComponent<Collider>();
            this.renderer = GetComponent<Renderer>();
            /*
            if (lo != null) {
                this.AddTags(lo.tags);
                Destroy(lo);
            }
            */
        }

        void OnDestroy() {
            // Debug.Log("Destroying");
            touching.Clear();
            /*
            foreach (string s in tags) {
                if (tagMap.ContainsKey(s)) {
                    tagMap[s].Remove(this.gameObject);
                }
            }
            if (tagMap.ContainsKey(unityTag)) {
                tagMap[unityTag].Remove(this.gameObject);
            }
            if (tagMap.ContainsKey(prefabName)) {
                tagMap[prefabName].Remove(this.gameObject);
            }
            if (raeMap.ContainsKey(raeTag)) {
                raeMap[raeTag].Remove(this.gameObject);
            }
            */
        }

        #endregion ===========================|-------------------------------|==============================

        #region ==============================|      Serialization Helpers    |==============================

        private float RoundFloat(float val, float roundto) {
            return float.IsNaN(roundto) ? val : Mathf.Round(val / roundto) * roundto;
        }

        private float RoundAngle(float val, float roundto) {
            return float.IsNaN(roundto) ? val : (Mathf.Round((val % 360) / roundto) * roundto % 360);
        }

        public JObject SerializePosition() { return SerializePosition(true, true, true,float.NaN); }

        public JObject SerializePosition(bool x, bool y, bool z) { return SerializePosition(x, y, z, float.NaN); }

        public JObject SerializePosition(bool x, bool y, bool z, float round) {
            JObject jo = new JObject();
            if (x) { jo["X"] = RoundFloat(transform.position.x, round); }
            if (y) { jo["Y"] = RoundFloat(transform.position.y, round); }
            if (z) { jo["Z"] = RoundFloat(transform.position.z, round); }
            return jo;
        }


        public JObject SerializeRotation() { return SerializeRotation(true, true, true, float.NaN); }

        public JObject SerializeRotation(bool x, bool y, bool z) { return SerializeRotation(x, y, z, float.NaN); }

        public JObject SerializeRotation(bool x, bool y, bool z, float round) {
            JObject jo = new JObject();
            if (x) { jo["X"] = RoundAngle(transform.rotation.eulerAngles.x,round); }
            if (y) { jo["Y"] = RoundAngle(transform.rotation.eulerAngles.y, round); }
            if (z) { jo["Z"] = RoundAngle(transform.rotation.eulerAngles.z, round); }
            return jo;
        }

        public JObject SerializeVelocity() { return SerializeVelocity(true, true, true, float.NaN); }

        public JObject SerializeVelocity(bool x, bool y, bool z) { return SerializeVelocity(x, y, z, float.NaN); }

        public JObject SerializeVelocity(bool x, bool y, bool z, float round) {
            JObject jo = new JObject();
            Rigidbody rb = this.GetComponent<Rigidbody>();
            if (rb == null) {
                return jo;
            }
            if (x) { jo["X"] = RoundFloat(rb.velocity.x, round); }
            if (y) { jo["Y"] = RoundFloat(rb.velocity.y, round); }
            if (z) { jo["Z"] = RoundFloat(rb.velocity.z, round); }
            return jo;
        }

        public JObject SerializeAngularVelocity() { return SerializeAngularVelocity(true, true, true,float.NaN); }

        public JObject SerializeAngularVelocity(bool x, bool y, bool z) { return SerializeAngularVelocity(x, y, z, float.NaN); }

        public JObject SerializeAngularVelocity(bool x, bool y, bool z, float round) {
            JObject jo = new JObject();
            Rigidbody rb = this.GetComponent<Rigidbody>();
            if (rb == null) {
                return jo;
            }
            if (x) { jo["X"] = RoundFloat(rb.angularVelocity.x, round); }
            if (y) { jo["Y"] = RoundFloat(rb.angularVelocity.y, round); }
            if (z) { jo["Z"] = RoundFloat(rb.angularVelocity.z, round); }
            return jo;
        }

        public JObject SerializeBounds() { return SerializeBounds(true, true, true); }

        public JObject SerializeBounds(bool x, bool y, bool z) { return SerializeBounds(x, y, z, float.NaN); }

        public JObject SerializeBounds(bool x, bool y, bool z,float round) {
            JObject jo = new JObject();
            Collider c = GetComponent<Collider>();
            if (c == null) {
                return jo;
            }
            if (x) { jo["X"] = RoundFloat(c.bounds.size.x, round);}
            if (y) { jo["Y"] = RoundFloat(c.bounds.size.y, round);}
            if (z) { jo["Z"] = RoundFloat(c.bounds.size.z, round);}
            return jo;
        }

        #endregion ===========================|-------------------------------|==============================

    }

}