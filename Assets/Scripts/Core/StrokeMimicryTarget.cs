using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StrokeMimicry
{
    // A model that can be targeted (drawn on) using Stroke Mimicry.
    public class StrokeMimicryTarget : MonoBehaviour
    {
        [Tooltip("Model name. This is interpreted as the shared prefix of the Phong projection files for this model.")]
        public string Name;

        [Tooltip("Does this model have an inside offset surface? Name_in.obj file must be present in the Phong Projection Folder (see StrokeMimicryManager).")]
        public bool LoadInsideOffsetSurface = false;

        // Phong projection object for this model.
        public PhongProjection Phong { get; private set; } = null;

        private Transform targetTransform = null;
        public Transform TargetTransform { get => targetTransform; }

        void Start()
        {
            if (Phong is null)
                Phong = new PhongProjection(Name, LoadInsideOffsetSurface);
            
            // find a mesh attached to this gameobject or to one of its descendents
            MeshFilter[] mfs = gameObject.GetComponentsInChildren<MeshFilter>();

            if (mfs.Length == 0)
            {
                Debug.LogError("No MeshFilter found! Unable to set a target surface.");
                return;
            }

            var mf = mfs[0];

            // transform of the GameObject actually containing the target mesh
            targetTransform = mf.transform;
            Projection.Target = this;
        }
    }
}
