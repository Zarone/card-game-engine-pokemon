using MLAPI.Serialization;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card : INetworkSerializable
{
    public string art;
    public PlayerInfoManager.CardType type;

    public void NetworkSerialize(NetworkSerializer serializer)
    {
        serializer.Serialize(ref art);
        serializer.Serialize(ref type);
    }
}
