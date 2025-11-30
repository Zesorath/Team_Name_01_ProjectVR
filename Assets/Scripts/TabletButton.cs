using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    [SerializeField] List<Transform> pages;
    int index = -1;
    [SerializeField] GameObject BackButton;
    [SerializeField] GameObject NextButton;

    private void Start()
    {
        BackButton.SetActive(false);
    }

    public void NextPage()
    {
        index++;
        ForwardButtonActions();
        pages[index].SetAsLastSibling();
    }

    public void ForwardButtonActions()
    {
        if (BackButton.activeInHierarchy == false)
        {
            BackButton.SetActive(false);
        }
        if (index == pages.Count - 1)
        {
            NextButton.SetActive(false);
        }
    }

    public void BackPage()
    {
        pages[index].SetAsLastSibling();
        BackButtonActions();

    }

    public void BackButtonActions()
    {
        if(NextButton.activeInHierarchy == false)
        {
            NextButton.SetActive(true);
        }
        if(index - 1 == -1)
        {
            BackButton.SetActive(false);
        }
    }

    
}
