using System.Runtime.CompilerServices;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.Rendering;

public class TabletContents : MonoBehaviour
{
    [TextArea(10, 20)]
    [SerializeField] private string content;
    [Space]
    [SerializeField] private TMP_Text display;
    [Space]
    [SerializeField] private TMP_Text textPagination;

    private void OnValidate()
    {
        UpdatePagination();

        if (display.text == content)
            return;

        SetupContent();
    }

    private void Awake()
    {
        SetupContent();
        UpdatePagination();
    }

    private void SetupContent()
    {
        display.text = content;
    }

    private void UpdatePagination()
    {
        textPagination.text = display.pageToDisplay.ToString();
    }

    public void PrevPage()
    {
        if (display.pageToDisplay <= 1)
        {
            display.pageToDisplay = 1;
            return;
        }

        display.pageToDisplay -= 1;

        UpdatePagination();
    }

    public void NextPage()
    {
        if (display.pageToDisplay >= display.textInfo.pageCount)
            return;

        display.pageToDisplay += 1;

        UpdatePagination();
    }

    
}
