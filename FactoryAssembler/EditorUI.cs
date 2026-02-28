using Raylib_cs;
using System.Numerics;
using System.Collections.Generic;

namespace FactoryAssembler;

public class EditorUI
{
    public bool IsVisible { get; private set; } = false;
    public Dictionary<string, int> GlobalUnlocks { get; set; }

    private Card? CurrentCard;
    private string CodeBuffer = "";
    
    // Állapotkezelés
    enum EditorState { LevelSelect, Solving }
    private EditorState CurrentState = EditorState.LevelSelect;
    
    private Puzzle? CurrentPuzzle;
    private int TargetLevel = 1; // Melyik szintet próbáljuk feloldani?
    private string TestResult = ""; // "SUCCESS" vagy hibaüzenet
    private Color ResultColor = Color.Gray;

    // Kurzor villogás
    private float cursorTimer = 0;
    private bool cursorVisible = true;

    public void Open(Card card)
    {
        CurrentCard = card;
        CodeBuffer = string.Join("\n", card.VM.Instructions);
        IsVisible = true;
        CurrentState = EditorState.LevelSelect; // Mindig a választóval nyitunk
        TestResult = "";
    }

    public void Close()
    {
        // Csak akkor mentjük a kódot a gépbe, ha bezárjuk
        if (CurrentCard != null) CurrentCard.VM.LoadCode(CodeBuffer);
        IsVisible = false;
        CurrentCard = null;
    }

