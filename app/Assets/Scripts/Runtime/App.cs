//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cuboid.Input;

namespace Cuboid
{
    public enum HandlePosition
    {
        Center = 0,
        Pivot
    }

    public enum HandleRotation
    {
        Global = 0,
        Local
    }

    /// <summary>
    /// Entry point for the app
    /// </summary>
    public sealed class App : MonoBehaviour
    {
        // Singleton implementation
        private static App _instance;
        public static App Instance => _instance;

        public Action<bool> OnApplicationFocusChanged;

        [NonSerialized] public StoredBinding<bool> ShowAdvancedCursor;
        [NonSerialized] public StoredBinding<float> ImportScale;
        [NonSerialized] public StoredBinding<bool> ShowWorldAxes;
        [NonSerialized] public StoredBinding<bool> ShowWorldYGrid;
        [NonSerialized] public StoredBinding<bool> Passthrough;
        [NonSerialized] public StoredBinding<RealityColorMode> ColorMode;
        [NonSerialized] public StoredBinding<KeyboardLayout> KeyboardLayout;

        [NonSerialized] public StoredBinding<HandlePosition> CurrentHandlePosition;
        [NonSerialized] public StoredBinding<HandleRotation> CurrentHandleRotation;

        /// <summary>
        /// TODO: Move away, just for testing the RealityShape object type
        /// </summary>
        public Material DefaultMaterial;

        private void Awake()
        {
            // Singleton implementation
            if (_instance != null && _instance != this) { Destroy(this); } else { _instance = this; }

            BetterStreamingAssets.Initialize();

            ShowAdvancedCursor = new("App_ShowAdvancedCursor", false);
            ImportScale = new("App_ImportScale", 1.0f);
            ShowWorldAxes = new("App_ShowWorldAxes", true);
            ShowWorldYGrid = new("App_ShowWorldYGrid", true);
            CurrentHandlePosition = new("App_HandlePosition", HandlePosition.Center);
            CurrentHandleRotation = new("App_HandleRotation", HandleRotation.Global);
            ColorMode = new("App_ColorMode", RealityColorMode.HSV);
            KeyboardLayout = new("App_KeyboardLayout", Input.KeyboardLayout.Qwerty);
            Passthrough = new(string.Join('_', nameof(App), nameof(Passthrough)), true);

            OVRManager.InputFocusAcquired += OVRManager_InputFocusAcquired;
            OVRManager.InputFocusLost += OVRManager_InputFocusLost;
        }

        private void Start()
        {
            //Application.targetFrameRate = 90;
            InitializeDirectories();
        }

        private void OVRManager_InputFocusLost()
        {
            OnApplicationFocusChanged?.Invoke(false);
            UserData.Save();
        }

        private void OVRManager_InputFocusAcquired()
        {
            OnApplicationFocusChanged?.Invoke(true);
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                UserData.Save();
            }
        }

        private void OnApplicationQuit()
        {
            UserData.Save();
        }

        /// <summary>
        /// Initialize app directories
        /// </summary>
        private void InitializeDirectories()
        {
            string[] directories = new string[]
            {
                Constants.LocalAssetsDirectoryPath,
                Constants.DocumentsDirectoryPath,
                Constants.TrashDirectoryPath
            };

            foreach (string directoryPath in directories)
            {
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
            }
        }
    }
}

