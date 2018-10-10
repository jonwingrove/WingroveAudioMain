using UnityEngine;
using System.Collections;
using System.Collections.Generic;
namespace WingroveAudio
{
    public abstract class BaseWingroveAudioSource : MonoBehaviour
    {
        [SerializeField]
        private bool m_looping = false;
        [SerializeField]
        private int m_importance = 0;
        [SerializeField]
        [FaderInterface]
        private float m_clipMixVolume = 1.0f;
        [SerializeField]
        private bool m_beatSynchronizeOnStart = false;
        [SerializeField]
        private int m_preCacheCount = 1;        

        public enum RetriggerOnSameObject
        {
            PlayAnother,
            DontPlay,
            Restart
        }

        [SerializeField]
        private bool m_is3DSound = false;
        [SerializeField]
        private bool m_instantRejectOnTooDistant;
        [SerializeField]
        private bool m_instantRejectHalfDistanceFewVoices;

        [SerializeField]
        private Audio3DSetting m_specify3DSettings = null;

        [SerializeField]
        private float m_randomVariationPitchMin = 1.0f;
        [SerializeField]
        private float m_randomVariationPitchMax = 1.0f;
        [SerializeField]
        private RetriggerOnSameObject m_retriggerOnSameObjectBehaviour = RetriggerOnSameObject.PlayAnother;

        [SerializeField]
        private int m_parameterCurveUpdateFrequencyBase = 1;
        [SerializeField]
        private int m_parameterCurveUpdateFrequencyOffset = 0;

        protected List<ActiveCue> m_currentActiveCues = new List<ActiveCue>(32);        
        protected int m_currentActiveCuesCount;
        protected List<ActiveCue> m_toRemove = new List<ActiveCue>(32);
        protected bool m_toRemoveDirty = false;
        protected WingroveMixBus m_mixBus;
        protected bool m_hasMixBus;
        protected InstanceLimiter m_instanceLimiter;
		protected List<ParameterModifierBase> m_parameterModifiersLive = new List<ParameterModifierBase>();
        protected List<ParameterModifierBase> m_parameterModifiersGlobalOpt = new List<ParameterModifierBase>();
        protected List<FilterApplicationBase> m_filterApplications = new List<FilterApplicationBase>();

        private int m_frameCtr;
        private bool m_hasCachedGlobalVolume;
        private bool m_hasCachedGlobalPitch;
        private float m_cachedGlobalVolume;
        private float m_cachedGlobalPitch;

        public Audio3DSetting Get3DSettings()
        {
            if(m_is3DSound)
            {
                if (m_specify3DSettings != null)
                {
                    return m_specify3DSettings;
                }
                else
                {
                    return WingroveRoot.Instance.GetDefault3DSettings();
                }
            }
            else
            {
                return null;
            }
        }

        public List<ActiveCue> GetActiveCuesDebug()
        {
            return m_currentActiveCues;
        }

        public bool ShouldUpdateParameterCurves()
        {
            if(m_parameterCurveUpdateFrequencyBase <= 1)
            {
                return true;
            }
            else
            {
                int val = (m_frameCtr + m_parameterCurveUpdateFrequencyOffset) % m_parameterCurveUpdateFrequencyBase;
                return (val == 0);
            }
        }

        void Awake()
        {
            m_mixBus = WingroveMixBus.FindParentMixBus(transform);
            m_instanceLimiter = WingroveMixBus.FindParentLimiter(transform);
            if (m_mixBus != null)
            {
                m_hasMixBus = true;
                m_mixBus.RegisterSource(this);
            }
			FindParameterModifiers(transform);
            // no filters on switch...
#if !UNITY_SWITCH
            FindFilterApplications(transform);
#endif
            WingroveRoot.Instance.RegisterAudioSource(this);
            Initialise();
        }
		
