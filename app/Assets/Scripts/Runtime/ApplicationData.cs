//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid
{
    [CreateAssetMenu(fileName = "ApplicationData", menuName = "ShapeReality/ApplicationData")]
    public class ApplicationData : ScriptableObject
    {
        /// <summary>
        /// 
        /// </summary>
        public string BuildDate;

        /// <summary>
        /// 
        /// </summary>
        public int BuildNumber;

        /// <summary>
        /// Computed property
        /// </summary>
        public string ProductName => Application.productName;

        /// <summary>
        /// Computed property
        /// </summary>
        public string Version => Application.version;

        /// <summary>
        /// 
        /// </summary>
        public string[] Authors;

        /// <summary>
        /// 
        /// </summary>
        public int FirstCopyrightYear;

        /// <summary>
        /// 
        /// </summary>
        public string CompanyName => Application.companyName;

        /// <summary>
        /// 
        /// </summary>
        public int BuildYear;

        /// <summary>
        /// 
        /// </summary>
        public string WebsiteUrl;

        /// <summary>
        /// 
        /// </summary>
        public string ManualUrl;

        /// <summary>
        /// 
        /// </summary>
        public string LicenseUrl;

    }
}
