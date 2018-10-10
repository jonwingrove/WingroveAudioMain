using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace WingroveAudio
{

    public class EditorUtilities
    {
        private static AudioNameGroup[] m_audioNameGroups;

        public static void DBLabel(string prefix, float amt)
        {
            if (WingroveRoot.InstanceEditor.UseDBScale)
            {
                float dbMix = 20 * Mathf.Log10(amt);
                if (dbMix == 0)
                {
                    GUILayout.Label(prefix + "-0.00 dB");
                }
                else if (float.IsInfinity(dbMix))
                {
                    GUILayout.Label(prefix + "-inf dB");
                }
                else
                {
                    GUILayout.Label(prefix + System.String.Format("{0:0.00}", dbMix) + " dB");
                }
            }
            else
            {
                GUILayout.Label(amt * 100.0f + "%");
            }
        }        

        [MenuItem("Assets/Create/WingroveAudio/Audio Name Group")]
        public static void CreateAudioNameGroup()
        {
            CreateAsset<AudioNameGroup>();
        }

        [MenuItem("Assets/Create/WingroveAudio/3D Settings Group")]
        public static void Create3DSettings()
        {
            CreateAsset<Audio3DSetting>();
        }

        /// <summary>
        //	This makes it easy to create, name and place unique new ScriptableObject asset files.
        /// </summary>
        public static void CreateAsset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();

            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (path == "")
            {
                path = "Assets";
            }
            else if (Path.GetExtension(path) != "")
            {
                path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
            }

            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/New " + typeof(T).ToString() + ".asset");

            AssetDatabase.CreateAsset(asset, assetPathAndName);

            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }

        /// <summary>
        //	This makes it easy to create, name and place unique new ScriptableObject asset files.
        /// </summary>
        public static T CreateRootAsset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();

            string path = "Assets";

            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/New " + typeof(T).ToString() + ".asset");

            AssetDatabase.CreateAsset(asset, assetPathAndName);

            AssetDatabase.SaveAssets();

            return asset;
        }

        [MenuItem("Wingrove Audio/Refresh Audio Name Groups")]
        static void RefreshGroups()
        {
            List<AudioNameGroup> allGroups = new List<AudioNameGroup>();
            string[] allAssets = AssetDatabase.FindAssets("t:AudioNameGroup");
            foreach(string s in allAssets)
            {
                AudioNameGroup ang = AssetDatabase.LoadAssetAtPath<AudioNameGroup>(AssetDatabase.GUIDToAssetPath(s));
                if(ang != null)
                {
                    allGroups.Add(ang);
                }
            }
            m_audioNameGroups = allGroups.ToArray();
        }

        public static int FindEvent(string eventName)
        {
            int index = 0;
            foreach (AudioNameGroup eg in m_audioNameGroups)
            {
                if (eg != null && eg.GetEvents() != null)
                {
                    foreach (string st in eg.GetEvents())
                    {
                        if (st == eventName)
                        {
                            return index;
                        }
                    }
                }
                ++index;
            }
            return -1;
        }

        public static int FindParameter(string parameterName)
        {
            int index = 0;
            foreach (AudioNameGroup eg in m_audioNameGroups)
            {
                if (eg != null && eg.GetParameters() != null)
                {
                    foreach (string st in eg.GetParameters())
                    {
                        if (st == parameterName)
                        {
                            return index;
                        }
                    }
                }
                ++index;
            }
            return -1;
        }
        public static AudioNameGroup[] GetAudioNameGroups()
        {
            if(m_audioNameGroups == null)
            {
                RefreshGroups();
            }
            return m_audioNameGroups;
        }
    }

}