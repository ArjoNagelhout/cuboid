using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid
{
    public class CustomSelectionOutlineTest : MonoBehaviour
    {
        [SerializeField] private float _interval = 0.5f;
        [SerializeField] private List<GameObject> _gameObjects = new List<GameObject>();

        private bool _selected = false;
        public bool Selected
        {
            get => _selected;
            set
            {
                _selected = value;

                foreach (GameObject gameObject in _gameObjects)
                {
                    gameObject.SetLayerRecursively(_selected ? Layers.Selected : Layers.Default);
                }
            }
        }

        private void Start()
        {
            StartCoroutine(ToggleSelected());
        }

        private IEnumerator ToggleSelected()
        {
            while (true)
            {
                // toggle
                Selected = !Selected;
                yield return new WaitForSeconds(_interval);
            }
        }
    }
}
