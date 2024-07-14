// Copyright (c) 2023 Arjo Nagelhout

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Cuboid.UI
{
    public class ToolSwitcherUIElement : MonoBehaviour
    {
        private ToolController _toolController;
        private Action<ToolController.Tool> _onActiveToolChanged;

        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _icon;

        [SerializeField] private float _activeScaleFactor;
        [SerializeField] private float _activeZOffset;

        private ToolController.Tool _tool;
        public ToolController.Tool Tool
        {
            get => _tool;
            set
            {
                _tool = value;
                // update the icon and text etc.

                if (_toolController == null)
                {
                    _toolController = ToolController.Instance;
                }

                // get the data from the tool controller
                if (_toolController.ToolsData.TryGetValue(_tool, out ToolController.ToolData toolData))
                {
                    _icon.sprite = toolData.Icon;
                }

                // make sure to also set whether it is active or not
                OnActiveToolChanged(_toolController.ActiveTool.Value);
            }
        }

        private bool _active = false;
        public bool Active
        {
            get => _active;
            private set
            {
                _active = value;
                // update the appearance

                ColorsScriptableObject colors = UIController.Instance.Colors.Soft;

                _backgroundImage.color = _active ? colors.Active : colors.Normal;
                _icon.color = _active ? colors.ActiveText : colors.Text;

                transform.DOKill();
                if (_active)
                {
                    transform.DOScale(_activeScaleFactor, 0.4f).SetEase(Ease.OutBack, 1.2f);
                    transform.DOLocalMoveZ(_activeZOffset, 0.4f).SetEase(Ease.OutBack, 1.2f);
                }
                else
                {
                    transform.DOScale(1f, 0.3f).SetEase(Ease.OutQuart);
                    transform.DOLocalMoveZ(0, 0.3f).SetEase(Ease.OutQuart);
                }
            }
        }

        private void Start()
        {
            _toolController = ToolController.Instance;
            _onActiveToolChanged = OnActiveToolChanged;

            Register();
        }

        private void OnActiveToolChanged(ToolController.Tool tool)
        {
            // set active
            Active = tool == Tool;
        }

        #region Action registration

        private void Register()
        {
            if (_toolController != null)
            {
                _toolController.ActiveTool.Register(_onActiveToolChanged);
            }
        }

        private void Unregister()
        {
            if (_toolController != null)
            {
                _toolController.ActiveTool.Unregister(_onActiveToolChanged);
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

        #endregion
    }
}
