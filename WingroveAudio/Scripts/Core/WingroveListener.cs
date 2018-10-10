using UnityEngine;
using System.Collections;
namespace WingroveAudio
{
    [AddComponentMenu("WingroveAudio/Wingrove Listeners")]
    public class WingroveListener : MonoBehaviour
    {
        private Vector3 m_position;        

        public void UpdatePosition()
        {
            m_position = transform.position;
        }

        public Vector3 GetPosition()
        {
            return m_position;
        }

        // Use this for initialization
        void OnEnable()
        {
            if (WingroveRoot.Instance != null)
            {
                WingroveRoot.Instance.RegisterListener(this);
            }
        }

        void OnDisable()
        {
            if (WingroveRoot.Instance != null)
            {
                WingroveRoot.Instance.UnregisterListener(this);
            }
        }
    }

}