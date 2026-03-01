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

        // --- SZERKESZTŐ LOGIKA (VÁLTOZATLAN) ---
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

        // --- GOMBOK ---
        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            Vector2 mouse = Raylib.GetMousePosition();
            int screenW = Raylib.GetScreenWidth();
            int panelX = screenW / 2;

            if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(panelX + 50, 100, 600, 60))) StartTask(1); 
            else if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(panelX + 50, 170, 600, 60))) StartTask(2); 
            else if (Raylib.CheckCollisionPointRec(mouse, new Rectangle(panelX + 50, 240, 600, 60))) StartTask(4); 

            if (IsTaskActive && Raylib.CheckCollisionPointRec(mouse, new Rectangle(panelX + 50, 750, 200, 60))) VerifyCode();
        }
    }

    // =========================================================
    // 18 QUEST DEFINÍCIÓJA (Gép típusonként)
    // =========================================================
    private void StartTask(int level)
    {
        if (CurrentCard == null) return;
        TargetLevel = level; IsTaskActive = true; FeedbackMsg = "";
        
        string name = CurrentCard.Name;

        // --- COAL MINER ---
        if (name == "Coal Miner") {
            if (level == 1) { // Easy
                CurrentTaskTitle = "COAL EASY: Constant Flow";
                CurrentTaskDesc = "Output the number 1 repeatedly to represent mining coal.";
                CurrentTaskTutorial = "SYNTAX: MOV AX, 1 -> OUT AX -> JMP START";
            } else if (level == 2) { // Normal
                CurrentTaskTitle = "COAL NORMAL: Batch Mining";
                CurrentTaskDesc = "Output the number 5 repeatedly (Batch of 5 coal).";
                CurrentTaskTutorial = "Change the value in MOV. Use MOV AX, 5.";
            } else if (level == 4) { // Hard
                CurrentTaskTitle = "COAL HARD: On/Off Switch";
                CurrentTaskDesc = "Output 1, then Output 0, repeat. (Toggle flow).";
                CurrentTaskTutorial = "LOGIC: OUT 1 -> OUT 0 -> JMP START.";
            }
        }
        // --- STONE MINER ---
        else if (name == "Stone Miner") {
            if (level == 1) { // Easy
                CurrentTaskTitle = "STONE EASY: Output 2";
                CurrentTaskDesc = "Output the number 2 repeatedly.";
                CurrentTaskTutorial = "Use MOV AX, 2 and OUT AX.";
            } else if (level == 2) { // Normal
                CurrentTaskTitle = "STONE NORMAL: Counter";
                CurrentTaskDesc = "Output 1, then 2, then 3... incrementing forever.";
                CurrentTaskTutorial = "LOGIC: MOV AX, 1 -> LABEL: OUT AX -> ADD AX, 1 -> JMP LABEL";
            } else if (level == 4) { // Hard
                CurrentTaskTitle = "STONE HARD: Countdown";
                CurrentTaskDesc = "Start at 5. Output 5, 4, 3, 2, 1. Then reset to 5.";
                CurrentTaskTutorial = "LOGIC: Set 5. Loop: Out, Sub 1. CMP 0. If Equal -> Reset.";
            }
        }
        // --- IRON MINER ---
        else if (name == "Iron Miner") {
            if (level == 1) { // Easy
                CurrentTaskTitle = "IRON EASY: Heavy Ore";
                CurrentTaskDesc = "Output the number 10 repeatedly.";
                CurrentTaskTutorial = "MOV AX, 10 -> OUT AX -> LOOP.";
            } else if (level == 2) { // Normal
                CurrentTaskTitle = "IRON NORMAL: Accumulator";
                CurrentTaskDesc = "Output 1, 3, 6, 10... (Add 1, then add 2, then add 3 to total).";
                CurrentTaskTutorial = "Use two registers! AX for total, BX for counter. ADD AX, BX.";
            } else if (level == 4) { // Hard
                CurrentTaskTitle = "IRON HARD: Even Numbers";
                CurrentTaskDesc = "Output 2, 4, 6, 8... (Only even numbers).";
                CurrentTaskTutorial = "Start at 2. Output. Add 2. Repeat.";
            }
        }
        // --- COPPER MINER ---
        else if (name == "Copper Miner") {
            if (level == 1) { // Easy
                CurrentTaskTitle = "COPPER EASY: Output 3";
                CurrentTaskDesc = "Output the number 3 repeatedly.";
                CurrentTaskTutorial = "Standard loop with MOV AX, 3.";
            } else if (level == 2) { // Normal
                CurrentTaskTitle = "COPPER NORMAL: Odd Numbers";
                CurrentTaskDesc = "Output 1, 3, 5, 7...";
                CurrentTaskTutorial = "Start at 1. Output. Add 2. Repeat.";
            } else if (level == 4) { // Hard
                CurrentTaskTitle = "COPPER HARD: Powers of 2";
                CurrentTaskDesc = "Output 2, 4, 8, 16, 32...";
                CurrentTaskTutorial = "Start at 2. Output. MUL AX, 2. Repeat.";
            }
        }
        // --- SMELTER ---
        else if (name == "Smelter") {
            if (level == 1) { // Easy
                CurrentTaskTitle = "SMELT EASY: Identity";
                CurrentTaskDesc = "Read Input. Output the same value.";
                CurrentTaskTutorial = "IN AX -> OUT AX -> JMP START.";
            } else if (level == 2) { // Normal
                CurrentTaskTitle = "SMELT NORMAL: Double Efficiency";
                CurrentTaskDesc = "Read Input. Output Input * 2.";
                CurrentTaskTutorial = "IN AX -> MUL AX, 2 -> OUT AX -> LOOP.";
            } else if (level == 4) { // Hard
                CurrentTaskTitle = "SMELT HARD: Quality Filter";
                CurrentTaskDesc = "Read Input. If Input > 5, Output it. Otherwise Output 0.";
                CurrentTaskTutorial = "IN AX -> CMP AX, 5 -> JG (Jump Greater) OK -> OUT 0 -> JMP START -> LABEL OK: OUT AX.";
            }
        }
        // --- ASSEMBLER ---
        else if (name == "Assembler") {
            if (level == 1) { // Easy
                CurrentTaskTitle = "ASM EASY: Add Header";
                CurrentTaskDesc = "Read Input. Add 10. Output Result.";
                CurrentTaskTutorial = "IN AX -> ADD AX, 10 -> OUT AX -> LOOP.";
            } else if (level == 2) { // Normal
                CurrentTaskTitle = "ASM NORMAL: Decrement";
                CurrentTaskDesc = "Read Input. Output Input, then Output Input - 1.";
                CurrentTaskTutorial = "IN AX -> OUT AX -> SUB AX, 1 -> OUT AX -> LOOP.";
            } else if (level == 4) { // Hard
                CurrentTaskTitle = "ASM HARD: Batch Sum (2 Inputs)";
                CurrentTaskDesc = "Read TWO inputs. Add them together. Output the sum.";
                CurrentTaskTutorial = "IN AX -> IN BX -> ADD AX, BX -> OUT AX -> LOOP.";
            }
        }
    }

    // =========================================================
    // 18 ELLENŐRZŐ LOGIKA (Validáció)
    // =========================================================
    private void VerifyCode()
    {
        if (CurrentCard == null) return;
        
        VirtualMachine vm = new VirtualMachine();
        vm.LoadCode(string.Join("\n", Lines));
        bool success = true;
        string name = CurrentCard.Name;

        // --- COAL MINER ---
        if (name == "Coal Miner") {
            if (TargetLevel == 1) { // Out 1
                for(int i=0; i<5; i++) { vm.Step(); if(vm.OutputBuffer.Count>0 && vm.OutputBuffer.Dequeue()!=1) success=false; }
            } else if (TargetLevel == 2) { // Out 5
                for(int i=0; i<5; i++) { vm.Step(); if(vm.OutputBuffer.Count>0 && vm.OutputBuffer.Dequeue()!=5) success=false; }
            } else if (TargetLevel == 4) { // 1, 0, 1, 0
                int[] expected = {1, 0, 1, 0}; int found=0;
                for(int i=0; i<20; i++) { vm.Step(); if(vm.OutputBuffer.Count>0) { if(vm.OutputBuffer.Dequeue()!=expected[found%2]) success=false; found++; } }
                if(found==0) success=false;
            }
        }
        // --- STONE MINER ---
        else if (name == "Stone Miner") {
            if (TargetLevel == 1) { // Out 2
                for(int i=0; i<5; i++) { vm.Step(); if(vm.OutputBuffer.Count>0 && vm.OutputBuffer.Dequeue()!=2) success=false; }
            } else if (TargetLevel == 2) { // 1, 2, 3...
                int next=1;
                for(int i=0; i<20; i++) { vm.Step(); if(vm.OutputBuffer.Count>0) { if(vm.OutputBuffer.Dequeue()!=next) success=false; next++; } }
            } else if (TargetLevel == 4) { // 5, 4, 3, 2, 1, 5...
                int[] seq = {5,4,3,2,1}; int idx=0;
                for(int i=0; i<30; i++) { vm.Step(); if(vm.OutputBuffer.Count>0) { if(vm.OutputBuffer.Dequeue()!=seq[idx%5]) success=false; idx++; } }
            }
        }
        // --- IRON MINER ---
        else if (name == "Iron Miner") {
            if (TargetLevel == 1) { // Out 10
                for(int i=0; i<5; i++) { vm.Step(); if(vm.OutputBuffer.Count>0 && vm.OutputBuffer.Dequeue()!=10) success=false; }
            } else if (TargetLevel == 2) { // 1, 3, 6, 10
                int[] seq = {1,3,6,10}; int idx=0;
                for(int i=0; i<30; i++) { vm.Step(); if(vm.OutputBuffer.Count>0 && idx<4) { if(vm.OutputBuffer.Dequeue()!=seq[idx]) success=false; idx++; } }
            } else if (TargetLevel == 4) { // 2, 4, 6...
                int next=2;
                for(int i=0; i<20; i++) { vm.Step(); if(vm.OutputBuffer.Count>0) { if(vm.OutputBuffer.Dequeue()!=next) success=false; next+=2; } }
            }
        }
        // --- COPPER MINER ---
        else if (name == "Copper Miner") {
            if (TargetLevel == 1) { // Out 3
                for(int i=0; i<5; i++) { vm.Step(); if(vm.OutputBuffer.Count>0 && vm.OutputBuffer.Dequeue()!=3) success=false; }
            } else if (TargetLevel == 2) { // 1, 3, 5...
                int next=1;
                for(int i=0; i<20; i++) { vm.Step(); if(vm.OutputBuffer.Count>0) { if(vm.OutputBuffer.Dequeue()!=next) success=false; next+=2; } }
            } else if (TargetLevel == 4) { // 2, 4, 8, 16
                int next=2;
                for(int i=0; i<20; i++) { vm.Step(); if(vm.OutputBuffer.Count>0) { if(vm.OutputBuffer.Dequeue()!=next) success=false; next*=2; } }
            }
        }
        // --- SMELTER ---
        else if (name == "Smelter") {
            if (TargetLevel == 1) { // In=Out
                vm.InputBuffer.Enqueue(5); int s=0; while(vm.OutputBuffer.Count==0 && s++<50) vm.Step();
                if(vm.OutputBuffer.Count==0 || vm.OutputBuffer.Dequeue()!=5) success=false;
            } else if (TargetLevel == 2) { // Out=In*2
                vm.InputBuffer.Enqueue(5); int s=0; while(vm.OutputBuffer.Count==0 && s++<50) vm.Step();
                if(vm.OutputBuffer.Count==0 || vm.OutputBuffer.Dequeue()!=10) success=false;
            } else if (TargetLevel == 4) { // Filter > 5
                vm.InputBuffer.Enqueue(3); int s=0; while(vm.OutputBuffer.Count==0 && s++<50) vm.Step();
                if(vm.OutputBuffer.Count==0 || vm.OutputBuffer.Dequeue()!=0) success=false; // 3->0
                vm.InputBuffer.Enqueue(8); s=0; while(vm.OutputBuffer.Count==0 && s++<50) vm.Step();
                if(vm.OutputBuffer.Count==0 || vm.OutputBuffer.Dequeue()!=8) success=false; // 8->8
            }
        }
        // --- ASSEMBLER ---
        else if (name == "Assembler") {
            if (TargetLevel == 1) { // In+10
                vm.InputBuffer.Enqueue(5); int s=0; while(vm.OutputBuffer.Count==0 && s++<50) vm.Step();
                if(vm.OutputBuffer.Count==0 || vm.OutputBuffer.Dequeue()!=15) success=false;
            } else if (TargetLevel == 2) { // In, In-1
                vm.InputBuffer.Enqueue(5); int s=0; while(vm.OutputBuffer.Count<2 && s++<100) vm.Step();
                if(vm.OutputBuffer.Count<2 || vm.OutputBuffer.Dequeue()!=5 || vm.OutputBuffer.Dequeue()!=4) success=false;
            } else if (TargetLevel == 4) { // In1 + In2
                vm.InputBuffer.Enqueue(5); vm.InputBuffer.Enqueue(3); // 5+3=8
                int s=0; while(vm.OutputBuffer.Count==0 && s++<100) vm.Step();
                if(vm.OutputBuffer.Count==0 || vm.OutputBuffer.Dequeue()!=8) success=false;
            }
        }

        // --- HA NEM TERMELT SEMMIT, AZ HIBA ---
        if (vm.Instructions.Count == 0) success = false;

        if (success) { 
            FeedbackMsg = "TEST PASSED!"; FeedbackColor = Color.Green; 
            GlobalUnlocks[CurrentCard.Name] = TargetLevel; CurrentCard.CurrentMultiplier = TargetLevel; IsTaskActive = false; 
        } else { FeedbackMsg = "FAILED! Check Logic."; FeedbackColor = Color.Red; }
    }

    private string WordWrap(string text, int fontSize, int maxWidth)
    {
        if (string.IsNullOrEmpty(text)) return "";
        string[] words = text.Split(new[] {' ', '\n'}, StringSplitOptions.RemoveEmptyEntries); 
        StringBuilder sb = new StringBuilder(); float lineWidth = 0;
        foreach (var word in words) {
            float wordWidth = Raylib.MeasureText(word + " ", fontSize);
            if (lineWidth + wordWidth > maxWidth) { sb.Append("\n"); lineWidth = 0; }
            sb.Append(word + " "); lineWidth += wordWidth;
        }
        return sb.ToString();
    }

    public void Draw(int screenW, int screenH)
    {
        if (!IsVisible || CurrentCard == null) return;

        Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 220));
        int margin = 50; int editorW = (screenW / 2) - margin; int editorH = screenH - (margin * 2);

        // EDITOR
        Raylib.DrawRectangle(margin, margin, editorW, editorH, new Color(20, 20, 20, 255));
        Raylib.DrawRectangleLines(margin, margin, editorW, editorH, Color.White);
        Raylib.DrawText($"EDITING: {CurrentCard.Name}", margin + 20, margin + 20, 30, CurrentCard.HeaderColor);

        int lineY = margin + 80;
        for (int i = 0; i < Lines.Count; i++) {
            Raylib.DrawText($"{i+1}.", margin + 15, lineY, 20, Color.Gray);
            Raylib.DrawText(Lines[i], margin + 60, lineY, 20, Color.White);
            if (i == CursorRow && cursorVisible) {
                int textW = Raylib.MeasureText(Lines[i].Substring(0, CursorCol), 20);
                Raylib.DrawRectangle(margin + 60 + textW, lineY, 2, 20, Color.Green);
            }
            lineY += 25;
        }

        // PANEL
        int panelX = screenW / 2; int rightMargin = 50; int contentWidth = 600;
        Raylib.DrawText("SELECT DIFFICULTY LEVEL", panelX + rightMargin, margin + 20, 30, Color.White);
        DrawLevelBtn(panelX + rightMargin, 100, contentWidth, "EASY (x1)", 1);
        DrawLevelBtn(panelX + rightMargin, 170, contentWidth, "NORMAL (x2)", 2);
        DrawLevelBtn(panelX + rightMargin, 240, contentWidth, "HARD (x4)", 4);

        if (IsTaskActive) {
            int startY = 320;
            // Goal
            string wrappedDesc = WordWrap(CurrentTaskDesc, 20, contentWidth - 40);
            int goalH = 60 + (wrappedDesc.Split('\n').Length * 25) + 20;
            Raylib.DrawRectangle(panelX + rightMargin, startY, contentWidth, goalH, new Color(40, 40, 40, 255));
            Raylib.DrawRectangleLines(panelX + rightMargin, startY, contentWidth, goalH, Color.Yellow);
            Raylib.DrawText("GOAL:", panelX + rightMargin + 20, startY + 15, 20, Color.Yellow);
            Raylib.DrawText(CurrentTaskTitle, panelX + rightMargin + 20, startY + 40, 25, Color.White);
            int dy = startY + 70; foreach (var line in wrappedDesc.Split('\n')) { Raylib.DrawText(line, panelX + rightMargin + 20, dy, 20, Color.LightGray); dy += 25; }

            // Tutorial
            int tutY = startY + goalH + 20;
            string wrappedTut = WordWrap(CurrentTaskTutorial, 20, contentWidth - 40);
            int tutH = 20 + (wrappedTut.Split('\n').Length * 25) + 20;
            Raylib.DrawRectangle(panelX + rightMargin, tutY, contentWidth, tutH, new Color(20, 20, 30, 255));
            Raylib.DrawRectangleLines(panelX + rightMargin, tutY, contentWidth, tutH, Color.Blue);
            int ty = tutY + 20; foreach (var line in wrappedTut.Split('\n')) { Raylib.DrawText(line, panelX + rightMargin + 20, ty, 20, Color.White); ty += 25; }

            // Run Btn
            int btnY = 750;
            Raylib.DrawRectangle(panelX + rightMargin, btnY, 200, 60, new Color(0, 100, 0, 255));
            Raylib.DrawText("RUN TESTS", panelX + rightMargin + 30, btnY + 20, 20, Color.White);
            if (!string.IsNullOrEmpty(FeedbackMsg)) Raylib.DrawText(FeedbackMsg, panelX + rightMargin + 220, btnY + 20, 25, FeedbackColor);
        } else {
            // Command List
            int refY = 320;
            Raylib.DrawText("COMMAND REFERENCE", panelX + rightMargin, refY, 25, Color.Gold);
            Raylib.DrawRectangle(panelX + rightMargin, refY + 40, contentWidth, 500, new Color(30, 30, 40, 255));
            Raylib.DrawRectangleLines(panelX + rightMargin, refY + 40, contentWidth, 500, Color.Gray);
            string[] cmds = { "MOV A,B - Set A to B", "ADD A,B - Add B to A", "SUB A,B - Sub B from A", "MUL A,B - Mul A by B", "DIV A,B - Div A by B", "IN A - Read Input", "OUT A - Send Output", "CMP A,B - Compare", "JE LBL - Jump Equal", "JG/JL - Jump > / <", "JMP LBL - Jump Always", "LBL: - Label" };
            int cy = refY + 60; foreach(var c in cmds) { Raylib.DrawText(c, panelX+rightMargin+20, cy, 20, Color.White); cy+=35; }
        }
        cursorTimer += Raylib.GetFrameTime(); if (cursorTimer >= 0.5f) { cursorTimer = 0; cursorVisible = !cursorVisible; }
    }

    private void DrawLevelBtn(int x, int y, int w, string text, int level) {
        if (CurrentCard == null) return;
        int unlocked = GlobalUnlocks.ContainsKey(CurrentCard.Name) ? GlobalUnlocks[CurrentCard.Name] : 0;
        Color col = (unlocked >= level) ? Color.DarkGreen : Color.DarkGray;
        if (IsTaskActive && TargetLevel == level) col = Color.Blue; 
        Raylib.DrawRectangle(x, y, w, 60, col);
        Raylib.DrawText(text, x + 20, y + 20, 20, Color.White);
        if (unlocked >= level) Raylib.DrawText("UNLOCKED", x + w - 120, y + 20, 20, Color.Lime);
    }
}