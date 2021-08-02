using System.Collections.Generic;
using UnityEngine;

public class PlayerInfoManager
{
    public static Card[] fullDeck;
    public static Card[] specialCharacterDeck;
    public static string deckName;
    public static int MaxCardPerFullDeck = 81;
    public static int CardsPerNormalDeck = 50;
    public static int MaxSpecialCharacterCardsPerDeck = 31;
    public static int StartingLife = 40;
    public static int CardsInHandStartingTheGame = 5;
    public static readonly string baseUrl = "https://card-game-engine-api.herokuapp.com";
    public static string RoomName;
    public static bool HasAddedApprovalCallback = false;
    public static string CurrentHostPassword;

    public static int FirstTurnQueue = -1;

    public enum CardType
    {
        Pokemon,
        Trainer,
        Energy
    }

    public static List<GameObject> players = new List<GameObject>();
}
