using Duckov.Buffs;
using Duckov.ItemUsage;
using Duckov.Utilities;
using ItemStatsSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;

namespace IsekaiItems
{
    /// <summary>
    /// 物品工具类。提供物品创建、配置、加载和注册的相关工具方法。
    /// </summary>
    public static class ItemUtils
    {
        /// <summary>
        /// 资源持有者。用于保持对物品图标纹理和精灵的引用，防止其被垃圾回收。
        /// </summary>
        private class ResourceHolder : MonoBehaviour
        {
            /// <summary>
            /// 物品图标纹理。
            /// </summary>
            public Texture2D iconTexture;

            /// <summary>
            /// 物品图标精灵。
            /// </summary>
            public Sprite iconSprite;
        }

        public static void CreateCustomItem(ItemData config)
        {
            try
            {
                GameObject gameObject = new GameObject($"GameObject_{config.localizationKey}");
                UnityEngine.Object.DontDestroyOnLoad(gameObject);
                gameObject.AddComponent<Item>();
                Item component = gameObject.GetComponent<Item>();
                SetItemProperties(component, config);
                SetItemIcon(component, config);
                RegisterItem(component);
            }
            catch (Exception arg)
            {
                Debug.LogError($"[{Constants.MODID}][CraftingUtils.cs] Exception at creating item: {arg}");
            }
        }

        /// <summary>
        /// 为物品创建并配置使用功能。
        /// </summary>
        /// <param name="item">目标物品。</param>
        /// <param name="config">物品配置数据。</param>
        private static void CreateUsage(Item item, ItemData config)
        {
            if (config.usages == null)
                return;

            item.AddComponent<UsageUtilities>();
            UsageUtilities usageUtilities = item.GetComponent<UsageUtilities>();

            // 配置音效
            if (!string.IsNullOrEmpty(config.usages.useSound))
            {
                usageUtilities.hasSound = true;
                usageUtilities.useSound = config.usages.useSound;
            }

            if (!string.IsNullOrEmpty(config.usages.actionSound))
            {
                usageUtilities.hasSound = true;
                usageUtilities.actionSound = config.usages.actionSound;
            }

            // 配置耐久度
            if (config.usages.useDurability && config.maxDurability > 0)
            {
                usageUtilities.useDurability = true;
                usageUtilities.durabilityUsage = config.usages.durabilityUsage;
            }

            // 使用反射设置私有的useTime字段
            FieldInfo useTimeField = typeof(UsageUtilities).GetField("useTime", BindingFlags.Instance | BindingFlags.NonPublic);
            if (useTimeField != null)
            {
                useTimeField.SetValueOptimized(usageUtilities, config.usages.useTime);
            }

            SetPrivateField(item, "usageUtilities", usageUtilities);

            // 创建所有使用行为
            foreach (var behavior in config.usages.behaviors)
            {
                CreateBehavior(item, behavior, item.UsageUtilities);
            }
        }

        /// <summary>
        /// 根据配置数据创建物品的使用行为。
        /// </summary>
        /// <param name="item">目标物品。</param>
        /// <param name="behaviorData">行为配置数据。</param>
        /// <param name="usageUtilities">物品的使用工具组件。</param>
        public static void CreateBehavior(Item item, UsageBehaviorData behaviorData, UsageUtilities usageUtilities)
        {
            if (behaviorData == null)
                return;

            switch (behaviorData.type)
            {
                case "FoodDrink":
                    CreateFoodBehavior(item, behaviorData as FoodData, usageUtilities);
                    break;

                case "Drug":
                    CreateDrugBehavior(item, behaviorData as HealData, usageUtilities);
                    break;

                case "AddBuff":
                    CreateAddBuffBehavior(item, behaviorData as AddBuffData, usageUtilities);
                    break;

                case "RemoveBuff":
                    CreateRemoveBuffBehavior(item, behaviorData as RemoveBuffData, usageUtilities);
                    break;

                default:
                    Debug.LogError($"[{Constants.MODID}][ItemUtils] Unknown usage behavior type: {behaviorData.type}");
                    break;
            }
        }

