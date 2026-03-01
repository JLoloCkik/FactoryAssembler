using Raylib_cs;
using System.Numerics;
using System.Collections.Generic;
using System; 
using System.Runtime.InteropServices; 
using System.Text; // StringBuilder-hez

namespace FactoryAssembler;

public class EditorUI
{
    public bool IsVisible { get; private set; } = false;
    private Card? CurrentCard;
    
    private List<string> Lines = new List<string>();
    private int CursorRow = 0;
    private int CursorCol = 0;
    private float cursorTimer = 0;
    private bool cursorVisible = true;

    public Dictionary<string, int> GlobalUnlocks { get; set; } = new Dictionary<string, int>();
    
    private string CurrentTaskTitle = "";
    private string CurrentTaskDesc = "";
    private string CurrentTaskTutorial = "";
    private int TargetLevel = 0; 
    private bool IsTaskActive = false;
    private string FeedbackMsg = ""; 
    private Color FeedbackColor = Color.White;

    public void Open(Card card)
    {
        CurrentCard = card;
        Lines.Clear();
        if (card.VM.Instructions.Count > 0) Lines.AddRange(card.VM.Instructions);
        else Lines.Add(""); 
        CursorRow = 0; CursorCol = 0; IsVisible = true; IsTaskActive = false; FeedbackMsg = "";
    }

    public void Close()
    {
        if (CurrentCard != null) CurrentCard.VM.LoadCode(string.Join("\n", Lines));
        IsVisible = false; CurrentCard = null;
    }

