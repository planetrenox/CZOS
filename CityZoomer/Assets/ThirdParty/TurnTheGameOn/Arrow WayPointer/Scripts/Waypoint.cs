using System;
using System.Diagnostics;
using Fragsurf.Movement;
using PR;


namespace TurnTheGameOn.ArrowWaypointer
{
    using UnityEngine;

    public class Waypoint : MonoBehaviour
    {
        public int radius;
        [HideInInspector] public WaypointController waypointController;
        [HideInInspector] public int waypointNumber;
        private Transform city1Transform, city2Transform;
        private SurfCharacter surfcharacter;
        public Material[] skybox;
        private int currSky = 0;


        private void Awake()
        {
            city1Transform = GameObject.Find("City1").GetComponent<Transform>();
            city2Transform = GameObject.Find("City2").GetComponent<Transform>();
            surfcharacter = GameObject.Find("PlayerController").GetComponent<SurfCharacter>();
        }

        private int nextCity = 2;
        // private Vector3 localPos;


        void changeSkyBox()
        {
            if (currSky != skybox.Length - 1) currSky++;
            RenderSettings.skybox = skybox[currSky];
        }

        void Update()
        {
            if (Vector3.Distance(transform.position, waypointController.player.position) < radius)
            {
                //waypointController.ChangeTarget();
                Vector3 tempPos;
                switch (nextCity)
                {
                    case 1:
                        changeSkyBox();
                        surfcharacter.city1Trigger();
                        nextCity = 2;
                        tempPos = city2Transform.position;
                        tempPos = new Vector3(tempPos.x, tempPos.y + 5, tempPos.z);
                        transform.position = tempPos;
                        break;
                    case 2:
                        surfcharacter.city2Trigger();
                        nextCity = 3;
                        tempPos = city1Transform.position;
                        tempPos = new Vector3(tempPos.x, tempPos.y + 5, tempPos.z);
                        transform.position = tempPos;
                        break;
                    case 3:
                        changeSkyBox();
                        surfcharacter.city3Trigger();
                        nextCity = 4;
                        tempPos = city2Transform.position;
                        tempPos = new Vector3(tempPos.x, tempPos.y + 5, tempPos.z);
                        transform.position = tempPos;
                        break;
                    case 4:
                        surfcharacter.city4Trigger();
                        nextCity = 1;
                        tempPos = city1Transform.position;
                        tempPos = new Vector3(tempPos.x, tempPos.y + 5, tempPos.z);
                        transform.position = tempPos;
                        break;
                }
            }
        }
        

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (waypointController != null) waypointController.OnDrawGizmosSelected(radius);
        }
#endif
    }
}