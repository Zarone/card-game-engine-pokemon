using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Windows;

public class DownloadSets : MonoBehaviour
{
    public GameObject LoadingIcon;
    public GameObject LoadingBackground;
    public Transform Content;

    public Text OverallPercentage;

    private readonly string AssetSourceUrl = "https://pokemon-card-api.herokuapp.com/";

    public void UnZip(string FilePath, string Set, System.Action callback)
    {
        if (!System.IO.File.Exists($"{FilePath}/{Set}.zip"))
        {
            Debug.LogError($"{FilePath}/{Set}.zip not found");
            return;
        }

        // Read file
        FileStream fs = null;
        try
        {
            fs = new FileStream($"{FilePath}/{Set}.zip", FileMode.Open);
        }
        catch
        {
            Debug.LogError("GameData file open exception: " + $"{FilePath}/{Set}.zip");
        }

        if (fs != null)
        {
            print($"input file: {FilePath}/{Set}.zip");
            TryCreateDirectory($"{FilePath}/{Set}");
            TryCreateDirectory($"{FilePath}/{Set}/0");
            TryCreateDirectory($"{FilePath}/{Set}/1");
            TryCreateDirectory($"{FilePath}/{Set}/2");
            try
            {

                // Read zip file
                ZipFile zf = new ZipFile(fs);
                int numFiles = 0;

                if (zf.TestArchive(true) == false)
                {
                    Debug.LogError("Zip file failed integrity check!");
                    zf.IsStreamOwner = false;
                    zf.Close();
                    fs.Close();
                }
                else
                {
                    foreach (ZipEntry zipEntry in zf)
                    {
                        // Ignore directories
                        if (!zipEntry.IsFile)
                            continue;

                        String entryFileName = zipEntry.Name;
                        print($"loading file: {entryFileName}");

                        // Skip .DS_Store files (these appear on OSX)
                        if (entryFileName.Contains("DS_Store") || entryFileName.Contains(".meta"))
                            continue;

                        //Debug.Log("Unpacking zip file entry: " + entryFileName);

                        byte[] buffer = new byte[4096];     // 4K is optimum
                        Stream zipStream = zf.GetInputStream(zipEntry);

                        // Manipulate the output filename here as desired.
                        string fullZipToPath = $"{FilePath}/{entryFileName}";

                        // Unzip file in buffered chunks. This is just as fast as unpacking to a buffer the full size
                        // of the file, but does not waste memory.
                        // The "using" will close the stream even if an exception occurs.

                        print($"output file: {fullZipToPath}");
                        using (FileStream streamWriter = System.IO.File.Create(fullZipToPath))
                        {
                            StreamUtils.Copy(zipStream, streamWriter, buffer);
                        }
                        numFiles++;
                    }

                    zf.IsStreamOwner = false;
                    zf.Close();
                    fs.Close();
                    callback?.Invoke();
                }
            }
            catch
            {
                Debug.Log("Zip file error!");
            }
        }
        else
        {
            Debug.LogError("fs is null");
        }
    }

    public IEnumerator GetEra(string era, System.Action callback = null)
    {
        UnityWebRequest www = UnityWebRequest.Get(AssetSourceUrl + era);
        www.SendWebRequest();
        while (!www.isDone)
        {
            LoadingIcon.GetComponent<RectTransform>().Rotate(new Vector3(0, 0, -3));
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

        OverallPercentage.text = $"Sets Downloaded: 0 / {allSets.Count}";

        // for each set in era
        for (int i = 0; i < allSets.Count; i++)
        {
            string thisSet = allSets[i].ToString().Trim('"');
            TryCreateDirectory($"{Application.streamingAssetsPath}/Cards/{era}/{thisSet}");

            // get JSON
            UnityWebRequest jsonRequest = UnityWebRequest.Get($"{AssetSourceUrl}JSON/{thisSet}.json");
            jsonRequest.SendWebRequest();
            while (!jsonRequest.isDone)
            {
                LoadingIcon.GetComponent<RectTransform>().Rotate(new Vector3(0, 0, -3));
                yield return new WaitForFixedUpdate();
            }

            System.IO.File.WriteAllText($"{Application.streamingAssetsPath}/Cards/{era}/{thisSet}.json", jsonRequest.downloadHandler.text);

            // get ZIP

            UnityWebRequest zipRequest = UnityWebRequest.Get($"{AssetSourceUrl}zip/{thisSet}.zip");
            zipRequest.SendWebRequest();
            while (!zipRequest.isDone)
            {
                LoadingIcon.GetComponent<RectTransform>().Rotate(new Vector3(0, 0, -3));
                yield return new WaitForFixedUpdate();
            }

            string path = $"{Application.streamingAssetsPath}/Cards/{era}";

            print("created zip at: " + $"{path}/{thisSet}.zip");
            System.IO.File.WriteAllBytes($"{path}/{thisSet}.zip", zipRequest.downloadHandler.data);

            UnZip(path, thisSet, () =>
            {
                System.IO.File.Delete($"{path}/{thisSet}.zip");
                OverallPercentage.text = $"Sets Downloaded: {i + 1} / {allSets.Count}";
            });

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
            LoadingBackground.SetActive(false);
            Content.GetChild(index).GetChild(1).gameObject.SetActive(false);
            Content.GetChild(index).GetChild(2).gameObject.SetActive(true);
        }));
    }

    public bool HasDownloadedSet()
    {
        for (int i = 0; i < Content.childCount; i++)
        {
            if (UnityEngine.Windows.Directory.Exists(Application.streamingAssetsPath + "/Cards/" + EraDirectoryNames[i]))
            {
                return true;
            }
        }
        return false;
    }
}
