using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using System.Linq;

public class Decision
{
    public static float defaultDecisionTime = 5f;

    public static float defaultAIDecisionTime
    {
        get
        {
            return 1f / SimulationController.instance.simulationSpeed;
        }
    }

    public enum ID
    {
        StartTurn,
        FirstRoll,
        IntermediateRoll,
        NoMoreRolls,
        LeaveOrStay,
        BrowseCards,
        BuyCardInstantly,
        EndTurn,
        NewGame,
        PayToReduceDamage,
        RollToReduceDamage,
        SpecialRollChange,
        SpecialDieUsage,
        GainRerollEnemyDie,
        RerollEnemyDie,
        SpecialDamageReduction,
        ChangeDieResult,
        ChangeDieResultToValue,
        ChooseEnemyToHeal,
        HealEnemy,
        ReduceVenomLevel,
        ReduceDeadDice,
        SetCardBeingDuplicatedFirstTime,
        ChangeCardBeingDuplicated,
        RefundCards
    }

    public ID id { get; }

    public Player player { get; }

    public string description { get; }

    public Option[] options { get; }

    public Option defaultOption { get; }

    public int argument;

    public int defaultArgument { get; }

    public float timeToRespond { get; set; }

    public bool isNecessary { get; }

    public bool isSequential { get; }

    public bool isDiscrete { get; }

    // Used only for Roll decisions
    public List<int> undecidedDice { get; set; }

    public bool decisionMade;

    public bool HasOption(Option.ID optionID)
    {
        return GetOption(optionID) != null;
    }

    public Option GetOption(Option.ID optionID)
    {
        return Array.Find(options, o => o.id == optionID);
    }

    public void MakeDecision(Option option, int argument)
    {
        if (!decisionMade)
        {
            decisionMade = true;
            option.method.Invoke(argument);
            GameController.instance.currentDecisionPresented = null;

            // If card is open, close in case of trying to buy or refund out of turn
            if (player != null)
            {
                GameObject currentCardLarge = UIController.instance.currentCardLargeObj;
                if (player.playerType == Player.PlayerType.Human && currentCardLarge != null)
                {
                    currentCardLarge.GetComponent<CardLargeObj>().CloseCardLargeView();
                }
            }
        }
        else
        {
            Debug.Log("A Decision has already been made for '" + description + "'.");
        }
    }

    public void MakeDefaultDecision()
    {
        MakeDecision(defaultOption, defaultArgument);
    }

