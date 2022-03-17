using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MTAssets.EasyMeshCombiner
{
    public class CombineInRuntimeDemo : MonoBehaviour
    {
        public GameObject combineButton;
        public GameObject undoButton;
        public RuntimeMeshCombiner runtimeCombiner;

        void Update()
        {
            //If meshes are not merged
            if (runtimeCombiner.isTargetMeshesMerged() == false)
            {
                combineButton.SetActive(true);
                undoButton.SetActive(false);
            }
            //If meshes are merged
            if (runtimeCombiner.isTargetMeshesMerged() == true)
            {
                combineButton.SetActive(false);
                undoButton.SetActive(true);
            }
        }

        public void CombineMeshes()
        {
            runtimeCombiner.CombineMeshes();
        }

        public void UndoMerge()
        {
            runtimeCombiner.UndoMerge();
        }
    }
}