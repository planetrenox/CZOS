using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SD
{

    public class PlayerController : MonoBehaviour
    {
     
        [Header("Player Setting")]
        public float turnSpeed = 10f;
        public float runSpeed = 3f;
        public bool stopMoverment = false;
        
        public bool moving { get; set; }
        
        float m_Horizontal, m_Vertical;
        private Vector3 m_MoveVector;
        private Rigidbody m_Rigidbody;

        [HideInInspector]
        public Animator m_Animator;
        private Quaternion m_Rotation = Quaternion.identity;
        private Transform camTrans;
        private Vector3 camForward;

        private Vector3 offset;  
        
        void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            m_Animator = GetComponent<Animator>();     
            camTrans = Camera.main.transform;     
        }
      

        void FixedUpdate()
        {
           
            //input 
            m_Horizontal = Input.GetAxis(Const.Horizontal);
            m_Vertical = Input.GetAxis(Const.Vetical);
 
            // move vector 
            if (camTrans != null)
            {
                camForward = Vector3.Scale(camTrans.forward, new Vector3(1, 0, 1).normalized);
                m_MoveVector = m_Vertical * camForward + m_Horizontal * camTrans.right;
                m_MoveVector.Normalize();
            }
            //animation    
            bool has_H_Input = !Mathf.Approximately(m_Horizontal, 0);
            bool has_V_Input = !Mathf.Approximately(m_Vertical, 0);

            if (!stopMoverment) moving = has_H_Input || has_V_Input;
            else moving = false;

            float inputSpeed = Mathf.Clamp01( Mathf.Abs(m_Horizontal) + Mathf.Abs(m_Vertical));

            m_Animator.SetBool(Const.Moving, moving);
            m_Animator.SetFloat(Const.Speed, inputSpeed);

            //move and rotate
            if (moving)
            {
                Vector3 desiredForward = Vector3.RotateTowards(transform.forward, m_MoveVector, turnSpeed * Time.deltaTime, 0f);
                m_Rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(desiredForward), turnSpeed);
                m_Rigidbody.MoveRotation(m_Rotation);
                m_Rigidbody.MovePosition(m_Rigidbody.position + inputSpeed*m_MoveVector * runSpeed * Time.deltaTime);
            }

        }   

    }
}

