using Duckov.Economy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace IsekaiItems
{
    /// <summary>
    /// 合成工具类。管理自定义合成公式和分解公式的动态添加和移除。
    /// </summary>
    public static class CraftingUtils
    {
        /// <summary>
        /// 已添加的合成公式ID列表。用于追踪哪些公式需要在卸载时移除。
        /// </summary>
        public static List<string> addedFormulaIds = new List<string>();

        /// <summary>
        /// 已添加的合成公式所产出物品的ID列表。
        /// </summary>
        public static List<int> addedFormulaResults = new List<int>();

        /// <summary>
        /// 已添加的分解公式所针对物品的ID列表。用于追踪哪些分解公式需要在卸载时移除。
        /// </summary>
        public static List<int> addedDecomposeItemIds = new List<int>();

        /// <summary>
        /// 添加物品分解公式。
        /// </summary>
        /// <param name="itemId">要分解的物品ID。</param>
        /// <param name="money">分解获得的金钱。</param>
        /// <param name="resultItems">分解结果物品数组，包含物品ID和数量。</param>
        public static void AddDecomposeFormula(int itemId, long money, (int id, long amount)[] resultItems)
        {
            DecomposeDatabase instance = DecomposeDatabase.Instance;
            FieldInfo field = typeof(DecomposeDatabase).GetField("entries", BindingFlags.Instance | BindingFlags.NonPublic);
            DecomposeFormula[] collection = (DecomposeFormula[])field.GetValue(instance);
            List<DecomposeFormula> formulaList = new List<DecomposeFormula>(collection);

            // 检查是否已存在相同的分解公式
            foreach (DecomposeFormula existingFormula in formulaList)
            {
                if (existingFormula.item == itemId && existingFormula.result.items.Any())
                {
                    Debug.LogWarning($"[{Constants.MODID}][CraftingUtils] Decompose formula already exists for item: {itemId}");
                    foreach (var itemResult in existingFormula.result.items)
                    {
                        Debug.LogWarning($"[{Constants.MODID}][CraftingUtils] Existing result - Item: {itemResult.id}, Amount: {itemResult.amount}");
                    }
                    return;
                }
            }

            // 创建新的分解公式
            DecomposeFormula newFormula = new DecomposeFormula
            {
                item = itemId,
                valid = true
            };

            Cost result = new Cost { money = money };
            Cost.ItemEntry[] itemEntries = new Cost.ItemEntry[resultItems.Length];

            for (int i = 0; i < resultItems.Length; i++)
            {
                itemEntries[i] = new Cost.ItemEntry
                {
                    id = resultItems[i].id,
                    amount = resultItems[i].amount
                };
            }

            result.items = itemEntries;
            newFormula.result = result;

            formulaList.Add(newFormula);
            field.SetValue(instance, formulaList.ToArray());

            if (!addedDecomposeItemIds.Contains(itemId))
            {
                addedDecomposeItemIds.Add(itemId);
            }

            Debug.Log($"[{Constants.MODID}][CraftingUtils] Added decompose formula for item: {itemId}");

            // 重建内部字典以刷新缓存
            typeof(DecomposeDatabase).GetMethod("RebuildDictionary", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(instance, null);
        }

        /// <summary>
        /// 移除所有已添加的分解公式。
        /// </summary>
        public static void RemoveAllAddedDecomposeFormulas()
        {
            try
            {
                DecomposeDatabase instance = DecomposeDatabase.Instance;
                FieldInfo field = typeof(DecomposeDatabase).GetField("entries", BindingFlags.Instance | BindingFlags.NonPublic);
                DecomposeFormula[] collection = (DecomposeFormula[])field.GetValue(instance);
                List<DecomposeFormula> formulaList = new List<DecomposeFormula>(collection);

                int removedCount = 0;
                for (int i = formulaList.Count - 1; i >= 0; i--)
                {
                    if (addedDecomposeItemIds.Contains(formulaList[i].item))
                    {
                        Debug.Log($"[{Constants.MODID}][CraftingUtils] Removing decompose formula for item: {formulaList[i].item}");
                        formulaList.RemoveAt(i);
                        removedCount++;
                    }
                }

                field.SetValue(instance, formulaList.ToArray());
                addedDecomposeItemIds.Clear();

                // 重建内部字典以刷新缓存
                typeof(DecomposeDatabase).GetMethod("RebuildDictionary", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(instance, null);

                Debug.Log($"[{Constants.MODID}][CraftingUtils] Removed {removedCount} decompose formulas");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{Constants.MODID}][CraftingUtils] Exception while removing decompose formulas: {ex}");
            }
        }

        /// <summary>
        /// 添加物品合成公式。
        /// </summary>
        /// <param name="formulaId">公式的唯一标识符。</param>
        /// <param name="money">合成需要花费的金钱。</param>
        /// <param name="costItems">合成所需的物品数组，包含物品ID和数量。</param>
        /// <param name="resultItemId">合成产出物品的ID。</param>
        /// <param name="resultItemAmount">合成产出物品的数量。</param>
        /// <param name="tags">公式的标签数组，用于分类。默认为"WorkBenchAdvanced"。</param>
        /// <param name="requirePerk">合成需要的特性ID。默认为空字符串。</param>
        /// <param name="unlockByDefault">是否默认解锁。默认为true。</param>
        /// <param name="hideInIndex">是否在合成菜单中隐藏。默认为false。</param>
        /// <param name="lockInDemo">是否在演示模式下锁定。默认为false。</param>
        public static void AddCraftingFormula(
            string formulaId,
            long money,
            (int id, long amount)[] costItems,
            int resultItemId,
            int resultItemAmount,
            string[] tags = null,
            string requirePerk = "",
            bool unlockByDefault = true,
            bool hideInIndex = false,
            bool lockInDemo = false)
        {
            CraftingFormulaCollection instance = CraftingFormulaCollection.Instance;
            FieldInfo field = typeof(CraftingFormulaCollection).GetField("list", BindingFlags.Instance | BindingFlags.NonPublic);
            List<CraftingFormula> formulaList = (List<CraftingFormula>)field.GetValue(instance);

            // 检查是否已存在相同的合成公式
            foreach (CraftingFormula existingFormula in formulaList)
            {
                if (existingFormula.id == formulaId)
                {
                    Debug.LogWarning($"[{Constants.MODID}][CraftingUtils] Crafting formula already exists: {formulaId}");
                    return;
                }
            }

            // 创建新的合成公式
            CraftingFormula newFormula = new CraftingFormula
            {
                id = formulaId,
                unlockByDefault = unlockByDefault,
                requirePerk = requirePerk,
                tags = tags ?? new[] { "WorkBenchAdvanced" },
                hideInIndex = hideInIndex,
                lockInDemo = lockInDemo
            };

            // 配置成本
            Cost cost = new Cost { money = money };
            Cost.ItemEntry[] costEntries = new Cost.ItemEntry[costItems.Length];

            for (int i = 0; i < costItems.Length; i++)
            {
                costEntries[i] = new Cost.ItemEntry
                {
                    id = costItems[i].id,
                    amount = costItems[i].amount
                };
            }

            cost.items = costEntries;
            newFormula.cost = cost;

            // 配置产出
            CraftingFormula.ItemEntry result = new CraftingFormula.ItemEntry
            {
                id = resultItemId,
                amount = resultItemAmount
            };
            newFormula.result = result;

            formulaList.Add(newFormula);

            if (!addedFormulaIds.Contains(formulaId))
            {
                addedFormulaIds.Add(formulaId);
                addedFormulaResults.Add(resultItemId);
            }

            Debug.Log($"[{Constants.MODID}][CraftingUtils] Added crafting formula: {formulaId}");

            // 清空缓存以刷新
            FieldInfo cacheField = typeof(CraftingFormulaCollection).GetField("_entries_ReadOnly", BindingFlags.Instance | BindingFlags.NonPublic);
            cacheField.SetValue(instance, null);
        }

        /// <summary>
        /// 移除所有已添加的合成公式。
        /// </summary>
        public static void RemoveAllAddedFormulas()
        {
            try
            {
                CraftingFormulaCollection instance = CraftingFormulaCollection.Instance;
                FieldInfo field = typeof(CraftingFormulaCollection).GetField("list", BindingFlags.Instance | BindingFlags.NonPublic);
                List<CraftingFormula> formulaList = (List<CraftingFormula>)field.GetValue(instance);

                int removedCount = 0;
                for (int i = formulaList.Count - 1; i >= 0; i--)
                {
                    if (addedFormulaIds.Contains(formulaList[i].id))
                    {
                        Debug.Log($"[{Constants.MODID}][CraftingUtils] Removing crafting formula: {formulaList[i].id}");
                        formulaList.RemoveAt(i);
                        removedCount++;
                    }
                }

                addedFormulaIds.Clear();

                // 清空缓存以刷新
                FieldInfo cacheField = typeof(CraftingFormulaCollection).GetField("_entries_ReadOnly", BindingFlags.Instance | BindingFlags.NonPublic);
                cacheField.SetValue(instance, null);

                Debug.Log($"[{Constants.MODID}][CraftingUtils] Removed {removedCount} crafting formulas");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{Constants.MODID}][CraftingUtils] Exception while removing crafting formulas: {ex}");
            }
        }
    }
}
