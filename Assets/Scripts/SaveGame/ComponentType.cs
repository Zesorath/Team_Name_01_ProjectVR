using System;
using UnityEngine;

public class ComponentType : MonoBehaviour
{
    [Tooltip("Component type (i.e., resistor, LED, etc.)")]
    public SaveManager.Type type;
}
