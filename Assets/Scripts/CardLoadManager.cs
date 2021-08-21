using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardLoadManager : MonoBehaviour
{
    public static Dictionary<string, Sprite> LoadedCards = new Dictionary<string, Sprite>();

    public static Dictionary<string, AssetBundle> LoadedBundles = new Dictionary<string, AssetBundle>();
 
    public static void LoadNewCard(string query)
    {
        string[] splitQuery = query.Split(new string[] { "/" }, StringSplitOptions.None);
        string era = splitQuery[1];
        string set = splitQuery[2];
        string type = splitQuery[3];
        string card = splitQuery[4];

        string keyPath = $"/Resources/Cards/{era}/{set}/{type}/cards.unity3d";

        if (!LoadedBundles.ContainsKey(keyPath))
        {
            LoadedBundles[keyPath] = AssetBundle.LoadFromFile(Application.persistentDataPath + keyPath);
        }
        LoadedCards[query] = LoadedBundles[keyPath].LoadAsset<Sprite>(card);
    }
}
