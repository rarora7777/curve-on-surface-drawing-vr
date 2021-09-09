using UnityEngine;
using Valve.VR;

namespace StrokeMimicry
{
    // InputManager processes all low-level input events and passes on high level information to the projection and UI scripts.
    // Currently based on SteamVR, but should be easy to modify this script to use Unity XR or anything else while maintaining the APIs assumed by other scripts.
    // All the public members are considered to be the API and should not be modified.
    public static class InputManager
    {
        // Action button is used for drawing/erasing
        public static bool ActionButtonPressed = false;
        public static bool ActionButtonJustPressed = false;
        public static bool ActionButtonJustReleased = false;

        public static bool DevicesReady { get; private set; } = false;

        private static SteamVR_Behaviour_Pose leftHandSVR = null;
        private static SteamVR_Behaviour_Pose rightHandSVR = null;

        

        public static void Awake()
        {
            // Action button is used for drawing/erasing
            SteamVR_Actions.default_DrawEraseAction.onChange += ActionButtonHandler;
            // Toggle button switches between drawing and erasing modes
            SteamVR_Actions.default_DrawEraseToggle.onStateUp += ToggleButtonHandler;

            var handObjects = GameObject.FindObjectsOfType<SteamVR_Behaviour_Pose>();

            foreach (var obj in handObjects)
            {
                obj.onConnectedChangedEvent += DeviceConnectionHandler;
            }
        }

        private static void ToggleButtonHandler(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
        {
            if (StrokeMimicryManager.Instance.CurrentInteractionMode == InteractionMode.Drawing)
            {
                StrokeMimicryManager.Instance.CurrentInteractionMode = InteractionMode.Erasing;
            }
            else
            {
                StrokeMimicryManager.Instance.CurrentInteractionMode = InteractionMode.Drawing;
            }

            Projection.TogglePenUI(StrokeMimicryManager.Instance.CurrentInteractionMode);
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

        private static void DeviceConnectionHandler(SteamVR_Behaviour_Pose fromController, SteamVR_Input_Sources fromSource, bool connected)
        {
            var pen = Projection.PenObject;

            if (connected)
            {
                if (fromSource == SteamVR_Input_Sources.LeftHand)
                    leftHandSVR = fromController;
                else if (fromSource == SteamVR_Input_Sources.RightHand)
                    rightHandSVR = fromController;
            }
            else
            {
                if (fromSource == SteamVR_Input_Sources.LeftHand)
                    leftHandSVR = null;
                else if (fromSource == SteamVR_Input_Sources.RightHand)
                    rightHandSVR = null;
            }

            if (leftHandSVR == null && rightHandSVR == null)
            {
                Debug.LogError("No controllers connected!");
                DeleteAllPenComponents();
                pen = null;
                DevicesReady = false;
            }

            if (StrokeMimicryManager.Instance.PenHand == ControllerHand.Left)
            {
                if (leftHandSVR != null)
                {
                    if (leftHandSVR.gameObject.GetComponent<Pen>() == null)
                    {
                        DeleteAllPenComponents();
                        Debug.Log("Stroke Mimicry drawing hand set to Left.");
                        pen = leftHandSVR.gameObject.AddComponent<Pen>();
                        DevicesReady = true;
                    }
                }
                else if (rightHandSVR != null)
                {
                    if (rightHandSVR.gameObject.GetComponent<Pen>() == null)
                    {
                        DeleteAllPenComponents();
                        Debug.LogWarning("Preferred hand controller not found! Using Right hand controller.");
                        pen = rightHandSVR.gameObject.AddComponent<Pen>();
                        DevicesReady = true;
                    }
                }
            }
            else
            {
                if (rightHandSVR != null)
                {
                    if (rightHandSVR.gameObject.GetComponent<Pen>() == null)
                    {
                        DeleteAllPenComponents();
                        Debug.Log("Stroke Mimicry drawing hand set to Right.");
                        pen = rightHandSVR.gameObject.AddComponent<Pen>();
                        DevicesReady = true;
                    }
                }
                else if (leftHandSVR != null)
                {
                    if (leftHandSVR.gameObject.GetComponent<Pen>() == null)
                    {
                        DeleteAllPenComponents();
                        Debug.LogWarning("Preferred hand controller not found! Using Left hand controller.");
                        pen = leftHandSVR.gameObject.AddComponent<Pen>();
                        DevicesReady = true;
                    }
                }
            }

            if (pen != null)
            {
                pen.PenTipPosition = StrokeMimicryManager.Instance.PenTipLocalPosition;
                pen.SprayDirection = StrokeMimicryManager.Instance.PenSprayLocalDirection.normalized;
            }

            Projection.PenObject = pen;
        }

        private static void DeleteAllPenComponents()
        {
            var pens = GameObject.FindObjectsOfType<Pen>();
            foreach (var pen in pens)
                UnityEngine.Object.Destroy(pen);
        }

        // Called once per frame when interaction mode is drawing
        public static void Draw()
        {
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

            Projection.UpdateProjectionPointerAndLaser();
        }

        // Called once per frame when interaction mode is erasing
        public static void Erase()
        {
            // Reserved for future use
            // Curve erasure logic is handled via `ProjectedCurve.OnTriggerStay()`
        }
    }
}
