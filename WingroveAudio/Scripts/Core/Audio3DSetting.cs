using UnityEngine;
using System.Collections;

namespace WingroveAudio
{

    public class Audio3DSetting : ScriptableObject
    {
        [SerializeField]
        private bool m_linearRolloff = true;
        [SerializeField]
        private float m_maxDistance = 100.0f;
        [SerializeField]
        private float m_minDistance = 1.0f;
        [SerializeField]
        private float m_dopplerMultiplier = 1.0f;
        [SerializeField]
        private bool m_useDynamicSpatialBlend = false;
        [SerializeField]
        [Range(0,1)]
        private float m_blendValueNear = 1.0f;
        [SerializeField]
        [Range(0, 1)]
        private float m_blendValueFar = 1.0f;
        [SerializeField]
        private float m_blendNearDistance = 0.0f;
        [SerializeField]
        private float m_blendFarDistance = 100.0f;

        public float GetMaxDistance()
        {
            return m_maxDistance;
        }
        public float GetMinDistance()
        {
            return m_minDistance;
        }
        public AudioRolloffMode GetRolloffMode()
        {
            return m_linearRolloff == true ? AudioRolloffMode.Linear : AudioRolloffMode.Logarithmic;
        }
        public float EvaluateStandard(float distance)
        {
            float ab = 1 - Mathf.Clamp01((distance - m_minDistance) / (m_maxDistance - m_minDistance));
            return ab;
        }
        public float GetSpatialBlend(float distance)
        {
            if(m_useDynamicSpatialBlend)
            {
                float distT = (distance - m_blendNearDistance) / (m_blendFarDistance - m_blendNearDistance);
                return Mathf.Lerp(m_blendValueNear, m_blendValueFar, Mathf.Clamp01(distT));
            }
            else
            {
                return m_blendValueNear;
            }
        }
        public float GetDopplerLevel()
        {
            return m_dopplerMultiplier;
        }
    }

}