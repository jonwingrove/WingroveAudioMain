using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WingroveAudio
{

    public class SimpleVolumeParameter : ParameterModifierBase
    {
        [SerializeField]
        [AudioParameterName]
        private string m_volumeParameter;
        [SerializeField]
        [FaderInterface(true)]
        private float m_maxGainReduction = 1;
        [SerializeField]
        private bool m_smooth;
        [SerializeField]
        private bool m_logarithmicFade;
        [SerializeField]
        private bool m_parameterHighMeansReduceVolume;

        private int m_cachedParameterId;

        public override float GetVolumeMultiplier(int linkedObjectId)
        {
            if(m_cachedParameterId == 0)
            {
                m_cachedParameterId = WingroveRoot.Instance.GetParameterId(m_volumeParameter);
            }
            float parameter = WingroveRoot.Instance.GetParameterForGameObject(m_cachedParameterId, linkedObjectId);

            if(m_parameterHighMeansReduceVolume)
            {
                parameter = 1 - parameter;
            }

            parameter = Mathf.Clamp01(parameter);
            if(m_logarithmicFade)
            {
                parameter = Mathf.Pow(parameter, 2);
            }
            
            if (!m_smooth)
            {
                return Mathf.Lerp(m_maxGainReduction, 1.0f, parameter);
            }
            else
            {
                return Mathf.SmoothStep(m_maxGainReduction, 1.0f, parameter);
            }
        }

    }

}