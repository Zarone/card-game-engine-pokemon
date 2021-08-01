using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using SimpleFileBrowser;
using System;

public class DeckScript : MonoBehaviour
{
    public Dictionary<PlayerInfoManager.CardType, Dictionary<string, int>> CurrentDeck = new Dictionary<PlayerInfoManager.CardType, Dictionary<string, int>>() {
        { PlayerInfoManager.CardType.SpecialCharacter, new Dictionary<string, int>() },
        { PlayerInfoManager.CardType.BasicCharacter, new Dictionary<string, int>() },
        { PlayerInfoManager.CardType.Action, new Dictionary<string, int>() },
        { PlayerInfoManager.CardType.ActionUnity, new Dictionary<string, int>() },
        { PlayerInfoManager.CardType.ActionLocation, new Dictionary<string, int>() },
        { PlayerInfoManager.CardType.ActionPerpetual, new Dictionary<string, int>() },
        { PlayerInfoManager.CardType.EventClimax, new Dictionary<string, int>() },
        { PlayerInfoManager.CardType.Event, new Dictionary<string, int>() },
    };

    [SerializeField] private Transform deckContent;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private GameObject nameField;
    [SerializeField] private GameObject alertObj;
    [SerializeField] private GameObject options;
    [SerializeField] private GameObject deckCount;
    [SerializeField] private GameObject specialDeckCount;

    [SerializeField] private CollectionScript collectionScript;

    List<string> fileNames;

    private void Start()
    {
        if (options != null)
        {
            LoadOption();
        }
        AddAllSpecialCharacterCards();
    }

    public void AddAllSpecialCharacterCards()
    {
        string directoryPath = Application.dataPath + @"/Resources" + @"/Cards";

        try
        {

            //Get the path of all files inside the directory and save them on a List  
            fileNames = new List<string>(Directory.GetFiles(directoryPath + @"/0"));

            //For each string in the fileNames List   
            for (int i = 0; i < fileNames.Count; i++)
            {
                string currentPath = Path.GetFileName(fileNames[i]);
                if (currentPath.EndsWith(".jpg"))
                {
                    AddToDeck(currentPath.Split(new string[] { "-01" }, StringSplitOptions.None)[0], PlayerInfoManager.CardType.SpecialCharacter);
                }
            }
        }
        catch (UnauthorizedAccessException UAEx)
        {
            Debug.LogError("ERROR: " + UAEx.Message);
        }
        catch (PathTooLongException PathEx)
        {
            Debug.LogError("ERROR: " + PathEx.Message);
        }
        catch (DirectoryNotFoundException DirNfEx)
        {
            Debug.LogError("ERROR: " + DirNfEx.Message);
        }
        catch (ArgumentException aEX)
        {
            Debug.LogError("ERROR: " + aEX.Message);
        }
    }

