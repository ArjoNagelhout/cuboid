//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using UnityEngine;
using System.Collections;

namespace Cuboid.Utils
{
    public static class QuaternionExtensions
    {
        public static void SetXMutating(this ref Quaternion rotation, float x) { rotation = Quaternion.Euler(rotation.eulerAngles.SetX(x)); }
        public static void SetYMutating(this ref Quaternion rotation, float y) { rotation = Quaternion.Euler(rotation.eulerAngles.SetY(y)); }
        public static void SetZMutating(this ref Quaternion rotation, float z) { rotation = Quaternion.Euler(rotation.eulerAngles.SetZ(z)); }

        public static bool RoughlyEquals(this Quaternion quaternion, Quaternion quaternion2)
        {
            return quaternion.x.RoughlyEquals(quaternion2.x) &&
                quaternion.y.RoughlyEquals(quaternion2.y) &&
                quaternion.z.RoughlyEquals(quaternion2.z) &&
                quaternion.w.RoughlyEquals(quaternion2.w);
        }

        /// <summary>
        /// https://gist.github.com/maxattack/4c7b4de00f5c1b95a33b
        /// </summary>
        /// <param name="rot"></param>
        /// <param name="target"></param>
        /// <param name="deriv"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public static Quaternion SmoothDamp(Quaternion rot, Quaternion target, ref Quaternion deriv, float time)
        {
            if (Time.deltaTime < Mathf.Epsilon) return rot;
            // account for double-cover
            var Dot = Quaternion.Dot(rot, target);
            var Multi = Dot > 0f ? 1f : -1f;
            target.x *= Multi;
            target.y *= Multi;
            target.z *= Multi;
            target.w *= Multi;
            // smooth damp (nlerp approx)
            var Result = new Vector4(
                Mathf.SmoothDamp(rot.x, target.x, ref deriv.x, time),
                Mathf.SmoothDamp(rot.y, target.y, ref deriv.y, time),
                Mathf.SmoothDamp(rot.z, target.z, ref deriv.z, time),
                Mathf.SmoothDamp(rot.w, target.w, ref deriv.w, time)
            ).normalized;

            // ensure deriv is tangent
            var derivError = Vector4.Project(new Vector4(deriv.x, deriv.y, deriv.z, deriv.w), Result);
            deriv.x -= derivError.x;
            deriv.y -= derivError.y;
            deriv.z -= derivError.z;
            deriv.w -= derivError.w;

            return new Quaternion(Result.x, Result.y, Result.z, Result.w);
        }
    }
}