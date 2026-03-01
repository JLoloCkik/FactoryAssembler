using Raylib_cs;
using System.Numerics;

namespace FactoryAssembler;

public class UIManager
{
    public bool ShowInfoPanel = false;

    public void Draw(FactoryGame game, int screenW, int screenH, Vector2 mouseScreen)
    {
        // 1. HOTBAR (Felül)
        int hotbarHeight = 50;
        Raylib.DrawRectangle(0, 0, screenW - 250, hotbarHeight, new Color(10, 10, 10, 200));
        Raylib.DrawLine(0, hotbarHeight, screenW - 250, hotbarHeight, Color.Gray);
        int itemX = 20;
        foreach (var item in GameState.Inventory)
        {
            Raylib.DrawText($"{item.Key}:", itemX, 15, 20, Color.LightGray);
            Raylib.DrawText($"{item.Value}", itemX + Raylib.MeasureText($"{item.Key}: ", 20), 15, 20, Color.White);
            itemX += 170;
        }

        // --- ÚJ HELY: KÜLDETÉSEK (Bal fent, a Hotbar alatt) ---
        // --- KÜLDETÉSEK (Bal fent) ---
        int questPanelY = hotbarHeight + 10; 
        Raylib.DrawRectangle(20, questPanelY, 350, 140, new Color(20, 20, 30, 220));
        Raylib.DrawRectangleLines(20, questPanelY, 350, 140, Color.Gold);
        Raylib.DrawText("CURRENT QUEST", 30, questPanelY + 10, 20, Color.Gold);

        if (GameState.CurrentQuestIndex < GameState.Quests.Count)
        {
            Quest q = GameState.Quests[GameState.CurrentQuestIndex];
            int currentAmount = GameState.Inventory.ContainsKey(q.TargetItem) ? GameState.Inventory[q.TargetItem] : 0;
            Raylib.DrawText(q.Title, 30, questPanelY + 40, 25, Color.White);
            
            // Progress bar helyett, ha kész van: GOMB!
            if (currentAmount >= q.TargetAmount)
            {
                // Zöld villogó gomb
                Color btnColor = (Raylib.GetTime() % 1.0 < 0.5) ? Color.Green : Color.DarkGreen;
                Raylib.DrawRectangle(30, questPanelY + 80, 330, 40, btnColor);
                Raylib.DrawText($"CLAIM REWARD (+${q.RewardCredits})", 60, questPanelY + 90, 20, Color.Black);
                
                // KATTINTÁS ELLENŐRZÉSE ITT (Egyszerűsítés miatt a Draw-ban, bár Logicban szebb lenne)
                if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                {
                    Vector2 m = Raylib.GetMousePosition();
                    if (Raylib.CheckCollisionPointRec(m, new Rectangle(30, questPanelY + 80, 330, 40)))
                    {
                        // JUTALOM BEGYŰJTÉSE!
                        GameState.Credits += q.RewardCredits;
                        // Opcionális: Levonjuk a nyersanyagot? (Factorio-ban nem szokás, de itt lehet)
                        // GameState.Inventory[q.TargetItem] -= q.TargetAmount; 
                        GameState.CurrentQuestIndex++;
                    }
                }
            }
            else
            {
                // Még nincs kész -> Progress bar
                Raylib.DrawText($"Gather {q.TargetAmount} {q.TargetItem}", 30, questPanelY + 75, 20, Color.LightGray);
                float progress = System.Math.Clamp((float)currentAmount / q.TargetAmount, 0f, 1f);
                Raylib.DrawRectangle(30, questPanelY + 105, 330, 15, Color.DarkGray);
                Raylib.DrawRectangle(30, questPanelY + 105, (int)(330 * progress), 15, Color.Orange);
                Raylib.DrawText($"{currentAmount} / {q.TargetAmount}", 160, questPanelY + 105, 15, Color.White);
            }
        }
        else
        {
            Raylib.DrawText("ALL QUESTS COMPLETED!", 30, questPanelY + 50, 20, Color.Green);
            Raylib.DrawText("You are a Factory Master!", 30, questPanelY + 80, 15, Color.White);
        }
        // --- KÜLDETÉS PANEL VÉGE ---


        // INFO GOMB (Legalsó sarok - ez marad)
        Rectangle infoBtn = new Rectangle(20, screenH - 60, 120, 40);
        Raylib.DrawRectangleRec(infoBtn, Color.DarkBlue);
        Raylib.DrawRectangleLinesEx(infoBtn, 2, Color.White);
        Raylib.DrawText("HOW TO PLAY", 30, screenH - 50, 15, Color.White);

        // ÉPÍTÉSI MENÜ (Jobb sáv - ez marad)
        Rectangle uiRect = new Rectangle(screenW - 250, 0, 250, screenH);
        Raylib.DrawRectangleRec(uiRect, new Color(20, 20, 20, 255));
        Raylib.DrawLine(screenW - 250, 0, screenW - 250, screenH, Color.Gray);
        Raylib.DrawText("BUILD MENU", screenW - 230, 20, 25, Color.White);
        Raylib.DrawText($"CREDITS: ${GameState.Credits}", screenW - 230, 55, 20, Color.Gold);

        for (int i = 0; i < GameState.Blueprints.Count; i++)
        {
            var bp = GameState.Blueprints[i];
            int btnX = screenW - 240; int btnY = 100 + (i * 90);
            Rectangle btnRect = new Rectangle(btnX, btnY, 230, 70);
            
            Color btnBgColor = GameState.Credits >= bp.Cost ? Color.DarkGray : new Color(60, 20, 20, 255);
            Raylib.DrawRectangle(btnX, btnY, 230, 70, btnBgColor);
            Raylib.DrawRectangle(btnX, btnY, 10, 70, bp.Color);
            Raylib.DrawText(bp.Name, btnX + 20, btnY + 10, 20, Color.White);
            Raylib.DrawText($"Cost: ${bp.Cost}", btnX + 20, btnY + 40, 15, GameState.Credits >= bp.Cost ? Color.Green : Color.Red);

            // TOOLTIP
            if (Raylib.CheckCollisionPointRec(mouseScreen, btnRect))
            {
                Raylib.DrawRectangleLines(btnX, btnY, 230, 70, Color.White);
                int tipX = btnX - 260;
                Raylib.DrawRectangle(tipX, btnY, 250, 100, new Color(0, 0, 0, 220));
                Raylib.DrawRectangleLines(tipX, btnY, 250, 100, bp.Color);
                Raylib.DrawText("INFO", tipX + 10, btnY + 10, 15, Color.Yellow);
                Raylib.DrawText(bp.Description, tipX + 10, btnY + 35, 15, Color.LightGray);
            }
        }

        // SÚGÓ ABLAK (ez marad)
        if (ShowInfoPanel)
        {
            Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 200));
            Raylib.DrawRectangle(screenW/2 - 300, screenH/2 - 200, 600, 400, new Color(30, 30, 30, 255));
            Raylib.DrawRectangleLines(screenW/2 - 300, screenH/2 - 200, 600, 400, Color.Gold);
            Raylib.DrawText("FACTORY PLANNER - HELP", screenW/2 - 180, screenH/2 - 170, 30, Color.Gold);
            Raylib.DrawText("- Buy machines from the right menu.", screenW/2 - 270, screenH/2 - 100, 20, Color.White);
            Raylib.DrawText("- Right-Click a machine to open the Assembly Editor.", screenW/2 - 270, screenH/2 - 60, 20, Color.White);
            Raylib.DrawText("- DEL or BACKSPACE to delete and refund.", screenW/2 - 270, screenH/2 - 20, 20, Color.White);
            Raylib.DrawText("- Complete Quests (top left) for Credits!", screenW/2 - 270, screenH/2 + 20, 20, Color.Green);
            Raylib.DrawText("Click anywhere to close", screenW/2 - 120, screenH/2 + 150, 20, Color.Gray);
        }
    }
}