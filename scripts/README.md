# Scripts

## GameController.cs

The GameController.cs script is the central controller for the game logic. It contains all the methods for each Decision a Player can make, and within its `Update()` method, decides which Decision to currently show to which Player.

## Decision.cs and Option.cs

The Decision.cs class contains information relating to each Decision, such as the Player making the Decision, and all possible Options for the Decision. It also contains static methods which can be used to create an instance of each kind of Decision, and static methods for AI to decide how to make a Decision. Each of the Options is an instance of the Option.cs class, which contain a reference to a method from the GameController.cs script.

## SimulationController.cs

## UIController.cs
