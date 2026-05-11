namespace AdventureGame;

public class DungeonData
{
public Room[,] Dungeon { get; set; }

public int ExitRow { get; set; }
public int ExitCol { get; set; }

public int LampRow { get; set; }
public int LampCol { get; set; }

public int KeyRow { get; set; }
public int KeyCol { get; set; }

public int ChestRow { get; set; }
public int ChestCol { get; set; }

public int GrueRow { get; set; }
public int GrueCol { get; set; }

}

public class DungeonLoader
{
    public DungeonData Load(string filePath)
    {
        string[] lines = File.ReadAllLines(filePath);

        int index = 0;

        int rows = int.Parse(lines[index++]);
        int cols = int.Parse(lines[index++]);

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

        Room[,] dungeon = new Room[rows, cols];

        for(int row = 0; row < rows; row++)
        {
            string mapLine = lines[index++];

                for(int col = 0; col < cols; col++)
                {
                    if(mapLine[col] != '#')
                    {
                        dungeon[row, col] = new Room();
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

            Room room = GetRoomByNumber(dungeon, descriptionIndex);

            if(room != null)
            {
                room.SetLit(isLit);
                room.SetDescription(description);
            }

            descriptionIndex++;
        }

        SetRoomConnections(dungeon);

        dungeon[lampRow, lampCol].SetLamp(true);
        dungeon[keyRow, keyCol].SetKey(true);
        dungeon[chestRow, chestCol].SetChest(true);

        return new DungeonData
        {
            Dungeon = dungeon,

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

    private Room GetRoomByNumber(Room[,] dungeon, int roomNumber)
    {
        int count = 0;

        for(int row = 0; row < dungeon.GetLength(0); row++)
        {
            for(int col = 0; col < dungeon.GetLength(1); col++)
            {
                if(dungeon[row, col] != null)
                {
                    if(count == roomNumber)
                    {
                        return dungeon[row, col];
                    }

                    count++;
                }
            }
        }

        return null;
    }

    private void SetRoomConnections(Room[,] dungeon)
    {
        int rows = dungeon.GetLength(0);
        int cols = dungeon.GetLength(1);

        for(int row = 0; row < rows; row++)
        {
            for(int col = 0; col < cols; col++)
            {
                Room room = dungeon[row, col];

                if(room == null)
                {
                    continue;
                }

                if(row > 0 && dungeon[row - 1, col] != null)
                    room.SetNorth(true);

                if(row < rows - 1 && dungeon[row + 1, col] != null)
                    room.SetSouth(true);

                if(col > 0 && dungeon[row, col - 1] != null)
                    room.SetWest(true);

                if(col < cols - 1 && dungeon[row, col + 1] != null)
                    room.SetEast(true);
            }
        }
    }
}