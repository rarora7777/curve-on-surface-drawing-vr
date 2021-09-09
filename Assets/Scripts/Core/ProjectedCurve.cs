using System.Collections.Generic;
using UnityEngine;

namespace StrokeMimicry
{
    // The ProjectedCurve component contains stores the data that can be used to reconstruct a stroke.
    [RequireComponent(typeof(MeshFilter), typeof(MeshCollider), typeof(MeshRenderer))]
    public class ProjectedCurve : MonoBehaviour
    {
        // Projected stroke points. Strictly speaking, this information is redundant.
        public List<Vector3> Points { get; private set; }
        
        // Information about each projected point. See details of the HitInfo class for more information.
        public List<HitInfo> HitInfoFrames { get; private set; }

        // Projection Mode used for crating this curve.
        public ProjectionMode ProjMode { get; private set; }
        
        // Model Matrix of the Target when the stroke was created.
        public Matrix4x4 ModelMatrix = Matrix4x4.identity;

        // Internal class that creates the mesh for rendering the curve.
        private CurveMeshBuilder MeshBuilder;

        public int PointCount { get { return Points.Count; } }

        public void Init(ProjectionMode mode, Matrix4x4 modelMat)
        {
            Points = new List<Vector3>();
            HitInfoFrames = new List<HitInfo>();
            ProjMode = mode;
            ModelMatrix = modelMat;
            MeshBuilder = new CurveMeshBuilder(this);
            gameObject.GetComponent<MeshRenderer>().material = StrokeMimicryManager.Instance.StrokeMaterial;
        }

        public bool TryDrawPoint(HitInfo hitInfo)
        {
            bool drawn = false;
            // Unsuccessful hit -> finish the current curve
            if (hitInfo.Success == false)
            {
                Finish();
            }
            // Successful hit -> Add a point, and create the corresponding mesh segment.
            else
            {
                AddPointAndHitInfo(hitInfo);
                MeshBuilder.DrawNewStrokeSegment();
                drawn = true;
            }

            return drawn;
        }


        public void Finish()
        {
            Debug.Assert(Points.Count == HitInfoFrames.Count);
            MeshBuilder.Finish();

            Projection.CurrentCurve = null;
        }

        public void AddPointAndHitInfo(HitInfo hit)
        {
            Debug.Assert(hit != null);
            HitInfoFrames.Add(new HitInfo(hit));
            Points.Add(hit.Point);
        }

        // This function handles stroke erasure.
        // Logic: Erase a stroke if the follwing conditions are met:
        // 1. A trigger collider is currently intersecting the stroke.
        // 2. The collider object's parent is the Pen. That is, the collider is the Eraser.
        // 3. Current interaction mode is Erasing (as opposed to Drawing).
        // 4. The Action button is pressed, showing the user's intention to erase.
        private void OnTriggerStay(Collider other)
        {
            if (other.transform.parent == Projection.PenObject.transform &&
                StrokeMimicryManager.Instance.CurrentInteractionMode == InteractionMode.Erasing && InputManager.ActionButtonPressed)
            {
                Destroy(gameObject);
            }
        }
    }
}
