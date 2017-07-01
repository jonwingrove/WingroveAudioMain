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

            WingroveAudioSource was = (WingroveAudioSource)target;
            if (was.GetActiveCuesDebug() != null)
            {
                foreach (ActiveCue ac in was.GetActiveCuesDebug())
                {
                    string asS = ac.GetCurrentAudioSource() == null ? "NULL" : ac.GetCurrentAudioSource().m_audioSource.name;
                    EditorGUILayout.LabelField("Active Cue: time: " + ac.GetTime() + " state: " + ac.GetState() + " fade: " + ac.GetFadeT()  + " " + asS + " " + ac.GetTheoreticalVolumeCached());
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

}