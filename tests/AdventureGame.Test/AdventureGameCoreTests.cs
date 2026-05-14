using System;
using System.IO;
using System.Reflection;
using Xunit;
using AdventureGame;

namespace AdventureGame.Tests
{
    public class AdventureGameCoreTests
    {
        private static T GetField<T>(object target, string name)
        {
            var f = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(f);
            return (T)f!.GetValue(target)!;
        }

        private static void SetField<T>(object target, string name, T value)
        {
            var f = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(f);
            f!.SetValue(target, value);
        }

        private static object? Call(object target, string name, params object?[] args)
        {
            var m = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(m);
            return m!.Invoke(target, args);
        }

        private static void ExecuteTurn(object game, string input)
        {
            Call(game, "ProcessInput", input);
            Call(game, "UpdateGameState");
        }

        private static string CaptureOut(Action act)
        {
            var sw = new StringWriter();
            var original = Console.Out;
            Console.SetOut(sw);
            try { act(); }
            finally { Console.SetOut(original); }
            return sw.ToString();
        }

        private static string Const(object g, string field) =>
            (string)g.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public)!.GetValue(g)!;

        [Fact]
        public void Init_LoadsExpectedMapState_FromDungeonFile()
        {
            var g = new AdventureGame();
            Call(g, "Init");

            // Coordinates now come from the 8x8 dungeon file.
            Assert.Equal(1, GetField<int>(g, "aRow"));
            Assert.Equal(1, GetField<int>(g, "aCol"));
            Assert.Equal(6, GetField<int>(g, "exitRow"));
            Assert.Equal(6, GetField<int>(g, "exitCol"));
            Assert.Equal(6, GetField<int>(g, "grueRow"));
            Assert.Equal(1, GetField<int>(g, "grueCol"));

            var dungeon = GetField<Room[,]>(g, "dungeon");
            Assert.Equal("Entrance chamber", dungeon[1, 1].GetDescription());
            Assert.True(dungeon[2, 1].HasLamp());
            Assert.True(dungeon[3, 5].HasKey());
            Assert.True(dungeon[3, 4].HasChest());

            Assert.False(GetField<bool>(g, "isChestOpen"));
            Assert.False(GetField<bool>(g, "hasPlayerQuit"));
            Assert.True(GetField<bool>(g, "isAdventureAlive"));
            Assert.False(GetField<bool>(g, "hasPlayerWon"));
            Assert.False(GetField<bool>(g, "grueIsChasing"));
            Assert.Equal(string.Empty, GetField<string>(g, "lastDirection"));
        }

        [Fact]
        public void Movement_UpdatesPositionAndLastDirection()
        {
            var g = new AdventureGame();
            Call(g, "Init");

            var GO_EAST  = Const(g, "GO_EAST");
            var GO_WEST  = Const(g, "GO_WEST");

            ExecuteTurn(g, GO_EAST);
            Assert.Equal(1, GetField<int>(g, "aRow"));
            Assert.Equal(2, GetField<int>(g, "aCol"));
            Assert.Equal(GO_EAST, GetField<string>(g, "lastDirection"));

            ExecuteTurn(g, GO_WEST);
            Assert.Equal(1, GetField<int>(g, "aRow"));
            Assert.Equal(1, GetField<int>(g, "aCol"));
            Assert.Equal(GO_WEST, GetField<string>(g, "lastDirection"));
        }

        [Fact]
        public void InvalidMove_PrintsErrorAndPositionUnchanged()
        {
            var g = new AdventureGame();
            Call(g, "Init");

            // From start [1,0] (Room 3) has no West exit.
            var GO_WEST = Const(g, "GO_WEST");

            var beforeRow = GetField<int>(g, "aRow");
            var beforeCol = GetField<int>(g, "aCol");

            var output = CaptureOut(() => ExecuteTurn(g, GO_WEST));
            Assert.Contains("cannot go west", output);
            Assert.Equal(beforeRow, GetField<int>(g, "aRow"));
            Assert.Equal(beforeCol, GetField<int>(g, "aCol"));
        }

