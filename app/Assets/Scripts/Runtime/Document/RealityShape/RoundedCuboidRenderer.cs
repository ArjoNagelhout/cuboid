// Copyright (c) 2023 Arjo Nagelhout

using System;
using System.Collections;
using System.Collections.Generic;
using Cuboid.Utils;
using UnityEngine;
using Unity.Mathematics;

namespace Cuboid
{
    /// <summary>
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class RoundedCuboidRenderer : MonoBehaviour
    {
        internal Guid _associatedShapeGuid;

        private float _cornerRadius;
        private int _cornerDivisions;
        private Vector3 _size;

        private float _correctedCornerRadius;
        private Mesh _mesh;
        private Vector3[] _vertices;
        private Vector3[] _normals;

        private void Awake()
        {
            Initialize();
        }

        /*
        private void OnDrawGizmos()
        {
            if (_vertices == null) { return; }
            Gizmos.color = Color.black;
            for (int i = 0; i < _vertices.Length; i++)
            {
                Gizmos.DrawSphere(_vertices[i], 0.01f);
            }
            Gizmos.color = new Color(1, 0, 0, 0.1f);
            Gizmos.DrawCube(transform.position + size / 2, size * 0.99f);
        }*/

        private void Initialize()
        {
            _mesh = new Mesh();
            GetComponent<MeshFilter>().mesh = _mesh;
            _mesh.name = $"mesh_{_associatedShapeGuid}"; // Guid set by the RealityShapeObjectData on instantiation
        }

        public void GenerateMesh(Vector3 size, float cornerRadius, int cornerDivisions)
        {
            _size = size;
            _cornerRadius = cornerRadius;
            _cornerDivisions = cornerDivisions;

            _size = _size.Clamp(Vector3.zero, _size);

            transform.localScale = _size.Inverse();

            _mesh.triangles = new int[0];

            ObjectFitUtils.GetSmallestComponent(_size, out int index, out float smallestSizeComponent);
            _correctedCornerRadius = math.clamp(_cornerRadius, 0, smallestSizeComponent / 2);
            _cornerDivisions = math.clamp(_cornerDivisions, 0, 10);

            CreateVertices();
            CreateTriangles();

            _mesh.bounds = new Bounds(Vector3.zero, _size);
            //_mesh.RecalculateBounds();
        }

        /// <summary>
        /// 
        /// </summary>
        private void CreateVertices()
        {
            int cornerVertices = _cornerDivisions + 2;
            //int totalVertices = (cornerVertices * cornerVertices) * 3 * 8;
            int ring = cornerVertices * 8 - 4;
            int around = ring * (cornerVertices * 2);
            int rowVertices = (cornerVertices * 2) - 2;
            int planeVertices = rowVertices * rowVertices;
            int totalVertices = 2 * planeVertices + around;
            //int topBottom = (cornerVertices * 2) - 1;

            _vertices = new Vector3[totalVertices];
            _normals = new Vector3[_vertices.Length];

            int v = 0;

            float positionPerIndex = _correctedCornerRadius / (cornerVertices - 1);

            for (int y = 0; y < cornerVertices; y++)
            {
                DrawRow(y * positionPerIndex);
            }
            for (int y = 0; y < cornerVertices; y++)
            {
                DrawRow(_size.y - _correctedCornerRadius + y * positionPerIndex);
            }

            // Draw row
            void DrawRow(float y)
            {
                // Side 1
                for (int x = 0; x < cornerVertices; x++)
                {
                    SetVertex(v++, new Vector3(x * positionPerIndex, y, 0));
                }
                for (int x = 0; x < cornerVertices; x++)
                {
                    SetVertex(v++, new Vector3(_size.x - _correctedCornerRadius + x * positionPerIndex, y, 0));
                }

                // Side 2
                for (int z = 1; z < cornerVertices; z++)
                {
                    SetVertex(v++, new Vector3(_size.x, y, z * positionPerIndex));
                }
                for (int z = 0; z < cornerVertices; z++)
                {
                    SetVertex(v++, new Vector3(_size.x, y, _size.z - _correctedCornerRadius + z * positionPerIndex));
                }

                // Side 3
                for (int x = 1; x < cornerVertices; x++)
                {
                    SetVertex(v++, new Vector3(_size.x - x * positionPerIndex, y, _size.z));
                }
                for (int x = 0; x < cornerVertices; x++)
                {
                    SetVertex(v++, new Vector3(_correctedCornerRadius - x * positionPerIndex, y, _size.z));
                }

                // Side 4
                for (int z = 1; z < cornerVertices; z++)
                {
                    SetVertex(v++, new Vector3(0, y, _size.z - z * positionPerIndex));
                }
                for (int z = 0; z < cornerVertices - 1; z++)
                {
                    SetVertex(v++, new Vector3(0, y, _correctedCornerRadius - z * positionPerIndex));
                }
            }


            DrawHorizontalPlane(_size.y); // Top plane
            DrawHorizontalPlane(0); // Bottom plane


            void DrawHorizontalPlane(float y)
            {
                for (int z = 1; z < cornerVertices; z++)
                {
                    DrawHorizontalRow(y, z * positionPerIndex);
                }
                for (int z = 0; z < cornerVertices - 1; z++)
                {
                    DrawHorizontalRow(y, _size.z - _correctedCornerRadius + z * positionPerIndex);
                }
            }

            void DrawHorizontalRow(float y, float z)
            {
                for (int x = 1; x < cornerVertices; x++)
                {
                    SetVertex(v++, new Vector3(x * positionPerIndex, y, z));
                }
                for (int x = 0; x < cornerVertices - 1; x++)
                {
                    SetVertex(v++, new Vector3(_size.x - _correctedCornerRadius + x * positionPerIndex, y, z));
                }
            }

            _mesh.vertices = _vertices;
            _mesh.normals = _normals;
        }

        private void SetVertex(int i, Vector3 position)
        {
            position -= _size / 2;
            Vector3 inner = position;
            _vertices[i] = position;

            inner = inner.Clamp(-_size / 2 + Vector3.one * _correctedCornerRadius, _size / 2 - Vector3.one * _correctedCornerRadius);

            _normals[i] = (_vertices[i] - inner).normalized;
            _vertices[i] = inner + _normals[i] * _correctedCornerRadius;
        }

        /// <summary>
        /// 
        /// </summary>
        private void CreateTriangles()
        {
            int quadsPerCornerOneAxis = _cornerDivisions + 1;
            int quadsPerRowOneFace = quadsPerCornerOneAxis * 2 + 1;
            int quadsPerFace = quadsPerRowOneFace * quadsPerRowOneFace;

            int quads = quadsPerFace * 6;
            int[] triangles = new int[quads * 6];

            int cornerVertices = _cornerDivisions + 2;

            int ring = cornerVertices * 8 - 4;
            //int ring = cornerVertices * 8 - 4;
            //int ring = //quadsPerRowOneFace * 4;
            int t = 0, v = 0;

            // Do the sides of the cube
            for (int y = 0; y < quadsPerRowOneFace; y++, v++)
            {
                for (int q = 0; q < ring - 1; q++, v++)
                {
                    t = SetQuad(triangles, t, v, v + 1, v + ring, v + ring + 1);
                }
                t = SetQuad(triangles, t, v, v - ring + 1, v + ring, v + 1);
            }

            t = CreateTopFace(triangles, t, ring);
            t = CreateBottomFace(triangles, t, ring);

            _mesh.triangles = triangles;
        }

        private int CreateTopFace(int[] triangles, int t, int ring)
        {
            int sizePerAxis = (_cornerDivisions + 2) * 2 - 1;
            int v = ring * sizePerAxis;
            for (int x = 0; x < (sizePerAxis) - 1; x++, v++)
            {
                t = SetQuad(triangles, t, v, v + 1, v + ring - 1, v + ring);
            }
            t = SetQuad(triangles, t, v, v + 1, v + ring - 1, v + 2);

            int vMin = ring * (sizePerAxis + 1) - 1;
            int vMid = vMin + 1;
            int vMax = v + 2;

            for (int z = 1; z < sizePerAxis - 1; z++, vMin--, vMid++, vMax++)
            {
                t = SetQuad(triangles, t, vMin, vMid, vMin - 1, vMid + sizePerAxis - 1);
                for (int x = 1; x < sizePerAxis - 1; x++, vMid++)
                {
                    t = SetQuad(
                        triangles, t,
                        vMid, vMid + 1, vMid + sizePerAxis - 1, vMid + sizePerAxis);
                }
                t = SetQuad(triangles, t, vMid, vMax, vMid + sizePerAxis - 1, vMax + 1);
            }

            int vTop = vMin - 2;
            t = SetQuad(triangles, t, vMin, vMid, vTop + 1, vTop);
            for (int x = 1; x < sizePerAxis - 1; x++, vTop--, vMid++)
            {
                t = SetQuad(triangles, t, vMid, vMid + 1, vTop, vTop - 1);
            }
            t = SetQuad(triangles, t, vMid, vTop - 2, vTop, vTop - 1);

            return t;
        }

        private int CreateBottomFace(int[] triangles, int t, int ring)
        {
            int sizePerAxis = (_cornerDivisions + 2) * 2 - 1;
            int v = 1;
            int vMid = _vertices.Length - (sizePerAxis - 1) * (sizePerAxis - 1);
            t = SetQuad(triangles, t, ring - 1, vMid, 0, 1);
            for (int x = 1; x < sizePerAxis - 1; x++, v++, vMid++)
            {
                t = SetQuad(triangles, t, vMid, vMid + 1, v, v + 1);
            }
            t = SetQuad(triangles, t, vMid, v + 2, v, v + 1);

            int vMin = ring - 2;
            vMid -= sizePerAxis - 2;
            int vMax = v + 2;

            for (int z = 1; z < sizePerAxis - 1; z++, vMin--, vMid++, vMax++)
            {
                t = SetQuad(triangles, t, vMin, vMid + sizePerAxis - 1, vMin + 1, vMid);
                for (int x = 1; x < sizePerAxis - 1; x++, vMid++)
                {
                    t = SetQuad(
                        triangles, t,
                        vMid + sizePerAxis - 1, vMid + sizePerAxis, vMid, vMid + 1);
                }
                t = SetQuad(triangles, t, vMid + sizePerAxis - 1, vMax + 1, vMid, vMax);
            }

            int vTop = vMin - 1;
            t = SetQuad(triangles, t, vTop + 1, vTop, vTop + 2, vMid);
            for (int x = 1; x < sizePerAxis - 1; x++, vTop--, vMid++)
            {
                t = SetQuad(triangles, t, vTop, vTop - 1, vMid, vMid + 1);
            }
            t = SetQuad(triangles, t, vTop, vTop - 1, vMid, vTop - 2);

            return t;
        }

        private static int SetQuad(int[] triangles, int i, int v00, int v10, int v01, int v11)
        {
            triangles[i] = v00;
            triangles[i + 1] = triangles[i + 4] = v01;
            triangles[i + 2] = triangles[i + 3] = v10;
            triangles[i + 5] = v11;
            return i + 6;
        }
    }
}