using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Player
{
    public static int minDiceRisk = 1;
    public static int maxDiceRisk = 5;
    public static int minPointsRisk = 0;
    public static int maxPointsRisk = 4;

    public static int minHealthWorryLevel = 2;
    public static int maxHealthWorryLevel = 7;
    public static int minHealthWorryHappyDiff = 1;
    public static int maxHealthWorryHappyDiff = 5;
    public static int minTooMuchCurrencyLevel = 5;
    public static int maxTooMuchCurrencyLevel = 8;
    
    public int ID;
    public string name;
    public string formattedName
    {
        get
        {
            return "<b><color=#" + ColorUtility.ToHtmlStringRGB(TextColour(ID)) + ">" + name + "</color></b>";
        }
    }

    public static List<float> allHues = new List<float> { 0, 30, 120, 190, 230, 280 };
    public static Color BackgroundColour(int i)
    {
        i = i % allHues.Count;
        return Color.HSVToRGB(allHues[i] / 360, 0.9f, 0.9f);
    }
    public static Color KittenColour(int i)
    {
        i = i % allHues.Count;
        return Color.HSVToRGB(allHues[i] / 360, 0.8f, 0.8f);
    }
    public static Color TextColour(int i)
    {
        i = i % allHues.Count;
        return Color.HSVToRGB(allHues[i] / 360, 0.8f, 0.8f);
    }

    public enum PlayerType { Human, AI, Network };
    public PlayerType playerType;

    private KittenStyle _kittenStyle;
    public KittenStyle kittenStyle
    {
        get
        {
            return _kittenStyle;
        }
        set
        {
            _kittenStyle = value;
        }
    }

    // AI traits
    // The order of preference of each Attribute which helps decides decisions - particularly with relation to dice rolls and buying cards
    // This variable is only concerned with Points, Health, Damage and Currency - i.e. what can be rolled
    public Card.AttributeFamilyID[] attributePreferences;
    // A damage-loving AI will focus on dealing damage a lot more and buy cards that fit that attribute
    // A health-loving AI will focus on keeping high health a lot more and buy cards that fit that attribute
    // A points-loving AI will focus on gaining points a lot more and buy cards that fit that attribute
    // A currency-loving AI will focus on gaining currency a lot more and buy cards that fit that attribute
    public int AttributePreference(Card.AttributeFamilyID attr)
    {
        return System.Array.IndexOf(attributePreferences, attr);
    }
    public bool AttributeIsPreferred(Card.AttributeFamilyID attr1, Card.AttributeFamilyID attr2)
    {
        int index1 = AttributePreference(attr1);
        int index2 = AttributePreference(attr2);
        if (index1 == -1)
        {
            Debug.Log(attr1 + " is not in " + formattedName + "'s Attribute Preferences.");
            return false;
        }
        if (index2 == -1)
        {
            Debug.Log(attr2 + " is not in " + formattedName + "'s Attribute Preferences.");
            return false;
        }
        if (index1 == index2)
        {
            Debug.Log(attr1 + " and " + attr2 + " are the same Attribute Family.");
            return false;
        }
        return index1 > index2;
    }

    public bool lovesCards; // A card AI will buy cards whenever they can afford one and don't want to save up for one
    public bool lovesDiceControl; // An AI that loves dice control will buy cards which give them greater control over their dice results
    public bool lovesBeingMean; // An AI that loves being mean will buy cards that affect the enemy negatively (but aren't damage-related)
    public bool hatesInside; // An inside-hating AI will leave inside at every opportunity

    public float riskiness; // An AI with risk 1 will take lots of risks, an AI will risk 0 will take few risks, this value is used to decide dice and points risk
    public int diceRisk // Number of dice an AI will need to be free to risk getting a particular result, the higher the less risky
    {
        get
        {
            return minDiceRisk + (int)((1 - riskiness) * (maxDiceRisk - minDiceRisk + 1));
        }
    }
    public int pointsRisk // Number of points an AI will need to be off winning (or less) to risk gaining points over health
    {
        get
        {
            return minPointsRisk + (int)(riskiness * (maxPointsRisk - minPointsRisk + 1));
        }
    }
    public int enemyPointsRisk // Number of points an enemy AI will need to be off winning (or less) for the AI to be worried about them winning, essentially the inverse of pointsRisk
    {
        get
        {
            return minPointsRisk + maxPointsRisk - pointsRisk;
        }
    }
    public int healthWorryLevel; // An AI will always leave inside when they go less than or equal to their health worry level, and will attempt to heal to at least greater than health worry level
    public int healthHappyLevel; // An AI at or above their health happy level will reroll health rolls in favour of somethig else (unless they have a health-preference)
    public int tooMuchCurrencyLevel; // If an AI has currency greater than or equal to this and there are no appealing cards, they will refresh the cards

    // Temporary variables
    public int incomingDamage = 0;

    // Health of the Player, lost when the Player takes damage, the Player is out when health hits 0
    private int _health;
    public int health
    {
        get
        {
            return _health;
        }
        private set
        {
            if (isAlive)
            {
                if (value > maxHealth)
                {
                    _health = maxHealth;
                }
                else if (value <= 0)
                {
                    foreach (Player player in GameController.instance.players)
                    {
                        if (player.HasCard(Card.ID.SkullCollector))
                        {
                            int newPoints = Card.pointsForSkullCollecting * player.CountCard(Card.ID.SkullCollector);
                            player.points += newPoints;
                        }
                    }
                    if (HasCard(Card.ID.Rebirth))
                    {
                        points = 0;
                        if (GameController.instance.PlayerIsInside(this))
                        {
                            GameController.instance.LeaveInside(this, true);
                        }
                        _health = 10;
                        UIController.instance.AddGameEvent(formattedName + " was reborn.");
                    }
                    else
                    {
                        _health = value;
                        UIController.instance.playersObj.transform.Find("Player" + (ID + 1)).Find("LeftSide").Find("Title").Find("Name").Find("IsDead").gameObject.SetActive(true);
                        GameEvent.Died(ID);
                        GameController.instance.KillPlayer(this);
                    }
                    RemoveAllCards();
                }
                else
                {
                    _health = value;
                }
            }
            UIController.instance.playersObj.transform.Find("Player" + (ID + 1)).Find("LeftSide").Find("Health").Find("Bar").Find("Fill").GetComponent<Image>().fillAmount = (float)health / maxHealth;
        }
    }
    public void SetHealthUnmodified(int newHealth)
    {
        health = newHealth;
        UIController.instance.playersObj.transform.Find("Player" + (ID + 1)).Find("LeftSide").Find("Health").Find("Bar").Find("Fill").GetComponent<Image>().fillAmount = (float)health / maxHealth;
    }
    public bool isAlive
    {
        get
        {
            return health > 0;
        }
    }

    public bool ChangeHealth(Player playerCausingChange, int healthDiff)
    {
        if (!isAlive)
        {
            Debug.Log(formattedName + " is dead and thus cannot change health.");
            return false;
        }

        if (healthDiff + health > maxHealth)
        {
            healthDiff = maxHealth - health;
        }

        if (healthDiff < 0)
        {
            if (isInvulnerable)
            {
                UIController.instance.AddGameEvent(formattedName + " avoided " + (-healthDiff) + " damage as they have Temporary Invulnerability.");
            }
            else if (healthDiff == -1 && HasCard(Card.ID.StrongArmour))
            {

                UIController.instance.AddGameEvent(formattedName + " prevented losing 1 health using their Strong Armour.");
            }
            else
            {

                List<Card.ID> availableSpecialDamageReductions = AvailableSpecialDamageReductions(-healthDiff);
                if (availableSpecialDamageReductions.Count > 0)
                {
                    GameController.instance.AddDecisionAsPriority(Decision.SpecialDamageReduction(this, availableSpecialDamageReductions, playerCausingChange, -healthDiff));
                    return false;
                }
                else
                {
                    if (healthDiff <= -2 && HasCard(Card.ID.BloodDonor))
                    {
                        int newCurrency = CountCard(Card.ID.BloodDonor);
                        currency += newCurrency;
                    }

                    if (playerCausingChange != null)
                    {
                        GameEvent.HealthChangeByPlayer(playerCausingChange.ID, ID, healthDiff);

                        if (playerCausingChange.HasCard(Card.ID.DieDie))
                        {
                            int newDeadDice = playerCausingChange.CountCard(Card.ID.DieDie);
                            deadDice += newDeadDice;
                            UIController.instance.AddGameEvent(formattedName + " lost " + newDeadDice + " dice due to " + playerCausingChange.formattedName + " saying 'Die, Die'.");
                        }
                        if (playerCausingChange.HasCard(Card.ID.VenomousBite))
                        {
                            int newVenomLevel = playerCausingChange.CountCard(Card.ID.VenomousBite);
                            venomLevel += newVenomLevel;
                            UIController.instance.AddGameEvent(formattedName + " gained " + newVenomLevel + " Venom Level due to " + playerCausingChange.formattedName + "'s Venomous Bite.");
                        }
                    }
                    else
                    {
                        GameEvent.HealthChange(ID, healthDiff);
                        // Change Info dealt with in attack otherwise
                        SimulationController.instance.AddSimulationToQueue(SimulationController.instance.ShowKittenStatChanges(ID, new SimulationController.ChangeInfoText[] { SimulationController.ChangeInfoText.Health }, new int[] { healthDiff }));
                    }
                }
            }
        }
        else if (healthDiff > 0)
        {
            if (HasCard(Card.ID.Vitality) && healthDiff + health < maxHealth)
            {
                int newHealth = CountCard(Card.ID.Vitality);
                healthDiff += newHealth;
            }

            if (playerCausingChange != null)
            {
                GameEvent.HealthChangeByPlayer(playerCausingChange.ID, ID, healthDiff);
            }
            else
            {
                GameEvent.HealthChange(ID, healthDiff);
            }
            SimulationController.instance.AddSimulationToQueue(SimulationController.instance.ShowKittenStatChanges(ID, new SimulationController.ChangeInfoText[] { SimulationController.ChangeInfoText.Health }, new int[] { healthDiff }));
        }

        // A successful change in health means the player can have all their damage reduction options available next time they are dealt damage
        hasPaidToReduceDamage = false;
        hasRolledToReduceDamage = false;
        hasHadOptionToBecomeInvulnerable = false;
        health += healthDiff;
        return true;
    }

    // Health-related variables
    public bool hasRolledToReduceDamage;
    public bool hasPaidToReduceDamage;
    public bool hasHadOptionToBecomeInvulnerable;
    public bool isInvulnerable;

    // Maximum health of the Player, cannot go higher than this without buffs
    private int _maxHealth;
    public int maxHealth
    {
        get
        {
            return _maxHealth;
        }
        set
        {
            _maxHealth = value;
            if (health > maxHealth)
            {
                _health = maxHealth;
            }
            Transform notches = UIController.instance.playersObj.transform.Find("Player" + (ID + 1)).Find("LeftSide").Find("Health").Find("Bar").Find("Notches");
            foreach (Transform child in notches)
            {
                Object.Destroy(child.gameObject);
            }
            if (maxHealth <= UIController.instance.maxNotches)
            {
                for (int i = 0; i < maxHealth; i++)
                {
                    GameObject newNotch = Object.Instantiate(UIController.instance.notchPrefab, notches);
                    newNotch.GetComponent<RectTransform>().anchorMin = new Vector2((float)i / maxHealth, 0);
                    newNotch.GetComponent<RectTransform>().anchorMax = new Vector2((float)(i + 1) / maxHealth, 1);
                }
            }
            UIController.instance.playersObj.transform.Find("Player" + (ID + 1)).Find("LeftSide").Find("Health").Find("Bar").Find("Fill").GetComponent<Image>().fillAmount = (float)health / maxHealth;
        }
    }

    // Points of the Player, once the Player reaches maximum points, the Player wins
    private int _points;
    public int points
    {
        get
        {
            return _points;
        }
        set
        {
            if (isAlive)
            {
                if (_points != value)
                {
                    GameEvent.PointsChange(ID, Mathf.Max(0, value) - _points);
                    if (value <= 0)
                    {
                        _points = 0;
                    }
                    else if (value >= GameController.instance.winPoints)
                    {
                        _points = value;
                    }
                    else
                    {
                        _points = value;
                    }
                }
            }
            UIController.instance.playersObj.transform.Find("Player" + (ID + 1)).Find("LeftSide").Find("Points").Find("Bar").Find("Fill").GetComponent<Image>().fillAmount = (float)points / GameController.instance.winPoints;
        }
    }
    public int pointsNeededToWin
    {
        get
        {
            return GameController.instance.winPoints - points;
        }
    }

    // Currency of the player, used to buy Cards
    private int _currency;
    public int currency
    {
        get
        {
            return _currency;
        }
        set
        {
            if (_currency != value)
            {
                if (value > currency && HasCard(Card.ID.Counterfeiting))
                {
                    int extraCurrency = CountCard(Card.ID.Counterfeiting);
                    value += extraCurrency;
                }
                else if (value < 0)
                {
                    Debug.Log(formattedName + " cannot go into negative currency. (" + value + ")");
                    _currency = 0;
                    return;
                }

                GameEvent.CurrencyChange(ID, Mathf.Max(0, value) - _currency);

                _currency = value;
            }
            UIController.instance.playersObj.transform.Find("Player" + (ID + 1)).Find("RightSide").Find("Currency").Find("Count").GetComponent<TextMeshProUGUI>().text = "<sprite index=0 color=#FFAA00>" + currency;
        }
    }

    // How many Cards the Player can choose to buy from
    private int _cardVisibility;
    public int cardVisibility
    {
        get
        {
            return _cardVisibility;
        }
        set
        {
            _cardVisibility = value;
        }
    }

    private List<int> _hedgeFund;
    public List<int> hedgeFund
    {
        get
        {
            return _hedgeFund;
        }
    }

    private int _extraRolls;
    public int extraRolls
    {
        get
        {
            return _extraRolls;
        }
        set
        {
            _extraRolls = value;
        }
    }

    private int _venomLevel;
    public int venomLevel
    {
        get
        {
            return _venomLevel;
        }
        set
        {
            _venomLevel = value;
        }
    }

    private int _deadDice;
    public int deadDice
    {
        get
        {
            return _deadDice;
        }
        set
        {
            _deadDice = value;
        }
    }

    private int _siegeDays;
    public int siegeDays
    {
        get
        {
            return _siegeDays;
        }
        set
        {
            _siegeDays = value;
            UIController.instance.playersObj.transform.Find("Player" + (ID + 1)).Find("RightSide").Find("SiegeInfo").Find("SiegeInfo").GetComponent<TextMeshProUGUI>().text = siegeDays + "-" + siegeHours;
        }
    }


    private int _siegeHours;
    public int siegeHours
    {
        get
        {
            return _siegeHours;
        }
        set
        {
            _siegeHours = value;
            UIController.instance.playersObj.transform.Find("Player" + (ID + 1)).Find("RightSide").Find("SiegeInfo").Find("SiegeInfo").GetComponent<TextMeshProUGUI>().text = siegeDays + "-" + siegeHours;
        }
    }

    public float hue
    {
        get
        {
            return allHues[ID] / 360;
        }
    }

    // Colour used to identify the Player
    public Color colour
    {
        get
        {
            return Color.HSVToRGB(hue, 1, 1);
        }
    }

    // Cards in the Players hand
    public List<Card> cards;
    public void RemoveCard(Card card)
    {
        if (HasCard(card.id))
        {
            card.OnRemove(this);
            cards.Remove(card);
            card.owner = null;
            UIController.instance.SetPlayerCardsObj(this, GameController.instance.playerMakingCurrentDecision);
            if (!card.useInstantly && isAlive)
            {
                GameEvent.CardRemoved(ID, card);
                UIController.instance.playersObj.transform.Find("Player" + (ID + 1)).Find("RightSide").Find("Cards").Find("Count").GetComponent<TextMeshProUGUI>().text = "<sprite index=0 color=#BBBBBB>" + cards.Count;
            }
        }
        else
        {
            Debug.Log("Card '" + card.name + "' not in " + formattedName + "'s cards.");
        }
    }
    public void RemoveAllCards()
    {
        //if (isAlive)
        //{
        //    UIController.instance.AddGameEvent(colouredName + " removed all their cards.");
        //}
        int count = cards.Count;
        for(int i = 0; i < count; i++)
        {
            RemoveCard(cards[0]);
        }
    }
    public void AddCard(Card card)
    {
        if (isAlive)
        {
            cards.Add(card);
            card.owner = this;
            card.OnAdd(this);
            UIController.instance.SetPlayerCardsObj(this, GameController.instance.playerMakingCurrentDecision);
            if (!card.useInstantly)
            {
                UIController.instance.playersObj.transform.Find("Player" + (ID + 1)).Find("RightSide").Find("Cards").Find("Count").GetComponent<TextMeshProUGUI>().text = "<sprite index=0 color=#BBBBBB>" + cards.Count;
            }
        }
        else
        {
            Debug.Log(formattedName + " is dead so cannot add cards.");
        }
    }
    public void BuyCard(Card card)
    {
        string placeOfPurchaseString;
        List<Card> placeOfPurchase;
        Player playerPurchasedFrom = card.owner;
        if (playerPurchasedFrom == null)
        {
            placeOfPurchase = GameController.instance.cardDrawPile;
            placeOfPurchaseString = "the Card Draw Pile";
        }
        else
        {
            placeOfPurchase = playerPurchasedFrom.cards;
            placeOfPurchaseString = playerPurchasedFrom + "' Cards";
        }

        if (placeOfPurchase.Contains(card))
        {
            int cost = card.CostToPlayer(this);
            if (currency >= cost)
            {
                if (HasCard(Card.ID.Mercentile))
                {
                    int newPoints = CountCard(Card.ID.Mercentile);
                    points += newPoints;
                }
                currency -= cost;
                if (playerPurchasedFrom == null)
                {
                    placeOfPurchase.Remove(card);
                }
                else
                {
                    playerPurchasedFrom.RemoveCard(card);
                    playerPurchasedFrom.currency += cost;
                }
                GameEvent.CardBought(ID, card);
                AddCard(card);

                if (placeOfPurchase == null)
                {
                    for (int i = 0; i < GameController.instance.numPlayers; i++)
                    {
                        GameController.instance.playerHasBoughtCardInstantly[i] = false;
                    }
                    GameController.instance.CheckIfPlayersCanBuyCardInstantly();
                }
            }
            else
            {
                Debug.Log(formattedName + " does not have enough currency to buy '" + card.name + "'.");
            }
        }
        else
        {
            Debug.Log(placeOfPurchaseString + " does not contain Card '" + card.name + "'.");
        }
    }

    public void RefundCard(Card card)
    {
        if (HasCard(card.id))
        {
            if (HasCard(Card.ID.FreeReturns))
            {
                currency += card.baseCost;
                RemoveCard(card);
            }
            else
            {
                Debug.Log(formattedName + " doesn't have the refund card.");
            }
        }
        else
        {
            Debug.Log("Card '" + card.name + "' not in " + formattedName + "'s cards.");
        }
    }

    public List<Card> activeDuplicates
    {
        get
        {
            return cards.Where(o => o.cardBeingDuplicated != null).ToList();
        }
    }
    public List<Card> cardsAndDuplicatedCards
    {
        get
        {
            List<Card> returnCards = new List<Card>();
            returnCards.AddRange(cards);
            returnCards.AddRange(activeDuplicates.Select(o => o.cardBeingDuplicated));
            return returnCards;
        }
    }

    public Card GetCard(Card.ID cardID)
    {
        // If Player has card, return that. If Player has duplicate of card, return that. Otherwise return null.
        Card regularCard = cards.FirstOrDefault(o => o.id == cardID);
        if (regularCard != null)
        {
            return regularCard;
        }

        Card duplicatedCard = activeDuplicates.FirstOrDefault(o => o.cardBeingDuplicated.id == cardID);
        return duplicatedCard;
    }
    public bool HasCard(Card.ID cardID)
    {
        return cardsAndDuplicatedCards.Find(o => o.id == cardID) != null;
    }
    public int CountCard(Card.ID cardID)
    {
        List<Card> activeDuplicates = cards.Where(o => o.cardBeingDuplicated != null).ToList();
        return cards.Where(o => o.id == cardID).Count() + activeDuplicates.Where(o => o.cardBeingDuplicated.id == cardID).Count();
    }
    public bool HasCardWithAttribute(Card.Attribute attribute)
    {
        return cardsAndDuplicatedCards.Find(o => o.HasAttribute(attribute)) != null;
    }
    public bool HasCardWithAttributeInFamily(Card.AttributeFamilyID attributeFamilyID)
    {
        return cardsAndDuplicatedCards.Find(o => o.HasAttributeInFamily(attributeFamilyID)) != null;
    }

    public List<Card.ID> AvailableSpecialRollChanges()
    {
        List<Card.ID> results = new List<Card.ID>();
        int[] currentDiceSummary = GameController.instance.DiceSummary();
        Card.ID[] possibleCards = new Card.ID[] { Card.ID.RockAndRoller, Card.ID.CostlyRoll, Card.ID.DieForger, Card.ID.OneToOne, Card.ID.DieCaster, Card.ID.TertiaryAllergy };
        bool[] extraConditions = new bool[] { true, currency >= 1, true, !GameController.instance.playerHasChangedDieToOne, currency >= 2, currentDiceSummary[2] > 0 };
        for (int i = 0; i < possibleCards.Length; i++)
        {
            if (HasCard(possibleCards[i]) && extraConditions[i])
            {
                results.Add(possibleCards[i]);
            }
        }
        return results;
    }

    public List<Card.ID> AvailableSpecialDieUsages()
    {
        int[] currentDiceSummary = GameController.instance.DiceSummary();
        List<Card.ID> results = new List<Card.ID>();
        Card.ID[] possibleCards = new Card.ID[] { Card.ID.AggressiveHealthcare };
        bool[] extraConditions = new bool[] { !GameController.instance.playerTurnHasUsedAggressiveHealthcare && GameController.instance.HealableEnemies(this).Length > 0 && currentDiceSummary[4] > 0 };
        for (int i = 0; i < possibleCards.Length; i++)
        {
            if (HasCard(possibleCards[i]) && extraConditions[i])
            {
                results.Add(possibleCards[i]);
            }
        }

        // Check again, but this time Player doesn't need to own card, these are just the cards that have caused the condition 
        possibleCards = new Card.ID[] { Card.ID.DieDie, Card.ID.VenomousBite };
        extraConditions = new bool[] { !GameController.instance.playerTurnHasNegatedDieDie && deadDice > 0 && currentDiceSummary[4] > 0, !GameController.instance.playerTurnHasNegatedVenomousBite && venomLevel > 0 && currentDiceSummary[4] > 0 };
        for (int i = 0; i < possibleCards.Length; i++)
        {
            if (extraConditions[i])
            {
                results.Add(possibleCards[i]);
            }
        }

        return results;
    }

    public List<Card.ID> AvailableSpecialDamageReductions(int damage)
    {
        incomingDamage = damage;
        List<Card.ID> results = new List<Card.ID>();
        Card.ID[] possibleCards = new Card.ID[] { Card.ID.HealthInsurance, Card.ID.TemporaryInvulnerability, Card.ID.HeartyRoll };
        bool[] extraConditions = new bool[] { !hasPaidToReduceDamage && -2 * (health - damage - 1) <= currency && currency >= 2, !hasHadOptionToBecomeInvulnerable && currency >= 2, !hasRolledToReduceDamage };
        for (int i = 0; i < possibleCards.Length; i++)
        {
            if (HasCard(possibleCards[i]) && extraConditions[i])
            {
                results.Add(possibleCards[i]);
            }
        }
        return results;
    }

    public Player(int ID, string name, PlayerType playerType)
    {
        cards = new List<Card>();
        this.ID = ID;
        this.name = name;
        this.playerType = playerType;
        maxHealth = GameOptions.instance.maxHealth;
        _health = maxHealth;
        health = health; // Have to make two calls, one to bypass the isAlive check as health defaults to 0, and one to update UI
        points = 0;
        currency = 0;
        _cardVisibility = GameController.instance.standardCardVisibility;
        _hedgeFund = new List<int>();
        extraRolls = 0;
        venomLevel = 0;
        deadDice = 0;
        siegeDays = 0;
        siegeHours = 0;
        hasRolledToReduceDamage = false;
        hasPaidToReduceDamage = false;
        hasHadOptionToBecomeInvulnerable = false;
        isInvulnerable = false;

        // AI Traits - assign to Human's anyway but unused
        attributePreferences = new Card.AttributeFamilyID[] { Card.AttributeFamilyID.Points, Card.AttributeFamilyID.Damage, Card.AttributeFamilyID.Health, Card.AttributeFamilyID.Currency };
        int seed = Random.Range(0, 1000);
        System.Random r = new System.Random(seed);
        attributePreferences = attributePreferences.OrderBy(x => r.Next()).ToArray();

        lovesCards = Random.value <= 0.5f;
        lovesDiceControl = Random.value <= 0.5f;
        lovesBeingMean = Random.value <= 0.5f;
        hatesInside = Random.value <= 0.2f;

        riskiness = Random.value;
        healthWorryLevel = Random.Range(minHealthWorryLevel, maxHealthWorryLevel);
        healthHappyLevel = Mathf.Min(10, healthWorryLevel + Random.Range(minHealthWorryHappyDiff, maxHealthWorryHappyDiff));
        tooMuchCurrencyLevel = Random.Range(minTooMuchCurrencyLevel, maxTooMuchCurrencyLevel);
    }
}
