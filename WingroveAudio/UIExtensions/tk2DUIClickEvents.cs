using UnityEngine;
using System.Collections;
namespace WingroveAudio
{
    [AddComponentMenu("WingroveAudio/Event Triggers/tk2D Click Events Trigger")]
    public class tk2DUIClickEvents : MonoBehaviour
    {

        public bool m_fireEventOnPress;
        [AudioEventName]
        public string m_onPressEvent;

        public bool m_fireEventOnClick;
        [AudioEventName]
        public string m_onClickEvent;

        public bool m_fireEventOnRelease;
        [AudioEventName]
        public string m_onReleaseEvent;

        public bool m_fireEventOnDoubleClick;
        [AudioEventName]
        public string m_onDoubleClickEvent;

        void Awake()
        {
            tk2dUIItem uiI = GetComponent<tk2dUIItem>();
            if (uiI != null)
            {
                uiI.OnClick+= OnClick;
                uiI.OnDown+=OnPress;
                uiI.OnUp+=OnRelease;
            }
        }

        void OnPress()
        {
            if (WingroveRoot.Instance != null)
            {
                if (m_fireEventOnPress)
                {
                    WingroveRoot.Instance.PostEvent(m_onPressEvent);
                }
            }

        }

        void OnRelease()
        {
            if (WingroveRoot.Instance != null)
            {
                if (m_fireEventOnRelease)
                {
                    WingroveRoot.Instance.PostEvent(m_onReleaseEvent);
                }
            }
        }

        void OnClick()
        {
            if (WingroveRoot.Instance != null)
            {
                if (m_fireEventOnClick)
                {
                    WingroveRoot.Instance.PostEvent(m_onClickEvent);
                }
            }
        }

    }
}