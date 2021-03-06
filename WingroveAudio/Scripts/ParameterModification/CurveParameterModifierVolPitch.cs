using UnityEngine;
using System.Collections;

namespace WingroveAudio
{
    [AddComponentMenu("WingroveAudio/Audio Source Modifiers/Pitch and Volume Curve")]
    public class CurveParameterModifierVolPitch : ParameterModifierBase
    {

        [AudioParameterName]
        public string m_parameter;

        public AnimationCurve m_volumeCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 1.0f), new Keyframe(1, 1.0f) });
        public AnimationCurve m_pitchCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 1.0f), new Keyframe(1, 1.0f) });

        [SerializeField]
        private bool m_doVolumeCurve = true;
        [SerializeField]
        private bool m_doPitchCurve = true;
        [SerializeField]
        private bool m_optimiseForGlobal = false;

        private int m_cachedParameter = 0;
        private WingroveRoot.CachedParameterValue m_cachedParameterValue;
        private bool m_hasCachedGlobalVol = false;
        private System.Int64 m_lastCachedNVol = 0;

        private bool m_hasCachedGlobalPitch = false;
        private System.Int64 m_lastCachedNPitch = 0;

        private float m_lastCachedNullParamValVol = 0;
        private float m_lastCachedNullParamValPitch = 0;

        private float m_lastCachedVolResult = 1.0f;
        private float m_lastCachedPitchResult = 1.0f;

        private float m_cachedZeroVol = 0.0f;
        private float m_cachedZeroPitch = 0.0f;

        private void Awake()
        {
            m_cachedZeroVol = m_volumeCurve.Evaluate(0.0f);
            m_cachedZeroPitch = m_pitchCurve.Evaluate(0.0f);
        }

        public override float GetVolumeMultiplier(int linkedObjectId)
        {
            if(!m_doVolumeCurve)
            {
                return 1.0f;
            }
            if(m_cachedParameter == 0)
            {
                m_cachedParameter = WingroveRoot.Instance.GetParameterId(m_parameter);
            }
            if(m_cachedParameterValue == null)
            {
                m_cachedParameterValue = WingroveRoot.Instance.GetParameter(m_cachedParameter);
            }
            if(m_cachedParameterValue != null)
            {
                if(m_cachedParameterValue.m_isGlobalValue)
                {
                    if (m_lastCachedNullParamValVol != m_cachedParameterValue.m_valueNull || !m_hasCachedGlobalVol)
                    {                                               
                        m_lastCachedVolResult = m_volumeCurve.Evaluate(m_cachedParameterValue.m_valueNull);
                        m_hasCachedGlobalVol = true;
                        m_lastCachedNullParamValVol = m_cachedParameterValue.m_valueNull;
                    }
                    return m_lastCachedVolResult;
                }
                else
                {
                    if(m_optimiseForGlobal)
                    {
                        Debug.LogError("[WINGROVEAUDIO] Parameter marked as optimise for global, but using non-global variable");
                    }
                    float val = 0.0f;
                    m_cachedParameterValue.m_valueObject.TryGetValue(linkedObjectId, out val);
                    return m_volumeCurve.Evaluate(val);
                }
            }
            else
            {
                return m_cachedZeroVol;
            }
        }

        public override bool IsGlobalOptimised()
        {
            return m_optimiseForGlobal;
        }

        public override float GetPitchMultiplier(int linkedObjectId)
        {
            if(!m_doPitchCurve)
            {
                return 1.0f;
            }
            if (m_cachedParameter == 0)
            {
                m_cachedParameter = WingroveRoot.Instance.GetParameterId(m_parameter);
            }
            if (m_cachedParameterValue == null)
            {
                m_cachedParameterValue = WingroveRoot.Instance.GetParameter(m_cachedParameter);
            }
            if (m_cachedParameterValue != null)
            {
                if (m_cachedParameterValue.m_isGlobalValue)
                {
                    if (m_lastCachedNullParamValPitch != m_cachedParameterValue.m_valueNull || !m_hasCachedGlobalPitch)
                    {
                        m_lastCachedPitchResult = m_pitchCurve.Evaluate(m_cachedParameterValue.m_valueNull);
                        m_hasCachedGlobalPitch = true;
                        m_lastCachedNullParamValPitch = m_cachedParameterValue.m_valueNull;
                    }
                    return m_lastCachedPitchResult;
                }
                else
                {
                    if (m_optimiseForGlobal)
                    {
                        Debug.LogError("[WINGROVEAUDIO] Parameter marked as optimise for global, but using non-global variable");
                    }
                    float val = 0.0f;
                    m_cachedParameterValue.m_valueObject.TryGetValue(linkedObjectId, out val);
                    return m_pitchCurve.Evaluate(val);
                }
            }
            else
            {
                return m_cachedZeroPitch;
            }
        }
    }

}