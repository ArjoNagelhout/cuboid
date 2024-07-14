//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cuboid.Utils;

namespace Cuboid.UI
{
    public class SpatialContextMenu : MonoBehaviour
    {
        [SerializeField] private float _referenceSize;

        public Transform ContextMenuTransform;

        public ContextMenu ContextMenu;

        //[SerializeField] private 
        // rotate towards user and scale
        private void Update()
        {
            Vector3 cameraPosition = Camera.main.transform.position;
            Vector3 position = transform.position;
            Vector3 delta = position - cameraPosition;
            transform.localRotation = Quaternion.LookRotation(delta, Vector3.up);

            transform.localScale = Vector3.Distance(position, cameraPosition) * Vector3.one * _referenceSize;
        }
    }
}