    public void Update()
    {
        if (!IsVisible || CurrentCard == null) return;

        // --- BILLENTYŰZET (KÓD SZERKESZTÉS) ---
        // Csak akkor engedünk írni, ha Solving módban vagyunk, VAGY ha a kódot nézzük
        // De egyszerűsítsünk: Mindig lehet írni.
        int key = Raylib.GetCharPressed();
        while (key > 0)
        {
            if ((key >= 32) && (key <= 125)) CodeBuffer += (char)key;
            key = Raylib.GetCharPressed();
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && CodeBuffer.Length > 0) CodeBuffer = CodeBuffer.Substring(0, CodeBuffer.Length - 1);
        if (Raylib.IsKeyPressed(KeyboardKey.Enter)) CodeBuffer += "\n";
        if (Raylib.IsKeyPressed(KeyboardKey.Escape)) Close();

        // --- EGÉR KEZELÉS ---
        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            Vector2 mouse = Raylib.GetMousePosition();
            int screenW = Raylib.GetScreenWidth();
            int midX = screenW / 2;

            if (CurrentState == EditorState.LevelSelect)
            {
                HandleLevelSelectClicks(mouse, midX);
            }
            else if (CurrentState == EditorState.Solving)
            {
                HandleSolvingClicks(mouse, midX);
            }
        }
    }

    private void HandleLevelSelectClicks(Vector2 mouse, int startX)
    {
        int unlocked = GlobalUnlocks.ContainsKey(CurrentCard.Name) ? GlobalUnlocks[CurrentCard.Name] : 0;
        
        // Gombok: Easy, Normal, Hard
        if (CheckBtn(mouse, startX + 50, 150)) SelectLevel(1, unlocked);
        if (CheckBtn(mouse, startX + 50, 300)) SelectLevel(2, unlocked);
        if (CheckBtn(mouse, startX + 50, 450)) SelectLevel(4, unlocked);
    }

    private void SelectLevel(int level, int unlocked)
    {
        // Csak akkor engedjük, ha az előző szint megvan (kivéve Easy)
        bool canTry = level == 1 || unlocked >= (level == 4 ? 2 : 1);
        
        if (canTry)
        {
            // Megkeressük a puzzle-t az adatbázisból
            string key = $"{CurrentCard.Name}_{level}";
            
            // Ha nincs ilyen puzzle (pl. nem írtuk meg), akkor alapértelmezetten feloldjuk
            if (!PuzzleDatabase.Puzzles.ContainsKey(key))
            {
                // Fallback: Ha nincs puzzle, csak aktiváljuk
                if (unlocked < level) GlobalUnlocks[CurrentCard.Name] = level;
                CurrentCard.CurrentMultiplier = level;
                Close();
                return;
            }

            CurrentPuzzle = PuzzleDatabase.Puzzles[key];
            TargetLevel = level;
            CurrentState = EditorState.Solving;
            TestResult = "Press RUN TEST to verify code.";
            ResultColor = Color.Gray;
        }
    }

    private void HandleSolvingClicks(Vector2 mouse, int startX)
    {
        // "RUN TEST" gomb
        Rectangle btnRun = new Rectangle(startX + 50, 400, 200, 50);
        if (Raylib.CheckCollisionPointRec(mouse, btnRun))
        {
            string result = PuzzleDatabase.RunTests(CodeBuffer, CurrentPuzzle);
            if (result == "SUCCESS")
            {
                TestResult = "SUCCESS! Level Unlocked.";
                ResultColor = Color.Green;
                
                // FELOLDÁS MENTÉSE!
                int currentMax = GlobalUnlocks.ContainsKey(CurrentCard.Name) ? GlobalUnlocks[CurrentCard.Name] : 0;
                if (currentMax < TargetLevel) GlobalUnlocks[CurrentCard.Name] = TargetLevel;
                CurrentCard.CurrentMultiplier = TargetLevel;
            }
            else
            {
                TestResult = result;
                ResultColor = Color.Red;
            }
        }

        // "BACK" gomb
        Rectangle btnBack = new Rectangle(startX + 270, 400, 150, 50);
        if (Raylib.CheckCollisionPointRec(mouse, btnBack))
        {
            CurrentState = EditorState.LevelSelect;
        }
    }

    private bool CheckBtn(Vector2 mouse, int x, int y)
    {
        return Raylib.CheckCollisionPointRec(mouse, new Rectangle(x, y, 500, 100));
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsVisible || CurrentCard == null) return;

        Raylib.DrawRectangle(0, 0, screenWidth, screenHeight, new Color(0, 0, 0, 220));

        int margin = 50;
        int editorW = (screenWidth / 2) - margin;
        
        // --- BAL OLDAL: EDITOR ---
        DrawEditorPanel(margin, margin, editorW, screenHeight - 100);

        // --- JOBB OLDAL: TARTALOM ---
        int rightX = screenWidth / 2;
        
        if (CurrentState == EditorState.LevelSelect)
        {
            DrawLevelSelect(rightX, margin);
        }
        else
        {
            DrawPuzzleView(rightX, margin);
        }
    }

    private void DrawEditorPanel(int x, int y, int w, int h)
    {
        Raylib.DrawRectangle(x, y, w, h, new Color(20, 20, 20, 255));
        Raylib.DrawRectangleLines(x, y, w, h, Color.White);
        Raylib.DrawText($"CODE: {CurrentCard.Name}", x + 20, y + 20, 30, CurrentCard.HeaderColor);

        string[] lines = CodeBuffer.Split('\n');
        int lineY = y + 80;
        for (int i = 0; i < lines.Length; i++)
        {
            Raylib.DrawText($"{i+1}.", x + 20, lineY, 30, Color.Gray);
            Raylib.DrawText(lines[i], x + 80, lineY, 30, Color.White);
            lineY += 35;
        }
        
        // Kurzor
        cursorTimer += Raylib.GetFrameTime();
        if (cursorTimer >= 0.5f) { cursorTimer = 0; cursorVisible = !cursorVisible; }
        if (cursorVisible) Raylib.DrawRectangle(x + 80 + (lines[lines.Length-1].Length * 18), lineY - 35, 15, 30, Color.Green);

        // --- TUTORIAL PANEL (Jobb Alul a Bal oldalon belül) ---
        int tutH = 200;
        int tutY = y + h - tutH;
        Raylib.DrawRectangle(x, tutY, w, tutH, new Color(10, 10, 15, 255));
        Raylib.DrawLine(x, tutY, x+w, tutY, Color.Gray);
        Raylib.DrawText("TUTORIAL / CHEATSHEET", x + 10, tutY + 10, 20, Color.Gold);
        
        Raylib.DrawText("MOV dest, val  -> Set value (MOV AX, 5)", x + 20, tutY + 40, 15, Color.White);
        Raylib.DrawText("ADD dest, val  -> Add value (ADD AX, 1)", x + 20, tutY + 60, 15, Color.White);
        Raylib.DrawText("SUB / MUL / DIV -> Math operations", x + 20, tutY + 80, 15, Color.White);
        Raylib.DrawText("IN reg         -> Read input (IN AX)", x + 20, tutY + 100, 15, Color.White);
        Raylib.DrawText("OUT reg        -> Send output (OUT AX)", x + 20, tutY + 120, 15, Color.White);
        Raylib.DrawText("JMP label      -> Jump to label (JMP START)", x + 20, tutY + 140, 15, Color.White);
        Raylib.DrawText("CMP a, b       -> Compare for JE/JNE/JL/JG", x + 20, tutY + 160, 15, Color.White);
    }

    private void DrawLevelSelect(int x, int y)
    {
        Raylib.DrawText("SELECT OPTIMIZATION LEVEL", x + 50, y + 20, 30, Color.White);
        int unlocked = GlobalUnlocks.ContainsKey(CurrentCard.Name) ? GlobalUnlocks[CurrentCard.Name] : 0;

        DrawLevelBtn(x + 50, 150, "EASY (x1)", "Basic Task", 1, unlocked);
        DrawLevelBtn(x + 50, 300, "NORMAL (x2)", "Advanced Logic", 2, unlocked);
        DrawLevelBtn(x + 50, 450, "HARD (x4)", "Master Class", 4, unlocked);
    }

    private void DrawLevelBtn(int x, int y, string title, string desc, int level, int unlocked)
    {
        int req = level == 4 ? 2 : (level == 2 ? 1 : 0);
        bool isLocked = unlocked < req;
        Color col = isLocked ? new Color(50, 20, 20, 255) : Color.DarkGray;
        if (CurrentCard.CurrentMultiplier == level) col = new Color(20, 80, 20, 255); // Active

        Raylib.DrawRectangle(x, y, 500, 100, col);
        Raylib.DrawRectangleLines(x, y, 500, 100, Color.White);
        Raylib.DrawText(title, x + 20, y + 20, 30, isLocked ? Color.Gray : Color.White);
        Raylib.DrawText(isLocked ? "LOCKED" : desc, x + 20, y + 60, 20, Color.LightGray);
    }

    private void DrawPuzzleView(int x, int y)
    {
        Raylib.DrawText($"PUZZLE: {TargetLevel}x Multiplier", x + 50, y + 20, 30, Color.Gold);
        
        // Leírás doboz
        Raylib.DrawRectangle(x + 50, y + 80, 500, 150, new Color(30, 30, 30, 255));
        Raylib.DrawRectangleLines(x + 50, y + 80, 500, 150, Color.White);
        
        // Sortörés egyszerű szimulálása
        Raylib.DrawText(CurrentPuzzle.Description, x + 70, y + 100, 20, Color.White);
        
        string inEx = "Inputs: " + string.Join(", ", CurrentPuzzle.TestInputs);
        string outEx = "Expected: " + string.Join(", ", CurrentPuzzle.ExpectedOutputs);
        Raylib.DrawText(inEx, x + 70, y + 160, 20, Color.Yellow);
        Raylib.DrawText(outEx, x + 70, y + 190, 20, Color.Green);

        // EREDMÉNY
        Raylib.DrawText("STATUS:", x + 50, y + 260, 20, Color.Gray);
        Raylib.DrawText(TestResult, x + 50, y + 290, 25, ResultColor);

        // GOMBOK
        Raylib.DrawRectangle(x + 50, 400, 200, 50, Color.Blue);
        Raylib.DrawText("RUN TEST", x + 85, 415, 20, Color.White);

        Raylib.DrawRectangle(x + 270, 400, 150, 50, Color.DarkGray);
        Raylib.DrawText("BACK", x + 315, 415, 20, Color.White);
    }
}