using Raylib_cs;
using System.Numerics;
using System.IO;
using System.Text.Json;
using System; 

namespace FactoryAssembler;

public enum GameMode { MainMenu, Playing }

public class FactoryGame
{
    public GameMode CurrentMode = GameMode.MainMenu;
    public Camera2D Camera;
    public FactoryGrid Grid;
    public EditorUI Editor;
    public UIManager UI;
    public MarketUI Market; 

    private Card? draggedCard = null;
    private Vector2 dragOffset = Vector2.Zero;
    private float tickTimer = 0f;
    private Random rng = new Random(); 

    public FactoryGame()
    {
        Camera = new Camera2D() { Zoom = 1.0f, Offset = new Vector2(1920 / 2f, 1080 / 2f) };
        Grid = new FactoryGrid(50, 50);
        Editor = new EditorUI() { GlobalUnlocks = GameState.GlobalUnlocks };
        UI = new UIManager();
        Market = new MarketUI();
    }

    // ÚJ: A StartNewGame most már paramétert vár!
    public void StartNewGame(Difficulty diff)
    {
        GameState.ResetGame(diff);
        Grid.PlacedCards.Clear();
        Card marketCard = new Card("MARKET", 25, 25, Color.Gold) { WidthSlots = 2, HeightSlots = 2 };
        Grid.AddCard(marketCard);
        Camera.Target = new Vector2(25 * 240 + 240, 25 * 320 + 320); 
        CurrentMode = GameMode.Playing;
    }

    public void LoadGame()
    {
        if (!File.Exists("savegame.json")) return;
        string json = File.ReadAllText("savegame.json");
        GameSaveData data = JsonSerializer.Deserialize<GameSaveData>(json);
        
        if (data != null) {
            GameState.CurrentDifficulty = (Difficulty)data.Difficulty; // Nehézség betöltése
            GameState.Credits = data.Credits; GameState.CurrentQuestIndex = data.QuestIndex;
            GameState.Inventory = data.Inventory; GameState.GlobalUnlocks = data.Unlocks;
            Editor.GlobalUnlocks = GameState.GlobalUnlocks; 
            Grid.PlacedCards.Clear();
            foreach(var cData in data.Cards) {
                Color cColor = Color.Gray; int w = 1, h = 1;
                if (cData.Name == "MARKET" || cData.Name == "HUB") { cData.Name = "MARKET"; cColor = Color.Gold; w = 2; h = 2; }
                else { foreach(var bp in GameState.Blueprints) if(bp.Name == cData.Name) cColor = bp.Color; }
                Card card = new Card(cData.Name, cData.X, cData.Y, cColor) { WidthSlots = w, HeightSlots = h };
                
                int unlocked = GameState.GlobalUnlocks.ContainsKey(card.Name) ? GameState.GlobalUnlocks[card.Name] : 0;
                card.CurrentMultiplier = (unlocked == 0) ? 1 : unlocked;
                card.VM.LoadCode(string.Join("\n", cData.Instructions));
                
                Grid.AddCard(card);
            }
            Camera.Target = new Vector2(25 * 240 + 240, 25 * 320 + 320); 
            CurrentMode = GameMode.Playing;
        }
    }

    private void InitMachineCodeIfUnlocked(Card card)
    {
        int unlockedLvl = GameState.GlobalUnlocks.GetValueOrDefault(card.Name, 0);
        card.CurrentMultiplier = (unlockedLvl == 0) ? 1 : unlockedLvl;

        if (card.VM.Instructions.Count == 0 && unlockedLvl > 0)
        {
            string autoCode = "";
            if (card.Name.Contains("Miner")) autoCode = "START:\nMOV AX, 1\nOUT AX\nJMP START";
            else if (card.Name != "MARKET") autoCode = "START:\nIN AX\nOUT AX\nJMP START";
            
            if (autoCode != "") card.VM.LoadCode(autoCode);
        }
    }

    private Card? GetCardAtPoint(Vector2 worldPos)
    {
        foreach (var card in Grid.PlacedCards) {
            Rectangle rect = new Rectangle(card.GridX * Grid.CardWidth, card.GridY * Grid.CardHeight, card.WidthSlots * Grid.CardWidth, card.HeightSlots * Grid.CardHeight);
            if (Raylib.CheckCollisionPointRec(worldPos, rect)) return card;
        }
        return null;
    }

