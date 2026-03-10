using System;
using System.IO;
using UnityEngine;

public class SavePaths
{
    readonly SaveDebug d = new SaveDebug("<color=#26A69A>[SavePaths] </color>");
    
    public string progPath = "";
    public string saveRootPath = "";
    public string saveFilesPath = "";
    public string manFilePath = "";

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

        // Path for save manifest file
        string manFileName = "saveManifest.json";
        manFilePath = Path.Combine(saveRootPath, manFileName);

        // Path for save files folder
        saveFilesPath = Path.Combine(saveRootPath, "saveFiles");
    }

    // Creates the saveFiles folder, if it does not exist
    public void EnsureSaveFolderExists()
    {
        d.Log("Searching for save file directory");

        // Do nothing if the directory already exists
        if (Directory.Exists(saveFilesPath)) 
            { d.Success("Save file directory FOUND."); return; }
        
        // Otherwise, generate all necessary directories
        d.Warn("Save file directory NOT FOUND.");
        d.Log($"CREATING {saveFilesPath}");
        Directory.CreateDirectory(saveFilesPath);
    }

    public void EnsureManifestFileExists()
    {
        d.Log("Searching for save manifest file");
        
        // Do nothing if the file already exists
        if (File.Exists(manFilePath)) 
            { d.Success($"{manFilePath} FOUND"); return; }

        // Otherwise, generate file and all necessary directories
        d.Warn("save manifest file NOT FOUND.");
        d.Log($"CREATING {manFilePath}");

        // Get the default JSON data
        SaveManifest emptyMan = new SaveManifest();
        string emptyMan_serial = 
            JsonUtility.ToJson(emptyMan, prettyPrint: true);
        
        // Populate the empty file with default data
        File.WriteAllText(manFilePath, emptyMan_serial);
    }
}
