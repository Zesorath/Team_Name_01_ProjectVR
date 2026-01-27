using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    //[SerializeField] private string openLessonName;
    public void OpenStartScene()
    {
        SceneManager.LoadScene(1);
    }
    public void OpenWorkshop()
    {
        SceneManager.LoadScene(2);
    }

}
