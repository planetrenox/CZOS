using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class AnimLegFirstPersonNanaScript : MonoBehaviour
{
    [Header("Anim Character")] public float minYRotation = -20f;
    public float maxYRotation = 90f;

    [Header("Feet")] public float verticalFriction = 7f;
    public float horizontalFriction = 7f;
    public Transform pivotCenter;

    private const float horizontalMin = -0.9f;
    private const float horizontalMax = 2.0f;
    private const float verticalMin = -0.9f;
    private const float verticalMax = 0.9f;
    private float horizontalValue = 0.0f;
    private float verticalValue = 0.0f;
    private float xMovement;
    private Animator animatorComponent;
    private Vector3 realRotation;
    private bool leftDir, rightDir, forwardDir, backDir;
    private float deltaTime;
    private static readonly int HorAimAngle = Animator.StringToHash("HorAimAngle");
    private static readonly int InputMagnitude = Animator.StringToHash("InputMagnitude");
    private static readonly int Horizontal = Animator.StringToHash("Horizontal");
    private static readonly int Vertical = Animator.StringToHash("Vertical");
    private Vector3 pivotCenterLockedRotation = new Vector3(0.0f, 0.0f, 0.0f);
    private PlayerAiming playerAiming;

    private Transform cameraSightTransform;
    private Transform playerTransform;


    private void Start()
    {
        animatorComponent = GetComponent<Animator>();
        animatorComponent.SetFloat(InputMagnitude, 0.6f);
        cameraSightTransform = GameObject.Find("CameraSight").GetComponent<Transform>();
        playerTransform = GameObject.Find("PlayerController").GetComponent<Transform>();
        playerAiming = GameObject.Find("CameraSight").GetComponent<PlayerAiming>();
    }

    

    public void updateLegAnimations(float xMovement)
    {
        deltaTime = Time.deltaTime;
        

        // Calculate real rotation from input
        realRotation = new Vector3(realRotation.x, Mathf.Clamp(realRotation.y + xMovement, minYRotation, maxYRotation), realRotation.z);

        // rotate animBody around center pivot
        // something is missing - cant figure out why i need to reset its position

        //TODO maintain local vector3 instead of getting eulerAngles?

        if (Math.Abs(realRotation.y - maxYRotation) < 0.000000001)
            if (cameraSightTransform.eulerAngles.y < 95f && cameraSightTransform.eulerAngles.y > 90f) pivotCenter.eulerAngles = pivotCenterLockedRotation;
            else pivotCenter.Rotate(0, xMovement, 0);
        else if (Math.Abs(realRotation.y - minYRotation) < 0.000000001)
            if (cameraSightTransform.eulerAngles.y < 345f && cameraSightTransform.eulerAngles.y > 340f) pivotCenter.eulerAngles = pivotCenterLockedRotation;
            else pivotCenter.Rotate(0, xMovement, 0);


        // pivotCenterLockedRotation.y = pivotCenter.eulerAngles.y;
        // pivotCenter.eulerAngles = pivotCenterLockedRotation;


        // if ((int)cameraSightTransform.eulerAngles.y == 0)
        // {
        //  pivotCenter.eulerAngles = pivotCenterLockedRotation;
        //  realRotation = Vector3.zero;
        // }

        // apply upperbody anim
        animatorComponent.SetFloat(HorAimAngle, realRotation.y);

        //ebug.Log(cameraSightTransform.eulerAngles.y + " piv " +  pivotCenter.eulerAngles.y);

        //// feet
        

        var a = Input.GetKey(KeyCode.A);
        var d = Input.GetKey(KeyCode.D);
        var w = Input.GetKey(KeyCode.W);
        var s = Input.GetKey(KeyCode.S);
        
        if (PR.UI.Toggle_AutoStrafe_READ)
        {
            bool isJumping = Input.GetKey(PR.UI.Button_Jump_READ); 
            if (isJumping && xMovement < 0) a = true;
            else if (isJumping && xMovement > 0) d = true;
        }
        
        leftDir = a;
        rightDir = d;
        forwardDir = w;
        backDir = s;

        
        

        // LEFT SIDE
        if (realRotation.y < -20.0f && w)
        {
            leftDir = true;
            if (realRotation.y <= -70.0f)
            {
                forwardDir = false;
            }
        }

        if (realRotation.y < -20.0f && s)
        {
            rightDir = true;
            if (realRotation.y <= -70.0f)
            {
                backDir = false;
            }
        }

        if (realRotation.y <= -30.0f && a)
        {
            backDir = true;
            if (realRotation.y <= -55.0f)
            {
                leftDir = false;
            }
        }

        if (realRotation.y <= -35.0f && d)
        {
            forwardDir = true;
            if (realRotation.y <= -55.0f)
            {
                rightDir = false;
            }
        }


        // RIGHT SIDE
        if (realRotation.y >= 45.0f && w)
        {
            rightDir = true;
            if (realRotation.y >= 70.0f)
            {
                forwardDir = false;
            }
        }

        if (realRotation.y >= 20.0f && s)
        {
            leftDir = true;
            if (realRotation.y >= 70.0f)
            {
                backDir = false;
            }
        }

        if (realRotation.y >= 20.0f && a)
        {
            forwardDir = true;
            if (realRotation.y >= 70.0f)
            {
                leftDir = false;
            }
        }

        if (realRotation.y >= 20.0f && d)
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
                horizontalValue -= (deltaTime * 550);
            }
            else
            {
                horizontalValue -= (deltaTime * horizontalFriction);
            }

            horizontalValue = Mathf.Clamp(horizontalValue, horizontalMin, horizontalMax);
            animatorComponent.SetFloat(Horizontal, horizontalValue);
        }

        if (rightDir)
        {
            if (horizontalValue < -0.75f) // switch direction while full strafe
            {
                horizontalValue += (deltaTime * 100);
            }
            else
            {
                horizontalValue += (deltaTime * horizontalFriction);
            }

            horizontalValue = Mathf.Clamp(horizontalValue, horizontalMin, horizontalMax);
            animatorComponent.SetFloat(Horizontal, horizontalValue);
        }

        if (!((leftDir || rightDir))) // return to center
        {
            if (horizontalValue > 0.05f)
            {
                horizontalValue -= (deltaTime * horizontalFriction * 2);
                animatorComponent.SetFloat(Horizontal, horizontalValue);
            }
            else if (horizontalValue < -0.05f)
            {
                horizontalValue += (deltaTime * horizontalFriction);
                animatorComponent.SetFloat(Horizontal, horizontalValue);
            }
            else
            {
                horizontalValue = 0.0f;
                animatorComponent.SetFloat(Horizontal, 0.0f);
            }
        }

        //VERT
        if (forwardDir)
        {
            verticalValue += (deltaTime * verticalFriction);
            verticalValue = Mathf.Clamp(verticalValue, verticalMin, verticalMax);
            animatorComponent.SetFloat(Vertical, verticalValue);
        }

        if (backDir)
        {
            verticalValue -= (deltaTime * verticalFriction);
            verticalValue = Mathf.Clamp(verticalValue, verticalMin, verticalMax);
            animatorComponent.SetFloat(Vertical, verticalValue);
        }

        if (!((forwardDir || backDir))) // return to center
        {
            if (verticalValue > 0.05f)
            {
                verticalValue -= (deltaTime * verticalFriction);
                animatorComponent.SetFloat(Vertical, verticalValue);
            }
            else if (horizontalValue < -0.05f)
            {
                verticalValue += (deltaTime * verticalFriction);
                animatorComponent.SetFloat(Vertical, verticalValue);
            }
            else
            {
                verticalValue = 0.0f;
                animatorComponent.SetFloat(Vertical, 0.0f);
            }
        }
    }
}