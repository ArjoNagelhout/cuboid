// Copyright (c) 2023 Arjo Nagelhout

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Cuboid
{
    public class SelectionOutlineRendererFeature : ScriptableRendererFeature
    {
        public class SelectionOutlineRenderPass : ScriptableRenderPass
        {
            private List<ShaderTagId> _shaderTagIdList = new List<ShaderTagId>();
            private FilteringSettings _filteringSettings;

            public Material SelectionOutlineMaterial;

            public SelectionOutlineRenderPass(LayerMask layerMask)
            {
                // filtering settings
                RenderQueueRange renderQueueRange = RenderQueueRange.opaque;
                _filteringSettings = new FilteringSettings(renderQueueRange, layerMask);

                // LightMode tags for which objects (that have a shader with this LightMode tag) should be rendered. 
                _shaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
                _shaderTagIdList.Add(new ShaderTagId("UniversalForward"));
                _shaderTagIdList.Add(new ShaderTagId("UniversalForwardOnly"));
            }

            // will be called by the pipeline to execute this pass. 
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;

                // draw outlines
                DrawingSettings outlineDrawingSettings = CreateDrawingSettings(_shaderTagIdList, ref renderingData, sortingCriteria);
                outlineDrawingSettings.overrideMaterial = SelectionOutlineMaterial;
                outlineDrawingSettings.overrideMaterialPassIndex = 0;

                context.DrawRenderers(renderingData.cullResults, ref outlineDrawingSettings, ref _filteringSettings);
            }
        }

        public Material SelectionOutlineMaterial;

        public LayerMask SelectedObjectsLayerMask;

        private SelectionOutlineRenderPass _renderPass;

        public override void Create()
        {
            _renderPass = new SelectionOutlineRenderPass(SelectedObjectsLayerMask);
            _renderPass.SelectionOutlineMaterial = SelectionOutlineMaterial;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(_renderPass);
        }
    }
}
