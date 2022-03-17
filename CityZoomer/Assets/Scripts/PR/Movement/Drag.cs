using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PR
{
    public class Drag : MonoBehaviour
    {
        float spring = 55.0f;
        float damper = 5.0f;
        float drag = 10.0f;
        float angularDrag = 5.0f;
        float distance = 0.0f;
        bool attachToCenterOfMass = false;
        private SpringJoint springJoint;
        private Camera sightCamera;
        private readonly float rangeClose = 4f;
        private readonly float rangeFar = 50f;



        private void Start()
        {
            sightCamera = GameObject.Find("CameraSight").GetComponent<Camera>();
        }

        void Update()
        {
            bool mouse0Down = Input.GetKeyDown(PR.UI.Button_Drag_READ);
            
            // Make sure the user pressed the mouse down
            if (mouse0Down)
            {
                // We need to actually hit an object
                RaycastHit hit;
                if (!Physics.Raycast(sightCamera.ScreenPointToRay(Input.mousePosition), out hit, rangeFar)) return;
                
                // if not tagged, return
                if (!hit.transform.CompareTag("DynamicMovable")) return;

                if (!springJoint)
                {
                    GameObject go = new GameObject("Rigidbody dragger");
                    Rigidbody body = go.AddComponent<Rigidbody>();
                    springJoint = go.AddComponent<SpringJoint>();
                    body.isKinematic = true;
                }

                springJoint.transform.position = hit.point;
                if (attachToCenterOfMass)
                {
                    Vector3 anchor = transform.TransformDirection(hit.rigidbody.centerOfMass) + hit.rigidbody.transform.position;
                    anchor = springJoint.transform.InverseTransformPoint(anchor);
                    springJoint.anchor = anchor;
                }
                else
                {
                    springJoint.anchor = Vector3.zero;
                }

                springJoint.spring = spring;
                springJoint.damper = damper;
                springJoint.maxDistance = distance;
                //springJoint.breakForce = 100;
                springJoint.connectedBody = hit.rigidbody;

                StartCoroutine(nameof(DragObject), hit.distance);
            }

        }

        private IEnumerator DragObject(float distance)
        {
            var connectedBody = springJoint.connectedBody;
            float oldDrag = connectedBody.drag;
            float oldAngularDrag = connectedBody.angularDrag;
            connectedBody.drag = drag;
            connectedBody.angularDrag = angularDrag;
            
            while (Input.GetKey(PR.UI.Button_Drag_READ) && springJoint)
            {
                Ray ray = sightCamera.ScreenPointToRay(Input.mousePosition);
                springJoint.transform.position = ray.GetPoint(distance);
                yield return 0;
            }

            if (springJoint && springJoint.connectedBody)
            {
                var body = springJoint.connectedBody;
                body.drag = oldDrag;
                body.angularDrag = oldAngularDrag;
                springJoint.connectedBody = null;
            }

        }
        
    }
}