using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;

namespace WingroveAudio
{
    public class BaseWingroveAudioSourceEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            SerializedProperty loopingProp = serializedObject.FindProperty("m_looping");
            GUILayout.BeginHorizontal();
            loopingProp.boolValue = GUILayout.Toggle(loopingProp.boolValue, "LOOPING", "button");
            loopingProp.boolValue = !GUILayout.Toggle(!loopingProp.boolValue, "PLAY ONCE", "button");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical("box", GUILayout.Width(150));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_clipMixVolume"), GUILayout.Width(150));
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.BeginVertical("box");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_importance"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_beatSynchronizeOnStart"));            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_preCacheCount"));
            GUILayout.EndVertical();
            GUILayout.BeginVertical("box");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_randomVariationPitchMin"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_randomVariationPitchMax"));
            GUILayout.EndVertical();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_retriggerOnSameObjectBehaviour"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_parameterCurveUpdateFrequencyBase"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_parameterCurveUpdateFrequencyOffset"));

            SerializedProperty spatialProp = serializedObject.FindProperty("m_is3DSound");

            GUILayout.BeginHorizontal();
            spatialProp.boolValue = GUILayout.Toggle(spatialProp.boolValue, "POSITIONAL SOUND (3D)", "button");
            spatialProp.boolValue = !GUILayout.Toggle(!spatialProp.boolValue, "2D SOUND", "button");
            GUILayout.EndHorizontal();

            
            if (spatialProp.boolValue)
            {
                SerializedProperty settingsProp = serializedObject.FindProperty("m_specify3DSettings");
                EditorGUILayout.PropertyField(settingsProp);
                if (WingroveRoot.InstanceEditor == null)
                {
                    EditorGUILayout.HelpBox("WingroveRoot not found!", MessageType.Error);
                }
                else
                {
                    if(WingroveRoot.InstanceEditor.GetDefault3DSettings() == null && settingsProp.objectReferenceValue == null)
                    {
                        EditorGUILayout.HelpBox("Without default 3D Settings (on WingroveRoot) or 3D Settings for this source, this audio will play as 2D", MessageType.Error);
                    }
                }
                SerializedProperty instantReject = serializedObject.FindProperty("m_instantRejectOnTooDistant");
                SerializedProperty instantRejectHalf = serializedObject.FindProperty("m_instantRejectHalfDistanceFewVoices");
                EditorGUILayout.PropertyField(instantReject);
                EditorGUILayout.PropertyField(instantRejectHalf);
            }

            
        }
    }

}