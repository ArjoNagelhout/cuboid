// Copyright (c) 2023 Arjo Nagelhout

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid
{
    /// <summary>
    /// Performs custom processing per value (x, y, z) in the command to determine
    /// the new values and whether anything has changed
    /// </summary>
    public class SetPropertyCommandVector3 : SetPropertyCommand<Vector3>
    {
        public SetPropertyCommandVector3(List<IBinding<Vector3>> bindings, Vector3 previousValue, Vector3 newValue, Vector3[] previousValues = null) : base(bindings, previousValue, newValue, previousValues)
        {
        }

        protected override void SetChangesAndNewValues(Vector3 previousValue, Vector3 newValue)
        {
            // get which one has been changed
            bool changedX = previousValue.x != newValue.x;
            bool changedY = previousValue.y != newValue.y;
            bool changedZ = previousValue.z != newValue.z;

            for (int i = 0; i < _targetBindings.Count; i++)
            {
                IBinding<Vector3> targetBinding = _targetBindings[i];
                Vector3 newTargetValue = targetBinding.Value;

                if (changedX) { newTargetValue.SetXMutating(newValue.x); }
                if (changedY) { newTargetValue.SetYMutating(newValue.y); }
                if (changedZ) { newTargetValue.SetZMutating(newValue.z); }

                if (_previousValues[i] != newTargetValue)
                {
                    _changes = true;
                }

                _newValues[i] = newTargetValue;
            }
        }
    }

    /// <summary>
    /// Performs custom processing per value (x, y, z) in the command to determine
    /// the new values and whether anything has changed
    /// </summary>
    public class SetPropertyCommandVector2 : SetPropertyCommand<Vector2>
    {
        public SetPropertyCommandVector2(List<IBinding<Vector2>> bindings, Vector2 previousValue, Vector2 newValue, Vector2[] previousValues = null) : base(bindings, previousValue, newValue, previousValues)
        {
        }

        protected override void SetChangesAndNewValues(Vector2 previousValue, Vector2 newValue)
        {
            // get which one has been changed
            bool changedX = previousValue.x != newValue.x;
            bool changedY = previousValue.y != newValue.y;

            for (int i = 0; i < _targetBindings.Count; i++)
            {
                IBinding<Vector2> targetBinding = _targetBindings[i];
                Vector2 newTargetValue = targetBinding.Value;

                if (changedX) { newTargetValue.SetXMutating(newValue.x); }
                if (changedY) { newTargetValue.SetYMutating(newValue.y); }

                if (_previousValues[i] != newTargetValue)
                {
                    _changes = true;
                }

                _newValues[i] = newTargetValue;
            }
        }
    }

    /// <summary>
    /// To be used via the Properties view to edit properties of a RealityObject
    /// such as the Corner Radius, Quality or Color. 
    /// </summary>
    public class SetPropertyCommand<T> : Command
    {
        protected List<IBinding<T>> _targetBindings;
        protected T[] _previousValues;
        protected T[] _newValues;

        protected bool _changes = false;

        /// <summary>
        /// Override this method for custom changes and new values calculation
        /// behaviour (e.g. for Vector2 and Vector3)
        /// </summary>
        /// <param name="previousValue"></param>
        /// <param name="newValue"></param>
        protected virtual void SetChangesAndNewValues(T previousValue, T newValue)
        {
            if (!previousValue.Equals(newValue))
            {
                _changes = true;
            }

            // just set the new values to the new value
            for (int i = 0; i < _targetBindings.Count; i++)
            {
                _newValues[i] = newValue;
            }
        }

        public SetPropertyCommand(List<IBinding<T>> bindings, T previousValue, T newValue, T[] previousValues = null)
        {
            _targetBindings = bindings;

            _newValues = new T[_targetBindings.Count];

            if (previousValues == null)
            {
                _previousValues = new T[_targetBindings.Count];
                for (int i = 0; i < _targetBindings.Count; i++)
                {
                    IBinding<T> binding = _targetBindings[i];
                    _previousValues[i] = binding.Value;
                }
            }
            else
            {
                _previousValues = previousValues;
            }

            SetChangesAndNewValues(previousValue, newValue);
        }

        protected override void OnDo(out bool changes, out bool needsSaving)
        {
            needsSaving = _changes;
            changes = _changes;

            if (!changes) { return; }

            // now set the new values
            for (int i = 0; i < _targetBindings.Count; i++)
            {
                IBinding<T> targetBinding = _targetBindings[i];
                targetBinding.Value = _newValues[i];
            }
        }

        protected override void OnUndo()
        {
            // set back to the old values
            for (int i = 0; i < _targetBindings.Count; i++)
            {
                IBinding<T> targetBinding = _targetBindings[i];
                targetBinding.Value = _previousValues[i];
            }
        }
    }
}
