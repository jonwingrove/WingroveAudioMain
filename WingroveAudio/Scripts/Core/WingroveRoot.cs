using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace WingroveAudio
{
    [AddComponentMenu("WingroveAudio/Wingrove Root")]
    public class WingroveRoot : MonoBehaviour
    {

        static WingroveRoot s_instance;
        [SerializeField]
        private bool m_useDecibelScale = true;
        [SerializeField]
        private bool m_allowMultipleListeners = false;
        [SerializeField]
        private bool m_dontDestroyOnLoad = false;

        public enum MultipleListenerPositioningModel
        {
            Simplified,
            InverseSquareDistanceWeighted
        }

        [SerializeField]
        private MultipleListenerPositioningModel m_listeningModel = MultipleListenerPositioningModel.InverseSquareDistanceWeighted;

        private GUISkin m_editorSkin;

        public static WingroveRoot Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = (WingroveRoot)GameObject.FindObjectOfType(typeof(WingroveRoot));
                }
                return s_instance;
            }
        }

        [SerializeField]
        private int m_audioSourcePoolSize = 32;
        [SerializeField]
        private int m_calculateRMSIntervalFrames = 1;
        [SerializeField]
        private bool m_useDSPTime = true;

        [SerializeField]
        private bool m_debugGUI = false;

        [SerializeField]
        private Vector3 m_listenerOffset;

        [SerializeField]
        private Audio3DSetting m_default3DAudioSettings;        

        private int m_rmsFrame;
        private static double m_lastDSPTime;
        private static double m_dspDeltaTime;

        public class AudioSourcePoolItem
        {
            public AudioSource m_audioSource;
            public PooledAudioSource m_pooledAudioSource;
            public ActiveCue m_user;
        }
        List<AudioSourcePoolItem> m_audioSourcePool = new List<AudioSourcePoolItem>();

        Dictionary<string, List<BaseEventReceiveAction>> m_eventReceivers = new Dictionary<string, List<BaseEventReceiveAction>>();

        [SerializeField]
        public AudioNameGroup[] m_audioNameGroups;

        private List<WingroveListener> m_listeners = new List<WingroveListener>();
        private List<BaseWingroveAudioSource> m_allRegisteredSources = new List<BaseWingroveAudioSource>();
        private List<WingroveMixBus> m_allMixBuses = new List<WingroveMixBus>();

        public class CachedParameterValue
        {
            public float m_valueNull;
            public Dictionary<GameObject, float> m_valueObject = new Dictionary<GameObject, float>();
            public bool m_isGlobalValue;
        }
		
		class ParameterValues
		{
			public Dictionary<string, CachedParameterValue> m_parameterValues = new Dictionary<string, CachedParameterValue>();
		}
		ParameterValues m_values = new ParameterValues();
        private GameObject m_thisListener;

#if UNITY_EDITOR
        public class LoggedEvent
        {
            public string m_eventName = null;
            public GameObject m_linkedObject = null;
            public double m_time;
            public string m_linkedObjectName = null;
        }

        private List<LoggedEvent> m_loggedEvents = new List<LoggedEvent>();
        private int m_maxEvents = 50;
        private double m_startTime;        

        public void LogEvent(string eventName, GameObject linkedObject)
        {
            LoggedEvent lge = new LoggedEvent();
            lge.m_eventName = eventName;
            lge.m_linkedObject = linkedObject;
            if ( linkedObject != null )
            {
                lge.m_linkedObjectName = linkedObject.name;
            }
            lge.m_time = AudioSettings.dspTime - m_startTime;
            m_loggedEvents.Add(lge);
            if (m_loggedEvents.Count > m_maxEvents)
            {
                m_loggedEvents.RemoveAt(0);
            }
        }

        public List<LoggedEvent> GetLogList()
        {
            return m_loggedEvents;
        }
