using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrap : MonoBehaviour
{
    void Awake()
    {
        SaveManager.Instance.Init();
        SceneManager.LoadScene("Starting Menu");
    }
}
