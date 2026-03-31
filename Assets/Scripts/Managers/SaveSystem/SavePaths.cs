using System;
using System.IO;
using UnityEngine;

public class SavePaths
{
    public string progPath = "";
    public string saveRootPath = "";
    public string saveFilesPath = "";

    public SavePaths()
    {
        // Path for CircuitSimVR root folder in AppData/Roaming/
        progPath = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData
            ), "CircuitSimVR"
        );

        // Path for save system utility files root
        saveRootPath = Path.Combine(progPath, "saveSystem");

        // Path for save files folder
        saveFilesPath = Path.Combine(saveRootPath, "saveFiles");
    }
}