        /// <summary>
        /// 创建食物/饮料行为。
        /// </summary>
        private static void CreateFoodBehavior(Item item, FoodData foodData, UsageUtilities usageUtilities)
        {
            if (foodData == null)
                return;

            FoodDrink foodDrinkBehavior = item.AddComponent<FoodDrink>();
            foodDrinkBehavior.energyValue = foodData.energyValue;
            foodDrinkBehavior.waterValue = foodData.waterValue;
            usageUtilities.behaviors.Add(foodDrinkBehavior);
        }

        /// <summary>
        /// 创建药物/治疗行为。
        /// </summary>
        private static void CreateDrugBehavior(Item item, HealData healData, UsageUtilities usageUtilities)
        {
            if (healData == null)
                return;

            Drug drugBehavior = item.AddComponent<Drug>();
            drugBehavior.healValue = healData.healValue;
            usageUtilities.behaviors.Add(drugBehavior);
        }

        /// <summary>
        /// 创建添加Buff行为。
        /// </summary>
        private static void CreateAddBuffBehavior(Item item, AddBuffData addBuffData, UsageUtilities usageUtilities)
        {
            if (addBuffData == null)
                return;

            Buff buff = AddBuffData.FindBuff(addBuffData.buff);
            if (buff == null)
            {
                Debug.LogWarning($"[{Constants.MODID}][ItemUtils] Buff with ID {addBuffData.buff} not found.");
                return;
            }

            AddBuff addBuffBehavior = item.AddComponent<AddBuff>();
            addBuffBehavior.buffPrefab = buff;
            addBuffBehavior.chance = addBuffData.chance;
            usageUtilities.behaviors.Add(addBuffBehavior);
        }

        /// <summary>
        /// 创建移除Buff行为。
        /// </summary>
        private static void CreateRemoveBuffBehavior(Item item, RemoveBuffData removeBuffData, UsageUtilities usageUtilities)
        {
            if (removeBuffData == null)
                return;

            RemoveBuff buffBehavior = item.AddComponent<RemoveBuff>();
            buffBehavior.buffID = removeBuffData.buffID;
            buffBehavior.removeLayerCount = removeBuffData.removeLayerCount;
            usageUtilities.behaviors.Add(buffBehavior);
        }

        /// <summary>
        /// 通过反射设置指定对象的私有字段值。
        /// </summary>
        /// <param name="item">目标对象。</param>
        /// <param name="fieldName">字段名称。</param>
        /// <param name="value">要设置的值。</param>
        /// <returns>设置成功返回true，否则返回false。</returns>
        public static bool SetPrivateField(Item item, string fieldName, object value)
        {
            FieldInfo field = typeof(Item).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValueOptimized(item, value);
                return true;
            }

            Debug.LogWarning($"[{Constants.MODID}][ItemUtils] Couldn't find field: {fieldName}");
            return false;
        }

