using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimScripts : MonoBehaviour
{
	
	private float minYRotation = -90f;
	private float maxYRotation = 90f;
	
	private float minXRotation = -180f;
	//private float maxXRotation = 180f;
	
	
	private float horizontalMin = -0.9f, horizontalMax = 2.0f;
	private float verticalMin = -0.9f, verticalMax = 0.9f;
	private float horizontalValue = 0.0f;
	private float verticalValue = 0.0f;
	[Header("Feet")]
	public float verticalFriction = 7f;
	public float horizontalFriction = 7f;
	private float xMovement;
	private float yMovement;
	
	private Animator animatorComponent;
	private Vector3 realRotation;
	[Header("Animated Player")]
	public Transform playerTransform;

	
	// Start is called before the first frame update
    void Start()
    {
        animatorComponent = GetComponent<Animator>();
        animatorComponent.SetFloat("InputMagnitude", 0.6f);
    }

    // Update is called once per frame
    void Update()
    {
	    
	    //// aim & rotation
	    
	    // Input
	    xMovement = Input.GetAxisRaw("Mouse X");
	    yMovement = -Input.GetAxisRaw("Mouse Y");
	    Debug.Log(new Vector3(Mathf.Clamp(realRotation.x + yMovement, minYRotation, maxYRotation), realRotation.y + xMovement, realRotation.z));
	    // Calculate real rotation from input
	    realRotation  = new Vector3(Mathf.Clamp(realRotation.x + yMovement, minYRotation, maxYRotation), realRotation.y + xMovement, realRotation.z);
	    
	    if (realRotation.y >= 89.0f)
	    {
		    // rotate character by 40
		    playerTransform.Rotate(0, 1.0f, 0);
		    
		    // subtract feet horizvalue by 40
		    realRotation.y = realRotation.y - 1.0f;
	    }
	    else if (realRotation.y <= -89.0f)
	    {
		    // rotate character by 40
		    playerTransform.Rotate(0, -1.0f, 0);
		    
		    // subtract feet horizvalue by 40
		    realRotation.y = realRotation.y + 1.0f;
	    }
	    
	    
	    //// feet

	    bool leftDir;
	    bool rightDir;
	    bool forwardDir;
	    bool backDir;

	    leftDir = Input.GetKey(KeyCode.A);
	    rightDir = Input.GetKey(KeyCode.D);
	    forwardDir = Input.GetKey(KeyCode.W);
	    backDir = Input.GetKey(KeyCode.S);

	    // LEFT SIDE
	    if (realRotation.y <= -20.0f && Input.GetKey(KeyCode.W))
	    {
		    leftDir = true;
		    if (realRotation.y <= -70.0f)
		    {
			    forwardDir = false;
		    }
	    }
	    if (realRotation.y <= -20.0f && Input.GetKey(KeyCode.S))
	    {
		    rightDir = true;
		    if (realRotation.y <= -70.0f)
		    {
			    backDir = false;
		    }
	    }
	    if (realRotation.y <= -30.0f && Input.GetKey(KeyCode.A))
	    {
		    backDir = true;
		    if (realRotation.y <= -55.0f)
		    {
			    leftDir = false;
		    }
	    }
	    if (realRotation.y <= -35.0f && Input.GetKey(KeyCode.D))
	    {
		    forwardDir = true;
		    if (realRotation.y <= -55.0f)
		    {
			    rightDir = false;
		    }
	    }
	    

	    // RIGHT SIDE
	    if (realRotation.y >= 45.0f && Input.GetKey(KeyCode.W))
	    {
		    rightDir = true;
		    if (realRotation.y >= 70.0f)
		    {
			    forwardDir = false;
		    }
	    }
	    if (realRotation.y >= 20.0f && Input.GetKey(KeyCode.S))
	    {
		    leftDir = true;
		    if (realRotation.y >= 70.0f)
		    {
			    backDir = false;
		    }
	    }
	    if (realRotation.y >= 20.0f && Input.GetKey(KeyCode.A))
	    {
		    forwardDir = true;
		    if (realRotation.y >= 70.0f)
		    {
			    leftDir = false;
		    }
	    }
	    if (realRotation.y >= 20.0f && Input.GetKey(KeyCode.D))
	    {
		    backDir = true;
		    if (realRotation.y >= 70.0f)
		    {
			    rightDir = false;
		    }
	    }
	    
	    
	    
	     
	    if (leftDir)
	    {
		    if (horizontalValue > 1.85f) // switch direction while full strafe
		    {
			    horizontalValue = horizontalValue - (Time.deltaTime*550);
		    }
		    else
		    {
			    horizontalValue = horizontalValue - (Time.deltaTime*horizontalFriction);
		    }
		    horizontalValue = Mathf.Clamp(horizontalValue, horizontalMin, horizontalMax);
		    animatorComponent.SetFloat("Horizontal", horizontalValue);
		}
	    if (rightDir)
	    {

		    if (horizontalValue < -0.75f) // switch direction while full strafe
		    {
			    horizontalValue = horizontalValue + (Time.deltaTime*100);
		    }
		    else
		    {
			    horizontalValue = horizontalValue + (Time.deltaTime*horizontalFriction);
		    }
		    horizontalValue = Mathf.Clamp(horizontalValue, horizontalMin, horizontalMax);
		    animatorComponent.SetFloat("Horizontal", horizontalValue);
	    }
	    if (!((leftDir || rightDir))) // return to center
	    {
		    if (horizontalValue > 0.05f)
		    {
			    horizontalValue = horizontalValue - (Time.deltaTime*horizontalFriction*2);
			    animatorComponent.SetFloat("Horizontal", horizontalValue);
		    }
		    else if (horizontalValue < -0.05f)
		    {
			    horizontalValue = horizontalValue + (Time.deltaTime*horizontalFriction);
			    animatorComponent.SetFloat("Horizontal", horizontalValue);
		    }
		    else
		    {
			    horizontalValue = 0.0f;
			    animatorComponent.SetFloat("Horizontal", 0.0f);
		    }
		    
	    }
	    //VERT
	    if (forwardDir)
	    {

		    verticalValue = verticalValue + (Time.deltaTime*verticalFriction);
		    verticalValue = Mathf.Clamp(verticalValue, verticalMin, verticalMax);
		    animatorComponent.SetFloat("Vertical", verticalValue);
	    }
	    if (backDir)
	    {
		    verticalValue = verticalValue - (Time.deltaTime*verticalFriction);
		    verticalValue = Mathf.Clamp(verticalValue, verticalMin, verticalMax);
		    animatorComponent.SetFloat("Vertical", verticalValue);
	    }
	    if (!((forwardDir || backDir))) // return to center
	    {
		    if (verticalValue > 0.05f)
		    {
			    verticalValue = verticalValue - (Time.deltaTime*verticalFriction);
			    animatorComponent.SetFloat("Vertical", verticalValue);
		    }
		    else if (horizontalValue < -0.05f)
		    {
			    verticalValue = verticalValue + (Time.deltaTime*verticalFriction);
			    animatorComponent.SetFloat("Vertical", verticalValue);
		    }
		    else
		    {
			    verticalValue = 0.0f;
			    animatorComponent.SetFloat("Vertical", 0.0f);
		    }
		    
	    }
	    

	    
	    //apply upper body rotation
	    animatorComponent.SetFloat("VerAimAngle", -realRotation.x);
	    animatorComponent.SetFloat("HorAimAngle", realRotation.y);
    }
}
