using System;
using System.IO;
using System.Reflection;

namespace IsekaiItems
{
    /// <summary>
    /// 负责从嵌入资源中加载0Harmony.dll。
    /// </summary>
    public static class HarmonyLoad
    {
        /// <summary>
        /// 从嵌入资源流加载0Harmony.dll程序集。
        /// 
        /// 前置要求：在项目属性中设置0Harmony.dll的"生成操作"为"嵌入的资源"。
        /// </summary>
        /// <returns>加载的Harmony程序集。</returns>
        /// <exception cref="InvalidOperationException">当资源流不存在时抛出。</exception>
        public static Assembly Load0Harmony()
        {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            string currentNamespace = typeof(HarmonyLoad).Namespace;
            
            using (Stream stream = executingAssembly.GetManifestResourceStream($"{currentNamespace}.0Harmony.dll"))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException(
                        $"Failed to load embedded resource: {currentNamespace}.0Harmony.dll. " +
                        "Ensure 0Harmony.dll is set as an embedded resource in the project properties.");
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    return Assembly.Load(ms.ToArray());
                }
            }
        }
    }
}