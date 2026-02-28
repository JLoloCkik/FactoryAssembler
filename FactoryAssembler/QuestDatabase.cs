using System.Collections.Generic;

namespace FactoryAssembler;

// Egy adott programozási feladat definíciója
public class AssemblyTask
{
    public string TargetMachine; // Pl. "Coal Miner", "Smelter"
    public int Level;            // 1: Easy, 2: Normal, 4: Hard
    public string Title;         // Pl. "Basic Output"
    public string Description;   // Mit kell csinálni a kódnak?
    
    // A teszteléshez szükséges adatok
    public List<int> TestInput;  // Miket kap az IN utasításnál
    public List<int> ExpectedOutput; // Miket kell kiadnia az OUT utasításnál
}

public static class QuestDatabase
{
    // Itt vannak a feladatok. Bármikor adhatsz hozzá újat!
    public static List<AssemblyTask> Tasks = new List<AssemblyTask>()
    {
        // --- MINER FELADATOK ---
        new AssemblyTask {
            TargetMachine = "Coal Miner", Level = 1,
            Title = "Basic Output",
            Description = "A Miner doesn't need input.\nJust output the number 1 constantly.\n\nExpected Output: 1, 1, 1",
            TestInput = new List<int>(), // Nincs bemenet
            ExpectedOutput = new List<int> { 1, 1, 1 } // Ezt várjuk a kimeneten
        },
        new AssemblyTask {
            TargetMachine = "Coal Miner", Level = 2,
            Title = "Double Output",
            Description = "Output the number 2 instead of 1.\n\nExpected Output: 2, 2, 2",
            TestInput = new List<int>(),
            ExpectedOutput = new List<int> { 2, 2, 2 }
        },
        new AssemblyTask {
            TargetMachine = "Coal Miner", Level = 4,
            Title = "Counting Up",
            Description = "Output an increasing sequence\nstarting from 1.\n\nExpected Output: 1, 2, 3",
            TestInput = new List<int>(),
            ExpectedOutput = new List<int> { 1, 2, 3 }
        },

        // --- SMELTER FELADATOK ---
        new AssemblyTask {
            TargetMachine = "Smelter", Level = 1,
            Title = "Pass Through",
            Description = "Read a value from IN, and output\nit without changing it.\n\nInput: 5, 10\nExpected Output: 5, 10",
            TestInput = new List<int> { 5, 10 },
            ExpectedOutput = new List<int> { 5, 10 }
        },
        new AssemblyTask {
            TargetMachine = "Smelter", Level = 2,
            Title = "Doubler",
            Description = "Read a value from IN, multiply\nit by 2, and output it.\n\nInput: 3, 5\nExpected Output: 6, 10",
            TestInput = new List<int> { 3, 5 },
            ExpectedOutput = new List<int> { 6, 10 }
        }
    };

    // Segédfüggvény: Kikeresi a géphez és szinthez tartozó feladatot
    public static AssemblyTask GetTask(string machineName, int level)
    {
        // Ha minden Miner ugyanazt a feladatot kapja, itt lehetne egyszerűsíteni, 
        // de most pontos név alapján keresünk. Ha nem talál, ad egy generikusat.
        foreach (var task in Tasks)
        {
            if (task.TargetMachine == machineName && task.Level == level)
                return task;
        }
        
        // Alapértelmezett feladat, ha még nem írtál hozzá
        return new AssemblyTask {
            TargetMachine = machineName, Level = level,
            Title = "Missing Task Data",
            Description = "Just output 0.\n\nExpected Output: 0",
            TestInput = new List<int>(), ExpectedOutput = new List<int> { 0 }
        };
    }
}