        [Fact]
        public void EnteringDarkRoomWithoutLamp_KillsPlayerAndEndsGame()
        {
            var g = new AdventureGame();
            Call(g, "Init");

            var GO_EAST = Const(g, "GO_EAST");

            ExecuteTurn(g, GO_EAST);
            ExecuteTurn(g, GO_EAST);
            var output = CaptureOut(() => ExecuteTurn(g, GO_EAST)); // enters dark room at [1,4]

            Assert.Contains("entered a dark room", output);
            Assert.False(GetField<bool>(g, "isAdventureAlive"));
            Assert.True((bool)Call(g, "IsGameOver")!);
        }

        [Fact]
        public void GetLampAndKey_WorkAndRemoveItemsFromRooms()
        {
            var g = new AdventureGame();
            Call(g, "Init");

            var GO_SOUTH = Const(g, "GO_SOUTH");
            var GO_NORTH = Const(g, "GO_NORTH");
            var GO_EAST = Const(g, "GO_EAST");
            var GO_WEST = Const(g, "GO_WEST");
            var GET_LAMP = Const(g, "GET_LAMP");
            var GET_KEY = Const(g, "GET_KEY");

            var dungeon = GetField<Room[,]>(g, "dungeon");
            Assert.True(dungeon[2, 1].HasLamp());
            Assert.True(dungeon[3, 5].HasKey());

            ExecuteTurn(g, GO_SOUTH);
            var lampOut = CaptureOut(() => ExecuteTurn(g, GET_LAMP));
            Assert.Contains("You got the lamp!", lampOut);

            ExecuteTurn(g, GO_NORTH);
            ExecuteTurn(g, GO_EAST);
            ExecuteTurn(g, GO_EAST);
            ExecuteTurn(g, GO_EAST);
            ExecuteTurn(g, GO_EAST);
            ExecuteTurn(g, GO_EAST);
            ExecuteTurn(g, GO_SOUTH);
            ExecuteTurn(g, GO_SOUTH);
            ExecuteTurn(g, GO_WEST);
            var keyOut = CaptureOut(() => ExecuteTurn(g, GET_KEY));
            Assert.Contains("You got the key!", keyOut);

            var adventurer = GetField<Adventurer>(g, "adventurer");
            Assert.True(adventurer.HasLamp());
            Assert.True(adventurer.HasKey());
            Assert.False(dungeon[2, 1].HasLamp());
            Assert.False(dungeon[3, 5].HasKey());
        }

        [Fact]
        public void OpenChest_NoKey_ShowsWarning_DoesNotEndGame()
        {
            var g = new AdventureGame();
            Call(g, "Init");

            var GO_SOUTH = Const(g, "GO_SOUTH");
            var GO_NORTH = Const(g, "GO_NORTH");
            var GO_EAST = Const(g, "GO_EAST");
            var GO_WEST = Const(g, "GO_WEST");
            var GET_LAMP = Const(g, "GET_LAMP");
            var OPEN_CHEST = Const(g, "OPEN_CHEST");

            ExecuteTurn(g, GO_SOUTH);
            ExecuteTurn(g, GET_LAMP);
            ExecuteTurn(g, GO_NORTH);
            ExecuteTurn(g, GO_EAST);
            ExecuteTurn(g, GO_EAST);
            ExecuteTurn(g, GO_EAST);
            ExecuteTurn(g, GO_EAST);
            ExecuteTurn(g, GO_EAST);
            ExecuteTurn(g, GO_SOUTH);
            ExecuteTurn(g, GO_SOUTH);
            ExecuteTurn(g, GO_WEST);
            ExecuteTurn(g, GO_WEST);

            var out1 = CaptureOut(() => ExecuteTurn(g, OPEN_CHEST));
            Assert.Contains("do not have the key", out1);
            Assert.False(GetField<bool>(g, "isChestOpen"));
            Assert.True(GetField<bool>(g, "isAdventureAlive"));
            Assert.False(GetField<bool>(g, "hasPlayerQuit"));
            Assert.False((bool)Call(g, "IsGameOver")!);
        }

