// 
// AddCommand.cs
// Cuboid
// 
// Copyright 2023 ShapeReality
// 

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Cuboid
{
    /// <summary>
    /// Adds a RealityObjectData to the RealityDocument and selected RealityScene
    ///
    /// 
    /// The RealitySceneController contains data for which reality objects are being instantiated
    /// The AddCommand can modify this data directly, no abstraction in the RealitySceneController needed.
    /// </summary>
    public class AddCommand : Command
    {
        private RealityDocumentController _realityDocumentController;
        private RealitySceneController _realitySceneController;
        private int _realitySceneIndex;
        private IEnumerable<RealityObjectData> _realityObjectsData;

        /// <summary>
        /// Convenience constructor for when adding one object to the scene
        /// </summary>
        public AddCommand(
            RealityDocumentController realityDocumentController,
            RealitySceneController realitySceneController,
            int realitySceneIndex,
            RealityObjectData realityObjectData)
        {
            _realityDocumentController = realityDocumentController;
            _realitySceneController = realitySceneController;
            _realitySceneIndex = realitySceneIndex;
            _realityObjectsData = new List<RealityObjectData>() { realityObjectData };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="realityObjects">Already instantiated objects</param>
        public AddCommand(
            RealityDocumentController realityDocumentController,
            RealitySceneController realitySceneController,
            int realitySceneIndex,
            IEnumerable<RealityObjectData> realityObjectsData)
        {
            _realityDocumentController = realityDocumentController;
            _realitySceneController = realitySceneController;
            _realitySceneIndex = realitySceneIndex;
            _realityObjectsData = realityObjectsData;
        }

        protected override void OnDo(out bool changes, out bool needsSaving)
        {
            changes = true;
            needsSaving = true;

            foreach (RealityObjectData realityObjectData in _realityObjectsData)
            {
                _realitySceneController.Instantiate(realityObjectData);

                // data can always be modified directly
                _realityDocumentController.RealityDocument.Value.ScenesData[_realitySceneIndex]
                    .RealityObjects.Add(realityObjectData.Guid, realityObjectData);
            }
        }

        protected override void OnUndo()
        {
            foreach (RealityObjectData realityObjectData in _realityObjectsData)
            {
                _realitySceneController.Destroy(realityObjectData);

                // data can always be modified directly
                _realityDocumentController.RealityDocument.Value.ScenesData[_realitySceneIndex]
                    .RealityObjects.Remove(realityObjectData.Guid);
            }
        }
    }
}

