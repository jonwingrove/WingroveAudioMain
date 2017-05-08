﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace WingroveAudio
{

    [CustomPropertyDrawer(typeof(AudioEventNameAttribute))]
    public class AudioEventNameAttributeDrawer : PropertyDrawer
    {

        int bankIndex = -1;        

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
                        
            SerializedProperty eventProperty = property;

            position.height = GetPropertyHeight(property, label);
            
            position.yMin += EditorGUIUtility.singleLineHeight;
            GUI.Box(position, "");
            position.yMin -= EditorGUIUtility.singleLineHeight;

            EditorGUI.LabelField(position, label);            
            position.height = EditorGUIUtility.singleLineHeight;
            position = Next(position);
                        

            string testString = eventProperty.stringValue;
            if (WingroveRoot.Instance != null)
            {
                if (WingroveRoot.Instance.m_audioNameGroups == null || WingroveRoot.Instance.m_audioNameGroups.Length == 0)
                {
                    EditorGUI.HelpBox(position, "WingroveRoot does not have any AudioNameGroups!", MessageType.Error);
                    position = Next(position);
                }
                else
                {
                    int inWhichBank = WingroveRoot.Instance.FindEvent(testString);
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
                        EditorGUI.HelpBox(position, "Event name " + testString + " does not exist in any AudioNameGroup", MessageType.Warning);
                        position = Next(position);
                        offerAdd = true;
                    }
                    else if (inWhichBank != bankIndex)
                    {
                        EditorGUI.HelpBox(position, "Event name \"" + testString + "\" has been found in a different AudioNameGroup", MessageType.Warning);
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
                    foreach (AudioNameGroup evg in WingroveRoot.Instance.m_audioNameGroups)
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

                    testString = EditorGUI.TextField(position, "Type event name:", testString);
                    position = Next(position);

                    List<GUIContent> eventDropdown = new List<GUIContent>();
                    eventDropdown.Add(new GUIContent(testString));
                    int selected = 0;
                    if (WingroveRoot.Instance.m_audioNameGroups[bankIndex] != null)
                    {
                        foreach (string evName in WingroveRoot.Instance.m_audioNameGroups[bankIndex].GetEvents())
                        {
                            eventDropdown.Add(new GUIContent(evName));
                        }
                    }
                    selected = EditorGUI.Popup(position, new GUIContent("or select:"), selected, eventDropdown.ToArray());
                    position = Next(position);
                    if (selected != 0)
                    {
                        testString = WingroveRoot.Instance.m_audioNameGroups[bankIndex].GetEvents()[selected - 1];
                    }

                    if (offerAdd)
                    {
                        if (GUI.Button(position, "Add \"" + testString + "\" to bank \"" +
                            WingroveRoot.Instance.m_audioNameGroups[bankIndex] + "\""))
                        {
                            WingroveRoot.Instance.m_audioNameGroups[bankIndex].AddEvent(testString);
                            EditorUtility.SetDirty(WingroveRoot.Instance.m_audioNameGroups[bankIndex]);
                        }
                        position = Next(position);
                    }                    

                    eventProperty.stringValue = testString;
                }
            }
            else
            {
                EditorGUI.HelpBox(position, "WingroveRoot does not exist!", MessageType.Error);
                position = Next(position);
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