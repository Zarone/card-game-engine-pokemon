using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using UnityEngine.UI;
using MLAPI.NetworkVariable;
using UnityEngine.Networking;
using System.Collections;

public class GameStateManager : MonoBehaviour
{
    public GameObject playerDeckSprite;
    public GameObject oppDeckSprite;

    public GameObject playerDiscardSprite;
    public GameObject oppDiscardSprite;

    public GameObject playerLostZoneSprite;
    public GameObject oppLostZoneSprite;

    public GameObject playerHand;
    public GameObject oppHand;

    public GameObject LocalBench;
    public GameObject LocalActive;
    public GameObject OppBench;
    public GameObject OppActive;

    public GameObject AttachmentPrefab;
    public GameObject LevelContainer;
    public GameObject CounterContainerPlayer;
    public GameObject CounterContainerOpp;

    public GameObject PlayerPrizeCounter;
    public GameObject OppPrizeCounter;

    public GameObject PlayerPrizes;

    public Image PlayerSupporter;
    public Image OppSupporter;

    public enum SelectingMode
    {
        None,
        Gallery,
        GalleryMultiview,
        Hand,
        Deck,
        DeckSection,
        OpponentsHand,
        Discard,
        LostZone,
        Bench,
        Active,
        Attaching,
        AttachedBench,
        AttachedActive,
        Evolve,
        DeckOptions,
        CustomSection,
        Prizes,
        SelectingStartingPokemon,
    }

    readonly Dictionary<string, List<SelectingMode>> ButtonNameKeyValuePairs = new Dictionary<string, List<SelectingMode>>()
    {

        { "Cancel", new List<SelectingMode>(){
            SelectingMode.Hand, SelectingMode.Deck,
            SelectingMode.Discard, SelectingMode.Bench,
            SelectingMode.Active, SelectingMode.Attaching,
            SelectingMode.AttachedBench, SelectingMode.AttachedActive,
            SelectingMode.LostZone, SelectingMode.DeckOptions,
            SelectingMode.OpponentsHand }
        },

        { "OwnInfo", new List<SelectingMode>(){ SelectingMode.None } },
        { "OppInfo", new List<SelectingMode>(){ SelectingMode.None } },
        { "DeckOptions", new List<SelectingMode>(){ SelectingMode.None } },
        { "SpecialDeckView", new List<SelectingMode>(){ SelectingMode.None } },
        { "DiscardView", new List<SelectingMode>(){ SelectingMode.None } },
        { "LostZoneView", new List<SelectingMode>(){ SelectingMode.None } },

        { "TakePrize", new List<SelectingMode>() { SelectingMode.None } },
        { "PrizesView", new List<SelectingMode>(){ SelectingMode.None } },

        { "Draw", new List<SelectingMode>(){ SelectingMode.DeckOptions } },
        { "Mill", new List<SelectingMode>(){ SelectingMode.DeckOptions } },
        { "Shuffle", new List<SelectingMode>(){ SelectingMode.DeckOptions } },
        { "DeckView", new List<SelectingMode>(){ SelectingMode.DeckOptions } },
        { "ViewTopXCards", new List<SelectingMode>(){ SelectingMode.DeckOptions } },

        { "ToHand", new List<SelectingMode>(){
            SelectingMode.Deck,
            SelectingMode.Discard, SelectingMode.LostZone,
            SelectingMode.DeckSection
        }
        },
        { "Zone_ToHand", new List<SelectingMode>(){
            SelectingMode.Bench, SelectingMode.Active,
            SelectingMode.AttachedBench, SelectingMode.AttachedActive,
        }
        },
        { "Discard", new List<SelectingMode>(){
            SelectingMode.Deck,
            SelectingMode.DeckSection }
        },
        { "Zone_Discard", new List<SelectingMode>(){
            SelectingMode.Hand, SelectingMode.Bench, SelectingMode.Active,
            SelectingMode.AttachedBench, SelectingMode.AttachedActive,
            SelectingMode.Attaching}
        },
        { "LostZone", new List<SelectingMode>(){
            SelectingMode.Discard }
        },
        { "Zone_LostZone", new List<SelectingMode>(){
            SelectingMode.Hand, SelectingMode.Bench, SelectingMode.Active,
            SelectingMode.AttachedBench, SelectingMode.AttachedActive,
            SelectingMode.Attaching}
        },
        { "Attach", new List<SelectingMode>() { SelectingMode.Hand } },
        { "ToBottomOfDeck", new List<SelectingMode>(){ SelectingMode.Hand, SelectingMode.DeckSection,
            SelectingMode.Attaching } },
        { "ToTopOfDeck", new List<SelectingMode>(){ SelectingMode.Hand, SelectingMode.DeckSection,
            SelectingMode.Attaching } },

        { "Evolve", new List<SelectingMode>() { SelectingMode.Hand } },
        { "Devolve", new List<SelectingMode>() { SelectingMode.Bench, SelectingMode.Active } },
        { "ShuffleIntoDeck", new List<SelectingMode>(){
            SelectingMode.Hand, SelectingMode.Discard, SelectingMode.Bench,
            SelectingMode.Active, SelectingMode.AttachedBench,
            SelectingMode.AttachedActive, SelectingMode.Attaching}
        },
        { "MoveToBench", new List<SelectingMode>(){
            SelectingMode.Deck }
        },
        { "MoveToActive", new List<SelectingMode>(){
            SelectingMode.Attaching}
        },
        { "Zone_MoveToBench", new List<SelectingMode>(){
            SelectingMode.Hand, SelectingMode.Active }
        },
        { "Zone_MoveToActive", new List<SelectingMode>(){
            SelectingMode.Hand, SelectingMode.Bench }
        },
        { "Zone_PlayStadium", new List<SelectingMode>(){ SelectingMode.Hand } },
        { "Zone_PlaySupporter", new List<SelectingMode>(){ SelectingMode.Hand } },
        { "Tap", new List<SelectingMode>(){
            SelectingMode.Active, SelectingMode.Bench }
        },
        { "UntapAll", new List<SelectingMode>(){ SelectingMode.None } },
        { "AddCounter", new List<SelectingMode>(){ SelectingMode.Active, SelectingMode.Bench }
        },
        { "Flip", new List<SelectingMode>(){
            SelectingMode.Bench, SelectingMode.Active }
        },
        { "DeckIsSelected", new List<SelectingMode>() { SelectingMode.DeckOptions } },
        { "Reveal", new List<SelectingMode>() { SelectingMode.Hand, SelectingMode.Deck,
            SelectingMode.DeckSection, SelectingMode.Attaching}
        },
        { "ViewCardAndAttachments", new List<SelectingMode>(){ SelectingMode.Bench, SelectingMode.Active } },
        { "ManualCoinFlip", new List<SelectingMode>() { SelectingMode.None } },
        { "ManualDieRoll", new List<SelectingMode>() { SelectingMode.None } },
        { "Mulligan", new List<SelectingMode>(){ SelectingMode.SelectingStartingPokemon } },
        { "ViewNextMulligan", new List<SelectingMode>(){ SelectingMode.GalleryMultiview } },
        { "Remote_ToTopOfDeck", new List<SelectingMode>() { SelectingMode.CustomSection } },
    };

