using UnityEngine;
using System.Collections;
namespace WingroveAudio
{
    [AddComponentMenu("WingroveAudio/Event Triggers/Animation Event Trigger")]
    public class AnimationEventTrigger : MonoBehaviour
    {
        [SerializeField]
        [AudioEventName]
        private string m_audioEvent = "";

        public void OnAnimationTrigger()
        {
            WingroveRoot.Instance.PostEvent(m_audioEvent);
        }
    }
}