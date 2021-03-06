using UnityEngine;
using System.Collections;
using UnityEditor;
namespace WingroveAudio
{
    [CustomEditor(typeof(NGUIHoverEvents))]
    public class NGUIHoverEventsEditor : Editor
    {
        int m_hoverIndex = -1;
        int m_unhoverIndex = -1;

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
            ShowEvent("m_hoverEvent", "m_fireEventOnHover", "OnHover(true)");
            ShowEvent("m_stopHoverEvent", "m_fireEventOnStopHover", "OnHover(false)");

            serializedObject.ApplyModifiedProperties();

        }
    }

}