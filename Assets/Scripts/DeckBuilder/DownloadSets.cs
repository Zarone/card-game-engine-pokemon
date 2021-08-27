using System.Collections;
using System.Collections.Generic;
using System.IO;
using SimpleJSON;
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

    public IEnumerator GetEra(string era, System.Action callback = null)
    {
        UnityWebRequest www = UnityWebRequest.Get(AssetSourceUrl + era);
        www.SendWebRequest();
        while (!www.isDone)
        {
            LoadingIcon.GetComponent<RectTransform>().Rotate(new Vector3(0, 0, -1));
            yield return new WaitForFixedUpdate();
        }

        if (www.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.LogError(www.error);
            yield break;
        }

        JSONNode allSets = JSON.Parse(www.downloadHandler.text);

        for (int i = 0; i < allSets.Count; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                UnityWebRequest findCardsInTypeRequest = UnityWebRequest.Get($"{AssetSourceUrl}{era}/{allSets[i].ToString().Trim('"')}/{j}");
                print($"{AssetSourceUrl}{era}/{allSets[i].ToString().Trim('"')}/{j}");
                findCardsInTypeRequest.SendWebRequest();
                while (!findCardsInTypeRequest.isDone)
                {
                    LoadingIcon.GetComponent<RectTransform>().Rotate(new Vector3(0, 0, -1));
                    yield return new WaitForFixedUpdate();
                }
                JSONNode allCardsInSet = JSON.Parse(findCardsInTypeRequest.downloadHandler.text);
                print(allCardsInSet.Count);
            }
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

    public void RenderCorrectButtons()
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
        LoadingBackground.SetActive(true);
        StartCoroutine(GetEra(EraDirectoryNames[index], () =>
        {
            print("done");
            LoadingBackground.SetActive(false);
        }));
    }
}
