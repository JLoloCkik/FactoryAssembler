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
    
    public Dictionary<string, int> GlobalUnlocks { get; set; }

    // ÚJ: Melyik feladatot nézzük éppen az Editorban? (1, 2, 4)
    private int SelectedTaskLevel = 1;
    private string TestResultMsg = "";
    private Color TestResultColor = Color.Gray;

    public void Open(Card card)
    {
        CurrentCard = card;
        CodeBuffer = string.Join("\n", card.VM.Instructions);
        SelectedTaskLevel = 1; // Alapból az Easy-t nyitjuk meg
        TestResultMsg = "";
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

        // --- 1. GÉPELÉS ---
        int key = Raylib.GetCharPressed();
        while (key > 0)
        {
            if ((key >= 32) && (key <= 125)) CodeBuffer += (char)key;
            key = Raylib.GetCharPressed();
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && CodeBuffer.Length > 0) CodeBuffer = CodeBuffer.Substring(0, CodeBuffer.Length - 1);
        if (Raylib.IsKeyPressed(KeyboardKey.Enter)) CodeBuffer += "\n";
        if (Raylib.IsKeyPressed(KeyboardKey.Escape)) Close();

        // Minden kódváltozásnál levesszük a teszt eredményét
        if (Raylib.IsKeyPressed((KeyboardKey)key) || Raylib.IsKeyPressed(KeyboardKey.Backspace) || Raylib.IsKeyPressed(KeyboardKey.Enter))
        {
            TestResultMsg = ""; 
        }

        // --- 2. EGÉR KATTINTÁSOK ---
        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            Vector2 mouse = Raylib.GetMousePosition();
            int screenWidth = Raylib.GetScreenWidth();
            int panelX = screenWidth / 2; 
            int currentMaxUnlocked = GlobalUnlocks.ContainsKey(CurrentCard.Name) ? GlobalUnlocks[CurrentCard.Name] : 0;

            // Gombok a jobb oldalon (Kiválasztás)
            Rectangle btnEasy   = new Rectangle(panelX + 50, 100, 300, 60);
            Rectangle btnNormal = new Rectangle(panelX + 50, 180, 300, 60);
            Rectangle btnHard   = new Rectangle(panelX + 50, 260, 300, 60);

            if (Raylib.CheckCollisionPointRec(mouse, btnEasy)) SelectedTaskLevel = 1;
            else if (Raylib.CheckCollisionPointRec(mouse, btnNormal)) SelectedTaskLevel = 2;
            else if (Raylib.CheckCollisionPointRec(mouse, btnHard)) SelectedTaskLevel = 4;

            // --- ÚJ: TEST CODE GOMB (Bal oldal alján) ---
            Rectangle testBtn = new Rectangle(50, Raylib.GetScreenHeight() - 100, 200, 50);
            if (Raylib.CheckCollisionPointRec(mouse, testBtn))
            {
                // 1. Mentsük el a kódot a VM-be, hogy leforduljon
                CurrentCard.VM.LoadCode(CodeBuffer);
                
                // 2. Kérjük le az aktuálisan kiválasztott feladatot
                AssemblyTask task = QuestDatabase.GetTask(CurrentCard.Name, SelectedTaskLevel);
                
                // 3. Futtassuk le a tesztet
                string failReason;
                bool passed = CurrentCard.VM.RunTest(task.TestInput, task.ExpectedOutput, out failReason);

                if (passed)
                {
                    TestResultMsg = "SUCCESS! Task Completed!";
                    TestResultColor = Color.Green;
                    
                    // Ha a feladat szintje magasabb, mint az eddigi feloldott, akkor frissítjük a globálist!
                    // Kivételkezelés: Csak akkor oldhatja fel a Normalt, ha az Easy már megvan (ezt itt ellenőrizzük)
                    bool canUnlock = true;
                    if (SelectedTaskLevel == 2 && currentMaxUnlocked < 1) canUnlock = false;
                    if (SelectedTaskLevel == 4 && currentMaxUnlocked < 2) canUnlock = false;

                    if (canUnlock)
                    {
                        if (SelectedTaskLevel > currentMaxUnlocked) GlobalUnlocks[CurrentCard.Name] = SelectedTaskLevel;
                        CurrentCard.CurrentMultiplier = SelectedTaskLevel; // Azonnal be is állítjuk a gépnek
                    }
                    else
                    {
                        TestResultMsg = "PASSED! But previous levels are locked.";
                        TestResultColor = Color.Yellow;
                    }
                }
                else
                {
                    TestResultMsg = "FAILED: " + failReason;
                    TestResultColor = Color.Red;
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
        int editorW = (screenWidth / 2) - margin; 
        int editorH = screenHeight - (margin * 2);

        // ==========================================
        // BAL OLDAL: FELADAT LEÍRÁS ÉS EDITOR
        // ==========================================
        Raylib.DrawRectangle(editorX, editorY, editorW, editorH, new Color(20, 20, 20, 255));
        Raylib.DrawRectangleLines(editorX, editorY, editorW, editorH, Color.White);
        
        // Aktuális feladat lekérése
        AssemblyTask currentTask = QuestDatabase.GetTask(CurrentCard.Name, SelectedTaskLevel);

        Raylib.DrawText($"TASK: {currentTask.Title} (Level {SelectedTaskLevel})", editorX + 20, editorY + 20, 30, Color.Gold);
        
        // Feladat leírása tördelve (Egyszerűsített tördelés \n alapján)
        string[] descLines = currentTask.Description.Split('\n');
        int descY = editorY + 60;
        foreach(var line in descLines) {
            Raylib.DrawText(line, editorX + 20, descY, 20, Color.LightGray);
            descY += 25;
        }

        // Vonal a leírás és a kód között
        Raylib.DrawLine(editorX, descY + 10, editorX + editorW, descY + 10, Color.DarkGray);

        // EDITOR RÉSZ
        int codeStartY = descY + 30;
        string[] lines = CodeBuffer.Split('\n');
        int lineY = codeStartY;
        for (int i = 0; i < lines.Length; i++)
        {
            Raylib.DrawText($"{i+1}.", editorX + 20, lineY, 30, Color.Gray);
            Raylib.DrawText(lines[i], editorX + 70, lineY, 30, Color.White);
            lineY += 35;
        }

        cursorTimer += Raylib.GetFrameTime();
        if (cursorTimer >= 0.5f) { cursorTimer = 0; cursorVisible = !cursorVisible; }
        if (cursorVisible) Raylib.DrawRectangle(editorX + 70 + (lines[lines.Length-1].Length * 18), lineY - 35, 15, 30, Color.Green);

        // TEST GOMB ÉS EREDMÉNY
        Rectangle testBtn = new Rectangle(editorX, screenHeight - margin - 60, 200, 50);
        Raylib.DrawRectangleRec(testBtn, Color.DarkGreen);
        Raylib.DrawRectangleLinesEx(testBtn, 2, Color.White);
        Raylib.DrawText("RUN TEST", editorX + 40, screenHeight - margin - 45, 20, Color.White);

        if (!string.IsNullOrEmpty(TestResultMsg))
        {
            // Tördeljük az eredmény üzenetet, ha hosszú (főleg hiba esetén)
            string[] resultLines = TestResultMsg.Split('\n');
            int ry = screenHeight - margin - 60;
            foreach(var rLine in resultLines) {
                Raylib.DrawText(rLine, editorX + 220, ry, 20, TestResultColor);
                ry += 25;
            }
        }

        // ==========================================
        // JOBB OLDAL: SZINTEK KIVÁLASZTÁSA ÉS TUTORIAL
        // ==========================================
        int panelX = screenWidth / 2;
        int currentMaxUnlocked = GlobalUnlocks.ContainsKey(CurrentCard.Name) ? GlobalUnlocks[CurrentCard.Name] : 0;

        Raylib.DrawText($"MACHINE: {CurrentCard.Name}", panelX + 50, editorY + 20, 40, CurrentCard.HeaderColor);
        
        // Szintek rajzolása
        DrawLevelButton(panelX + 50, 100, "EASY (x1)", 1, currentMaxUnlocked, SelectedTaskLevel == 1);
        DrawLevelButton(panelX + 50, 180, "NORMAL (x2)", 2, currentMaxUnlocked, SelectedTaskLevel == 2);
        DrawLevelButton(panelX + 50, 260, "HARD (x4)", 4, currentMaxUnlocked, SelectedTaskLevel == 4);

        // --- ÚJ: CHEAT SHEET (TUTORIAL) ---
        int tutY = 350;
        Raylib.DrawText("--- ASSEMBLY CHEAT SHEET ---", panelX + 50, tutY, 25, Color.Gold);
        
        string[] tutorialLines = new string[] {
            "REGISTERS: AX, BX, CX, DX (Variables to store numbers)",
            "",
            "MOV [reg], [val]  -> Move a number or reg into another reg.",
            "                     Example: MOV AX, 5",
            "ADD [reg], [val]  -> Add a number to a register.",
            "                     Example: ADD AX, 1",
            "SUB [reg], [val]  -> Subtract a number from a register.",
            "MUL [reg], [val]  -> Multiply a register by a number.",
            "",
            "IN [reg]          -> Read an item from the Belt/Inventory into reg.",
            "OUT [reg]         -> Output the value of reg to the Belt/Inventory.",
            "",
            "[LABEL]:          -> Create a jump point. Example: START:",
            "JMP [LABEL]       -> Jump back/forward to a label. Example: JMP START",
            "CMP [reg], [val]  -> Compare register with a value.",
            "JE [LABEL]        -> Jump if Equal (after CMP)."
        };

        int ty = tutY + 40;
        foreach(var tLine in tutorialLines)
        {
            if (tLine.StartsWith("---")) Raylib.DrawText(tLine, panelX + 50, ty, 20, Color.Yellow);
            else if (tLine.Contains("->")) 
            {
                string[] parts = tLine.Split("->");
                Raylib.DrawText(parts[0], panelX + 50, ty, 20, Color.Green); // Parancs kékkel
                Raylib.DrawText("-> " + parts[1], panelX + 250, ty, 20, Color.LightGray); // Magyarázat szürkén
            }
            else Raylib.DrawText(tLine, panelX + 50, ty, 20, Color.LightGray);
            
            ty += 25;
        }
    }

    private void DrawLevelButton(int x, int y, string title, int reqLevel, int unlockedLevel, bool isSelected)
    {
        bool isUnlocked = unlockedLevel >= (reqLevel == 4 ? 2 : reqLevel == 2 ? 1 : 0);
        Color boxColor = isSelected ? new Color(20, 80, 20, 255) : (isUnlocked ? Color.DarkGray : new Color(50, 20, 20, 255));

        Raylib.DrawRectangle(x, y, 400, 60, boxColor);
        if (isSelected) Raylib.DrawRectangleLinesEx(new Rectangle(x,y,400,60), 3, Color.Green);
        else Raylib.DrawRectangleLines(x, y, 400, 60, Color.White);

        Raylib.DrawText(title, x + 20, y + 15, 30, Color.White);

        if (isUnlocked && reqLevel <= unlockedLevel)
            Raylib.DrawText("UNLOCKED", x + 250, y + 20, 20, Color.Lime);
        else if (reqLevel == 2 && unlockedLevel == 0)
            Raylib.DrawText("LOCKED", x + 250, y + 20, 20, Color.Red);
        else if (reqLevel == 4 && unlockedLevel < 2)
            Raylib.DrawText("LOCKED", x + 250, y + 20, 20, Color.Red);
    }
}