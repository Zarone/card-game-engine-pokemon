using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SerializableCard
{
    public string Art;
    public PlayerInfoManager.CardType Type;

    public SerializableCard(string art, PlayerInfoManager.CardType type)
    {
        Art = art;
        Type = type;
    }
}
