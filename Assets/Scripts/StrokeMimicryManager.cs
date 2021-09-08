using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using System;

namespace StrokeMimicry
{
    public class StrokeMimicryManager : Singleton<StrokeMimicryManager>
    {
        
        [Tooltip("Number of vertices per stroke point making up the cross-section of the cylindrical stroke mesh.")]
        [Range(3, 10)]
        public int MeshVerticesPerPoint = 6;

        [Tooltip("Thickness of the stroke mesh cross-section (in metres).")]
        public float MeshThickness = 0.0015f;

        public bool LogDebugInfo = false;

        [Tooltip("Path to the folder containing the MATLAB-generated files required for Phong projection.")]
        public string PhongFilesPath = Application.streamingAssetsPath;

        [Tooltip("The projection mode.")]
        public ProjectionMode ProjectionMode = ProjectionMode.MimicryClosest;

        [Tooltip("The button the user presses to draw or erase strokes.")]
        public InputFeatureUsage<bool> ActionButton = CommonUsages.triggerButton;

        //[Tooltip("Set the preferred hand for draw/erase button.")]
        //public ControllerHand ActionButtonHand = ControllerHand.Right;

        [Tooltip("The button the user presses to toggle between drawing and erasing modes.")]
        public InputFeatureUsage<bool> ToggleButton = CommonUsages.primaryButton;

        //[Tooltip("Set the preferred hand for toggle button.")]
        //public ControllerHand ToggleButtonHand = ControllerHand.Right;

        [Tooltip("The preferred drawing hand.")]
        public ControllerHand PenHand = ControllerHand.Right;

        [Tooltip("Position of the pen tip in the controller's local coordinate system.")]
        public Vector3 PenTipLocalPosition = new Vector3(-0.02f, -0.01f, -0.01f);

        public Vector3 PenSprayLocalDirection = new Vector3(0f, 1f, 0f);

        [Tooltip("Material for stroke meshes.")]
        public Material StrokeMaterial;

        [Tooltip("Material for projection pointer.")]
        public Material PointerMaterial;

        [Tooltip("Material for projection laser.")]
        public Material LaserMaterial;

        [Tooltip("Material for eraser.")]
        public Material EraserMaterial;

        public DateTime StartTime { get; private set; }

        public InteractionMode CurrentInteractionMode { get; set; } = InteractionMode.Drawing;


        protected StrokeMimicryManager() { }

        void Awake()
        {
            StartTime = DateTime.Now;
            InputManager.Awake();
        }

        void Update()
        {
            Projection.Update();

            switch(CurrentInteractionMode)
            {
                case InteractionMode.Drawing:
                    InputManager.Draw();
                    break;
                case InteractionMode.Erasing:
                    InputManager.Erase();
                    break;
            }
        }
    }
}