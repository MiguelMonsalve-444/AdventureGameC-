using System.Diagnostics.Contracts;

namespace AdventureGame;

public class AdventureGame
{
	public readonly string GO_NORTH = "W";
	public readonly string GO_SOUTH = "S";
	public readonly string GO_EAST = "D";
	public readonly string GO_WEST = "A";
	public readonly string GET_LAMP = "L";
	public readonly string GET_KEY = "K";
	public readonly string OPEN_CHEST = "O";
	public readonly string QUIT = "Q";

	private Adventurer adventurer;
	private Room[,] dungeon;
	private int aRow;
	private int aCol;
	private bool isChestOpen;
	private bool hasPlayerQuit;
	private bool isAdventureAlive;
	private string lastDirection;
	private bool grueIsChasing;
	private bool hasPlayerWon;
	private int grueRow;
	private int grueCol;
	private int exitRow;
	private int exitCol;



	public AdventureGame()
	{
		
	}

	public void Start()
	{
		Init();

		ShowGameStartScreen();

		string input;

		do
		{
			ShowScene();

			do
			{
				ShowInputOptions();

				input = GetInput();
			}
			while(!IsValidInput(input));

			ProcessInput(input);

			UpdateGameState();
		}
		while(!IsGameOver());

		ShowGameOverScreen();
	}

	private void Init()
	{
		adventurer = new Adventurer();

		DungeonData data = LoadDungeon("res/Dungeon.txt");

		dungeon = data.Dungeon;
		
		aRow = data.StartRow;
		aCol = data.StartCol;

		grueCol = data.GrueCol;
		grueRow = data.GrueRow;

		exitRow = data.ExitRow;
		exitCol = data.ExitCol;

		isChestOpen = false;
		hasPlayerQuit = false;
		isAdventureAlive = true;
		grueIsChasing = false;
		hasPlayerWon = false;

		lastDirection = string.Empty;
	}

	private DungeonData LoadDungeon(string filePath)
	{
		string[] lines = File.ReadAllLines(filePath);
		int index = 0;

		int rows = int.Parse(lines[index++]);
		int cols = int.Parse(lines[index++]);

		int startRow = int.Parse(lines[index++]);
		int startCol = int.Parse(lines[index++]);

		int exitRow = int.Parse(lines[index++]);
		int exitCol = int.Parse(lines[index++]);

		int lampRow = int.Parse(lines[index++]);
		int lampCol = int.Parse(lines[index++]);

		int keyRow = int.Parse(lines[index++]);
		int keyCol = int.Parse(lines[index++]);

		int chestRow = int.Parse(lines[index++]);
		int chestCol = int.Parse(lines[index++]);

		int grueRow = int.Parse(lines[index++]);
		int grueCol = int.Parse(lines[index++]);

		Room[,] loadedDungeon = new Room[rows, cols];

		for(int row = 0; row < rows; row++)
		{
			string mapLine = lines[index++];

			for(int col = 0; col < cols; col++)
			{
				if(mapLine[col] != '#')
				{
					loadedDungeon[row, col] = new Room();
				}
			}
		}

		int descriptionIndex = 0;

		while(index < lines.Length)
		{
			string line = lines[index++];

			if(string.IsNullOrWhiteSpace(line))
			{
				continue;
			}

			string[] parts = line.Split('|');
			bool isLit = parts[0] == "1";
			string description = parts[1];

			Room room = GetRoomByNumber(loadedDungeon, descriptionIndex);

			if(room != null)
			{
				room.SetLit(isLit);
				room.SetDescription(description);
			}

			descriptionIndex++;
		}

		SetRoomConnections(loadedDungeon);

		GetRoomOrThrow(loadedDungeon, startRow, startCol, "start");
		GetRoomOrThrow(loadedDungeon, exitRow, exitCol, "exit");
		GetRoomOrThrow(loadedDungeon, grueRow, grueCol, "grue");

		GetRoomOrThrow(loadedDungeon, lampRow, lampCol, "lamp").SetLamp(true);
		GetRoomOrThrow(loadedDungeon, keyRow, keyCol, "key").SetKey(true);
		GetRoomOrThrow(loadedDungeon, chestRow, chestCol, "chest").SetChest(true);

		return new DungeonData
		{
			Dungeon = loadedDungeon,
			StartRow = startRow,
			StartCol = startCol,
			ExitRow = exitRow,
			ExitCol = exitCol,
			LampRow = lampRow,
			LampCol = lampCol,
			KeyRow = keyRow,
			KeyCol = keyCol,
			ChestRow = chestRow,
			ChestCol = chestCol,
			GrueRow = grueRow,
			GrueCol = grueCol
		};
	}

