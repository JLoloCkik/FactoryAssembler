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

    // FIX KOORDINÁTÁK
    private const int Y_LVL_1 = 80;
    private const int Y_LVL_2 = 140;
    private const int Y_LVL_3 = 200;
    private const int Y_GOAL = 270;
    private const int H_GOAL = 140; // Megnövelt hely!
    private const int Y_TEST = 420;
    private const int H_TEST = 120;
    private const int Y_RUN = 550;
    private const int H_RUN = 60;

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
            Vector2 mouse = Raylib.GetMousePosition(); int panelX = Raylib.GetScreenWidth() / 2; int rightMargin = 50;
            
            if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(panelX + rightMargin, Y_LVL_1, 600, 60))) StartTask(1); 
            else if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(panelX + rightMargin, Y_LVL_2, 600, 60))) StartTask(2); 
            else if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(panelX + rightMargin, Y_LVL_3, 600, 60))) StartTask(4); 

            if (IsTaskActive && Raylib.CheckCollisionPointRec(mouse, new Rectangle(panelX + rightMargin, Y_RUN, 200, H_RUN))) VerifyCode();
        }
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

        LastTestOutput = producedOutput.Count > 0 ? string.Join(", ", producedOutput) : "Nothing";
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
            FeedbackMsg = "TEST PASSED!"; FeedbackColor = Color.Green; 
            int currentLevel = GlobalUnlocks.GetValueOrDefault(CurrentCard.Name, 0);
            GlobalUnlocks[CurrentCard.Name] = Math.Max(currentLevel, TargetLevel); 
            CurrentCard.CurrentMultiplier = Math.Max(currentLevel, TargetLevel); 
            IsTaskActive = false; 
        } else { FeedbackMsg = "FAILED! Check Logic."; FeedbackColor = Color.Red; }
    }

    public void Draw(int screenW, int screenH) {
        if (!IsVisible || CurrentCard == null) return;
        Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 230));
        
        int margin = 50; int editorW = (screenW / 2) - margin; int editorH = screenH - (margin * 2);

        // --- BAL OLDAL (EDITOR) + VÁGÓMASZK (SCISSOR MODE) ---
        Raylib.DrawRectangle(margin, margin, editorW, editorH, new Color(20, 20, 20, 255));
        Raylib.DrawRectangleLines(margin, margin, editorW, editorH, Color.White);
        Program.DrawText($"EDITING: {CurrentCard.Name}", margin + 20, margin + 20, 35, CurrentCard.HeaderColor);

        // Vágómaszk: innen kezdve semmi nem rajzolódik a dobozon kívül!
        Raylib.BeginScissorMode(margin + 5, margin + 70, editorW - 10, editorH - 80);
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
        Raylib.EndScissorMode(); // Vágómaszk vége

        // --- JOBB OLDAL ---
        int panelX = screenW / 2; int rightMargin = 50; int contentWidth = 600;
        Program.DrawText("SELECT DIFFICULTY LEVEL", panelX + rightMargin, margin + 20, 30, Color.White);
        
        DrawLevelBtn(panelX + rightMargin, Y_LVL_1, contentWidth, "EASY (x1) - Output", 1);
        DrawLevelBtn(panelX + rightMargin, Y_LVL_2, contentWidth, "NORMAL (x2) - Logic", 2);
        DrawLevelBtn(panelX + rightMargin, Y_LVL_3, contentWidth, "HARD (x4) - Math", 4);

        int refY = Y_GOAL; // Command ref kezdete, ha nincs aktív task

        if (IsTaskActive) {
            // GOAL DOBOZ (Vágómaszkkal)
            Raylib.DrawRectangle(panelX + rightMargin, Y_GOAL, contentWidth, H_GOAL, new Color(40, 40, 40, 255));
            Raylib.DrawRectangleLines(panelX + rightMargin, Y_GOAL, contentWidth, H_GOAL, Color.Yellow);
            Program.DrawText("GOAL:", panelX + rightMargin + 20, Y_GOAL + 15, 24, Color.Yellow);
            
            Raylib.BeginScissorMode(panelX + rightMargin, Y_GOAL + 40, contentWidth, H_GOAL - 40);
            Program.DrawText(CurrentTaskTitle, panelX + rightMargin + 20, Y_GOAL + 45, 26, Color.White);
            string wrappedDesc = Program.WordWrap(CurrentTaskDesc, 22, contentWidth - 40);
            int dy = Y_GOAL + 80; foreach (var line in wrappedDesc.Split('\n')) { Program.DrawText(line, panelX + rightMargin + 20, dy, 22, Color.LightGray); dy += 25; }
            Raylib.EndScissorMode();

            // TEST RESULTS DOBOZ (Vágómaszkkal)
            Raylib.DrawRectangle(panelX + rightMargin, Y_TEST, contentWidth, H_TEST, new Color(20, 20, 30, 255));
            Raylib.DrawRectangleLines(panelX + rightMargin, Y_TEST, contentWidth, H_TEST, Color.Blue);
            
            Raylib.BeginScissorMode(panelX + rightMargin, Y_TEST, contentWidth, H_TEST);
            Program.DrawText($"Test Input: {LastTestInput}", panelX + rightMargin + 20, Y_TEST + 15, 22, Color.LightGray);
            Program.DrawText($"Your Output: {LastTestOutput}", panelX + rightMargin + 20, Y_TEST + 45, 22, Color.White);
            Color errCol = LastErrorMsg == "No errors." ? Color.Green : Color.Red;
            Program.DrawText($"Status: {LastErrorMsg}", panelX + rightMargin + 20, Y_TEST + 75, 22, errCol);
            Raylib.EndScissorMode();

            // RUN BUTTON
            Raylib.DrawRectangle(panelX + rightMargin, Y_RUN, 200, H_RUN, new Color(0, 100, 0, 255));
            Program.DrawText("RUN TESTS", panelX + rightMargin + 30, Y_RUN + 15, 26, Color.White);
            if (!string.IsNullOrEmpty(FeedbackMsg)) Program.DrawText(FeedbackMsg, panelX + rightMargin + 220, Y_RUN + 15, 30, FeedbackColor);

            refY = Y_RUN + H_RUN + 20; // Command list csúsztatása a gomb alá
        } 

        // COMMAND REFERENCE (Vágómaszkkal, így alul sem fog soha kilógni a képernyőről)
        int refH = screenH - refY - margin; 
        Program.DrawText("COMMAND REFERENCE (How to code)", panelX + rightMargin, refY, 26, Color.Gold);
        Raylib.DrawRectangle(panelX + rightMargin, refY + 35, contentWidth, refH - 35, new Color(30, 30, 40, 255));
        Raylib.DrawRectangleLines(panelX + rightMargin, refY + 35, contentWidth, refH - 35, Color.Gray);
        
        Raylib.BeginScissorMode(panelX + rightMargin, refY + 35, contentWidth, refH - 35);
        string[] cmds = { 
            "AX, BX, CX : Variables (Registers)", "MOV A, B   : Sets A to B. (MOV AX, 5)", 
            "ADD A, B   : Adds B to A. (ADD AX, 2)", "SUB A, B   : Subtracts B from A.", 
            "MUL A, B   : Multiplies A by B.", "DIV A, B   : Divides A by B.", 
            "IN A       : Reads input to A.", "OUT A      : Sends A to output.", 
            "CMP A, B   : Compares A and B.", "JE LBL     : Jumps to LBL if A == B.", 
            "JG LBL     : Jumps if A > B.", "JL LBL     : Jumps if A < B.",
            "JMP LBL    : Jumps always.", "LBL:       : Defines a line name." 
        };
        
        int fontSizeCmd = IsTaskActive ? 16 : 20;
        int spacingCmd = IsTaskActive ? 22 : 28;
        int cy = refY + 45; 
        foreach(var c in cmds) { 
            string[] parts = c.Split(new[] { " : " }, StringSplitOptions.None);
            Program.DrawText(parts[0], panelX + rightMargin + 10, cy, fontSizeCmd, Color.Yellow);
            if (parts.Length > 1) Program.DrawText("- " + parts[1], panelX + rightMargin + 140, cy, fontSizeCmd, Color.LightGray);
            cy += spacingCmd; 
        }
        Raylib.EndScissorMode();
        
        cursorTimer += Raylib.GetFrameTime(); if (cursorTimer >= 0.5f) { cursorTimer = 0; cursorVisible = !cursorVisible; }
    }

    private void DrawLevelBtn(int x, int y, int w, string text, int level) {
        if (CurrentCard == null) return;
        int unlocked = GlobalUnlocks.ContainsKey(CurrentCard.Name) ? GlobalUnlocks[CurrentCard.Name] : 0;
        Color col = (unlocked >= level) ? Color.DarkGreen : Color.DarkGray;
        if (IsTaskActive && TargetLevel == level) col = Color.Blue; 

        Raylib.DrawRectangle(x, y, w, 50, col);
        Program.DrawText(text, x + 20, y + 12, 24, Color.White);
        if (unlocked >= level) Program.DrawText("UNLOCKED", x + w - 140, y + 12, 24, Color.Lime);
    }
}