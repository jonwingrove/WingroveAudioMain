using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ParameterModifierBase : MonoBehaviour
{
    public virtual float GetVolumeMultiplier(int linkedObjectId)
    {
        return 1.0f;
    }
    public virtual float GetPitchMultiplier(int linkedObjectId)
    {
        return 1.0f;
    }
}
