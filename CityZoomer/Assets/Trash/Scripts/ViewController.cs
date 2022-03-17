using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewController : MonoBehaviour
{
    public float sensitivity = 1;
    public Transform cameraTransform;


    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 mouseLocationVector2 = new Vector2(Input.GetAxisRaw("Mouse X"),
            Input.GetAxisRaw("Mouse Y")) * sensitivity;

        cameraTransform.Rotate(-mouseLocationVector2.y, 0, 0f, Space.Self);
        cameraTransform.Rotate(0, mouseLocationVector2.x, 0f, Space.World);
        
        
    }
}