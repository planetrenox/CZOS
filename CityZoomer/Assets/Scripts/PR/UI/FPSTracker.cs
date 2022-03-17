using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PR
{
    public class FPSTracker : MonoBehaviour
    {
        private float framerateAvg;
        private static float framerate;

        void Update()
        {
            TrackFramerateAverage();
        }
        
        private void TrackFramerateAverage()
        {
            if (Mathf.Abs(Time.timeScale) <= 0) return;

            framerateAvg += (Time.deltaTime - framerateAvg) * 0.03f; //run this every frame
            framerate = (int)(1F / framerateAvg); //display this value
        }

        public static float GetFramerate()
        {
            return framerate;
        }
        
        
    }
}
