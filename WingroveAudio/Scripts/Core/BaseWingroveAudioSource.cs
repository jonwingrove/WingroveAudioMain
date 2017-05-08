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
        private Audio3DSetting m_specify3DSettings = null;

        [SerializeField]
        private float m_randomVariationPitchMin = 1.0f;
        [SerializeField]
        private float m_randomVariationPitchMax = 1.0f;
        [SerializeField]
        private RetriggerOnSameObject m_retriggerOnSameObjectBehaviour = RetriggerOnSameObject.PlayAnother;
         
        protected List<ActiveCue> m_currentActiveCues = new List<ActiveCue>(32);
        protected List<ActiveCue> m_toRemove = new List<ActiveCue>(32);
        protected WingroveMixBus m_mixBus;
        protected InstanceLimiter m_instanceLimiter;
		protected List<ParameterModifierBase> m_parameterModifiers = new List<ParameterModifierBase>();
        protected List<FilterApplicationBase> m_filterApplications = new List<FilterApplicationBase>();

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

        void Awake()
        {
            m_mixBus = WingroveMixBus.FindParentMixBus(transform);
            m_instanceLimiter = WingroveMixBus.FindParentLimiter(transform);
            if (m_mixBus != null)
            {
                m_mixBus.RegisterSource(this);
            }
			FindParameterModifiers(transform);
            FindFilterApplications(transform);
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
					m_parameterModifiers.Add(mod);
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
		
		public float GetPitchModifier(GameObject go)
		{
			float pMod = 1.0f;
            List<ParameterModifierBase>.Enumerator pvEn = m_parameterModifiers.GetEnumerator();
            while(pvEn.MoveNext())
            {
                ParameterModifierBase pvMod = pvEn.Current;
				pMod *= pvMod.GetPitchMultiplier(go);
			}			
			return pMod;
		}
		
		public float GetVolumeModifier(GameObject go)
		{
			float vMod = m_clipMixVolume;

            List<ParameterModifierBase>.Enumerator pvEn = m_parameterModifiers.GetEnumerator();
            while(pvEn.MoveNext())
            {
                ParameterModifierBase pvMod = pvEn.Current;
				vMod *= pvMod.GetVolumeMultiplier(go);
			}			
			return vMod;
		}

        public void UpdateFilters(PooledAudioSource targetPlayer, GameObject go)
        {
            targetPlayer.ResetFiltersForFrame();

            List<FilterApplicationBase>.Enumerator filtEn = m_filterApplications.GetEnumerator();
            while (filtEn.MoveNext())
            {
                FilterApplicationBase faB = filtEn.Current;
                faB.UpdateFor(targetPlayer, go);
            }

            targetPlayer.CommitFiltersForFrame();
        }

        public bool IsPlaying()
        {
            return (m_currentActiveCues.Count > 0);
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

        public void DoUpdate()
        {
            UpdateInternal();
        }

        protected void UpdateInternal()
        {
            foreach (ActiveCue c in m_currentActiveCues)
            {
                c.Update();
            }
            foreach (ActiveCue c in m_toRemove)
            {
                m_currentActiveCues.Remove(c);
            }
            m_toRemove.Clear();
        }

        void OnDestroy()
        {
            if (m_mixBus != null)
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
            if (m_mixBus == null)
            {
                return m_importance;
            }
            else
            {
                return m_importance + m_mixBus.GetImportance();
            }
        }

        public bool HasActiveCues()
        {
            return (m_currentActiveCues.Count != 0);
        }

        public ActiveCue GetCueForGameObject(GameObject go)
        {
            foreach (ActiveCue cue in m_currentActiveCues)
            {
                if (cue.GetTargetObject() == go && 
                    cue.GetState() != ActiveCue.CueState.PlayingFadeOut 
                    && cue.GetState() != ActiveCue.CueState.Stopped)
                {
                    return cue;
                }
            }

            foreach (ActiveCue cue in m_currentActiveCues)
            {
                if (cue.GetTargetObject() == go)
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
            if (m_mixBus)
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
        }

        public ActiveCue Play(ActiveCue cue, float fade, GameObject target)
        {
            if (m_instanceLimiter == null || m_instanceLimiter.CanPlay(target))
            {
                if ((cue == null)||(m_retriggerOnSameObjectBehaviour == RetriggerOnSameObject.PlayAnother))
                {                    
                    cue = GetNextCue();
                    cue.Initialise(gameObject, target);
                    m_currentActiveCues.Add(cue);
                    if (m_beatSynchronizeOnStart)
                    {
                        BeatSyncSource current = BeatSyncSource.GetCurrent();
                        if ( current != null )
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