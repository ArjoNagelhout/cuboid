using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace Cuboid
{

    public class DOTweenTests : MonoBehaviour
    {
        [SerializeField] private Transform _cubeToTransform;

        [SerializeField] private Material _normalMaterial;
        [SerializeField] private Material _hoveredMaterial;
        [SerializeField] private Material _pressedMaterial;

        [SerializeField] private MeshRenderer _meshRenderer;
         
        [SerializeField] private float _pressedHoveredChangeInterval = 1.0f;
        [SerializeField] private float _animationDuration = 0.2f;

        [SerializeField] private Ease _ease;

        private bool _hovered;
        public bool Hovered
        {
            get => _hovered;
            set
            {
                _hovered = value;
                Debug.Log($"hovered: {_hovered}");
                UpdateHoverPressedAppearance();
            }
        }

        private bool _pressed;
        public bool Pressed
        {
            get => _pressed;
            set
            {
                _pressed = value;
                Debug.Log($"pressed: {_pressed}");
                UpdateHoverPressedAppearance();
            }
        }

        private IEnumerator WalkThroughHoverPressed()
        {
            while (true)
            {
                yield return new WaitForSeconds(_pressedHoveredChangeInterval);
                Hovered = true;
                yield return new WaitForSeconds(_pressedHoveredChangeInterval);
                Pressed = true;
                yield return new WaitForSeconds(_pressedHoveredChangeInterval);
                Pressed = false;
                yield return new WaitForSeconds(_pressedHoveredChangeInterval);
                Hovered = false;
            }
        }

        private void Start()
        {
            StartCoroutine(WalkThroughHoverPressed());
        }

        private void UpdateHoverPressedAppearance()
        {
            float target = 0f;
            if (Pressed)
            {
                _meshRenderer.material = _pressedMaterial;
                target = 0.5f;
            }
            else if (Hovered)
            {
                target = 1.0f;
                _meshRenderer.material = _hoveredMaterial;
            }
            else
            {
                target = 0.0f;
                _meshRenderer.material = _normalMaterial;
            }

            transform.DOMoveX(target, _animationDuration, false).SetEase(_ease);

            // animate between the values
        }
    }
}
