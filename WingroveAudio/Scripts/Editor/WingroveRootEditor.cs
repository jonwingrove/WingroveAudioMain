using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace WingroveAudio
{
    [CustomEditor(typeof(WingroveRoot))]
    public class WingroveRootEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.Label("Settings");
            GUILayout.BeginVertical("box");

            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_useDecibelScale"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_allowMultipleListeners"), new GUIContent("Multiple listener (split scren) experimental support"));

            if(serializedObject.FindProperty("m_allowMultipleListeners").boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_listeningModel"), new GUIContent("Multiple listener model"));
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_dontDestroyOnLoad"));
           
            GUILayout.EndVertical();

            GUILayout.Label("Audio Name Groups");
            GUILayout.BeginVertical("box");

            SerializedProperty AudioNameGroups = serializedObject.FindProperty("m_audioNameGroups");
            if(AudioNameGroups.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No AudioNameGroups set up! Create or add one!", MessageType.Error);
            }
            else
            {
                for(int index = 0; index < AudioNameGroups.arraySize; ++index)
                {
                    GUILayout.BeginHorizontal("box");
                    Object foundObject = AudioNameGroups.GetArrayElementAtIndex(index).objectReferenceValue;
                    if (foundObject != null)
                    {
                        AudioNameGroups.GetArrayElementAtIndex(index).objectReferenceValue = EditorGUILayout.ObjectField(foundObject, typeof(AudioNameGroup), false);
                        GUILayout.Label(((AudioNameGroup)foundObject).GetEvents().Length + " events");
                        if (GUILayout.Button("Remove", GUILayout.Width(64)))
                        {
                            AudioNameGroups.DeleteArrayElementAtIndex(index);
                            AudioNameGroups.DeleteArrayElementAtIndex(index);
                            break;
                        }
                    }
                    else
                    {
                        Debug.LogWarning("A null AudioNameGroup was referenced. It has been removed");
                        AudioNameGroups.DeleteArrayElementAtIndex(index);
                        break;          
                    }
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.Space(16);
            Object toAdd = EditorGUILayout.ObjectField(new GUIContent("Add AudioNameGroup"), (Object)null, typeof(AudioNameGroup), false);
            if (GUILayout.Button("Create AudioNameGroup"))
            {
                toAdd = EditorUtilities.CreateRootAsset<AudioNameGroup>();
            }

            if (toAdd != null)
            {
                int insertIndex = AudioNameGroups.arraySize;
                AudioNameGroups.InsertArrayElementAtIndex(insertIndex);
                AudioNameGroups.GetArrayElementAtIndex(insertIndex).objectReferenceValue = toAdd;
            }


            GUILayout.EndVertical();

            GUILayout.Label("Default 3D Settings");

            GUILayout.BeginVertical("box");

            SerializedProperty audioSettingsProp = serializedObject.FindProperty("m_default3DAudioSettings");

            EditorGUILayout.PropertyField(audioSettingsProp);
            if(audioSettingsProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("No Default 3D Settings selected! Audio will be 2D unless settings are specified per-component", MessageType.Warning);
            }

            GUILayout.EndVertical();

            GUILayout.Label("Advanced");
            GUILayout.BeginVertical("box");
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_audioSourcePoolSize"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_calculateRMSIntervalFrames"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_useDSPTime"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_listenerOffset"));
            
            GUILayout.EndVertical();

            GUILayout.Label("Debug & Audition");
            GUILayout.BeginVertical("box");
            

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_debugGUI"), new GUIContent("Show Debug GUI in game"));
            if(GUILayout.Button("Show Mixer Window"))
            {
                EditorWindow.GetWindow(typeof(WingroveMixerWindow));
            }
            if(GUILayout.Button("Show Event Log Window"))
            {
                EditorWindow.GetWindow(typeof(WingroveEventLogWindow));
            }
            if (GUILayout.Button("Show Auditioning Window"))
            {
                EditorWindow.GetWindow(typeof(WingroveAuditioningGrid));
            }

            GUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
