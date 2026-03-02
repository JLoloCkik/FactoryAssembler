using System;
using System.Collections.Generic;
using Raylib_cs;

namespace FactoryAssembler;

public class FactoryGrid
{
    public int Columns { get; private set; }
    public int Rows { get; private set; }
    public int CardWidth { get; private set; } = 240; 
    public int CardHeight { get; private set; } = 320;
    public List<Card> PlacedCards { get; private set; } = new List<Card>();

    public FactoryGrid(int cols, int rows) { Columns = cols; Rows = rows; }

    public void AddCard(Card card) { PlacedCards.Add(card); }

    public void Draw(Card? draggedCard)
    {
        for (int x = 0; x <= Columns; x++) {
            for (int y = 0; y <= Rows; y++) {
                Raylib.DrawCircle(x * CardWidth, y * CardHeight, 2, new Color(200, 200, 200, 30)); 
            }
        }
        foreach (var card in PlacedCards) {
            if (card != draggedCard) card.Draw(CardWidth, CardHeight);
        }
    }

    public (int x, int y) GetGridPos(float worldX, float worldY)
    {
        return ((int)Math.Floor(worldX / CardWidth), (int)Math.Floor(worldY / CardHeight));
    }
}