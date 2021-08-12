using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StrokeMimicry
{
    public enum ProjectionMode
    {
        Spraypaint = 0,
        MimicryPhong = 1,
        MimicryClosest = 2
    }

    public enum InteractionMode
    {
        Drawing,
        Erasing
    }

    public enum ClosestPointMethod
    {
        Vanilla,
        Phong
    }


    public class StrokeMimicrySettings : MonoBehaviour
    {
        [Tooltip("Position of the pen tip in the local frame of the controller.")]
        public Vector3 PenTipPosition = new Vector3(0f, -0.01f, -0.02f);

        [Tooltip("Number of vertices per stroke point making up the cross-section of the cylindrical stroke mesh.")]
        public int MeshVerticesPerPoint = 6;

        [Tooltip("Thickness of the stroke mesh cross-section (in metres).")]
        public float MeshThickness = 0.0015f;

        public bool LogDebugInfo = false;

        [Tooltip("Path to the folder containgin the MATLAB-generated files required for Phong projection.")]
        public string PhongFilesPath = Application.streamingAssetsPath;

        void Awake()
        {
            var settingObjects = FindObjectsOfType<StrokeMimicrySettings>();
            if (settingObjects.Length > 1)
            {
                Debug.LogError("There must be only one stroke mimicry settings object!");
            }
        }
    }
}