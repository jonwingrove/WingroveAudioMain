using UnityEngine;
using System.Collections;

namespace WingroveAudio
{

    public class ActiveCue
    {
        public enum CueState
        {
            Initial,
            PlayingFadeIn,
            Playing,
            PlayingFadeOut,
            Stopped
        }

        private GameObject m_originatorSource;
        private BaseWingroveAudioSource m_audioClipSource;
        private GameObject m_targetGameObject;
        private int m_targetGameObjectId;
        private AudioArea m_targetAudioArea;
        private double m_dspStartTime = 0.0f;
        private bool m_hasDSPStartTime = false;
        private Audio3DSetting m_audioSettings;
        private bool m_hasAudioSettings;
        
        private float m_fadeT;
        private float m_fadeSpeed;
        private CueState m_currentState;

        private bool m_isPaused;
        private int m_currentPosition;

        private float m_pitch;
        
        private WingroveRoot.AudioSourcePoolItem m_currentAudioSource;
        private bool m_currentAudioSourceExists;
        
        float[] m_bufferDataL;
        float[] m_bufferDataR;
        
        private float m_rms;
        
        private bool m_rmsRequested;
        private int m_framesAtZero = 0;
        private Vector3 m_audioPositioning;
        private float m_audioPositioningDistanceSqr;
        private float m_theoreticalVolumeCached;
        private int m_importanceCached;

        public void Initialise(GameObject originator, BaseWingroveAudioSource bas, GameObject target)
        {
            m_rms = 0;
            m_rmsRequested = false;
            m_framesAtZero = 0;
            m_currentState = CueState.Initial;
            m_hasDSPStartTime = false;
            m_fadeT = 0.0f;
            m_fadeSpeed = 0.0f;
            m_currentAudioSource = null;
            m_currentAudioSourceExists = false;
            m_currentPosition = 0;

            m_originatorSource = originator;
            m_targetGameObject = target;
            if (m_targetGameObject != null)
            {
                m_targetAudioArea = target.GetComponent<AudioArea>();
                m_targetGameObjectId = target.GetInstanceID();
            }
            m_audioClipSource = bas;
            m_pitch = m_audioClipSource.GetNewPitch();
            m_audioClipSource.AddUsage();
            m_importanceCached = m_audioClipSource.GetImportance();

            m_audioSettings = m_audioClipSource.Get3DSettings();
            if (m_audioSettings != null)
            {
                m_hasAudioSettings = true;
            }

            //transform.parent = m_originatorSource.transform;
            Update();            
        }       

        public GameObject GetOriginatorSource()
        {
            return m_originatorSource;
        }

        public GameObject GetTargetObject()
        {
            return m_targetGameObject;
        }

        public int GetTargetObjectId()
        {
            return m_targetGameObjectId;
        }

        public int GetImportance()
        {
            return m_importanceCached;
        }


