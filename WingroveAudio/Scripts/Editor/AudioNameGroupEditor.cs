using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace WingroveAudio
{
    [CustomEditor(typeof(AudioNameGroup))]
    public class AudioNameGroupEditor : Editor
    {

        string m_eventToAdd = "";
        string m_parameterToAdd = "";
        Vector2 m_eventScrollPosition;
        Vector2 m_parameterScrollPosition;
        bool m_showEvents = true;
        bool m_showParameters = true;

        void DoNameArray(SerializedProperty sp, string typeName, bool isInScrollView)
        {
            int indexToDelete = -1;
            bool alternate = false;
            if (sp.arraySize != 0)
            {
                for (int index = 0; index < sp.arraySize; ++index)
                {
                    if (alternate)
                    {
                        GUI.backgroundColor = new Color(0.8f, 1.0f, 0.8f);
                    }
                    else
                    {
                        GUI.backgroundColor = new Color(0.8f, 0.8f, 1.0f);
                    }

                    SerializedProperty str = sp.GetArrayElementAtIndex(index);
                    GUILayout.BeginHorizontal();
                    str.stringValue = EditorGUILayout.TextField(str.stringValue);
                    if (GUILayout.Button("Remove"))
                    {
                        indexToDelete = index;
                    }
                    GUILayout.EndHorizontal();
                    alternate = !alternate;

                }
            }
            GUI.backgroundColor = Color.white;
            if (indexToDelete != -1)
            {
                sp.DeleteArrayElementAtIndex(indexToDelete);
            }

            if (isInScrollView)
            {
                GUILayout.EndScrollView();
            }

            GUILayout.Label("Add new " + typeName + ":");
            GUILayout.BeginHorizontal();

            if (typeName == "event")
            {

                m_eventToAdd = GUILayout.TextField(m_eventToAdd);
                if (GUILayout.Button("Add"))
                {
                    sp.arraySize++;
                    SerializedProperty newEvent = sp.GetArrayElementAtIndex(sp.arraySize - 1);
                    newEvent.stringValue = m_eventToAdd;
                    m_eventToAdd = "";
                }
            }
            else
            {
                m_parameterToAdd = GUILayout.TextField(m_parameterToAdd);
                if (GUILayout.Button("Add"))
                {
                    sp.arraySize++;
                    SerializedProperty newEvent = sp.GetArrayElementAtIndex(sp.arraySize - 1);
                    newEvent.stringValue = m_parameterToAdd;
                    m_parameterToAdd = "";
                }
            }
            GUILayout.EndHorizontal();

        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (GUILayout.Button("Export to C# strings"))
            {
                string textToWrite = ((AudioNameGroup)target).GenerateStaticCSharp();

                string assetPath = AssetDatabase.GetAssetPath(target);
                string appPath = Application.dataPath.Substring(0,
                    Application.dataPath.Length - "/Assets".Length);
                string newPath = Path.ChangeExtension(assetPath, ".cs");
                string combined = Path.Combine(appPath, newPath);

                if (File.Exists(combined))
                {
                    FileInfo fi = new FileInfo(combined);
                    fi.IsReadOnly = false;
                    Object o = AssetDatabase.LoadAssetAtPath(newPath, typeof(MonoScript));

                    File.WriteAllText(combined, textToWrite);

                    EditorUtility.SetDirty(o);
                }
                else
                {
                    MonoScript ms = new MonoScript();
                    AssetDatabase.CreateAsset(ms, newPath);

                    File.WriteAllText(combined, textToWrite);

                    EditorUtility.SetDirty(ms);
                }

                AssetDatabase.ImportAsset(newPath);

            }


            SerializedProperty spEvents = serializedObject.FindProperty("m_events");
            SerializedProperty spParameters = serializedObject.FindProperty("m_parameters");

            GUILayout.BeginVertical("box");
            m_showEvents = EditorGUILayout.Foldout(m_showEvents, "EVENT NAMES (" + spEvents.arraySize + " events)");
            if (m_showEvents)
            {
                m_eventScrollPosition = GUILayout.BeginScrollView(m_eventScrollPosition);

                DoNameArray(spEvents, "event", true);
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            m_showParameters = EditorGUILayout.Foldout(m_showParameters, "PARAMETER NAMES (" + spParameters.arraySize + " parameters)");
            if (m_showParameters)
            {
                m_parameterScrollPosition = GUILayout.BeginScrollView(m_parameterScrollPosition);

                DoNameArray(spParameters, "parameter", true);
            }
            GUILayout.EndVertical();

            if (GUILayout.Button("Sort alphabetically"))
            {
                List<string> eventList = new List<string>();
                for (int index = 0; index < spEvents.arraySize; ++index)
                {
                    SerializedProperty str = spEvents.GetArrayElementAtIndex(index);
                    eventList.Add(str.stringValue);
                }

                eventList.Sort();
                for (int index = 0; index < spEvents.arraySize; ++index)
                {
                    SerializedProperty str = spEvents.GetArrayElementAtIndex(index);
                    str.stringValue = eventList[index];
                }

                List<string> paramList = new List<string>();
                for (int index = 0; index < spParameters.arraySize; ++index)
                {
                    SerializedProperty str = spParameters.GetArrayElementAtIndex(index);
                    paramList.Add(str.stringValue);
                }

                paramList.Sort();
                for (int index = 0; index < spParameters.arraySize; ++index)
                {
                    SerializedProperty str = spParameters.GetArrayElementAtIndex(index);
                    str.stringValue = paramList[index];
                }
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
                serializedObject.ApplyModifiedProperties();
            }




        }
    }

}