using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PooledAudioSource : MonoBehaviour {

    private AudioLowPassFilter m_lowPassFilter;
    private AudioHighPassFilter m_highPassFilter;
    private AudioReverbFilter m_reverbFilter;

    private int m_numLowPassFilters = 0;
    private float m_lowPassResTotal;
    private float m_lowPassFreq;

    private int m_numHighPassFilters = 0;
    private float m_highPassResTotal;
    private float m_highPassFreq;

    private bool m_previouslyWasEnabledHP = false;
    private bool m_previouslyWasEnabledLP = false;

    private float m_previousLowFreq = 999999;
    private float m_previousHighFreq = 999999;
    private float m_previousLowQ = 99999;
    private float m_previousHighQ = 99999;

    void Awake()
    {
        m_lowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();
        m_lowPassFilter.enabled = false;
        m_highPassFilter = gameObject.AddComponent<AudioHighPassFilter>();
        m_highPassFilter.enabled = false;
    }

    public void SetLowPassFilter(float freq, float res)
    {
        // optimise out if too high...
        if (freq < 44000)
        {
            m_lowPassFreq = Mathf.Min(freq, m_lowPassFreq);
            m_lowPassResTotal += res;
            m_numLowPassFilters++;
        }
    }

    public void SetHighPassFilter(float freq, float res)
    {
        // optimise out if too low...
        if (freq > 100)
        {
            m_highPassFreq = Mathf.Max(freq, m_highPassFreq);
            m_highPassResTotal += res;
            m_numHighPassFilters++;
        }
    }
    
    public void ResetFiltersForFrame()
    {
        m_lowPassFreq = 96000;
        m_numLowPassFilters = 0;
        m_lowPassResTotal = 1;

        m_highPassFreq = 0;
        m_highPassResTotal = 1;
        m_numHighPassFilters = 0;
    }

    public void CommitFiltersForFrame()
    {
        if(m_numLowPassFilters == 0)
        {
            if (m_previouslyWasEnabledLP)
            {
                m_lowPassFilter.enabled = false;
            }
            m_previouslyWasEnabledLP = false;
        }
        else
        {
            if (!m_previouslyWasEnabledLP)
            {
                m_previouslyWasEnabledLP = true;
            }
            m_lowPassFilter.enabled = true;
            if (m_previousLowFreq != m_lowPassFreq)
            {
                m_lowPassFilter.cutoffFrequency = m_lowPassFreq;
            }
            m_previousLowFreq = m_lowPassFreq;
            float lowQ = m_lowPassResTotal / m_numLowPassFilters;
            if (m_previousLowQ != lowQ)
            {
                m_lowPassFilter.lowpassResonanceQ = lowQ;
            }
            m_previousLowQ = lowQ;

        }

        if (m_numHighPassFilters == 0)
        {
            if (m_previouslyWasEnabledHP)
            {
                m_highPassFilter.enabled = false;
            }
            m_previouslyWasEnabledHP = false;
        }
        else
        {
            if (!m_previouslyWasEnabledHP)
            {
                m_highPassFilter.enabled = true;
            }
            m_previouslyWasEnabledHP = true;
            if (m_previousHighFreq != m_highPassFreq)
            {
                m_highPassFilter.cutoffFrequency = m_highPassFreq;
            }
            m_previousHighFreq = m_highPassFreq;
            float highQ = m_highPassResTotal / m_numHighPassFilters;
            if (m_previousHighQ != highQ)
            {
                m_highPassFilter.highpassResonanceQ = highQ;
            }
            m_previousHighQ = highQ;
        }
    }
}
