using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.NetworkVariable;
using UnityEngine.UI;

public class CardSection : NetworkBehaviour
{
    [System.NonSerialized]
    public NetworkVariable<Card[]> Bench = new NetworkVariable<Card[]>(
        new NetworkVariableSettings
        {
            WritePermission = NetworkVariablePermission.OwnerOnly,
            ReadPermission = NetworkVariablePermission.Everyone
        },
        new Card[0]
    );

    [System.NonSerialized]
    public NetworkVariable<Card[]> Active = new NetworkVariable<Card[]>(
        new NetworkVariableSettings
        {
            WritePermission = NetworkVariablePermission.OwnerOnly,
            ReadPermission = NetworkVariablePermission.Everyone
        },
        new Card[0]
    );

    [System.NonSerialized]
    public NetworkVariable<Card[][]> BenchAttachments = new NetworkVariable<Card[][]>(
        new NetworkVariableSettings
        {
            WritePermission = NetworkVariablePermission.Everyone,
            ReadPermission = NetworkVariablePermission.Everyone
        },
        new Card[0][]
    );

    [System.NonSerialized]
    public NetworkVariable<Card[][]> ActiveAttachments = new NetworkVariable<Card[][]>(
        new NetworkVariableSettings
        {
            WritePermission = NetworkVariablePermission.Everyone,
            ReadPermission = NetworkVariablePermission.Everyone
        },
        new Card[0][]
    );

    [System.NonSerialized]
    public NetworkVariable<bool[][]> ActiveCardStates = new NetworkVariable<bool[][]>(
        new NetworkVariableSettings
        {
            WritePermission = NetworkVariablePermission.OwnerOnly,
            ReadPermission = NetworkVariablePermission.Everyone
        },
        new bool[0][]
    );

    [System.NonSerialized]
    public NetworkVariable<Card[][]> BenchCardOldEvolutions = new NetworkVariable<Card[][]>(
        new NetworkVariableSettings
        {
            WritePermission = NetworkVariablePermission.OwnerOnly,
            ReadPermission = NetworkVariablePermission.Everyone
        },
        new Card[0][]
    );

    [System.NonSerialized]
    public NetworkVariable<Card[][]> ActiveCardOldEvolutions = new NetworkVariable<Card[][]>(
        new NetworkVariableSettings
        {
            WritePermission = NetworkVariablePermission.OwnerOnly,
            ReadPermission = NetworkVariablePermission.Everyone
        },
        new Card[0][]
    );

    public NetworkVariable<int[]> BenchCounters = new NetworkVariable<int[]>(
        new NetworkVariableSettings
        {
            WritePermission = NetworkVariablePermission.OwnerOnly,
            ReadPermission = NetworkVariablePermission.Everyone
        },
        new int[0]
    );

    public NetworkVariable<int[]> ActiveCounters = new NetworkVariable<int[]>(
        new NetworkVariableSettings
        {
            WritePermission = NetworkVariablePermission.OwnerOnly,
            ReadPermission = NetworkVariablePermission.Everyone
        },
        new int[0]
    );

    public GameObject BenchObj;
    public GameObject ActiveObj;

    private PlayerScript playerRef;

