using UnityEngine;
using UnityEngine.SceneManagement;
public class saveSystem : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        SaveManager.Instance.Init();
        UndoManager.Instance.Init();
    }
    }