    public void Update()
    {
        if (!IsVisible || CurrentCard == null) return;

        // --- NAVIGÁCIÓ & SZERKESZTÉS (Változatlan) ---
        if (Raylib.IsKeyPressed(KeyboardKey.Right)) {
            if (CursorCol < Lines[CursorRow].Length) CursorCol++;
            else if (CursorRow < Lines.Count - 1) { CursorRow++; CursorCol = 0; }
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Left)) {
            if (CursorCol > 0) CursorCol--;
            else if (CursorRow > 0) { CursorRow--; CursorCol = Lines[CursorRow].Length; }
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Up)) { if (CursorRow > 0) { CursorRow--; CursorCol = Math.Min(CursorCol, Lines[CursorRow].Length); } }
        if (Raylib.IsKeyPressed(KeyboardKey.Down)) { if (CursorRow < Lines.Count - 1) { CursorRow++; CursorCol = Math.Min(CursorCol, Lines[CursorRow].Length); } }

        int key = Raylib.GetCharPressed();
        while (key > 0) {
            if (key >= 32 && key <= 125) { Lines[CursorRow] = Lines[CursorRow].Insert(CursorCol, ((char)key).ToString()); CursorCol++; }
            key = Raylib.GetCharPressed();
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Backspace)) {
            if (CursorCol > 0) { Lines[CursorRow] = Lines[CursorRow].Remove(CursorCol - 1, 1); CursorCol--; }
            else if (CursorRow > 0) { int prevLen = Lines[CursorRow - 1].Length; Lines[CursorRow - 1] += Lines[CursorRow]; Lines.RemoveAt(CursorRow); CursorRow--; CursorCol = prevLen; }
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Enter)) {
            string remaining = Lines[CursorRow].Substring(CursorCol);
            Lines[CursorRow] = Lines[CursorRow].Substring(0, CursorCol);
            Lines.Insert(CursorRow + 1, remaining);
            CursorRow++; CursorCol = 0;
        }

        // VÁGÓLAP
        bool ctrl = Raylib.IsKeyDown(KeyboardKey.LeftControl) || Raylib.IsKeyDown(KeyboardKey.RightControl);
        if (ctrl && Raylib.IsKeyPressed(KeyboardKey.C)) { Raylib.SetClipboardText(string.Join("\n", Lines)); FeedbackMsg = "Code copied!"; FeedbackColor = Color.Yellow; }
        if (ctrl && Raylib.IsKeyPressed(KeyboardKey.V)) {
            unsafe {
                sbyte* ptr = Raylib.GetClipboardText();
                if (ptr != null) {
                    string clipboard = Marshal.PtrToStringUTF8((IntPtr)ptr) ?? "";
                    if (!string.IsNullOrEmpty(clipboard)) {
                        Lines.Clear(); Lines.AddRange(clipboard.Split('\n'));
                        CursorRow = Lines.Count - 1; CursorCol = Lines[CursorRow].Length;
                        FeedbackMsg = "Code pasted!"; FeedbackColor = Color.Yellow;
                    }
                }
            }
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Escape)) Close();

        // --- GOMBOK KEZELÉSE ---
        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            Vector2 mouse = Raylib.GetMousePosition();
            int screenW = Raylib.GetScreenWidth();
            int panelX = screenW / 2;

            // Szintek
            if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(panelX + 50, 100, 500, 60))) StartTask(1); 
            else if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(panelX + 50, 200, 500, 60))) StartTask(2); 
            else if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(panelX + 50, 300, 500, 60))) StartTask(4); 

            // A Verify gomb pozíciója most dinamikus, de a logikában fix helyen keressük egyszerűsítésként, 
            // vagy meg kellene jegyeznünk a kirajzolásnál hol volt.
            // Hogy ne legyen bonyolult: A gomra kattintást a Draw-ban nem tudjuk kezelni, 
            // ezért itt egy "nagyjából" helyet nézünk, VAGY a Draw-ban tároljuk el a gomb rect-jét.
            // Egyszerűbb megoldás: Ha aktív a feladat, a Verify gomb mindig a képernyő jobb alján legyen fixen? 
            // Vagy számoljuk ki itt is a pozíciót. Számoljuk ki:
            
            if (IsTaskActive)
            {
                // Újraszámoljuk a magasságokat, hogy tudjuk hol a gomb (ez nem a leghatékonyabb, de működik)
                string wrappedDesc = WordWrap(CurrentTaskDesc, 20, 460);
                int descLines = wrappedDesc.Split('\n').Length;
                int goalHeight = 60 + (descLines * 25) + 20;
                int goalY = 400;
                int btnY = goalY + goalHeight + 20;

                if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(panelX + 50, btnY, 200, 60)))
                    VerifyCode();
            }
        }
    }

    // --- ÚJ: DINAMIKUS TÖRDELÉS ---
    private string WordWrap(string text, int fontSize, int maxWidth)
    {
        if (string.IsNullOrEmpty(text)) return "";
        string[] words = text.Split(new[] {' ', '\n'}, StringSplitOptions.RemoveEmptyEntries); // Szavakra bontás
        StringBuilder sb = new StringBuilder();
        float lineWidth = 0;

        foreach (var word in words)
        {
            float wordWidth = Raylib.MeasureText(word + " ", fontSize);
            if (lineWidth + wordWidth > maxWidth)
            {
                sb.Append("\n");
                lineWidth = 0;
            }
            sb.Append(word + " ");
            lineWidth += wordWidth;
        }
        return sb.ToString();
    }

    public void Draw(int screenW, int screenH)
    {
        if (!IsVisible || CurrentCard == null) return;

        // Háttér
        Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 220));
        int margin = 50;
        int editorW = (screenW / 2) - margin;
        int editorH = screenH - (margin * 2);

        // BAL OLDAL: EDITOR
        Raylib.DrawRectangle(margin, margin, editorW, editorH, new Color(20, 20, 20, 255));
        Raylib.DrawRectangleLines(margin, margin, editorW, editorH, Color.White);
        Raylib.DrawText($"EDITING: {CurrentCard.Name}", margin + 20, margin + 20, 30, CurrentCard.HeaderColor);

        int lineY = margin + 80;
        for (int i = 0; i < Lines.Count; i++)
        {
            Raylib.DrawText($"{i+1}.", margin + 15, lineY, 20, Color.Gray);
            Raylib.DrawText(Lines[i], margin + 60, lineY, 20, Color.White);
            if (i == CursorRow && cursorVisible) {
                int textW = Raylib.MeasureText(Lines[i].Substring(0, CursorCol), 20);
                Raylib.DrawRectangle(margin + 60 + textW, lineY, 2, 20, Color.Green);
            }
            lineY += 25;
        }

        // JOBB OLDAL
        int panelX = screenW / 2;
        int rightMargin = 50;
        int contentWidth = 600; // Dobozok szélessége

        Raylib.DrawText("SELECT DIFFICULTY LEVEL", panelX + rightMargin, margin + 20, 30, Color.White);

        DrawLevelBtn(panelX + rightMargin, 100, contentWidth, "EASY (x1) - Output constant", 1);
        DrawLevelBtn(panelX + rightMargin, 200, contentWidth, "NORMAL (x2) - Simple math", 2);
        DrawLevelBtn(panelX + rightMargin, 300, contentWidth, "HARD (x4) - Logic & Branching", 4);

        if (IsTaskActive)
        {
            int startY = 400;
            
            // 1. CÉL DOBOZ (GOAL) - Dinamikus magasság
            string wrappedDesc = WordWrap(CurrentTaskDesc, 20, contentWidth - 40); // -40 padding
            int descLineCount = wrappedDesc.Split('\n').Length;
            int goalBoxHeight = 60 + (descLineCount * 25) + 20; // Title + Text + Padding

            Raylib.DrawRectangle(panelX + rightMargin, startY, contentWidth, goalBoxHeight, new Color(40, 40, 40, 255));
            Raylib.DrawRectangleLines(panelX + rightMargin, startY, contentWidth, goalBoxHeight, Color.Yellow);
            
            Raylib.DrawText("CURRENT GOAL:", panelX + rightMargin + 20, startY + 15, 20, Color.Yellow);
            Raylib.DrawText(CurrentTaskTitle, panelX + rightMargin + 20, startY + 40, 25, Color.White);
            
            // Tördelt leírás kirajzolása soronként
            int dy = startY + 70;
            foreach (var line in wrappedDesc.Split('\n'))
            {
                Raylib.DrawText(line, panelX + rightMargin + 20, dy, 20, Color.LightGray);
                dy += 25;
            }

            // 2. RUN BUTTON (A Goal doboz alá kerül)
            int btnY = startY + goalBoxHeight + 20;
            Raylib.DrawRectangle(panelX + rightMargin, btnY, 200, 60, new Color(0, 100, 0, 255));
            Raylib.DrawText("RUN TESTS", panelX + rightMargin + 30, btnY + 20, 20, Color.White);

            // Visszajelzés szöveg
            if (!string.IsNullOrEmpty(FeedbackMsg)) Raylib.DrawText(FeedbackMsg, panelX + rightMargin + 220, btnY + 20, 25, FeedbackColor);

            // 3. TUTORIAL DOBOZ (A Gomb alá kerül)
            int tutY = btnY + 80;
            string wrappedTut = WordWrap(CurrentTaskTutorial, 20, contentWidth - 40);
            int tutLineCount = wrappedTut.Split('\n').Length;
            int tutBoxHeight = 20 + (tutLineCount * 25) + 20; // Padding + Lines + Padding
            
            // Ha lelógna a képernyőről, vágjuk le, vagy engedjük (most engedjük)
            Raylib.DrawRectangle(panelX + rightMargin, tutY, contentWidth, tutBoxHeight, new Color(20, 20, 30, 255));
            Raylib.DrawRectangleLines(panelX + rightMargin, tutY, contentWidth, tutBoxHeight, Color.Blue);

            int ty = tutY + 20;
            foreach (var line in wrappedTut.Split('\n'))
            {
                Raylib.DrawText(line, panelX + rightMargin + 20, ty, 20, Color.White);
                ty += 25;
            }
        }

        cursorTimer += Raylib.GetFrameTime();
        if (cursorTimer >= 0.5f) { cursorTimer = 0; cursorVisible = !cursorVisible; }
    }

    private void DrawLevelBtn(int x, int y, int w, string text, int level)
    {
        if (CurrentCard == null) return;
        int unlocked = GlobalUnlocks.ContainsKey(CurrentCard.Name) ? GlobalUnlocks[CurrentCard.Name] : 0;
        Color col = (unlocked >= level) ? Color.DarkGreen : Color.DarkGray;
        if (IsTaskActive && TargetLevel == level) col = Color.Blue; 

        Raylib.DrawRectangle(x, y, w, 60, col);
        Raylib.DrawText(text, x + 20, y + 20, 20, Color.White);
        if (unlocked >= level) Raylib.DrawText("UNLOCKED", x + w - 120, y + 20, 20, Color.Lime);
    }
    
    // --- FELADAT DEFINÍCIÓK (Változatlan) ---
    private void StartTask(int level)
    {
        TargetLevel = level; IsTaskActive = true; FeedbackMsg = "";
        if (level == 1) {
            CurrentTaskTitle = "EASY TASK: Constant Output";
            CurrentTaskDesc = "Write a program that outputs the number 1 repeatedly.";
            CurrentTaskTutorial = "TUTORIAL:\nUse 'MOV AX, 1' to set value.\nUse 'OUT AX' to output.\nUse 'JMP START' to loop.";
        } else if (level == 2) {
            CurrentTaskTitle = "NORMAL TASK: Pass Through";
            CurrentTaskDesc = "Read input, add 10 to it, and output the result.";
            CurrentTaskTutorial = "TUTORIAL:\nUse 'IN AX' to read.\nUse 'ADD AX, 10'.\nOutput and Loop.";
        } else if (level == 4) {
            CurrentTaskTitle = "HARD TASK: Multiply";
            CurrentTaskDesc = "Read input. If it is 0, output 0. Otherwise multiply by 2.";
            CurrentTaskTutorial = "TUTORIAL:\nUse 'CMP AX, 0' to compare.\nUse 'JE LABEL' to jump if equal.\nElse use 'MUL AX, 2'.";
        }
    }

    private void VerifyCode()
    {
        VirtualMachine testVM = new VirtualMachine();
        testVM.LoadCode(string.Join("\n", Lines));
        bool success = true;
        
        if (TargetLevel == 1) {
            for(int i=0; i<5; i++) { testVM.Step(); if(testVM.OutputBuffer.Count > 0 && testVM.OutputBuffer.Dequeue() != 1) success=false; }
            if(testVM.Instructions.Count==0) success=false;
        } else if (TargetLevel == 2) {
            testVM.InputBuffer.Enqueue(5); int s=0; while(testVM.OutputBuffer.Count==0 && s<100){testVM.Step();s++;}
            if(testVM.OutputBuffer.Count==0 || testVM.OutputBuffer.Dequeue()!=15) success=false;
        } else if (TargetLevel == 4) {
            testVM.InputBuffer.Enqueue(0); int s=0; while(testVM.OutputBuffer.Count==0 && s<100){testVM.Step();s++;}
            if(testVM.OutputBuffer.Count==0 || testVM.OutputBuffer.Dequeue()!=0) success=false;
            testVM.InputBuffer.Enqueue(5); s=0; while(testVM.OutputBuffer.Count==0 && s<100){testVM.Step();s++;}
            if(testVM.OutputBuffer.Count==0 || testVM.OutputBuffer.Dequeue()!=10) success=false;
        }

        if (success && CurrentCard != null) { 
            FeedbackMsg = "TEST PASSED!"; FeedbackColor = Color.Green; 
            GlobalUnlocks[CurrentCard.Name] = TargetLevel; CurrentCard.CurrentMultiplier = TargetLevel; IsTaskActive = false; 
        } else { FeedbackMsg = "FAILED!"; FeedbackColor = Color.Red; }
    }
}