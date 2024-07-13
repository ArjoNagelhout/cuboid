// 
// SelectionController.cs
// Cuboid
// 
// Copyright 2023 ShapeReality
// 

using System.Collections;
using System.Collections.Generic;
using Cuboid.Models;
using UnityEngine;
using System;
using Cuboid.Utils;

namespace Cuboid
{
    /// <summary>
    /// Calculates the bounding box around all selected objects
    /// and updates the selected RealityObjectData's
    /// transforms based on the updated selection transform.
    ///
    /// The updated transforms can be applied to the RealityObjects
    /// using <see cref="GetTransformCommand"/>
    /// and this is then added to the Undo stack.
    /// </summary>
    public sealed class SelectionController : MonoBehaviour
    {
        private static SelectionController _instance;
        public static SelectionController Instance => _instance;

        [NonSerialized]
        public Binding<Selection> Selection = new Binding<Selection>();

        private RealityDocumentController _realityDocumentController;
        private RealitySceneController _realitySceneController;
        private Action<Dictionary<Guid, RealityObject>> _onInstantiatedRealityObjectsChanged;

        private Action<RealityDocument> _onRealityDocumentChanged;

        private Action<RealityObjectData> _onInstantiatedRealityObject;

        public class RealityObjectTransformData
        {
            public TransformData InitialTransformData;
            public TransformData CurrentTransformData;
        }

        public TransformData InitialSelectionTransformData;

        /// <summary>
        /// Should be set via <see cref="SetCurrentSelectionTransformData"/>
        /// </summary>
        public TransformData CurrentSelectionTransformData { get; private set; }

        /// <summary>
        /// Adds hints for whether it has only set the position, rotation etc.
        /// This way, because it calculates the reality object instance transforms
        /// using a matrix, it can be ensured that when just setting the position,
        /// just the position of the objects get changed.
        /// </summary>
        public void SetCurrentSelectionTransformData(TransformData newTransformData)
        {
            CurrentSelectionTransformData = newTransformData;
            SelectionTransformDataChanged?.Invoke(CurrentSelectionTransformData);
        }

        /// <summary>
        /// Doesn't get set internally, should be changed by external class, e.g.
        /// <see cref="SelectCommand"/>or <see cref="TransformCommand"/>. 
        /// </summary>
        internal Quaternion SelectionTransformRotation = Quaternion.identity;

        public Action<TransformData> SelectionTransformDataChanged;
        private Dictionary<RealityObject, RealityObjectTransformData> _instancesTransformData;

        private void Awake()
        {
            // Singleton implemention
            if (_instance != null && _instance != this) { Destroy(this); } else { _instance = this; }
        }

        private void Start()
        {
            _realityDocumentController = RealityDocumentController.Instance;
            _realitySceneController = RealitySceneController.Instance;

            _onInstantiatedRealityObjectsChanged = OnInstantiatedRealityObjectsChanged;
            _onInstantiatedRealityObject = OnInstantiatedRealityObject;
            _onRealityDocumentChanged = OnRealityDocumentLoaded;
            Register();

            RecalculateSelectionTransformData();
        }

        private void OnRealityDocumentLoaded(RealityDocument realityDocument)
        {
            Reset();
        }

        private void Reset()
        {
            Selection.Value.SelectedRealityObjects.Clear();
        }

        public void UpdateSelection(IEnumerable<RealityObjectData> objectsToSelect, IEnumerable<RealityObjectData> objectsToDeselect)
        {
            Selection.Value.SelectedRealityObjects.UnionWith(new HashSet<RealityObjectData>(objectsToSelect));
            Selection.Value.SelectedRealityObjects.ExceptWith(new HashSet<RealityObjectData>(objectsToDeselect));

            // Change the appearance of the objects
            foreach (RealityObjectData realityObject in objectsToSelect)
            {
                realityObject.Selected.Value = true;
            }

            // Change the appearance of the objects
            foreach (RealityObjectData realityObject in objectsToDeselect)
            {
                realityObject.Selected.Value = false;
            }

            string str = "selection: [";
            int i = 0;
            foreach (RealityObjectData obj in Selection.Value.SelectedRealityObjects)
            {
                str += $"{i++}: {obj.Name.Value}, ";
            }
            str += "]";
            Debug.Log(str);
        }

