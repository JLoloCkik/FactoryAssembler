using System.Collections.Generic;

namespace FactoryAssembler;

public class Puzzle
{
    public string Description;
    public int[] TestInputs;    // Mit adunk a gépnek (IN)
    public int[] ExpectedOutputs; // Mit várunk tőle (OUT)
}

public static class PuzzleDatabase
{
    // Itt tároljuk, melyik géphez milyen nehézségen mi a feladat
    // Kulcs: MachineName_Level (pl. "Coal Miner_1")
    public static Dictionary<string, Puzzle> Puzzles = new Dictionary<string, Puzzle>()
    {
        // --- MINERS (Nincs bemenet, csak termelnie kell) ---
        { "Coal Miner_1", new Puzzle { // Easy
            Description = "Output the number 1 repeatedly to simulate mining coal.",
            TestInputs = new int[] {}, 
            ExpectedOutputs = new int[] { 1, 1, 1 } 
        }},
        { "Coal Miner_2", new Puzzle { // Normal (x2)
            Description = "Output the number 2 repeatedly (Higher efficiency).",
            TestInputs = new int[] {}, 
            ExpectedOutputs = new int[] { 2, 2, 2 } 
        }},
        { "Coal Miner_4", new Puzzle { // Hard (x4)
            Description = "Output the sequence: 1, 2, 3, 4, then repeat or stop.",
            TestInputs = new int[] {}, 
            ExpectedOutputs = new int[] { 1, 2, 3, 4 } 
        }},

        // --- SMELTER (Van bemenet, fel kell dolgozni) ---
        { "Smelter_1", new Puzzle { 
            Description = "Pass-through: Read value from IN, send it to OUT unchanged.",
            TestInputs = new int[] { 5, 8, 2 }, 
            ExpectedOutputs = new int[] { 5, 8, 2 } 
        }},
        { "Smelter_2", new Puzzle { 
            Description = "Increment: Read IN, add 1 to it, send to OUT.",
            TestInputs = new int[] { 5, 10, 0 }, 
            ExpectedOutputs = new int[] { 6, 11, 1 } 
        }},
        { "Smelter_4", new Puzzle { 
            Description = "Double: Read IN, multiply by 2, send to OUT.",
            TestInputs = new int[] { 3, 5, 10 }, 
            ExpectedOutputs = new int[] { 6, 10, 20 } 
        }},
        
        // (Ide jöhet a többi gép is ugyanígy...)
    };

    // --- AZ INTERPRETER / VALIDÁTOR ---
    // Lefuttatja a kódot egy izolált VM-ben, és megnézi, jó-e
    public static string RunTests(string code, Puzzle puzzle)
    {
        VirtualMachine vm = new VirtualMachine();
        try 
        {
            vm.LoadCode(code);
        }
        catch 
        {
            return "COMPILE ERROR: Invalid syntax.";
        }

        // Feltöltjük a bemenetet tesztadatokkal
        foreach (int val in puzzle.TestInputs) vm.InputBuffer.Enqueue(val);

        List<int> actualOutputs = new List<int>();
        int steps = 0;
        int maxSteps = 100; // Végtelen ciklus védelem

        // Futtatás
        while (actualOutputs.Count < puzzle.ExpectedOutputs.Length && steps < maxSteps && !vm.IsHalted)
        {
            vm.Step();
            
            // Ha dobott hibát (pl. 0-val osztás)
            if (!string.IsNullOrEmpty(vm.LastError)) return $"RUNTIME ERROR: {vm.LastError}";

            // Ha termelt valamit, mentsük el
            while (vm.OutputBuffer.Count > 0)
            {
                actualOutputs.Add(vm.OutputBuffer.Dequeue());
            }
            steps++;
        }

        // Ellenőrzés
        if (actualOutputs.Count < puzzle.ExpectedOutputs.Length)
            return $"FAIL: Expected {puzzle.ExpectedOutputs.Length} outputs, got {actualOutputs.Count}.";

        for (int i = 0; i < puzzle.ExpectedOutputs.Length; i++)
        {
            if (actualOutputs[i] != puzzle.ExpectedOutputs[i])
                return $"FAIL: At index {i}, expected {puzzle.ExpectedOutputs[i]} but got {actualOutputs[i]}.";
        }

        return "SUCCESS"; // Minden teszt átment!
    }
}