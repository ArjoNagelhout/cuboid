// Copyright (c) 2023 Arjo Nagelhout

using System.Collections;
using System.Collections.Generic;
using Cuboid.Input;
using Cuboid.Models;
using Shapes;
using UnityEngine;

namespace Cuboid
{
    public class SelectionBoundsHandleData : HandleData
    {
        /// <summary>
        /// The directional vector that gets scaled along
        ///
        ///             ^ <- this vector
        ///            /
        ///           /
        /// 0 ------ 0
        /// |        |
        /// |        |
        /// 0 ------ 0
        ///
        /// Or rotated.
        ///
        /// Is in local space, example values:
        /// 
        /// = (1, 0, 0)
        /// = (1, 1, -1)
        /// = (0, 1, -1)
        /// </summary>
        public Vector3 Position;

        public enum HandleType
        {
            Corner,
            Face,
            Edge
        }

        public HandleType handleType;
    }

    public class SelectionBoundsHandle : Handle<SelectionBoundsHandleData>,
        ISpatialPointerCustomReticle
    {
        [Header("Reticle")]
        [SerializeField] private bool _useDefaultReticle = true;
        [SerializeField] protected SpatialPointerReticleData _reticleData;
        SpatialPointerReticleData ISpatialPointerCustomReticle.ReticleData
        {
            get
            {
                if (_useDefaultReticle) { return SpatialPointerReticleData.Default; }

                // assign the calculate rotation method to the update rotation delegate, so that
                // the reticle can call this to get the new rotation
                _reticleData.updateRotation = CalculateReticleRotation;
                return _reticleData;
            }
            set => _reticleData = value;
        }

        [Header("Appearance")]
        [SerializeField] private Material _normalMaterial;
        [SerializeField] private Material _hoveredMaterial;
        [SerializeField] private Material _pressedMaterial;

        [SerializeField] private MeshRenderer _meshRenderer;

        protected override void UpdateHoverPressedAppearance()
        {
            if (Pressed)
            {
                _meshRenderer.material = _pressedMaterial;
            }
            else if (Hovered)
            {
                _meshRenderer.material = _hoveredMaterial;
            }
            else
            {
                _meshRenderer.material = _normalMaterial;
            }
        }

        private Quaternion CalculateReticleRotation()
        {
            TransformData td = SelectionController.Instance.CurrentSelectionTransformData;

            switch (Data.handleType)
            {
                case SelectionBoundsHandleData.HandleType.Face: // face and corner share implementation
                case SelectionBoundsHandleData.HandleType.Corner:
                    // face the camera, but lock (such as is achieved in the line renderer)
                    {
                        Vector3 worldPosition = td.LocalToWorldMatrix.MultiplyPoint3x4(Data.Position);
                        Vector3 cameraPosition = Camera.main.transform.position;
                        Vector3 cameraDirection = (cameraPosition - worldPosition).normalized;
                        Vector3 localDeltaPosition = worldPosition - td.Position;

                        Quaternion rot = Quaternion.LookRotation(localDeltaPosition, cameraDirection);

                        // offset, because scale icon is expected to be horizontal:
                        //
                        //    <-->
                        //
                        // and we need
                        //
                        //     ^
                        //     |
                        //     v
                        rot *= Quaternion.Euler(90, 0, 90); 

                        return rot;
                    }
                case SelectionBoundsHandleData.HandleType.Edge:
                    {
                        // rotation, so position along plane that will be rotated on
                        Vector3 rotationPlaneNormal = SelectionBoundsTool.GetRotationPlaneNormal(
                            Data.Position, td);
                        Vector3 localDeltaPosition = td.LocalToWorldMatrix.MultiplyPoint3x4(Data.Position) - td.Position;
                        Quaternion rot = Quaternion.LookRotation(rotationPlaneNormal, localDeltaPosition);

                        // offset because the rotation icon is expected to be
                        // in the following orientation:
                        //
                        // <--_
                        //     |
                        //     v
                        //
                        // and we need:
                        //
                        // <-
                        //    \
                        //     |
                        //    /
                        // <-
                        rot *= Quaternion.Euler(0, 0, 45); 

                        return rot;
                    }
            }

            return Quaternion.identity;
        }
    }
}

