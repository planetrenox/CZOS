using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PR
{
    public class statictest : MonoBehaviour
    {
        public static int test = 0;
        
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
        
        
        void Update()
        {
            Debug.Log(test++);
        }
    }
}