		void FindParameterModifiers(Transform t)
		{			
            if (t == null)
            {
                return;
            }
            else
            {
                ParameterModifierBase[] paramMods = t.gameObject.GetComponents<ParameterModifierBase>();
				foreach(ParameterModifierBase mod in paramMods)
				{
                    if (mod.IsGlobalOptimised())
                    {
                        m_parameterModifiersGlobalOpt.Add(mod);
                    }
                    else
                    {
                        m_parameterModifiersLive.Add(mod);
                    }
				}				
                FindParameterModifiers(t.parent);
			}
		}

        void FindFilterApplications(Transform t)
        {
            if (t == null)
            {
                return;
            }
            else
            {
                FilterApplicationBase[] paramMods = t.gameObject.GetComponents<FilterApplicationBase>();
                foreach (FilterApplicationBase mod in paramMods)
                {
                    m_filterApplications.Add(mod);
                }
                FindFilterApplications(t.parent);
            }
        }

		public float GetPitchModifier(int goId)
		{
            if(!m_hasCachedGlobalPitch)
            {
                m_cachedGlobalPitch = 1.0f;
                List<ParameterModifierBase>.Enumerator pvEnG = m_parameterModifiersGlobalOpt.GetEnumerator();
                while (pvEnG.MoveNext())
                {
                    ParameterModifierBase pvMod = pvEnG.Current;
                    m_cachedGlobalPitch *= pvMod.GetPitchMultiplier(goId);
                }
                m_hasCachedGlobalPitch = true;
            }

			float pMod = m_cachedGlobalPitch;
            List<ParameterModifierBase>.Enumerator pvEn = m_parameterModifiersLive.GetEnumerator();
            while(pvEn.MoveNext())
            {
                ParameterModifierBase pvMod = pvEn.Current;
				pMod *= pvMod.GetPitchMultiplier(goId);
			}			
			return pMod;
		}
		
		public float GetVolumeModifier(int goId)
		{
            if (!m_hasCachedGlobalVolume)
            {
                m_cachedGlobalVolume = 1.0f;
                List<ParameterModifierBase>.Enumerator pvEnG = m_parameterModifiersGlobalOpt.GetEnumerator();
                while (pvEnG.MoveNext())
                {
                    ParameterModifierBase pvMod = pvEnG.Current;
                    m_cachedGlobalVolume *= pvMod.GetVolumeMultiplier(goId);
                }
                m_hasCachedGlobalVolume = true;
            }

            float vMod = m_clipMixVolume * m_cachedGlobalVolume;

            List<ParameterModifierBase>.Enumerator pvEn = m_parameterModifiersLive.GetEnumerator();
            while(pvEn.MoveNext())
            {
                ParameterModifierBase pvMod = pvEn.Current;
				vMod *= pvMod.GetVolumeMultiplier(goId);
			}			
			return vMod;
		}

        public void UpdateFilters(PooledAudioSource targetPlayer, int goId)
        {
            targetPlayer.ResetFiltersForFrame();

            List<FilterApplicationBase>.Enumerator filtEn = m_filterApplications.GetEnumerator();
            while (filtEn.MoveNext())
            {
                FilterApplicationBase faB = filtEn.Current;
                faB.UpdateFor(targetPlayer, goId);
            }

            targetPlayer.CommitFiltersForFrame();
        }

        public bool IsPlaying()
        {
            return m_currentActiveCuesCount > 0;
        }

        public float GetCurrentTime()
        {
            float result = 0.0f;
            foreach (ActiveCue cue in m_currentActiveCues)
            {
                result = Mathf.Max(cue.GetTime(), result);
            }
            return result;
        }

        public float GetTimeUntilFinished(WingroveGroupInformation.HandleRepeatingAudio handleRepeats)
        {
            float result = 0.0f;
            foreach (ActiveCue cue in m_currentActiveCues)
            {
                result = Mathf.Max(cue.GetTimeUntilFinished(handleRepeats), result);
            }
            return result;
        }

