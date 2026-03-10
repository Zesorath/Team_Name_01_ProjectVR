using System;
using System.IO;
using UnityEngine;

public class SavePaths
{

    [NonSerialized] SaveDebug d;
    SaveDebug D()
    {
        if (d == null)
            d = new SaveDebug("<color=#26A69A>[SavePaths] </color>");
        return d;
    }
    
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
        D().Log("Searching for save file directory");

        // Do nothing if the directory already exists
        if (Directory.Exists(saveFilesPath)) 
            { D().Success("Save file directory FOUND."); return; }
        
        // Otherwise, generate all necessary directories
        D().Warn("Save file directory NOT FOUND.");
        D().Log($"CREATING {saveFilesPath}");
        Directory.CreateDirectory(saveFilesPath);
    }

    public void EnsureManifestFileExists()
    {
        D().Log("Searching for save manifest file");
        
        // Do nothing if the file already exists
        if (File.Exists(manFilePath)) 
            { D().Success($"{manFilePath} FOUND"); return; }

        // Otherwise, generate file and all necessary directories
        D().Warn("save manifest file NOT FOUND.");
        D().Log($"CREATING {manFilePath}");

        // Get the default JSON data
        SaveManifest emptyMan = new SaveManifest();
        string emptyMan_serial = 
            JsonUtility.ToJson(emptyMan, prettyPrint: true);
        
        // Populate the empty file with default data
        File.WriteAllText(manFilePath, emptyMan_serial);
    }

    public void DeleteSaveFile(string path)
    {
        D().Log($"Searching for {path}");
        
        // Do nothing if the file doesn't exist
        if (!File.Exists(path))
            { D().Error("File NOT FOUND. Nothing deleted"); return; }
        
        // Otherwise, delete the file
        File.Delete(path);
        D().Success("File DELETED");
    }
}
