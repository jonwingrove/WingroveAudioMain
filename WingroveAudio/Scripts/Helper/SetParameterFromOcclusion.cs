using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WingroveAudio
{

    public class SetParameterFromOcclusion : MonoBehaviour
    {

        [SerializeField]
        [AudioParameterName]
        private string m_parameterToSet;
        [SerializeField]
        private float m_distanceContributionPerUnit = 0;
        [SerializeField]
        private float m_fadeSpeedLinear = 1.0f;
        [SerializeField]
        private float m_fadeSpeedRelative = 0.0f;
        [SerializeField]
        private float m_contributionPerCollider = 1.0f;
        [SerializeField]
        private LayerMask m_layersToRaycast;
        [SerializeField]
        private float m_ignoreDistanceNear;
        [SerializeField]
        private float m_ignoreDistanceFar;        
        [SerializeField]
        private bool m_forObject = true;

        private float m_occlusion = 0;
        private bool m_hasRun = false;

        // Update is called once per frame
        void Update()
        {
            float targetOcclusion = 0;
            WingroveListener listener = WingroveRoot.Instance.GetSingleListener();
            if(listener != null)
            {
                Vector3 delta = listener.transform.position - transform.position;
                Vector3 startPos = transform.position + delta.normalized * m_ignoreDistanceNear;

                targetOcclusion += delta.magnitude * m_distanceContributionPerUnit;

                RaycastHit[] results = Physics.RaycastAll(startPos, delta.normalized, delta.magnitude - (m_ignoreDistanceFar + m_ignoreDistanceNear), m_layersToRaycast, QueryTriggerInteraction.Ignore);
                foreach(RaycastHit rr in results)
                {
                    targetOcclusion += m_contributionPerCollider;
                }
            }

            if(!m_hasRun)
            {
                m_occlusion = targetOcclusion;
                m_hasRun = true;
            }
            else
            {
                if(targetOcclusion > m_occlusion)
                {
                    m_occlusion += (targetOcclusion - m_occlusion) * m_fadeSpeedRelative * Time.deltaTime;
                    m_occlusion += m_fadeSpeedLinear * Time.deltaTime;
                    
                    if(m_occlusion > targetOcclusion)
                    {
                        m_occlusion = targetOcclusion;
                    }
                }

                if (targetOcclusion < m_occlusion)
                {
                    m_occlusion += (targetOcclusion - m_occlusion) * m_fadeSpeedRelative * Time.deltaTime;
                    m_occlusion -= m_fadeSpeedLinear * Time.deltaTime;

                    if (m_occlusion < targetOcclusion)
                    {
                        m_occlusion = targetOcclusion;
                    }
                }
            }

            if (m_forObject)
            {
                WingroveRoot.Instance.SetParameterForObject(m_parameterToSet, gameObject, m_occlusion);
            }
            else
            {
                WingroveRoot.Instance.SetParameterGlobal(m_parameterToSet, m_occlusion);
            }
        }
    }

}