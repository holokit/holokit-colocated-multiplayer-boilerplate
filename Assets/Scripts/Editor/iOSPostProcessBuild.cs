// SPDX-FileCopyrightText: Copyright 2023 Reality Design Lab <dev@reality.design>
// SPDX-FileContributor: Yuchen Zhang <yuchenz27@outlook.com>
// SPDX-License-Identifier: MIT

#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using UnityEditor.iOS.Xcode;
#endif

public class iOSPostProcessBuild 
{
#if UNITY_IOS
#pragma warning disable 0162
	// Runs after the HoloKit SDK's post-processor (order 0) so it can undo the flag noted below.
	[PostProcessBuild(1000)]
	public static void OnPostprocessBuild(BuildTarget buildTarget, string buildPath)
	{
		if (buildTarget == BuildTarget.iOS)
		{
			string projectPath = PBXProject.GetPBXProjectPath(buildPath);
			string plistPath = Path.Combine(buildPath, "Info.plist");

			PlistDocument plist = new PlistDocument();
			plist.ReadFromString(File.ReadAllText(plistPath));
			PlistElementDict rootDict = plist.root;
			rootDict.SetBoolean("ITSAppUsesNonExemptEncryption", false);
			File.WriteAllText(plistPath, plist.WriteToString());

			var pbxProject = new PBXProject();
			pbxProject.ReadFromFile(projectPath);
			// Some of these targets don't exist in every Unity version; skip the missing ones.
			string[] targets =
			{
				pbxProject.GetUnityMainTargetGuid(),
				pbxProject.GetUnityFrameworkTargetGuid(),
				pbxProject.TargetGuidByName(PBXProject.GetUnityTestTargetName()),
				pbxProject.TargetGuidByName("GameAssembly"),
			};
			foreach (string target in targets)
			{
				if (!string.IsNullOrEmpty(target))
					pbxProject.SetBuildProperty(target, "ENABLE_BITCODE", "NO");
			}

			// HoloKit SDK 0.5.5 adds "-ld64" (the Xcode 15 classic-linker switch). Newer Xcode
			// linkers reject it and clang reads it as "-l d64", so the link fails. Remove it.
			pbxProject.UpdateBuildProperty(pbxProject.GetUnityFrameworkTargetGuid(), "OTHER_LDFLAGS",
				new string[0], new[] { "-ld64" });

	        pbxProject.WriteToFile(projectPath);
		}
	}
#pragma warning restore 0162
#endif
}
