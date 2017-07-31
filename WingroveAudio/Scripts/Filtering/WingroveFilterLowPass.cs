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

        private bool m_hasEverCalculated;
        private float m_previousDt = 0;
        private float m_previousFilter;
        private float m_previousResonance;

        public override void UpdateFor(PooledAudioSource playingSource, int linkedObjectId)
        {
            if(m_cachedParameterValue == 0)
            {
                m_cachedParameterValue = WingroveRoot.Instance.GetParameterId(m_filterParameterController);
            }
            float dT = Mathf.Clamp01(WingroveRoot.Instance.GetParameterForGameObject(m_cachedParameterValue, linkedObjectId));
            float filter = m_previousFilter;
            float resonance = m_previousResonance;
            if (dT != m_previousDt || !m_hasEverCalculated)
            {
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
                m_hasEverCalculated = true;
                m_previousFilter = filter;
                m_previousResonance = resonance;
                m_previousDt = dT;
            }

            playingSource.SetLowPassFilter(filter, resonance);            
        }

    }

}