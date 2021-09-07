using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using g3;
using System.Linq;

namespace StrokeMimicry
{
    public enum ProjectionMode
    {
        Spraypaint = 0,
        MimicryPhong = 1,
        MimicryClosest = 2
    }

    public enum InteractionMode
    {
        Drawing,
        Erasing
    }

    public enum ClosestPointMethod
    {
        Vanilla,
        Phong
    }

    public enum ControllerHand
    {
        Left,
        Right
    }

    public enum Input
    {
        Pen,
        Action,
        Toggle
    }

    public static class StrokeMimicryUtils
    {
        public static Vector3 ChangeHandedness(Vector3 v)
        {
            return new Vector3(-v.x, v.y, v.z);
        }

        public static Vector3d ChangeHandedness(Vector3d v)
        {
            return new Vector3d(-v.x, v.y, v.z);
        }

        public static Quaternion ChangeHandedness(Quaternion q)
        {
            return new Quaternion(-q.x, q.y, q.z, -q.w);
        }

        public class PointSet : g3.IPointSet
        {
            private Vector3d[] vertices;

            public PointSet(Vector3d[] vertIn)
            {
                vertices = vertIn;
            }

            public PointSet(IEnumerable<Vector3d> vertIn)
            {
                vertices = vertIn.ToArray();
            }

            public int VertexCount
            {
                get { return vertices.Length; }
            }

            public int MaxVertexID
            {
                get { return vertices.Length - 1; }
            }

            public bool HasVertexNormals { get { return false; } }
            public bool HasVertexColors { get { return false; } }

            public Vector3d GetVertex(int i)
            {
                return i < vertices.Length ? vertices[i] : Vector3d.Zero;
            }

            public Vector3f GetVertexNormal(int i)
            {
                return Vector3f.Zero;
            }

            public Vector3f GetVertexColor(int i)
            {
                return Vector3f.Zero;
            }

            public bool IsVertex(int vID)
            {
                return vID < vertices.Length ? true : false;
            }

            // iterators allow us to work with gaps in index space
            public IEnumerable<int> VertexIndices()
            {
                for (int i = 0; i < vertices.Length; ++i)
                    yield return i;
            }

            public int Timestamp { get { return -1; } }
        }
    }
}

namespace ExtensionMethods
{
    public static class CustomExtensions
    {
        public static Vector3d GetTriNormalLeftHanded(this g3.DMesh3 mesh, int tidx)
        {
            return -mesh.GetTriNormal(tidx);
        }
    }
}

