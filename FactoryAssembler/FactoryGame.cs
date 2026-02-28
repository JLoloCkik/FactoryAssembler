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
        Camera = new Camera2D() { Zoom = 1.0f, Offset = new Vector2(1920 / 2f, 1080 / 2f) };
        Grid = new FactoryGrid(50, 50);
        Editor = new EditorUI() { GlobalUnlocks = GameState.GlobalUnlocks };
        UI = new UIManager();

        Grid.AddCard(new Card("HUB (Quests)", 25, 25, Color.Gold));
    }

    public void Update()
    {
        int screenW = Raylib.GetScreenWidth();
        int screenH = Raylib.GetScreenHeight();
        Vector2 mouseScreen = Raylib.GetMousePosition();
        Vector2 mouseWorld = Raylib.GetScreenToWorld2D(mouseScreen, Camera);
        
        bool isMouseOnUI = mouseScreen.X > screenW - 250 || Editor.IsVisible || UI.ShowInfoPanel;

        // --- 1. TICK ÉS KÜLDETÉS ELLENŐRZÉS ---
        if (!Editor.IsVisible)
        {
            tickTimer += Raylib.GetFrameTime();
            if (tickTimer >= 1.0f)
            {
                tickTimer = 0;
                foreach (var card in Grid.PlacedCards)
                {
                    for (int i = 0; i < card.CurrentMultiplier; i++) card.VM.Step();
                }

                // Küldetés teljesítésének ellenőrzése
                if (GameState.CurrentQuestIndex < GameState.Quests.Count)
                {
                    var q = GameState.Quests[GameState.CurrentQuestIndex];
                    if (GameState.Inventory[q.TargetItem] >= q.TargetAmount)
                    {
                        GameState.Credits += q.RewardCredits;
                        GameState.CurrentQuestIndex++; // Következő küldetés
                    }
                }
            }
        }

        // --- 2. INTERAKCIÓK ---
        if (Editor.IsVisible) Editor.Update();
        else if (UI.ShowInfoPanel)
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Escape) || Raylib.IsMouseButtonPressed(MouseButton.Left)) UI.ShowInfoPanel = false;
        }
        else
        {
            // Info gomb
            if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mouseScreen, new Rectangle(20, screenH - 60, 120, 40)))
                UI.ShowInfoPanel = true;

            // Törlés
            if (Raylib.IsKeyPressed(KeyboardKey.Delete) || Raylib.IsKeyPressed(KeyboardKey.Backspace))
            {
                (int gx, int gy) = Grid.GetGridPos(mouseWorld.X, mouseWorld.Y);
                Card? toRemove = null;
                foreach (var card in Grid.PlacedCards) if (card.GridX == gx && card.GridY == gy && card.Name != "HUB (Quests)") toRemove = card;
                
                if (toRemove != null)
                {
                    foreach (var bp in GameState.Blueprints) if (bp.Name == toRemove.Name) GameState.Credits += bp.Cost;
                    Grid.PlacedCards.Remove(toRemove);
                }
            }

            // Vásárlás
            if (isMouseOnUI && Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                for (int i = 0; i < GameState.Blueprints.Count; i++)
                {
                    if (Raylib.CheckCollisionPointRec(mouseScreen, new Rectangle(screenW - 240, 100 + (i * 90), 230, 70)))
                    {
                        var bp = GameState.Blueprints[i];
                        if (GameState.Credits >= bp.Cost)
                        {
                            GameState.Credits -= bp.Cost;
                            Card newCard = new Card(bp.Name, 0, 0, bp.Color);
                            newCard.CurrentMultiplier = GameState.GlobalUnlocks.ContainsKey(bp.Name) ? GameState.GlobalUnlocks[bp.Name] : 1;
                            draggedCard = newCard; Grid.AddCard(newCard);
                            dragOffset = new Vector2(Grid.CardWidth / 2, Grid.CardHeight / 2);
                        }
                    }
                }
            }
            // Mozgatás / Klónozás
            else if (!isMouseOnUI && Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                (int gx, int gy) = Grid.GetGridPos(mouseWorld.X, mouseWorld.Y);
                foreach (var card in Grid.PlacedCards)
                {
                    if (card.GridX == gx && card.GridY == gy && card.Name != "HUB (Quests)")
                    {
                        int cost = 0; foreach(var bp in GameState.Blueprints) if(bp.Name==card.Name) cost=bp.Cost;
                        bool ctrl = Raylib.IsKeyDown(KeyboardKey.LeftControl);

                        if (ctrl && GameState.Credits >= cost)
                        {
                            GameState.Credits -= cost;
                            Card clone = new Card(card.Name, gx, gy, card.HeaderColor);
                            clone.VM.LoadCode(string.Join("\n", card.VM.Instructions));
                            clone.CurrentMultiplier = GameState.GlobalUnlocks.ContainsKey(clone.Name) ? GameState.GlobalUnlocks[clone.Name] : 1;
                            Grid.AddCard(clone); draggedCard = clone;
                        }
                        else if (!ctrl) draggedCard = card;

                        if (draggedCard != null) dragOffset = new Vector2(mouseWorld.X - draggedCard.GridX * Grid.CardWidth, mouseWorld.Y - draggedCard.GridY * Grid.CardHeight);
                        break;
                    }
                }
            }

            if (Raylib.IsMouseButtonReleased(MouseButton.Left) && draggedCard != null)
            {
                (int x, int y) = Grid.GetGridPos(mouseWorld.X, mouseWorld.Y);
                draggedCard.GridX = x; draggedCard.GridY = y; draggedCard = null;
            }

            // Editor megnyitás
            if (!isMouseOnUI && draggedCard == null && Raylib.IsMouseButtonPressed(MouseButton.Right))
            {
                (int gx, int gy) = Grid.GetGridPos(mouseWorld.X, mouseWorld.Y);
                foreach (var card in Grid.PlacedCards) if (card.GridX == gx && card.GridY == gy) { Editor.Open(card); break; }
            }

            // Kamera
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
        
        // Pálya rajzolása
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

        // UI rajzolása
        UI.Draw(this, Raylib.GetScreenWidth(), Raylib.GetScreenHeight(), Raylib.GetMousePosition());
        if (Editor.IsVisible) Editor.Draw(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());

        Raylib.EndDrawing();
    }
}