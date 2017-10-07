using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace WingroveAudio
{

    public class EditorUtilities
    {


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
       
    }

}