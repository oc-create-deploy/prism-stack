using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;
using UnityEngine;

public static class BuildScript
{
    public static void BuildIos()
    {
        PrepareFonts();
        const string outputPath = "build/iOS";
        Directory.CreateDirectory(outputPath);

        PlayerSettings.productName = "Prism Stack";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, "com.play.blockpuzzle");
        PlayerSettings.bundleVersion = "1.0";
        PlayerSettings.iOS.buildNumber = "1";
        PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneOnly;
        PlayerSettings.iOS.targetOSVersionString = "15.0";
        PlayerSettings.iOS.appleEnableAutomaticSigning = false;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.iOS.sdkVersion = string.Equals(
            Environment.GetEnvironmentVariable("UNITY_IOS_SIMULATOR"),
            "true",
            StringComparison.OrdinalIgnoreCase)
            ? iOSSdkVersion.SimulatorSDK
            : iOSSdkVersion.DeviceSDK;

        const string appIconPath = "Assets/Brand/PrismStack-AppIcon.png";
        AssetDatabase.ImportAsset(appIconPath, ImportAssetOptions.ForceUpdate);
        var appIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(appIconPath);
        if (appIcon == null)
        {
            throw new InvalidOperationException($"Unable to import App Store icon at {appIconPath}.");
        }

        var iconSizes = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.iOS);
        PlayerSettings.SetIconsForTargetGroup(
            BuildTargetGroup.iOS,
            Enumerable.Repeat(appIcon, iconSizes.Length).ToArray());

        var scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            throw new InvalidOperationException("No enabled scenes are configured for the iOS build.");
        }

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.iOS,
            targetGroup = BuildTargetGroup.iOS,
            extraScriptingDefines = string.Equals(Environment.GetEnvironmentVariable("UNITY_IOS_SIMULATOR"), "true", StringComparison.OrdinalIgnoreCase)
                ? new[] { "PLAYABLE_IOS_SIMULATOR" } : new string[0],
            options = BuildOptions.CleanBuildCache
        });

        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException($"Unity iOS export failed: {report.summary.result}");
        }

        var plistPath = Path.Combine(outputPath, "Info.plist");
        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);
        plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
        plist.WriteToFile(plistPath);
    }
    private static void PrepareFonts()
    {
        const string fontPath = "Assets/Resources/PrismSans.ttf";
        AssetDatabase.ImportAsset(fontPath, ImportAssetOptions.ForceUpdate);
        var importer = (TrueTypeFontImporter)AssetImporter.GetAtPath(fontPath);
        importer.includeFontData = true;
        importer.SaveAndReimport();
        var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
        var asset = TMPro.TMP_FontAsset.CreateFontAsset(font);
        if (asset == null) throw new InvalidOperationException("Bundled Prism font generation failed");
        asset.name = "PrismFont";
        AssetDatabase.CreateAsset(asset, "Assets/Resources/PrismFont.asset");
        AssetDatabase.AddObjectToAsset(asset.material, asset);
        foreach (var texture in asset.atlasTextures)
            if (texture != null) AssetDatabase.AddObjectToAsset(texture, asset);
        var settings = ScriptableObject.CreateInstance<TMPro.TMP_Settings>();
        var serialized = new SerializedObject(settings);
        serialized.FindProperty("m_defaultFontAsset").objectReferenceValue = asset;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(settings, "Assets/Resources/TMP Settings.asset");
        AssetDatabase.SaveAssets();
        foreach (var entry in EditorBuildSettings.scenes.Where(s => s.enabled))
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(entry.path);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var text in root.GetComponentsInChildren<TMPro.TMP_Text>(true))
                {
                    text.font = asset;
                    text.fontSharedMaterial = asset.material;
                    EditorUtility.SetDirty(text);
                }
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        }
    }

}