    public void Start()
    {

        void ReRenderBench()
        {
            RenderSection(Bench.Value, BenchObj, GameStateManager.SelectingMode.Bench,
                BenchAttachments, null, BenchCardOldEvolutions, BenchCounters);
        }

        void ReRenderActive()
        {
            RenderSection(Active.Value, ActiveObj, GameStateManager.SelectingMode.Active,
                ActiveAttachments, ActiveCardStates, ActiveCardOldEvolutions, ActiveCounters);
            playerRef.RenderPoisonBurn();
        }

        Bench.OnValueChanged += (Card[] oldValue, Card[] newValue) =>
        {
            if (newValue != null)
            {
                ReRenderBench();
            }
        };

        Active.OnValueChanged += (Card[] oldValue, Card[] newValue) =>
        {
            if (newValue != null)
            {
                ReRenderActive();
            }
        };

        BenchAttachments.OnValueChanged += (Card[][] oldValue, Card[][] newValue) =>
        {
            ReRenderBench();
        };

        ActiveAttachments.OnValueChanged += (Card[][] oldValue, Card[][] newValue) =>
        {
            ReRenderActive();
        };

        //BenchCardStates.OnValueChanged += (bool[][] oldValue, bool[][] newValue) =>
        //{
        //    if (newValue != null)
        //    {
        //        ReRenderBench();
        //    }
        //};

        ActiveCardStates.OnValueChanged += (bool[][] oldValue, bool[][] newValue) =>
        {
            if (newValue != null)
            {
                ReRenderActive();
            }
        };

        BenchCardOldEvolutions.OnValueChanged += (Card[][] oldValue, Card[][] newValue) =>
        {
            if (newValue != null)
            {
                ReRenderBench();
            }
        };

        ActiveCardOldEvolutions.OnValueChanged += (Card[][] oldValue, Card[][] newValue) =>
        {
            if (newValue != null)
            {
                ReRenderActive();
            }
        };

        BenchCounters.OnValueChanged += (int[] oldValue, int[] newValue) =>
        {
            if (newValue != null)
            {
                ReRenderBench();
            }
        };

        ActiveCounters.OnValueChanged += (int[] oldValue, int[] newValue) =>
        {
            if (newValue != null)
            {
                ReRenderActive();
            }
        };

        playerRef = gameObject.GetComponent<PlayerScript>();
    }

    public float GetProperWidth(GameStateManager.SelectingMode mode, bool isLocal)
    {
        if (isLocal)
        {
            if (mode == GameStateManager.SelectingMode.Active)
            {
                return 77f;
            }
            else if (mode == GameStateManager.SelectingMode.Bench)
            {
                return 65f;
            }
            else
            {
                Debug.LogError("confused by selecting mode");
                return 0;
            }
        }
        else
        {
            if (mode == GameStateManager.SelectingMode.Active)
            {
                return 63f;
            }
            else if (mode == GameStateManager.SelectingMode.Bench)
            {
                return 51f;
            }
            else
            {
                Debug.LogError("confused by selecting mode");
                return 0;
            }
        }

    }

