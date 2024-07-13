using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid.UI
{
    public interface IProperty<T>
    {
        public Property<T> Property { get; }
    }

    public interface IGenericProperty
    {
        public Property<T> CreateProperty<T>();
    }

    /// <summary>
    /// An abstract class that all properties (e.g. Vector3Property) should subclass
    /// from to enable the PropertiesViewController to instantiate the properties
    /// and registering them to listen to changes that are made by the user.
    ///
    /// For an example:
    /// See the <see cref="FloatProperty"/> or <see cref="Vector3Property"/> implementation.
    /// </summary>
    public abstract class Property<T> : IDisposable
    {
        /// <summary>
        /// The _sourceBinding shouldn't be directly used by the subclass of the
        /// Property, because this will create a circular reference and result in
        /// an overflow.
        /// You need to create a separate Binding (or via some other method) that
        /// stores the value. When the value changes, you should update the _sourceBinding.
        ///
        /// There's a virtual <see cref="OnValueChanged(T)"/> method that can be used to
        /// update this separate value. 
        /// </summary>
        protected IBinding<T> _sourceBinding;
        protected Action<T> _onValueChanged;

        protected T[] _previousValues;
        protected T _previousValue;
        protected List<IBinding<T>> _targetBindings = new();

        /// <summary>
        /// Set this to false if you don't want to add the property change onto the
        /// undo stack. 
        /// </summary>
        public bool CanUndo = true;

        /// <summary>
        /// These values are used for example for the slider.
        /// The slider has already set the values of the bindings to the new value
        /// (so that live updating is possible, e.g. for the CornerRadius of the Cuboid shape)
        ///
        /// But then, it will calculate the delta on ConfirmValue(), and see that it hasn't
        /// changed, won't set _changed to true and thus won't add the SetPropertyCommand to the stack.
        ///
        /// Therefore we need to store the previous values of the bindings somewhere, so that
        /// the command can calculate the delta from where the user started dragging. 
        /// </summary>
        private void StorePreviousValues()
        {
            _previousValue = _sourceBinding.Value;
            _previousValues = new T[_targetBindings.Count];
            for (int i = 0; i < _targetBindings.Count; i++)
            {
                IBinding<T> binding = _targetBindings[i];
                _previousValues[i] = binding.Value;
            }
        }

        public Property()
        {
            _onValueChanged = OnValueChanged;
        }

        /// <summary>
        /// Add code here for setting the values for individual elements (e.g. ValueFields)
        /// </summary>
        protected virtual void OnValueChanged(T value)
        {
            CalculateIsEditingMultiple();
        }

        protected static bool IsEditingMultiple(List<IBinding<T>> targetBindings)
        {
            bool setValue = false;
            bool isEditingMultiple = false;
            T previousValue = default;

            if (targetBindings == null) { return false; }

            foreach (IBinding<T> targetBinding in targetBindings)
            {
                if (setValue && !targetBinding.Value.Equals(previousValue))
                {
                    isEditingMultiple = true;
                    break;
                }

                previousValue = targetBinding.Value;
                setValue = true;
            }

            return isEditingMultiple;
        }

        /// <summary>
        /// Call to update the value, without adding a command to the undo stack. 
        /// </summary>
        protected void SetValue()
        {
            GetCommand(false).Do();
        }

        /// <summary>
        /// Call to add the <see cref="SetPropertyCommand"/> to the undo stack. 
        /// </summary>
        protected void ConfirmValue()
        {
            if (CanUndo)
            {
                UndoRedoController.Instance.Execute(GetCommand(true));
            }
            else
            {
                // just execute the command, but don't add it to the undo stack
                GetCommand(true).Do();
            }
            StorePreviousValues();
        }

        protected virtual SetPropertyCommand<T> GetCommand(bool passPreviousValues)
        {
            return new SetPropertyCommand<T>(_targetBindings, _previousValue, _sourceBinding.Value, passPreviousValues ? _previousValues : null);
        }

        private PropertiesController.RuntimeSerializedPropertyData _data;
        public PropertiesController.RuntimeSerializedPropertyData Data
        {
            get => _data;
            set
            {
                _data = value;
                OnDataChanged(_data);
            }
        }

        protected abstract void CalculateIsEditingMultiple();

        /// <summary>
        /// Override to set certain properties (e.g. MinValue for a slider) that
        /// are defined in the data.PropertyAttribute
        /// (note: not to be confused with Unity's PropertyAttribute class)
        /// </summary>
        protected virtual void OnDataChanged(PropertiesController.RuntimeSerializedPropertyData data)
        {
            // should get the list of bindings of all the selected reality objects
            // that belong to this specific property.
            List<IBinding<T>> targetBindings = _data.GetBindings<T>(out bool differentValues, out T lastValue);
            IBinding<T> sourceBinding = new BindingWithoutNew<T>(lastValue);

            SetTargetBindings(targetBindings);
            SetBinding(sourceBinding);

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
        private void SetTargetBindings(List<IBinding<T>> targetBindings)
        {
            // unregister
            foreach (IBinding<T> targetBinding in _targetBindings)
            {
                targetBinding.OnValueChanged -= _onValueChanged;
            }

            _targetBindings = targetBindings;

            // register
            if (_targetBindings != null)
            {
                foreach (IBinding<T> targetBinding in _targetBindings)
                {
                    targetBinding.OnValueChanged += _onValueChanged;
                }
            }

            CalculateIsEditingMultiple();
        }

        protected virtual void SetBinding(IBinding<T> binding)
        {
            // unregister
            if (_sourceBinding != null)
            {
                _sourceBinding.Unregister(_onValueChanged);
            }

            _sourceBinding = binding;

            // register
            if (_sourceBinding != null)
            {
                _sourceBinding.Register(_onValueChanged);
            }
        }

        public virtual void Dispose()
        {
            // unregister
            SetTargetBindings(null);
            SetBinding(null);
        }
    }
}