        public void Update()
        {
            if (m_currentState == CueState.Stopped)
            {
                return;
            }

            m_theoreticalVolumeCached = GetTheoreticalVolume();

            bool queueEnableAndPlay = false;
            if (!m_currentAudioSourceExists)
            {                
                // don't bother stealing if we're going to be silent anyway... (optimise: don't re-get if fading out)       
                if (m_theoreticalVolumeCached > 0 && GetState() != CueState.PlayingFadeOut)
                {
                    m_currentAudioSource = WingroveRoot.Instance.TryClaimPoolSource(this);
                    if (m_currentAudioSource != null)
                    {
                        m_currentAudioSourceExists = true;
                        // turn off doppler for a frame or two...
                        m_currentAudioSource.m_audioSource.dopplerLevel = 0.0f;
                    }
                }
                if (m_currentAudioSourceExists)
                {
                    m_currentAudioSource.m_audioSource.clip = m_audioClipSource.GetAudioClip();
                    m_currentAudioSource.m_audioSource.loop = m_audioClipSource.GetLooping();
                    if (!m_isPaused)
                    {
                        queueEnableAndPlay = true;
                    }
                }
                else
                {
                    if (!m_isPaused)
                    {
                        m_currentPosition += (int)(WingroveRoot.GetDeltaTime() * m_audioClipSource.GetAudioClip().frequency * GetMixPitch());
                        if (m_currentPosition > m_audioClipSource.GetAudioClip().samples)
                        {
                            if (m_audioClipSource.GetLooping())
                            {
                                m_currentPosition -= m_audioClipSource.GetAudioClip().samples;
                            }
                            else
                            {
                                StopInternal();
                            }
                        }
                    }
                }
            }
            else
            {
                if (!m_isPaused)
                {
#if UNITY_WEBGL
                    m_currentPosition += (int)(WingroveRoot.GetDeltaTime() * m_audioClipSource.GetAudioClip().frequency * GetMixPitch());
#else
                    m_currentPosition = m_currentAudioSource.m_audioSource.timeSamples;
#endif
                }
            }

            if (!m_isPaused)
            {
                switch (m_currentState)
                {
                    case CueState.Initial:
                        break;
                    case CueState.Playing:
                        m_fadeT = 1;
                        break;
                    case CueState.PlayingFadeIn:
                        m_fadeT += m_fadeSpeed * WingroveRoot.GetDeltaTime();
                        if (m_fadeT >= 1)
                        {
                            m_fadeT = 1.0f;
                            m_currentState = CueState.Playing;
                        }
                        break;
                    case CueState.PlayingFadeOut:
                        m_fadeT -= m_fadeSpeed * WingroveRoot.GetDeltaTime();
                        if (m_fadeT <= 0)
                        {
                            m_fadeT = 0.0f;
                            StopInternal();
                            // early return!!!!
                            return;
                        }
                        break;
                    case CueState.Stopped:
                        break;
                }

                if (!m_audioClipSource.GetLooping())
                {
                    if (m_currentPosition > m_audioClipSource.GetAudioClip().samples - 1000)
                    {
                        StopInternal();
                        return;
                    }
                    if ( m_currentPosition == 0 )
                    {
                        m_framesAtZero++;
                        if ( m_framesAtZero == 100 )
                        {
                            StopInternal();
                            return;
                        }
                    }
                }
            }

            SetMix();

            if (queueEnableAndPlay)
            {
                if (m_currentAudioSourceExists)
                {
                    m_currentAudioSource.m_audioSource.timeSamples = m_currentPosition;
                    m_currentAudioSource.m_audioSource.enabled = true;
                    if (!m_hasAudioSettings)
                    {
                        if (m_currentAudioSource.m_audioSource.spatialBlend != 0.0f)
                        {
                            m_currentAudioSource.m_audioSource.spatialBlend = 0.0f;
                        }
                        if (m_currentAudioSource.m_audioSource.spatialize)
                        {
                            m_currentAudioSource.m_audioSource.spatialize = false;
                        }
                    }
                    else
                    {
                        AudioRolloffMode rolloffMode = m_audioSettings.GetRolloffMode();
                        m_currentAudioSource.m_audioSource.rolloffMode = rolloffMode;
                        m_currentAudioSource.m_audioSource.minDistance = m_audioSettings.GetMinDistance();
                        m_currentAudioSource.m_audioSource.maxDistance = m_audioSettings.GetMaxDistance();
                        if (!m_currentAudioSource.m_audioSource.spatialize)
                        {
                            m_currentAudioSource.m_audioSource.spatialize = true;
                        }
                    }

                    if ((m_hasDSPStartTime) && (m_dspStartTime > AudioSettings.dspTime))
                    {
                        m_currentAudioSource.m_audioSource.timeSamples = m_currentPosition = 0;
                        m_currentAudioSource.m_audioSource.PlayScheduled(m_dspStartTime);
                    }
                    else
                    {
                        m_currentAudioSource.m_audioSource.Play();
                        m_currentAudioSource.m_audioSource.timeSamples = m_currentPosition;
                    }
                }
            }
        }

        private float GetTheoreticalVolume()
        {
            if (m_isPaused)
            {
                return 0;
            }
            else
            {
                float v3D = 1.0f;
                if(m_hasAudioSettings)
                {
                    float distM = Mathf.Sqrt(m_audioPositioningDistanceSqr);
                    float spab = m_audioSettings.GetSpatialBlend(distM);
                    v3D = Mathf.Lerp(m_audioSettings.EvaluateStandard(distM), 1.0f, spab);
                }                
                return m_fadeT * m_audioClipSource.GetMixBusLevel() * v3D;
            }
        }

        public float GetTheoreticalVolumeCached()
        {
            return m_theoreticalVolumeCached;
        }
		
		public float GetMixPitch()
		{
			return m_pitch * m_audioClipSource.GetPitchModifier(m_targetGameObjectId);
		}

