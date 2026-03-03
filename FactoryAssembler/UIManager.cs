using Raylib_cs;
using System.Numerics;
using System;
using System.IO;

namespace FactoryAssembler;

public class UIManager
{
    public bool ShowInfoPanel = false;
    private Difficulty selectedDifficulty = Difficulty.Standard; // A menüben választott

    private void DrawInfoPanel(int screenW, int screenH)
    {
        Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 200));
        Raylib.DrawRectangle(screenW/2 - 350, screenH/2 - 250, 700, 500, new Color(30, 30, 30, 255));
        Raylib.DrawRectangleLines(screenW/2 - 350, screenH/2 - 250, 700, 500, Color.Gold);
        Program.DrawText("FACTORY PLANNER - HELP", screenW/2 - 200, screenH/2 - 220, 35, Color.Gold);
        Program.DrawText("- Buy machines from the right menu.", screenW/2 - 320, screenH/2 - 130, 24, Color.White);
        Program.DrawText("- Machines automatically send items to the MARKET.", screenW/2 - 320, screenH/2 - 90, 24, Color.White);
        Program.DrawText("- Right-Click MARKET to sell items for Credits.", screenW/2 - 320, screenH/2 - 50, 24, Color.Green);
        Program.DrawText("- Right-Click a machine to open the Assembly Editor.", screenW/2 - 320, screenH/2 - 10, 24, Color.White);
        Program.DrawText("- SOLVE THE EASY TASK TO START PRODUCTION!", screenW/2 - 320, screenH/2 + 30, 24, Color.Orange);
        Program.DrawText("- DEL or BACKSPACE to delete and refund.", screenW/2 - 320, screenH/2 + 70, 24, Color.White);
        Program.DrawText("Click anywhere to close", screenW/2 - 150, screenH/2 + 180, 22, Color.Gray);
    }

    public void UpdateMainMenu(FactoryGame game)
    {
        Vector2 mouse = Raylib.GetMousePosition();
        int screenW = Raylib.GetScreenWidth(); int screenH = Raylib.GetScreenHeight();
        int menuX = screenW / 2 - 200; int startY = screenH / 2;

        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            // NEHÉZSÉG VÁLASZTÁS
            if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(menuX - 250, startY - 80, 200, 50))) selectedDifficulty = Difficulty.Relaxed;
            if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(menuX, startY - 80, 400, 50))) selectedDifficulty = Difficulty.Standard;
            if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(menuX + 450, startY - 80, 200, 50))) selectedDifficulty = Difficulty.Challenge;

            // MENÜ GOMBOK
            if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(menuX, startY, 400, 60))) {
                game.StartNewGame(selectedDifficulty); // Indítás a választott nehézséggel
            }
            if (File.Exists("savegame.json") && Raylib.CheckCollisionPointRec(mouse, new Rectangle(menuX, startY + 80, 400, 60))) game.LoadGame();
            if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(menuX, startY + 160, 400, 60))) ShowInfoPanel = true;
            if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(menuX, startY + 240, 400, 60))) System.Environment.Exit(0); 
        }
    }

    public void DrawMainMenu()
    {
        int screenW = Raylib.GetScreenWidth(); int screenH = Raylib.GetScreenHeight();
        Vector2 mouse = Raylib.GetMousePosition();
        Program.DrawText("FACTORY ASSEMBLER", screenW / 2 - 320, screenH / 3 - 50, 70, Color.Gold);
        Program.DrawText("Program machines. Automate production. Launch the Rocket.", screenW / 2 - 380, screenH / 3 + 40, 26, Color.LightGray);

        int menuX = screenW / 2 - 200; int startY = screenH / 2;

        // NEHÉZSÉG GOMBOK
        int diffY = startY - 80;
        
        Color cRel = selectedDifficulty == Difficulty.Relaxed ? Color.Green : Color.DarkGray;
        Raylib.DrawRectangle(menuX - 250, diffY, 200, 50, cRel);
        Program.DrawText("RELAXED (0.5x)", menuX - 240, diffY + 12, 22, Color.White);

        Color cStd = selectedDifficulty == Difficulty.Standard ? Color.Blue : Color.DarkGray;
        Raylib.DrawRectangle(menuX, diffY, 400, 50, cStd);
        Program.DrawText("STANDARD (1.0x)", menuX + 110, diffY + 12, 22, Color.White);

        Color cCha = selectedDifficulty == Difficulty.Challenge ? Color.Red : Color.DarkGray;
        Raylib.DrawRectangle(menuX + 450, diffY, 200, 50, cCha);
        Program.DrawText("CHALLENGE (2.5x)", menuX + 460, diffY + 12, 22, Color.White);


        // FŐ GOMBOK
        Color c1 = Raylib.CheckCollisionPointRec(mouse, new Rectangle(menuX, startY, 400, 60)) ? Color.Green : Color.DarkGreen;
        Raylib.DrawRectangle(menuX, startY, 400, 60, c1);
        Program.DrawText("NEW GAME", menuX + 130, startY + 15, 30, Color.White);

        bool hasSave = File.Exists("savegame.json");
        Color c2 = hasSave ? (Raylib.CheckCollisionPointRec(mouse, new Rectangle(menuX, startY + 80, 400, 60)) ? Color.Blue : Color.DarkBlue) : Color.DarkGray;
        Raylib.DrawRectangle(menuX, startY + 80, 400, 60, c2);
        Program.DrawText("CONTINUE", menuX + 130, startY + 95, 30, hasSave ? Color.White : Color.Gray);

        Color cInfo = Raylib.CheckCollisionPointRec(mouse, new Rectangle(menuX, startY + 160, 400, 60)) ? Color.SkyBlue : Color.DarkBlue;
        Raylib.DrawRectangle(menuX, startY + 160, 400, 60, cInfo);
        Program.DrawText("INFO / HELP", menuX + 110, startY + 175, 30, Color.White);

        Color c3 = Raylib.CheckCollisionPointRec(mouse, new Rectangle(menuX, startY + 240, 400, 60)) ? Color.Red : Color.Maroon;
        Raylib.DrawRectangle(menuX, startY + 240, 400, 60, c3);
        Program.DrawText("QUIT", menuX + 160, startY + 255, 30, Color.White);

        Program.DrawText("Game Created by Jankai Lőrinc", screenW / 2 - 180, screenH - 50, 24, Color.Gray);

        if (ShowInfoPanel) DrawInfoPanel(screenW, screenH);
    }

    public void Draw(FactoryGame game, int screenW, int screenH, Vector2 mouseScreen)
    {
        // 1. HOTBAR
        int hotbarHeight = 85; 
        int usableWidth = screenW - 250;
        Raylib.DrawRectangle(0, 0, usableWidth, hotbarHeight, new Color(10, 10, 10, 200));
        Raylib.DrawLine(0, hotbarHeight, usableWidth, hotbarHeight, Color.Gray);
        int columns = 4; int colWidth = usableWidth / columns; 
        int itemIndex = 0;
        foreach (var item in GameState.Inventory) {
            int row = itemIndex / columns; int col = itemIndex % columns;
            int itemX = 20 + (col * colWidth); int itemY = 15 + (row * 35); 
            DrawItemIcon(item.Key, itemX, itemY);
            Program.DrawText($"{item.Key}:", itemX + 25, itemY, 20, Color.LightGray);
            Program.DrawText($"{item.Value}", itemX + 25 + Program.MeasureText($"{item.Key}: ", 20), itemY, 20, Color.White);
            itemIndex++;
        }

        // 2. QUEST
        int questPanelY = hotbarHeight + 15; 
        Raylib.DrawRectangle(20, questPanelY, 350, 140, new Color(20, 20, 30, 220));
        Raylib.DrawRectangleLines(20, questPanelY, 350, 140, Color.Gold);
        Program.DrawText("CURRENT QUEST", 30, questPanelY + 10, 20, Color.Gold);

        if (GameState.CurrentQuestIndex < GameState.Quests.Count) {
            Quest q = GameState.Quests[GameState.CurrentQuestIndex];
            int currentAmount = GameState.Inventory.ContainsKey(q.TargetItem) ? GameState.Inventory[q.TargetItem] : 0;
            Program.DrawText(q.Title, 30, questPanelY + 40, 25, Color.White);
            
            if (currentAmount >= q.TargetAmount) {
                Color btnColor = (Raylib.GetTime() % 1.0 < 0.5) ? Color.Green : Color.DarkGreen;
                Raylib.DrawRectangle(30, questPanelY + 80, 330, 40, btnColor);
                Program.DrawText($"CLAIM REWARD (+${q.RewardCredits})", 45, questPanelY + 90, 20, Color.Black);
                if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mouseScreen, new Rectangle(30, questPanelY + 80, 330, 40))) {
                    GameState.Credits += q.RewardCredits;
                    GameState.CurrentQuestIndex++;
                }
            } else {
                Program.DrawText($"Gather {q.TargetAmount} {q.TargetItem}", 30, questPanelY + 75, 20, Color.LightGray);
                float progress = System.Math.Clamp((float)currentAmount / q.TargetAmount, 0f, 1f);
                Raylib.DrawRectangle(30, questPanelY + 105, 330, 15, Color.DarkGray);
                Raylib.DrawRectangle(30, questPanelY + 105, (int)(330 * progress), 15, Color.Orange);
                Program.DrawText($"{currentAmount} / {q.TargetAmount}", 160, questPanelY + 105, 15, Color.White);
            }
        } else { Program.DrawText("ALL QUESTS DONE!", 30, questPanelY + 50, 20, Color.Green); }

        // 3. BOTTOM BUTTONS
        Rectangle infoBtn = new Rectangle(20, screenH - 60, 150, 40);
        Raylib.DrawRectangleRec(infoBtn, Color.DarkBlue);
        Raylib.DrawRectangleLinesEx(infoBtn, 2, Color.White);
        Program.DrawText("HOW TO PLAY", 30, screenH - 50, 18, Color.White);

        Rectangle saveBtn = new Rectangle(190, screenH - 60, 150, 40);
        Raylib.DrawRectangleRec(saveBtn, Color.DarkGreen);
        Raylib.DrawRectangleLinesEx(saveBtn, 2, Color.White);
        Program.DrawText("SAVE GAME", 215, screenH - 50, 18, Color.White);
        if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mouseScreen, saveBtn)) GameState.SaveGame(game.Grid);

        // 4. BUILD MENU (Cost Multiplierrel!)
        Rectangle uiRect = new Rectangle(screenW - 250, 0, 250, screenH);
        Raylib.DrawRectangleRec(uiRect, new Color(20, 20, 20, 255));
        Raylib.DrawLine(screenW - 250, 0, screenW - 250, screenH, Color.Gray);
        Program.DrawText("BUILD MENU", screenW - 230, 20, 30, Color.White);
        Program.DrawText($"CREDITS: ${GameState.Credits}", screenW - 230, 60, 25, Color.Gold);

        // Nehézség kijelzése
        string diffText = GameState.CurrentDifficulty == Difficulty.Relaxed ? "RELAXED" : (GameState.CurrentDifficulty == Difficulty.Challenge ? "CHALLENGE" : "STANDARD");
        Color diffColor = GameState.CurrentDifficulty == Difficulty.Relaxed ? Color.Green : (GameState.CurrentDifficulty == Difficulty.Challenge ? Color.Red : Color.Blue);
        Program.DrawText(diffText, screenW - 230, 90, 20, diffColor);

        for (int i = 0; i < GameState.Blueprints.Count; i++)
        {
            var bp = GameState.Blueprints[i];
            int actualCost = (int)(bp.Cost * GameState.CostMultiplier); // Kalkulált ár megjelenítése!

            int btnX = screenW - 240; int btnY = 130 + (i * 90);
            Rectangle btnRect = new Rectangle(btnX, btnY, 230, 70);
            Color btnBgColor = GameState.Credits >= actualCost ? Color.DarkGray : new Color(60, 20, 20, 255);
            Raylib.DrawRectangle(btnX, btnY, 230, 70, btnBgColor);
            Raylib.DrawRectangle(btnX, btnY, 10, 70, bp.Color);
            Program.DrawText(bp.Name, btnX + 20, btnY + 10, 22, Color.White);
            Program.DrawText($"Cost: ${actualCost}", btnX + 20, btnY + 40, 18, GameState.Credits >= actualCost ? Color.Green : Color.Red);

            if (Raylib.CheckCollisionPointRec(mouseScreen, btnRect)) {
                Raylib.DrawRectangleLines(btnX, btnY, 230, 70, Color.White);
                int tipX = btnX - 260;
                // ... (Tooltip rajzolás ugyanaz)
                string wrappedDesc = Program.WordWrap(bp.Description, 18, 230);
                int descLines = wrappedDesc.Split('\n').Length;
                int tipH = 40 + (descLines * 22);
                Raylib.DrawRectangle(tipX, btnY, 250, tipH, new Color(0, 0, 0, 220));
                Raylib.DrawRectangleLines(tipX, btnY, 250, tipH, bp.Color);
                Program.DrawText("INFO", tipX + 10, btnY + 10, 18, Color.Yellow);
                int lineY = btnY + 35;
                foreach(var line in wrappedDesc.Split('\n')) { Program.DrawText(line, tipX + 10, lineY, 18, Color.LightGray); lineY += 22; }
            }
        }

        if (ShowInfoPanel) DrawInfoPanel(screenW, screenH);

        if (GameState.Inventory["Rocket"] >= 1) { /* Win Screen (változatlan) */ 
            Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 230));
            int winX = screenW / 2 - 400; int winY = screenH / 2 - 200;
            Raylib.DrawRectangle(winX, winY, 800, 450, new Color(20, 20, 30, 255));
            Raylib.DrawRectangleLinesEx(new Rectangle(winX, winY, 800, 450), 5, Color.Gold);
            Program.DrawText("FACTORY MASTER!", winX + 250, winY + 50, 40, Color.Gold);
            Program.DrawText("Congratulations! You have successfully built a Rocket", winX + 60, winY + 130, 28, Color.White);
            Program.DrawText("using the power of Assembly Programming.", winX + 130, winY + 170, 28, Color.White);
            Program.DrawText($"Final Credits: ${GameState.Credits}", winX + 260, winY + 230, 35, Color.Green);
            Program.DrawText("Game Created by: Jankai Lőrinc", winX + 200, winY + 290, 26, Color.Gray);
            Raylib.DrawRectangle(winX + 250, winY + 340, 300, 60, Color.DarkGreen);
            Program.DrawText("KEEP PLAYING", winX + 310, winY + 355, 28, Color.White);
            if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mouseScreen, new Rectangle(winX + 250, winY + 340, 300, 60))) { GameState.Inventory["Rocket"] = 0; }
        }
    }

    private void DrawItemIcon(string name, int x, int y) { /* (Ikon rajzolás változatlan) */ 
        switch (name) {
            case "Coal": Raylib.DrawRectangle(x, y + 2, 16, 16, new Color(40, 40, 40, 255)); Raylib.DrawRectangleLines(x, y + 2, 16, 16, Color.DarkGray); break;
            case "Iron Ore": Raylib.DrawCircle(x + 8, y + 10, 8, new Color(200, 120, 50, 255)); break;
            case "Copper Ore": Raylib.DrawCircle(x + 8, y + 10, 8, new Color(200, 80, 40, 255)); break;
            case "Iron Ingot": Raylib.DrawRectangle(x, y + 4, 18, 12, Color.LightGray); Raylib.DrawRectangleLines(x, y + 4, 18, 12, Color.Gray); break;
            case "Copper Ingot": Raylib.DrawRectangle(x, y + 4, 18, 12, new Color(210, 100, 50, 255)); Raylib.DrawRectangleLines(x, y + 4, 18, 12, new Color(150, 60, 30, 255)); break;
            case "Wall": Raylib.DrawRectangle(x, y + 2, 16, 16, Color.Gray); Raylib.DrawLine(x, y + 10, x + 16, y + 10, Color.DarkGray); Raylib.DrawLine(x + 8, y + 2, x + 8, y + 18, Color.DarkGray); break;
            case "Gear": Raylib.DrawCircleLines(x + 8, y + 10, 8, Color.LightGray); Raylib.DrawCircle(x + 8, y + 10, 3, Color.LightGray); break;
            case "Rocket": Raylib.DrawTriangle(new Vector2(x + 8, y), new Vector2(x, y + 18), new Vector2(x + 16, y + 18), Color.Red); break;
        }
    }
}