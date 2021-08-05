using MLAPI.Serialization;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CardInformation;

public class Card : INetworkSerializable
{
    public string art;
    public CardType type;

    public void NetworkSerialize(NetworkSerializer serializer)
    {
        serializer.Serialize(ref art);
        serializer.Serialize(ref type);
    }
}
