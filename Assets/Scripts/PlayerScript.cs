using System.Collections;
using System.Collections.Generic;
using MLAPI;
using MLAPI.NetworkVariable;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using MLAPI.Messaging;
using System.Linq;

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
        Tap,
        Flip,
        LevelUpStart,
        LevelDown,
        AddCounter,
        ViewTopOfDeck,
        Mill,
        ToBottomOfDeck,
        ToTopOfDeck,
        ToTopOfDeckFromSection,
        RevealTopOfDeck,
        AllowEditTopOfDeck
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
    public NetworkVariable<bool> isActivePlayer = new NetworkVariable<bool>(
        new NetworkVariableSettings
        {
            WritePermission = NetworkVariablePermission.OwnerOnly,
            ReadPermission = NetworkVariablePermission.Everyone
        },
        false
    );

    [System.NonSerialized]
    public NetworkVariable<int> Friendship = new NetworkVariable<int>(
        new NetworkVariableSettings
        {
            WritePermission = NetworkVariablePermission.OwnerOnly,
            ReadPermission = NetworkVariablePermission.Everyone
        },
        0
    );

    [System.NonSerialized]
    public NetworkVariable<int> TempFriendship = new NetworkVariable<int>(
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

    [System.NonSerialized] public Card[] deck = new Card[0];

    [System.NonSerialized] public LocalDeck Deck;

    [System.NonSerialized] public Card[] hand = new Card[0];

    [System.NonSerialized] public LocalDeck Hand;

    public GameObject CardPrefab;

    public GameObject CardSpritePrefab;
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


    [System.NonSerialized] public GameObject PrizeLabel;

    [System.NonSerialized] public CardSection cardSection;

    public void Start()
    {

        Deck = new LocalDeck(deck, () =>
        {
            deckSize.Value = Deck.Value.Length;
            gameManagerReference.RenderDeck(IsLocalPlayer, deckSize.Value);
        });
        Hand = new LocalDeck(hand, () =>
        {
            handSize.Value = Hand.Value.Length;
            RenderHand();
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
        }

        Discard.OnValueChanged += (Card[] previousValue, Card[] newValue) =>
        {
            RenderDiscard();
        };

        LostZone.OnValueChanged += (Card[] previousValue, Card[] newValue) =>
        {
            RenderRemoveFromPlay();
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
        //RenderHand();
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
                        //EditingCard.GetComponent<CardRightClickHandler>().onRightClick =
                        //    gameManagerReference.OnCardRightClick;

                        EditingCard.GetComponent<CardRightClickHandler>().onRightClick = (Sprite image) =>
                         {
                             gameManagerReference.OnCardRightClick(image);

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
                                            child.gameObject.GetComponent<Image>().color = CardManipulation.PossibleMoveTo;
                                        }

                                        foreach (Transform child in playerClient.GetComponent<PlayerScript>().cardSection.ActiveObj.transform)
                                        {
                                            child.gameObject.GetComponent<Image>().color = CardManipulation.PossibleMoveTo;
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
                                if (EditingCard.GetComponent<Image>().color == CardManipulation.Unselected)
                                {
                                    gameManagerReference.selectedCards.Add(byte.Parse(EditingCard.name));
                                    EditingCard.GetComponent<Image>().color = CardManipulation.Selected;
                                }
                                else if (EditingCard.GetComponent<Image>().color == CardManipulation.Selected)
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
                                                    child.gameObject.GetComponent<Image>().color = CardManipulation.Normal;
                                                }
                                                foreach (Transform child in playerCardSection.ActiveObj.transform)
                                                {
                                                    child.gameObject.GetComponent<Image>().color = CardManipulation.Normal;
                                                }
                                            }

                                        }

                                        RenderHandSelectingCancel();
                                        gameManagerReference.RenderCorrectButtons(GameStateManager.SelectingMode.None);
                                    }
                                    else
                                    {
                                        EditingCard.GetComponent<Image>().color = CardManipulation.Unselected;
                                    }
                                }
                            }
                        });

                        //string query = "Cards/" + ((int)Hand.Value[i].type).ToString() + "/" + Hand.Value[i].art + "-01";
                        string query = Hand.Value[i].art;
                        Sprite[] sprites = Resources.LoadAll<Sprite>(query);
                        if (sprites.Length == 1)
                        {
                            EditingCard.GetComponent<Image>().sprite = sprites[0];
                        }
                        else
                        {
                            Debug.LogError($"{query} returned {sprites.Length} results");
                        }
                    }
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
                        EditingCard.GetComponent<Image>().sprite = sprites[0];
                    }
                    else
                    {
                        Debug.LogError($"{query} returned {sprites.Length} results");
                    }
                }
            }

            FormatHandSpacing(0, IsLocalPlayer);

            if (gameManagerReference.selectedCards.Count == 0)
            {
                GameStateManager.selectingMode = GameStateManager.SelectingMode.None;
                gameManagerReference.RenderCorrectButtons(GameStateManager.SelectingMode.None);
                gameManagerReference.selectedCards = new List<byte>();

            }

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
            //query = "Cards/" + ((int)Discard.Value[Discard.Value.Length - 1].type).ToString() + "/" + Discard.Value[Discard.Value.Length - 1].art + "-01";
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
            gameManagerReference.playerDiscardSprite.GetComponent<Image>().sprite = sprites[0];
        }
        else
        {
            gameManagerReference.oppDiscardSprite.GetComponent<CardRightClickHandler>().onRightClick = gameManagerReference.OnCardRightClick;
            gameManagerReference.oppDiscardSprite.GetComponent<Image>().sprite = sprites[0];
        }
    }

    public void RenderRemoveFromPlay()
    {
        string query;
        if (LostZone.Value.Length > 0)
        {
            query = "Cards/" + ((int)LostZone.Value[LostZone.Value.Length - 1].type).ToString() + "/" + LostZone.Value[LostZone.Value.Length - 1].art + "-01";
        }
        else
        {
            query = CardManipulation.DefaultCard;
        }
        Sprite[] sprites = Resources.LoadAll<Sprite>(query);
        if (IsLocalPlayer)
        {
            gameManagerReference.playerLostZoneSprite.GetComponent<CardRightClickHandler>().onRightClick = gameManagerReference.OnCardRightClick;
            gameManagerReference.playerLostZoneSprite.GetComponent<Image>().sprite = sprites[0];
        }
        else
        {
            gameManagerReference.oppLostZoneSprite.GetComponent<CardRightClickHandler>().onRightClick = gameManagerReference.OnCardRightClick;
            gameManagerReference.oppLostZoneSprite.GetComponent<Image>().sprite = sprites[0];
        }
    }

    public void RenderHandSelecting()
    {
        //GameStateManager.selectingMode = GameStateManager.SelectingMode.Hand;
        foreach (Transform child in PlayerHand.transform)
        {
            child.gameObject.GetComponent<Image>().color = CardManipulation.Unselected;
        }
    }

    public void RenderHandSelectingCancel()
    {
        gameManagerReference.selectedCards = new List<byte>();
        GameStateManager.selectingMode = GameStateManager.SelectingMode.None;

        foreach (Transform child in PlayerHand.transform)
        {
            child.gameObject.GetComponent<Image>().color = CardManipulation.Normal;
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
            _ => null,
        };
    }

    public NetworkVariable<Card[]> ModeToNetworkDeck(GameStateManager.SelectingMode mode)
    {
        return mode switch
        {
            GameStateManager.SelectingMode.Active => cardSection.Active,
            GameStateManager.SelectingMode.Bench => cardSection.Bench,
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
            _ => null,
        };
    }

    public NetworkVariable<Card[][]> CheckAttachments(GameStateManager.SelectingMode mode)
    {
        return mode switch
        {
            GameStateManager.SelectingMode.Active => cardSection.ActiveAttachments,
            GameStateManager.SelectingMode.Bench => cardSection.BenchAttachments,
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
            GameStateManager.SelectingMode.Bench => cardSection.BenchCardStates,
            _ => null
        };
    }

    public NetworkVariable<Card[][]> GetLevel(GameStateManager.SelectingMode mode)
    {
        return mode switch
        {
            GameStateManager.SelectingMode.Bench => cardSection.BenchCardOldEvolutions,
            GameStateManager.SelectingMode.Active => cardSection.ActiveCardOldEvolutions,
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

    public void FromXToY(bool isNetworkX, bool isNetworkY, List<byte> selectedIndexes,
        GameObject xObj = null, GameObject yObj = null, bool shuffleOutput = false, bool toTop = false,
        NetworkVariable<Card[]> NetworkX = null, NetworkVariable<Card[]> NetworkY = null,
        LocalDeck localX = null, LocalDeck localY = null,
        NetworkVariable<Card[][]> attachmentsX = null, NetworkVariable<Card[][]> attachmentsY = null,
        NetworkVariable<bool[][]> gameStateX = null, NetworkVariable<bool[][]> gameStateY = null,
        NetworkVariable<Card[][]> levelsX = null, NetworkVariable<Card[][]> levelsY = null,
        NetworkVariable<int[]> countersX = null, NetworkVariable<int[]> countersY = null,
        int numberOfAttachedCardsOnSelectedCards = 0, System.Action additionalCallback = null)
    {

        if (isInAnimation)
        {
            return;
        };
        isInAnimation = true;

        Card[] newX = new Card[(isNetworkX ? NetworkX.Value.Length : localX.Value.Length) - selectedIndexes.Count];

        Card[] newY = new Card[(isNetworkY ? NetworkY.Value.Length : localY.Value.Length)
            + selectedIndexes.Count + numberOfAttachedCardsOnSelectedCards];

        Card[][] newAttachmentsX = new Card[newX.Length][];
        Card[][] newAttachmentsY = new Card[newY.Length][];

        bool[][] newGameStateX = new bool[newX.Length][];
        bool[][] newGameStateY = new bool[newY.Length][];

        Card[][] newLevelsX = new Card[newX.Length][];
        Card[][] newLevelsY = new Card[newY.Length][];

        int[] newCountersX = new int[newX.Length];
        int[] newCountersY = new int[newY.Length];

        Card lastDiscardedCard = null; // this is used only for the animation

        int YOffset = toTop ? selectedIndexes.Count + numberOfAttachedCardsOnSelectedCards : 0;

        for (int k = 0; k < (isNetworkY ? NetworkY.Value.Length : localY.Value.Length); k++)
        {
            newY[k + YOffset] = isNetworkY ? NetworkY.Value[k] : localY.Value[k];
            if (attachmentsY != null)
            {
                newAttachmentsY[k + YOffset] = attachmentsY.Value[k] ?? (new Card[0]);
            }
            if (gameStateY != null)
            {
                newGameStateY[k + YOffset] = new bool[2];
                newGameStateY[k + YOffset][0] = gameStateY.Value[k][0];
                newGameStateY[k + YOffset][1] = gameStateY.Value[k][1];
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
        int attachedCardsOffset = 0;

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
                    newGameStateX[j] = new bool[2];
                    newGameStateX[j][0] = gameStateX.Value[i][0];
                    newGameStateX[j][1] = gameStateX.Value[i][1];
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
                    newGameStateY[index] = new bool[2];

                    if (gameStateX.Value.Length <= i)
                    {
                        newGameStateY[index][0] = false;
                        newGameStateY[index][1] = false;
                    }
                    else
                    {
                        newGameStateY[index][0] = gameStateX.Value[i][0];
                        newGameStateY[index][1] = gameStateX.Value[i][1];
                    }

                }
                else if (gameStateY != null)
                {
                    newGameStateY[index] = new bool[2];
                    newGameStateY[index][0] = false;
                    newGameStateY[index][1] = false;
                }


                if (levelsX != null && levelsY != null)
                {

                    newLevelsY[index] = levelsX.Value[i];
                }
                else if (levelsY != null)
                {
                    newLevelsY[index] = new Card[0];
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
                    child.gameObject.GetComponent<Image>().color = CardManipulation.Normal;
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


            GameStateManager.selectingMode = GameStateManager.SelectingMode.None;
            gameManagerReference.selectedCards = new List<byte>();

            isInAnimation = false;

            RenderHand();

            additionalCallback?.Invoke();
        }

        if (xObj != null && yObj != null)
        {
            animTempSprite = Instantiate(CardSpritePrefab, xObj.transform);
            animTempSprite.transform.rotation = Quaternion.identity;
            //string query = "Cards/" + ((int)lastDiscardedCard.type).ToString() + "/" + lastDiscardedCard.art + "-01";
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
        NetworkVariable<bool[][]> gameStateX = null, NetworkVariable<bool[][]> gameStateY = null)
    {
        FromXToY(false, false, selectedIndexes, xObj, yObj, shuffleOutput, false, null, null, LocalX, LocalY, attachmentsX, attachmentsY,
            gameStateX, gameStateY);
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

            foreach (Transform child in yObj.transform)
            {
                if (child.gameObject.GetComponent<Image>() != null) break;

                child.gameObject.GetComponent<Image>().color = CardManipulation.Normal;
            }

            X.Value = newX;

            //handSize.Value = Hand.Value.Length;
            RenderHand();

            gameManagerReference.RenderCorrectButtons(GameStateManager.SelectingMode.None);

        }

        if (yObj != null && xObj != null)
        {
            animTempSprite = Instantiate(CardSpritePrefab, xObj.transform);
            //string query = "Cards/" + ((int)lastCardMovedFromXToY.type).ToString() + "/" + lastCardMovedFromXToY.art + "-01";
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
        bool shuffle = false, bool toTop = false)
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
            FromXToY(
                !IsLocal(fromMode), !IsLocal(toMode), gameManagerReference.selectedCards, ModeToGameObject(fromMode),
                ModeToGameObject(toMode), shuffle, toTop, ModeToNetworkDeck(fromMode),
                ModeToNetworkDeck(toMode), ModeToLocalDeck(fromMode), ModeToLocalDeck(toMode),
                CheckAttachments(fromMode), CheckAttachments(toMode), GetState(fromMode), GetState(toMode), GetLevel(fromMode),
                GetLevel(toMode), GetCounter(fromMode), GetCounter(toMode),
                CheckAttachments(toMode) == null ? GetNumberOfAttachedCards(fromMode) : 0, callback
            );


            if (fromMode == GameStateManager.SelectingMode.Deck ||
                fromMode == GameStateManager.SelectingMode.Discard ||
                fromMode == GameStateManager.SelectingMode.LostZone)
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
            else if (fromMode == GameStateManager.SelectingMode.Attaching)
            {
                foreach (GameObject client in PlayerInfoManager.players)
                {
                    CardSection playerCardSection = client.GetComponent<PlayerScript>().cardSection;
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



    public IEnumerator MoveSprite()
    {
        while (animTempSprite != null && animTempTarget != null && Vector3.Distance(animTempSprite.transform.position, animTempTarget.position) > 3f)
        {
            animTempSprite.transform.position = Vector3.MoveTowards(animTempSprite.transform.position, animTempTarget.position, 25);
            yield return new WaitForFixedUpdate();
        }
        animCallback();
    }

    public void GameAction(Action action, Card[] extraArgs = null)
    {

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

            Hand.Value = newHandSetup;
            handSize.Value = Hand.Value.Length;

            Card[] newDeckSetup = new Card[Deck.Value.Length - steps];
            for (int i = 0; i < newDeckSetup.Length; i++)
            {
                newDeckSetup[i] = Deck.Value[i];
            }

            Deck.Value = newDeckSetup;

        }
        else if (action == Action.Draw)
        {
            List<byte> cardsToDraw = new List<byte>();

            for (byte i = 0; i < GameStateManager.howMany; i++)
            {
                if (i < Deck.Value.Length) cardsToDraw.Add(i);
            }

            FromXToY(Deck, Hand, cardsToDraw, gameManagerReference.playerDeckSprite, gameManagerReference.playerHand);

        }
        else if (action == Action.Discard)
        {
            FromToWithModes(GameStateManager.selectingMode, GameStateManager.SelectingMode.Discard);
        }
        else if (action == Action.LostZone)
        {
            FromToWithModes(GameStateManager.selectingMode, GameStateManager.SelectingMode.LostZone);
        }
        else if (action == Action.ShuffleIntoDeck)
        {
            FromToWithModes(GameStateManager.selectingMode, GameStateManager.SelectingMode.Deck, true);

        }
        else if (action == Action.Bench)
        {
            FromToWithModes(GameStateManager.selectingMode, GameStateManager.SelectingMode.Bench);
        }
        else if (action == Action.Active)
        {
            FromToWithModes(GameStateManager.selectingMode, GameStateManager.SelectingMode.Active);

        }
        else if (action == Action.ToHand)
        {
            FromToWithModes(GameStateManager.selectingMode, GameStateManager.SelectingMode.Hand);
        }
        else if (action == Action.AttachStart)
        {
            foreach (GameObject playerClient in PlayerInfoManager.players)
            {
                foreach (Transform child in playerClient.GetComponent<PlayerScript>().cardSection.BenchObj.transform)
                {
                    child.gameObject.GetComponent<Image>().color = CardManipulation.PossibleMoveTo;
                }

                foreach (Transform child in playerClient.GetComponent<PlayerScript>().cardSection.ActiveObj.transform)
                {
                    child.gameObject.GetComponent<Image>().color = CardManipulation.PossibleMoveTo;
                }
            }
        }
        else if (action == Action.LevelUpStart)
        {
            if (cardSection.Bench.Value.Length > 0)
            {
                foreach (Transform child in cardSection.BenchObj.transform)
                {
                    child.gameObject.GetComponent<Image>().color = CardManipulation.Unselected;
                }
            }

            if (cardSection.Active.Value.Length > 0)
            {
                foreach (Transform child in cardSection.ActiveObj.transform)
                {
                    child.gameObject.GetComponent<Image>().color = CardManipulation.Unselected;
                }
            }
        }
        else if (action == Action.LevelDown)
        {
            print("commented out leveldown in game action function ");
            //if (cardSection.CardOldLevels.Value[
            //            gameManagerReference.selectedCards[0]
            //        ].Length == 0) return;

            //// send last levelup to Special Deck
            //Card[] newSpecialDeck = new Card[SpecialDeck.Value.Length + 1];
            //for (int i = 0; i < SpecialDeck.Value.Length; i++)
            //{
            //    newSpecialDeck[i] = SpecialDeck.Value[i];
            //}
            //newSpecialDeck[newSpecialDeck.Length - 1] = cardSection.Reserve.Value[gameManagerReference.selectedCards[0]];

            //// switch selected card with it's last levelup
            //Card[] newReserve = cardSection.Reserve.Value;
            //newReserve[gameManagerReference.selectedCards[0]] = cardSection.CardOldLevels.Value[
            //    gameManagerReference.selectedCards[0]
            //][cardSection.CardOldLevels.Value[gameManagerReference.selectedCards[0]].Length - 1];

            //Card[] newLevelUp = new Card[
            //    cardSection.CardOldLevels.Value[
            //        gameManagerReference.selectedCards[0]
            //    ].Length - 1
            //];

            //for (byte i = 0; i < newLevelUp.Length; i++)
            //{
            //    newLevelUp[i] = cardSection.CardOldLevels.Value[gameManagerReference.selectedCards[0]][i];
            //}

            //// set values
            //cardSection.CardOldLevels.Value[gameManagerReference.selectedCards[0]] = newLevelUp;

            //cardSection.Reserve.Value = null;
            //cardSection.Reserve.Value = newReserve;

            //SpecialDeck.Value = newSpecialDeck;

            //gameManagerReference.selectedCards = new List<byte>();
            //GameStateManager.selectingMode = GameStateManager.SelectingMode.None;
            //gameManagerReference.RenderCorrectButtons(GameStateManager.SelectingMode.None);
        }
        else if (action == Action.Tap)
        {
            if (gameManagerReference.selectedCards.Count < 1) return;

            GameObject tapObj = ModeToGameObject(GameStateManager.selectingMode);
            NetworkVariable<bool[][]> tapStates = GetState(GameStateManager.selectingMode);
            bool[][] newTapStates = tapStates.Value;

            for (byte i = 0; i < tapObj.transform.childCount; i++)
            {
                if (gameManagerReference.selectedCards.Contains(i))
                {
                    newTapStates[i][0] = !newTapStates[i][0];
                }
            }

            tapStates.Value = null;
            tapStates.Value = newTapStates;
            cardSection.RenderSectionSelectingCancel(tapObj);
        }
        else if (action == Action.Flip)
        {
            GameObject flipObj = ModeToGameObject(GameStateManager.selectingMode);
            NetworkVariable<bool[][]> flipStates = GetState(GameStateManager.selectingMode);
            bool[][] newFlipStates = flipStates.Value;

            for (byte i = 0; i < flipObj.transform.childCount; i++)
            {
                if (gameManagerReference.selectedCards.Contains(i))
                {
                    newFlipStates[i][1] = !newFlipStates[i][1];
                }
            }

            flipStates.Value = null;
            flipStates.Value = newFlipStates;
            cardSection.RenderSectionSelectingCancel(flipObj);
        }
        else if (action == Action.AddCounter)
        {
            if (GameStateManager.selectingMode == GameStateManager.SelectingMode.Bench)
            {
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
            for (int i = 0; i < GameStateManager.howMany; i++)
            {
                gameManagerReference.selectedCards.Add((byte)i);
            }

            FromToWithModes(GameStateManager.SelectingMode.Deck, GameStateManager.SelectingMode.Discard);
        }
        else if (action == Action.ToBottomOfDeck)
        {
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
            if (GameStateManager.viewingMode == GameStateManager.SelectingMode.DeckSection)
            {
                ToTopOfDeckFromSection();
            }
            else
            {
                FromToWithModes(GameStateManager.selectingMode, GameStateManager.SelectingMode.Deck, false, true);
            }

        }
        else
        {
            Debug.LogError("no action provided");
        }

    }



    public void ManageStartOfTurn()
    {
        if (AutoDraw)
        {
            GameStateManager.howMany = 1;
            GameAction(Action.Draw);
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
        player.Friendship.Value = System.Math.Min(player.Friendship.Value + 1, 10);
        player.TempFriendship.Value = player.Friendship.Value;
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
    public void GiveTurnInfoClientRpc(int info, bool autoDraw, bool autoUntap)
    {
        NetworkManager.Singleton.ConnectedClients.TryGetValue(NetworkManager.Singleton.LocalClientId, out var client);
        client.PlayerObject.GetComponent<PlayerScript>().AutoDraw = autoDraw;
        client.PlayerObject.GetComponent<PlayerScript>().AutoUntap = autoUntap;
        client.PlayerObject.GetComponent<PlayerScript>().FirstTurnInfo = info;
        GetTurnInfo();
    }

    public void GetTurnInfo()
    {

        NetworkManager.Singleton.ConnectedClients.TryGetValue(NetworkManager.Singleton.LocalClientId, out var client);

        int info = client.PlayerObject.GetComponent<PlayerScript>().FirstTurnInfo;
        if (!HasStarted || info == -3)
        {

            if (info == -3)
            {
                Debug.LogError("info == -3");
            }

            Invoke(nameof(GetTurnInfo), 0.5f);
            return;
        }

        if (info == -2)
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

            if (IsServer)
            {
                if (!heads && result == 1 || heads && result == 0) // the host was correct
                {
                    gameManagerReference.coinManager.FirstOrSecondText.SetActive(true);
                    gameManagerReference.coinManager.FirstOrSecondButtons.SetActive(true);
                }
                else
                {
                    gameManagerReference.coinManager.WaitingText.GetComponent<Text>().text = "Waiting for opponent to select first or second.";
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
                    gameManagerReference.coinManager.WaitingText.GetComponent<Text>().text = "Waiting for opponent to select first or second.";
                    gameManagerReference.coinManager.WaitingText.SetActive(true);
                }
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
        clientScript.RenderTurnInfo();
        clientScript.Friendship.Value = 1;
        clientScript.TempFriendship.Value = 1;
        //clientScript.gameManagerReference.OnSpecialDeckView();
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
        playerScript.Friendship.Value = 0;
        playerScript.TempFriendship.Value = 0;

        gameManagerReference.selectedCards = new List<byte>();
        GameStateManager.selectingMode = GameStateManager.SelectingMode.None;

        playerScript.cardSection.Bench.Value = new Card[0];
        playerScript.cardSection.Active.Value = new Card[0];

        playerScript.cardSection.BenchAttachments.Value = new Card[0][];
        playerScript.cardSection.ActiveAttachments.Value = new Card[0][];

        playerScript.cardSection.BenchCardStates.Value = new bool[0][];
        playerScript.cardSection.ActiveCardStates.Value = new bool[0][];

        playerScript.cardSection.BenchCounters.Value = new int[0];
        playerScript.cardSection.ActiveCounters.Value = new int[0];

        playerScript.cardSection.BenchCardOldEvolutions.Value = new Card[0][];
        playerScript.cardSection.ActiveCardOldEvolutions.Value = new Card[0][];

        playerScript.GameAction(Action.Setup, CardManipulation.Shuffle(PlayerInfoManager.fullDeck));

        gameManagerReference.gameSettingsManager.RematchPanel.SetActive(false);
        gameManagerReference.gameSettingsManager.SettingsMenu.SetActive(false);
        gameManagerReference.gameSettingsManager.VictoryScreen.SetActive(false);
        gameManagerReference.gameSettingsManager.DefeatScreen.SetActive(false);

        playerScript.IsReadyForRematch.Value = false;


        playerScript.isActivePlayer.Value = false;

        // manage coin flip
        playerScript.GetTurnInfo();

        gameManagerReference.RenderCorrectButtons(GameStateManager.SelectingMode.None);

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


    IEnumerator RollDie(int seed)
    {
        gameManagerReference.coinManager.DieContainer.SetActive(true);

        int speed = 15;

        int uniqueTrajectories = 10;
        int framePerTrajectory = 200 / uniqueTrajectories;

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

}
