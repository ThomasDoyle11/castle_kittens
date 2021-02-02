using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    // Customisable
    public int winPoints;
    public int numPlayers;
    public Player.PlayerType[] playerTypes;
    public string[] playerNames;
    public int minSiegeHoursToWin;
    public bool minSiegeHoursCondition
    {
        get
        {
            return minSiegeHoursToWin > 0;
        }
    }

    // Quasi-customisable
    public int enterInsidePoints;
    public int startTurnInsidePoints;
    public int refreshCardsCost;
    public int maxDice;
    public int standardNumDice;
    public int standardMaxRolls;
    public int standardCardVisibility;

    // Determined
    public Player[] players;
    public Player[] livingPlayers
    {
        get
        {
            return players.Where(o => o.isAlive).ToArray();
        }
    }
    public Player[] playersOutside
    {
        get
        {
            return livingPlayers.Where(o => !playersInside.Contains(o)).ToArray();
        }
    }
    public int numLivingPlayers
    {
        get
        {
            return livingPlayers.Length;
        }
    }
    public int maxNumPlayersInside
    {
        get
        {
            if (numLivingPlayers > 4)
            {
                return 2;
            }
            else
            {
                return 1;
            }
        }
    }
    public bool insideHasFreeSlot
    {
        get
        {
            return playersInside.Count < maxNumPlayersInside;
        }
    }
    public bool playerInsideDiedThisTurn;
    public bool playerHasEndedTurn;
    public int[] damageDealtThisTurn;

    public bool[] playerFinishedWithChangingEnemyDie;
    public bool playerFinishedWithSpecialRollChanges;
    public bool playerFinishedWithSpecialDieUsage;
    public bool playerHasChangedDieToOne;
    public Player enemyToHeal;

    public bool playerTurnHasUsedAggressiveHealthcare;
    public bool playerTurnHasNegatedDieDie;
    public bool playerTurnHasNegatedVenomousBite;

    public bool playerTurnDealtDamageThisTurn;
    public bool playerGetsAnExtraTurn;
    public int playerExtraTurnNumber;

    public Player playerTurn;
    public Player playerMakingCurrentDecision
    {
        get
        {
            if (currentDecisionPresented != null)
            {
                return currentDecisionPresented.player;
            }
            else
            {
                return null;
            }
        }
    }
    public List<Player> playersInside;
    public Player previousPlayerInside;
    public List<Player> playersEnteredInsideThisTurn;

    public int pivotPlayerID;
    public int currentSiegeDay;
    public int currentSiegeHour;
    public int currentTotalSiegeHours;
    public int numLivingPlayersAtStartOfSiegeDay;

    public Player[] playersWithLeastPoints
    {
        get
        {
            int lowestPoints = livingPlayers.Min(o => o.points);
            return livingPlayers.Where(o => o.points == lowestPoints).ToArray();
        }
    }

    // Time-related variables
    public float timeSinceDecisionStarted;
    public bool timedTurnsOn;
    public bool isPaused;

    // Dice related variables
    public int rollNum;
    public int maxRolls;
    public int[] currentDiceResults; // Represents what the physical dice are currently showing
    public bool[] currentDiceHeld;
    public bool rollsResolved;
    public string rollSummary;
    public bool canSelectSingleDieOnly;

    // Card related variables
    public List<Card> cardDrawPile;
    public List<Card> cardDiscardPile;
    public Card activeCard;
    public bool[] playerHasBoughtCardInstantly;

    // Decisions
    public List<Decision> nextDecisions;
    public Decision currentDecisionPresented;

    // Singleton pattern
    public static GameController instance;
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
        GameOptions.instance.SaveGameplayOptions();

        // Options
        winPoints = GameOptions.instance.winPoints;
        numPlayers = GameOptions.instance.numPlayers;
        playerTypes = GameOptions.instance.playerTypes;
        playerNames = GameOptions.instance.playerNames;
        minSiegeHoursToWin = GameOptions.instance.minSiegeHoursToWin;
        enterInsidePoints = 1;
        startTurnInsidePoints = 2;
        refreshCardsCost = 2;
        standardCardVisibility = 3;
        standardNumDice = 6;
        standardMaxRolls = 3;
        maxDice = 8;

        playerGetsAnExtraTurn = false;
        playerExtraTurnNumber = 0;
        SetDiceVisible(0);


        players = new Player[numPlayers];
        damageDealtThisTurn = new int[numPlayers];
        playerFinishedWithChangingEnemyDie = new bool[numPlayers];
        UIController.instance.InitiatePlayerListObj(numPlayers);

        nextDecisions = new List<Decision>();
        currentDecisionPresented = null;

        timedTurnsOn = false;
        timeSinceDecisionStarted = 0;
        isPaused = false;

        for (int i = 0; i < numPlayers; i++)
        {
            players[i] = new Player(i, playerNames[i], playerTypes[i]);
        }
        UIController.instance.SetPlayerReferencesInPlayersObj(numPlayers);

        playerTurn = RandomPlayer();
        playersInside = new List<Player>();
        playersEnteredInsideThisTurn = new List<Player>();
        previousPlayerInside = null;

        pivotPlayerID = playerTurn.ID;
        currentSiegeDay = 1;
        currentSiegeHour = 1;
        currentTotalSiegeHours = 0;
        numLivingPlayersAtStartOfSiegeDay = numPlayers;

        playerHasBoughtCardInstantly = new bool[numPlayers];
        cardDrawPile = new List<Card>();
        cardDiscardPile = new List<Card>();
        for (int i = 0; i < 1; i++)
        {
            cardDrawPile.Add(new Card(Card.ID.FreeReturns));
            cardDrawPile.Add(new Card(Card.ID.DiceCloning));
            cardDrawPile.Add(new Card(Card.ID.Vitality));
            cardDrawPile.Add(new Card(Card.ID.GoodInvestments));
            cardDrawPile.Add(new Card(Card.ID.Aggressive));
            cardDrawPile.Add(new Card(Card.ID.Counterfeiting));
            cardDrawPile.Add(new Card(Card.ID.StrongArmour));
            cardDrawPile.Add(new Card(Card.ID.Rebirth));
            cardDrawPile.Add(new Card(Card.ID.FullHouse));
            cardDrawPile.Add(new Card(Card.ID.CollateralDamage));
            cardDrawPile.Add(new Card(Card.ID.OneLove));
            cardDrawPile.Add(new Card(Card.ID.HiddenWeapon));
            cardDrawPile.Add(new Card(Card.ID.Resourceful));
            cardDrawPile.Add(new Card(Card.ID.DamageForTwo));
            cardDrawPile.Add(new Card(Card.ID.BloodDonor));
            cardDrawPile.Add(new Card(Card.ID.Gallant));
            cardDrawPile.Add(new Card(Card.ID.Healthy));
            cardDrawPile.Add(new Card(Card.ID.Thrifty));
            cardDrawPile.Add(new Card(Card.ID.Mercentile));
            cardDrawPile.Add(new Card(Card.ID.Pathetic));
            cardDrawPile.Add(new Card(Card.ID.Sneaky));
            cardDrawPile.Add(new Card(Card.ID.SplashDamage));
            cardDrawPile.Add(new Card(Card.ID.SpikeySides));
            cardDrawPile.Add(new Card(Card.ID.CountingCards));
            cardDrawPile.Add(new Card(Card.ID.Institutionalized));
            cardDrawPile.Add(new Card(Card.ID.Foresight));
            cardDrawPile.Add(new Card(Card.ID.HedgeFund));
            cardDrawPile.Add(new Card(Card.ID.SkullCollector));
            cardDrawPile.Add(new Card(Card.ID.Pacifist));
            cardDrawPile.Add(new Card(Card.ID.OneMore));
            cardDrawPile.Add(new Card(Card.ID.HighRoller));
            cardDrawPile.Add(new Card(Card.ID.Spiteful));
            cardDrawPile.Add(new Card(Card.ID.GloriousGrenade));
            cardDrawPile.Add(new Card(Card.ID.MoraleBoost));
            cardDrawPile.Add(new Card(Card.ID.Prestigious));
            cardDrawPile.Add(new Card(Card.ID.Tourniquet));
            cardDrawPile.Add(new Card(Card.ID.Masochistic));
            cardDrawPile.Add(new Card(Card.ID.MinorVictory));
            cardDrawPile.Add(new Card(Card.ID.Martyrish));
            cardDrawPile.Add(new Card(Card.ID.Sacrifice));
            cardDrawPile.Add(new Card(Card.ID.Honorable));
            cardDrawPile.Add(new Card(Card.ID.InstantDividend));
            cardDrawPile.Add(new Card(Card.ID.RobinHood));
            cardDrawPile.Add(new Card(Card.ID.NonStop));
            cardDrawPile.Add(new Card(Card.ID.MinorSelfDestruct));
            cardDrawPile.Add(new Card(Card.ID.WellRenowned));
            cardDrawPile.Add(new Card(Card.ID.TearGas));
            cardDrawPile.Add(new Card(Card.ID.Invasion));
            cardDrawPile.Add(new Card(Card.ID.HealthInsurance));
            cardDrawPile.Add(new Card(Card.ID.TemporaryInvulnerability));
            cardDrawPile.Add(new Card(Card.ID.HeartyRoll));
            cardDrawPile.Add(new Card(Card.ID.RockAndRoller));
            cardDrawPile.Add(new Card(Card.ID.CostlyRoll));
            cardDrawPile.Add(new Card(Card.ID.DieForger));
            cardDrawPile.Add(new Card(Card.ID.DieCaster));
            cardDrawPile.Add(new Card(Card.ID.TertiaryAllergy));
            cardDrawPile.Add(new Card(Card.ID.OneToOne));
            cardDrawPile.Add(new Card(Card.ID.DieKing));
            cardDrawPile.Add(new Card(Card.ID.AggressiveHealthcare));
            cardDrawPile.Add(new Card(Card.ID.DieDie));
            cardDrawPile.Add(new Card(Card.ID.VenomousBite));
            cardDrawPile.Add(new Card(Card.ID.Bargainer));
            cardDrawPile.Add(new Card(Card.ID.QuickDeal));
            cardDrawPile.Add(new Card(Card.ID.Duplicate));
        }
        ShuffleDrawPile();

        GameEvent.GameStarted(numPlayers, playerTurn.ID);
        UIController.instance.UpdateSiegeSummary();

        AddDecision(Decision.StartTurn(playerTurn));
    }

    void Update()
    {
        // Select next Decision and present to Human Player if necessary
        // Otherwise, if Decision is for AI, respond after AI response time, or autopick next move if Player is Human
        if (!SimulationController.instance.simulationPlaying)
        {
            if (currentDecisionPresented == null)
            {
                UIController.instance.ClearDecisionObj();
                if (nextDecisions.Count > 0)
                {
                    timeSinceDecisionStarted = 0;
                    currentDecisionPresented = nextDecisions[0];
                    nextDecisions.RemoveAt(0);
                    PresentDecision(currentDecisionPresented);
                }
                else
                {
                    Debug.Log("No Decisions lined up, this shouldn't happen, whoops.");
                }
            }
            else
            {
                if (!isPaused)
                {
                    timeSinceDecisionStarted += Time.deltaTime;
                    if (currentDecisionPresented.player != null)
                    {
                        if (currentDecisionPresented.player.playerType == Player.PlayerType.AI)
                        {
                            if (timeSinceDecisionStarted > Decision.defaultAIDecisionTime)
                            {
                                currentDecisionPresented.MakeAIDecision();
                            }
                        }
                        else if (timeSinceDecisionStarted > currentDecisionPresented.timeToRespond && timedTurnsOn)
                        {
                            currentDecisionPresented.MakeDefaultDecision();
                        }
                    }
                    else if (timeSinceDecisionStarted > currentDecisionPresented.timeToRespond && timedTurnsOn)
                    {
                        currentDecisionPresented.MakeDefaultDecision();
                    }
                }
            }
        }
    }

    // Present Decision to Player
    public void PresentDecision(Decision decision)
    {
        UIController.instance.SetDecisionsObj(decision);


        UIController.instance.SetGameCardsAvailableToPlayerObj(GameCardsAvailableToPlayer(playerMakingCurrentDecision), playerMakingCurrentDecision);

        //if (decision.player != null)
        //{
        //    UIController.instance.SetGameCardsAvailableToPlayerObj(GameCardsAvailableToPlayer(decision.player), decision.player);
        //    UIController.instance.SetPlayerCardsObj(decision.player, decision.player);
        //}
        //string debugString = "Upcoming Decisions: ";
        //if (nextDecisions.Count > 0)
        //{
        //    for (int i = 0; i < nextDecisions.Count; i++)
        //    {
        //        debugString += nextDecisions[i].description + " for " + nextDecisions[i].player.colouredName + ", ";
        //    }
        //}
        //else
        //{
        //    debugString += "None";
        //}
        //Debug.Log(debugString);
    }

    public void AddDecision(Decision decision)
    {
        nextDecisions.Add(decision);
    }

    public void AddDecisionAsPriority(Decision decision)
    {
        nextDecisions.Insert(0, decision);
    }

    // Remove Decision from Player in case of no longer available
    public void ClearDecisions(Player player)
    {
        List<Decision> removals = new List<Decision>();
        for (int i = 0; i < nextDecisions.Count; i++)
        {
            if (nextDecisions[i].player != null)
            {
                if (nextDecisions[i].player.ID == player.ID)
                {
                    removals.Add(nextDecisions[i]);
                }
            }
        }

        for (int i = 0; i < removals.Count; i++)
        {
            nextDecisions.Remove(removals[i]);
        }

        removals.Clear();

        if (currentDecisionPresented != null)
        {
            if (currentDecisionPresented.player != null)
            {
                if (currentDecisionPresented.player.ID == player.ID)
                {
                    currentDecisionPresented = null;
                    UIController.instance.ClearDecisionObj();
                }
            }
        }
    }

    public void ClearAllDecisions()
    {
        nextDecisions.Clear();
        currentDecisionPresented = null;
        UIController.instance.ClearDecisionObj();
    }

    // Sequential parts of a turn

    public void StartTurn()
    {
        SimulationController.instance.AddSimulationToQueue(SimulationController.instance.ShowKittenHalo(playerTurn.ID, true));
        GameEvent.TurnStarted(currentSiegeDay, currentSiegeHour, playerTurn.ID);

        SetSingleDieSelectableOnly(false);
        playerHasEndedTurn = false;
        rollSummary = "";
        playerTurnDealtDamageThisTurn = false;
        playerFinishedWithSpecialRollChanges = false;
        playerFinishedWithSpecialDieUsage = false;
        playerHasChangedDieToOne = false;
        enemyToHeal = null;
        activeCard = null;
        for (int i = 0; i < numPlayers; i++)
        {
            damageDealtThisTurn[i] = 0;
            playerFinishedWithChangingEnemyDie[i] = false;
            playerHasBoughtCardInstantly[i] = false;
            players[i].isInvulnerable = false;
            players[i].hasHadOptionToBecomeInvulnerable = false;
        }

        if (playerTurn.HasCard(Card.ID.HedgeFund))
        {
            int newCurrency = 2 * playerTurn.CountCard(Card.ID.HedgeFund);
            playerTurn.currency += newCurrency;

            int hedgeFundsAtZero = 0;
            for (int i = 0; i < playerTurn.hedgeFund.Count; i++)
            {
                playerTurn.hedgeFund[i] -= 1;
                if (playerTurn.hedgeFund[i] == 0)
                {
                    hedgeFundsAtZero += 1;
                }
            }
            for (int i = 0; i < hedgeFundsAtZero; i++)
            {
                playerTurn.RemoveCard(playerTurn.cards.Find(o => o.id == Card.ID.HedgeFund));
            }
        }

        rollsResolved = false;
        playersEnteredInsideThisTurn.Clear();
        UIController.instance.playersObj.transform.Find("Player" + (playerTurn.ID + 1)).Find("LeftSide").Find("IsTurn").Find("Image").gameObject.SetActive(true);
        UIController.instance.SetGameCardsAvailableToPlayerObj(GameCardsAvailableToPlayer(playerTurn), playerMakingCurrentDecision);
        UIController.instance.SetPlayerCardsObj(playerTurn, playerMakingCurrentDecision);
        SetAllDiceHeld(false);
        playerInsideDiedThisTurn = false;

        if (PlayerIsInside(playerTurn))
        {
            int newPoints = startTurnInsidePoints;
            if (playerTurn.HasCard(Card.ID.Institutionalized))
            {
                newPoints += playerTurn.CountCard(Card.ID.Institutionalized);
            }
            playerTurn.points += newPoints;

            playerTurn.siegeDays += 1;
        }

        // Calculate how many rolls Player gets
        maxRolls = standardMaxRolls;
        if (playerTurn.HasCard(Card.ID.HighRoller))
        {
            int newRolls = playerTurn.CountCard(Card.ID.HighRoller);
            maxRolls += newRolls;
        }

        // Calculate how many dice Player can use
        int numDiceForPlayer = standardNumDice;
        int extraDice = playerTurn.CountCard(Card.ID.DiceCloning);
        if (extraDice > 0)
        {
            numDiceForPlayer += extraDice;
        }
        if (playerGetsAnExtraTurn)
        {
            numDiceForPlayer -= playerExtraTurnNumber;
            playerGetsAnExtraTurn = false;
        }
        numDiceForPlayer -= playerTurn.deadDice;
        numDiceForPlayer = Mathf.Min(numDiceForPlayer, maxDice);
        numDiceForPlayer = Mathf.Max(numDiceForPlayer, 0);
        InitiateDice(numDiceForPlayer);

        AddDecision(Decision.FirstRoll(playerTurn));
    }

    public void InitiateDice(int numDice)
    {
        UnsetAllDiceValues();
        SetDiceVisible(numDice);
        rollNum = 0;
        currentDiceResults = new int[numDice];
        currentDiceHeld = new bool[numDice];
        SetAllDiceHeld(false);
    }

    public void StandardTurnRoll()
    {
        if (playerTurn.playerType == Player.PlayerType.Human)
        {
            SetDiceInteractable(true);
        }

        if (rollNum < maxRolls)
        {
            rollNum += 1;
            RollDice(false);
        }
        else
        {
            Debug.Log("You shouldn't be able to roll right now.");
        }

        if (!AllDiceAreHeld() && rollNum < maxRolls)
        {
            AddDecision(Decision.IntermediateRoll(playerTurn));
        }
        else
        {
            //SetDiceInteractable(false);
            KeepRolls();
        }
    }

    public void KeepRolls()
    {
        rollNum = maxRolls;
        AddPostRollsDecisions();
    }

    public void Resolve()
    {
        rollsResolved = true;
        SetAllDiceHeld(true);
        int[] diceSummary = DiceSummary();

        GameEvent.DiceResults(currentDiceResults);

        // Check Card-related
        if (playerTurn.HasCard(Card.ID.FullHouse))
        {
            bool getsFullHousePoints = true;
            for (int i = 0; i < 6; i++)
            {
                if (diceSummary[i] < 1)
                {
                    getsFullHousePoints = false;
                }
            }
            if (getsFullHousePoints)
            {
                int newPoints = playerTurn.CountCard(Card.ID.FullHouse);
                playerTurn.points += 9 * newPoints;
            }
        }
        if (playerTurn.HasCard(Card.ID.CountingCards))
        {
            bool getsCountingCardsPoints = true;
            for (int i = 0; i < 3; i++)
            {
                if (diceSummary[i] < 1)
                {
                    getsCountingCardsPoints = false;
                }
            }
            if (getsCountingCardsPoints)
            {
                int newPoints = 2 * playerTurn.CountCard(Card.ID.CountingCards);
                playerTurn.points += newPoints;
            }
        }

        // Check for points
        for (int i = 0; i <= 2; i++)
        {
            if (diceSummary[i] >= 3)
            {
                int pointsEarned = (i + 1) + (diceSummary[i] - 3);
                if (i == 0)
                {
                    if (playerTurn.HasCard(Card.ID.OneLove))
                    {
                        int newPoints = 2 * playerTurn.CountCard(Card.ID.OneLove);
                        pointsEarned += newPoints;
                    }
                    if (playerTurn.HasCard(Card.ID.OneMore))
                    {
                        playerGetsAnExtraTurn = true;
                        playerExtraTurnNumber += 1;
                        UIController.instance.AddGameEvent(playerTurn.formattedName + " earned One More turn.");
                    }
                }
                else if (i == 1 && playerTurn.HasCard(Card.ID.DamageForTwo))
                {
                    int newPoints = 2 * playerTurn.CountCard(Card.ID.DamageForTwo);
                    diceSummary[3] += newPoints;
                }
                playerTurn.points += pointsEarned;
            }
        }

        // Heal
        if (diceSummary[4] > 0)
        {
            if (!PlayerIsInside(playerTurn))
            {
                playerTurn.ChangeHealth(null, diceSummary[4]);
            }
        }

        // Currency
        if (diceSummary[5] > 0)
        {
            playerTurn.currency += diceSummary[5];
        }

        // Deal damage - do this last in case of win and entering Inside (get to heal first)
        if (playerTurn.HasCard(Card.ID.Aggressive) && PlayerIsInside(playerTurn))
        {
            int newPoints = playerTurn.CountCard(Card.ID.Aggressive);
            diceSummary[3] += newPoints;
        }
        if (playerTurn.HasCard(Card.ID.HiddenWeapon))
        {
            int newPoints = playerTurn.CountCard(Card.ID.HiddenWeapon);
            diceSummary[3] += newPoints;
        }
        if (diceSummary[3] > 0)
        {
            if (playerTurn.HasCard(Card.ID.SpikeySides))
            {
                int healthLost = playerTurn.CountCard(Card.ID.SpikeySides);
                Player nextLivingPlayer = NextLivingPlayer();
                damageDealtThisTurn[nextLivingPlayer.ID] += healthLost;

                Player previousLivingPlayer = PreviousLivingPlayer();
                if (previousLivingPlayer.ID != nextLivingPlayer.ID)
                {
                    damageDealtThisTurn[previousLivingPlayer.ID] += healthLost;
                }
            }
            if (playerTurn.HasCard(Card.ID.Gallant))
            {
                playerTurn.points += playerTurn.CountCard(Card.ID.Gallant);
            }
            if (playerTurn.HasCard(Card.ID.CollateralDamage))
            {
                int newDamage = playerTurn.CountCard(Card.ID.CollateralDamage);
                diceSummary[3] += newDamage;
            }
            if (playerTurn.HasCard(Card.ID.Institutionalized) && PlayerIsInside(playerTurn))
            {
                int newDamage = playerTurn.CountCard(Card.ID.Institutionalized);
                diceSummary[3] += newDamage;
            }

            if (playerTurn.HasCard(Card.ID.SplashDamage))
            {
                foreach (Player livingPlayer in OtherLivingPlayers(playerTurn))
                {
                    damageDealtThisTurn[livingPlayer.ID] += diceSummary[3];
                }
            }
            else if (PlayerIsInside(playerTurn))
            {
                foreach (Player playerOutside in playersOutside)
                {
                    damageDealtThisTurn[playerOutside.ID] += diceSummary[3];
                }
            }
            else
            {
                foreach (Player playerInside in playersInside)
                {
                    damageDealtThisTurn[playerInside.ID] += diceSummary[3];
                }
            }

            // Check for Strong Armour
            foreach (Player livingPlayer in livingPlayers)
            {
                if (damageDealtThisTurn[livingPlayer.ID] == 1 && livingPlayer.HasCard(Card.ID.StrongArmour))
                {
                    damageDealtThisTurn[livingPlayer.ID] = 0;
                }
            }

            // Sumlate the attack
            if (PlayerIsInside(playerTurn))
            {
                if (playersInside[0].ID == playerTurn.ID)
                {
                    SimulationController.instance.AddSimulationToQueue(SimulationController.instance.AttackKittensFromInside(playerTurn.ID, 0));
                }
                else
                {
                    SimulationController.instance.AddSimulationToQueue(SimulationController.instance.AttackKittensFromInside(playerTurn.ID, 1));
                }
            }
            else
            {
                SimulationController.instance.AddSimulationToQueue(SimulationController.instance.AttackKittensFromOutside(playerTurn.ID));
            }

            // Finally deal the damage
            for (int i = 0; i < damageDealtThisTurn.Length; i++)
            {
                if (damageDealtThisTurn[i] > 0)
                {
                    if (players[i].HasCard(Card.ID.Sneaky) && damageDealtThisTurn[i] >= players[i].health && PlayerIsInside(players[i]))
                    {
                        damageDealtThisTurn[i] = 0;
                        LeaveInside(players[i], true);
                    }
                    else
                    {
                        players[i].ChangeHealth(playerTurn, -damageDealtThisTurn[i]);
                        playerTurnDealtDamageThisTurn = true;
                    }
                }
            }
        }

        if (playerTurn.HasCard(Card.ID.Pacifist) && !playerTurnDealtDamageThisTurn)
        {
            int newPoints = playerTurn.CountCard(Card.ID.Pacifist);
            playerTurn.points += newPoints;
        }

        // If a slot is available, you must enter Inside
        if (insideHasFreeSlot && !PlayerIsInside(playerTurn))
        {
            EnterInside();
        }

        for (int i = playersInside.Count - 1; i >= 0; i--)
        {
            Player player = playersInside[i];
            if (damageDealtThisTurn[player.ID] > 0 && !playersEnteredInsideThisTurn.Contains(player))
            {
                AddDecision(Decision.LeaveOrStay(player));
            }
        }
        AddDecision(Decision.BrowseCards(playerTurn));
    }

    public void FinishedBrowsingCards()
    {
        if (playerTurn.HasCard(Card.ID.FreeReturns) && playerTurn.cards.Count > 0)
        {
            AddDecision(Decision.RefundCards(playerTurn));
        }
        AddDecision(Decision.EndTurn(playerTurn));
    }

    public void EndTurn()
    {
        playerHasEndedTurn = true;
        if (playerTurn.HasCard(Card.ID.Pathetic))
        {
            if (playersWithLeastPoints.Contains(playerTurn) && playersWithLeastPoints.Length == 1)
            {
                int newPoints = playerTurn.CountCard(Card.ID.Pathetic);
                playerTurn.points += newPoints;
            }
        }
        if (playerTurn.HasCard(Card.ID.GoodInvestments))
        {
            if (playerTurn.currency >= 6)
            {
                int newPoints = playerTurn.currency / 6;
                playerTurn.points += newPoints;
            }
        }
        if (playerTurn.HasCard(Card.ID.Resourceful))
        {
            if (playerTurn.currency == 0)
            {
                playerTurn.currency = 1;
            }
        }

        if (playerTurn.venomLevel > 0)
        {
            playerTurn.ChangeHealth(null, -playerTurn.venomLevel);
        }

        if (livingPlayers.Length == 0)
        {
            EndGame(null);
            return;
        }
        else if (livingPlayers.Length == 1)
        {
            EndGame(livingPlayers);
            return;
        }
        else if (livingPlayers.Length > 1)
        {
            List<Player> winners = new List<Player>();
            foreach (Player livingPlayer in livingPlayers)
            {
                bool winCondition = livingPlayer.points >= winPoints && (!minSiegeHoursCondition || livingPlayer.siegeHours > minSiegeHoursToWin);
                if (winCondition)
                {
                    winners.Add(livingPlayer);
                }
            }
            if (winners.Count > 0)
            {
                EndGame(winners.ToArray());
                return;
            }
        }

        SimulationController.instance.AddSimulationToQueue(SimulationController.instance.ShowKittenHalo(playerTurn.ID, false));

        if (!playerGetsAnExtraTurn)
        {
            playerExtraTurnNumber = 0;
            UIController.instance.playersObj.transform.Find("Player" + (playerTurn.ID + 1)).Find("LeftSide").Find("IsTurn").Find("Image").gameObject.SetActive(false);

            playerTurn = NextLivingPlayer();
        }

        playerHasEndedTurn = false;

        if (playerTurn.HasCard(Card.ID.Duplicate))
        {
            if (AllEnemyCards(playerTurn).Length > 0)
            {
                Card[] duplicateCards = playerTurn.cards.Where(o => o.id == Card.ID.Duplicate).ToArray();
                for (int i = 0; i < duplicateCards.Length; i++)
                {
                    if (playerTurn.currency >= i + 1)
                    {
                        AddDecisionAsPriority(Decision.ChangeCardBeingDuplicated(playerTurn, duplicateCards[i]));
                    }
                }
            }
        }


        currentTotalSiegeHours += 1;
        foreach (Player playerInside in playersInside)
        {
            playerInside.siegeHours += 1;
        }
        if (playerTurn.ID == pivotPlayerID)
        {
            currentSiegeDay += 1;
            currentSiegeHour = 1;
            numLivingPlayersAtStartOfSiegeDay = numLivingPlayers;
        }
        else
        {
            currentSiegeHour += 1;
        }
        SimulationController.instance.AddSimulationToQueue(SimulationController.instance.SetTimeOfDay());
        UIController.instance.UpdateSiegeSummary();

        AddDecision(Decision.StartTurn(playerTurn));
        // StartTurn();
    }

    // Dice-related

    public void SetDieVisible(int die, bool isVisible)
    {
        SimulationController.instance.SetDieVisibleObj(die, isVisible);
    }

    public void SetDiceVisible(int numDice)
    {
        for (int i = 0; i < numDice; i++)
        {
            SetDieVisible(i, true);
        }
        for (int i = numDice; i < maxDice; i++)
        {
            SetDieVisible(i, false);
        }
        SimulationController.instance.SetDiceBordersObj(numDice);
    }

    public void SetDieInteractable(int die, bool isActive)
    {
        SimulationController.instance.SetDieButtonInteractableObj(die, isActive);
    }

    public void SetDiceInteractable(bool isActive)
    {
        for (int i = 0; i < currentDiceResults.Length; i++)
        {
            SetDieInteractable(i, isActive);
        }
        for (int i = currentDiceResults.Length; i < maxDice; i++)
        {
            SetDieInteractable(i, !isActive);
        }
    }

    public void SetAllAvailableDiceActive(bool isActive)
    {
        for (int i = 0; i < currentDiceResults.Length; i++)
        {
            SetDieInteractable(i, isActive);
        }
    }

    public void SetDieHeld(int die, bool isHeld)
    {
        currentDiceHeld[die] = isHeld;
        SimulationController.instance.SetDieHeldObj(die, isHeld);
    }

    public int[] SetAllDiceWithValueHeld(int value, bool isHeld)
    {
        List<int> returnInts = new List<int>();
        for (int i = 0; i < currentDiceResults.Length; i++)
        {
            if (currentDiceResults[i] == value && currentDiceHeld[i] != isHeld)
            {
                SetDieHeld(i, isHeld);
                returnInts.Add(i);
            }
        }
        return returnInts.ToArray();
    }

    // Attempts to set a certain number of dice with the given value held, and returns the number of dice held, which may be less than the target if there weren't enough
    public int SetSomeDiceWithValueHeld(int value, int numToHold, bool isHeld)
    {
        int heldDice = 0;
        for (int i = 0; i < currentDiceResults.Length; i++)
        {
            if (currentDiceResults[i] == value)
            {
                SetDieHeld(i, isHeld);
                heldDice += 1;
                if (heldDice == numToHold)
                {
                    return heldDice;
                }
            }
        }
        return heldDice;
    }

    public void AlternateDieHeld(int die)
    {
        SetDieHeld(die, !currentDiceHeld[die]);
    }

    public void SetAllDiceHeld(bool isHeld)
    {
        if (isHeld)
        {
            rollNum = maxRolls;
        }
        for (int i = 0; i < currentDiceResults.Length; i++)
        {
            SetDieHeld(i, isHeld);
        }
    }

    public void SetDieValue(int die, int value)
    {
        currentDiceResults[die] = value;
        SimulationController.instance.SetDieObj(die, value);
    }

    public void UnsetAllDiceValues()
    {
        for (int i = 0; i < currentDiceResults.Length; i++)
        {
            SetDieValue(i, -1);
        }
    }

    public void RollDice(bool allDice)
    {
        List<int> dice = new List<int>();
        for (int i = 0; i < currentDiceResults.Length; i++)
        {
            if (!currentDiceHeld[i] || allDice)
            {
                dice.Add(i);
            }
        }
        RollDice(dice.ToArray());
    }

    public void RollDice(int[] dice)
    {
        int[] values = new int[dice.Length];
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = UnityEngine.Random.Range(0, 6);
            currentDiceResults[dice[i]] = values[i];
        }
        SimulationController.instance.AddSimulationToQueue(SimulationController.instance.RollDiceToValues(dice, values));
    }

    public int FirstUnheldDie()
    {
        for (int i = 0; i < currentDiceHeld.Length; i++)
        {
            if (!currentDiceHeld[i])
            {
                return i;
            }
        }
        return -1;
    }

    public void SetSingleDieSelectableOnly(bool isActive)
    {
        if (isActive)
        {
            canSelectSingleDieOnly = true;
            int firstUnheldDie = FirstUnheldDie();
            if (firstUnheldDie == -1)
            {
                PlayerClickDie(0);
            }
            else
            {
                PlayerClickDie(firstUnheldDie);
            }
        }
        else
        {
            canSelectSingleDieOnly = false;
        }
    }

    // Non-sequential parts of a turn

    public void EndGame(Player[] winners)
    {
        if (winners != null)
        {
            GameEvent.GameOver(winners.Select(o => o.ID).ToArray());
        }
        else
        {
            GameEvent.GameOver(new int[] { });
        }
        ClearAllDecisions();
        AddDecision(Decision.NewGame());
    }

    public void NewGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void EnterInside(Player newPlayerInside)
    {
        if (newPlayerInside.isAlive)
        {
            if (playersInside.Count < maxNumPlayersInside)
            {
                if (newPlayerInside != null)
                {
                    if (!PlayerIsInside(newPlayerInside))
                    {
                        if (previousPlayerInside != null)
                        {
                            if (previousPlayerInside.HasCard(Card.ID.Aggressive))
                            {
                                int damageDealt = previousPlayerInside.CountCard(Card.ID.Aggressive);
                                newPlayerInside.ChangeHealth(null, -damageDealt);
                                if (previousPlayerInside.ID == playerTurn.ID)
                                {
                                    playerTurnDealtDamageThisTurn = true;
                                }
                            }
                        }

                        if (playersInside.Count == 0)
                        {
                            SimulationController.instance.AddSimulationToQueue(SimulationController.instance.MoveKittenToInside1ViaDoor(newPlayerInside.ID));
                        }
                        else
                        {
                            SimulationController.instance.AddSimulationToQueue(SimulationController.instance.MoveKittenToInside2ViaDoorThenInsideLeft(newPlayerInside.ID));
                        }

                        playersInside.Add(newPlayerInside);
                        GameEvent.EnteredInside(newPlayerInside.ID);
                        newPlayerInside.points += enterInsidePoints;
                        playersEnteredInsideThisTurn.Add(newPlayerInside);
                        UIController.instance.SetPlayerInsideObj(newPlayerInside, true);
                    }
                    else
                    {
                        Debug.Log(newPlayerInside.formattedName + " is already Inside.");
                    }
                }
                else
                {
                    Debug.Log("New Player Inside is null.");
                }
            }
            else
            {
                Debug.Log("Max number of Players already Inside.");
            }
        }
        else
        {
            Debug.Log("Dead Players cannot Enter Inside.");
        }
    }

    public void EnterInside()
    {
        EnterInside(playerTurn);
    }

    public void KillPlayer(Player player)
    {
        UIController.instance.AddGameEvent(player.formattedName + " died.");

        if (player.ID == pivotPlayerID)
        {
            Player nextLivingPlayer = NextLivingPlayer(player);
            if (nextLivingPlayer != null)
            {
                pivotPlayerID = nextLivingPlayer.ID;
            }
        }

        if (PlayerIsInside(player))
        {
            playerInsideDiedThisTurn = true;
            LeaveInside(player, true);
        }

        SimulationController.instance.AddSimulationToQueue(SimulationController.instance.KillKitten(player.ID));
        UIController.instance.KillPlayer(player.ID);

        if (playersInside.Count > maxNumPlayersInside)
        {
            LeaveInside(playersInside[playersInside.Count - 1], false);
        }

        ClearDecisions(player);

        if (player.ID == playerTurn.ID && !playerHasEndedTurn)
        {
            EndTurn();
        }
    }

    public void ChangePauseState()
    {
        isPaused = !isPaused;
        UIController.instance.SetPauseButtonObj(isPaused);
    }

    // Card-related

    public void RefreshCards()
    {
        playerTurn.currency -= refreshCardsCost;
        int cardsAvailable = Mathf.Min(standardCardVisibility, cardDrawPile.Count);
        for (int i = 0; i < cardsAvailable; i++)
        {
            Card newCard = cardDrawPile[0];
            cardDrawPile.RemoveAt(0);
            cardDiscardPile.Add(newCard);
        }
        UIController.instance.SetGameCardsAvailableToPlayerObj(GameCardsAvailableToPlayer(playerTurn), playerMakingCurrentDecision);
        AddDecision(Decision.BrowseCards(playerMakingCurrentDecision));
        GameEvent.CardsRefreshed(playerTurn.ID);
    }

    public void GainPayToReduceDamage(Player playerToReduceDamage, Player playerCausingDamage, int damage)
    {
        AddDecisionAsPriority(Decision.PayToReduceDamage(playerToReduceDamage, playerCausingDamage, damage));
    }
    public void PayToReduceDamage(Player playerToReduceDamage, Player playerCausingDamage, int damage, int damageReduction)
    {
        if (damageReduction > 0)
        {
            UIController.instance.AddGameEvent(playerToReduceDamage.formattedName + " reduced damage taken by " + damageReduction + " for " + (2 * damageReduction) + " currency by using their Health Insurance.");
        }
        else
        {
            Debug.Log(playerToReduceDamage.formattedName + " should not be able to reduce damage taken by 0.");
        }
        playerToReduceDamage.currency -= 2 * damageReduction;
        playerToReduceDamage.hasPaidToReduceDamage = true;
        playerToReduceDamage.ChangeHealth(playerCausingDamage, -(damage - damageReduction));
    }

    public void PayToBecomeInvulnerable(Player playerToBecomeInvulnerable, Player playerDealingDamage, int damage)
    {
        UIController.instance.AddGameEvent(playerToBecomeInvulnerable.formattedName + " negated " + damage + " damage" + (playerDealingDamage != null ? " from " + playerDealingDamage.formattedName : "") + " due to their Temporary Invulnerability.");
        playerToBecomeInvulnerable.currency -= 2;
        playerToBecomeInvulnerable.isInvulnerable = true;
        playerToBecomeInvulnerable.hasHadOptionToBecomeInvulnerable = true;
    }

    public void GainRollToReduceDamage(Player playerToReduceDamage, Player playerCausingDamage, int damage)
    {
        InitiateDice(Mathf.Min(damage, maxDice));
        AddDecisionAsPriority(Decision.RollToReduceDamage(playerToReduceDamage, playerCausingDamage, damage));
    }
    public void RollToReduceDamage(Player playerToReduceDamage, Player playerDealingDamage, int damage)
    {
        rollNum = maxRolls;
        RollDice(true);

        int numHeals = 0;
        for (int i = 0; i < currentDiceResults.Length; i++)
        {
            if (currentDiceResults[i] == 4)
            {
                numHeals += 1;
            }
        }
        if (numHeals > 0)
        {
            UIController.instance.AddGameEvent(playerToReduceDamage.formattedName + " reduced their damage taken by " + numHeals + " with their Hearty Roll.");
        }
        else
        {
            UIController.instance.AddGameEvent(playerToReduceDamage.formattedName + " did not reduce their damage taken with their Hearty Roll.");
        }
        playerToReduceDamage.hasRolledToReduceDamage = true;
        playerToReduceDamage.ChangeHealth(playerDealingDamage, - (damage - numHeals));
    }

    public void GainRerollEnemyDie(Player playerWithCard)
    {
        SetSingleDieSelectableOnly(true);
        if (playerWithCard.playerType == Player.PlayerType.Human)
        {
            SetDiceInteractable(true);
        }
        AddDecision(Decision.RerollEnemyDie(playerWithCard));
    }

    public void IgnoreSpecialEnemyRollChanges(Player playerWithCard)
    {
        UIController.instance.AddGameEvent(playerWithCard.formattedName + " did not use any Special Enemy Roll Changes.");
        AddPostRollsDecisions();
    }

    public void RerollEnemyDie(Player playerWithCard)
    {
        int oldResult = currentDiceResults[FirstUnheldDie()];
        RollDice(false);
        int result = currentDiceResults[FirstUnheldDie()];
        UIController.instance.AddGameEvent(playerWithCard.formattedName + " rerolled a " + oldResult + " to a " + result + " as they are a Die King.");
        if (result == 4)
        {
            playerWithCard.RemoveCard(playerWithCard.GetCard(Card.ID.DieKing));
        }
        SetSingleDieSelectableOnly(false);
        SetDiceInteractable(false);
        AddPostRollsDecisions();
    }

    public void GainExtraRoll(int cost)
    {
        if (cost == 0)
        {
            playerTurn.extraRolls -= 1;
            UIController.instance.AddGameEvent(playerTurn.formattedName + " used an extra roll because they are a Rock and Roller.");
            if (playerTurn.extraRolls == 0)
            {
                playerTurn.RemoveCard(playerTurn.cards.Find(o => o.id == Card.ID.RockAndRoller));
            }
        }
        else
        {
            UIController.instance.AddGameEvent(playerTurn.formattedName + " bought an extra roll for 1 currency by using their Costly Roll.");
            playerTurn.currency -= cost;
        }
        maxRolls += 1;
        AddDecision(Decision.IntermediateRoll(playerTurn));
    }

    public void GainThreesReroll()
    {
        for (int i = 0; i < currentDiceResults.Length; i++)
        {
            if (currentDiceResults[i] == 2)
            {
                SetDieInteractable(i, true);
            }
            else
            {
                SetDieHeld(i, true);
                SetDieInteractable(i, false);
            }
        }
        UIController.instance.AddGameEvent(playerTurn.formattedName + " may reroll their threes due to a Tertiary Allergy.");
        maxRolls += 1;
        AddDecision(Decision.IntermediateRoll(playerTurn));
    }

    public void GainDieChange(int cost)
    {
        if (cost == 0)
        {
            UIController.instance.AddGameEvent(playerTurn.formattedName + " is changing a die result because they are a Die Forger.");
            playerTurn.RemoveCard(playerTurn.cards.Find(o => o.id == Card.ID.DieForger));
        }
        else
        {
            UIController.instance.AddGameEvent(playerTurn.formattedName + " is changing a die result for 2 currency because they are a Die Caster.");
            playerTurn.currency -= cost;
        }
        SetSingleDieSelectableOnly(true);
        AddDecision(Decision.ChangeDieResult(playerTurn));
    }

    public void GainDieChangeToValue(int dieValue)
    {
        UIController.instance.AddGameEvent(playerTurn.formattedName + " is changing a die result to " + dieValue + " using their One to One.");

        if (dieValue == 0)
        {
            playerHasChangedDieToOne = true;
        }

        SetSingleDieSelectableOnly(true);
        AddDecision(Decision.ChangeDieResultToValue(playerTurn, dieValue));
    }

    public void ChangeDieResult(int result)
    {
        int dieToChange = FirstUnheldDie();
        UIController.instance.AddGameEvent(playerTurn.formattedName + " changed a die from a " + currentDiceResults[dieToChange] + " to a " + result + ".");
        SetDieValue(dieToChange, result);
        SetSingleDieSelectableOnly(false);
        AddPostRollsDecisions();
    }

    public void GainHealEnemy()
    {
        AddDecision(Decision.ChooseEnemyToHeal(playerTurn));
    }

    public void ChooseEnemyToHeal(int playerToHealID)
    {
        enemyToHeal = players[playerToHealID];
        AddDecision(Decision.HealEnemy(playerTurn));
    }

    public void HealEnemy(int healAmount)
    {
        enemyToHeal.ChangeHealth(playerTurn, healAmount);
        int cost = Mathf.Min(enemyToHeal.currency, healAmount * 2);
        enemyToHeal.currency -= cost;
        playerTurn.currency += cost;
        for (int i = 0; i < healAmount; i++)
        {
            int dieToUse = FirstDieWithResult(4);
            SetDieValue(dieToUse, -1);
            SetDieInteractable(dieToUse, false);
        }
        AddPostRollsDecisions();
    }

    public void GainReduceVenomLevel()
    {
        AddDecision(Decision.ReduceVenomLevel(playerTurn));
    }

    public void ReduceVenomLevel(int reduction)
    {
        UIController.instance.AddGameEvent(playerTurn.formattedName + " reduced " + reduction + " venom level.");
        playerTurn.venomLevel -= reduction;
        for (int i = 0; i < reduction; i++)
        {
            int dieToUse = FirstDieWithResult(4);
            SetDieValue(dieToUse, -1);
            SetDieInteractable(dieToUse, false);
        }
        AddPostRollsDecisions();
    }

    public void GainReviveDeadDice()
    {
        AddDecision(Decision.ReduceDeadDice(playerTurn));
    }

    public void ReviveDeadDie(int reduction)
    {
        UIController.instance.AddGameEvent(playerTurn.formattedName + " revived " + reduction + " dead dice.");
        playerTurn.deadDice -= reduction;
        for (int i = 0; i < reduction; i++)
        {
            int dieToUse = FirstDieWithResult(4);
            SetDieValue(dieToUse, -1);
            SetDieInteractable(dieToUse, false);
        }
        AddPostRollsDecisions();
    }

    public void IgnoreSpecialRollChanges()
    {
        playerFinishedWithSpecialRollChanges = true;
        AddPostRollsDecisions();
    }

    public void IgnoreSpecialDiceUsage()
    {
        playerFinishedWithSpecialDieUsage = true;
        AddPostRollsDecisions();
    }

    public void IgnoreSpecialDamageReduction(Player player, Player playerCausingDamage, int damage)
    {
        player.hasPaidToReduceDamage = true;
        player.hasHadOptionToBecomeInvulnerable = true;
        player.hasRolledToReduceDamage = true;
        player.ChangeHealth(playerCausingDamage, -damage);
    }

    public void FinishedDuplicatingCards()
    {

    }

    public void FinishedRefundingCards()
    {

    }

    // Player-invoked

    public void LeaveInside(Player playerToLeave, bool tryForcePlayerTurnToEnter)
    {
        if (playerToLeave != null)
        {
            if (PlayerIsInside(playerToLeave))
            {
                if (playerToLeave.HasCard(Card.ID.Sneaky) && damageDealtThisTurn[playerToLeave.ID] > 0)
                {
                    playerToLeave.ChangeHealth(null, damageDealtThisTurn[playerToLeave.ID]);
                }
                GameEvent.LeftInside(playerToLeave.ID);

                if (playersInside[0].ID == playerToLeave.ID)
                {
                    SimulationController.instance.AddSimulationToQueue(SimulationController.instance.MoveKittenToHomeViaDoor(playerToLeave.ID));
                    if (playersInside.Count > 1)
                    {
                        SimulationController.instance.AddSimulationToQueue(SimulationController.instance.MoveKittenToInside1ViaInsideLeftThenDoor(playersInside[1].ID));
                    }
                }
                else
                {
                    SimulationController.instance.AddSimulationToQueue(SimulationController.instance.MoveKittenToHomeViaInsideLeftThenDoor(playerToLeave.ID));
                }

                playersInside.Remove(playerToLeave);
                UIController.instance.SetPlayerInsideObj(playerToLeave, false);
                previousPlayerInside = playerToLeave;

                if (tryForcePlayerTurnToEnter)
                {
                    if (playerToLeave.ID == playerTurn.ID)
                    {
                        Debug.Log("The Player that has just left is the current Player Turn (" + playerTurn.formattedName + "); they cannot enter Inside on the same turn.");
                    }
                    else if (!insideHasFreeSlot)
                    {
                        Debug.Log("No free slot to enter Inside.");
                    }
                    else if (livingPlayers.Length == 0)
                    {
                        Debug.Log("No Living Players to enter Inside.");
                    }
                    else
                    {
                        EnterInside();
                    }
                }
            }
            else
            {
                Debug.Log(playerToLeave.formattedName + " is not inside and thus cannot leave.");
            }
        }
        else
        {
            Debug.Log("Player To Leave is null.");
        }
    }

    public void LeaveInside()
    {
        if (playersInside.Count > 0)
        {
            LeaveInside(playersInside[playersInside.Count - 1], true);
        }
        else
        {
            Debug.Log("No Players are inside.");
        }
    }

    public void StayInside(Player playerToStay)
    {

    }

    public void PlayerClickDie(int die)
    {
        if (!canSelectSingleDieOnly)
        {
            AlternateDieHeld(die);
        }
        else
        {
            Debug.Log("single die only");
            SetDieHeld(die, false);
            for (int i = 0; i < currentDiceHeld.Length; i++)
            {
                if (i != die)
                {
                    SetDieHeld(i, true);
                }
            }
        }
    }

    public void BuyCardInstantly(Player player, Card card)
    {
        player.BuyCard(card);
    }
    public void CheckIfPlayersCanBuyCardInstantly()
    {
        foreach (Player livingPlayer in livingPlayers)
        {
            if (livingPlayer.ID != playerTurn.ID && livingPlayer.HasCard(Card.ID.QuickDeal) && cardDrawPile.Count > standardCardVisibility && !playerHasBoughtCardInstantly[livingPlayer.ID])
            {
                Card newCard = cardDrawPile[standardCardVisibility];
                if (livingPlayer.currency >= newCard.CostToPlayer(livingPlayer))
                {
                    Debug.Log("Decision given to " + livingPlayer.name);
                    AddDecisionAsPriority(Decision.BuyCardInstantly(livingPlayer, newCard));
                    playerHasBoughtCardInstantly[livingPlayer.ID] = true;
                    return;
                }
            }
        }
    }

    // Debug Only

    public void PlayerKillSelf(Player player)
    {
        if (player != null)
        {
            if (player.isAlive)
            {
                player.ChangeHealth(null, -player.health);
            }
            else
            {
                Debug.Log(player.formattedName + " is already dead.");
            }
        }
    }

    public void PlayerKillSelf()
    {
        PlayerKillSelf(playerTurn);
    }

    public void PlayerAdd10Currency()
    {
        if (playerTurn != null)
        {
            playerTurn.currency += 10;
        }
    }

    public void PlayerAdd1Currency(Player player)
    {
        if (player != null)
        {
            player.currency += 1;
        }
    }

    public void PlayerSubtract1Currency(Player player)
    {
        if (player != null)
        {
            player.currency -= 1;
        }
    }

    public void PlayerAdd1Currency()
    {
        PlayerAdd1Currency(playerTurn);
    }

    public void PlayerAdd1Point(Player player)
    {
        if (player != null)
        {
            player.points += 1;
        }
    }

    public void PlayerSubtract1Point(Player player)
    {
        if (player != null)
        {
            player.points -= 1;
        }
    }

    public void PlayerAdd1Health(Player player)
    {
        player.SetHealthUnmodified(player.health + 1);
    }

    public void PlayerSubtract1Health(Player player)
    {
        player.SetHealthUnmodified(player.health - 1);
    }

    // Useful Functions
    public Player NextLivingPlayer(Player player)
    {
        if (livingPlayers.Length > 0)
        {
            int returnID = player.ID;
            while (true)
            {
                if (returnID == numPlayers - 1)
                {
                    returnID = 0;
                }
                else
                {
                    returnID += 1;
                }
                if (players[returnID].isAlive)
                {
                    return players[returnID];
                }
            }
        }
        else
        {
            return null;
        }
    }
    public Player NextLivingPlayer()
    {
        return NextLivingPlayer(playerTurn);
    }
    public Player NextLivingPlayerOutside(Player player)
    {
        if (livingPlayers.Length > 0 && playersOutside.Length > 0)
        {
            int returnID = player.ID;
            while (true)
            {
                if (returnID == numPlayers - 1)
                {
                    returnID = 0;
                }
                else
                {
                    returnID += 1;
                }
                if (players[returnID].isAlive && !PlayerIsInside(players[returnID]))
                {
                    return players[returnID];
                }
            }
        }
        else
        {
            return null;
        }
    }
    public Player RandomPlayer()
    {
        if (players.Length > 0)
        {
            return players[UnityEngine.Random.Range(0, players.Length)];
        }
        else
        {
            return null;
        }
    }

    public Player PreviousLivingPlayer(Player player)
    {
        if (livingPlayers.Length > 0)
        {
            int returnID = player.ID;
            while (true)
            {
                if (returnID == 0)
                {
                    returnID = numPlayers - 1;
                }
                else
                {
                    returnID -= 1;
                }
                if (players[returnID].isAlive)
                {
                    return players[returnID];
                }
            }
        }
        else
        {
            return null;
        }
    }
    public Player PreviousLivingPlayer()
    {
        return PreviousLivingPlayer(playerTurn);
    }

    public Player PlayerWithLeastHealth(bool useInsidePlayers)
    {
        Player[] players = useInsidePlayers ? playersInside.ToArray() : playersOutside;
        if (players.Length > 1)
        {
            Player returnPlayer = players[0];
            for (int i = 1; i < players.Length; i++)
            {
                if (players[i].health < returnPlayer.health)
                {
                    returnPlayer = players[i];
                }
            }
            return returnPlayer;
        }
        else if (players.Length == 1)
        {
            return players[0];
        }
        else
        {
            return null;
        }
    }

    public Player PlayerWithMostHealth(bool useInsidePlayers)
    {
        Player[] players = useInsidePlayers ? playersInside.ToArray() : playersOutside;
        if (players.Length > 1)
        {
            Player returnPlayer = players[0];
            for (int i = 1; i < players.Length; i++)
            {
                if (players[i].health > returnPlayer.health)
                {
                    returnPlayer = players[i];
                }
            }
            return returnPlayer;
        }
        else if (players.Length == 1)
        {
            return players[0];
        }
        else
        {
            return null;
        }
    }

    public Player[] HealableEnemies(Player playerHealing)
    {
        // Player is alive, Player has less than max health, Player has more than 0 currency, Player is not the Player healing
        return livingPlayers.Where(o => o.health < o.maxHealth && o.currency > 0 && o.ID != playerHealing.ID).ToArray();
    }

    public Player[] OtherLivingPlayers(Player playerOtherThan)
    {
        return players.Where(o => o.isAlive).Where(o => o.ID != playerOtherThan.ID).ToArray();
    }

    public Player[] OtherVulnerablePlayers(Player playerOtherThan)
    {
        return OtherLivingPlayers(playerOtherThan).Where(o => o.health <= playerOtherThan.healthWorryLevel).ToArray();
    }

    public Player[] OtherVulnerablePlayers(Player playerOtherThan, bool areInside)
    {
        return OtherLivingPlayers(playerOtherThan).Where(o => o.health <= playerOtherThan.healthWorryLevel).Where(o => PlayerIsInside(o) == areInside).ToArray();
    }

    // Game cards that can be bought by Player
    public Card[] GameCardsAvailableToPlayer(Player player)
    {
        int cardVisibility = player != null ? player.cardVisibility : standardCardVisibility;
        int cardsAvailable = Mathf.Min(cardVisibility, cardDrawPile.Count);
        return cardDrawPile.Take(cardsAvailable).ToArray();
    }

    // All cards that can be bought by Player (including Player cards if Player has right card)
    public Card[] AllCardsAvailableToPlayer(Player player)
    {
        List<Card> returnList = new List<Card>();
        returnList.AddRange(GameCardsAvailableToPlayer(player));
        if (player.HasCard(Card.ID.Bargainer))
        {
            foreach (Player livingPlayer in livingPlayers)
            {
                if (livingPlayer.ID != player.ID)
                {
                    returnList.AddRange(livingPlayer.cards);
                }
            }
        }
        return returnList.ToArray();
    }

    public bool PlayerIsInside(Player player)
    {
        return playersInside.Contains(player);
    }

    public void ShuffleDrawPile()
    {
        System.Random rng = new System.Random();
        int n = cardDrawPile.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            Card card = cardDrawPile[k];
            cardDrawPile[k] = cardDrawPile[n];
            cardDrawPile[n] = card;
        }
    }

    public bool AllDiceAreHeld()
    {
        bool returnBool = true;
        for (int i = 0; i < currentDiceHeld.Length; i++)
        {
            if (!currentDiceHeld[i])
            {
                returnBool = false;
            }
        }
        return returnBool;
    }

    public int[] DiceSummary()
    {
        int[] diceSummary = new int[6];
        rollSummary = "";
        for (int i = 0; i < currentDiceResults.Length; i++)
        {
            if (currentDiceResults[i] != -1)
            {
                rollSummary += "[" + currentDiceResults[i] + "] ";
                diceSummary[currentDiceResults[i]] += 1;
            }
        }
        return diceSummary;
    }

    public int[] DiceSummary(List<int> diceIndicies)
    {
        int[] diceSummary = new int[6];
        rollSummary = "";
        for (int i = 0; i < currentDiceResults.Length; i++)
        {
            if (currentDiceResults[i] != -1 && diceIndicies.Contains(i))
            {
                rollSummary += "[" + currentDiceResults[i] + "] ";
                diceSummary[currentDiceResults[i]] += 1;
            }
        }
        return diceSummary;
    }

    public int[] HeldDiceSummary()
    {
        int[] diceSummary = new int[6];
        for (int i = 0; i < currentDiceResults.Length; i++)
        {
            if (currentDiceHeld[i] && currentDiceResults[i] != -1)
            {
                diceSummary[currentDiceResults[i]] += 1;
            }
        }
        return diceSummary;
    }

    public int[] NotHeldDiceSummary()
    {
        int[] diceSummary = new int[6];
        for (int i = 0; i < currentDiceResults.Length; i++)
        {
            if (!currentDiceHeld[i] && currentDiceResults[i] != -1)
            {
                diceSummary[currentDiceResults[i]] += 1;
            }
        }
        return diceSummary;
    }
    public int NumDiceHeld()
    {
        return currentDiceHeld.Count(o => o);
    }

    public int FirstDieWithResult(int result)
    {
        return Array.FindIndex(currentDiceResults, o => o == result);
    }

    public Card[] AllEnemyCards(Player player)
    {
        List<Card> returnList = new List<Card>();
        foreach (Player livingPlayer in livingPlayers)
        {
            if (livingPlayer.ID != player.ID)
            {
                returnList.AddRange(livingPlayer.cards);
            }
        }
        return returnList.ToArray();
    }

    public void AddPostRollsDecisions()
    {
        // See if Player can do a Special Roll Change
        List<Card.ID> specialRollChange;
        if (!playerFinishedWithSpecialRollChanges)
        {
            specialRollChange = playerTurn.AvailableSpecialRollChanges();
            if (specialRollChange.Count > 0)
            {
                AddDecision(Decision.SpecialRollChanges(playerTurn, specialRollChange));
                return;
            }
        }
        playerFinishedWithSpecialRollChanges = true;

        // See if Enemy can do a Special Roll Change
        for (int i = 0; i < livingPlayers.Length; i++)
        {
            if (!playerFinishedWithChangingEnemyDie[livingPlayers[i].ID])
            {
                playerFinishedWithChangingEnemyDie[livingPlayers[i].ID] = true;
                if (livingPlayers[i].HasCard(Card.ID.DieKing) && playerTurn.ID != livingPlayers[i].ID)
                {
                    AddDecision(Decision.SpecialEnemyRollChanges(livingPlayers[i]));
                    return;
                }
            }
        }

        // See if Player can do a Special Die Usage
        List<Card.ID> specialDieUsages;
        if (!playerFinishedWithSpecialDieUsage)
        {
            specialDieUsages = playerTurn.AvailableSpecialDieUsages();
            if (specialDieUsages.Count > 0)
            {
                AddDecision(Decision.SpecialDieUsage(playerTurn, specialDieUsages));
                return;
            }
        }

        // No more Special Post Rolls Decisions, just go for standard
        SetAllAvailableDiceActive(false);
        AddDecision(Decision.NoMoreRolls(playerTurn));
        //Resolve();
    }

    public void TestFunction()
    {
        Debug.Log("Yes");
    }
}
