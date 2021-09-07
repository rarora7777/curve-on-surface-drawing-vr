using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StrokeMimicry
{
    public class Target : MonoBehaviour
    {
        public string Name;
        public bool LoadInsideOffsetSurface = false;
        public PhongProjection Phong { get; private set; } = null;

        private Transform targetTransform = null;
        public Transform TargetTransform { get => targetTransform; }

        void Start()
        {
            if (Phong is null)
                Phong = new PhongProjection(Name, LoadInsideOffsetSurface);
            
            MeshFilter mf = GetTargetComponent<MeshFilter>();
            Projection.Target = this;
        }

        public T GetTargetComponent<T>() where T : Component
        {
            T component = GetComponent<T>();
            if (component == null || component.Equals(null))
            {
                T[] components = GetComponentsInChildren<T>();
                if (components.Length > 1)
                {
                    Debug.LogWarning("Model " + Name + " has zero or more than one components of type " + typeof(T).FullName + ".");
                }
                else if (components.Length == 1)
                {
                    component = components[0];
                }
                else
                {
                    Debug.LogError("The requested component of type " + typeof(T).FullName + " could not be found!");
                }
            }
            return component;
        }
    }
}
