/*
 
 void ConfirmDiscard()
    {
        if (isInAnimation) return;

        isInAnimation = true;
        Card[] newHand = new Card[Hand.Value.Length - selectedCards.Count];
        Card[] newDiscard = new Card[Discard.Value.Length + selectedCards.Count];
        Card lastDiscardedCard = null;

        int i = 0; // tracks total iterations
        int j = 0; // tracks current position in newHand

        for (int k = 0; k < Discard.Value.Length; k++)
        {
            newDiscard[k] = Discard.Value[k];
        }

        while (i < Hand.Value.Length)
        {
            if (!selectedCards.Contains((byte)i))
            {
                newHand[j] = Hand.Value[i];
                j++;
            }
            else
            {
                lastDiscardedCard = Hand.Value[i];
                newDiscard[i - j + Discard.Value.Length] = Hand.Value[i];
            }

            i++;
        }

        if (lastDiscardedCard == null) return;

        Hand.Value = newHand;
        //RenderHand();

        animTempSprite = Instantiate(CardSpritePrefab, PlayerHand.transform);
        string query = "Cards/" + ((int)lastDiscardedCard.type).ToString() + "/" + lastDiscardedCard.art + "-01";
        Sprite[] sprites = Resources.LoadAll<Sprite>(query);
        animTempSprite.GetComponent<SpriteRenderer>().sprite = sprites[0];
        animTempSprite.transform.localScale = new Vector3(10, 10);
        animTempTarget = gameManagerReference.playerDiscardSprite.transform;
        animCallback = () =>
        {
            if (selectedCards.Count < 1)
            {
                foreach (Transform child in PlayerHand.transform)
                {
                    child.gameObject.GetComponent<Image>().color = CardManipulation.Normal;
                }
            }

            isSelecting = false;
            selectedCards = new List<byte>();
            isInAnimation = false;
            Discard.Value = newDiscard;

        };

        StartCoroutine("MoveSprite");

    }

    void ConfirmDraw()
    {
        if (isInAnimation) return;

        if (Deck.Value.Length > 0)
        {
            isInAnimation = true;
            Card drawnCard = Deck.Value[Deck.Value.Length - 1];

            Card[] newHand = new Card[Hand.Value.Length + 1];
            for (int i = 0; i < Hand.Value.Length; i++)
            {
                newHand[i] = Hand.Value[i];
            }
            newHand[newHand.Length - 1] = drawnCard;

            Card[] newDeck = new Card[Deck.Value.Length - 1];
            for (int i = 0; i < newDeck.Length; i++)
            {
                newDeck[i] = Deck.Value[i];
            }


            //instantiate placeholder card
            GameObject EditingCard = Instantiate(CardPrefab, PlayerHand.transform);
            EditingCard.GetComponent<Button>().interactable = false;
            string query = "Cards/" + ((int)newHand[newHand.Length - 1].type).ToString() + "/" + newHand[newHand.Length - 1].art + "-01";
            Sprite[] sprites = Resources.LoadAll<Sprite>(query);
            if (PlayerHand != null)
            {
                if (sprites.Length == 1)
                {
                    EditingCard.GetComponent<Image>().sprite = sprites[0];
                }
                else
                {
                    Debug.LogError($"{query} returned {sprites.Length} results");
                }
                EditingCard.GetComponent<Image>().color = CardManipulation.Placeholder;
            }
            else
            {
                Debug.LogError("playerhand is null");
            }
            FormatHandSpacing(1);

            // instantiate sprite
            GameObject editingSprite = Instantiate(CardSpritePrefab, gameManagerReference.playerDeckSprite.transform);
            editingSprite.GetComponent<SpriteRenderer>().color = CardManipulation.Normal;
            editingSprite.GetComponent<SpriteRenderer>().sprite = sprites[0];

            // move instantiated sprite on top of the newly rendered card
            animTempSprite = editingSprite;
            animTempTarget = EditingCard.transform;
            StartCoroutine("MoveSprite");
            animCallback = () =>
            {
                Destroy(editingSprite);
                Destroy(EditingCard);
                Hand.Value = newHand;
                Deck.Value = newDeck;
            };
        }
    }

    void ConfirmShuffleToDeck()
    {
        if (isInAnimation) return;

        isInAnimation = true;

        Card[] newDeck = new Card[Deck.Value.Length + selectedCards.Count];
        for (int i = 0; i < Deck.Value.Length; i++)
        {
            newDeck[i] = Deck.Value[i];
        }
        for (int i = 0; i < selectedCards.Count; i++)
        {
            newDeck[i + Deck.Value.Length] = Hand.Value[selectedCards[i]];
        }

        Card[] newHand = new Card[Hand.Value.Length - selectedCards.Count];
        int j = 0;
        for (int i = 0; i < Hand.Value.Length; i++)
        {
            if (!selectedCards.Contains((byte)i))
            {
                newHand[j] = Hand.Value[i];
                j++;
            }
        }

        Card lastShuffledCard = Hand.Value[selectedCards[selectedCards.Count - 1]];

        animTempSprite = Instantiate(CardSpritePrefab, PlayerHand.transform);
        string query = "Cards/" + ((int)lastShuffledCard.type).ToString() + "/" + lastShuffledCard.art + "-01";
        Sprite[] sprites = Resources.LoadAll<Sprite>(query);
        animTempSprite.GetComponent<SpriteRenderer>().sprite = sprites[0];
        animTempSprite.transform.localScale = new Vector3(10, 10);
        animTempTarget = gameManagerReference.playerDeckSprite.transform;
        animCallback = () =>
        {
            if (selectedCards.Count < 1)
            {
                foreach (Transform child in PlayerHand.transform)
                {
                    Debug.LogError(child);
                    Debug.LogError(child.gameObject);
                    Debug.LogError(child.gameObject.GetComponent<Image>());
                    Debug.LogError(child.gameObject.GetComponent<Image>().color);
                    child.gameObject.GetComponent<Image>().color = CardManipulation.Normal;
                }
            }

            isSelecting = false;
            selectedCards = new List<byte>();
            isInAnimation = false;
            Hand.Value = newHand;
            Deck.Value = CardManipulation.Shuffle(newDeck);
        };

        StartCoroutine("MoveSprite");
    }

        void ConfirmToReserve()
    {

        if (selectedCards.Count < 1)
        {
            foreach (Transform child in PlayerHand.transform)
            {
                child.gameObject.GetComponent<Image>().color = CardManipulation.Normal;
            }
            isInAnimation = false;
            isSelecting = false;
            return;
        }

        if (isInAnimation) return;

        isInAnimation = true;

        Card[] newSection = new Card[cardSection.Reserve.Value.Length + selectedCards.Count];
        for (int i = 0; i < cardSection.Reserve.Value.Length; i++)
        {
            newSection[i] = cardSection.Reserve.Value[i];
        }
        for (int i = 0; i < selectedCards.Count; i++)
        {
            newSection[i + cardSection.Reserve.Value.Length] = Hand.Value[selectedCards[i]];
        }

        cardSection.Reserve.Value = newSection;

        Card[] newHand = new Card[Hand.Value.Length - selectedCards.Count];
        int j = 0;
        for (int i = 0; i < Hand.Value.Length; i++)
        {
            if (!selectedCards.Contains((byte)i))
            {
                newHand[j] = Hand.Value[i];
                j++;
            }
        }


        Card lastCardSelected = Hand.Value[selectedCards[selectedCards.Count - 1]];

        animTempSprite = Instantiate(CardSpritePrefab, PlayerHand.transform);
        string query = "Cards/" + ((int)lastCardSelected.type).ToString() + "/" + lastCardSelected.art + "-01";
        Sprite[] sprites = Resources.LoadAll<Sprite>(query);
        animTempSprite.GetComponent<SpriteRenderer>().sprite = sprites[0];
        animTempSprite.transform.localScale = new Vector3(10, 10);
        animTempTarget = gameManagerReference.LocalReserve.transform;
        animCallback = () =>
        {
            if (selectedCards.Count < 1)
            {
                foreach (Transform child in PlayerHand.transform)
                {
                    child.gameObject.GetComponent<Image>().color = CardManipulation.Normal;
                }
            }

            Hand.Value = newHand;
            isSelecting = false;
            selectedCards = new List<byte>();
            isInAnimation = false;
        };

        StartCoroutine("MoveSprite");

    }

    void ConfirmToBattlefield()
    {
        if (selectedCards.Count < 1)
        {
            foreach (Transform child in PlayerHand.transform)
            {
                child.gameObject.GetComponent<Image>().color = CardManipulation.Normal;
            }
            isInAnimation = false;
            isSelecting = false;
            return;
        }

        if (isInAnimation) return;

        isInAnimation = true;

        Card[] newSection = new Card[cardSection.Battlefield.Value.Length + selectedCards.Count];
        for (int i = 0; i < cardSection.Battlefield.Value.Length; i++)
        {
            newSection[i] = cardSection.Battlefield.Value[i];
        }
        for (int i = 0; i < selectedCards.Count; i++)
        {
            newSection[i + cardSection.Battlefield.Value.Length] = Hand.Value[selectedCards[i]];
        }

        cardSection.Battlefield.Value = newSection;

        Card[] newHand = new Card[Hand.Value.Length - selectedCards.Count];
        int j = 0;
        for (int i = 0; i < Hand.Value.Length; i++)
        {
            if (!selectedCards.Contains((byte)i))
            {
                newHand[j] = Hand.Value[i];
                j++;
            }
        }

        Card lastCardSelected = Hand.Value[selectedCards[selectedCards.Count - 1]];

        animTempSprite = Instantiate(CardSpritePrefab, PlayerHand.transform);
        string query = "Cards/" + ((int)lastCardSelected.type).ToString() + "/" + lastCardSelected.art + "-01";
        Sprite[] sprites = Resources.LoadAll<Sprite>(query);
        animTempSprite.GetComponent<SpriteRenderer>().sprite = sprites[0];
        animTempSprite.transform.localScale = new Vector3(10, 10);
        animTempTarget = gameManagerReference.LocalBattlefield.transform;

        animCallback = () =>
        {
            Hand.Value = newHand;
            isSelecting = false;
            selectedCards = new List<byte>();
            isInAnimation = false;
        };

        StartCoroutine("MoveSprite");
    } 

        //public void OnGalllerySelectConfirm()
    //{
        //PlayerScript clientCode = null;

        //foreach (GameObject client in PlayerInfoManager.players)
        //{
        //    clientCode = client.GetComponent<PlayerScript>();
        //    if (clientCode.IsLocalPlayer)
        //    {
        //        break;
        //    }
        //}

        ////switch (GallerySelectingMode)
        //switch (selectingMode)
        //{
        //    case SelectingMode.Hand:
        //        Card[] newHand = new Card[clientCode.Hand.Value.Length + selectedCards.Count];
        //        for (int i = 0; i < clientCode.Hand.Value.Length; i++)
        //        {
        //            newHand[i] = clientCode.Hand.Value[i];
        //        }
        //        for (int i = 0; i < selectedCards.Count; i++)
        //        {
        //            //newHand[i + clientCode.Hand.Value.Length] = GalleryViewingMode == SelectingMode.Deck ? clientCode.Deck.Value[selectedCards[i]] : clientCode.Discard.Value[selectedCards[i]];
        //            newHand[i + clientCode.Hand.Value.Length] = viewingMode == SelectingMode.Deck ? clientCode.Deck.Value[selectedCards[i]] : clientCode.Discard.Value[selectedCards[i]];
        //        }
        //        clientCode.Hand.Value = newHand;

        //        List<Card> handNewDeck = new List<Card>();
        //        //int toHandDeckLen = GalleryViewingMode == SelectingMode.Deck ? clientCode.Deck.Value.Length : clientCode.Discard.Value.Length;
        //        int toHandDeckLen = viewingMode == SelectingMode.Deck ? clientCode.Deck.Value.Length : clientCode.Discard.Value.Length;
        //        for (int i = 0; i < toHandDeckLen; i++)
        //        {
        //            if (!selectedCards.Contains((byte)i))
        //            {
        //                handNewDeck.Add(
        //                    //GalleryViewingMode == SelectingMode.Deck ? clientCode.Deck.Value[i] : clientCode.Discard.Value[i]
        //                    viewingMode == SelectingMode.Deck ? clientCode.Deck.Value[i] : clientCode.Discard.Value[i]
        //                    );
        //            }
        //        }

        //        //if (GalleryViewingMode == SelectingMode.Deck)
        //        if (viewingMode == SelectingMode.Deck)
        //        {
        //            clientCode.Deck.Value = handNewDeck.ToArray();
        //        }
        //        else
        //        {
        //            clientCode.Discard.Value = handNewDeck.ToArray();
        //        }

        //        break;
        //    case SelectingMode.Discard:
        //        Card[] newDiscard = new Card[clientCode.Discard.Value.Length + selectedCards.Count];
        //        for (int i = 0; i < clientCode.Discard.Value.Length; i++)
        //        {
        //            newDiscard[i] = clientCode.Discard.Value[i];
        //        }
        //        for (int i = 0; i < selectedCards.Count; i++)
        //        {
        //            newDiscard[i + clientCode.Discard.Value.Length] = clientCode.Deck.Value[selectedCards[i]];
        //        }
        //        clientCode.Discard.Value = newDiscard;

        //        List<Card> discard_NewDeck = new List<Card>();

        //        for (int i = 0; i < clientCode.Deck.Value.Length; i++)
        //        {
        //            if (!selectedCards.Contains((byte)i))
        //            {
        //                discard_NewDeck.Add(clientCode.Deck.Value[i]);
        //            }
        //        }

        //        clientCode.Deck.Value = discard_NewDeck.ToArray();

        //        break;
        //    case SelectingMode.Deck:
        //        Card[] newDeck = new Card[clientCode.Deck.Value.Length + selectedCards.Count];
        //        for (int i = 0; i < clientCode.Deck.Value.Length; i++)
        //        {
        //            newDeck[i] = clientCode.Deck.Value[i];
        //        }
        //        for (int i = 0; i < selectedCards.Count; i++)
        //        {
        //            newDeck[i + clientCode.Deck.Value.Length] = clientCode.Discard.Value[selectedCards[i]];
        //        }
        //        clientCode.Deck.Value = CardManipulation.Shuffle(newDeck);

        //        List<Card> deck_NewDiscard = new List<Card>();

        //        for (int i = 0; i < clientCode.Discard.Value.Length; i++)
        //        {
        //            if (!selectedCards.Contains((byte)i))
        //            {
        //                deck_NewDiscard.Add(clientCode.Discard.Value[i]);
        //            }
        //        }

        //        clientCode.Discard.Value = deck_NewDiscard.ToArray();

        //        break;
        //    default:
        //        break;
        //}

        ////if (GalleryViewingMode == SelectingMode.Deck)
        //if (viewingMode == SelectingMode.Deck)
        //{
        //    OnShuffle();
        //}
        //RenderCorrectButtons(SelectingMode.None);
        ////GalleryView.SetActive(false);
        ////mainButtonsPanel.SetActive(true);
        //selectedCards = new List<byte>();
    //}

    private void OnGallerySelectButton(SelectingMode mode)
    {

        //RenderCorrectButtons(mode);
        ////GallerySelectingMode = mode;
        //selectingMode = mode;

        //var children = new List<GameObject>();
        //foreach (Transform child in GalleryContent.transform) children.Add(child.gameObject);

        //for (int i = 0; i < children.Count; i++)
        //{
        //    children[i].GetComponent<Image>().color = CardManipulation.Unselected;
        //    byte index = (byte)i;
        //    children[i].GetComponent<Button>().onClick.RemoveAllListeners();
        //    children[i].GetComponent<Button>().onClick.AddListener(() =>
        //    {
        //        if (children[index].GetComponent<Image>().color == CardManipulation.Unselected)
        //        {
        //            children[index].GetComponent<Image>().color = CardManipulation.Selected;
        //            selectedCards.Add(index);
        //        }
        //        else if (children[index].GetComponent<Image>().color == CardManipulation.Selected)
        //        {
        //            children[index].GetComponent<Image>().color = CardManipulation.Unselected;
        //            selectedCards.Remove(index);
        //        }
        //    });
        //}

    }

//private void RenderReserve()
    //{
    //    var children = new List<GameObject>();
    //    foreach (Transform child in ReserveObj.transform) children.Add(child.gameObject);
    //    children.ForEach(child => Destroy(child));

    //    for (int i = 0; i < Reserve.Value.Length; i++)
    //    {
    //        if (Reserve.Value[i] != null && Reserve.Value[i].art != null)
    //        {
    //            GameObject EditingCard = Instantiate(playerRef.CardPrefab, ReserveObj.transform);
    //            EditingCard.GetComponent<CardRightClickHandler>().onRightClick = playerRef.gameManagerReference.OnCardRightClick;

    //            EditingCard.name = i.ToString();
    //            EditingCard.GetComponent<Button>().onClick.AddListener(() =>
    //            {
    //                if (IsLocalPlayer && (GameStateManager.selectingMode == GameStateManager.SelectingMode.None ||
    //                GameStateManager.selectingMode == GameStateManager.SelectingMode.Reserve))
    //                {
    //                    if (GameStateManager.selectingMode != GameStateManager.SelectingMode.Reserve)
    //                    {
    //                        RenderReserveSelecting();
    //                        playerRef.gameManagerReference.RenderCorrectButtons(GameStateManager.SelectingMode.Reserve);
    //                    }
    //                    if (EditingCard.GetComponent<Image>().color == CardManipulation.Unselected)
    //                    {
    //                        playerRef.gameManagerReference.selectedCards.Add(byte.Parse(EditingCard.name));
    //                        EditingCard.GetComponent<Image>().color = CardManipulation.Selected;
    //                    }
    //                    else if (EditingCard.GetComponent<Image>().color == CardManipulation.Selected)
    //                    {
    //                        playerRef.gameManagerReference.selectedCards.Remove(byte.Parse(EditingCard.name));
    //                        if (playerRef.gameManagerReference.selectedCards.Count < 1)
    //                        {
    //                            RenderReserveSelectingCancel();
    //                            playerRef.gameManagerReference.RenderCorrectButtons(GameStateManager.SelectingMode.None);
    //                        }
    //                        else
    //                        {
    //                            EditingCard.GetComponent<Image>().color = CardManipulation.Unselected;
    //                        }
    //                    }
    //                }
    //            });

    //            string query = "Cards/" + ((int)Reserve.Value[i].type).ToString() + "/" + Reserve.Value[i].art + "-01";
    //            Sprite[] sprites = Resources.LoadAll<Sprite>(query);
    //            if (sprites.Length == 1)
    //            {
    //                EditingCard.GetComponent<Image>().sprite = sprites[0];
    //            }
    //            else
    //            {
    //                Debug.LogError($"{query} returned {sprites.Length} results");
    //            }
    //        }
    //    }

    //    int cardsInHand = Reserve.Value.Length;
    //    float cardSize = 77.19f;
    //    int handRenderSize = System.Math.Min(800, cardsInHand * 75);
    //    ReserveObj.GetComponent<HorizontalLayoutGroup>().spacing = cardsInHand != 1 ? (-(cardSize * cardsInHand - handRenderSize) / (cardsInHand - 1)) : 0;

    //}

    //private void RenderReserveSelecting()
    //{
    //    GameStateManager.selectingMode = GameStateManager.SelectingMode.Reserve;
    //    foreach (Transform child in ReserveObj.transform)
    //    {
    //        child.gameObject.GetComponent<Image>().color = CardManipulation.Unselected;
    //    }
    //}

    //public void RenderReserveSelectingCancel()
    //{
    //    playerRef.gameManagerReference.selectedCards = new List<byte>();
    //    GameStateManager.selectingMode = GameStateManager.SelectingMode.None;
    //    foreach (Transform child in ReserveObj.transform)
    //    {
    //        child.gameObject.GetComponent<Image>().color = CardManipulation.Normal;
    //    }
    //}

    //private void RenderBattlefield()
    //{
    //    var children = new List<GameObject>();
    //    foreach (Transform child in BattlefieldObj.transform) children.Add(child.gameObject);
    //    children.ForEach(child => Destroy(child));

    //    for (int i = 0; i < Battlefield.Value.Length; i++)
    //    {
    //        if (Battlefield.Value[i] != null && Battlefield.Value[i].art != null)
    //        {
    //            GameObject EditingCard = Instantiate(playerRef.CardPrefab, BattlefieldObj.transform);
    //            EditingCard.GetComponent<CardRightClickHandler>().onRightClick = playerRef.gameManagerReference.OnCardRightClick;

    //            EditingCard.name = i.ToString();
    //            EditingCard.GetComponent<Button>().onClick.AddListener(() =>
    //            {
    //                if (IsLocalPlayer && (GameStateManager.selectingMode == GameStateManager.SelectingMode.None ||
    //                GameStateManager.selectingMode == GameStateManager.SelectingMode.Battlefield))
    //                {
    //                    if (GameStateManager.selectingMode != GameStateManager.SelectingMode.Battlefield)
    //                    {
    //                        RenderBattlefieldSelecting();
    //                        playerRef.gameManagerReference.RenderCorrectButtons(GameStateManager.SelectingMode.Battlefield);
    //                    }
    //                    if (EditingCard.GetComponent<Image>().color == CardManipulation.Unselected)
    //                    {
    //                        playerRef.gameManagerReference.selectedCards.Add(byte.Parse(EditingCard.name));
    //                        EditingCard.GetComponent<Image>().color = CardManipulation.Selected;
    //                    }
    //                    else if (EditingCard.GetComponent<Image>().color == CardManipulation.Selected)
    //                    {
    //                        playerRef.gameManagerReference.selectedCards.Remove(byte.Parse(EditingCard.name));
    //                        if (playerRef.gameManagerReference.selectedCards.Count < 1)
    //                        {
    //                            RenderBattlefieldSelectingCancel();
    //                            playerRef.gameManagerReference.RenderCorrectButtons(GameStateManager.SelectingMode.None);
    //                        }
    //                        else
    //                        {
    //                            EditingCard.GetComponent<Image>().color = CardManipulation.Unselected;
    //                        }
    //                    }
    //                }
    //            });

    //            string query = "Cards/" + ((int)Battlefield.Value[i].type).ToString() + "/" + Battlefield.Value[i].art + "-01";
    //            Sprite[] sprites = Resources.LoadAll<Sprite>(query);
    //            if (sprites.Length == 1)
    //            {
    //                EditingCard.GetComponent<Image>().sprite = sprites[0];
    //            }
    //            else
    //            {
    //                Debug.LogError($"{query} returned {sprites.Length} results");
    //            }
    //        }
    //    }

    //    int cardsInHand = Battlefield.Value.Length;
    //    float cardSize = 77.19f;
    //    int handRenderSize = System.Math.Min(800, cardsInHand * 75);
    //    BattlefieldObj.GetComponent<HorizontalLayoutGroup>().spacing = cardsInHand != 1 ? (-(cardSize * cardsInHand - handRenderSize) / (cardsInHand - 1)) : 0;
    //}

    //private void RenderBattlefieldSelecting()
    //{
    //    GameStateManager.selectingMode = GameStateManager.SelectingMode.Battlefield;
    //    foreach (Transform child in BattlefieldObj.transform)
    //    {
    //        child.gameObject.GetComponent<Image>().color = CardManipulation.Unselected;
    //    }
    //}

    //public void RenderBattlefieldSelectingCancel()
    //{
    //    playerRef.gameManagerReference.selectedCards = new List<byte>();
    //    GameStateManager.selectingMode = GameStateManager.SelectingMode.None;
    //    foreach (Transform child in BattlefieldObj.transform)
    //    {
    //        child.gameObject.GetComponent<Image>().color = CardManipulation.Normal;
    //    }
    //}

    //public void FromXToY(NetworkVariable<Card[]> NetworkX,
    //    NetworkVariable<Card[]> NetworkY, List<byte> selectedIndexes, GameObject xObj = null, GameObject yObj = null,
    //    bool shuffleOutput = false, NetworkVariable<Card[][]> attachmentsX = null, NetworkVariable<Card[][]> attachmentsY = null,
    //    NetworkVariable<bool[][]> gameStateX = null, NetworkVariable<bool[][]> gameStateY = null)
    //{
    //    FromXToY(true, true, selectedIndexes, xObj, yObj, shuffleOutput, NetworkX, NetworkY, null, null, attachmentsX,
    //        attachmentsY, gameStateX, gameStateY);
    //}

    //public void FromXToY(NetworkVariable<Card[]> NetworkX,
    //    LocalDeck LocalY, List<byte> selectedIndexes, GameObject xObj = null, GameObject yObj = null,
    //    bool shuffleOutput = false, NetworkVariable<Card[][]> attachmentsX = null, NetworkVariable<Card[][]> attachmentsY = null,
    //    NetworkVariable<bool[][]> gameStateX = null, NetworkVariable<bool[][]> gameStateY = null)
    //{
    //    FromXToY(true, false, selectedIndexes, xObj, yObj, shuffleOutput, NetworkX, null, null, LocalY, attachmentsX, attachmentsY,
    //        gameStateX, gameStateY);
    //}

    //public void FromXToY(LocalDeck LocalX,
    //    NetworkVariable<Card[]> NetworkY, List<byte> selectedIndexes, GameObject xObj = null, GameObject yObj = null,
    //    bool shuffleOutput = false, NetworkVariable<Card[][]> attachmentsX = null, NetworkVariable<Card[][]> attachmentsY = null,
    //    NetworkVariable<bool[][]> gameStateX = null, NetworkVariable<bool[][]> gameStateY = null)
    //{
    //    FromXToY(false, true, selectedIndexes, xObj, yObj, shuffleOutput, null, NetworkY, LocalX, null, attachmentsX, attachmentsY,
    //        gameStateX, gameStateY);
    //}

    //switch (action)
        //{

        //    case Action.Setup:
        //        if (extraArgs != null)
        //        {
        //            Deck.Value = extraArgs;
        //        }
        //        else Debug.LogError("Invalid Action.Set call, missing deck");

        //        if (extraArgsSpecial != null)
        //        {
        //            SpecialDeck.Value = extraArgsSpecial;
        //        }
        //        else Debug.LogError("Invalid Action.Set call, missing special deck");

        //        int steps;
        //        if (Deck.Value.Length > 6) steps = PlayerInfoManager.CardsInHandStartingTheGame;
        //        else steps = Deck.Value.Length;

        //        Card[] drawnCards = new Card[steps];
        //        for (int i = 0; i < steps; i++)
        //        {
        //            drawnCards[i] = Deck.Value[Deck.Value.Length - 1 - i];
        //        }

        //        Card[] newHandSetup = new Card[Hand.Value.Length + steps];
        //        for (int i = 0; i < Hand.Value.Length; i++)
        //        {
        //            newHandSetup[i] = Hand.Value[i];
        //        }

        //        for (int i = 0; i < newHandSetup.Length; i++)
        //        {
        //            newHandSetup[i + Hand.Value.Length] = drawnCards[i];
        //        }

        //        Hand.Value = newHandSetup;
        //        handSize.Value = Hand.Value.Length;

        //        Card[] newDeckSetup = new Card[Deck.Value.Length - steps];
        //        for (int i = 0; i < newDeckSetup.Length; i++)
        //        {
        //            newDeckSetup[i] = Deck.Value[i];
        //        }

        //        Deck.Value = newDeckSetup;
        //        break;

        //    case Action.Draw:
        //        //if (Deck.Value.Length < GameStateManager.howMany) return;

        //        List<byte> cardsToDraw = new List<byte>();

        //        for (byte i = 0; i < GameStateManager.howMany; i++)
        //        {
        //            if (i < Deck.Value.Length) cardsToDraw.Add(i);
        //        }

        //        FromXToY(Deck, Hand, cardsToDraw, gameManagerReference.playerDeckSprite, gameManagerReference.playerHand);
        //        break;

        //    case Action.Discard:
        //        FromToWithModes(GameStateManager.selectingMode, GameStateManager.SelectingMode.Discard);
        //        break;

        //    case Action.RemoveFromPlay:
        //        FromToWithModes(GameStateManager.selectingMode, GameStateManager.SelectingMode.RemoveFromPlay);
        //        break;

        //    case Action.ShuffleIntoDeck:
        //        FromToWithModes(GameStateManager.selectingMode, GameStateManager.SelectingMode.Deck, true);
        //        break;

        //    case Action.Reserve:
        //        FromToWithModes(GameStateManager.selectingMode, GameStateManager.SelectingMode.Reserve);
        //        break;

        //    case Action.Battlefield:
        //        FromToWithModes(GameStateManager.selectingMode, GameStateManager.SelectingMode.Battlefield);
        //        break;

        //    case Action.ExtraZone:
        //        FromToWithModes(GameStateManager.selectingMode, GameStateManager.SelectingMode.ExtraZone);
        //        break;

        //    case Action.ToHand:
        //        FromToWithModes(GameStateManager.selectingMode, GameStateManager.SelectingMode.Hand);
        //        break;

        //    case Action.ToSpecialDeck:
        //        FromToWithModes(GameStateManager.selectingMode, GameStateManager.SelectingMode.SpecialDeck);
        //        break;

        //    case Action.AttachStart:


        //        foreach (GameObject playerClient in PlayerInfoManager.players)
        //        {
        //            foreach (Transform child in playerClient.GetComponent<PlayerScript>().cardSection.ReserveObj.transform)
        //            {
        //                child.gameObject.GetComponent<Image>().color = CardManipulation.PossibleMoveTo;
        //            }

        //            foreach (Transform child in playerClient.GetComponent<PlayerScript>().cardSection.ExtraZoneObj.transform)
        //            {
        //                child.gameObject.GetComponent<Image>().color = CardManipulation.PossibleMoveTo;
        //            }
        //        }

        //        break;

        //    case Action.LevelUpStart:

        //        if (cardSection.Reserve.Value.Length > 0)
        //        {
        //            foreach (Transform child in cardSection.ReserveObj.transform)
        //            {
        //                child.gameObject.GetComponent<Image>().color = CardManipulation.Unselected;
        //            }

        //            gameManagerReference.GalleryView.SetActive(false);
        //        }

        //        break;

        //    case Action.LevelDown:
        //        if (cardSection.CardOldLevels.Value[
        //                gameManagerReference.selectedCards[0]
        //            ].Length == 0) return;

        //        // send last levelup to Special Deck
        //        Card[] newSpecialDeck = new Card[SpecialDeck.Value.Length + 1];
        //        for (int i = 0; i < SpecialDeck.Value.Length; i++)
        //        {
        //            newSpecialDeck[i] = SpecialDeck.Value[i];
        //        }
        //        newSpecialDeck[newSpecialDeck.Length - 1] = cardSection.Reserve.Value[gameManagerReference.selectedCards[0]];

        //        // switch selected card with it's last levelup
        //        Card[] newReserve = cardSection.Reserve.Value;
        //        newReserve[gameManagerReference.selectedCards[0]] = cardSection.CardOldLevels.Value[
        //            gameManagerReference.selectedCards[0]
        //        ][cardSection.CardOldLevels.Value[gameManagerReference.selectedCards[0]].Length - 1];

        //        Card[] newLevelUp = new Card[
        //            cardSection.CardOldLevels.Value[
        //                gameManagerReference.selectedCards[0]
        //            ].Length - 1
        //        ];

        //        for (byte i = 0; i < newLevelUp.Length; i++)
        //        {
        //            newLevelUp[i] = cardSection.CardOldLevels.Value[gameManagerReference.selectedCards[0]][i];
        //        }

        //        // set values
        //        cardSection.CardOldLevels.Value[gameManagerReference.selectedCards[0]] = newLevelUp;

        //        cardSection.Reserve.Value = null;
        //        cardSection.Reserve.Value = newReserve;

        //        SpecialDeck.Value = newSpecialDeck;

        //        gameManagerReference.selectedCards = new List<byte>();
        //        GameStateManager.selectingMode = GameStateManager.SelectingMode.None;
        //        gameManagerReference.RenderCorrectButtons(GameStateManager.SelectingMode.None);

        //        break;

        //    case Action.Tap:

        //        if (gameManagerReference.selectedCards.Count < 1) return;

        //        GameObject tapObj = ModeToGameObject(GameStateManager.selectingMode);
        //        NetworkVariable<bool[][]> tapStates = GetState(GameStateManager.selectingMode);
        //        bool[][] newTapStates = tapStates.Value;

        //        for (byte i = 0; i < tapObj.transform.childCount; i++)
        //        {
        //            if (gameManagerReference.selectedCards.Contains(i))
        //            {
        //                newTapStates[i][0] = !newTapStates[i][0];
        //            }
        //        }

        //        tapStates.Value = null;
        //        tapStates.Value = newTapStates;
        //        cardSection.RenderSectionSelectingCancel(tapObj);

        //        break;

        //    case Action.Flip:
        //        GameObject flipObj = ModeToGameObject(GameStateManager.selectingMode);
        //        NetworkVariable<bool[][]> flipStates = GetState(GameStateManager.selectingMode);
        //        bool[][] newFlipStates = flipStates.Value;

        //        for (byte i = 0; i < flipObj.transform.childCount; i++)
        //        {
        //            if (gameManagerReference.selectedCards.Contains(i))
        //            {
        //                newFlipStates[i][1] = !newFlipStates[i][1];
        //            }
        //        }

        //        flipStates.Value = null;
        //        flipStates.Value = newFlipStates;
        //        cardSection.RenderSectionSelectingCancel(flipObj);

        //        break;

        //    case Action.UntapAll:

        //        bool[][] newReserveCardStates = new bool[cardSection.ReserveCardStates.Value.Length][];
        //        for (int i = 0; i < newReserveCardStates.Length; i++)
        //        {
        //            newReserveCardStates[i] = new bool[2];
        //            newReserveCardStates[i][0] = cardSection.Reserve.Value[i].type == PlayerInfoManager.CardType.Event
        //                || cardSection.Reserve.Value[i].type == PlayerInfoManager.CardType.EventClimax;
        //            newReserveCardStates[i][1] = cardSection.ReserveCardStates.Value[i][1];
        //        }

        //        bool[][] newBattlefieldCardStates = new bool[cardSection.BattlefieldCardStates.Value.Length][];
        //        for (int i = 0; i < newBattlefieldCardStates.Length; i++)
        //        {
        //            newBattlefieldCardStates[i] = new bool[2];
        //            newBattlefieldCardStates[i][0] = cardSection.Battlefield.Value[i].type == PlayerInfoManager.CardType.Event
        //                || cardSection.Battlefield.Value[i].type == PlayerInfoManager.CardType.EventClimax;
        //            newBattlefieldCardStates[i][1] = cardSection.BattlefieldCardStates.Value[i][1];
        //        }

        //        bool[][] newExtraZoneCardStates = new bool[cardSection.ExtraZoneCardStates.Value.Length][];
        //        for (int i = 0; i < newExtraZoneCardStates.Length; i++)
        //        {
        //            newExtraZoneCardStates[i] = new bool[2];
        //            newExtraZoneCardStates[i][0] = cardSection.ExtraZone.Value[i].type == PlayerInfoManager.CardType.Event
        //                || cardSection.ExtraZone.Value[i].type == PlayerInfoManager.CardType.EventClimax;
        //            newExtraZoneCardStates[i][1] = cardSection.ExtraZoneCardStates.Value[i][1];
        //        }

        //        cardSection.ReserveCardStates.Value = null;
        //        cardSection.ReserveCardStates.Value = newReserveCardStates;

        //        cardSection.BattlefieldCardStates.Value = null;
        //        cardSection.BattlefieldCardStates.Value = newBattlefieldCardStates;

        //        cardSection.ExtraZoneCardStates.Value = null;
        //        cardSection.ExtraZoneCardStates.Value = newExtraZoneCardStates;

        //        break;

        //    case Action.AddCounter:

        //        if (GameStateManager.selectingMode == GameStateManager.SelectingMode.Reserve)
        //        {
        //            int[] newReserveCounters = cardSection.ReserveCounters.Value;
        //            cardSection.ReserveCounters.Value = null;

        //            foreach (byte card in gameManagerReference.selectedCards)
        //            {
        //                newReserveCounters[card] = 0;
        //            }
        //            cardSection.ReserveCounters.Value = newReserveCounters;
        //        }
        //        else if (GameStateManager.selectingMode == GameStateManager.SelectingMode.Battlefield)
        //        {
        //            int[] newBattlefieldCounters = cardSection.BattlefieldCounters.Value;
        //            cardSection.BattlefieldCounters.Value = null;

        //            foreach (byte card in gameManagerReference.selectedCards)
        //            {
        //                newBattlefieldCounters[card] = 0;
        //            }
        //            cardSection.BattlefieldCounters.Value = newBattlefieldCounters;
        //        }
        //        else if (GameStateManager.selectingMode == GameStateManager.SelectingMode.ExtraZone)
        //        {
        //            int[] newExtraZoneCounters = cardSection.ExtraZoneCounters.Value;
        //            cardSection.ExtraZoneCounters.Value = null;

        //            foreach (byte card in gameManagerReference.selectedCards)
        //            {
        //                newExtraZoneCounters[card] = 0;
        //            }
        //            cardSection.ExtraZoneCounters.Value = newExtraZoneCounters;
        //        }

        //        cardSection.RenderSectionSelectingCancel(cardSection.ExtraZoneObj);


        //        break;

        //    case Action.Mill:
        //        for (int i = 0; i < GameStateManager.howMany; i++)
        //        {
        //            gameManagerReference.selectedCards.Add((byte)i);
        //        }

        //        FromToWithModes(GameStateManager.SelectingMode.Deck, GameStateManager.SelectingMode.Discard);

        //        break;

        //    case Action.ToBottomOfDeck:

        //        if (GameStateManager.viewingMode == GameStateManager.SelectingMode.DeckSection)
        //        {
        //            ToBottomOfDeckFromSection();
        //        }
        //        else
        //        {
        //            FromToWithModes(GameStateManager.selectingMode, GameStateManager.SelectingMode.Deck);
        //        }

        //        break;

        //    case Action.ToTopOfDeck:
        //        if (GameStateManager.viewingMode == GameStateManager.SelectingMode.DeckSection)
        //        {
        //            ToTopOfDeckFromSection();
        //        }
        //        else
        //        {
        //            FromToWithModes(GameStateManager.selectingMode, GameStateManager.SelectingMode.Deck, false, true);
        //        }
        //        break;

        //    default:
        //        Debug.LogError("no action provided");
        //        break;
        //}

 */