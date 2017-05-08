using UnityEngine;
using System.Collections;
using UnityEditor;
namespace WingroveAudio
{
    [CustomEditor(typeof(DuckMixBusOnEvent))]
    public class DuckOnEventEditor : Editor
    {

        void ShowEvent(string eventStringPropertyName)
        {
            SerializedProperty eventProperty = serializedObject.FindProperty(eventStringPropertyName);
            EditorGUILayout.PropertyField(eventProperty);
        }


        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty volumeProperty = serializedObject.FindProperty("m_duckingMixAmount");
            GUILayout.BeginHorizontal("box");

            EditorGUILayout.PropertyField(volumeProperty);

            GUILayout.BeginVertical(GUILayout.Width(100));

            SerializedProperty attackProp = serializedObject.FindProperty("m_attack");
            attackProp.floatValue = EditorGUILayout.FloatField("Attack", attackProp.floatValue);
            SerializedProperty releaseProp = serializedObject.FindProperty("m_release");
            releaseProp.floatValue = EditorGUILayout.FloatField("Release", releaseProp.floatValue);

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            ShowEvent("m_startDuckEvent");
            ShowEvent("m_endDuckEvent");

            serializedObject.ApplyModifiedProperties();

        }
    }

}