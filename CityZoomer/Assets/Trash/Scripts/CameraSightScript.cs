using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSightScript : MonoBehaviour
{
    public Camera sightCamera;
    
    [Header("Sight Camera Settings")]
    public float FOV; // 106.26
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    void Awake()
    {
        sightCamera.fieldOfView = FOV;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
