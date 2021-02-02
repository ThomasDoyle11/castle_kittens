using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

public class Option
{
    public string name { get; }

    public Action<int> method { get; }

    public enum ID
    {
        StartTurn,
        Roll,
        KeepRolls,
        Resolve,
        Leave,
        Stay,
        FinishedBrowsingCards,
        BuyActiveCard,
        RefreshCards,
        BuyCardInstantly,
        DontBuyCardInstantly,
        EndTurn,
        NewGame,
        PayToBecomeInvulnerable,
        GainPayToReduceDamage,
        GainRollToReduceDamage,
        PayToReduceDamage,
        RollToReduceDamage,
        GainRerollEnemyDie,
        PerformRerollEnemyDie,
        GainHealEnemy,
        ChooseEnemyToHeal,
        PerformHealEnemy,
        GainReduceVenomLevel,
        GainReviveDeadDice,
        ReduceVenomLevel,
        ReviveDeadDice,
        GainExtraRoll,
        PayToGainExtraRoll,
        GainChangeDieResult,
        PayToGainChangeDieResult,
        PerformDieResultChange,
        GainChangeDieResultTo1,
        PerformDieResultChangeToValue,
        GainRerollThrees,
        IgnoreSpecialRollChanges,
        IgnoreSpecialEnemyRollChanges,
        IgnoreSpecialDieUsage,
        IgnoreSpecialDamageReduction,
        FinishedDuplicatingCard,
        FinishedRefundingCards
    }

    public ID id;

    public static Option StartTurn = new Option("Start Turn", (x) => GameController.instance.StartTurn(), ID.StartTurn);
    public static Option Roll = new Option("Roll", (x) => GameController.instance.StandardTurnRoll(), ID.Roll);
    public static Option KeepRolls = new Option("Keep Rolls", (x) => GameController.instance.KeepRolls(), ID.KeepRolls);
    public static Option Resolve = new Option("Resolve", (x) => GameController.instance.Resolve(), ID.Resolve);
    public static Option Leave(Player playerToLeave)
    {
        return new Option("Leave", delegate { GameController.instance.LeaveInside(playerToLeave, true); }, ID.Leave);
    }
    public static Option Stay(Player playerToStay)
    {
        return new Option("Stay", delegate { GameController.instance.StayInside(playerToStay); }, ID.Stay);
    }

    public static Option FinishedBrowsingCards = new Option("Finished Browsing Cards", (x) => GameController.instance.FinishedBrowsingCards(), ID.FinishedBrowsingCards);
    public static Option RefreshCards = new Option("Refresh Cards", (x) => GameController.instance.RefreshCards(), ID.RefreshCards);

    public static Option BuyCardInstantly(Player player, Card card)
    {
        return new Option("Buy Card", (x) => GameController.instance.BuyCardInstantly(player, card), ID.BuyCardInstantly);
    }

    public static Option DontBuyCardInstantly = new Option("Leave Card", (x) => GameController.instance.CheckIfPlayersCanBuyCardInstantly(), ID.DontBuyCardInstantly);

    public static Option EndTurn = new Option("End Turn", (x) => GameController.instance.EndTurn(), ID.EndTurn);

    public static Option NewGame = new Option("New Game", (x) => GameController.instance.NewGame(), ID.NewGame);

    public static Option PayToBecomeInvulnerable(Player playerToBecomeInvulnerable, Player playerCausingDamage, int damage)
    {
        return new Option("Become Invulnerable", x => GameController.instance.PayToBecomeInvulnerable(playerToBecomeInvulnerable, playerCausingDamage, damage), ID.PayToBecomeInvulnerable);
    }
    public static Option GainPayToReduceDamage(Player playerToPay, Player playerCausingDamage, int damage)
    {
        return new Option("Pay to Reduce Damage", x => GameController.instance.GainPayToReduceDamage(playerToPay, playerCausingDamage, damage), ID.GainPayToReduceDamage);
    }
    public static Option GainRollToReduceDamage(Player playerToRoll, Player playerCausingDamage, int damage)
    {
        return new Option("Roll to Reduce Damage", x => GameController.instance.GainRollToReduceDamage(playerToRoll, playerCausingDamage, damage), ID.GainRollToReduceDamage);
    }

    public static Option PayToReduceDamage(Player playerToHeal, Player playerCausingDamage, int damage)
    {
        return new Option("Pay to Reduce Damage", x => GameController.instance.PayToReduceDamage(playerToHeal, playerCausingDamage, damage, x), ID.PayToReduceDamage);
    }
    public static Option RollToReduceDamage(Player playerToHeal, Player playerDealingDamage, int damage)
    {
        return new Option("Roll", x => GameController.instance.RollToReduceDamage(playerToHeal, playerDealingDamage, damage), ID.RollToReduceDamage);
    }

