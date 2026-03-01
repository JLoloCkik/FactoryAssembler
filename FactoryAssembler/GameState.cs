using System.Collections.Generic;
using Raylib_cs;

namespace FactoryAssembler;

public struct CardBlueprint
{
    public string Name; public Color Color; public int Cost; public string Description;
}

public class Quest
{
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
        {"Coal", 0}, {"Iron Ore", 0}, {"Copper Ore", 0},
        {"Iron Ingot", 0}, {"Copper Ingot", 0}, {"Wall", 0}, {"Gear", 0}, {"Rocket", 0}
    };

    public static List<CardBlueprint> Blueprints = new List<CardBlueprint>()
    {
        new CardBlueprint { Name = "Coal Miner", Color = new Color(50, 50, 50, 255), Cost = 50, Description = "Mines: Coal" },
        new CardBlueprint { Name = "Iron Miner", Color = new Color(200, 120, 50, 255), Cost = 150, Description = "Mines: Iron Ore" },
        new CardBlueprint { Name = "Copper Miner", Color = new Color(200, 80, 40, 255), Cost = 150, Description = "Mines: Copper Ore" },
        new CardBlueprint { Name = "Smelter", Color = new Color(230, 41, 55, 255), Cost = 200, Description = "Cost: 1 Ore + 1 Coal\nMakes: Ingot" },
        new CardBlueprint { Name = "Assembler", Color = new Color(0, 228, 48, 255), Cost = 500, Description = "Cost: 2 Iron Ingot\nMakes: Gear" },
        new CardBlueprint { Name = "Rocket Silo", Color = new Color(200, 0, 200, 255), Cost = 2000, Description = "Cost: 10 Gear + 10 Copper Ingot\nMakes: ROCKET (WIN)" }
    };

    public static Dictionary<string, int> GlobalUnlocks = new Dictionary<string, int>();
    public static List<Quest> Quests = QuestDatabase.GetQuests();
    public static int CurrentQuestIndex = 0;
}