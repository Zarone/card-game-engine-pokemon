using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameLog : MonoBehaviour
{
    public GameObject GameLogContainer;
    public GameObject GameLogExpandButton;
    public Text GameLogText;

    public void OnExpand()
    {
        GameLogContainer.SetActive(true);
        GameLogExpandButton.SetActive(false);
    }

    public void OnHide()
    {
        GameLogContainer.SetActive(false);
        GameLogExpandButton.SetActive(true);
    }

    public void CopyLogToClipboard()
    {
        GUIUtility.systemCopyBuffer = GameLogText.text;
    }
}
