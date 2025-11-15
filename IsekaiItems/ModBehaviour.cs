using Duckov.Utilities;
using HarmonyLib;
using ItemStatsSystem.Data;
using System;
using UnityEngine;

namespace IsekaiItems
{
    /// <summary>
    /// 模组行为管理器。负责模组的初始化、Harmony补丁管理和本地化设置。
    /// </summary>
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        /// <summary>
        /// Harmony实例，用于管理所有的运行时补丁。
        /// </summary>
        private Harmony _harmony;

        /// <summary>
        /// Harmony标识符，用于唯一标识该模组的补丁。
        /// </summary>
        private const string HarmonyId = "isekaiitems";

        /// <summary>
        /// 当MonoBehaviour实例被加载时调用。初始化本地化系统和注册语言切换事件。
        /// </summary>
        private void Awake()
        {
            Debug.Log($"[{Constants.MODID}][ModBehaviour] {Constants.MODID} awaked, version: {Constants.VERSION}, author: {Constants.AUTHOR}");
            
            // 注册语言变更事件处理器
            SodaCraft.Localizations.LocalizationManager.OnSetLanguage += OnLanguageChanged;
            Debug.Log($"[{Constants.MODID}][ModBehaviour] Localization trigger set.");
        }

        /// <summary>
        /// 处理语言变更事件，动态加载对应的本地化文件。
        /// </summary>
        /// <param name="language">目标语言类型。</param>
        private void OnLanguageChanged(SystemLanguage language)
        {
            string localizationFileName = I18n.localizedNames.ContainsKey(language)
                ? I18n.localizedNames[language]
                : I18n.localizedNames[SystemLanguage.English];
            
            I18n.loadFileJson($"/{localizationFileName}");
        }

        /// <summary>
        /// 在模组停用前调用。清理所有动态添加的合成公式和分解公式。
        /// </summary>
        protected override void OnBeforeDeactivate()
        {
            CraftingUtils.RemoveAllAddedFormulas();
            CraftingUtils.RemoveAllAddedDecomposeFormulas();
            
            // 反注册语言变更事件
            SodaCraft.Localizations.LocalizationManager.OnSetLanguage -= OnLanguageChanged;
        }

        /// <summary>
        /// 在模组设置完成后调用。加载初始本地化文件并执行后续初始化。
        /// </summary>
        protected override void OnAfterSetup()
        {
            // TODO: ItemUtils.CreateCustomItem(Items.item);
            ItemUtils.CreateCustomItem(Items.testItem);

            string currentLanguageFileName = I18n.localizedNames[SodaCraft.Localizations.LocalizationManager.CurrentLanguage];
            I18n.loadFileJson($"/{currentLanguageFileName}");

            // TODO: CraftingUtils.AddCraftingFormula
            // TODO: CraftingUtils.AddDecomposeFormula
        }

        /// <summary>
        /// 当脚本组件被启用时调用。预加载Harmony库。
        /// </summary>
        private void OnEnable()
        {
            HarmonyLoad.Load0Harmony();
        }

        /// <summary>
        /// 在第一帧更新前调用。初始化Harmony实例并应用所有补丁。
        /// </summary>
        private void Start()
        {
            _harmony = new Harmony(HarmonyId);
            _harmony.PatchAll();
            Debug.Log($"[{Constants.MODID}][ModBehaviour] Harmony patches applied.");
        }

        /// <summary>
        /// 当脚本组件被禁用时调用。卸载所有Harmony补丁。
        /// </summary>
        private void OnDisable()
        {
            if (_harmony != null)
            {
                _harmony.UnpatchAll(HarmonyId);
                Debug.Log($"[{Constants.MODID}][ModBehaviour] Harmony patches removed.");
            }
        }
    }
}