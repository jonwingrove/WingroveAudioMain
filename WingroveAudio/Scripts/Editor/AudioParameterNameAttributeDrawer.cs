﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace WingroveAudio
{

    [CustomPropertyDrawer(typeof(AudioParameterNameAttribute))]
    public class AudioParameterNameAttributeDrawer : PropertyDrawer
    {

        int bankIndex = -1;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {

            SerializedProperty paramProperty = property;

            position.height = GetPropertyHeight(property, label);

            position.yMin += EditorGUIUtility.singleLineHeight;
            GUI.Box(position, "");
            position.yMin -= EditorGUIUtility.singleLineHeight;

            EditorGUI.LabelField(position, label);
            position.height = EditorGUIUtility.singleLineHeight;
            position = Next(position);

            string testString = paramProperty.stringValue;

            AudioNameGroup[] nameGroups = WingroveAudio.EditorUtilities.GetAudioNameGroups();
            if (nameGroups == null || nameGroups.Length == 0)
            {
                EditorGUI.HelpBox(position, "WingroveRoot does not have any AudioNameGroups!", MessageType.Error);
                position = Next(position);
            }
            else
            {
                int inWhichBank = WingroveAudio.EditorUtilities.FindParameter(testString);
                if (bankIndex == -1 && inWhichBank != -1)
                {
                    bankIndex = inWhichBank;
                }
                if (bankIndex == -1 && inWhichBank == -1)
                {
                    bankIndex = 0;
                }
                bool offerAdd = false;
                if (inWhichBank == -1)
                {
                    EditorGUI.HelpBox(position, "Parameter name " + testString + " does not exist in any AudioNameGroup", MessageType.Warning);
                    position = Next(position);
                    offerAdd = true;
                }
                else if (inWhichBank != bankIndex)
                {
                    EditorGUI.HelpBox(position, "Parameter name \"" + testString + "\" has been found in a different AudioNameGroup", MessageType.Warning);
                    position = Next(position);
                    if (GUI.Button(position, "Select correct bank"))
                    {
                        bankIndex = inWhichBank;
                        position = Next(position);
                    }
                }
                else
                {
                    EditorGUI.HelpBox(position, "", MessageType.None);
                    position = Next(position);
                }

                List<GUIContent> displayOptions = new List<GUIContent>();
                foreach (AudioNameGroup evg in nameGroups)
                {
                    if (evg != null)
                    {
                        displayOptions.Add(new GUIContent(evg.name));
                    }
                    else
                    {
                        displayOptions.Add(new GUIContent("null AudioNameGroup - check your WingroveRoot"));
                    }
                }

                bankIndex = EditorGUI.Popup(position, new GUIContent("Select bank"), bankIndex, displayOptions.ToArray());
                position = Next(position);

                testString = EditorGUI.TextField(position, "Type parameter name:", testString);
                position = Next(position);

                List<GUIContent> parameterDropdown = new List<GUIContent>();
                parameterDropdown.Add(new GUIContent(testString));
                int selected = 0;
                if (nameGroups[bankIndex] != null)
                {
                    foreach (string evName in nameGroups[bankIndex].GetParameters())
                    {
                        parameterDropdown.Add(new GUIContent(evName));
                    }
                }
                selected = EditorGUI.Popup(position, new GUIContent("or select:"), selected, parameterDropdown.ToArray());
                position = Next(position);
                if (selected != 0)
                {
                    testString = nameGroups[bankIndex].GetParameters()[selected - 1];
                }

                if (offerAdd)
                {
                    if (GUI.Button(position, "Add \"" + testString + "\" to bank \"" +
                        nameGroups[bankIndex] + "\""))
                    {
                        nameGroups[bankIndex].AddParameter(testString);
                        EditorUtility.SetDirty(nameGroups[bankIndex]);
                    }
                    position = Next(position);
                }

                paramProperty.stringValue = testString;
            }


        }

        Rect Next(Rect inRect)
        {
            inRect.yMin += EditorGUIUtility.singleLineHeight;
            inRect.yMax += EditorGUIUtility.singleLineHeight;
            return inRect;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 6;
        }

    }

}