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

        private GUISkin m_editorSkin;
        private static bool s_hasInstance;

        public static WingroveRoot Instance
        {
            get
            {
                if (!s_hasInstance)
                {
                    s_instance = (WingroveRoot)GameObject.FindObjectOfType(typeof(WingroveRoot));
                    if(s_instance != null)
                    {
                        s_hasInstance = true;
                    }
                }
                return s_instance;
            }
        }


        public static WingroveRoot InstanceEditor
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
        private int m_frameCtr;
        private static double m_lastDSPTime;
        private static double m_dspDeltaTime;

        public class AudioSourcePoolItem
        {
            public AudioSource m_audioSource;
            public PooledAudioSource m_pooledAudioSource;
            public ActiveCue m_user;
            public int m_useCount;
        }
        List<AudioSourcePoolItem> m_audioSourcePool = new List<AudioSourcePoolItem>(128);
        HashSet<AudioSourcePoolItem> m_freeAudioSources = new HashSet<AudioSourcePoolItem>();

        Dictionary<string, List<BaseEventReceiveAction>> m_eventReceivers = new Dictionary<string, List<BaseEventReceiveAction>>();

        [SerializeField]
        public AudioNameGroup[] m_audioNameGroups;

        private List<WingroveListener> m_listeners = new List<WingroveListener>();
        private int m_listenerCount = 0;
        private List<BaseWingroveAudioSource> m_allRegisteredSources = new List<BaseWingroveAudioSource>();
        private List<WingroveMixBus> m_allMixBuses = new List<WingroveMixBus>();
        private List<InstanceLimiter> m_allInstanceLimiters = new List<InstanceLimiter>();

        public class CachedParameterValue
        {
            public float m_valueNull;
            public Dictionary<int, float> m_valueObject = new Dictionary<int, float>();
            public Dictionary<int, GameObject> m_nullCheckDictionary = new Dictionary<int, GameObject>();            
            public bool m_isGlobalValue;
            public System.Int64 m_lastNonGlobalValueN = 0;
        }
		
		class ParameterValues
		{
			public Dictionary<int, CachedParameterValue> m_parameterValues = new Dictionary<int, CachedParameterValue>();
            public List<CachedParameterValue> m_parameterValuesFast = new List<CachedParameterValue>();
            public Dictionary<string, int> m_parameterToIntValues = new Dictionary<string, int>();
            public Dictionary<int, string> m_intToParameterValues = new Dictionary<int, string>();
        }
		ParameterValues m_values = new ParameterValues();
        private GameObject m_thisListener;
        private int m_cachedCurrentVoices = 0;

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
                CreateAudioSource(false);
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


            if (m_thisListener == null)
            {
                m_thisListener = new GameObject("Listener");
                m_thisListener.transform.parent = transform;
                m_thisListener.AddComponent<AudioListener>();
                m_thisListener.transform.localPosition = m_listenerOffset;
            }
        
            transform.position = Vector3.zero;
            m_lastDSPTime = AudioSettings.dspTime;
#if UNITY_EDITOR
            m_startTime = AudioSettings.dspTime;
