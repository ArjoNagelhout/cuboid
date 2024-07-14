//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid.Utils
{
    public static class Math
    {
        /// <summary>
        /// Re-maps a number from one range to another.
        /// See https://processing.org/reference/map_.html
        /// </summary>
        /// <param name="value">the incoming value to be converted</param>
        /// <param name="min">lower bound of the value's current range</param>
        /// <param name="max">upper bound of the value's current range</param>
        /// <param name="targetMin">lower bound of the value's target range</param>
        /// <param name="targetMax">upper bound of the value's target range</param>
        /// <returns></returns>
        public static float Map(float value, float min, float max, float targetMin, float targetMax)
        {
            return targetMin + (targetMax - targetMin) * ((value - min) / (max - min));
        }

        public static Vector2 Map(Vector2 value, Vector2 min, Vector2 max, Vector2 targetMin, Vector2 targetMax)
        {
            float x = Map(value.x, min.x, max.x, targetMin.x, targetMax.x);
            float y = Map(value.y, min.y, max.y, targetMin.y, targetMax.y);
            return new Vector2(x, y);
        }

        public static Vector2 Clamp(Vector2 vector, Vector2 min, Vector2 max)
        {
            return new Vector2(
                Mathf.Clamp(vector.x, min.x, max.x),
                Mathf.Clamp(vector.y, min.y, max.y)
                );
        }

        /// <summary>
        /// Helper function for determining where two rays intersect
        /// </summary>
        /// Implemented from https://www.crewes.org/Documents/ResearchReports/2010/CRR201032.pdf
        public static Vector3 ClosestPointBetweenTwoRays(Ray rayOnWhichPointShouldBePlaced, Ray otherRay)
        {
            Vector3 ray1Position = rayOnWhichPointShouldBePlaced.origin;
            Vector3 ray1Direction = rayOnWhichPointShouldBePlaced.direction;
            Vector3 ray2Position = otherRay.origin;
            Vector3 ray2Direction = otherRay.direction;

            Vector3 p21 = ray2Position - ray1Position;

            Vector3 m = Vector3.Cross(ray2Direction, ray1Direction);
            float m2 = Vector3.Dot(m, m);

            Vector3 r = Vector3.Cross(p21, m / m2);

            float scalar = Vector3.Dot(r, ray2Direction);

            var q1 = ray1Position + scalar * ray1Direction;

            return q1;
        }

        /// <summary>
        /// Snaps a float to the nearest value.
        /// 
        /// To be used by moving or rotating objects while holding shift for example. 
        ///
        /// Usage:
        /// float snappedAngle = Snap(angleInDegrees, 360, 8);
        ///
        /// Snaps in 45 degrees increments (because 360/8 = 45)
        /// </summary>
        /// <param name="original"></param>
        /// <param name="numerator"></param>
        /// <param name="denominator"></param>
        /// <returns></returns>
        public static float Snap(this float original, float numerator, float denominator)
        {
            return Mathf.Round(original * denominator / numerator) * numerator / denominator;
        }
    }
}