	private Room GetRoomByNumber(Room[,] loadedDungeon, int roomNumber)
	{
		int count = 0;

		for(int row = 0; row < loadedDungeon.GetLength(0); row++)
		{
			for(int col = 0; col < loadedDungeon.GetLength(1); col++)
			{
				if(loadedDungeon[row, col] != null)
				{
					if(count == roomNumber)
					{
						return loadedDungeon[row, col];
					}

					count++;
				}
			}
		}

		return null;
	}

	private Room GetRoomOrThrow(Room[,] loadedDungeon, int row, int col, string entityName)
	{
		int rows = loadedDungeon.GetLength(0);
		int cols = loadedDungeon.GetLength(1);

		if(row < 0 || row >= rows || col < 0 || col >= cols)
		{
			throw new InvalidDataException(
				$"Invalid {entityName} coordinates ({row}, {col}). Valid range is row 0-{rows - 1}, col 0-{cols - 1}.");
		}

		Room room = loadedDungeon[row, col];

		if(room == null)
		{
			throw new InvalidDataException(
				$"Invalid {entityName} coordinates ({row}, {col}). The selected cell is a wall, not a room.");
		}

		return room;
	}

	private void SetRoomConnections(Room[,] loadedDungeon)
	{
		int rows = loadedDungeon.GetLength(0);
		int cols = loadedDungeon.GetLength(1);

		for(int row = 0; row < rows; row++)
		{
			for(int col = 0; col < cols; col++)
			{
				Room room = loadedDungeon[row, col];

				if(room == null)
				{
					continue;
				}

				if(row > 0 && loadedDungeon[row - 1, col] != null)
					room.SetNorth(true);

				if(row < rows - 1 && loadedDungeon[row + 1, col] != null)
					room.SetSouth(true);

				if(col > 0 && loadedDungeon[row, col - 1] != null)
					room.SetWest(true);

				if(col < cols - 1 && loadedDungeon[row, col + 1] != null)
					room.SetEast(true);
			}
		}
	}

	private void ShowGameStartScreen()
	{
		Console.WriteLine("Welcome to Adventure Game!");
	}

	private void ShowScene()
	{
		var r = dungeon[aRow, aCol];

		if(adventurer.HasLamp() || r.IsLit())
		{
			Console.WriteLine($"{r.GetDescription()} [{aRow},{aCol}]");

			if(r.HasChest() && !isChestOpen )
			{
				Console.WriteLine("There is a chest in this room!");
			}
			if(r.HasKey())
			{
				Console.WriteLine("This room has a key.");
			}
			if(r.HasLamp())
			{
				Console.WriteLine("There is a lamp in this room.");
			}
		} else
			{
				Console.WriteLine("The room is pitch black.");
			}
		
	}

	private void ShowInputOptions()
	{
		string options = ""
		+ $"GO NORTH [{GO_NORTH}] | GO EAST [{GO_EAST}] | GET LAMP [{GET_LAMP}] | OPEN CHEST [{OPEN_CHEST}]\n"
		+ $"GO SOUTH [{GO_SOUTH}] | GO WEST [{GO_WEST}] | GET KEY  [{GET_KEY}] | QUIT       [{QUIT}]\n"
		+ $"> ";

		Console.Write(options);
	}

	private string GetInput()
	{
		return (Console.ReadLine() ?? QUIT).ToUpper();
	}

	private bool IsValidInput(string input)
	{
		string[] validInputs = { GO_NORTH, GO_SOUTH, GO_EAST, GO_WEST, GET_LAMP, GET_KEY, OPEN_CHEST, QUIT };

		if(!validInputs.Contains(input))
		{
			Console.WriteLine("ERROR: Invalid input. Please try again.");
			return false;
		}

		return true;
	}

