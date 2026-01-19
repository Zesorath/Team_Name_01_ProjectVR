using UnityEngine;
using System.Collections.Generic;
using System;

public class ComponentTypes
{
    List<int> typeCounters;
    List<string> typeNames;
    
    public enum Types
    {
        DEFAULT,
        WIRE,
        POWER_SOURCE,
        RESISTOR,
        LED,      
        GROUND,    
        OTHER,
        TYPES_COUNT
    }

    /// <summary>
    /// 
    /// REWRITE THIS
    /// 
    /// Runs on load scene
    /// 
    /// Creates static ComponentTypes instance. Initializes counter for each
    /// component type to zero and name to the enum constant, converted to
    /// lowercase.
    /// </summary>
    public ComponentTypes()
    {
        typeCounters = new List<int>();
        typeNames = new List<string>();

        for ( int i = 0 ; i < (int)Types.TYPES_COUNT ; i++ )
        {
            // Initialize counters
            typeCounters.Add(0);

            // Initialize name strings to lowercase, replacing underscores with 
            // spaces
            string name = Enum.GetName(typeof(Types), i);
            typeNames.Add(name.ToLower().Replace("_"," "));
        }
    }

    /// <summary>
    /// Exposes next available index for specified type for label suggestion.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public int GetNextTypeIndex(Types type)
    {
        typeCounters[(int)type]++;
        return typeCounters[(int)type];
    }

    /// <summary>
    /// Exposes specified type name
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public string GetTypeName(Types type) { return typeNames[(int)type]; }
}
