#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.SceneManagement;
using System.IO;
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;

namespace MTAssets.EasyMeshCombiner
{
    /*
      This class is responsible for the functioning of the "Combined Meshes Manager" component, and all its functions.
    */
    /*
     * The Easy Mesh Combiner was developed by Marcos Tomaz in 2019.
     * Need help? Contact me (mtassets@windsoft.xyz)
     */

    [AddComponentMenu("")] //Hide this script in component menu.
    public class CombinedMeshesManager : MonoBehaviour
    {
#if UNITY_EDITOR
        //------------------------------------------- START OF PARAMETERS OF INFORMATION OF MERGE -----------------------------------------
        //Enums of script
        public enum MergeMethod
        {
            OneMeshPerMaterial,
            AllInOne,
            JustMaterialColors
        }
        public enum UndoMethod
        {
            EnableOriginalMeshes,
            ReactiveOriginalGameObjects,
            DoNothing
        }
        public enum AssetType
        {
            Unknown,
            Mesh,
            Texture,
            Material
        }

        //Classes of script
        [System.Serializable]
        public class OriginalGameObjectWithMesh
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
        [System.Serializable]
        public class PathAndTypeOfAAsset
        {
            //Class that stores path and type of a asset

            public AssetType type = AssetType.Unknown;
            public string path;

            public PathAndTypeOfAAsset(AssetType type, string path)
            {
                this.type = type;
                this.path = path;
            }
        }

        //Variables of script, informations about this merge
        ///<summary>[WARNING] This variable is only available in the Editor and will not be included in the compilation of your project, in the final Build.</summary> 
        [HideInInspector]
        public MergeMethod mergeMethodUsed;
        ///<summary>[WARNING] This variable is only available in the Editor and will not be included in the compilation of your project, in the final Build.</summary> 
        [HideInInspector]
        public UndoMethod undoMethod;
        ///<summary>[WARNING] This variable is only available in the Editor and will not be included in the compilation of your project, in the final Build.</summary> 
        [HideInInspector]
        public List<OriginalGameObjectWithMesh> originalsGameObjectsWithMesh = new List<OriginalGameObjectWithMesh>();
        ///<summary>[WARNING] This variable is only available in the Editor and will not be included in the compilation of your project, in the final Build.</summary> 
        [HideInInspector]
        public List<PathAndTypeOfAAsset> pathsAndTypesOfAssetsOfThisMerge = new List<PathAndTypeOfAAsset>();
        ///<summary>[WARNING] This variable is only available in the Editor and will not be included in the compilation of your project, in the final Build.</summary> 
        [HideInInspector]
        public bool thisIsPrefab = false;
        //------------------------------------------ END OF PARAMETERS OF INFORMATION OF MERGE ------------------------------------------

        //Private variables of Editor Methods
        [HideInInspector]
        [SerializeField]
        private int exportToObjStartIndexOffSet = 0;
        [HideInInspector]
        [SerializeField]
        private bool hideWarningsForThisMerge = false;
        //Private variables of Editor Interface
        private bool gizmosOfThisComponentIsDisabled = false;

        //The UI of this component
        #region INTERFACE_CODE
        [UnityEditor.CustomEditor(typeof(CombinedMeshesManager))]
        public class CustomInspector : UnityEditor.Editor
        {
            //Private temp variables
            Vector2 scrollviewMaterials = Vector2.zero;
            List<string> warningsOfChecks = new List<string>();

