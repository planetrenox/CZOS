using UnityEngine;
namespace Fragsurf.Movement {

    [System.Serializable]
    public class MovementConfig {

        [Header ("Jumping and gravity")]
        public bool autoBhop = true;
        public static float gravity = 20f;
        public static float jumpForce = 6.5f;
        
        [Header ("General physics")]
        public float friction = 5f;
        public float maxSpeed = 6f;
        public static float maxVelocity = 55f;
        [Range (10f, 75f)] public float slopeLimit = 45f;

        [Header ("Air movement")]
        public bool clampAirSpeed = false;
        public float airCap = 2f;
        public static float airAcceleration = 22f;
        public float airFriction = 0.4f;

        [Header ("Ground movement")]
        public float walkSpeed = 7f;
        public float sprintSpeed = 12f;
        public float acceleration = 14f;
        public float deceleration = 10f;

        [Header ("Crouch movement")]
        public float crouchSpeed = 4f;
        public float crouchAcceleration = 8f;
        public float crouchDeceleration = 4f;
        public float crouchFriction = 3f;

        [Header ("Sliding")]
        public float minimumSlideSpeed = 11f;
        public float maximumSlideSpeed = 19f;
        public float slideSpeedMultiplier = 1.75f;
        public float slideFriction = 14f;
        public float downhillSlideSpeedMultiplier = 2.5f;
        public float slideDelay = 0.5f;

        [Header ("Underwater")]
        public float swimUpSpeed = 12f;
        public float underwaterSwimSpeed = 3f;
        public float underwaterAcceleration = 6f;
        public float underwaterDeceleration = 3f;
        public float underwaterFriction = 2f;
        public float underwaterGravity = 6f;
        public float underwaterVelocityDampening = 2f;
        
    }

}
