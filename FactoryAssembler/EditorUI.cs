using Raylib_cs;
using System.Numerics;
using System.Collections.Generic;
using System; 
using System.Runtime.InteropServices; 
using System.Text; 

namespace FactoryAssembler;

public class EditorUI
{
    public bool IsVisible { get; private set; } = false;
    private Card? CurrentCard;
    
    private List<string> Lines = new List<string>();
    private int CursorRow = 0; private int CursorCol = 0;
    private float cursorTimer = 0; private bool cursorVisible = true;
    public Dictionary<string, int> GlobalUnlocks { get; set; } = new Dictionary<string, int>();
    
    private string CurrentTaskTitle = ""; private string CurrentTaskDesc = ""; 
    private int TargetLevel = 0; private bool IsTaskActive = false;
    
    private string FeedbackMsg = ""; private Color FeedbackColor = Color.White;
    private string LastTestInput = ""; private string LastTestOutput = ""; private string LastErrorMsg = "";

    // --- MODERN LAYOUT STRUKTÚRA ---
    private struct UILayout
    {
        public Rectangle PanelArea;    // A teljes jobb oldali panel
        public Rectangle HeaderArea;   // "Difficulty" szöveg
        public Rectangle BtnEasy;
        public Rectangle BtnNormal;
        public Rectangle BtnHard;
        
        // Dinamikus dobozok (csak ha aktív a task)
        public Rectangle GoalBox;
        public Rectangle TestBox;
        public Rectangle RunButton;
        
        // Referencia (mindig látszik a maradék helyen)
        public Rectangle ReferenceBox;
    }

    private UILayout CalculateLayout(int screenW, int screenH)
    {
        UILayout l = new UILayout();
        int margin = 30; 
        
        // 1. A JOBB OLDALI PANEL
        int panelX = screenW / 2;
        int panelW = screenW / 2;
        l.PanelArea = new Rectangle(panelX, 0, panelW, screenH);

        int contentX = panelX + margin;
        int contentW = panelW - (margin * 2);

        // 2. CÍMSOR ÉS GOMBOK
        int currentY = 50; 
        
        l.HeaderArea = new Rectangle(contentX, currentY, contentW, 40);
        currentY += 50;

        int btnHeight = 55;
        int btnGap = 15;

        l.BtnEasy = new Rectangle(contentX, currentY, contentW, btnHeight);
        currentY += btnHeight + btnGap;

        l.BtnNormal = new Rectangle(contentX, currentY, contentW, btnHeight);
        currentY += btnHeight + btnGap;

        l.BtnHard = new Rectangle(contentX, currentY, contentW, btnHeight);
        currentY += btnHeight + 30; 

        // 3. DINAMIKUS TARTALOM
        if (IsTaskActive)
        {
            l.GoalBox = new Rectangle(contentX, currentY, contentW, 140);
            currentY += 140 + 15;

            l.TestBox = new Rectangle(contentX, currentY, contentW, 110);
            currentY += 110 + 15;

            l.RunButton = new Rectangle(contentX, currentY, contentW, 50);
            currentY += 50 + 15;
        }

        // 4. REFERENCE BOX (Kitölti a maradékot)
        int refHeight = screenH - currentY - margin;
        if (refHeight < 150) refHeight = 150; // Minimum magasság, hogy olvasható legyen

        l.ReferenceBox = new Rectangle(contentX, currentY, contentW, refHeight);

        return l;
    }

    public void Open(Card card) {
        CurrentCard = card; Lines.Clear();
        if (card.VM.Instructions.Count > 0) Lines.AddRange(card.VM.Instructions); else Lines.Add(""); 
        CursorRow = 0; CursorCol = 0; IsVisible = true; IsTaskActive = false; FeedbackMsg = "";
        LastTestInput = ""; LastTestOutput = ""; LastErrorMsg = "";
    }

