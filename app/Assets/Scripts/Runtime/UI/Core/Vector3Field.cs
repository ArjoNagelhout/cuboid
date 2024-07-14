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
    public class Vector3Field : MonoBehaviour
    {
        public ValueField X;
        public ValueField Y;
        public ValueField Z;

        [NonSerialized] public Binding<float> ValueX = new();
        [NonSerialized] public Binding<float> ValueY = new();
        [NonSerialized] public Binding<float> ValueZ = new();

        private void SetBindings()
        {
            X.SetBinding(ValueX);
            Y.SetBinding(ValueY);
            Z.SetBinding(ValueZ);
        }

        private void Awake()
        {
            SetBindings();
        }
    }
}
