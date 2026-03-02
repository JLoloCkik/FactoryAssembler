using System.Collections.Generic;
using Raylib_cs;
using System.IO;
using System.Text.Json;

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

public class GameSaveData
{
    public int Credits { get; set; }
    public int QuestIndex { get; set; }
    public Dictionary<string, int> Inventory { get; set; } = new();
    public Dictionary<string, int> Unlocks { get; set; } = new();
    public List<CardSaveData> Cards { get; set; } = new();
}

public class CardSaveData
{
    public string Name { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public List<string> Instructions { get; set; } = new();
}

public static class GameState
{
    public static int Credits = 100; 
    public static Dictionary<string, int> Inventory = new Dictionary<string, int>();
    public static Dictionary<string, int> GlobalUnlocks = new Dictionary<string, int>();
    public static List<Quest> Quests = QuestDatabase.GetQuests();
    public static int CurrentQuestIndex = 0;

    public static Dictionary<string, int> Prices = new Dictionary<string, int>()
    {
        {"Coal", 1}, {"Iron Ore", 2}, {"Copper Ore", 2},
        {"Iron Ingot", 6}, {"Copper Ingot", 6}, {"Wall", 10}, {"Gear", 15}, {"Rocket", 0} 
    };

    public static List<CardBlueprint> Blueprints = new List<CardBlueprint>()
    {
        new CardBlueprint { Name = "Coal Miner", Color = new Color(50, 50, 50, 255), Cost = 50, Description = "Mines: Coal\nTime: 1s" },
        new CardBlueprint { Name = "Iron Miner", Color = new Color(200, 120, 50, 255), Cost = 150, Description = "Mines: Iron Ore\nTime: 5s" },
        new CardBlueprint { Name = "Copper Miner", Color = new Color(200, 80, 40, 255), Cost = 150, Description = "Mines: Copper Ore\nTime: 5s" },
        new CardBlueprint { Name = "Smelter", Color = new Color(230, 41, 55, 255), Cost = 200, Description = "Requires: 1 Ore + 1 Coal\nMakes: Ingot" },
        new CardBlueprint { Name = "Assembler", Color = new Color(0, 228, 48, 255), Cost = 500, Description = "Requires: 2 Iron Ingot\nMakes: Gear" },
        new CardBlueprint { Name = "Rocket Silo", Color = new Color(200, 0, 200, 255), Cost = 2000, Description = "Req: 10 Gear + 10 Cu Ingot\nMakes: ROCKET (WIN!)" }
    };

    public static void ResetGame()
    {
        Credits = 100;
        CurrentQuestIndex = 0;
        GlobalUnlocks.Clear();
        Inventory.Clear();
        Inventory.Add("Coal", 0); Inventory.Add("Iron Ore", 0); Inventory.Add("Copper Ore", 0);
        Inventory.Add("Iron Ingot", 0); Inventory.Add("Copper Ingot", 0); 
        Inventory.Add("Wall", 0); Inventory.Add("Gear", 0); Inventory.Add("Rocket", 0);
    }

    public static void SaveGame(FactoryGrid grid)
    {
        GameSaveData data = new GameSaveData
        {
            Credits = Credits,
            QuestIndex = CurrentQuestIndex,
            Inventory = Inventory,
            Unlocks = GlobalUnlocks
        };

        foreach(var card in grid.PlacedCards) {
            data.Cards.Add(new CardSaveData {
                Name = card.Name, X = card.GridX, Y = card.GridY,
                Instructions = new List<string>(card.VM.Instructions)
            });
        }

        string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText("savegame.json", json);
    }
}