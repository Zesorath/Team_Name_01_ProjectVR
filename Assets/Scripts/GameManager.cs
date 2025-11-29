using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    //[SerializeField] private string openLessonName;

    public void OpenLesson()
    {
        SceneManager.LoadScene(2);
    }

}