    public void Close() {
        if (CurrentCard != null) CurrentCard.VM.LoadCode(string.Join("\n", Lines));
        IsVisible = false; CurrentCard = null;
    }

    public void Update() {
        if (!IsVisible || CurrentCard == null) return;

        // Billentyűzet logika
        if (Raylib.IsKeyPressed(KeyboardKey.Right)) { if (CursorCol < Lines[CursorRow].Length) CursorCol++; else if (CursorRow < Lines.Count - 1) { CursorRow++; CursorCol = 0; } }
        if (Raylib.IsKeyPressed(KeyboardKey.Left)) { if (CursorCol > 0) CursorCol--; else if (CursorRow > 0) { CursorRow--; CursorCol = Lines[CursorRow].Length; } }
        if (Raylib.IsKeyPressed(KeyboardKey.Up)) { if (CursorRow > 0) { CursorRow--; CursorCol = Math.Min(CursorCol, Lines[CursorRow].Length); } }
        if (Raylib.IsKeyPressed(KeyboardKey.Down)) { if (CursorRow < Lines.Count - 1) { CursorRow++; CursorCol = Math.Min(CursorCol, Lines[CursorRow].Length); } }

        int key = Raylib.GetCharPressed();
        while (key > 0) { if (key >= 32 && key <= 125) { Lines[CursorRow] = Lines[CursorRow].Insert(CursorCol, ((char)key).ToString()); CursorCol++; } key = Raylib.GetCharPressed(); }
        if (Raylib.IsKeyPressed(KeyboardKey.Backspace)) { if (CursorCol > 0) { Lines[CursorRow] = Lines[CursorRow].Remove(CursorCol - 1, 1); CursorCol--; } else if (CursorRow > 0) { int prevLen = Lines[CursorRow - 1].Length; Lines[CursorRow - 1] += Lines[CursorRow]; Lines.RemoveAt(CursorRow); CursorRow--; CursorCol = prevLen; } }
        if (Raylib.IsKeyPressed(KeyboardKey.Enter)) { string remaining = Lines[CursorRow].Substring(CursorCol); Lines[CursorRow] = Lines[CursorRow].Substring(0, CursorCol); Lines.Insert(CursorRow + 1, remaining); CursorRow++; CursorCol = 0; }

        bool ctrl = Raylib.IsKeyDown(KeyboardKey.LeftControl) || Raylib.IsKeyDown(KeyboardKey.RightControl);
        if (ctrl && Raylib.IsKeyPressed(KeyboardKey.C)) { Raylib.SetClipboardText(string.Join("\n", Lines)); FeedbackMsg = "Copied!"; FeedbackColor = Color.Yellow; }
        if (ctrl && Raylib.IsKeyPressed(KeyboardKey.V)) { unsafe { sbyte* ptr = Raylib.GetClipboardText(); if (ptr != null) { string c = Marshal.PtrToStringUTF8((IntPtr)ptr) ?? ""; if (!string.IsNullOrEmpty(c)) { Lines.Clear(); Lines.AddRange(c.Split('\n')); CursorRow = Lines.Count-1; CursorCol = Lines[CursorRow].Length; FeedbackMsg = "Pasted!"; FeedbackColor = Color.Yellow; } } } }
        if (Raylib.IsKeyPressed(KeyboardKey.Escape)) Close();

        // Egér kezelés
        if (Raylib.IsMouseButtonPressed(MouseButton.Left)) {
            Vector2 mouse = Raylib.GetMousePosition(); 
            UILayout l = CalculateLayout(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());

            if (Raylib.CheckCollisionPointRec(mouse, l.BtnEasy)) StartTask(1);
            else if (Raylib.CheckCollisionPointRec(mouse, l.BtnNormal)) StartTask(2);
            else if (Raylib.CheckCollisionPointRec(mouse, l.BtnHard)) StartTask(4);

            if (IsTaskActive && Raylib.CheckCollisionPointRec(mouse, l.RunButton)) VerifyCode();
        }
    }

