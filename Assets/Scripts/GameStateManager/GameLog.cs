using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLog : MonoBehaviour
{
    public GameObject GameLogContainer;
    public GameObject GameLogExpandButton;

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
}