#endif

            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += SceneChanged;
        }

        private int m_slowConstantCtr = 0;
        private List<int> m_toRemoveParamsIntCached = new List<int>(32);
        void ClearParamSlowConstant(int count)
        {
            int numDone = 0;
            if (m_values.m_parameterValuesFast.Count != 0)
            {
                CachedParameterValue cpv = m_values.m_parameterValuesFast[m_slowConstantCtr %
                    m_values.m_parameterValuesFast.Count];
                if (!cpv.m_isGlobalValue)
                {
                    foreach (KeyValuePair<int, GameObject> kvp in cpv.m_nullCheckDictionary)
                    {
                        if (kvp.Value == null)
                        {
                            m_toRemoveParamsIntCached.Add(kvp.Key);
                            numDone++;
                            if (count != 0 && numDone == count)
                            {
                                break;
                            }
                        }
                    }
                    if (numDone > 0)
                    {
                        foreach (int tr in m_toRemoveParamsIntCached)
                        {
                            cpv.m_nullCheckDictionary.Remove(tr);
                            cpv.m_valueObject.Remove(tr);
                        }
                        m_toRemoveParamsIntCached.Clear();
                    }
                }
            }
            m_slowConstantCtr++;

        }
        
        public void RegisterIL(InstanceLimiter il)
        {
            m_allInstanceLimiters.Add(il);
        }
        
        void ClearParams(int count)
        {
            int numDone = 0;            
            foreach (KeyValuePair<int, CachedParameterValue> cpv in m_values.m_parameterValues)
            {
                if (!cpv.Value.m_isGlobalValue)
                {
                    foreach (KeyValuePair<int, GameObject> kvp in cpv.Value.m_nullCheckDictionary)
                    {
                        if(kvp.Value == null)
                        {
                            m_toRemoveParamsIntCached.Add(kvp.Key);
                            numDone++;
                            if(count != 0 && numDone == count)
                            {
                                break;
                            }
                        }
                    }
                    if (numDone > 0)
                    {
                        foreach (int tr in m_toRemoveParamsIntCached)
                        {
                            cpv.Value.m_nullCheckDictionary.Remove(tr);
                            cpv.Value.m_valueObject.Remove(tr);
                        }
                        m_toRemoveParamsIntCached.Clear();
                    }
                }
                if (count != 0 && numDone >= count)
                {
                    break;
                }
            }

        }

        public int GetParameterId(string fromParameter)
        {
            int result = 0;
            if ( !m_values.m_parameterToIntValues.TryGetValue(fromParameter, out result) )
            {
                result = m_values.m_parameterToIntValues.Count + 1;
                m_values.m_parameterToIntValues[fromParameter] = result;
                m_values.m_intToParameterValues[result] = fromParameter;
            }
            return result;
        }

        void SceneChanged(UnityEngine.SceneManagement.Scene sca, UnityEngine.SceneManagement.Scene scb)
        {
            // reset has instance...just in case.
            s_hasInstance = false;
            // full clear on scene change...
            ClearParams(0);
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
                GUI.BeginGroup(new Rect(0,0,600,1000),"", "box");
                GUI.Label(new Rect(pos.x,pos.y,300,20),"Number of sources: " + m_audioSourcePool.Count);
                pos.y+=20;
                foreach(AudioSourcePoolItem aspi in m_audioSourcePool)
                {
                    if ( aspi.m_user != null )
                    {
                        GUI.Label(new Rect(pos.x,pos.y,500,20), aspi.m_audioSource.clip.name + " @ " + aspi.m_user.GetState() + " " + (int)(100 * aspi.m_audioSource.time / aspi.m_audioSource.clip.length) + " " + aspi.m_useCount);
                    }
                    else
                    {
                        GUI.Label(new Rect(pos.x,pos.y,500,20), "Unused : " + aspi.m_useCount);
                    }

                    pos.y+=20;
                    if(pos.y>=980)
                    {
                        pos.y = 40;
                        pos.x += 300;
                    }
                }
                GUI.EndGroup();
            }
        }

        public CachedParameterValue GetParameter(int parameter)
        {
            CachedParameterValue result;
            m_values.m_parameterValues.TryGetValue(parameter, out result);
            return result;
        }
        
		public float GetParameterForGameObject(int parameter, int gameObjectId)
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
                    result.m_valueObject.TryGetValue(gameObjectId, out resultF);
                    return resultF;
                }
            }
		}
        
		public void SetParameterGlobal(int parameter, float setValue)
		{
            CachedParameterValue cpv = null;
            m_values.m_parameterValues.TryGetValue(parameter, out cpv);
            if(cpv == null)
            {
                cpv = new CachedParameterValue();
                m_values.m_parameterValues[parameter] = cpv;
                m_values.m_parameterValuesFast.Add(cpv);
            }
            cpv.m_valueNull = setValue;            
            cpv.m_isGlobalValue = true;
		}

        public void SetParameterForObject(int parameter, int gameObjectId, GameObject go, float setValue)
		{
            CachedParameterValue cpv = null;
            m_values.m_parameterValues.TryGetValue(parameter, out cpv);
            if (cpv == null)
            {
                cpv = new CachedParameterValue();
                m_values.m_parameterValues[parameter] = cpv;                
                m_values.m_parameterValuesFast.Add(cpv);
            }
            cpv.m_valueObject[gameObjectId] = setValue;
            cpv.m_nullCheckDictionary[gameObjectId] = go;
            cpv.m_lastNonGlobalValueN++;
            cpv.m_isGlobalValue = false;
		}

        public Dictionary<int, CachedParameterValue> GetAllParams()
        {
            return m_values.m_parameterValues;
        }        

        public string GetParamName(int val)
        {
            return m_values.m_intToParameterValues[val];
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

        public void PostEventGOAA(string eventName, GameObject targetObject, AudioArea aa)
        {
            PostEventGOAA(eventName, targetObject, null, aa);
        }

        public void PostEventGOAA(string eventName, GameObject targetObject, List<ActiveCue> cuesOut, AudioArea aa)
        {
#if UNITY_EDITOR
            LogEvent(eventName, targetObject);
#endif
            List<BaseEventReceiveAction> listOfReceivers = null;
            if (m_eventReceivers.TryGetValue(eventName, out listOfReceivers))
            {
                foreach (BaseEventReceiveAction evr in listOfReceivers)
                {
                    evr.PerformAction(eventName, targetObject, aa, cuesOut);
                }
            }
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
            AudioSourcePoolItem bestQuick = null;
            foreach(AudioSourcePoolItem aspi in m_freeAudioSources)
            {
                if(aspi.m_user == null || aspi.m_user.GetState() == ActiveCue.CueState.Stopped)
                {
                    bestQuick = aspi;
                    break;
                }
            }
            if(bestQuick != null)
            {
                if (bestQuick.m_user != null)
                {
                    bestQuick.m_user.Virtualise();
                }
                m_freeAudioSources.Remove(bestQuick);
                bestQuick.m_user = cue;
                return bestQuick;
            }

            AudioSourcePoolItem bestSteal = null;
            int lowestImportance = cue.GetImportance();
            float quietestSimilarImportance = 1.0f;
            foreach (AudioSourcePoolItem aspi in m_audioSourcePool)
            {
                if (aspi.m_user == null || aspi.m_user.GetState() == ActiveCue.CueState.Stopped)
                {
                    if (aspi.m_user != null)
                    {
                        aspi.m_user.Virtualise();
                    }
                    aspi.m_user = cue;                    
                    m_freeAudioSources.Remove(aspi);
                    return aspi;
                }
                else
                {
                    int aspiImportance = aspi.m_user.GetImportance();
                    if (aspiImportance < cue.GetImportance())
                    {
                        if (aspiImportance < lowestImportance)
                        {
                            lowestImportance = aspi.m_user.GetImportance();
                            bestSteal = aspi;
                        }
                        else if (aspiImportance == lowestImportance)
                        {
                            if (aspi.m_user.GetState() == ActiveCue.CueState.PlayingFadeOut )
                            {
                                bestSteal = aspi;
                            }
                        }
                    }
                    else if (aspiImportance == lowestImportance)
                    {
                        if (aspi.m_user.GetState() == ActiveCue.CueState.PlayingFadeOut ||
                            aspi.m_user.GetTheoreticalVolumeCached() < quietestSimilarImportance)
                        {
                            quietestSimilarImportance = aspi.m_user.GetTheoreticalVolumeCached();
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
                m_freeAudioSources.Remove(bestSteal);
                return bestSteal;
            } else if (m_audioSourcePool.Count < m_audioSourcePoolSize)
            {
                AudioSourcePoolItem aspi = CreateAudioSource(true);
                aspi.m_user = cue;
                return aspi;
            } else
            {
                return null;
            }
        }

        private AudioSourcePoolItem CreateAudioSource(bool usingInstantly)
        {
            GameObject newAudioSource = new GameObject("PooledAudioSource_"+ m_audioSourcePool.Count);
            newAudioSource.transform.parent = m_pool.transform;

            AudioSource aSource = newAudioSource.AddComponent<AudioSource>();
            

            AudioSourcePoolItem aspi = new AudioSourcePoolItem();
            aspi.m_audioSource = aSource;
            aspi.m_pooledAudioSource = newAudioSource.AddComponent<PooledAudioSource>();
            aSource.enabled = false;
            m_audioSourcePool.Add(aspi);
            if (!usingInstantly)
            {
                m_freeAudioSources.Add(aspi);
            }

            return aspi;
        }

        public void UnlinkSource(AudioSourcePoolItem item, bool fromVirtualise)
        {
#if UNITY_PS4
            item.m_useCount++;
            if (item.m_useCount > 35 && !fromVirtualise)
            {
                Destroy(item.m_audioSource.gameObject);
                item.m_user = null;
                m_audioSourcePool.Remove(item);
                if (m_audioSourcePool.Count < m_audioSourcePoolSize)
                {
                    AudioSourcePoolItem alwaysCreate = CreateAudioSource(false);
                }                
                return;
            }
            else
            {
                item.m_audioSource.Stop();
                item.m_audioSource.enabled = false;
                item.m_audioSource.clip = null;
                item.m_user = null;
                m_freeAudioSources.Add(item);
            }
#else
            item.m_audioSource.Stop();
            item.m_audioSource.enabled = false;
            item.m_audioSource.clip = null;
            item.m_user = null;
            m_freeAudioSources.Add(item);
#endif
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
#if UNITY_EDITOR
            if(Input.GetKeyDown(KeyCode.P))
            {
                List<BaseWingroveAudioSource> sortedList = new List<BaseWingroveAudioSource>();
                sortedList.AddRange(m_allRegisteredSources);
                sortedList.Sort((a, b) =>
                {
                    if (a.GetAudioClip().length / a.GetPitchMax() > b.GetAudioClip().length / b.GetPitchMax())
                    {
                        return 1;
                    }
                    else if(a.GetAudioClip().length / a.GetPitchMax() < b.GetAudioClip().length / b.GetPitchMax())
                    {
                        return -1;
                    }
                    else
                    {
                        return 0;
                    }
                });
                string output = "";
                foreach(BaseWingroveAudioSource bwas in sortedList)
                {
                    if(bwas.GetAudioClip().length < 1.0f)
                    {
                        output += bwas.GetAudioClip().name + " : " + bwas.GetAudioClip().length / bwas.GetPitchMax() + "\n";
                    }
                }
                Debug.Log(output);
            }
#endif

            // do some clearing of old parameters...
            ClearParamSlowConstant(1);
            m_frameCtr = (m_frameCtr + 1) % 512;

            if (m_listenerCount > 0)
            {
                GetSingleListener().UpdatePosition();
            }

            // update mix buses first
            foreach (WingroveMixBus wmb in m_allMixBuses)
            {
                wmb.DoUpdate();
            }

            // then audio sources
            foreach(BaseWingroveAudioSource bwas in m_allRegisteredSources)
            {
                bwas.DoUpdate(m_frameCtr);
            }

            foreach(InstanceLimiter il in m_allInstanceLimiters)
            {
                il.ResetFrameFlags();
            }

            m_cachedCurrentVoices = 0;
            foreach (AudioSourcePoolItem aspi in m_audioSourcePool)
            {
                if(aspi.m_user != null)
                {
                    m_cachedCurrentVoices++;
                }
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

        public int GetCurrentVoices()
        {
            return m_cachedCurrentVoices;
        }

        public int GetMaxVoices()
        {
#if UNITY_SWITCH
            return 32;
#else
            return m_audioSourcePoolSize;
#endif
        }

        public bool IsCloseToMax()
        {
            return m_cachedCurrentVoices > m_audioSourcePoolSize - 8;
        }

        public void RegisterListener(WingroveListener listener)
        {
            m_listeners.Add(listener);
            m_listenerCount++;
            if(m_thisListener == null)
            {
                m_thisListener = new GameObject("Listener");
                m_thisListener.transform.parent = transform;
                m_thisListener.AddComponent<AudioListener>();
                m_thisListener.transform.localPosition = m_listenerOffset;
            }
            m_thisListener.transform.parent = listener.transform;
            m_thisListener.transform.localPosition = Vector3.zero;
            m_thisListener.transform.localRotation = Quaternion.identity;
            m_thisListener.transform.localScale = Vector3.one;
            while(m_listeners.Contains(null))
            {
                m_listeners.Remove(null);
                m_listenerCount = m_listeners.Count;
            }
        }

        public void UnregisterListener(WingroveListener listener)
        {
            m_listeners.Remove(listener);
            m_listenerCount--;
            Transform newParent = null;
            if (m_listenerCount != 0)
            {
                newParent = m_listeners[m_listenerCount - 1].transform;
            }
        }

        public WingroveListener GetSingleListener()
        {
            if(m_listenerCount == 1)
            {
                return m_listeners[0];
            }
            else
            {
                return null;
            }
        }

        public Vector3 GetRelativeListeningPosition(Vector3 inPosition)
        {
            return inPosition;
        }

        public Vector3 GetRelativeListeningPosition(AudioArea aa, Vector3 inPosition)
        {
            if (aa != null)
            {
                inPosition = aa.GetListeningPosition(m_listeners[0].transform.position, inPosition);
                if (inPosition == m_listeners[0].transform.position)
                {
                    return m_listeners[0].transform.position;
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
    }

}
