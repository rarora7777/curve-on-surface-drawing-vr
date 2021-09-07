﻿using System.Collections;
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

        [Tooltip("Set the projection mode here.")]
        public ProjectionMode ProjectionMode = ProjectionMode.MimicryClosest;

        [Tooltip("Set the button the user presses to draw or erase strokes.")]
        public InputFeatureUsage<bool> ActionButton = CommonUsages.triggerButton;

        [Tooltip("Set the preferred hand for draw/erase button.")]
        public ControllerHand ActionButtonHand = ControllerHand.Right;

        [Tooltip("Set the button the user presses to toggle between drawing and erasing modes.")]
        public InputFeatureUsage<bool> ToggleButton = CommonUsages.primaryButton;

        [Tooltip("Set the preferred hand for toggle button.")]
        public ControllerHand ToggleButtonHand = ControllerHand.Right;

        [Tooltip("Set the preferred drawing hand.")]
        public ControllerHand PenHand = ControllerHand.Right;

        public DateTime StartTime { get; private set; }

        public InteractionMode CurrentInteractionMode { get; set; } = InteractionMode.Drawing;


        protected StrokeMimicryManager() { }

        void Awake()
        {
            StartTime = DateTime.Now;
        }

        void Update()
        {
            Projection.Update();

            InputManager.HandleToggleInputs();

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

        void OnEnable()
        {
            InputManager.OnEnable();
        }

        void OnDisable()
        {
            InputManager.OnDisable();
        }
    }
}