    public void Update()
    {
        if (CurrentMode == GameMode.MainMenu) { 
            if (UI.ShowInfoPanel) {
                if (Raylib.IsKeyPressed(KeyboardKey.Escape) || Raylib.IsMouseButtonPressed(MouseButton.Left)) UI.ShowInfoPanel = false;
            } else {
                UI.UpdateMainMenu(this); 
            }
            return; 
        }

        int screenW = Raylib.GetScreenWidth(); int screenH = Raylib.GetScreenHeight();
        Vector2 mouseScreen = Raylib.GetMousePosition(); Vector2 mouseWorld = Raylib.GetScreenToWorld2D(mouseScreen, Camera);
        
        bool isMouseOnUI = mouseScreen.X > screenW - 250 || Editor.IsVisible || Market.IsVisible || UI.ShowInfoPanel || GameState.Inventory["Rocket"] >= 1;

        if (!Editor.IsVisible && !Market.IsVisible && GameState.Inventory["Rocket"] == 0)
        {
            tickTimer += Raylib.GetFrameTime();
            if (tickTimer >= 0.2f)
            {
                tickTimer = 0;
                foreach (var card in Grid.PlacedCards) {
                    if (card.Name == "MARKET") continue;
                    
                    int unlockedLvl = GameState.GlobalUnlocks.GetValueOrDefault(card.Name, 0);
                    card.CurrentMultiplier = (unlockedLvl == 0) ? 1 : unlockedLvl;

                    if (unlockedLvl > 0) {
                        if (card.VM.IsHalted) card.VM.Reset();
                        if (card.VM.Instructions.Count == 0) InitMachineCodeIfUnlocked(card);

                        while (card.VM.InputBuffer.Count < 50) card.VM.InputBuffer.Enqueue(rng.Next(1, 100)); 
                        
                        for (int i = 0; i < card.CurrentMultiplier; i++) card.VM.Step();
                    }

                    while (card.VM.OutputBuffer.Count > 0)
                    {
                        card.VM.OutputBuffer.Dequeue(); 
                        
                        if (card.Name == "Coal Miner") GameState.Inventory["Coal"]++;
                        else if (card.Name == "Iron Miner") GameState.Inventory["Iron Ore"]++;
                        else if (card.Name == "Copper Miner") GameState.Inventory["Copper Ore"]++;
                        else if (card.Name == "Smelter") {
                            int ironOre = GameState.Inventory["Iron Ore"];
                            int copperOre = GameState.Inventory["Copper Ore"];
                            int coal = GameState.Inventory["Coal"];
                            bool crafted = false;

                            if (copperOre > ironOre && copperOre > 0 && coal > 0) {
                                GameState.Inventory["Copper Ore"]--; GameState.Inventory["Coal"]--; GameState.Inventory["Copper Ingot"]++;
                                crafted = true;
                            }
                            else if (ironOre > 0 && coal > 0) {
                                GameState.Inventory["Iron Ore"]--; GameState.Inventory["Coal"]--; GameState.Inventory["Iron Ingot"]++;
                                crafted = true;
                            }
                            else if (copperOre > 0 && coal > 0) {
                                GameState.Inventory["Copper Ore"]--; GameState.Inventory["Coal"]--; GameState.Inventory["Copper Ingot"]++;
                                crafted = true;
                            }

                            if (!crafted) card.HasMissingMaterials = true;
                            else card.HasMissingMaterials = false;
                        }
                        else if (card.Name == "Assembler") {
                            if (GameState.Inventory["Iron Ingot"] >= 2) { GameState.Inventory["Iron Ingot"] -= 2; GameState.Inventory["Gear"]++; card.HasMissingMaterials = false; } 
                            else card.HasMissingMaterials = true;
                        }
                        else if (card.Name == "Rocket Silo") {
                            if (GameState.Inventory["Gear"] >= 10 && GameState.Inventory["Copper Ingot"] >= 10) { 
                                GameState.Inventory["Gear"] -= 10; GameState.Inventory["Copper Ingot"] -= 10; GameState.Inventory["Rocket"]++; 
                                card.HasMissingMaterials = false;
                            } 
                            else card.HasMissingMaterials = true;
                        }
                    }
                }
            }
        }

        if (Editor.IsVisible) Editor.Update();
        else if (Market.IsVisible) Market.Update();
        else if (UI.ShowInfoPanel) { if (Raylib.IsKeyPressed(KeyboardKey.Escape) || Raylib.IsMouseButtonPressed(MouseButton.Left)) UI.ShowInfoPanel = false; }
        else if (GameState.Inventory["Rocket"] >= 1) { /* Win screen */ }
        else
        {
            if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mouseScreen, new Rectangle(20, screenH - 60, 120, 40))) UI.ShowInfoPanel = true;

            Card? hoveredCard = GetCardAtPoint(mouseWorld);

            // TÖRLÉS - VISSZATÉRÍTÉS A NEHÉZSÉG ALAPJÁN
            if (Raylib.IsKeyPressed(KeyboardKey.Delete) || Raylib.IsKeyPressed(KeyboardKey.Backspace)) {
                if (hoveredCard != null && hoveredCard.Name != "MARKET") {
                    foreach (var bp in GameState.Blueprints) if (bp.Name == hoveredCard.Name) {
                        int cost = (int)(bp.Cost * GameState.CostMultiplier); // Kalkulált ár visszatérítése
                        GameState.Credits += cost;
                    }
                    Grid.PlacedCards.Remove(hoveredCard);
                }
            }