    public static Option GainRerollEnemyDie(Player playerWithCard)
    {
        return new Option("Reroll Player's Die", (x) => GameController.instance.GainRerollEnemyDie(playerWithCard), ID.GainRerollEnemyDie);
    }
    public static Option PerformRerollEnemyDie(Player playerWithCard)
    {
        return new Option("Reroll Player's Die", (x) => GameController.instance.RerollEnemyDie(playerWithCard), ID.PerformRerollEnemyDie);
    }

    public static Option GainHealEnemy = new Option("Heal Enemies", x => GameController.instance.GainHealEnemy(), ID.GainHealEnemy);
    public static Option ChooseEnemyToHeal = new Option("Choose Enemy", x => GameController.instance.ChooseEnemyToHeal(x), ID.ChooseEnemyToHeal);
    public static Option PerformHealEnemy = new Option("Heal", x => GameController.instance.HealEnemy(x), ID.PerformHealEnemy);

    public static Option GainReduceVenomLevel = new Option("Reduce Venom Level", x => GameController.instance.GainReduceVenomLevel(), ID.GainReduceVenomLevel);
    public static Option GainReviveDeadDice = new Option("Revive Dead Dice", x => GameController.instance.GainReviveDeadDice(), ID.GainReviveDeadDice);

    public static Option ReduceVenomLevel = new Option("Devenom", x => GameController.instance.ReduceVenomLevel(x), ID.ReduceVenomLevel);
    public static Option ReviveDeadDice = new Option("Revive", x => GameController.instance.ReviveDeadDie(x), ID.ReviveDeadDice);

    public static Option GainExtraRoll = new Option("Use Extra Roll", x => GameController.instance.GainExtraRoll(0), ID.GainExtraRoll);
    public static Option PayToGainExtraRoll = new Option("Buy Extra Roll", x => GameController.instance.GainExtraRoll(1), ID.PayToGainExtraRoll);

    public static Option GainChangeDieResult = new Option("Change Die Result", x => GameController.instance.GainDieChange(0), ID.GainChangeDieResult);
    public static Option PayToGainChangeDieResult = new Option("Pay to Change Die Result", x => GameController.instance.GainDieChange(2), ID.PayToGainChangeDieResult);
    public static Option PerformDieResultChange = new Option("Change", x => GameController.instance.ChangeDieResult(x), ID.PerformDieResultChange);

    public static Option GainChangeDieResultTo1 = new Option("Change Die Result To 1", x => GameController.instance.GainDieChangeToValue(0), ID.GainChangeDieResultTo1);
    public static Option PerformDieResultChangeToValue(int dieValue)
    {
        return new Option("Change", x => GameController.instance.ChangeDieResult(dieValue), ID.PerformDieResultChangeToValue);
    }
    
    public static Option GainRerollThrees = new Option("Reroll Threes", x => GameController.instance.GainThreesReroll(), ID.GainRerollThrees);

    public static Option IgnoreSpecialRollChanges = new Option("Continue", x => GameController.instance.IgnoreSpecialRollChanges(), ID.IgnoreSpecialRollChanges);
    public static Option IgnoreSpecialEnemyRollChanges(Player playerWithCard)
    {
        return new Option("Continue", (x) => GameController.instance.IgnoreSpecialEnemyRollChanges(playerWithCard), ID.IgnoreSpecialEnemyRollChanges);
    }
    public static Option IgnoreSpecialDieUsage = new Option("Continue", x => GameController.instance.IgnoreSpecialDiceUsage(), ID.IgnoreSpecialDieUsage);
    public static Option IgnoreSpecialDamageReduction(Player player, Player playerCausingDamage, int damage)
    {
        return new Option("Continue", x => GameController.instance.IgnoreSpecialDamageReduction(player, playerCausingDamage, damage), ID.IgnoreSpecialDamageReduction);
    }

    public static Option FinishedDuplicatingCard = new Option("Continue", x => GameController.instance.FinishedDuplicatingCards(), ID.FinishedDuplicatingCard);

    public static Option FinishedRefundingCards = new Option("Continue", x => GameController.instance.FinishedRefundingCards(), ID.FinishedRefundingCards);

    public Option(string name, Action<int> method, ID id)
    {
        this.name = name;
        this.method = method;
        this.id = id;
    }
}
