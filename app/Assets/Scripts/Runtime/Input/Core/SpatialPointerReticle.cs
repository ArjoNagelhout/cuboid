//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Shapes;
using Cuboid.Utils;

namespace Cuboid.Input
{
    /// <summary>
    /// The SpatialPointerReticle determines how it should display the reticle depending on the
    /// current EventData's eventData.enterObjectType.
    /// </summary>
    public class SpatialPointerReticle : MonoBehaviour
    {
        private SpatialPointerReticleData _data = new SpatialPointerReticleData();
        public SpatialPointerReticleData Data
        {
            get => _data;
            set
            {
                if (_data == value) { return; } // don't update when it has already been loaded
                _data = value;
                OnDataChanged(_data);
            }
        }

        // Custom image
        [SerializeField] private Image _customReticleImage;
        private GameObject _customReticle;

        // Default reticle (to disable when a custom image has been added)
        [SerializeField] private GameObject _defaultReticle;

        [SerializeField] private Disc _currentTransformDisc;
        [SerializeField] private Disc _targetTransformDisc;

        [SerializeField] private Transform _currentTransform;
        [SerializeField] private Transform _targetTransform;
        [SerializeField] private Line _line;

        private void Awake()
        {
            
        }

        private void Start()
        {
            if (_customReticleImage == null)
            {
                // Try to get the image in the game object
                _customReticleImage = GetComponentInChildren<Image>();
            }

            _customReticle = _customReticleImage.gameObject;
        }

        private void UpdateRotation()
        {
            if (Data.updateRotation != null)
            {
                _targetTransform.localRotation = Data.updateRotation.Invoke();
            }
            else
            {
                if (Data.Billboard)
                {
                    // Rotate towards the camera (don't use the supplied rotation)
                    Vector3 cameraPosition = Camera.main.transform.position;
                    Vector3 position = _targetTransform.position;
                    Vector3 delta = position - cameraPosition;
                    _targetTransform.localRotation = Quaternion.LookRotation(delta, Vector3.up);
                }
                else
                {
                    _targetTransform.localRotation = Data.Rotation;
                }
            }
        }

        private void LateUpdate()
        {
            if (Data == null) { return; }

            UpdateScale();
            UpdateRotation();
        }

        private void UpdateScale()
        {
            if (Data.ReticleImage != null)
            {
                _customReticle.transform.localScale = ConstantScaleOnScreen.GetConstantScale(_customReticle.transform.position, Data.ReferenceSizeAtOneMeterDistance);
            }
        }

        private void UpdateReticleVisibility(bool visible, bool spatial, bool showCurrentSpatialPointerPosition)
        {
            _targetTransform.gameObject.SetActive(visible);
            _currentTransform.gameObject.SetActive(visible && spatial && showCurrentSpatialPointerPosition);
            _line.gameObject.SetActive(visible && spatial && showCurrentSpatialPointerPosition);
        }

        public void OnSpatialPointerEventDataUpdated(SpatialPointerEventData eventData)
        {
            bool pressed = eventData.pointerPress != null || eventData.outsideUIPointerPress == true;
            Color color = pressed ? Data.PressedColor : Data.Color;

            bool visible = false;

            if (eventData.outsideUIPointerEnter)
            {
                visible = eventData.outsideUIValidPointerPosition;
            }
            else
            {
                visible = eventData.pointerEnter != null;
            }

            //if (eventData.invalidRaycastWasIntercepted)
            //{
            //    visible = false;
            //}

            // also show the current spatial pointer position when the outside UI has been entered and registered. 
            //bool showCurrentSpatialPointerPosition = (pressed && eventData.outsideUIPressRaycastResult.isValid) || eventData.outsideUIPointerEnter;

            bool showCurrentSpatialPointerPosition = false;

            if (eventData.outsideUIPointerEnter)
            {
                // otherwise, only show if valid
                showCurrentSpatialPointerPosition = pressed && eventData.outsideUIPointerDrag && eventData.outsideUIPressRaycastResult.isValid;
            }
            else
            {
                showCurrentSpatialPointerPosition = pressed && eventData.pointerDrag != null;
            }

            UpdateReticleVisibility(
                visible,
                true,//eventData.enterObjectType != SpatialPointerEventData.ObjectType.UI,
                showCurrentSpatialPointerPosition);

            UpdateScale();

            // set the reticle color based on whether the user is pressing
            _customReticleImage.color = color;
            _currentTransformDisc.Color = color;
            _targetTransformDisc.Color = color;

            Vector3 current = eventData.spatialPosition;
            Vector3 target = eventData.spatialTargetPosition;
            _currentTransform.position = current;
            _targetTransform.position = target;
            _line[0] = current;
            _line[1] = target;
        }

        private void SetReticleImageVisibility(bool showReticleImage)
        {
            _customReticle.SetActive(showReticleImage);
            _defaultReticle.SetActive(!showReticleImage);
        }

        private void OnDataChanged(SpatialPointerReticleData data)
        {
            bool showReticleImage = data.ReticleImage != null;

            if (showReticleImage)
            {
                _customReticleImage.sprite = data.ReticleImage;
            }

            UpdateScale();
            UpdateRotation();

            SetReticleImageVisibility(showReticleImage);
        }
    }
}
