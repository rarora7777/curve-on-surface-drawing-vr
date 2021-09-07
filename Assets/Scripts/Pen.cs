using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pen : MonoBehaviour
{
    [Tooltip("Position of the pen tip in the local frame of the controller.")]
    public Vector3 PenTipPosition = new Vector3(0f, -0.01f, -0.02f);

    public Vector3 SprayDirection = new Vector3(0f, -1f, 0f);

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