#endif
        GameObject m_pool;
        // Use this for initialization
        void Awake()
        {
            if (m_dontDestroyOnLoad)
            {
                DontDestroyOnLoad(this);
            }

            s_instance = this;
            m_pool = new GameObject("AudioSourcePool");
            m_pool.transform.parent = transform;
            for (int i = 0; i < m_audioSourcePoolSize; ++i)
            {
                CreateAudioSource();
            }

            BaseEventReceiveAction[] evrs = GetComponentsInChildren<BaseEventReceiveAction>();
            foreach (BaseEventReceiveAction evr in evrs)
            {
                string[] events = evr.GetEvents();
                if (events != null)
                {
                    foreach (string ev in events)
                    {
                        if (!m_eventReceivers.ContainsKey(ev))
                        {
                            m_eventReceivers[ev] = new List<BaseEventReceiveAction>();
                        }
                        m_eventReceivers[ev].Add(evr);
                    }
                }
            }

            
            m_thisListener = new GameObject("Listener");
            m_thisListener.transform.parent = transform;
            m_thisListener.AddComponent<AudioListener>();
            m_thisListener.transform.localPosition = m_listenerOffset;
        
            transform.position = Vector3.zero;
            m_lastDSPTime = AudioSettings.dspTime;
#if UNITY_EDITOR
            m_startTime = AudioSettings.dspTime;
#endif
        }

        public Audio3DSetting GetDefault3DSettings()
        {
            return m_default3DAudioSettings;
        }

        public void RegisterAudioSource(BaseWingroveAudioSource bas)
        {
            m_allRegisteredSources.Add(bas);
        }

        public void RegisterMixBus(WingroveMixBus wmb)
        {
            m_allMixBuses.Add(wmb);
        }

        public float EvaluteDefault3D(float distance)
        {
            if (m_default3DAudioSettings != null)
            {
                return m_default3DAudioSettings.EvaluateStandard(distance);
            }
            else
            {
                return 1 / distance;
            }
        }

        void OnGUI()
        {
            if ( m_debugGUI )
            {
                Vector3 pos = new Vector3(0,0);
                GUI.BeginGroup(new Rect(0,0,500,1000),"", "box");
                GUI.Label(new Rect(pos.x,pos.y,300,20),"Number of sources: " + m_audioSourcePool.Count);
                pos.y+=20;
                foreach(AudioSourcePoolItem aspi in m_audioSourcePool)
                {
                    if ( aspi.m_user != null )
                    {
                        GUI.Label(new Rect(pos.x,pos.y,500,20), aspi.m_audioSource.clip.name + " @ " + aspi.m_user.GetState() + " " + (int)(100 * aspi.m_audioSource.time / aspi.m_audioSource.clip.length));
                    }
                    else
                    {
                        GUI.Label(new Rect(pos.x,pos.y,500,20), "Unused");
                    }

                    pos.y+=20;
                }
                GUI.EndGroup();
            }
        }
        
		public float GetParameterForGameObject(string parameter, GameObject go)
		{
            CachedParameterValue result;
            m_values.m_parameterValues.TryGetValue(parameter, out result);
            if(result == null)
            {
                return 0.0f;
            }
            else
            {
                if(result.m_isGlobalValue)
                {
                    return result.m_valueNull;
                }
                else
                {
                    float resultF = 0.0f;
                    result.m_valueObject.TryGetValue(go, out resultF);
                    return resultF;
                }
            }
		}
		
		public void SetParameterGlobal(string parameter, float setValue)
		{
            CachedParameterValue cpv = null;
            m_values.m_parameterValues.TryGetValue(parameter, out cpv);
            if(cpv == null)
            {
                cpv = new CachedParameterValue();
                m_values.m_parameterValues[parameter] = cpv;
            }
            cpv.m_valueNull = setValue;
            cpv.m_isGlobalValue = true;
		}
		
		public void SetParameterForObject(string parameter, GameObject go, float setValue)
		{
            CachedParameterValue cpv = null;
            m_values.m_parameterValues.TryGetValue(parameter, out cpv);
            if (cpv == null)
            {
                cpv = new CachedParameterValue();
                m_values.m_parameterValues[parameter] = cpv;
            }
            cpv.m_valueObject[go] = setValue;
            cpv.m_isGlobalValue = false;
		}

        public bool UseDBScale
        {
            get { return m_useDecibelScale; }
            set { m_useDecibelScale = value; }
        }

        //StoredImportSettings m_storedImportSettings;

        //public bool Is3D(string path)
        //{
        //    if (m_storedImportSettings == null)
        //    {
        //        m_storedImportSettings = (StoredImportSettings)Resources.Load("ImportSettings");
        //    }
        //    return m_storedImportSettings.Is3D(path);
        //}

        public int FindEvent(string eventName)
        {
            int index = 0;
            foreach (AudioNameGroup eg in m_audioNameGroups)
            {
                if (eg != null && eg.GetEvents() != null)
                {
                    foreach (string st in eg.GetEvents())
                    {
                        if (st == eventName)
                        {
                            return index;
                        }
                    }
                }
                ++index;
            }
            return -1;
        }

        public int FindParameter(string parameterName)
        {
            int index = 0;
            foreach (AudioNameGroup eg in m_audioNameGroups)
            {
                if (eg != null && eg.GetParameters() != null)
                {
                    foreach (string st in eg.GetParameters())
                    {
                        if (st == parameterName)
                        {
                            return index;
                        }
                    }
                }
                ++index;
            }
            return -1;
        }

        public GUISkin GetSkin()
        {
            if (m_editorSkin == null)
            {
                m_editorSkin = (GUISkin)Resources.Load("WingroveAudioSkin");
            }
            return m_editorSkin;
        }

        public void PostEvent(string eventName)
        {
            PostEventCL(eventName, (List<ActiveCue>)null, null);
        }

        public void PostEventCL(string eventName, List<ActiveCue> cuesIn)
        {
            PostEventCL(eventName, cuesIn, null);
        }

        public void PostEventGO(string eventName, GameObject targetObject)
        {            
            PostEventGO(eventName, targetObject, null);
        }

        public void PostEventGO(string eventName, GameObject targetObject, List<ActiveCue> cuesOut)
        {
#if UNITY_EDITOR
            LogEvent(eventName, targetObject);
#endif
            List<BaseEventReceiveAction> listOfReceivers = null;
            if (m_eventReceivers.TryGetValue(eventName, out listOfReceivers))
            {
                foreach (BaseEventReceiveAction evr in listOfReceivers)
                {
                    evr.PerformAction(eventName, targetObject, cuesOut);
                }
            }
        }

        public void PostEventCL(string eventName, List<ActiveCue> cuesIn, List<ActiveCue> cuesOut)
        {
#if UNITY_EDITOR            
            LogEvent(eventName, null);
#endif
            List<BaseEventReceiveAction> listOfReceivers = null;
            if (m_eventReceivers.TryGetValue(eventName, out listOfReceivers))
            {
                foreach (BaseEventReceiveAction evr in listOfReceivers)
                {
                    evr.PerformAction(eventName, cuesIn, cuesOut);
                }
            }
        }

        public AudioSourcePoolItem TryClaimPoolSource(ActiveCue cue)
        {
            AudioSourcePoolItem bestSteal = null;
            int lowestImportance = cue.GetImportance();
            float quietestSimilarImportance = 1.0f;
            foreach (AudioSourcePoolItem aspi in m_audioSourcePool)
            {
                if (aspi.m_user == null || aspi.m_user.GetState() == ActiveCue.CueState.Stopped)
                {
                    aspi.m_user = cue;
                    return aspi;
                }
                else
                {
                    if (aspi.m_user.GetImportance() < cue.GetImportance())
                    {
                        if (aspi.m_user.GetImportance() < lowestImportance)
                        {
                            lowestImportance = aspi.m_user.GetImportance();
                            bestSteal = aspi;
                        }
                        else if (aspi.m_user.GetImportance() == lowestImportance)
                        {
                            if (aspi.m_user.GetState() == ActiveCue.CueState.PlayingFadeOut )
                            {
                                bestSteal = aspi;
                            }
                        }
                    }
                    else if (aspi.m_user.GetImportance() == lowestImportance)
                    {
                        if (aspi.m_user.GetState() == ActiveCue.CueState.PlayingFadeOut ||
                            aspi.m_user.GetTheoreticalVolume() < quietestSimilarImportance)
                        {
                            quietestSimilarImportance = aspi.m_user.GetTheoreticalVolume();
                            bestSteal = aspi;
                        }
                    }
                }
            }
            if (bestSteal != null)
            {
                bestSteal.m_user.Virtualise();
                bestSteal.m_pooledAudioSource.ResetFiltersForFrame();
                bestSteal.m_user = cue;
                return bestSteal;
            } else if (m_audioSourcePool.Count < m_audioSourcePoolSize)
            {
                AudioSourcePoolItem aspi = CreateAudioSource();                
                return aspi;
            } else
            {
                return null;
            }
        }

        private AudioSourcePoolItem CreateAudioSource()
        {
            GameObject newAudioSource = new GameObject("PooledAudioSource");
            newAudioSource.transform.parent = m_pool.transform;

            AudioSource aSource = newAudioSource.AddComponent<AudioSource>();
            

            AudioSourcePoolItem aspi = new AudioSourcePoolItem();
            aspi.m_audioSource = aSource;
            aspi.m_pooledAudioSource = newAudioSource.AddComponent<PooledAudioSource>();
            aSource.enabled = false;
            m_audioSourcePool.Add(aspi);

            return aspi;
        }

        public void UnlinkSource(AudioSourcePoolItem item)
        {
            item.m_audioSource.Stop();
            item.m_audioSource.enabled = false;
            item.m_audioSource.clip = null;
            item.m_user = null;
            GameObject sourceItem = item.m_audioSource.gameObject;
            //Destroy(item.m_audioSource);
            //AudioSource aSource = sourceItem.AddComponent<AudioSource>();
            //aSource.playOnAwake = false;
            //aSource.enabled = false;
            //aSource.rolloffMode = m_defaultRolloffMode;
            //aSource.maxDistance = m_defaultMaxDistance;
            //aSource.minDistance = m_defaultMinDistance;

            //item.m_audioSource = aSource;
        }

        public string dbStringUtil(float amt)
        {
            string result = "";
            float dbMix = 20 * Mathf.Log10(amt);
            if (dbMix == 0)
            {
                result = "-0.00 dB";
            }
            else if (float.IsInfinity(dbMix))
            {
                result = "-inf dB";
            }
            else
            {
                result = System.String.Format("{0:0.00}", dbMix) + " dB";
            }
            return result;
        }

        public bool ShouldCalculateMS(int index)
        {
            return true;
            //if (m_calculateRMSIntervalFrames <= 1)
            //{
            //    return true;
            //}
            //if ((index % m_calculateRMSIntervalFrames)
            //    == m_rmsFrame)
            //{
            //    return true;
            //}
            //else
            //{
            //    return false;
            //}
        }

        void Update()
        {
            // update mix buses first
            foreach(WingroveMixBus wmb in m_allMixBuses)
            {
                wmb.DoUpdate();
            }

            // then audio sources
            foreach(BaseWingroveAudioSource bwas in m_allRegisteredSources)
            {
                bwas.DoUpdate();
            }

            m_rmsFrame++;
            if (m_rmsFrame >= m_calculateRMSIntervalFrames)
            {
                m_rmsFrame = 0;
            }
#if !UNITY_WEBGL
            if (m_useDSPTime)
            {
                m_dspDeltaTime = AudioSettings.dspTime - m_lastDSPTime;
            }
            else
#endif
            {
                m_dspDeltaTime = Time.deltaTime;
            }
#if !UNITY_WEBGL
            m_lastDSPTime = AudioSettings.dspTime;
#endif
        }

        public static float GetDeltaTime()
        {
            return (float)m_dspDeltaTime;
        }

        public void RegisterListener(WingroveListener listener)
        {
            m_listeners.Add(listener);        
            if(m_thisListener == null)
            {
                m_thisListener = new GameObject("Listener");
                m_thisListener.transform.parent = transform;
                m_thisListener.AddComponent<AudioListener>();
                m_thisListener.transform.localPosition = m_listenerOffset;
            }
            if(m_listeningModel == MultipleListenerPositioningModel.Simplified)
            {
                m_thisListener.transform.parent = listener.transform;
                m_thisListener.transform.localPosition = Vector3.zero;
                m_thisListener.transform.localRotation = Quaternion.identity;
                m_thisListener.transform.localScale = Vector3.one;
            }
        }

        public void UnregisterListener(WingroveListener listener)
        {
            m_listeners.Remove(listener);
            Transform newParent = null;
            if (m_listeningModel == MultipleListenerPositioningModel.Simplified)
            {
                if (m_listeners.Count != 0)
                {
                    newParent = m_listeners[m_listeners.Count - 1].transform;
                }
            }
        }

        public WingroveListener GetSingleListener()
        {
            if(m_listeners.Count == 1)
            {
                return m_listeners[0];
            }
            else
            {
                return null;
            }
        }

        Vector3 GetRelativeListeningPositionSimplified(AudioArea aa, Vector3 inPosition)
        {
            int listenerCount = m_listeners.Count;
            if (listenerCount == 0)
            {
                return inPosition;
            }

            if (aa != null)
            {
                inPosition = aa.GetListeningPosition(m_listeners[0].transform.position, inPosition);
                // the matrix inaccuracies of transforming a position at the listener
                // to essentially the same place cause a weird flickering sound- so let's just return V3.zero
                if (inPosition == m_listeners[0].transform.position)
                {
                    return Vector3.zero;
                }
                else
                {
                    return inPosition;
                }
            }
            else
            {
                return inPosition;
            }

        }

        public Vector3 GetRelativeListeningPosition(AudioArea aa, Vector3 inPosition)
        {
            if (m_listeningModel == MultipleListenerPositioningModel.Simplified)
            {
                return GetRelativeListeningPositionSimplified(aa, inPosition);
            }
            else
            {
                int listenerCount = m_listeners.Count;
                if (!m_allowMultipleListeners || listenerCount <= 1)
                {
                    if (listenerCount == 0)
                    {
                        return inPosition;
                    }

                    if (aa != null)
                    {
                        inPosition = aa.GetListeningPosition(m_listeners[0].transform.position, inPosition);
                        // the matrix inaccuracies of transforming a position at the listener
                        // to essentially the same place cause a weird flickering sound- so let's just return V3.zero
                        if (inPosition == m_listeners[0].transform.position)
                        {
                            return Vector3.zero;
                        }
                        else
                        {
                            return m_listeners[0].transform.worldToLocalMatrix * new Vector4(inPosition.x, inPosition.y, inPosition.z, 1.0f);
                        }
                    }
                    else
                    {
                        return m_listeners[0].transform.worldToLocalMatrix * new Vector4(inPosition.x, inPosition.y, inPosition.z, 1.0f);
                    }
                }
                else
                {
                    if (m_listeningModel == MultipleListenerPositioningModel.InverseSquareDistanceWeighted)
                    {
                        float totalWeight = 0;
                        Vector3 totalPosition = Vector3.zero;
                        foreach (WingroveListener listener in m_listeners)
                        {
                            Vector3 dist = inPosition - listener.transform.position;
                            if (dist.magnitude == 0)
                            {
                                // early out if one is right here
                                return Vector3.zero;
                            }
                            else
                            {
                                float weight = 1 / (dist.magnitude * dist.magnitude);
                                totalWeight += weight;
                                totalPosition += (Vector3)(listener.transform.worldToLocalMatrix * new Vector4(inPosition.x, inPosition.y, inPosition.z, 1.0f)) * weight;
                            }
                        }
                        totalPosition /= totalWeight;
                        return totalPosition;
                    }
                    return Vector3.zero;
                }
            }
        }
    }

}
