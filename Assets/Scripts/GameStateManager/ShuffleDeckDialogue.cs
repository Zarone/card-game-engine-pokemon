using MLAPI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShuffleDeckDialogue : MonoBehaviour
{
    public GameObject ShuffleDialogue;

    public void OnShuffleDialogueCancel()
    {
        ShuffleDialogue.SetActive(false);
    }

    public void OnShuffleDialogueConfirm()
    {
        NetworkManager.Singleton.ConnectedClients.TryGetValue(NetworkManager.Singleton.LocalClientId, out var networkClient);
        PlayerScript playerScript = networkClient.PlayerObject.GetComponent<PlayerScript>();
        playerScript.Deck.Value = CardManipulation.Shuffle(playerScript.Deck.Value);

        ShuffleDialogue.SetActive(false);
    }
}