        private void OnInstantiatedRealityObjectsChanged(Dictionary<Guid, RealityObject> realityObjects)
        {
            Selection.ValueChanged(); // to make sure the Select All, Deselect All and Delete Selection buttons get changed depending on whether they can be executed.
        }

        private void OnInstantiatedRealityObject(RealityObjectData realityObjectData)
        {
            // only if the guid is in the selection
            if (!Selection.Value.SelectedRealityObjects.Contains(realityObjectData))
            {
                return;
            }

            RecalculateSelectionTransformData();
        }

        /// <summary>
        /// Executes select all command
        /// </summary>
        public void SelectAll()
        {
            SelectCommand selectAllCommand = SelectCommand.SelectAllCommand(this, RealitySceneController.Instance);
            UndoRedoController.Instance.Execute(selectAllCommand);
        }

        /// <summary>
        /// Executes deselect all command
        /// </summary>
        public void DeselectAll()
        {
            SelectCommand deselectAllCommand = SelectCommand.DeselectAllCommand(this);
            UndoRedoController.Instance.Execute(deselectAllCommand);
        }

        /// <summary>
        /// Executes delete selection command
        /// </summary>
        public void DeleteSelection()
        {
            RemoveCommand deleteSelectionCommand = new RemoveCommand(
                RealityDocumentController.Instance,
                _realitySceneController,
                this,
                Selection.Value.SelectedRealityObjects,
                _realitySceneController.OpenedRealitySceneIndex);
            UndoRedoController.Instance.Execute(deleteSelectionCommand);
        }

        #region Selection transform

        public Quaternion CalculateSelectionTransformRotation()
        {
            return SelectionController.CalculateSelectionTransformRotation(Selection.Value.SelectedRealityObjects);
        }

        public static Quaternion CalculateSelectionTransformRotation(IEnumerable<RealityObjectData> realityObjects)
        {
            Quaternion? rotation = null;
            bool useWorldSpace = true;

            foreach (RealityObjectData realityObjectData in realityObjects)
            {
                TransformData data = realityObjectData.Transform.Value;

                if (!rotation.HasValue)
                {
                    // initialize rotation value
                    rotation = data.Rotation;
                    useWorldSpace = false;
                }
                else
                {
                    // compare with the stored rotation
                    // if it's the same, do nothing and compare the next
                    if (rotation.Value != data.Rotation)
                    {
                        // if not the same, break and set useWorldSpace to true,
                        // because one of the rotations in the selection is not the same
                        // this is the same behaviour as is present in design tools
                        // such as Adobe Illustrator. 
                        useWorldSpace = true;
                        break;
                    }
                }
            }

            return useWorldSpace ? Quaternion.identity : rotation.Value;
        }

        /// <summary>
        /// Calculates the new selection transform data and bounds based on the
        /// currently set <see cref="SelectionTransformRotation"/>.
        ///
        /// Sets <see cref="CurrentSelectionTransformData"/>, which the
        /// <see cref="SelectionBoundsGizmosController"/> registers to to update
        /// the selection bounds gizmos. 
        /// </summary>
        public void RecalculateSelectionTransformData()
        {
            // Store the current transform matrices of the RealityObjects
            _instancesTransformData = new Dictionary<RealityObject, RealityObjectTransformData>();

            foreach (RealityObjectData realityObjectData in Selection.Value.SelectedRealityObjects)
            {
                if (!_realitySceneController.InstantiatedRealityObjects.Value
                    .TryGetValue(realityObjectData.Guid, out RealityObject realityObject))
                {
                    //Debug.Log($"{realityObjectData.Name} not yet instantiated");
                    continue;
                };

                TransformData initialTransformData = realityObjectData.Transform.Value; // TransformData is a struct (value type) so it's automatically copied. 
                RealityObjectTransformData data = new RealityObjectTransformData
                {
                    InitialTransformData = initialTransformData,
                    CurrentTransformData = initialTransformData
                };

                _instancesTransformData.TryAdd(realityObject, data);
            }

            TransformData rotatedSelectionTransform = new TransformData(Vector3.zero, SelectionTransformRotation, Vector3.one);
            Bounds totalBounds = BoundsUtils.GetBoundsTransformed(_instancesTransformData.Keys, rotatedSelectionTransform);

            Vector3 position = rotatedSelectionTransform.Matrix * totalBounds.center;
            Quaternion rotation = SelectionTransformRotation;
            Vector3 scale = totalBounds.extents * 2;

            InitialSelectionTransformData = new TransformData(position, rotation, scale);
            SetCurrentSelectionTransformData(InitialSelectionTransformData);
        }

