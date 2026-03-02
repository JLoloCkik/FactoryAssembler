using Raylib_cs;
using System.Numerics;

namespace FactoryAssembler;

public class FactoryGame
{
    public Camera2D Camera;
    public FactoryGrid Grid;
    public EditorUI Editor;
    public UIManager UI;

    private Card? draggedCard = null;
    private Vector2 dragOffset = Vector2.Zero;
    private float tickTimer = 0f;

    public FactoryGame()
    {
        // ÓRIÁSI HUB: 2x2-es méretű!
        Card hubCard = new Card("HUB", 25, 25, Color.Gold) { WidthSlots = 2, HeightSlots = 2 };
        Vector2 hubPos = new Vector2(25 * 240 + 240, 25 * 320 + 320); // Közepére fókuszál
        
        Camera = new Camera2D() { Zoom = 1.0f, Target = hubPos, Offset = new Vector2(1920 / 2f, 1080 / 2f) };
        Grid = new FactoryGrid(50, 50);
        Editor = new EditorUI() { GlobalUnlocks = GameState.GlobalUnlocks };
        UI = new UIManager();

        Grid.AddCard(hubCard);
    }

    // ÚJ: Precíz kattintás érzékelő (a több helyet foglaló kártyákhoz)
    private Card? GetCardAtPoint(Vector2 worldPos)
    {
        foreach (var card in Grid.PlacedCards)
        {
            Rectangle rect = new Rectangle(card.GridX * Grid.CardWidth, card.GridY * Grid.CardHeight, 
                                           card.WidthSlots * Grid.CardWidth, card.HeightSlots * Grid.CardHeight);
            if (Raylib.CheckCollisionPointRec(worldPos, rect)) return card;
        }
        return null;
    }

    public void Update()
    {
        int screenW = Raylib.GetScreenWidth(); int screenH = Raylib.GetScreenHeight();
        Vector2 mouseScreen = Raylib.GetMousePosition();
        Vector2 mouseWorld = Raylib.GetScreenToWorld2D(mouseScreen, Camera);
        bool isMouseOnUI = mouseScreen.X > screenW - 250 || Editor.IsVisible || UI.ShowInfoPanel || GameState.Inventory["Rocket"] >= 1;

        // --- TICK: GÉPEK TERMELNEK, HUB BESZÍV MINDENT ---
        if (!Editor.IsVisible && GameState.Inventory["Rocket"] == 0)
        {
            tickTimer += Raylib.GetFrameTime();
            if (tickTimer >= 1.0f)
            {
                tickTimer = 0;

                foreach (var card in Grid.PlacedCards)
                {
                    if (card.Name == "HUB") continue;

                    int unlockedLvl = GameState.GlobalUnlocks.GetValueOrDefault(card.Name, 0);
                    if (unlockedLvl > 0)
                    {
                        while (card.VM.InputBuffer.Count < 5) card.VM.InputBuffer.Enqueue(1); // Fake input
                        for (int i = 0; i < card.CurrentMultiplier; i++) card.VM.Step();
                    }

                    // A HUB végtelen sebességgel kiszív mindent
                    while (card.VM.OutputBuffer.Count > 0)
                    {
                        card.VM.OutputBuffer.Dequeue();
                        
                        if (card.Name == "Coal Miner") GameState.Inventory["Coal"]++;
                        else if (card.Name == "Iron Miner") GameState.Inventory["Iron Ore"]++;
                        else if (card.Name == "Copper Miner") GameState.Inventory["Copper Ore"]++;
                        else if (card.Name == "Smelter") {
                            if (GameState.Inventory["Iron Ore"] > 0 && GameState.Inventory["Coal"] > 0) {
                                GameState.Inventory["Iron Ore"]--; GameState.Inventory["Coal"]--; GameState.Inventory["Iron Ingot"]++;
                            } else if (GameState.Inventory["Copper Ore"] > 0 && GameState.Inventory["Coal"] > 0) {
                                GameState.Inventory["Copper Ore"]--; GameState.Inventory["Coal"]--; GameState.Inventory["Copper Ingot"]++;
                            } else card.HasMissingMaterials = true;
                        }
                        else if (card.Name == "Assembler") {
                            if (GameState.Inventory["Iron Ingot"] >= 2) {
                                GameState.Inventory["Iron Ingot"] -= 2; GameState.Inventory["Gear"]++;
                            } else card.HasMissingMaterials = true;
                        }
                        else if (card.Name == "Rocket Silo") {
                            if (GameState.Inventory["Gear"] >= 10 && GameState.Inventory["Copper Ingot"] >= 10) {
                                GameState.Inventory["Gear"] -= 10; GameState.Inventory["Copper Ingot"] -= 10; GameState.Inventory["Rocket"]++;
                            } else card.HasMissingMaterials = true;
                        }
                    }
                }
            }
        }

        // --- INTERAKCIÓK ---
        if (Editor.IsVisible) Editor.Update();
        else if (UI.ShowInfoPanel) { if (Raylib.IsKeyPressed(KeyboardKey.Escape) || Raylib.IsMouseButtonPressed(MouseButton.Left)) UI.ShowInfoPanel = false; }
        else if (GameState.Inventory["Rocket"] >= 1) { /* Win screen */ }
        else
        {
            if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mouseScreen, new Rectangle(20, screenH - 60, 120, 40)))
                UI.ShowInfoPanel = true;

