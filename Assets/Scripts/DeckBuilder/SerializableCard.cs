using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CardInformation;

[System.Serializable]
public class SerializableCard
{
    public string Art;
    public CardType Type;

    public SerializableCard(string art, CardType type)
    {
        Art = art;
        Type = type;
    }
}
