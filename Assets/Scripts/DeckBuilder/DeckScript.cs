using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using SimpleFileBrowser;
using System;
using CardInformation;

public class DeckScript : MonoBehaviour
{
    public Dictionary<CardType, Dictionary<string, int>> CurrentDeck = new Dictionary<CardType, Dictionary<string, int>>() {
        { CardType.Pokemon, new Dictionary<string, int>() },
        { CardType.Trainer, new Dictionary<string, int>() },
        { CardType.Energy, new Dictionary<string, int>() },
    };

    [SerializeField] private Transform deckContent;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private GameObject nameField;
    [SerializeField] private GameObject alertObj;
    [SerializeField] private GameObject options;
    [SerializeField] private GameObject deckCount;

    [SerializeField] private CollectionScript collectionScript;

    List<string> fileNames;

    private void Start()
    {
        if (options != null)
        {
            LoadOption();
        }
    }

    public void RenderDeck()
    {
        int countNormal = 0;
        int i = 1;
        foreach (KeyValuePair<CardType, Dictionary<string, int>> section in CurrentDeck)
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

                        countNormal += card.Value;



                        GameObject cardObj = Instantiate(cardPrefab, deckContentChild);
                        cardObj.transform.GetChild(0).GetComponentInChildren<Text>().text = 
                            $"{CollectionScript.FileToName(card.Key)} x  {card.Value}";
                        cardObj.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() =>
                        {

                            string targetPath = card.Key;

                            Sprite cardSprite = CollectionScript.LocationsToSprite(targetPath);

                            collectionScript.ViewCard(cardSprite);
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
            else
            {
                Debug.LogError("child not found in hierarchy");
            }

            i += 2;
        }

        deckCount.GetComponent<Text>().text = "Deck: " + countNormal.ToString();
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
            alertObj.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "You do not have a name for this deck";
            alertObj.SetActive(true);
            return;
        }

        BinaryFormatter formatter = new BinaryFormatter();
        string path = Application.persistentDataPath + "/" + deckName + ".deck";

        FileStream stream = new FileStream(path, FileMode.Create);

        SerializableCard[] nonSerializedDeck = new SerializableCard[PlayerInfoManager.CardsPerNormalDeck];

        int i = 0;

        // for each section of cards
        foreach (KeyValuePair<CardType, Dictionary<string, int>> section in CurrentDeck)
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

            CurrentDeck = new Dictionary<CardType, Dictionary<string, int>>() {
                { CardType.Pokemon, new Dictionary<string, int>() },
                { CardType.Trainer, new Dictionary<string, int>() },
                { CardType.Energy, new Dictionary<string, int>() },
            };


            for (int i = 0; i < PlayerInfoManager.CardsPerNormalDeck; i++)
            {
                if (data.Deck[i] != null)
                {
                    CardType type = data.Deck[i].Type;
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

    public void NewDeck()
    {
        nameField.GetComponent<InputField>().text = "";
        options.GetComponent<Dropdown>().value = 0;

        CurrentDeck = new Dictionary<CardType, Dictionary<string, int>>() 
        {
            { CardType.Pokemon, new Dictionary<string, int>() },
            { CardType.Trainer, new Dictionary<string, int>() },
            { CardType.Energy, new Dictionary<string, int>() },
        };

        RenderDeck();
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

    public void RemoveFromDeck(string card, CardType type)
    {
        if (CurrentDeck[type].ContainsKey(card) && CurrentDeck[type][card] > 0)
        {
            CurrentDeck[type][card] -= 1;
        }

        RenderDeck();
    }

    public void AddToDeck(string card, CardType type)
    {
        if (!CurrentDeck[type].ContainsKey(card))
        {
            CurrentDeck[type].Add(card, 1);
        }
        else
        {
            CurrentDeck[type][card] += 1;
        }

        RenderDeck();
    }
}