    private void RenderSection(Card[] cardlist, GameObject obj,
        GameStateManager.SelectingMode ownType,
        NetworkVariable<Card[][]> attachedCards,
        NetworkVariable<bool[][]> cardStates,
        NetworkVariable<Card[][]> levelInfo = null,
        NetworkVariable<int[]> counterInfo = null)
    {
        var children = new List<GameObject>();
        foreach (Transform child in obj.transform) children.Add(child.gameObject);
        children.ForEach(child => Destroy(child));

        int renderedAttachments = 0;

        for (int i = 0; i < cardlist.Length; i++)
        {
            if (cardlist[i] != null && cardlist[i].art != null)
            {
                GameObject EditingCard = Instantiate(playerRef.CardPrefab, obj.transform);
                EditingCard.name = i.ToString();
                EditingCard.GetComponent<Canvas>().overrideSorting = true;
                EditingCard.GetComponent<CardRightClickHandler>().onRightClick = (Sprite image) =>
                {
                    playerRef.gameManagerReference.OnCardRightClick(image);

                    //playerRef.gameManagerReference.CardCloseupCard.transform.localRotation = Quaternion.Euler(0, 0, 0);
                };

                EditingCard.GetComponent<Canvas>().sortingOrder = 2;
                EditingCard.GetComponent<RectTransform>().sizeDelta = new Vector2(GetProperWidth(ownType, playerRef.IsLocalPlayer), 79.1726f);
                EditingCard.GetComponent<Button>().onClick.AddListener(() =>
                {
                    if (IsLocalPlayer && (GameStateManager.selectingMode == GameStateManager.SelectingMode.None ||
                    GameStateManager.selectingMode == ownType))
                    {
                        if (GameStateManager.selectingMode != ownType)
                        {
                            RenderSectionSelecting(obj, ownType);
                            playerRef.gameManagerReference.RenderCorrectButtons(ownType);
                        }
                        if (EditingCard.GetComponent<Image>().color == CardManipulation.Unselected)
                        {
                            playerRef.gameManagerReference.selectedCards.Add(byte.Parse(EditingCard.name));
                            EditingCard.GetComponent<Image>().color = CardManipulation.Selected;
                        }
                        else if (EditingCard.GetComponent<Image>().color == CardManipulation.Selected)
                        {
                            playerRef.gameManagerReference.selectedCards.Remove(byte.Parse(EditingCard.name));
                            if (playerRef.gameManagerReference.selectedCards.Count < 1)
                            {
                                RenderSectionSelectingCancel(obj);
                                playerRef.gameManagerReference.RenderCorrectButtons(GameStateManager.SelectingMode.None);
                            }
                            else
                            {
                                EditingCard.GetComponent<Image>().color = CardManipulation.Unselected;
                            }
                        }
                    }
                    else if (GameStateManager.selectingMode == GameStateManager.SelectingMode.None || GameStateManager.selectingMode == PlayerScript.ToOpponentVersion(ownType))
                    {

                        if (GameStateManager.selectingMode != PlayerScript.ToOpponentVersion(ownType))
                        {
                            RenderSectionSelecting(obj, PlayerScript.ToOpponentVersion(ownType));
                            playerRef.gameManagerReference.RenderCorrectButtons(GameStateManager.selectingMode);
                        }

                        if (EditingCard.GetComponent<Image>().color == CardManipulation.Unselected)
                        {
                            playerRef.gameManagerReference.selectedCards.Add(byte.Parse(EditingCard.name));
                            EditingCard.GetComponent<Image>().color = CardManipulation.Selected;
                        }
                        else if (EditingCard.GetComponent<Image>().color == CardManipulation.Selected)
                        {
                            playerRef.gameManagerReference.selectedCards.Remove(byte.Parse(EditingCard.name));
                            if (playerRef.gameManagerReference.selectedCards.Count < 1)
                            {
                                RenderSectionSelectingCancel(obj);
                                playerRef.gameManagerReference.RenderCorrectButtons(GameStateManager.SelectingMode.None);
                            }
                            else
                            {
                                EditingCard.GetComponent<Image>().color = CardManipulation.Unselected;
                            }
                        }
                    }
                    else if (GameStateManager.selectingMode == GameStateManager.SelectingMode.Attaching)
                    {
                        LocalDeck from;

                        if (IsLocalPlayer)
                        {
                            from = playerRef.Hand;
                        }
                        else
                        {
                            PlayerScript clientCode = null;
                            foreach (GameObject client in PlayerInfoManager.players)
                            {
                                clientCode = client.GetComponent<PlayerScript>();
                                if (clientCode.IsLocalPlayer)
                                {
                                    break;
                                }
                            }

                            from = clientCode.Hand;
                        }

                        if (playerRef.isInAnimation)
                        {
                            return;
                        };
                        playerRef.isInAnimation = true;

                        Card[] newX = new Card[from.Value.Length - playerRef.gameManagerReference.selectedCards.Count];

                        Card[] newY = new Card[attachedCards.Value[
                            int.Parse(EditingCard.name)].Length + playerRef.gameManagerReference.selectedCards.Count
                        ];
                        Card lastDiscardedCard = null;

                        int i = 0; // tracks total iterations
                        int j = 0; // tracks current position in newHand

                        for (int k = 0; k < attachedCards.Value[int.Parse(EditingCard.name)].Length; k++)
                        {
                            newY[k] = attachedCards.Value[int.Parse(EditingCard.name)][k];
                        }

                        while (i < from.Value.Length)
                        {
                            if (!playerRef.gameManagerReference.selectedCards.Contains((byte)i))
                            {
                                newX[j] = from.Value[i];
                                j++;
                            }
                            else
                            {
                                lastDiscardedCard = from.Value[i];
                                newY[i - j + attachedCards.Value[int.Parse(EditingCard.name)].Length] = from.Value[i];
                            }

                            i++;
                        }

                        if (lastDiscardedCard == null) return;



                        void newCallback()
                        {
                            playerRef.AppendGameLogServerRpc(PlayerInfoManager.Username + ": Attached " +
                                CollectionScript.FileToName(
                                    from.Value[playerRef.gameManagerReference.selectedCards[0]].art
                                ) +
                                (playerRef.gameManagerReference.selectedCards.Count > 1
                                    ? $" and {playerRef.gameManagerReference.selectedCards.Count - 1} others"
                                    : "") +
                                 $" to {CollectionScript.FileToName(cardlist[int.Parse(EditingCard.name)].art)}");
                            from.Value = newX;

                            Card[][] tempAttachedCards = attachedCards.Value;
                            tempAttachedCards[int.Parse(EditingCard.name)] = newY;
                            attachedCards.Value = new Card[0][];
                            attachedCards.Value = tempAttachedCards;

                            if (playerRef.animTempSprite != null)
                            {
                                Destroy(playerRef.animTempSprite);
                            }

                            playerRef.isInAnimation = false;

                            GameStateManager.selectingMode = GameStateManager.SelectingMode.None;
                            playerRef.gameManagerReference.selectedCards = new List<byte>();

                            foreach (GameObject playerClient in PlayerInfoManager.players)
                            {
                                foreach (Transform child in playerClient.GetComponent<PlayerScript>().cardSection.ActiveObj.transform)
                                {
                                    child.gameObject.GetComponent<Image>().color = CardManipulation.Normal;
                                }

                                foreach (Transform child in playerClient.GetComponent<PlayerScript>().cardSection.BenchObj.transform)
                                {
                                    child.gameObject.GetComponent<Image>().color = CardManipulation.Normal;
                                }
                            }


                            playerRef.gameManagerReference.RenderCorrectButtons(GameStateManager.SelectingMode.None);

                        }

                        if (playerRef.gameManagerReference.playerHand != null && EditingCard.transform.GetChild(0).gameObject != null)
                        {
                            playerRef.animTempSprite = Instantiate(playerRef.CardSpritePrefab, playerRef.gameManagerReference.playerHand.transform);
                            playerRef.animTempSprite.transform.rotation = Quaternion.identity;
                            //string query = "Cards/" + ((int)lastDiscardedCard.type).ToString() + "/" + lastDiscardedCard.art + "-01";
                            string query = lastDiscardedCard.art;
                            Sprite[] sprites = Resources.LoadAll<Sprite>(query);
                            playerRef.animTempSprite.GetComponent<SpriteRenderer>().sprite = sprites[0];
                            playerRef.animTempSprite.transform.localScale = new Vector3(10, 10);
                            playerRef.animTempTarget = EditingCard.transform.GetChild(0).gameObject.transform;
                            playerRef.animCallback = newCallback;

                            StartCoroutine(playerRef.MoveSprite());
                        }
                        else
                        {
                            newCallback();
                        }
                    }
                    else if (GameStateManager.selectingMode == GameStateManager.SelectingMode.Evolve)
                    {
                        NetworkVariable<Card[]> section = playerRef.ModeToNetworkDeck(ownType);

                        Card[] newLevelInfo = new Card[levelInfo.Value[int.Parse(EditingCard.name)].Length + 1];
                        for (int j = 0; j < levelInfo.Value[int.Parse(EditingCard.name)].Length; j++)
                        {
                            newLevelInfo[j] = levelInfo.Value[int.Parse(EditingCard.name)][j];
                        }

                        newLevelInfo[levelInfo.Value[int.Parse(EditingCard.name)].Length] = section.Value[int.Parse(EditingCard.name)];

                        Card[] newHand = new Card[playerRef.Hand.Value.Length - 1];
                        int k = 0;
                        for (byte j = 0; j < playerRef.Hand.Value.Length; j++)
                        {
                            if (j != playerRef.gameManagerReference.selectedCards[0])
                            {
                                newHand[k] = playerRef.Hand.Value[j];
                                k++;
                            }
                        }

                        void newCallback()
                        {

                            playerRef.AppendGameLogServerRpc(PlayerInfoManager.Username + ": Evolved from " +
                                CollectionScript.FileToName(cardlist[int.Parse(EditingCard.name)].art) +
                                " to " +
                                CollectionScript.FileToName(
                                    playerRef.Hand.Value[playerRef.gameManagerReference.selectedCards[0]].art)
                                );

                            Card[][] tempLevelInfo = levelInfo.Value;
                            tempLevelInfo[int.Parse(EditingCard.name)] = newLevelInfo;
                            levelInfo.Value = null;
                            levelInfo.Value = tempLevelInfo;

                            if (playerRef.animTempSprite != null)
                            {
                                Destroy(playerRef.animTempSprite);
                            }

                            playerRef.isInAnimation = false;

                            Card[] newSection = section.Value;

                            newSection[int.Parse(EditingCard.name)] = playerRef.Hand.Value[
                                playerRef.gameManagerReference.selectedCards[0]
                            ];
                            section.Value = null;
                            section.Value = newSection;

                            playerRef.Hand.Value = newHand;

                            GameStateManager.selectingMode = GameStateManager.SelectingMode.None;
                            playerRef.gameManagerReference.selectedCards = new List<byte>();

                            foreach (Transform child in BenchObj.transform)
                            {
                                child.gameObject.GetComponent<Image>().color = CardManipulation.Normal;
                            }

                            foreach (Transform child in ActiveObj.transform)
                            {
                                child.gameObject.GetComponent<Image>().color = CardManipulation.Normal;
                            }

                            playerRef.gameManagerReference.RenderCorrectButtons(GameStateManager.SelectingMode.None);
                            playerRef.RenderHand();

                        }

                        if (playerRef.PlayerHand != null &&
                            EditingCard.transform.GetChild(1).gameObject != null)
                        {
                            playerRef.animTempSprite = Instantiate(playerRef.CardSpritePrefab,
                                playerRef.PlayerHand.transform);
                            playerRef.animTempSprite.transform.rotation = Quaternion.identity;
                            //string query = "Cards/" + (
                            //    (int)(playerRef.SpecialDeck.Value[playerRef.gameManagerReference.selectedCards[0]].type)
                            //).ToString() + "/" + playerRef.SpecialDeck.Value[playerRef.gameManagerReference.selectedCards[0]].art + "-01";
                            string query = playerRef.Hand.Value[playerRef.gameManagerReference.selectedCards[0]].art;
                            Sprite[] sprites = Resources.LoadAll<Sprite>(query);
                            playerRef.animTempSprite.GetComponent<SpriteRenderer>().sprite = sprites[0];
                            playerRef.animTempSprite.transform.localScale = new Vector3(10, 10);
                            playerRef.animTempTarget = EditingCard.transform.GetChild(1).gameObject.transform;
                            playerRef.animCallback = newCallback;

                            StartCoroutine(playerRef.MoveSprite());
                        }
                        else
                        {
                            newCallback();
                        }
                    }
                });

                void setSprite()
                {
                    //string query = "Cards/" + ((int)cardlist[i].type).ToString() + "/" + cardlist[i].art + "-01";
                    string query = cardlist[i].art;
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

                if (cardStates != null && cardStates.Value.Length > i)
                {
                    if (cardStates.Value[i][0])
                    {
                        EditingCard.transform.Rotate(new Vector3(0, 0, 90));
                    }
                    else if (cardStates.Value[i][2])
                    {
                        EditingCard.transform.Rotate(new Vector3(0, 0, 180));
                    }
                    else if (cardStates.Value[i][3])
                    {
                        EditingCard.transform.Rotate(new Vector3(0, 0, -90));
                    }

                    //if (cardStates.Value[i][1])
                    //{
                    //    string query = CardManipulation.DefaultCard;
                    //    Sprite[] sprites = Resources.LoadAll<Sprite>(query);
                    //    if (sprites.Length == 1)
                    //    {
                    //        EditingCard.GetComponent<Image>().sprite = sprites[0];
                    //    }
                    //    else
                    //    {
                    //        Debug.LogError($"{query} returned {sprites.Length} results");
                    //    }
                    //}
                    //else
                    //{
                    setSprite();
                    //}

                }
                else
                {
                    setSprite();
                }

                GameObject attachmentSection = Instantiate(playerRef.gameManagerReference.AttachmentPrefab, EditingCard.transform);

                if (attachedCards.Value.Length > i)
                {
                    attachmentSection.GetComponent<Canvas>().overrideSorting = true;
                    attachmentSection.GetComponent<Canvas>().sortingOrder = 3;
                    if (cardStates != null && cardStates.Value.Length > i)
                    {
                        if (cardStates.Value[i][0])
                        {
                            attachmentSection.transform.Rotate(0, 0, -90);
                            attachmentSection.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
                            attachmentSection.GetComponent<RectTransform>().anchorMax = new Vector2(0, 0);
                            attachmentSection.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 77.2f);
                            attachmentSection.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
                            attachmentSection.GetComponent<RectTransform>().localPosition = new Vector2(0, -64.1726f);
                        }
                        else if (cardStates.Value[i][2])
                        {
                            attachmentSection.transform.Rotate(0, 0, -180);
                            attachmentSection.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
                            attachmentSection.GetComponent<RectTransform>().anchorMax = new Vector2(0, 1);
                            attachmentSection.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 77.2f);
                            attachmentSection.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
                            attachmentSection.GetComponent<RectTransform>().localPosition = new Vector2(-70, -30);
                        }
                        else if (cardStates.Value[i][3])
                        {
                            attachmentSection.transform.Rotate(0, 0, 90);
                            attachmentSection.GetComponent<RectTransform>().anchorMin = new Vector2(1, 1);
                            attachmentSection.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
                            attachmentSection.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 77.2f);
                            attachmentSection.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
                            attachmentSection.GetComponent<RectTransform>().localPosition = new Vector2(0, 60);
                        }
                    }

                    RenderAttachments(attachmentSection, attachedCards.Value[i], ownType, renderedAttachments);
                    renderedAttachments += attachedCards.Value[i].Length;
                }

                if (levelInfo != null && levelInfo.Value.Length > i)
                {
                    RenderLevels(EditingCard, levelInfo.Value[i]);
                }

                if (counterInfo != null && counterInfo.Value.Length > i && counterInfo.Value[i] != -1)
                {
                    RenderCounter(EditingCard, counterInfo, counterInfo.Value[i]);
                }

            }
        }

        int cardsInHand = cardlist.Length;
        float cardSize = 67.5f;
        int handRenderSize = System.Math.Min(IsLocalPlayer ? 800 : 600, cardsInHand * 75);
        obj.GetComponent<HorizontalLayoutGroup>().spacing = cardsInHand != 1 ? (-(cardSize * cardsInHand - handRenderSize) / (cardsInHand - 1)) : 0;

    }

    public void RenderCounter(GameObject attachSection, NetworkVariable<int[]> Counters, int counterValue)
    {
        GameObject CounterObj = Instantiate(
            IsLocalPlayer ? playerRef.gameManagerReference.CounterContainerPlayer : playerRef.gameManagerReference.CounterContainerOpp,
            attachSection.transform);
        CounterObj.transform.GetChild(
            IsLocalPlayer ? 1 : 0).GetComponent<Text>().text = counterValue.ToString();

        if (IsLocalPlayer)
        {
            CounterObj.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() =>
            {
                int[] newExtraZoneCounter = Counters.Value;
                Counters.Value = null;
                newExtraZoneCounter[byte.Parse(attachSection.name)]--;
                Counters.Value = newExtraZoneCounter;
            });
            CounterObj.transform.GetChild(2).GetComponent<Button>().onClick.AddListener(() =>
            {
                int[] newExtraZoneCounter = Counters.Value;
                Counters.Value = null;
                newExtraZoneCounter[byte.Parse(attachSection.name)]++;
                Counters.Value = newExtraZoneCounter;
            });
        }
    }

    public void RenderLevels(GameObject attachSection, Card[] levelInfo)
    {
        GameObject LevelObj = Instantiate(playerRef.gameManagerReference.LevelContainer, attachSection.transform);
        LevelObj.GetComponent<Canvas>().overrideSorting = true;
        LevelObj.GetComponent<Canvas>().sortingOrder = 1;

        for (int j = 0; j < levelInfo.Length; j++)
        {
            GameObject thisLevel = Instantiate(playerRef.CardPrefab, LevelObj.transform);

            thisLevel.name = j.ToString();

            //thisLevel.GetComponent<CardRightClickHandler>().onRightClick = playerRef.gameManagerReference.OnCardRightClick;

            thisLevel.GetComponent<CardRightClickHandler>().onRightClick = (Sprite image) =>
            {
                playerRef.gameManagerReference.OnCardRightClick(image);

                playerRef.gameManagerReference.CardCloseupCard.transform.localRotation = Quaternion.Euler(0, 0, 0);
            };

            //string queryAttach = "Cards/" + ((int)levelInfo[j].type).ToString() + "/" + levelInfo[j].art + "-01";
            string queryAttach = levelInfo[j].art;
            Sprite[] spritesAttach = Resources.LoadAll<Sprite>(queryAttach);
            if (spritesAttach.Length == 1)
            {
                thisLevel.GetComponent<Image>().sprite = spritesAttach[0];
            }
            else
            {
                Debug.LogError($"{queryAttach} returned {spritesAttach.Length} results");
            }
        }
    }

    public void RenderAttachments(GameObject attachmentSection, Card[] attachedCards,
        GameStateManager.SelectingMode ParentType, int startingIndex)
    {
        var ownType = ParentType switch
        {
            GameStateManager.SelectingMode.Active => GameStateManager.SelectingMode.AttachedActive,
            GameStateManager.SelectingMode.Bench => GameStateManager.SelectingMode.AttachedBench,
            _ => (GameStateManager.SelectingMode)(-1),
        };

        for (int j = 0; j < attachedCards.Length; j++)
        {
            GameObject attachment = Instantiate(playerRef.CardPrefab, attachmentSection.transform);

            attachment.name = (startingIndex + j).ToString();
            //attachment.GetComponent<CardRightClickHandler>().onRightClick = playerRef.gameManagerReference.OnCardRightClick;
            attachment.GetComponent<CardRightClickHandler>().onRightClick = (Sprite image) =>
             {
                 playerRef.gameManagerReference.OnCardRightClick(image);

                 playerRef.gameManagerReference.CardCloseupCard.transform.localRotation = Quaternion.Euler(0, 0, 0);
             };

            attachment.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (IsLocalPlayer && (GameStateManager.selectingMode == GameStateManager.SelectingMode.None ||
            GameStateManager.selectingMode == ownType))
                {
                    if (GameStateManager.selectingMode != ownType)
                    {

                        foreach (Transform Card in attachmentSection.transform.parent.parent)
                        {
                            foreach (Transform AttachedCard in Card.GetChild(0))
                            {
                                AttachedCard.GetComponent<Image>().color = CardManipulation.Unselected;
                            }
                        }

                        GameStateManager.selectingMode = ownType;

                        playerRef.gameManagerReference.RenderCorrectButtons(ownType);
                    }
                    if (attachment.GetComponent<Image>().color == CardManipulation.Unselected)
                    {
                        playerRef.gameManagerReference.selectedCards.Add(byte.Parse(attachment.name));
                        attachment.GetComponent<Image>().color = CardManipulation.Selected;
                    }
                    else if (attachment.GetComponent<Image>().color == CardManipulation.Selected)
                    {
                        playerRef.gameManagerReference.selectedCards.Remove(byte.Parse(attachment.name));
                        if (playerRef.gameManagerReference.selectedCards.Count < 1)
                        {
                            RenderAttachmentSelectionSelectingCancel(attachmentSection.transform.parent.parent);
                        }
                        else
                        {
                            attachment.GetComponent<Image>().color = CardManipulation.Unselected;
                        }
                    }
                }
            });

            //string queryAttach = "Cards/" + ((int)attachedCards[j].type).ToString() + "/" + attachedCards[j].art + "-01";
            string queryAttach = attachedCards[j].art;
            Sprite[] spritesAttach = Resources.LoadAll<Sprite>(queryAttach);
            if (spritesAttach.Length == 1)
            {
                attachment.GetComponent<Image>().sprite = spritesAttach[0];
                attachment.GetComponent<RectTransform>().sizeDelta = new Vector2(48.6f, 67.8857143f);
            }
            else
            {
                Debug.LogError($"{queryAttach} returned {spritesAttach.Length} results");
            }
        }
    }

    public void RenderAttachmentSelectionSelectingCancel(Transform obj)
    {
        playerRef.gameManagerReference.selectedCards = new List<byte>();
        GameStateManager.selectingMode = GameStateManager.SelectingMode.None;

        foreach (Transform Card in obj)
        {
            foreach (Transform AttachedCard in Card.GetChild(0))
            {
                AttachedCard.GetComponent<Image>().color = CardManipulation.Normal;
            }
        }

        playerRef.gameManagerReference.RenderCorrectButtons(GameStateManager.SelectingMode.None);

    }

    public void RenderSectionSelecting(GameObject obj, GameStateManager.SelectingMode ownType)
    {
        GameStateManager.selectingMode = ownType;
        foreach (Transform child in obj.transform)
        {
            child.gameObject.GetComponent<Image>().color = CardManipulation.Unselected;
        }
    }

    public void RenderSectionSelectingCancel(GameObject obj)
    {
        playerRef.gameManagerReference.selectedCards = new List<byte>();
        GameStateManager.selectingMode = GameStateManager.SelectingMode.None;
        playerRef.gameManagerReference.RenderCorrectButtons(GameStateManager.SelectingMode.None);
        foreach (Transform child in obj.transform)
        {
            child.gameObject.GetComponent<Image>().color = CardManipulation.Normal;
        }
    }
}
