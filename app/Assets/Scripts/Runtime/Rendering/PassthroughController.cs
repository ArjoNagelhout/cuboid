// Copyright (c) 2023 Arjo Nagelhout

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid
{
    public class PassthroughController : MonoBehaviour
    {
        private App _app;
        private Action<bool> _onPassthroughChanged;

        [SerializeField] private Camera _mainCamera;
        [SerializeField] private OVRManager _ovrManager;
        [SerializeField] private OVRPassthroughLayer _passthroughLayer;

        private void Start()
        {
            _app = App.Instance;
            _onPassthroughChanged = OnPassthroughChanged;

            Register();

            _app.Passthrough.Value = true;
        }

        private void OnPassthroughChanged(bool passthrough)
        {
            _ovrManager.isInsightPassthroughEnabled = passthrough;
            _mainCamera.backgroundColor = passthrough ? Color.clear : Color.black;
            _mainCamera.clearFlags = passthrough ? CameraClearFlags.SolidColor : CameraClearFlags.Skybox;
            _passthroughLayer.enabled = passthrough;
            Debug.Log("Changed passthrough");
            Debug.Log(_mainCamera.clearFlags);
            Debug.Log(_mainCamera.backgroundColor);
            Debug.Log(_ovrManager.isInsightPassthroughEnabled);
        }

        private void Register()
        {
            if (_app != null)
            {
                _app.Passthrough.Register(_onPassthroughChanged);
            }
        }

        private void Unregister()
        {
            if (_app != null)
            {
                _app.Passthrough.Unregister(_onPassthroughChanged);
            }
        }

        private void OnEnable()
        {
            Register();
        }

        private void OnDisable()
        {
            Unregister();
        }

        private void OnDestroy()
        {
            Unregister();
        }
    }
}
