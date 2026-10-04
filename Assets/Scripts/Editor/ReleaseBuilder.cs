using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DrownedDream.EditorTools
{
    /// <summary>發佈用建置（Windows x64 / macOS Universal）。可由選單或命令列 -executeMethod 呼叫，輸出到專案根目錄 Builds/。</summary>
    public static class ReleaseBuilder
    {
        /// <summary>要打包的場景（只包正式關卡）。</summary>
        private static readonly string[] Scenes = { "Assets/Scenes/Prototype.unity" };
        /// <summary>執行檔名稱。</summary>
        private const string ExeName = "TheDrownedDream";

        /// <summary>建置 Windows 64-bit。</summary>
        [MenuItem("Drowned Dream/Release/Build Windows x64")]
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, $"Builds/Windows/{ExeName}.exe");

        /// <summary>建置 macOS Universal（Intel + Apple Silicon）。</summary>
        [MenuItem("Drowned Dream/Release/Build macOS Universal")]
        public static void BuildMac()
        {
            SetMacUniversal();
            Build(BuildTarget.StandaloneOSX, $"Builds/Mac/{ExeName}.app");
        }

        /// <summary>以反射設定 macOS 架構為 x64ARM64（沒裝 Mac 模組的 Editor 也能編譯）。</summary>
        private static void SetMacUniversal()
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("UnityEditor.OSXStandalone.UserBuildSettings"))
                .FirstOrDefault(t => t != null);
            var prop = type?.GetProperty("architecture");
            if (prop == null)
            {
                Debug.LogWarning("找不到 macOS 架構設定，沿用目前設定");
                return;
            }
            prop.SetValue(null, Enum.Parse(prop.PropertyType, "x64ARM64"));
        }

        /// <summary>執行建置，失敗時在 batchmode 以錯誤碼結束。</summary>
        private static void Build(BuildTarget target, string path)
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = path,
                target = target,
                options = BuildOptions.None,
            });
            Debug.Log($"[ReleaseBuilder] {target} → {path}: {report.summary.result}");
            if (report.summary.result != BuildResult.Succeeded && Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}