        /// <summary>
        /// 从文件系统加载嵌入的精灵资源。
        /// </summary>
        /// <param name="resourceName">资源文件名称（不含路径前缀）。</param>
        /// <param name="newItemId">新物品ID，用于生成唯一的GameObject名称。</param>
        /// <returns>加载的精灵，加载失败返回null。</returns>
        public static Sprite LoadEmbeddedSprite(string resourceName, int newItemId)
        {
            try
            {
                string dllPath = Assembly.GetExecutingAssembly().Location;
                string modDirectory = Path.GetDirectoryName(dllPath);
                StringBuilder assetLocation = new StringBuilder($"assets/{Constants.MODID}/textures/");
                assetLocation.Append(resourceName);
                string filePath = Path.Combine(modDirectory, assetLocation.ToString());

                if (!File.Exists(filePath))
                {
                    Debug.LogError($"[{Constants.MODID}][ItemUtils] Sprite file not found: {filePath}");
                    return null;
                }

                byte[] imageData = File.ReadAllBytes(filePath);
                Texture2D texture2D = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);

                if (!texture2D.LoadImage(imageData))
                {
                    Debug.LogError($"[{Constants.MODID}][ItemUtils] Invalid sprite image format: {resourceName}");
                    return null;
                }

                texture2D.filterMode = FilterMode.Bilinear;
                texture2D.Apply();

                Sprite sprite = Sprite.Create(
                    texture2D,
                    new Rect(0f, 0f, texture2D.width, texture2D.height),
                    new Vector2(0.5f, 0.5f),
                    100f);

                // 创建持有者以防止资源被垃圾回收
                GameObject iconHolder = new GameObject($"IconHolder_{newItemId}");
                UnityEngine.Object.DontDestroyOnLoad(iconHolder);
                
                ResourceHolder resourceHolder = iconHolder.AddComponent<ResourceHolder>();
                resourceHolder.iconTexture = texture2D;
                resourceHolder.iconSprite = sprite;

                Debug.Log($"[{Constants.MODID}][ItemUtils] Loaded sprite: {resourceName}");
                return sprite;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{Constants.MODID}][ItemUtils] Exception loading sprite: {ex}");
                return null;
            }
        }

        /// <summary>
        /// 使用配置数据设置物品的所有属性。
        /// </summary>
        /// <param name="item">目标物品。</param>
        /// <param name="config">物品配置数据。</param>
        public static void SetItemProperties(Item item, ItemData config)
        {
            SetPrivateField(item, "typeID", config.itemId);
            SetPrivateField(item, "order", config.order);
            SetPrivateField(item, "weight", config.weight);
            SetPrivateField(item, "value", config.value);
            SetPrivateField(item, "displayName", config.localizationKey);
            SetPrivateField(item, "quality", config.quality);

            item.MaxStackCount = config.maxStackCount;
            item.MaxDurability = config.maxDurability;
            item.Durability = config.maxDurability;

            CreateUsage(item, config);

            // 配置物品标签
            item.Tags.Clear();
            foreach (string tagName in config.tags)
            {
                Tag targetTag = GetTargetTag(tagName);
                if (targetTag != null)
                {
                    item.Tags.Add(targetTag);
                }
            }

            // 设置物品图标
            SetItemIcon(item, config);
        }

        /// <summary>
        /// 通过名称查找对应的物品标签。
        /// </summary>
        /// <param name="tagName">标签名称。</param>
        /// <returns>找到的标签，如果不存在返回null。</returns>
        public static Tag GetTargetTag(string tagName)
        {
            Tag[] allTags = Resources.FindObjectsOfTypeAll<Tag>();
            return allTags.FirstOrDefault(t => t.name == tagName);
        }

        /// <summary>
        /// 通过反射获取指定对象的私有字段值。
        /// </summary>
        /// <param name="item">目标对象。</param>
        /// <param name="fieldName">字段名称。</param>
        /// <returns>字段值，如果字段不存在返回null。</returns>
        public static object GetPrivateField(Item item, string fieldName)
        {
            FieldInfo field = typeof(Item).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                return field.GetValueOptimized(item);
            }

            Debug.LogWarning($"[{Constants.MODID}][ItemUtils] Couldn't find field: {fieldName}");
            return null;
        }

        /// <summary>
        /// 设置物品的图标精灵。
        /// </summary>
        /// <param name="item">目标物品。</param>
        /// <param name="config">物品配置数据。</param>
        private static void SetItemIcon(Item item, ItemData config)
        {
            FieldInfo field = typeof(Item).GetField("icon", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
                return;

            Sprite sprite = LoadEmbeddedSprite(config.embeddedSpritePath, config.itemId);
            if (sprite != null)
            {
                field.SetValueOptimized(item, sprite);
            }
        }

        /// <summary>
        /// 将自定义物品注册到游戏物品系统中。
        /// </summary>
        /// <param name="item">要注册的物品。</param>
        public static void RegisterItem(Item item)
        {
            ItemAssetsCollection.AddDynamicEntry(item);
            Debug.Log($"[{Constants.MODID}][ItemUtils] Registered item: {item.TypeID} - {item.DisplayName}");
        }
    }
}
