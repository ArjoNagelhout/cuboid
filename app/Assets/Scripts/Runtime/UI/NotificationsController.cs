//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;

namespace Cuboid.UI
{
    public class NotificationsController : MonoBehaviour
    {
        private static NotificationsController _instance;
        public static NotificationsController Instance => _instance;

        [SerializeField] private GameObject _notificationPrefab;

        private PopupsController _popupsController;

        [SerializeField] private float _notificationEnterScale;
        [SerializeField] private float _notificationExitScale;
        [SerializeField] private float _notificationEnterDuration;
        [SerializeField] private float _notificationExitDuration;

        [SerializeField] private float _zOffsetPerNotification;

        private void Awake()
        {
            // Singleton implementation
            if (_instance != null && _instance != this) { Destroy(this); } else { _instance = this; }
        }

        private void Start()
        {
            _popupsController = PopupsController.Instance;
        }

        private int _notificationCount = 0;

        private Transform GetParent()
        {
            Transform parent = _popupsController._popupTransform;
            if (_popupsController._popups.Count == 0) { return parent; }

            int worldSpacePopupIndex = _popupsController._popups.FindIndex(p => p.Params.WorldSpace);
            if (worldSpacePopupIndex != -1)
            {
                // world space
                parent = _popupsController._worldSpacePopupTransform;
            }
            return parent;
        }

        private RectTransform GetLastPopupRectTransform()
        {
            if (_popupsController._popups.Count == 0)
            {
                return _popupsController._popupTransform.GetComponent<RectTransform>();
            }

            // get the transform of the uppermost popup,
            // this, because the notification should be positioned exactly on the top of the popup. 
            RectTransform rectTransform = _popupsController._popups.Last()._rectTransform;
            return rectTransform;
        }

        public void OpenNotification(Notification.Data data)
        {
            Transform parent = GetParent();
            RectTransform lastPopupRectTransform = GetLastPopupRectTransform();

            // instantiate the notification with a transition
            GameObject notificationGameObject = Instantiate(_notificationPrefab, parent, false);
            RectTransform notificationRectTransform = notificationGameObject.GetComponent<RectTransform>();

            // set data
            Notification notification = notificationGameObject.GetComponent<Notification>();
            notification.ActiveData = data;

            _notificationCount++;
            
            // set recttransform
            SetNotificationRectTransform(notificationRectTransform, lastPopupRectTransform);

            // animate in
            notificationRectTransform.localScale = Vector3.one * _notificationEnterScale;
            notificationRectTransform.DOScale(1.0f, _notificationEnterDuration).SetEase(Ease.OutBack, 1.2f);

            // start the close coroutine
            StartCoroutine(CloseNotificationAfterDelay(notificationGameObject, data.DisplayDurationInSeconds));
        }

        private void SetNotificationRectTransform(RectTransform r, RectTransform lastPopup)
        {
            // top, center
            r.pivot = new Vector2(0.5f, 1);

            // top, center
            r.anchorMin = new Vector2(0.5f, 1);
            r.anchorMax = new Vector2(0.5f, 1);

            // set position to the top of the lastPopup rect transform
            Vector3[] corners = new Vector3[4];
            lastPopup.GetWorldCorners(corners); // order: bottom left, top left, top right, bottom right
            Vector3 position = (corners[1] + corners[2]) / 2;
            r.position = position;

            Vector3 p = r.localPosition;
            r.localPosition = p.SetZ(p.z + _notificationCount * _zOffsetPerNotification);

            // limit the size of the notification to the width of the popup. 
            float w = lastPopup.rect.width;
            if (r.sizeDelta.x > w)
            {
                r.sizeDelta = new Vector2(w, r.sizeDelta.y);
            }
        }

        private IEnumerator CloseNotificationAfterDelay(GameObject notification, float delayInSeconds)
        {
            yield return new WaitForSeconds(delayInSeconds);

            // animate out, then destroy notification
            Transform notificationTransform = notification.transform;
            notificationTransform.DOScale(_notificationExitScale, _notificationExitDuration).SetEase(Ease.OutQuart).OnComplete(() =>
            {
                notificationTransform.DOKill();
                Destroy(notification);
                _notificationCount--;
            });
        }
    }
}
