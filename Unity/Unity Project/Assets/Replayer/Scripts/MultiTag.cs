using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

[ExecuteInEditMode()]
public class MultiTag : MonoBehaviour {

    #region ================ Static Properties ======================

    private static Dictionary<string, HashSet<GameObject>> tagMap = new Dictionary<string,HashSet<GameObject>>();

    private static bool AddToMap(string tag, GameObject go) {
        if (!tagMap.ContainsKey(tag)) {
            tagMap[tag] = new HashSet<GameObject>();
        }
        if (!tagMap[tag].Contains(go)) {
            tagMap[tag].Add(go);
            return true;
        }
        return false;
    }

    private static bool RemoveFromMap(string tag, GameObject go) {
        if (tagMap.ContainsKey(tag)) {
            if (tagMap[tag].Remove(go)) {
                if (tagMap[tag].Count == 0) {
                    tagMap.Remove(tag);
                }
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Find a single GameObject with the given tag. Designed to mimic the standard tag API but 
    /// really just a wrapper for calling FindOneGameObjectWithTag().
    /// </summary>
    /// <param name="tag"></param>
    /// <returns></returns>
    public static GameObject FindGameObjectWithTag(string tag) {
        return FindOneGameObjectWithTag(tag);
    }

    public static T FindComponentWithTag<T>(string tag) where T: Component {
        return FindOneComponentWithTag<T>(tag);
    }

    public static GameObject[] FindGameObjectsWithTag(string tag) {
        return FindAllGameObjectsWithTag(tag);
    }

    /// <summary>
    /// Find a single GameObject with the given tag. What I feel is a far more sensible naming 
    /// convention for the methods where they differ by more than a single 's' somewhere in
    /// the signature.
    /// </summary>
    /// <param name="tag"></param>
    /// <returns></returns>
    public static GameObject FindOneGameObjectWithTag(string tag) {
        if (tagMap.ContainsKey(tag)) {
            return tagMap[tag].First<GameObject>();
        }
        return null;
    }

    public static T FindOneComponentWithTag<T>(string tag) where T: Component {
        if (tagMap.ContainsKey(tag)) {
            return tagMap[tag].First<GameObject>().GetComponent<T>();
        }
        return null;
    }

    public static GameObject[] FindAllGameObjectsWithTag(string tag) {
        if(tagMap.ContainsKey(tag)) {
            return tagMap[tag].ToArray();
        }
        return new GameObject[] {};
    }

    public static T[] FindAllComponentsWithTag<T>(string tag) where T: Component {
        if (tagMap.ContainsKey(tag)) {
            return (from GameObject go 
                    in tagMap[tag]
                    select go.GetComponent<T>()).Where<T>(c => c != null).ToArray<T>();
        }
        return new T[] { };
    }

    /// <summary>
    /// Find any GameObjects with any of the tags in the provided array.
    /// </summary>
    /// <param name="tags"></param>
    /// <returns></returns>
    public static GameObject[] FindAnyGameObjectsWithTags(params string[] tags) {
        HashSet<GameObject> ret = new HashSet<GameObject>();
        foreach (string t in tags) {
            ret.UnionWith(FindAllGameObjectsWithTag(t));
        }
        return ret.ToArray();
    }

    public static T[] FindAnyComponentsWithTags<T>(params string [] tags) where T:Component {
        return (from GameObject go 
                in FindAnyGameObjectsWithTags(tags)
                select go.GetComponent<T>()).Where<T>(c => c !=null).ToArray<T>();
    }

    public static int CountGameObjectsWithTag(string tag) {
        if(tagMap.ContainsKey(tag)) {
            return tagMap.Count;
        }
        else {
            return 0;
        }
    }


    public static string[] SortedTagOptions() {
        List<string> keys = new List<string>();
        foreach (string k in tagMap.Keys) {
            keys.Add(k);
        }
        keys.Sort();
        return keys.ToArray();
    }


    #endregion ======================================================

    public IList<string> multitags {
        get {
            return _multitags.AsReadOnly();
        }
    }

    [SerializeField()]
    private  List<string> _multitags = new List<string>();

    public bool HasMultiTag(string tag) {
        return _multitags.Contains(tag);
    }

    public void AddMultiTag(string tag) {
        if (!_multitags.Contains(tag)) {
            AddToMap(tag, this.gameObject);
            _multitags.Add(tag);
        }
    }

    public void AddMultiTags(string[] tags) {
        foreach (string s in tags) {
            if (!_multitags.Contains(s)) {
                AddToMap(s, this.gameObject);
                _multitags.Add(s);
            }
        }
    }

    public void RemoveMultiTag(string tag) {
        if (_multitags.Contains(tag) && tagMap.ContainsKey(tag)) {
            RemoveFromMap(tag, this.gameObject);
            _multitags.Remove(tag);
        }
    }

    public void RemoveMultiTags(string[] tags) {
        foreach (string s in tags) {
            _multitags.Remove(s);
            RemoveFromMap(s,this.gameObject);
        }
    }

    public void ChangeMultiTag(string oldtag, string newtag) {
        for (int i = 0; i < _multitags.Count; i++) {
            if (_multitags[i] == oldtag) {
                _multitags[i] = newtag;
                RemoveFromMap(oldtag, this.gameObject);
                AddToMap(newtag, this.gameObject);
            }
        }
    }


    public void ClearMultiTags() {
        foreach (string s in _multitags) {
            RemoveFromMap(s, this.gameObject);
        }
        _multitags.Clear();
    }

    void OnEnable() {
        //this.multitags = this.multitags.Distinct().ToList<string>();
        foreach (string s in _multitags) {
            AddToMap(s, this.gameObject);
        }
    }

    void OnDestroy() {
        ClearMultiTags();
    }
}
