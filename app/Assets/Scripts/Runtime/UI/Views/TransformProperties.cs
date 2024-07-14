//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System;
using System.Collections;
using System.Collections.Generic;
using Cuboid.Models;
using Cuboid.UI;
using UnityEngine;

namespace Cuboid
{
    public sealed class TransformProperties : MonoBehaviour
    {
        private SelectionController _selectionController;

        [SerializeField] private GameObject _transformPropertiesHeader;
        [SerializeField] private GameObject _transformPropertiesContent;

        [SerializeField] private Vector3Field Position;
        [SerializeField] private Vector3Field Rotation;
        [SerializeField] private Vector3Field Scale;

        private Action<TransformData> _onSelectionTransformDataChanged;

        private void Start()
        {
            _onSelectionTransformDataChanged = OnSelectionTransformDataChanged;
            _selectionController = SelectionController.Instance;

            Position.X.OnSetValue += (value) => Transform(data => data.SetPositionXMutating(value));
            Position.Y.OnSetValue += (value) => Transform(data => data.SetPositionYMutating(value));
            Position.Z.OnSetValue += (value) => Transform(data => data.SetPositionZMutating(value));

            Rotation.X.OnSetValue += (value) => Transform(data => data.SetRotationXMutating(value));
            Rotation.Y.OnSetValue += (value) => Transform(data => data.SetRotationYMutating(value));
            Rotation.Z.OnSetValue += (value) => Transform(data => data.SetRotationZMutating(value));

            Scale.X.OnSetValue += (value) => Transform(data => data.SetScaleXMutating(value));
            Scale.Y.OnSetValue += (value) => Transform(data => data.SetScaleYMutating(value));
            Scale.Z.OnSetValue += (value) => Transform(data => data.SetScaleZMutating(value));

            Position.X.OnConfirmValue += (value) => Transform(data => data.SetPositionXMutating(value), apply: true);
            Position.Y.OnConfirmValue += (value) => Transform(data => data.SetPositionYMutating(value), apply: true);
            Position.Z.OnConfirmValue += (value) => Transform(data => data.SetPositionZMutating(value), apply: true);

            Rotation.X.OnConfirmValue += (value) => Transform(data => data.SetRotationXMutating(value), apply: true);
            Rotation.Y.OnConfirmValue += (value) => Transform(data => data.SetRotationYMutating(value), apply: true);
            Rotation.Z.OnConfirmValue += (value) => Transform(data => data.SetRotationZMutating(value), apply: true);

            Scale.X.OnConfirmValue += (value) => Transform(data => data.SetScaleXMutating(value), apply: true);
            Scale.Y.OnConfirmValue += (value) => Transform(data => data.SetScaleYMutating(value), apply: true);
            Scale.Z.OnConfirmValue += (value) => Transform(data => data.SetScaleZMutating(value), apply: true);

            Register();
        }

        private delegate TransformData CalculateTransform(TransformData transformData);

        private void Transform(CalculateTransform calculateTransform, bool apply = false)
        {
            List<TransformCommand.Data> data = new List<TransformCommand.Data>();
            HashSet<RealityObjectData> realityObjects = _selectionController.Selection.Value.SelectedRealityObjects;

            foreach (RealityObjectData realityObject in realityObjects)
            {
                TransformData newTransformData = calculateTransform(realityObject.Transform.Value);
                data.Add(new TransformCommand.Data()
                {
                    RealityObjectData = realityObject,
                    NewTransformData = newTransformData
                });

                if (!apply)
                {
                    // HACK: This is to make sure the on value changed gets called (so that the instance transform
                    // gets updated to the preview value),
                    // 
                    // but without having to resort to storing a dictionary with original values
                    // or changing the transform command or exposing any variables of the SelectionController. 
                    realityObject.Transform.OnValueChanged?.Invoke(newTransformData);
                }
            }

            if (apply)
            {
                TransformCommand transformCommand = new TransformCommand(
                _selectionController, data, _selectionController.SelectionTransformRotation, true);
                UndoRedoController.Instance.Execute(transformCommand);
            }
        }

        private void SetVisible(bool visible)
        {
            _transformPropertiesHeader.SetActive(visible);
            _transformPropertiesContent.SetActive(visible);
        }

        private void UpdatePropertyValues(Selection selection)
        {
            // make sure to set the transform value properties
            // these are not relative to the selection bounds, but the actual transform
            // values of the objects that are currently selected.

            // when multiple objects are selected: use Unity's behaviour:
            // when the values are the same, show the value, otherwise show a dash --
            // the user can still edit these values

            bool hasObjects = selection.ContainsObjects;
            SetVisible(hasObjects);

            if (!hasObjects)
            {
                return;
            }

            bool initialized = false;
            foreach (RealityObjectData realityObjectData in selection.SelectedRealityObjects)
            {
                TransformData transformData = realityObjectData.Transform.Value;
                SetVector(Position, transformData.Position);
                SetVector(Rotation, transformData.Rotation.eulerAngles);
                SetVector(Scale, transformData.Scale);

                void SetVector(Vector3Field vector3Field, Vector3 newValue)
                {
                    SetValue(vector3Field.X, vector3Field.ValueX, newValue.x);
                    SetValue(vector3Field.Y, vector3Field.ValueY, newValue.y);
                    SetValue(vector3Field.Z, vector3Field.ValueZ, newValue.z);
                }

                void SetValue(ValueField valueField, Binding<float> binding, float newValue)
                {
                    float originalValue = binding.Value;
                    if (!initialized) { valueField.IsEditingMultiple = false; }
                    if (initialized && originalValue != newValue)
                    {
                        valueField.IsEditingMultiple = true;
                    }
                    binding.Value = newValue;
                }
                initialized = true;
            }
        }

        private void OnSelectionChanged(Selection selection)
        {
            UpdatePropertyValues(selection);
        }

        private void OnSelectionTransformDataChanged(TransformData transformData)
        {
            UpdatePropertyValues(_selectionController.Selection.Value);
        }

        #region Action registration

        private void Register()
        {
            if (_selectionController != null)
            {
                _selectionController.Selection.Register(OnSelectionChanged);
                _selectionController.SelectionTransformDataChanged += _onSelectionTransformDataChanged;
            }
        }

        private void Unregister()
        {
            if (_selectionController != null)
            {
                _selectionController.Selection.Unregister(OnSelectionChanged);
                _selectionController.SelectionTransformDataChanged -= _onSelectionTransformDataChanged;
            }
        }

        private void OnEnable()
        {
            Register();
        }

        private void OnDisable()
        {
            Unregister();
        }

        private void OnDestroy()
        {
            Unregister();
        }

        #endregion
    }
}
