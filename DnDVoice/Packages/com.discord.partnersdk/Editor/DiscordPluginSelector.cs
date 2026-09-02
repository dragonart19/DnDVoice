using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Discord.Sdk.Editor {
/// <summary>
/// Selects the correct Discord native library (Debug or Release) before each build.
/// Development builds use the Debug library; non-development (release) builds use Release.
/// In the Editor, the Debug library is always active.
/// </summary>
public class DiscordPluginSelector
  : IPreprocessBuildWithReport
  , IPostprocessBuildWithReport {
    public int callbackOrder => 0;

    private const string PluginsRoot = "Packages/com.discord.partnersdk/Runtime/Plugins";

    private static readonly string[] KrispModelFileNames = new[] {
        "krisp-vad-o-v2.kef",
        "krisp-nc-o-nb-v2.kef",
        "krisp-nc-o-lite-v1.kef",
        "krisp-nc-o-med-v7.kef",
        "krisp-bvc-o-pro-v3.kef",
    };

    private static readonly string[] PluginFileNames =
      new[] {
          "discord_partner_sdk.dll",
          "discord_krisp.dll",
          "libdiscord_partner_sdk.so",
          "libdiscord_partner_sdk.dylib",
          "libdiscord_krisp.dylib",
          "discord_partner_sdk.prx",
          "discord_partner_sdk.aar",
          "discord_partner_sdk_krisp.aar",
          "discord_partner_sdk.framework",
          "discord_partner_sdk_krisp.framework",
          "DiscordKrisp.framework",
      }
        .Concat(KrispModelFileNames)
        .ToArray();

    private static readonly Dictionary<string, BuildTarget[]> PluginBuildTargets = new() {
        { "discord_partner_sdk.dll",
          new[] { BuildTarget.StandaloneWindows, BuildTarget.StandaloneWindows64 } },
        { "discord_krisp.dll",
          new[] { BuildTarget.StandaloneWindows, BuildTarget.StandaloneWindows64 } },
        { "libdiscord_partner_sdk.so", new[] { BuildTarget.StandaloneLinux64 } },
        { "libdiscord_partner_sdk.dylib", new[] { BuildTarget.StandaloneOSX } },
        { "libdiscord_krisp.dylib", new[] { BuildTarget.StandaloneOSX } },
        { "discord_partner_sdk.prx", new BuildTarget[0] },
        { "discord_partner_sdk.aar", new[] { BuildTarget.Android } },
        { "discord_partner_sdk_krisp.aar", new[] { BuildTarget.Android } },
    };

    private static readonly HashSet<string> EditorCompatiblePlugins = new() {
        "discord_partner_sdk.dll",
        "discord_krisp.dll",
        "libdiscord_partner_sdk.so",
        "libdiscord_partner_sdk.dylib",
        "libdiscord_krisp.dylib",
    };

    public void OnPreprocessBuild(BuildReport report) {
        bool isDevelopment = (report.summary.options & BuildOptions.Development) != 0;
        SetPluginConfig(isDevelopment);
    }

    public void OnPostprocessBuild(BuildReport report) {
        bool isDevelopment = (report.summary.options & BuildOptions.Development) != 0;
        if (IsStandaloneBuildTarget(report.summary.platform)) {
            CopyKrispModelsToStandaloneBuild(
              report.summary.outputPath, report.summary.platform, isDevelopment);
        }
        SetPluginConfig(true);
    }

    [InitializeOnLoadMethod]
    private static void OnEditorLoad() { SetPluginConfig(true); }

    private static void SetPluginConfig(bool useDebug) {
        string pluginsRoot = Path.GetFullPath(PluginsRoot);

        if (!Directory.Exists(pluginsRoot)) {
            return;
        }

        var importers =
          PluginImporter.GetAllImporters()
            .Where(p => p.assetPath.StartsWith("Packages/com.discord.partnersdk/Runtime/Plugins"))
            .Where(p => IsDiscordNativePlugin(p.assetPath))
            .ToArray();

        // Disable first so we never have two same-named plugins enabled simultaneously.
        foreach (var importer in importers) {
            bool isDebugPlugin = importer.assetPath.Contains("/Debug/");
            bool isReleasePlugin = importer.assetPath.Contains("/Release/");
            bool shouldEnable = (useDebug && isDebugPlugin) || (!useDebug && isReleasePlugin);
            if (!shouldEnable) {
                SetPluginEnabled(importer, false);
            }
        }

        foreach (var importer in importers) {
            bool isDebugPlugin = importer.assetPath.Contains("/Debug/");
            bool isReleasePlugin = importer.assetPath.Contains("/Release/");
            bool shouldEnable = (useDebug && isDebugPlugin) || (!useDebug && isReleasePlugin);
            if (shouldEnable) {
                SetPluginEnabled(importer, true);
            }
        }
    }

    private static bool IsDiscordNativePlugin(string path) {
        string fileName = Path.GetFileName(path);
        return PluginFileNames.Any(n => n == fileName);
    }

    private static void SetPluginEnabled(PluginImporter importer, bool enabled) {
        bool changed = false;
        string fileName = Path.GetFileName(importer.assetPath);

        bool isIosFramework =
          fileName == "discord_partner_sdk.framework" && importer.assetPath.Contains("/iOS/");
        bool editorTarget =
          enabled && EditorCompatiblePlugins.Contains(fileName) && !isIosFramework;
        if (importer.GetCompatibleWithEditor() != editorTarget) {
            importer.SetCompatibleWithEditor(editorTarget);
            changed = true;
        }

        BuildTarget[] supportedTargets = GetSupportedTargets(fileName, importer.assetPath);
        var supportedSet = new HashSet<BuildTarget>(supportedTargets);

        foreach (var target in new[] {
                     BuildTarget.StandaloneWindows,
                     BuildTarget.StandaloneWindows64,
                     BuildTarget.StandaloneLinux64,
                     BuildTarget.StandaloneOSX,
                     BuildTarget.Android,
                     BuildTarget.iOS,
                 }) {
            bool targetEnabled = enabled && supportedSet.Contains(target);
            if (importer.GetCompatibleWithPlatform(target) != targetEnabled) {
                importer.SetCompatibleWithPlatform(target, targetEnabled);
                changed = true;
            }
        }

        if (changed) {
            importer.SaveAndReimport();
        }
    }

    private static BuildTarget[] GetSupportedTargets(string fileName, string assetPath) {
        if (fileName == "discord_partner_sdk.framework" ||
            fileName == "discord_partner_sdk_krisp.framework" ||
            fileName == "DiscordKrisp.framework") {
            return assetPath.Contains("/iOS/") ? new[] { BuildTarget.iOS }
                                               : System.Array.Empty<BuildTarget>();
        }

        if (KrispModelFileNames.Contains(fileName)) {
            return assetPath.Contains("/x86_64/")
              ? new[] {
                    BuildTarget.StandaloneWindows,
                    BuildTarget.StandaloneWindows64,
                    BuildTarget.StandaloneLinux64,
                }
              : new[] { BuildTarget.StandaloneOSX };
        }

        return PluginBuildTargets.TryGetValue(fileName, out var targets)
          ? targets
          : System.Array.Empty<BuildTarget>();
    }

    private static void CopyKrispModelsToStandaloneBuild(string buildOutputPath,
                                                         BuildTarget buildTarget,
                                                         bool useDebug) {
        string pluginPath = GetStandalonePluginPath(buildOutputPath, buildTarget);
        if (pluginPath == null) {
            return;
        }

        string sourcePath = GetKrispModelSourcePath(buildTarget, useDebug);
        CopyKrispModels(sourcePath, pluginPath);
    }

    private static List<string> CopyKrispModels(string sourcePath, string outputPath) {
        var copiedFiles = new List<string>();
        if (!Directory.Exists(sourcePath)) {
            Debug.LogWarning($"Krisp model source directory not found: {sourcePath}");
            return copiedFiles;
        }

        Directory.CreateDirectory(outputPath);
        foreach (string modelFileName in KrispModelFileNames) {
            string sourceFile = Path.Combine(sourcePath, modelFileName);
            if (!File.Exists(sourceFile)) {
                Debug.LogWarning($"Krisp model file not found: {sourceFile}");
                continue;
            }

            string destinationFile = Path.Combine(outputPath, modelFileName);
            File.Copy(sourceFile, destinationFile, true);
            copiedFiles.Add(destinationFile);
            Debug.Log($"Copied Krisp model {modelFileName} to {destinationFile}");
        }

        return copiedFiles;
    }

    private static string GetKrispModelSourcePath(BuildTarget buildTarget, bool useDebug) {
        string config = useDebug ? "Debug" : "Release";
        string archPath = Path.GetFullPath(Path.Combine(PluginsRoot, "x86_64", config));
        if ((buildTarget == BuildTarget.StandaloneWindows ||
             buildTarget == BuildTarget.StandaloneWindows64 ||
             buildTarget == BuildTarget.StandaloneLinux64) &&
            Directory.Exists(archPath)) {
            return archPath;
        }

        return Path.GetFullPath(Path.Combine(PluginsRoot, config));
    }

    private static bool IsStandaloneBuildTarget(BuildTarget buildTarget) {
        return buildTarget == BuildTarget.StandaloneWindows ||
          buildTarget == BuildTarget.StandaloneWindows64 ||
          buildTarget == BuildTarget.StandaloneLinux64 || buildTarget == BuildTarget.StandaloneOSX;
    }

    private static string GetStandalonePluginPath(string buildOutputPath, BuildTarget buildTarget) {
        switch (buildTarget) {
        case BuildTarget.StandaloneWindows:
            return Path.Combine(GetStandaloneDataPath(buildOutputPath), "Plugins", "x86");
        case BuildTarget.StandaloneWindows64:
        case BuildTarget.StandaloneLinux64:
            return Path.Combine(GetStandaloneDataPath(buildOutputPath), "Plugins", "x86_64");
        case BuildTarget.StandaloneOSX:
            return Path.Combine(buildOutputPath, "Contents", "Plugins");
        default:
            return null;
        }
    }

    private static string GetStandaloneDataPath(string buildOutputPath) {
        string outputDirectory = Path.GetDirectoryName(buildOutputPath);
        string playerName = Path.GetFileNameWithoutExtension(buildOutputPath);
        return Path.Combine(outputDirectory, $"{playerName}_Data");
    }
}
}
