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
        private WingroveRoot.CachedParameterValue m_cachedParameterValueActual;

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
            else if(m_cachedParameterValueActual == null)
            {
                m_cachedParameterValueActual = WingroveRoot.Instance.GetParameter(m_cachedParameterValue);
            }

            float filter = m_previousFilter;
            float resonance = m_previousResonance;
            float dT = 0.0f;
            if (m_cachedParameterValueActual != null)
            {
                if(m_cachedParameterValueActual.m_isGlobalValue)
                {
                    dT = m_cachedParameterValueActual.m_valueNull;
                }
                else
                {
                    m_cachedParameterValueActual.m_valueObject.TryGetValue(linkedObjectId, out dT);
                }
            }

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