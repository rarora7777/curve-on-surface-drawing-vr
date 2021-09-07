using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace StrokeMimicry
{
    public static class InputManager
    {
        public static bool ActionButtonPressed = false;
        public static bool ActionButtonJustPressed = false;
        public static bool ActionButtonJustReleased = false;

        private static InputDevice? penDevice;
        private static InputDevice? actionButtonDevice;
        private static InputDevice? toggleButtonDevice;

        private static List<InputDevice> leftHandControllers;
        private static List<InputDevice> rightHandControllers;

        private static List<InputDevice> leftHandControllersWithActionButton;
        private static List<InputDevice> rightHandControllersWithActionButton;

        private static List<InputDevice> leftHandControllersWithToggleButton;
        private static List<InputDevice> rightHandControllersWithToggleButton;

        private static bool actionButtonLastState = false;
        private static bool toggleButtonLastState = false;

        public static bool DevicesReady { get; private set; } = false;

        // HandleToggleInputs is called once per frame
        public static void HandleToggleInputs()
        {
            ActionButtonJustPressed = ActionButtonJustReleased = false;

            if (!DevicesReady)
                return;


            bool actionButtonState = actionButtonDevice != null &&    // device present
                actionButtonDevice.Value.TryGetFeatureValue(StrokeMimicryManager.Instance.ActionButton, out bool temp)  // received a value
                && temp;    // value is true

            if (actionButtonState != actionButtonLastState)
            {
                if (actionButtonState)
                    ActionButtonJustPressed = true;
                else
                    ActionButtonJustReleased = true;
            }

            ActionButtonJustPressed = actionButtonLastState = actionButtonState;

            // do not process toggle button actions during draw/erase action
            if (ActionButtonPressed || ActionButtonJustReleased)
                return;

            bool toggleButtonState = actionButtonDevice != null &&    // device present
                toggleButtonDevice.Value.TryGetFeatureValue(StrokeMimicryManager.Instance.ToggleButton, out temp)  // received a value
                && temp;    // value is true

            if (toggleButtonState != toggleButtonLastState && toggleButtonState == false)   // toggle button released
            {
                if (StrokeMimicryManager.Instance.CurrentInteractionMode == InteractionMode.Drawing)
                    StrokeMimicryManager.Instance.CurrentInteractionMode = InteractionMode.Erasing;
                else
                    StrokeMimicryManager.Instance.CurrentInteractionMode = InteractionMode.Drawing;
            }

            toggleButtonLastState = toggleButtonState;
        }

        public static void Draw()
        {
            if (!Projection.IsReady)
                return;

            if (ActionButtonJustPressed)
            {
                Projection.TryCreateNewStroke();
            }
            else if (ActionButtonPressed)
            {
                Projection.Project();
            }
            else if (ActionButtonJustReleased)
            {
                Projection.TryFinishStroke();
            }
        }

        public static void Erase()
        {

        }

        public static void Awake()
        {
            
        }

        public static void OnEnable()
        {
            List<InputDevice> allDevices = new();
            var deviceCharacteristics = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller;
            InputDevices.GetDevicesWithCharacteristics(deviceCharacteristics, allDevices);

            foreach (InputDevice device in allDevices)
                InputDevices_deviceConnected(device);
            InputDevices.deviceConnected += InputDevices_deviceConnected;
            InputDevices.deviceDisconnected += InputDevices_deviceDisconnected;
        }

        public static void OnDisable()
        {
            InputDevices.deviceConnected -= InputDevices_deviceConnected;
            InputDevices.deviceDisconnected -= InputDevices_deviceDisconnected;
        }

        static void InputDevices_deviceConnected(InputDevice device)
        {

            bool hasActionButton = device.TryGetFeatureValue(StrokeMimicryManager.Instance.ActionButton, out _);
            bool hasToggleButton = device.TryGetFeatureValue(StrokeMimicryManager.Instance.ToggleButton, out _);
            
            if ((device.characteristics & InputDeviceCharacteristics.Left) == InputDeviceCharacteristics.Left)
            {
                leftHandControllers.Add(device);

                if (hasActionButton)
                    leftHandControllersWithActionButton.Add(device);
                if (hasToggleButton)
                    leftHandControllersWithToggleButton.Add(device);
                
            }

            if ((device.characteristics & InputDeviceCharacteristics.Right) == InputDeviceCharacteristics.Left)
            {
                rightHandControllers.Add(device);

                if (hasActionButton)
                    rightHandControllersWithActionButton.Add(device);
                if (hasToggleButton)
                    rightHandControllersWithToggleButton.Add(device);
            }

            MapDevicesToInputs();
        }

        static void InputDevices_deviceDisconnected(InputDevice device)
        {
            if (leftHandControllers.Contains(device))
                leftHandControllers.Remove(device);

            if (rightHandControllers.Contains(device))
                rightHandControllers.Remove(device);

            if (leftHandControllersWithActionButton.Contains(device))
                leftHandControllersWithActionButton.Remove(device);

            if (rightHandControllersWithActionButton.Contains(device))
                rightHandControllersWithActionButton.Remove(device);

            if (leftHandControllersWithToggleButton.Contains(device))
                leftHandControllersWithToggleButton.Remove(device);

            if (rightHandControllersWithToggleButton.Contains(device))
                rightHandControllersWithToggleButton.Remove(device);

            MapDevicesToInputs();
        }

        static void MapDevicesToInputs()
        {
            if (StrokeMimicryManager.Instance.PenHand == ControllerHand.Left && leftHandControllers.Count > 0)
                penDevice = leftHandControllers[0];
            else if (StrokeMimicryManager.Instance.PenHand == ControllerHand.Right && rightHandControllers.Count > 0)
                penDevice = rightHandControllers[0];
            else if (rightHandControllers.Count > 0)
                penDevice = rightHandControllers[0];
            else if (leftHandControllers.Count > 0)
                penDevice = leftHandControllers[0];

            if (StrokeMimicryManager.Instance.ActionButtonHand == ControllerHand.Left && leftHandControllersWithActionButton.Count > 0)
                actionButtonDevice = leftHandControllersWithActionButton[0];
            else if (StrokeMimicryManager.Instance.ActionButtonHand == ControllerHand.Right && rightHandControllersWithActionButton.Count > 0)
                actionButtonDevice = rightHandControllersWithActionButton[0];
            else if (rightHandControllersWithActionButton.Count > 0)
                actionButtonDevice = rightHandControllersWithActionButton[0];
            else if (leftHandControllersWithActionButton.Count > 0)
                actionButtonDevice = leftHandControllersWithActionButton[0];

            if (StrokeMimicryManager.Instance.ToggleButtonHand == ControllerHand.Left && leftHandControllersWithToggleButton.Count > 0)
                toggleButtonDevice = leftHandControllersWithToggleButton[0];
            else if (StrokeMimicryManager.Instance.ToggleButtonHand == ControllerHand.Right && rightHandControllersWithToggleButton.Count > 0)
                toggleButtonDevice = rightHandControllersWithToggleButton[0];
            else if (rightHandControllersWithToggleButton.Count > 0)
                toggleButtonDevice = rightHandControllersWithToggleButton[0];
            else if (leftHandControllersWithToggleButton.Count > 0)
                toggleButtonDevice = leftHandControllersWithToggleButton[0];

            if (penDevice is null || actionButtonDevice is null || toggleButtonDevice is null)
                DevicesReady = false;
            else
                DevicesReady = true;
        }
    }
}
