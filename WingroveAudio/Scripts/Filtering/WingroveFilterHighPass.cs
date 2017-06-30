using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WingroveAudio
{

    public class WingroveFilterHighPass : FilterApplicationBase {

        [SerializeField]
        [AudioParameterName]
        private string m_filterParameterController;
        [SerializeField]
        private float m_filterHighCutParameterAtZero = 5000;
        [SerializeField]
        private float m_filterHighCutParameterAtOne = 5000;
        [SerializeField]
        private float m_resonanceAtZero = 1;
        [SerializeField]
        private float m_resonanceAtOne = 1;
        [SerializeField]
        private bool m_smoothStep = true;

        private int m_cachedParameterId;

        public override void UpdateFor(PooledAudioSource playingSource, int linkedObjectId)
        {
            if(m_cachedParameterId == 0)
            {
                m_cachedParameterId = WingroveRoot.Instance.GetParameterId(m_filterParameterController);
            }
            float dT = Mathf.Clamp01(WingroveRoot.Instance.GetParameterForGameObject(m_cachedParameterId, linkedObjectId));
            float filter = 0;
            float resonance = 0;
            if (m_smoothStep)
            {
                filter = Mathf.SmoothStep(m_filterHighCutParameterAtZero, m_filterHighCutParameterAtOne, dT);
                resonance = Mathf.SmoothStep(m_resonanceAtZero, m_resonanceAtOne, dT);
            }
            else
            {
                filter = Mathf.Lerp(m_filterHighCutParameterAtZero, m_filterHighCutParameterAtOne, dT);
                resonance = Mathf.Lerp(m_resonanceAtZero, m_resonanceAtOne, dT);
            }

            playingSource.SetHighPassFilter(filter, resonance);            
        }

    }

}