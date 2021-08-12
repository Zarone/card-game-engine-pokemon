using MLAPI;
using UnityEngine;

public class CoinManager : MonoBehaviour
{
    public GameObject CoinHead;
    public GameObject CoinTail;
    public GameObject CoinContainer;

    public GameObject FirstOrSecondText;
    public GameObject FirstOrSecondButtons;

    public GameObject HeadsOrTailsText;
    public GameObject HeadsOrTailsButtons;

    public GameObject WaitingText;

    public GameObject DieContainer;
    public GameObject Dice;

    public GameObject BlockView;

    public void OnCoinMenuHeadsOrTails(bool heads)
    {
        HeadsOrTailsButtons.SetActive(false);
        HeadsOrTailsText.SetActive(false);

        NetworkManager.Singleton.ConnectedClients.TryGetValue(NetworkManager.Singleton.LocalClientId, out var networkClient);

        networkClient.PlayerObject.GetComponent<PlayerScript>().SelectedHeadsOrTailsClientRpc(heads, Random.Range(0, 2));
    }

    public void OnCoinMenuFirstOrSecond(bool first)
    {
        NetworkManager.Singleton.ConnectedClients.TryGetValue(NetworkManager.Singleton.LocalClientId, out var networkClient);
        PlayerScript playerScript = networkClient.PlayerObject.GetComponent<PlayerScript>();
        playerScript.SelectedFirstOrSecondServerRpc(first, NetworkManager.Singleton.LocalClientId);
    }

    public void OnManualCoinFlipStart()
    {
        NetworkManager.Singleton.ConnectedClients.TryGetValue(NetworkManager.Singleton.LocalClientId, out var networkClient);
        networkClient.PlayerObject.GetComponent<PlayerScript>().ManualCoinFlipServerRpc(Random.Range(0, 2));
    }

    public void OnManualDieRollStart()
    {
        NetworkManager.Singleton.ConnectedClients.TryGetValue(NetworkManager.Singleton.LocalClientId, out var networkClient);
        networkClient.PlayerObject.GetComponent<PlayerScript>().ManualDieRollServerRpc(Random.Range(0, 101));
    }
}