            Card? hoveredCard = GetCardAtPoint(mouseWorld);

            // Törlés
            if (Raylib.IsKeyPressed(KeyboardKey.Delete) || Raylib.IsKeyPressed(KeyboardKey.Backspace))
            {
                if (hoveredCard != null && hoveredCard.Name != "HUB") {
                    foreach (var bp in GameState.Blueprints) if (bp.Name == hoveredCard.Name) GameState.Credits += bp.Cost;
                    Grid.PlacedCards.Remove(hoveredCard);
                }
            }

            // Vásárlás
            if (isMouseOnUI && Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                for (int i = 0; i < GameState.Blueprints.Count; i++) {
                    if (Raylib.CheckCollisionPointRec(mouseScreen, new Rectangle(screenW - 240, 100 + (i * 90), 230, 70))) {
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
            // Mozgatás / Klónozás
            else if (!isMouseOnUI && Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                if (hoveredCard != null && hoveredCard.Name != "HUB") {
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
                var (x, y) = Grid.GetGridPos(mouseWorld.X, mouseWorld.Y);
                draggedCard.GridX = x; draggedCard.GridY = y; draggedCard = null;
            }

            // Editor megnyitás
            if (!isMouseOnUI && draggedCard == null && Raylib.IsMouseButtonPressed(MouseButton.Right)) {
                if (hoveredCard != null && hoveredCard.Name != "HUB") Editor.Open(hoveredCard);
            }

            if (!isMouseOnUI && draggedCard == null && Raylib.IsMouseButtonDown(MouseButton.Right))
                Camera.Target -= Raylib.GetMouseDelta() / Camera.Zoom;

            float wheel = Raylib.GetMouseWheelMove();
            if (wheel != 0) Camera.Zoom = System.Math.Max(0.1f, Camera.Zoom + wheel * 0.1f);
        }
    }

    public void Draw()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(new Color(30, 30, 30, 255));
        
        Raylib.BeginMode2D(Camera);
        Grid.Draw(draggedCard);
        if (draggedCard != null)
        {
            Vector2 mouseWorld = Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), Camera);
            Rectangle ghost = new Rectangle(mouseWorld.X - dragOffset.X, mouseWorld.Y - dragOffset.Y, Grid.CardWidth - 20, Grid.CardHeight - 20);
            Raylib.DrawRectangleRounded(ghost, 0.1f, 10, new Color((int)draggedCard.HeaderColor.R, (int)draggedCard.HeaderColor.G, (int)draggedCard.HeaderColor.B, 150));
            Raylib.DrawRectangleRoundedLines(ghost, 0.1f, 10, Color.White);
        }
        Raylib.EndMode2D();

        UI.Draw(this, Raylib.GetScreenWidth(), Raylib.GetScreenHeight(), Raylib.GetMousePosition());
        if (Editor.IsVisible) Editor.Draw(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());

        Raylib.EndDrawing();
    }
}