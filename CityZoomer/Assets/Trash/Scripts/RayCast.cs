using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PR
{
    public class RayCast : MonoBehaviour
    {
        // idk
        
        
        //public Camera sightCamera;
        /*private void raycastTrace()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = sightCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                // Find the line from the gun to the point that was clicked.
                Vector3 incomingVec = hit.point - weaponTransform.position;

                // Use the point's normal to calculate the reflection vector.
                Vector3 reflectVec = Vector3.Reflect(incomingVec, hit.normal);

                // Draw lines to show the incoming "beam" and the reflection.
                Debug.DrawLine(weaponTransform.position, hit.point, Color.red, 3);
                
                RaycastHit hit2;
                Ray ray2 = new Ray(hit.point,reflectVec);

                if (Physics.Raycast(ray2, out hit2))
                {
                    Vector3 reflectVec2 = Vector3.Reflect(reflectVec, hit2.normal);
                    Debug.DrawLine(hit.point, hit2.point, Color.yellow, 3);

                    RaycastHit hit3;
                    Ray ray3 = new Ray(hit2.point,reflectVec2);
                    if (Physics.Raycast(ray3, out hit3))
                    {
                        Vector3 reflectVec3 = Vector3.Reflect(reflectVec2, hit3.normal);
                        Debug.DrawLine(hit2.point, hit3.point, Color.white, 3);
                    }
                    else
                    {
                        Debug.DrawRay(hit2.point, reflectVec2, Color.white, 3, true);
                    }
                }
                else
                {
                    Debug.DrawRay(hit.point, reflectVec, Color.yellow, 3, true);
                }
                
            }
        }
    }*/
        
        // Start is called before the first frame update
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