    public void MakeAIDecision()
    {
        switch (id)
        {
            case ID.IntermediateRoll:
                undecidedDice = new List<int>();
                int pointsNeededToWin;
                int[] currentHeldSummary = GameController.instance.HeldDiceSummary();

                GameController.instance.DiceSummary();
                Debug.Log(player.formattedName + " rolled " + GameController.instance.rollSummary);

                // Dice that aren't held and aren't definitely going to be rerolled
                ResetUndecidedDice();

                // Change priorities under certain circumstances
                pointsNeededToWin = GameController.instance.winPoints - player.points;
                List<Card.AttributeFamilyID> modifiedPreferences = new List<Card.AttributeFamilyID>();
                if (pointsNeededToWin > 0 && pointsNeededToWin <= player.pointsRisk)
                {
                    // Set points as priority
                    AddDecisionEvent("wants to prioritise points", "they think they can win this turn.");
                    modifiedPreferences.Add(Card.AttributeFamilyID.Points);
                }
                else if (player.health + currentHeldSummary[4] <= player.healthWorryLevel)
                {
                    // Set health as priority
                    modifiedPreferences.Add(Card.AttributeFamilyID.Health);
                    AddDecisionEvent("wants to prioritise health", "they think they're health is too low.");
                }
                else if (player.HasCardWithAttributeInFamily(Card.AttributeFamilyID.CurrencyHoarding))
                {
                    // Set currency as a priority
                    modifiedPreferences.Add(Card.AttributeFamilyID.Currency);
                    AddDecisionEvent("wants to prioritise currency", "they have cards for which currency is useful.");
                }
                else
                {
                    // AddDecisionEvent("doesn't want to prioritise anything in particular", "they see no urgent reason to change their play style.");
                }

                for (int i = 0; i < player.attributePreferences.Length; i++)
                {
                    if (!modifiedPreferences.Contains(player.attributePreferences[i]))
                    {
                        modifiedPreferences.Add(player.attributePreferences[i]);
                    }
                }

                // Make decision of which Dice to hold
                for (int i = 0; i < modifiedPreferences.Count; i++)
                {
                    AIDiceDecision(modifiedPreferences[i]);
                }
                if (GameController.instance.AllDiceAreHeld())
                {
                    MakeDecision(Option.KeepRolls, 0);
                    AddDecisionEvent("is keeping their rolls", "they like all of the current dice results.");
                }
                else
                {
                    MakeDecision(Option.Roll, 0);
                    AddDecisionEvent("is rolling again", "they don't like all of the current dice results.");
                }
                break;

            case ID.LeaveOrStay:
                // Leave if health is less than or equal to health worry level, or player hates inside
                // BUT stay inside if points needed to win is less than or equal to points for staying inside, OR slightly more than that but player is risky, and your turn is next
                if (GameController.instance.NextLivingPlayer().ID == player.ID && player.points >= GameController.instance.winPoints - GameController.instance.startTurnInsidePoints - player.pointsRisk && player.points < GameController.instance.winPoints)
                {
                    MakeDecision(Option.Stay(player), 0);
                    AddDecisionEvent("is staying inside", "they think they can get the points to win next turn.");
                }
                else if (GameController.instance.playerTurn.points >= GameController.instance.winPoints - GameController.instance.enterInsidePoints)
                {
                    MakeDecision(Option.Stay(player), 0);
                    AddDecisionEvent("is staying inside", GameController.instance.playerTurn.formattedName + " will have enough points to win if they enter inside now.");
                }
                else if (player.health <= player.healthWorryLevel)
                {
                    MakeDecision(Option.Leave(player), 0);
                    AddDecisionEvent("is leaving inside", "they think they will die if they stay.");
                }
                else if (player.hatesInside)
                {
                    MakeDecision(Option.Leave(player), 0);
                    AddDecisionEvent("is leaving inside", "they don't like being inside.");
                }
                else
                {
                    MakeDecision(Option.Stay(player), 0);
                    AddDecisionEvent("is staying inside", "they see no reason to leave.");
                }
                break;

            case ID.BrowseCards:
                Card[] cardsAvailable = GameController.instance.AllCardsAvailableToPlayer(player);

                if (cardsAvailable.Length == 0)
                {
                    MakeDecision(Option.FinishedBrowsingCards, 0);
                    AddDecisionEvent("is not browsing the cards", "there are no cards available to them.");
                    return;
                }

                // Buy any affordable NoBrainer cards
                Card[] noBrainerCards = Card.CardsWithAttribute(cardsAvailable, Card.Attribute.NoBrainer);
                for (int i = 0; i < noBrainerCards.Length; i++)
                {
                    if (player.currency >= noBrainerCards[i].CostToPlayer(player))
                    {
                        AIBuyCard(noBrainerCards[i]);
                        AddDecisionEvent("is buying '" + noBrainerCards[i].name + "'", "it's a No Brainer.");
                        return;
                    }
                }

                // First things first, if a card is available that will make you win and you'll survive, then buy it.
                Card[] instantPointsCards = Card.CardsWithAttribute(cardsAvailable, Card.Attribute.PointsInstant);
                // Only check for single Cards at first, expand this functionality later to check for combinations
                for (int i = 0; i < instantPointsCards.Length; i++)
                {
                    if (player.points < GameController.instance.winPoints)
                    {
                        if (player.points + instantPointsCards[i].GetAttributeValue(Card.Attribute.PointsInstant) >= GameController.instance.winPoints)
                        {
                            if (player.currency >= instantPointsCards[i].CostToPlayer(player))
                            {
                                if (instantPointsCards[i].GetAttributeValue(Card.Attribute.HealthInstant) + player.health > 0)
                                {
                                    AIBuyCard(instantPointsCards[i]);
                                    AddDecisionEvent("is buying '" + instantPointsCards[i].name + "'", "it will win them the game.");
                                    return;
                                }
                            }
                        }
                    }
                }

                // If there is a card available that would likely cause another player to win next turn, either buy or refresh cards
                // Set a flag that player wants to refresh, then only buy a card if it will leave at least 2 currency, otherwise refresh.
                bool wantsToRefreshIfNotBuying = false;
                foreach (Player livingPlayer in GameController.instance.OtherLivingPlayers(player))
                {
                    // Only check for single Cards at first, expand this functionality later to check for combinations
                    for (int i = 0; i < instantPointsCards.Length; i++)
                    {
                        if (livingPlayer.points + instantPointsCards[i].GetAttributeValue(Card.Attribute.PointsInstant) >= GameController.instance.winPoints)
                        {
                            wantsToRefreshIfNotBuying = true;
                            AddDecisionEvent("will refesh the cards if they don't find one to buy", "they think " + livingPlayer.formattedName + " will win if they buy the card '" + instantPointsCards[i].name + "'.");
                            break;
                        }
                    }
                }

                // Conversely, if no cards exist that could make a Player win but another Player is close to victory, then actively dont refresh cards
                // However, still buy cards if one is desired
                bool activelyWantsToNotRefresh = false;
                if (!wantsToRefreshIfNotBuying)
                {
                    foreach (Player livingPlayer in GameController.instance.OtherLivingPlayers(player))
                    {
                        if (GameController.instance.winPoints - livingPlayer.points <= player.enemyPointsRisk)
                        {
                            activelyWantsToNotRefresh = true;
                            AddDecisionEvent("will definitely not refesh the cards if they don't find one to buy", "they think a card may appear that will help " + livingPlayer.formattedName + " to win.");
                            break;
                        }
                    }
                }

                // If the player is below their low health threshold, then buy

                // If a Card is available that would kill this Player if not bought, either buy or reset
                Card[] damageCards = Card.CardsWithAttribute(cardsAvailable, Card.Attribute.DamageInstant);
                // Only check for single Cards at first, expand this functionality later to check for combinations
                for (int i = 0; i < damageCards.Length; i++)
                {
                    if (player.health + damageCards[i].GetAttributeValue(Card.Attribute.DamageInstant) <= 0)
                    {
                        if (player.currency + (wantsToRefreshIfNotBuying ? -2 : 0) >= damageCards[i].CostToPlayer(player) && player.health + damageCards[i].GetAttributeValue(Card.Attribute.HealthInstant) <= 0)
                        {
                            AIBuyCard(damageCards[i]);
                            AddDecisionEvent("is buying '" + damageCards[i].name + "'", "they think they will die if someone else buys it.");
                            return;
                        }
                        else if (!wantsToRefreshIfNotBuying)
                        {
                            wantsToRefreshIfNotBuying = true;
                            AddDecisionEvent("will refesh the cards if they don't find one to buy", "they think they will die if someone buys the card '" + damageCards[i].name + "'.");
                        }
                    }
                }

                // If a card is available that will kill another Player without detriment to you and , kill them
                // Detriment may include taking more than acceptable damage or having to enter inside

                // If a card that is available matches your personality, then buy it
                if (player.lovesBeingMean)
                {
                    Card[] meanCards = Card.CardsWithAttributeInFamily(cardsAvailable, Card.AttributeFamilyID.WeakenEnemies);
                    for (int i = 0; i < meanCards.Length; i++)
                    {
                        if (player.currency + (wantsToRefreshIfNotBuying ? -2 : 0) >= meanCards[i].CostToPlayer(player))
                        {
                            AIBuyCard(meanCards[i]);
                            AddDecisionEvent("is buying '" + meanCards[i].name + "'", "they like being mean.");
                            return;
                        }
                    }
                }

                if (player.lovesDiceControl)
                {
                    Card[] diceControlCards = Card.CardsWithAttributeInFamily(cardsAvailable, Card.AttributeFamilyID.DiceControl);
                    for (int i = 0; i < diceControlCards.Length; i++)
                    {
                        if (player.currency + (wantsToRefreshIfNotBuying ? -2 : 0) >= diceControlCards[i].CostToPlayer(player))
                        {
                            AIBuyCard(diceControlCards[i]);
                            AddDecisionEvent("is buying '" + diceControlCards[i].name + "'", "they like having better control over their dice rolls.");
                            return;
                        }
                    }
                }

                if (player.lovesCards)
                {
                    for (int i = 0; i < cardsAvailable.Length; i++)
                    {
                        // CHECK IF CARD IS DETRIMENTAL, I.E. LOSE HEALTH TO UNDER WORRY LEVEL, GO INSIDE BUT HATES INSIDE,
                        bool isDetrimental = cardsAvailable[i].GetAttributeValue(Card.Attribute.HealthInstant) + player.health <= player.healthWorryLevel;
                        isDetrimental = isDetrimental || cardsAvailable[i].HasAttribute(Card.Attribute.EnterInside) && player.hatesInside;
                        if (!isDetrimental)
                        {
                            if (player.currency + (wantsToRefreshIfNotBuying ? -2 : 0) >= cardsAvailable[i].CostToPlayer(player))
                            {
                                AIBuyCard(cardsAvailable[i]);
                                AddDecisionEvent("is buying '" + cardsAvailable[i].name + "'", "they think buying any card is useful.");
                                return;
                            }
                        }
                    }
                }

                if (!activelyWantsToNotRefresh)
                {
                    if (player.currency >= 2)
                    {
                        if (wantsToRefreshIfNotBuying)
                        {
                            MakeDecision(Option.RefreshCards, 0);
                            AddDecisionEvent("is refreshing the game cards", "they think don't want any other players to get the current game cards.");
                            return;
                        }
                        else if (player.currency >= player.tooMuchCurrencyLevel && !player.HasCardWithAttributeInFamily(Card.AttributeFamilyID.CurrencyHoarding))
                        {
                            MakeDecision(Option.RefreshCards, 0);
                            AddDecisionEvent("is refreshing the game cards", "they think they have too much currency but none of the cards are appealing to them.");
                            return;
                        }
                    }
                }

                MakeDecision(Option.FinishedBrowsingCards, 0);
                break;

            case ID.FirstRoll:
                MakeDefaultDecision();
                break;

            case ID.StartTurn:
                MakeDefaultDecision();
                break;

            case ID.NoMoreRolls:
                MakeDefaultDecision();
                break;

            case ID.EndTurn:
                MakeDefaultDecision();
                break;

            case ID.SpecialRollChange:
                Option gainExtraRollOption = GetOption(Option.ID.GainExtraRoll);
                Option payToGainExtraRollOption = GetOption(Option.ID.PayToGainExtraRoll);
                Option gainChangeDieResultOption = GetOption(Option.ID.GainChangeDieResult);
                Option gainChangeDieResultTo1Option = GetOption(Option.ID.GainChangeDieResultTo1);
                Option payToGainChangeDieResultOption = GetOption(Option.ID.PayToGainChangeDieResult);
                Option gainRerollThreesOption = GetOption(Option.ID.GainRerollThrees);
                // Firstly, if the play has held all their dice, then don't change any
                if (GameController.instance.AllDiceAreHeld())
                {
                    MakeDefaultDecision();
                    AddDecisionEvent("isn't making any special roll changes", "they are happy with the rolls they have.");
                    return;
                }
                // Then, if there are ways you can win with your die changes, then do
                int[] currentDiceSummary = GameController.instance.DiceSummary();
                int numDice = GameController.instance.currentDiceResults.Length;
                bool changingTo1WillBringPoints = currentDiceSummary[0] >= 2; // There are at least 2 1s in the roll
                changingTo1WillBringPoints = changingTo1WillBringPoints && currentDiceSummary[0] < numDice; // All dice aren't 1s
                changingTo1WillBringPoints = changingTo1WillBringPoints && (currentDiceSummary[3] > 0 || currentDiceSummary[4] > 0 || currentDiceSummary[5] > 0 || (currentDiceSummary[1] > 0 && currentDiceSummary[1] < 3) || (currentDiceSummary[2] > 0 && currentDiceSummary[2] < 3));
                // There other dice the player will want to change
                int currentPointsFrom1s = currentDiceSummary[0] >= 3 ? 1 + currentDiceSummary[0] - 3 : 0;
                pointsNeededToWin = GameController.instance.winPoints - player.points;
                if (gainChangeDieResultTo1Option != null)
                {
                    if (changingTo1WillBringPoints)
                    {
                        if (pointsNeededToWin == currentPointsFrom1s + 1)
                        {
                            MakeDecision(gainChangeDieResultTo1Option, 0);
                            AddDecisionEvent("is changing a dice result to 1", "it will give them enough points to win.");
                            return;
                        }
                    }
                }

                // Check if player will want to change to 1 for points
                if (gainChangeDieResultTo1Option != null)
                {
                    if (changingTo1WillBringPoints)
                    {
                        if (currentDiceSummary[1] < 3 || currentDiceSummary[2] < 3)
                        {
                            MakeDecision(gainChangeDieResultTo1Option, 0);
                            AddDecisionEvent("is changing a dice result to 1", "they would rather have points than useless rolls.");
                            return;
                        }
                        else if (player.AttributeIsPreferred(Card.AttributeFamilyID.Points, Card.AttributeFamilyID.Currency) && currentDiceSummary[5] > 0)
                        {
                            MakeDecision(gainChangeDieResultTo1Option, 0);
                            AddDecisionEvent("is changing a dice result to 1", "they would rather have points than currency.");
                            return;
                        }
                        else if (player.AttributeIsPreferred(Card.AttributeFamilyID.Points, Card.AttributeFamilyID.Damage) && currentDiceSummary[3] > 0)
                        {
                            MakeDecision(gainChangeDieResultTo1Option, 0);
                            AddDecisionEvent("is changing a dice result to 1", "they would rather have points than deal damage.");
                            return;
                        }
                        else if (player.AttributeIsPreferred(Card.AttributeFamilyID.Points, Card.AttributeFamilyID.Health) && currentDiceSummary[4] > 0)
                        {
                            MakeDecision(gainChangeDieResultTo1Option, 0);
                            AddDecisionEvent("is changing a dice result to 1", "they would rather have points than heal.");
                            return;
                        }
                    }
                }

                MakeDefaultDecision();
                AddDecisionEvent("isn't making any special roll changes", "they can't make any changes they think are useful.");
                break;

            case ID.SpecialDamageReduction:
                Option gainRollToReduceDamageOption = GetOption(Option.ID.GainRollToReduceDamage);
                Option payToBecomeInvulnerableOption = GetOption(Option.ID.PayToBecomeInvulnerable);
                Option gainPayToReduceDamageOption = GetOption(Option.ID.GainPayToReduceDamage);
                if (gainRollToReduceDamageOption != null)
                {
                    MakeDecision(gainRollToReduceDamageOption, 0);
                    AddDecisionEvent("is going to roll to reduce damage taken", "it's free damage reduction.");
                    break;
                }
                else if (payToBecomeInvulnerableOption != null )
                {
                    if (player.incomingDamage >= player.health)
                    {
                        MakeDecision(payToBecomeInvulnerableOption, 0);
                        AddDecisionEvent("is going to pay to become invulnerable", "they will die if they don't.");
                        break;
                    }
                    else if (player.AttributeIsPreferred(Card.AttributeFamilyID.Health, Card.AttributeFamilyID.Currency))
                    {
                        MakeDecision(payToBecomeInvulnerableOption, 0);
                        AddDecisionEvent("is going to pay to become invulnerable", "they care more about health than currency.");
                        break;
                    }
                }
                else if (gainPayToReduceDamageOption != null)
                {
                    if (player.incomingDamage >= player.health)
                    {
                        MakeDecision(gainPayToReduceDamageOption, 0);
                        AddDecisionEvent("is going to pay to reduce damage taken", "they will die if they don't.");
                        break;
                    }
                    else if (player.AttributeIsPreferred(Card.AttributeFamilyID.Health, Card.AttributeFamilyID.Currency))
                    {
                        MakeDecision(gainPayToReduceDamageOption, 0);
                        AddDecisionEvent("is going to pay to reduce damage taken", "they care more about health than currency.");
                        break;
                    }
                }
                MakeDefaultDecision();
                AddDecisionEvent("is not going to reduce damage taken", "they see no reason to.");
                break;

            case ID.RollToReduceDamage:
                MakeDefaultDecision();
                break;

            case ID.PayToReduceDamage:
                // Heal to 1 if Currency preferred over Health, otherwise max heal
                int maxArgument = Mathf.Min(player.incomingDamage, player.currency / 2);
                int minArgument = Mathf.Min(Mathf.Max(0, player.incomingDamage - player.health + 1), player.currency / 2);
                if (player.AttributeIsPreferred(Card.AttributeFamilyID.Health, Card.AttributeFamilyID.Currency))
                {
                    MakeDecision(GetOption(Option.ID.PayToReduceDamage), maxArgument);
                    AddDecisionEvent("is paying " + maxArgument * 2 + " currency to reduce damage taken by " + maxArgument, "it's the maximum they can reduce.");
                    break;
                }
                else
                {
                    MakeDecision(GetOption(Option.ID.PayToReduceDamage), minArgument);
                    AddDecisionEvent("is paying " + minArgument * 2 + " currency to reduce damage taken by " + minArgument, "it's the minimum they need to survive.");
                    break;
                }

            default:
                Debug.Log("Decision: '" + id + "' should explicitly define an AI Decision logic.");
                MakeDefaultDecision();
                break;
        }
    }

