using System.Collections.Generic;
using UnityEngine;

namespace StrokeMimicry
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshCollider))]
    public class Stroke : MonoBehaviour
    {
        public List<Vector3> Points { get; private set; }
        public List<HitInfo> HitInfoFrames { get; private set; }
        public ProjectionMode ProjMode { get; private set; }
        
        public Matrix4x4 ModelMatrix = Matrix4x4.identity;

        private StrokeMesh Mesh;

        public int PointCount { get { return Points.Count; } }

        public void Init(ProjectionMode mode, Matrix4x4 modelMat)
        {
            Points = new List<Vector3>();
            HitInfoFrames = new List<HitInfo>();
            ProjMode = mode;
            ModelMatrix = modelMat;
            Mesh = new StrokeMesh(this);
        }

        public bool TryDrawPoint(HitInfo hitInfo)
        {
            bool drawn = false;
            if (hitInfo.Success == false)
            {
                Finish();
            }
            else
            {
                AddPointAndHitInfo(hitInfo);
                Mesh.DrawNewStrokeSegment();
                drawn = true;
            }

            return drawn;
        }


        public void Finish()
        {
            Debug.Assert(Points.Count == HitInfoFrames.Count);
            Projection.CurrentStroke = null;
            Mesh.Finish();
        }

        public void AddPointAndHitInfo(HitInfo hit)
        {
            Debug.Assert(hit != null);
            HitInfoFrames.Add(new HitInfo(hit));
            Points.Add(hit.Point);
        }
    }
}
