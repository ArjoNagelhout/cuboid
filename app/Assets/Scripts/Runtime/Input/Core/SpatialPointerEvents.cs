// Copyright (c) 2023 Arjo Nagelhout

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using static Cuboid.Input.ExecuteEvents;

namespace Cuboid.Input
{
    /// <summary>
    /// Interface to implement if you wish to receive OnPointerMove callbacks.
    /// </summary>
    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface ISpatialPointerMoveHandler : IEventSystemHandler
    {
        /// <summary>
        /// Use this callback to detect pointer move events
        /// </summary>
        void OnSpatialPointerMove(SpatialPointerEventData eventData);
    }

    /// <summary>
    /// Interface to implement if you wish to receive OnPointerEnter callbacks.
    /// </summary>
    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface ISpatialPointerEnterHandler : IEventSystemHandler
    {
        /// <summary>
        /// Use this callback to detect pointer enter events
        /// </summary>
        void OnSpatialPointerEnter(SpatialPointerEventData eventData);
    }

    /// <summary>
    /// Interface to implement if you wish to receive OnPointerExit callbacks.
    /// </summary>
    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface ISpatialPointerExitHandler : IEventSystemHandler
    {
        /// <summary>
        /// Use this callback to detect pointer exit events
        /// </summary>
        void OnSpatialPointerExit(SpatialPointerEventData eventData);
    }

    /// <summary>
    /// Interface to implement if you wish to receive OnPointerDown callbacks.
    /// </summary>
    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface ISpatialPointerDownHandler : IEventSystemHandler
    {
        /// <summary>
        /// Use this callback to detect pointer down events.
        /// </summary>
        void OnSpatialPointerDown(SpatialPointerEventData eventData);
    }

    /// <summary>
    /// Interface to implement if you wish to receive OnPointerUp callbacks.
    /// Note: In order to receive OnPointerUp callbacks, you must also implement the EventSystems.IPointerDownHandler|IPointerDownHandler interface
    /// </summary>
    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface ISpatialPointerUpHandler : IEventSystemHandler
    {
        /// <summary>
        /// Use this callback to detect pointer up events.
        /// </summary>
        void OnSpatialPointerUp(SpatialPointerEventData eventData);
    }

    /// <summary>
    /// Interface to implement if you wish to receive OnPointerClick callbacks.
    /// </summary>
    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    /// <remarks>
    /// Use the IPointerClickHandler Interface to handle click input using OnPointerClick callbacks. Ensure an Event System exists in the Scene to allow click detection. For click detection on non-UI GameObjects, ensure a EventSystems.PhysicsRaycaster is attached to the Camera.
    /// </remarks>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// using UnityEngine;
    /// using UnityEngine.EventSystems;
    ///
    /// public class Example : MonoBehaviour, IPointerClickHandler
    /// {
    ///     //Detect if a click occurs
    ///     public void OnPointerClick(PointerEventData pointerEventData)
    ///     {
    ///         //Output to console the clicked GameObject's name and the following message. You can replace this with your own actions for when clicking the GameObject.
    ///         Debug.Log(name + " Game Object Clicked!");
    ///     }
    /// }
    /// ]]>
    ///</code>
    /// </example>
    public interface ISpatialPointerClickHandler : IEventSystemHandler
    {
        /// <summary>
        /// Use this callback to detect clicks.
        /// </summary>
        void OnSpatialPointerClick(SpatialPointerEventData eventData);
    }

    /// <summary>
    /// Interface to implement if you wish to receive OnBeginDrag callbacks.
    /// Note: You need to implement IDragHandler in addition to IBeginDragHandler.
    /// </summary>
    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface ISpatialBeginDragHandler : IEventSystemHandler
    {
        /// <summary>
        /// Called by a BaseInputModule before a drag is started.
        /// </summary>
        void OnSpatialBeginDrag(SpatialPointerEventData eventData);
    }

    /// <summary>
    /// Interface to implement if you wish to receive OnInitializePotentialDrag callbacks.
    /// </summary>
    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface ISpatialInitializePotentialDragHandler : IEventSystemHandler
    {
        /// <summary>
        /// Called by a BaseInputModule when a drag has been found but before it is valid to begin the drag.
        /// </summary>
        void OnSpatialInitializePotentialDrag(SpatialPointerEventData eventData);
    }

