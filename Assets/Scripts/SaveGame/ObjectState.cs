using System;
using UnityEngine;

public class ObjectState
{
    public Guid id;
    public string label;
    public Vector3 position;
    public Quaternion rotation;
    public float voltage = 0;
    public float resistance = 0;
    public float ledVoltage;
    public bool isGround;
}

