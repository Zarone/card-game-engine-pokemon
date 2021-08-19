using System.Collections;
using System.Collections.Generic;
using MLAPI;
using MLAPI.NetworkVariable;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using MLAPI.Messaging;
using System.Linq;
using CardInformation;

public class PlayerScript : NetworkBehaviour
{
    public enum Action
    {
        Setup,
        Draw,
        Discard,
        LostZone,
        ShuffleIntoDeck,
        Bench,
        Active,
        ToHand,
        AttachStart,
        AttachConfirm,
        LevelUpStart,
        LevelDown,
        AddCounter,
        ViewTopOfDeck,
        Mill,
        ToBottomOfDeck,
        ToTopOfDeck,
        ToTopOfDeckFromSection,
        RevealTopOfDeck,
        AllowEditTopOfDeck,
        TakePrize,
        Mulligan,
        DrawMulligan,
        PlaySupporter,
        MoveToPrizes
    }

    [System.NonSerialized]
    public NetworkVariable<int> handSize = new NetworkVariable<int>(
        new NetworkVariableSettings
        {
            WritePermission = NetworkVariablePermission.OwnerOnly,
            ReadPermission = NetworkVariablePermission.Everyone
        },
        0
    );

    [System.NonSerialized]
    public NetworkVariable<int> deckSize = new NetworkVariable<int>(
        new NetworkVariableSettings
        {
            WritePermission = NetworkVariablePermission.OwnerOnly,
            ReadPermission = NetworkVariablePermission.Everyone
        },
        0
    );

    [System.NonSerialized]
    public NetworkVariable<Card[]> SpecialDeck = new NetworkVariable<Card[]>(
        new NetworkVariableSettings
        {
            WritePermission = NetworkVariablePermission.OwnerOnly,
            ReadPermission = NetworkVariablePermission.Everyone
        },
        new Card[0]
    );

    [System.NonSerialized]
    public NetworkVariable<Card[]> Discard = new NetworkVariable<Card[]>(
        new NetworkVariableSettings
        {
            WritePermission = NetworkVariablePermission.OwnerOnly,
            ReadPermission = NetworkVariablePermission.Everyone
        },
        new Card[0]
    );

    [System.NonSerialized]
    public NetworkVariable<Card[]> LostZone = new NetworkVariable<Card[]>(
        new NetworkVariableSettings
        {
            WritePermission = NetworkVariablePermission.OwnerOnly,
            ReadPermission = NetworkVariablePermission.Everyone
        },
        new Card[0]
    );

    [System.NonSerialized]
    public NetworkVariable<Card> SupporterCard = new NetworkVariable<Card>(
        new NetworkVariableSettings
        {
            WritePermission = NetworkVariablePermission.OwnerOnly,
            ReadPermission = NetworkVariablePermission.Everyone
        },
        null
    );

    [System.NonSerialized]
    public NetworkVariable<bool> isActivePlayer = new NetworkVariable<bool>(
        new NetworkVariableSettings
        {
            WritePermission = NetworkVariablePermission.OwnerOnly,
            ReadPermission = NetworkVariablePermission.Everyone
        },
        false
    );

    [System.NonSerialized]
    public NetworkVariable<int> PrizesRemaining = new NetworkVariable<int>(
        new NetworkVariableSettings
        {
            WritePermission = NetworkVariablePermission.OwnerOnly,
            ReadPermission = NetworkVariablePermission.Everyone
        },
        0
    );

    [System.NonSerialized]
    public NetworkVariable<bool> IsReadyForRematch = new NetworkVariable<bool>(
        new NetworkVariableSettings
        {
            WritePermission = NetworkVariablePermission.OwnerOnly,
            ReadPermission = NetworkVariablePermission.Everyone
        },
        false
    );

    //[System.NonSerialized] private Card[] deck = new Card[0];

    [System.NonSerialized] public LocalDeck Deck;

    //[System.NonSerialized] private Card[] hand = new Card[0];

    [System.NonSerialized] public LocalDeck Hand;

    //[System.NonSerialized] private Card[] prizes = new Card[0];

    [System.NonSerialized] public LocalDeck Prizes;


    public GameObject CardPrefab;

    public GameObject CardSpritePrefab;

    public GameObject AttachmentOverflowPrefab;

    [System.NonSerialized] public GameStateManager gameManagerReference = null;
    [System.NonSerialized] public GameObject PlayerHand;

    [System.NonSerialized] public bool HasStarted = false;

    [System.NonSerialized] public bool isInAnimation = false;
    [System.NonSerialized] public GameObject animTempSprite;
    [System.NonSerialized] public Transform animTempTarget;
    [System.NonSerialized] public System.Action animCallback;

    private int FirstTurnInfo = -3;
    private bool AutoDraw = false;
    private bool AutoUntap = false;

    private bool CoinFlipWinnerDecidesTurnOrder;
    private bool TurnOrderDeterminedAfterGameSetup;
    private bool FirstPlayerDraws;

    public Card[][] mulligans = new Card[0][];

    [System.NonSerialized] public GameObject PrizeLabel;
    [System.NonSerialized] public GameObject PrizeObj;

    [System.NonSerialized] public GameObject SupporterObj;

    [System.NonSerialized] public CardSection cardSection;

    public void Start()
    {

        Deck = new LocalDeck(new Card[0], () =>
        {
            deckSize.Value = Deck.Value.Length;
            gameManagerReference.RenderDeck(IsLocalPlayer, deckSize.Value);
        });
        Hand = new LocalDeck(new Card[0], () =>
        {
            handSize.Value = Hand.Value.Length;
            RenderHand();
        });
        Prizes = new LocalDeck(new Card[0], () =>
        {
            PrizesRemaining.Value = Prizes.Value.Length;
            RenderPrizes();
        });

        if (!IsLocalPlayer)
        {
            handSize.OnValueChanged += (int oldValue, int newValue) =>
            {
                RenderHand();
            };

            deckSize.OnValueChanged += (int oldValue, int newValue) =>
            {
                if (gameManagerReference != null)
                {
                    gameManagerReference.RenderDeck(false, newValue);
                }
            };

            PrizesRemaining.OnValueChanged += (int oldValue, int newValue) =>
            {
                RenderPrizes();
            };
        }

        Discard.OnValueChanged += (Card[] previousValue, Card[] newValue) =>
        {
            RenderDiscard();
        };

        LostZone.OnValueChanged += (Card[] previousValue, Card[] newValue) =>
        {
            RenderRemoveFromPlay();
        };

        SupporterCard.OnValueChanged += (Card previousValue, Card newValue) =>
        {
            RenderSupporterCard();
        };

        PlayerInfoManager.players.Add(gameObject);

        cardSection = gameObject.GetComponent<CardSection>();
    }

    public void RunFirst()
    {
        gameManagerReference = GameObject.Find("GameStateManager").GetComponent<GameStateManager>();
        if (CardPrefab != null)
        {
            if (IsLocalPlayer)
            {
                PlayerHand = gameManagerReference.playerHand;
            }
            else
            {
                PlayerHand = gameManagerReference.oppHand;
            }
        }
        else
        {
            Debug.LogError("no card prefab found");
        }

        if (IsLocalPlayer)
        {
            GameAction(Action.Setup, CardManipulation.Shuffle(PlayerInfoManager.fullDeck));
        }
        HasStarted = true;

        if (TurnOrderDeterminedAfterGameSetup)
        {
            gameManagerReference.coinManager.BlockView.SetActive(false);
        }
    }

    void FormatHandSpacing(int extra = 0, bool isLocal = false)
    {
        int cardsInHand = (isLocal ? Hand.Value.Length : handSize.Value) + extra;
        float cardSize = 67.5f;
        int handRenderSize = System.Math.Min(isLocal ? 800 : 600, cardsInHand * 75);
        PlayerHand.GetComponent<HorizontalLayoutGroup>().spacing = cardsInHand != 1 ? (-(cardSize * cardsInHand - handRenderSize) / (cardsInHand - 1)) : 0;
    }

    public void RenderHand()
    {
        if (SceneManager.GetActiveScene().name != "GameScreen") return;
        if (isInAnimation)
        {
            Invoke(nameof(RenderHand), 0.1f);
            return;
        }

        if (PlayerHand != null)
        {
            var children = new List<GameObject>();
            foreach (Transform child in PlayerHand.transform) children.Add(child.gameObject);
            children.ForEach(child => Destroy(child));

            if (IsLocalPlayer)
            {

                for (int i = 0; i < Hand.Value.Length; i++)
                {
                    if (Hand.Value[i] != null && Hand.Value[i].art != null)
                    {
                        GameObject EditingCard = Instantiate(CardPrefab, PlayerHand.transform);
                        EditingCard.GetComponent<RectTransform>().sizeDelta = new Vector2(67.5f, 109.5076f);

                        EditingCard.GetComponent<CardRightClickHandler>().onRightClick = (Sprite image) =>
                         {
                             gameManagerReference.OnCardRightClick(image);

                             // I could reuse this for BREAK cards
                             //gameManagerReference.CardCloseupCard.transform.localRotation = Quaternion.Euler(0, 0, 0);
                         };

                        EditingCard.name = i.ToString();
                        EditingCard.GetComponent<Button>().onClick.AddListener(() =>
                        {
                            if (GameStateManager.selectingMode == GameStateManager.SelectingMode.None)
                            {
                                RenderHandSelecting();

                                Card localCard = Hand.Value[int.Parse(EditingCard.name)];

                                if (Hand.Value[byte.Parse(EditingCard.name)].type == CardInformation.CardType.Energy)
                                {
                                    foreach (GameObject playerClient in PlayerInfoManager.players)
                                    {
                                        foreach (Transform child in playerClient.GetComponent<PlayerScript>().cardSection.BenchObj.transform)
                                        {
                                            child.gameObject.transform.GetChild(0).GetComponent<Image>().color = CardManipulation.PossibleMoveTo;
                                        }

                                        foreach (Transform child in playerClient.GetComponent<PlayerScript>().cardSection.ActiveObj.transform)
                                        {
                                            child.gameObject.transform.GetChild(0).GetComponent<Image>().color = CardManipulation.PossibleMoveTo;
                                        }
                                    }

                                    GameStateManager.selectingMode = GameStateManager.SelectingMode.Attaching;
                                    gameManagerReference.RenderCorrectButtons(GameStateManager.SelectingMode.Attaching);
                                }
                                else
                                {
                                    GameStateManager.selectingMode = GameStateManager.SelectingMode.Hand;
                                    gameManagerReference.RenderCorrectButtons(GameStateManager.SelectingMode.Hand);
                                }

                            }

                            if (GameStateManager.selectingMode == GameStateManager.SelectingMode.Hand ||
                                GameStateManager.selectingMode == GameStateManager.SelectingMode.Attaching)
                            {
                                if (EditingCard.transform.GetChild(0).GetComponent<Image>().color == CardManipulation.Unselected)
                                {
                                    gameManagerReference.selectedCards.Add(byte.Parse(EditingCard.name));
                                    EditingCard.transform.GetChild(0).GetComponent<Image>().color = CardManipulation.Selected;
                                }
                                else if (EditingCard.transform.GetChild(0).GetComponent<Image>().color == CardManipulation.Selected)
                                {
                                    gameManagerReference.selectedCards.Remove(byte.Parse(EditingCard.name));
                                    if (gameManagerReference.selectedCards.Count < 1)
                                    {

                                        if (GameStateManager.selectingMode == GameStateManager.SelectingMode.Attaching)
                                        {

                                            foreach (GameObject client in PlayerInfoManager.players)
                                            {
                                                CardSection playerCardSection = client.GetComponent<PlayerScript>().cardSection;
                                                foreach (Transform child in playerCardSection.BenchObj.transform)
                                                {
                                                    child.gameObject.transform.GetChild(0).GetComponent<Image>().color = CardManipulation.Normal;
                                                }
                                                foreach (Transform child in playerCardSection.ActiveObj.transform)
                                                {
                                                    child.gameObject.transform.GetChild(0).GetComponent<Image>().color = CardManipulation.Normal;
                                                }
                                            }

                                        }

                                        RenderHandSelectingCancel();
                                        gameManagerReference.RenderCorrectButtons(GameStateManager.SelectingMode.None);
                                    }
                                    else
                                    {
                                        EditingCard.transform.GetChild(0).GetComponent<Image>().color = CardManipulation.Unselected;
                                    }
                                }
                            }
                            else if (GameStateManager.selectingMode == GameStateManager.SelectingMode.SelectingStartingPokemon)
                            {
                                gameManagerReference.selectedCards = new List<byte>() { byte.Parse(EditingCard.name) };
                                GameAction(Action.Active);
                                GameStateManager.selectingMode = GameStateManager.SelectingMode.None;

                                foreach (GameObject player in PlayerInfoManager.players)
                                {
                                    PlayerScript tempPlayerScript = player.GetComponent<PlayerScript>();
                                    if (!tempPlayerScript.IsLocalPlayer)
                                    {
                                        if (tempPlayerScript.cardSection.Active.Value.Length > 0)
                                        {
                                            RequestShareMulliganInfoServerRpc(NetworkManager.Singleton.LocalClientId, 0);
                                            if (mulligans.Length > 0)
                                            {
                                                ShareMulliganInfoServerRpc(NetworkManager.Singleton.LocalClientId, mulligans[0], 0, mulligans.Length - 1);
                                            }
                                            else
                                            {
                                                ShareMulliganInfoServerRpc(NetworkManager.Singleton.LocalClientId, null, 0, -1);
                                            }

                                        }
                                        break;
                                    }
                                }

                            }
                        });

                        string query = Hand.Value[i].art;
                        Sprite[] sprites = Resources.LoadAll<Sprite>(query);
                        if (sprites.Length == 1)
                        {
                            EditingCard.transform.GetChild(0).GetComponent<Image>().sprite = sprites[0];
                        }
                        else
                        {
                            Debug.LogError($"{query} returned {sprites.Length} results");
                        }

                        if (GameStateManager.selectingMode == GameStateManager.SelectingMode.SelectingStartingPokemon && Hand.Value[i].type == CardType.Pokemon)
                        {
                            EditingCard.transform.GetChild(0).GetComponent<Image>().color = CardManipulation.PossibleMoveTo;
                        }
                    }
                }


                if (gameManagerReference.selectedCards.Count == 0 && GameStateManager.selectingMode != GameStateManager.SelectingMode.SelectingStartingPokemon)
                {
                    GameStateManager.selectingMode = GameStateManager.SelectingMode.None;
                    gameManagerReference.RenderCorrectButtons(GameStateManager.SelectingMode.None);
                    gameManagerReference.selectedCards = new List<byte>();
                }
            }
            else
            {
                for (int i = 0; i < handSize.Value; i++)
                {
                    GameObject EditingCard = Instantiate(CardPrefab, PlayerHand.transform);
                    EditingCard.GetComponent<RectTransform>().sizeDelta = new Vector2(67.5f, 109.5076f);
                    string query = CardManipulation.DefaultCard;
                    Sprite[] sprites = Resources.LoadAll<Sprite>(query);
                    if (sprites.Length == 1)
                    {
                        EditingCard.transform.GetChild(0).GetComponent<Image>().sprite = sprites[0];
                    }
                    else
                    {
                        Debug.LogError($"{query} returned {sprites.Length} results");
                    }
                }
            }

            FormatHandSpacing(0, IsLocalPlayer);


        }
        else
        {
            RunFirst();
            if (PlayerHand == null)
            {
                Debug.LogError("player hand still not loaded");
            }
            else
            {
                RenderHand();
            }
        }
    }

