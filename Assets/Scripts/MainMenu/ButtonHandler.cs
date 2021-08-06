using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using MLAPI;
using MLAPI.Spawning;
using MLAPI.Transports.UNET;
using MLAPI.SceneManagement;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.Networking;
using SimpleJSON;
using CardInformation;

namespace MainMenu
{
    public class ButtonHandler : MonoBehaviour
    {
        [SerializeField] private GameObject StartCanvas;
        [SerializeField] private InputField InputField; // this is the client room name but I'm too lazy to rename all it's references
        [SerializeField] private InputField HostRoomNameField;
        [SerializeField] private InputField PasswordField;
        [SerializeField] private InputField HostPasswordField;

        [SerializeField] private GameObject options;
        private readonly string WaitingText = "Select a deck...";

        [SerializeField] private GameObject Alert;
        [SerializeField] private GameObject MoreSettings;
        [SerializeField] private Dropdown FirstTurnDropdownObject;
        [SerializeField] private Dropdown AutoDrawDropdownObject;
        [SerializeField] private Dropdown AutoUntapDropdownObject;

        [SerializeField] private GameObject WaitingCanvas;
        [SerializeField] private Text WaitingCanvasText;
        [SerializeField] private GameObject LoadingIcon;

        private void ApprovalCheck(byte[] connectionData, ulong clientID, NetworkManager.ConnectionApprovedDelegate callback)
        {
            bool approve = System.Text.Encoding.ASCII.GetString(connectionData) == PlayerInfoManager.CurrentHostPassword;
            ulong? prefabHash = NetworkSpawnManager.GetPrefabHashFromGenerator("Player");

            callback(true, prefabHash, approve, new Vector3(0, 0, 0), Quaternion.identity);
        }

        public void Start()
        {
            if (!PlayerInfoManager.HasAddedApprovalCallback)
            {
                PlayerInfoManager.HasAddedApprovalCallback = true;
                NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
            }
            LoadOptions();
        }

        public void OnHostStart()
        {
            if (options.GetComponentInChildren<Text>().text == WaitingText)
            {
                Alert.SetActive(true);
                Alert.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "no deck selected";
                return;
            }

            MoreSettings.SetActive(true);
        }

        private string ToCorrectPasswordSize(string input)
        {
            char[] charRoomPassword = input.ToCharArray();
            char[] correctlySizedRoomPassword = new char[16];

            for (int i = 0; i < 16; i++)
            {
                if (i < charRoomPassword.Length)
                {
                    correctlySizedRoomPassword[i] = charRoomPassword[i];
                }
                else
                {
                    correctlySizedRoomPassword[i] = ' ';
                }
            }

            return new string(correctlySizedRoomPassword);
        }

        IEnumerator AddRoomNameToServer(Action callback, string roomName, string roomIp)
        {

#pragma warning disable IDE0028 // Simplify collection initialization
            List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
#pragma warning restore IDE0028 // Simplify collection initialization


            formData.Add(new MultipartFormDataSection($"roomName={roomName}&roomIp=" +
                $"{ DataEncryptDecrypt.Encrypt(roomIp, ToCorrectPasswordSize(HostPasswordField.GetComponent<InputField>().text)) }"));

            UnityWebRequest www = UnityWebRequest.Post(PlayerInfoManager.baseUrl, formData);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);

                if (www.responseCode == 503)
                {
                    MoreSettings.SetActive(false);
                    WaitingCanvas.SetActive(false);
                    StartCanvas.SetActive(true);
                    Alert.SetActive(true);
                    Alert.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "room name already taken";
                }

            }
            else
            {
                //Debug.Log("Form upload complete!");
                //Debug.Log(www.responseCode);
                callback();
            }




