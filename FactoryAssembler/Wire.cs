using Raylib_cs;
using System.Numerics;

namespace FactoryAssembler;

public class Wire
{
    public Card Source { get; private set; }
    public Card Target { get; private set; }

    public Wire(Card source, Card target)
    {
        Source = source;
        Target = target;
    }

    // Ez a függvény viszi át az adatot a két gép között a Tick alatt
    public void TransferData()
    {
        if (Source.VM.OutputBuffer.Count > 0)
        {
            int data = Source.VM.OutputBuffer.Dequeue();
            Target.VM.InputBuffer.Enqueue(data);
        }
    }

    // Kábel kirajzolása (Szép görbe vonal az OUT-ból az IN-be)
    public void Draw(int cardWidth, int cardHeight)
    {
        // Source OUT portjának helye (Jobb oldal közepe)
        float startX = (Source.GridX * cardWidth) + cardWidth;
        float startY = (Source.GridY * cardHeight) + (cardHeight / 2) + 20;
        Vector2 startPos = new Vector2(startX, startY);

        // Target IN portjának helye (Bal oldal közepe)
        float endX = (Target.GridX * cardWidth) + 10;
        float endY = (Target.GridY * cardHeight) + (cardHeight / 2) + 20;
        Vector2 endPos = new Vector2(endX, endY);

        // Szép "Bezier" görbe rajzolása
        Raylib.DrawLineBezier(startPos, endPos, 4, Color.Orange);
    }
}