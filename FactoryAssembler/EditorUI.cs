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
    
    // Teszt eredmények tárolása a UI-hoz
    private string FeedbackMsg = ""; private Color FeedbackColor = Color.White;
    private string LastTestInput = "";
    private string LastTestOutput = "";
    private string LastErrorMsg = "";

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

        if (Raylib.IsMouseButtonPressed(MouseButton.Left)) {
            Vector2 mouse = Raylib.GetMousePosition(); int panelX = Raylib.GetScreenWidth() / 2;
            
            if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(panelX + 50, 100, 600, 60))) StartTask(1); 
            else if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(panelX + 50, 170, 600, 60))) StartTask(2); 
            else if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(panelX + 50, 240, 600, 60))) StartTask(4); 

            if (IsTaskActive && Raylib.CheckCollisionPointRec(mouse, new Rectangle(panelX + 50, 520, 200, 60))) VerifyCode();
        }
    }

    private void StartTask(int level) {
        if (CurrentCard == null) return;
        TargetLevel = level; IsTaskActive = true; FeedbackMsg = ""; string name = CurrentCard.Name;
        LastTestInput = ""; LastTestOutput = ""; LastErrorMsg = "";

        // ITT DEFINIÁLJUK A FELADATOKAT (Tutorial leírás KIVÉVE, az lent lesz az eszköztárban fixen)
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
        } else {
            CurrentTaskTitle = "EASY"; CurrentTaskDesc = "Output 1.";
        }
    }

    private void VerifyCode() {
        if (CurrentCard == null) return;
        VirtualMachine vm = new VirtualMachine(); 
        vm.LoadCode(string.Join("\n", Lines));
        bool success = true; string name = CurrentCard.Name;
        List<int> producedOutput = new List<int>();

        // Beállítunk valami teszt inputot
        if (TargetLevel == 2 && name == "Iron Miner") { vm.InputBuffer.Enqueue(2); vm.InputBuffer.Enqueue(-5); LastTestInput = "2, -5"; }
        else if (TargetLevel == 4 && name == "Iron Miner") { vm.InputBuffer.Enqueue(-2); vm.InputBuffer.Enqueue(5); LastTestInput = "-2, 5"; }
        else if (TargetLevel == 2 && name == "Copper Miner") { vm.InputBuffer.Enqueue(9); vm.InputBuffer.Enqueue(8); LastTestInput = "9, 8"; }
        else if (TargetLevel == 4 && name == "Copper Miner") { vm.InputBuffer.Enqueue(4); vm.InputBuffer.Enqueue(5); LastTestInput = "4, 5"; }
        else if (TargetLevel == 2 && name == "Assembler") { vm.InputBuffer.Enqueue(2); vm.InputBuffer.Enqueue(4); LastTestInput = "2, 4"; }
        else if (TargetLevel == 4 && name == "Assembler") { vm.InputBuffer.Enqueue(5); LastTestInput = "5"; }
        else if (TargetLevel == 2 && name == "Rocket Silo") { vm.InputBuffer.Enqueue(10); LastTestInput = "10"; }
        else LastTestInput = "None";

        // Futtatás (Max 200 lépés a végtelen ciklus ellen)
        int steps = 0;
        while (steps < 200 && !vm.IsHalted)
        {
            if (vm.OutputBuffer.Count > 0) {
                int val = vm.OutputBuffer.Dequeue();
                producedOutput.Add(val);
            }
            vm.Step();
            steps++;
            // Ne ragadjunk be az IN-be, ha nincs több adat a tesztben
            if (vm.IsWaiting) break; 
        }

        // Összeszedjük a végén is a maradékot
        while (vm.OutputBuffer.Count > 0) producedOutput.Add(vm.OutputBuffer.Dequeue());

        // Eredmény sztringesítése a UI-hoz
        LastTestOutput = producedOutput.Count > 0 ? string.Join(", ", producedOutput) : "Nothing";
        LastErrorMsg = vm.LastError != "" ? vm.LastError : "No errors.";

        // ELLENŐRZÉS: Helyes az output?
        try {
            if (name == "Coal Miner") {
                if (TargetLevel == 1) { if (producedOutput.Count == 0 || producedOutput[0] != 1) success = false; }
                if (TargetLevel == 2) { if (producedOutput.Count < 3 || producedOutput[0]!=1 || producedOutput[1]!=2 || producedOutput[2]!=3) success=false; }
                if (TargetLevel == 4) { if (producedOutput.Count == 0 || producedOutput[0] != 2) success=false; } // 3-1 = 2
            } else if (name == "Iron Miner") {
                if (TargetLevel == 1) { if (producedOutput.Count < 2 || producedOutput[0]!=1 || producedOutput[1]!=3) success=false; }
                if (TargetLevel == 2) { if (producedOutput.Count < 2 || producedOutput[0]!=1 || producedOutput[1]!=0) success=false; } // 2->1, -5->0
                if (TargetLevel == 4) { if (producedOutput.Count < 2 || producedOutput[0]!=1 || producedOutput[1]!=0) success=false; } // -2->1, 5->0
            } else if (name == "Copper Miner") {
                if (TargetLevel == 1) { if (producedOutput.Count == 0 || producedOutput[0] != 99) success = false; }
                if (TargetLevel == 2) { if (producedOutput.Count < 2 || producedOutput[0]!=0 || producedOutput[1]!=1) success=false; } // 9->0, 8->1
                if (TargetLevel == 4) { if (producedOutput.Count < 2 || producedOutput[0]!=0 || producedOutput[1]!=1) success=false; } // 4->0, 5->1
            } else if (name == "Smelter") {
                if (TargetLevel == 1) { if (producedOutput.Count == 0 || producedOutput[0] != 404) success = false; }
                if (TargetLevel == 2) { if (producedOutput.Count < 5 || producedOutput[0]!=2 || producedOutput[4]!=10) success=false; }
                if (TargetLevel == 4) { if (producedOutput.Count < 5 || producedOutput[0]!=1 || producedOutput[4]!=9) success=false; }
            } else if (name == "Assembler") {
                if (TargetLevel == 1) { if (producedOutput.Count == 0 || producedOutput[0] != 0) success = false; }
                if (TargetLevel == 2) { if (producedOutput.Count < 2 || producedOutput[0]!=1 || producedOutput[1]!=0) success=false; } // 2->1, 4->0
                if (TargetLevel == 4) { if (producedOutput.Count == 0 || producedOutput[0] != 15) success=false; } // 5+10=15
            } else if (name == "Rocket Silo") {
                if (TargetLevel == 1) { if (producedOutput.Count == 0 || producedOutput[0] != 100) success = false; }
                if (TargetLevel == 2) { if (producedOutput.Count == 0 || producedOutput[0] != 20) success=false; } // 10*2=20
                if (TargetLevel == 4) { if (producedOutput.Count < 5 || producedOutput[4] != 5) success=false; } // 1,1,2,3,5
            }
        } catch { success = false; }

        if (vm.Instructions.Count == 0) success = false;

        if (success) { 
            FeedbackMsg = "TEST PASSED!"; FeedbackColor = Color.Green; 
            int currentLevel = GlobalUnlocks.GetValueOrDefault(CurrentCard.Name, 0);
            GlobalUnlocks[CurrentCard.Name] = Math.Max(currentLevel, TargetLevel); 
            CurrentCard.CurrentMultiplier = Math.Max(currentLevel, TargetLevel); 
            IsTaskActive = false; 
        } else { FeedbackMsg = "FAILED! Check Logic."; FeedbackColor = Color.Red; }
    }

    private string WordWrap(string text, int fontSize, int maxWidth) {
        if (string.IsNullOrEmpty(text)) return "";
        string[] words = text.Split(new[] {' ', '\n'}, StringSplitOptions.RemoveEmptyEntries); 
        StringBuilder sb = new StringBuilder(); float lineWidth = 0;
        foreach (var word in words) {
            float wordWidth = Program.MeasureText(word + " ", fontSize);
            if (lineWidth + wordWidth > maxWidth) { sb.Append("\n"); lineWidth = 0; }
            sb.Append(word + " "); lineWidth += wordWidth;
        }
        return sb.ToString();
    }

    public void Draw(int screenW, int screenH) {
        if (!IsVisible || CurrentCard == null) return;
        Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 230));
        int margin = 50; int editorW = (screenW / 2) - margin; int editorH = screenH - (margin * 2);

        Raylib.DrawRectangle(margin, margin, editorW, editorH, new Color(20, 20, 20, 255));
        Raylib.DrawRectangleLines(margin, margin, editorW, editorH, Color.White);
        Program.DrawText($"EDITING: {CurrentCard.Name}", margin + 20, margin + 20, 35, CurrentCard.HeaderColor);

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

        int panelX = screenW / 2; int rightMargin = 50; int contentWidth = 600;
        Program.DrawText("SELECT DIFFICULTY LEVEL", panelX + rightMargin, margin + 20, 30, Color.White);
        
        DrawLevelBtn(panelX + rightMargin, 110, contentWidth, "EASY (x1) - Output", 1);
        DrawLevelBtn(panelX + rightMargin, 180, contentWidth, "NORMAL (x2) - Logic", 2);
        DrawLevelBtn(panelX + rightMargin, 250, contentWidth, "HARD (x4) - Math", 4);

        if (IsTaskActive) {
            int startY = 330;
            string wrappedDesc = WordWrap(CurrentTaskDesc, 24, contentWidth - 40);
            int goalH = 60 + (wrappedDesc.Split('\n').Length * 28) + 20;
            Raylib.DrawRectangle(panelX + rightMargin, startY, contentWidth, goalH, new Color(40, 40, 40, 255));
            Raylib.DrawRectangleLines(panelX + rightMargin, startY, contentWidth, goalH, Color.Yellow);
            Program.DrawText("GOAL:", panelX + rightMargin + 20, startY + 15, 24, Color.Yellow);
            Program.DrawText(CurrentTaskTitle, panelX + rightMargin + 20, startY + 45, 28, Color.White);
            int dy = startY + 80; foreach (var line in wrappedDesc.Split('\n')) { Program.DrawText(line, panelX + rightMargin + 20, dy, 24, Color.LightGray); dy += 28; }

            // TEST RESULTS PANEL
            int testY = startY + goalH + 20;
            Raylib.DrawRectangle(panelX + rightMargin, testY, contentWidth, 120, new Color(20, 20, 30, 255));
            Raylib.DrawRectangleLines(panelX + rightMargin, testY, contentWidth, 120, Color.Blue);
            Program.DrawText($"Test Input: {LastTestInput}", panelX + rightMargin + 20, testY + 15, 20, Color.LightGray);
            Program.DrawText($"Your Output: {LastTestOutput}", panelX + rightMargin + 20, testY + 45, 20, Color.White);
            Color errCol = LastErrorMsg == "No errors." ? Color.Green : Color.Red;
            Program.DrawText($"Status: {LastErrorMsg}", panelX + rightMargin + 20, testY + 80, 20, errCol);

            int btnY = testY + 140;
            Raylib.DrawRectangle(panelX + rightMargin, btnY, 200, 60, new Color(0, 100, 0, 255));
            Program.DrawText("RUN TESTS", panelX + rightMargin + 30, btnY + 15, 26, Color.White);
            if (!string.IsNullOrEmpty(FeedbackMsg)) Program.DrawText(FeedbackMsg, panelX + rightMargin + 220, btnY + 15, 30, FeedbackColor);
        } else {
            // COMMAND REFERENCE (Ide került a kért i/tooltip szerű infó!)
            int refY = 330;
            Program.DrawText("COMMAND REFERENCE (How to code)", panelX + rightMargin, refY, 28, Color.Gold);
            Raylib.DrawRectangle(panelX + rightMargin, refY + 40, contentWidth, 540, new Color(30, 30, 40, 255));
            Raylib.DrawRectangleLines(panelX + rightMargin, refY + 40, contentWidth, 540, Color.Gray);
            
            string[] cmds = { 
                "AX, BX, CX - Variables (Registers) to store numbers.",
                "MOV A,B  - Overwrites A with B. (e.g. MOV AX, 5 -> AX is 5)", 
                "ADD A,B  - Adds B to A. (e.g. ADD AX, 2 -> AX is 7)", 
                "SUB A,B  - Subtracts B from A.", 
                "MUL A,B  - Multiplies A by B.", 
                "DIV A,B  - Divides A by B. (Fails if B is 0!)", 
                "IN A     - Reads a number from the game into A.", 
                "OUT A    - Sends the number in A to the game.", 
                "CMP A,B  - Compares A and B. Sets a hidden flag.", 
                "JE LBL   - Jumps to LBL if A == B (after CMP).", 
                "JG LBL   - Jumps to LBL if A > B (after CMP).",
                "JL LBL   - Jumps to LBL if A < B (after CMP).",
                "JMP LBL  - Jumps always (used for infinite loops).", 
                "LBL:     - Defines a line name (e.g. START: )" 
            };
            
            int cy = refY + 60; 
            foreach(var c in cmds) { 
                string[] parts = c.Split(new[] { " - " }, StringSplitOptions.None);
                Program.DrawText(parts[0], panelX + rightMargin + 20, cy, 20, Color.Yellow);
                if (parts.Length > 1) Program.DrawText("- " + parts[1], panelX + rightMargin + 130, cy, 18, Color.LightGray);
                cy+=32; 
            }
        }
        cursorTimer += Raylib.GetFrameTime(); if (cursorTimer >= 0.5f) { cursorTimer = 0; cursorVisible = !cursorVisible; }
    }

    private void DrawLevelBtn(int x, int y, int w, string text, int level) {
        if (CurrentCard == null) return;
        int unlocked = GlobalUnlocks.ContainsKey(CurrentCard.Name) ? GlobalUnlocks[CurrentCard.Name] : 0;
        Color col = (unlocked >= level) ? Color.DarkGreen : Color.DarkGray;
        if (IsTaskActive && TargetLevel == level) col = Color.Blue; 

        Raylib.DrawRectangle(x, y, w, 60, col);
        Program.DrawText(text, x + 20, y + 15, 26, Color.White);
        if (unlocked >= level) Program.DrawText("UNLOCKED", x + w - 150, y + 15, 26, Color.Lime);
    }
}