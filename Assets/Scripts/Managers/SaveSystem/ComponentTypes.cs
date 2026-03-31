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
        WIRE_START,
        WIRE_END,
        DIRECT_CURRENT,
        RESISTOR,
        LED,      
        GROUND,
        SWITCH,
        CAPACITOR,
        BLOCK,
        OTHER,
        TYPES_COUNT // This one must always be last
    }

    // Initializes counter for each component type to zero and name to the enum
    // constant, converted to lowercase.
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

    // Restores the type counters from save data. 
    public void RestoreTypeCounters(SaveData sd)
    {
        // Get max index for each component type
        int[] maxTypeIndices = new int[(int)ComponentTypes.Types.TYPES_COUNT];
        foreach (ObjectState obj in sd.objectStates.Values)
        {
            int i = (int)obj.type;
            if (obj.index > maxTypeIndices[i]) maxTypeIndices[i] = obj.index;
        }

        // Restore counters
        for (int i = 0; i < (int)Types.TYPES_COUNT; i++)
            typeCounters[i] = maxTypeIndices[i];
    }

    // Resets all counters to 0
    public void ResetTypeCounters()
    {
        for (int i = 0; i < (int)Types.TYPES_COUNT; i++)
            typeCounters[i] = 0;
    }

    // Exposes next available index for specified type for label suggestion.
    public int GetNextTypeIndex(Types type) {return ++typeCounters[(int)type];}

    // Exposes specified type name
    public string GetTypeName(Types type) { return typeNames[(int)type]; }
}
