using Duckov.Economy;
using ItemStatsSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace IsekaiItems
{
    public static class LockedFogDuckovTools
    {
        /// <summary>
        /// 向制作配方集合中添加新的制作配方。
        /// </summary>
        /// <param name="formulaId">配方的唯一标识符。</param>
        /// <param name="money">制作该配方所需的金钱成本。</param>
        /// <param name="costItems">制作所需的物品成本数组，包含物品ID和数量。</param>
        /// <param name="resultItemId">制作结果物品的ID。</param>
        /// <param name="resultItemAmount">制作结果物品的数量。</param>
        /// <param name="tags">配方的标签数组。如果为 null，将默认使用 "WorkBenchAdvanced" 标签。</param>
        /// <param name="requirePerk">解锁此配方所需的特殊技能或属性ID，默认为空字符串（无要求）。</param>
        /// <param name="unlockByDefault">是否在游戏开始时默认解锁此配方，默认为 true。</param>
        /// <param name="hideInIndex">是否在配方索引中隐藏此配方，默认为 false。</param>
        /// <param name="lockInDemo">是否在演示模式中锁定此配方，默认为 false。</param>
        /// <remarks>
        /// 此方法使用反射访问 <see cref="CraftingFormulaCollection"/> 的私有字段以添加配方。
        /// 如果指定ID的配方已存在，将跳过添加并记录警告日志。
        /// 添加成功后会清空配方集合的只读缓存以确保数据同步。
        /// 已添加的配方ID和结果物品ID将被记录到 <see cref="addedFormulaIds"/> 和 <see cref="addedFormulaResults"/> 集合中。
        /// </remarks>
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
            const BindingFlags privateInstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;

            CraftingFormulaCollection instance = CraftingFormulaCollection.Instance;

            FieldInfo listField = typeof(CraftingFormulaCollection).GetField("list", privateInstanceFlags);
            List<CraftingFormula> formulaList = (List<CraftingFormula>)listField.GetValue(instance);

            foreach (CraftingFormula craftingFormula in formulaList)
            {
                if (craftingFormula.id == formulaId)
                {
                    Debug.LogWarning($"[IsekaiItems]Crafting formula with ID '{formulaId}' already exists. Skipping addition.");
                    return;
                }
            }

            Cost.ItemEntry[] costItemEntries = new Cost.ItemEntry[costItems.Length];
            for (int i = 0; i < costItems.Length; i++)
            {
                costItemEntries[i] = new Cost.ItemEntry
                {
                    id = costItems[i].id,
                    amount = costItems[i].amount
                };
            }

            CraftingFormula newFormula = new CraftingFormula
            {
                id = formulaId,
                unlockByDefault = unlockByDefault,

                cost = new Cost
                {
                    money = money,
                    items = costItemEntries
                },

                result = new CraftingFormula.ItemEntry
                {
                    id = resultItemId,
                    amount = resultItemAmount
                },

                requirePerk = requirePerk,

                tags = tags ?? new string[] { "WorkBenchAdvanced" },

                hideInIndex = hideInIndex,
                lockInDemo = lockInDemo
            };

            formulaList.Add(newFormula);

            if (!LockedFogDuckovTools.addedFormulaIds.Contains(formulaId))
            {
                LockedFogDuckovTools.addedFormulaIds.Add(formulaId);
                LockedFogDuckovTools.addedFormulaResults.Add(resultItemId);
            }

            Debug.Log($"[IsekaiItems]Added crafting formula with ID '{formulaId}' for item ID {resultItemId}.");

            FieldInfo cacheField = typeof(CraftingFormulaCollection).GetField("_entries_ReadOnly", privateInstanceFlags);
            cacheField.SetValue(instance, null);
        }

        /// <summary>
        /// 移除由本 Mod 添加到 <see cref="CraftingFormulaCollection"/> 的所有配方，并尝试清理只读缓存。
        /// </summary>
        /// <remarks>
        /// 实现细节：
        /// - 通过反射获取 <see cref="CraftingFormulaCollection"/> 的私有字段 "list" 并将其转换为 <c>List&lt;CraftingFormula&gt;</c>。
        /// - 从列表末尾向前遍历，检查每个配方的 ID 是否存在于 <see cref="addedFormulaIds"/> 中；
        ///   若存在则移除该配方并计数。
        /// - 在移除完成后清空本类保存的已添加配方 ID 与结果列表。
        /// - 尝试将私有缓存字段 "_entries_ReadOnly" 设为 <c>null</c>，如果找不到该字段则记录警告。
        /// - 所有异常均会被捕获并记录为错误日志，方法不会向调用方抛出异常。
        /// </remarks>
        public static void RemoveAllAddedFormulas()
        {
            const BindingFlags privateInstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;

            try
            {
                CraftingFormulaCollection instance = CraftingFormulaCollection.Instance;
                FieldInfo listField = typeof(CraftingFormulaCollection).GetField("list", privateInstanceFlags);

                if (listField == null)
                {
                    Debug.LogError("[IsekaiItems] Could not remove formulas: Could not find 'list' field");
                    return;
                }

                List<CraftingFormula> formulaList = (List<CraftingFormula>)listField.GetValue(instance);
                int removedCount = 0;

                for (int i = formulaList.Count - 1; i >= 0; i--)
                {
                    string formulaId = formulaList[i].id;

                    // 检查此配方是否由本Mod添加
                    if (LockedFogDuckovTools.addedFormulaIds.Contains(formulaId))
                    {
                        Debug.Log($"[IsekaiItems] Removed formula: {formulaId}");
                        formulaList.RemoveAt(i);
                        removedCount++;
                    }
                }

                LockedFogDuckovTools.addedFormulaIds.Clear();
                LockedFogDuckovTools.addedFormulaResults.Clear();

                FieldInfo cacheField = typeof(CraftingFormulaCollection).GetField("_entries_ReadOnly", privateInstanceFlags);
                if(cacheField != null)
                {
                    cacheField.SetValue(instance, null);
                }
                else
                {
                    Debug.LogWarning("[IsekaiItems] Could not find '_entries_ReadOnly' cache field, maybe not have to clear?");
                }

                Debug.Log($"[IsekaiItems] Removed {removedCount} formulas successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[IsekaiItems] Error removing formula: {ex}");
            }
        }

        /// <summary>
        /// 向分解配方数据库中添加新的分解配方。
        /// </summary>
        /// <param name="itemId">要添加分解配方的物品ID。</param>
        /// <param name="money">分解该物品所获得的金钱奖励。</param>
        /// <param name="resultItems">分解结果物品数组，包含物品ID和数量对。</param>
        /// <remarks>
        /// 此方法使用反射访问 <see cref="DecomposeDatabase"/> 的私有字段以添加新的分解配方。
        /// 
        /// 实现细节：
        /// - 通过反射获取私有字段 "entries" 并将其转换为 <c>DecomposeFormula[]</c>。
        /// - 检查指定物品ID的分解配方是否已存在；若存在则跳过添加。
        /// - 创建包含金钱奖励和物品结果的新 <see cref="DecomposeFormula"/> 结构体。
        /// - 将新配方添加到列表并转换回数组后重新赋值给私有字段。
        /// - 尝试调用私有方法 "RebuildDictionary" 以重建内部字典缓存，确保配方立即生效。
        /// - 已添加的物品ID将被记录到 <see cref="addedDecomposeItemIds"/> 集合中，以便后续移除操作。
        /// 
        /// 异常处理：
        /// - 所有异常均会被捕获并记录为错误日志，方法不会向调用方抛出异常。
        /// - 如果无法找到 "entries" 字段或 "RebuildDictionary" 方法，将记录相应的错误或警告日志。
        /// </remarks>
        /// <exception cref="System.Reflection.FieldAccessException">
        /// 在反射操作过程中可能发生，但会被内部捕获并记录为错误。
        /// </exception>
        public static void AddDecomposeFormula(
            int itemId,
            long money,
            (int id, long amount)[] resultItems
        )
        {
            try
            {
                const BindingFlags privateInstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;
                DecomposeDatabase instance = DecomposeDatabase.Instance;

                FieldInfo entriesField = typeof(DecomposeDatabase).GetField("entries", privateInstanceFlags);
                if (entriesField == null)
                {
                    Debug.LogError("[IsekaiItems] Could not add decompose formula: could not find 'entries' field");
                    return;
                }

                DecomposeFormula[] originalEntries = (DecomposeFormula[])entriesField.GetValue(instance);
                List<DecomposeFormula> entriesList = new List<DecomposeFormula>(originalEntries);

                if (entriesList.Any(formula => formula.item == itemId))
                {
                    Debug.LogWarning($"[IsekaiItems] Decompose formula already existed, skip add: {itemId}");
                    return;
                }

                Cost.ItemEntry[] resultItemEntries = resultItems.Select(item => new Cost.ItemEntry
                {
                    id = item.id,
                    amount = item.amount
                }).ToArray();

                DecomposeFormula newFormula = new DecomposeFormula
                {
                    item = itemId,
                    valid = true,
                    result = new Cost
                    {
                        money = money,
                        items = resultItemEntries
                    }
                };

                entriesList.Add(newFormula);

                entriesField.SetValue(instance, entriesList.ToArray());

                if (!LockedFogDuckovTools.addedDecomposeItemIds.Contains(itemId))
                {
                    LockedFogDuckovTools.addedDecomposeItemIds.Add(itemId);
                }

                Debug.Log($"[IsekaiItems] Added decompose formula {itemId} successfully.");

                MethodInfo rebuildMethod = typeof(DecomposeDatabase).GetMethod("RebuildDictionary", privateInstanceFlags);
                if (rebuildMethod != null)
                {
                    rebuildMethod.Invoke(instance, null);
                }
                else
                {
                    Debug.LogWarning("[IsekaiItems] Could not find 'RebuildDictionary' method, formula may not enabled.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[IsekaiItems] Error adding decompose formula: {ex}");
            }
        }

        /// <summary>
        /// 从分解配方数据库中移除由本 Mod 添加的所有分解配方。
        /// </summary>
        /// <remarks>
        /// 实现细节：
        /// - 通过反射获取 <see cref="DecomposeDatabase"/> 的私有字段 "entries" 并将其转换为 <c>DecomposeFormula[]</c>。
        /// - 将数组转换为列表以便进行移除操作。
        /// - 从列表末尾向前遍历，检查每个分解配方的物品ID是否存在于 <see cref="addedDecomposeItemIds"/> 中；
        ///   若存在则移除该配方并计数。
        /// - 将修改后的列表转换回数组并重新赋值给私有字段 "entries"。
        /// - 清空本类保存的已添加分解配方的物品ID集合。
        /// - 尝试调用私有方法 "RebuildDictionary" 以重建内部字典缓存，确保分解配方立即生效；
        ///   如果找不到该方法则记录警告日志。
        /// 
        /// 异常处理：
        /// - 所有异常均会被捕获并记录为错误日志，方法不会向调用方抛出异常。
        /// - 如果无法找到 "entries" 字段，将记录错误日志并提前返回。
        /// </remarks>
        public static void RemoveAllAddedDecomposeFormulas()
        {
            try
            {
                const BindingFlags privateInstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;
                DecomposeDatabase instance = DecomposeDatabase.Instance;

                FieldInfo entriesField = typeof(DecomposeDatabase).GetField("entries", privateInstanceFlags);
                if (entriesField == null)
                {
                    Debug.LogError("[IsekaiItems] Could not remove decompose formula: could not find 'entries' field");
                    return;
                }

                DecomposeFormula[] originalEntries = (DecomposeFormula[])entriesField.GetValue(instance);
                List<DecomposeFormula> entriesList = new List<DecomposeFormula>(originalEntries);
                int removedCound = 0;

                for (int i = entriesList.Count -1; i>=0; i--)
                {
                    int itemId = entriesList[i].item;

                    if (LockedFogDuckovTools.addedDecomposeItemIds.Contains(itemId))
                    {
                        Debug.Log($"[IsekaiItems] Removed decompose formula: {itemId}");
                        entriesList.RemoveAt(i);
                        removedCound++;
                    }
                }

                entriesField.SetValue(instance, entriesList.ToArray());

                LockedFogDuckovTools.addedDecomposeItemIds.Clear();

                MethodInfo rebuildMethod = typeof(DecomposeDatabase).GetMethod("RebuildDictionary", privateInstanceFlags);
                if (rebuildMethod != null)
                {
                    rebuildMethod.Invoke(instance, null);
                }
                else
                {
                    Debug.LogWarning("[IsekaiItems] Could not find method 'RebuildDectionary', decompose formula may not be removed.");
                }

                Debug.Log($"[IsekaiItems] Removed {removedCound} decompose formulas successfully.");
            }
            catch (Exception ex)
            {
                Debug.Log($"[IsekaiItems] Error removing decompose formula: {ex}");
            }
        }

        /// <summary>
        /// 通过反射从物品的修饰符集合中获取指定索引的修饰符描述。
        /// </summary>
        /// <param name="item">目标物品对象。不可为 null。</param>
        /// <param name="index">要获取的修饰符在集合中的索引（从 0 开始）。</param>
        /// <returns>
        /// 指定索引位置的 <see cref="ModifierDescription"/> 对象。
        /// </returns>
        /// <remarks>
        /// 实现细节：
        /// - 使用反射访问 <see cref="ModifierDescriptionCollection"/> 的私有字段 "list"），
        ///   该字段存储了所有 <see cref="ModifierDescription"/> 的集合。
        /// - 获取字段值并强制转换为 <see cref="System.Collections.IList"/> 以支持索引访问。
        /// - 返回集合中指定索引的元素。
        /// 
        /// 性能考量：
        /// - 此方法涉及反射操作，每次调用都会执行类型查询和字段解析，存在性能开销。
        /// - 若需频繁访问修饰符，建议缓存反射结果或使用公开 API（如果可用）。
        /// 
        /// 异常处理：
        /// - 若 <paramref name="item"/> 为 null，将抛出 <see cref="System.NullReferenceException"/>。
        /// - 若 <paramref name="index"/> 超出集合范围，将抛出 <see cref="System.IndexOutOfRangeException"/>。
        /// - 若无法通过反射找到 "list" 字段，将抛出 <see cref="System.NullReferenceException"/>。
        /// </remarks>
        /// <exception cref="System.NullReferenceException">
        /// 当 <paramref name="item"/> 为 null，或 "list" 字段不存在时抛出。
        /// </exception>
        /// <exception cref="System.IndexOutOfRangeException">
        /// 当 <paramref name="index"/> 小于 0 或大于等于集合大小时抛出。
        /// </exception>
        public static ModifierDescription FindModify(Item item, int index)
        {
            const BindingFlags findFieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            Type modifiersType = item.Modifiers.GetType();
            FieldInfo listField = modifiersType.GetField("list", findFieldFlags);

            IList modifierList = listField.GetValue(item.Modifiers) as IList;

            return (ModifierDescription)modifierList[index];
        }

        /// <summary>
        /// 通过反射克隆一个修饰符描述对象的所有字段值。
        /// </summary>
        /// <param name="source">要克隆的源 <see cref="ModifierDescription"/> 对象。不可为 null。</param>
        /// <returns>
        /// 一个新的 <see cref="ModifierDescription"/> 对象，其所有字段值都是从源对象复制而来。
        /// </returns>
        /// <remarks>
        /// 实现细节：
        /// - 创建一个新的空 <see cref="ModifierDescription"/> 实例作为克隆对象。
        /// - 通过反射获取 <see cref="ModifierDescription"/> 的所有实例字段（包括公开和私有字段）。
        /// - 逐个遍历字段，从源对象获取字段值并将其设置到克隆对象中。
        /// - 支持克隆所有类型的字段，包括值类型、引用类型和序列化字段。
        /// 
        /// 性能考量：
        /// - 此方法使用反射操作，存在相对较高的性能开销。
        /// - 若需频繁克隆大量对象，建议缓存反射结果或使用其他克隆策略。
        /// - 字段复制采用直接赋值方式，对于引用类型字段，克隆后的对象将与源对象共享引用。
        /// 
        /// 异常处理：
        /// - 若 <paramref name="source"/> 为 null，将抛出 <see cref="System.NullReferenceException"/>。
        /// - 若反射操作失败（如字段无法访问），将抛出相应的反射异常。
        /// </remarks>
        /// <exception cref="System.NullReferenceException">
        /// 当 <paramref name="source"/> 为 null 时抛出。
        /// </exception>
        private static ModifierDescription CloneModifier(ModifierDescription source)
        {
            const BindingFlags allInstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            ModifierDescription clone = new ModifierDescription();

            FieldInfo[] fields = typeof(ModifierDescription).GetFields(allInstanceFields);

            foreach (FieldInfo field in fields)
            {
                object value = field.GetValue(source);

                field.SetValue(clone, value);
            }

            return clone;
        }

        /// <summary>
        /// 通过反射设置修饰符描述对象的值字段。
        /// </summary>
        /// <param name="modifier">目标 <see cref="ModifierDescription"/> 对象。不可为 null。</param>
        /// <param name="value">要设置的新浮点数值。</param>
        /// <remarks>
        /// 实现细节：
        /// - 使用反射获取 <see cref="ModifierDescription"/> 的私有字段 "value"。
        /// - 通过 <see cref="FieldInfo.SetValue(object, object)"/> 方法将新值赋予修饰符对象。
        /// - 如果字段不存在或无法访问，则跳过操作不抛出异常。
        /// 
        /// 性能考量：
        /// - 此方法涉及反射操作，每次调用都会执行类型查询和字段解析，存在明显的性能开销。
        /// - 建议在批量修改修饰符值时缓存反射结果（即预先获取 <see cref="FieldInfo"/>），
        ///   避免重复执行反射查询。
        /// - 若性能至关重要，可考虑使用表达式树或其他反射替代方案以减少开销。
        /// 
        /// 异常处理：
        /// - 若 <paramref name="modifier"/> 为 null，将抛出 <see cref="System.NullReferenceException"/>。
        /// - 若字段设置过程中发生异常（如类型不匹配），异常将向调用方传播。
        /// - 若找不到 "value" 字段，方法将安quiet地返回而不抛出异常。
        /// </remarks>
        /// <exception cref="System.NullReferenceException">
        /// 当 <paramref name="modifier"/> 为 null 时抛出。
        /// </exception>
        /// <exception cref="System.FieldAccessException">
        /// 当字段值设置失败（如类型转换异常）时抛出。
        /// </exception>
        private static void SetModifierValue(ModifierDescription modifier, float value)
        {
            const BindingFlags allInstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            FieldInfo valueField = typeof(ModifierDescription).GetField("value", allInstanceFields);

            if (valueField != null)
            {
                valueField.SetValue(modifier, value);
            }
        }

        /// <summary>
        /// 重置物品的所有修饰符为指定的新修饰符集合。
        /// </summary>
        /// <param name="item">要重置修饰符的目标物品对象。不可为 null。</param>
        /// <param name="newModifierData">包含新修饰符及其偏移值的数组。不可为 null，但可以为空数组。</param>
        /// <remarks>
        /// 此方法用于批量替换物品的所有修饰符，常用于物品强化、装备更新或特殊效果应用等场景。
        /// 
        /// 实现细节：
        /// - 首先调用 <see cref="ModifierDescriptionCollection.Clear"/> 清空物品的现有修饰符集合。
        /// - 逐个遍历 <paramref name="newModifierData"/> 数组中的每个修饰符数据。
        /// - 对每个修饰符数据执行以下操作：
        ///   1. 通过 <see cref="CloneModifier"/> 克隆源修饰符描述，以创建独立副本。
        ///   2. 通过 <see cref="SetModifierValue"/> 将克隆的修饰符值设置为指定的偏移值。
        ///   3. 通过 <see cref="ModifierDescriptionCollection.Add"/> 将配置后的修饰符添加到物品。
        /// 
        /// 性能考量：
        /// - 此方法对每个新修饰符都执行克隆和反射操作，存在相对较高的性能开销。
        /// - 若需频繁重置大量物品或修饰符数量众多，建议在批处理前缓存反射结果或考虑使用性能优化方案。
        /// - 修饰符集合的清空操作会触发相关事件（如 <see cref="ModifierDescriptionCollection.OnItemTreeChange"/>），
        ///   可能导致额外的性能消耗，具体取决于物品系统的监听器实现。
        /// 
        /// 异常处理：
        /// - 若 <paramref name="item"/> 为 null，将抛出 <see cref="System.NullReferenceException"/>。
        /// - 若 <paramref name="newModifierData"/> 为 null，将抛出 <see cref="System.NullReferenceException"/>。
        /// - 若克隆或设置修饰符值过程中发生异常，异常将向调用方传播。
        /// </remarks>
        /// <exception cref="System.NullReferenceException">
        /// 当 <paramref name="item"/> 或 <paramref name="newModifierData"/> 为 null 时抛出。
        /// </exception>
        /// <exception cref="System.FieldAccessException">
        /// 当反射操作失败（如修饰符值字段无法访问）时抛出。
        /// </exception>
        /// <example>
        /// 以下示例演示如何使用此方法重置物品的修饰符：
        /// <code>
        /// // 创建新修饰符数据
        /// var newModifiers = new[]
        /// {
        ///     new LockedFogDuckovTools.ModifierDescriptionWithOffset
        ///     {
        ///         modifierDescription = new ModifierDescription(),
        ///         modifierOffset = 10.5f
        ///     },
        ///     new LockedFogDuckovTools.ModifierDescriptionWithOffset
        ///     {
        ///         modifierDescription = new ModifierDescription(),
        ///         modifierOffset = 20.0f
        ///     }
        /// };
        /// 
        /// // 重置物品修饰符
        /// LockedFogDuckovTools.ResetItemModifier(targetItem, newModifiers);
        /// </code>
        /// </example>
        public static void ResetItemModifier(Item item, ModifierDescriptionWithOffset[] newModifierData)
        {
            item.Modifiers.Clear();
            foreach(ModifierDescriptionWithOffset modData in newModifierData)
            {
                ModifierDescription newModifier = LockedFogDuckovTools.CloneModifier(modData.modifierDescription);
                LockedFogDuckovTools.SetModifierValue(newModifier, modData.modifierOffset);
                item.Modifiers.Add(newModifier);
            }
        }

        public class ModifierDescriptionWithOffset
        {
            public ModifierDescription modifierDescription;
            public float modifierOffset;
        }

        public enum DecomMode
        {
            None, 
            Free,
            Auto
        }

        public static List<string> addedFormulaIds = new List<string>();
        public static List<int> addedFormulaResults = new List<int>();
        public static List<int> addedDecomposeItemIds = new List<int>();
    }
}
