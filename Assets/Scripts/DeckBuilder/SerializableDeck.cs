using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SerializableDeck
{
    public SerializableCard[] Deck;

    public SerializableDeck(SerializableCard[] deck)
    {
        Deck = deck;
    }
}