    public void AIDiceDecision(Card.AttributeFamilyID attribute)
    {
        int[] currentHeldSummary = GameController.instance.HeldDiceSummary();
        int[] currentDiceSummary = GameController.instance.DiceSummary();
        int[] undecidedDiceSummary = GameController.instance.DiceSummary(undecidedDice);
        if (attribute == Card.AttributeFamilyID.Health)
        {
            if (currentDiceSummary[4] > 0 && undecidedDice.Count > 0)
            {
                int maxHeal = player.maxHealth - (player.health);
                if (maxHeal > 0)
                {
                    // Keep health if health after these rolls is less than health worry level and can heal
                    if (!GameController.instance.PlayerIsInside(player))
                    {
                        int availableHeal = Mathf.Min(currentDiceSummary[4], maxHeal);
                        GameController.instance.SetSomeDiceWithValueHeld(4, availableHeal, true);
                        UpdateUndecidedDice();
                        AddDecisionEvent("is keeping " + availableHeal + " health rolls", "they think it would be useful to heal.");
                        if (player.health + currentHeldSummary[4] <= player.healthWorryLevel && undecidedDice.Count > 0)
                        {
                            // Clear undecided dice so that all dice are rerolled
                            ClearUndecidedDice();
                            AddDecisionEvent("is rolling all other dice for health", "they think they're health is too low.");
                        }
                    }
                    else
                    {
                        AddDecisionEvent("is not keeping any health rolls", "they cannot heal inside.");
                    }
                }
                else
                {
                    AddDecisionEvent("is not keeping any health rolls", "they are at max health.");
                }
            }
            else if (!GameController.instance.PlayerIsInside(player))
            {

                if (player.health <= player.healthWorryLevel && undecidedDice.Count > 0)
                {
                    // Clear free dice so that all dice are rerolled
                    ClearUndecidedDice();
                    AddDecisionEvent("is rolling all other dice for health", "they think they're health is too low.");
                }
            }
        }
        else if (attribute == Card.AttributeFamilyID.Damage)
        {
            if (currentDiceSummary[3] > 0 && undecidedDice.Count > 0)
            {
                if (player.health < player.healthWorryLevel && !GameController.instance.PlayerIsInside(player))
                {
                    AddDecisionEvent("is not keeping any damage rolls", "they don't want to end up inside as their health is too low.");
                    return;
                }

                Player[] otherLivingPlayers = GameController.instance.OtherLivingPlayers(player);
                Player[] otherPlayersWithKillCard = Card.PlayersWithCard(otherLivingPlayers, Card.ID.SkullCollector);
                foreach (Player otherPlayer in otherPlayersWithKillCard)
                {
                    Debug.Log(otherPlayer.name);
                    if (currentDiceSummary[3] >= otherPlayer.health)
                    {
                        GameController.instance.SetAllDiceWithValueHeld(3, true);
                        UpdateUndecidedDice();
                        AddDecisionEvent("is keeping all damage rolls", "it will kill " + otherPlayer.formattedName + ".");
                        return;
                    }
                    Debug.Log(GameController.instance.OtherVulnerablePlayers(player, !GameController.instance.PlayerIsInside(player)).Length);
                    if (otherPlayer.pointsNeededToWin <= Card.pointsForSkullCollecting * GameController.instance.OtherVulnerablePlayers(player, !GameController.instance.PlayerIsInside(player)).Length)
                    {
                        AddDecisionEvent("is not keeping any damage rolls", "they think " + otherPlayer.formattedName + " will win by Skull Collecting if anyone else dies.");
                        return;
                    }
                }

                if (GameController.instance.playersInside.Count > 0)
                {
                    GameController.instance.SetAllDiceWithValueHeld(3, true);
                    UpdateUndecidedDice();
                    AddDecisionEvent("is keeping all damage rolls", "they want to deal damage.");

                    // Decide which remaining dice to force reroll
                    currentHeldSummary = GameController.instance.HeldDiceSummary();
                    int currentDamage = currentHeldSummary[3];
                    // Pick a player to target - if damage could kill them then aim for that damage, otherwise aim to do maximal damage
                    Player targetPlayer = GameController.instance.PlayerWithLeastHealth(!GameController.instance.PlayerIsInside(player));
                    return;
                }
                AddDecisionEvent("is not keeping any damage rolls", "there is no-one inside to damage.");
            }
        }
        else if (attribute == Card.AttributeFamilyID.Currency) 
        {
            if (currentDiceSummary[5] > 0 && undecidedDice.Count > 0)
            {
                // Always keep currency, and reroll other dice if more currency is wanted for specific reason
                GameController.instance.SetAllDiceWithValueHeld(5, true);
                UpdateUndecidedDice();
                AddDecisionEvent("is keeping their currency rolls", "they think currency is useful.");
            }
        }
        else if (attribute == Card.AttributeFamilyID.Points)
        {
            // If Player already has enough points to win, ignore holding any further dice
            if (player.points >= GameController.instance.winPoints)
            {
                AddDecisionEvent("is not keeping any points rolls", "they already have enough points to win.");
                return;
            }

            // First, check if points dice being held from a previous roll are no longer worth the risk
            if (currentHeldSummary[0] > 0 || currentHeldSummary[1] > 0 || currentHeldSummary[2] > 0)
            {
                for (int j = 2; j >= 0; j--)
                {
                    if (currentDiceSummary[j] == 2 && undecidedDice.Count < player.diceRisk)
                    {
                        UpdateUndecidedDice(GameController.instance.SetAllDiceWithValueHeld(j, false));
                        AddDecisionEvent("is no longer keeping their " + (j + 1) + " rolls", "they don't think they'll be able to roll more for points with this amount of free dice.");
                        currentHeldSummary = GameController.instance.HeldDiceSummary();
                    }
                }
            }

            if (undecidedDice.Count > 0)
            {
                if (currentDiceSummary[0] > 0 || currentDiceSummary[1] > 0 || currentDiceSummary[2] > 0)
                {
                    bool keptPointRolls = false;
                    // Keep points rolls if you have 3 or more, or if you have 2 or more and have enough free dice for your acceptable dice risk
                    for (int j = 2; j >= 0; j--)
                    {
                        if (currentDiceSummary[j] >= 3)
                        {
                            GameController.instance.SetAllDiceWithValueHeld(j, true);
                            UpdateUndecidedDice();
                            AddDecisionEvent("is keeping their " + (j + 1) + " rolls", "they want the points.");
                            keptPointRolls = true;
                        }
                    }
                    for (int j = 2; j >= 0; j--)
                    {
                        if (currentDiceSummary[j] == 2 && undecidedDice.Count + currentHeldSummary[j] - 2 >= player.diceRisk)
                        {
                            GameController.instance.SetAllDiceWithValueHeld(j, true);
                            ClearUndecidedDice();
                            AddDecisionEvent("is keeping their " + (j + 1) + " rolls and is rolling all other unheld dice for points", "they think they'll be able to roll more for points.");
                            keptPointRolls = true;
                        }
                    }
                    if (!keptPointRolls)
                    {
                        AddDecisionEvent("is not keeping any points rolls", "they don't think they'll be able to get points.");
                    }
                }
            }
        }
        else
        {
            Debug.Log(attribute + " is not a valid Attribute Family for dice rolls.");
        }
        //Debug.Log(undecidedDice.Count);
    }

