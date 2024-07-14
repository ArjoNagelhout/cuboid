// Copyright (c) 2023 Arjo Nagelhout

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid
{
    public class RoundedCubeTest : MonoBehaviour
    {
        private Vector3 _lastVector = Vector3.zero;

        [SerializeField] private RoundedCuboidRenderer _roundedCubeGenerator;

        private void Update()
        {
            if (!transform.localScale.RoughlyEquals(_lastVector))
            {
                _lastVector = transform.localScale;
                _roundedCubeGenerator.GenerateMesh(_lastVector, 0.5f, 2);
            }
            _roundedCubeGenerator.transform.SetLocalPositionAndRotation(transform.localPosition, transform.localRotation);
        }
    }
}
