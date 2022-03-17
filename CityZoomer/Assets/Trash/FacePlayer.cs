using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PR
{
    public class FacePlayer : MonoBehaviour
    {

        private Transform playerTransform;
        
        void Start()
        {
            playerTransform = GameObject.Find("PlayerController").GetComponent<Transform>();
        }

        // Update is called once per frame
        void Update()
        {
            transform.LookAt(playerTransform);
            transform.Rotate (0, 180, 0 );
        }
    }
}
