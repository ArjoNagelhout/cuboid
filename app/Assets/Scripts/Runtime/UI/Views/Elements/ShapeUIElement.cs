using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cuboid.Input;
using TMPro;
using Cuboid.Models;
using static Cuboid.TransformCommand;

namespace Cuboid.UI
{
    /// <summary>
    /// A Shape UI element that can be drawn using the DrawShapeTool. 
    /// </summary>
    public class ShapeUIElement : MonoBehaviour,
        ISpatialPointerClickHandler
    {
        [SerializeField] private DrawShapeTool.Shape _shape = DrawShapeTool.Shape.Cuboid;

        private DrawShapeTool _drawShapeTool;
        private Action<DrawShapeTool.Shape> _onActiveShapeChanged;

        //private DrawShapeToolViewController _viewController;

        [SerializeField] private Transform _shapePreviewTransform;

        [SerializeField] private Image[] _activeOutlines = new Image[0];
        [SerializeField] private TextMeshProUGUI _text;

        [SerializeField] private float _activeZOffset = 0f;

        private bool _active;
        public bool Active
        {
            get => _active;
            set
            {
                _active = value;
                // set active appearance

                foreach (Image image in _activeOutlines)
                {
                    image.enabled = _active;
                }

                _text.color = _active ? Color.white : Color.black;
                _shapePreviewTransform.localPosition = _shapePreviewTransform.localPosition.SetZ(_active ? _activeZOffset : 0.0f);
            }
        }

        private void Start()
        {
            _drawShapeTool = DrawShapeTool.Instance;

            _onActiveShapeChanged = OnActiveShapeChanged;

            Register();
        }

        private void OnActiveShapeChanged(DrawShapeTool.Shape shape)
        {
            Active = shape == _shape;
        }

        #region Input events

        void ISpatialPointerClickHandler.OnSpatialPointerClick(SpatialPointerEventData eventData)
        {
            //_drawShapeTool.ActiveShape.Value = _shape;

            // Instantiate the object at the center of the scene

            Guid guid = Guid.NewGuid();
            RealityShapeObjectData newObjectData = new RealityShapeObjectData()
            {
                Guid = guid,
                Name = new($"Shape_{guid}"),
                Transform = new(new TransformData(Vector3.zero, Quaternion.identity, Vector3.one * 1f)),
                Selected = new(false),

                Color = new(RealityColor.FromColor32(Color.black)),
                CornerQuality = new(2),
                CornerRadius = new(0.1f)
            };

            RealitySceneController.Instance.Instantiate(newObjectData, (onInstantiateResult) =>
            {
                // when the object has instantiated
                RealityObject _instantiatedRealityObject = onInstantiateResult;

                AddCommand addCommand = new AddCommand(
                    RealityDocumentController.Instance,
                    RealitySceneController.Instance,
                    RealitySceneController.Instance.OpenedRealitySceneIndex,
                    newObjectData
                    );

                UndoRedoController.Instance.Execute(addCommand);
            });
        }

        #endregion

        #region Action registration

        private void Register()
        {
            if (_drawShapeTool != null)
            {
                //_drawShapeTool.ActiveShape.Register(_onActiveShapeChanged);
            }
        }

        private void Unregister()
        {
            if (_drawShapeTool != null)
            {
                //_drawShapeTool.ActiveShape.Unregister(_onActiveShapeChanged);
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
