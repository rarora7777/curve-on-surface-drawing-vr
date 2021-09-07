using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using g3;

namespace StrokeMimicry
{
    public class HitInfo
    {
        public Vector3 Point = Vector3.zero;
        public Vector3 Normal = Vector3.up;
        public int TriangleIndex = -1;
        public Vector3 BarycentricCoordinate = Vector3.zero;
        public bool Success = false;
        public float Distance = Mathf.Infinity;

        public DataFrame Frame;

        public HitInfo(DataFrame currentDataFrame, Transform modelTransform)
        {
            Frame = DataFrame.WorldToLocal(currentDataFrame, modelTransform);
        }

        public void Init(Vector3d pt, Vector3d n, int tIdx, double dist, Vector3d b)
        {
            Point = (Vector3)pt;
            Normal = (Vector3)n;
            TriangleIndex = tIdx;
            Distance = (float)dist;
            BarycentricCoordinate = (Vector3)b;

            Success = true;
        }

        public HitInfo(HitInfo old)
        {
            Point = old.Point;
            Normal = old.Normal;
            TriangleIndex = old.TriangleIndex;
            Distance = old.Distance;
            BarycentricCoordinate = old.BarycentricCoordinate;
            Success = old.Success;
            Frame = old.Frame;
        }
    }
}