# Scripts

The game as a whole can be thought of simply as a series of Decisions to be made by a Player (Human or AI), the result of which informs the next Decision. The game is largely turn-based with the turns rotating between Players, but it is possible for a Decision to be given to a Player when it's not currently their turn. When a certain win condition is met, the game ends. The win condition of this game is to either reach the necessary amount of points (20 as standard) or be the last player alive, and it is only checked at the end of each turn. This means it is possible for there to be multiple winners, or none at all.

All 'Controller' scripts are singletons, meaning only one instance of this script can exist and can be called from other scripts.

## GameController.cs

The GameController.cs script is the central controller for the game logic. It contains all the methods for each Decision a Player can make, and within its `Update()` method, decides which Decision to currently show to which Player.

## Decision.cs and Option.cs

The Decision.cs class contains information relating to each Decision, such as the Player making the Decision, and all possible Options for the Decision. It also contains static methods which can be used to create an instance of each kind of Decision, and static methods for AI to decide how to make a Decision. Each of the Options is an instance of the Option.cs class, which contain a reference to a method from the GameController.cs script.

## Player.cs

The Player.cs class contains all the data relating to a Player, be they Human or AI. It stores the more obvious data, such as health, points and currency, and also data which could be considered 'modifiers' for the player, such as `venomLevel` and `deadDice`, both of which are effects that can be forced on a player and affect their gameplay by making them passively lose health and have a reduced number of dice to roll, respectively.

The class also contains values which drive the AI's decision making. This consists of: A ranking of preference of the 4 possible qualities to be gained from dice rolls (health, points, currency and damage); a series of flags to add unique qualities to each AI (e.g. `hatesInside` => AI will always leave the Castle when possible, `lovesBeingMean` => AI will make decisions that hinder other's progress over furthering their own progress); and a 'riskiness' level, which drives how much of a risk an AI is willing to take when it comes to desired dice results or going for the win over guaranteeing staying alive.

## SimulationController.cs

The SimulationController.cs script controls the 'Simulation' part of the game. That is, the parts of the game which are not essential to gameplay, but display gameplay elements and events to the players (this is separate to the 'UI' part of the game, which displays direct player stats to the players). The Simulation is driven by the gameplay, but does not affect the gameplay, but for a flag which disallows gameplay to continue whilst a Simulation is currently running.

## UIController.cs

The UIController.cs script controls the UI part of the game. This includes the players' health, points, currency and cards, and the current decision being presented. When the equivalent data within the game changes, the UI is updated accordingly. For example, when a Player increases their points, the method which performs this increase in the Player class also uses the UIController to change the UI element relating to that Player's points.