    /// <summary>
    /// Interface to implement if you wish to receive OnDrag callbacks.
    /// </summary>
    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// using UnityEngine;
    /// using UnityEngine.EventSystems;
    /// using UnityEngine.UI;
    ///
    /// [RequireComponent(typeof(Image))]
    /// public class DragMe : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    /// {
    ///     public bool dragOnSurfaces = true;
    ///
    ///     private GameObject m_DraggingIcon;
    ///     private RectTransform m_DraggingPlane;
    ///
    ///     public void OnBeginDrag(PointerEventData eventData)
    ///     {
    ///         var canvas = FindInParents<Canvas>(gameObject);
    ///         if (canvas == null)
    ///             return;
    ///
    ///         // We have clicked something that can be dragged.
    ///         // What we want to do is create an icon for this.
    ///         m_DraggingIcon = new GameObject("icon");
    ///
    ///         m_DraggingIcon.transform.SetParent(canvas.transform, false);
    ///         m_DraggingIcon.transform.SetAsLastSibling();
    ///
    ///         var image = m_DraggingIcon.AddComponent<Image>();
    ///
    ///         image.sprite = GetComponent<Image>().sprite;
    ///         image.SetNativeSize();
    ///
    ///         if (dragOnSurfaces)
    ///             m_DraggingPlane = transform as RectTransform;
    ///         else
    ///             m_DraggingPlane = canvas.transform as RectTransform;
    ///
    ///         SetDraggedPosition(eventData);
    ///     }
    ///
    ///     public void OnDrag(PointerEventData data)
    ///     {
    ///         if (m_DraggingIcon != null)
    ///             SetDraggedPosition(data);
    ///     }
    ///
    ///     private void SetDraggedPosition(PointerEventData data)
    ///     {
    ///         if (dragOnSurfaces && data.pointerEnter != null && data.pointerEnter.transform as RectTransform != null)
    ///             m_DraggingPlane = data.pointerEnter.transform as RectTransform;
    ///
    ///         var rt = m_DraggingIcon.GetComponent<RectTransform>();
    ///         Vector3 globalMousePos;
    ///         if (RectTransformUtility.ScreenPointToWorldPointInRectangle(m_DraggingPlane, data.position, data.pressEventCamera, out globalMousePos))
    ///         {
    ///             rt.position = globalMousePos;
    ///             rt.rotation = m_DraggingPlane.rotation;
    ///         }
    ///     }
    ///
    ///     public void OnEndDrag(PointerEventData eventData)
    ///     {
    ///         if (m_DraggingIcon != null)
    ///             Destroy(m_DraggingIcon);
    ///     }
    ///
    ///     static public T FindInParents<T>(GameObject go) where T : Component
    ///     {
    ///         if (go == null) return null;
    ///         var comp = go.GetComponent<T>();
    ///
    ///         if (comp != null)
    ///             return comp;
    ///
    ///         Transform t = go.transform.parent;
    ///         while (t != null && comp == null)
    ///         {
    ///             comp = t.gameObject.GetComponent<T>();
    ///             t = t.parent;
    ///         }
    ///         return comp;
    ///     }
    /// }
    /// ]]>
    ///</code>
    /// </example>
    public interface ISpatialDragHandler : IEventSystemHandler
    {
        /// <summary>
        /// When dragging is occurring this will be called every time the cursor is moved.
        /// </summary>
        void OnSpatialDrag(SpatialPointerEventData eventData);
    }

    /// <summary>
    /// Interface to implement if you wish to receive OnEndDrag callbacks.
    /// Note: You need to implement IDragHandler in addition to IEndDragHandler.
    /// </summary>
    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface ISpatialEndDragHandler : IEventSystemHandler
    {
        /// <summary>
        /// Called by a BaseInputModule when a drag is ended.
        /// </summary>
        void OnSpatialEndDrag(SpatialPointerEventData eventData);
    }

    /// <summary>
    /// Interface to implement if you wish to receive OnDrop callbacks.
    /// </summary>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// using UnityEngine;
    /// using UnityEngine.EventSystems;
    ///
    /// public class DropMe : MonoBehaviour, IDropHandler
    /// {
    ///     public void OnDrop(PointerEventData data)
    ///     {
    ///         if (data.pointerDrag != null)
    ///         {
    ///             Debug.Log ("Dropped object was: "  + data.pointerDrag);
    ///         }
    ///     }
    /// }
    /// ]]>
    ///</code>
    /// </example>
    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface ISpatialDropHandler : IEventSystemHandler
    {
        /// <summary>
        /// Called by a BaseInputModule on a target that can accept a drop.
        /// </summary>
        void OnSpatialDrop(SpatialPointerEventData eventData);
    }

    /// <summary>
    /// Interface to implement if you wish to receive OnScroll callbacks.
    /// </summary>
    /// <remarks>
    /// Criteria for this event is implementation dependent. For example see StandAloneInputModule.
    /// </remarks>
    public interface ISpatialScrollHandler : IEventSystemHandler
    {
        /// <summary>
        /// Use this callback to detect scroll events.
        /// </summary>
        void OnSpatialScroll(SpatialPointerEventData eventData);
    }

    public static class SpatialPointerEvents
    {
        private static readonly EventFunction<ISpatialPointerMoveHandler> s_SpatialPointerMoveHandler = Execute;

        private static void Execute(ISpatialPointerMoveHandler handler, BaseEventData eventData)
        {
            handler.OnSpatialPointerMove(ValidateEventData<SpatialPointerEventData>(eventData));
        }

        private static readonly EventFunction<ISpatialPointerEnterHandler> s_SpatialPointerEnterHandler = Execute;

