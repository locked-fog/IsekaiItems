using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace IsekaiItems
{
    /// <summary>
    /// 本地化管理器。负责加载和管理多语言本地化文件。
    /// </summary>
    public static class I18n
    {
        /// <summary>
        /// 语言到本地化文件名的映射字典。
        /// </summary>
        public static Dictionary<SystemLanguage, string> localizedNames = new Dictionary<SystemLanguage, string>()
        {
            { SystemLanguage.English, "en_us.json" },
            { SystemLanguage.ChineseSimplified, "zh_cn.json" },
            { SystemLanguage.ChineseTraditional, "zh_tw.json" }
        };

        /// <summary>
        /// 从指定路径加载JSON格式的本地化文件，并将其注册到游戏的本地化管理器中。
        /// </summary>
        /// <param name="localizationFileName">本地化文件的路径和文件名（包含前导斜杠）。</param>
        public static void loadFileJson(string localizationFileName)
        {
            try
            {
                string dllPath = Assembly.GetEntryAssembly().Location;
                string modDirectory = Path.GetDirectoryName(dllPath);
                StringBuilder assetLocation = new StringBuilder($"assets/{Constants.MODID}/lang");
                assetLocation.Append(localizationFileName);
                string filePath = Path.Combine(modDirectory, assetLocation.ToString());

                if (!File.Exists(filePath))
                {
                    // 如果指定文件不存在，尝试回退到英文版本
                    Debug.LogWarning($"[{Constants.MODID}][I18n] Localization file not found: {localizationFileName}");

                    if (localizationFileName != $"/{localizedNames[SystemLanguage.English]}")
                    {
                        Debug.LogWarning($"[{Constants.MODID}][I18n] Falling back to English");
                        loadFileJson($"/{localizedNames[SystemLanguage.English]}");
                    }
                    else
                    {
                        Debug.LogError($"[{Constants.MODID}][I18n] No localization files found at {assetLocation}");
                    }
                    return;
                }

                string jsonContent = File.ReadAllText(filePath, Encoding.UTF8);
                JObject jsonObject = JObject.Parse(jsonContent);

                // 将JSON中的所有键值对注册到游戏的本地化系统
                foreach (var item in jsonObject)
                {
                    if (item.Value == null)
                        continue;

                    string key = item.Key;
                    string value = item.Value.ToString();
                    SodaCraft.Localizations.LocalizationManager.SetOverrideText(key, value);
                }

                Debug.Log($"[{Constants.MODID}][I18n] Loaded localization file: {localizationFileName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{Constants.MODID}][I18n] Exception while loading localization file: {ex}");
            }
        }
    }
}