    public void RenderDeck(bool showSpecial = false)
    {
        int countNormal = 0;
        int countSpecial = 0;
        int i = 1;
        foreach (KeyValuePair<PlayerInfoManager.CardType, Dictionary<string, int>> section in CurrentDeck)
        {

            Transform deckContentChild = deckContent.GetChild(i);
            if (deckContentChild != null)
            {
                var children = new List<GameObject>();
                foreach (Transform child in deckContentChild) children.Add(child.gameObject);
                children.ForEach(child => Destroy(child));

                foreach (KeyValuePair<string, int> card in section.Value)
                {
                    if (card.Value > 0)
                    {
                        if (section.Key == PlayerInfoManager.CardType.SpecialCharacter)
                        {
                            countSpecial += card.Value;
                        }
                        else
                        {
                            countNormal += card.Value;
                        }

                        if ((showSpecial && deckContentChild.name == "Special Character") ||
                            (!showSpecial && deckContentChild.name != "Special Character"))
                        {
                            GameObject cardObj = Instantiate(cardPrefab, deckContentChild);
                            cardObj.transform.GetChild(0).GetComponentInChildren<Text>().text = $"{card.Key} x  {card.Value}";
                            cardObj.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() => 
                            {

                                string targetPath = @"Cards/" + (int)section.Key + @"/" + card.Key + "-01";

                                Sprite[] cardSprites = Resources.LoadAll<Sprite>(targetPath);
                                if (cardSprites.Length != 1)
                                {
                                    Debug.LogError("the number of sprites found for selected card was not zero");
                                    return;
                                } 

                                collectionScript.ViewCard(cardSprites[0], section.Key == PlayerInfoManager.CardType.Event || section.Key == PlayerInfoManager.CardType.EventClimax);
                            });
                            cardObj.transform.GetChild(1).GetComponent<Button>().onClick.AddListener(() => 
                            {
                                RemoveFromDeck(card.Key, section.Key);
                            });
                            cardObj.transform.GetChild(2).GetComponent<Button>().onClick.AddListener(() =>
                            {
                                AddToDeck(card.Key, section.Key);
                            });
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("child not found in hierarchy");
            }

            i += 2;
        }

        deckCount.GetComponent<Text>().text = "Deck: " + countNormal.ToString();
        specialDeckCount.GetComponent<Text>().text = "Special Deck: " + countSpecial.ToString();
    }

    public void OnHome()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void SaveData()
    {
        if (nameField == null || alertObj == null) return;
        string deckName = nameField.GetComponent<InputField>().text;
        if (deckName == "")
        {
            alertObj.SetActive(true);
            return;
        }

        BinaryFormatter formatter = new BinaryFormatter();
        string path = Application.persistentDataPath + "/" + deckName + ".deck";

        FileStream stream = new FileStream(path, FileMode.Create);

        SerializableCard[] nonSerializedDeck = new SerializableCard[PlayerInfoManager.MaxCardPerFullDeck];

        int i = 0;

        // for each section of cards
        foreach (KeyValuePair<PlayerInfoManager.CardType, Dictionary<string, int>> section in CurrentDeck)
        {

            // for each card (multiples are grouped together)
            foreach (KeyValuePair<string, int> card in section.Value)
            {
                // for each individual card
                for (int j = 0; j < card.Value; j++)
                {
                    nonSerializedDeck[i] = new SerializableCard(card.Key, section.Key);
                    i++;
                }
            }
        }

        SerializableDeck serializableDeck = new SerializableDeck(nonSerializedDeck);
        formatter.Serialize(stream, serializableDeck);
        stream.Close();
        LoadOption();
    }

    void LoadOption()
    {
        options.GetComponent<Dropdown>().ClearOptions();

        var decks = new List<string>();


        fileNames = new List<string>(Directory.GetFiles(Application.persistentDataPath));

        //For each string in the fileNames List   
        for (int i = 0; i < fileNames.Count; i++)
        {
            string currentPath = Path.GetFileName(fileNames[i]);

            if (currentPath.EndsWith(".deck"))
            {
                decks.Add(currentPath.Split(new string[] { ".deck" }, StringSplitOptions.None)[0]);
            }
        }

        options.GetComponent<Dropdown>().AddOptions(new List<string> { "Select a deck to..." });
        options.GetComponent<Dropdown>().AddOptions(decks);
    }

    public void LoadData(string overloadPath = "")
    {

        string optionName = options.GetComponentInChildren<Text>().text;
        if (optionName == "Select a deck to..." && overloadPath == "") return;

        string path = overloadPath == "" ? (Application.persistentDataPath + "/" + optionName + ".deck") : overloadPath;

        nameField.GetComponent<InputField>().text = overloadPath == "" ? optionName : "";

        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);

            SerializableDeck data = formatter.Deserialize(stream) as SerializableDeck;

            CurrentDeck = new Dictionary<PlayerInfoManager.CardType, Dictionary<string, int>>() {
                { PlayerInfoManager.CardType.SpecialCharacter, new Dictionary<string, int>() },
                { PlayerInfoManager.CardType.BasicCharacter, new Dictionary<string, int>() },
                { PlayerInfoManager.CardType.Action, new Dictionary<string, int>() },
                { PlayerInfoManager.CardType.ActionUnity, new Dictionary<string, int>() },
                { PlayerInfoManager.CardType.ActionLocation, new Dictionary<string, int>() },
                { PlayerInfoManager.CardType.ActionPerpetual, new Dictionary<string, int>() },
                { PlayerInfoManager.CardType.EventClimax, new Dictionary<string, int>() },
                { PlayerInfoManager.CardType.Event, new Dictionary<string, int>() },
            };


            for (int i = 0; i < PlayerInfoManager.MaxCardPerFullDeck; i++)
            {
                if (data.Deck[i] != null)
                {
                    PlayerInfoManager.CardType type = data.Deck[i].Type;
                    string card = data.Deck[i].Art;

                    if (!CurrentDeck[type].ContainsKey(card))
                    {
                        CurrentDeck[type].Add(card, 1);
                    }
                    else
                    {
                        CurrentDeck[type][card] += 1;
                    }
                }
            }


            RenderDeck();
            stream.Close();
        }
        else
        {
            Debug.LogError("Error: Save file not found in " + path);
        }

    }

    public void CloseAlert()
    {
        alertObj.SetActive(false);
    }

    public void ExportDeck()
    {
        Application.OpenURL("file://" + Application.persistentDataPath);
    }

    public void ImportDeck()
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Decks", ".deck"));
        FileBrowser.SetDefaultFilter(".deck");
        StartCoroutine(ShowLoadDialogCoroutine());
    }

    IEnumerator ShowLoadDialogCoroutine()
    {
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.FilesAndFolders, true, null, null, "Load Files and Folders", "Load");
        //Debug.Log(FileBrowser.Success);

        if (FileBrowser.Success)
        {
            for (int i = 0; i < FileBrowser.Result.Length; i++)
            {
                string from = FileBrowser.Result[i];
                LoadData(from);
            }

        }
    }

    public void OnViewSpecialDeck()
    {
        RenderDeck(true);
    }

    public void OnViewDeck()
    {
        RenderDeck(false);
    }

    public void RemoveFromDeck(string card, PlayerInfoManager.CardType type)
    {
        if (CurrentDeck[type].ContainsKey(card) && CurrentDeck[type][card] > 0)
        {
            CurrentDeck[type][card] -= 1;
        }

        RenderDeck(type == PlayerInfoManager.CardType.SpecialCharacter);
    }

    public void AddToDeck(string card, PlayerInfoManager.CardType type)
    {
        if (!CurrentDeck[type].ContainsKey(card))
        {
            CurrentDeck[type].Add(card, 1);
        }
        else
        {
            CurrentDeck[type][card] += 1;
        }

        RenderDeck(type == PlayerInfoManager.CardType.SpecialCharacter);
    }
}
