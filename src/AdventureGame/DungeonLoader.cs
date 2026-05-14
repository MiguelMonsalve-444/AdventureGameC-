namespace AdventureGame;

public class DungeonData
{
    public Room[,] Dungeon { get; set; }

    public int StartRow { get; set; }
    public int StartCol { get; set; }

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