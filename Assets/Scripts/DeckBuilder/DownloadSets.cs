using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Windows;

public class DownloadSets : MonoBehaviour
{
    public GameObject LoadingIcon;
    public GameObject LoadingBackground;
    public Transform Content;
    //public enum Eras
    //{
    //    PreRubySaphire,
    //    RubySaphire,
    //    DiamondPearl,
    //    HeartgoldSoulsilver,
    //    BlackWhite,
    //    XY,
    //    SunMoon
    //}

    public string[] EraDirectoryNames = new string[] {
        "Pre Ruby Saphire Era",
        "Ruby & Saphire Era",
        "Diamond & Pearl Era",
        "HeartGold & SoulSilver Era",
        "Black & White Era",
        "X & Y Era",
        "Sun & Moon Era"
    };

    //public bool[] HasDownloaded;

    public void Start()
    {
        RenderCorrectButtons();
    }

    public void RenderCorrectButtons ()
    {
        for (int i = 0; i < EraDirectoryNames.Length; i++)
        {
            if (UnityEngine.Windows.Directory.Exists(Application.streamingAssetsPath + "/Cards/" + EraDirectoryNames))
            {
                Content.GetChild(i).GetChild(1).gameObject.SetActive(false);
                Content.GetChild(i).GetChild(2).gameObject.SetActive(true);
            }
            else
            {
                Content.GetChild(i).GetChild(1).gameObject.SetActive(true);
                Content.GetChild(i).GetChild(2).gameObject.SetActive(false);
            }
        }
    }

    public void DownloadEra(int index)
    {

    }
}