        /// <summary>
        /// Updates all selected RealityObjectInstances' transforms based on the updated
        /// <see cref="CurrentSelectionTransformData"/>.
        ///
        /// Note: This is for preview purposes during manipulation. After manipulation is complete
        /// <see cref="GetTransformCommand"/> should be called.
        ///
        /// The calculated values for the new transforms are stored in <see cref="_instancesTransformData"/>
        /// so that they don't have to be recalculated when applying the transforms. 
        /// </summary>
        /// <param name="realityObjectInstancesData"></param>
        internal void UpdateRealityObjectInstanceTransforms()
        {
            Matrix4x4 currentLocalToWorldMatrix = CurrentSelectionTransformData.LocalToWorldMatrix;

            // Uses the _gizmosTransform to set all transforms
            foreach (KeyValuePair<RealityObject, RealityObjectTransformData> realityObjectWithTransformData in _instancesTransformData)
            {
                RealityObject realityObject = realityObjectWithTransformData.Key;
                RealityObjectTransformData transformData = realityObjectWithTransformData.Value;

                Matrix4x4 localMatrix = InitialSelectionTransformData.WorldToLocalMatrix * transformData.InitialTransformData.LocalToWorldMatrix;

                Matrix4x4 newTransformMatrix = currentLocalToWorldMatrix * localMatrix;
                TransformData newTransformData = new TransformData(newTransformMatrix);

                realityObject.transform.SetFromTransformData(newTransformData);
                transformData.CurrentTransformData = newTransformData;
            }
        }

        /// <summary>
        /// Applies the stored RealityObjectInstance transforms that are stored in
        /// <see cref="_instancesTransformData"/>.
        ///
        /// Creates a command that transforms all objects in the same command and
        /// adds it to the undo stack. 
        /// </summary>
        public TransformCommand GetTransformCommand()
        {
            // Add the command to the undo stack
            List<TransformCommand.Data> transformCommandData = new List<TransformCommand.Data>();

            // Loop through all the instances that have been edited, and set the transform command
            // data to the CurrentTransform of the data associated to a given instance. 
            foreach (KeyValuePair<RealityObject, RealityObjectTransformData> realityObjectWithTransformData in _instancesTransformData)
            {
                RealityObject realityObject = realityObjectWithTransformData.Key;
                RealityObjectTransformData transformData = realityObjectWithTransformData.Value;

                TransformCommand.Data newTransformCommandData = new TransformCommand.Data()
                {
                    RealityObjectData = realityObject.RealityObjectData,
                    NewTransformData = transformData.CurrentTransformData
                };
                transformCommandData.Add(newTransformCommandData);
            }

            return new TransformCommand(this, transformCommandData, CurrentSelectionTransformData.Rotation);
        }

        #endregion

        #region Action registration

        private void Register()
        {
            if (_realitySceneController != null)
            {
                _realitySceneController.InstantiatedRealityObjects.Register(_onInstantiatedRealityObjectsChanged);
                _realitySceneController.OnInstantiatedRealityObject += _onInstantiatedRealityObject;
            }

            if (_realityDocumentController != null)
            {
                _realityDocumentController.RealityDocument.Register(_onRealityDocumentChanged);
            }
        }

        private void Unregister()
        {
            if (_realitySceneController != null)
            {
                _realitySceneController.InstantiatedRealityObjects.Unregister(_onInstantiatedRealityObjectsChanged);
                _realitySceneController.OnInstantiatedRealityObject -= _onInstantiatedRealityObject;
            }

            if (_realityDocumentController != null)
            {
                _realityDocumentController.RealityDocument.Unregister(_onRealityDocumentChanged);
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

