using MLAPI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSettingsManager : MonoBehaviour
{
    public GameObject SettingsMenu;

    public void OnSettingsButton()
    {
        SettingsMenu.SetActive(true);
    }

    public void OnSettingsExit()
    {
        SettingsMenu.SetActive(false);
    }



    public GameObject VictoryScreen;
    public GameObject DefeatScreen;

    public void OnConcede()
    {
        ulong id = NetworkManager.Singleton.LocalClientId;
        NetworkManager.Singleton.ConnectedClients.TryGetValue(id, out var networkClient);
        networkClient.PlayerObject.GetComponent<PlayerScript>().AnnounceWinnerServerRpc(false, id);

        DefeatScreen.SetActive(true);
    }

    public void OnReturnHome()
    {
        StartCoroutine(PlayerScript.StopNetwork(() =>
        {
            SceneManager.LoadScene("MainMenu");
        }));

    }

    public GameObject RematchPanel;
    public GameObject RematchPanelPlayer1;
    public GameObject RematchPanelPlayer2;

    public void OnRematchButton()
    {
        ulong id = NetworkManager.Singleton.LocalClientId;
        NetworkManager.Singleton.ConnectedClients.TryGetValue(id, out var networkClient);
        networkClient.PlayerObject.GetComponent<PlayerScript>().ConnectToRematch();

        RematchPanel.SetActive(true);
    }

    public void DisconnectFromRematch()
    {
        NetworkManager.Singleton.ConnectedClients.TryGetValue(NetworkManager.Singleton.LocalClientId, out var networkClient);
        networkClient.PlayerObject.GetComponent<PlayerScript>().IsReadyForRematch.Value = false;
        RematchPanel.SetActive(false);
    }
}