        public void UpdatePosition()
        {
            if (m_hasAudioSettings)
            {
                if (m_targetGameObject != null)
                {
                    // update audio area                    
                    m_audioPositioning = 
                        WingroveRoot.Instance.GetRelativeListeningPosition(m_targetAudioArea, m_targetGameObject.transform.position);
                    m_audioPositioningDistanceSqr = (WingroveRoot.Instance.GetSingleListener().transform.position - 
                        m_audioPositioning).sqrMagnitude;
                }
            }
        }        

        public void SetMix()
        {
            UpdatePosition();
            if (m_currentAudioSourceExists)
            {
                if (!m_hasAudioSettings)
                {
                    // no 3d settings? put at root
                    if (m_currentAudioSource.m_audioSource.spatialBlend != 0.0f)
                    {
                        m_currentAudioSource.m_audioSource.spatialBlend = 0.0f;
                    }
                    if (m_currentAudioSource.m_audioSource.spatialize)
                    {
                        m_currentAudioSource.m_audioSource.spatialize = false;
                    }
                    m_currentAudioSource.m_audioSource.transform.position = Vector3.zero;
                }
                else
                {
                    // we have 3d settings, so place correctly & apply spatial blend
                    float spBlend = m_audioSettings.GetSpatialBlend(m_audioPositioningDistanceSqr);
                    if (m_currentAudioSource.m_audioSource.spatialBlend != spBlend)
                    {
                        m_currentAudioSource.m_audioSource.spatialBlend = spBlend;
                    }
                    if (!m_currentAudioSource.m_audioSource.spatialize)
                    {
                        m_currentAudioSource.m_audioSource.spatialize = true;
                    }
                    if (m_targetGameObject != null)
                    {                        
                        m_currentAudioSource.m_audioSource.transform.position = m_audioPositioning;
                    }
                }
                // apply the full mix, including custom rolloff
                m_currentAudioSource.m_audioSource.volume = m_fadeT * m_audioClipSource.GetMixBusLevel()
                        * m_audioClipSource.GetVolumeModifier(m_targetGameObjectId);
                m_currentAudioSource.m_audioSource.pitch = GetMixPitch();
                float targDoppler = m_hasAudioSettings ? m_audioSettings.GetDopplerLevel() : 1.0f;
                if (m_currentAudioSource.m_audioSource.dopplerLevel < targDoppler)
                {
                    m_currentAudioSource.m_audioSource.dopplerLevel = Mathf.Clamp(m_currentAudioSource.m_audioSource.dopplerLevel + 0.1f, 0, targDoppler);
                }
                else
                {
                    m_currentAudioSource.m_audioSource.dopplerLevel = targDoppler;
                }
                // update any filters
                m_audioClipSource.UpdateFilters(m_currentAudioSource.m_pooledAudioSource, m_targetGameObjectId);

                if (WingroveRoot.Instance.ShouldCalculateMS(0))
                {
                    if (m_rmsRequested)
                    {
                        if ( m_bufferDataL == null )
                        {
                            m_bufferDataL = new float[512];
                        }
                        if ( m_bufferDataR == null )
                        {
                            m_bufferDataR = new float[512];
                        }
                        float rms = 0;
                        if (m_audioClipSource.GetAudioClip().channels == 2)
                        {
                            m_currentAudioSource.m_audioSource.GetOutputData(m_bufferDataL, 0);
                            m_currentAudioSource.m_audioSource.GetOutputData(m_bufferDataR, 1);
                            for (int index = 0; index < 512; ++index)
                            {
                                rms += (Mathf.Abs(m_bufferDataR[index]) + Mathf.Abs(m_bufferDataL[index]))
                                    * (Mathf.Abs(m_bufferDataR[index]) + Mathf.Abs(m_bufferDataL[index]));
                            }
                        }
                        else
                        {
                            m_currentAudioSource.m_audioSource.GetOutputData(m_bufferDataL, 0);
                            for (int index = 0; index < 512; ++index)
                            {
                                rms += (m_bufferDataL[index] * m_bufferDataL[index]);
                            }
                        }
                        rms = Mathf.Sqrt(Mathf.Clamp01(rms / 512.0f));
                        m_rms = Mathf.Max(rms * m_fadeT * m_audioClipSource.GetMixBusLevel(),
                            m_rms * 0.9f);// ((m_rms * 2 + rms) / 3.0f) * m_fadeT * m_audioClipSource.GetMixBusLevel();
                    }
                    m_rmsRequested = false;
                }                
            }
            else
            {
                m_rms = 0;
            }
        }

        public float GetRMS()
        {
            m_rmsRequested = true;
            return m_rms;
        }

