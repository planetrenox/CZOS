using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MovementController : MonoBehaviour
{
    public Transform cameraTransform;
    public CharacterController characterController;

    public float speed;
    public float friction;
    public float jump;
    public float gravity;
    private float acceleration;
    public float weight;
    private Vector3 velocity;
    private Vector3 input;
    

    

    // Update is called once per frame
    void Update()
    {
        
            
            


        if (characterController.isGrounded)
        {
            input = cameraTransform.right * Input.GetAxisRaw("Horizontal") +
                    new Vector3(cameraTransform.forward.x, 0f,
                        cameraTransform.forward.z) * Input.GetAxisRaw("Vertical");
        
            input.Normalize();
            
            if (speed > acceleration && (Math.Abs(input.x) > 0 || Math.Abs(input.z) > 0))
            {
                acceleration += speed / weight;
            }
            else if (Math.Abs(input.x) == 0 || Math.Abs(input.z) == 0)
            {
                acceleration = 0;
            }

            velocity += (input * acceleration);
            
            // apply general friction
            velocity.x *= (1-friction);
            velocity.y = Physics.gravity.y * gravity * Time.deltaTime; 
            velocity.z *= (1-friction);
        }
        else
        {
            
            
            //velocity.x *= (1-(friction/2));
            velocity.y += Physics.gravity.y * gravity * Time.deltaTime;
            //velocity.z *= (1-(friction/2));
            
        }

        // jump
        if (Input.GetKeyDown(KeyCode.Space) && characterController.isGrounded)
        {
            velocity.y += jump;
        }


        //apply
        characterController.Move(velocity * Time.deltaTime);

        
    }
}