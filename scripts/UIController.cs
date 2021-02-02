using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class UIController : MonoBehaviour
{
    // Objects in Scene
    public GameObject gameEventsObj;
    public Scrollbar gameEventsScrollbar;
    public ScrollRect gameEventsScrollRect;
    public GameObject decisionsObj;
    public GameObject playersObj;
    public GameObject playerCardsObj;
    public GameObject playerCardsTitleObj;
    public GameObject gameCardsObj;
    public GameObject backgroundObj;
    public Text currentSiegeDayText;
    public Text currentSiegeHourText;
    public Transform ingameOptionsButtonsObj;
    public Transform pauseMenuButtonObj;
    public Transform pauseButtonObj;
    public Transform playNormalButtonObj;
    public Transform playFastButtonObj;
    public Transform playSuperFastButtonObj;
    public Transform pauseMenu;

    // Variables created at runtime
    public Transform[] speedButtonObjs;

    // Prefabs for instantiating
    public GameObject playerObjPrefab;
    public GameObject gameEventPrefab;
    public GameObject gameEventNewDayPrefab;
    public GameObject cardSmallPrefab;
    public GameObject cardLargePrefab;
    public GameObject buttonPrefab;
    public GameObject buttonDecisionPrefab;
    public GameObject chooseAmountDecisionPrefab;
    public GameObject debugMenuPrefab;
    public GameObject debugItemPrefab;
    public GameObject debugCardItemPrefab;
    public GameObject debugDiceItemPrefab;
    public GameObject notchPrefab;
    public GameObject hoverOverPrefab;
    public GameObject borderPrefab;

    // References
    public GameObject currentDecisionObj;
    public Text timeForDecisionText;
    public Text timeRemainingText;
    public GameObject currentCardLargeObj;

    // Options
    public int maxNotches;
    public int maxGameEvents;
    public float playerListItemWidth;
    public float playerListItemHeight;
    public float playerListItemGap;
    public float cardAreaWidth;
    public Color activeSpeedColour;
    public Color inactiveSpeedColour;
    public Color pausedColour;
    public Color unpausedColour;
    public Color deadColour;

    public Color healthColour;
    public Color pointsColour;

    // UI data
    public float totalGameEventsHeight = 0;
    public List<string> allGameEvents = new List<string>();
    public List<GameEvent> gameEvents = new List<GameEvent>();
    private bool gameWasPausedBeforeMenuOpened;
    private bool pauseMenuIsOpen;
    private bool[] playerCardAreaOpen;

    // Singleton pattern
    public static UIController instance;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        speedButtonObjs = new Transform[] { playNormalButtonObj, playFastButtonObj, playSuperFastButtonObj };
        gameWasPausedBeforeMenuOpened = false;
        pauseMenuIsOpen = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (currentDecisionObj != null)
        {
            timeRemainingText.text =  GameController.instance.timeSinceDecisionStarted.ToString("0.0");
        }
    }

    public void SetDecisionsObj(Decision decision)
    {
        if (decision != null)
        {
            if (decision.isDiscrete)
            {
                currentDecisionObj = Instantiate(buttonDecisionPrefab, decisionsObj.transform);
                ButtonDecisionObj newButtonDecisionObj = currentDecisionObj.GetComponent<ButtonDecisionObj>();
                newButtonDecisionObj.InitialiseObject(decision);
            }
            else
            {
                currentDecisionObj = Instantiate(chooseAmountDecisionPrefab, decisionsObj.transform);
                ChooseAmountDecisionObj newChooseAmountDecisionObj = currentDecisionObj.GetComponent<ChooseAmountDecisionObj>();
                newChooseAmountDecisionObj.InitialiseObject(decision);
            }
            timeForDecisionText = currentDecisionObj.transform.Find("Player").Find("TimeForDecision").GetComponent<Text>();
            timeRemainingText = currentDecisionObj.transform.Find("Player").Find("TimeRemaining").GetComponent<Text>();

            timeForDecisionText.text = GameController.instance.timedTurnsOn ? "" + decision.timeToRespond : "infinity";
        }
        else
        {
            Debug.Log("Decision cannot be null.");
        }
    }

    public void ClearDecisionObj()
    {
        foreach (Transform child in decisionsObj.transform)
        {
            Destroy(child.gameObject);
        }
    }

    public void SetPlayerInsideObj(Player player, bool isInside)
    {
        playersObj.transform.Find("Player" + (player.ID + 1)).Find("RightSide").Find("Inside").Find("IsInside").gameObject.SetActive(isInside);
        playersObj.transform.Find("Player" + (player.ID + 1)).Find("RightSide").Find("Inside").Find("IsOutside").gameObject.SetActive(!isInside);
    }

    public void SetGameCardsAvailableToPlayerObj(Card[] cardsAvailable, Player player)
    {
        foreach (Transform child in gameCardsObj.transform)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < cardsAvailable.Length; i++)
        {
            GameObject newCardObj = Instantiate(cardSmallPrefab, gameCardsObj.transform);
            newCardObj.GetComponent<RectTransform>().anchorMin = new Vector2(i / 3f, 0.1f);
            newCardObj.GetComponent<RectTransform>().anchorMax = new Vector2((i + 1) / 3f, 0.9f);
            newCardObj.GetComponent<CardSmallObj>().SetCardParams(cardsAvailable[i]);
        }
    }

    public void SetPlayerCardsObj(Player playerCards, Player playerViewing)
    {
        Transform playerCardsArea = playersObj.transform.Find("Player" + (playerCards.ID + 1)).Find("CardsArea").Find("Viewport").Find("Content");
        foreach (Transform child in playerCardsArea)
        {
            Destroy(child.gameObject);
        }
        playerCardsArea.transform.DetachChildren();

        for (int i = 0; i < playerCards.cards.Count; i++)
        {
            GameObject newCardObj = Instantiate(cardSmallPrefab, playerCardsArea);
            newCardObj.GetComponent<CardSmallObj>().SetCardParams(playerCards.cards[i]);
        }

    }

    public void SetButtonImageColour(Transform buttonTransform, Color colour)
    {
        buttonTransform.Find("Image").GetComponent<Image>().color = colour;
    }

    public void SetPauseButtonObj(bool isPaused)
    {
        if (isPaused)
        {
            SetButtonImageColour(pauseButtonObj, pausedColour);
        }
        else
        {
            SetButtonImageColour(pauseButtonObj, unpausedColour);
        }
    }

    public void SetSpeedButtonsObj(int speed)
    {
        speed = Mathf.Clamp(speed, 0, 2);
        for (int i = 0; i < speedButtonObjs.Length; i++)
        {
            if (speed != i)
            {
                SetButtonImageColour(speedButtonObjs[i], inactiveSpeedColour);
            }
            else
            {
                SetButtonImageColour(speedButtonObjs[i], activeSpeedColour);
            }
        }
    }

    public void SetPauseMenuState(bool setOpen)
    {
        if (setOpen)
        {
            pauseMenuIsOpen = true;
            if (GameController.instance.isPaused)
            {
                gameWasPausedBeforeMenuOpened = true;
            }
            else
            {
                gameWasPausedBeforeMenuOpened = false;
                GameController.instance.ChangePauseState();
            }
            pauseMenu.gameObject.SetActive(true);
            pauseMenuButtonObj.SetParent(pauseMenu);
        }
        else
        {
            pauseMenuIsOpen = false;
            pauseMenu.gameObject.SetActive(false);
            pauseMenuButtonObj.SetParent(ingameOptionsButtonsObj);
            if (!gameWasPausedBeforeMenuOpened)
            {
                GameController.instance.ChangePauseState();
            }
        }
    }

    public void ChangePauseMenuState()
    {
        SetPauseMenuState(!pauseMenuIsOpen);
    }

    public void InitiatePlayerListObj(int numPlayers)
    {
        for (int i = 0; i < numPlayers; i++)
        {
            GameObject newPlayerObj = Instantiate(playerObjPrefab, playersObj.transform);
            newPlayerObj.transform.SetAsFirstSibling();
            //newPlayerObj.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1 - (i + 1) * playerListItemHeight);
            //newPlayerObj.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1 - i * playerListItemHeight);

            newPlayerObj.GetComponent<RectTransform>().localPosition = new Vector2(0, -(playerListItemHeight + playerListItemGap) * i);
            newPlayerObj.GetComponent<RectTransform>().sizeDelta = new Vector2(playerListItemWidth, playerListItemHeight);
            newPlayerObj.name = "Player" + (i + 1);
            Color backgroundColor = Player.BackgroundColour(i);
            backgroundColor.a = 1f;
            newPlayerObj.transform.Find("Background").GetComponent<Image>().color = backgroundColor;
            int index = i;
            newPlayerObj.transform.Find("Background").GetComponent<Button>().onClick.AddListener(delegate { DebugPersonalityAttributes(index); });
            newPlayerObj.transform.Find("Background").GetComponent<Button>().onClick.AddListener(delegate { AlternateCardView(index); });

            Transform notches = newPlayerObj.transform.Find("LeftSide").Find("Points").Find("Bar").Find("Notches");
            int winPoints = GameController.instance.winPoints;
            if (winPoints <= maxNotches)
            {
                for (int j = 0; j < winPoints; j++)
                {
                    GameObject newNotch = Instantiate(notchPrefab, notches);
                    newNotch.GetComponent<RectTransform>().anchorMin = new Vector2((float)j / winPoints, 0);
                    newNotch.GetComponent<RectTransform>().anchorMax = new Vector2((float)(j + 1) / winPoints, 1);
                }
            }

            newPlayerObj.transform.Find("RightSide").Find("Cards").Find("Count").GetComponent<TextMeshProUGUI>().text = "<sprite index=0 color=#BBBBBB>0";

            SimulationController.instance.kittenSpots[i].gameObject.SetActive(true);
        }
        for (int i = numPlayers; i < 6; i++)
        {
            SimulationController.instance.kittenSpots[i].gameObject.SetActive(false);
        }
    }

    public void KillPlayer(int player)
    {
        playersObj.transform.Find("Player" + (player + 1)).Find("Background").GetComponent<Image>().color = deadColour;
    }

    public void SetPlayerReferencesInPlayersObj(int numPlayers)
    {
        playerCardAreaOpen = new bool[numPlayers];
        for (int i = 0; i < numPlayers; i++)
        {
            playerCardAreaOpen[i] = false;
            ShowPlayerCards(i, false);

            playersObj.transform.Find("Player" + (i + 1)).Find("Background").GetComponent<RightClickDebug>().player = GameController.instance.players[i];

            playersObj.transform.Find("Player" + (i + 1)).Find("LeftSide").Find("Title").Find("Name").GetComponent<Text>().text = GameController.instance.players[i].name;

            playersObj.transform.Find("Player" + (i + 1)).Find("LeftSide").Find("Health").GetComponent<HoverOverObjectManager>().player = GameController.instance.players[i];
            playersObj.transform.Find("Player" + (i + 1)).Find("LeftSide").Find("Health").GetComponent<HoverOverObjectManager>().id = HoverOverObjectManager.ID.Health;

            playersObj.transform.Find("Player" + (i + 1)).Find("LeftSide").Find("Points").GetComponent<HoverOverObjectManager>().player = GameController.instance.players[i];
            playersObj.transform.Find("Player" + (i + 1)).Find("LeftSide").Find("Points").GetComponent<HoverOverObjectManager>().id = HoverOverObjectManager.ID.Points;

            playersObj.transform.Find("Player" + (i + 1)).Find("RightSide").Find("Currency").GetComponent<HoverOverObjectManager>().player = GameController.instance.players[i];
            playersObj.transform.Find("Player" + (i + 1)).Find("RightSide").Find("Currency").GetComponent<HoverOverObjectManager>().id = HoverOverObjectManager.ID.Currency;

            playersObj.transform.Find("Player" + (i + 1)).Find("RightSide").Find("Inside").GetComponent<HoverOverObjectManager>().player = GameController.instance.players[i];
            playersObj.transform.Find("Player" + (i + 1)).Find("RightSide").Find("Inside").GetComponent<HoverOverObjectManager>().id = HoverOverObjectManager.ID.Inside;
        }
    }

    public void UpdateSiegeSummary()
    {
        currentSiegeDayText.text = "Siege Day: " +  GameController.instance.currentSiegeDay;
        currentSiegeHourText.text = "Siege Hour: " + GameController.instance.currentSiegeHour;
    }

    public void AddGameEvent(string gameEventString)
    {
        allGameEvents.Add(gameEventString);
        if (allGameEvents.Count > maxGameEvents)
        {
            allGameEvents.RemoveAt(0);
        }
        
        //Transform newGameEvent = Instantiate(gameEventPrefab, gameEventsObj.transform).transform;
        //newGameEvent.GetComponent<GameEvent>().SetText(gameEventString);

        //RectTransform newGameEventRect = newGameEvent.GetComponent<RectTransform>();

        //newGameEventRect.anchoredPosition = new Vector2(0.5f, -totalGameEventsHeight);
        //totalGameEventsHeight += newGameEventRect.sizeDelta.y;

        //gameEventsObj.GetComponent<RectTransform>().sizeDelta += new Vector2(0, newGameEventRect.sizeDelta.y);
    }

    public void PopulateGameEvents(bool toPopulate)
    {
        float screenHeight = Screen.height;
        int numToDisplay = Mathf.Min(gameEvents.Count, 50);
        if (toPopulate)
        {
            for (int i = 0; i < numToDisplay; i++)
            {
                GameEvent gameEvent = gameEvents[i + gameEvents.Count - numToDisplay];
                Transform newGameEvent;
                if (gameEvent.type != GameEvent.GameEventType.TurnStarted && gameEvent.type != GameEvent.GameEventType.GameStarted)
                {
                    newGameEvent = Instantiate(gameEventPrefab, gameEventsObj.transform).transform;
                }
                else
                {
                    newGameEvent = Instantiate(gameEventNewDayPrefab, gameEventsObj.transform).transform;
                }
                newGameEvent.GetComponent<GameEventObj>().SetParams(gameEvent);

                RectTransform newGameEventRect = newGameEvent.GetComponent<RectTransform>();

                newGameEventRect.anchoredPosition = new Vector2(0.5f, -totalGameEventsHeight);
                totalGameEventsHeight += newGameEventRect.sizeDelta.y;
            }
            gameEventsObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0, totalGameEventsHeight - screenHeight);
            gameEventsScrollRect.verticalNormalizedPosition = 0;
        }
        else
        {
            foreach (Transform child in gameEventsObj.transform)
            {
                Destroy(child.gameObject);
            }
            totalGameEventsHeight = 0;
            gameEventsObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0, -screenHeight);
        }
    }

    public void DebugPersonalityAttributes(int kitten)
    {
        Player player = GameController.instance.players[kitten];
        string debugString = "";
        debugString += player.formattedName + "'s personality:" + "\n";
        debugString += "Loves Cards: " + player.lovesCards + "\n";
        debugString += "Loves Dice Control: " + player.lovesDiceControl + "\n";
        debugString += "Loves Being Mean: " + player.lovesBeingMean + "\n";
        debugString += "Hates Inside: " + player.hatesInside + "\n";
        debugString += "Riskiness: " + player.riskiness + "\n";
        debugString += "Dice Risk: " + player.diceRisk + "\n";
        debugString += "Points Risk: " + player.pointsRisk + "\n";
        debugString += "Enemy Points Risk: " + player.enemyPointsRisk + "\n";
        debugString += "Health Worry Level: " + player.healthWorryLevel + "\n";
        debugString += "Health Happy Level: " + player.healthHappyLevel + "\n";
        debugString += "Too Much Currency Level: " + player.tooMuchCurrencyLevel + "\n";

        string attrPrefString = "Attribute Preferences: ";
        foreach (Card.AttributeFamilyID attrFamID in player.attributePreferences)
        {
            attrPrefString += attrFamID + ", ";
        }
        debugString += attrPrefString;

        Debug.Log(debugString);
    }

    public void ShowPlayerCards(int kitten, bool toShow)
    {
        float newPosition = toShow ? cardAreaWidth : 0;
        playersObj.transform.Find("Player" + (kitten + 1)).Find("CardsArea").GetComponent<RectTransform>().sizeDelta = new Vector2(newPosition, 0);
        playerCardAreaOpen[kitten] = toShow;
    }

    public void AlternateCardView(int kitten)
    {
        ShowPlayerCards(kitten, !playerCardAreaOpen[kitten]);
    }
}
