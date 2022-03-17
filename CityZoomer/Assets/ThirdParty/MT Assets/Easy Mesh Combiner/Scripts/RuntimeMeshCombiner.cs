#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MTAssets.EasyMeshCombiner
{
    /*
       This class is responsible for the functioning of the "Runtime Mesh Combiner" component, and all its functions.
    */
    /*
     * The Easy Mesh Combiner was developed by Marcos Tomaz in 2019.
     * Need help? Contact me (mtassets@windsoft.xyz)
     */

    [AddComponentMenu("MT Assets/Easy Mesh Combiner/Runtime Mesh Combiner")] //Add this component in a category of addComponent menu
    public class RuntimeMeshCombiner : MonoBehaviour
    {
        //Private constants
        private int MAX_VERTICES_FOR_16BITS_MESH = 50000; //Not change this

        //Classes of script
        private class GameObjectWithMesh
        {
            //Class that stores a valid gameobject that contains a mesh
            public GameObject gameObject;
            public MeshFilter meshFilter;
            public MeshRenderer meshRenderer;
            public GameObjectWithMesh(GameObject gameObject, MeshFilter meshFilter, MeshRenderer meshRenderer)
            {
                this.gameObject = gameObject;
                this.meshFilter = meshFilter;
                this.meshRenderer = meshRenderer;
            }
        }
        private class OriginalGameObjectWithMesh
        {
            //Class that stores a original GameObject With Mesh data, to restore on undo merge.

            public GameObject gameObject;
            public bool originalGoState;
            public MeshRenderer meshRenderer;
            public bool originalMrState;

            public OriginalGameObjectWithMesh(GameObject gameObject, bool originalGoState, MeshRenderer meshRenderer, bool originalMrState)
            {
                this.gameObject = gameObject;
                this.originalGoState = originalGoState;
                this.meshRenderer = meshRenderer;
                this.originalMrState = originalMrState;
            }
        }
        private class SubMeshToCombine
        {
            //Class that stores a mesh filter/renderer and respective submesh index, to combine
            public Transform transform;
            public MeshFilter meshFilter;
            public MeshRenderer meshRenderer;
            public int subMeshIndex;

            public SubMeshToCombine(Transform transform, MeshFilter meshFilter, MeshRenderer meshRenderer, int subMeshIndex)
            {
                this.transform = transform;
                this.meshFilter = meshFilter;
                this.meshRenderer = meshRenderer;
                this.subMeshIndex = subMeshIndex;
            }
        }

        //Enums of script
        public enum CombineOnStart
        {
            Disabled,
            OnStart,
            OnAwake
        }
        public enum AfterMerge
        {
            DisableOriginalMeshes,
            DeactiveOriginalGameObjects,
            DoNothing
        }

        //Variables of script
        private List<OriginalGameObjectWithMesh> originalGameObjectsWithMeshToRestore = new List<OriginalGameObjectWithMesh>();
        private bool targetMeshesMerged = false;

        //Variables of merge
        [HideInInspector]
        public AfterMerge afterMerge;
        [HideInInspector]
        public bool addMeshColliderAfter = true;
        [HideInInspector]
        public CombineOnStart combineMeshesAtStartUp = CombineOnStart.Disabled;
        [HideInInspector]
        public bool combineInChildren = false;
        [HideInInspector]
        public bool combineInactives = false;
        [HideInInspector]
        public bool recalculateNormals = true;
        [HideInInspector]
        public bool recalculateTangents = true;
        [HideInInspector]
        public bool optimizeResultingMesh = false;
        [HideInInspector]
        public List<GameObject> targetMeshes = new List<GameObject>();
        [HideInInspector]
        public bool showDebugLogs = true;
        [HideInInspector]
        public bool garbageCollectorAfterUndo = true;
        public UnityEvent onDoneMerge;
        public UnityEvent onDoneUnmerge;

        //The UI of this component
#if UNITY_EDITOR
        //Private variables of Interface
        private bool gizmosOfThisComponentIsDisabled = false;

        #region INTERFACE_CODE
        [UnityEditor.CustomEditor(typeof(RuntimeMeshCombiner))]
        public class CustomInspector : UnityEditor.Editor
        {
            //Private temp variables
            public Vector2 targetMeshes_ScrollPos;

            public override void OnInspectorGUI()
            {
                //Start the undo event support, draw default inspector and monitor of changes
                RuntimeMeshCombiner script = (RuntimeMeshCombiner)target;
                EditorGUI.BeginChangeCheck();
                Undo.RecordObject(target, "Undo Event");
                script.gizmosOfThisComponentIsDisabled = MTAssetsEditorUi.DisableGizmosInSceneView("RuntimeMeshCombiner", script.gizmosOfThisComponentIsDisabled);

                //Verify if this gameobject, already have the Mesh Renderer or Mesh Filter
                MeshFilter meshFilter = script.GetComponent<MeshFilter>();
                MeshRenderer meshRenderer = script.GetComponent<MeshRenderer>();
                if (meshRenderer != null || meshFilter != null)
                {
                    if (Application.isPlaying == false)
                    {
                        EditorGUILayout.HelpBox("Error. This GameObject already has a Mesh Filter/Renderer. Please add Runtime Combiner to a GameObject that does not have these components.", MessageType.Error);
                        return;
                    }
                }

                //Support reminder
                GUILayout.Space(10);
                EditorGUILayout.HelpBox("Remember to read the Easy Mesh Combiner documentation to understand how to use it.\nGet support at: mtassets@windsoft.xyz", MessageType.None);

                //Start of preferences
                GUILayout.Space(10);
                EditorGUILayout.LabelField("Preferences", EditorStyles.boldLabel);
                GUILayout.Space(10);

                script.afterMerge = (AfterMerge)EditorGUILayout.EnumPopup(new GUIContent("After Combine",
                                "What do you do after you complete the merge?\n\nDisable Original Meshes - The original meshes will be deactivated, so all the colliders and other components of the scenario will be kept intact, but the meshes will still be combined!\n\nDeactive Original GameObjects - All original GameObjects will be disabled. When you do not need to keep colliders and other active components in the scene, this is a good option!\n\nRelax, however, it is possible to undo the merge later and re-activate everything again!"),
                                script.afterMerge);
                if (script.afterMerge == AfterMerge.DeactiveOriginalGameObjects)
                {
                    EditorGUI.indentLevel += 1;
                    script.addMeshColliderAfter = (bool)EditorGUILayout.Toggle(new GUIContent("Add Mesh Collider After",
                            "Add Mesh Collider to the merge mesh after combining?"),
                            script.addMeshColliderAfter);
                    EditorGUI.indentLevel -= 1;
                }

                script.combineMeshesAtStartUp = (CombineOnStart)EditorGUILayout.EnumPopup(new GUIContent("Combine On Start",
                                        "Here you can enable or disable automatic meshing of meshes, right at the beginning of the execution of this scene in your game." +
                                        "\n\nDisabled - Auto merge will not be performed." +
                                        "\n\nOnAwake - Merging will take place in your game's Awake. Awake is executed before all the Start methods in your scene." +
                                        "\n\nOnStart - The merging will be done at the Start of your game."),
                                        script.combineMeshesAtStartUp);

                script.combineInChildren = EditorGUILayout.Toggle(new GUIContent("Combine Childrens Too",
                                "If this option is enabled, the EMC will combine the GameObjects children of the registered GameObjects for merging, too!"),
                                script.combineInChildren);

                script.combineInactives = EditorGUILayout.Toggle(new GUIContent("Combine Inactives Too",
                                "If this option is active, the EMC will combine inactive GameObjects as well, even if they are registered in the list of meshes to be merged."),
                                script.combineInactives);

                script.recalculateNormals = EditorGUILayout.Toggle(new GUIContent("Recalculate Normals",
                                "Enable this and the EMC will recalculate the normals of the mesh resulting from the merge. The EMC will preserve the normal data for the original fabrics if this is disabled."),
                                script.recalculateNormals);

                script.recalculateTangents = EditorGUILayout.Toggle(new GUIContent("Recalculate Tangents",
                                "Enable this and the EMC will recalculate the tangents of the mesh resulting from the merge. The EMC will preserve the tangent data for the original meshes if this is disabled."),
                                script.recalculateTangents);

                script.optimizeResultingMesh = EditorGUILayout.Toggle(new GUIContent("Optimize Resulting Mesh",
                                "If this option is enabled, the Runtime Mesh Combiner will optimize the mesh resulting from the merge. This may lead to performance gains in rendering the mesh resulting from the merging, through the mechanism of Unity.\n\nThis can slightly increase the mesh processing time."),
                                script.optimizeResultingMesh);

                script.garbageCollectorAfterUndo = EditorGUILayout.Toggle(new GUIContent("Run GC After Undo",
                                "Garbage Collector will free up memory that was used by unnecessary assets after undoing a merge, but this can negatively impact your game performance, depending on the complexity of the mesh combined. Run garbage collector after undoing a merge?"),
                                script.garbageCollectorAfterUndo);

                //Start of target meshes
                GUILayout.Space(10);
                EditorGUILayout.LabelField("Target Meshes", EditorStyles.boldLabel);
                GUILayout.Space(10);

                Texture2D removeItemIcon = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/ThirdParty/MT Assets/Easy Mesh Combiner/Editor/Images/Remove.png", typeof(Texture2D));
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Target Meshes To Merge", GUILayout.Width(145));
                GUILayout.Space(MTAssetsEditorUi.GetInspectorWindowSize().x - 145);
                EditorGUILayout.LabelField("Size", GUILayout.Width(30));
                EditorGUILayout.IntField(script.targetMeshes.Count, GUILayout.Width(50));
                EditorGUILayout.EndHorizontal();
                GUILayout.BeginVertical("box");
                targetMeshes_ScrollPos = EditorGUILayout.BeginScrollView(targetMeshes_ScrollPos, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.Width(MTAssetsEditorUi.GetInspectorWindowSize().x), GUILayout.Height(100));
                if (script.targetMeshes.Count == 0)
                    EditorGUILayout.HelpBox("Oops! No GameObjects with meshes was registered to be combined! If you want to subscribe any, click the button below!", MessageType.Info);
                if (script.targetMeshes.Count > 0)
                    for (int i = 0; i < script.targetMeshes.Count; i++)
                    {
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button(removeItemIcon, GUILayout.Width(25), GUILayout.Height(16)))
                            script.targetMeshes.RemoveAt(i);
                        script.targetMeshes[i] = (GameObject)EditorGUILayout.ObjectField(new GUIContent("GameObject " + i.ToString(), "The mesh found in this GameObject will be combined. Click the button to the left if you want to remove this GameObject from the list."), script.targetMeshes[i], typeof(GameObject), true, GUILayout.Height(16));
                        GUILayout.EndHorizontal();
                    }
                EditorGUILayout.EndScrollView();
                GUILayout.EndVertical();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Add New Slot"))
                {
                    script.targetMeshes.Add(null);
                    targetMeshes_ScrollPos.y += 999999;
                }
                if (script.targetMeshes.Count > 0)
                    if (GUILayout.Button("Remove Empty Slots", GUILayout.Width(Screen.width * 0.48f)))
                        for (int i = script.targetMeshes.Count - 1; i >= 0; i--)
                            if (script.targetMeshes[i] == null)
                                script.targetMeshes.RemoveAt(i);
                GUILayout.EndHorizontal();

                //Merge Events
                GUILayout.Space(10);
                EditorGUILayout.LabelField("Merge Events", EditorStyles.boldLabel);
                GUILayout.Space(10);
                DrawDefaultInspector();

                //Start of debug
                GUILayout.Space(10);
                EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
                GUILayout.Space(10);

                EditorGUILayout.Toggle(new GUIContent("Target Meshes Merged",
                                "Are the target meshes currently combined?"),
                                script.isTargetMeshesMerged());

                if (script.showDebugLogs == true)
                    EditorGUILayout.HelpBox("Excessive debug logs can cause performance fluctuations in your game. Just enable this function while debugging your merge and game.", MessageType.Warning);
                script.showDebugLogs = EditorGUILayout.Toggle(new GUIContent("Show Debug Logs",
                                "Debug logs notify you if the combiner encounters any invalid or similar mesh, but excessive debug logs can cause performance fluctuations in your game. View logs for debugging?"),
                                script.showDebugLogs);

                //Final space
                GUILayout.Space(10);

                //Stop paint of GUI, if this gameobject no more exists
                if (script == null)
                    return;

                //Apply changes on script, case is not playing in editor
                if (GUI.changed == true && Application.isPlaying == false)
                {
                    EditorUtility.SetDirty(script);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(script.gameObject.scene);
                }
                if (EditorGUI.EndChangeCheck() == true)
                {

                }
            }
        }
        #endregion
#endif

        //Component code

        void Awake()
        {
            //Combine meshes on start
            if (combineMeshesAtStartUp == CombineOnStart.OnAwake)
            {
                if (showDebugLogs == true)
                    Debug.Log("The merge started in Runtime Combiner \"" + this.gameObject.name + "\".");
                CombineMeshes();
            }
        }

        void Start()
        {
            //Combine meshes on start
            if (combineMeshesAtStartUp == CombineOnStart.OnStart)
            {
                if (showDebugLogs == true)
                    Debug.Log("The merge started in Runtime Combiner \"" + this.gameObject.name + "\".");
                CombineMeshes();
            }
        }

        GameObjectWithMesh[] GetValidatedTargetGameObjects()
        {
            //Validate the target gameobjects and return a list of valids GameObjects with mesh

            //Get all found gameobjects in targets gameobjects
            List<Transform> foundGameObjects = new List<Transform>();
            for (int i = 0; i < targetMeshes.Count; i++)
            {
                //Skips if this target mesh is null
                if (targetMeshes[i] == null)
                {
                    continue;
                }
                if (combineInChildren == true)
                {
                    Transform[] childrenGameObjectsInThis = targetMeshes[i].GetComponentsInChildren<Transform>(true);
                    foreach (Transform trs in childrenGameObjectsInThis)
                        if (foundGameObjects.Contains(trs) == false)             //<-- Check if this GameObject has already been added to the list before adding it, to avoid duplicates
                            foundGameObjects.Add(trs);
                }
                if (combineInChildren == false)
                {
                    Transform thisGameObjectTrs = targetMeshes[i].GetComponent<Transform>();
                    if (foundGameObjects.Contains(thisGameObjectTrs) == false)   //<-- Check if this GameObject has already been added to the list before adding it, to avoid duplicates
                        foundGameObjects.Add(thisGameObjectTrs);
                }
            }

            //Validate each found gameobject and split gameobjects that contains mesh filter or/and mesh renderer, and add to list of valid gameObjects
            List<GameObjectWithMesh> gameObjectsWithMesh = new List<GameObjectWithMesh>();
            for (int i = 0; i < foundGameObjects.Count; i++)
            {
                MeshFilter mf = foundGameObjects[i].GetComponent<MeshFilter>();
                MeshRenderer mr = foundGameObjects[i].GetComponent<MeshRenderer>();
                if (mf != null || mr != null)
                {
                    //If combine inactives is disabled, and mesh filter component is disabled in this object, skips this
                    if (combineInactives == false && mr.enabled == false)
                        continue;
                    if (combineInactives == false && foundGameObjects[i].gameObject.activeSelf == false)
                        continue;
                    if (combineInactives == false && foundGameObjects[i].gameObject.activeInHierarchy == false)
                        continue;

                    gameObjectsWithMesh.Add(new GameObjectWithMesh(foundGameObjects[i].gameObject, mf, mr));
                }
            }

            //Verify if each gameObject with mesh, is valid and have correct components settings
            List<GameObjectWithMesh> validsGameObjectsWithMesh = new List<GameObjectWithMesh>();
            for (int i = 0; i < gameObjectsWithMesh.Count; i++)
            {
                bool canAddToValidGameObjects = true;

                //Verify if MeshFilter is null
                if (gameObjectsWithMesh[i].meshFilter == null)
                {
                    if (showDebugLogs == true)
                    {
                        Debug.LogError("GameObject \"" + gameObjectsWithMesh[i].gameObject.name + "\" does not have the Mesh Filter component, so it is not a valid mesh and will be ignored in the merge process.");
                    }
                    canAddToValidGameObjects = false;
                }
                //Verify if MeshRenderer is null
                if (gameObjectsWithMesh[i].meshRenderer == null)
                {
                    if (showDebugLogs == true)
                    {
                        Debug.LogError("GameObject \"" + gameObjectsWithMesh[i].gameObject.name + "\" does not have the Mesh Renderer component, so it is not a valid mesh and will be ignored in the merge process.");
                    }
                    canAddToValidGameObjects = false;
                }
                //Verify if SharedMesh is null
                if (gameObjectsWithMesh[i].meshFilter != null && gameObjectsWithMesh[i].meshFilter.sharedMesh == null)
                {
                    if (showDebugLogs == true)
                    {
                        Debug.LogError("GameObject \"" + gameObjectsWithMesh[i].gameObject.name + "\" does not have a Mesh in Mesh Filter component, so it is not a valid mesh and will be ignored in the merge process.");
                    }
                    canAddToValidGameObjects = false;
                }
                //Verify if count of materials is different of count of submeshes
                if (gameObjectsWithMesh[i].meshFilter != null && gameObjectsWithMesh[i].meshRenderer != null && gameObjectsWithMesh[i].meshFilter.sharedMesh != null)
                {
                    if (gameObjectsWithMesh[i].meshFilter.sharedMesh.subMeshCount != gameObjectsWithMesh[i].meshRenderer.sharedMaterials.Length)
                    {
                        if (showDebugLogs == true)
                        {
                            Debug.LogError("The Mesh Renderer component found in GameObject \"" + gameObjectsWithMesh[i].gameObject.name + "\" has more or less material needed. The mesh that is in this GameObject has " + gameObjectsWithMesh[i].meshFilter.sharedMesh.subMeshCount.ToString() + " submeshes, but has a number of " + gameObjectsWithMesh[i].meshRenderer.sharedMaterials.Length.ToString() + " materials. This mesh will be ignored during the merge process.");
                        }
                        canAddToValidGameObjects = false;
                    }
                }
                //Verify if has null materials in MeshRenderer
                if (gameObjectsWithMesh[i].meshRenderer != null)
                {
                    for (int x = 0; x < gameObjectsWithMesh[i].meshRenderer.sharedMaterials.Length; x++)
                    {
                        if (gameObjectsWithMesh[i].meshRenderer.sharedMaterials[x] == null)
                        {
                            if (showDebugLogs == true)
                            {
                                Debug.LogError("Material " + x.ToString() + " in Mesh Renderer present in component \"" + gameObjectsWithMesh[i].gameObject.name + "\" is null. For the merge process to work well, all materials must be completed. This GameObject will be ignored in the merge process.");
                            }
                            canAddToValidGameObjects = false;
                        }
                    }
                }
                //Verify if this gameobject is already merged
                if (gameObjectsWithMesh[i].gameObject.GetComponent<CombinedMeshesManager>() != null)
                {
                    if (showDebugLogs == true)
                    {
                        Debug.LogError("GameObject \"" + gameObjectsWithMesh[i].gameObject.name + "\" is the result of a previous merge, so it will be ignored by this merge.");
                    }
                    canAddToValidGameObjects = false;
                }

                //If can add to valid GameObjects, add this gameobject
                if (canAddToValidGameObjects == true)
                {
                    validsGameObjectsWithMesh.Add(gameObjectsWithMesh[i]);
                }
            }

            return validsGameObjectsWithMesh.ToArray();
        }

        //API Methods

        public bool CombineMeshes()
        {
            //If meshes already are merged
            if (isTargetMeshesMerged() == true)
            {
                if (showDebugLogs == true)
                {
                    Debug.Log("The Runtime Combiner \"" + this.gameObject.name + "\" meshes are already combined!");
                }
                return true;
            }
            //If meshes is not merged
            if (isTargetMeshesMerged() == false)
            {
                //Get the GameObjectsWithMesh validated
                GameObjectWithMesh[] validsGameObjectsWithMesh = GetValidatedTargetGameObjects();

                //Verify if has valid gameObjects
                if (validsGameObjectsWithMesh.Length == 0)
                {
                    if (showDebugLogs == true)
                    {
                        Debug.LogError("No valid, meshed GameObjects were found in the target GameObjects list. Therefore the merge was interrupted.");
                    }
                    return false;
                }

                //Separate each submesh according to your material
                Dictionary<Material, List<SubMeshToCombine>> subMeshesPerMaterial = new Dictionary<Material, List<SubMeshToCombine>>();
                for (int i = 0; i < validsGameObjectsWithMesh.Length; i++)
                {
                    GameObjectWithMesh thisGoWithMesh = validsGameObjectsWithMesh[i];

                    for (int x = 0; x < thisGoWithMesh.meshFilter.sharedMesh.subMeshCount; x++)
                    {
                        Material currentMaterial = thisGoWithMesh.meshRenderer.sharedMaterials[x];
                        if (subMeshesPerMaterial.ContainsKey(currentMaterial) == true)
                        {
                            subMeshesPerMaterial[currentMaterial].Add(new SubMeshToCombine(thisGoWithMesh.gameObject.transform, thisGoWithMesh.meshFilter, thisGoWithMesh.meshRenderer, x));
                        }
                        if (subMeshesPerMaterial.ContainsKey(currentMaterial) == false)
                        {
                            subMeshesPerMaterial.Add(currentMaterial, new List<SubMeshToCombine>() { new SubMeshToCombine(thisGoWithMesh.gameObject.transform, thisGoWithMesh.meshFilter, thisGoWithMesh.meshRenderer, x) });
                        }
                    }
                }

                //Configure this GameObject
                MeshFilter holderMeshFilter = this.gameObject.AddComponent<MeshFilter>();
                MeshRenderer holderMeshRenderer = this.gameObject.AddComponent<MeshRenderer>();

                //Count the vertex in valids gameobjects
                int vertexCountInValidsGameObjects = 0;
                foreach (GameObjectWithMesh obj in validsGameObjectsWithMesh)
                {
                    vertexCountInValidsGameObjects += obj.meshFilter.sharedMesh.vertexCount;
                }

                //Combine the submeshes into one submesh according the material
                List<Mesh> combinedSubmehesPerMaterial = new List<Mesh>();
                foreach (var key in subMeshesPerMaterial)
                {
                    //Get the submeshes to merge, of current material
                    List<SubMeshToCombine> subMeshesOfCurrentMaterial = key.Value;

                    //Combine instances of submeshes from this material
                    List<CombineInstance> combineInstancesOfCurrentMaterial = new List<CombineInstance>();

                    //Process each submesh
                    for (int i = 0; i < subMeshesOfCurrentMaterial.Count; i++)
                    {
                        CombineInstance combineInstance = new CombineInstance();
                        combineInstance.mesh = subMeshesOfCurrentMaterial[i].meshFilter.sharedMesh;
                        combineInstance.subMeshIndex = subMeshesOfCurrentMaterial[i].subMeshIndex;
                        combineInstance.transform = subMeshesOfCurrentMaterial[i].transform.localToWorldMatrix;
                        combineInstancesOfCurrentMaterial.Add(combineInstance);
                    }

                    //Create the submesh with all submeshes with current material, and set limitation of vertices
                    Mesh mesh = new Mesh();
#if UNITY_2017_4 || UNITY_2018_1_OR_NEWER
                    if (vertexCountInValidsGameObjects <= MAX_VERTICES_FOR_16BITS_MESH)
                        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
                    if (vertexCountInValidsGameObjects > MAX_VERTICES_FOR_16BITS_MESH)
                        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
#endif
                    mesh.CombineMeshes(combineInstancesOfCurrentMaterial.ToArray(), true, true);

                    //Add to list of combined submeshes per material
                    combinedSubmehesPerMaterial.Add(mesh);
                }

                //Process each submeshes per material, creating final combine instances
                List<CombineInstance> finalCombineInstances = new List<CombineInstance>();
                foreach (Mesh mesh in combinedSubmehesPerMaterial)
                {
                    CombineInstance combineInstanceOfThisSubMesh = new CombineInstance();
                    combineInstanceOfThisSubMesh.mesh = mesh;
                    combineInstanceOfThisSubMesh.subMeshIndex = 0;
                    combineInstanceOfThisSubMesh.transform = Matrix4x4.identity;
                    finalCombineInstances.Add(combineInstanceOfThisSubMesh);
                }

                //Create the final mesh that contains all submeshes divided per material
                Mesh finalMesh = new Mesh();
#if UNITY_2017_4 || UNITY_2018_1_OR_NEWER
                if (vertexCountInValidsGameObjects <= MAX_VERTICES_FOR_16BITS_MESH)
                    finalMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
                if (vertexCountInValidsGameObjects > MAX_VERTICES_FOR_16BITS_MESH)
                    finalMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
#endif
#if !UNITY_2017_4 && !UNITY_2018_1_OR_NEWER
                if (vertexCountInValidsGameObjects > MAX_VERTICES_FOR_16BITS_MESH)
                        Debug.Log("The sum of vertices in the target GameObjects is greater than 65k. The resulting mesh may contain artifacts or be deformed. You can work around this issue only if your version of Unity is 2017.4 or higher.");
#endif
                finalMesh.name = this.gameObject.name + " (Temp Merge)";
                finalMesh.CombineMeshes(finalCombineInstances.ToArray(), false);
                finalMesh.RecalculateBounds();
                if (recalculateNormals == true)
                    finalMesh.RecalculateNormals();
                if (recalculateTangents == true)
                    finalMesh.RecalculateTangents();
                if (optimizeResultingMesh == true)
                    finalMesh.Optimize();

                //Polulate this GameObject with the data of combined mesh
                holderMeshFilter.sharedMesh = finalMesh;
                List<Material> materialsForSubMeshes = new List<Material>();
                foreach (var key in subMeshesPerMaterial)
                {
                    materialsForSubMeshes.Add(key.Key);
                }
                holderMeshRenderer.sharedMaterials = materialsForSubMeshes.ToArray();

                //Deactive original GameObjects if is desired
                if (afterMerge == AfterMerge.DeactiveOriginalGameObjects)
                {
                    foreach (GameObjectWithMesh obj in validsGameObjectsWithMesh)
                    {
                        originalGameObjectsWithMeshToRestore.Add(new OriginalGameObjectWithMesh(obj.gameObject, obj.gameObject.activeSelf, obj.meshRenderer, obj.meshRenderer.enabled));
                        obj.gameObject.SetActive(false);
                    }
                    //Add mesh collider, after merge, if disable gameobjects originals
                    if (addMeshColliderAfter == true)
                    {
                        this.gameObject.AddComponent<MeshCollider>();
                    }
                }
                //Disable original mesh filters and renderers if is desired
                if (afterMerge == AfterMerge.DisableOriginalMeshes)
                {
                    foreach (GameObjectWithMesh obj in validsGameObjectsWithMesh)
                    {
                        originalGameObjectsWithMeshToRestore.Add(new OriginalGameObjectWithMesh(obj.gameObject, obj.gameObject.activeSelf, obj.meshRenderer, obj.meshRenderer.enabled));
                        obj.meshRenderer.enabled = false;
                    }
                }
                //Do nothing if is desired
                if (afterMerge == AfterMerge.DoNothing)
                {

                }

                //Center the position
                this.gameObject.transform.position = new Vector3(0, 0, 0);

                //Show stats
                if (showDebugLogs == true)
                {
                    Debug.Log("The merge has been successfully completed in Runtime Combiner \"" + this.gameObject.name + "\"!");
                }

                //Run events
                if (onDoneMerge != null)
                {
                    onDoneMerge.Invoke();
                }

                targetMeshesMerged = true;
                return true;
            }
            return false;
        }

        public bool UndoMerge()
        {
            //If meshes already uncombined
            if (isTargetMeshesMerged() == false)
            {
                if (showDebugLogs == true)
                {
                    Debug.Log("The Runtime Combiner \"" + this.gameObject.name + "\" meshes are already uncombined!");
                }
                return true;
            }
            //If meshes are merged
            if (isTargetMeshesMerged() == true)
            {
                //Undo the merge according the type of merge
                if (afterMerge == AfterMerge.DisableOriginalMeshes)
                {
                    foreach (OriginalGameObjectWithMesh original in originalGameObjectsWithMeshToRestore)
                    {
                        //Skip, if is null
                        if (original.meshRenderer == null)
                        {
                            continue;
                        }
                        original.meshRenderer.enabled = original.originalMrState;
                    }
                }
                if (afterMerge == AfterMerge.DeactiveOriginalGameObjects)
                {
                    foreach (OriginalGameObjectWithMesh original in originalGameObjectsWithMeshToRestore)
                    {
                        //Skip, if is null
                        if (original.gameObject == null)
                        {
                            continue;
                        }
                        original.gameObject.SetActive(original.originalGoState);
                    }
                    if (addMeshColliderAfter == true)
                    {
                        MeshCollider meshCollider = this.GetComponent<MeshCollider>();

                        //Remove the mesh collider
                        if (meshCollider != null)
                        {
                            Destroy(meshCollider);
                        }
                    }
                }
                if (afterMerge == AfterMerge.DoNothing)
                {

                }

                //Reset variables
                originalGameObjectsWithMeshToRestore.Clear();

                //Remove unecessary components
                Destroy(this.GetComponent<MeshRenderer>());
                Destroy(this.GetComponent<MeshFilter>());
                if (garbageCollectorAfterUndo == true)
                {
                    Resources.UnloadUnusedAssets();
                    System.GC.Collect();
                }

                //Show stats
                if (showDebugLogs == true)
                {
                    Debug.Log("The Runtime Combiner \"" + this.gameObject.name + "\" merge was successfully undone!");
                }

                //Run events
                if (onDoneUnmerge != null)
                {
                    onDoneUnmerge.Invoke();
                }

                targetMeshesMerged = false;
                return true;
            }
            return false;
        }

        public bool isTargetMeshesMerged()
        {
            //Return if the meshes are merged
            return targetMeshesMerged;
        }
    }
}