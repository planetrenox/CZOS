using UnityEngine;
using Fragsurf.Movement;

namespace PR
{
    public class CarController : MonoBehaviour
    {
        private Rigidbody thisRigidbody;
        private Transform thisTransform;
        public static float speed;
        private AudioSource AudioSource_Car;
        private bool isInitialized = false;
        private static bool isReseting = false;
        private Vector3 startPosition;
        private Quaternion startRotation;

        private void Awake()
        {
            thisRigidbody = GetComponent<Rigidbody>();
            thisTransform = GetComponent<Transform>();
            AudioSource_Car = GetComponent<AudioSource>();
            startPosition = thisTransform.localPosition;
            startRotation = thisTransform.localRotation;


        }

        private void Start()
        {
            speed = 17f;
        }

        private void OnEnable()
        {
            if (!isInitialized)
            {
                isInitialized = true;
                return;
            }
            AudioSource_Car.Play();
            thisRigidbody.velocity = Vector3.zero;
            thisRigidbody.angularVelocity = Vector3.zero;
            thisTransform.localRotation = startRotation;
            

        }

        private void OnDisable()
        {
            AudioSource_Car.Stop();
            thisTransform.localPosition = startPosition;
        }

        private void FixedUpdate()
        {
            thisRigidbody.AddForce(thisTransform.right * speed);
        }

        
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer == 7) SurfCharacter.deathEvent();
        }
    }
}