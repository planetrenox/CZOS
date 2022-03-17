using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PR
{
    public class TrainController : MonoBehaviour
    {
        private Transform thisTrainTransform;
        //private Rigidbody thisTrainRB;
        private float speed = 0.04f;

        private void Start()
        {
            thisTrainTransform = transform;
        }

        private void FixedUpdate()
        {
            var position = thisTrainTransform.position;
            thisTrainTransform.position = new Vector3(position.x + speed, position.y, position.z);
            
        }
    }
}
