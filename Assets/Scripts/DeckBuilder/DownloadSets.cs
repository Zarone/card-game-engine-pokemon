using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Windows;

public class DownloadSets : MonoBehaviour
{
    public GameObject LoadingIcon;
    public GameObject LoadingBackground;
    public Transform Content;

    private readonly string AssetSourceUrl = "https://pokemon-card-api.herokuapp.com/";

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

    public IEnumerator GetEra(int index, System.Action callback = null)
    {
        UnityWebRequest www = UnityWebRequest.Get(PlayerInfoManager.baseUrl);
        //yield return www.SendWebRequest();
        while (!www.isDone)
        {
            print("rotate loading");
            LoadingIcon.GetComponent<RectTransform>().Rotate(new Vector3(0, 0, -1));
            yield return new WaitForFixedUpdate();
        }
        callback?.Invoke();
    }

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
        //for (int i = 0; i < EraDirectoryNames.Length; i++)
        for (int i = 0; i < Content.childCount; i++)
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
        print(EraDirectoryNames[index]);
        LoadingBackground.SetActive(true);
        StartCoroutine(GetEra(index, () =>
        {
            print("done");
            LoadingBackground.SetActive(false);
        }));
    }
}
