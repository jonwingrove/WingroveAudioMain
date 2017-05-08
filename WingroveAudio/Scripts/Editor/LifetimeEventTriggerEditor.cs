using UnityEngine;
using System.Collections;
using UnityEditor;
namespace WingroveAudio
{
    [CustomEditor(typeof(LifetimeEventTrigger))]
    public class LifetimeEventTriggerEditor : Editor
    {

        void ShowEvent(string eventStringPropertyName, string boolName, string funcName)
        {
            
            SerializedProperty eventProperty = serializedObject.FindProperty(eventStringPropertyName);
            SerializedProperty eventFProperty = serializedObject.FindProperty(boolName);
            GUILayout.BeginVertical("box");
            GUILayout.Label("Event to fire on " + funcName);
            if (eventFProperty.boolValue == true)
            {
                if (GUILayout.Button("Remove " + funcName + " action"))
                {
                    eventFProperty.boolValue = false;
                }
                EditorGUILayout.PropertyField(eventProperty);
            }
            else
            {
                if ( GUILayout.Button("Add " + funcName + " action") )
                {
                    eventFProperty.boolValue = true;
                }
            }
            GUILayout.EndVertical();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            ShowEvent("m_startEvent", "m_fireEventOnStart", "Start()");
            ShowEvent("m_onEnableEvent", "m_fireEventOnEnable", "OnEnable()");
            ShowEvent("m_onDisableEvent", "m_fireEventOnDisable", "OnDisable()");
            ShowEvent("m_onDestroyEvent", "m_fireEventOnDestroy", "OnDestroy()");

            SerializedProperty linkProperty = serializedObject.FindProperty("m_linkToObject");
            EditorGUILayout.PropertyField(linkProperty);
            SerializedProperty dontPlay = serializedObject.FindProperty("m_dontPlayDestroyIfDisabled");
            EditorGUILayout.PropertyField(dontPlay);
            serializedObject.ApplyModifiedProperties();

        }
    }

}