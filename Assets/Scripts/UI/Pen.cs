using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StrokeMimicry
{
    // Pen is a UI class handling the display of the projection pointer, projection laser, and the eraser.
    public class Pen : MonoBehaviour
    {
        [Tooltip("Position of the pen tip in the local frame of the controller.")]
        public Vector3 PenTipPosition;

        [Tooltip("Spraypaint direction in the local frame of the controller.")]
        public Vector3 SprayDirection;

        public bool ShowProjectionPointer = true;
        
        private const float laserLength = 10f;
        private const float laserThickness = 0.00075f;
        private const float pointerRelativeThickness = 2.0f;
        private const float eraserThickness = 0.01f;

        private bool _showProjectionLaser = true;
        public bool ShowProjectionLaser
        {
            get => _showProjectionLaser;
            set
            {
                ShowProjectionLaser = value;
                if (laserRenderer != null)
                    laserRenderer.enabled = value;
            }
        }


        private MeshRenderer pointerRenderer = null;
        private MeshRenderer laserRenderer = null;
        private MeshRenderer eraserRenderer = null;
        private SphereCollider eraserCollider = null;

        void Start()
        {
            // Create the pointer: a sphere that marks the current projected point on the target mesh
            float strokeWidth = StrokeMimicryManager.Instance.MeshThickness;
            GameObject pointer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pointer.name = "ProjectionPointer";
            pointerRenderer = pointer.GetComponent<MeshRenderer>();
            pointerRenderer.material = StrokeMimicryManager.Instance.PointerMaterial;
            pointerRenderer.enabled = false;
            pointer.transform.SetParent(transform, false);
            pointer.transform.localScale = new Vector3(
                pointerRelativeThickness * strokeWidth,
                pointerRelativeThickness * strokeWidth,
                pointerRelativeThickness * strokeWidth);
            Destroy(pointer.GetComponent<MeshCollider>());

            // Create the laser: a thin cylinder that goes from the pen tip to the current projected point
            // If no projected point exists, the cylinder extends out to infinity in the spray direction
            GameObject laser = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            laser.name = "ProjectionLaser";
            laserRenderer = laser.GetComponent<MeshRenderer>();
            laserRenderer.material = StrokeMimicryManager.Instance.LaserMaterial;
            laser.transform.SetParent(transform, false);
            laser.transform.localScale = new Vector3(laserThickness, 0.5f*laserLength, laserThickness);
            Destroy(laser.GetComponent<MeshCollider>());

            // Create the eraser object: a sphere with a trigger collider. The curve erasure logic is handled by the `ProjectedCurve` class.
            GameObject eraser = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eraser.name = "CurveEraser";
            eraserRenderer = eraser.GetComponent<MeshRenderer>();
            eraserRenderer.material = StrokeMimicryManager.Instance.EraserMaterial;
            eraserCollider = eraser.GetComponent<SphereCollider>();
            eraserCollider.isTrigger = true;
            eraser.AddComponent<Rigidbody>();
            eraser.GetComponent<Rigidbody>().useGravity = false;
            eraser.transform.SetParent(transform, false);
            eraser.transform.localPosition = PenTipPosition;
            eraser.transform.localScale = new Vector3(eraserThickness, eraserThickness, eraserThickness);

            // Show the drawing or erasing UI based on the current interaction mode
            if (StrokeMimicryManager.Instance.CurrentInteractionMode == InteractionMode.Drawing)
            {
                if (ShowProjectionLaser)
                    laserRenderer.enabled = true;
                eraserRenderer.enabled = false;
                eraserCollider.enabled = false;
            }
            else
            {
                laserRenderer.enabled = false;
                eraserRenderer.enabled = true;
                eraserCollider.enabled = true;
            }

            // Add the marker script `StrokeMimicryUI` so that these objects can be deleted when needed
            pointer.AddComponent<StrokeMimicryUI>();
            laser.AddComponent<StrokeMimicryUI>();
            eraser.AddComponent<StrokeMimicryUI>();
        }

        
        public void UpdatePointerAndLaser(Ray ray, HitInfo hit, Transform targetTransform)
        {
            if (hit.Success)
            {
                // hit point in local coordinates of the controller, that is, of `this.gameObject`
                Vector3 hitPoint = transform.InverseTransformPoint(targetTransform.TransformPoint(hit.Point));

                pointerRenderer.transform.localPosition = hitPoint;
                
                laserRenderer.transform.localPosition = 0.5f * (hitPoint + PenTipPosition);
                laserRenderer.transform.localScale = new Vector3(
                    laserThickness,
                    0.5f*(hitPoint - PenTipPosition).magnitude,
                    laserThickness);
                laserRenderer.transform.up = ray.direction;

                if (ShowProjectionPointer)
                {
                    pointerRenderer.enabled = true;
                }
            }
            else
            {
                pointerRenderer.enabled = false;

                laserRenderer.transform.localPosition = PenTipPosition + 0.5f * laserLength * SprayDirection;
                laserRenderer.transform.localScale = new Vector3(
                    laserThickness,
                    0.5f*laserLength,
                    laserThickness);
                laserRenderer.transform.up = transform.TransformDirection(SprayDirection);
            }
        }

        public void ToggleUI(InteractionMode newMode)
        {
            if (newMode == InteractionMode.Drawing)
            {
                if (ShowProjectionLaser)
                    laserRenderer.enabled = true;

                eraserRenderer.enabled = false;
                eraserCollider.enabled = false;
            }
            else
            {
                laserRenderer.enabled = false;
                pointerRenderer.enabled = false;

                eraserRenderer.enabled = true;
                eraserCollider.enabled = true;
            }
        }

        // Remove the objects created by this script
        public void OnDestroy()
        {
            var objs = gameObject.GetComponentsInChildren<StrokeMimicryUI>();

            foreach (var obj in objs)
                Destroy(obj.gameObject);
        }
    }

}
