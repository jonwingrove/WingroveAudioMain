using UnityEngine;
using System.Collections;

public class AudioSpamTester : MonoBehaviour {

    [SerializeField]
    private float m_timer;
    [SerializeField]
    private string m_event;

    private float m_t;
	
	// Update is called once per frame
	void Update () {

        m_t += Time.deltaTime;
        if (m_timer > 0)
        {
            while (m_t > m_timer)
            {
                WingroveAudio.WingroveRoot.Instance.PostEventGO(m_event, gameObject);
                m_t -= m_timer;
            }
        }
	}
}
