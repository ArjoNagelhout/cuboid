// 
// SpatialGraphicRaycaster.cs
// Cuboid
// 
// Copyright 2023 ShapeReality
// 

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cuboid.Input
{
    public class SpatialGraphicRaycaster : BaseRaycaster
    {
        private const int k_MaxRaycastHits = 10;

        public override int sortOrderPriority => Constants.k_UISortOrderPriority;

        private readonly struct RaycastHitData
        {
            public RaycastHitData(Graphic graphic, Vector3 worldHitPosition, Vector2 screenPosition, float distance, int displayIndex)
            {
                this.graphic = graphic;
                this.worldHitPosition = worldHitPosition;
                this.screenPosition = screenPosition;
                this.distance = distance;
                this.displayIndex = displayIndex;
            }

            public Graphic graphic { get; }
            public Vector3 worldHitPosition { get; }
            public Vector2 screenPosition { get; }
            public float distance { get; }
            public int displayIndex { get; }
        }

        /// <summary>
        /// Compares ray cast hits by graphic depth, to sort in descending order.
        /// </summary>
        sealed class RaycastHitComparer : IComparer<RaycastHitData>
        {
            public int Compare(RaycastHitData a, RaycastHitData b)
                => b.graphic.depth.CompareTo(a.graphic.depth);
        }

        [SerializeField]
        [Tooltip("Whether Graphics facing away from the ray caster are checked for ray casts. Enable this to ignore backfacing Graphics.")]
        private bool _ignoreReversedGraphics;

        [SerializeField]
        [Tooltip("Whether or not 3D occlusion is checked when performing ray casts. Enable to make Graphics be blocked by 3D objects that exist in front of it.")]
        private bool _checkFor3DOcclusion;

        [SerializeField]
        [Tooltip("The layers of objects that are checked to determine if they block Graphic ray casts when checking for 2D or 3D occlusion.")]
        private LayerMask _blockingMask = -1;

        [SerializeField]
        [Tooltip("Specifies whether the ray cast should hit Triggers when checking for 3D occlusion.")]
        private QueryTriggerInteraction _raycastTriggerInteraction = QueryTriggerInteraction.Ignore;

        /// <summary>
        /// See BaseRaycaster.eventCamera
        /// </summary>
        public override Camera eventCamera => _canvas != null && _canvas.worldCamera != null ? _canvas.worldCamera : Camera.main;

        /// <summary>
        /// Performs a ray cast against objects within this Raycaster's domain.
        /// </summary>
        /// <param name="eventData">Data containing where and how to ray cast.</param>
        /// <param name="resultAppendList">The resultant hits from the ray cast.</param>
        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            if (eventData is SpatialPointerEventData trackedEventData)
            {
                PerformRaycasts(trackedEventData, resultAppendList);
            }
        }

        private Canvas __canvas;
        private Canvas _canvas
        {
            get
            {
                if (__canvas != null)
                    return __canvas;

                __canvas = GetComponent<Canvas>();
                return __canvas;
            }
        }

        private bool _hasWarnedEventCameraNull;

        private readonly RaycastHit[] _occlusionHits3D = new RaycastHit[k_MaxRaycastHits];

        private static readonly RaycastHitComparer s_RaycastHitComparer = new RaycastHitComparer();

        private static readonly Vector3[] s_Corners = new Vector3[4];

        // Use this list on each ray cast to avoid continually allocating.
        private readonly List<RaycastHitData> _raycastResultsCache = new List<RaycastHitData>();

        [NonSerialized]
        private static readonly List<RaycastHitData> s_SortedGraphics = new List<RaycastHitData>();

        private PhysicsScene _localPhysicsScene;

        static RaycastHit FindClosestHit(RaycastHit[] hits, int count)
        {
            var index = 0;
            var distance = float.MaxValue;
            for (var i = 0; i < count; i++)
            {
                if (hits[i].distance < distance)
                {
                    distance = hits[i].distance;
                    index = i;
                }
            }

            return hits[index];
        }

        /// <inheritdoc />
        protected override void Awake()
        {
            base.Awake();
            _localPhysicsScene = gameObject.scene.GetPhysicsScene();
        }

        private void PerformRaycasts(SpatialPointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            if (_canvas == null)
                return;

            // Property can call Camera.main, so cache the reference
            var currentEventCamera = eventCamera;
            if (currentEventCamera == null)
            {
                if (!_hasWarnedEventCameraNull)
                {
                    Debug.LogWarning("Event Camera must be set on World Space Canvas to perform ray casts with tracked device." +
                        " UI events will not function correctly until it is set.",
                        this);
                    _hasWarnedEventCameraNull = true;
                }

                return;
            }

            var rayPoints = eventData.rayPoints;
            var layerMask = eventData.layerMask;
            for (var i = 1; i < rayPoints.Count; i++)
            {
                var from = rayPoints[i - 1];
                var to = rayPoints[i];
                if (PerformRaycast(from, to, layerMask, currentEventCamera, resultAppendList))
                {
                    eventData.rayHitIndex = i;
                    break;
                }
            }
        }

        private bool PerformRaycast(Vector3 from, Vector3 to, LayerMask layerMask, Camera currentEventCamera, List<RaycastResult> resultAppendList)
        {
            var hitSomething = false;

            var rayDistance = Vector3.Distance(to, from);
            var ray = new Ray(from, (to - from).normalized * rayDistance);

            var hitDistance = rayDistance;
            if (_checkFor3DOcclusion)
            {
                var hitCount = _localPhysicsScene.Raycast(ray.origin, ray.direction, _occlusionHits3D, hitDistance, _blockingMask, _raycastTriggerInteraction);

                if (hitCount > 0)
                {
                    var hit = FindClosestHit(_occlusionHits3D, hitCount);
                    hitDistance = hit.distance;
                    hitSomething = true;
                }
            }

            _raycastResultsCache.Clear();
            SortedRaycastGraphics(_canvas, ray, hitDistance, layerMask, currentEventCamera, _raycastResultsCache);

            // Now that we have a list of sorted hits, process any extra settings and filters.
            foreach (var hitData in _raycastResultsCache)
            {
                var validHit = true;

                var go = hitData.graphic.gameObject;
                if (_ignoreReversedGraphics)
                {
                    var forward = ray.direction;
                    var goDirection = go.transform.rotation * Vector3.forward;
                    validHit = Vector3.Dot(forward, goDirection) > 0;
                }

                validHit &= hitData.distance < hitDistance;

                if (validHit)
                {
                    var trans = go.transform;
                    var transForward = trans.forward;
                    var castResult = new RaycastResult
                    {
                        gameObject = go,
                        module = this,
                        distance = hitData.distance,
                        index = resultAppendList.Count,
                        depth = hitData.graphic.depth,
                        sortingLayer = _canvas.sortingLayerID,
                        sortingOrder = _canvas.sortingOrder,
                        worldPosition = hitData.worldHitPosition,
                        worldNormal = -transForward,
                        screenPosition = hitData.screenPosition,
                        displayIndex = hitData.displayIndex,
                    };
                    resultAppendList.Add(castResult);

                    hitSomething = true;
                }
            }

            return hitSomething;
        }

        private static void SortedRaycastGraphics(Canvas canvas, Ray ray, float maxDistance, LayerMask layerMask, Camera eventCamera, List<RaycastHitData> results)
        {
            var graphics = GraphicRegistry.GetGraphicsForCanvas(canvas);

            s_SortedGraphics.Clear();
            for (int i = 0; i < graphics.Count; ++i)
            {
                var graphic = graphics[i];

                // -1 means it hasn't been processed by the canvas, which means it isn't actually drawn
                if (graphic.depth == -1 || !graphic.raycastTarget || graphic.canvasRenderer.cull)
                    continue;

                // while debugging: the layer value = 5, layermask value = 0
                if (((1 << graphic.gameObject.layer) & layerMask) == 0)
                    continue;

                var raycastPadding = graphic.raycastPadding;

                if (RayIntersectsRectTransform(graphic.rectTransform, raycastPadding, ray, out var worldPos, out var distance))
                {
                    if (distance <= maxDistance)
                    {
                        Vector2 screenPos = eventCamera.WorldToScreenPoint(worldPos);
                        // mask/image intersection - See Unity docs on eventAlphaThreshold for when this does anything
                        if (graphic.Raycast(screenPos, eventCamera))
                        {
                            s_SortedGraphics.Add(new RaycastHitData(graphic, worldPos, screenPos, distance, eventCamera.targetDisplay));
                        }
                    }
                }
            }

            SortUtils.Sort(s_SortedGraphics, s_RaycastHitComparer);
            results.AddRange(s_SortedGraphics);
        }

        private static bool RayIntersectsRectTransform(RectTransform transform, Vector4 raycastPadding, Ray ray, out Vector3 worldPosition, out float distance)
        {
            GetRectTransformWorldCorners(transform, raycastPadding, s_Corners);
            var plane = new Plane(s_Corners[0], s_Corners[1], s_Corners[2]);

            if (plane.Raycast(ray, out var enter))
            {
                var intersection = ray.GetPoint(enter);

                var bottomEdge = s_Corners[3] - s_Corners[0];
                var leftEdge = s_Corners[1] - s_Corners[0];
                var bottomDot = Vector3.Dot(intersection - s_Corners[0], bottomEdge);
                var leftDot = Vector3.Dot(intersection - s_Corners[0], leftEdge);

                // If the intersection is right of the left edge and above the bottom edge.
                if (leftDot >= 0f && bottomDot >= 0f)
                {
                    var topEdge = s_Corners[1] - s_Corners[2];
                    var rightEdge = s_Corners[3] - s_Corners[2];
                    var topDot = Vector3.Dot(intersection - s_Corners[2], topEdge);
                    var rightDot = Vector3.Dot(intersection - s_Corners[2], rightEdge);

                    // If the intersection is left of the right edge, and below the top edge
                    if (topDot >= 0f && rightDot >= 0f)
                    {
                        worldPosition = intersection;
                        distance = enter;
                        return true;
                    }
                }
            }

            worldPosition = Vector3.zero;
            distance = 0f;
            return false;
        }

        // This method is similar to RecTransform.GetWorldCorners, but with support for the raycastPadding offset.
        static void GetRectTransformWorldCorners(RectTransform transform, Vector4 offset, Vector3[] fourCornersArray)
        {
            if (fourCornersArray == null || fourCornersArray.Length < 4)
            {
                Debug.LogError("Calling GetRectTransformWorldCorners with an array that is null or has less than 4 elements.");
                return;
            }

            // GraphicRaycaster.Raycast uses RectTransformUtility.RectangleContainsScreenPoint instead,
            // which redirects to PointInRectangle defined in RectTransformUtil.cpp. However, that method
            // uses the Camera to convert from the given screen point to a ray, but this class uses
            // the ray from the Ray Interactor that feeds the event data.
            // Offset calculation for raycastPadding from PointInRectangle method, which replaces RectTransform.GetLocalCorners.
            var rect = transform.rect;
            var x0 = rect.x + offset.x;
            var y0 = rect.y + offset.y;
            var x1 = rect.xMax - offset.z;
            var y1 = rect.yMax - offset.w;
            fourCornersArray[0] = new Vector3(x0, y0, 0f);
            fourCornersArray[1] = new Vector3(x0, y1, 0f);
            fourCornersArray[2] = new Vector3(x1, y1, 0f);
            fourCornersArray[3] = new Vector3(x1, y0, 0f);

            // Transform the local corners to world space, which is from RectTransform.GetWorldCorners.
            var localToWorldMatrix = transform.localToWorldMatrix;
            for (var index = 0; index < 4; ++index)
                fourCornersArray[index] = localToWorldMatrix.MultiplyPoint(fourCornersArray[index]);
        }
    }
}