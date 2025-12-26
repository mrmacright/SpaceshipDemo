#if UNITY_IOS

using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

public static class iOSRemoveMacAndVision
{
    [PostProcessBuild(2000)]
    public static void OnPostProcessBuild(BuildTarget target, string path)
    {
        if (target != BuildTarget.iOS)
            return;

        Debug.Log("Forcing iOS-only destinations (removing Mac + Vision)");

        var projectPath = PBXProject.GetPBXProjectPath(path);
        var project = new PBXProject();
        project.ReadFromFile(projectPath);

        var mainTarget = project.GetUnityMainTargetGuid();
        var frameworkTarget = project.GetUnityFrameworkTargetGuid();

        ApplyPBXSettings(project, mainTarget);
        ApplyPBXSettings(project, frameworkTarget);

        project.WriteToFile(projectPath);

        // HARD OVERRIDE — required for Xcode 26
        var text = File.ReadAllText(projectPath);

        text = text.Replace(
            "SUPPORTS_MAC_DESIGNED_FOR_IPHONE_IPAD = YES;",
            "SUPPORTS_MAC_DESIGNED_FOR_IPHONE_IPAD = NO;"
        );

        // Force-disable VisionOS (new in Xcode 26)
        if (!text.Contains("ENABLE_VISIONOS"))
        {
            text = text.Replace(
                "SUPPORTS_MAC_DESIGNED_FOR_IPHONE_IPAD = NO;",
                "SUPPORTS_MAC_DESIGNED_FOR_IPHONE_IPAD = NO;\n\t\t\tENABLE_VISIONOS = NO;"
            );
        }
        else
        {
            text = text.Replace(
                "ENABLE_VISIONOS = YES;",
                "ENABLE_VISIONOS = NO;"
            );
        }

        File.WriteAllText(projectPath, text);
    }

    private static void ApplyPBXSettings(PBXProject project, string targetGuid)
    {
        project.SetBuildProperty(
            targetGuid,
            "SUPPORTS_MAC_DESIGNED_FOR_IPHONE_IPAD",
            "NO"
        );

        project.SetBuildProperty(
            targetGuid,
            "SUPPORTED_PLATFORMS",
            "iphoneos iphonesimulator"
        );
    }
}

#endif