        public void DoUpdate(int frameCtr)
        {
            m_frameCtr = frameCtr;
            m_hasCachedGlobalPitch = false;
            m_hasCachedGlobalVolume = false;
            if (m_currentActiveCuesCount > 0)
            {
                UpdateInternal();
            }
        }

        protected void UpdateInternal()
        {
            List<ActiveCue>.Enumerator en = m_currentActiveCues.GetEnumerator();
            while (en.MoveNext())
            {
                ActiveCue c = en.Current;
                c.Update();
            }
            if (m_toRemoveDirty)
            {
                foreach (ActiveCue c in m_toRemove)
                {                    
                    m_currentActiveCues.Remove(c);
                    if(m_instanceLimiter != null)
                    {
                        m_instanceLimiter.RemoveCue(c);
                    }
                    m_currentActiveCuesCount--;
                }
                m_toRemove.Clear();
                m_toRemoveDirty = false;
            }
        }

        void OnDestroy()
        {
            if (m_hasMixBus)
            {
                m_mixBus.RemoveSource(this);
            }
        }

        public float GetClipMixVolume()
        {
            return m_clipMixVolume;
        }

        public int GetImportance()
        {
            if (!m_hasMixBus)
            {
                return m_importance;
            }
            else
            {
                return m_importance + m_mixBus.GetImportance();
            }
        }       

        public ActiveCue GetCueForGameObject(GameObject go)
        {
            int goId = go.GetInstanceID();
            foreach (ActiveCue cue in m_currentActiveCues)
            {
                if (cue.GetTargetObjectId() == goId && 
                    cue.GetState() != ActiveCue.CueState.PlayingFadeOut 
                    && cue.GetState() != ActiveCue.CueState.Stopped)
                {
                    return cue;
                }
            }

            foreach (ActiveCue cue in m_currentActiveCues)
            {
                if (cue.GetTargetObjectId() == goId)
                {
                    return cue;
                }
            }
            return null;
        }

        public float GetRMS()
        {
            float tSqr = 0;
            foreach (ActiveCue cue in m_currentActiveCues)
            {
                float rms = cue.GetRMS();
                tSqr += (rms * rms);
            }

            return Mathf.Sqrt(tSqr);
        }

        public float GetMixBusLevel()
        {
            if (m_hasMixBus)
            {
                return m_mixBus.GetMixedVol();
            }
            else
            {
                return 1.0f;
            }
        }


        public float GetNewPitch()
        {
            return Random.Range(m_randomVariationPitchMin,
                m_randomVariationPitchMax);
        }

        public float GetPitchMax()
        {
            return m_randomVariationPitchMax;
        }

        public bool GetLooping()
        {
            return m_looping;
        }

        public ActiveCue Stop(ActiveCue cue, float fade)
        {
            if (cue == null)
            {
                foreach (ActiveCue fCue in m_currentActiveCues)
                {
                    fCue.Stop(fade);
                }
            }
            else
            {
                cue.Stop(fade);
            }
            return cue;
        }

        ActiveCue GetNextCue()
        {
            return new ActiveCue();
        }

        public void RePool(ActiveCue cue)
        {
            m_toRemove.Add(cue);
            m_toRemoveDirty = true;
        }