    public void Draw(int screenW, int screenH) {
        if (!IsVisible || CurrentCard == null) return;
        
        Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(10, 10, 15, 240));
        
        UILayout l = CalculateLayout(screenW, screenH);

        // --- BAL OLDAL (KÓD EDITOR) ---
        int margin = 30;
        
        // JAVÍTÁS: (int) konverzió a lebegőpontos hiba ellen
        int editorW = (int)l.PanelArea.X - (margin * 2);
        int editorH = screenH - (margin * 2);
        
        Raylib.DrawRectangle(margin, margin, editorW, editorH, new Color(30, 30, 35, 255));
        Raylib.DrawRectangleLinesEx(new Rectangle(margin, margin, editorW, editorH), 2, new Color(60, 60, 70, 255));
        
        Program.DrawText($"FILE: {CurrentCard.Name}.ASM", margin + 20, margin + 20, 30, CurrentCard.HeaderColor);
        Raylib.DrawLine(margin, margin + 60, margin + editorW, margin + 60, new Color(60, 60, 70, 255));

        Raylib.BeginScissorMode(margin + 5, margin + 70, editorW - 10, editorH - 80);
        int lineY = margin + 70;
        for (int i = 0; i < Lines.Count; i++) {
            Program.DrawText($"{i+1, 2}", margin + 15, lineY, 22, new Color(80, 80, 80, 255));
            Program.DrawText(Lines[i], margin + 60, lineY, 22, new Color(220, 220, 220, 255));
            if (i == CursorRow && cursorVisible) {
                int textW = Program.MeasureText(Lines[i].Substring(0, CursorCol), 22);
                Raylib.DrawRectangle(margin + 60 + textW, lineY, 2, 22, Color.Green);
            }
            lineY += 28;
        }
        Raylib.EndScissorMode();

        // --- JOBB OLDAL (DASHBOARD) ---
        Program.DrawText("DIFFICULTY SELECTOR", (int)l.HeaderArea.X, (int)l.HeaderArea.Y, 24, Color.Gray);

        DrawModernButton(l.BtnEasy, "EASY (x1) - Output", 1);
        DrawModernButton(l.BtnNormal, "NORMAL (x2) - Logic", 2);
        DrawModernButton(l.BtnHard, "HARD (x4) - Math", 4);