    public void UpdateUndecidedDice()
    {
        bool[] currentDiceHeld = GameController.instance.currentDiceHeld;
        for (int i = 0; i < currentDiceHeld.Length; i++)
        {
            if (currentDiceHeld[i])
            {
                if (undecidedDice.Contains(i))
                {
                    undecidedDice.Remove(i);
                }
            }
        }
    }

    public void UpdateUndecidedDice(int[] newUndecidedDice)
    {
        UpdateUndecidedDice();
        for (int i = 0; i < newUndecidedDice.Length; i++)
        {
            if (!undecidedDice.Contains(newUndecidedDice[i]))
            {
                undecidedDice.Add(newUndecidedDice[i]);
            }
        }
        //for (int i = 0; i < undecidedDice.Count; i++)
        //{
        //    Debug.Log(undecidedDice[i]);
        //}
    }

    public void ResetUndecidedDice()
    {
        for (int i = 0; i < GameController.instance.currentDiceHeld.Length; i++)
        {
            if (!GameController.instance.currentDiceHeld[i])
            {
                undecidedDice.Add(i);
            }
        }
    }

    public void ClearUndecidedDice()
    {
        undecidedDice = new List<int>();
    }

    public void AIBuyCard(Card card)
    {
        player.BuyCard(card);
        UIController.instance.SetGameCardsAvailableToPlayerObj(GameController.instance.GameCardsAvailableToPlayer(player), player);
        UIController.instance.SetPlayerCardsObj(player, player);
    }
    
