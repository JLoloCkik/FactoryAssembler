using Raylib_cs;
using System.Numerics;
using System.Collections.Generic;

namespace FactoryAssembler;

public class EditorUI
{
    public bool IsVisible { get; private set; } = false;
    private Card? CurrentCard;
    private string CodeBuffer = "";
    private float cursorTimer = 0;
    private bool cursorVisible = true;
    
    // Hivatkozás a globális feloldásokra (Program.cs-ből kapjuk)
    public Dictionary<string, int> GlobalUnlocks { get; set; }

    public void Open(Card card)
    {
        CurrentCard = card;
        CodeBuffer = string.Join("\n", card.VM.Instructions);
        IsVisible = true;
    }

    public void Close()
    {
        if (CurrentCard != null) CurrentCard.VM.LoadCode(CodeBuffer);
        IsVisible = false;
        CurrentCard = null;
    }

    public void Update()
    {
        if (!IsVisible || CurrentCard == null) return;

        // 1. BILLENTYŰZET (Csak gépelés a bal oldalra)
        int key = Raylib.GetCharPressed();
        while (key > 0)
        {
            if ((key >= 32) && (key <= 125)) CodeBuffer += (char)key;
            key = Raylib.GetCharPressed();
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && CodeBuffer.Length > 0) CodeBuffer = CodeBuffer.Substring(0, CodeBuffer.Length - 1);
        if (Raylib.IsKeyPressed(KeyboardKey.Enter)) CodeBuffer += "\n";
        if (Raylib.IsKeyPressed(KeyboardKey.Escape)) Close();

        // 2. EGÉR KATTINTÁSOK A JOBB OLDALI GOMBOKRA
        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            Vector2 mouse = Raylib.GetMousePosition();
            int screenWidth = Raylib.GetScreenWidth();
            int screenHeight = Raylib.GetScreenHeight();
            int panelX = screenWidth / 2; // Jobb oldal kezdete
            
            int currentMaxUnlocked = GlobalUnlocks.ContainsKey(CurrentCard.Name) ? GlobalUnlocks[CurrentCard.Name] : 0;

            // Gombok pozíciói (ugyanaz, mint a Draw-ban)
            Rectangle btnEasy   = new Rectangle(panelX + 50, 150, 300, 60);
            Rectangle btnNormal = new Rectangle(panelX + 50, 300, 300, 60);
            Rectangle btnHard   = new Rectangle(panelX + 50, 450, 300, 60);

            // EASY GOMB KATTINTÁS
            if (Raylib.CheckCollisionPointRec(mouse, btnEasy))
            {
                if (currentMaxUnlocked < 1) GlobalUnlocks[CurrentCard.Name] = 1; // Feloldás
                CurrentCard.CurrentMultiplier = 1; // Kiválasztás
            }
            // NORMAL GOMB KATTINTÁS
            else if (Raylib.CheckCollisionPointRec(mouse, btnNormal))
            {
                if (currentMaxUnlocked >= 1) // Csak ha az előző megvan
                {
                    if (currentMaxUnlocked < 2) GlobalUnlocks[CurrentCard.Name] = 2;
                    CurrentCard.CurrentMultiplier = 2;
                }
            }
            // HARD GOMB KATTINTÁS
            else if (Raylib.CheckCollisionPointRec(mouse, btnHard))
            {
                if (currentMaxUnlocked >= 2) 
                {
                    if (currentMaxUnlocked < 4) GlobalUnlocks[CurrentCard.Name] = 4;
                    CurrentCard.CurrentMultiplier = 4;
                }
            }
        }
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsVisible || CurrentCard == null) return;

        Raylib.DrawRectangle(0, 0, screenWidth, screenHeight, new Color(0, 0, 0, 220));

        int margin = 50;
        int editorX = margin;
        int editorY = margin;
        int editorW = (screenWidth / 2) - margin; // Csak a képernyő bal fele
        int editorH = screenHeight - (margin * 2);

        // --- BAL OLDAL: ASSEMBLY EDITOR ---
        Raylib.DrawRectangle(editorX, editorY, editorW, editorH, new Color(20, 20, 20, 255));
        Raylib.DrawRectangleLines(editorX, editorY, editorW, editorH, Color.White);
        Raylib.DrawText($"ASSEMBLY: {CurrentCard.Name}", editorX + 20, editorY + 20, 40, CurrentCard.HeaderColor);
        
        string[] lines = CodeBuffer.Split('\n');
        int lineY = editorY + 100;
        for (int i = 0; i < lines.Length; i++)
        {
            Raylib.DrawText($"{i+1}.", editorX + 20, lineY, 40, Color.Gray);
            Raylib.DrawText(lines[i], editorX + 100, lineY, 40, Color.White);
            lineY += 45;
        }

        cursorTimer += Raylib.GetFrameTime();
        if (cursorTimer >= 0.5f) { cursorTimer = 0; cursorVisible = !cursorVisible; }
        if (cursorVisible) Raylib.DrawRectangle(editorX + 100 + (lines[lines.Length-1].Length * 24), lineY - 45, 20, 40, Color.Green);

        // --- JOBB OLDAL: FELADATOK ÉS FELOLDÁSOK ---
        int panelX = screenWidth / 2;
        int currentMaxUnlocked = GlobalUnlocks.ContainsKey(CurrentCard.Name) ? GlobalUnlocks[CurrentCard.Name] : 0;

        Raylib.DrawText("TASKS & PERFORMANCE", panelX + 50, editorY + 20, 40, Color.White);
        Raylib.DrawText("Solve tasks to permanently unlock higher multipliers for all machines of this type.", panelX + 50, editorY + 70, 20, Color.Gray);

        // Segédfüggvény a szintek rajzolásához
        DrawLevelButton(panelX + 50, 150, "EASY (x1 Multiplier)", "Basic logic. Always available.", 1, currentMaxUnlocked, CurrentCard.CurrentMultiplier == 1);
        DrawLevelButton(panelX + 50, 300, "NORMAL (x2 Multiplier)", "Requires solving Easy first.", 2, currentMaxUnlocked, CurrentCard.CurrentMultiplier == 2);
        DrawLevelButton(panelX + 50, 450, "HARD (x4 Multiplier)", "Requires solving Normal first.", 4, currentMaxUnlocked, CurrentCard.CurrentMultiplier == 4);
    }

    private void DrawLevelButton(int x, int y, string title, string desc, int reqLevel, int unlockedLevel, bool isSelected)
    {
        bool isUnlocked = unlockedLevel >= (reqLevel == 4 ? 2 : reqLevel == 2 ? 1 : 0);
        Color boxColor = isSelected ? Color.Green : (isUnlocked ? Color.DarkGray : new Color(50, 20, 20, 255));

        Raylib.DrawRectangle(x, y, 600, 100, boxColor);
        Raylib.DrawRectangleLines(x, y, 600, 100, Color.White);

        Raylib.DrawText(title, x + 20, y + 15, 30, Color.White);
        Raylib.DrawText(desc, x + 20, y + 60, 20, Color.LightGray);

        // Gomb rajzolása belül
        Rectangle btnRec = new Rectangle(x, y, 300, 60); // Láthatatlan kattintási zóna (Update-ben kezelve)
        
        if (isSelected)
            Raylib.DrawText("ACTIVE", x + 450, y + 35, 30, Color.Black);
        else if (isUnlocked)
            Raylib.DrawText("CLICK TO SELECT", x + 400, y + 35, 20, Color.Lime);
        else if (reqLevel == 2 && unlockedLevel == 0)
            Raylib.DrawText("LOCKED (Solve Easy)", x + 380, y + 35, 20, Color.Red);
        else if (reqLevel == 4 && unlockedLevel < 2)
            Raylib.DrawText("LOCKED (Solve Normal)", x + 350, y + 35, 20, Color.Red);
        else
            Raylib.DrawText("CLICK TO UNLOCK", x + 400, y + 35, 20, Color.Yellow);
    }
}