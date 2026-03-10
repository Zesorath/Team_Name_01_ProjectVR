using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    //[SerializeField] private string openLessonName;

    public void ReloadCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void Tutorial()
    {
        SceneManager.LoadScene(1);
    }
   

    public void Lesson_1()
    {
        SceneManager.LoadScene(2);
    }
    public void Lesson_2()
    {
        SceneManager.LoadScene(3);
    }
    public void Lesson_3()
    {
        SceneManager.LoadScene(4);
    }
    public void Lesson_4()
    {
        SceneManager.LoadScene(5);
    }

    public void Lesson_5()
    {
        SceneManager.LoadScene(6);
    }
    public void Lesson_6()
    {
        SceneManager.LoadScene(7);
    }

    public static void ExitGame()
    {
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }
}
