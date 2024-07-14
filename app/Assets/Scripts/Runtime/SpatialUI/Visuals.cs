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
    /// <summary>
    /// Class for displaying world axes and world origin
    /// </summary>
    public class Visuals : MonoBehaviour
    {
        // GameObjects
        [SerializeField] private GameObject _worldXAxis;
        [SerializeField] private GameObject _worldYAxis;
        [SerializeField] private GameObject _worldZAxis;
        [SerializeField] private GameObject _worldOrigin;
        [SerializeField] private GameObject _worldYGrid;

        private Action<bool> _onShowWorldAxesChanged;
        private Action<bool> _onShowWorldYGridChanged;

        private App _app;

        private void Start()
        {
            _app = App.Instance;

            _onShowWorldAxesChanged = OnShowWorldAxesChanged;
            _onShowWorldYGridChanged = OnShowWorldYGridChanged;

            Register();
        }

        private void OnShowWorldAxesChanged(bool visible)
        {
            _worldXAxis.SetActive(visible);
            _worldYAxis.SetActive(visible);
            _worldZAxis.SetActive(visible);
            _worldOrigin.SetActive(visible);
        }

        private void OnShowWorldYGridChanged(bool visible)
        {
            _worldYGrid.SetActive(visible);
        }

        private void Register()
        {
            if (_app != null)
            {
                _app.ShowWorldAxes.Register(_onShowWorldAxesChanged);
                _app.ShowWorldYGrid.Register(_onShowWorldYGridChanged);
            }
        }

        private void Unregister()
        {
            if (_app != null)
            {
                _app.ShowWorldAxes.Unregister(_onShowWorldAxesChanged);
                _app.ShowWorldYGrid.Unregister(_onShowWorldYGridChanged);
            }
        }

        private void OnDestroy()
        {
            Unregister();
        }

        private void OnDisable()
        {
            Unregister();
        }

        private void OnEnable()
        {
            Register();
        }
    }
}