        public ActiveCue Play(ActiveCue cue, float fade, GameObject target, AudioArea aa)
        {
            if (m_instanceLimiter == null || m_instanceLimiter.CanPlay(target))
            {
                if ((cue == null) || (m_retriggerOnSameObjectBehaviour == RetriggerOnSameObject.PlayAnother))
                {

                    bool rejected = false;
                    if (m_is3DSound && m_instantRejectOnTooDistant)
                    {
                        if (gameObject != null)
                        {
                            Vector3 pos =
                                WingroveRoot.Instance.GetRelativeListeningPosition(target.transform.position);
                            float dist = (WingroveRoot.Instance.GetSingleListener().transform.position -
                                pos).magnitude;
                            float maxDist = Get3DSettings().GetMaxDistance();
                            if (dist > maxDist)
                            {
                                rejected = true;
                            }
                            else if (m_instantRejectHalfDistanceFewVoices && dist > maxDist * 0.5f
                                && WingroveRoot.Instance.IsCloseToMax())
                            {
                                rejected = true;
                            }
                        }
                    }

                    if (!rejected)
                    {
                        cue = GetNextCue();
                        cue.Initialise(gameObject, this, target, aa);
                        m_currentActiveCues.Add(cue);
                        m_currentActiveCuesCount++;
                        if (m_beatSynchronizeOnStart)
                        {
                            BeatSyncSource current = BeatSyncSource.GetCurrent();
                            if (current != null)
                            {
                                cue.Play(fade, current.GetNextBeatTime());
                            }
                            else
                            {
                                cue.Play(fade);
                            }
                        }
                        else
                        {
                            cue.Play(fade);
                        }
                        if (m_instanceLimiter != null)
                        {
                            m_instanceLimiter.AddCue(cue, target);
                        }
                    }
                }
                else
                {
                    if (m_retriggerOnSameObjectBehaviour != RetriggerOnSameObject.DontPlay)
                    {
                        cue.Play(fade);
                    }
                }


            }
            return cue;
        }

        public ActiveCue Play(ActiveCue cue, float fade, GameObject target)
        {
            if (m_instanceLimiter == null || m_instanceLimiter.CanPlay(target))
            {
                if ((cue == null)||(m_retriggerOnSameObjectBehaviour == RetriggerOnSameObject.PlayAnother))
                {

                    bool rejected = false;
                    if (m_is3DSound && m_instantRejectOnTooDistant)
                    {
                        if (gameObject != null)
                        {
                            Vector3 pos =
                                WingroveRoot.Instance.GetRelativeListeningPosition(target.transform.position);
                            float dist = (WingroveRoot.Instance.GetSingleListener().transform.position -
                                pos).magnitude;
                            float maxDist = Get3DSettings().GetMaxDistance();
                            if (dist > maxDist)
                            {
                                rejected = true;
                            }
                            else if(m_instantRejectHalfDistanceFewVoices && dist > maxDist * 0.5f
                                && WingroveRoot.Instance.IsCloseToMax())
                            {
                                rejected = true;
                            }
                        }
                    }
                    
                    if (!rejected)
                    {
                        cue = GetNextCue();
                        cue.Initialise(gameObject, this, target);
                        m_currentActiveCues.Add(cue);
                        m_currentActiveCuesCount++;
                        if (m_beatSynchronizeOnStart)
                        {
                            BeatSyncSource current = BeatSyncSource.GetCurrent();
                            if (current != null)
                            {
                                cue.Play(fade, current.GetNextBeatTime());
                            }
                            else
                            {
                                cue.Play(fade);
                            }
                        }
                        else
                        {
                            cue.Play(fade);
                        }
                        if (m_instanceLimiter != null)
                        {
                            m_instanceLimiter.AddCue(cue, target);
                        }
                    }
                }
                else
                {
                    if (m_retriggerOnSameObjectBehaviour != RetriggerOnSameObject.DontPlay)
                    {
                        cue.Play(fade);
                    }
                }
                

            }
            return cue;
        }

        public ActiveCue Pause(ActiveCue cue)
        {
            if (cue == null)
            {
                foreach (ActiveCue fCue in m_currentActiveCues)
                {
                    fCue.Pause();
                }
            }
            else
            {
                cue.Pause();
            }
            return cue;
        }

        public ActiveCue Unpause(ActiveCue cue)
        {
            if (cue == null)
            {
                foreach (ActiveCue fCue in m_currentActiveCues)
                {
                    fCue.Unpause();
                }
            }
            else
            {
                cue.Unpause();
            }
            return cue;
        }

        public virtual AudioClip GetAudioClip()
        {
            // null implementation
            Debug.LogError("Using null implementation");
            return null;
        }
        public abstract void RemoveUsage();
        public abstract void AddUsage();
        public virtual void Initialise()
        {

        }
    }

}