using System.Numerics;
using System.Collections.Generic;
using Raylib_cs;

namespace FactoryAssembler;

public class Card
{
    public string Name { get; set; }
    public int GridX { get; set; }
    public int GridY { get; set; }
    public Color HeaderColor { get; set; }
    
    public int WidthSlots { get; set; } = 1;
    public int HeightSlots { get; set; } = 1;
    
    public int CurrentMultiplier { get; set; } = 1;
    public bool HasMissingMaterials { get; set; } = false;

    public VirtualMachine VM { get; private set; }

    public Card(string name, int x, int y, Color color)
    {
        Name = name; GridX = x; GridY = y; HeaderColor = color;
        VM = new VirtualMachine();
        
        VM.OnOutput += (val) => 
        {
            int unlockedLevel = GameState.GlobalUnlocks.ContainsKey(Name) ? GameState.GlobalUnlocks[Name] : 0;
            if (unlockedLevel >= 1) ProduceItem(val);
        };
    }

    private void ProduceItem(int val)
    {
        HasMissingMaterials = false; 

        if (Name == "Coal Miner") GameState.Inventory["Coal"]++;
        else if (Name == "Iron Miner") GameState.Inventory["Iron Ore"]++;
        else if (Name == "Copper Miner") GameState.Inventory["Copper Ore"]++;
        else if (Name == "Smelter") {
            if (GameState.Inventory["Iron Ore"] > 0 && GameState.Inventory["Coal"] > 0) {
                GameState.Inventory["Iron Ore"]--; GameState.Inventory["Coal"]--; GameState.Inventory["Iron Ingot"]++;
            } else if (GameState.Inventory["Copper Ore"] > 0 && GameState.Inventory["Coal"] > 0) {
                GameState.Inventory["Copper Ore"]--; GameState.Inventory["Coal"]--; GameState.Inventory["Copper Ingot"]++;
            } else HasMissingMaterials = true; 
        }
        else if (Name == "Assembler") {
            if (GameState.Inventory["Iron Ingot"] >= 2) {
                GameState.Inventory["Iron Ingot"] -= 2; GameState.Inventory["Gear"]++;
            } else HasMissingMaterials = true;
        }
        else if (Name == "Rocket Silo") {
            if (GameState.Inventory["Gear"] >= 10 && GameState.Inventory["Copper Ingot"] >= 10) {
                GameState.Inventory["Gear"] -= 10; GameState.Inventory["Copper Ingot"] -= 10; GameState.Inventory["Rocket"]++;
            } else HasMissingMaterials = true;
        }
    }

    public void Draw(int cellWidth, int cellHeight)
    {
        float drawX = (GridX * cellWidth) + 10; 
        float drawY = (GridY * cellHeight) + 10;
        float cardW = (cellWidth * WidthSlots) - 20; 
        float cardH = (cellHeight * HeightSlots) - 20;

        Raylib.DrawRectangleRounded(new Rectangle(drawX + 8, drawY + 8, cardW, cardH), 0.05f, 10, new Color(0,0,0,100));
        Rectangle bodyRect = new Rectangle(drawX, drawY, cardW, cardH);
        Raylib.DrawRectangleRounded(bodyRect, 0.05f, 10, new Color(40, 44, 52, 255)); 

        float headerHeight = 50;
        Raylib.DrawRectangleRounded(new Rectangle(drawX, drawY, cardW, headerHeight), 0.05f, 10, HeaderColor);
        Raylib.DrawRectangle((int)drawX, (int)drawY + 20, (int)cardW, (int)headerHeight - 20, HeaderColor); 
        Raylib.DrawRectangleRoundedLines(bodyRect, 0.05f, 10, Color.LightGray);

        if (Name == "MARKET")
        {
            Program.DrawText("GLOBAL MARKET", drawX + 20, drawY + 15, 30, Color.White);
            string hubText = "Automatically receives all produced items.\n\nProvides materials for Crafting and Quests.";
            string wrappedText = Program.WordWrap(hubText, 22, cardW - 40);
            int textY = (int)drawY + 80;
            foreach(var line in wrappedText.Split('\n')) {
                Program.DrawText(line, drawX + 20, textY, 22, Color.LightGray);
                textY += 30;
            }
            Program.DrawText("RIGHT-CLICK TO SELL", drawX + 20, drawY + cardH - 40, 24, Color.Lime);
            return; 
        }

        int portY = (int)drawY + (int)(cardH / 2) + 20;
        Raylib.DrawCircle((int)drawX, portY, 10, Color.Black); Raylib.DrawCircle((int)drawX, portY, 6, Color.White); 
        Raylib.DrawRectangle((int)drawX + 15, portY - 10, 40, 25, Color.Black);
        Program.DrawText(VM.InputBuffer.Count.ToString(), drawX + 25, portY - 5, 20, Color.Yellow);

        Raylib.DrawCircle((int)drawX + (int)cardW, portY, 10, Color.Black); Raylib.DrawCircle((int)drawX + (int)cardW, portY, 6, Color.White);
        Raylib.DrawRectangle((int)drawX + (int)cardW - 55, portY - 10, 40, 25, Color.Black);
        Program.DrawText(VM.OutputBuffer.Count.ToString(), drawX + cardW - 45, portY - 5, 20, Color.Green);

        int fontSize = 24; int textW = Program.MeasureText(Name, fontSize);
        Program.DrawText(Name, drawX + (cardW / 2) - (textW / 2), drawY + (headerHeight / 2) - (fontSize / 2), fontSize, Color.White);
        
        int unlockedLvl = GameState.GlobalUnlocks.ContainsKey(Name) ? GameState.GlobalUnlocks[Name] : 0;
        if (unlockedLvl > 0) Program.DrawText($"x{CurrentMultiplier}", drawX + cardW - 45, drawY + 10, 24, Color.Yellow);
        else Program.DrawText("LOCKED", drawX + cardW - 80, drawY + 15, 15, Color.Red);

        if (unlockedLvl == 0) Program.DrawText("NEEDS CODING", drawX + 15, drawY + 80, 20, Color.Red);
        else if (VM.IsHalted) Program.DrawText("STOPPED/ERR", drawX + 20, drawY + 80, 20, Color.Red);
        else if (HasMissingMaterials) Program.DrawText("NO MATERIALS!", drawX + 15, drawY + 80, 20, Color.Orange);
        else Program.DrawText("PRODUCING", drawX + 20, drawY + 80, 20, Color.Green);
            
        Program.DrawText($"Line: {VM.IP + 1}", drawX + 20, drawY + 110, 20, Color.LightGray);
    }
}