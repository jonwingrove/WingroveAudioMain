using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace WingroveAudio
{

    [CustomPropertyDrawer(typeof(FaderInterfaceAttribute))]
    public class FaderInterfaceAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            FaderInterfaceAttribute att = (FaderInterfaceAttribute)attribute;

            if(!att.IsHorizontal())
            {
                Rect toFill = new Rect(position);                
                toFill.height = 130;

                float amt = property.floatValue;
                
                GUISkin oldSkin = GUI.skin;
                Rect topRect = new Rect(position);
                GUI.Label(topRect, label);
                GUI.skin = WingroveRoot.Instance == null ? oldSkin : WingroveRoot.Instance.GetSkin();

                Rect centre = new Rect(toFill);
                centre.xMin += 30;
                centre.width -= 60;
                centre.yMin += 16;
                centre.height = 100;

                amt = Mathf.Pow(GUI.VerticalSlider(centre, Mathf.Sqrt(amt), 1, 0), 2);
                
                GUI.skin = oldSkin;

                toFill.yMin += 116;

                if (WingroveRoot.Instance == null || WingroveRoot.Instance.UseDBScale)
                {
                    float dbMix = 20 * Mathf.Log10(amt);
                    if (dbMix == 0)
                    {
                        GUI.Label(toFill, "-0.00 dB");
                    }
                    else if (float.IsInfinity(dbMix))
                    {
                        GUI.Label(toFill, "-inf dB");
                    }
                    else
                    {
                        GUI.Label(toFill, System.String.Format("{0:0.00}", dbMix) + " dB");
                    }
                }
                else
                {
                    GUI.Label(toFill, amt * 100.0f + "%");
                }

                property.floatValue = amt;        
            }
            else
            {
                Rect pos = position;
                float width = pos.width / 3;
                pos.width = width;
                GUI.Label(pos, label);
                pos.xMax += width;
                pos.xMin += width;
                

                float amt = property.floatValue;
                amt = Mathf.Pow(1-GUI.HorizontalSlider(pos, 1-Mathf.Sqrt(amt), 1, 0), 2);

                pos.xMax += width;
                pos.xMin += width;

                if (WingroveRoot.Instance == null || WingroveRoot.Instance.UseDBScale)
                {
                    float dbMix = 20 * Mathf.Log10(amt);
                    if (dbMix == 0)
                    {
                        GUI.Label(pos, "-0.00 dB");
                    }
                    else if (float.IsInfinity(dbMix))
                    {
                        GUI.Label(pos, "-inf dB");
                    }
                    else
                    {
                        GUI.Label(pos, System.String.Format("{0:0.00}", dbMix) + " dB");
                    }
                }
                else
                {
                    GUI.Label(pos, amt * 100.0f + "%");
                }

                property.floatValue = amt;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            FaderInterfaceAttribute att = (FaderInterfaceAttribute)attribute;

            if (!att.IsHorizontal())
            {
                return 140;
            }
            else
            {
                return EditorGUIUtility.singleLineHeight;
            }
        }
    }

}