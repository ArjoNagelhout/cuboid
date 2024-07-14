// Copyright (c) 2023 Arjo Nagelhout

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace Cuboid.UI
{
    /// <summary>
    /// A tooltip is a component that can be added to any object so that when hovering over it
    /// for a certain amount of time (e.g. 1 second), it will show a popup with a
    /// Title and a Description
    /// </summary>
    public class Tooltip : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler
    {
        private const float k_TooltipDelay = 1f;

        private TooltipPopup _instantiatedTooltipPopup;

        private RectTransform _rectTransform;

        private IEnumerator _openPopupCoroutine;

        [Header("Data")]
        [SerializeField] private TooltipData _data;

        [System.Serializable]
        public class TooltipData
        {
            /// <summary>
            /// The title will be shown in bold
            /// </summary>
            public string Title = null;

            /// <summary>
            /// Description can be a paragraph underneath, popup will be automatically
            /// scaled to fit the description. 
            /// </summary>
            public string Description = null;
        }

        private void Start()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            ClosePopup();
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            if (_openPopupCoroutine != null)
            {
                ClosePopup();
            }
            _openPopupCoroutine = OpenPopupCoroutine();
            StartCoroutine(_openPopupCoroutine);
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            ClosePopup();
        }

        private IEnumerator OpenPopupCoroutine()
        {
            yield return new WaitForSeconds(k_TooltipDelay);
            OpenPopup();
        }

        private void ClosePopup()
        {
            if (_openPopupCoroutine != null)
            {
                StopCoroutine(_openPopupCoroutine);
            }
            _openPopupCoroutine = null;

            if (_instantiatedTooltipPopup == null) { return; } // no popup to destroy

            _instantiatedTooltipPopup.transform.DOKill();

            // animate scale to 0, then destroy
            _instantiatedTooltipPopup.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.OutQuart).OnComplete(() =>
            {
                Destroy(_instantiatedTooltipPopup);
            });
        }

        private void OpenPopup()
        {
            // instantiate
            GameObject prefab = PopupsController.Instance.TooltipPopupPrefab;
            if (prefab == null) { return; }

            GameObject popupGameObject = Instantiate(prefab, transform, false);
            TooltipPopup tooltipPopup = popupGameObject.GetComponent<TooltipPopup>();
            tooltipPopup.Data = _data;

            _instantiatedTooltipPopup = tooltipPopup;

            // set position to the bottom of the rect of this image / rect transform
            //popupGameObject.transform.position =
            if (_rectTransform != null)
            {
                Transform t = popupGameObject.transform;
                t.localPosition = t.localPosition.SetY(_rectTransform.rect.yMin);
            }

            // animate in
            popupGameObject.transform.localScale = Vector3.zero;
            popupGameObject.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack, 1.2f);
        }

        private void OnDisable()
        {
            Destroy(_instantiatedTooltipPopup);
        }

        private void OnDestroy()
        {
            if (_instantiatedTooltipPopup != null)
            {
                Destroy(_instantiatedTooltipPopup);
            }
        }
    }
}
