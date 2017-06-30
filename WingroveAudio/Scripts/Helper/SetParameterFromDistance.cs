using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WingroveAudio
{

    public class SetParameterFromDistance : MonoBehaviour
    {

        [SerializeField]
        [AudioParameterName]
        private string m_parameterToSet;
        [SerializeField]
        private float m_minDist = 0;
        [SerializeField]
        private float m_maxDist = 0;
        [SerializeField]
        private bool m_smooth;
        [SerializeField]
        private bool m_forObject;
        [SerializeField]
        private AudioArea m_useAudioArea;

        private int m_cachedGameObjectId;
        private int m_parameterId;

        // Update is called once per frame
        void Update()
        {
            float targetValue = 0;
            WingroveListener listener = WingroveRoot.Instance.GetSingleListener();
            if(listener != null)
            {
                float delta = (listener.transform.position - transform.position).magnitude;
                if (m_useAudioArea != null)
                {
                    Vector3 aaPos = m_useAudioArea.GetListeningPosition(listener.transform.position, m_useAudioArea.transform.position);
                    delta = (listener.transform.position - aaPos).magnitude;
                }
                
                targetValue = Mathf.Clamp01((delta - m_minDist) / (m_maxDist - m_minDist));
            }


            if (m_parameterId == 0)
            {
                m_parameterId = WingroveRoot.Instance.GetParameterId(m_parameterToSet);
            }

            if (m_forObject)
            {
                if(m_cachedGameObjectId == 0)
                {
                    m_cachedGameObjectId = gameObject.GetInstanceID();
                }
                WingroveRoot.Instance.SetParameterForObject(m_parameterId, m_cachedGameObjectId, gameObject, targetValue);
            }
            else
            {
                WingroveRoot.Instance.SetParameterGlobal(m_parameterId, targetValue);
            }
        }
    }

}