        private static void Execute(ISpatialPointerEnterHandler handler, BaseEventData eventData)
        {
            handler.OnSpatialPointerEnter(ValidateEventData<SpatialPointerEventData>(eventData));
        }

        private static readonly EventFunction<ISpatialPointerExitHandler> s_SpatialPointerExitHandler = Execute;

        private static void Execute(ISpatialPointerExitHandler handler, BaseEventData eventData)
        {
            handler.OnSpatialPointerExit(ValidateEventData<SpatialPointerEventData>(eventData));
        }

        private static readonly EventFunction<ISpatialPointerDownHandler> s_SpatialPointerDownHandler = Execute;

        private static void Execute(ISpatialPointerDownHandler handler, BaseEventData eventData)
        {
            handler.OnSpatialPointerDown(ValidateEventData<SpatialPointerEventData>(eventData));
        }

        private static readonly EventFunction<ISpatialPointerUpHandler> s_SpatialPointerUpHandler = Execute;

        private static void Execute(ISpatialPointerUpHandler handler, BaseEventData eventData)
        {
            handler.OnSpatialPointerUp(ValidateEventData<SpatialPointerEventData>(eventData));
        }

        private static readonly EventFunction<ISpatialPointerClickHandler> s_SpatialPointerClickHandler = Execute;

        private static void Execute(ISpatialPointerClickHandler handler, BaseEventData eventData)
        {
            handler.OnSpatialPointerClick(ValidateEventData<SpatialPointerEventData>(eventData));
        }

        private static readonly EventFunction<ISpatialInitializePotentialDragHandler> s_SpatialInitializePotentialDragHandler = Execute;

        private static void Execute(ISpatialInitializePotentialDragHandler handler, BaseEventData eventData)
        {
            handler.OnSpatialInitializePotentialDrag(ValidateEventData<SpatialPointerEventData>(eventData));
        }

        private static readonly EventFunction<ISpatialBeginDragHandler> s_SpatialBeginDragHandler = Execute;

        private static void Execute(ISpatialBeginDragHandler handler, BaseEventData eventData)
        {
            handler.OnSpatialBeginDrag(ValidateEventData<SpatialPointerEventData>(eventData));
        }

        private static readonly EventFunction<ISpatialDragHandler> s_SpatialDragHandler = Execute;

        private static void Execute(ISpatialDragHandler handler, BaseEventData eventData)
        {
            handler.OnSpatialDrag(ValidateEventData<SpatialPointerEventData>(eventData));
        }

        private static readonly EventFunction<ISpatialEndDragHandler> s_SpatialEndDragHandler = Execute;

        private static void Execute(ISpatialEndDragHandler handler, BaseEventData eventData)
        {
            handler.OnSpatialEndDrag(ValidateEventData<SpatialPointerEventData>(eventData));
        }

        private static readonly EventFunction<ISpatialDropHandler> s_SpatialDropHandler = Execute;

        private static void Execute(ISpatialDropHandler handler, BaseEventData eventData)
        {
            handler.OnSpatialDrop(ValidateEventData<SpatialPointerEventData>(eventData));
        }

        private static readonly EventFunction<ISpatialScrollHandler> s_SpatialScrollHandler = Execute;

        private static void Execute(ISpatialScrollHandler handler, BaseEventData eventData)
        {
            handler.OnSpatialScroll(ValidateEventData<SpatialPointerEventData>(eventData));
        }

        public static EventFunction<ISpatialPointerMoveHandler> spatialPointerMoveHandler => s_SpatialPointerMoveHandler;

        public static EventFunction<ISpatialPointerEnterHandler> spatialPointerEnterHandler => s_SpatialPointerEnterHandler;

        public static EventFunction<ISpatialPointerExitHandler> spatialPointerExitHandler => s_SpatialPointerExitHandler;

        public static EventFunction<ISpatialPointerDownHandler> spatialPointerDownHandler => s_SpatialPointerDownHandler;

        public static EventFunction<ISpatialPointerUpHandler> spatialPointerUpHandler => s_SpatialPointerUpHandler;
        
        public static EventFunction<ISpatialPointerClickHandler> spatialPointerClickHandler => s_SpatialPointerClickHandler;

        public static EventFunction<ISpatialInitializePotentialDragHandler> spatialInitializePotentialDrag => s_SpatialInitializePotentialDragHandler;

        public static EventFunction<ISpatialBeginDragHandler> spatialBeginDragHandler => s_SpatialBeginDragHandler;

        public static EventFunction<ISpatialDragHandler> spatialDragHandler => s_SpatialDragHandler;

        public static EventFunction<ISpatialEndDragHandler> spatialEndDragHandler => s_SpatialEndDragHandler;

        public static EventFunction<ISpatialDropHandler> spatialDropHandler => s_SpatialDropHandler;

        public static EventFunction<ISpatialScrollHandler> spatialScrollHandler => s_SpatialScrollHandler;

    }
}