    [System.NonSerialized] public List<byte> selectedCards = new List<byte>();

    public static SelectingMode selectingMode = SelectingMode.None; // this is location of the cards being targeted
    public static SelectingMode viewingMode = SelectingMode.None; // this is only an indicator for the gallery view to tell what it's viewing

    public GameSettingsManager gameSettingsManager;
    public ShuffleDeckDialogue shuffleDeckDialogue;
    public CoinManager coinManager;

    void Start()
    {
        if (shuffleDeckDialogue == null)
        {
            Debug.LogError("shuffle deck dialogue is null");
        };
        NetworkManager.Singleton.OnClientDisconnectCallback += (ulong id) =>
        {
            StartCoroutine(DeleteRoom());
        };

        foreach (GameObject client in PlayerInfoManager.players)
        {
            PlayerScript player = client.GetComponent<PlayerScript>();

            if (player.IsLocalPlayer)
            {
                player.PrizeObj = PlayerPrizes;
                player.PrizeLabel = PlayerPrizeCounter;
                player.SupporterObj = PlayerSupporter;
            }
            else
            {
                player.PrizeLabel = OppPrizeCounter;
                player.SupporterObj = OppSupporter;
            }

            if (!player.HasStarted) player.RunFirst();

            CardSection section = client.GetComponent<CardSection>();
            if (section.IsLocalPlayer)
            {
                section.BenchObj = LocalBench;
                section.ActiveObj = LocalActive;
            }
            else
            {
                section.BenchObj = OppBench;
                section.ActiveObj = OppActive;
            }
        }
        //RenderCorrectButtons(SelectingMode.None);
    }

