using System.Collections.Generic;
using UnityEngine;

public class PlayerInfoManager
{
    public static Card[] fullDeck;

    public static string deckName;
    public static int CardsPerNormalDeck = 60;
    public static int CardsInHandStartingTheGame = 7;
    public static readonly string baseUrl = "https://card-game-engine-api.herokuapp.com";
    public static string RoomName;
    public static bool HasAddedApprovalCallback = false;
    public static string CurrentHostPassword;

    public static int FirstTurnQueue = -1;

    //public enum CardType
    //{
    //    Pokemon,
    //    Trainer,
    //    Energy
    //}

    public static List<GameObject> players = new List<GameObject>();
}
