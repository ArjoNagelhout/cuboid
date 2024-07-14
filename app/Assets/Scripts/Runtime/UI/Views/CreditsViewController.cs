//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Cuboid.Utils;
using DG.Tweening;

namespace Cuboid.UI
{
    /// <summary>
    /// Populate Credits panel with author information, copyright information
    /// year, and version
    /// </summary>
    public class CreditsViewController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text;

        /// <summary>
        /// For some funky animations :)
        /// </summary>
        [SerializeField] private Transform _imageTransform;
        [SerializeField] private Vector3 _startRotation;
        [SerializeField] private float _startScaleFactor = 0.8f;
        [SerializeField] private float _startZPosition = 0f;
        [SerializeField] private float _endZPosition = -40f;
        [SerializeField] private float _imageAnimationDuration = 0.5f;

        [SerializeField] private ApplicationData _applicationData;

        private void Start()
        {
            SetText();

            _imageTransform.localPosition = _imageTransform.localPosition.SetZ(_startZPosition);
            _imageTransform.localRotation = Quaternion.Euler(_startRotation);
            _imageTransform.localScale = Vector3.one * _startScaleFactor;
            _imageTransform.DOLocalMoveZ(_endZPosition, _imageAnimationDuration).SetEase(Ease.OutBack, 1.2f);
            _imageTransform.DOLocalRotate(Vector3.zero, _imageAnimationDuration).SetEase(Ease.OutBack, 1.2f);
            _imageTransform.DOScale(1.0f, _imageAnimationDuration).SetEase(Ease.OutBack, 1.2f);
        }

        private void SetText()
        {
            string text = "";
            text += $"Version {_applicationData.Version} (Build {_applicationData.BuildNumber})\n";
            text += $"Unity version {Application.unityVersion} \n\n";

            // authors
            text += "Authors\n";

            string[] authors = _applicationData.Authors;

            if (authors.Length != 0)
            {
                // optimized by GPT-4
                string authorsString = string.Join(", ", authors);
                if (authors.Length > 1)
                {
                    int lastCommaIndex = authorsString.LastIndexOf(',');
                    authorsString = authorsString.Remove(lastCommaIndex, 1).Insert(lastCommaIndex, " and");
                }
                text += authorsString + "\n\n";
            }

            int firstCopyrightYear = _applicationData.FirstCopyrightYear;
            int buildYear = _applicationData.BuildYear;
            string copyrightYearsString = firstCopyrightYear == buildYear ?
                buildYear.ToString() : string.Join('-', firstCopyrightYear, buildYear);

            text += $"Copyright {copyrightYearsString} {_applicationData.CompanyName}. All rights reserved.\n\n";

            text += "Made with a lot of <3, sweat and tears in Eindhoven :)";

            _text.text = text;
        }

        public void OpenWebsite()
        {
            Application.OpenURL(_applicationData.WebsiteUrl);
        }

        public void OpenManual()
        {
            Application.OpenURL(_applicationData.ManualUrl);
        }

        public void OpenLicense()
        {
            Application.OpenURL(_applicationData.LicenseUrl);
        }
    }
}
