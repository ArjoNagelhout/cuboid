// Copyright (c) 2023 Arjo Nagelhout

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid
{
    public interface IHasPrimaryColor
    {
        public Binding<RealityColor> PrimaryColor { get; set; }
    }

    /// <summary>
    /// ColorsController is responsible for storing the last selected color,
    /// and can be retrieved
    /// </summary>
    public class ColorsController : MonoBehaviour
    {
        private static ColorsController _instance;
        public static ColorsController Instance => _instance;

        public StoredBinding<RealityColor> ActiveColor;

        private SelectionController _selectionController;
        private Action<Selection> _onSelectionChanged;

        private void Awake()
        {
            // Singleton implementation
            if (_instance != null && _instance != this) { Destroy(this); } else { _instance = this; }

            ActiveColor = new("ColorsController_ActiveColor", new RealityColor());
        }

        private void Start()
        {
            _selectionController = SelectionController.Instance;
            _onSelectionChanged = OnSelectionChanged;

            _onValueChanged = OnValueChanged;

            Register();
        }

        /// <summary>
        /// Cached list of objects that should get their color changed
        /// </summary>
        private List<IBinding<RealityColor>> _targetBindings = new List<IBinding<RealityColor>>();
        private RealityColor _previousValue;
        private RealityColor[] _previousValues;
        private Action<RealityColor> _onValueChanged;

        private void StorePreviousValues()
        {
            _previousValue = ActiveColor.Value;
            _previousValues = new RealityColor[_targetBindings.Count];
            for (int i = 0; i < _targetBindings.Count; i++)
            {
                IBinding<RealityColor> binding = _targetBindings[i];
                _previousValues[i] = binding.Value;
            }
        }

        private void OnValueChanged(RealityColor color)
        {
            ActiveColor.Value = color;
        }

        public void OnSetValue(RealityColor color)
        {
            GetCommand(false).Do();
        }

        public void OnConfirmValue(RealityColor color)
        {
            UndoRedoController.Instance.Execute(GetCommand(true));
            StorePreviousValues();
        }

        private void OnSelectionChanged(Selection selection)
        {
            List<IBinding<RealityColor>> newTargetBindings = new List<IBinding<RealityColor>>();
            RealityColor? newColor = null;
            foreach (RealityObjectData realityObject in selection.SelectedRealityObjects)
            {
                IHasPrimaryColor primaryColorObject = realityObject as IHasPrimaryColor;
                if (primaryColorObject == null) { continue; }

                newColor = primaryColorObject.PrimaryColor.Value;

                newTargetBindings.Add(primaryColorObject.PrimaryColor);
            }
            if (newColor.HasValue)
            {
                ActiveColor.Value = newColor.Value;
            }

            SetTargetBindings(newTargetBindings);
            StorePreviousValues();
        }

        /// <summary>
        /// The target bindings should be listened to by the Property
        /// because if the SetPropertyCommand gets executed, it does not update
        /// the source binding (the _binding in this class), but it updates
        /// the target bindings.
        //
        /// So, if any target bindings get changed, the source binding value
        /// should be changed.
        ///
        /// Note: The target bindings are the bindings stored in the <see cref="RealityObjectData"/>
        /// instances. 
        /// </summary>
        private void SetTargetBindings(List<IBinding<RealityColor>> targetBindings)
        {
            // unregister
            if (_targetBindings != null)
            {
                foreach (IBinding<RealityColor> targetBinding in _targetBindings)
                {
                    if (targetBinding == null) { continue; }
                    targetBinding.OnValueChanged -= _onValueChanged;
                }
            }

            _targetBindings = targetBindings;

            // register
            if (_targetBindings != null)
            {
                foreach (IBinding<RealityColor> targetBinding in _targetBindings)
                {
                    targetBinding.OnValueChanged += _onValueChanged;
                }
            }
        }

        private SetPropertyCommand<RealityColor> GetCommand(bool passPreviousValues)
        {
            return new SetPropertyCommand<RealityColor>(_targetBindings, _previousValue, ActiveColor.Value, passPreviousValues ? _previousValues : null);
        }

        #region Action registration

        private void Register()
        {
            if (_selectionController != null)
            {
                _selectionController.Selection.Register(_onSelectionChanged);
            }
        }

        private void Unregister()
        {
            if (_selectionController != null)
            {
                _selectionController.Selection.Unregister(_onSelectionChanged);
            }

            SetTargetBindings(null);
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
