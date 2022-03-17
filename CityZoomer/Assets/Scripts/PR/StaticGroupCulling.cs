// gets nearby objects using CullingGroup
// Assign this script to some gameobject, that the distance is measured from

using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UnityLibrary
{
    public class StaticGroupCulling : MonoBehaviour
    {
        private float[] distances = {0f, 150f};
        GameObject[] CullList;
        CullingGroup cullGroup;
        BoundingSphere[] bounds;


        private void Awake()
        {
            CullList = GameObject.FindGameObjectsWithTag("Car");
        }

        void Start()
        {
            cullGroup = new CullingGroup();
            cullGroup.targetCamera = GetComponent<Camera>();
            cullGroup.SetDistanceReferencePoint(transform);
            cullGroup.SetBoundingDistances(distances);

            bounds = new BoundingSphere[CullList.Length];
            for (int i = 0; i < CullList.Length; i++)
            {
                var boundingSphere = new BoundingSphere {position = CullList[i].transform.position, radius = 1f};
                CullList[i].SetActive(false);
                bounds[i] = boundingSphere;
            }

            cullGroup.SetBoundingSpheres(bounds);
            cullGroup.SetBoundingSphereCount(CullList.Length);
            cullGroup.onStateChanged += StateChanged;
        }


        void StateChanged(CullingGroupEvent e)
        {
            var curr = e.currentDistance;
            if (curr == e.previousDistance) return;
            switch (curr)
            {
                case 1:
                    CullList[e.index].SetActive(true);
                    break;
                case 2:
                    CullList[e.index].SetActive(false);
                    break;
            }
        }
        
        private void OnDestroy()
        {
            cullGroup.onStateChanged -= StateChanged;
            cullGroup.Dispose();
            cullGroup = null;
        }
    }
}