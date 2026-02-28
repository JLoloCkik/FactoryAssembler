using System.Collections.Generic;

namespace FactoryAssembler;

public class Task
{
    public string Title;
    public string Description;
    public List<int> TestInputs;  // A teszt bemenete (mit kap az IN?)
    public List<int> ExpectedOutputs; // A helyes kimenet (mit várunk a OUT-tól?)
}

public static class TaskDatabase
{
    private static Dictionary<string, Dictionary<string, Task>> tasks = new();

    static TaskDatabase()
    {
        // --- COAL MINER FELADATOK ---
        var coalMinerTasks = new Dictionary<string, Task>();
        coalMinerTasks["Easy"] = new Task
        {
            Title = "Easy: Produce a single piece of Coal",
            Description = "The machine should output the number '1' to indicate it has produced one piece of Coal.",
            TestInputs = new List<int>(), // Nincs bemenete
            ExpectedOutputs = new List<int> { 1 }
        };
        coalMinerTasks["Normal"] = new Task
        {
            Title = "Normal: Overclocked Production",
            Description = "Output the number '1' twice to signify double production speed.",
            TestInputs = new List<int>(),
            ExpectedOutputs = new List<int> { 1, 1 }
        };
        tasks["Coal Miner"] = coalMinerTasks;

        // --- SMELTER FELADATOK ---
        var smelterTasks = new Dictionary<string, Task>();
        smelterTasks["Easy"] = new Task
        {
            Title = "Easy: Simple Pass-through",
            Description = "The machine receives an input value (an ore). Simply output the same value without changing it.",
            TestInputs = new List<int> { 42 },
            ExpectedOutputs = new List<int> { 42 }
        };
        tasks["Smelter"] = smelterTasks;
        
        // Ide jöhet a többi gép feladata...
    }

    public static Task? GetTask(string machineName, string difficulty)
    {
        if (tasks.ContainsKey(machineName) && tasks[machineName].ContainsKey(difficulty))
        {
            return tasks[machineName][difficulty];
        }
        return null;
    }
}