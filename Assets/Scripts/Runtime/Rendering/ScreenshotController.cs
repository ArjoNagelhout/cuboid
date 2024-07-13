using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid
{
    public class ScreenshotController : MonoBehaviour
    {
        private static ScreenshotController _instance;
        public static ScreenshotController Instance => _instance;

        [SerializeField] private Camera _screenshotCamera;

        private RenderTexture _renderTexture;

        private int _width = 256;
        private int _height = 256;

        private void Awake()
        {
            // Singleton implementation
            if (_instance != null && _instance != this) { Destroy(this); } else { _instance = this; }
        }

        private void Start()
        {
            _width = UnityPlugin.Constants.k_ThumbnailSize;
            _height = _width;
            _renderTexture = new RenderTexture(_width, _height, 16);
        }

        public byte[] CaptureScreenshot()
        {
            _screenshotCamera.targetTexture = _renderTexture;
            RenderTexture.active = _renderTexture;
            _screenshotCamera.Render();
            Texture2D renderedTexture = new Texture2D(_width, _height);
            renderedTexture.ReadPixels(new Rect(0, 0, _width, _height), 0, 0);
            RenderTexture.active = null;
            return renderedTexture.EncodeToPNG();
        }
    }
}