            // VÁSÁRLÁS - NEHÉZSÉG ALAPJÁN
            if (isMouseOnUI && Raylib.IsMouseButtonPressed(MouseButton.Left)) {
                for (int i = 0; i < GameState.Blueprints.Count; i++) {
                    if (Raylib.CheckCollisionPointRec(mouseScreen, new Rectangle(screenW - 240, 110 + (i * 90), 230, 70))) {
                        var bp = GameState.Blueprints[i];
                        int actualCost = (int)(bp.Cost * GameState.CostMultiplier); // <--- ITT A LÉNYEG!

                        if (GameState.Credits >= actualCost) {
                            GameState.Credits -= actualCost;
                            Card newCard = new Card(bp.Name, 0, 0, bp.Color);
                            InitMachineCodeIfUnlocked(newCard);
                            draggedCard = newCard; Grid.AddCard(newCard);
                            dragOffset = new Vector2(Grid.CardWidth / 2, Grid.CardHeight / 2);
                        }
                    }
                }
            }
            // MOZGATÁS / KLÓNOZÁS
            else if (!isMouseOnUI && Raylib.IsMouseButtonPressed(MouseButton.Left)) {
                if (hoveredCard != null && hoveredCard.Name != "MARKET") {
                    int baseCost = 0; foreach(var bp in GameState.Blueprints) if(bp.Name==hoveredCard.Name) baseCost=bp.Cost;
                    int actualCost = (int)(baseCost * GameState.CostMultiplier); // <--- KLÓNOZÁSNÁL IS!

                    bool ctrl = Raylib.IsKeyDown(KeyboardKey.LeftControl);

                    if (ctrl && GameState.Credits >= actualCost) {
                        GameState.Credits -= actualCost;
                        Card clone = new Card(hoveredCard.Name, hoveredCard.GridX, hoveredCard.GridY, hoveredCard.HeaderColor);
                        clone.VM.LoadCode(string.Join("\n", hoveredCard.VM.Instructions));
                        int unlocked = GameState.GlobalUnlocks.ContainsKey(clone.Name) ? GameState.GlobalUnlocks[clone.Name] : 0;
                        clone.CurrentMultiplier = (unlocked == 0) ? 1 : unlocked;
                        Grid.AddCard(clone); draggedCard = clone;
                    }
                    else if (!ctrl) draggedCard = hoveredCard;

                    if (draggedCard != null) dragOffset = new Vector2(mouseWorld.X - draggedCard.GridX * Grid.CardWidth, mouseWorld.Y - draggedCard.GridY * Grid.CardHeight);
                }
            }

            if (Raylib.IsMouseButtonReleased(MouseButton.Left) && draggedCard != null) {
                var (x, y) = Grid.GetGridPos(mouseWorld.X, mouseWorld.Y); draggedCard.GridX = x; draggedCard.GridY = y; draggedCard = null;
            }

            if (!isMouseOnUI && draggedCard == null && Raylib.IsMouseButtonPressed(MouseButton.Right)) {
                if (hoveredCard != null) {
                    if (hoveredCard.Name == "MARKET") Market.Open(); 
                    else Editor.Open(hoveredCard); 
                }
            }

            if (!isMouseOnUI && draggedCard == null && Raylib.IsMouseButtonDown(MouseButton.Right)) Camera.Target -= Raylib.GetMouseDelta() / Camera.Zoom;
            float wheel = Raylib.GetMouseWheelMove(); if (wheel != 0) Camera.Zoom = System.Math.Max(0.1f, Camera.Zoom + wheel * 0.1f);
        }
    }

    public void Draw()
    {
        Raylib.BeginDrawing(); Raylib.ClearBackground(new Color(30, 30, 30, 255));
        
        if (CurrentMode == GameMode.MainMenu) { UI.DrawMainMenu(); }
        else {
            Raylib.BeginMode2D(Camera);
            Grid.Draw(draggedCard);
            if (draggedCard != null) {
                Vector2 mouseWorld = Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), Camera);
                Rectangle ghost = new Rectangle(mouseWorld.X - dragOffset.X, mouseWorld.Y - dragOffset.Y, Grid.CardWidth - 20, Grid.CardHeight - 20);
                Raylib.DrawRectangleRounded(ghost, 0.05f, 10, new Color((int)draggedCard.HeaderColor.R, (int)draggedCard.HeaderColor.G, (int)draggedCard.HeaderColor.B, 150));
                Raylib.DrawRectangleRoundedLines(ghost, 0.05f, 10, Color.White);
            }
            Raylib.EndMode2D();

            UI.Draw(this, Raylib.GetScreenWidth(), Raylib.GetScreenHeight(), Raylib.GetMousePosition());
            if (Editor.IsVisible) Editor.Draw(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
            if (Market.IsVisible) Market.Draw(); 
        }
        Raylib.EndDrawing();
    }
}