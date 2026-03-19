using UnityEngine;

public class SaveDebug
{
    string sysSplash = "<color=#FFFFFF>[SaveSystem]</color>";
    string splash;

    public SaveDebug(string localSplash) { splash = localSplash; }
    public void Log(string msg) { Debug.Log($"{sysSplash}{splash}{msg}"); }
    public void Success(string msg)
        { Debug.Log($"{sysSplash}{splash}<color=green>{msg}</color>"); }
    public void Warn(string msg) 
        { Debug.LogWarning($"{sysSplash}{splash}<color=yellow>{msg}</color>"); }
    public void Error(string msg) 
        { Debug.LogError($"{sysSplash}{splash}<color=#B71C1C>{msg}</color>"); }
}
