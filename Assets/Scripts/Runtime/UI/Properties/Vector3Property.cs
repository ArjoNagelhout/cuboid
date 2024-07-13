using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid.UI
{
    [RequireComponent(typeof(Vector3Field))]
    public class Vector3Property : MonoBehaviour, IProperty<Vector3>
    {
        private Vector3Field _vector3Field;

        public Property<Vector3> Property { get; private set; }

        private void Awake()
        {
            _vector3Field = GetComponent<Vector3Field>();
            Property = new Vector3PropertyInternal(_vector3Field);
        }

        private void OnDestroy()
        {
            Property.Dispose();
        }

        /// <summary>
        /// 
        /// </summary>
        public class Vector3PropertyInternal : Property<Vector3>
        {
            private Vector3Field _vector3Field;

            public Vector3PropertyInternal(Vector3Field vector3Field) : base()
            {
                _vector3Field = vector3Field;
                _vector3Field.X.OnSetValue = (value) => { _sourceBinding.Value = _sourceBinding.Value.SetX(value); ConfirmValue(); };
                _vector3Field.Y.OnSetValue = (value) => { _sourceBinding.Value = _sourceBinding.Value.SetY(value); ConfirmValue(); };
                _vector3Field.Z.OnSetValue = (value) => { _sourceBinding.Value = _sourceBinding.Value.SetZ(value); ConfirmValue(); };
            }

            protected override void OnValueChanged(Vector3 value)
            {
                base.OnValueChanged(value);

                // update the values of the value fields
                _vector3Field.ValueX.Value = value.x;
                _vector3Field.ValueY.Value = value.y;
                _vector3Field.ValueZ.Value = value.z;
            }

            protected override void CalculateIsEditingMultiple()
            {
                (bool, bool, bool) isEditingMultiple = IsEditingMultiple(_targetBindings);
                _vector3Field.X.IsEditingMultiple = isEditingMultiple.Item1;
                _vector3Field.Y.IsEditingMultiple = isEditingMultiple.Item2;
                _vector3Field.Z.IsEditingMultiple = isEditingMultiple.Item3;
            }

            new private static (bool, bool, bool) IsEditingMultiple(List<IBinding<Vector3>> targetBindings)
            {
                bool x = false, y = false, z = false;

                if (targetBindings == null) { return (false, false, false); }

                Vector3 previousValue = targetBindings[0].Value;
                for (int i = 1; i < targetBindings.Count; i++)
                {
                    Vector3 currentValue = targetBindings[i].Value;

                    if (previousValue.x != currentValue.x) { x = true; }
                    if (previousValue.y != currentValue.y) { y = true; }
                    if (previousValue.z != currentValue.z) { z = true; }

                    if (x && y && z) { break; }

                    previousValue = currentValue;
                }

                return (x, y, z);
            }

            protected override SetPropertyCommand<Vector3> GetCommand(bool passPreviousValues)
            {
                return new SetPropertyCommandVector3(_targetBindings, _previousValue, _sourceBinding.Value, passPreviousValues ? _previousValues : null);
            }
        }
    }
}
