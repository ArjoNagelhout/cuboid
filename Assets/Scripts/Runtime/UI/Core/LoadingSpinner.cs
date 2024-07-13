using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid.UI
{
    /// <summary>
    /// 
    /// </summary>
    public class LoadingSpinner : MonoBehaviour
    {
        private const float k_DegreesPerSecond = -360f;

        private void OnEnable()
        {
            transform.localRotation = Quaternion.identity;
        }

        private void Update()
        {
            Vector3 r = transform.localRotation.eulerAngles;
            transform.localRotation = Quaternion.Euler(r.SetZ(r.z + k_DegreesPerSecond * Time.deltaTime));
        }
    }
}
