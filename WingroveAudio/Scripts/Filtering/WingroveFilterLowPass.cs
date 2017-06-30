using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WingroveAudio
{

    public class WingroveFilterLowPass : FilterApplicationBase {

        [SerializeField]
        [AudioParameterName]
        private string m_filterParameterController;
        [SerializeField]
        private float m_filterLowCutParameterAtZero = 5000;
        [SerializeField]
        private float m_filterLowCutParameterAtOne = 5000;
        [SerializeField]
        private float m_resonanceAtZero = 1;
        [SerializeField]
        private float m_resonanceAtOne = 1;
        [SerializeField]
        private bool m_smoothStep = true;


        private int m_cachedParameterValue = 0;

        public override void UpdateFor(PooledAudioSource playingSource, int linkedObjectId)
        {
            if(m_cachedParameterValue == 0)
            {
                m_cachedParameterValue = WingroveRoot.Instance.GetParameterId(m_filterParameterController);
            }
            float dT = Mathf.Clamp01(WingroveRoot.Instance.GetParameterForGameObject(m_cachedParameterValue, linkedObjectId));
            float filter = 0;
            float resonance = 0;
            if (m_smoothStep)
            {
                filter = Mathf.SmoothStep(m_filterLowCutParameterAtZero, m_filterLowCutParameterAtOne, dT);
                resonance = Mathf.SmoothStep(m_resonanceAtZero, m_resonanceAtOne, dT);
            }
            else
            {
                filter = Mathf.Lerp(m_filterLowCutParameterAtZero, m_filterLowCutParameterAtOne, dT);
                resonance = Mathf.Lerp(m_resonanceAtZero, m_resonanceAtOne, dT);
            }

            playingSource.SetLowPassFilter(filter, resonance);            
        }

    }

}