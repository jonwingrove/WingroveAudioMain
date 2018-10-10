using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetSurroundSoundOptions : MonoBehaviour {

    [SerializeField]
    private List<AudioSpeakerMode> m_preferredSpeakerModesInOrder;
    [SerializeField]
    private List<AudioSpeakerMode> m_outputModesInOrder;

    // Use this for initialization
    void Start ()
    {
        AudioSettings.OnAudioConfigurationChanged += OnAudioConfigChanged;

        OnAudioConfigChanged(true);
    }

    void OnAudioConfigChanged(bool deviceWasChanged)
    {
        if (deviceWasChanged)
        {
            AudioConfiguration ac = AudioSettings.GetConfiguration();
            AudioSpeakerMode asm = AudioSettings.driverCapabilities;
            AudioSpeakerMode targMode = AudioSpeakerMode.Stereo;
            if (m_preferredSpeakerModesInOrder.Contains(asm))
            {
                targMode = m_outputModesInOrder[m_preferredSpeakerModesInOrder.IndexOf(asm)];
            }
            // don't change it unless we really have to...
            if (targMode != ac.speakerMode)
            {
                ac.speakerMode = targMode;
                AudioSettings.Reset(ac);
            }
            DebugLogWrapper.Log("[SURROUND SOUND] mode: " + ac.speakerMode);
        }
    }
	
}
