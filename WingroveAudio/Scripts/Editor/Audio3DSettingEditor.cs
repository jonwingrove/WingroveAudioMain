using UnityEngine;
using System.Collections;
using UnityEditor;

namespace WingroveAudio
{
    [CustomEditor(typeof(Audio3DSetting))]
    public class Audio3DSettingEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();


            SerializedProperty rolloffProp = serializedObject.FindProperty("m_linearRolloff");
            int newVal = EditorGUILayout.Popup(new GUIContent("Rolloff Mode"), rolloffProp.boolValue == true ? 0 : 1, new GUIContent[] { new GUIContent("Linear"), new GUIContent("Logarithmic") });
            rolloffProp.boolValue = newVal == 0 ? true : false;
            
            SerializedProperty maxDistProp = serializedObject.FindProperty("m_maxDistance");
            SerializedProperty minDistProp = serializedObject.FindProperty("m_minDistance");            
            EditorGUILayout.PropertyField(minDistProp, new GUIContent("Min Dist (dist for full volume"));
            EditorGUILayout.PropertyField(maxDistProp, new GUIContent("Max Dist (inaudible past this distance"));

            SerializedProperty dopplerProp = serializedObject.FindProperty("m_dopplerMultiplier");
            EditorGUILayout.PropertyField(dopplerProp);

            SerializedProperty dynaBlend = serializedObject.FindProperty("m_useDynamicSpatialBlend");
            dynaBlend.boolValue = GUILayout.Toggle(dynaBlend.boolValue, "Use Dynamic Spatial Blend", "button");
            if (dynaBlend.boolValue == true)
            {
                SerializedProperty blendNear = serializedObject.FindProperty("m_blendValueNear");
                SerializedProperty blendFar = serializedObject.FindProperty("m_blendValueFar");
                SerializedProperty distNear = serializedObject.FindProperty("m_blendNearDistance");
                SerializedProperty distFar = serializedObject.FindProperty("m_blendFarDistance");

                EditorGUILayout.PropertyField(blendNear);
                EditorGUILayout.PropertyField(blendFar);
                EditorGUILayout.PropertyField(distNear);
                EditorGUILayout.PropertyField(distFar);

                GUILayout.Label("At " + distNear.floatValue + " and under, spatial blend is " + blendNear.floatValue);
                GUILayout.Label("At " + distFar.floatValue + " and over, spatial blend is " + blendFar.floatValue);
                GUILayout.Label("It will be blended between distances of " + distNear.floatValue + " and " + distFar.floatValue);                
            }
            else
            {
                SerializedProperty blendNear = serializedObject.FindProperty("m_blendValueNear");
                EditorGUILayout.PropertyField(blendNear, new GUIContent("Spatial blend mix"));
            }

            GUILayout.Label("Spatial blend: 1==fully 3d, 0==fully 2d (no positioning)");

            serializedObject.ApplyModifiedProperties();
        }
    }

}