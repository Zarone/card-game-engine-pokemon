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

        if (allSets.Count > 0)
        {
            TryCreateDirectory($"{Application.streamingAssetsPath}/Cards/{era}");
        }

        // for each set in era
        for (int i = 0; i < allSets.Count; i++)
        {
            print($"{i}/{allSets.Count}");
            string thisSet = allSets[i].ToString().Trim('"');
            TryCreateDirectory($"{Application.streamingAssetsPath}/Cards/{era}/{thisSet}");

            // get JSON
            UnityWebRequest jsonRequest = UnityWebRequest.Get($"{AssetSourceUrl}JSON/{thisSet}.json");
            jsonRequest.SendWebRequest();
            while (!jsonRequest.isDone)
            {
                LoadingIcon.GetComponent<RectTransform>().Rotate(new Vector3(0, 0, -1));
                yield return new WaitForFixedUpdate();
            }

            System.IO.File.WriteAllText($"{Application.streamingAssetsPath}/Cards/{era}/{thisSet}.json", jsonRequest.downloadHandler.text);

            // for each type
            for (int j = 0; j < 3; j++)
            {
                UnityWebRequest findCardsInTypeRequest = UnityWebRequest.Get($"{AssetSourceUrl}{era}/{thisSet}/{j}");
                findCardsInTypeRequest.SendWebRequest();
                while (!findCardsInTypeRequest.isDone)
                {
                    LoadingIcon.GetComponent<RectTransform>().Rotate(new Vector3(0, 0, -1));
                    yield return new WaitForFixedUpdate();
                }
                JSONNode allCardsInSet = JSON.Parse(findCardsInTypeRequest.downloadHandler.text);
                if (allCardsInSet.Count > 0)
                {


                    TryCreateDirectory($"{Application.streamingAssetsPath}/Cards/{era}/{thisSet}/{j}");
                    for (int k = 0; k < allCardsInSet.Count; k++)
                    {
                        string thisCardName = allCardsInSet[k]["name"].ToString().Trim('"');
                        string thisCardID = allCardsInSet[k]["id"].ToString().Trim('"');
                        UnityWebRequest requestCard = UnityWebRequestTexture.GetTexture($"{AssetSourceUrl}card/{thisCardID}");
                        requestCard.SendWebRequest();
                        while (!requestCard.isDone)
                        {
                            LoadingIcon.GetComponent<RectTransform>().Rotate(new Vector3(0, 0, -1));
                            yield return new WaitForFixedUpdate();
                        }

                        if (requestCard.result != UnityWebRequest.Result.Success)
                        {
                            Debug.Log(requestCard.error);
                        }
                        else
                        {
                            byte[] bytes = ((DownloadHandlerTexture)requestCard.downloadHandler).texture.EncodeToPNG();
                            System.IO.File.WriteAllBytes($"{Application.streamingAssetsPath}/Cards/{era}/{thisSet}/{j}/{thisCardName}", bytes);
                        }
                    }
                }
            }
        }

        callback?.Invoke();
    }

    public void TryCreateDirectory(string path)
    {
        if (!UnityEngine.Windows.Directory.Exists(path))
        {
            UnityEngine.Windows.Directory.CreateDirectory(path);
        }
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
            if (UnityEngine.Windows.Directory.Exists(Application.streamingAssetsPath + "/Cards/" + EraDirectoryNames[i]))
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
