//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Cuboid
{
    public class BuildProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            UpdateApplicationDataBuildData();
        }

        public static void UpdateApplicationDataBuildData()
        {
            // find any ApplicationData scriptable object instances in the editor
            string[] assetGuids = AssetDatabase.FindAssets($"t:{typeof(ApplicationData).Name}");

            if (assetGuids.Length == 0) { return; }

            foreach (string assetGuid in assetGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);

                ApplicationData applicationData = AssetDatabase.LoadAssetAtPath<ApplicationData>(assetPath);

                if (applicationData == null) { continue; }

                applicationData.BuildDate = DateTime.Now.ToString("yyyy-MM-dd");
                int buildNumber = PlayerSettings.Android.bundleVersionCode;
                Debug.Log($"buildNumber: {buildNumber}");
                applicationData.BuildNumber = buildNumber;
                applicationData.BuildYear = DateTime.Now.Year;

                AssetDatabase.Refresh();
            }
        }
    }
}