    public static IEnumerator DeleteRoom(System.Action callback = null)
    {
        if (PlayerInfoManager.RoomName == default)
        {
            callback?.Invoke();
            yield break;
        }

        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();

        UnityWebRequest www = UnityWebRequest.Post(PlayerInfoManager.baseUrl + "/"
         + PlayerInfoManager.RoomName + "/delete", formData);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
        }
        else
        {
            //Debug.Log(www.responseCode);
            callback?.Invoke();
        }

    }

    public void RenderDeck(bool isLocal, int numberOfCards)
    {
        if (isLocal)
        {
            playerDeckSprite.GetComponent<Image>().color = new Color(1, 1, 1, numberOfCards > 0 ? numberOfCards / 50f + 0.3f : 0);
        }
        else
        {
            oppDeckSprite.GetComponent<Image>().color = new Color(1, 1, 1, numberOfCards > 0 ? numberOfCards / 50f + 0.3f : 0);
        }
    }

    public GameObject mainButtonsPanel;
    public GameObject ButtonContent;
    [SerializeField] private GameObject ClickMenu;
    public void RenderCorrectButtons(SelectingMode selecting)
    {
        if (selectingMode != SelectingMode.None)
        {
            shuffleDeckDialogue.OnShuffleDialogueCancel();
        }

        foreach (Transform child in ButtonContent.transform)
        {
            if (!ButtonNameKeyValuePairs.ContainsKey(child.gameObject.name)) Debug.LogError($"couldn't find button {child.gameObject.name}");
            child.gameObject.SetActive(ButtonNameKeyValuePairs[child.gameObject.name].Contains(selecting));
        }

        foreach (Transform child in ClickMenu.transform)
        {
            if (!ButtonNameKeyValuePairs.ContainsKey(child.gameObject.name)) Debug.LogError($"couldn't find button {child.gameObject.name}");
            child.gameObject.SetActive(ButtonNameKeyValuePairs[child.gameObject.name].Contains(selecting));
        }
    }

    public GameObject CardCloseupPanel;
    public GameObject CardCloseupCard;
    public void OnCardRightClick(Sprite image)
    {
        CardCloseupCard.GetComponent<Image>().sprite = image;
        CardCloseupPanel.SetActive(true);
        mainButtonsPanel.SetActive(false);
    }

    public void OnCardCloseupClose()
    {
        CardCloseupPanel.SetActive(false);
        mainButtonsPanel.SetActive(true);
    }

    public GameObject TurnText;
    public GameObject TurnColor;
    public GameObject TurnButton;
    public void EndTurn()
    {
        ulong id = NetworkManager.Singleton.LocalClientId;
        NetworkManager.Singleton.ConnectedClients.TryGetValue(id, out var client);
        client.PlayerObject.GetComponent<PlayerScript>().SwitchTurnsServerRpc(id);
    }



    public void OnSelectCancel()
    {
        if (selectingMode == SelectingMode.Hand)
        {
            foreach (GameObject client in PlayerInfoManager.players)
            {
                PlayerScript player = client.GetComponent<PlayerScript>();
                if (player.IsLocalPlayer)
                {
                    player.RenderHandSelectingCancel();
                }
            }
        }
        else if (selectingMode == SelectingMode.Deck || selectingMode == SelectingMode.Discard || selectingMode == SelectingMode.LostZone)
        {
            OnGallerySelectExit();
        }
        else if (selectingMode == SelectingMode.Bench)
        {
            foreach (GameObject client in PlayerInfoManager.players)
            {
                PlayerScript player = client.GetComponent<PlayerScript>();
                if (player.IsLocalPlayer)
                {
                    player.cardSection.RenderSectionSelectingCancel(player.cardSection.BenchObj);
                }
            }
        }
        else if (selectingMode == SelectingMode.Active)
        {
            foreach (GameObject client in PlayerInfoManager.players)
            {
                PlayerScript player = client.GetComponent<PlayerScript>();
                if (player.IsLocalPlayer)
                {
                    player.cardSection.RenderSectionSelectingCancel(player.cardSection.ActiveObj);
                }
            }
        }
        else if (selectingMode == SelectingMode.Attaching)
        {
            selectingMode = SelectingMode.None;
            selectedCards = new List<byte>();

            foreach (GameObject client in PlayerInfoManager.players)
            {
                PlayerScript player = client.GetComponent<PlayerScript>();
                if (player.IsLocalPlayer)
                {
                    foreach (Transform child in player.PlayerHand.transform)
                    {
                        child.gameObject.GetComponent<Image>().color = CardManipulation.Normal;
                    }

                }
                foreach (Transform child in player.cardSection.ActiveObj.transform)
                {
                    child.gameObject.GetComponent<Image>().color = CardManipulation.Normal;
                }

                foreach (Transform child in player.cardSection.BenchObj.transform)
                {
                    child.gameObject.GetComponent<Image>().color = CardManipulation.Normal;
                }
            }
        }
        else if (selectingMode == SelectingMode.AttachedBench)
        {
            foreach (GameObject client in PlayerInfoManager.players)
            {
                PlayerScript player = client.GetComponent<PlayerScript>();
                if (player.IsLocalPlayer)
                {
                    player.cardSection.RenderAttachmentSelectionSelectingCancel(player.cardSection.BenchObj.transform);
                }
            }
        }
        else if (selectingMode == SelectingMode.AttachedActive)
        {
            foreach (GameObject client in PlayerInfoManager.players)
            {
                PlayerScript player = client.GetComponent<PlayerScript>();
                if (player.IsLocalPlayer)
                {
                    player.cardSection.RenderAttachmentSelectionSelectingCancel(player.cardSection.ActiveObj.transform);
                }
            }
        }
        else if (selectingMode == SelectingMode.DeckOptions)
        {
            selectingMode = SelectingMode.None;
        }

        RenderCorrectButtons(SelectingMode.None);
    }

    public void OnRevealHand()
    {
        foreach (GameObject client in PlayerInfoManager.players)
        {
            PlayerScript player = client.GetComponent<PlayerScript>();
            if (player.IsLocalPlayer)
            {
                player.RevealHandServerRpc(NetworkManager.Singleton.LocalClientId, player.Hand.Value);
            }
        }

        RenderCorrectButtons(SelectingMode.None);
    }

    public void OnReveal()
    {


        foreach (GameObject client in PlayerInfoManager.players)
        {
            PlayerScript player = client.GetComponent<PlayerScript>();
            if (player.IsLocalPlayer)
            {
                Card[] revealedCards = new Card[selectedCards.Count];
                Card[] from = player.IsLocal(selectingMode) ?
                    player.ModeToLocalDeck(selectingMode).Value : player.ModeToNetworkDeck(selectingMode).Value;

                for (int i = 0; i < revealedCards.Length; i++)
                {
                    revealedCards[i] = from[selectedCards[i]];
                }

                if (selectingMode == SelectingMode.Attaching)
                {
                    foreach (GameObject _client in PlayerInfoManager.players)
                    {
                        CardSection playerCardSection = _client.GetComponent<PlayerScript>().cardSection;
                        foreach (Transform child in playerCardSection.BenchObj.transform)
                        {
                            child.gameObject.GetComponent<Image>().color = CardManipulation.Normal;
                        }
                        foreach (Transform child in playerCardSection.ActiveObj.transform)
                        {
                            child.gameObject.GetComponent<Image>().color = CardManipulation.Normal;
                        }
                    }
                }

                player.RevealHandServerRpc(NetworkManager.Singleton.LocalClientId, revealedCards);
                OnSelectCancel();
            }
        }

        RenderCorrectButtons(SelectingMode.None);
    }

    public void OnDiscard()
    {
        foreach (GameObject client in PlayerInfoManager.players)
        {
            PlayerScript player = client.GetComponent<PlayerScript>();
            if (player.IsLocalPlayer)
            {
                player.GameAction(PlayerScript.Action.Discard);
            }
        }

        RenderCorrectButtons(SelectingMode.None);
    }

    public void OnRemoveFromPlay()
    {
        foreach (GameObject client in PlayerInfoManager.players)
        {
            PlayerScript player = client.GetComponent<PlayerScript>();
            if (player.IsLocalPlayer)
            {
                player.GameAction(PlayerScript.Action.LostZone);
            }
        }

        RenderCorrectButtons(SelectingMode.None);
    }

    public void OnShuffle()
    {
        foreach (GameObject client in PlayerInfoManager.players)
        {
            PlayerScript player = client.GetComponent<PlayerScript>();
            if (player.IsLocalPlayer)
            {
                player.Deck.Value = CardManipulation.Shuffle(player.Deck.Value);
            }
        }
        selectingMode = SelectingMode.None;
        RenderCorrectButtons(SelectingMode.None);
    }

    public void OnShuffleIntoDeck()
    {
        foreach (GameObject client in PlayerInfoManager.players)
        {
            PlayerScript player = client.GetComponent<PlayerScript>();
            if (player.IsLocalPlayer)
            {
                player.GameAction(PlayerScript.Action.ShuffleIntoDeck);
            }
        }
        RenderCorrectButtons(SelectingMode.None);
    }

    public void OnMoveToBottomOfDeck()
    {
        foreach (GameObject client in PlayerInfoManager.players)
        {
            PlayerScript player = client.GetComponent<PlayerScript>();
            if (player.IsLocalPlayer)
            {
                player.GameAction(PlayerScript.Action.ToBottomOfDeck);
            }
        }
        RenderCorrectButtons(SelectingMode.None);
    }

    public void OnMoveToTopOfDeck()
    {
        foreach (GameObject client in PlayerInfoManager.players)
        {
            PlayerScript player = client.GetComponent<PlayerScript>();
            if (player.IsLocalPlayer)
            {
                player.GameAction(PlayerScript.Action.ToTopOfDeck);
            }
        }
        RenderCorrectButtons(SelectingMode.None);
    }

    public void OnMoveToTopOfDeckFromSection()
    {
        foreach (GameObject client in PlayerInfoManager.players)
        {
            PlayerScript player = client.GetComponent<PlayerScript>();
            if (player.IsLocalPlayer)
            {
                player.GameAction(PlayerScript.Action.ToTopOfDeckFromSection);
            }
        }
        RenderCorrectButtons(SelectingMode.None);
    }

    public void OnMoveToReserve()
    {
        foreach (GameObject client in PlayerInfoManager.players)
        {
            PlayerScript player = client.GetComponent<PlayerScript>();
            if (player.IsLocalPlayer)
            {
                player.GameAction(PlayerScript.Action.Bench);
            }
        }
        RenderCorrectButtons(SelectingMode.None);
    }

    public void OnMoveToBattlefield()
    {
        foreach (GameObject client in PlayerInfoManager.players)
        {
            PlayerScript player = client.GetComponent<PlayerScript>();
            if (player.IsLocalPlayer)
            {
                player.GameAction(PlayerScript.Action.Active);
            }
        }
        RenderCorrectButtons(SelectingMode.None);
    }

    public void OnToHand()
    {
        foreach (GameObject client in PlayerInfoManager.players)
        {
            PlayerScript player = client.GetComponent<PlayerScript>();
            if (player.IsLocalPlayer)
            {
                player.GameAction(PlayerScript.Action.ToHand);
            }
        }
        RenderCorrectButtons(SelectingMode.None);
    }

    public void OnTap()
    {
        foreach (GameObject client in PlayerInfoManager.players)
        {
            PlayerScript player = client.GetComponent<PlayerScript>();
            if (player.IsLocalPlayer)
            {
                player.GameAction(PlayerScript.Action.Tap);
            }
        }
        RenderCorrectButtons(SelectingMode.None);
    }

    public void OnFlip()
    {
        foreach (GameObject client in PlayerInfoManager.players)
        {
            PlayerScript player = client.GetComponent<PlayerScript>();
            if (player.IsLocalPlayer)
            {
                player.GameAction(PlayerScript.Action.Flip);
            }
        }
        RenderCorrectButtons(SelectingMode.None);
    }

    public void OnAddCounter()
    {
        foreach (GameObject client in PlayerInfoManager.players)
        {
            PlayerScript player = client.GetComponent<PlayerScript>();
            if (player.IsLocalPlayer)
            {
                player.GameAction(PlayerScript.Action.AddCounter);
            }
        }
        selectedCards = new List<byte>();
        selectingMode = SelectingMode.None;
        RenderCorrectButtons(SelectingMode.None);
    }

    public void OnAttach()
    {
        selectingMode = SelectingMode.Attaching;

        foreach (GameObject client in PlayerInfoManager.players)
        {
            PlayerScript player = client.GetComponent<PlayerScript>();
            if (player.IsLocalPlayer)
            {
                player.GameAction(PlayerScript.Action.AttachStart);
            }
        }
        RenderCorrectButtons(SelectingMode.Attaching);
    }

    public void OnLevelUp()
    {
        if (selectedCards.Count == 1)
        {
            selectingMode = SelectingMode.Evolve;

            foreach (GameObject client in PlayerInfoManager.players)
            {
                PlayerScript player = client.GetComponent<PlayerScript>();
                if (player.IsLocalPlayer)
                {
                    player.GameAction(PlayerScript.Action.LevelUpStart);
                }
            }
            RenderCorrectButtons(SelectingMode.Evolve);
        }

    }

    public void OnLevelDown()
    {
        if (selectedCards.Count == 1)
        {
            foreach (GameObject client in PlayerInfoManager.players)
            {
                PlayerScript player = client.GetComponent<PlayerScript>();
                if (player.IsLocalPlayer)
                {
                    player.GameAction(PlayerScript.Action.LevelDown);
                }
            }
        }
    }

    public void OnDeckZone()
    {
        selectedCards = new List<byte>();
        selectingMode = SelectingMode.DeckOptions;
        RenderCorrectButtons(SelectingMode.DeckOptions);
    }

    public void OnRemoteToTopOfDeck()
    {
        NetworkManager.Singleton.ConnectedClients.TryGetValue(NetworkManager.Singleton.LocalClientId, out var networkClient);
        networkClient.PlayerObject.GetComponent<PlayerScript>().RemoteToTopOfDeckServerRpc(NetworkManager.Singleton.LocalClientId,
            CustomViewBounds, selectedCards.ToArray());
        selectedCards = new List<byte>();
    }

    public void OnMulligan()
    {
        foreach (GameObject client in PlayerInfoManager.players)
        {
            PlayerScript player = client.GetComponent<PlayerScript>();
            if (player.IsLocalPlayer)
            {
                player.GameAction(PlayerScript.Action.Mulligan);
            }
        }
    }
    
    public void OnViewNextMulligan()
    {
        if (MultiviewIndex != MultiviewFinalIndex)
        {
            NetworkManager.Singleton.ConnectedClients.TryGetValue(NetworkManager.Singleton.LocalClientId, out var networkClient);
            PlayerScript localScript = networkClient.PlayerObject.GetComponent<PlayerScript>();

            localScript.RequestShareMulliganInfoServerRpc(NetworkManager.Singleton.LocalClientId, MultiviewIndex + 1);
        }
    }
    
    public void OnTopOfDeckFirstButton()
    {
        actionQueue = PlayerScript.Action.ViewTopOfDeck;
        howMany = 0;
        howManyCountObj.GetComponent<Text>().text = howMany.ToString();
        howManyObj.SetActive(true);
    }

    public void OnMillFirstButton()
    {
        actionQueue = PlayerScript.Action.Mill;
        howMany = 0;
        howManyCountObj.GetComponent<Text>().text = howMany.ToString();
        howManyObj.SetActive(true);
    }

    public void OnDrawFirstButton()
    {
        actionQueue = PlayerScript.Action.Draw;
        howMany = 0;
        howManyCountObj.GetComponent<Text>().text = howMany.ToString();
        howManyObj.SetActive(true);
    }

    public void OnRevealTopOfDeckFirstButton()
    {
        actionQueue = PlayerScript.Action.RevealTopOfDeck;
        howMany = 0;
        howManyCountObj.GetComponent<Text>().text = howMany.ToString();
        howManyObj.SetActive(true);
    }

    public void OnAllowEditTopOfDeckFirstButton()
    {
        actionQueue = PlayerScript.Action.AllowEditTopOfDeck;
        howMany = 0;
        howManyCountObj.GetComponent<Text>().text = howMany.ToString();
        howManyObj.SetActive(true);
    }

    public void OnTakePrizeFirstButton()
    {
        actionQueue = PlayerScript.Action.TakePrize;
        howMany = 0;
        howManyCountObj.GetComponent<Text>().text = howMany.ToString();
        howManyObj.SetActive(true);
    }

    public void OnPlaySupporter()
    {
        if (selectedCards.Count == 1)
        {

            foreach (GameObject client in PlayerInfoManager.players)
            {
                PlayerScript player = client.GetComponent<PlayerScript>();
                if (player.IsLocalPlayer)
                {
                    player.GameAction(PlayerScript.Action.PlaySupporter);
                }
            }

            RenderCorrectButtons(SelectingMode.None);
        }
    }

    public void OnPlayStadium()
    {

    }



    public GameObject GalleryView;
    [SerializeField] private Transform GalleryContent;

    public void OnDeckView()
    {
        viewingMode = SelectingMode.Deck;
        selectingMode = SelectingMode.None;
        AdjustGalleryViewSize();
        GalleryView.SetActive(true);

        var children = new List<GameObject>();
        foreach (Transform child in GalleryContent.transform) children.Add(child.gameObject);
        children.ForEach(child => Destroy(child));

        PlayerScript clientCode = null;

        foreach (GameObject client in PlayerInfoManager.players)
        {
            clientCode = client.GetComponent<PlayerScript>();
            if (clientCode.IsLocalPlayer)
            {
                break;
            }
        }


        for (int i = 0; i < clientCode.Deck.Value.Length; i++)
        {
            GameObject cardObj = Instantiate(clientCode.CardPrefab, GalleryContent);
            //cardObj.GetComponent<CardRightClickHandler>().onRightClick = OnCardRightClick;
            cardObj.name = i.ToString();
            cardObj.GetComponent<CardRightClickHandler>().onRightClick = (Sprite image) =>
            {
                OnCardRightClick(image);

                //CardCloseupCard.transform.localRotation = Quaternion.Euler(0, 0, 0);
            };


            cardObj.GetComponent<Button>().onClick.RemoveAllListeners();
            cardObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (clientCode.IsLocalPlayer && (selectingMode == SelectingMode.Deck || selectingMode == SelectingMode.None))
                {
                    if (selectingMode != SelectingMode.Deck)
                    {
                        RenderGalleryCardSelected();
                        selectingMode = SelectingMode.Deck;
                        RenderCorrectButtons(SelectingMode.Deck);
                    }
                    if (cardObj.GetComponent<Image>().color == CardManipulation.Unselected)
                    {
                        selectedCards.Add(byte.Parse(cardObj.name));
                        cardObj.GetComponent<Image>().color = CardManipulation.Selected;
                    }
                    else if (cardObj.GetComponent<Image>().color == CardManipulation.Selected)
                    {
                        selectedCards.Remove(byte.Parse(cardObj.name));
                        if (selectedCards.Count < 1)
                        {
                            selectingMode = SelectingMode.None;
                            RenderGalleryCardSelectedCancel();
                            RenderCorrectButtons(SelectingMode.None);
                        }
                        else
                        {
                            cardObj.GetComponent<Image>().color = CardManipulation.Unselected;
                        }
                    }
                }
            });

            //string query = "Cards/" + ((int)clientCode.Deck.Value[i].type).ToString() + "/" + clientCode.Deck.Value[i].art + "-01";
            string query = clientCode.Deck.Value[i].art;
            Sprite[] sprites = Resources.LoadAll<Sprite>(query);
            cardObj.GetComponent<Image>().sprite = sprites[0];
        }

        RenderCorrectButtons(SelectingMode.Gallery);
    }

    public void OnTopOfDeckView()
    {
        viewingMode = SelectingMode.DeckSection;
        selectingMode = SelectingMode.None;
        AdjustGalleryViewSize();
        GalleryView.SetActive(true);

        var children = new List<GameObject>();
        foreach (Transform child in GalleryContent.transform) children.Add(child.gameObject);
        children.ForEach(child => Destroy(child));

        PlayerScript clientCode = null;

        foreach (GameObject client in PlayerInfoManager.players)
        {
            clientCode = client.GetComponent<PlayerScript>();
            if (clientCode.IsLocalPlayer)
            {
                break;
            }
        }

        for (int i = 0; i < howMany && i < clientCode.deckSize.Value; i++)
        {
            GameObject cardObj = Instantiate(clientCode.CardPrefab, GalleryContent);
            cardObj.name = i.ToString();
            cardObj.GetComponent<CardRightClickHandler>().onRightClick = (Sprite image) =>
            {
                OnCardRightClick(image);

                //CardCloseupCard.transform.localRotation = Quaternion.Euler(0, 0, 0);
            };

            cardObj.GetComponent<Button>().onClick.RemoveAllListeners();
            cardObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (clientCode.IsLocalPlayer && (selectingMode == SelectingMode.DeckSection || selectingMode == SelectingMode.None))
                {
                    if (selectingMode != SelectingMode.DeckSection)
                    {
                        RenderGalleryCardSelected();
                        selectingMode = SelectingMode.DeckSection;
                        RenderCorrectButtons(SelectingMode.DeckSection);
                    }
                    if (cardObj.GetComponent<Image>().color == CardManipulation.Unselected)
                    {
                        selectedCards.Add(byte.Parse(cardObj.name));
                        cardObj.GetComponent<Image>().color = CardManipulation.Selected;
                    }
                    else if (cardObj.GetComponent<Image>().color == CardManipulation.Selected)
                    {
                        selectedCards.Remove(byte.Parse(cardObj.name));
                        if (selectedCards.Count < 1)
                        {
                            selectingMode = SelectingMode.None;
                            RenderGalleryCardSelectedCancel();
                            RenderCorrectButtons(SelectingMode.None);
                        }
                        else
                        {
                            cardObj.GetComponent<Image>().color = CardManipulation.Unselected;
                        }
                    }
                }
            });

            //string query = "Cards/" + ((int)clientCode.Deck.Value[i].type).ToString() + "/" + clientCode.Deck.Value[i].art + "-01";
            string query = clientCode.Deck.Value[i].art;
            Sprite[] sprites = Resources.LoadAll<Sprite>(query);
            cardObj.GetComponent<Image>().sprite = sprites[0];
        }

        RenderCorrectButtons(SelectingMode.Gallery);
    }

    public void OnDiscardView()
    {
        viewingMode = SelectingMode.Discard;
        AdjustGalleryViewSize();
        GalleryView.SetActive(true);

        var children = new List<GameObject>();
        foreach (Transform child in GalleryContent.transform) children.Add(child.gameObject);
        children.ForEach(child => Destroy(child));

        PlayerScript clientCode = null;

        foreach (GameObject client in PlayerInfoManager.players)
        {
            clientCode = client.GetComponent<PlayerScript>();
            if (clientCode.IsLocalPlayer)
            {
                break;
            }
        }


        for (int i = 0; i < clientCode.Discard.Value.Length; i++)
        {
            GameObject cardObj = Instantiate(clientCode.CardPrefab, GalleryContent);
            cardObj.name = i.ToString();
            //cardObj.GetComponent<CardRightClickHandler>().onRightClick = OnCardRightClick;
            cardObj.GetComponent<CardRightClickHandler>().onRightClick = (Sprite image) =>
            {
                OnCardRightClick(image);

                //CardCloseupCard.transform.localRotation = Quaternion.Euler(0, 0, 0);
            };

            cardObj.GetComponent<Button>().onClick.RemoveAllListeners();
            cardObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (clientCode.IsLocalPlayer && (selectingMode == SelectingMode.Discard || selectingMode == SelectingMode.None))
                {
                    if (selectingMode != SelectingMode.Discard)
                    {
                        RenderGalleryCardSelected();
                        selectingMode = SelectingMode.Discard;
                        RenderCorrectButtons(SelectingMode.Discard);
                    }
                    if (cardObj.GetComponent<Image>().color == CardManipulation.Unselected)
                    {
                        selectedCards.Add(byte.Parse(cardObj.name));
                        cardObj.GetComponent<Image>().color = CardManipulation.Selected;
                    }
                    else if (cardObj.GetComponent<Image>().color == CardManipulation.Selected)
                    {
                        selectedCards.Remove(byte.Parse(cardObj.name));
                        if (selectedCards.Count < 1)
                        {
                            selectingMode = SelectingMode.None;
                            RenderGalleryCardSelectedCancel();
                            RenderCorrectButtons(SelectingMode.None);
                        }
                        else
                        {
                            cardObj.GetComponent<Image>().color = CardManipulation.Unselected;
                        }
                    }
                }
            });

            //string query = "Cards/" + ((int)clientCode.Discard.Value[i].type).ToString() + "/" + clientCode.Discard.Value[i].art + "-01";
            string query = clientCode.Discard.Value[i].art;
            Sprite[] sprites = Resources.LoadAll<Sprite>(query);
            cardObj.GetComponent<Image>().sprite = sprites[0];
        }

        RenderCorrectButtons(SelectingMode.Gallery);
    }

    public void OnRemoveFromPlayView()
    {
        viewingMode = SelectingMode.LostZone;
        AdjustGalleryViewSize();
        GalleryView.SetActive(true);

        var children = new List<GameObject>();
        foreach (Transform child in GalleryContent.transform) children.Add(child.gameObject);
        children.ForEach(child => Destroy(child));

        PlayerScript clientCode = null;

        foreach (GameObject client in PlayerInfoManager.players)
        {
            clientCode = client.GetComponent<PlayerScript>();
            if (clientCode.IsLocalPlayer)
            {
                break;
            }
        }


        for (int i = 0; i < clientCode.LostZone.Value.Length; i++)
        {
            GameObject cardObj = Instantiate(clientCode.CardPrefab, GalleryContent);
            cardObj.name = i.ToString();
            //cardObj.GetComponent<CardRightClickHandler>().onRightClick = OnCardRightClick;
            cardObj.GetComponent<CardRightClickHandler>().onRightClick = (Sprite image) =>
            {
                OnCardRightClick(image);

                //CardCloseupCard.transform.localRotation = Quaternion.Euler(0, 0, 0);
            };

            cardObj.GetComponent<Button>().onClick.RemoveAllListeners();
            cardObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (clientCode.IsLocalPlayer && (selectingMode == SelectingMode.LostZone || selectingMode == SelectingMode.None))
                {
                    if (selectingMode != SelectingMode.LostZone)
                    {
                        RenderGalleryCardSelected();
                        selectingMode = SelectingMode.LostZone;
                        RenderCorrectButtons(SelectingMode.LostZone);
                    }
                    if (cardObj.GetComponent<Image>().color == CardManipulation.Unselected)
                    {
                        selectedCards.Add(byte.Parse(cardObj.name));
                        cardObj.GetComponent<Image>().color = CardManipulation.Selected;
                    }
                    else if (cardObj.GetComponent<Image>().color == CardManipulation.Selected)
                    {
                        selectedCards.Remove(byte.Parse(cardObj.name));
                        if (selectedCards.Count < 1)
                        {
                            selectingMode = SelectingMode.None;
                            RenderGalleryCardSelectedCancel();
                            RenderCorrectButtons(SelectingMode.None);
                        }
                        else
                        {
                            cardObj.GetComponent<Image>().color = CardManipulation.Unselected;
                        }
                    }
                }
            });

            //string query = "Cards/" + ((int)clientCode.LostZone.Value[i].type).ToString() + "/" + clientCode.LostZone.Value[i].art + "-01";
            string query = clientCode.LostZone.Value[i].art;
            Sprite[] sprites = Resources.LoadAll<Sprite>(query);
            cardObj.GetComponent<Image>().sprite = sprites[0];
        }

        RenderCorrectButtons(SelectingMode.Gallery);
    }

    public void OnOppSpecialDeckView()
    {
        foreach (GameObject client in PlayerInfoManager.players)
        {
            PlayerScript player = client.GetComponent<PlayerScript>();
            if (!player.IsLocalPlayer)
            {
                OnCustomViewOnly(player.SpecialDeck.Value);
                break;
            }
        }
    }

    public void OnOppDiscardView()
    {
        foreach (GameObject client in PlayerInfoManager.players)
        {
            PlayerScript player = client.GetComponent<PlayerScript>();
            if (!player.IsLocalPlayer)
            {
                OnCustomViewOnly(player.Discard.Value);
                break;
            }
        }
    }

    public void OnOppRemoveFromPlayView()
    {
        foreach (GameObject client in PlayerInfoManager.players)
        {
            PlayerScript player = client.GetComponent<PlayerScript>();
            if (!player.IsLocalPlayer)
            {
                OnCustomViewOnly(player.LostZone.Value);
                break;
            }
        }
    }

    public void OnViewCardAndAttachments()
    {
        if (selectedCards.Count != 1) return;

        NetworkManager.Singleton.ConnectedClients.TryGetValue(NetworkManager.Singleton.LocalClientId, out var networkClient);
        PlayerScript playerScript = networkClient.PlayerObject.GetComponent<PlayerScript>();

        List<Card> cards = new List<Card>
        {
            playerScript.ModeToNetworkDeck(selectingMode).Value[selectedCards[0]]
        };

        NetworkVariable<Card[][]> levelInfo = playerScript.GetLevel(selectingMode);

        if (levelInfo != null)
        {
            foreach (Card card in levelInfo.Value[selectedCards[0]])
            {
                cards.Add(card);
            }
        }
        foreach (Card card in playerScript.CheckAttachments(selectingMode).Value[selectedCards[0]])
        {
            cards.Add(card);
        }

        selectedCards = new List<byte>();
        selectingMode = SelectingMode.None;
        RenderCorrectButtons(SelectingMode.None);

        foreach (Transform child in playerScript.cardSection.BenchObj.transform)
        {
            child.gameObject.GetComponent<Image>().color = CardManipulation.Normal;
        }
        foreach (Transform child in playerScript.cardSection.ActiveObj.transform)
        {
            child.gameObject.GetComponent<Image>().color = CardManipulation.Normal;
        }

        OnCustomViewOnly(cards.ToArray());
    }

    private int MultiviewIndex = -1;
    private int MultiviewFinalIndex = -1;
    // the multi view variables are for viewing mulligans
    public void OnCustomViewOnly(Card[] cards, bool multiview = false, int multiviewIndex = -1, int multiviewFinalIndex = -1)
    {
        PlayerScript clientCode = PlayerInfoManager.players[0].GetComponent<PlayerScript>();

        //clientCode.RenderHandSelectingCancel();

        AdjustGalleryViewSize();
        GalleryView.SetActive(true);

        var children = new List<GameObject>();
        foreach (Transform child in GalleryContent.transform) children.Add(child.gameObject);
        children.ForEach(child => Destroy(child));


        for (int i = 0; i < cards.Length; i++)
        {
            GameObject cardObj = Instantiate(clientCode.CardPrefab, GalleryContent);
            cardObj.name = i.ToString();
            cardObj.GetComponent<CardRightClickHandler>().onRightClick = (Sprite image) =>
            {
                OnCardRightClick(image);
            };

            string query = cards[i].art;
            Sprite[] sprites = Resources.LoadAll<Sprite>(query);
            cardObj.GetComponent<Image>().sprite = sprites[0];
        }

        MultiviewIndex = multiviewIndex;
        MultiviewFinalIndex = multiviewFinalIndex;
        RenderCorrectButtons(multiview ? SelectingMode.GalleryMultiview : SelectingMode.Gallery);
    }

    private byte CustomViewBounds;
    public void OnCustomViewWithEditAccess(Card[] cards)
    {
        CustomViewBounds = (byte)cards.Length;
        selectingMode = SelectingMode.Gallery;
        AdjustGalleryViewSize();
        GalleryView.SetActive(true);

        var children = new List<GameObject>();
        foreach (Transform child in GalleryContent.transform) children.Add(child.gameObject);
        children.ForEach(child => Destroy(child));

        PlayerScript clientCode = PlayerInfoManager.players[0].GetComponent<PlayerScript>();

        for (int i = 0; i < cards.Length; i++)
        {
            GameObject cardObj = Instantiate(clientCode.CardPrefab, GalleryContent);
            cardObj.name = i.ToString();
            cardObj.GetComponent<CardRightClickHandler>().onRightClick = OnCardRightClick;
            cardObj.GetComponent<CardRightClickHandler>().onRightClick = (Sprite image) =>
            {
                OnCardRightClick(image);

                //CardCloseupCard.transform.localRotation = Quaternion.Euler(0, 0, 0);
            };

            cardObj.GetComponent<Button>().onClick.RemoveAllListeners();
            cardObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (selectingMode == SelectingMode.Gallery)
                {
                    RenderGalleryCardSelected();
                    selectingMode = SelectingMode.CustomSection;
                    RenderCorrectButtons(SelectingMode.CustomSection);
                }

                if (selectingMode == SelectingMode.CustomSection)
                {

                    if (cardObj.GetComponent<Image>().color == CardManipulation.Unselected)
                    {
                        selectedCards.Add(byte.Parse(cardObj.name));
                        cardObj.GetComponent<Image>().color = CardManipulation.Selected;
                    }
                    else if (cardObj.GetComponent<Image>().color == CardManipulation.Selected)
                    {
                        selectedCards.Remove(byte.Parse(cardObj.name));
                        cardObj.GetComponent<Image>().color = CardManipulation.Unselected;

                        if (selectedCards.Count < 1)
                        {
                            RenderGalleryCardSelectedCancel();
                            selectingMode = SelectingMode.Gallery;
                            RenderCorrectButtons(SelectingMode.Gallery);
                        }

                    }


                }

            });

            string query = "Cards/" + ((int)cards[i].type).ToString() + "/" + cards[i].art + "-01";
            Sprite[] sprites = Resources.LoadAll<Sprite>(query);
            cardObj.GetComponent<Image>().sprite = sprites[0];
        }

        RenderCorrectButtons(SelectingMode.Gallery);
    }

    public void AdjustGalleryViewSize()
    {
        GalleryView.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<GridLayoutGroup>().cellSize =
            new Vector2(Screen.width / 8, Screen.width / 6);
    }

    public void OnGallerySelectExit()
    {
        selectedCards = new List<byte>();
        selectingMode = SelectingMode.None;

        RenderCorrectButtons(SelectingMode.Gallery);

        var children = new List<GameObject>();
        foreach (Transform child in GalleryContent.transform) children.Add(child.gameObject);

        foreach (GameObject child in children)
        {
            child.GetComponent<Image>().color = CardManipulation.Normal;
        }
    }

    public void OnGalleryViewExit()
    {
        if (viewingMode == SelectingMode.Deck)
        {
            OnShuffle();
        }
        else if (viewingMode == SelectingMode.DeckSection && howMany > 0)
        {
            shuffleDeckDialogue.ShuffleDialogue.SetActive(true);

        }

        if (selectingMode != SelectingMode.SelectingStartingPokemon)
        {
            selectedCards = new List<byte>();
            selectingMode = SelectingMode.None;
            RenderCorrectButtons(SelectingMode.None);
        }
        else
        {
            RenderCorrectButtons(SelectingMode.SelectingStartingPokemon);
        }

        GalleryView.SetActive(false);
        viewingMode = SelectingMode.None;

        if (MultiviewIndex != -1)
        {
            MultiviewIndex = -1;

            PlayerScript localScript = NetworkManager.Singleton.ConnectedClients[NetworkManager.Singleton.LocalClientId].PlayerObject.GetComponent<PlayerScript>();

            int cardsDrawn = MultiviewFinalIndex + 1 - localScript.mulligans.Length;
            if (cardsDrawn < 1)
            {
                howMany = 0;
                localScript.GameAction(PlayerScript.Action.DrawMulligan);
            }
            else
            {
                howMany = cardsDrawn;
                actionQueue = PlayerScript.Action.DrawMulligan;
                howManyCountObj.GetComponent<Text>().text = howMany.ToString();
                howManyObj.SetActive(true);
            }
        }
    }

    public void OnGalleryReload()
    {
        selectingMode = SelectingMode.None;
        RenderCorrectButtons(SelectingMode.None);

        //if (howMany > 0)
        //{
        OnTopOfDeckView();
        //}
    }

    private void RenderGalleryCardSelected()
    {
        var children = new List<GameObject>();
        foreach (Transform child in GalleryContent.transform) children.Add(child.gameObject);

        for (int i = 0; i < children.Count; i++)
        {
            children[i].GetComponent<Image>().color = CardManipulation.Unselected;
        }
    }

    public void RenderGalleryCardSelectedCancel()
    {
        var children = new List<GameObject>();
        foreach (Transform child in GalleryContent.transform) children.Add(child.gameObject);

        for (int i = 0; i < children.Count; i++)
        {
            children[i].GetComponent<Image>().color = CardManipulation.Normal;
        }
    }



    [SerializeField] private GameObject UserInfoButtons;
    [SerializeField] private GameObject OppInfoButtons;

    [SerializeField] private GameObject infoPanel;

    [SerializeField] private Text infoDeckSize;
    [SerializeField] private Text infoDiscardSize;
    [SerializeField] private Text infoHandSize;
    private readonly string deckSizeString = "Cards in Deck: ";
    private readonly string discardSizeString = "Cards in Discard: ";
    private readonly string handSizeString = "Cards in Hand: ";

    // this input is simply 0 for user and 1 for opponent
    public void OnInfo(int playerId)
    {
        infoPanel.SetActive(true);
        if (playerId == 0)
        {
            UserInfoButtons.SetActive(true);
            OppInfoButtons.SetActive(false);
            foreach (GameObject client in PlayerInfoManager.players)
            {
                if (client.GetComponent<PlayerScript>().IsLocalPlayer)
                {
                    infoDeckSize.text = deckSizeString + client.GetComponent<PlayerScript>().Deck.Value.Length.ToString();
                    infoDiscardSize.text = discardSizeString + client.GetComponent<PlayerScript>().Discard.Value.Length.ToString();
                    infoHandSize.text = handSizeString + client.GetComponent<PlayerScript>().Hand.Value.Length.ToString();
                    break;
                }
            }
        }
        else if (playerId == 1)
        {
            UserInfoButtons.SetActive(false);
            OppInfoButtons.SetActive(true);
            foreach (GameObject client in PlayerInfoManager.players)
            {
                if (!client.GetComponent<PlayerScript>().IsLocalPlayer)
                {
                    infoDeckSize.text = deckSizeString + client.GetComponent<PlayerScript>().deckSize.Value.ToString();
                    infoDiscardSize.text = discardSizeString + client.GetComponent<PlayerScript>().Discard.Value.Length.ToString();
                    infoHandSize.text = handSizeString + client.GetComponent<PlayerScript>().handSize.Value.ToString();
                    break;
                }
            }
        }

    }

    public void OnInfoExit()
    {
        infoPanel.SetActive(false);
    }




    public static PlayerScript.Action actionQueue;
    public static int howMany = 0;
    public GameObject howManyObj;
    public Text howManyCountObj;

    public void OnHowManyConfirm()
    {
        howManyObj.SetActive(false);

        if (howMany < 1)
        {
            return;
        }

        switch (actionQueue)
        {
            case PlayerScript.Action.ViewTopOfDeck:
                OnTopOfDeckView();
                break;
            case PlayerScript.Action.Mill:
                NetworkManager.Singleton.ConnectedClients.TryGetValue(NetworkManager.Singleton.LocalClientId, out var client);
                client.PlayerObject.GetComponent<PlayerScript>().GameAction(PlayerScript.Action.Mill);
                break;
            case PlayerScript.Action.RevealTopOfDeck:
                NetworkManager.Singleton.ConnectedClients.TryGetValue(NetworkManager.Singleton.LocalClientId,
                    out var RevealTopOfDeck_client);
                PlayerScript playerScript = RevealTopOfDeck_client.PlayerObject.GetComponent<PlayerScript>();

                Card[] topOfDeck = new Card[howMany];
                for (int i = 0; i < howMany; i++)
                {
                    topOfDeck[i] = playerScript.Deck.Value[i];
                }

                playerScript.RevealHandServerRpc(NetworkManager.Singleton.LocalClientId, topOfDeck);
                break;
            case PlayerScript.Action.AllowEditTopOfDeck:
                NetworkManager.Singleton.ConnectedClients.TryGetValue(NetworkManager.Singleton.LocalClientId,
                    out var AllowEditTopOfDeck_client);
                PlayerScript AllowEdit_playerScript = AllowEditTopOfDeck_client.PlayerObject.GetComponent<PlayerScript>();

                Card[] AllowEdit_topOfDeck = new Card[howMany];
                for (int i = 0; i < howMany; i++)
                {
                    AllowEdit_topOfDeck[i] = AllowEdit_playerScript.Deck.Value[i];
                }

                AllowEdit_playerScript.AllowEditDeckServerRpc(NetworkManager.Singleton.LocalClientId, AllowEdit_topOfDeck);
                break;
            case PlayerScript.Action.Draw:
                NetworkManager.Singleton.ConnectedClients.TryGetValue(NetworkManager.Singleton.LocalClientId,
                    out var Draw_client);

                Draw_client.PlayerObject.GetComponent<PlayerScript>().GameAction(PlayerScript.Action.Draw);

                break;
            case PlayerScript.Action.DrawMulligan:
                NetworkManager.Singleton.ConnectedClients.TryGetValue(NetworkManager.Singleton.LocalClientId,
                    out var DrawMulligan_client);

                DrawMulligan_client.PlayerObject.GetComponent<PlayerScript>().GameAction(PlayerScript.Action.DrawMulligan);
                break;
            case PlayerScript.Action.TakePrize:
                NetworkManager.Singleton.ConnectedClients.TryGetValue(NetworkManager.Singleton.LocalClientId,
                    out var TakePrize_client);

                TakePrize_client.PlayerObject.GetComponent<PlayerScript>().GameAction(PlayerScript.Action.TakePrize);

                break;

            default:
                break;
        }

    }

    public void OnHowManyCancel()
    {
        if (actionQueue == PlayerScript.Action.DrawMulligan)
        {
            howMany = 0;
            NetworkManager.Singleton.ConnectedClients.TryGetValue(NetworkManager.Singleton.LocalClientId,
                out var client);

            client.PlayerObject.GetComponent<PlayerScript>().GameAction(PlayerScript.Action.DrawMulligan);
        }


        howManyObj.SetActive(false);
    }

    public void OnHowManyPlus()
    {
        howMany++;
        howManyCountObj.GetComponent<Text>().text = howMany.ToString();
    }

    public void OnHowManyMinus()
    {
        howMany--;
        howManyCountObj.GetComponent<Text>().text = howMany.ToString();
    }

}
