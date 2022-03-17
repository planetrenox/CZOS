using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PR
{
    public class MenuSky : MonoBehaviour
    {
        
        private float skyboxRotationSpeed = 0.4f;
        private static readonly int Rotation = Shader.PropertyToID("_Rotation");

        // Update is called once per frame
        void Update()
        {
            
            RenderSettings.skybox.SetFloat(Rotation, Time.time * skyboxRotationSpeed);
        
        }
    }
}