    public void RenderDiscard()
    {
        string query;
        if (Discard.Value.Length > 0)
        {
            query = Discard.Value[Discard.Value.Length - 1].art;
        }
        else
        {
            query = CardManipulation.DefaultCard;
        }
        Sprite[] sprites = Resources.LoadAll<Sprite>(query);
        if (IsLocalPlayer)
        {
            gameManagerReference.playerDiscardSprite.GetComponent<CardRightClickHandler>().onRightClick = gameManagerReference.OnCardRightClick;
            gameManagerReference.playerDiscardSprite.transform.GetChild(0).GetComponent<Image>().sprite = sprites[0];
        }
        else
        {
            gameManagerReference.oppDiscardSprite.GetComponent<CardRightClickHandler>().onRightClick = gameManagerReference.OnCardRightClick;
            gameManagerReference.oppDiscardSprite.transform.GetChild(0).GetComponent<Image>().sprite = sprites[0];
        }
    }

    public void RenderRemoveFromPlay()
    {
        string query;
        if (LostZone.Value.Length > 0)
        {
            query = LostZone.Value[LostZone.Value.Length - 1].art;
        }
        else
        {
            query = CardManipulation.DefaultCard;
        }
        Sprite[] sprites = Resources.LoadAll<Sprite>(query);
        if (IsLocalPlayer)
        {
            gameManagerReference.playerLostZoneSprite.GetComponent<CardRightClickHandler>().onRightClick = gameManagerReference.OnCardRightClick;
            gameManagerReference.playerLostZoneSprite.transform.GetChild(0).GetComponent<Image>().sprite = sprites[0];
        }
        else
        {
            gameManagerReference.oppLostZoneSprite.GetComponent<CardRightClickHandler>().onRightClick = gameManagerReference.OnCardRightClick;
            gameManagerReference.oppLostZoneSprite.transform.GetChild(0).GetComponent<Image>().sprite = sprites[0];
        }
    }

    private void RenderPrizes()
    {
        PrizeLabel.GetComponent<Text>().text = PrizesRemaining.Value.ToString();
    }

    private void RenderSupporterCard()
    {
        if (SupporterCard.Value != null)
        {
            string query = SupporterCard.Value.art;
            Sprite[] sprites = Resources.LoadAll<Sprite>(query);
            if (sprites.Length == 1)
            {
                SupporterObj.transform.GetChild(0).GetComponent<Image>().color = Color.white;
                SupporterObj.transform.GetChild(0).GetComponent<Image>().sprite = sprites[0];
            }
            else
            {
                Debug.LogError($"{query} returned {sprites.Length} results");
            }
            SupporterObj.GetComponent<CardRightClickHandler>().onRightClick = gameManagerReference.OnCardRightClick;
        }
        else
        {
            SupporterObj.transform.GetChild(0).GetComponent<Image>().color = Color.clear;
        }
    }

