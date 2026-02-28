using Raylib_cs;

namespace FactoryAssembler;

class Program
{
    public static void Main()
    {
        // 1. Ablak Inicializálása
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(1920, 1080, "Factory Planner Prototype");
        Raylib.SetExitKey(KeyboardKey.Null);
        Raylib.SetTargetFPS(60);

        // 2. A Játék Motor példányosítása
        FactoryGame game = new FactoryGame();

        // 3. Fő Játék Ciklus (Game Loop)
        while (!Raylib.WindowShouldClose())
        {
            game.Update(); // Logika, Inputok, Matek
            game.Draw();   // Kirajzolás
        }

        // 4. Kilépés
        Raylib.CloseWindow();
    }
}