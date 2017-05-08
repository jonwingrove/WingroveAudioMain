using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioArea : MonoBehaviour
{

    [SerializeField]
    private Vector3 m_centreOffset;
    [SerializeField]
    private Vector3 m_size;

    public Vector3 GetListeningPosition(Vector3 audioCtrPos, Vector3 myRelativePos)
    {
        Vector3 maxCorn = myRelativePos + m_centreOffset + (m_size * 0.5f);
        Vector3 minCorn = myRelativePos + m_centreOffset - (m_size * 0.5f);

        Vector3 result = audioCtrPos;

        if (audioCtrPos.x < minCorn.x)
        {
            result.x = minCorn.x;
        }
        else if (audioCtrPos.x > maxCorn.x)
        {
            result.x = maxCorn.x;
        }

        if (audioCtrPos.y < minCorn.y)
        {
            result.y = minCorn.y;
        }
        else if (audioCtrPos.y > maxCorn.y)
        {
            result.y = maxCorn.y;
        }

        if (audioCtrPos.z < minCorn.z)
        {
            result.z = minCorn.z;
        }
        else if (audioCtrPos.z > maxCorn.z)
        {
            result.z = maxCorn.z;
        }

        return result;
    }

    public void SetSize(Vector3 size)
    {
        m_size = size;
    }

    public void SetCentreOffset(Vector3 cOff)
    {
        m_centreOffset = cOff;
    }

    void OnDrawGizmosSelected()
    {
        Color c = Gizmos.color;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + m_centreOffset, m_size);
        Gizmos.color = c;
    }

}