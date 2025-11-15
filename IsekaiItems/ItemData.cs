using Duckov.Buffs;
using ItemStatsSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IsekaiItems
{
    /// <summary>
    /// 自定义物品的配置数据容器。包含物品的基础属性、图标、标签和使用方式等信息。
    /// </summary>
    public class ItemData
    {
        /// <summary>
        /// 物品的唯一标识符。
        /// </summary>
        public int itemId;

        /// <summary>
        /// 物品在背包中的排列顺序。默认为0。
        /// </summary>
        public int order = 0;

        /// <summary>
        /// 物品显示名称的本地化键。
        /// </summary>
        public string localizationKey = string.Empty;

        /// <summary>
        /// 物品描述的本地化键。
        /// </summary>
        public string localizationDescription = string.Empty;

        /// <summary>
        /// 物品的重量。
        /// </summary>
        public float weight;

        /// <summary>
        /// 物品的价值或价格。
        /// </summary>
        public int value;

        /// <summary>
        /// 物品的最大堆叠数量。默认为1（不可堆叠）。
        /// </summary>
        public int maxStackCount = 1;

        /// <summary>
        /// 物品的最大耐久度。0表示无限耐久度。
        /// </summary>
        public float maxDurability = 0f;

        /// <summary>
        /// 物品的品质级别。
        /// </summary>
        public int quality;

        /// <summary>
        /// 物品的显示品质。
        /// </summary>
        public DisplayQuality displayQuality = DisplayQuality.None;

        /// <summary>
        /// 物品图标的嵌入资源路径。
        /// </summary>
        public string embeddedSpritePath = string.Empty;

        /// <summary>
        /// 物品的标签列表，用于分类和过滤。
        /// </summary>
        public List<string> tags = new List<string>();

        /// <summary>
        /// 物品的使用配置（可选）。如果为null，表示物品不可使用。
        /// </summary>
        public UsageData usages;
    }

    /// <summary>
    /// 物品使用功能的配置数据。定义物品如何被使用及相关的行为。
    /// </summary>
    public class UsageData
    {
        /// <summary>
        /// 物品行动时播放的音效资源键。
        /// </summary>
        public string actionSound = string.Empty;

        /// <summary>
        /// 物品使用时播放的音效资源键。
        /// </summary>
        public string useSound = string.Empty;

        /// <summary>
        /// 是否使用耐久度系统。
        /// </summary>
        public bool useDurability = false;

        /// <summary>
        /// 每次使用消耗的耐久度数值。
        /// </summary>
        public int durabilityUsage = 1;

        /// <summary>
        /// 物品的使用时间（秒）。
        /// </summary>
        public float useTime = 2f;

        /// <summary>
        /// 物品使用时触发的行为列表。
        /// </summary>
        public List<UsageBehaviorData> behaviors = new List<UsageBehaviorData>();
    }

    /// <summary>
    /// 物品使用行为的基类。所有具体的使用行为都应继承此类。
    /// </summary>
    public abstract class UsageBehaviorData
    {
        /// <summary>
        /// 行为的类型标识符。用于在运行时进行类型判断和行为创建。
        /// </summary>
        public abstract string type { get; }
    }

    /// <summary>
    /// 食物/饮料使用行为。使物品可以恢复角色的饥饿度和渴望度。
    /// </summary>
    public class FoodData : UsageBehaviorData
    {
        /// <summary>
        /// 使用此物品恢复的能量值。
        /// </summary>
        public float energyValue;

        /// <summary>
        /// 使用此物品恢复的水分值。
        /// </summary>
        public float waterValue;

        /// <summary>
        /// 行为类型标识。
        /// </summary>
        public override string type => "FoodDrink";
    }

    /// <summary>
    /// 药物/治疗使用行为。使物品可以恢复角色的生命值。
    /// </summary>
    public class HealData : UsageBehaviorData
    {
        /// <summary>
        /// 使用此物品恢复的生命值。
        /// </summary>
        public int healValue;

        /// <summary>
        /// 行为类型标识。
        /// </summary>
        public override string type => "Drug";
    }

    /// <summary>
    /// 添加Buff使用行为。使物品可以给角色添加指定的Buff效果。
    /// </summary>
    public class AddBuffData : UsageBehaviorData
    {
        /// <summary>
        /// 要添加的Buff的唯一标识符。
        /// </summary>
        public int buff;

        /// <summary>
        /// 添加Buff的概率（0-1）。默认为100%概率。
        /// </summary>
        public float chance = 1f;

        /// <summary>
        /// 行为类型标识。
        /// </summary>
        public override string type => "AddBuff";

        /// <summary>
        /// 通过Buff ID查找对应的Buff预制体。
        /// </summary>
        /// <param name="id">Buff的唯一标识符。</param>
        /// <returns>找到的Buff预制体，如果不存在则返回null。</returns>
        public static Buff FindBuff(int id)
        {
            Buff[] allBuffs = Resources.FindObjectsOfTypeAll<Buff>();
            return allBuffs.FirstOrDefault(b => b != null && b.ID == id);
        }
    }

    /// <summary>
    /// 移除Buff使用行为。使物品可以移除角色身上的指定Buff效果。
    /// </summary>
    public class RemoveBuffData : UsageBehaviorData
    {
        /// <summary>
        /// 要移除的Buff的唯一标识符。
        /// </summary>
        public int buffID;

        /// <summary>
        /// 移除的Buff层数。默认为2层。
        /// </summary>
        public int removeLayerCount = 2;

        /// <summary>
        /// 行为类型标识。
        /// </summary>
        public override string type => "RemoveBuff";
    }
}
