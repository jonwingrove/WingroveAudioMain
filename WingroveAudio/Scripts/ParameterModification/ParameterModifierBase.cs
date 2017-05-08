using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ParameterModifierBase : MonoBehaviour
{
    public virtual float GetVolumeMultiplier(GameObject linkedObject)
    {
        return 1.0f;
    }
    public virtual float GetPitchMultiplier(GameObject linkedObject)
    {
        return 1.0f;
    }
}
