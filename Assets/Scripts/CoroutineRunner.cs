using UnityEngine;
using System.Collections;

public class CoroutineRunner : MonoBehaviour
{
    private static CoroutineRunner instance;

    public static CoroutineRunner Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject obj = new GameObject("CoroutineRunner");
                instance = obj.AddComponent<CoroutineRunner>();
                DontDestroyOnLoad(obj);
            }
            return instance;
        }
    }

    public static Coroutine RunCoroutine(IEnumerator routine)
    {
        return Instance.StartCoroutine(routine);
    }
}
