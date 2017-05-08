using UnityEngine;
using System.Collections;
using UnityEditor;
namespace WingroveAudio
{
    [CustomEditor(typeof(NGUIClickEvents))]
    public class NGUIClickEventsEditor : Editor
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
            ShowEvent("m_onPressEvent", "m_fireEventOnPress", "OnPress(true)");
            ShowEvent("m_onReleaseEvent", "m_fireEventOnRelease", "OnPress(false)");
            ShowEvent("m_onClickEvent", "m_fireEventOnClick", "OnClick()");
            ShowEvent("m_onDoubleClickEvent", "m_fireEventOnDoubleClick", "OnDoubleClick()");

            serializedObject.ApplyModifiedProperties();

        }
    }

}