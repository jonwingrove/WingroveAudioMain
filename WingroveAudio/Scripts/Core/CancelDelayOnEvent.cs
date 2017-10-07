using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace WingroveAudio
{
    [AddComponentMenu("WingroveAudio/Cancel Delay Event Action")]
    public class CancelDelayOnEvent : BaseEventReceiveAction
    {

      

        [SerializeField]
        [AudioEventName]
        private string m_event = "";
        [SerializeField]
        private EventReceiveAction[] m_toCancel;

        public override string[] GetEvents()
        {
            return new string[] { m_event };
        }
        
        public override void PerformAction(string eventName, GameObject targetObject, List<ActiveCue> cuesOut)
        {            
            foreach(EventReceiveAction era in m_toCancel)
            {
                era.CancelDelay();
            }
        }

        public override void PerformAction(string eventName, List<ActiveCue> cuesIn, List<ActiveCue> cuesOut)
        {
            foreach (EventReceiveAction era in m_toCancel)
            {
                era.CancelDelay();
            }
        }
    }

}