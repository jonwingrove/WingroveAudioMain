using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System;

namespace WingroveAudio
{
    public class WingroveEventLogWindow : EditorWindow
    {

        [MenuItem("Window/Wingrove Audio/Event Log Window")]
        static void Init()
        {
            EditorWindow.GetWindow(typeof(WingroveEventLogWindow));
        }

        private Vector2 m_scrollPos;
        private bool m_follow;

        private bool m_showParameters;

        void Update()
        {
            Repaint();
        }

        void OnGUI()
        {
            this.maxSize = new Vector2(400, 2000);
            this.minSize = new Vector3(400, 100);
            if (WingroveRoot.Instance != null)
            {
                GUILayout.BeginHorizontal();
                m_showParameters = GUILayout.Toggle(m_showParameters, "Show Parameters");
                GUILayout.EndHorizontal();
                if (m_showParameters)
                {
                    Dictionary<int, WingroveRoot.CachedParameterValue> all = WingroveRoot.Instance.GetAllParams();
                    foreach(KeyValuePair<int, WingroveRoot.CachedParameterValue> cpv in all)
                    {
                        
                        

                        if(cpv.Value.m_isGlobalValue)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(WingroveRoot.Instance.GetParamName(cpv.Key));
                            GUILayout.Label(string.Format("{0:0.00}", cpv.Value.m_valueNull));
                            GUILayout.EndHorizontal();
                        }
                        else
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(WingroveRoot.Instance.GetParamName(cpv.Key));
                            foreach (KeyValuePair<int, float> dn in cpv.Value.m_valueObject)
                            {
                                GameObject ro = cpv.Value.m_nullCheckDictionary[dn.Key];
                                if(ro != null)
                                {
                                    EditorGUILayout.ObjectField(ro, typeof(GameObject), true);
                                    GUILayout.Label(string.Format("{0:0.00}", dn.Value));
                                }
                            }
                            GUILayout.EndHorizontal();
                        }
                        
                    }
                }
                else
                {
                    m_follow = EditorGUILayout.Toggle("Snap to end", m_follow);
                    List<WingroveRoot.LoggedEvent> eventList = WingroveRoot.Instance.GetLogList();
                    m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos);
                    GUILayout.BeginVertical(GUILayout.Width(400));
                    foreach (WingroveRoot.LoggedEvent loggedEvent in eventList)
                    {
                        EditorGUILayout.BeginHorizontal();
                        int minutes = Mathf.FloorToInt((float)(loggedEvent.m_time / 60.0));
                        float seconds = (float)(loggedEvent.m_time - (minutes * 60.0));
                        EditorGUILayout.LabelField(String.Format("{0:00}:{1:00.00}", minutes, seconds), GUILayout.Width(100));
                        EditorGUILayout.LabelField(loggedEvent.m_eventName, GUILayout.Width(200));
                        if (loggedEvent.m_linkedObject != null)
                        {
                            EditorGUILayout.ObjectField(loggedEvent.m_linkedObject, typeof(GameObject), true, GUILayout.Width(100));
                        }
                        else if (!string.IsNullOrEmpty(loggedEvent.m_linkedObjectName))
                        {
                            GUILayout.Label(loggedEvent.m_linkedObjectName, GUILayout.Width(100));
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                    EditorGUILayout.EndScrollView();
                    if (m_follow)
                    {
                        m_scrollPos.y = float.MaxValue;
                    }
                }
            }
        }


    }

}