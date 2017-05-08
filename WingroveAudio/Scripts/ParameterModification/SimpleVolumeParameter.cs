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

        public override float GetVolumeMultiplier(GameObject linkedObject)
        {
            float parameter = WingroveRoot.Instance.GetParameterForGameObject(m_volumeParameter, linkedObject);

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