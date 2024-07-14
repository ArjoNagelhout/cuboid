//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cuboid.Models;
using System.Linq;

namespace Cuboid
{
    /// <summary>
    /// Only removes the <see cref="RealityObjectData"/> from the <see cref="RealitySceneData"/>.
    /// Don't call this directly, use the <see cref="RemoveCommand"/>
    /// </summary>
    internal class BaseRemoveCommand : Command
    {
        private RealityDocumentController _realityDocumentController;
        private RealitySceneController _realitySceneController;
        private IEnumerable<RealityObjectData> _realityObjectsData;
        private int _realitySceneIndex;

        public BaseRemoveCommand(
            RealityDocumentController realityDocumentController,
            RealitySceneController realitySceneController,
            IEnumerable<RealityObjectData> realityObjectsData,
            int realitySceneIndex)
        {
            _realityDocumentController = realityDocumentController;
            _realitySceneController = realitySceneController;
            _realityObjectsData = realityObjectsData;
            _realitySceneIndex = realitySceneIndex;
        }

        protected override void OnDo(out bool changes, out bool needsSaving)
        {
            changes = true;
            needsSaving = true;

            foreach (RealityObjectData realityObjectData in _realityObjectsData)
            {
                _realitySceneController.Destroy(realityObjectData);

                // data can always be modified directly
                _realityDocumentController.RealityDocument.Value.ScenesData[_realitySceneIndex]
                    .RealityObjects.Remove(realityObjectData.Guid);
            }
        }

        protected override void OnUndo()
        {
            foreach (RealityObjectData realityObjectData in _realityObjectsData)
            {
                _realitySceneController.Instantiate(realityObjectData);

                // data can always be modified directly
                _realityDocumentController.RealityDocument.Value.ScenesData[_realitySceneIndex]
                    .RealityObjects.Add(realityObjectData.Guid, realityObjectData);
            }
        }
    }

    /// <summary>
    /// Removes the <see cref="RealityObjectData"/> from the <see cref="RealitySceneData"/>,
    /// but keeps it in memory.
    /// 
    /// Before it removes it from the scene, it deselects it
    /// in the <see cref="Selection"/>
    /// </summary>
    public class RemoveCommand : Command
    {
        public RemoveCommand(
            RealityDocumentController realityDocumentController,
            RealitySceneController realitySceneController,
            SelectionController selectionController,
            IEnumerable<RealityObjectData> realityObjectsData,
            int realitySceneIndex)
        {
            SelectCommand deselectCommand = new SelectCommand(selectionController, realityObjectsData,
                SelectCommand.SelectOperation.Deselect);
            Children.Add(deselectCommand);

            // ToList is to ensure that if the IEnumerable is of type HashSet, the deselectCommand
            // doesn't remove it from the realityObjects, (because the ExceptWith method on it is mutating,
            // and thus changes the passed collection to this BaseRemoveCommand. 
            BaseRemoveCommand baseRemoveCommand = new BaseRemoveCommand(
                realityDocumentController,
                realitySceneController,
                realityObjectsData.ToList(),
                realitySceneIndex);
            Children.Add(baseRemoveCommand);
        }
    }
}

