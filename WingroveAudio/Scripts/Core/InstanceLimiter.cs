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
        [SerializeField]
        private bool m_perGameObject = false;

        List<ActiveCue> m_activeCues = new List<ActiveCue>();
        Dictionary<GameObject, List<ActiveCue>> m_activeCuesPerObject = new Dictionary<GameObject, List<ActiveCue>>(8);

        public bool CanPlay(GameObject attachedObject)
        {
            Tidy();
            if (m_limitMethod == LimitMethod.DontCreateNew)
            {
                if ((attachedObject!=null)&&(m_perGameObject))
                {
                    List<ActiveCue> numActive = null;
                    if (m_activeCuesPerObject.TryGetValue(attachedObject, out numActive))
                    {
                        if (numActive.Count >= m_instanceLimit)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    if (m_activeCues.Count >= m_instanceLimit)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public void AddCue(ActiveCue cue, GameObject attachedObject)
        {
            m_activeCues.Add(cue);
            if ((attachedObject != null) && (m_perGameObject))
            {
                if (!m_activeCuesPerObject.ContainsKey(attachedObject))
                {
                    m_activeCuesPerObject[attachedObject] = new List<ActiveCue>();
                }
                m_activeCuesPerObject[attachedObject].Add(cue);
            }
            Limit(attachedObject);
        }

        List<GameObject> m_reusableLeysToRemove = new List<GameObject>(8);
        void Tidy()
        {
            Tidy(m_activeCues);

            if (m_perGameObject)
            {
                m_reusableLeysToRemove.Clear();
                Dictionary<GameObject,List<ActiveCue>>.ValueCollection.Enumerator enumerator = m_activeCuesPerObject.Values.GetEnumerator();
                while(enumerator.MoveNext())
                {
                    List<ActiveCue> cueList = enumerator.Current;
                    Tidy(cueList);
                }
                Dictionary<GameObject, List<ActiveCue>>.Enumerator enumAc = m_activeCuesPerObject.GetEnumerator();
                while(enumAc.MoveNext())
                {
                    KeyValuePair<GameObject, List<ActiveCue>> kvp = enumAc.Current;
                    if (kvp.Value.Count == 0)
                    {
                        m_reusableLeysToRemove.Add(kvp.Key);
                    }
                }
                List<GameObject>.Enumerator enumRC = m_reusableLeysToRemove.GetEnumerator();
                while(enumRC.MoveNext())
                {
                    m_activeCuesPerObject.Remove(enumRC.Current);
                }
            }
        }

        List<ActiveCue> m_toRemoveTidyInternal = new List<ActiveCue>(8);
        void Tidy(List<ActiveCue> list)
        {
            m_toRemoveTidyInternal.Clear();
            List<ActiveCue>.Enumerator enumerator = list.GetEnumerator();
            while(enumerator.MoveNext())
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
            while(enumerator.MoveNext())
            {
                list.Remove(enumerator.Current);
            }
        }

        void Limit(GameObject attachedObject)
        {
            Tidy();

            if ((attachedObject != null) && (m_perGameObject))
            {
                if (m_activeCuesPerObject.ContainsKey(attachedObject))
                {
                    if (m_activeCuesPerObject[attachedObject].Count > m_instanceLimit)
                    {
                        if (m_activeCuesPerObject[attachedObject][0] != null)
                        {
                            m_activeCuesPerObject[attachedObject][0].Stop(m_removedSourceFade);
                        }
                        m_activeCuesPerObject[attachedObject].Remove(m_activeCuesPerObject[attachedObject][0]);
                    }                    
                }
            }
            else
            {
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

}