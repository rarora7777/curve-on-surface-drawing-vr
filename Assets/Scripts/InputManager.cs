using System;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

namespace StrokeMimicry
{
    public static class InputManager
    {
        public static bool ActionButtonPressed = false;
        public static bool ActionButtonJustPressed = false;
        public static bool ActionButtonJustReleased = false;

        

        public static bool DevicesReady { get; private set; } = false;

        private static SteamVR_Behaviour_Pose leftHandSVR = null;
        private static SteamVR_Behaviour_Pose rightHandSVR = null;
        private static Pen pen = null;



        public static void Awake()
        {
            SteamVR_Actions.default_DrawEraseAction.onChange += ActionButtonHandler;
            SteamVR_Actions.default_DrawEraseToggle.onStateUp += ToggleButtonHandler;

            var handObjects = GameObject.FindObjectsOfType<SteamVR_Behaviour_Pose>();

            foreach(var obj in handObjects)
            {
                if (obj.inputSource == SteamVR_Input_Sources.LeftHand)
                    leftHandSVR = obj;
                else if (obj.inputSource == SteamVR_Input_Sources.RightHand)
                    rightHandSVR = obj;
            }

            if (StrokeMimicryManager.Instance.PenHand == ControllerHand.Left)
            {
                if (leftHandSVR != null)
                {
                    pen = leftHandSVR.gameObject.AddComponent<Pen>();
                    DevicesReady = true;
                }
                else if (rightHandSVR != null)
                {
                    Debug.LogWarning("Preferred hand controller not found!");
                    pen = rightHandSVR.gameObject.AddComponent<Pen>();
                    DevicesReady = true;
                }
                else
                {
                    Debug.LogError("No controllers found!");
                    DevicesReady = false;
                }
            }
            else
            {
                if (rightHandSVR != null)
                {
                    pen = rightHandSVR.gameObject.AddComponent<Pen>();
                    DevicesReady = true;
                }
                else if (leftHandSVR != null)
                {
                    Debug.LogWarning("Preferred hand controller not found!");
                    pen = leftHandSVR.gameObject.AddComponent<Pen>();
                    DevicesReady = true;
                }
                else
                {
                    Debug.LogError("No controllers found!");
                    DevicesReady = false;
                }
            }

            pen.PenTipPosition = StrokeMimicryManager.Instance.PenTipLocalPosition;
            pen.SprayDirection = StrokeMimicryManager.Instance.PenSprayLocalDirection.normalized;
            Projection.PenObject = pen;
        }

        private static void ToggleButtonHandler(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
        {
            if (StrokeMimicryManager.Instance.CurrentInteractionMode == InteractionMode.Drawing)
                StrokeMimicryManager.Instance.CurrentInteractionMode = InteractionMode.Erasing;
            else
                StrokeMimicryManager.Instance.CurrentInteractionMode = InteractionMode.Drawing;
        }

        
        private static void ActionButtonHandler(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, bool newState)
        {
            ActionButtonJustPressed = ActionButtonJustReleased = false;
            ActionButtonPressed = newState;
            if (newState)
                ActionButtonJustPressed = true;
            else
                ActionButtonJustReleased = true;
        }

        // HandleToggleInputs is called once per frame
        public static void Draw()
        {
            if (!Projection.IsReady)
                return;

            if (ActionButtonJustPressed)
            {
                Projection.TryCreateNewStroke();
                ActionButtonJustPressed = false;
            }
            else if (ActionButtonPressed)
            {
                Projection.Project();
            }
            else if (ActionButtonJustReleased)
            {
                Projection.TryFinishStroke();
                ActionButtonJustReleased = false;
            }

            Projection.UpdateProjectionPointer();
        }

        public static void Erase()
        {
            throw new NotImplementedException();
        }
    }
}
