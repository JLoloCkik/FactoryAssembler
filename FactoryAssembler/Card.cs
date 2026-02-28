using System.Numerics;
using Raylib_cs;

namespace FactoryAssembler;

public class Card
{
    public string Name { get; set; }
    public int GridX { get; set; }
    public int GridY { get; set; }
    public Color HeaderColor { get; set; }
    
    // ÚJ: A gép aktuális szorzója (1, 2 vagy 4)
    public int CurrentMultiplier { get; set; } = 1;

    public VirtualMachine VM { get; private set; }

    public Card(string name, int x, int y, Color color)
    {
        Name = name;
        GridX = x;
        GridY = y;
        HeaderColor = color;
        VM = new VirtualMachine();
    }

    public void Draw(int width, int height)
    {
        float posX = GridX * width;
        float posY = GridY * height;

        float cardW = width - 20; 
        float cardH = height - 20;
        float drawX = posX + 10; 
        float drawY = posY + 10;

        // 1. ÁRNYÉK
        Raylib.DrawRectangleRounded(new Rectangle(drawX + 8, drawY + 8, cardW, cardH), 0.1f, 10, new Color(0,0,0,100));

        // 2. FŐ TEST
        Rectangle bodyRect = new Rectangle(drawX, drawY, cardW, cardH);
        Raylib.DrawRectangleRounded(bodyRect, 0.1f, 10, new Color(40, 44, 52, 255)); 

        // 3. FEJLÉC
        float headerHeight = 50;
        Raylib.DrawRectangleRounded(new Rectangle(drawX, drawY, cardW, headerHeight), 0.1f, 10, HeaderColor);
        Raylib.DrawRectangle((int)drawX, (int)drawY + 25, (int)cardW, 25, HeaderColor);
        Raylib.DrawRectangleRoundedLines(bodyRect, 0.1f, 10, Color.LightGray);

        // 4. PORTOK
        int portY = (int)drawY + (int)(cardH / 2) + 20;
        
        // IN
        Raylib.DrawCircle((int)drawX, portY, 10, Color.Black); 
        Raylib.DrawCircle((int)drawX, portY, 6, Color.White); 
        Raylib.DrawRectangle((int)drawX + 15, portY - 10, 40, 25, Color.Black);
        Raylib.DrawText(VM.InputBuffer.Count.ToString(), (int)drawX + 25, portY - 5, 20, Color.Yellow);

        // OUT
        Raylib.DrawCircle((int)drawX + (int)cardW, portY, 10, Color.Black);
        Raylib.DrawCircle((int)drawX + (int)cardW, portY, 6, Color.White);
        Raylib.DrawRectangle((int)drawX + (int)cardW - 55, portY - 10, 40, 25, Color.Black);
        Raylib.DrawText(VM.OutputBuffer.Count.ToString(), (int)drawX + (int)cardW - 45, portY - 5, 20, Color.Green);

        // 5. FEJLÉC SZÖVEG (KÖZÉPRE ZÁRVA FÜGGŐLEGESEN ÉS VÍZSZINTESEN)
        int fontSize = 24;
        int textWidth = Raylib.MeasureText(Name, fontSize);
        int textX = (int)drawX + (int)(cardW / 2) - (textWidth / 2);
        int textY = (int)drawY + (int)(headerHeight / 2) - (fontSize / 2); // Pont középen
        Raylib.DrawText(Name, textX, textY, fontSize, Color.White);

        // Szorzó kiírása a jobb felső sarokba
        Raylib.DrawText($"x{CurrentMultiplier}", (int)drawX + (int)cardW - 35, textY, 20, Color.Yellow);

        // 6. STÁTUSZ (Hozzáadva a WAITING állapot)
        if (VM.IsHalted)
            Raylib.DrawText("STOPPED", (int)drawX + 20, (int)drawY + 80, 20, Color.Red);
        else if (VM.IsWaiting)
            Raylib.DrawText("WAITING FOR INPUT...", (int)drawX + 20, (int)drawY + 80, 20, Color.Orange);
        else
            Raylib.DrawText("RUNNING", (int)drawX + 20, (int)drawY + 80, 20, Color.Green);
            
        Raylib.DrawText($"Line: {VM.IP + 1}", (int)drawX + 20, (int)drawY + 110, 20, Color.LightGray);
    }
}