        if (IsTaskActive) {
            DrawPanel(l.GoalBox, "MISSION OBJECTIVE", Color.Gold);
            Raylib.BeginScissorMode((int)l.GoalBox.X + 5, (int)l.GoalBox.Y + 35, (int)l.GoalBox.Width - 10, (int)l.GoalBox.Height - 40);
                Program.DrawText(CurrentTaskTitle, (int)l.GoalBox.X + 15, (int)l.GoalBox.Y + 40, 22, Color.White);
                
                string wrappedDesc = Program.WordWrap(CurrentTaskDesc, 20, (int)l.GoalBox.Width - 30);
                int dy = (int)l.GoalBox.Y + 70; 
                foreach (var line in wrappedDesc.Split('\n')) { 
                    Program.DrawText(line, (int)l.GoalBox.X + 15, dy, 20, Color.LightGray); dy += 24; 
                }
            Raylib.EndScissorMode();

            DrawPanel(l.TestBox, "DEBUG CONSOLE", new Color(100, 149, 237, 255)); 
            Raylib.BeginScissorMode((int)l.TestBox.X + 5, (int)l.TestBox.Y + 35, (int)l.TestBox.Width - 10, (int)l.TestBox.Height - 40);
                Program.DrawText($"IN : {LastTestInput}", (int)l.TestBox.X + 15, (int)l.TestBox.Y + 40, 20, Color.LightGray);
                Program.DrawText($"OUT: {LastTestOutput}", (int)l.TestBox.X + 15, (int)l.TestBox.Y + 65, 20, Color.White);
                Color stColor = LastErrorMsg == "No errors." ? Color.Green : Color.Red;
                Program.DrawText($"STA: {LastErrorMsg}", (int)l.TestBox.X + 15, (int)l.TestBox.Y + 90, 20, stColor);
            Raylib.EndScissorMode();

            Color runColor = new Color(0, 120, 60, 255);
            if (Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), l.RunButton)) runColor = new Color(0, 160, 80, 255);
            Raylib.DrawRectangleRec(l.RunButton, runColor);
            Raylib.DrawRectangleLinesEx(l.RunButton, 2, Color.Green);
            
            string btnText = string.IsNullOrEmpty(FeedbackMsg) ? "COMPILE & RUN" : FeedbackMsg;
            Color txtColor = string.IsNullOrEmpty(FeedbackMsg) ? Color.White : FeedbackColor;
            
            Vector2 txtSize = Raylib.MeasureTextEx(Program.AppFont, btnText, 24, 1);
            Program.DrawText(btnText, (int)(l.RunButton.X + l.RunButton.Width/2 - txtSize.X/2), (int)(l.RunButton.Y + l.RunButton.Height/2 - txtSize.Y/2), 24, txtColor);
        }

        // REFERENCE MANUÁL KIRAJZOLÁSA
        DrawReference(l.ReferenceBox);

        cursorTimer += Raylib.GetFrameTime(); if (cursorTimer >= 0.5f) { cursorTimer = 0; cursorVisible = !cursorVisible; }
    }

    private void DrawPanel(Rectangle rect, string title, Color accent) {
        Raylib.DrawRectangleRec(rect, new Color(35, 35, 40, 255)); 
        Raylib.DrawRectangleLinesEx(rect, 1, new Color(60, 60, 70, 255)); 
        
        Raylib.DrawRectangle((int)rect.X, (int)rect.Y, (int)rect.Width, 30, new Color(25, 25, 30, 255));
        Raylib.DrawLine((int)rect.X, (int)rect.Y + 30, (int)rect.X + (int)rect.Width, (int)rect.Y + 30, accent); 
        
        Program.DrawText(title, (int)rect.X + 10, (int)rect.Y + 5, 18, accent);
    }

    private void DrawModernButton(Rectangle rect, string text, int level) {
        if (CurrentCard == null) return;
        int unlocked = GlobalUnlocks.ContainsKey(CurrentCard.Name) ? GlobalUnlocks[CurrentCard.Name] : 0;
        bool isHover = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), rect);
        bool isActive = IsTaskActive && TargetLevel == level;

        Color bg = new Color(40, 40, 45, 255);
        Color border = new Color(70, 70, 80, 255);
        Color txt = Color.Gray;

        if (isActive) {
            bg = new Color(0, 60, 100, 255); 
            border = new Color(0, 120, 200, 255);
            txt = Color.White;
        } else if (unlocked >= level) {
            bg = new Color(30, 50, 30, 255); 
            if (isHover) bg = new Color(40, 60, 40, 255);
            txt = Color.White;
        }

        Raylib.DrawRectangleRec(rect, bg);
        Raylib.DrawRectangleLinesEx(rect, isActive ? 2 : 1, border);
        
        Program.DrawText(text, (int)rect.X + 15, (int)rect.Y + 15, 22, txt);
        if (unlocked >= level && !isActive) {
             Program.DrawText("✔", (int)rect.X + (int)rect.Width - 30, (int)rect.Y + 15, 22, Color.Green);
        }
    }

    // --- ÚJ, KÉTOSZLOPOS REFERENCE MANUAL ---
    private void DrawReference(Rectangle rect) {
        // Címsor és keret
        DrawPanel(rect, "REFERENCE MANUAL", Color.Gray);
        
        // Vágás, hogy ne lógjon ki semmi
        Raylib.BeginScissorMode((int)rect.X + 5, (int)rect.Y + 35, (int)rect.Width - 10, (int)rect.Height - 40);
            
            int lineHeight = 20;
            int startY = (int)rect.Y + 45;

            // 1. OSZLOP: ADAT & MATEK
            int col1X = (int)rect.X + 15;
            int y = startY;

            Program.DrawText("-- DATA & MATH --", col1X, y, 18, Color.Yellow); y += 25;
            
            DrawCmd(col1X, y, "MOV A, B", "Copy B to A"); y += lineHeight;
            DrawCmd(col1X, y, "MOV A, 5", "Set A to 5"); y += lineHeight;
            DrawCmd(col1X, y, "ADD A, B", "A = A + B"); y += lineHeight;
            DrawCmd(col1X, y, "SUB A, B", "A = A - B"); y += lineHeight;
            DrawCmd(col1X, y, "MUL 5", "AX = AX * 5"); y += lineHeight;
            DrawCmd(col1X, y, "DIV 2", "AX = AX / 2"); y += lineHeight;

            // 2. OSZLOP: I/O & UGRÁSOK
            int col2X = (int)rect.X + (int)rect.Width / 2 + 10; 
            y = startY; // Vissza fentre

            Program.DrawText("-- I/O & CONTROL --", col2X, y, 18, Color.Yellow); y += 25;

            DrawCmd(col2X, y, "IN A", "Read Input -> A"); y += lineHeight;
            DrawCmd(col2X, y, "OUT A", "Output value of A"); y += lineHeight;
            DrawCmd(col2X, y, "CMP A, B", "Compare A and B"); y += lineHeight;
            DrawCmd(col2X, y, "JE LBL", "Jump if Equal"); y += lineHeight;
            DrawCmd(col2X, y, "JG/JL L", "Jump Greater/Less"); y += lineHeight;
            DrawCmd(col2X, y, "JMP LBL", "Jump always"); y += lineHeight;
            DrawCmd(col2X, y, "LBL:", "Define Label"); y += lineHeight;

        Raylib.EndScissorMode();
    }

    // Segédfüggvény a szép formázáshoz (Kék parancs, szürke leírás)
    private void DrawCmd(int x, int y, string cmd, string desc) {
        Program.DrawText(cmd, x, y, 18, new Color(100, 149, 237, 255)); // Kék
        Program.DrawText(desc, x + 90, y, 18, Color.Gray);
    }

    private void StartTask(int level) {
        if (CurrentCard == null) return;
        TargetLevel = level; IsTaskActive = true; FeedbackMsg = ""; string name = CurrentCard.Name;
        LastTestInput = ""; LastTestOutput = ""; LastErrorMsg = "";

        if (name == "Coal Miner") {
            if (level == 1) { CurrentTaskTitle = "EASY: Output 1"; CurrentTaskDesc = "Write code that outputs the number 1."; }
            else if (level == 2) { CurrentTaskTitle = "NORMAL: Output 1, 2, 3"; CurrentTaskDesc = "Write code that outputs 1, then 2, then 3."; }
            else if (level == 4) { CurrentTaskTitle = "HARD: Output 3-1"; CurrentTaskDesc = "Calculate 3 minus 1 and output the result."; }
        } else if (name == "Iron Miner") {
            if (level == 1) { CurrentTaskTitle = "EASY: Output 1 and 3"; CurrentTaskDesc = "Output 1, then output 3."; }
            else if (level == 2) { CurrentTaskTitle = "NORMAL: Is +2 Positive?"; CurrentTaskDesc = "Read input. If it is positive (>0), output 1. Else 0."; }
            else if (level == 4) { CurrentTaskTitle = "HARD: Is -2 Negative?"; CurrentTaskDesc = "Read input. If it is negative (<0), output 1. Else 0."; }
        } else if (name == "Copper Miner") {
            if (level == 1) { CurrentTaskTitle = "EASY: Say 'hello'"; CurrentTaskDesc = "Output the number 99 (represents 'hello')."; }
            else if (level == 2) { CurrentTaskTitle = "NORMAL: Is 9 Even?"; CurrentTaskDesc = "Read input. If even, output 1. Else 0."; }
            else if (level == 4) { CurrentTaskTitle = "HARD: Is 4 Odd?"; CurrentTaskDesc = "Read input. If odd, output 1. Else 0."; }
        } else if (name == "Smelter") {
            if (level == 1) { CurrentTaskTitle = "EASY: Say 'TDK'"; CurrentTaskDesc = "Output the number 404 (represents 'TDK')."; }
            else if (level == 2) { CurrentTaskTitle = "NORMAL: Output 5 Even Numbers"; CurrentTaskDesc = "Output 2, 4, 6, 8, 10."; }
            else if (level == 4) { CurrentTaskTitle = "HARD: Output 5 Odd Numbers"; CurrentTaskDesc = "Output 1, 3, 5, 7, 9."; }
        } else if (name == "Assembler") {
            if (level == 1) { CurrentTaskTitle = "EASY: Boolean False"; CurrentTaskDesc = "Output 0 (represents False)."; }
            else if (level == 2) { CurrentTaskTitle = "NORMAL: Prime Test (Is 2 Prime?)"; CurrentTaskDesc = "Read input. If it is 2, output 1. Else 0."; }
            else if (level == 4) { CurrentTaskTitle = "HARD: Add 10 to Input"; CurrentTaskDesc = "Read input, add 10 to it, and output."; }
        } else if (name == "Rocket Silo") {
            if (level == 1) { CurrentTaskTitle = "EASY: Output 100"; CurrentTaskDesc = "Output 100 to prepare the launch."; }
            else if (level == 2) { CurrentTaskTitle = "NORMAL: Multiply Input by 2"; CurrentTaskDesc = "Read input. Multiply by 2. Output."; }
            else if (level == 4) { CurrentTaskTitle = "HARD: Fibonacci"; CurrentTaskDesc = "Output 1, 1, 2, 3, 5."; }
        }
    }

    private void VerifyCode() {
        if (CurrentCard == null) return;
        VirtualMachine vm = new VirtualMachine(); 
        vm.LoadCode(string.Join("\n", Lines));
        bool success = true; string name = CurrentCard.Name;
        List<int> producedOutput = new List<int>();

        if (TargetLevel == 2 && name == "Iron Miner") { vm.InputBuffer.Enqueue(2); vm.InputBuffer.Enqueue(-5); LastTestInput = "2, -5"; }
        else if (TargetLevel == 4 && name == "Iron Miner") { vm.InputBuffer.Enqueue(-2); vm.InputBuffer.Enqueue(5); LastTestInput = "-2, 5"; }
        else if (TargetLevel == 2 && name == "Copper Miner") { vm.InputBuffer.Enqueue(9); vm.InputBuffer.Enqueue(8); LastTestInput = "9, 8"; }
        else if (TargetLevel == 4 && name == "Copper Miner") { vm.InputBuffer.Enqueue(4); vm.InputBuffer.Enqueue(5); LastTestInput = "4, 5"; }
        else if (TargetLevel == 2 && name == "Assembler") { vm.InputBuffer.Enqueue(2); vm.InputBuffer.Enqueue(4); LastTestInput = "2, 4"; }
        else if (TargetLevel == 4 && name == "Assembler") { vm.InputBuffer.Enqueue(5); LastTestInput = "5"; }
        else if (TargetLevel == 2 && name == "Rocket Silo") { vm.InputBuffer.Enqueue(10); LastTestInput = "10"; }
        else LastTestInput = "None";

        int steps = 0;
        while (steps < 200 && !vm.IsHalted) {
            if (vm.OutputBuffer.Count > 0) producedOutput.Add(vm.OutputBuffer.Dequeue());
            vm.Step(); steps++;
            if (vm.IsWaiting) break; 
        }
        while (vm.OutputBuffer.Count > 0) producedOutput.Add(vm.OutputBuffer.Dequeue());

        if (producedOutput.Count > 10) LastTestOutput = string.Join(", ", producedOutput.GetRange(0, 10)) + "...";
        else if (producedOutput.Count > 0) LastTestOutput = string.Join(", ", producedOutput);
        else LastTestOutput = "Nothing";

        LastErrorMsg = vm.LastError != "" ? vm.LastError : "No errors.";

        try {
            if (name == "Coal Miner") {
                if (TargetLevel == 1) { if (producedOutput.Count == 0 || producedOutput[0] != 1) success = false; }
                if (TargetLevel == 2) { if (producedOutput.Count < 3 || producedOutput[0]!=1 || producedOutput[1]!=2 || producedOutput[2]!=3) success=false; }
                if (TargetLevel == 4) { if (producedOutput.Count == 0 || producedOutput[0] != 2) success=false; } 
            } else if (name == "Iron Miner") {
                if (TargetLevel == 1) { if (producedOutput.Count < 2 || producedOutput[0]!=1 || producedOutput[1]!=3) success=false; }
                if (TargetLevel == 2) { if (producedOutput.Count < 2 || producedOutput[0]!=1 || producedOutput[1]!=0) success=false; } 
                if (TargetLevel == 4) { if (producedOutput.Count < 2 || producedOutput[0]!=1 || producedOutput[1]!=0) success=false; } 
            } else if (name == "Copper Miner") {
                if (TargetLevel == 1) { if (producedOutput.Count == 0 || producedOutput[0] != 99) success = false; }
                if (TargetLevel == 2) { if (producedOutput.Count < 2 || producedOutput[0]!=0 || producedOutput[1]!=1) success=false; } 
                if (TargetLevel == 4) { if (producedOutput.Count < 2 || producedOutput[0]!=0 || producedOutput[1]!=1) success=false; } 
            } else if (name == "Smelter") {
                if (TargetLevel == 1) { if (producedOutput.Count == 0 || producedOutput[0] != 404) success = false; }
                if (TargetLevel == 2) { if (producedOutput.Count < 5 || producedOutput[0]!=2 || producedOutput[4]!=10) success=false; }
                if (TargetLevel == 4) { if (producedOutput.Count < 5 || producedOutput[0]!=1 || producedOutput[4]!=9) success=false; }
            } else if (name == "Assembler") {
                if (TargetLevel == 1) { if (producedOutput.Count == 0 || producedOutput[0] != 0) success = false; }
                if (TargetLevel == 2) { if (producedOutput.Count < 2 || producedOutput[0]!=1 || producedOutput[1]!=0) success=false; } 
                if (TargetLevel == 4) { if (producedOutput.Count == 0 || producedOutput[0] != 15) success=false; } 
            } else if (name == "Rocket Silo") {
                if (TargetLevel == 1) { if (producedOutput.Count == 0 || producedOutput[0] != 100) success = false; }
                if (TargetLevel == 2) { if (producedOutput.Count == 0 || producedOutput[0] != 20) success=false; } 
                if (TargetLevel == 4) { if (producedOutput.Count < 5 || producedOutput[4] != 5) success=false; } 
            }
        } catch { success = false; }

        if (vm.Instructions.Count == 0) success = false;

        if (success) { 
            FeedbackMsg = "PASSED!"; FeedbackColor = Color.Lime; 
            int currentLevel = GlobalUnlocks.GetValueOrDefault(CurrentCard.Name, 0);
            GlobalUnlocks[CurrentCard.Name] = Math.Max(currentLevel, TargetLevel); 
            CurrentCard.CurrentMultiplier = Math.Max(currentLevel, TargetLevel); 
        } else { FeedbackMsg = "FAILED!"; FeedbackColor = Color.Red; }
    }
}