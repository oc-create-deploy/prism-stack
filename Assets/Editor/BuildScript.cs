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
        const string outputPath = "build/iOS";
        Directory.CreateDirectory(outputPath);

        PlayerSettings.productName = "Prism Stack";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, "com.play.blockpuzzle");
        PlayerSettings.bundleVersion = "1.0";
        PlayerSettings.iOS.buildNumber = "1";
        PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneOnly;
        PlayerSettings.iOS.targetOSVersionString = "15.0";
        PlayerSettings.iOS.appleEnableAutomaticSigning = false;
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
}
