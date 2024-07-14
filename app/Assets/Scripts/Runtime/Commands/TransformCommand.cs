//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System;
using System.Collections.Generic;
using Cuboid.Models;
using UnityEngine;

namespace Cuboid
{
    /// <summary>
    /// <para>
    /// The <see cref="TransformCommand"/> allows for transforming a collection
    /// of <see cref="OldRealityObject"/>s.
    /// </para>
    ///
    /// <para>
    /// It also stores the <see cref="SelectionController.SelectionTransformRotation"/>,
    /// so that when rotating a collection of RealityObjects, the selection
    /// transform doesn't jarringly pop back into world space rotation. 
    /// </para>
    /// </summary>
    public class TransformCommand : Command
    {
        public class Data
        {
            public RealityObjectData RealityObjectData;
            public TransformData NewTransformData;
        }

        private class InternalData
        {
            public RealityObjectData RealityObjectData;
            public TransformData OldTransformData;
            public TransformData NewTransformData;
        }

        private List<InternalData> _internalRealityObjectsWithTransformData;
        private Quaternion _oldSelectionTransformRotation;
        private Quaternion _newSelectionTransformRotation;
        private SelectionController _selectionController;

        private bool _recalculateSelectionTransformRotation = false;

        public TransformCommand(
            SelectionController selectionController,
            IEnumerable<Data> realityObjectsWithTransformData,
            Quaternion newSelectionTransformRotation,
            bool recalculateSelectionTransformRotation = false)
        {
            _selectionController = selectionController;
            _oldSelectionTransformRotation = _selectionController.SelectionTransformRotation;
            _newSelectionTransformRotation = newSelectionTransformRotation;
            _recalculateSelectionTransformRotation = recalculateSelectionTransformRotation;

            _internalRealityObjectsWithTransformData = new List<InternalData>();
            foreach (Data data in realityObjectsWithTransformData)
            {
                _internalRealityObjectsWithTransformData.Add(new InternalData()
                {
                    RealityObjectData = data.RealityObjectData,
                    NewTransformData = data.NewTransformData,
                    OldTransformData = data.RealityObjectData.Transform.Value // copies the current transform data
                });
            }
        }

        protected override void OnDo(out bool changes, out bool needsSaving)
        {
            changes = true;
            needsSaving = true;

            foreach (InternalData data in _internalRealityObjectsWithTransformData)
            {
                data.RealityObjectData.Transform.Value = data.NewTransformData;
            }

            if (_recalculateSelectionTransformRotation)
            {
                // should recalculate, e.g. TransformCommand is not called by the
                // SelectionController but by some other script, such as the PropertiesViewController
                _newSelectionTransformRotation = _selectionController.CalculateSelectionTransformRotation();
            }

            _selectionController.SelectionTransformRotation = _newSelectionTransformRotation;
            _selectionController.RecalculateSelectionTransformData();
        }

        protected override void OnUndo()
        {
            foreach (InternalData data in _internalRealityObjectsWithTransformData)
            {
                data.RealityObjectData.Transform.Value = data.OldTransformData;
            }

            _selectionController.SelectionTransformRotation = _oldSelectionTransformRotation;
            _selectionController.RecalculateSelectionTransformData();
        }
    }
}

