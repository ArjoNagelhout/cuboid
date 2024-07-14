//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid.UI
{
    [RequireComponent(typeof(Vector2Field))]
    public class Vector2Property : MonoBehaviour, IProperty<Vector2>
    {
        private Vector2Field _vector2Field;

        public Property<Vector2> Property { get; private set; }

        private void Awake()
        {
            _vector2Field = GetComponent<Vector2Field>();

            Property = new Vector2PropertyInternal(_vector2Field);
        }

        private void OnDestroy()
        {
            Property.Dispose();
        }

        /// <summary>
        /// 
        /// </summary>
        public class Vector2PropertyInternal : Property<Vector2>
        {
            private Vector2Field _vector2Field;

            public Vector2PropertyInternal(Vector2Field vector2Field) : base()
            {
                _vector2Field = vector2Field;
                _vector2Field.X.OnSetValue = (value) => { _sourceBinding.Value = _sourceBinding.Value.SetX(value); ConfirmValue(); };
                _vector2Field.Y.OnSetValue = (value) => { _sourceBinding.Value = _sourceBinding.Value.SetY(value); ConfirmValue(); };
            }

            protected override void OnValueChanged(Vector2 value)
            {
                base.OnValueChanged(value);

                // update the values of the value fields
                _vector2Field.ValueX.Value = value.x;
                _vector2Field.ValueY.Value = value.y;
            }

            protected override void CalculateIsEditingMultiple()
            {
                (bool, bool) isEditingMultiple = IsEditingMultiple(_targetBindings);
                _vector2Field.X.IsEditingMultiple = isEditingMultiple.Item1;
                _vector2Field.Y.IsEditingMultiple = isEditingMultiple.Item2;
            }

            new private static (bool, bool) IsEditingMultiple(List<IBinding<Vector2>> targetBindings)
            {
                bool x = false, y = false;

                if (targetBindings == null) { return (false, false); }

                Vector3 previousValue = targetBindings[0].Value;
                for (int i = 1; i < targetBindings.Count; i++)
                {
                    Vector3 currentValue = targetBindings[i].Value;

                    if (previousValue.x != currentValue.x) { x = true; }
                    if (previousValue.y != currentValue.y) { y = true; }

                    if (x && y) { break; }

                    previousValue = currentValue;
                }

                return (x, y);
            }

            protected override SetPropertyCommand<Vector2> GetCommand(bool passPreviousValues)
            {
                return new SetPropertyCommandVector2(_targetBindings, _previousValue, _sourceBinding.Value, passPreviousValues ? _previousValues : null);
            }
        }
    }
}
