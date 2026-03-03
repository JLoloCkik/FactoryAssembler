using Raylib_cs;
using System.Numerics;

namespace FactoryAssembler;

public class MarketUI
{
    public bool IsVisible { get; private set; } = false;

    public void Open() { IsVisible = true; }
    public void Close() { IsVisible = false; }

    public void Update()
    {
        if (!IsVisible) return;

        if (Raylib.IsKeyPressed(KeyboardKey.Escape)) Close();

        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            Vector2 mouse = Raylib.GetMousePosition();
            int screenW = Raylib.GetScreenWidth(); int screenH = Raylib.GetScreenHeight();

            int winW = 800; int winH = 700;
            int winX = screenW / 2 - winW / 2; int winY = screenH / 2 - winH / 2;

            if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(winX + winW - 50, winY + 10, 40, 40))) Close();

            // ITT VOLT A HIBA: Az Update-ben 100 volt, a Draw-ban 160. Most már mindkettő 160!
            int itemY = winY + 160; 
            foreach (var item in GameState.Inventory)
            {
                if (item.Key == "Rocket") continue; 

                int amount = item.Value; int price = GameState.Prices[item.Key];
                Rectangle sellBtn = new Rectangle(winX + winW - 180, itemY, 150, 40);

                if (amount > 0 && Raylib.CheckCollisionPointRec(mouse, sellBtn)) {
                    GameState.Credits += amount * price;
                    GameState.Inventory[item.Key] = 0; 
                }
                itemY += 60;
            }
        }
    }

    public void Draw()
    {
        if (!IsVisible) return;
        int screenW = Raylib.GetScreenWidth(); int screenH = Raylib.GetScreenHeight();

        Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 200));

        int winW = 800; int winH = 700;
        int winX = screenW / 2 - winW / 2; int winY = screenH / 2 - winH / 2;

        Raylib.DrawRectangle(winX, winY, winW, winH, new Color(30, 30, 40, 255));
        Raylib.DrawRectangleLinesEx(new Rectangle(winX, winY, winW, winH), 4, Color.Gold);

        Program.DrawText("GLOBAL MARKET", winX + 270, winY + 20, 35, Color.Gold);
        Program.DrawText("Sell your automated production for Credits!", winX + 160, winY + 60, 22, Color.LightGray);
        
        Raylib.DrawRectangle(winX + winW - 50, winY + 10, 40, 40, Color.Maroon);
        Program.DrawText("X", winX + winW - 38, winY + 18, 24, Color.White);

        Program.DrawText("ITEM", winX + 50, winY + 110, 20, Color.Gray);
        Program.DrawText("STORED", winX + 250, winY + 110, 20, Color.Gray);
        Program.DrawText("PRICE/UNIT", winX + 400, winY + 110, 20, Color.Gray);
        Program.DrawText("ACTION", winX + 660, winY + 110, 20, Color.Gray);
        Raylib.DrawLine(winX + 40, winY + 140, winX + winW - 40, winY + 140, Color.DarkGray);

        int itemY = winY + 160;
        foreach (var item in GameState.Inventory)
        {
            if (item.Key == "Rocket") continue; 
            int amount = item.Value; int price = GameState.Prices[item.Key];

            Program.DrawText(item.Key, winX + 50, itemY + 10, 24, Color.White);
            Program.DrawText($"{amount} pcs", winX + 250, itemY + 10, 24, amount > 0 ? Color.Lime : Color.Gray);
            Program.DrawText($"${price}", winX + 400, itemY + 10, 24, Color.Gold);

            Rectangle sellBtn = new Rectangle(winX + winW - 180, itemY, 150, 40);
            if (amount > 0) {
                Raylib.DrawRectangleRec(sellBtn, Color.DarkGreen);
                Raylib.DrawRectangleLinesEx(sellBtn, 2, Color.Lime);
                Program.DrawText($"SELL ALL (${amount * price})", (int)sellBtn.X + 15, (int)sellBtn.Y + 12, 18, Color.White);
            } else {
                Raylib.DrawRectangleRec(sellBtn, Color.DarkGray);
                Program.DrawText("NO STOCK", (int)sellBtn.X + 35, (int)sellBtn.Y + 12, 18, Color.Gray);
            }
            itemY += 60;
        }
    }
}