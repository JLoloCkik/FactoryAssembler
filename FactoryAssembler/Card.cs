using System.Numerics;
using Raylib_cs;

namespace FactoryAssembler;

public class Card
{
    public string Name { get; set; }
    public int GridX { get; set; }
    public int GridY { get; set; }
    public Color HeaderColor { get; set; }
    public int CurrentMultiplier { get; set; } = 1;

    public VirtualMachine VM { get; private set; }

    public Card(string name, int x, int y, Color color)
    {
        Name = name;
        GridX = x;
        GridY = y;
        HeaderColor = color;
        VM = new VirtualMachine();
        
        VM.OnOutput += (val) => 
        {
            int unlockedLevel = GameState.GlobalUnlocks.ContainsKey(Name) ? GameState.GlobalUnlocks[Name] : 0;
            if (unlockedLevel >= 1) ProduceItem(val);
        };
    }

    private void ProduceItem(int val)
    {
        string product = "";
        
        if (Name == "Coal Miner") product = "Coal";
        else if (Name == "Iron Miner") product = "Iron Ore";
        else if (Name == "Copper Miner") product = "Copper Ore";
        else if (Name == "Smelter") product = "Iron Ingot"; 
        else if (Name == "Assembler") product = "Gear"; 

        if (!string.IsNullOrEmpty(product) && GameState.Inventory.ContainsKey(product))
        {
            GameState.Inventory[product] += 1;
        }
    }

    public void Draw(int width, int height)
    {
        float posX = GridX * width; float posY = GridY * height;
        float cardW = width - 20; float cardH = height - 20;
        float drawX = posX + 10; float drawY = posY + 10;

        Raylib.DrawRectangleRounded(new Rectangle(drawX + 8, drawY + 8, cardW, cardH), 0.1f, 10, new Color(0,0,0,100));
        Rectangle bodyRect = new Rectangle(drawX, drawY, cardW, cardH);
        Raylib.DrawRectangleRounded(bodyRect, 0.1f, 10, new Color(40, 44, 52, 255)); 

        float headerHeight = 50;
        Raylib.DrawRectangleRounded(new Rectangle(drawX, drawY, cardW, headerHeight), 0.1f, 10, HeaderColor);
        Raylib.DrawRectangle((int)drawX, (int)drawY + 25, (int)cardW, 25, HeaderColor);
        Raylib.DrawRectangleRoundedLines(bodyRect, 0.1f, 10, Color.LightGray);

        int portY = (int)drawY + (int)(cardH / 2) + 20;
        Raylib.DrawCircle((int)drawX, portY, 10, Color.Black); Raylib.DrawCircle((int)drawX, portY, 6, Color.White); 
        Raylib.DrawRectangle((int)drawX + 15, portY - 10, 40, 25, Color.Black);
        Raylib.DrawText(VM.InputBuffer.Count.ToString(), (int)drawX + 25, portY - 5, 20, Color.Yellow);

        Raylib.DrawCircle((int)drawX + (int)cardW, portY, 10, Color.Black); Raylib.DrawCircle((int)drawX + (int)cardW, portY, 6, Color.White);
        Raylib.DrawRectangle((int)drawX + (int)cardW - 55, portY - 10, 40, 25, Color.Black);
        Raylib.DrawText(VM.OutputBuffer.Count.ToString(), (int)drawX + (int)cardW - 45, portY - 5, 20, Color.Green);

        int fontSize = 24; int textWidth = Raylib.MeasureText(Name, fontSize);
        Raylib.DrawText(Name, (int)drawX + (int)(cardW / 2) - (textWidth / 2), (int)drawY + (int)(headerHeight / 2) - (fontSize / 2), fontSize, Color.White);
        Raylib.DrawText($"x{CurrentMultiplier}", (int)drawX + (int)cardW - 35, (int)drawY + 10, 20, Color.Yellow);

        if (VM.IsHalted) Raylib.DrawText("STOPPED", (int)drawX + 20, (int)drawY + 80, 20, Color.Red);
        else if (VM.IsWaiting) Raylib.DrawText("WAITING...", (int)drawX + 20, (int)drawY + 80, 20, Color.Orange);
        else Raylib.DrawText("RUNNING", (int)drawX + 20, (int)drawY + 80, 20, Color.Green);
            
        Raylib.DrawText($"Line: {VM.IP + 1}", (int)drawX + 20, (int)drawY + 110, 20, Color.LightGray);
    }
}