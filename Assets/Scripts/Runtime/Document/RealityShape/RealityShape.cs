using System;
using System.Collections;
using System.Collections.Generic;
using Cuboid.Models;
using UnityEngine;

namespace Cuboid
{
    /// <summary>
    /// The current issue is that the RealityShapeRenderer is being instantiated *next* to the
    /// RealityShape object. This means that when selecting the object, it will try to get the
    /// bounds of the object, but not use any references / renderers that are not children /
    /// components in the object itself.
    ///
    /// The reason it needs to be two separate objects is because the shape renderer doesn't
    /// calculate normals well when scaling the object and then setting the vertex positions and normals
    /// with the inverse scale.
    ///
    /// Maybe we can change the way the normals get calculated.
    ///
    /// This approach with two GameObjects also breaks the undo / redo command, so it would be incredibly
    /// preferable to fix this using the updated normals approach. 
    /// </summary>
    [PrettyTypeNameAttribute(Name = "Shape")]
    public class RealityShapeObjectData : RealityObjectData, IHasPrimaryColor
    {
        [RuntimeSerializedPropertyColor]
        public Binding<RealityColor> Color = new();

        public Binding<RealityColor> PrimaryColor
        {
            get => Color;
            set => Color = value;
        }

        [RuntimeSerializedPropertyInt(Min = 0, Max = 4)]
        public Binding<int> CornerQuality = new(1);

        [RuntimeSerializedPropertyFloat(Min = 0, Max = 0.25f)]
        public Binding<float> CornerRadius = new(0.1f);

        public override IEnumerator InstantiateAsync(Action<RealityObject> completed)
        {
            // first create the Shape
            GameObject gameObject = new GameObject(Name.Value);
            gameObject.AddComponent<BoxCollider>();
            RealityShape realityShape = gameObject.AddComponent<RealityShape>();

            // then create the ShapeRenderer
            GameObject shapeRendererGameObject = new GameObject($"ShapeRenderer_{Guid}");
            shapeRendererGameObject.transform.SetParent(gameObject.transform, false);
            shapeRendererGameObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = shapeRendererGameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = new Material(App.Instance.DefaultMaterial);// TODO: change. Also, should instantiate the material so that the color can be changed. 
            RoundedCuboidRenderer shapeRenderer = shapeRendererGameObject.AddComponent<RoundedCuboidRenderer>();
            shapeRenderer._associatedShapeGuid = Guid;

            // Binding
            realityShape.ShapeRenderer = shapeRenderer;
            realityShape._meshRenderer = meshRenderer;
            completed?.Invoke(realityShape);

            yield return null;
        }
    }

    /// <summary>
    /// The RealityShape is a RealityObject subclass used by the DrawShapeTool
    /// it is able to render
    /// </summary>
    public class RealityShape : RealityObject
    {
        private Action<RealityColor> _onColorChanged;

        internal MeshRenderer _meshRenderer;

        public RoundedCuboidRenderer ShapeRenderer;
        private Vector3 _lastScale;

        RealityShapeObjectData ShapeData => base.RealityObjectData as RealityShapeObjectData;

        private void OnColorChanged(RealityColor color)
        {
            // set the mesh color to the provided color
            _meshRenderer.sharedMaterial.color = color.ToColor32();
        }

        private void Update()
        {
             Render();
        }

        protected override void OnTransformDataChanged(TransformData transformData)
        {
            base.OnTransformDataChanged(transformData);

            Render();
        }

        private void Render(bool check = true)
        {
            Render(transform.localScale, check);
        }

        private void Render(Vector3 scale, bool check)
        {
            // rerender the shape only when the scale was changed. 
            if (!scale.RoughlyEquals(_lastScale) || !check)
            {
                _lastScale = scale;
                ShapeRenderer.GenerateMesh(_lastScale, ShapeData.CornerRadius.Value, ShapeData.CornerQuality.Value);
            }
        }

        private Action<float> _onRadiusChanged;
        private Action<int> _onCornerQualityChanged;

        protected override void Register()
        {
            base.Register();

            if (_onColorChanged == null)
            {
                _onColorChanged = OnColorChanged;
            }

            if (_onRadiusChanged == null)
            {
                _onRadiusChanged = (_) => { Render(false); };
                _onCornerQualityChanged = (_) => { Render(false); };
            }

            if (ShapeData != null)
            {
                ShapeData.Color.Register(_onColorChanged);
                ShapeData.CornerRadius.Register(_onRadiusChanged);
                ShapeData.CornerQuality.Register(_onCornerQualityChanged);
            }
        }

        protected override void Unregister()
        {
            base.Unregister();

            if (ShapeData != null)
            {
                ShapeData.Color.Unregister(_onColorChanged);
                ShapeData.CornerRadius.Unregister(_onRadiusChanged);
                ShapeData.CornerQuality.Unregister(_onCornerQualityChanged);
            }
        }
    }
}
