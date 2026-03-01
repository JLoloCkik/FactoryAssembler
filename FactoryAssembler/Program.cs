using Raylib_cs;

namespace FactoryAssembler;

class Program
{
    public static void Main()
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(1920, 1080, "Factory Planner Prototype");
        Raylib.SetExitKey(KeyboardKey.Null);
        Raylib.SetTargetFPS(60);

        FactoryGame game = new FactoryGame();

        while (!Raylib.WindowShouldClose())
        {
            game.Update();
            game.Draw();
        }

        Raylib.CloseWindow();
    }
}