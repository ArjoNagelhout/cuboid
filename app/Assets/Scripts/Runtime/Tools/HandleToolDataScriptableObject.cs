using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid
{
    /// <summary>
    /// Data for the HandleTool (e.g. TranslateTool, RotateTool and ScaleTool)
    /// so that the materials don't have to get assigned everytime. 
    /// </summary>
    [CreateAssetMenu(fileName = "HandleToolData", menuName = "ShapeReality/HandleToolData")]
    public class HandleToolDataScriptableObject : ScriptableObject
    {
        public Vector3[] AxisRotations = new Vector3[3]
        {
            new Vector3(0, 90f, 90f),
            new Vector3(-90f, -90f, 0),
            new Vector3(0, 0, 0)
        };

        [Header("Materials")]
        public Material PressedMaterial;

        public Material[] DefaultMaterials = new Material[3];
        public Material[] HoveredMaterials = new Material[3];
    }
}
