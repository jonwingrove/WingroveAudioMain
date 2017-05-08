using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;

namespace WingroveAudio
{
    [CustomEditor(typeof(WingroveAudioSource))]
    public class WingroveAudioSourceEditor : BaseWingroveAudioSourceEditor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty audioClip = serializedObject.FindProperty("m_audioClip");
            if(audioClip.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("No audio clip defined!", MessageType.Error);
            }
            EditorGUILayout.PropertyField(audioClip);

            base.OnInspectorGUI();
            serializedObject.ApplyModifiedProperties();
        }
    }

}