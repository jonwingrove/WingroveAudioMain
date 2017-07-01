using UnityEngine;
using System.Collections;
using System.Collections.Generic;
namespace WingroveAudio
{
    [AddComponentMenu("WingroveAudio/Instance Limiter")]
    public class InstanceLimiter : MonoBehaviour
    {
        [SerializeField]
        private int m_instanceLimit = 1;
        public enum LimitMethod
        {
            RemoveOldest,
            DontCreateNew
        }
        [SerializeField]
        private LimitMethod m_limitMethod = LimitMethod.RemoveOldest;

        [SerializeField]
        private float m_removedSourceFade = 0.0f;
        [SerializeField]
        private bool m_ignoreStopping = true;

        List<ActiveCue> m_activeCues = new List<ActiveCue>();

        private bool m_requiresTidy = false;
        private int m_addedThisFrame = 0;

        private void Start()
        {
            WingroveAudio.WingroveRoot.Instance.RegisterIL(this);
        }

        public void ResetFrameFlags()
        {
            m_addedThisFrame = 0;
            m_requiresTidy = true;
        }

        public bool CanPlay(GameObject attachedObject)
        {
            if (m_requiresTidy)
            {
                Tidy();
            }
            if (m_limitMethod == LimitMethod.DontCreateNew)
            {
                if (m_activeCues.Count >= m_instanceLimit)
                {
                    return false;
                }
            }
            else
            {
                // we've already added a bunch this frame...unlikely these new 
                // ones are any better...
                if (m_addedThisFrame >= m_instanceLimit)
                {
                    return false;
                }
            }
            return true;
        }

        public void AddCue(ActiveCue cue, GameObject attachedObject)
        {
            m_addedThisFrame++;
            m_activeCues.Add(cue);
            Limit(attachedObject);
        }

        List<GameObject> m_reusableLeysToRemove = new List<GameObject>(8);
        void Tidy()
        {
            // done this frame... noice
            m_requiresTidy = false;
            Tidy(m_activeCues);
        }

        List<ActiveCue> m_toRemoveTidyInternal = new List<ActiveCue>(8);
        void Tidy(List<ActiveCue> list)
        {
            m_toRemoveTidyInternal.Clear();
            List<ActiveCue>.Enumerator enumerator = list.GetEnumerator();
            while (enumerator.MoveNext())
            {
                ActiveCue c = enumerator.Current;
                if (c == null || c.GetState() == ActiveCue.CueState.Stopped)
                {
                    m_toRemoveTidyInternal.Add(c);
                }
                else if (m_ignoreStopping)
                {
                    if (c.GetState() == ActiveCue.CueState.PlayingFadeOut)
                    {
                        m_toRemoveTidyInternal.Add(c);
                    }
                }
            }

            enumerator = m_toRemoveTidyInternal.GetEnumerator();
            while (enumerator.MoveNext())
            {
                list.Remove(enumerator.Current);
            }
        }

        void Limit(GameObject attachedObject)
        {
            if (m_requiresTidy)
            {
                Tidy();
            }

            if (m_activeCues.Count > m_instanceLimit)
            {
                if (m_activeCues[0] != null)
                {
                    m_activeCues[0].Stop(m_removedSourceFade);
                }
                m_activeCues.Remove(m_activeCues[0]);
            }

        }
    }

}