    public void AddDecisionEvent(string decision, string reason)
    {
        Debug.Log(player.formattedName + " " + decision + " because " + reason);
    }

    public static Decision StartTurn(Player player)
    {
        Option[] options = new Option[] { Option.StartTurn };
        return new Decision(ID.StartTurn, player, "Start Turn", options, options[0], 0, defaultDecisionTime, true, true, true);
    }

    public static Decision FirstRoll(Player player)
    {
        Option[] options = new Option[] { Option.Roll };
        return new Decision(ID.FirstRoll, player, "Roll", options, options[0], 0, defaultDecisionTime, true, true, true);
    }

    public static Decision IntermediateRoll(Player player)
    {
        Option[] options = new Option[] { Option.Roll, Option.KeepRolls };
        return new Decision(ID.IntermediateRoll, player, "Roll or Keep?", options, options[0], 0, defaultDecisionTime, true, false, true);
    }
    
    public static Decision NoMoreRolls(Player player)
    {
        Option[] options = new Option[] { Option.Resolve };
        return new Decision(ID.NoMoreRolls, player, "Resolve Rolls", options, options[0], 0, defaultDecisionTime, true, true, true);
    }

    public static Decision LeaveOrStay(Player player)
    {
        Option[] options = new Option[] { Option.Stay(player), Option.Leave(player)  };
        return new Decision(ID.LeaveOrStay, player, "Leave or Stay?", options, options[0], 0, defaultDecisionTime, true, false, true);
    }