	private void ProcessInput(string input)
	{
		Room r = dungeon[aRow, aCol];

		if(input == GO_NORTH)
		{
			GoNorth(r);
		}
		else if(input == GO_SOUTH)
		{
			GoSouth(r);
		}
		else if(input == GO_EAST)
		{
			GoEast(r);
		}
		else if(input == GO_WEST)
		{
			GoWest(r);
		}
		else if(input == GET_LAMP)
		{
			GetLamp(r);
		}
		else if(input == GET_KEY)
		{
			GetKey(r);
		}
		else if(input == OPEN_CHEST)
		{
			OpenChest(r);
		}
		else// if(input == QUIT)
		{
			Quit();
		}
		
		// Verificar si el jugador entro a una habitacion oscura sin la lampara
		if(isMovementInput(input))
		{
			Room currentRoom = dungeon[aRow, aCol];

			if(!adventurer.HasLamp() && !currentRoom.IsLit())
			{
				Console.WriteLine("You entered a dark room and got eaten by the Grue!");
				isAdventureAlive = false;
			}
		}
	}

	//Verificar si hubo imput despues de entrar en la habitacion con el grue 
	private bool isMovementInput(string input)
	{
		return input == GO_NORTH ||
			   input == GO_SOUTH ||
			   input == GO_EAST  ||
			   input == GO_WEST;
	}

	private void UpdateGameState()
	{
		if(grueIsChasing && aRow == grueRow && aCol == grueCol)
		{
			Console.WriteLine("Now you walked into the Grue.");
			isAdventureAlive = false;
			return;

		}

		if(grueIsChasing)
		{
			MoveGrue();

			if(aRow == grueRow && aCol == grueCol)
			{
				Console.WriteLine("The grue caught you!");
				isAdventureAlive = false;
				return;
			}
		}

		if(isChestOpen && aRow == exitRow && aCol == exitCol)
		{
			Console.WriteLine("You escaped with the treasure! You win!");
			hasPlayerWon = true;
		}
	}

	// Mover el grue hacia el jugador
	private void MoveGrue()
	{
		if(grueRow < aRow)
		{
			grueRow += 1;
		}
		else if(grueRow > aRow)
		{
			grueRow -= 1;
		}else if (grueCol < aCol)
		{
			grueCol += 1;
		}
		else if(grueCol > aCol)
		{
			grueCol -= 1;
		}
	}

	private bool IsGameOver()
	{
		return hasPlayerWon || hasPlayerQuit || !isAdventureAlive;
	}

	private void ShowGameOverScreen()
	{
		if(hasPlayerWon == true)
		{
			Console.WriteLine("Congrats, You Won!");
		}
		else
		{
			Console.WriteLine("Game Over!"); 
		}
	}

	private void GoNorth(Room r)
	{
		if(r.HasNorth())
		{
			aRow -= 1;
			lastDirection = GO_NORTH;
		}
		else
		{
			Console.WriteLine("You cannot go north!\a");
		}
	}

	private void GoSouth(Room r)
	{
		if(r.HasSouth())
		{
			aRow += 1;
			lastDirection = GO_SOUTH;
		}
		else
		{
			Console.WriteLine("You cannot go south!\a");
		}
	}

	private void GoEast(Room r)
	{
		if(r.HasEast())
		{
			aCol += 1;
			lastDirection = GO_EAST;
		}
		else
		{
			Console.WriteLine("You cannot go east!\a");
		}
	}

	private void GoWest(Room r)
	{
		if(r.HasWest())
		{
			aCol -= 1;
			lastDirection = GO_WEST;
		}
		else
		{
			Console.WriteLine("You cannot go west!\a");
		}
	}

	private void GetLamp(Room r)
	{
		if(r.HasLamp())
		{
			Console.WriteLine("You got the lamp!");
			adventurer.SetLamp(true);
			r.SetLamp(false);
		}
		else
		{
			Console.WriteLine("There is no lamp in this room.");
		}
	}

	private void GetKey(Room r)
	{
		if(r.HasKey())
		{
			Console.WriteLine("You got the key!");
			adventurer.SetKey(true);
			r.SetKey(false);
		}
		else
		{
			Console.WriteLine("There is no key in this room.");
		}
	}

	private void OpenChest(Room r)
	{
		if(r.HasChest())
		{
			if(adventurer.HasKey())
			{
				Console.WriteLine("You got the treasure!");
				Console.WriteLine("The grue is chasing you!");
				isChestOpen = true;
				grueIsChasing = true;
			}
			else
			{
				Console.WriteLine("You do not have the key!");
			}
		}
		else
		{
			Console.WriteLine("There is no chest in this room.");
		}
	}

	private void Quit()
	{
		Console.WriteLine("You quit the game!");
		hasPlayerQuit = true;
	}
}
