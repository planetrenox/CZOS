using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using PR;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace Fragsurf.Movement
{
    [AddComponentMenu("Fragsurf/Surf Character")]
    public class SurfCharacter : MonoBehaviour, ISurfControllable
    {
        public enum ColliderType
        {
            Capsule,
            Box
        }


        public static bool god = false;

        ///// Fields /////

        [Header("Physics Settings")] public Vector3 colliderSize = new Vector3(1f, 2f, 1f);

        [HideInInspector]
        public ColliderType collisionType
        {
            get { return ColliderType.Box; }
        } // Capsule doesn't work anymore; I'll have to figure out why some other time, sorry.

        public float weight = 75f;
        public float rigidbodyPushForce = 2f;
        public bool solidCollider = false;

        private GameObject quad;
        

        [Header("View Settings")] public Transform viewTransform;
        public Transform playerRotationTransform;

        [Header("Crouching setup")] public float crouchingHeightMultiplier = 0.5f;
        public float crouchingSpeed = 10f;
        float defaultHeight;

        bool
            allowCrouch =
                true; // This is separate because you shouldn't be able to toggle crouching on and off during gameplay for various reasons

        [Header("Features")] public bool crouchingEnabled = true;
        public bool slidingEnabled = false;
        public bool laddersEnabled = true;
        public bool supportAngledLadders = true;

        [Header("Step offset (can be buggy, enable at your own risk)")]
        public bool useStepOffset = false;

        public float stepOffset = 0.35f;

        [Header("Movement Config")] [SerializeField]
        public MovementConfig movementConfig;


        private GameObject _groundObject;
        private Vector3 _baseVelocity;
        private Collider _collider;
        private Vector3 _angles;
        private Vector3 _startPosition;
        private GameObject _colliderObject;
        private GameObject _cameraWaterCheckObject;
        private CameraWaterCheck _cameraWaterCheck;

        private MoveData _moveData = new MoveData();
        private SurfController _controller = new SurfController();

        private Rigidbody rb;

        private List<Collider> triggers = new List<Collider>();
        private int numberOfTriggers = 0;

        private bool underwater = false;

        ///// City Stuff /////

        private GameObject city1, city2;

        private Transform city1Transform, city2Transform;

        // audio
        public static AudioSource footstepsAudioSource;
        public AudioClip[] footstepAudioList;
        public static AudioClip[] footstepAudioArrayStatic;

        // score
        private static float distanceProgress;
        public static float bestDistanceThisRun = 0;
        private Transform nextCityTransform;
        private float prevCityCachedProgress = 0;
        private float maxDistanceToNextCity;
        private bool patCompleted = false;
        private Transform playerTransform;
        private GameObject cameraStats;
        private GameObject panelDistance, panelTime;
        private GameObject city2blocksign1b, city2blocksign1a;
        private bool isOver5k;


        public static float timeAt5KHalf;


        ///// Properties /////

        public MoveType moveType
        {
            get { return MoveType.Walk; }
        }

        public MovementConfig moveConfig
        {
            get { return movementConfig; }
        }

        public MoveData moveData
        {
            get { return _moveData; }
        }

        public new Collider collider
        {
            get { return _collider; }
        }

        public GameObject groundObject
        {
            get { return _groundObject; }
            set { _groundObject = value; }
        }

        public Vector3 baseVelocity
        {
            get { return _baseVelocity; }
        }

        public Vector3 forward
        {
            get { return viewTransform.forward; }
        }

        public Vector3 right
        {
            get { return viewTransform.right; }
        }

        public Vector3 up
        {
            get { return viewTransform.up; }
        }

        Vector3 prevPosition;


        ///// Methods /////

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, colliderSize);
        }


        private void Awake()
        {
            // city stuff
            city1 = GameObject.Find("City1");
            city2 = GameObject.Find("City2");
            city2blocksign1b = GameObject.Find("city2blocksign1b");
            city2blocksign1a = GameObject.Find("city2blocksign1a");
            

            panelDistance = GameObject.Find("PanelDistance");
            panelTime = GameObject.Find("PanelTime");


            bestDistanceThisRun = 0f;
            distanceProgress = 0f;
            prevCityCachedProgress = 0;
            maxDistanceToNextCity = 0;
            isOver5k = false;
            timeAt5KHalf = 0;
            quad = GameObject.Find("StatsQuad");
            cameraStats = GameObject.Find("CameraStats");


            _controller.playerTransform = playerRotationTransform;
            if (viewTransform != null)
            {
                _controller.camera = viewTransform;
                _controller.cameraYPos = viewTransform.localPosition.y;
            }
        }

        private PlayerAiming playerAiming;

        private void Start()
        {
            playerTransform = transform;
            playerAiming = GameObject.Find("CameraSight").GetComponent<PlayerAiming>();
            Steamworks.SteamUserStats.GetAchievement("headpat", out patCompleted);


            #region fragsurf

            _colliderObject = new GameObject("PlayerCollider");
            _colliderObject.layer = gameObject.layer;
            _colliderObject.transform.SetParent(transform);
            _colliderObject.transform.rotation = Quaternion.identity;
            _colliderObject.transform.localPosition = Vector3.zero;
            _colliderObject.transform.SetSiblingIndex(0);

            // Water check
            _cameraWaterCheckObject = new GameObject("Camera water check");
            _cameraWaterCheckObject.layer = gameObject.layer;
            _cameraWaterCheckObject.transform.position = viewTransform.position;

            SphereCollider _cameraWaterCheckSphere = _cameraWaterCheckObject.AddComponent<SphereCollider>();
            _cameraWaterCheckSphere.radius = 0.1f;
            _cameraWaterCheckSphere.isTrigger = true;

            Rigidbody _cameraWaterCheckRb = _cameraWaterCheckObject.AddComponent<Rigidbody>();
            _cameraWaterCheckRb.useGravity = false;
            _cameraWaterCheckRb.isKinematic = true;

            _cameraWaterCheck = _cameraWaterCheckObject.AddComponent<CameraWaterCheck>();

            prevPosition = transform.position;

            if (viewTransform == null) viewTransform = Camera.main.transform;

            if (playerRotationTransform == null && transform.childCount > 0) playerRotationTransform = transform.GetChild(0);

            _collider = gameObject.GetComponent<Collider>();

            if (_collider != null) GameObject.Destroy(_collider);

            // rigidbody is required to collide with triggers
            rb = gameObject.GetComponent<Rigidbody>();


            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

            allowCrouch = crouchingEnabled;

            rb.isKinematic = true;
            rb.useGravity = false;
            rb.angularDrag = 0f;
            rb.drag = 0f;
            rb.mass = weight;


            switch (collisionType)
            {
                // Box collider
                case ColliderType.Box:

                    _collider = _colliderObject.AddComponent<BoxCollider>();
                    var boxc = (BoxCollider) _collider;


                    boxc.size = colliderSize;

                    defaultHeight = boxc.size.y;

                    break;

                // Capsule collider
                case ColliderType.Capsule:

                    _collider = _colliderObject.AddComponent<CapsuleCollider>();

                    var capc = (CapsuleCollider) _collider;
                    capc.height = colliderSize.y;
                    capc.radius = colliderSize.x / 2f;

                    defaultHeight = capc.height;

                    break;
            }

            _moveData.slopeLimit = movementConfig.slopeLimit;

            _moveData.rigidbodyPushForce = rigidbodyPushForce;

            _moveData.slidingEnabled = slidingEnabled;
            _moveData.laddersEnabled = laddersEnabled;
            _moveData.angledLaddersEnabled = supportAngledLadders;

            _moveData.playerTransform = transform;
            _moveData.viewTransform = viewTransform;
            _moveData.viewTransformDefaultLocalPos = viewTransform.localPosition;

            _moveData.defaultHeight = defaultHeight;
            _moveData.crouchingHeight = crouchingHeightMultiplier;
            _moveData.crouchingSpeed = crouchingSpeed;

            _collider.isTrigger = !solidCollider;
            _moveData.origin = transform.position;
            _startPosition = transform.position;

            _moveData.useStepOffset = useStepOffset;
            _moveData.stepOffset = stepOffset;

            #endregion

            // City stuff

            city1Transform = city1.transform;
            city2Transform = city2.transform;
            


            // audio
            footstepsAudioSource = GameObject.Find("AudioSource_Footsteps").GetComponent<AudioSource>();
            footstepAudioArrayStatic = footstepAudioList;
            footstepAudioList = null; //clear memory


            // score
            maxDistanceToNextCity = Vector3.Distance(city2Transform.position, playerTransform.position);
            nextCityTransform = city2Transform;

            panelTime.SetActive(false);
            
            
            
        }

        private float prevVerticalVel;

        private void Update()
        {
            //AIM PUNCH ON FALL DAMAGE
            if (prevVerticalVel < _moveData.velocity.y)
            {
                if (prevVerticalVel < -26) playerAiming.ViewPunch(Input.GetAxis("Mouse X") < 0 ? new Vector2(-3f, -2f) : new Vector2(-3f, 2f));
                else if (prevVerticalVel < -22) playerAiming.ViewPunch(Input.GetAxis("Mouse X") < 0 ? new Vector2(-2f, -1.5f) : new Vector2(-2f, 1.5f));
                else if (prevVerticalVel < -14.5) playerAiming.ViewPunch(Input.GetAxis("Mouse X") < 0 ? new Vector2(-1f, -1f) : new Vector2(-1f, 1f));
            }

            prevVerticalVel = _moveData.velocity.y;


            #region fragsurf

            // movement
            _colliderObject.transform.rotation = Quaternion.identity;


            //UpdateTestBinds ();
            UpdateMoveData();

            // Previous movement code
            Vector3 positionalMovement = transform.position - prevPosition;
            playerTransform.position = prevPosition;
            moveData.origin += positionalMovement;

            // Triggers
            if (numberOfTriggers != triggers.Count)
            {
                numberOfTriggers = triggers.Count;

                underwater = false;
                triggers.RemoveAll(item => item == null);
                foreach (Collider trigger in triggers)
                {
                    if (trigger == null) continue;

                    if (trigger.GetComponentInParent<Water>()) underwater = true;
                }
            }

            _moveData.cameraUnderwater = _cameraWaterCheck.IsUnderwater();
            _cameraWaterCheckObject.transform.position = viewTransform.position;
            moveData.underwater = underwater;

            if (allowCrouch) _controller.Crouch(this, movementConfig, Time.deltaTime);

            _controller.ProcessMovement(this, movementConfig, Time.deltaTime);

            transform.position = moveData.origin;
            prevPosition = playerTransform.position;

            _colliderObject.transform.rotation = Quaternion.identity;

            #endregion
        }


        public void distanceTrack()
        {
            // score
            var distanceToNextCityCurr = Vector3.Distance(nextCityTransform.position, playerTransform.position);
            distanceProgress = (maxDistanceToNextCity - distanceToNextCityCurr) + prevCityCachedProgress;
            if (distanceProgress > bestDistanceThisRun) bestDistanceThisRun = distanceProgress;

            if (isOver5k == false && bestDistanceThisRun >= 5000)
            {
                timeAt5KHalf = Time.timeSinceLevelLoad / 2;
                isOver5k = true;
            }
        }

        private void UpdateTestBinds()
        {
            if (Input.GetKeyDown(KeyCode.Backspace)) ResetPosition();
        }

        private void ResetPosition()
        {
            moveData.velocity = Vector3.zero;
            moveData.origin = _startPosition;
        }

        private readonly float localMaxFloat = 99999f;
        private const float localMaxFloatStatic = 99999f;
        public static void deathEvent()
        {
            if (PR.UI.isInLevelTest || god) return;
            if (PR.Command.s_sandbox || Application.isEditor) PR.UI.RestartCurrentScene();
            var timeAt5k = timeAt5KHalf * 2;
            var currentWeek = UIStats.getCurrentWeek();
            var storeStats = false;


            Steamworks.SteamUserStats.GetStat("dist_1", out float currBestDistanceAllTime);
            Steamworks.SteamUserStats.GetStat("currweek", out int lastWeekPlayed);
            Steamworks.SteamUserStats.GetStat("distweek", out float lastWeekPlayedBestDistance);
            Steamworks.SteamUserStats.GetStat("currweek5ktime", out float lastWeek5kTime);

            if (lastWeek5kTime < 1) lastWeek5kTime = localMaxFloatStatic;
            if (timeAt5k < 1) timeAt5k = localMaxFloatStatic;


            if (currentWeek != lastWeekPlayed) // if this is the first time the user is playing since last reset
            {
                Steamworks.SteamUserStats.SetStat("currweek", currentWeek);
                Steamworks.SteamUserStats.SetStat("distweek", bestDistanceThisRun);
                Steamworks.SteamUserStats.SetStat("currweek5ktime", localMaxFloatStatic);
                lastWeekPlayedBestDistance = 0;
                lastWeek5kTime = localMaxFloatStatic;
                storeStats = true;
            }

            if (bestDistanceThisRun >= 5000 && lastWeek5kTime > timeAt5k) //5k time leader for this week
            {
                Steamworks.SteamUserStats.SetStat("currweek5ktime", timeAt5k);
                UIStats.UpdateScore5K(timeAt5k);
                storeStats = true;
            }

            if (lastWeekPlayedBestDistance < bestDistanceThisRun) // new high distance for this week
            {
                Steamworks.SteamUserStats.SetStat("distweek", bestDistanceThisRun);
                UIStats.UpdateScoreWeeklyDistance((int) bestDistanceThisRun, new[] {(int) Time.timeSinceLevelLoad, 4});
                storeStats = true;
            }

            if (currBestDistanceAllTime < bestDistanceThisRun) // if beat alltime
            {
                Steamworks.SteamUserStats.SetStat("dist_1", bestDistanceThisRun);
                UIStats.UpdateScoreAllTime((int) bestDistanceThisRun, new[] {(int) Time.timeSinceLevelLoad, 4});
                storeStats = true; 
            }


            if (storeStats) Steamworks.SteamUserStats.StoreStats(); // upload scores
            
            PR.UI.RestartCurrentScene();
        }

        private void UpdateMoveData()
        {
            _moveData.verticalAxis = Input.GetAxisRaw("Vertical");
            _moveData.horizontalAxis = Input.GetAxisRaw("Horizontal");


            _moveData.sprinting = Input.GetButton("SourceSprint");

            if (Input.GetKeyDown(PR.UI.Button_Crouch_READ)) _moveData.crouching = true;

            if (!Input.GetKey(PR.UI.Button_Crouch_READ)) _moveData.crouching = false;

            bool moveLeft = _moveData.horizontalAxis < 0f;
            bool moveRight = _moveData.horizontalAxis > 0f;
            bool moveFwd = _moveData.verticalAxis > 0f;
            bool moveBack = _moveData.verticalAxis < 0f;
            //bool jump = Input.GetButton("SourceJump");


            if (PR.UI.Toggle_AutoStrafe_READ)
            {
                float xmovement = playerAiming.xMovement;
                moveRight = xmovement > 0;
                moveLeft = xmovement < 0;
                if (Input.GetKey(PR.UI.Button_Jump_READ)) moveFwd = false;
            }

            if (!moveLeft && !moveRight) _moveData.sideMove = 0f;
            else if (moveLeft) _moveData.sideMove = -moveConfig.acceleration;
            else if (moveRight) _moveData.sideMove = moveConfig.acceleration;

            if (!moveFwd && !moveBack) _moveData.forwardMove = 0f;
            else if (moveFwd) _moveData.forwardMove = moveConfig.acceleration;
            else if (moveBack) _moveData.forwardMove = -moveConfig.acceleration;

            if (Input.GetKeyDown(PR.UI.Button_Jump_READ)) _moveData.wishJump = true;

            if (!Input.GetKey(PR.UI.Button_Jump_READ)) _moveData.wishJump = false;

            _moveData.viewAngles = _angles;
        }

        private void DisableInput()
        {
            _moveData.verticalAxis = 0f;
            _moveData.horizontalAxis = 0f;
            _moveData.sideMove = 0f;
            _moveData.forwardMove = 0f;
            _moveData.wishJump = false;
        }


        public static float ClampAngle(float angle, float from, float to)
        {
            if (angle < 0f) angle = 360 + angle;

            if (angle > 180f) return Mathf.Max(angle, 360 + from);

            return Mathf.Min(angle, to);
        }

        private bool isFirstTimeTrigger = true;

        public void city1Trigger()
        {
            CarController.speed+=3;
            

            Vector3 city1TransformPosition = city1Transform.position;
            city2Transform.position = new Vector3(city1TransformPosition.x - 440.9f, city2Transform.position.y, city1TransformPosition.z + 136.25f);
            city2Transform.eulerAngles = new Vector3(270, 180, 0);
            
            // Score
            prevCityCachedProgress = distanceProgress;
            distanceProgress = 0;
            nextCityTransform = city2Transform;
            maxDistanceToNextCity = Vector3.Distance(nextCityTransform.position, playerTransform.position);

        }

        public void city2Trigger()
        {
            
            CarController.speed+=2;
            cameraStats.SetActive(false);
            
            var city2TransformPosition = city2Transform.position;
            var city1EulerAngles = city1Transform.eulerAngles;
            city1Transform.position = new Vector3(city2TransformPosition.x - 434.5f, city1Transform.position.y, city2TransformPosition.z - 433.16f);
            city1Transform.eulerAngles = new Vector3(city1EulerAngles.x, 90, city1EulerAngles.z);
            
            // Score
            prevCityCachedProgress = distanceProgress;
            distanceProgress = 0;
            nextCityTransform = city1Transform;
            maxDistanceToNextCity = Vector3.Distance(nextCityTransform.position, playerTransform.position);
        }

        public void city3Trigger()
        {
            
            CarController.speed+=2;
            
            
            Vector3 city1TransformPosition = city1Transform.position;
            var city2EulerAngles = city2Transform.eulerAngles;
            city2Transform.position = new Vector3(city1TransformPosition.x - 441.9f, city2Transform.position.y, city1TransformPosition.z - 443.245f);
            city2Transform.eulerAngles = new Vector3(city2EulerAngles.x, 270, city2EulerAngles.z);
            
            city2blocksign1a.SetActive(false);
            city2blocksign1b.SetActive(false);
            
            // score
            prevCityCachedProgress = distanceProgress;
            distanceProgress = 0;
            nextCityTransform = city2Transform;
            maxDistanceToNextCity = Vector3.Distance(nextCityTransform.position, playerTransform.position);


            
        }

        public void city4Trigger()
        {
           
            CarController.speed+=2;
            
            
            Vector3 city2TransformPosition = city2Transform.position;
            var city1EulerAngles = city1Transform.eulerAngles;
            city1Transform.position = new Vector3(city2TransformPosition.x - 434.41f, city1Transform.position.y,
                city2TransformPosition.z + 442.71552f);
            city1Transform.eulerAngles = new Vector3(city1EulerAngles.x, 0, city1EulerAngles.z);
            
            city2blocksign1a.SetActive(true);
            city2blocksign1b.SetActive(true);
            
            // score
            prevCityCachedProgress = distanceProgress;
            distanceProgress = 0;
            nextCityTransform = city1Transform;
            maxDistanceToNextCity = Vector3.Distance(nextCityTransform.position, playerTransform.position);


            
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!triggers.Contains(other)) triggers.Add(other);
        }


        private void OnTriggerExit(Collider other)
        {
            if (triggers.Contains(other)) triggers.Remove(other);
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("BusStop"))
            {
                if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Mouse0))
                {
                    RaycastHit hit;
                    if (Physics.Raycast(viewTransform.position, viewTransform.TransformDirection(Vector3.forward), out hit, 20f))
                    {
                        if (hit.collider.name.Equals("Next"))
                        {
                            panelDistance.SetActive(false);
                            panelTime.SetActive(true);
                        }

                        if (hit.collider.name.Equals("Back"))
                        {
                            panelDistance.SetActive(true);
                            panelTime.SetActive(false);
                        }

                        if (patCompleted == false && hit.collider.name.Equals("Kiki-v2"))
                        {
                            Steamworks.SteamUserStats.SetAchievement("headpat");
                            Steamworks.SteamUserStats.StoreStats();
                            patCompleted = true;
                        }
                    }
                }
            }
        }

        // private void OnCollisionStay(Collision collision)
        // {
        //     //if (collision.gameObject.layer == 7) Debug.Log("Collision with layer 7");
        //     Debug.Log(collision.gameObject.layer);
        //     if (collision.rigidbody == null) return;
        //
        //     Vector3 relativeVelocity = collision.relativeVelocity * collision.rigidbody.mass / 50f;
        //
        //     Vector3 impactVelocity = new Vector3(relativeVelocity.x * 0.0025f, relativeVelocity.y * 0.00025f,
        //         relativeVelocity.z * 0.0025f);
        //
        //     float maxYVel = Mathf.Max(moveData.velocity.y, 10f);
        //     Vector3 newVelocity = new Vector3(moveData.velocity.x + impactVelocity.x,
        //         Mathf.Clamp(moveData.velocity.y + Mathf.Clamp(impactVelocity.y, -0.5f, 0.5f), -maxYVel, maxYVel),
        //         moveData.velocity.z + impactVelocity.z);
        //
        //     newVelocity = Vector3.ClampMagnitude(newVelocity, Mathf.Max(moveData.velocity.magnitude, 30f));
        //     moveData.velocity = newVelocity;
        // }
    }
}