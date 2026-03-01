using System.Collections.Generic;
using Raylib_cs;

namespace FactoryAssembler;

public struct CardBlueprint
{
    public string Name; public Color Color; public int Cost; public string Description;
}

public class Quest
{
    // Itt adunk alap értéket (= "") a Warningok ellen
    public string Title { get; set; } = "";
    public string TargetItem { get; set; } = "";
    public int TargetAmount { get; set; }
    public int RewardCredits { get; set; }
}

public static class GameState
{
    public static int Credits = 100; 
    
    public static Dictionary<string, int> Inventory = new Dictionary<string, int>()
    {
        {"Coal", 0}, {"Stone", 0}, {"Iron Ore", 0}, {"Copper Ore", 0},
        {"Iron Ingot", 0}, {"Copper Ingot", 0}, {"Wall", 0}, {"Gear", 0}, {"Rocket", 0}
    };

    public static List<CardBlueprint> Blueprints = new List<CardBlueprint>()
    {
        new CardBlueprint { Name = "Coal Miner", Color = new Color(50, 50, 50, 255), Cost = 50, Description = "Mines: Coal\nTime: 1s\nRequires: None" },
        new CardBlueprint { Name = "Stone Miner", Color = new Color(130, 130, 130, 255), Cost = 50, Description = "Mines: Stone\nTime: 1s\nRequires: None" },
        new CardBlueprint { Name = "Iron Miner", Color = new Color(200, 120, 50, 255), Cost = 150, Description = "Mines: Iron Ore\nTime: 5s\nRequires: None" },
        new CardBlueprint { Name = "Copper Miner", Color = new Color(200, 80, 40, 255), Cost = 150, Description = "Mines: Copper Ore\nTime: 5s\nRequires: None" },
        new CardBlueprint { Name = "Smelter", Color = new Color(230, 41, 55, 255), Cost = 200, Description = "Smelts: Ingots\nTime: 5s\nRequires: 1 Ore + 1 Coal" },
        new CardBlueprint { Name = "Assembler", Color = new Color(0, 228, 48, 255), Cost = 500, Description = "Crafts: Wall, Gear, Rocket\nTime: 10s\nRequires: Various Parts" }
    };

    public static Dictionary<string, int> GlobalUnlocks = new Dictionary<string, int>();

    public static List<Quest> Quests = new List<Quest>()
    {
        new Quest { Title = "First Steps", TargetItem = "Stone", TargetAmount = 10, RewardCredits = 100 },
        new Quest { Title = "Fuel the Fire", TargetItem = "Coal", TargetAmount = 25, RewardCredits = 150 },
        new Quest { Title = "Iron Age", TargetItem = "Iron Ore", TargetAmount = 20, RewardCredits = 300 }
    };
    public static int CurrentQuestIndex = 0;
}