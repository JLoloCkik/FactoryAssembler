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

    // --- ELRENDEZÉS DEFINÍCIÓ ---
    private struct UILayout
    {
        public Rectangle GoalBox;
        public Rectangle TestBox;
        public Rectangle RunButton;
        public Rectangle ReferenceBox;
    }

    // Ez a függvény számolja ki a dobozokat. 
    // Mivel fix magasságokat használunk, nem fog ugrálni!
    private UILayout CalculateLayout(int panelX, int rightMargin, int width, int screenH)
    {
        UILayout l = new UILayout();
        int startY = 300; // Innen indulnak a dobozok a gombok alatt

        // 1. GOAL DOBOZ (Fix 200px magas - bőven elég a leírásnak)
        l.GoalBox = new Rectangle(panelX + rightMargin, startY, width, 200);
        
        // 2. TEST DOBOZ (Fix 150px magas)
        l.TestBox = new Rectangle(panelX + rightMargin, l.GoalBox.Y + l.GoalBox.Height + 20, width, 150);

        // 3. RUN GOMB (Fix 60px magas)
        l.RunButton = new Rectangle(panelX + rightMargin, l.TestBox.Y + l.TestBox.Height + 20, 250, 60);

        // 4. REFERENCE DOBOZ (A gomb aljától a képernyő aljáig)
        float refY = l.RunButton.Y + l.RunButton.Height + 20;
        float refH = screenH - refY - 50; // 50px margó alul
        if (refH < 100) refH = 100; // Minimum méret
        l.ReferenceBox = new Rectangle(panelX + rightMargin, refY, width, refH);

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

        // Billentyűzet (Változatlan)
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

        // Egér kezelés (A Layout kalkulátorral)
        if (Raylib.IsMouseButtonPressed(MouseButton.Left)) {
            Vector2 mouse = Raylib.GetMousePosition(); 
            int screenW = Raylib.GetScreenWidth(); int screenH = Raylib.GetScreenHeight();
            int panelX = screenW / 2; int rightMargin = 50; int contentWidth = 600;

            // Szint gombok
            if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(panelX + rightMargin, 80, contentWidth, 60))) StartTask(1); 
            else if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(panelX + rightMargin, 150, contentWidth, 60))) StartTask(2); 
            else if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(panelX + rightMargin, 220, contentWidth, 60))) StartTask(4); 

            // Run gomb (Csak ha aktív a task)
            if (IsTaskActive) {
                UILayout layout = CalculateLayout(panelX, rightMargin, contentWidth, screenH);
                if (Raylib.CheckCollisionPointRec(mouse, layout.RunButton)) VerifyCode();
            }
        }
    }

    public void Draw(int screenW, int screenH) {
        if (!IsVisible || CurrentCard == null) return;
        Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 230));
        
        int margin = 50; int editorW = (screenW / 2) - margin; int editorH = screenH - (margin * 2);

        // BAL OLDAL (EDITOR)
        Raylib.DrawRectangle(margin, margin, editorW, editorH, new Color(20, 20, 20, 255));
        Raylib.DrawRectangleLines(margin, margin, editorW, editorH, Color.White);
        Program.DrawText($"EDITING: {CurrentCard.Name}", margin + 20, margin + 20, 35, CurrentCard.HeaderColor);

        Raylib.BeginScissorMode(margin + 5, margin + 80, editorW - 10, editorH - 90);
        int lineY = margin + 80;
        for (int i = 0; i < Lines.Count; i++) {
            Program.DrawText($"{i+1}.", margin + 15, lineY, 24, Color.Gray);
            Program.DrawText(Lines[i], margin + 60, lineY, 24, Color.White);
            if (i == CursorRow && cursorVisible) {
                int textW = Program.MeasureText(Lines[i].Substring(0, CursorCol), 24);
                Raylib.DrawRectangle(margin + 60 + textW, lineY, 3, 24, Color.Green);
            }
            lineY += 30;
        }
        Raylib.EndScissorMode();

        // JOBB OLDAL
        int panelX = screenW / 2; int rightMargin = 50; int contentWidth = 600;
        Program.DrawText("SELECT DIFFICULTY LEVEL", panelX + rightMargin, margin + 20, 30, Color.White);
        
        DrawLevelBtn(panelX + rightMargin, 80, contentWidth, "EASY (x1) - Output", 1);
        DrawLevelBtn(panelX + rightMargin, 150, contentWidth, "NORMAL (x2) - Logic", 2);
        DrawLevelBtn(panelX + rightMargin, 220, contentWidth, "HARD (x4) - Math", 4);

        if (IsTaskActive) {
            // Itt használjuk a kalkulált layoutot, hogy szinkronban legyen az Update-tel!
            UILayout l = CalculateLayout(panelX, rightMargin, contentWidth, screenH);

            // GOAL DOBOZ
            Raylib.DrawRectangleRec(l.GoalBox, new Color(40, 40, 40, 255));
            Raylib.DrawRectangleLinesEx(l.GoalBox, 1, Color.Yellow);
            
            // Scissor: A szöveg nem lóghat ki a dobozból!
            Raylib.BeginScissorMode((int)l.GoalBox.X, (int)l.GoalBox.Y, (int)l.GoalBox.Width, (int)l.GoalBox.Height);
            Program.DrawText("GOAL:", (int)l.GoalBox.X + 20, (int)l.GoalBox.Y + 15, 24, Color.Yellow);
            Program.DrawText(CurrentTaskTitle, (int)l.GoalBox.X + 20, (int)l.GoalBox.Y + 45, 26, Color.White);
            
            string wrappedDesc = Program.WordWrap(CurrentTaskDesc, 22, contentWidth - 40);
            int dy = (int)l.GoalBox.Y + 80; 
            foreach (var line in wrappedDesc.Split('\n')) { 
                Program.DrawText(line, (int)l.GoalBox.X + 20, dy, 22, Color.LightGray); dy += 28; 
            }
            Raylib.EndScissorMode();

            // TEST RESULTS DOBOZ
            Raylib.DrawRectangleRec(l.TestBox, new Color(20, 20, 30, 255));
            Raylib.DrawRectangleLinesEx(l.TestBox, 1, Color.Blue);
            
            Raylib.BeginScissorMode((int)l.TestBox.X, (int)l.TestBox.Y, (int)l.TestBox.Width, (int)l.TestBox.Height);
            Program.DrawText($"Input: {LastTestInput}", (int)l.TestBox.X + 20, (int)l.TestBox.Y + 15, 22, Color.LightGray);
            Program.DrawText($"Output: {LastTestOutput}", (int)l.TestBox.X + 20, (int)l.TestBox.Y + 50, 22, Color.White);
            Color errCol = LastErrorMsg == "No errors." ? Color.Green : Color.Red;
            Program.DrawText($"Status: {LastErrorMsg}", (int)l.TestBox.X + 20, (int)l.TestBox.Y + 85, 22, errCol);
            Raylib.EndScissorMode();

            // RUN BUTTON
            Raylib.DrawRectangleRec(l.RunButton, new Color(0, 100, 0, 255));
            Program.DrawText("RUN TESTS", (int)l.RunButton.X + 30, (int)l.RunButton.Y + 15, 26, Color.White);
            if (!string.IsNullOrEmpty(FeedbackMsg)) Program.DrawText(FeedbackMsg, (int)l.RunButton.X + 220, (int)l.RunButton.Y + 15, 30, FeedbackColor);

            // REFERENCE (A gomb alatt)
            DrawReference(l.ReferenceBox);
        } 
        else {
            // REFERENCE (Ha nincs aktív feladat, kitölti a helyet a gombok alatt)
            // Kezdés: 300px (gombok alatt)
            int refY = 300;
            int refH = screenH - refY - 50;
            DrawReference(new Rectangle(panelX + rightMargin, refY, contentWidth, refH));
        }

        cursorTimer += Raylib.GetFrameTime(); if (cursorTimer >= 0.5f) { cursorTimer = 0; cursorVisible = !cursorVisible; }
    }

    private void DrawReference(Rectangle rect) {
        Program.DrawText("COMMAND REFERENCE", (int)rect.X, (int)rect.Y - 35, 26, Color.Gold);
        Raylib.DrawRectangleRec(rect, new Color(30, 30, 40, 255));
        Raylib.DrawRectangleLinesEx(rect, 1, Color.Gray);
        
        Raylib.BeginScissorMode((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
        string[] cmds = { 
            "AX, BX: Registers", "MOV A,B: Set A to B", "ADD A,B: Add B to A", "SUB A,B: Sub B from A", 
            "MUL/DIV: Math", "IN A: Read Input", "OUT A: Output A", "CMP A,B: Compare", 
            "JE/JG/JL: Jumps", "JMP L: Jump", "LBL:: Label" 
        };
        int cy = (int)rect.Y + 20; 
        foreach(var c in cmds) { 
            Program.DrawText(c, (int)rect.X + 20, cy, 22, Color.Yellow);
            cy += 30; 
        }
        Raylib.EndScissorMode();
    }

    private void DrawLevelBtn(int x, int y, int w, string text, int level) {
        if (CurrentCard == null) return;
        int unlocked = GlobalUnlocks.ContainsKey(CurrentCard.Name) ? GlobalUnlocks[CurrentCard.Name] : 0;
        Color col = (unlocked >= level) ? Color.DarkGreen : Color.DarkGray;
        if (IsTaskActive && TargetLevel == level) col = Color.Blue; 

        Raylib.DrawRectangle(x, y, w, 60, col);
        Program.DrawText(text, x + 20, y + 15, 24, Color.White);
        if (unlocked >= level) Program.DrawText("UNLOCKED", x + w - 150, y + 15, 24, Color.Lime);
    }
    
    // ... (StartTask és VerifyCode változatlan maradhat, mert azok csak logikát tartalmaznak)
    // Csak másold be ide a korábbi StartTask és VerifyCode függvényeket, amikben a feladatok vannak.
    // De a teljesség kedvéért ideírom őket újra, hogy egyben legyen a fájl:

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