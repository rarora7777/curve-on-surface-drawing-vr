using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StrokeMimicry
{
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


            GameObject laser = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            laser.name = "ProjectionLaser";
            laserRenderer = laser.GetComponent<MeshRenderer>();
            laserRenderer.material = StrokeMimicryManager.Instance.LaserMaterial;
            laser.transform.SetParent(transform, false);
            laser.transform.localScale = new Vector3(laserThickness, 0.5f*laserLength, laserThickness);
            Destroy(laser.GetComponent<MeshCollider>());

            GameObject eraser = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eraser.name = "StrokeEraser";
            eraserRenderer = eraser.GetComponent<MeshRenderer>();
            eraserRenderer.material = StrokeMimicryManager.Instance.EraserMaterial;
            eraserCollider = eraser.GetComponent<SphereCollider>();
            eraserCollider.isTrigger = true;
            eraser.AddComponent<Rigidbody>();
            eraser.GetComponent<Rigidbody>().useGravity = false;
            eraser.transform.SetParent(transform, false);
            eraser.transform.localPosition = PenTipPosition;
            eraser.transform.localScale = new Vector3(eraserThickness, eraserThickness, eraserThickness);

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
        }

        // Update is called once per frame
        void Update()
        {

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
    }

}