        public void Play(float fade)
        {
            m_currentPosition = 0;
            if (m_currentAudioSourceExists)
            {
                m_currentAudioSource.m_audioSource.timeSamples = 0;
            }
            if (fade == 0.0f)
            {
                m_currentState = CueState.Playing;
                m_fadeT = 1.0f;
            }
            else
            {
                m_currentState = CueState.PlayingFadeIn;
                m_fadeSpeed = 1.0f / fade;
            }
        }

        public void Play(float fade, double dspStartTime)
        {
            m_currentPosition = 0;
            m_hasDSPStartTime = true;
            m_dspStartTime = dspStartTime;
            if (m_currentAudioSourceExists)
            {
                m_currentAudioSource.m_audioSource.timeSamples = 0;
            }
            if (fade == 0.0f)
            {
                m_currentState = CueState.Playing;
            }
            else
            {
                m_currentState = CueState.PlayingFadeIn;
                m_fadeSpeed = 1.0f / fade;
            }
        }

        public float GetTime()
        {
            float currentTime =
    (m_currentPosition) /
    (float)(m_audioClipSource.GetAudioClip().frequency * GetMixPitch());

            return currentTime;
        }

        public void SetTime(float time)
        {
            m_currentPosition = (int)(time * (float)(m_audioClipSource.GetAudioClip().frequency * GetMixPitch()))
                % m_audioClipSource.GetAudioClip().samples;
        }

        public float GetTimeUntilFinished(WingroveGroupInformation.HandleRepeatingAudio handleRepeat)
        {
            float timeRemaining =
                (m_audioClipSource.GetAudioClip().samples - m_currentPosition) /
                (float)(m_audioClipSource.GetAudioClip().frequency * GetMixPitch());
            if (m_audioClipSource.GetLooping())
            {
                switch(handleRepeat)
                {
                    case WingroveGroupInformation.HandleRepeatingAudio.IgnoreRepeatingAudio:
                        timeRemaining = 0.0f;
                        break;
                    case WingroveGroupInformation.HandleRepeatingAudio.ReturnFloatMax:
                    case WingroveGroupInformation.HandleRepeatingAudio.ReturnNegativeOne:
                        timeRemaining = float.MaxValue;
                        break;
                    case WingroveGroupInformation.HandleRepeatingAudio.GiveTimeUntilLoop:
                    default:
                        break;
                }
            }
            if (m_currentState == CueState.PlayingFadeOut)
            {
                if (m_fadeSpeed != 0)
                {
                    timeRemaining = Mathf.Min(m_fadeT / m_fadeSpeed, timeRemaining);
                }
            }
            return timeRemaining;
        }

        public float GetFadeT()
        {
            return m_fadeT;
        }

        public WingroveRoot.AudioSourcePoolItem GetCurrentAudioSource()
        {
            return m_currentAudioSource;
        }

        public void Stop(float fade)
        {
            if (fade == 0.0f)
            {
                StopInternal();
            }
            else
            {
                if (m_currentState != CueState.PlayingFadeOut)
                {
                    m_currentState = CueState.PlayingFadeOut;
                    m_fadeSpeed = 1.0f / fade;
                }
                else
                {
                    m_currentState = CueState.PlayingFadeOut;
                    m_fadeSpeed = Mathf.Max(1.0f / fade, m_fadeSpeed);
                }
            }
        }

        void StopInternal()
        {
            Unlink();
        }

        public void Unlink()
        {
            m_currentState = CueState.Stopped;
            if (m_currentAudioSourceExists)
            {
                WingroveRoot.Instance.UnlinkSource(m_currentAudioSource);
                m_currentAudioSource = null;
                m_currentAudioSourceExists = false;
            }
            m_audioClipSource.RemoveUsage();
            m_audioClipSource.RePool(this);
        }

        public void Virtualise()
        {
            if (m_currentAudioSourceExists)
            {
                WingroveRoot.Instance.UnlinkSource(m_currentAudioSource);
                m_currentAudioSource = null;
                m_currentAudioSourceExists = false;
            }
        }

        public void Pause()
        {
            if (!m_isPaused)
            {
                if (m_currentAudioSourceExists)
                {
                    m_currentAudioSource.m_audioSource.Pause();
                }
            }
            m_isPaused = true;

        }

        public void Unpause()
        {
            if (m_isPaused)
            {
                if (m_currentAudioSourceExists)
                {
                    m_currentAudioSource.m_audioSource.Play();
                }
            }
            m_isPaused = false;

        }

        public CueState GetState()
        {
            return m_currentState;
        }
    }

}