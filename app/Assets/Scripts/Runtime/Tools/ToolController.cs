//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cuboid.Utils;
using Cuboid.Input;

namespace Cuboid
{
    public interface IToolHasProperties
    {
    }

    /// <summary>
    /// ToolController is responsible for storing the active tool state
    /// and instantiating the active tool
    /// </summary>
    public class ToolController : MonoBehaviour
    {
        private static ToolController _instance;
        public static ToolController Instance => _instance;

        public StoredBinding<Tool> ActiveTool;

        /// <summary>
        /// The different tools that are available in the design app
        /// </summary>
        public enum Tool : int
        {
            SelectTool = 0,

            TranslateTool,

            RotateTool,

            ScaleTool,

            SelectionBoundsTool,

            DrawShapeTool,

            TextTool
        }

        [System.Serializable]
        public class ToolData
        {
            public string Name;
            public Sprite Icon;
        }

        [SerializeField] private PrefabDictionary<Tool> _toolPrefabs;
        public Cuboid.Utils.SerializableDictionary<Tool, ToolData> ToolsData;

        private SpatialInputModule _spatialInputModule;

        private void Awake()
        {
            // Singleton implementation
            if (_instance != null && _instance != this) { Destroy(this); } else { _instance = this; }

            ActiveTool = new("ActiveTool", Tool.SelectTool);
        }

        private void Start()
        {
            _spatialInputModule = SpatialInputModule.Instance;

            ActiveTool.Register(OnActiveToolChanged);
        }

        [NonSerialized] public Binding<GameObject> InstantiatedTool = new(null);

        private void OnActiveToolChanged(Tool tool)
        {
            InstantiateTool(tool);

            // notify the spatial input module, otherwise it won't try to get the reticle information
            // because the object that is being hovered over hasn't changed.
            _spatialInputModule.GetConfigurationOutsideUINextUpdate = true;
        }

        private void InstantiateTool(Tool tool)
        {
            // Remove the previous panel if it's still active
            if (InstantiatedTool.Value != null)
            {
                Destroy(InstantiatedTool.Value);
                InstantiatedTool.Value = null;
            }

            _toolPrefabs.InstantiateAsync(tool, transform, (result) =>
            {
                InstantiatedTool.Value = result;

                // make sure to enable or disable the DefaultSelectBehaviour based on whether the currently
                // instantiated tool has DefaultSelectBehaviour enabled.
                if (result.TryGetComponent<IToolHasDefaultSelectBehaviour>(out IToolHasDefaultSelectBehaviour _))
                {
                    // if already active and enabled, don't do anything
                    if (!DefaultSelectBehaviour.Instance.isActiveAndEnabled)
                    {
                        DefaultSelectBehaviour.Instance.enabled = true; // this will call register again
                    }
                }
                else
                {
                    // set enabled to false
                    DefaultSelectBehaviour.Instance.enabled = false;
                }
            });
        }
    }
}

