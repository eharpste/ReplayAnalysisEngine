using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;


[CustomEditor(typeof(MultiTag))]
class MultiTagEditor : Editor {

    private int errorState = 0;
    private bool showList = true;
    private string newTagString = string.Empty;
    private const string removeTag = "-- Remove Tag --";

    void OnEnable() {
        errorState = 0;
        newTagString = string.Empty;
    }


    public override void OnInspectorGUI() {
        MultiTag mt = target as MultiTag;

        List<string> tagList = new List<string>();
        tagList.Add(removeTag);
        tagList.AddRange(MultiTag.SortedTagOptions());
        string[] tagOptions = tagList.ToArray();

        EditorGUILayout.BeginVertical();

        int toRemove = -1;

        float labWidth = GUI.skin.label.CalcSize(new GUIContent(mt.multitags.Count + ".")).x;

        showList = EditorGUILayout.Foldout(showList, "MultiTags");

        if (showList && mt.multitags.Count > 0) {
            //old tags
            for (int i = 0; i < mt.multitags.Count; i++) {
                EditorGUILayout.BeginHorizontal();

                string oldtag = mt.multitags[i];
                int olddex = Array.IndexOf(tagOptions, oldtag);

                //if(GUILayout.Button("-",GUILayout.Width(25))){
                //    toRemove = i;
                //}
                EditorGUILayout.LabelField((i + 1) + ".", GUILayout.Width(labWidth));

                int newdex = EditorGUILayout.Popup(olddex, tagOptions);

                if (newdex != olddex) {
                    //something changed
                    if (newdex == 0) {
                        //remove the tag
                        toRemove = i;
                        errorState = 0;
                    }
                    else if (!mt.multitags.Contains(tagOptions[newdex])) {
                        //update the tag if its new
                        mt.ChangeMultiTag(tagOptions[olddex], tagOptions[newdex]);
                        errorState = 0;
                    }
                    else {
                        errorState = 1;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
        }

        tagOptions = tagOptions.Except(mt.multitags).ToArray();
        //new tag
        if (tagOptions.Length > 1) {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Add Current MultiTag");
            tagOptions[0] = string.Empty;
            int test = EditorGUILayout.Popup(0, tagOptions);
            if (test != 0) {
                if (!mt.multitags.Contains(tagOptions[test])) {
                    //update the tag if its new
                    mt.AddMultiTag(tagOptions[test]);
                    errorState = 0;
                }
                else {
                    errorState = 1;
                }
            }
            EditorGUILayout.EndHorizontal();
        }


        Event e = Event.current;

        if (e.type == EventType.KeyDown &&  e.keyCode == KeyCode.Return) {
            AddNewMultiTag(newTagString,mt);
        }
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Add New MultiTag");
        newTagString = EditorGUILayout.TextField(newTagString);
        EditorGUILayout.EndHorizontal();
      //  EditorGUILayout.EndHorizontal();


        //error state
        switch(errorState) {
            case 1:
                EditorGUILayout.HelpBox("Can't add multitags more than once to the same object", MessageType.Error);
                break;
            case 2:
                EditorGUILayout.HelpBox("Can't have \""+removeTag+"\" as a tag name",MessageType.Error);
                break;
            default:
                break;
        }

        EditorGUILayout.EndVertical();

        if (toRemove > -1) {
            mt.RemoveMultiTag(mt.multitags[toRemove]);
        }

        if (GUI.changed) {
            EditorUtility.SetDirty(target);
        }
    }


    void AddNewMultiTag(string tag, MultiTag mt) {
        if (!string.IsNullOrEmpty(newTagString)) {
            if (newTagString == removeTag) {
                errorState = 2;
            }
            else if (mt.multitags.Contains(newTagString)) {
                errorState = 1;
            }
            else {
                //add the new tag
                mt.AddMultiTag(newTagString);
                newTagString = string.Empty;
                errorState = 0;
            }
        }
        else {
            errorState = 3;
        }
    }
}


