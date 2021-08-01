using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

public class CollectionScript : MonoBehaviour
{

    //A string that holds the directory path  
    private string directoryPath;

    //A List of strings that holds the file names with their respective extensions  
    private List<string> fileNames;

    [SerializeField] private DeckScript Deck;

    private Transform ContentDiv;
    [SerializeField] private GameObject CardPrefab;

    [SerializeField] private GameObject CardFull;

    [SerializeField] private GameObject SearchField;

    void Start()
    {
        ContentDiv = gameObject.transform.GetChild(0).GetChild(0).GetChild(0);

        directoryPath = Application.dataPath + @"/Resources" + @"/Cards";

        try
        {
            for (int path = 0; path < 8; path++)
            {
                //Get the path of all files inside the directory and save them on a List  
                fileNames = new List<string>(Directory.GetFiles(directoryPath + @"/" + path));

                //For each string in the fileNames List   
                for (int i = 0; i < fileNames.Count; i++)
                {
                    string currentPath = Path.GetFileName(fileNames[i]);

                    //Remove the file path, leaving only the file name and extension  

                    if (currentPath.EndsWith(".jpg"))
                    {
                        string targetPath = @"Cards/" + path + @"/" + currentPath.Remove(currentPath.IndexOf("."), 4);
                        Sprite[] cardSprites = Resources.LoadAll<Sprite>(targetPath);
                        if (cardSprites.Length == 1)
                        {
                            Sprite card = cardSprites[0];

                            if (CardPrefab != null && ContentDiv != null)
                            {
                                GameObject clone = Instantiate(CardPrefab, ContentDiv);
                                clone.GetComponentInChildren<Image>().sprite = card;

                                PlayerInfoManager.CardType cardType = (PlayerInfoManager.CardType)path;

                                //Add
                                clone.transform.GetChild(1).GetComponent<Button>().onClick.AddListener(
                                    () =>
                                    {
                                        Deck.AddToDeck(currentPath.Split(new string[] { "-01" }, StringSplitOptions.None)[0], cardType);
                                    }
                                );

                                //Remove
                                clone.transform.GetChild(2).GetComponent<Button>().onClick.AddListener(
                                    () =>
                                    {
                                        Deck.RemoveFromDeck(currentPath.Split(new string[] { "-01" }, StringSplitOptions.None)[0], cardType);
                                    }
                                );

                                //View
                                clone.transform.GetChild(3).GetComponent<Button>().onClick.AddListener(
                                    () =>
                                    {
                                        ViewCard(cardSprites[0], cardType == PlayerInfoManager.CardType.Event || cardType == PlayerInfoManager.CardType.EventClimax);
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
        }

        //Catch any of the following exceptions and store the error message at the outputMessage string  
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

    public void ViewCard(Sprite image, bool isRotated)
    {
        CardFull.SetActive(true);

        CardFull.transform.GetChild(1).GetComponent<Image>().sprite = image;
        CardFull.transform.GetChild(1).rotation = Quaternion.Euler(0, 0, isRotated ? -90 : 0);

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
            for (int path = 0; path < 8; path++)
            {
                //Get the path of all files inside the directory and save them on a List  
                fileNames = new List<string>(Directory.GetFiles(directoryPath + @"\" + path));

                //For each string in the fileNames List   
                for (int i = 0; i < fileNames.Count; i++)
                {
                    string currentPath = Path.GetFileName(fileNames[i]);

                    //Remove the file path, leaving only the file name and extension  

                    if (currentPath.EndsWith(".jpg") && currentPath.ToLower().Contains(check))
                    {
                        string targetPath = @"Cards/" + path + @"/" + currentPath.Remove(currentPath.IndexOf("."), 4);
                        Sprite[] cardSprites = Resources.LoadAll<Sprite>(targetPath);
                        if (cardSprites.Length == 1)
                        {
                            Sprite card = cardSprites[0];

                            if (CardPrefab != null && ContentDiv != null)
                            {
                                GameObject clone = Instantiate(CardPrefab, ContentDiv);
                                clone.GetComponentInChildren<Image>().sprite = card;
                                
                                PlayerInfoManager.CardType cardType = (PlayerInfoManager.CardType)path;

                                //Add
                                clone.transform.GetChild(1).GetComponent<Button>().onClick.AddListener(
                                    () =>
                                    {
                                        Deck.AddToDeck(currentPath.Split(new string[] { "-01" }, StringSplitOptions.None)[0], cardType);
                                    }
                                );

                                //Remove
                                clone.transform.GetChild(2).GetComponent<Button>().onClick.AddListener(
                                    () =>
                                    {
                                        Deck.RemoveFromDeck(currentPath.Split(new string[] { "-01" }, StringSplitOptions.None)[0], cardType);
                                    }
                                );

                                //View
                                clone.transform.GetChild(3).GetComponent<Button>().onClick.AddListener(
                                    () =>
                                    {
                                        ViewCard(cardSprites[0], cardType == PlayerInfoManager.CardType.Event || cardType == PlayerInfoManager.CardType.EventClimax);
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
        }

        //Catch any of the following exceptions and store the error message at the outputMessage string  
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

}
