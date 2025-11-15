using System;
using System.Collections.Generic;
using System.Text;

namespace IsekaiItems
{
    /// <summary>
    /// 自定义物品定义容器。在此类中定义和注册所有模组添加的自定义物品。
    /// 
    /// 使用示例：
    /// <code>
    /// public static class Items
    /// {
    ///     public static ItemData customItem = new ItemData
    ///     {
    ///         itemId = 1000,
    ///         localizationKey = "item.custom.name",
    ///         // ... 其他配置
    ///     };
    /// }
    /// </code>
    /// </summary>
    public static class Items
    {
        // TODO: 在此添加自定义物品定义

        public static ItemData testItem = new ItemData
        {
            itemId = 33210002,
            order = 11,
            localizationKey = "L1",
            localizationDescription = "L1_Desc",
            weight = 0.02f,
            value = 2254,
            maxStackCount = 5,
            maxDurability = 1,
            quality = 3,
            displayQuality = ItemStatsSystem.DisplayQuality.Green,
            embeddedSpritePath = "items/lockedfog_tarkov_injection_L1.png",
            tags = {
                "Injector","Medic","TerraLabs"
            },
            usages = new UsageData
            {
                actionSound = "SFX/Item/use_cola",
                useSound = string.Empty,
                useTime = 0.5f,
                useDurability = true,
                durabilityUsage = 1,
                behaviors = new List<UsageBehaviorData>
                {
                    new FoodData
                    {
                        energyValue = -24f,
                        waterValue = -24f
                    },
                    new AddBuffData
                    {
                        buff = 1018,
                        chance = 1f
                    },
                    new AddBuffData
                    {
                        buff = 1014,
                        chance = 1f
                    },
                    new AddBuffData
                    {
                        buff = 1084,
                        chance = 1f
                    }
                }
            }
        };
    }
}