            public override void OnInspectorGUI()
            {
                //Start the undo event support, draw default inspector and monitor of changes
                CombinedMeshesManager script = (CombinedMeshesManager)target;
                EditorGUI.BeginChangeCheck();
                Undo.RecordObject(target, "Undo Event");
                script.gizmosOfThisComponentIsDisabled = MTAssetsEditorUi.DisableGizmosInSceneView("CombinedMeshesManager", script.gizmosOfThisComponentIsDisabled);

                //Start of UI
                EditorGUILayout.HelpBox("This GameObject contains the meshes you previously combined. Through this component you can manage the mesh resulting from the merge.", MessageType.Info);
                GUILayout.Space(10);

                //Show the merge method used in this merge
                GUIStyle titulo = new GUIStyle();
                titulo.fontSize = 16;
                titulo.normal.textColor = new Color(0, 79.0f / 250.0f, 3.0f / 250.0f);
                titulo.alignment = TextAnchor.MiddleCenter;
                if (script.mergeMethodUsed == MergeMethod.OneMeshPerMaterial)
                    EditorGUILayout.LabelField("Merged Using One Mesh Per Material", titulo);
                if (script.mergeMethodUsed == MergeMethod.AllInOne)
                    EditorGUILayout.LabelField("Merged Using All In One", titulo);
                if (script.mergeMethodUsed == MergeMethod.JustMaterialColors)
                    EditorGUILayout.LabelField("Merged Using Just Material Colors", titulo);
                //Show warning box, if necessary
                RunFilesAndCheckersAndGenerateWarningBoxIfNecessary(script);
                GUILayout.Space(10);
                //Render the box of materials and assets
                RenderTheBoxOfMaterialsAndAssetsManagement(script);
                //Select all original gameObjects
                if (GUILayout.Button("Select All Original GameObjects", GUILayout.Height(30)))
                {
                    List<GameObject> gameObjects = new List<GameObject>();
                    foreach (OriginalGameObjectWithMesh ogo in script.originalsGameObjectsWithMesh)
                        gameObjects.Add(ogo.gameObject);
                    Selection.objects = gameObjects.ToArray();
                }
                GUILayout.Space(10);
                //Render the management buttons of this mesh
                RenderTheMainManagementButtonsForThisMesh(script);
                GUILayout.Space(10);
                EditorGUILayout.LabelField("More Informations About", EditorStyles.boldLabel);
                GUILayout.Space(10);
                EditorGUILayout.BeginVertical("box");
                StringBuilder informationsBuild = new StringBuilder();
                informationsBuild.Append("What will be done after undoing the merge\n");
                if (script.undoMethod == UndoMethod.EnableOriginalMeshes)
                    informationsBuild.Append("The original Mesh Renderers will return to their original state.");
                if (script.undoMethod == UndoMethod.ReactiveOriginalGameObjects)
                    informationsBuild.Append("The original GameObjects will return to their original state.");
                if (script.undoMethod == UndoMethod.DoNothing)
                    informationsBuild.Append("Nothing will be done.");
                informationsBuild.Append("\n\nAbout this GameObject\n");
                if (script.thisIsPrefab == true)
                    informationsBuild.Append("This merged GameObject is or was originally generated as a Prefab. If you decide to undo the merging, the generated files will not be deleted, in order to avoid breaking the copy of this prefab in other scenes.");
                if (script.thisIsPrefab == false)
                    informationsBuild.Append("This merged GameObject is not and was not originally generated as a Prefab. If you decide to undo the merge, the generated files will be deleted automatically, too.");
                EditorGUILayout.HelpBox(informationsBuild.ToString(), MessageType.Info);
                EditorGUILayout.EndVertical();

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

            private void RunFilesAndCheckersAndGenerateWarningBoxIfNecessary(CombinedMeshesManager script)
            {
                //This method will run some checkers and verifications and, if needed will show a box with warnings about merge
                warningsOfChecks.Clear();

                //Verify if has missing files of merge, if data save in assets option is enabled
                bool haveFilesMissing = false;
                foreach (PathAndTypeOfAAsset item in script.pathsAndTypesOfAssetsOfThisMerge)
                    if (File.Exists(item.path) == false)
                        haveFilesMissing = true;
                if (haveFilesMissing == true)
                    warningsOfChecks.Add("It appears that one or more files that were generated for this merge, are missing or have been moved from their original locations. If your merge is not as expected, try to redo it.");

                //Verify it the mesh of mesh filter is missing
                MeshFilter mergedMesh = script.GetComponent<MeshFilter>();
                if (mergedMesh.sharedMesh == null)
                    warningsOfChecks.Add("It looks like there are missing mesh file in this merge. To solve this problem, you can undo this merge and re-do it again!");

                //Verify if some original GameObject is not present more
                bool haveOriginalGameObjectsMissing = false;
                foreach (OriginalGameObjectWithMesh obj in script.originalsGameObjectsWithMesh)
                    if (obj.gameObject == null || obj.meshRenderer == null)
                        haveOriginalGameObjectsMissing = true;
                if (haveOriginalGameObjectsMissing == true)
                    warningsOfChecks.Add("It seems that some of the original GameObjects or MeshRenderers that make up this merge are no longer present in this scene, or have been deleted. The original GameObjects that are not found cannot be restored to their original state if you decide to undo this merge.");

                //Show the warnings
                if (warningsOfChecks.Count > 0)
                {
                    GUILayout.Space(10);
                    EditorGUILayout.BeginHorizontal("box");
                    GUIStyle tituloBox = new GUIStyle();
                    tituloBox.fontStyle = FontStyle.Bold;
                    tituloBox.alignment = TextAnchor.MiddleLeft;
                    EditorGUILayout.LabelField("Warnings For This Merge (" + warningsOfChecks.Count + ")", tituloBox);
                    if (script.hideWarningsForThisMerge == true)
                        if (GUILayout.Button("Show", GUILayout.Height(18), GUILayout.Width(50)))
                            script.hideWarningsForThisMerge = false;
                    if (script.hideWarningsForThisMerge == false)
                        if (GUILayout.Button("Hide", GUILayout.Height(18), GUILayout.Width(50)))
                            script.hideWarningsForThisMerge = true;
                    EditorGUILayout.EndHorizontal();
                    if (script.hideWarningsForThisMerge == false)
                    {
                        EditorGUILayout.BeginVertical("box");
                        foreach (string str in warningsOfChecks)
                            EditorGUILayout.HelpBox(str, MessageType.Warning);
                        EditorGUILayout.EndVertical();
                    }
                }
            }

            private void RenderTheBoxOfMaterialsAndAssetsManagement(CombinedMeshesManager script)
            {
                //If this is this mesh was generated by One Mesh Per Material
                if (script.mergeMethodUsed == MergeMethod.OneMeshPerMaterial)
                {
                    //Create a scroll view to select all gameobjects where material is equal to...
                    EditorGUILayout.LabelField("Selection By Materials", EditorStyles.boldLabel);
                    GUILayout.Space(10);
                    //Select all original gameObjects with X material
                    Dictionary<Material, List<GameObject>> objects = new Dictionary<Material, List<GameObject>>();
                    foreach (OriginalGameObjectWithMesh oGo in script.originalsGameObjectsWithMesh)
                    {
                        if (oGo == null || oGo.meshRenderer == null)
                            continue;
                        for (int i = 0; i < oGo.meshRenderer.sharedMaterials.Length; i++)
                        {
                            Material mat = oGo.meshRenderer.sharedMaterials[i];
                            if (mat == null)
                                continue;
                            if (objects.ContainsKey(mat) == false)
                                objects.Add(mat, new List<GameObject>() { oGo.gameObject });
                            if (objects.ContainsKey(mat) == true)
                                objects[mat].Add(oGo.gameObject);
                        }
                    }
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Select All Original Meshes That Uses...", GUILayout.Width(320));
                    GUILayout.Space(MTAssetsEditorUi.GetInspectorWindowSize().x - 320);
                    EditorGUILayout.LabelField("Size", GUILayout.Width(30));
                    EditorGUILayout.IntField(objects.Keys.Count, GUILayout.Width(50));
                    EditorGUILayout.EndHorizontal();
                    GUILayout.BeginVertical("box");
                    scrollviewMaterials = EditorGUILayout.BeginScrollView(scrollviewMaterials, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.Width(MTAssetsEditorUi.GetInspectorWindowSize().x), GUILayout.Height(150));
                    if (objects.Keys.Count == 0)
                        EditorGUILayout.HelpBox("Oops! The original materials of this merge were not found!", MessageType.Info);
                    if (objects.Keys.Count > 0)
                        foreach (var key in objects.Keys)
                            if (GUILayout.Button("\"" + key.name + "\" Material", GUILayout.Height(24)))
                                Selection.objects = objects[key].ToArray();
                    EditorGUILayout.EndScrollView();
                    GUILayout.EndVertical();
                }
                //If this is this mesh was generated by other merge method
                if (script.mergeMethodUsed != MergeMethod.OneMeshPerMaterial)
                {
                    //Create a scroll view to view all generated files...
                    EditorGUILayout.LabelField("All Generated Assets", EditorStyles.boldLabel);
                    GUILayout.Space(10);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("All Generated Assets In This Merge...", GUILayout.Width(320));
                    GUILayout.Space(MTAssetsEditorUi.GetInspectorWindowSize().x - 320);
                    EditorGUILayout.LabelField("Size", GUILayout.Width(30));
                    EditorGUILayout.IntField(script.pathsAndTypesOfAssetsOfThisMerge.Count, GUILayout.Width(50));
                    EditorGUILayout.EndHorizontal();
                    GUILayout.BeginVertical("box");
                    scrollviewMaterials = EditorGUILayout.BeginScrollView(scrollviewMaterials, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.Width(MTAssetsEditorUi.GetInspectorWindowSize().x), GUILayout.Height(118));
                    foreach (PathAndTypeOfAAsset item in script.pathsAndTypesOfAssetsOfThisMerge)
                        DrawItemOfListOfResourcesInStats(item.path);
                    EditorGUILayout.EndScrollView();
                    GUILayout.EndVertical();
                    //Draw export options
                    DrawExportAllAtlasOrMaterialsOption(script);
                }
            }

            private void RenderTheMainManagementButtonsForThisMesh(CombinedMeshesManager script)
            {
                //This method will render the buttons to manage this merge
                EditorGUILayout.LabelField("Management Of This Merge", EditorStyles.boldLabel);
                GUILayout.Space(10);

                if (script.GetComponent<MeshCollider>() == null)
                    if (GUILayout.Button("Add Mesh Collider To This Mesh", GUILayout.Height(30)))
                    {
                        script.gameObject.AddComponent<MeshCollider>();
                        Debug.Log("A Mesh Collider was added to this mesh!.");
                    }
                if (GUILayout.Button("Recalculate Normals Of This Mesh", GUILayout.Height(30)))
                {
                    script.GetComponent<MeshFilter>().sharedMesh.RecalculateNormals();
                    Debug.Log("The normals of this mesh resulting from the merging were recalculated.");
                }
                if (GUILayout.Button("Recalculate Tangents Of This Mesh", GUILayout.Height(30)))
                {
                    script.GetComponent<MeshFilter>().sharedMesh.RecalculateTangents();
                    Debug.Log("The tangents of this mesh resulting from the merging were recalculated.");
                }
                if (GUILayout.Button("Export This Mesh As OBJ", GUILayout.Height(30)))
                    script.ExportMeshAsObj(script);
                if (GUILayout.Button("Optimize This Mesh", GUILayout.Height(30)))
                {
                    script.GetComponent<MeshFilter>().sharedMesh.Optimize();
                    Debug.Log("The mesh resulting from the merge has been optimized!");
                }
                if (GUILayout.Button("Undo And Delete This Merge", GUILayout.Height(30)))
                {
                    bool confirmation = EditorUtility.DisplayDialog("Undo",
                        "This combined mesh and your GameObject will be deleted and removed from your scene. The original GameObjects/Meshes will be restored to their original state before the merge.\n\nAre you sure you want to undo this merge?",
                        "Yes",
                        "No");
                    if (confirmation == true)
                        script.UndoAndDeleteThisMerge();
                }
            }

            private void DrawItemOfListOfResourcesInStats(string filePath)
            {
                //Get the type of asset
                Type assetType = AssetDatabase.GetMainAssetTypeAtPath(filePath);

                //Load the asset
                var asset = AssetDatabase.LoadAssetAtPath(filePath, assetType);

                //Draw the item and represent the desired file
                GUILayout.Space(2);
                EditorGUILayout.BeginHorizontal();
                if (assetType == null)
                {
                    assetType = typeof(object);
                }
                if (assetType != null)
                {
                    if (assetType != typeof(Texture) && assetType != typeof(Texture2D))
                    {
                        GUILayout.Box("R", GUILayout.Width(28), GUILayout.Height(28));
                    }
                    if (assetType == typeof(Texture) || assetType == typeof(Texture2D))
                    {
                        GUIStyle estiloIcone = new GUIStyle();
                        estiloIcone.border = new RectOffset(0, 0, 0, 0);
                        estiloIcone.margin = new RectOffset(4, 0, 4, 0);
                        GUILayout.Box((Texture)asset, estiloIcone, GUILayout.Width(28), GUILayout.Height(28));
                    }
                }
                EditorGUILayout.BeginVertical();
                if (asset == null)
                    EditorGUILayout.LabelField("Resource Not Found", EditorStyles.boldLabel);
                if (asset != null)
                    EditorGUILayout.LabelField(asset.name, EditorStyles.boldLabel);
                GUILayout.Space(-3);
                if (assetType == typeof(Mesh))
                    EditorGUILayout.LabelField("Mesh/" + Path.GetExtension(filePath));
                if (assetType == typeof(Texture) || assetType == typeof(Texture2D))
                    EditorGUILayout.LabelField("Texture/" + Path.GetExtension(filePath));
                if (assetType == typeof(Material))
                    EditorGUILayout.LabelField("Material/" + Path.GetExtension(filePath));
                if (assetType == typeof(object))
                    EditorGUILayout.LabelField("Unknow/???");
                EditorGUILayout.EndVertical();
                GUILayout.Space(20);
                EditorGUILayout.BeginVertical();
                GUILayout.Space(8);
                if (GUILayout.Button("Resource", GUILayout.Height(20)))
                {
                    EditorGUIUtility.PingObject(asset);
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(2);
            }

            private void DrawExportAllAtlasOrMaterialsOption(CombinedMeshesManager script)
            {
                //Draw buttons to export resources
                EditorGUILayout.BeginHorizontal();
                //Create button to export all textures
                if (GUILayout.Button("Export All Atlas", GUILayout.Height(30)))
                {
                    //Open selection of folder
                    string folder = EditorUtility.OpenFolderPanel("Select Folder To Save", "", "");
                    if (String.IsNullOrEmpty(folder) == true)
                        return;

                    //Show progress bar
                    EditorUtility.DisplayProgressBar("A moment", "Exporting Atlas as PNG", 1.0f);

                    //For file of list of assets saved
                    foreach (PathAndTypeOfAAsset item in script.pathsAndTypesOfAssetsOfThisMerge)
                    {
                        //Load type of asset and the asset
                        var asset = AssetDatabase.LoadAssetAtPath(item.path, typeof(object));
                        var type = typeof(object);
                        if (asset != null)
                            type = asset.GetType();

                        if (asset == null)
                        {
                            EditorUtility.DisplayDialog("Atlas Not Found", "It was not possible to export the texture, as it was not found in the directory below\n\n" + item.path, "Continue");
                            continue;
                        }
                        if (asset != null && type != typeof(Texture2D))
                            continue;
                        if (asset != null && type == typeof(Texture2D))
                        {
                            Texture2D texture = asset as Texture2D;
                            byte[] mainTextureBytes = texture.EncodeToPNG();
                            File.WriteAllBytes(folder + "/" + asset.name + ".png", mainTextureBytes);
                        }
                    }

                    //Clear progress bar
                    EditorUtility.ClearProgressBar();

                    //Show warning
                    EditorUtility.DisplayDialog("Done", "Exporting process is finished. All atlas generated by this merge, was exported to path below\n\n" + folder, "Ok");
                }
                //Create button to export all materials
                if (GUILayout.Button("Export Material", GUILayout.Height(30)))
                {
                    //Open selection of folder
                    string folder = EditorUtility.OpenFolderPanel("Select Folder To Save", "", "");
                    if (String.IsNullOrEmpty(folder) == true)
                        return;

                    //Show progress bar
                    EditorUtility.DisplayProgressBar("A moment", "Exporting Materials", 1.0f);

                    //For file of list of assets saved
                    foreach (PathAndTypeOfAAsset item in script.pathsAndTypesOfAssetsOfThisMerge)
                    {
                        //Load type of asset and the asset
                        var asset = AssetDatabase.LoadAssetAtPath(item.path, typeof(object));
                        var type = typeof(object);
                        if (asset != null)
                            type = asset.GetType();

                        if (asset == null)
                        {
                            EditorUtility.DisplayDialog("Materials Not Found", "It was not possible to export the material, as it was not found in the directory below\n\n" + item.path, "Continue");
                            continue;
                        }
                        if (asset != null && type != typeof(Material))
                            continue;
                        if (asset != null && type == typeof(Material))
                        {
                            File.Copy(item.path, folder + "/" + asset.name + ".mat");
                        }
                    }

                    //Clear progress bar
                    EditorUtility.ClearProgressBar();

                    //Show warning
                    EditorUtility.DisplayDialog("Done", "Exporting process is finished. All materials generated by this merge, was exported to path below\n\n" + folder, "Ok");
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        #endregion

        //Core Methods

        private void UndoAndDeleteThisMerge()
        {
            //Show progress bar
            EditorUtility.DisplayProgressBar("A moment", "Undoing...", 1.0f);

            //Undo the merge according the type of merge
            if (undoMethod == UndoMethod.EnableOriginalMeshes)
                foreach (OriginalGameObjectWithMesh original in originalsGameObjectsWithMesh)
                {
                    //Skip, if is null
                    if (original.meshRenderer == null)
                        continue;
                    original.meshRenderer.enabled = original.originalMrState;
                }
            if (undoMethod == UndoMethod.ReactiveOriginalGameObjects)
                foreach (OriginalGameObjectWithMesh original in originalsGameObjectsWithMesh)
                {
                    //Skip, if is null
                    if (original.gameObject == null)
                        continue;
                    original.gameObject.SetActive(original.originalGoState);
                }

            //Delete unused asset, if this is not a prefab
            if (thisIsPrefab == false)
                foreach (PathAndTypeOfAAsset item in pathsAndTypesOfAssetsOfThisMerge)
                    if (AssetDatabase.LoadAssetAtPath(item.path, typeof(object)) != null)
                        AssetDatabase.DeleteAsset(item.path);

            //Set scene as dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            //Show dialog
            Debug.Log("The merge was successfully undone. All of the original GameObject/Meshes that this Manager could still access have been restored!\n\nIf you had chosen to save the merged meshes to your project files, all useless mesh files were deleted automatically!");

            //Destroy this merge
            DestroyImmediate(this.gameObject, true);

            //Clear progress bar
            EditorUtility.ClearProgressBar();
        }

        private void ExportMeshAsObj(CombinedMeshesManager script)
        {
            //Open the export window
            string folder = EditorUtility.OpenFolderPanel("Select Folder To Export", "", "");
            if (String.IsNullOrEmpty(folder) == true)
                return;

            //Show progress bar
            EditorUtility.DisplayProgressBar("A moment", "Exporting Mesh as OBJ", 1.0f);

            //Get this mesh
            MeshRenderer meshRenderer = this.gameObject.GetComponent<MeshRenderer>();
            MeshFilter meshFilter = this.gameObject.GetComponent<MeshFilter>();

            //Start export of mesh
            exportToObjStartIndexOffSet = 0;
            StringBuilder meshString = new StringBuilder();
            meshString.Append("#" + meshFilter.sharedMesh.name + ".obj"
                                + "\n#" + System.DateTime.Now.ToLongDateString()
                                + "\n#" + System.DateTime.Now.ToLongTimeString()
                                + "\n#-------"
                                + "\n\n");
            Transform transform = this.gameObject.transform;
            Vector3 originalPosition = transform.position;
            transform.position = Vector3.zero;
            meshString.Append(ExportToObjProcessTransform(transform, script));
            string meshStringResult = meshString.ToString();
            using (StreamWriter stringWriter = new StreamWriter(folder + "/" + meshFilter.sharedMesh.name + ".obj"))
            {
                stringWriter.Write(meshStringResult);
            }
            transform.position = originalPosition;
            exportToObjStartIndexOffSet = 0;

            //Clear progress bar
            EditorUtility.ClearProgressBar();

            //Show warning
            Debug.Log("The mesh was successfully exported to the directory \"" + folder + "\".");
        }

        //Tools Methods For Cor Methods

        private static string ExportToObjProcessTransform(Transform transform, CombinedMeshesManager script)
        {
            StringBuilder meshString = new StringBuilder();
            meshString.Append("#" + transform.name
                            + "\n#-------"
                            + "\n");

            meshString.Append("g ").Append(transform.name).Append("\n");

            MeshFilter mf = transform.GetComponent<MeshFilter>();

            if (mf)
                meshString.Append(ExportToObjMeshToString(mf, transform, script));

            for (int i = 0; i < transform.childCount; i++)
                meshString.Append(ExportToObjProcessTransform(transform.GetChild(i), script));

            return meshString.ToString();
        }

        private static string ExportToObjMeshToString(MeshFilter mf, Transform t, CombinedMeshesManager script)
        {
            Vector3 s = t.localScale;
            Vector3 p = t.localPosition;
            Quaternion r = t.localRotation;
            int numVertices = 0;

            Mesh m = mf.sharedMesh;
            if (!m)
                return "####Error####";

            Material[] mats = mf.GetComponent<MeshRenderer>().sharedMaterials;
            StringBuilder sb = new StringBuilder();

            foreach (Vector3 vv in m.vertices)
            {
                Vector3 v = t.TransformPoint(vv);
                numVertices++;
                sb.Append(string.Format("v {0} {1} {2}\n", v.x, v.y, -v.z));
            }

            sb.Append("\n");

            foreach (Vector3 nn in m.normals)
            {
                Vector3 v = r * nn;
                sb.Append(string.Format("vn {0} {1} {2}\n", -v.x, -v.y, v.z));
            }

            sb.Append("\n");

            foreach (Vector3 v in m.uv)
            {
                sb.Append(string.Format("vt {0} {1}\n", v.x, v.y));
            }

            for (int material = 0; material < m.subMeshCount; material++)
            {
                sb.Append("\n");
                sb.Append("usemtl ").Append(mats[material].name).Append("\n");
                sb.Append("usemap ").Append(mats[material].name).Append("\n");

                int[] triangles = m.GetTriangles(material);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n",
                        triangles[i] + 1 + script.exportToObjStartIndexOffSet, triangles[i + 1] + 1 + script.exportToObjStartIndexOffSet, triangles[i + 2] + 1 + script.exportToObjStartIndexOffSet));
                }
            }

            script.exportToObjStartIndexOffSet += numVertices;
            return sb.ToString();
        }
#endif
    }
}