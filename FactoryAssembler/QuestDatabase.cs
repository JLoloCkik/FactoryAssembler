using System.Collections.Generic;

namespace FactoryAssembler;

public static class QuestDatabase
{
    public static List<Quest> GetQuests()
    {
        return new List<Quest>()
        {
            new Quest { Title = "Fuel the Fire", TargetItem = "Coal", TargetAmount = 25, RewardCredits = 150 },
            new Quest { Title = "Iron Age", TargetItem = "Iron Ore", TargetAmount = 20, RewardCredits = 300 },
            new Quest { Title = "Bronze Age", TargetItem = "Copper Ore", TargetAmount = 20, RewardCredits = 300 },
            new Quest { Title = "Mass Production", TargetItem = "Iron Ingot", TargetAmount = 50, RewardCredits = 1000 },
            new Quest { Title = "Advanced Tech", TargetItem = "Gear", TargetAmount = 20, RewardCredits = 2000 },
            new Quest { Title = "To The Stars", TargetItem = "Rocket", TargetAmount = 1, RewardCredits = 10000 }
        };
    }
}