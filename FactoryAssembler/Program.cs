using Raylib_cs;
using System.Numerics;
using System.IO;
using System.Text;
using System;

namespace FactoryAssembler;

class Program
{
    public static Font AppFont;

    public static void Main()
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.VSyncHint);
        Raylib.InitWindow(1920, 1080, "Factory Planner - Full Game");
        Raylib.SetExitKey(KeyboardKey.Null);
        Raylib.SetTargetFPS(60);

        string[] fontPaths = { 
            "/usr/share/fonts/truetype/ubuntu/UbuntuMono-R.ttf", 
            "/usr/share/fonts/truetype/dejavu/DejaVuSansMono.ttf",
            "/usr/share/fonts/truetype/liberation/LiberationMono-Regular.ttf" 
        };
        
        AppFont = Raylib.GetFontDefault(); 
        foreach(var path in fontPaths) {
            if (File.Exists(path)) {
                AppFont = Raylib.LoadFontEx(path, 64, null, 0);
                Raylib.SetTextureFilter(AppFont.Texture, TextureFilter.Bilinear);
                break;
            }
        }

        FactoryGame game = new FactoryGame();

        while (!Raylib.WindowShouldClose())
        {
            game.Update();
            game.Draw();
        }

        Raylib.CloseWindow();
    }

    public static void DrawText(string text, float x, float y, float fontSize, Color color) {
        Raylib.DrawTextEx(AppFont, text, new Vector2(x, y), fontSize, 1, color);
    }
    
    public static int MeasureText(string text, float fontSize) {
        return (int)Raylib.MeasureTextEx(AppFont, text, fontSize, 1).X;
    }

    public static string WordWrap(string text, float fontSize, float maxWidth)
    {
        if (string.IsNullOrEmpty(text)) return "";
        string[] words = text.Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        StringBuilder sb = new StringBuilder();
        float lineWidth = 0;
        foreach (var word in words)
        {
            float wordWidth = MeasureText(word + " ", fontSize);
            if (lineWidth + wordWidth > maxWidth) { sb.Append("\n"); lineWidth = 0; }
            sb.Append(word + " "); lineWidth += wordWidth;
        }
        return sb.ToString();
    }
}