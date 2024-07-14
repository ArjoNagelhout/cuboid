// Copyright (c) 2023 Arjo Nagelhout

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cuboid.Models;
using Cuboid.Utils;
using UnityEngine.EventSystems;
using Cuboid.Input;
using Newtonsoft.Json;

namespace Cuboid
{
    /// <summary>
    /// There are two options:
    ///
    /// 1. If at least one object in the selection prefers proportional scaling,
    ///    it should use proportional scaling
    /// 2. If at least one object doesn't prefer proportional scaling,
    ///    it shouldn't use proportional scaling. 
    /// </summary>
    public interface IPrefersProportionalScaling
    {
    }

    [Serializable]
    public abstract class RealityObjectData
    {
        /// <summary>
        /// Guid, access object
        /// </summary>
        public Guid Guid;

        /// <summary>
        /// Object Name (multiple objects can have the same name)
        /// </summary>
        [RuntimeSerializedPropertyString]
        public BindingWithoutNew<string> Name = new();

        /// <summary>
        /// 
        /// </summary>
        public Binding<TransformData> Transform;

        [NonSerialized]
        public Binding<bool> Selected = new();

        public abstract IEnumerator InstantiateAsync(Action<RealityObject> completed);
    }


    /// <summary>
    /// Abstract class that any object can inherit from that should be able to be
    /// instantiated in the scene, and controlled in the scene hierarchy, transform.
    ///
    /// These are MonoBehaviour instances inside the scene
    /// </summary>
    public abstract class RealityObject : MonoBehaviour
    {
        private Action<TransformData> _onTransformDataChanged = null;
        private Action<bool> _onSelectedChanged = null;

        private float k_MinimumBoundsExtentsSqrMagnitude = 0.0001f;

        private RealityObjectData _realityObjectData = null;
        /// <summary>
        /// The data that contains all transform data, the guid of the object etc.
        /// </summary>
        public RealityObjectData RealityObjectData
        {
            get => _realityObjectData;
            set
            {
                Unregister();

                _realityObjectData = value;

                SetupColliders();

                Register();
            }
        }

        protected virtual void Start()
        {
        }

        private void SetupColliders()
        {
            bool hasValidColliders = false;

            // 1. First determine whether the asset already has any collider active on it,
            // if so, don’t try to add a new collider

            Collider[] existingColliders = GetComponentsInChildren<Collider>();
            foreach (Collider collider in existingColliders)
            {
                // make sure the bounds are not zero
                if (collider.bounds.extents.sqrMagnitude > k_MinimumBoundsExtentsSqrMagnitude)
                {
                    hasValidColliders = true;
                }
            }

            if (hasValidColliders) { return; }

            // 2. If there is no collider active, try to create a mesh collider for all
            // mesh renderers, however, for this the Mesh should have been set to
            // Read / write enabled on asset bundle build

            MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();

            foreach (MeshFilter meshFilter in meshFilters)
            {
                bool readable = meshFilter.sharedMesh.isReadable;

                if (readable)
                {
                    // readable, so add the new mesh collider.
                    MeshCollider newMeshCollider = meshFilter.gameObject.AddComponent<MeshCollider>();
                    newMeshCollider.sharedMesh = meshFilter.sharedMesh;
                }
                else
                {
                    // not readable, so make a box collider around the mesh.
                    BoxCollider newBoxCollider = meshFilter.gameObject.AddComponent<BoxCollider>();
                }
            }
        }

        private void OnSelectedChanged(bool selected)
        {
            gameObject.SetLayerRecursively(selected ? Layers.Selected : Layers.Default);
        }

        protected virtual void OnTransformDataChanged(TransformData transformData)
        {
            transform.SetFromTransformData(transformData);
        }

        #region Action registration

        private void OnEnable()
        {
            Register();
        }

        private void OnDestroy()
        {
            Unregister();
        }

        private void OnDisable()
        {
            Unregister();
        }

        protected virtual void Register()
        {
            if (_onTransformDataChanged == null)
            {
                _onTransformDataChanged = OnTransformDataChanged;
            }

            if (_onSelectedChanged == null)
            {
                _onSelectedChanged = OnSelectedChanged;
            }

            if (_realityObjectData != null)
            {
                _realityObjectData.Transform.Register(_onTransformDataChanged);
                _realityObjectData.Selected.Register(_onSelectedChanged);
            }
        }

        protected virtual void Unregister()
        {
            if (_realityObjectData != null)
            {
                _realityObjectData.Transform.Unregister(_onTransformDataChanged);
                _realityObjectData.Selected.Unregister(_onSelectedChanged);
            }
        }

        #endregion

    }
}