    public static Decision BrowseCards(Player player)
    {
        List<Option> options = new List<Option>();
        options.Add(Option.FinishedBrowsingCards);
        if (GameController.instance.cardDrawPile.Count > 0 && player.currency >= GameController.instance.refreshCardsCost)
        {
            options.Add(Option.RefreshCards);
        }
        Option[] newOptions = options.ToArray();
        return new Decision(ID.BrowseCards, player, "Browse Cards", newOptions, newOptions[0], 0, defaultDecisionTime, true, true, true);
    }

    public static Decision BuyCardInstantly(Player player, Card card)
    {
        Option[] options = new Option[] { Option.DontBuyCardInstantly, Option.BuyCardInstantly(player, card) };
        return new Decision(ID.BuyCardInstantly, player, "Buy Card: " + card.name, options, options[0], 0, defaultDecisionTime, true, false, false);
    }

    public static Decision EndTurn(Player player)
    {
        Option[] options = new Option[] { Option.EndTurn };
        return new Decision(ID.EndTurn, player, "End Turn", options, options[0], 0, defaultDecisionTime, true, true, true);
    }

    public static Decision NewGame()
    {
        Option[] options = new Option[] { Option.NewGame };
        return new Decision(ID.NewGame, null, "New Game", options, options[0], 0, defaultDecisionTime, true, false, false);
    }

