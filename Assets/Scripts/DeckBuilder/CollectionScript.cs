using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using Newtonsoft.Json;
using CardInformation;

public class CollectionScript : MonoBehaviour
{

    //A string that holds the directory path  
    //private string directoryPath;

    //A List of strings that holds the file names with their respective extensions  
    //private List<string> fileNames;

    [SerializeField] private DeckScript Deck;

    private Transform ContentDiv;
    [SerializeField] private GameObject CardPrefab;

    [SerializeField] private GameObject CardFull;

    [SerializeField] private GameObject SearchField;

    string directoryPath;

    void Start()
    {
        directoryPath = Application.dataPath + @"/Resources/Cards/";

        ContentDiv = gameObject.transform.GetChild(0).GetChild(0).GetChild(0);

        try
        {
            int limit = 50;
            int cardsAdded = 0;

            //Get the path of all files inside the directory and save them on a List  
            List<string> eraList = new List<string>(Directory.GetDirectories(directoryPath));

            foreach (string era in eraList)
            {
                cardsAdded += RenderEra(Path.GetFileName(era), limit - cardsAdded);
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

    public int RenderEra(string eraName, int limit, string filter = default, bool passSetInfo = false)
    {
        if (filter == null || filter.Length < 1) return 0;
        List<string> setList = new List<string>(Directory.GetDirectories(directoryPath + eraName + "/"));

        int cardsAdded = 0;

        // for each set
        for (int i = 0; i < setList.Count; i++)
        {
            string currentPath = Path.GetFileName(setList[i]);

            Dictionary<string, string> setInfo = null;
            if (passSetInfo)
            {
                StreamReader reader = new StreamReader(directoryPath + eraName + "/" + currentPath + ".json");
                setInfo = JsonConvert.DeserializeObject<Dictionary<string, string>>(reader.ReadToEnd());
                reader.Close();
            }

            cardsAdded += RenderSet(eraName, currentPath, limit - cardsAdded, filter, setInfo);
        }

        return cardsAdded;
    }

    public int RenderSet(string eraName, string setName, int limit, string filter = default, Dictionary<string, string> setInfo = null)
    {
        List<string> typeList = new List<string>(Directory.GetDirectories(directoryPath + eraName + "/" + setName + "/"));

        int cardsAdded = 0;

        // for each type
        for (int i = 0; i < typeList.Count; i++)
        {
            List<string> fileNames = new List<string>(Directory.GetFiles(typeList[i] + "/"));

            // for each card
            for (int j = 0; j < fileNames.Count && cardsAdded < limit; j++)
            {
                string currentPath = Path.GetFileName(fileNames[j]);

                string targetPath = fileNames[j].Split(new string[] { "Resources/" }, StringSplitOptions.None)[1];
                targetPath = targetPath.Remove(targetPath.IndexOf("."), 4);

                //bool passFilter = false;

                //print(targetPath);

                if (!currentPath.EndsWith(".png"))
                {
                    continue;
                }

                if (filter[0] == '"' && filter[filter.Length - 1] == '"' ?
                        setInfo[Path.GetFileName(targetPath)].ToLower() == filter.Substring(1, filter.Length - 2) :
                        filter == default || setInfo[Path.GetFileName(targetPath)].ToLower().Contains(filter))

                {
                    Sprite[] cardSprites = Resources.LoadAll<Sprite>(targetPath);
                    if (cardSprites.Length == 1)
                    {
                        Sprite card = cardSprites[0];

                        if (CardPrefab != null && ContentDiv != null)
                        {
                            GameObject clone = Instantiate(CardPrefab, ContentDiv);
                            clone.GetComponentInChildren<Image>().sprite = card;
                            cardsAdded++;

                            CardType cardType = (CardType)i;

                            //Add
                            clone.transform.GetChild(1).GetComponent<Button>().onClick.AddListener(
                                () =>
                                {
                                    Deck.AddToDeck(targetPath, cardType);
                                }
                            );

                            //Remove
                            clone.transform.GetChild(2).GetComponent<Button>().onClick.AddListener(
                                () =>
                                {
                                    Deck.RemoveFromDeck(targetPath, cardType);
                                }
                            );

                            //View
                            clone.transform.GetChild(3).GetComponent<Button>().onClick.AddListener(
                                () =>
                                {
                                    ViewCard(cardSprites[0]);
                                }
                            );
                        }
                        else
                        {
                            Debug.LogError("Content object or Card Prefab not found");
                        }
                    }
                    else
                    {
                        Debug.LogError("did not find exactly one result for sprite");
                        Debug.LogError($"Target path was: {targetPath}, return {cardSprites.Length} results");
                    }
                }
            }
        }

        return cardsAdded;
    }

    public string FileToName(string fileAfterDirectoryPath)
    {
        string[] cardInfo = fileAfterDirectoryPath.Split('/');
        string era = cardInfo[1];
        string set = cardInfo[2];
        string cardNumber = cardInfo[4];

        StreamReader reader = new StreamReader(directoryPath + era + "/" + set + ".json");
        Dictionary<string, string> setInfo = JsonConvert.DeserializeObject<Dictionary<string, string>>(reader.ReadToEnd());

        reader.Close();

        return setInfo[cardNumber];
    }

    public void ViewCard(Sprite image)
    {
        CardFull.SetActive(true);

        CardFull.transform.GetChild(1).GetComponent<Image>().sprite = image;
        //CardFull.transform.GetChild(1).rotation = Quaternion.Euler(0, 0, 0);

    }

    public void BackFromFullCard()
    {
        CardFull.SetActive(false);
    }

    public void OnSearch()
    {
        string check = SearchField.GetComponent<Text>().text.ToLower();

        var children = new List<GameObject>();

        foreach (Transform child in ContentDiv) children.Add(child.gameObject);
        children.ForEach(child => Destroy(child));

        try
        {
            int limit = 50;
            int cardsAdded = 0;

            //Get the path of all files inside the directory and save them on a List  
            List<string> eraList = new List<string>(Directory.GetDirectories(directoryPath));

            foreach (string era in eraList)
            {
                cardsAdded += RenderEra(Path.GetFileName(era), limit - cardsAdded, check, true);
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

        //try
        //    {
        //        for (int path = 0; path < 8; path++)
        //        {
        //            //Get the path of all files inside the directory and save them on a List  
        //            List<string> fileNames = new List<string>(Directory.GetFiles(directoryPath + @"\" + path));

        //            //For each string in the fileNames List   
        //            for (int i = 0; i < fileNames.Count; i++)
        //            {
        //                string currentPath = Path.GetFileName(fileNames[i]);

        //                //Remove the file path, leaving only the file name and extension  

        //                if (currentPath.EndsWith(".jpg") && currentPath.ToLower().Contains(check))
        //                {
        //                    string targetPath = @"Cards/" + path + @"/" + currentPath.Remove(currentPath.IndexOf("."), 4);
        //                    Sprite[] cardSprites = Resources.LoadAll<Sprite>(targetPath);
        //                    if (cardSprites.Length == 1)
        //                    {
        //                        Sprite card = cardSprites[0];

        //                        if (CardPrefab != null && ContentDiv != null)
        //                        {
        //                            GameObject clone = Instantiate(CardPrefab, ContentDiv);
        //                            clone.GetComponentInChildren<Image>().sprite = card;

        //                            CardType cardType = (CardType)path;

        //                            //Add
        //                            clone.transform.GetChild(1).GetComponent<Button>().onClick.AddListener(
        //                                () =>
        //                                {
        //                                    Deck.AddToDeck(currentPath.Split(new string[] { "-01" }, StringSplitOptions.None)[0], cardType);
        //                                }
        //                            );

        //                            //Remove
        //                            clone.transform.GetChild(2).GetComponent<Button>().onClick.AddListener(
        //                                () =>
        //                                {
        //                                    Deck.RemoveFromDeck(currentPath.Split(new string[] { "-01" }, StringSplitOptions.None)[0], cardType);
        //                                }
        //                            );

        //                            //View
        //                            clone.transform.GetChild(3).GetComponent<Button>().onClick.AddListener(
        //                                () =>
        //                                {
        //                                    ViewCard(cardSprites[0], cardType == CardType.Event || cardType == CardType.EventClimax);
        //                                }
        //                            );
        //                        }
        //                        else
        //                        {
        //                            Debug.LogError("Content object or Card Prefab not found");
        //                        }
        //                    }
        //                    else
        //                    {
        //                        Debug.LogError("did not find exactly one result for sprite");
        //                        Debug.LogError($"Target path was: {targetPath}, return {cardSprites.Length} results");
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    //Catch any of the following exceptions and store the error message at the outputMessage string  
        //    catch (UnauthorizedAccessException UAEx)
        //    {
        //        Debug.LogError("ERROR: " + UAEx.Message);
        //    }
        //    catch (PathTooLongException PathEx)
        //    {
        //        Debug.LogError("ERROR: " + PathEx.Message);
        //    }
        //    catch (DirectoryNotFoundException DirNfEx)
        //    {
        //        Debug.LogError("ERROR: " + DirNfEx.Message);
        //    }
        //    catch (ArgumentException aEX)
        //    {
        //        Debug.LogError("ERROR: " + aEX.Message);
        //    }
    }

}
