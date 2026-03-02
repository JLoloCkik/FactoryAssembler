using Raylib_cs;
using System.Numerics;
using System.IO;
using System.Text.Json;

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

    public FactoryGame()
    {
        Camera = new Camera2D() { Zoom = 1.0f, Offset = new Vector2(1920 / 2f, 1080 / 2f) };
        Grid = new FactoryGrid(50, 50);
        Editor = new EditorUI() { GlobalUnlocks = GameState.GlobalUnlocks };
        UI = new UIManager();
        Market = new MarketUI();
    }

    public void StartNewGame()
    {
        GameState.ResetGame();
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
            GameState.Credits = data.Credits; GameState.CurrentQuestIndex = data.QuestIndex;
            GameState.Inventory = data.Inventory; GameState.GlobalUnlocks = data.Unlocks;
            Editor.GlobalUnlocks = GameState.GlobalUnlocks; 
            Grid.PlacedCards.Clear();
            foreach(var cData in data.Cards) {
                Color cColor = Color.Gray; int w = 1, h = 1;
                if (cData.Name == "MARKET" || cData.Name == "HUB") { cData.Name = "MARKET"; cColor = Color.Gold; w = 2; h = 2; }
                else { foreach(var bp in GameState.Blueprints) if(bp.Name == cData.Name) cColor = bp.Color; }
                Card card = new Card(cData.Name, cData.X, cData.Y, cColor) { WidthSlots = w, HeightSlots = h };
                card.CurrentMultiplier = GameState.GlobalUnlocks.GetValueOrDefault(card.Name, 1);
                card.VM.LoadCode(string.Join("\n", cData.Instructions));
                Grid.AddCard(card);
            }
            Camera.Target = new Vector2(25 * 240 + 240, 25 * 320 + 320); 
            CurrentMode = GameMode.Playing;
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
        if (CurrentMode == GameMode.MainMenu) { UI.UpdateMainMenu(this); return; }

        int screenW = Raylib.GetScreenWidth(); int screenH = Raylib.GetScreenHeight();
        Vector2 mouseScreen = Raylib.GetMousePosition(); Vector2 mouseWorld = Raylib.GetScreenToWorld2D(mouseScreen, Camera);
        
        bool isMouseOnUI = mouseScreen.X > screenW - 250 || Editor.IsVisible || Market.IsVisible || UI.ShowInfoPanel || GameState.Inventory["Rocket"] >= 1;

        if (!Editor.IsVisible && !Market.IsVisible && GameState.Inventory["Rocket"] == 0)
        {
            tickTimer += Raylib.GetFrameTime();
            if (tickTimer >= 1.0f)
            {
                tickTimer = 0;
                foreach (var card in Grid.PlacedCards) {
                    if (card.Name == "MARKET") continue;
                    int unlockedLvl = GameState.GlobalUnlocks.GetValueOrDefault(card.Name, 0);
                    if (unlockedLvl > 0) {
                        while (card.VM.InputBuffer.Count < 5) card.VM.InputBuffer.Enqueue(1); 
                        for (int i = 0; i < card.CurrentMultiplier; i++) card.VM.Step();
                    }

                    while (card.VM.OutputBuffer.Count > 0)
                    {
                        card.VM.OutputBuffer.Dequeue(); 
                        if (card.Name == "Coal Miner") GameState.Inventory["Coal"]++;
                        else if (card.Name == "Iron Miner") GameState.Inventory["Iron Ore"]++;
                        else if (card.Name == "Copper Miner") GameState.Inventory["Copper Ore"]++;
                        else if (card.Name == "Smelter") {
                            if (GameState.Inventory["Iron Ore"] > 0 && GameState.Inventory["Coal"] > 0) { GameState.Inventory["Iron Ore"]--; GameState.Inventory["Coal"]--; GameState.Inventory["Iron Ingot"]++; } 
                            else if (GameState.Inventory["Copper Ore"] > 0 && GameState.Inventory["Coal"] > 0) { GameState.Inventory["Copper Ore"]--; GameState.Inventory["Coal"]--; GameState.Inventory["Copper Ingot"]++; } 
                            else card.HasMissingMaterials = true;
                        }
                        else if (card.Name == "Assembler") {
                            if (GameState.Inventory["Iron Ingot"] >= 2) { GameState.Inventory["Iron Ingot"] -= 2; GameState.Inventory["Gear"]++; } 
                            else card.HasMissingMaterials = true;
                        }
                        else if (card.Name == "Rocket Silo") {
                            if (GameState.Inventory["Gear"] >= 10 && GameState.Inventory["Copper Ingot"] >= 10) { GameState.Inventory["Gear"] -= 10; GameState.Inventory["Copper Ingot"] -= 10; GameState.Inventory["Rocket"]++; } 
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

            if (Raylib.IsKeyPressed(KeyboardKey.Delete) || Raylib.IsKeyPressed(KeyboardKey.Backspace)) {
                if (hoveredCard != null && hoveredCard.Name != "MARKET") {
                    foreach (var bp in GameState.Blueprints) if (bp.Name == hoveredCard.Name) GameState.Credits += bp.Cost;
                    Grid.PlacedCards.Remove(hoveredCard);
                }
            }

            if (isMouseOnUI && Raylib.IsMouseButtonPressed(MouseButton.Left)) {
                for (int i = 0; i < GameState.Blueprints.Count; i++) {
                    if (Raylib.CheckCollisionPointRec(mouseScreen, new Rectangle(screenW - 240, 110 + (i * 90), 230, 70))) {
                        var bp = GameState.Blueprints[i];
                        if (GameState.Credits >= bp.Cost) {
                            GameState.Credits -= bp.Cost;
                            Card newCard = new Card(bp.Name, 0, 0, bp.Color);
                            newCard.CurrentMultiplier = GameState.GlobalUnlocks.GetValueOrDefault(bp.Name, 1);
                            draggedCard = newCard; Grid.AddCard(newCard);
                            dragOffset = new Vector2(Grid.CardWidth / 2, Grid.CardHeight / 2);
                        }
                    }
                }
            }
            else if (!isMouseOnUI && Raylib.IsMouseButtonPressed(MouseButton.Left)) {
                if (hoveredCard != null && hoveredCard.Name != "MARKET") {
                    int cost = 0; foreach(var bp in GameState.Blueprints) if(bp.Name==hoveredCard.Name) cost=bp.Cost;
                    bool ctrl = Raylib.IsKeyDown(KeyboardKey.LeftControl);

                    if (ctrl && GameState.Credits >= cost) {
                        GameState.Credits -= cost;
                        Card clone = new Card(hoveredCard.Name, hoveredCard.GridX, hoveredCard.GridY, hoveredCard.HeaderColor);
                        clone.VM.LoadCode(string.Join("\n", hoveredCard.VM.Instructions));
                        clone.CurrentMultiplier = GameState.GlobalUnlocks.GetValueOrDefault(clone.Name, 1);
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