    public static Decision SpecialDamageReduction(Player player, List<Card.ID> cardIDs, Player playerCausingDamage, int damage)
    {
        List<Option> options = new List<Option>();
        options.Insert(0, Option.IgnoreSpecialDamageReduction(player, playerCausingDamage, damage));
        for (int i = 0; i < cardIDs.Count; i++)
        {
            if (cardIDs[i] == Card.ID.HeartyRoll)
            {
                options.Add(Option.GainRollToReduceDamage(player, playerCausingDamage, damage));
            }
            else if (cardIDs[i] == Card.ID.TemporaryInvulnerability)
            {
                options.Add(Option.PayToBecomeInvulnerable(player, playerCausingDamage, damage));
            }
            else if (cardIDs[i] == Card.ID.HealthInsurance)
            {
                options.Add(Option.GainPayToReduceDamage(player, playerCausingDamage, damage));
            }
            else
            {
                Debug.Log("'" + cardIDs[i] + "' is not a valid Card ID.");
            }
        }
        Option[] newOptions = options.ToArray();
        return new Decision(ID.SpecialDamageReduction, player, "Use Special Damage Reduction?", newOptions, newOptions[0], 0, defaultDecisionTime, true, false, false);
    }

    public static Decision PayToReduceDamage(Player player, Player playerDealingDamage, int damage)
    {
        Option[] options = new Option[] { Option.PayToReduceDamage(player, playerDealingDamage, damage) };
        Decision newDecision = new Decision(ID.PayToReduceDamage, player, "Pay to Reduce Damage", options, options[0], 0, defaultDecisionTime, false, false, true);
        return newDecision;
    }

    public static Decision RollToReduceDamage(Player player, Player playerCausingDamage, int damage)
    {
        Option[] options = new Option[] { Option.RollToReduceDamage(player, playerCausingDamage, damage) };
        return new Decision(ID.RollToReduceDamage, player, "Roll to Heal", options, options[0], 0, defaultDecisionTime, true, false, false);
    }

    public static Decision SpecialRollChanges(Player player, List<Card.ID> cardIDs)
    {
        List<Option> options = new List<Option>();
        options.Add(Option.IgnoreSpecialRollChanges);
        for (int i = 0; i < cardIDs.Count; i++)
        {
            if (cardIDs[i] == Card.ID.RockAndRoller)
            {
                options.Add(Option.GainExtraRoll);
            }
            else if (cardIDs[i] == Card.ID.CostlyRoll)
            {
                options.Add(Option.PayToGainExtraRoll);
            }
            else if (cardIDs[i] == Card.ID.DieForger)
            {
                options.Add(Option.GainChangeDieResult);
            }
            else if (cardIDs[i] == Card.ID.OneToOne)
            {
                options.Add(Option.GainChangeDieResultTo1);
            }
            else if (cardIDs[i] == Card.ID.DieCaster)
            {
                options.Add(Option.PayToGainChangeDieResult);
            }
            else if (cardIDs[i] == Card.ID.TertiaryAllergy)
            {
                options.Add(Option.GainRerollThrees);
            }
            else
            {
                Debug.Log("'" + cardIDs[i] + "' is not a valid Card ID.");
            }
        }
        Option[] newOptions = options.ToArray();
        return new Decision(ID.SpecialRollChange, player, "Use Special Roll Changes?", newOptions, newOptions[0], 0, defaultDecisionTime, true, false, true);
    }