    private void RenderStadium()
    {
        if (gameManagerReference.CurrentStadium != null)
        {
            string query = gameManagerReference.CurrentStadium.art;
            Sprite[] sprites = Resources.LoadAll<Sprite>(query);
            if (sprites.Length == 1)
            {
                gameManagerReference.StadiumObj.transform.GetChild(0).GetComponent<Image>().color = Color.white;
                gameManagerReference.StadiumObj.transform.GetChild(0).GetComponent<Image>().sprite = sprites[0];
                gameManagerReference.StadiumObj.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0, 0,
                    gameManagerReference.StadiumFacingSelf ? 180 : 0);

            }
            else
            {
                Debug.LogError($"{query} returned {sprites.Length} results");
            }
            gameManagerReference.StadiumObj.GetComponent<CardRightClickHandler>().onRightClick = gameManagerReference.OnCardRightClick;
        }
        else
        {
            gameManagerReference.StadiumObj.transform.GetChild(0).GetComponent<Image>().color = Color.clear;
        }
    }


    public void RenderHandSelecting()
    {
        foreach (Transform child in PlayerHand.transform)
        {
            child.gameObject.transform.GetChild(0).GetComponent<Image>().color = CardManipulation.Unselected;
        }
    }

    public void RenderHandSelectingCancel()
    {
        gameManagerReference.selectedCards = new List<byte>();
        GameStateManager.selectingMode = GameStateManager.SelectingMode.None;

        foreach (Transform child in PlayerHand.transform)
        {
            child.gameObject.transform.GetChild(0).GetComponent<Image>().color = CardManipulation.Normal;
        }
    }

    public void RenderTurnInfo()
    {
        if (!HasStarted)
        {
            Invoke(nameof(RenderTurnInfo), 0.5f);
            return;
        }

        gameManagerReference.TurnColor.GetComponent<Image>().color = isActivePlayer.Value
            ? new Color(0, 1, 0, 0.2f) : new Color(1, 0, 0, 0.2f);
        gameManagerReference.TurnText.GetComponent<Text>().text = isActivePlayer.Value ? "Your turn" : "Opponent's turn";
        gameManagerReference.TurnButton.GetComponent<Button>().interactable = isActivePlayer.Value;
    }



    public LocalDeck ModeToLocalDeck(GameStateManager.SelectingMode mode)
    {
        return mode switch
        {
            GameStateManager.SelectingMode.Deck => Deck,
            GameStateManager.SelectingMode.DeckSection => Deck,
            GameStateManager.SelectingMode.Hand => Hand,
            GameStateManager.SelectingMode.Attaching => Hand,
            GameStateManager.SelectingMode.SelectingStartingPokemon => Hand,
            GameStateManager.SelectingMode.Prizes => Prizes,
            _ => null,
        };
    }

    public NetworkVariable<Card[]> ModeToNetworkDeck(GameStateManager.SelectingMode mode)
    {
        return mode switch
        {
            GameStateManager.SelectingMode.Active => cardSection.Active,
            GameStateManager.SelectingMode.Bench => cardSection.Bench,
            GameStateManager.SelectingMode.OppActive => cardSection.Active,
            GameStateManager.SelectingMode.OppBench => cardSection.Bench,
            GameStateManager.SelectingMode.Discard => Discard,
            GameStateManager.SelectingMode.LostZone => LostZone,
            _ => null,
        };
    }

    private NetworkVariable<Card[][]> ModeToNetworkDeck2D(GameStateManager.SelectingMode mode)
    {
        return mode switch
        {
            GameStateManager.SelectingMode.AttachedBench => cardSection.BenchAttachments,
            GameStateManager.SelectingMode.AttachedActive => cardSection.ActiveAttachments,
            _ => null,
        };
    }

    public bool IsLocal(GameStateManager.SelectingMode mode)
    {
        return mode switch
        {
            GameStateManager.SelectingMode.Deck => true,
            GameStateManager.SelectingMode.DeckSection => true,
            GameStateManager.SelectingMode.Hand => true,
            GameStateManager.SelectingMode.Attaching => true,
            GameStateManager.SelectingMode.SelectingStartingPokemon => true,
            GameStateManager.SelectingMode.Prizes => true,
            _ => false,
        };
    }

    private GameObject ModeToGameObject(GameStateManager.SelectingMode mode)
    {
        return mode switch
        {
            GameStateManager.SelectingMode.Deck => gameManagerReference.playerDeckSprite,
            GameStateManager.SelectingMode.DeckSection => gameManagerReference.playerDeckSprite,
            GameStateManager.SelectingMode.Discard => gameManagerReference.playerDiscardSprite,
            GameStateManager.SelectingMode.LostZone => gameManagerReference.playerLostZoneSprite,
            GameStateManager.SelectingMode.Hand => gameManagerReference.playerHand,
            GameStateManager.SelectingMode.Attaching => gameManagerReference.playerHand,
            GameStateManager.SelectingMode.Bench => cardSection.BenchObj,
            GameStateManager.SelectingMode.Active => cardSection.ActiveObj,
            GameStateManager.SelectingMode.AttachedBench => cardSection.BenchObj,
            GameStateManager.SelectingMode.AttachedActive => cardSection.ActiveObj,
            GameStateManager.SelectingMode.Prizes => PrizeObj,
            _ => null,
        };
    }

    public NetworkVariable<Card[][]> CheckAttachments(GameStateManager.SelectingMode mode)
    {
        return mode switch
        {
            GameStateManager.SelectingMode.Active => cardSection.ActiveAttachments,
            GameStateManager.SelectingMode.Bench => cardSection.BenchAttachments,
            GameStateManager.SelectingMode.OppActive => cardSection.ActiveAttachments,
            GameStateManager.SelectingMode.OppBench => cardSection.BenchAttachments,
            _ => null,
        };
    }

    private bool IsAttachment(GameStateManager.SelectingMode mode)
    {
        return mode switch
        {
            GameStateManager.SelectingMode.AttachedBench => true,
            GameStateManager.SelectingMode.AttachedActive => true,
            _ => false
        };
    }

    private NetworkVariable<bool[][]> GetState(GameStateManager.SelectingMode mode)
    {
        return mode switch
        {
            GameStateManager.SelectingMode.Active => cardSection.ActiveCardStates,
            GameStateManager.SelectingMode.OppActive => cardSection.ActiveCardStates,
            //GameStateManager.SelectingMode.Bench => cardSection.BenchCardStates,
            _ => null
        };
    }

    public NetworkVariable<Card[][]> GetLevel(GameStateManager.SelectingMode mode)
    {
        return mode switch
        {
            GameStateManager.SelectingMode.Bench => cardSection.BenchCardOldEvolutions,
            GameStateManager.SelectingMode.Active => cardSection.ActiveCardOldEvolutions,
            GameStateManager.SelectingMode.OppBench => cardSection.BenchCardOldEvolutions,
            GameStateManager.SelectingMode.OppActive => cardSection.ActiveCardOldEvolutions,
            _ => null
        };
    }

    private NetworkVariable<int[]> GetCounter(GameStateManager.SelectingMode mode)
    {
        return mode switch
        {
            GameStateManager.SelectingMode.Bench => cardSection.BenchCounters,
            GameStateManager.SelectingMode.Active => cardSection.ActiveCounters,
            _ => null
        };
    }

    private string GetName(GameStateManager.SelectingMode mode)
    {
        return mode switch
        {
            GameStateManager.SelectingMode.Active => "active",
            GameStateManager.SelectingMode.Bench => "bench",
            GameStateManager.SelectingMode.Deck => "deck",
            GameStateManager.SelectingMode.Discard => "discard",
            GameStateManager.SelectingMode.Hand => "hand",
            GameStateManager.SelectingMode.LostZone => "lost zone",
            GameStateManager.SelectingMode.Prizes => "prizes",
            GameStateManager.SelectingMode.Stadium => "stadium zone",
            GameStateManager.SelectingMode.Supporter => "supporter zone",
            GameStateManager.SelectingMode.AttachedActive => "active attachment",
            GameStateManager.SelectingMode.AttachedBench => "bench attachment",
            _ => "unnamed location"
        };
    }

    public int GetNumberOfAttachedCards(GameStateManager.SelectingMode mode)
    {
        NetworkVariable<Card[][]> attachedCards = CheckAttachments(mode);
        if (attachedCards == null) return 0;

        int total = 0;

        for (byte i = 0; i < attachedCards.Value.Length; i++)
        {
            if (gameManagerReference.selectedCards.Contains(i))
            {
                total += attachedCards.Value[i].Length;
            }
        }

        return total;
    }

    public int GetNumberOfOldLevels(GameStateManager.SelectingMode mode)
    {
        NetworkVariable<Card[][]> attachedCards = GetLevel(mode);
        if (attachedCards == null) return 0;

        int total = 0;

        for (byte i = 0; i < attachedCards.Value.Length; i++)
        {
            if (gameManagerReference.selectedCards.Contains(i))
            {
                total += attachedCards.Value[i].Length;
            }
        }

        return total;
    }

    public static GameStateManager.SelectingMode ToOpponentVersion(GameStateManager.SelectingMode mode)
    {
        return mode switch
        {
            GameStateManager.SelectingMode.Bench => GameStateManager.SelectingMode.OppBench,
            GameStateManager.SelectingMode.Active => GameStateManager.SelectingMode.OppActive,
            _ => GameStateManager.SelectingMode.None
        };
    }

    public void FromXToY(bool isNetworkX, bool isNetworkY, List<byte> selectedIndexes,
        GameObject xObj = null, GameObject yObj = null, bool shuffleOutput = false, bool toTop = false,
        NetworkVariable<Card[]> NetworkX = null, NetworkVariable<Card[]> NetworkY = null,
        LocalDeck localX = null, LocalDeck localY = null,
        NetworkVariable<Card[][]> attachmentsX = null, NetworkVariable<Card[][]> attachmentsY = null,
        NetworkVariable<bool[][]> gameStateX = null, NetworkVariable<bool[][]> gameStateY = null,
        NetworkVariable<Card[][]> levelsX = null, NetworkVariable<Card[][]> levelsY = null,
        NetworkVariable<int[]> countersX = null, NetworkVariable<int[]> countersY = null,
        int numberOfAttachedCardsOnSelectedCards = 0, int numberOfOldCardsOnSelectedCards = 0,
        System.Action additionalCallback = null)
    {
        if (isInAnimation)
        {
            return;
        };
        isInAnimation = true;

        if ((isNetworkX ? NetworkX.Value.Length : localX.Value.Length) - selectedIndexes.Count < 0)
        {
            Debug.LogError(isNetworkX);
            Debug.LogError(NetworkX.Value.Length);
            Debug.LogError(localX.Value.Length);
            Debug.LogError(selectedIndexes.Count);

        }

        Card[] newX = new Card[(isNetworkX ? NetworkX.Value.Length : localX.Value.Length) - selectedIndexes.Count];

        Card[] newY = new Card[(isNetworkY ? NetworkY.Value.Length : localY.Value.Length)
            + selectedIndexes.Count + numberOfAttachedCardsOnSelectedCards + numberOfOldCardsOnSelectedCards];

        Card[][] newAttachmentsX = new Card[newX.Length][];
        Card[][] newAttachmentsY = new Card[newY.Length][];

        bool[][] newGameStateX = new bool[newX.Length][];
        bool[][] newGameStateY = new bool[newY.Length][];

        Card[][] newLevelsX = new Card[newX.Length][];
        Card[][] newLevelsY = new Card[newY.Length][];

        int[] newCountersX = new int[newX.Length];
        int[] newCountersY = new int[newY.Length];

        Card lastDiscardedCard = null; // this is used only for the animation

        int YOffset = toTop ? selectedIndexes.Count + numberOfAttachedCardsOnSelectedCards + numberOfOldCardsOnSelectedCards : 0;

        for (int k = 0; k < (isNetworkY ? NetworkY.Value.Length : localY.Value.Length); k++)
        {
            newY[k + YOffset] = isNetworkY ? NetworkY.Value[k] : localY.Value[k];
            if (attachmentsY != null)
            {
                newAttachmentsY[k + YOffset] = attachmentsY.Value[k] ?? (new Card[0]);
            }
            if (gameStateY != null)
            {
                newGameStateY[k + YOffset] = new bool[5];
                newGameStateY[k + YOffset][0] = gameStateY.Value[k][0];
                newGameStateY[k + YOffset][1] = gameStateY.Value[k][1];
                newGameStateY[k + YOffset][2] = gameStateY.Value[k][2];
                newGameStateY[k + YOffset][3] = gameStateY.Value[k][3];
                newGameStateY[k + YOffset][4] = gameStateY.Value[k][4];
            }
            if (levelsY != null)
            {
                newLevelsY[k + YOffset] = levelsY.Value[k];
            }
            if (countersY != null)
            {
                newCountersY[k + YOffset] = countersY.Value[k];
            }
        }

        int i = 0; // tracks total iterations
        int j = 0; // tracks current position in newHand
        int attachedCardsOffset = 0; // this tracks cards like attachments and old evolutions that also moved

        while (i < (isNetworkX ? NetworkX.Value.Length : localX.Value.Length))
        {

            if (!selectedIndexes.Contains((byte)i)) // if card wasn't moved
            {
                newX[j] = (isNetworkX ? NetworkX.Value[i] : localX.Value[i]);
                if (attachmentsX != null)
                {
                    newAttachmentsX[j] = attachmentsX.Value[i] ?? (new Card[0]);
                }
                if (gameStateX != null)
                {
                    newGameStateX[j] = new bool[5];
                    newGameStateX[j][0] = gameStateX.Value[i][0];
                    newGameStateX[j][1] = gameStateX.Value[i][1];
                    newGameStateX[j][2] = gameStateX.Value[i][2];
                    newGameStateX[j][3] = gameStateX.Value[i][3];
                    newGameStateX[j][4] = gameStateX.Value[i][4];
                }
                if (levelsX != null)
                {
                    newLevelsX[j] = levelsX.Value[i];
                }
                if (countersX != null)
                {
                    newCountersX[j] = countersX.Value[i];
                }
                j++;
            }
            else
            {
                int offset = toTop ? 0 : (isNetworkY ? NetworkY.Value.Length : localY.Value.Length);
                int index = i - j + offset + attachedCardsOffset;

                lastDiscardedCard = (isNetworkX ? NetworkX.Value[i] : localX.Value[i]);
                newY[index] = (isNetworkX ? NetworkX.Value[i] : localX.Value[i]);

                if (attachmentsY != null && attachmentsX != null)
                {
                    newAttachmentsY[index] = attachmentsX.Value[i] ?? (new Card[0]);
                }
                else if (attachmentsY != null)
                {
                    newAttachmentsY[index] = new Card[0];
                }
                else if (attachmentsX != null)
                {
                    // move attached card to new section

                    foreach (Card attachedCard in attachmentsX.Value[i])
                    {
                        attachedCardsOffset++;
                        newY[i - j + offset + attachedCardsOffset] = attachedCard;
                    }

                }


                if (gameStateX != null && gameStateY != null)
                {
                    newGameStateY[index] = new bool[5];

                    if (gameStateX.Value.Length <= i)
                    {
                        newGameStateY[index][0] = false;
                        newGameStateY[index][1] = false;
                        newGameStateY[index][2] = false;
                        newGameStateY[index][3] = false;
                        newGameStateY[index][4] = false;
                    }
                    else
                    {
                        newGameStateY[index][0] = gameStateX.Value[i][0];
                        newGameStateY[index][1] = gameStateX.Value[i][1];
                        newGameStateY[index][2] = gameStateX.Value[i][2];
                        newGameStateY[index][3] = gameStateX.Value[i][3];
                        newGameStateY[index][4] = gameStateX.Value[i][4];
                    }

                }
                else if (gameStateY != null)
                {
                    newGameStateY[index] = new bool[5];
                    newGameStateY[index][0] = false;
                    newGameStateY[index][1] = false;
                    newGameStateY[index][2] = false;
                    newGameStateY[index][3] = false;
                    newGameStateY[index][4] = false;
                }


                if (levelsX != null && levelsY != null)
                {

                    newLevelsY[index] = levelsX.Value[i];
                }
                else if (levelsY != null)
                {
                    newLevelsY[index] = new Card[0];
                }
                else if (levelsX != null)
                {
                    // move level to new section, ex: acerola on a golisopod puts wimpod in hand

                    foreach (Card attachedCard in levelsX.Value[i])
                    {
                        attachedCardsOffset++;
                        newY[i - j + offset + attachedCardsOffset] = attachedCard;
                    }
                }

                if (countersX != null && countersY != null)
                {
                    newCountersY[index] = countersX.Value[i];
                }
                else if (countersY != null)
                {
                    newCountersY[index] = -1;
                }
            }

            i++;
        }

        if (lastDiscardedCard == null) return;

        if (attachmentsX != null) attachmentsX.Value = newAttachmentsX;

        if (gameStateX != null) gameStateX.Value = newGameStateX;

        if (levelsX != null) levelsX.Value = newLevelsX;

        if (countersX != null) countersX.Value = newCountersX;

        if (isNetworkX)
        {
            NetworkX.Value = newX;
        }
        else
        {
            localX.Value = newX;
        }


        void newCallback()
        {
            if (selectedIndexes.Count < 1)
            {
                foreach (Transform child in xObj.transform)
                {
                    child.gameObject.transform.GetChild(0).GetComponent<Image>().color = CardManipulation.Normal;
                }
            }

            if (attachmentsY != null) attachmentsY.Value = newAttachmentsY;

            if (gameStateY != null) gameStateY.Value = newGameStateY;

            if (levelsY != null) levelsY.Value = newLevelsY;

            if (countersY != null) countersY.Value = newCountersY;

            if (isNetworkY)
            {
                NetworkY.Value = shuffleOutput ? CardManipulation.Shuffle(newY) : newY;
            }
            else
            {
                localY.Value = shuffleOutput ? CardManipulation.Shuffle(newY) : newY;
            }

            if (animTempSprite != null)
            {
                Destroy(animTempSprite);
            }


            //if (GameStateManager.selectingMode != GameStateManager.SelectingMode.SelectingStartingPokemon)
            //{
            GameStateManager.selectingMode = GameStateManager.SelectingMode.None;
            gameManagerReference.selectedCards = new List<byte>();

            RenderHand();

            isInAnimation = false;

            additionalCallback?.Invoke();
        }


        if (xObj != null && yObj != null)
        {
            animTempSprite = Instantiate(CardSpritePrefab, xObj.transform);
            animTempSprite.transform.rotation = Quaternion.identity;
            string query = lastDiscardedCard.art;
            Sprite[] sprites = Resources.LoadAll<Sprite>(query);
            animTempSprite.GetComponent<SpriteRenderer>().sprite = sprites[0];
            animTempSprite.transform.localScale = new Vector3(10, 10);
            animTempTarget = yObj.transform;
            animCallback = newCallback;

            StartCoroutine(nameof(MoveSprite));
        }
        else
        {
            newCallback();
        }
    }

    public void FromXToY(LocalDeck LocalX,
        LocalDeck LocalY, List<byte> selectedIndexes, GameObject xObj = null, GameObject yObj = null,
        bool shuffleOutput = false, NetworkVariable<Card[][]> attachmentsX = null, NetworkVariable<Card[][]> attachmentsY = null,
        NetworkVariable<bool[][]> gameStateX = null, NetworkVariable<bool[][]> gameStateY = null, System.Action callback = null)
    {
        FromXToY(false, false, selectedIndexes, xObj, yObj, shuffleOutput, false, null, null, LocalX, LocalY, attachmentsX, attachmentsY,
            gameStateX, gameStateY, null, null, null, null, 0, 0, callback);
    }

    public void FromXToY(NetworkVariable<Card[][]> X, GameObject xObj, GameObject yObj, bool isLocalY,
        NetworkVariable<Card[]> NetworkY = null, LocalDeck LocalY = null, NetworkVariable<Card[][]> YAttachments = null)
    {
        if (isInAnimation)
        {
            return;
        };
        isInAnimation = true;



        Card[] newY = new Card[(isLocalY ? LocalY.Value.Length : NetworkY.Value.Length) + gameManagerReference.selectedCards.Count];
        Card[][] newYAttachments = new Card[newY.Length][];

        for (int a = 0; a < (isLocalY ? LocalY.Value.Length : NetworkY.Value.Length); a++)
        {
            newY[a] = (isLocalY ? LocalY.Value[a] : NetworkY.Value[a]);
            if (YAttachments != null)
            {
                newYAttachments[a] = YAttachments.Value[a];
            }
        }

        List<Card>[] newXList = new List<Card>[X.Value.Length];

        int i = 0; // this tracks the index along the 2D list of attached cards
        int cardsAddedToHand = 0;
        Card lastCardMovedFromXToY = null;

        for (int j = 0; j < newXList.Length; j++)
        {
            newXList[j] = new List<Card>();
            for (int k = 0; k < X.Value[j].Length; k++)
            {
                if (!gameManagerReference.selectedCards.Contains((byte)i)) // if the card isn't moved to hand
                {
                    newXList[j].Add(X.Value[j][k]);
                }
                else
                {
                    newY[cardsAddedToHand + (isLocalY ? LocalY.Value.Length : NetworkY.Value.Length)] = X.Value[j][k];

                    if (YAttachments != null)
                    {
                        newYAttachments[cardsAddedToHand + (isLocalY ? LocalY.Value.Length : NetworkY.Value.Length)] =
                            new Card[0];
                    }

                    lastCardMovedFromXToY = X.Value[j][k];
                    cardsAddedToHand++;
                }
                i++;
            }
        }

        Card[][] newX = new Card[X.Value.Length][];

        for (int l = 0; l < newXList.Length; l++)
        {
            newX[l] = newXList[l].ToArray();
        }

        if (lastCardMovedFromXToY == null) return;

        if (isLocalY)
        {
            LocalY.Value = newY;

        }
        else
        {
            NetworkY.Value = newY;
        }

        if (YAttachments != null)
        {
            YAttachments.Value = newYAttachments;

        }

        void newCallback()
        {
            if (gameManagerReference.selectedCards.Count < 1)
            {
                cardSection.RenderAttachmentSelectionSelectingCancel(xObj.transform);
            }

            if (animTempSprite != null)
            {
                Destroy(animTempSprite);
            }

            isInAnimation = false;

            GameStateManager.selectingMode = GameStateManager.SelectingMode.None;
            gameManagerReference.selectedCards = new List<byte>();

            //foreach (Transform child in yObj.transform)
            //{
            //    if (child.GetChild(0).GetComponent<Image>() != null) break;

            //    child.GetChild(0).GetComponent<Image>().color = CardManipulation.Normal;
            //}

            X.Value = newX;

            RenderHand();

            gameManagerReference.RenderCorrectButtons(GameStateManager.SelectingMode.None);

        }

        if (yObj != null && xObj != null)
        {
            animTempSprite = Instantiate(CardSpritePrefab, xObj.transform);
            string query = lastCardMovedFromXToY.art;
            Sprite[] sprites = Resources.LoadAll<Sprite>(query);
            animTempSprite.GetComponent<SpriteRenderer>().sprite = sprites[0];
            animTempSprite.transform.localScale = new Vector3(10, 10);
            animTempTarget = yObj.transform;
            animCallback = newCallback;

            StartCoroutine(nameof(MoveSprite));
        }
        else
        {
            newCallback();
        }
    }





    public void FromToWithModes(GameStateManager.SelectingMode fromMode, GameStateManager.SelectingMode toMode,
        bool shuffle = false, bool toTop = false, System.Action passedCallback = null)
    {
        if (IsAttachment(fromMode))
        {
            FromXToY(ModeToNetworkDeck2D(fromMode), ModeToGameObject(fromMode),
                ModeToGameObject(toMode), IsLocal(toMode), ModeToNetworkDeck(toMode), ModeToLocalDeck(toMode),
                CheckAttachments(toMode));
        }
        else
        {
            System.Action callback = null;
            if (fromMode == GameStateManager.SelectingMode.DeckSection)
            {
                callback = () =>
                {
                    gameManagerReference.RenderGalleryCardSelectedCancel();
                };
            }
            else if (passedCallback != null)
            {
                callback = passedCallback;
            }
            FromXToY(
                !IsLocal(fromMode), !IsLocal(toMode), gameManagerReference.selectedCards, ModeToGameObject(fromMode),
                ModeToGameObject(toMode), shuffle, toTop, ModeToNetworkDeck(fromMode),
                ModeToNetworkDeck(toMode), ModeToLocalDeck(fromMode), ModeToLocalDeck(toMode),
                CheckAttachments(fromMode), CheckAttachments(toMode), GetState(fromMode), GetState(toMode), GetLevel(fromMode),
                GetLevel(toMode), GetCounter(fromMode), GetCounter(toMode),
                CheckAttachments(toMode) == null ? GetNumberOfAttachedCards(fromMode) : 0, GetLevel(toMode) == null ? GetNumberOfOldLevels(fromMode) : 0, callback
            );


            if (fromMode == GameStateManager.SelectingMode.Deck ||
                fromMode == GameStateManager.SelectingMode.Discard ||
                fromMode == GameStateManager.SelectingMode.LostZone ||
                fromMode == GameStateManager.SelectingMode.Prizes)
            {
                gameManagerReference.OnGalleryViewExit();
            }
            else if (fromMode == GameStateManager.SelectingMode.DeckSection)
            {
                if (toMode == GameStateManager.SelectingMode.Hand || toMode == GameStateManager.SelectingMode.Discard)
                {
                    GameStateManager.howMany -= gameManagerReference.selectedCards.Count;
                }

                gameManagerReference.OnGalleryReload();
                //gameManagerReference.shuffleDeckDialogue.ShuffleDialogue.SetActive(true);
            }
            // this is important if you click on a trainer, then click to discard
            else if (fromMode == GameStateManager.SelectingMode.Attaching)
            {
                foreach (GameObject client in PlayerInfoManager.players)
                {
                    CardSection playerCardSection = client.GetComponent<PlayerScript>().cardSection;
                    foreach (Transform child in playerCardSection.BenchObj.transform)
                    {
                        child.gameObject.transform.GetChild(0).GetComponent<Image>().color = CardManipulation.Normal;
                    }
                    foreach (Transform child in playerCardSection.ActiveObj.transform)
                    {
                        child.gameObject.transform.GetChild(0).GetComponent<Image>().color = CardManipulation.Normal;
                    }
                }
            }
            //else if (fromMode == GameStateManager.SelectingMode.Hand)
            //{
            //    RenderHand();
            //}


        }

    }



    public void ToTopOfDeckFromSection()
    {
        Card[] newDeck = new Card[Deck.Value.Length];

        for (int i = 0; i < gameManagerReference.selectedCards.Count; i++)
        {
            newDeck[i] = Deck.Value[gameManagerReference.selectedCards[i]];
        }

        int j = 0;

        for (byte i = 0; i < Deck.Value.Length; i++)
        {
            if (!gameManagerReference.selectedCards.Contains(i))
            {
                newDeck[j + gameManagerReference.selectedCards.Count] = Deck.Value[i];
                j++;
            }
        }

        gameManagerReference.selectedCards = new List<byte>();
        GameStateManager.selectingMode = GameStateManager.SelectingMode.None;

        Deck.Value = newDeck;
        gameManagerReference.OnGalleryReload();


    }

    private void ToBottomOfDeckFromSection()
    {

        Card[] newDeck = new Card[Deck.Value.Length];

        int j = 0;
        for (byte i = 0; i < Deck.Value.Length; i++)
        {
            if (!gameManagerReference.selectedCards.Contains(i))
            {
                newDeck[j] = Deck.Value[i];
                j++;
            }
            else
            {
                newDeck[i - j + Deck.Value.Length - gameManagerReference.selectedCards.Count] = Deck.Value[i];
            }
        }

        gameManagerReference.selectedCards = new List<byte>();
        GameStateManager.selectingMode = GameStateManager.SelectingMode.None;
        Deck.Value = newDeck;
        GameStateManager.howMany--;
        gameManagerReference.OnGalleryReload();
    }

    private void AfterBasicPokemonSetup()
    {
        gameManagerReference.coinManager.CoinContainer.SetActive(false);
        List<byte> cardsMoved = new List<byte>();
        for (byte i = 0; i < 6; i++)
        {
            cardsMoved.Add(i);
        }

        PlayerScript localScript = NetworkManager.Singleton.ConnectedClients[NetworkManager.Singleton.LocalClientId].PlayerObject.GetComponent<PlayerScript>();

        GameStateManager.SelectingMode preserveMode = GameStateManager.selectingMode;

        FromXToY(localScript.Deck, localScript.Prizes, cardsMoved, null, null, false, null, null, null, null, () =>
        {

            if (localScript.TurnOrderDeterminedAfterGameSetup)
            {
                GetTurnInfo();
            }

            if (localScript.FirstPlayerDraws && localScript.isActivePlayer.Value)
            {
                GameStateManager.howMany = 1;
                localScript.GameAction(Action.Draw);
            }
            GameStateManager.selectingMode = preserveMode;
            gameManagerReference.RenderCorrectButtons(preserveMode);

        });

    }


    public IEnumerator MoveSprite()
    {
        while (animTempSprite != null && animTempTarget != null && Vector3.Distance(animTempSprite.transform.position, animTempTarget.position) > 3f)
        {
            animTempSprite.transform.position = Vector3.MoveTowards(animTempSprite.transform.position, animTempTarget.position, 25);
            yield return new WaitForFixedUpdate();
        }
        animCallback();
    }

    [ServerRpc]
    public void DoneWithMulliganServerRpc(ulong id)
    {
        DoneWithMulliganClientRpc(id);
    }

    [ClientRpc]
    public void DoneWithMulliganClientRpc(ulong id)
    {
        if (id != NetworkManager.Singleton.LocalClientId)
        {
            gameManagerReference.otherPlayerHasCompletedMulliganStep = true;
        }

        //NetworkManager.Singleton.ConnectedClients[NetworkManager.Singleton.LocalClientId].PlayerObject
        //    .GetComponent<PlayerScript>().AfterBasicPokemonSetup();
    }

    [ServerRpc]
    public void StartBothPlayersServerRpc()
    {
        StartBothPlayersClientRpc();
    }

    [ClientRpc]
    public void StartBothPlayersClientRpc()
    {
        NetworkManager.Singleton.ConnectedClients[NetworkManager.Singleton.LocalClientId].PlayerObject
            .GetComponent<PlayerScript>().AfterBasicPokemonSetup();
    }

    public void GameAction(Action action, Card[] extraArgs = null)
    {
        string[] extraInfo = new string[0];

        if (action == Action.Setup)
        {
            if (extraArgs != null)
            {
                Deck.Value = extraArgs;
            }
            else Debug.LogError("Invalid Action.Set call, missing deck");

            int steps;
            if (Deck.Value.Length > 6) steps = PlayerInfoManager.CardsInHandStartingTheGame;
            else steps = Deck.Value.Length;

            Card[] drawnCards = new Card[steps];
            for (int i = 0; i < steps; i++)
            {
                drawnCards[i] = Deck.Value[Deck.Value.Length - 1 - i];
            }

            Card[] newHandSetup = new Card[Hand.Value.Length + steps];
            for (int i = 0; i < Hand.Value.Length; i++)
            {
                newHandSetup[i] = Hand.Value[i];
            }

            for (int i = 0; i < newHandSetup.Length; i++)
            {
                newHandSetup[i + Hand.Value.Length] = drawnCards[i];
            }

            GameStateManager.selectingMode = GameStateManager.SelectingMode.SelectingStartingPokemon;
            gameManagerReference.RenderCorrectButtons(GameStateManager.SelectingMode.SelectingStartingPokemon);
            Hand.Value = newHandSetup;

            Card[] newDeckSetup = new Card[Deck.Value.Length - steps];
            for (int i = 0; i < newDeckSetup.Length; i++)
            {
                newDeckSetup[i] = Deck.Value[i];
            }

            Deck.Value = newDeckSetup;

        }
        else if (action == Action.Mulligan)
        {
            if (Hand.Value.Length == 0) return;
            List<byte> allCardsInHand = new List<byte>() { 0, 1, 2, 3, 4, 5, 6 };
            gameManagerReference.selectedCards = allCardsInHand;

            Card[][] newMulliganInfo = new Card[mulligans.Length + 1][];
            for (int i = 0; i < mulligans.Length; i++)
            {
                newMulliganInfo[i] = mulligans[i];
            }
            newMulliganInfo[mulligans.Length] = Hand.Value;
            mulligans = newMulliganInfo;

            FromToWithModes(GameStateManager.SelectingMode.Hand, GameStateManager.SelectingMode.Deck, true, false, () =>
            {
                FromXToY(Deck, Hand, allCardsInHand, gameManagerReference.playerDeckSprite, gameManagerReference.playerHand, false, null, null, null, null, () =>
                {
                    GameStateManager.selectingMode = GameStateManager.SelectingMode.SelectingStartingPokemon;
                    gameManagerReference.RenderCorrectButtons(GameStateManager.SelectingMode.SelectingStartingPokemon);
                    RenderHand();
                });
            });
        }
        else if (action == Action.Draw)
        {
            extraInfo = new string[1] { GameStateManager.howMany.ToString() };
            List<byte> cardsToDraw = new List<byte>();

            for (byte i = 0; i < GameStateManager.howMany; i++)
            {
                if (i < Deck.Value.Length) cardsToDraw.Add(i);
            }

            FromXToY(Deck, Hand, cardsToDraw, gameManagerReference.playerDeckSprite, gameManagerReference.playerHand);

        }
        else if (action == Action.DrawMulligan)
        {
            List<byte> cardsToDraw = new List<byte>();
            extraInfo = new string[1] { GameStateManager.howMany.ToString() };

            if (GameStateManager.howMany > 0)
            {
                for (byte i = 0; i < GameStateManager.howMany; i++)
                {
                    if (i < Deck.Value.Length) cardsToDraw.Add(i);
                }

                FromXToY(Deck, Hand, cardsToDraw, gameManagerReference.playerDeckSprite, gameManagerReference.playerHand, false, null, null, null, null, () =>
                {
                    if (gameManagerReference.otherPlayerHasCompletedMulliganStep)
                    {
                        StartBothPlayersServerRpc();
                    }
                    else
                    {
                        DoneWithMulliganServerRpc(NetworkManager.Singleton.LocalClientId);
                    }
                });
            }
            else
            {
                if (gameManagerReference.otherPlayerHasCompletedMulliganStep)
                {
                    StartBothPlayersServerRpc();
                }
                else
                {
                    DoneWithMulliganServerRpc(NetworkManager.Singleton.LocalClientId);
                }
            }


        }
        else if (action == Action.Discard)
        {
            //extraInfo = gameManagerReference.selectedCards.Count.ToString();
            if (IsAttachment(GameStateManager.selectingMode))
            {
                Card FirstCardMoved = null;

                byte k = 0;
                for (int i = 0; i < ModeToNetworkDeck2D(GameStateManager.selectingMode).Value.Length; i++)
                {
                    foreach (Card card in ModeToNetworkDeck2D(GameStateManager.selectingMode).Value[i])
                    {
                        if (k == gameManagerReference.selectedCards[0])
                        {
                            FirstCardMoved = card;
                            i = ModeToNetworkDeck2D(GameStateManager.selectingMode).Value.Length;
                            break;
                        }
                        k++;
                    }
                }
                extraInfo = new string[1] { CollectionScript.FileToName(FirstCardMoved.art) };
            }
            else
            {
                extraInfo = new string[1]{ CollectionScript.FileToName(IsLocal(GameStateManager.selectingMode)
                    ? ModeToLocalDeck(GameStateManager.selectingMode)
                        .Value[gameManagerReference.selectedCards[0]].art
                    : ModeToNetworkDeck(GameStateManager.selectingMode)
                        .Value[gameManagerReference.selectedCards[0]].art
                ) + (gameManagerReference.selectedCards.Count > 1
                    ? $" and {gameManagerReference.selectedCards.Count - 1} others"
                    : ""
            ) };
            }
            FromToWithModes(GameStateManager.selectingMode, GameStateManager.SelectingMode.Discard);
        }
        else if (action == Action.LostZone)
        {
            //extraInfo = gameManagerReference.selectedCards.Count.ToString();
            if (IsAttachment(GameStateManager.selectingMode))
            {
                Card FirstCardMoved = null;

                byte k = 0;
                for (int i = 0; i < ModeToNetworkDeck2D(GameStateManager.selectingMode).Value.Length; i++)
                {
                    foreach (Card card in ModeToNetworkDeck2D(GameStateManager.selectingMode).Value[i])
                    {
                        if (k == gameManagerReference.selectedCards[0])
                        {
                            FirstCardMoved = card;
                            i = ModeToNetworkDeck2D(GameStateManager.selectingMode).Value.Length;
                            break;
                        }
                        k++;
                    }
                }
                extraInfo = new string[1] { CollectionScript.FileToName(FirstCardMoved.art) };
            }
            else
            {
                extraInfo = new string[1] { CollectionScript.FileToName(IsLocal(GameStateManager.selectingMode)
                    ? ModeToLocalDeck(GameStateManager.selectingMode)
                        .Value[gameManagerReference.selectedCards[0]].art
                    : ModeToNetworkDeck(GameStateManager.selectingMode)
                        .Value[gameManagerReference.selectedCards[0]].art
                ) + (gameManagerReference.selectedCards.Count > 1
                    ? $" and {gameManagerReference.selectedCards.Count - 1} others"
                    : ""
                ) };
            }
            FromToWithModes(GameStateManager.selectingMode, GameStateManager.SelectingMode.LostZone);
        }
        else if (action == Action.ShuffleIntoDeck)
        {
            extraInfo = new string[1] { gameManagerReference.selectedCards.Count.ToString() };
            FromToWithModes(GameStateManager.selectingMode, GameStateManager.SelectingMode.Deck, true);
        }
        else if (action == Action.Bench)
        {
            extraInfo = new string[1] { CollectionScript.FileToName(IsLocal(GameStateManager.selectingMode)
                ? ModeToLocalDeck(GameStateManager.selectingMode)
                    .Value[gameManagerReference.selectedCards[0]].art
                : ModeToNetworkDeck(GameStateManager.selectingMode)
                    .Value[gameManagerReference.selectedCards[0]].art
            ) + (gameManagerReference.selectedCards.Count > 1
                ? $" and {gameManagerReference.selectedCards.Count - 1} others"
                : ""
            ) };
            FromToWithModes(GameStateManager.selectingMode, GameStateManager.SelectingMode.Bench);
        }
        else if (action == Action.Active)
        {
            extraInfo = new string[1] { CollectionScript.FileToName(IsLocal(GameStateManager.selectingMode)
                ? ModeToLocalDeck(GameStateManager.selectingMode)
                    .Value[gameManagerReference.selectedCards[0]].art
                : ModeToNetworkDeck(GameStateManager.selectingMode)
                    .Value[gameManagerReference.selectedCards[0]].art
            ) + (gameManagerReference.selectedCards.Count > 1
                ? $" and {gameManagerReference.selectedCards.Count - 1} others"
                : ""
            ) };
            FromToWithModes(GameStateManager.selectingMode, GameStateManager.SelectingMode.Active);
        }
        else if (action == Action.ToHand)
        {
            extraInfo = new string[2] { gameManagerReference.selectedCards.Count.ToString(),
                GetName(GameStateManager.selectingMode) };
            FromToWithModes(GameStateManager.selectingMode, GameStateManager.SelectingMode.Hand);
        }
        else if (action == Action.AttachStart)
        {
            foreach (GameObject playerClient in PlayerInfoManager.players)
            {
                foreach (Transform child in playerClient.GetComponent<PlayerScript>().cardSection.BenchObj.transform)
                {
                    child.gameObject.transform.GetChild(0).GetComponent<Image>().color = CardManipulation.PossibleMoveTo;
                }

                foreach (Transform child in playerClient.GetComponent<PlayerScript>().cardSection.ActiveObj.transform)
                {
                    child.gameObject.transform.GetChild(0).GetComponent<Image>().color = CardManipulation.PossibleMoveTo;
                }
            }
        }
        else if (action == Action.LevelUpStart)
        {
            if (cardSection.Bench.Value.Length > 0)
            {
                foreach (Transform child in cardSection.BenchObj.transform)
                {
                    child.gameObject.transform.GetChild(0).GetComponent<Image>().color = CardManipulation.Unselected;
                }
            }

            if (cardSection.Active.Value.Length > 0)
            {
                foreach (Transform child in cardSection.ActiveObj.transform)
                {
                    child.gameObject.transform.GetChild(0).GetComponent<Image>().color = CardManipulation.Unselected;
                }
            }
        }
        else if (action == Action.LevelDown)
        {
            NetworkVariable<Card[][]> oldCardLevels = GetLevel(GameStateManager.selectingMode);
            NetworkVariable<Card[]> from = ModeToNetworkDeck(GameStateManager.selectingMode);

            if (oldCardLevels.Value[
                        gameManagerReference.selectedCards[0]
                    ].Length == 0) return;

            // send last levelup to Hand
            Card[] newHand = new Card[Hand.Value.Length + 1];
            for (int i = 0; i < Hand.Value.Length; i++)
            {
                newHand[i] = Hand.Value[i];
            }
            newHand[newHand.Length - 1] = from.Value[gameManagerReference.selectedCards[0]];

            // switch selected card with it's last levelup
            Card[] newFrom = from.Value;
            newFrom[gameManagerReference.selectedCards[0]] = oldCardLevels.Value[
                gameManagerReference.selectedCards[0]
            ][oldCardLevels.Value[gameManagerReference.selectedCards[0]].Length - 1];

            Card[] newLevelUp = new Card[
                oldCardLevels.Value[
                    gameManagerReference.selectedCards[0]
                ].Length - 1
            ];

            for (byte i = 0; i < newLevelUp.Length; i++)
            {
                newLevelUp[i] = oldCardLevels.Value[gameManagerReference.selectedCards[0]][i];
            }

            // set values
            oldCardLevels.Value[gameManagerReference.selectedCards[0]] = newLevelUp;

            from.Value = null;
            from.Value = newFrom;

            Hand.Value = newHand;

            extraInfo = new string[1] { CollectionScript.FileToName(from.Value[gameManagerReference.selectedCards[0]].art) +
                (gameManagerReference.selectedCards.Count > 1
                    ? $" and {gameManagerReference.selectedCards.Count - 1} more" : "") };
            gameManagerReference.selectedCards = new List<byte>();
            GameStateManager.selectingMode = GameStateManager.SelectingMode.None;
            gameManagerReference.RenderCorrectButtons(GameStateManager.SelectingMode.None);

        }
        else if (action == Action.AddCounter)
        {

            if (GameStateManager.selectingMode == GameStateManager.SelectingMode.Bench)
            {
                extraInfo = new string[1] { CollectionScript.FileToName(
                    cardSection.Bench.Value[gameManagerReference.selectedCards[0]].art) +
                    (gameManagerReference.selectedCards.Count > 1
                        ? $" and {gameManagerReference.selectedCards.Count - 1} more" : ""
                ) };
                int[] newBenchCounters = cardSection.BenchCounters.Value;
                cardSection.BenchCounters.Value = null;

                foreach (byte card in gameManagerReference.selectedCards)
                {
                    newBenchCounters[card] = 0;
                }
                cardSection.BenchCounters.Value = newBenchCounters;
            }
            else if (GameStateManager.selectingMode == GameStateManager.SelectingMode.Active)
            {
                extraInfo = new string[1] { CollectionScript.FileToName(
                    cardSection.Active.Value[gameManagerReference.selectedCards[0]].art) +
                    (gameManagerReference.selectedCards.Count > 1
                        ? $" and {gameManagerReference.selectedCards.Count - 1} more" : ""
                ) };
                int[] newActiveCounters = cardSection.ActiveCounters.Value;
                cardSection.ActiveCounters.Value = null;

                foreach (byte card in gameManagerReference.selectedCards)
                {
                    newActiveCounters[card] = 0;
                }
                cardSection.ActiveCounters.Value = newActiveCounters;
            }

            //cardSection.RenderSectionSelectingCancel(cardSection.ExtraZoneObj);
        }
        else if (action == Action.Mill)
        {
            extraInfo = new string[1] { GameStateManager.howMany.ToString() };
            for (int i = 0; i < GameStateManager.howMany; i++)
            {
                gameManagerReference.selectedCards.Add((byte)i);
            }

            FromToWithModes(GameStateManager.SelectingMode.Deck, GameStateManager.SelectingMode.Discard);
        }
        else if (action == Action.TakePrize)
        {
            extraInfo = new string[1] { GameStateManager.howMany.ToString() };
            for (int i = 0; i < GameStateManager.howMany; i++)
            {
                gameManagerReference.selectedCards.Add((byte)i);
            }

            FromToWithModes(GameStateManager.SelectingMode.Prizes, GameStateManager.SelectingMode.Hand);
        }
        else if (action == Action.ToBottomOfDeck)
        {
            extraInfo = new string[1] { gameManagerReference.selectedCards.Count.ToString() };
            if (GameStateManager.viewingMode == GameStateManager.SelectingMode.DeckSection)
            {
                ToBottomOfDeckFromSection();
            }
            else
            {
                FromToWithModes(GameStateManager.selectingMode, GameStateManager.SelectingMode.Deck);
            }
        }
        else if (action == Action.ToTopOfDeck)
        {
            extraInfo = new string[1] { gameManagerReference.selectedCards.Count.ToString() };
            if (GameStateManager.viewingMode == GameStateManager.SelectingMode.DeckSection)
            {
                ToTopOfDeckFromSection();
            }
            else
            {
                FromToWithModes(GameStateManager.selectingMode, GameStateManager.SelectingMode.Deck, false, true);
            }

        }
        else if (action == Action.PlaySupporter)
        {
            Card[] newHand = new Card[Hand.Value.Length - 1];
            Card newSupporter = null;

            byte j = 0;
            for (byte i = 0; i < Hand.Value.Length; i++)
            {
                if (gameManagerReference.selectedCards.Contains(i))
                {
                    newSupporter = Hand.Value[i];
                }
                else
                {
                    newHand[j] = Hand.Value[i];
                    j++;
                }
            }

            if (newSupporter != null)
            {
                Hand.Value = newHand;
                SupporterCard.Value = newSupporter;
                extraInfo = new string[1] { CollectionScript.FileToName(newSupporter.art) };
            }
            else
            {
                Debug.LogError("no card selected");
            }

            GameStateManager.selectingMode = GameStateManager.SelectingMode.None;
            gameManagerReference.selectedCards = new List<byte>();
            gameManagerReference.RenderCorrectButtons(GameStateManager.SelectingMode.None);

        }
        else if (action == Action.MoveToPrizes)
        {
            extraInfo = new string[1] { gameManagerReference.selectedCards.Count.ToString() };
            FromToWithModes(GameStateManager.selectingMode, GameStateManager.SelectingMode.Prizes);
        }
        else
        {
            Debug.LogError("no action provided");
        }

        AppendGameLogServerRpc(ActionToString(action, extraInfo));
    }

    private string ActionToString(Action action, string[] extraInfo = null)
    {
        return PlayerInfoManager.Username + ": " + action switch
        {
            Action.Setup => "Setup",
            Action.Mulligan => "Mulliganed",
            Action.Draw => $"Drew {extraInfo[0]} cards",
            Action.DrawMulligan => $"Drew {extraInfo[0]} cards from mulligan",
            Action.Discard => $"Discarded {extraInfo[0]} card",
            Action.LostZone => $"Moved {extraInfo[0]} cards to lost zone",
            Action.ShuffleIntoDeck => $"Shuffled {extraInfo[0]} cards in deck",
            Action.Bench => $"Moved {extraInfo[0]} to bench",
            Action.Active => $"Moved {extraInfo[0]} to active",
            Action.ToHand => $"Moved {extraInfo[0]} cards to hand from {extraInfo[1]}",
            Action.AttachStart => "Began attachment",
            Action.LevelUpStart => "Began level up",
            Action.LevelDown => $"Devolved to {extraInfo[0]}",
            Action.AddCounter => $"Added counter to {extraInfo[0]}",
            Action.Mill => $"Discard the top {extraInfo[0]} cards of deck",
            Action.TakePrize => $"Took {extraInfo[0]} prize cards",
            Action.ToBottomOfDeck => $"Moved {extraInfo[0]} cards to bottom of deck",
            Action.ToTopOfDeck => $"Moved {extraInfo[0]} cards to top of deck",
            Action.PlaySupporter => $"Played supporter {extraInfo[0]}",
            Action.MoveToPrizes => $"Moved {extraInfo[0]} cards to prizes",
            _ => "unnamed game action"
        };
    }

    [ServerRpc]
    public void AppendGameLogServerRpc(string log)
    {
        AppendGameLogClientRpc(log);
    }

    [ClientRpc]
    public void AppendGameLogClientRpc(string log)
    {
        if (gameManagerReference.gameLog != null && gameManagerReference.gameLog.GameLogText != null)
            gameManagerReference.gameLog.GameLogText.text += log + $"\n";
    }

    public Image PoisonMarker;
    public Image BurnMarker;

    public void ToggleCardState(byte stateIndex)
    {
        if (gameManagerReference.selectedCards.Count != 1) return;

        NetworkVariable<bool[][]> states = GetState(GameStateManager.selectingMode);
        bool[][] newStates = states.Value;

        //for (byte i = 0; i < newStates.Length; i++)
        //{
        //    if (gameManagerReference.selectedCards.Contains(i))
        //    {
        //        newStates[i][stateIndex] = !newStates[i][stateIndex];
        //    }
        //}

        newStates[gameManagerReference.selectedCards[0]][stateIndex] = !newStates[gameManagerReference.selectedCards[0]][stateIndex];

        if (newStates[gameManagerReference.selectedCards[0]][stateIndex] &&
            (stateIndex == 0 || stateIndex == 2 || stateIndex == 3))
        {
            // this code triggers if asleep, confusion, or paralyzes is applied because that eliminates other status conditions
            switch (stateIndex)
            {
                case 0:
                    newStates[gameManagerReference.selectedCards[0]][2] = false;
                    newStates[gameManagerReference.selectedCards[0]][3] = false;
                    break;
                case 2:
                    newStates[gameManagerReference.selectedCards[0]][0] = false;
                    newStates[gameManagerReference.selectedCards[0]][3] = false;
                    break;
                case 3:
                    newStates[gameManagerReference.selectedCards[0]][0] = false;
                    newStates[gameManagerReference.selectedCards[0]][2] = false;
                    break;
                default:
                    break;
            }
        }

        gameManagerReference.StatusMenu.SetActive(false);
        gameManagerReference.mainButtonsPanel.SetActive(true);

        states.Value = null;
        states.Value = newStates;

        GameObject obj = ModeToGameObject(GameStateManager.selectingMode);
        cardSection.RenderSectionSelectingCancel(obj);

    }

    public void RenderPoisonBurn()
    {
        if (cardSection.ActiveCardStates.Value.Length != 1)
        {
            PoisonMarker.color = new Color(1, 1, 1, .196f);
            BurnMarker.color = new Color(1, 1, 1, .196f);
            return;
        }

        if (cardSection.ActiveCardStates.Value[0][1])
        {
            BurnMarker.color = Color.white;
        }
        else
        {
            BurnMarker.color = new Color(1, 1, 1, .196f);
        }

        if (cardSection.ActiveCardStates.Value[0][4])
        {
            PoisonMarker.color = Color.white;

        }
        else
        {
            PoisonMarker.color = new Color(1, 1, 1, .196f);
        }
    }


    public void ManageStartOfTurn()
    {
        // tell gamelog that your turn started
        AppendGameLogServerRpc(PlayerInfoManager.Username + ": Started turn");

        // draw for turn
        if (AutoDraw)
        {
            GameStateManager.howMany = 1;
            GameAction(Action.Draw);
        }

        // discard supporter
        if (SupporterCard.Value != null)
        {
            Card[] newDiscard = new Card[Discard.Value.Length + 1];

            for (int i = 0; i < newDiscard.Length - 1; i++)
            {
                newDiscard[i] = Discard.Value[i];
            }
            newDiscard[newDiscard.Length - 1] = SupporterCard.Value;
            Discard.Value = newDiscard;

            SupporterCard.Value = null;
            GameStateManager.selectingMode = GameStateManager.SelectingMode.None;
            gameManagerReference.RenderCorrectButtons(GameStateManager.SelectingMode.None);
        }

    }

    [ServerRpc]
    public void SwitchTurnsServerRpc(ulong playerID)
    {
        SwitchTurnsClientRpc(playerID);
    }

    [ClientRpc]
    public void SwitchTurnsClientRpc(ulong playerID)
    {
        // playerID is the id of the player who just passed their turn

        NetworkManager.Singleton.ConnectedClients.TryGetValue(NetworkManager.Singleton.LocalClientId, out var client);
        PlayerScript player = client.PlayerObject.GetComponent<PlayerScript>();
        player.isActivePlayer.Value = playerID != NetworkManager.Singleton.LocalClientId;
        player.RenderTurnInfo();
        if (NetworkManager.Singleton.LocalClientId != playerID) // if it's now this player's turn
        {
            //GameStateManager.selectingMode = GameStateManager.SelectingMode.None;
            //gameManagerReference.RenderCorrectButtons(GameStateManager.SelectingMode.None);
            player.ManageStartOfTurn();
        }
    }



    [ClientRpc]
    public void GiveTurnInfoClientRpc(int info, bool autoDraw, bool autoUntap, int format)
    {
        NetworkManager.Singleton.ConnectedClients.TryGetValue(NetworkManager.Singleton.LocalClientId, out var client);
        PlayerScript player = client.PlayerObject.GetComponent<PlayerScript>();
        player.AutoDraw = autoDraw;
        player.AutoUntap = autoUntap;
        player.FirstTurnInfo = info;

        switch (format)
        {
            case 0: // 2013-On
                player.CoinFlipWinnerDecidesTurnOrder = true;
                player.TurnOrderDeterminedAfterGameSetup = false;
                player.FirstPlayerDraws = true;
                break;

            case 1: // 2011-2012
                player.CoinFlipWinnerDecidesTurnOrder = false;
                player.TurnOrderDeterminedAfterGameSetup = false;
                player.FirstPlayerDraws = true;
                break;

            case 2: // 2007-2010
                player.CoinFlipWinnerDecidesTurnOrder = false;
                player.TurnOrderDeterminedAfterGameSetup = true;
                player.FirstPlayerDraws = true;
                break;

            case 3: // 2004-2006
                player.CoinFlipWinnerDecidesTurnOrder = false;
                player.TurnOrderDeterminedAfterGameSetup = true;
                player.FirstPlayerDraws = false;
                break;

            case 4: // Start-2003
                player.CoinFlipWinnerDecidesTurnOrder = false;
                player.TurnOrderDeterminedAfterGameSetup = true;
                player.FirstPlayerDraws = true;
                break;

            default:
                break;
        }

        if (!client.PlayerObject.GetComponent<PlayerScript>().TurnOrderDeterminedAfterGameSetup)
        {
            GetTurnInfo();
        }

    }

    public void GetTurnInfo()
    {

        NetworkManager.Singleton.ConnectedClients.TryGetValue(NetworkManager.Singleton.LocalClientId, out var client);
        PlayerScript clientScript = client.PlayerObject.GetComponent<PlayerScript>();

        int info = clientScript.FirstTurnInfo;
        if (!HasStarted || info == -3) // error
        {

            if (info == -3)
            {
                Debug.LogError("info == -3");
            }

            Invoke(nameof(GetTurnInfo), 0.5f);
            return;
        }

        if (info == -2) // coin flip
        {
            gameManagerReference.coinManager.CoinContainer.SetActive(true);
            if (IsServer)
            {
                gameManagerReference.coinManager.HeadsOrTailsText.SetActive(true);
                gameManagerReference.coinManager.HeadsOrTailsButtons.SetActive(true);
                gameManagerReference.coinManager.FirstOrSecondText.SetActive(false);
                gameManagerReference.coinManager.FirstOrSecondButtons.SetActive(false);
                gameManagerReference.coinManager.WaitingText.SetActive(false);
            }
            else
            {
                gameManagerReference.coinManager.FirstOrSecondText.SetActive(false);
                gameManagerReference.coinManager.FirstOrSecondButtons.SetActive(false);
                gameManagerReference.coinManager.WaitingText.SetActive(true);
            }
            return;
        }

        // explicitely stated

        gameManagerReference.coinManager.BlockView.SetActive(false);
        client.PlayerObject.GetComponent<PlayerScript>().isActivePlayer.Value = info == (int)NetworkManager.Singleton.LocalClientId;
        client.PlayerObject.GetComponent<PlayerScript>().RenderTurnInfo();
    }

    IEnumerator RotateCoin(int result, System.Action callback = null)
    {
        float totalRotation = 0;
        float speed = 12;


        while (totalRotation % 180 != 0 ||
            (result == 0 && !gameManagerReference.coinManager.CoinHead.activeSelf) ||
            (result == 1 && !gameManagerReference.coinManager.CoinTail.activeSelf) ||
            totalRotation < 180 * 8)
        {
            totalRotation += speed;
            gameManagerReference.coinManager.CoinHead.transform.localRotation = Quaternion.Euler(totalRotation, 0, 0);
            gameManagerReference.coinManager.CoinTail.transform.localRotation = Quaternion.Euler(totalRotation, 0, 0);

            if ((int)totalRotation % 360 > 90 && (int)totalRotation % 360 < 90 + speed * 10)
            {
                gameManagerReference.coinManager.CoinHead.SetActive(false);
                gameManagerReference.coinManager.CoinTail.SetActive(true);
            }
            else if ((int)totalRotation % 360 > 270 && (int)totalRotation % 360 < 270 + speed * 10)
            {
                gameManagerReference.coinManager.CoinHead.SetActive(true);
                gameManagerReference.coinManager.CoinTail.SetActive(false);
            }
            yield return new WaitForFixedUpdate();
        }

        yield return new WaitForSeconds(1.5f);
        if (IsOwner)
        {
            AppendGameLogServerRpc(PlayerInfoManager.Username + ": Flipped coin with result of " +
                (result == 0 ? "heads" : "tails"));
        }
        //NetworkManager.Singleton.ConnectedClients[NetworkManager.Singleton.LocalClientId]
        //    .PlayerObject.GetComponent<PlayerScript>()
        //    .AppendGameLogServerRpc(PlayerInfoManager.Username + ": Flipped coin with result of " +
        //    (result == 0 ? "heads" : "tails"));
        callback?.Invoke();

    }

    [ClientRpc]
    public void SelectedHeadsOrTailsClientRpc(bool heads, int result)
    {
        if (!IsServer)
        {
            gameManagerReference.coinManager.WaitingText.GetComponent<Text>().text = "Opponent has chosen " +
                (heads ? "heads" : "tails") + ".";
        }

        StartCoroutine(RotateCoin(result, () =>
        {
            gameManagerReference.coinManager.HeadsOrTailsText.SetActive(false);
            gameManagerReference.coinManager.HeadsOrTailsButtons.SetActive(false);

            NetworkManager.Singleton.ConnectedClients.TryGetValue(NetworkManager.Singleton.LocalClientId, out var client);
            PlayerScript clientScript = client.PlayerObject.GetComponent<PlayerScript>();

            if (clientScript.CoinFlipWinnerDecidesTurnOrder)
            {
                if (IsServer)
                {
                    if (!heads && result == 1 || heads && result == 0) // the host was correct
                    {
                        gameManagerReference.coinManager.FirstOrSecondText.SetActive(true);
                        gameManagerReference.coinManager.FirstOrSecondButtons.SetActive(true);
                    }
                    else
                    {
                        gameManagerReference.coinManager.WaitingText.SetActive(true);
                    }
                }
                else
                {
                    if (heads && result == 1 || !heads && result == 0) // the host was incorrect
                    {
                        gameManagerReference.coinManager.WaitingText.SetActive(false);
                        gameManagerReference.coinManager.FirstOrSecondText.SetActive(true);
                        gameManagerReference.coinManager.FirstOrSecondButtons.SetActive(true);
                    }
                    else
                    {
                        gameManagerReference.coinManager.WaitingText.SetActive(true);
                    }
                }
            }
            else
            {
                gameManagerReference.coinManager.CoinContainer.SetActive(false);
                if (IsServer)
                {
                    if (!heads && result == 1 || heads && result == 0) // the host was correct
                    {
                        // go first
                        clientScript.isActivePlayer.Value = true;
                    }
                    else
                    {
                        // go second
                        clientScript.isActivePlayer.Value = false;
                    }
                }
                else
                {

                    if (heads && result == 1 || !heads && result == 0) // the host was incorrect
                    {
                        // go first
                        clientScript.isActivePlayer.Value = true;
                    }
                    else
                    {
                        // go second
                        clientScript.isActivePlayer.Value = false;
                    }
                }
                clientScript.RenderTurnInfo();

                if (AutoDraw && clientScript.TurnOrderDeterminedAfterGameSetup && clientScript.FirstPlayerDraws)
                {
                    GameStateManager.howMany = 1;
                    clientScript.GameAction(Action.Draw);
                }
                gameManagerReference.coinManager.BlockView.SetActive(false);
            }

        }));
    }


    [ServerRpc]
    public void SelectedFirstOrSecondServerRpc(bool first, ulong id)
    {
        SelectedFirstOrSecondClientRpc(first, id);
    }

    [ClientRpc]
    public void SelectedFirstOrSecondClientRpc(bool first, ulong id)
    {
        gameManagerReference.coinManager.CoinContainer.SetActive(false);
        NetworkManager.Singleton.ConnectedClients.TryGetValue(NetworkManager.Singleton.LocalClientId, out var client);

        PlayerScript clientScript = client.PlayerObject.GetComponent<PlayerScript>();

        clientScript.isActivePlayer.Value = id == NetworkManager.Singleton.LocalClientId ?
            first : !first;
        gameManagerReference.coinManager.BlockView.SetActive(false);
        clientScript.RenderTurnInfo();
    }


    [ServerRpc]
    public void RevealHandServerRpc(ulong senderPlayerID, Card[] hand)
    {
        RevealHandClientRpc(senderPlayerID, hand);
    }

    [ClientRpc]
    public void RevealHandClientRpc(ulong senderPlayerID, Card[] hand)
    {
        if (senderPlayerID != NetworkManager.Singleton.LocalClientId)
        {
            gameManagerReference.GalleryTitle.text = "Viewing Cards Your Opponent Selected";
            gameManagerReference.OnCustomViewOnly(hand);
        }
    }

    [ServerRpc]
    public void AllowEditDeckServerRpc(ulong senderPlayerID, Card[] hand)
    {
        AllowEditDeckClientRpc(senderPlayerID, hand);
    }

    [ClientRpc]
    public void AllowEditDeckClientRpc(ulong senderPlayerID, Card[] hand)
    {
        if (senderPlayerID != NetworkManager.Singleton.LocalClientId)
        {
            //gameManagerReference.OnCustomViewWithEditAccess(hand, GameStateManager.SelectingMode.Deck);
            gameManagerReference.GalleryTitle.text = "Viewing Shared Cards With Edit Access";
            gameManagerReference.OnCustomViewWithEditAccess(hand);
        }
    }


    [ServerRpc]
    public void RemoteToTopOfDeckServerRpc(ulong senderPlayerID, byte originalLength, byte[] selected)
    {
        RemoteToTopOfDeckClientRpc(senderPlayerID, originalLength, selected);
    }

    [ClientRpc]
    public void RemoteToTopOfDeckClientRpc(ulong senderPlayerID, byte originalLength, byte[] selected)
    {
        if (senderPlayerID != NetworkManager.Singleton.LocalClientId)
        {
            NetworkManager.Singleton.ConnectedClients.TryGetValue(NetworkManager.Singleton.LocalClientId, out var networkClient);
            PlayerScript playerCode = networkClient.PlayerObject.GetComponent<PlayerScript>();

            if (GameStateManager.selectingMode != GameStateManager.SelectingMode.None)
            {
                gameManagerReference.OnSelectCancel();
            }


            Card[] newDeck = new Card[playerCode.Deck.Value.Length];

            for (int i = 0; i < selected.Length; i++)
            {
                newDeck[i] = playerCode.Deck.Value[selected[i]];
            }

            int j = 0;

            for (byte i = 0; i < playerCode.Deck.Value.Length; i++)
            {
                if (!selected.Contains(i))
                {
                    newDeck[j + selected.Length] = playerCode.Deck.Value[i];
                    j++;
                }
            }

            playerCode.Deck.Value = newDeck;
            GameStateManager.selectingMode = GameStateManager.SelectingMode.None;

            playerCode.AllowEditDeckServerRpc(NetworkManager.Singleton.LocalClientId, playerCode.Deck.Value
                .Skip(0).Take(originalLength).ToArray());

        }
    }


    public static IEnumerator StopNetwork(System.Action callback)
    {
        yield return new WaitForFixedUpdate();

        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.StopHost();


        }
        else if (NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.StopClient();
        }

        PlayerInfoManager.players = new List<GameObject>();

        callback();
    }

    [ServerRpc]
    public void AnnounceWinnerServerRpc(bool didWin, ulong id)
    {
        AnnounceWinnerClientRpc(didWin, id);
    }

    [ClientRpc]
    public void AnnounceWinnerClientRpc(bool didWin, ulong id)
    {

        if (id != NetworkManager.Singleton.LocalClientId)
        {
            //StartCoroutine(StopNetwork());
            if ((id == NetworkManager.Singleton.LocalClientId && didWin) || (id != NetworkManager.Singleton.LocalClientId && !didWin))
            {
                gameManagerReference.gameSettingsManager.VictoryScreen.SetActive(true);
            }
            else
            {
                gameManagerReference.gameSettingsManager.DefeatScreen.SetActive(true);
            }
        }
    }

    IEnumerator StartRematch()
    {
        gameManagerReference.gameSettingsManager.RematchPanelPlayer2.GetComponent<Image>().color = Color.green;

        NetworkManager.Singleton.ConnectedClients.TryGetValue(NetworkManager.Singleton.LocalClientId, out var networkClient);
        PlayerScript playerScript = networkClient.PlayerObject.GetComponent<PlayerScript>();
        yield return new WaitForSeconds(1);

        playerScript.handSize.Value = 0;
        playerScript.deckSize.Value = 0;

        playerScript.Hand.Value = new Card[0];
        playerScript.Deck.Value = new Card[0];
        playerScript.SpecialDeck.Value = new Card[0];
        playerScript.Discard.Value = new Card[0];
        playerScript.LostZone.Value = new Card[0];

        gameManagerReference.selectedCards = new List<byte>();
        GameStateManager.selectingMode = GameStateManager.SelectingMode.None;

        playerScript.cardSection.Bench.Value = new Card[0];
        playerScript.cardSection.Active.Value = new Card[0];

        playerScript.cardSection.BenchAttachments.Value = new Card[0][];
        playerScript.cardSection.ActiveAttachments.Value = new Card[0][];

        //playerScript.cardSection.BenchCardStates.Value = new bool[0][];
        playerScript.cardSection.ActiveCardStates.Value = new bool[0][];

        playerScript.cardSection.BenchCounters.Value = new int[0];
        playerScript.cardSection.ActiveCounters.Value = new int[0];

        playerScript.cardSection.BenchCardOldEvolutions.Value = new Card[0][];
        playerScript.cardSection.ActiveCardOldEvolutions.Value = new Card[0][];

        playerScript.Prizes.Value = new Card[0];
        playerScript.SupporterCard.Value = null;

        gameManagerReference.CurrentStadium = null;
        RenderStadium();

        if (!playerScript.TurnOrderDeterminedAfterGameSetup)
        {
            gameManagerReference.coinManager.BlockView.SetActive(true);
        }

        playerScript.GameAction(Action.Setup, CardManipulation.Shuffle(PlayerInfoManager.fullDeck));

        gameManagerReference.gameSettingsManager.RematchPanel.SetActive(false);
        gameManagerReference.gameSettingsManager.SettingsMenu.SetActive(false);
        gameManagerReference.gameSettingsManager.VictoryScreen.SetActive(false);
        gameManagerReference.gameSettingsManager.DefeatScreen.SetActive(false);

        playerScript.IsReadyForRematch.Value = false;

        gameManagerReference.otherPlayerHasCompletedMulliganStep = false;

        playerScript.isActivePlayer.Value = false;

        playerScript.mulligans = new Card[0][];

        // manage coin flip
        if (!playerScript.TurnOrderDeterminedAfterGameSetup)
        {
            GetTurnInfo();
        }

        gameManagerReference.RenderCorrectButtons(GameStateManager.SelectingMode.SelectingStartingPokemon);

    }

    public void ConnectToRematch()
    {
        IsReadyForRematch.Value = true;

        byte count = 0;

        foreach (GameObject client in PlayerInfoManager.players)
        {
            if (client.GetComponent<PlayerScript>().IsReadyForRematch.Value) count++;
        }

        if (count == 1)
        {
            gameManagerReference.gameSettingsManager.RematchPanelPlayer1.GetComponent<Image>().color = Color.green;
            gameManagerReference.gameSettingsManager.RematchPanelPlayer2.GetComponent<Image>().color = Color.gray;
        }
        else if (count == 2)
        {
            gameManagerReference.gameSettingsManager.RematchPanelPlayer1.GetComponent<Image>().color = Color.green;
            gameManagerReference.gameSettingsManager.RematchPanelPlayer2.GetComponent<Image>().color = Color.green;

            BroadcastRematchServerRpc();
        }
    }

    [ServerRpc]
    public void BroadcastRematchServerRpc()
    {
        BroadcastRematchClientRpc();
    }

    [ClientRpc]
    public void BroadcastRematchClientRpc()
    {
        StartCoroutine(StartRematch());
    }

    [ServerRpc]
    public void ManualCoinFlipServerRpc(int result)
    {
        ManualCoinFlipClientRpc(result);
    }

    [ClientRpc]
    public void ManualCoinFlipClientRpc(int result)
    {
        gameManagerReference.coinManager.CoinContainer.SetActive(true);
        gameManagerReference.coinManager.FirstOrSecondButtons.SetActive(false);
        gameManagerReference.coinManager.FirstOrSecondText.SetActive(false);
        gameManagerReference.coinManager.WaitingText.SetActive(false);
        StartCoroutine(RotateCoin(result, () =>
        {
            gameManagerReference.coinManager.CoinContainer.SetActive(false);
        }));
    }

    public static void LeftShiftArray<T>(T[] arr, int shift)
    {
        // 1, 2, 3, 4
        // 6

        shift = shift % arr.Length;
        // shift = 2

        T[] buffer = new T[shift];
        // new T[2]

        System.Array.Copy(arr, buffer, shift);
        // buffer = { 1, 2 }

        System.Array.Copy(arr, shift, arr, 0, arr.Length - shift);
        // arr = { 3, 4, 3, 4 }

        System.Array.Copy(buffer, 0, arr, arr.Length - shift, shift);
        // arr = { 3, 4, 1, 2 }
    }

    public static void RightShiftArray<T>(T[] arr, int shift)
    {
        // 1, 2, 3, 4, 5
        // 1

        shift = shift % arr.Length;

        T[] buffer = new T[shift];
        // new T[1]

        System.Array.Copy(arr, arr.Length - shift, buffer, 0, shift);
        // buffer = { 1 }

        System.Array.Copy(arr, 0, arr, shift, arr.Length - shift);
        // arr = { 1, 1, 2, 3, 4 }

        System.Array.Copy(buffer, 0, arr, 0, shift);
        // arr = { 5, 1, 2, 3, 4 }

    }

    IEnumerator RollDie(int seed)
    {
        gameManagerReference.coinManager.DieContainer.SetActive(true);

        int speed = 20;

        int uniqueTrajectories = 30;
        int framePerTrajectory = 30 / uniqueTrajectories;

        Random.InitState(seed);
        for (int i = 0; i < uniqueTrajectories; i++)
        {

            Quaternion trajectory = Quaternion.Euler(
                Random.value * speed, Random.value * speed, Random.value * speed);
            for (int j = 0; j < framePerTrajectory; j++)
            {
                gameManagerReference.coinManager.Dice.transform.rotation *= trajectory;
                yield return new WaitForFixedUpdate();
            }
        }

        Vector3 oldDieTransform = gameManagerReference.coinManager.Dice.transform.rotation.eulerAngles;

        float newX = Mathf.Round(oldDieTransform.x / 90) * 90;
        float newY = Mathf.Round(oldDieTransform.y / 90) * 90;
        float newZ = Mathf.Round(oldDieTransform.z / 90) * 90;

        gameManagerReference.coinManager.Dice.transform.localRotation = Quaternion.Euler(newX, newY, newZ);

        yield return new WaitForSeconds(1.5f);
        gameManagerReference.coinManager.DieContainer.SetActive(false);

        if (IsOwner)
        {
            int result = 7;

            int[] sides = { 4, 3 };
            int[] verticalSides = { 6, 2, 1, 5 };
            int localX = (int)Mathf.Round(newX < 0 ? 180 - newX : newX) % 360;
            RightShiftArray<int>(verticalSides, localX / 90);

            int localY = (int)Mathf.Round(newY < 0 ? 180 - newY : newY) % 360;

            void horizontalRotate()
            {
                int[] newVerticalSides = new int[4];
                newVerticalSides[0] = sides[1];
                newVerticalSides[1] = verticalSides[1];
                newVerticalSides[2] = sides[0];
                newVerticalSides[3] = verticalSides[3];

                int[] newSides = new int[2];
                newSides[0] = verticalSides[0];
                newSides[1] = verticalSides[2];

                verticalSides = newVerticalSides;
                sides = newSides;
            }

            for (int i = 0; i < localY / 90; i++)
            {
                horizontalRotate();
            }

            int localZ = ((int)Mathf.Round(newZ < 0 ? 180 - newZ : newZ)) % 360;
            if (localX % 180 == 0 && localY % 180 == 0)
            {
                result = verticalSides[0];
            }
            else if (localX == 90)
            {
                int rotations = 4 - ((localZ / 90) % 4);
                print(rotations);
                for (int i = 0; i < rotations; i++)
                {
                    horizontalRotate();
                    //print($"{verticalSides[0]} {sides[0]} {verticalSides[2]} {sides[1]} ");
                }
                result = verticalSides[0];

            }
            else if (localX == 270)
            {
                int rotations = localZ / 90;
                for (int i = 0; i < rotations; i++)
                {
                    horizontalRotate();
                }
                result = verticalSides[0];
            }
            // localX should now be 180 or 0
            else if ((localX == 180 && localY == 270) || (localX == 0 && localY == 90))
            {
                int rotations = localZ / 90;
                RightShiftArray(verticalSides, rotations);
                result = verticalSides[0];
            }

            else if ((localX == 180 && localY == 90) || (localX == 0 && localY == 270))
            {
                int rotations = localZ / 90;
                LeftShiftArray(verticalSides, rotations);
                result = verticalSides[0];
            }
            else
            {
                Debug.Log(localX);
                Debug.Log(localY);
                Debug.Log(localZ);
            }

            AppendGameLogServerRpc(PlayerInfoManager.Username + ": Flipped coin with result of " +
            result.ToString());
        }
    }

    [ServerRpc]
    public void ManualDieRollServerRpc(int seed)
    {
        ManualDieRollClientRpc(seed);
    }

    [ClientRpc]
    public void ManualDieRollClientRpc(int seed)
    {
        StartCoroutine(RollDie(seed));
    }


    [ServerRpc]
    public void ShareMulliganInfoServerRpc(ulong id, Card[] mulligans, int index, int finalIndex)
    {
        ShareMulliganInfoClientRpc(id, mulligans, index, finalIndex);
    }

    [ClientRpc]
    public void ShareMulliganInfoClientRpc(ulong id, Card[] mulligans, int index, int finalIndex)
    {
        if (NetworkManager.Singleton.LocalClientId != id)
        {

            NetworkManager.Singleton.ConnectedClients.TryGetValue(NetworkManager.Singleton.LocalClientId, out var networkClient);
            PlayerScript playerScript = networkClient.PlayerObject.GetComponent<PlayerScript>();

            // if neither player mulliganed
            if (finalIndex == -1 && playerScript.mulligans.Length == 0) // -1 means that the opponent mulliganed 0 times
            {
                AfterBasicPokemonSetup();
            }
            // if your opponent mulliganed
            else if (finalIndex != -1)
            {
                // if only your opponent mulliganed
                if (playerScript.mulligans.Length == 0)
                {
                    gameManagerReference.otherPlayerHasCompletedMulliganStep = true;
                }
                gameManagerReference.GalleryTitle.text = "Viewing Mulligans";
                gameManagerReference.OnCustomViewOnly(mulligans, GameStateManager.SelectingMode.MulliganView, index != finalIndex, index, finalIndex);
            }
            // if only you mulliganed
            else if (playerScript.mulligans.Length != 0)
            {
                gameManagerReference.coinManager.CoinContainer.SetActive(true);
                gameManagerReference.coinManager.WaitingText.SetActive(true);
                gameManagerReference.coinManager.WaitingText.GetComponent<Text>().text = "Waiting for opponent...";
                gameManagerReference.coinManager.FirstOrSecondButtons.SetActive(false);
                gameManagerReference.coinManager.FirstOrSecondText.SetActive(false);
            }
        }
    }


    [ServerRpc]
    public void RequestShareMulliganInfoServerRpc(ulong id, int index)
    {
        RequestShareMulliganInfoClientRpc(id, index);
    }

    [ClientRpc]
    public void RequestShareMulliganInfoClientRpc(ulong id, int index)
    {
        ulong localId = NetworkManager.Singleton.LocalClientId;
        if (localId != id)
        {
            NetworkManager.Singleton.ConnectedClients.TryGetValue(localId, out var localClient);
            PlayerScript localScript = localClient.PlayerObject.GetComponent<PlayerScript>();

            if (localScript.mulligans.Length > 0)
            {
                localScript.ShareMulliganInfoServerRpc(NetworkManager.Singleton.LocalClientId, localScript.mulligans[index], index, localScript.mulligans.Length - 1);
            }
            else
            {
                localScript.ShareMulliganInfoServerRpc(NetworkManager.Singleton.LocalClientId, null, 0, -1);
            }
        }
    }


    [ServerRpc]
    public void PlayStadiumServerRpc(Card newStadium, ulong playerID, bool toHand = false)
    {
        PlayStadiumClientRpc(newStadium, playerID, toHand);
    }

    [ClientRpc]
    public void PlayStadiumClientRpc(Card newStadium, ulong playerID, bool toHand = false)
    {
        if (gameManagerReference.StadiumOwner == NetworkManager.Singleton.LocalClientId && gameManagerReference.CurrentStadium != null)
        {
            PlayerScript localPlayer = NetworkManager.Singleton.ConnectedClients[NetworkManager.Singleton.LocalClientId].PlayerObject.GetComponent<PlayerScript>();

            if (toHand)
            {
                Card[] newHand = new Card[localPlayer.Hand.Value.Length + 1];
                for (int i = 0; i < localPlayer.Hand.Value.Length; i++)
                {
                    newHand[i] = localPlayer.Hand.Value[i];
                }
                newHand[localPlayer.Hand.Value.Length] = gameManagerReference.CurrentStadium;
                localPlayer.Hand.Value = newHand;
            }
            else
            {
                Card[] newDiscard = new Card[localPlayer.Discard.Value.Length + 1];
                for (int i = 0; i < localPlayer.Discard.Value.Length; i++)
                {
                    newDiscard[i] = localPlayer.Discard.Value[i];
                }
                newDiscard[localPlayer.Discard.Value.Length] = gameManagerReference.CurrentStadium;
                localPlayer.Discard.Value = newDiscard;
            }

        }
        gameManagerReference.StadiumOwner = playerID;
        gameManagerReference.CurrentStadium = newStadium;

        gameManagerReference.StadiumFacingSelf = playerID != NetworkManager.Singleton.LocalClientId;

        RenderStadium();
    }


    [ServerRpc]
    public void FlipStadiumServerRpc()
    {
        FlipStadiumClientRpc();
    }

    [ClientRpc]
    public void FlipStadiumClientRpc()
    {
        gameManagerReference.StadiumFacingSelf = !gameManagerReference.StadiumFacingSelf;
        RenderStadium();
    }

}
