using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ads : MonoBehaviour
{
    public Transform bodyCameraTransform;
    public Transform sightCameraTransform;
    
    public Transform playerTransform;
    
    public Transform bodyTransform;
    
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        if(Input.GetKey(KeyCode.Mouse1))
        {
            // zoom camera out
            if (bodyCameraTransform.localPosition.z > -10)
                bodyCameraTransform.localPosition -= playerTransform.forward * Time.deltaTime * 40;
            
        }
        else
        {
            // reset pos
            bodyCameraTransform.localPosition = Vector3.zero;
        }

        // lock x axis rotation relative to sight camera
        //bodyTransform.rotation = Quaternion.Euler(0, sightCameraTransform.rotation.eulerAngles.y, sightCameraTransform.rotation.eulerAngles.z);

    }
}
