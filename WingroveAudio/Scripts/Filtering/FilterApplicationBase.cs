using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class FilterApplicationBase : MonoBehaviour {    
    public abstract void UpdateFor(PooledAudioSource playingSource, GameObject linkedObject);    
}