    public static Decision ChangeDieResult(Player player)
    {
        Option[] options = new Option[] { Option.PerformDieResultChange };
        return new Decision(ID.ChangeDieResult, player, "Select Die to Change and Desired Value", options, options[0], 0, defaultDecisionTime, false, false, true);
    }

    public static Decision ChangeDieResultToValue(Player player, int dieValue)
    {
        Option[] options = new Option[] { Option.PerformDieResultChangeToValue(dieValue) };
        return new Decision(ID.ChangeDieResultToValue, player, "Select Die to Change to " + dieValue, options, options[0], 0, defaultDecisionTime, true, false, true);
    }

    public static Decision SpecialDieUsage(Player player, List<Card.ID> cardIDs)
    {
        List<Option> options = new List<Option>();
        options.Add(Option.IgnoreSpecialDieUsage);
        for (int i = 0; i < cardIDs.Count; i++)
        {
            if (cardIDs[i] == Card.ID.AggressiveHealthcare)
            {
                options.Add(Option.GainHealEnemy);
            }
            else if (cardIDs[i] == Card.ID.DieDie)
            {
                options.Add(Option.GainReviveDeadDice);
            }
            else if (cardIDs[i] == Card.ID.VenomousBite)
            {
                options.Add(Option.GainReduceVenomLevel);
            }
            else
            {
                Debug.Log("'" + cardIDs[i] + "' is not a valid Card ID.");
            }
        }
        Option[] newOptions = options.ToArray();
        return new Decision(ID.SpecialDieUsage, player, "Use Special Die Usage?", newOptions, newOptions[0], 0, defaultDecisionTime, true, false, true);
    }

    public static Decision ChooseEnemyToHeal(Player player)
    {
        Option[] options = new Option[] { Option.ChooseEnemyToHeal };
        return new Decision(ID.ChooseEnemyToHeal, player, "Choose Enemy to Heal", options, options[0], GameController.instance.HealableEnemies(player)[0].ID, defaultDecisionTime, false, false, true);
    }

    public static Decision HealEnemy(Player player)
    {
        Option[] options = new Option[] { Option.PerformHealEnemy };
        return new Decision(ID.HealEnemy, player, "Choose Amount to Heal", options, options[0], 1, defaultDecisionTime, false, false, true);
    }

    public static Decision ReduceVenomLevel(Player player)
    {
        Option[] options = new Option[] { Option.ReduceVenomLevel };
        return new Decision(ID.ReduceVenomLevel, player, "Choose Venom Level Reduction", options, options[0], 1, defaultDecisionTime, false, false, true);
    }

    public static Decision ReduceDeadDice(Player player)
    {
        Option[] options = new Option[] { Option.ReviveDeadDice };
        return new Decision(ID.ReduceDeadDice, player, "Choose Dead Dice to Revive", options, options[0], 1, defaultDecisionTime, false, false, true);
    }

    public static Decision SpecialEnemyRollChanges(Player player)
    {
        Option[] options = new Option[] { Option.IgnoreSpecialEnemyRollChanges(player), Option.GainRerollEnemyDie(player) };
        return new Decision(ID.GainRerollEnemyDie, player, "Reroll Enemy Die?", options, options[0], 0, defaultDecisionTime, true, false, true);
    }

    public static Decision RerollEnemyDie(Player player)
    {
        Option[] options = new Option[] { Option.PerformRerollEnemyDie(player) };
        return new Decision(ID.RerollEnemyDie, player, "Choose Die to Reroll", options, options[0], 0, defaultDecisionTime, true, false, true);
    }

    public static Decision SetCardBeingDuplicatedFirstTime(Player player)
    {
        Option[] options = new Option[] { Option.FinishedDuplicatingCard };
        string desc = "Choose Enemy Card to Duplicate";
        return new Decision(ID.SetCardBeingDuplicatedFirstTime, player, desc, options, options[0], 0, defaultDecisionTime, true, false, false);
    }

    public static Decision ChangeCardBeingDuplicated(Player player, Card cardBecomingDuplicate)
    {
        Option[] options = new Option[] { Option.FinishedDuplicatingCard };
        string desc = " Enemy Card to Duplicate";
        if (cardBecomingDuplicate.cardBeingDuplicated != null)
        {
            desc = "Change" + desc;
            desc += " (currently '" + cardBecomingDuplicate.cardBeingDuplicated.name + "')";
        }
        else
        {
            desc = "Choose" + desc + " for 1 Currency";
        }
        return new Decision(ID.ChangeCardBeingDuplicated, player, desc, options, options[0], 0, defaultDecisionTime, true, false, false);
    }

    public static Decision RefundCards(Player player)
    {
        Option[] options = new Option[] { Option.FinishedRefundingCards };
        return new Decision(ID.RefundCards, player, "Choose a Card to Refund", options, options[0], 0, defaultDecisionTime, true, false, true);
    }

    public Decision(ID id, Player player, string description, Option[] options, Option defaultOption, int defaultArgument, float timeToRespond, bool isDiscrete, bool isNecessary, bool isSequential)
    {
        this.id = id;
        this.player = player;
        this.description = description;
        this.options = options;
        this.defaultOption = defaultOption;
        this.defaultArgument = defaultArgument;
        this.timeToRespond = timeToRespond;
        this.isDiscrete = isDiscrete;
        this.isNecessary = isNecessary;
        this.isSequential = isSequential;
        argument = defaultArgument;
        decisionMade = false;
    }
}