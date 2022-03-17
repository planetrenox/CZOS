using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class GhostBehaviour : MonoBehaviour
{
    [Header("Stats")] 
    public int ghostHealth = 100;
    
    [Header("Movement")]
    public GameObject playerGameObject;
    public GameObject ghostGameObject;
    public float ghostSpeed = 30.0f;
    public float ghostConstantUpHoverForce = 6.0f;
    public float ghostConstantUpHoverForceMultiplier = 1.5f;
    public float ghostAttackImpulseSpeed = 1.0f;
    public float ghostAttackImpulseDistanceMinTrigger;
    public float ghostAttackImpulseDistanceMaxTrigger;
    [Header("Ghost Impulse Movement")]
    public bool enableGhostUpImpulse = false;
    public float ghostUpImpulseSpeed = 1.0f;
    public float ghostUpImpulseDistanceTrigger = 20.0f;
    
    

    private Transform playerTransform;
    private Transform ghostTransform;
    private Rigidbody ghostRigidbody;
    
    private Vector3 playerTransformPosition;
    private Vector3 ghostTransformPosition;
    private Vector3 distanceToPlayer;
    private Vector3 distanceToPlayerFlat;
    private float distanceToPlayerFlatMagnitude;


    // Use this for initialization
    private void Start()
    {
        playerTransform = playerGameObject.transform;
        ghostTransform = ghostGameObject.transform;
        ghostRigidbody = ghostGameObject.GetComponent<Rigidbody>();
    }

    // Update is called once per physics frame
    private  void FixedUpdate()
    {
        playerTransformPosition = playerTransform.position;
        ghostTransformPosition = ghostTransform.position;
        distanceToPlayer = playerTransformPosition - ghostTransformPosition;
        distanceToPlayerFlat = new Vector3(distanceToPlayer.x, 0, distanceToPlayer.y);
        distanceToPlayerFlatMagnitude = distanceToPlayerFlat.magnitude;

        // follow player
        ghostRigidbody.AddForce(distanceToPlayer.normalized * ghostSpeed);
        ghostTransform.LookAt(playerTransformPosition);
        
        // constant hover based on distance
        ghostRigidbody.AddForce(ghostTransform.up * Mathf.Lerp(distanceToPlayerFlatMagnitude, ghostConstantUpHoverForce, Time.fixedDeltaTime * ghostConstantUpHoverForceMultiplier));

        if (ghostAttackImpulseDistanceMinTrigger < distanceToPlayer.magnitude && distanceToPlayer.magnitude < ghostAttackImpulseDistanceMaxTrigger)
        {
            ghostRigidbody.AddForce(ghostTransform.forward * ghostAttackImpulseSpeed, ForceMode.Impulse);
        }
        
        // if far away, impulse fly
        if (enableGhostUpImpulse && distanceToPlayerFlatMagnitude > ghostUpImpulseDistanceTrigger)
        {
            ghostRigidbody.AddForce(ghostTransform.up * ghostUpImpulseSpeed, ForceMode.Impulse);
        }
    }
    
    // Ray .SendMessage
    void ApplyDamage(int incomingDamage)
    {
        ghostHealth -= incomingDamage;
        if (ghostHealth <= 0)
        {
            ghostGameObject.SetActive(false);
        }

    }
    
}