        [Fact]
        public void OpenChest_ThenReachExit_WinsGameAndEnablesGrueChase()
        {
            var g = new AdventureGame();
            Call(g, "Init");

            var GO_SOUTH = Const(g, "GO_SOUTH");
            var GO_NORTH = Const(g, "GO_NORTH");
            var GO_EAST = Const(g, "GO_EAST");
            var GO_WEST = Const(g, "GO_WEST");
            var GET_LAMP = Const(g, "GET_LAMP");
            var GET_KEY = Const(g, "GET_KEY");
            var OPEN_CHEST = Const(g, "OPEN_CHEST");

            ExecuteTurn(g, GO_SOUTH);
            ExecuteTurn(g, GET_LAMP);
            ExecuteTurn(g, GO_NORTH);
            ExecuteTurn(g, GO_EAST);
            ExecuteTurn(g, GO_EAST);
            ExecuteTurn(g, GO_EAST);
            ExecuteTurn(g, GO_EAST);
            ExecuteTurn(g, GO_EAST);
            ExecuteTurn(g, GO_SOUTH);
            ExecuteTurn(g, GO_SOUTH);
            ExecuteTurn(g, GO_WEST);
            ExecuteTurn(g, GET_KEY);
            ExecuteTurn(g, GO_WEST);

            var output = CaptureOut(() => ExecuteTurn(g, OPEN_CHEST));
            Assert.Contains("You got the treasure!", output);
            Assert.True(GetField<bool>(g, "grueIsChasing"));
            Assert.True(GetField<bool>(g, "isChestOpen"));
            Assert.False(GetField<bool>(g, "hasPlayerWon"));

            ExecuteTurn(g, GO_EAST);
            ExecuteTurn(g, GO_EAST);
            ExecuteTurn(g, GO_SOUTH);
            ExecuteTurn(g, GO_SOUTH);
            ExecuteTurn(g, GO_SOUTH);

            Assert.True(GetField<bool>(g, "hasPlayerWon"));
            Assert.True((bool)Call(g, "IsGameOver")!);
        }

        [Fact]
        public void Quit_SetsFlagAndEndsGame()
        {
            var g = new AdventureGame();
            Call(g, "Init");

            var QUIT = Const(g, "QUIT");
            var output = CaptureOut(() => ExecuteTurn(g, QUIT));

            Assert.Contains("quit the game", output);
            Assert.True(GetField<bool>(g, "hasPlayerQuit"));
            Assert.True((bool)Call(g, "IsGameOver")!);
        }

        [Theory]
        [InlineData("X")]
        [InlineData("")]
        [InlineData(" north ")]
        public void IsValidInput_InvalidInputs_PrintError_AndReturnFalse(string raw)
        {
            var g = new AdventureGame();
            Call(g, "Init");

            var outText = CaptureOut(() =>
            {
                var result = (bool)Call(g, "IsValidInput", raw)!;
                Assert.False(result);
            });

            Assert.Contains("Invalid input", outText);
        }

        [Fact]
        public void ShowGameStartAndGameOver_SideEffectsArePrinted()
        {
            var g = new AdventureGame();

            var startOut = CaptureOut(() => Call(g, "ShowGameStartScreen"));
            Assert.Contains("Welcome to Adventure Game!", startOut);

            var endOut = CaptureOut(() => Call(g, "ShowGameOverScreen"));
            Assert.Contains("Game Over!", endOut);
        }

        [Fact]
        public void Start_FullHappyPath_WinsWithTreasureAndExit()
        {
            // Path: get lamp, get key, open chest, then escape through exit.
            var inputs = string.Join(Environment.NewLine, new[]
            {
                "S", "L", "W", "D", "D", "D", "D", "D", "S", "S", "A", "K", "A", "O", "D", "D", "S", "S", "S"
            }) + Environment.NewLine;

            Console.SetIn(new StringReader(inputs));

            var outWriter = new StringWriter();
            var originalOut = Console.Out;
            Console.SetOut(outWriter);

            try
            {
                var g = new AdventureGame();
                g.Start();
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            var output = outWriter.ToString();
            Assert.Contains("Welcome to Adventure Game!", output);
            Assert.Contains("You got the lamp!", output);
            Assert.Contains("You got the key!", output);
            Assert.Contains("You got the treasure!", output);
            Assert.Contains("You escaped with the treasure! You win!", output);
            Assert.Contains("Congrats, You Won!", output);
        }
    }
}
