// 
// SelectCommand.cs
// Cuboid
// 
// Copyright 2023 ShapeReality
// 

using System;
using System.Collections;
using System.Collections.Generic;
using Cuboid.Models;
using UnityEngine;

namespace Cuboid
{
    public class SelectCommand : Command
    {
        private SelectionController _selectionController;
        private SelectOperation _selectOperation;
        private HashSet<RealityObjectData> _objectsToSelect;
        private HashSet<RealityObjectData> _objectsToDeselect;

        private Quaternion _oldSelectionTransformRotation;
        private Quaternion? _newSelectionTransformRotation;

        public enum SelectOperation
        {
            Select,
            Deselect,
            SetTo,
            Toggle
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="selectionController"></param>
        /// <param name="objects"></param>
        /// <param name="selectOperation"></param>
        /// <param name="captureTransformRotation">
        /// Capturing the _selectionTransformRotation should always be done when
        /// using the SelectCommand in singular form,
        ///
        /// but when compounding this with the TransformCommand, which
        /// also captures and updates the _selectionTransformRotation, it will
        /// incorrectly store that already changed rotation, resulting in no
        /// change. 
        /// </param>
        public SelectCommand(
            SelectionController selectionController,
            IEnumerable<RealityObjectData> objects,
            SelectOperation selectOperation = SelectOperation.Select)
        {
            _selectionController = selectionController;
            _selectOperation = selectOperation;

            _oldSelectionTransformRotation = _selectionController.SelectionTransformRotation;

            HashSet<RealityObjectData> selectedObjects = _selectionController.Selection.Value.SelectedRealityObjects;

            switch (_selectOperation)
            {
                case SelectOperation.Select:

                    _objectsToSelect = new HashSet<RealityObjectData>(objects);
                    _objectsToDeselect = new HashSet<RealityObjectData>(); // empty

                    // Remove the objects that were already in the selection
                    _objectsToSelect.ExceptWith(
                        new HashSet<RealityObjectData>(selectedObjects)); // (mutating method)

                    break;
                case SelectOperation.Deselect:

                    _objectsToSelect = new HashSet<RealityObjectData>(); // empty
                    _objectsToDeselect = new HashSet<RealityObjectData>(objects);

                    // Only deselect the objects that are inside the selection
                    _objectsToDeselect.IntersectWith(
                        new HashSet<RealityObjectData>(selectedObjects)); // (mutating method)

                    break;
                case SelectOperation.SetTo:

                    _objectsToSelect = new HashSet<RealityObjectData>(objects);

                    // Remove the objects that were already in the selection
                    _objectsToSelect.ExceptWith(
                        new HashSet<RealityObjectData>(selectedObjects));

                    _objectsToDeselect = new HashSet<RealityObjectData>(selectedObjects);

                    // Remove the objects that should be selected
                    _objectsToDeselect.ExceptWith(new HashSet<RealityObjectData>(objects));

                    break;
                case SelectOperation.Toggle:

                    _objectsToDeselect = new HashSet<RealityObjectData>(objects);
                    _objectsToDeselect.IntersectWith(new HashSet<RealityObjectData>(selectedObjects));

                    _objectsToSelect = new HashSet<RealityObjectData>(objects);
                    _objectsToSelect.ExceptWith(new HashSet<RealityObjectData>(selectedObjects));

                    break;
            }
        }

        public static SelectCommand SelectAllCommand(SelectionController selectionController, RealitySceneController realitySceneController)
        {
            return new SelectCommand(
                selectionController,
                new HashSet<RealityObjectData>(realitySceneController.GetOpenedRealityScene().RealityObjects.Values),
                SelectOperation.Select);
        }

        public static SelectCommand DeselectAllCommand(SelectionController selectionController)
        {
            return new SelectCommand(
                selectionController,
                // Important to copy the data, otherwise the ExceptWith operation will
                // do weird things and cancel out the deselection
                new HashSet<RealityObjectData>(selectionController.Selection.Value.SelectedRealityObjects),
                SelectOperation.Deselect);
        }

        protected override void OnDo(out bool changes, out bool needsSaving)
        {
            needsSaving = false; // selection doesn't need to be saved
            changes = _objectsToSelect.Count > 0 || _objectsToDeselect.Count > 0;

            // To make sure the changes only get propagated when something actually
            // has to be selected or deselected.
            if (!changes) { return; }
            
            _selectionController.UpdateSelection(_objectsToSelect, _objectsToDeselect);
            
            if (!_newSelectionTransformRotation.HasValue)
            {
                _newSelectionTransformRotation = SelectionController.CalculateSelectionTransformRotation(_selectionController.Selection.Value.SelectedRealityObjects);
            }

            _selectionController.SelectionTransformRotation = _newSelectionTransformRotation.Value;
            _selectionController.RecalculateSelectionTransformData();

            _selectionController.Selection.ValueChanged();
        }

        protected override void OnUndo()
        {
            _selectionController.UpdateSelection(_objectsToDeselect, _objectsToSelect); // reversed operation
            
            _selectionController.SelectionTransformRotation = _oldSelectionTransformRotation;
            _selectionController.RecalculateSelectionTransformData();

            _selectionController.Selection.ValueChanged();
        }
    }
}