            //yield return null;

        }

        private static string GetLocalIPAddress()
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }

            throw new System.Exception("No network adapters with an IPv4 address in the system!");
        }

        IEnumerator SpinLoadingIcon()
        {
            int i = 0;
            while (true)
            {
                LoadingIcon.transform.localRotation = Quaternion.Euler(0, 0, i);
                i--;
                yield return new WaitForFixedUpdate();
            }
        }

        public void OnHostFinalStart()
        {
            PlayerInfoManager.FirstTurnQueue = FirstTurnDropdownObject.value;
            PlayerInfoManager.RoomName = HostRoomNameField.GetComponent<InputField>().text;
            PlayerInfoManager.CurrentHostPassword = HostPasswordField.GetComponent<InputField>().text;
            StartCanvas.SetActive(false);
            WaitingCanvasText.text = "Connecting to server...";
            WaitingCanvas.SetActive(true);
            StartCoroutine(SpinLoadingIcon());

            StartCoroutine(
                AddRoomNameToServer(() =>
                {
                    NetworkManager.Singleton.StartHost();
                    NetworkManager.Singleton.ConnectedClients.TryGetValue(NetworkManager.Singleton.LocalClientId, out var networkedClient);
                    if (networkedClient != null)
                    {
                        WaitingCanvasText.text = "Waiting for player to connect...";
                        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnect;
                    }
                    else
                    {
                        Debug.LogError("not connected successfully");
                    }
                }, HostRoomNameField.GetComponent<InputField>().text, GetLocalIPAddress())
            );
        }

        private delegate void startClient(string ip);

        IEnumerator GetIpAtRoom(startClient callback)
        {
            UnityWebRequest webRequest = UnityWebRequest.Get(PlayerInfoManager.baseUrl + "/" + InputField.text);

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogError(webRequest.error);
                yield break;
            }

            JSONNode jsonNode = JSON.Parse(webRequest.downloadHandler.text);

            if (jsonNode["roomIp"] != null)
            {
                string encryptedIP = jsonNode["roomIp"];
                string ip = DataEncryptDecrypt.Decrypt(
                    encryptedIP, ToCorrectPasswordSize(PasswordField.GetComponent<InputField>().text));

                if (ip == null)
                {
                    Alert.SetActive(true);
                    Alert.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "password was incorrect";
                }
                else
                {
                    callback(ip);
                }
            }
            else
            {
                Alert.SetActive(true);
                Alert.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "room not found";
            }

        }

        public void OnClientStart()
        {
            if (options.GetComponentInChildren<Text>().text == WaitingText)
            {
                Alert.SetActive(true);
                Alert.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "no deck selected";
                return;
            }


            StartCoroutine(GetIpAtRoom((string ip) =>
            {
                if (ip.Length == 0)
                {
                    NetworkManager.Singleton.GetComponent<UNetTransport>().ConnectAddress = "127.0.0.1";
                }
                else
                {
                    NetworkManager.Singleton.GetComponent<UNetTransport>().ConnectAddress = ip;
                }

                PlayerInfoManager.RoomName = InputField.GetComponent<InputField>().text;

                string password = PasswordField.text;
                NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes(password);
                NetworkManager.Singleton.StartClient();


                StartCanvas.SetActive(false);
            }));


        }

        public void OnClientConnect<T>(T obj)
        {
            NetworkManager.Singleton.ConnectedClients.TryGetValue(NetworkManager.Singleton.LocalClientId, out var networkedClient);
            if (networkedClient != null)
            {
                int input = -1;

                if (PlayerInfoManager.FirstTurnQueue == -1)
                {
                    Debug.LogError("No FirstTurnQueue provided");
                    return;
                }
                else if (PlayerInfoManager.FirstTurnQueue == 0)
                {
                    input = -2;
                }
                else if (PlayerInfoManager.FirstTurnQueue == 1)
                {
                    input = (int)NetworkManager.Singleton.LocalClientId;
                }
                else if (PlayerInfoManager.FirstTurnQueue == 2)
                {
                    foreach (var client in NetworkManager.Singleton.ConnectedClients)
                    {
                        if (!client.Value.PlayerObject.IsLocalPlayer)
                        {
                            input = (int)client.Value.ClientId;
                            break;
                        }
                    }
                }

                if (input == -1)
                {
                    Debug.LogError("invalid option on first turn");
                    return;
                }

                networkedClient.PlayerObject.GetComponent<PlayerScript>().GiveTurnInfoClientRpc(
                    input, AutoDrawDropdownObject.value == 0, AutoUntapDropdownObject.value == 0
                );
            }
            else
            {
                Debug.LogError("not connected successfully");
            }

            if (NetworkManager.Singleton.IsHost)
            {
                NetworkSceneManager.SwitchScene("GameScreen");
            }

        }



        public void OnDeckBuilder()
        {
            SceneManager.LoadScene("DeckBuilder");
        }

        void LoadOptions()
        {
            options.GetComponent<Dropdown>().ClearOptions();

            var decks = new List<string>();

            List<string> fileNames = new List<string>(Directory.GetFiles(Application.persistentDataPath));

            //For each string in the fileNames List   
            for (int i = 0; i < fileNames.Count; i++)
            {
                string currentPath = Path.GetFileName(fileNames[i]);

                if (currentPath.EndsWith(".deck"))
                {
                    decks.Add(currentPath.Split(new string[] { ".deck" }, StringSplitOptions.None)[0]);
                }
            }

            options.GetComponent<Dropdown>().AddOptions(new List<string> { WaitingText });
            options.GetComponent<Dropdown>().AddOptions(decks);
        }

        public void OnAlertClose()
        {
            Alert.SetActive(false);
        }

        public void OnSelectDeck()
        {
            PlayerInfoManager.deckName = GameObject.Find("StartCanvas/Deck/DeckSelector/Label").GetComponent<Text>().text;
            if (PlayerInfoManager.deckName == "Select a deck to...") return;

            string path = Application.persistentDataPath + "/" + PlayerInfoManager.deckName + ".deck";

            if (File.Exists(path))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                FileStream stream = new FileStream(path, FileMode.Open);

                SerializableDeck data = formatter.Deserialize(stream) as SerializableDeck;

                PlayerInfoManager.fullDeck = new Card[PlayerInfoManager.CardsPerNormalDeck];

                int i = 0;
                int j = 0;
                int k = 0;

                while (i < PlayerInfoManager.CardsPerNormalDeck && i + j + k < data.Deck.Length)
                {
                    if (data.Deck[i + j + k] != null)
                    {
                        CardType type = data.Deck[i + j + k].Type;
                        string card = data.Deck[i + j + k].Art;

                        PlayerInfoManager.fullDeck[i] = new Card()
                        {
                            art = card,
                            type = type
                        };
                        i++;

                    }
                    else
                    {
                        k++;
                    }

                }

                stream.Close();
            }
            else
            {
                Debug.LogError("Error: Save file not found in " + path);
            }
        }

        public void OnMoreSettingsCancel()
        {
            MoreSettings.SetActive(false);
        }

        public void OnCancelHost()
        {
            StopAllCoroutines();
            WaitingCanvas.SetActive(false);
            StartCanvas.SetActive(true);
            StartCoroutine(GameStateManager.DeleteRoom());
        }
    }
}