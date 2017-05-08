using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaderInterfaceAttribute : PropertyAttribute {

    private bool m_horizontal = false;

    public FaderInterfaceAttribute()
    {

    }

    public FaderInterfaceAttribute(bool horizontalLayout)
    {
        m_horizontal = horizontalLayout;
    }

    public bool IsHorizontal()
    {
        return m_horizontal;
    }

}
