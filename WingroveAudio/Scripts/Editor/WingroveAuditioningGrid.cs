using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

namespace WingroveAudio
{

    public class WingroveAuditioningGrid : EditorWindow
    {
        [MenuItem("Window/Wingrove Audio/Auditioning Window")]
        static void Init()
        {
            EditorWindow.GetWindow(typeof(WingroveAuditioningGrid));
        }

        int m_bankIndex = 0;

        void OnGUI()
        {
            title = "Wingove Auditioning";
            if (Application.isPlaying)
            {
                if (WingroveRoot.InstanceEditor != null)
                {
                    if (WingroveRoot.InstanceEditor.m_audioNameGroups == null || WingroveRoot.InstanceEditor.m_audioNameGroups.Length == 0)
                    {
                        EditorGUILayout.HelpBox("WingroveRoot does not have any AudioNameGroups!", MessageType.Error);
                    }
                    else
                    {
                        List<GUIContent> displayOptions = new List<GUIContent>();
                        foreach (AudioNameGroup evg in WingroveRoot.InstanceEditor.m_audioNameGroups)
                        {
                            displayOptions.Add(new GUIContent(evg.name));
                        }

                        m_bankIndex = EditorGUILayout.Popup(new GUIContent("Select bank"), m_bankIndex, displayOptions.ToArray());

                        int x = 0;
                        EditorGUILayout.BeginHorizontal();
                        foreach (string ev in WingroveRoot.InstanceEditor.m_audioNameGroups[m_bankIndex].GetEvents())
                        {
                            if (GUILayout.Button(ev))
                            {
                                WingroveRoot.InstanceEditor.PostEvent(ev);
                            }
                            ++x;
                            if (x == 5)
                            {
                                EditorGUILayout.EndHorizontal();
                                EditorGUILayout.BeginHorizontal();
                                x = 0;
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("WingroveRoot not found - create your audio hierarchy!", MessageType.Error);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Auditioning Window is only active when running", MessageType.Info);
            }
        }

    }

}