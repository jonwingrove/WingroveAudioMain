using UnityEngine;
using System.Collections;
using System.Collections.Generic;
namespace WingroveAudio
{
    public abstract class BaseEventReceiveAction : MonoBehaviour
    {

        public abstract string[] GetEvents();

        public abstract void PerformAction(string eventName, GameObject targetObject, List<ActiveCue> cuesOut);
        public abstract void PerformAction(string eventName, List<ActiveCue> cuesIn, List<ActiveCue> cuesOut);

    }
}