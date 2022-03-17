using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEditor.SceneManagement;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
using System.Threading;
using System.Linq;

namespace MTAssets.EasyMeshCombiner.Editor
{
    public class MeshCombinerTool : EditorWindow
    {

        /*
         This class is responsible for the functioning of the "Easy Mesh Combiner" component, and all its functions.
        */
        /*
         * The Easy Mesh Combiner was developed by Marcos Tomaz in 2019.
         * Need help? Contact me (mtassets@windsoft.xyz)
         */

        //Private constants
        private int MAX_VERTICES_FOR_16BITS_MESH = 50000; //Not change this
        private float MULTIPLIER_FOR_CUBE_OF_MESH_SELECTION = 1.0f;

        //Enums of window
        public enum RenderPipelineCurrentlyBeingUsed
        {
            Loading,
            BuiltIn,
            LWRP,
            URP,
            HDRP
        }

        //Private variables of window
        private static MeshCombinerPreferences meshCombinerPreferences;
        private bool preferencesLoadedOnInspectorUpdate = false;
        private bool isWindowOnFocus = false;
        private bool isOnGuiMethodExitedFromLoadingScreen = false;
        private static ListRequest requestOfListOfAllPackages;
        private static RenderPipelineCurrentlyBeingUsed currentPipelineOfProject = RenderPipelineCurrentlyBeingUsed.Loading;

        //Private variables of cache for window
        private Vector2 scrollPosForPreferences = Vector2.zero;
        private Vector2 scrollPosForLogs = Vector2.zero;
        private int lastQuantityOfLogs = -1;
        private Vector2 scrollPosForStats = Vector2.zero;

        //Enums of script
        public enum GameObjectWithMeshValidation
        {
            Valid,
            ValidWithWarnings,
            Invalid
        }

        //Classes of script
        public class GameObjectWithMesh
        {
            //Class that store a gameobject that contains mesh filter and mesh renderer
            public GameObject gameObject;
            public MeshFilter meshFilter;
            public MeshRenderer meshRenderer;
            public GameObjectWithMeshValidation validation = GameObjectWithMeshValidation.Invalid;

            public GameObjectWithMesh(GameObject gameObject, GameObjectWithMeshValidation validation, MeshFilter meshFilter, MeshRenderer meshRenderer)
            {
                this.gameObject = gameObject;
                this.meshFilter = meshFilter;
                this.meshRenderer = meshRenderer;
                this.validation = validation;
            }
        }
        public class LogOfMerge
        {
            //Class that stores information about one log of merge
            public MessageType logType;
            public string message;

            public LogOfMerge(MessageType logType, string message)
            {
                this.logType = logType;
                this.message = message;
            }
        }
        public class StatisticsOfMerge
        {
            //Class that store the stats of merge, can be used to store statics before and after merge
            public int totalVertices;
            public int meshesCount;
            public int materialsCount;
            public int drawCallsAproximate;
            public float optimizationRate;
        }

        //Private variables from script (For before of merge)
        private List<GameObjectWithMesh> validsGameObjectsSelected = new List<GameObjectWithMesh>();
        private List<GameObjectWithMesh> invalidsGameObjectsSelected = new List<GameObjectWithMesh>();
        private int totalOfGameObjectsSelected = 0;
        private Transform bestParentTransformForGameObjectResultOfMerge = null;
        private int bestSibilingForGameObjectResultOfMerge = -1;
        private StatisticsOfMerge statisticsBeforeMerge = new StatisticsOfMerge();
        private StatisticsOfMerge statisticsAfterMerge = new StatisticsOfMerge();
        private List<LogOfMerge> logsOfBeforeMerge = new List<LogOfMerge>();
        private bool isAllMergeParamsValids = false;
        private bool mergeIsDone = false;

        public static void OpenWindow()
        {
            //Method to open the Window
            var window = GetWindow<MeshCombinerTool>("Combiner Tool");
            window.minSize = new Vector2(620, 670);
            window.maxSize = new Vector2(620, 670);
            var position = window.position;
            position.center = new Rect(0f, 0f, Screen.currentResolution.width, Screen.currentResolution.height).center;
            window.position = position;
            window.Show();
        }

        //UI Code
        #region INTERFACE_CODE
        void OnEnable()
        {
            //On enable this window, on re-start this window after compilation
            isWindowOnFocus = true;

            //Load the preferences
            LoadThePreferences(this);

            //Register the OnSceneGUI
#if !UNITY_2019_1_OR_NEWER
            SceneView.onSceneGUIDelegate += this.OnSceneGUI;
#endif
#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui += this.OnSceneGUI;
#endif
        }

        void OnDisable()
        {
            //On disable this window, after compilation, disables the window and enable again
            isWindowOnFocus = false;

            //Save the preferences
            SaveThePreferences(this);

            //Clear all validation gameobjects list and run on scene gui
            validsGameObjectsSelected.Clear();
            invalidsGameObjectsSelected.Clear();

            //Unregister the OnSceneGUI
#if !UNITY_2019_1_OR_NEWER
            SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
#endif
#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui += this.OnSceneGUI;
#endif
        }

        void OnDestroy()
        {
            //On close this window
            isWindowOnFocus = false;

            //Save the preferences
            SaveThePreferences(this);

            //Clear all validation gameobjects list and run on scene gui
            validsGameObjectsSelected.Clear();
            invalidsGameObjectsSelected.Clear();
        }

        void OnFocus()
        {
            //On focus this window
            isWindowOnFocus = true;
        }

        void OnLostFocus()
        {
            //On lose focus in window
            isWindowOnFocus = false;
        }

        void OnGUI()
        {
            //Start the undo event support, draw default inspector and monitor of changes 
            EditorGUI.BeginChangeCheck();

            //If the current pipeline information not received yet, stop the render of UI
            if (currentPipelineOfProject == RenderPipelineCurrentlyBeingUsed.Loading)
            {
                GUIStyle tituloBox = new GUIStyle();
                tituloBox.fontStyle = FontStyle.BoldAndItalic;
                tituloBox.alignment = TextAnchor.MiddleCenter;
                GUILayout.Space(320);
                EditorGUILayout.LabelField("Loading Render Pipeline Data...", tituloBox);

                //Run the checker to get render pipeline data (unregister automatically after get list of packages)
                if (requestOfListOfAllPackages == null)
                {
                    requestOfListOfAllPackages = Client.List();
                    EditorApplication.update += VerifyIfHaveAnotherRenderPipelinePackage;
                }
                return;
            }
            //Report that the OnGUI is already exited from loading screen
            isOnGuiMethodExitedFromLoadingScreen = true;

            //Try to load needed assets
            Texture iconOfUi = (Texture)AssetDatabase.LoadAssetAtPath("Assets/ThirdParty/MT Assets/Easy Mesh Combiner/Editor/Images/Icon.png", typeof(Texture));
            Texture iconDoneOfUi = (Texture)AssetDatabase.LoadAssetAtPath("Assets/ThirdParty/MT Assets/Easy Mesh Combiner/Editor/Images/IconDone.png", typeof(Texture));
            Texture arrowIcon = (Texture)AssetDatabase.LoadAssetAtPath("Assets/ThirdParty/MT Assets/Easy Mesh Combiner/Editor/Images/Arrow.png", typeof(Texture));
            Texture arrowDoneIcon = (Texture)AssetDatabase.LoadAssetAtPath("Assets/ThirdParty/MT Assets/Easy Mesh Combiner/Editor/Images/ArrowDone.png", typeof(Texture));
            //If fails on load needed assets, locks ui
            if (iconOfUi == null || arrowIcon == null || arrowDoneIcon == null || iconDoneOfUi == null)
            {
                EditorGUILayout.HelpBox("Unable to load required files. Please reinstall Easy Mesh Combiner to correct this problem.", MessageType.Error);
                return;
            }

            //Validate the current selection in each update of this UI
            if (mergeIsDone == false)
                ValidateGameObjectsSelection();

            //Render the TopBar of UI
            UI_TopBar(iconOfUi, iconDoneOfUi);

            GUILayout.BeginHorizontal();

            //Render the Merge Preferences
            UI_MergePreferences();
            //Render the Logs Of Merge
            UI_LogsOfMerge();

            GUILayout.EndHorizontal();

            //Render the Stats Bar
            UI_StatsBar(arrowIcon, arrowDoneIcon);

            //Bottom bar
            GUILayout.BeginHorizontal("box");
            if (validsGameObjectsSelected.Count == 0 || isAllMergeParamsValids == false)
            {
                GUILayout.Space(157);
                GUILayout.BeginVertical();
                GUILayout.Space(12);
                EditorGUILayout.HelpBox("Cannot merge GameObjects and meshes. Check the Logs of Merge above to understand why.", MessageType.Warning);
                GUILayout.Space(11);
                GUILayout.EndVertical();
                GUILayout.Space(153);
            }
            if (validsGameObjectsSelected.Count > 0 && isAllMergeParamsValids == true)
            {
                GUILayout.Space(200);
                GUILayout.BeginVertical();
                GUILayout.Space(7);
                if (mergeIsDone == false)
                {
                    if (GUILayout.Button("Combine Meshes!", GUILayout.Height(49)))
                    {
                        //Do the scene dirty
                        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

                        //Save the preferences
                        SaveThePreferences(this);

                        //Start the merge
                        if (meshCombinerPreferences.mergeMethod == MeshCombinerPreferences.MergeMethod.OneMeshPerMaterial)
                            DoCombineMeshes_OneMeshPerMaterial();
                        if (meshCombinerPreferences.mergeMethod == MeshCombinerPreferences.MergeMethod.AllInOne)
                            DoCombineMeshes_AllInOne();
                        if (meshCombinerPreferences.mergeMethod == MeshCombinerPreferences.MergeMethod.JustMaterialColors)
                            DoCombineMeshes_JustMaterialColors();
                    }
                }
                if (mergeIsDone == true)
                {
                    if (GUILayout.Button("Ok, Close This!", GUILayout.Height(49)))
                    {
                        //Save the preferences
                        SaveThePreferences(this);

                        //Close the window
                        this.Close();
                    }
                }
                GUILayout.Space(6);
                GUILayout.EndVertical();
                GUILayout.Space(200);
            }

            GUILayout.EndHorizontal();

            //Apply changes on script, case is not playing in editor
            if (GUI.changed == true && Application.isPlaying == false)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
            if (EditorGUI.EndChangeCheck() == true)
            {

            }
        }

        void UI_TopBar(Texture iconOfUi, Texture iconDoneOfUi)
        {
            GUIStyle estiloIcone = new GUIStyle();
            estiloIcone.border = new RectOffset(0, 0, 0, 0);
            estiloIcone.margin = new RectOffset(4, 0, 4, 0);

            //Calculate the text for render pipeline detected
            string renderPipelineDetected = "";
            switch (currentPipelineOfProject)
            {
                case RenderPipelineCurrentlyBeingUsed.BuiltIn:
                    renderPipelineDetected = "Built-In RP";
                    break;
                case RenderPipelineCurrentlyBeingUsed.HDRP:
                    renderPipelineDetected = "HDRP";
                    break;
                case RenderPipelineCurrentlyBeingUsed.LWRP:
                    renderPipelineDetected = "LWRP";
                    break;
                case RenderPipelineCurrentlyBeingUsed.URP:
                    renderPipelineDetected = "URP";
                    break;
            }

            //Topbar
            GUILayout.BeginHorizontal("box");
            GUILayout.Space(8);
            GUILayout.BeginVertical();
            GUILayout.Space(8);
            if (mergeIsDone == false)
                GUILayout.Box(iconOfUi, estiloIcone, GUILayout.Width(48), GUILayout.Height(44));
            if (mergeIsDone == true)
                GUILayout.Box(iconDoneOfUi, estiloIcone, GUILayout.Width(48), GUILayout.Height(44));
            GUILayout.Space(6);
            GUILayout.EndVertical();
            GUILayout.Space(8);
            GUILayout.Space(-118);
            GUILayout.BeginVertical();
            GUILayout.Space(14);
            GUIStyle titulo = new GUIStyle();
            titulo.fontSize = 25;
            titulo.normal.textColor = Color.black;
            titulo.alignment = TextAnchor.MiddleLeft;
            EditorGUILayout.LabelField("Easy Mesh Combiner", titulo);
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            GUILayout.Space(4);
            if (mergeIsDone == false)
            {
                GUIStyle subTitulo = new GUIStyle();
                subTitulo.fontSize = 11;
                subTitulo.alignment = TextAnchor.MiddleLeft;
                if (totalOfGameObjectsSelected == 0)
                    EditorGUILayout.LabelField("No GameObject has been selected.", subTitulo);
                if (totalOfGameObjectsSelected > 0)
                    EditorGUILayout.LabelField(totalOfGameObjectsSelected.ToString() + " GameObjects selected. " + validsGameObjectsSelected.Count.ToString() + " valid Meshes found. " + invalidsGameObjectsSelected.Count.ToString() + " Meshes ignored. " + renderPipelineDetected + " detected.", subTitulo);
            }
            if (mergeIsDone == true)
            {
                GUIStyle subTitulo = new GUIStyle();
                subTitulo.fontSize = 11;
                subTitulo.fontStyle = FontStyle.Bold;
                subTitulo.alignment = TextAnchor.MiddleLeft;
                subTitulo.normal.textColor = new Color(64f / 255.0f, 108f / 255.0f, 0f / 255.0f, 1.0f);
                EditorGUILayout.LabelField("The merge has been completed.", subTitulo);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        void UI_MergePreferences()
        {
            GUIStyle tituloBox = new GUIStyle();
            tituloBox.fontStyle = FontStyle.Bold;
            tituloBox.alignment = TextAnchor.MiddleCenter;

            //Preferences
            GUILayout.BeginVertical("box", GUILayout.Width(304));
            EditorGUILayout.LabelField("Preferences of Merge", tituloBox);
            GUILayout.Space(20);
            scrollPosForPreferences = EditorGUILayout.BeginScrollView(scrollPosForPreferences, GUILayout.Width(298), GUILayout.Height(297));

            meshCombinerPreferences.afterMerge = (MeshCombinerPreferences.AfterMerge)EditorGUILayout.EnumPopup(new GUIContent("After Combine",
                        "What do you do after you complete the merge?\n\nDisable Original Meshes - The original meshes will be deactivated, so all the colliders and other components of the scenario will be kept intact, but the meshes will still be combined!\n\nDeactive Original GameObjects - All original GameObjects will be disabled. When you do not need to keep colliders and other active components in the scene, this is a good option!\n\nDo Nothing - Easy Mesh Combiner will do absolutely nothing with the original meshes after merging.\n\nRelax, however, it is possible to undo the merge later and re-activate everything again!"),
                        meshCombinerPreferences.afterMerge);
            meshCombinerPreferences.mergeMethod = (MeshCombinerPreferences.MergeMethod)EditorGUILayout.EnumPopup(new GUIContent("Combine Method",
                        "Method to which the Easy Mesh Combiner will use to merge the meshes.\n\nOne Mesh Per Material - Combines all meshes that share the same materials in just one mesh. All meshes will continue to use the original materials. It is a fast method.\n\nAll In One - This merge method combines all meshes in just one mesh. Even if each mesh uses different materials. The textures and materials will also be merged into just one.\n\nJust Material Colors - It only works with the main colors of the materials. This merge method does not work with textures, it's perfect for people who do not use textures, just color their Materials of meshes. All meshes of the model are merged into one, and all colors of the materials are combined in an atlas color palette. The combined mesh will use the colors of this palette."),
                        meshCombinerPreferences.mergeMethod);
            meshCombinerPreferences.combineChildrens = (bool)EditorGUILayout.Toggle(new GUIContent("Combine Children",
                        "If you want to combine childrens of the selected GameObjects, enable this option!"),
                        meshCombinerPreferences.combineChildrens);
            if (meshCombinerPreferences.combineChildrens == true)
            {
                EditorGUI.indentLevel += 1;
                meshCombinerPreferences.combineInactives = (bool)EditorGUILayout.Toggle(new GUIContent("Combine Inactives",
                        "If you want to combine the GameObjects children that are disabled, just enable this option."),
                        meshCombinerPreferences.combineInactives);
                EditorGUI.indentLevel -= 1;
            }
            meshCombinerPreferences.lightmapSupport = (bool)EditorGUILayout.Toggle(new GUIContent("Lightmap Support",
                        "If you will use Lightmaps, enable this option so that the merged meshes can support it.\n\nNote that by enabling this option, the merged mesh will have more vertices than it should have, and if the vertex count exceeds 64k, support for lightmaps in that mesh will be canceled.\n\n** Keep in mind that enabling this option can greatly increase mescaling's processing time! **"),
                        meshCombinerPreferences.lightmapSupport);
            if (meshCombinerPreferences.lightmapSupport == true)
            {
                EditorGUI.indentLevel += 1;
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(154);
                if (GUILayout.Button("Deselect Excessive"))
                    DeselectExcessiveMeshesAt64kVertex();
                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel -= 1;
            }
            meshCombinerPreferences.saveMeshInAssets = (bool)EditorGUILayout.Toggle(new GUIContent("Save Mesh In Assets",
                        "After merging the meshes, the resulting mesh will be saved in your project files. That way, you will not lose the Combined Mesh and you can still build your game with the combined scene!"),
                        meshCombinerPreferences.saveMeshInAssets);
            meshCombinerPreferences.savePrefabOfThis = (bool)EditorGUILayout.Toggle(new GUIContent("Save Prefab Of This",
                        "After merge, Easy Mesh Combiner will save the prefab of this merge to your project files."),
                        meshCombinerPreferences.savePrefabOfThis);
            if (meshCombinerPreferences.savePrefabOfThis == true)
            {
                meshCombinerPreferences.saveMeshInAssets = true;
                EditorGUI.indentLevel += 1;
                EditorGUILayout.BeginHorizontal();
                meshCombinerPreferences.prefabName = EditorGUILayout.TextField(new GUIContent("Prefab Name",
                                "The name that will be given to the prefab generated after the merge."),
                                meshCombinerPreferences.prefabName);
                if (GUILayout.Button("Auto", GUILayout.Height(18), GUILayout.Width(38)) == true || meshCombinerPreferences.prefabName == "")
                {
                    DateTime now = DateTime.Now;
                    meshCombinerPreferences.prefabName = "prefab_of_merge_" + now.Ticks;
                }
                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel -= 1;
            }
            meshCombinerPreferences.nameOfThisMerge = (string)EditorGUILayout.TextField(new GUIContent("Name Of This Merge",
                        "The name that will be given to GameObject resulting from this merge."),
                        meshCombinerPreferences.nameOfThisMerge);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Additional Preferences", tituloBox);
            GUILayout.Space(10);
            switch (meshCombinerPreferences.mergeMethod)
            {
                case MeshCombinerPreferences.MergeMethod.OneMeshPerMaterial:
                    UI_MergePreferences_OneMeshPerMaterial();
                    break;
                case MeshCombinerPreferences.MergeMethod.AllInOne:
                    UI_MergePreferences_AllInOne();
                    break;
                case MeshCombinerPreferences.MergeMethod.JustMaterialColors:
                    UI_MergePreferences_JustMaterialColors();
                    break;
            }

            EditorGUILayout.EndScrollView();
            GUILayout.Space(4);
            EditorGUILayout.HelpBox("Remember to read the Easy Mesh Combiner documentation to understand how to use it.\nGet support at: mtassets@windsoft.xyz", MessageType.None);
            GUILayout.EndVertical();
        }

        void UI_MergePreferences_OneMeshPerMaterial()
        {
            //Additional settings for OneMeshPerMaterial merge method
            meshCombinerPreferences.oneMeshPerMaterialParams.addMeshCollider = (bool)EditorGUILayout.Toggle(new GUIContent("Add Mesh Collider",
                        "If this option is enabled, a Mesh Collider will be added to the resulting mesh, at the end of the merge."),
                        meshCombinerPreferences.oneMeshPerMaterialParams.addMeshCollider);
        }

        void UI_MergePreferences_AllInOne()
        {
            //Additional settings for AllInOne merge method

            //Validate additional effects to avoid problems
            if (meshCombinerPreferences.allInOneParams.specularMapSupport == true && meshCombinerPreferences.allInOneParams.metallicMapSupport == true)
            {
                meshCombinerPreferences.allInOneParams.metallicMapSupport = false;
                meshCombinerPreferences.allInOneParams.specularMapSupport = false;
            }

            //Define default main texture property, if desired
            if (currentPipelineOfProject != RenderPipelineCurrentlyBeingUsed.BuiltIn && meshCombinerPreferences.allInOneParams.useDefaultMainTextureProperty == true)
            {
                meshCombinerPreferences.allInOneParams.mainTexturePropertyToFind = "_BaseMap";
                meshCombinerPreferences.allInOneParams.mainTexturePropertyToInsert = "_BaseMap";
            }
            if (currentPipelineOfProject == RenderPipelineCurrentlyBeingUsed.BuiltIn && meshCombinerPreferences.allInOneParams.useDefaultMainTextureProperty == true)
            {
                meshCombinerPreferences.allInOneParams.mainTexturePropertyToFind = "_MainTex";
                meshCombinerPreferences.allInOneParams.mainTexturePropertyToInsert = "_MainTex";
            }

            //Get all properties of material to use
            Dictionary<string, string> propertiesOfMaterialToUse = new Dictionary<string, string>();
            if (meshCombinerPreferences.allInOneParams.materialToUse != null)
                for (int i = 0; i < ShaderUtil.GetPropertyCount(meshCombinerPreferences.allInOneParams.materialToUse.shader); i++)
                    if (propertiesOfMaterialToUse.ContainsKey(ShaderUtil.GetPropertyName(meshCombinerPreferences.allInOneParams.materialToUse.shader, i)) == false)
                        if (ShaderUtil.GetPropertyType(meshCombinerPreferences.allInOneParams.materialToUse.shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                            propertiesOfMaterialToUse.Add(ShaderUtil.GetPropertyName(meshCombinerPreferences.allInOneParams.materialToUse.shader, i), ShaderUtil.GetPropertyDescription(meshCombinerPreferences.allInOneParams.materialToUse.shader, i));
            //Get all unique materials found in all valid meshes
            Dictionary<Material, bool> allUniqueMaterialsDict = new Dictionary<Material, bool>();
            foreach (GameObjectWithMesh obj in validsGameObjectsSelected)
                for (int i = 0; i < obj.meshRenderer.sharedMaterials.Length; i++)
                    if (allUniqueMaterialsDict.ContainsKey(obj.meshRenderer.sharedMaterials[i]) == false)
                        allUniqueMaterialsDict.Add(obj.meshRenderer.sharedMaterials[i], true);
            List<Material> allMaterialsFound = new List<Material>();
            foreach (var item in allUniqueMaterialsDict)
                allMaterialsFound.Add(item.Key);
            //Get all properties of all materials founded
            Dictionary<string, string> propertiesOfAllMaterialsFounded = new Dictionary<string, string>();
            foreach (Material mat in allMaterialsFound)
                for (int i = 0; i < ShaderUtil.GetPropertyCount(mat.shader); i++)
                    if (propertiesOfAllMaterialsFounded.ContainsKey(ShaderUtil.GetPropertyName(mat.shader, i)) == false)
                        if (ShaderUtil.GetPropertyType(mat.shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                            propertiesOfAllMaterialsFounded.Add(ShaderUtil.GetPropertyName(mat.shader, i), ShaderUtil.GetPropertyDescription(mat.shader, i));

            if (meshCombinerPreferences.allInOneParams.materialToUse == null)
                EditorGUILayout.HelpBox("Please add a custom material. This custom material will have its properties copied and will be associated with the merged mesh. This merge method cannot function without a material.", MessageType.Error);
            meshCombinerPreferences.allInOneParams.materialToUse = (Material)EditorGUILayout.ObjectField(new GUIContent("Material To Use",
                "This custom material will have its properties copied and will be associated with the Merged mesh."),
                meshCombinerPreferences.allInOneParams.materialToUse, typeof(Material), true, GUILayout.Height(16));

            meshCombinerPreferences.allInOneParams.maxTexturesPerAtlas = EditorGUILayout.IntSlider(new GUIContent("Max Text. Per Atlas",
                        "In order to preserve the quality of textures as much as possible in the Atlas, Easy Mesh Combiner will divide your textures into several Atlas if there are many textures, because the less textures there are in an Atlas, the higher the quality of each texture.\n\nHere you can define the maximum amount of textures that each Atlas can have. The greater the amount of textures per Atlas, the greater the optimization as well."),
                        meshCombinerPreferences.allInOneParams.maxTexturesPerAtlas, 4, 20);

            meshCombinerPreferences.allInOneParams.atlasResolution = (MeshCombinerPreferences.AtlasSize)EditorGUILayout.EnumPopup(new GUIContent("Atlas Max Resolution",
                        "The maximum resolution that the generated atlases can have. The higher the texture, the more detail in the model. Larger textures will also consume more video memory."),
                        meshCombinerPreferences.allInOneParams.atlasResolution);

            meshCombinerPreferences.allInOneParams.mipMapEdgesSize = (MeshCombinerPreferences.MipMapEdgesSize)EditorGUILayout.EnumPopup(new GUIContent("Mip Map Edges Size",
                        "Each texture in the atlas must have borders to avoid rendering problems at certain camera angles, and when the atlas is submitted to different levels of detail according to distance (MipMaps). The larger the edges of each texture, the less chance that the textures appear to be in the wrong place depending on the distance or angle of the camera, however, the larger the edges of the textures, the smaller the size of the respective texture and, consequently, the smaller the detail of the textures, forcing you to increase the size of your atlas. In this option, you can select the size in pixels that the edges of the textures will have. Some effects like Height Maps and the like may require a larger border, such as 64 pixels or more.\n\nAlso keep in mind that increasing the size of the edges can increase the copy time for each texture.\n\nTry not to make the edges larger than the textures themselves, as it will cause them to repeat and the quality of each texture will be very low when rendered in your model."),
                        meshCombinerPreferences.allInOneParams.mipMapEdgesSize);

            meshCombinerPreferences.allInOneParams.atlasPadding = (MeshCombinerPreferences.AtlasPadding)EditorGUILayout.EnumPopup(new GUIContent("Atlas Padding",
                        "Here you can select the pixel spacing between each texture packaged in the atlas. If you are having problems with parts of the texture being rendered in unwanted locations on your model, try increasing this field. It is recommended that this field be 0 pixels, but if you want to increase it, keep in mind that this will increase the distance between each texture in the atlas, however, it will reduce the quality of all textures the higher the value selected here."),
                        meshCombinerPreferences.allInOneParams.atlasPadding);

            meshCombinerPreferences.allInOneParams.mergeTiledTextures = (MeshCombinerPreferences.MergeTiledTextures)EditorGUILayout.EnumPopup(new GUIContent("Merge Tiled Textures",
                        "Here you can define how Easy Mesh Combiner will handle Tiled meshes. This applies to meshes with a Tiled texture or Materials that have a Tiling value different than 1.\n\nSkip All - All meshes with Tiling will be ignored.\n\nLegacy Mode - This is the standard and most recommended mode for treating textures with Tiling. In this mode, all textures that have tiling will still be combined with the other meshes, but will have a dedicated texture for you. This way, the Tiling of the textures will not have a reduced quality and will not be negatively affected."),
                        meshCombinerPreferences.allInOneParams.mergeTiledTextures);

            meshCombinerPreferences.allInOneParams.useDefaultMainTextureProperty = (bool)EditorGUILayout.Toggle(new GUIContent("Default Main Tex. Prop.",
                        "If this option is disabled, the Easy Mesh Combiner will try to look up Main Textures in each material of this model and combine them as well.\n\nYou will also need to provide the name of the property on which the shaders save the Main Texture in \"Main Texture Settings\". Usually the most used name is \"_MainTex\" or \"_BaseMap\" (HDRP and URP)."),
                        meshCombinerPreferences.allInOneParams.useDefaultMainTextureProperty);
            if (meshCombinerPreferences.allInOneParams.useDefaultMainTextureProperty == false)
            {
                EditorGUI.indentLevel += 1;
                meshCombinerPreferences.allInOneParams.mainTexturePropertyToFind = DrawDropDownOfProperties("Find Main Text. In",
                "The name of the shader property, which is responsible for storing the Main Texture, in the materials of its meshes. The Easy Mesh Combiner will use the property here reported to fetch the Main Texture on each material in each mesh. Usually the name used by most shaders is \"_MainTex\" or \"_BaseMap\" (HDRP and URP), but if any of your shaders have a different name, you can enter it here.\n\nIf the Easy Mesh Combiner can not find the Main Texture in the mesh material, it will be without Main Texture after merging.",
                currentPipelineOfProject == RenderPipelineCurrentlyBeingUsed.BuiltIn ? "_MainTex" : "_BaseMap", (currentPipelineOfProject == RenderPipelineCurrentlyBeingUsed.BuiltIn) ? "(Main Texture Property)" : "(Albedo Property)", meshCombinerPreferences.allInOneParams.mainTexturePropertyToFind, propertiesOfAllMaterialsFounded);

                meshCombinerPreferences.allInOneParams.mainTexturePropertyToInsert = DrawDropDownOfProperties("Apply Main Text. In",
                "The name of the shader property, which will be responsible for storing the Main Texture of atlas, in the COMBINED mesh material. The Easy Mesh Combiner will use the property here informed to apply the Main Texture in the final material after the merge. Normally the name used by most shaders (including the standard pre-built shader) is \"_MainTex\" or \"_BaseMap\" (HDRP and URP), but if you have defined a custom shader and it has a different property name, you can enter it here.\n\nIf the Easy Mesh Combiner can not find the Main Texture in the final material of the mesh merge, it will be without Main Texture after merging.",
                currentPipelineOfProject == RenderPipelineCurrentlyBeingUsed.BuiltIn ? "_MainTex" : "_BaseMap", (currentPipelineOfProject == RenderPipelineCurrentlyBeingUsed.BuiltIn) ? "(Main Texture Property)" : "(Albedo Property)", meshCombinerPreferences.allInOneParams.mainTexturePropertyToInsert, propertiesOfMaterialToUse);
                EditorGUI.indentLevel -= 1;
            }
            if (meshCombinerPreferences.allInOneParams.useDefaultMainTextureProperty == true)
            {
                EditorGUI.indentLevel += 1;
                EditorGUILayout.TextField(new GUIContent("Finding Textures In", "This is the default property that Easy Mesh Combiner will look for your meshes material Main Textures. To change this and choose a non-standard property, uncheck the box above.\n\nThe Easy Mesh Combiner has determined that this is the default property, based on the Scriptable Render Pipeline that is being used now."), meshCombinerPreferences.allInOneParams.mainTexturePropertyToFind + (((currentPipelineOfProject == RenderPipelineCurrentlyBeingUsed.BuiltIn) ? " (Built-in RP)" : " (" + currentPipelineOfProject.ToString() + ")")));
                EditorGUILayout.TextField(new GUIContent("Applying Atlas Map In", "This is the default property that Easy Mesh Combiner will apply the generated atlas texture to your meshes. To change this and choose a non-standard property, uncheck the box above.\n\nThe Easy Mesh Combiner has determined that this is the default property, based on the Scriptable Render Pipeline that is being used now."), meshCombinerPreferences.allInOneParams.mainTexturePropertyToInsert + (((currentPipelineOfProject == RenderPipelineCurrentlyBeingUsed.BuiltIn) ? " (Built-in RP)" : " (" + currentPipelineOfProject.ToString() + ")")));
                EditorGUI.indentLevel -= 1;
            }

            meshCombinerPreferences.allInOneParams.metallicMapSupport = (bool)EditorGUILayout.Toggle(new GUIContent("Metallic Map Support",
                        "If this option is enabled, the Easy Mesh Combiner will try to look up Mettalic Map textures in each material of this model and combine them as well.\n\nYou will also need to provide the name of the property on which the shaders save the metallic texture map in \"Metallic Map Settings\". Usually the most used name is \"_MetallicGlossMap\"."),
                        meshCombinerPreferences.allInOneParams.metallicMapSupport);
            if (meshCombinerPreferences.allInOneParams.metallicMapSupport == true)
            {
                EditorGUI.indentLevel += 1;
                meshCombinerPreferences.allInOneParams.metallicMapPropertyToFind = DrawDropDownOfProperties("Find Text. Maps In",
                    "The name of the shader property, which is responsible for storing the metallic map texture, in the material of its meshes. The Easy Mesh Combiner will use the property here reported to fetch the metallic texture map on each material in each mesh. Usually the name used by most shaders is \"_MetallicGlossMap\", but if any of your shaders have a different name, you can enter it here.\n\nIf the Easy Mesh Combiner can not find the metallic map in the mesh material, it will be without metallic map after merging.",
                    "_MetallicGlossMap", "(Metallic Map Property)", meshCombinerPreferences.allInOneParams.metallicMapPropertyToFind, propertiesOfAllMaterialsFounded);

                meshCombinerPreferences.allInOneParams.metallicMapPropertyToInsert = DrawDropDownOfProperties("Apply Merged Map In",
                    "The name of the shader property, which will be responsible for storing the texture of metallic map atlas, in the COMBINED mesh material. The Easy Mesh Combiner will use the property here informed to apply the metallic atlas map texture in the final material after the merge. Normally the name used by most shaders (including the standard pre-built shader) is \"_MetallicGlossMap\", but if you have defined a custom shader and it has a different property name, you can enter it here.\n\nIf the Easy Mesh Combiner can not find the metallic map in the final material of the mesh merge, it will be without metallic map after merging.",
                    "_MetallicGlossMap", "(Metallic Map Property)", meshCombinerPreferences.allInOneParams.metallicMapPropertyToInsert, propertiesOfMaterialToUse);
                EditorGUI.indentLevel -= 1;
            }

            meshCombinerPreferences.allInOneParams.specularMapSupport = (bool)EditorGUILayout.Toggle(new GUIContent("Specu. Map Support",
                    "If this option is enabled, the Easy Mesh Combiner will try to look up Specular Map textures in each material of this model and combine them as well.\n\nYou will also need to provide the name of the property on which the shaders save the specular texture map in \"Specular Map Settings\". Usually the most used name is \"_SpecGlossMap\".\n\nKeep in mind that this function can increase the time taken to process the merge."),
                    meshCombinerPreferences.allInOneParams.specularMapSupport);
            if (meshCombinerPreferences.allInOneParams.specularMapSupport == true)
            {
                EditorGUI.indentLevel += 1;
                meshCombinerPreferences.allInOneParams.specularMapPropertyToFind = DrawDropDownOfProperties("Find Text. Maps In",
                    "The name of the shader property, which is responsible for storing the specular map texture, in the material of its meshes. The Easy Mesh Combiner will use the property here reported to fetch the specular texture map on each material in each mesh. Usually the name used by most shaders is \"_SpecGlossMap\", but if any of your shaders have a different name, you can enter it here.\n\nIf the Easy Mesh Combiner can not find the specular map in the mesh material, it will be without specular map after merging.",
                    "_SpecGlossMap", "(Specular Map Property)", meshCombinerPreferences.allInOneParams.specularMapPropertyToFind, propertiesOfAllMaterialsFounded);

                meshCombinerPreferences.allInOneParams.specularMapPropertyToInsert = DrawDropDownOfProperties("Apply Merged Map In",
                    "The name of the shader property, which will be responsible for storing the texture of specular map atlas, in the COMBINED mesh material. The Easy Mesh Combiner will use the property here informed to apply the specular atlas map texture in the final material after the merge. Normally the name used by most shaders (including the standard pre-built shader) is \"_SpecGlossMap\", but if you have defined a custom shader and it has a different property name, you can enter it here.\n\nIf the Easy Mesh Combiner can not find the specular map in the final material of the mesh merge, it will be without specular map after merging.",
                    "_SpecGlossMap", "(Specular Map Property)", meshCombinerPreferences.allInOneParams.specularMapPropertyToInsert, propertiesOfMaterialToUse);
                EditorGUI.indentLevel -= 1;
            }

            meshCombinerPreferences.allInOneParams.normalMapSupport = (bool)EditorGUILayout.Toggle(new GUIContent("Normal Map Support",
                    "If this option is enabled, the Easy Mesh Combiner will try to look up Normal Map textures in each material of this model and combine them as well.\n\nYou will also need to provide the name of the property on which the shaders save the normal texture map in \"Normal Map Settings\". Usually the most used name is \"_BumpMap\".\n\nKeep in mind that this function can increase the time taken to process the merge."),
                    meshCombinerPreferences.allInOneParams.normalMapSupport);
            if (meshCombinerPreferences.allInOneParams.normalMapSupport == true)
            {
                EditorGUI.indentLevel += 1;
                meshCombinerPreferences.allInOneParams.normalMapPropertyToFind = DrawDropDownOfProperties("Find Text. Maps In",
                    "The name of the shader property, which is responsible for storing the normal map texture, in the material of its meshes. The Easy Mesh Combiner will use the property here reported to fetch the normal texture map on each material in each mesh. Usually the name used by most shaders is \"_BumpMap\", but if any of your shaders have a different name, you can enter it here.\n\nIf the Easy Mesh Combiner can not find the normal map in the mesh material, it will be without normal map after merging.",
                    "_BumpMap", "(Normal Map Property)", meshCombinerPreferences.allInOneParams.normalMapPropertyToFind, propertiesOfAllMaterialsFounded);

                meshCombinerPreferences.allInOneParams.normalMapPropertyToInsert = DrawDropDownOfProperties("Apply Merged Map In",
                    "The name of the shader property, which will be responsible for storing the texture of normal map atlas, in the COMBINED mesh material. The Easy Mesh Combiner will use the property here informed to apply the normal atlas map texture in the final material after the merge. Normally the name used by most shaders (including the standard pre-built shader) is \"_BumpMap\", but if you have defined a custom shader and it has a different property name, you can enter it here.\n\nIf the Easy Mesh Combiner can not find the normal map in the final material of the mesh merge, it will be without normal map after merging.",
                    "_BumpMap", "(Normal Map Property)", meshCombinerPreferences.allInOneParams.normalMapPropertyToInsert, propertiesOfMaterialToUse);
                EditorGUI.indentLevel -= 1;
            }

            meshCombinerPreferences.allInOneParams.normalMap2Support = (bool)EditorGUILayout.Toggle(new GUIContent("Norm. Map 2 Support",
                    "If this option is enabled, the Easy Mesh Combiner will try to look up 2x Normal Map textures in each material of this model and combine them as well.\n\nYou will also need to provide the name of the property on which the shaders save the second normal texture map in \"Normal Map 2x Settings\". Usually the most used name is \"_DetailNormalMap\".\n\nKeep in mind that this function can increase the time taken to process the merge."),
                    meshCombinerPreferences.allInOneParams.normalMap2Support);
            if (meshCombinerPreferences.allInOneParams.normalMap2Support == true)
            {
                EditorGUI.indentLevel += 1;
                meshCombinerPreferences.allInOneParams.normalMap2PropertyFind = DrawDropDownOfProperties("Find Text. Maps In",
                    "The name of the shader property, which is responsible for storing the second normal map texture, in the material of its meshes. The Easy Mesh Combiner will use the property here reported to fetch the second normal texture map on each material in each mesh. Usually the name used by most shaders is \"_DetailNormalMap\", but if any of your shaders have a different name, you can enter it here.\n\nIf the Easy Mesh Combiner can not find the second normal map in the mesh material, it will be without second normal map after merging.",
                    "_DetailNormalMap", "(Normal Map Property)", meshCombinerPreferences.allInOneParams.normalMap2PropertyFind, propertiesOfAllMaterialsFounded);

                meshCombinerPreferences.allInOneParams.normalMap2PropertyToInsert = DrawDropDownOfProperties("Apply Merged Map In",
                    "The name of the shader property, which will be responsible for storing the texture of second normal map atlas, in the COMBINED mesh material. The Easy Mesh Combiner will use the property here informed to apply the second normal atlas map texture in the final material after the merge. Normally the name used by most shaders (including the standard pre-built shader) is \"_DetailNormalMap\", but if you have defined a custom shader and it has a different property name, you can enter it here.\n\nIf the Easy Mesh Combiner can not find the second normal map in the final material of the mesh merge, it will be without second normal map after merging.",
                    "_DetailNormalMap", "(Normal Map Property)", meshCombinerPreferences.allInOneParams.normalMap2PropertyToInsert, propertiesOfMaterialToUse);
                EditorGUI.indentLevel -= 1;
            }

            meshCombinerPreferences.allInOneParams.heightMapSupport = (bool)EditorGUILayout.Toggle(new GUIContent("Height Map Support",
                    "If this option is enabled, the Easy Mesh Combiner will try to look up Height Map textures in each material of this model and combine them as well.\n\nYou will also need to provide the name of the property on which the shaders save the height map texture in \"Height Map Settings\". Usually the most used name is \"_ParallaxMap\".\n\nKeep in mind that this function can increase the time taken to process the merge."),
                    meshCombinerPreferences.allInOneParams.heightMapSupport);
            if (meshCombinerPreferences.allInOneParams.heightMapSupport == true)
            {
                EditorGUI.indentLevel += 1;
                meshCombinerPreferences.allInOneParams.heightMapPropertyToFind = DrawDropDownOfProperties("Find Text. Maps In",
                    "The name of the shader property, which is responsible for storing the height map texture, in the material of its meshes. The Easy Mesh Combiner will use the property here reported to fetch the height texture map on each material in each mesh. Usually the name used by most shaders is \"_ParallaxMap\", but if any of your shaders have a different name, you can enter it here.\n\nIf the Easy Mesh Combiner can not find the height map in the mesh material, it will be without height map after merging.",
                    "_ParallaxMap", "(Height Map Property)", meshCombinerPreferences.allInOneParams.heightMapPropertyToFind, propertiesOfAllMaterialsFounded);

                meshCombinerPreferences.allInOneParams.heightMapPropertyToInsert = DrawDropDownOfProperties("Apply Merged Map In",
                    "The name of the shader property, which will be responsible for storing the texture of height map atlas, in the COMBINED mesh material. The Easy Mesh Combiner will use the property here informed to apply the height atlas map texture in the final material after the merge. Normally the name used by most shaders (including the standard pre-built shader) is \"_ParallaxMap\", but if you have defined a custom shader and it has a different property name, you can enter it here.\n\nIf the Easy Mesh Combiner can not find the height map in the final material of the mesh merge, it will be without height map after merging.",
                    "_ParallaxMap", "(Height Map Property)", meshCombinerPreferences.allInOneParams.heightMapPropertyToInsert, propertiesOfMaterialToUse);
                EditorGUI.indentLevel -= 1;
            }

            meshCombinerPreferences.allInOneParams.occlusionMapSupport = (bool)EditorGUILayout.Toggle(new GUIContent("Occlus. Map Support",
                    "If this option is enabled, the Easy Mesh Combiner will try to look up Occlusion Map textures in each material of this model and combine them as well.\n\nYou will also need to provide the name of the property on which the shaders save the occlusion texture map in \"Occlusion Map Settings\". Usually the most used name is \"_OcclusionMap\".\n\nKeep in mind that this function can increase the time taken to process the merge."),
                    meshCombinerPreferences.allInOneParams.occlusionMapSupport);
            if (meshCombinerPreferences.allInOneParams.occlusionMapSupport == true)
            {
                EditorGUI.indentLevel += 1;
                meshCombinerPreferences.allInOneParams.occlusionMapPropertyToFind = DrawDropDownOfProperties("Find Text. Maps In",
                    "The name of the shader property, which is responsible for storing the occlusion map texture, in the material of its meshes. The Easy Mesh Combiner will use the property here reported to fetch the occlusion texture map on each material in each mesh. Usually the name used by most shaders is \"_OcclusionMap\", but if any of your shaders have a different name, you can enter it here.\n\nIf the Easy Mesh Combiner can not find the occlusion map in the mesh material, it will be without occlusion map after merging.",
                    "_OcclusionMap", "(Occlusion Map Property)", meshCombinerPreferences.allInOneParams.occlusionMapPropertyToFind, propertiesOfAllMaterialsFounded);

                meshCombinerPreferences.allInOneParams.occlusionMapPropertyToInsert = DrawDropDownOfProperties("Apply Merged Map In",
                    "The name of the shader property, which will be responsible for storing the texture of occlusion map atlas, in the COMBINED mesh material. The Easy Mesh Combiner will use the property here informed to apply the occlusion atlas map texture in the final material after the merge. Normally the name used by most shaders (including the standard pre-built shader) is \"_OcclusionMap\", but if you have defined a custom shader and it has a different property name, you can enter it here.\n\nIf the Easy Mesh Combiner can not find the occlusion map in the final material of the mesh merge, it will be without occlusion map after merging.",
                    "_OcclusionMap", "(Occlusion Map Property)", meshCombinerPreferences.allInOneParams.occlusionMapPropertyToInsert, propertiesOfMaterialToUse);
                EditorGUI.indentLevel -= 1;
            }

            meshCombinerPreferences.allInOneParams.detailAlbedoMapSupport = (bool)EditorGUILayout.Toggle(new GUIContent("Detail Map Support",
                    "If this option is enabled, the Easy Mesh Combiner will try to look up Detail Albedo Map textures in each material of this model and combine them as well.\n\nYou will also need to provide the name of the property on which the shaders save the detail albedo texture map in \"Detail Albedo Map Settings\". Usually the most used name is \"_DetailAlbedoMap\".\n\nKeep in mind that this function can increase the time taken to process the merge."),
                    meshCombinerPreferences.allInOneParams.detailAlbedoMapSupport);
            if (meshCombinerPreferences.allInOneParams.detailAlbedoMapSupport == true)
            {
                EditorGUI.indentLevel += 1;
                meshCombinerPreferences.allInOneParams.detailMapPropertyToFind = DrawDropDownOfProperties("Find Text. Maps In",
                    "The name of the shader property, which is responsible for storing the detail albedo map texture, in the material of its meshes. The Easy Mesh Combiner will use the property here reported to fetch the detail albedo texture map on each material in each mesh. Usually the name used by most shaders is \"_DetailAlbedoMap\", but if any of your shaders have a different name, you can enter it here.\n\nIf the Easy Mesh Combiner can not find the detail albedo map in the mesh material, it will be without detail albedo map after merging.",
                    "_DetailAlbedoMap", "(Detail Map Property)", meshCombinerPreferences.allInOneParams.detailMapPropertyToFind, propertiesOfAllMaterialsFounded);

                meshCombinerPreferences.allInOneParams.detailMapPropertyToInsert = DrawDropDownOfProperties("Apply Merged Map In",
                    "The name of the shader property, which will be responsible for storing the texture of detail albedo map atlas, in the COMBINED mesh material. The Easy Mesh Combiner will use the property here informed to apply the detail albedo atlas map texture in the final material after the merge. Normally the name used by most shaders (including the standard pre-built shader) is \"_DetailAlbedoMap\", but if you have defined a custom shader and it has a different property name, you can enter it here.\n\nIf the Easy Mesh Combiner can not find the detail albedo map in the final material of the mesh merge, it will be without detail albedo map after merging.",
                    "_DetailAlbedoMap", "(Detail Map Property)", meshCombinerPreferences.allInOneParams.detailMapPropertyToInsert, propertiesOfMaterialToUse);
                EditorGUI.indentLevel -= 1;
            }

            meshCombinerPreferences.allInOneParams.detailMaskSupport = (bool)EditorGUILayout.Toggle(new GUIContent("Detail Mask Support",
                    "If this option is enabled, the Easy Mesh Combiner will try to look up Detail Mask textures in each material of this model and combine them as well.\n\nYou will also need to provide the name of the property on which the shaders save the detail mask texture map in \"Detail Mask Settings\". Usually the most used name is \"_DetailMask\".\n\nKeep in mind that this function can increase the time taken to process the merge."),
                    meshCombinerPreferences.allInOneParams.detailMaskSupport);
            if (meshCombinerPreferences.allInOneParams.detailMaskSupport == true)
            {
                EditorGUI.indentLevel += 1;
                meshCombinerPreferences.allInOneParams.detailMaskPropertyToFind = DrawDropDownOfProperties("Find Text. Maps In",
                    "The name of the shader property, which is responsible for storing the detail mask texture, in the material of its meshes. The Easy Mesh Combiner will use the property here reported to fetch the detail mask texture on each material in each mesh. Usually the name used by most shaders is \"_DetailMask\", but if any of your shaders have a different name, you can enter it here.\n\nIf the Easy Mesh Combiner can not find the detail albedo map in the mesh material, it will be without detail mask map after merging.",
                    "_DetailMask", "(Detail Mask Property)", meshCombinerPreferences.allInOneParams.detailMaskPropertyToFind, propertiesOfAllMaterialsFounded);

                meshCombinerPreferences.allInOneParams.detailMaskPropertyToInsert = DrawDropDownOfProperties("Apply Merged Map In",
                    "The name of the shader property, which will be responsible for storing the texture of detail mask map atlas, in the COMBINED mesh material. The Easy Mesh Combiner will use the property here informed to apply the detail mask atlas map texture in the final material after the merge. Normally the name used by most shaders (including the standard pre-built shader) is \"_DetailMask\", but if you have defined a custom shader and it has a different property name, you can enter it here.\n\nIf the Easy Mesh Combiner can not find the detail mask map in the final material of the mesh merge, it will be without detail mask map after merging.",
                    "_DetailMask", "(Detail Mask Property)", meshCombinerPreferences.allInOneParams.detailMaskPropertyToInsert, propertiesOfMaterialToUse);
                EditorGUI.indentLevel -= 1;
            }

            if (meshCombinerPreferences.allInOneParams.normalMapSupport == true || meshCombinerPreferences.allInOneParams.normalMap2Support == true)
            {
                meshCombinerPreferences.allInOneParams.pinkNormalMapsFix = (bool)EditorGUILayout.Toggle(new GUIContent("Pink Normal Maps Fix",
                "If this option is active, the Easy Mesh Combiner will execute an algorithm that will try to prevent the atlases generated from Normal Maps from becoming Pink/Orange, thanks to a different decoding of the colors of the original Normal Maps textures."),
                meshCombinerPreferences.allInOneParams.pinkNormalMapsFix);
            }

            meshCombinerPreferences.allInOneParams.addMeshCollider = (bool)EditorGUILayout.Toggle(new GUIContent("Add Mesh Collider",
                        "If this option is enabled, a Mesh Collider will be added to the resulting mesh, at the end of the merge."),
                        meshCombinerPreferences.allInOneParams.addMeshCollider);

            GUIStyle tituloBox = new GUIStyle();
            tituloBox.fontStyle = FontStyle.Bold;
            tituloBox.alignment = TextAnchor.MiddleCenter;
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Debugging Tools", tituloBox);
            GUILayout.Space(10);

            meshCombinerPreferences.allInOneParams.highlightUvVertices = (bool)EditorGUILayout.Toggle(new GUIContent("Highlight UV Vertices",
                        "If this option is enabled, after combining the textures in an atlas, the UV map vertices of the combined mesh will be displayed in the atlas by yellow pixels.\n\nKeep in mind that enabling this option will increase processing time when merging meshes."),
                        meshCombinerPreferences.allInOneParams.highlightUvVertices);
        }

        void UI_MergePreferences_JustMaterialColors()
        {
            //Additional settings for JustMaterialColors merge method

            //Define default main texture property, if desired
            if (currentPipelineOfProject != RenderPipelineCurrentlyBeingUsed.BuiltIn && meshCombinerPreferences.justMaterialColorsParams.useDefaultColorProperty == true)
            {
                meshCombinerPreferences.justMaterialColorsParams.colorPropertyToFind = "_BaseColor";
                meshCombinerPreferences.justMaterialColorsParams.mainTexturePropertyToInsert = "_BaseMap";
            }
            if (currentPipelineOfProject == RenderPipelineCurrentlyBeingUsed.BuiltIn && meshCombinerPreferences.justMaterialColorsParams.useDefaultColorProperty == true)
            {
                meshCombinerPreferences.justMaterialColorsParams.colorPropertyToFind = "_Color";
                meshCombinerPreferences.justMaterialColorsParams.mainTexturePropertyToInsert = "_MainTex";
            }

            //Get all properties of material to use
            Dictionary<string, string> propertiesOfMaterialToUse = new Dictionary<string, string>();
            if (meshCombinerPreferences.justMaterialColorsParams.materialToUse != null)
                for (int i = 0; i < ShaderUtil.GetPropertyCount(meshCombinerPreferences.justMaterialColorsParams.materialToUse.shader); i++)
                    if (propertiesOfMaterialToUse.ContainsKey(ShaderUtil.GetPropertyName(meshCombinerPreferences.justMaterialColorsParams.materialToUse.shader, i)) == false)
                        if (ShaderUtil.GetPropertyType(meshCombinerPreferences.justMaterialColorsParams.materialToUse.shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                            propertiesOfMaterialToUse.Add(ShaderUtil.GetPropertyName(meshCombinerPreferences.justMaterialColorsParams.materialToUse.shader, i), ShaderUtil.GetPropertyDescription(meshCombinerPreferences.justMaterialColorsParams.materialToUse.shader, i));
            //Get all unique materials found in all valid meshes
            Dictionary<Material, bool> allUniqueMaterialsDict = new Dictionary<Material, bool>();
            foreach (GameObjectWithMesh obj in validsGameObjectsSelected)
                for (int i = 0; i < obj.meshRenderer.sharedMaterials.Length; i++)
                    if (allUniqueMaterialsDict.ContainsKey(obj.meshRenderer.sharedMaterials[i]) == false)
                        allUniqueMaterialsDict.Add(obj.meshRenderer.sharedMaterials[i], true);
            List<Material> allMaterialsFound = new List<Material>();
            foreach (var item in allUniqueMaterialsDict)
                allMaterialsFound.Add(item.Key);
            //Get all properties of all materials founded
            Dictionary<string, string> propertiesOfAllMaterialsFounded = new Dictionary<string, string>();
            foreach (Material mat in allMaterialsFound)
                for (int i = 0; i < ShaderUtil.GetPropertyCount(mat.shader); i++)
                    if (propertiesOfAllMaterialsFounded.ContainsKey(ShaderUtil.GetPropertyName(mat.shader, i)) == false)
                        if (ShaderUtil.GetPropertyType(mat.shader, i) == ShaderUtil.ShaderPropertyType.Color)
                            propertiesOfAllMaterialsFounded.Add(ShaderUtil.GetPropertyName(mat.shader, i), ShaderUtil.GetPropertyDescription(mat.shader, i));

            if (meshCombinerPreferences.justMaterialColorsParams.materialToUse == null)
                EditorGUILayout.HelpBox("Please add a custom material. This custom material will have its properties copied and will be associated with the merged mesh. This merge method cannot function without a material.", MessageType.Error);
            meshCombinerPreferences.justMaterialColorsParams.materialToUse = (Material)EditorGUILayout.ObjectField(new GUIContent("Material To Use",
                "This custom material will have its properties copied and will be associated with the merged mesh."),
                meshCombinerPreferences.justMaterialColorsParams.materialToUse, typeof(Material), true, GUILayout.Height(16));

            meshCombinerPreferences.justMaterialColorsParams.useDefaultColorProperty = (bool)EditorGUILayout.Toggle(new GUIContent("Default Color Property",
               "If this option is disabled, the Easy Mesh Combiner will try to look up Colors in each material of the meshes and combine them as well.\n\nYou will also need to provide the name of the property on which the shaders save the Color in \"Color Settings\". Usually the most used name is \"_Color\" or \"_BaseColor\" (HDRP or URP)."),
               meshCombinerPreferences.justMaterialColorsParams.useDefaultColorProperty);
            if (meshCombinerPreferences.justMaterialColorsParams.useDefaultColorProperty == false)
            {
                EditorGUI.indentLevel += 1;
                meshCombinerPreferences.justMaterialColorsParams.colorPropertyToFind = DrawDropDownOfProperties("Find Colors In",
                    "The name of the shader property, which is responsible for storing the Color, in the material of its meshes. The Easy Mesh Combiner will use the property here reported to fetch the Color on each material in each mesh. Usually the name used by most shaders is \"_Color\" or \"_BaseColor\" (HDRP or URP), but if any of your shaders have a different name, you can enter it here.",
                    (currentPipelineOfProject == RenderPipelineCurrentlyBeingUsed.BuiltIn) ? "_Color" : "_BaseColor", (currentPipelineOfProject == RenderPipelineCurrentlyBeingUsed.BuiltIn) ? "(Color Property)" : "(Base Color Property)", meshCombinerPreferences.justMaterialColorsParams.colorPropertyToFind, propertiesOfAllMaterialsFounded);

                meshCombinerPreferences.justMaterialColorsParams.mainTexturePropertyToInsert = DrawDropDownOfProperties("Apply Color Atlas In",
                    "The name of the shader property, which will be responsible for storing the Color Atlas, in the COMBINED mesh material. The Easy Mesh Combiner will use the property here informed to apply the Color Atlas in the final material after the merge. Normally the name used by most shaders (including the standard pre-built shader) is \"_MainTex\" or \"_BaseMap\" (HDRP and URP), but if you have defined a custom shader and it has a different property name, you can enter it here.",
                    (currentPipelineOfProject == RenderPipelineCurrentlyBeingUsed.BuiltIn) ? "_MainTex" : "_BaseMap", (currentPipelineOfProject == RenderPipelineCurrentlyBeingUsed.BuiltIn) ? "(Main Texture Property)" : "(Albedo Property)", meshCombinerPreferences.justMaterialColorsParams.mainTexturePropertyToInsert, propertiesOfMaterialToUse);
                EditorGUI.indentLevel -= 1;
            }
            if (meshCombinerPreferences.justMaterialColorsParams.useDefaultColorProperty == true)
            {
                EditorGUI.indentLevel += 1;
                EditorGUILayout.TextField(new GUIContent("Finding Colors In", "This is the default property that Easy Mesh Combiner will look for your meshes material colors. To change this and choose a non-standard property, uncheck the box above.\n\nThe Easy Mesh Combiner has determined that this is the default property, based on the Scriptable Render Pipeline that is being used now."), meshCombinerPreferences.justMaterialColorsParams.colorPropertyToFind + (((currentPipelineOfProject == RenderPipelineCurrentlyBeingUsed.BuiltIn) ? " (Built-in RP)" : " (" + currentPipelineOfProject.ToString() + ")")));
                EditorGUILayout.TextField(new GUIContent("Applying Atlas Map In", "This is the default property that Easy Mesh Combiner will apply the generated color atlas texture to your combined mesh. To change this and choose a non-standard property, uncheck the box above.\n\nThe Easy Mesh Combiner has determined that this is the default property, based on the Scriptable Render Pipeline that is being used now."), meshCombinerPreferences.justMaterialColorsParams.mainTexturePropertyToInsert + (((currentPipelineOfProject == RenderPipelineCurrentlyBeingUsed.BuiltIn) ? " (Built-in RP)" : " (" + currentPipelineOfProject.ToString() + ")")));
                EditorGUI.indentLevel -= 1;
            }

            meshCombinerPreferences.justMaterialColorsParams.addMeshCollider = (bool)EditorGUILayout.Toggle(new GUIContent("Add Mesh Collider",
                        "If this option is enabled, a Mesh Collider will be added to the resulting mesh, at the end of the merge."),
                        meshCombinerPreferences.justMaterialColorsParams.addMeshCollider);
        }

        void UI_LogsOfMerge()
        {
            GUIStyle tituloBox = new GUIStyle();
            tituloBox.fontStyle = FontStyle.Bold;
            tituloBox.alignment = TextAnchor.MiddleCenter;

            //Logs of Merge
            GUILayout.BeginVertical("box", GUILayout.Width(304));
            EditorGUILayout.LabelField("Logs of Merge (" + logsOfBeforeMerge.Count.ToString() + ")", tituloBox);
            GUILayout.Space(20);
            scrollPosForLogs = EditorGUILayout.BeginScrollView(scrollPosForLogs, GUILayout.Width(298), GUILayout.Height(325));
            for (int i = 0; i < logsOfBeforeMerge.Count; i++)
                EditorGUILayout.HelpBox(logsOfBeforeMerge[i].message, logsOfBeforeMerge[i].logType);
            EditorGUILayout.EndScrollView();
            //Set the scroll of logs to end, if has new logs
            if (logsOfBeforeMerge.Count != lastQuantityOfLogs)
            {
                scrollPosForLogs.y += 99999;
                lastQuantityOfLogs = logsOfBeforeMerge.Count;
            }
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(60);
            meshCombinerPreferences.representLogsInScene = (bool)EditorGUILayout.Toggle(new GUIContent("Represent logs in scene",
                        "Check this option to have Easy Mesh Combiner represent in your scene the valid and invalid meshes found in your selection."),
                        meshCombinerPreferences.representLogsInScene);
            EditorGUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        void UI_StatsBar(Texture arrowIcon, Texture arrowDoneIcon)
        {
            GUIStyle tituloBox = new GUIStyle();
            tituloBox.fontStyle = FontStyle.Bold;
            tituloBox.alignment = TextAnchor.MiddleCenter;

            GUIStyle estiloIcone = new GUIStyle();
            estiloIcone.border = new RectOffset(0, 0, 0, 0);
            estiloIcone.margin = new RectOffset(4, 0, 4, 0);

            //Stats bar
            GUILayout.BeginHorizontal("box");
            scrollPosForStats = EditorGUILayout.BeginScrollView(scrollPosForStats, GUILayout.Width(606), GUILayout.Height(127));
            if (validsGameObjectsSelected.Count == 0)
                GUILayout.Space(14);
            if (validsGameObjectsSelected.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Current Statistics", tituloBox);
                EditorGUILayout.LabelField("After This Merge (Estimate)", tituloBox);
                EditorGUILayout.EndHorizontal();
            }
            if (validsGameObjectsSelected.Count == 0)
            {
                GUILayout.BeginVertical();
                GUILayout.Space(40);
                GUIStyle noGameObjectsStats = new GUIStyle();
                noGameObjectsStats.alignment = TextAnchor.MiddleCenter;
                EditorGUILayout.LabelField("No valid meshes selected.", noGameObjectsStats);
                GUILayout.EndVertical();
            }
            if (validsGameObjectsSelected.Count > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                GUILayout.Space(6);
                GUIStyle gameObjectsStatsBefore = new GUIStyle();
                gameObjectsStatsBefore.alignment = TextAnchor.MiddleLeft;
                EditorGUILayout.LabelField("Vertex Count: " + statisticsBeforeMerge.totalVertices, gameObjectsStatsBefore);
                EditorGUILayout.LabelField("Meshes Count: " + statisticsBeforeMerge.meshesCount, gameObjectsStatsBefore);
                EditorGUILayout.LabelField("Materials Count: " + statisticsBeforeMerge.materialsCount, gameObjectsStatsBefore);
                EditorGUILayout.LabelField("Draw Calls ± " + statisticsBeforeMerge.drawCallsAproximate, gameObjectsStatsBefore);
                EditorGUILayout.LabelField("Optimization Rate: " + statisticsBeforeMerge.optimizationRate + "%", gameObjectsStatsBefore);
                GUILayout.EndVertical();
                GUILayout.Space(16);
                GUILayout.BeginVertical();
                GUILayout.Space(32);
                if (mergeIsDone == false)
                {
                    GUILayout.Box(arrowIcon, estiloIcone, GUILayout.Width(40), GUILayout.Height(44));
                }
                if (mergeIsDone == true)
                {
                    GUILayout.Box(arrowDoneIcon, estiloIcone, GUILayout.Width(40), GUILayout.Height(44));
                }
                GUILayout.EndVertical();
                GUILayout.BeginVertical();
                GUILayout.Space(6);
                GUIStyle gameObjectsStatsAfter = new GUIStyle();
                gameObjectsStatsAfter.alignment = TextAnchor.MiddleRight;
                EditorGUILayout.LabelField("Vertex Count: " + statisticsAfterMerge.totalVertices, gameObjectsStatsAfter);
                EditorGUILayout.LabelField("Meshes Count: " + statisticsAfterMerge.meshesCount, gameObjectsStatsAfter);
                EditorGUILayout.LabelField("Materials Count: " + statisticsAfterMerge.materialsCount, gameObjectsStatsAfter);
                EditorGUILayout.LabelField("Draw Calls ± " + statisticsAfterMerge.drawCallsAproximate, gameObjectsStatsAfter);
                EditorGUILayout.LabelField("Optimization Rate: " + statisticsAfterMerge.optimizationRate.ToString("F1") + "%", gameObjectsStatsAfter);
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            GUILayout.EndHorizontal();
        }

        void OnSceneGUI(SceneView sceneView)
        {
            //If a merge already is done, not runs the highlight
            if (mergeIsDone == true)
                return;

            //Show the selected GameObject in scene GUI, if is enabled. Only works if merge is not ended.
            if (meshCombinerPreferences.representLogsInScene == true)
            {
                //Do a loop in list of valid meshes
                foreach (GameObjectWithMesh obj in validsGameObjectsSelected)
                {
                    //If this item has deleted, continues to next
                    if (obj.gameObject == null)
                        continue;

                    //Set the default color
                    Handles.color = Color.blue;
                    //Set the color as yellow, if this mesh have a warning
                    if (obj.validation == GameObjectWithMeshValidation.ValidWithWarnings)
                        Handles.color = Color.yellow;

                    //Render a cube in the mesh position
                    Bounds bounds = obj.meshRenderer.bounds;
                    Handles.DrawWireCube(obj.gameObject.transform.position, new Vector3(bounds.size.x * MULTIPLIER_FOR_CUBE_OF_MESH_SELECTION, bounds.size.y * MULTIPLIER_FOR_CUBE_OF_MESH_SELECTION, bounds.size.z * MULTIPLIER_FOR_CUBE_OF_MESH_SELECTION));
                }
                //Do a loop in list of invalid meshes
                foreach (GameObjectWithMesh obj in invalidsGameObjectsSelected)
                {
                    //If this item has deleted, continues to next
                    if (obj.gameObject == null)
                        continue;

                    //Set the default color
                    Handles.color = Color.red;

                    //Render a sphere in the mesh position
                    Bounds bounds = obj.meshRenderer.bounds;
                    Handles.DrawWireCube(obj.gameObject.transform.position, new Vector3(bounds.size.x * MULTIPLIER_FOR_CUBE_OF_MESH_SELECTION, bounds.size.y * MULTIPLIER_FOR_CUBE_OF_MESH_SELECTION, bounds.size.z * MULTIPLIER_FOR_CUBE_OF_MESH_SELECTION));
                }
            }
        }

        void OnInspectorUpdate()
        {
            //On inspector update, on lost focus in this Window, update the GUI (if the GUI is not exited from loading screen yet, force update too)
            if (isWindowOnFocus == false || isOnGuiMethodExitedFromLoadingScreen == false)
            {
                //Update this window
                Repaint();
                //Update the scene GUI
                if (SceneView.lastActiveSceneView != null)
                    SceneView.lastActiveSceneView.Repaint();
            }

            //Try to load the preferences on inspector update (if this window is in focus or not, try to load here, because this method runs after OpenWindow() method)
            if (preferencesLoadedOnInspectorUpdate == false)
            {
                if (meshCombinerPreferences.windowPosition.x != 0 && meshCombinerPreferences.windowPosition.y != 0)
                    LoadThePreferences(this);
                preferencesLoadedOnInspectorUpdate = true;
            }
        }

        private static void VerifyIfHaveAnotherRenderPipelinePackage()
        {
            //If request is not done yet, return
            if (requestOfListOfAllPackages.IsCompleted == false)
                return;

            //Data about other package
            bool haveAnotherRenderPipelinePackage = false;

            //Scan all packages, and if is using BuiltIn Render Pipeline, return true
            foreach (UnityEditor.PackageManager.PackageInfo package in requestOfListOfAllPackages.Result)
            {
                if (package.name.Contains("render-pipelines.universal"))
                {
                    haveAnotherRenderPipelinePackage = true;
                    currentPipelineOfProject = RenderPipelineCurrentlyBeingUsed.URP;
                }
                if (package.name.Contains("render-pipelines.high-definition"))
                {
                    haveAnotherRenderPipelinePackage = true;
                    currentPipelineOfProject = RenderPipelineCurrentlyBeingUsed.HDRP;
                }
                if (package.name.Contains("render-pipelines.lightweight"))
                {
                    haveAnotherRenderPipelinePackage = true;
                    currentPipelineOfProject = RenderPipelineCurrentlyBeingUsed.LWRP;
                }
            }

            //If not have other package, set as built in
            if (haveAnotherRenderPipelinePackage == false)
                currentPipelineOfProject = RenderPipelineCurrentlyBeingUsed.BuiltIn;

            //Unregister this method from Editor update
            EditorApplication.update -= VerifyIfHaveAnotherRenderPipelinePackage;
        }

        private void ValidateGameObjectsSelection()
        {
            //If a merge already is done, not run the validation, and mantains the last validation
            if (mergeIsDone == true)
                return;

            //Clear the valid and invalids gameobjects found in last verification, and reset other informations
            validsGameObjectsSelected.Clear();
            invalidsGameObjectsSelected.Clear();
            logsOfBeforeMerge.Clear();
            totalOfGameObjectsSelected = 0;
            int verticesCountInValidGos = 0;

            //Reset statstics
            if (statisticsBeforeMerge == null)
                statisticsBeforeMerge = new StatisticsOfMerge();
            if (statisticsAfterMerge == null)
                statisticsAfterMerge = new StatisticsOfMerge();
            statisticsBeforeMerge.totalVertices = 0;
            statisticsBeforeMerge.materialsCount = 0;
            statisticsBeforeMerge.meshesCount = 0;
            statisticsBeforeMerge.drawCallsAproximate = 0;
            statisticsBeforeMerge.optimizationRate = 0;
            statisticsAfterMerge.totalVertices = 0;
            statisticsAfterMerge.materialsCount = 0;
            statisticsAfterMerge.meshesCount = 0;
            statisticsAfterMerge.drawCallsAproximate = 0;
            statisticsAfterMerge.optimizationRate = 0;

            //Get all the selected game objects in scene
            GameObject[] gameObjectsSelected = Selection.gameObjects;

            //If not have selected GameObjects, return
            if (gameObjectsSelected.Length == 0)
            {
                logsOfBeforeMerge.Add(new LogOfMerge(MessageType.Info, "No GameObject has been selected. Select at least one GameObject so that Easy Mesh Combiner can work."));
                return;
            }

            //Calculate the index of gameobject result of merge, in hierarchy of scene. put it after last gameobject selected on hierarchy
            bestParentTransformForGameObjectResultOfMerge = gameObjectsSelected[0].transform.parent;
            bestSibilingForGameObjectResultOfMerge = gameObjectsSelected[0].transform.GetSiblingIndex() + 1;

            //Get all found gameobjects in this selection, with parameters (avoiding duplicated GameObjects)
            List<Transform> allFoundGameObjects = new List<Transform>();
            for (int i = 0; i < gameObjectsSelected.Length; i++)
            {
                if (meshCombinerPreferences.combineChildrens == true)
                {
                    Transform[] childrenGameObjectsInThis = gameObjectsSelected[i].GetComponentsInChildren<Transform>(true);
                    foreach (Transform trs in childrenGameObjectsInThis)
                        if (allFoundGameObjects.Contains(trs) == false)             //<-- Check if this GameObject has already been added to the list before adding it, to avoid duplicates
                            allFoundGameObjects.Add(trs);
                }
                if (meshCombinerPreferences.combineChildrens == false)
                {
                    Transform thisGameObjectTrs = gameObjectsSelected[i].GetComponent<Transform>();
                    if (allFoundGameObjects.Contains(thisGameObjectTrs) == false)   //<-- Check if this GameObject has already been added to the list before adding it, to avoid duplicates
                        allFoundGameObjects.Add(thisGameObjectTrs);
                }
            }
            totalOfGameObjectsSelected = allFoundGameObjects.Count;

            //Verify if has found gameObjects
            if (allFoundGameObjects.Count == 0)
            {
                logsOfBeforeMerge.Add(new LogOfMerge(MessageType.Info, "No GameObject has been selected. Select at least one GameObject so that Easy Mesh Combiner can work."));
                return;
            }

            //Alocate a list to store all GameObjects with mesh found, that have a mesh renderer and/or mesh filter
            List<GameObjectWithMesh> gameObjectsWithMeshFound = new List<GameObjectWithMesh>();

            //Validate each found gameobject and split gameobjects that contains mesh filter or/and mesh renderer, and add to list of valid gameObjects
            for (int i = 0; i < allFoundGameObjects.Count; i++)
            {
                MeshFilter mf = allFoundGameObjects[i].GetComponent<MeshFilter>();
                MeshRenderer mr = allFoundGameObjects[i].GetComponent<MeshRenderer>();
                if (mf != null || mr != null)
                {
                    //If combine inactives is disabled, and mesh filter component/gameobject is disabled in this object, skips this
                    if (meshCombinerPreferences.combineInactives == false && mr != null && mr.enabled == false)
                        continue;
                    if (meshCombinerPreferences.combineInactives == false && allFoundGameObjects[i].gameObject.activeSelf == false)
                        continue;
                    if (meshCombinerPreferences.combineInactives == false && allFoundGameObjects[i].gameObject.activeInHierarchy == false)
                        continue;

                    gameObjectsWithMeshFound.Add(new GameObjectWithMesh(allFoundGameObjects[i].gameObject, GameObjectWithMeshValidation.Invalid, mf, mr));
                }
            }

            ////------------------- START OF COMPONENTS AND GAMEOBJECTS REAL VALIDATION -----------------------////
            for (int i = 0; i < gameObjectsWithMeshFound.Count; i++)
            {
                //Verify if each gameObject with mesh, is valid and have correct components settings
                GameObject thisGameObject = gameObjectsWithMeshFound[i].gameObject;
                MeshFilter thisMeshFilter = gameObjectsWithMeshFound[i].meshFilter;
                MeshRenderer thisMeshRenderer = gameObjectsWithMeshFound[i].meshRenderer;
                bool haveWarningsForThisMesh = false;
                bool canAddThisGameObjectToValidGameObjects = true;

                //Verify if MeshFilter is null
                if (thisMeshFilter == null)
                {
                    logsOfBeforeMerge.Add(new LogOfMerge(MessageType.Error, "GameObject \"" + thisGameObject.name + "\" does not have the Mesh Filter component, so it is not a valid mesh and will be ignored in the merge process."));
                    canAddThisGameObjectToValidGameObjects = false;
                }
                //Verify if MeshRenderer is null
                if (thisMeshRenderer == null)
                {
                    logsOfBeforeMerge.Add(new LogOfMerge(MessageType.Error, "GameObject \"" + thisGameObject.name + "\" does not have the Mesh Renderer component, so it is not a valid mesh and will be ignored in the merge process."));
                    canAddThisGameObjectToValidGameObjects = false;
                }
                //Verify if SharedMesh is null
                if (thisMeshRenderer != null && thisMeshFilter.sharedMesh == null)
                {
                    logsOfBeforeMerge.Add(new LogOfMerge(MessageType.Error, "GameObject \"" + thisGameObject + "\" does not have a Mesh in Mesh Filter component, so it is not a valid mesh and will be ignored in the merge process."));
                    canAddThisGameObjectToValidGameObjects = false;
                }
                //Verify if count of materials is different of count of submeshes
                if (thisMeshFilter != null && thisMeshRenderer != null && thisMeshFilter.sharedMesh != null)
                    if (thisMeshFilter.sharedMesh.subMeshCount != thisMeshRenderer.sharedMaterials.Length)
                    {
                        logsOfBeforeMerge.Add(new LogOfMerge(MessageType.Error, "The Mesh Renderer component found in GameObject \"" + thisGameObject.name + "\" has more or less material needed. The mesh that is in this GameObject has " + thisMeshFilter.sharedMesh.subMeshCount.ToString() + " submeshes, but has a number of " + thisMeshRenderer.sharedMaterials.Length.ToString() + " materials. This mesh will be ignored during the merge process."));
                        canAddThisGameObjectToValidGameObjects = false;
                    }
                //Verify if has null materials in MeshRenderer
                if (thisMeshRenderer != null)
                {
                    for (int x = 0; x < thisMeshRenderer.sharedMaterials.Length; x++)
                        if (thisMeshRenderer.sharedMaterials[x] == null)
                        {
                            logsOfBeforeMerge.Add(new LogOfMerge(MessageType.Error, "Material " + x.ToString() + " in Mesh Renderer present in component \"" + thisGameObject.name + "\" is null. For the merge process to work well, all materials must be completed. This GameObject will be ignored in the merge process."));
                            canAddThisGameObjectToValidGameObjects = false;
                        }
                    //If this GameObjects contains more than 2 materials, add to the list of warning of many materials
                    if (thisMeshFilter != null && thisMeshRenderer != null && thisMeshFilter.sharedMesh != null && thisMeshFilter.sharedMesh.vertexCount > 1500 && thisMeshRenderer.sharedMaterials.Length > 2 && meshCombinerPreferences.lightmapSupport == true)
                    {
                        logsOfBeforeMerge.Add(new LogOfMerge(MessageType.Warning, "The mesh in GameObject \"" + thisGameObject.name + "\" contains many vertices and a large amount of materials. Due to a Unity limitation, you may experience a longer time to combine meshes using Lightmap Support, or you may experience errors during the merge process. If this happens, try reducing the amount of submeshes present in this mesh. If no problem occurs, do not worry, everything went as expected."));
                        haveWarningsForThisMesh = true;
                    }
                }
                //Verify if this gameobject is already merged
                if (thisGameObject.GetComponent<CombinedMeshesManager>() != null)
                {
                    logsOfBeforeMerge.Add(new LogOfMerge(MessageType.Error, "GameObject \"" + thisGameObject.name + "\" is the result of a previous merge, so it will be ignored by this merge."));
                    canAddThisGameObjectToValidGameObjects = false;
                }

                //If can add to valid GameObjects, add this gameobject to list of valid gameobjects selected
                if (canAddThisGameObjectToValidGameObjects == true)
                {
                    validsGameObjectsSelected.Add(new GameObjectWithMesh(thisGameObject, ((haveWarningsForThisMesh == true) ? GameObjectWithMeshValidation.ValidWithWarnings : GameObjectWithMeshValidation.Valid), thisMeshFilter, thisMeshRenderer));
                    verticesCountInValidGos += thisMeshFilter.sharedMesh.vertexCount;

                    //Incremente statistics of before
                    statisticsBeforeMerge.totalVertices += thisMeshFilter.sharedMesh.vertexCount;
                    statisticsBeforeMerge.meshesCount += thisMeshFilter.sharedMesh.subMeshCount;
                    statisticsBeforeMerge.drawCallsAproximate += thisMeshFilter.sharedMesh.subMeshCount;
                    statisticsBeforeMerge.optimizationRate = 0.0f;

                    //Incremente statistics of after
                    statisticsAfterMerge.totalVertices += thisMeshFilter.sharedMesh.vertexCount;
                }
                //If cannot add to valid gameobjects, add this gameobject to list of invalid gameobjects selected
                if (canAddThisGameObjectToValidGameObjects == false)
                    invalidsGameObjectsSelected.Add(new GameObjectWithMesh(thisGameObject, GameObjectWithMeshValidation.Invalid, thisMeshFilter, thisMeshRenderer));
            }
            ////-------------------- END OF COMPONENTS AND GAMEOBJECTS REAL VALIDATION ------------------------////

            //Calculate prediction statistics for after merge, according to merge method selected
            switch (meshCombinerPreferences.mergeMethod)
            {
                case MeshCombinerPreferences.MergeMethod.OneMeshPerMaterial:
                    //---> Start OneMeshPerMaterial
                    Dictionary<Material, bool> uniqueMaterialsOmpm = new Dictionary<Material, bool>();
                    foreach (GameObjectWithMesh obj in validsGameObjectsSelected)
                        for (int i = 0; i < obj.meshRenderer.sharedMaterials.Length; i++)
                            if (uniqueMaterialsOmpm.ContainsKey(obj.meshRenderer.sharedMaterials[i]) == false)
                                uniqueMaterialsOmpm.Add(obj.meshRenderer.sharedMaterials[i], true);
                    statisticsBeforeMerge.materialsCount = uniqueMaterialsOmpm.Keys.Count;
                    statisticsAfterMerge.materialsCount = uniqueMaterialsOmpm.Keys.Count;
                    statisticsAfterMerge.meshesCount = uniqueMaterialsOmpm.Keys.Count;
                    statisticsAfterMerge.drawCallsAproximate = uniqueMaterialsOmpm.Keys.Count;
                    statisticsAfterMerge.optimizationRate = (1 - ((float)uniqueMaterialsOmpm.Keys.Count / (float)statisticsBeforeMerge.meshesCount)) * (float)100;
                    //---> End OneMeshPerMaterial
                    break;
                case MeshCombinerPreferences.MergeMethod.AllInOne:
                    //---> Start AllInOne
                    Dictionary<Material, bool> uniqueMaterialsAio = new Dictionary<Material, bool>();
                    foreach (GameObjectWithMesh obj in validsGameObjectsSelected)
                        for (int i = 0; i < obj.meshRenderer.sharedMaterials.Length; i++)
                            if (uniqueMaterialsAio.ContainsKey(obj.meshRenderer.sharedMaterials[i]) == false)
                                uniqueMaterialsAio.Add(obj.meshRenderer.sharedMaterials[i], true);
                    float quantityOfAtlasFloat = (float)uniqueMaterialsAio.Keys.Count / (float)meshCombinerPreferences.allInOneParams.maxTexturesPerAtlas;
                    int quantityOfAtlasInt = (int)((float)uniqueMaterialsAio.Keys.Count / (float)meshCombinerPreferences.allInOneParams.maxTexturesPerAtlas);
                    if (quantityOfAtlasFloat > quantityOfAtlasInt)
                        quantityOfAtlasInt += 1;
                    statisticsBeforeMerge.materialsCount = uniqueMaterialsAio.Keys.Count;
                    statisticsAfterMerge.materialsCount = quantityOfAtlasInt;
                    statisticsAfterMerge.meshesCount = quantityOfAtlasInt;
                    statisticsAfterMerge.drawCallsAproximate = quantityOfAtlasInt;
                    statisticsAfterMerge.optimizationRate = (1 - ((float)statisticsAfterMerge.meshesCount / (float)statisticsBeforeMerge.meshesCount)) * (float)100;
                    //---> End AllInOne
                    break;
                case MeshCombinerPreferences.MergeMethod.JustMaterialColors:
                    //---> Start JustMaterialColors
                    Dictionary<Material, bool> uniqueMaterialsJmc = new Dictionary<Material, bool>();
                    foreach (GameObjectWithMesh obj in validsGameObjectsSelected)
                        for (int i = 0; i < obj.meshRenderer.sharedMaterials.Length; i++)
                            if (uniqueMaterialsJmc.ContainsKey(obj.meshRenderer.sharedMaterials[i]) == false)
                                uniqueMaterialsJmc.Add(obj.meshRenderer.sharedMaterials[i], true);
                    statisticsBeforeMerge.materialsCount = uniqueMaterialsJmc.Keys.Count;
                    statisticsAfterMerge.materialsCount = 1;
                    statisticsAfterMerge.meshesCount = 1;
                    statisticsAfterMerge.drawCallsAproximate = 1;
                    statisticsAfterMerge.optimizationRate = (1 - ((float)statisticsAfterMerge.meshesCount / (float)statisticsBeforeMerge.meshesCount)) * (float)100;
                    //---> End JustMaterialColors
                    break;
            }

            //Check if all merge params is valids
            if (meshCombinerPreferences.mergeMethod == MeshCombinerPreferences.MergeMethod.OneMeshPerMaterial)
                isAllMergeParamsValids = true;
            if (meshCombinerPreferences.mergeMethod == MeshCombinerPreferences.MergeMethod.AllInOne && meshCombinerPreferences.allInOneParams.materialToUse == null)
                isAllMergeParamsValids = false;
            if (meshCombinerPreferences.mergeMethod == MeshCombinerPreferences.MergeMethod.AllInOne && meshCombinerPreferences.allInOneParams.materialToUse != null)
                isAllMergeParamsValids = true;
            if (meshCombinerPreferences.mergeMethod == MeshCombinerPreferences.MergeMethod.JustMaterialColors && meshCombinerPreferences.justMaterialColorsParams.materialToUse == null)
                isAllMergeParamsValids = false;
            if (meshCombinerPreferences.mergeMethod == MeshCombinerPreferences.MergeMethod.JustMaterialColors && meshCombinerPreferences.justMaterialColorsParams.materialToUse != null)
                isAllMergeParamsValids = true;
            if (isAllMergeParamsValids == false)
                logsOfBeforeMerge.Add(new LogOfMerge(MessageType.Error, "There are invalid merge parameters in the \"Preferences of Merge\" box. Please check all parameters and make sure that they are valid so that it is possible to perform the merge."));
            //Show conditional warning about highlight uv vertices
            if (meshCombinerPreferences.mergeMethod == MeshCombinerPreferences.MergeMethod.AllInOne && meshCombinerPreferences.allInOneParams.highlightUvVertices == true)
                logsOfBeforeMerge.Add(new LogOfMerge(MessageType.Warning, "WARNING: The \"Highlight UV Vertices\" option is activated. This can GREATLY increase the processing and creation time of the combined mesh."));
            //Show conditional warnings about lightmaps, if necessary
            if (meshCombinerPreferences.lightmapSupport == true)
                logsOfBeforeMerge.Add(new LogOfMerge(MessageType.Warning, "WARNING: Support for Lightmaps has been enabled. Easy Mesh Combiner will process the meshes and generate a UV so that the mesh resulting from the merge supports Lightmaps.\nThe processing of Lightmaps can considerably increase the processing time of the merge."));
            if (meshCombinerPreferences.lightmapSupport == true && statisticsBeforeMerge.totalVertices >= MAX_VERTICES_FOR_16BITS_MESH)
                logsOfBeforeMerge.Add(new LogOfMerge(MessageType.Warning, "WARNING: The mesh resulting from the merge will have more than " + MAX_VERTICES_FOR_16BITS_MESH + " vertices, and \"Lightmap Support\" is enabled. You can generate combined meshes with more than " + MAX_VERTICES_FOR_16BITS_MESH + " vertices without problems, but generating Lightmap Support for combined meshes with more than " + MAX_VERTICES_FOR_16BITS_MESH + " vertices can cause problems during the merge. Reduce the number of selected meshes until the vertex counter is below " + MAX_VERTICES_FOR_16BITS_MESH + " or turn off the \"Lightmaps Support\" option to avoid problems.\n\nTIP: Use the \"Deselect Excessive\" button beside, Easy Mesh Combiner will deselect all excess GameObjects automatically.\n\nNOTE: The value of " + MAX_VERTICES_FOR_16BITS_MESH + " vertices is just a recommended number to keep below if you use the \"Lightmaps Support\" option."));

            //Verify if can start merge
            if (validsGameObjectsSelected.Count == 0)
                logsOfBeforeMerge.Add(new LogOfMerge(MessageType.Warning, "Cannot start a merge as there are no valid meshes in the selected GameObjects. Please select GameObjects that contain valid meshes so that the merge can be done. For the merge process to be done, there must be at least 1 valid and active mesh and found in your selection."));
            //Update the scene GUI
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.Repaint();
        }

        private void DeselectExcessiveMeshesAt64kVertex()
        {
            //This method will deselect all excessive meshes and remain only gameobjects with total of 63999 vertices or smaller

            //If not have items selected, cancel
            if (validsGameObjectsSelected.Count == 0)
            {
                EditorUtility.DisplayDialog("Oops!", "Please select at least one Valid GameObject for this function to work!", "Ok!");
                return;
            }
            //Show the warning, cancel this method if the response is "No"
            if (EditorUtility.DisplayDialog("Continue?", "This button has the function of Deselecting all GameObjects that exceed the limit of " + MAX_VERTICES_FOR_16BITS_MESH + " vertices, recommended by Easy Mesh Combiner when combining meshes with \"Lightmaps Support\" enabled.\nThis is useful to avoid merging problems or LONG processing times.\n\nNOTHING will be done with the GameObjects or Meshes of your scene, this function will only Deselect the excess GameObjects, until only GameObjects that have their vertices total sum, to a maximum of " + MAX_VERTICES_FOR_16BITS_MESH + " vertices remain.\n\nThe \"Combine Childrens\" option will also be disabled to prevent GameObjects children from being selected without your desire! You can reactivate it later if you want! Do you wish to continue?", "Yes! Go Ahead!", "No") == false)
                return;
            //Disable the "Combine Children" option
            meshCombinerPreferences.combineChildrens = false;

            //Create a dictionary with all valid GameObjects selected
            Dictionary<GameObject, int> allValidsGameObjectsAndRespectiveVerticesCount = new Dictionary<GameObject, int>();

            //Fill the dictionary
            foreach (GameObjectWithMesh obj in validsGameObjectsSelected)
                if (allValidsGameObjectsAndRespectiveVerticesCount.ContainsKey(obj.gameObject) == false)
                    allValidsGameObjectsAndRespectiveVerticesCount.Add(obj.gameObject, obj.meshFilter.sharedMesh.vertexCount);

            //Order the dictionary by bigger to smaller count of vertices
            allValidsGameObjectsAndRespectiveVerticesCount = allValidsGameObjectsAndRespectiveVerticesCount.OrderBy(key => key.Value).ToDictionary(item => item.Key, item => item.Value);

            //Create a new dictionary of items to select, that not pass the max vertices number
            List<GameObject> gameObjectsThatWillMakeTheSelection = new List<GameObject>();
            int totalVerticesOfSelection = 0;

            //Fill the list
            foreach (var item in allValidsGameObjectsAndRespectiveVerticesCount)
            {
                //If the total of vertices will pass the recommended number, break the loop
                if ((totalVerticesOfSelection + item.Value) > MAX_VERTICES_FOR_16BITS_MESH)
                    break;

                //Add this item to list
                gameObjectsThatWillMakeTheSelection.Add(item.Key);

                //Add the vertices count
                totalVerticesOfSelection += item.Value;
            }

            //Select all items
            Selection.objects = gameObjectsThatWillMakeTheSelection.ToArray();
        }

        private string DrawDropDownOfProperties(string title, string tooltip, string defaultValue, string defaultValueSuffix, string currentSelected, Dictionary<string, string> allPropertiesToShow)
        {
            //Prepare the options formatation to show, and a copy list, that contains only the name of property
            List<string> allOptions = new List<string>();
            List<string> allOptionsFormated = new List<string>();
            allOptions.Add(defaultValue);
            allOptionsFormated.Add(defaultValue + " " + defaultValueSuffix);
            foreach (var entry in allPropertiesToShow)
            {
                if (entry.Key != defaultValue)
                {
                    allOptions.Add(entry.Key);
                    allOptionsFormated.Add(entry.Key + " (" + entry.Value + " Property)");
                }
            }

            //Identify the ID of property name that is using at moment, and show as select at moment, in enum
            int selected = 0;
            for (int i = 0; i < allOptions.Count; i++)
            {
                if (allOptions[i] == currentSelected)
                {
                    selected = i;
                    break;
                }
            }

            //Show a enum with all properties formatted and return the propertie name only, of the selected
            return allOptions[EditorGUILayout.Popup(new GUIContent(title, tooltip), selected, allOptionsFormated.ToArray())];
        }
        #endregion

        private static void LoadThePreferences(MeshCombinerTool instance)
        {
            //Create the default directory, if not exists
            if (!AssetDatabase.IsValidFolder("Assets/ThirdParty/MT Assets/_AssetsData"))
                AssetDatabase.CreateFolder("Assets/ThirdParty/MT Assets", "_AssetsData");
            if (!AssetDatabase.IsValidFolder("Assets/ThirdParty/MT Assets/_AssetsData/Preferences"))
                AssetDatabase.CreateFolder("Assets/ThirdParty/MT Assets/_AssetsData", "Preferences");

            //Try to load the preferences file
            meshCombinerPreferences = (MeshCombinerPreferences)AssetDatabase.LoadAssetAtPath("Assets/ThirdParty/MT Assets/_AssetsData/Preferences/EasyMeshCombiner.asset", typeof(MeshCombinerPreferences));
            //Validate the preference file. if this preference file is of another project, delete then
            if (meshCombinerPreferences != null)
            {
                if (meshCombinerPreferences.projectName != Application.productName)
                {
                    AssetDatabase.DeleteAsset("Assets/ThirdParty/MT Assets/_AssetsData/Preferences/EasyMeshCombiner.asset");
                    meshCombinerPreferences = null;
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
                if (meshCombinerPreferences != null && meshCombinerPreferences.projectName == Application.productName)
                {
                    //Set the position of Window 
                    instance.position = meshCombinerPreferences.windowPosition;
                }
            }
            //If null, create and save a preferences file
            if (meshCombinerPreferences == null)
            {
                meshCombinerPreferences = ScriptableObject.CreateInstance<MeshCombinerPreferences>();
                meshCombinerPreferences.projectName = Application.productName;
                AssetDatabase.CreateAsset(meshCombinerPreferences, "Assets/ThirdParty/MT Assets/_AssetsData/Preferences/EasyMeshCombiner.asset");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static void SaveThePreferences(MeshCombinerTool instance)
        {
            //Save the preferences in Prefs.asset
            meshCombinerPreferences.projectName = Application.productName;
            meshCombinerPreferences.windowPosition = new Rect(instance.position.x, instance.position.y, instance.position.width, instance.position.height);
            EditorUtility.SetDirty(meshCombinerPreferences);
            AssetDatabase.SaveAssets();
        }

        //Core methods

        public void DoCombineMeshes_OneMeshPerMaterial()
        {
            //Show progress bar
            ShowProgressBar("Merging...", true, 1.0f);

            //Create the holder GameObject
            GameObject holderGameObject = new GameObject(meshCombinerPreferences.nameOfThisMerge);
            CombinedMeshesManager holderManager = holderGameObject.AddComponent<CombinedMeshesManager>();
            MeshFilter holderMeshFilter = holderGameObject.AddComponent<MeshFilter>();
            MeshRenderer holderMeshRenderer = holderGameObject.AddComponent<MeshRenderer>();
            if (meshCombinerPreferences.lightmapSupport == false)
                GameObjectUtility.SetStaticEditorFlags(holderGameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.NavigationStatic);
            if (meshCombinerPreferences.lightmapSupport == true)
                GameObjectUtility.SetStaticEditorFlags(holderGameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.NavigationStatic | StaticEditorFlags.ContributeGI);
            holderGameObject.transform.SetParent(bestParentTransformForGameObjectResultOfMerge);
            holderGameObject.transform.SetSiblingIndex(bestSibilingForGameObjectResultOfMerge);

            //Allocate space for a list that stores reference for ALL gameobjects that have meshes that was processed in this merge
            List<MeshRenderer> listOfAllMeshesThatWasProcessedsInThisMerge = new List<MeshRenderer>();

            //Store the time of start of merge
            DateTime startingTime = DateTime.Now;

            //------------------------------- START OF MERGE CODE --------------------------------

            //Allocate a list to store all materials and yours respective submeshes that use each
            Dictionary<Material, List<SubMeshToCombine>> subMeshesPerMaterial = new Dictionary<Material, List<SubMeshToCombine>>();

            //Separate each submesh of all valid GameObjects find according to your material
            for (int i = 0; i < validsGameObjectsSelected.Count; i++)
            {
                GameObjectWithMesh thisGoWithMesh = validsGameObjectsSelected[i];
                for (int x = 0; x < thisGoWithMesh.meshFilter.sharedMesh.subMeshCount; x++)
                {
                    Material currentMaterial = thisGoWithMesh.meshRenderer.sharedMaterials[x];
                    if (subMeshesPerMaterial.ContainsKey(currentMaterial) == true)
                        subMeshesPerMaterial[currentMaterial].Add(new SubMeshToCombine(thisGoWithMesh.gameObject.transform, thisGoWithMesh.meshFilter, thisGoWithMesh.meshRenderer, x));
                    if (subMeshesPerMaterial.ContainsKey(currentMaterial) == false)
                        subMeshesPerMaterial.Add(currentMaterial, new List<SubMeshToCombine>() { new SubMeshToCombine(thisGoWithMesh.gameObject.transform, thisGoWithMesh.meshFilter, thisGoWithMesh.meshRenderer, x) });
                }

                //Add this mesh to list of all meshes that was processed in this merge
                listOfAllMeshesThatWasProcessedsInThisMerge.Add(thisGoWithMesh.meshRenderer);
            }

            //Combine the submeshes into one submesh according the material
            List<Mesh> combinedSubmehesPerMaterial = new List<Mesh>();
            foreach (var key in subMeshesPerMaterial)
            {
                //Get the submeshes to merge, of current material
                List<SubMeshToCombine> subMeshesOfCurrentMaterial = key.Value;

                //Combine instances of submeshes from this material
                List<CombineInstance> combineInstancesOfCurrentMaterial = new List<CombineInstance>();

                //Count of vertices for all submeshes of this material
                int totalVerticesCount = 0;

                //Process each submesh
                for (int i = 0; i < subMeshesOfCurrentMaterial.Count; i++)
                {
                    CombineInstance combineInstance = new CombineInstance();
                    combineInstance.mesh = subMeshesOfCurrentMaterial[i].meshFilter.sharedMesh;
                    combineInstance.subMeshIndex = subMeshesOfCurrentMaterial[i].subMeshIndex;
                    combineInstance.transform = subMeshesOfCurrentMaterial[i].transform.localToWorldMatrix;
                    combineInstancesOfCurrentMaterial.Add(combineInstance);
                    totalVerticesCount += combineInstance.mesh.vertexCount;
                }

                //Create the submesh with all submeshes with current material, and set limitation of vertices
                Mesh mesh = new Mesh();
                if (totalVerticesCount <= MAX_VERTICES_FOR_16BITS_MESH)
                    mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
                if (totalVerticesCount > MAX_VERTICES_FOR_16BITS_MESH)
                    mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.CombineMeshes(combineInstancesOfCurrentMaterial.ToArray(), true, true, meshCombinerPreferences.lightmapSupport);

                //Add to list of combined submeshes per material
                combinedSubmehesPerMaterial.Add(mesh);
            }

            //Process each combined submeshes per material, creating final combine instances
            List<CombineInstance> finalCombineInstances = new List<CombineInstance>();
            int totalFinalVerticesCount = 0;
            foreach (Mesh mesh in combinedSubmehesPerMaterial)
            {
                CombineInstance combineInstanceOfThisSubMesh = new CombineInstance();
                combineInstanceOfThisSubMesh.mesh = mesh;
                combineInstanceOfThisSubMesh.subMeshIndex = 0;
                combineInstanceOfThisSubMesh.transform = Matrix4x4.identity;
                finalCombineInstances.Add(combineInstanceOfThisSubMesh);
                totalFinalVerticesCount += combineInstanceOfThisSubMesh.mesh.vertexCount;
            }

            //Create the final mesh that contains all submeshes divided per material
            Mesh finalMesh = new Mesh();
            if (totalFinalVerticesCount <= MAX_VERTICES_FOR_16BITS_MESH)
                finalMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
            if (totalFinalVerticesCount > MAX_VERTICES_FOR_16BITS_MESH)
                finalMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            finalMesh.CombineMeshes(finalCombineInstances.ToArray(), false);
            finalMesh.RecalculateBounds();
            if (meshCombinerPreferences.lightmapSupport == true)
                Unwrapping.GenerateSecondaryUVSet(finalMesh);

            //Polulate the holder GameObject with the data of combined mesh
            holderMeshFilter.sharedMesh = finalMesh;
            List<Material> materialsForSubMeshes = new List<Material>();
            foreach (var key in subMeshesPerMaterial)
                materialsForSubMeshes.Add(key.Key);
            holderMeshRenderer.sharedMaterials = materialsForSubMeshes.ToArray();

            //Add the MeshCollider if is desired
            if (meshCombinerPreferences.oneMeshPerMaterialParams.addMeshCollider == true)
                holderGameObject.AddComponent<MeshCollider>();

            //-------------------------------- END OF MERGE CODE ---------------------------------

            //Set scene dirty and refresh asset data
            AssetDatabase.Refresh();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            //Select and ping the gameobject of merge
            Selection.activeGameObject = holderGameObject;
            EditorGUIUtility.PingObject(holderGameObject);

            //Save the mesh of merge in assets, if desired
            if (meshCombinerPreferences.saveMeshInAssets == true)
            {
                ShowProgressBar("Saving Generated Mesh...", true, 1.0f);
                string generatedMeshPath = SaveAssetAsFile("Meshes", holderMeshFilter.sharedMesh, meshCombinerPreferences.nameOfThisMerge, "asset");
                holderManager.pathsAndTypesOfAssetsOfThisMerge.Add(new CombinedMeshesManager.PathAndTypeOfAAsset(CombinedMeshesManager.AssetType.Mesh, generatedMeshPath));
            }

            //Run last steps of merge
            ShowProgressBar("Finishing Merge...", true, 1.0f);
            //Do the desired action to all meshes processed by this merge. For example, disable all original GameObjects (save the originals stats of all GameObjects in the manager too)
            DoTheSelectedActionAfterMerge(holderManager, listOfAllMeshesThatWasProcessedsInThisMerge.ToArray());
            //Feed the CombinedMeshesManager in the holder GameObject with all needed data, to be possible a management of merge
            if (meshCombinerPreferences.mergeMethod == MeshCombinerPreferences.MergeMethod.OneMeshPerMaterial)
                holderManager.mergeMethodUsed = CombinedMeshesManager.MergeMethod.OneMeshPerMaterial;
            if (meshCombinerPreferences.mergeMethod == MeshCombinerPreferences.MergeMethod.AllInOne)
                holderManager.mergeMethodUsed = CombinedMeshesManager.MergeMethod.AllInOne;
            if (meshCombinerPreferences.mergeMethod == MeshCombinerPreferences.MergeMethod.JustMaterialColors)
                holderManager.mergeMethodUsed = CombinedMeshesManager.MergeMethod.JustMaterialColors;
            //Save the undo method to use, if user wish
            if (meshCombinerPreferences.afterMerge == MeshCombinerPreferences.AfterMerge.DeactiveOriginalGameObjects)
                holderManager.undoMethod = CombinedMeshesManager.UndoMethod.ReactiveOriginalGameObjects;
            if (meshCombinerPreferences.afterMerge == MeshCombinerPreferences.AfterMerge.DisableOriginalMeshes)
                holderManager.undoMethod = CombinedMeshesManager.UndoMethod.EnableOriginalMeshes;
            if (meshCombinerPreferences.afterMerge == MeshCombinerPreferences.AfterMerge.DoNothing)
                holderManager.undoMethod = CombinedMeshesManager.UndoMethod.DoNothing;
            //Save information if the GameObject of merge is a prefab
            holderManager.thisIsPrefab = meshCombinerPreferences.savePrefabOfThis;

            //Save a prefab, if is desired
            if (meshCombinerPreferences.savePrefabOfThis == true)
                SaveMergeAsPrefab(meshCombinerPreferences.prefabName, holderGameObject);

            //Get finishing time of merge
            DateTime finishingTime = DateTime.Now;
            //Calculate the difference between starting and finishing time
            TimeSpan processingTime = finishingTime - startingTime;

            //Hide progress bar
            ShowProgressBar("", false, 0.0f);

            //Add the log
            logsOfBeforeMerge.Add(new LogOfMerge(MessageType.Info, "The merge has been successfully completed! See merge statistics in the box below.\n\nProcessed in " + processingTime.Minutes + " minutes and " + processingTime.Seconds + " seconds."));

            //Report that the merge is ended
            mergeIsDone = true;
        }

        public void DoCombineMeshes_AllInOne()
        {
            //Show progress bar
            ShowProgressBar("Merging...", true, 1.0f);

            //Create the holder GameObject
            GameObject holderGameObject = new GameObject(meshCombinerPreferences.nameOfThisMerge);
            CombinedMeshesManager holderManager = holderGameObject.AddComponent<CombinedMeshesManager>();
            MeshFilter holderMeshFilter = holderGameObject.AddComponent<MeshFilter>();
            MeshRenderer holderMeshRenderer = holderGameObject.AddComponent<MeshRenderer>();
            if (meshCombinerPreferences.lightmapSupport == false)
                GameObjectUtility.SetStaticEditorFlags(holderGameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.NavigationStatic);
            if (meshCombinerPreferences.lightmapSupport == true)
                GameObjectUtility.SetStaticEditorFlags(holderGameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.NavigationStatic | StaticEditorFlags.ContributeGI);
            holderGameObject.transform.SetParent(bestParentTransformForGameObjectResultOfMerge);
            holderGameObject.transform.SetSiblingIndex(bestSibilingForGameObjectResultOfMerge);

            //Allocate space for a list that stores reference for ALL gameobjects that have meshes that was processed in this merge
            List<MeshRenderer> listOfAllMeshesThatWasProcessedsInThisMerge = new List<MeshRenderer>();

            //Store the time of start of merge
            DateTime startingTime = DateTime.Now;

            //------------------------------- START OF MERGE CODE --------------------------------

            //Allocate space to store a value for max steps needed to finish the merge
            float stepsNeededToFinishTheMerge = 5; //<-- 1 step is the merging proccess, 2 step is enable RW in all textures that will be used, 3 step is to restore original state of RW in all textures, 4 step is the merging proccess, 5 for none
            float stepsElapsedUntilHereToFinishTheMerge = 0;
            //Update progress bar
            ShowProgressBar("Reading Meshes...", true, stepsElapsedUntilHereToFinishTheMerge / stepsNeededToFinishTheMerge);

            //Allocate lists to separete meshes without tilling and meshes with tiling
            List<TemporarySplitedSubMesh> texturesAndSubMeshesWithTilings = new List<TemporarySplitedSubMesh>();
            List<TemporarySplitedSubMesh> texturesAndSubMeshesWithoutTilings = new List<TemporarySplitedSubMesh>();
            int totalVerticesCount = 0;

            //Fill the two lists separating into groups, the tiled meshes and untiled meshes
            foreach (GameObjectWithMesh obj in validsGameObjectsSelected)
                for (int i = 0; i < obj.meshFilter.sharedMesh.subMeshCount; i++)
                {
                    //Get UV of this mesh
                    Vector2[] uvOfThisSubMesh = obj.meshFilter.sharedMesh.EmcGetSubmesh(i).uv;
                    //If this is a tiling mesh
                    if (isTiledTexture(uvOfThisSubMesh, obj.meshRenderer.sharedMaterials[i]) == true)
                    {
                        //If is desired to skip all tiled textures, skip this
                        if (meshCombinerPreferences.allInOneParams.mergeTiledTextures == MeshCombinerPreferences.MergeTiledTextures.SkipAll)
                            continue;

                        //Create the CombineInstance for this submesh
                        CombineInstance combineInstance = new CombineInstance();
                        combineInstance.mesh = obj.meshFilter.sharedMesh;
                        combineInstance.subMeshIndex = i;
                        combineInstance.transform = obj.gameObject.transform.localToWorldMatrix;

                        //Add to vertices count
                        totalVerticesCount += uvOfThisSubMesh.Length;

                        //Create the temporary storage for store combine instance of this mesh, and add to list
                        TemporarySplitedSubMesh submesh = new TemporarySplitedSubMesh();
                        submesh.combineInstance = combineInstance;
                        submesh.uvMap = uvOfThisSubMesh;
                        submesh.material = obj.meshRenderer.sharedMaterials[i];
                        texturesAndSubMeshesWithTilings.Add(submesh);

                        //Add this mesh to list of meshes readed
                        if (listOfAllMeshesThatWasProcessedsInThisMerge.Contains(obj.meshRenderer) == false)
                            listOfAllMeshesThatWasProcessedsInThisMerge.Add(obj.meshRenderer);
                    }
                    //If this is not a tiling mesh
                    if (isTiledTexture(uvOfThisSubMesh, obj.meshRenderer.sharedMaterials[i]) == false)
                    {
                        //Create the CombineInstance for this submesh
                        CombineInstance combineInstance = new CombineInstance();
                        combineInstance.mesh = obj.meshFilter.sharedMesh;
                        combineInstance.subMeshIndex = i;
                        combineInstance.transform = obj.gameObject.transform.localToWorldMatrix;

                        //Add to vertices count
                        totalVerticesCount += uvOfThisSubMesh.Length;

                        //Create the temporary storage for store combine instance of this mesh, and add to list
                        TemporarySplitedSubMesh submesh = new TemporarySplitedSubMesh();
                        submesh.combineInstance = combineInstance;
                        submesh.uvMap = uvOfThisSubMesh;
                        submesh.material = obj.meshRenderer.sharedMaterials[i];
                        texturesAndSubMeshesWithoutTilings.Add(submesh);

                        //Add this mesh to list of meshes readed
                        if (listOfAllMeshesThatWasProcessedsInThisMerge.Contains(obj.meshRenderer) == false)
                            listOfAllMeshesThatWasProcessedsInThisMerge.Add(obj.meshRenderer);
                    }
                }

            //Check all textures that will be used and separate all in a list for prepare them to be used in this merge
            List<Material> allMaterialsThatWillBeUsedOfTiledMeshes = new List<Material>();
            List<Material> allMaterialsThatWillBeUsedOfNonTiledMeshes = new List<Material>();
            List<Material> allMaterialsThatWillBeUsed = new List<Material>();
            foreach (TemporarySplitedSubMesh item in texturesAndSubMeshesWithTilings)
                if (allMaterialsThatWillBeUsedOfTiledMeshes.Contains(item.material) == false)
                    allMaterialsThatWillBeUsedOfTiledMeshes.Add(item.material);
            foreach (TemporarySplitedSubMesh item in texturesAndSubMeshesWithoutTilings)
                if (allMaterialsThatWillBeUsedOfNonTiledMeshes.Contains(item.material) == false)
                    allMaterialsThatWillBeUsedOfNonTiledMeshes.Add(item.material);
            foreach (Material material in allMaterialsThatWillBeUsedOfTiledMeshes)
                allMaterialsThatWillBeUsed.Add(material);
            foreach (Material material in allMaterialsThatWillBeUsedOfNonTiledMeshes)
                allMaterialsThatWillBeUsed.Add(material);
            Dictionary<Texture2D, bool> allTexturesThatWillBeUsedAndIfRWIsEnabled = new Dictionary<Texture2D, bool>();
            allTexturesThatWillBeUsedAndIfRWIsEnabled = ExtractReferenceOfAllTexturesThatWillBeUsed(allMaterialsThatWillBeUsed.ToArray());
            //Store the quantity of textures that will be processed and the textures processed until here
            int quantityOfTexturesToBeProcessed = GetCountOfStepsNeededToProcessEachTextureInThisMerge(allMaterialsThatWillBeUsed.Count);
            int quantityOfTexturesProcessedsAtHere = 0;
            stepsNeededToFinishTheMerge += quantityOfTexturesToBeProcessed;
            //Enable RW for all textures that will be used in this merge
            stepsElapsedUntilHereToFinishTheMerge += 1;
            ShowProgressBar("Preparing Textures...", true, stepsElapsedUntilHereToFinishTheMerge / stepsNeededToFinishTheMerge);
            EnableReadWriteForAllTexturesThatWillBeUsedInThisMerge(allTexturesThatWillBeUsedAndIfRWIsEnabled, stepsElapsedUntilHereToFinishTheMerge / stepsNeededToFinishTheMerge);

            //Allocate a space to save all submeshes for the Final Merged Mesh
            Dictionary<Material, Mesh> submeshesOfFinalMergedMesh = new Dictionary<Material, Mesh>();
            List<Texture2D> allTextures2dOfFinalMergedMesh = new List<Texture2D>();

            //-------- PROCESSING TILED MESHES

            //Groups all submeshes of texturesAndSubMeshesWithTilings according to material that each submeshes use
            Dictionary<Material, List<CombineInstance>> tiledSubmeshesByMaterial = new Dictionary<Material, List<CombineInstance>>();
            foreach (TemporarySplitedSubMesh item in texturesAndSubMeshesWithTilings)
            {
                if (tiledSubmeshesByMaterial.ContainsKey(item.material) == true)
                    tiledSubmeshesByMaterial[item.material].Add(item.combineInstance);
                if (tiledSubmeshesByMaterial.ContainsKey(item.material) == false)
                    tiledSubmeshesByMaterial.Add(item.material, new List<CombineInstance>() { item.combineInstance });
            }

            //Combine all submeshes that used the same Material, into a unique submesh for each material, and add each submesh into list of final submeshes
            foreach (var item in tiledSubmeshesByMaterial)
            {
                //Update the progress
                stepsElapsedUntilHereToFinishTheMerge += 1;
                ShowProgressBar("Merging Tiled Meshes...", true, stepsElapsedUntilHereToFinishTheMerge / stepsNeededToFinishTheMerge);

                //Create the submesh with all submeshes that uses this material
                Mesh submesh = new Mesh();
                if (totalVerticesCount <= MAX_VERTICES_FOR_16BITS_MESH)
                    submesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
                if (totalVerticesCount > MAX_VERTICES_FOR_16BITS_MESH)
                    submesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                submesh.CombineMeshes(item.Value.ToArray(), true, true, meshCombinerPreferences.lightmapSupport);

                //Create a material for this submesh, using the original material as base
                Material material = GetValidatedCopyOfMaterial(item.Key, true, true);

                //Calculate and get original resolution of main texture of this material
                Texture2D mainTextureOfThisMaterial = (Texture2D)item.Key.GetTexture(meshCombinerPreferences.allInOneParams.mainTexturePropertyToFind);
                Vector2Int mainTextureSize = Vector2Int.zero;
                Vector2Int mainTextureSizeWithEdges = Vector2Int.zero;
                if (mainTextureOfThisMaterial == null)
                    mainTextureSize = new Vector2Int(64, 64);
                if (mainTextureOfThisMaterial != null)
                    mainTextureSize = new Vector2Int(mainTextureOfThisMaterial.width, mainTextureOfThisMaterial.height);
                mainTextureSizeWithEdges = new Vector2Int(mainTextureSize.x + (GetEdgesSizeForTextures() * 2), mainTextureSize.y + (GetEdgesSizeForTextures() * 2));

                //Get validated copyies of all textures of original material for this new material
                stepsElapsedUntilHereToFinishTheMerge += 1;
                quantityOfTexturesProcessedsAtHere += 1;
                Texture2D mainTexture = GetSimpleCopyOfTexture(item.Key, meshCombinerPreferences.allInOneParams.mainTexturePropertyToFind, mainTextureSizeWithEdges.x, mainTextureSizeWithEdges.y, TextureType.MainTexture, stepsElapsedUntilHereToFinishTheMerge / stepsNeededToFinishTheMerge, quantityOfTexturesProcessedsAtHere, quantityOfTexturesToBeProcessed);
                ApplyAtlasInPropertyOfMaterial(material, meshCombinerPreferences.allInOneParams.mainTexturePropertyToInsert, mainTexture);
                allTextures2dOfFinalMergedMesh.Add(mainTexture);
                if (meshCombinerPreferences.allInOneParams.metallicMapSupport == true)
                {
                    stepsElapsedUntilHereToFinishTheMerge += 1;
                    quantityOfTexturesProcessedsAtHere += 1;
                    Texture2D map = GetSimpleCopyOfTexture(item.Key, meshCombinerPreferences.allInOneParams.metallicMapPropertyToFind, mainTextureSizeWithEdges.x, mainTextureSizeWithEdges.y, TextureType.MetallicMap, stepsElapsedUntilHereToFinishTheMerge / stepsNeededToFinishTheMerge, quantityOfTexturesProcessedsAtHere, quantityOfTexturesToBeProcessed);
                    ApplyAtlasInPropertyOfMaterial(material, meshCombinerPreferences.allInOneParams.metallicMapPropertyToInsert, map);
                    allTextures2dOfFinalMergedMesh.Add(map);
                }
                if (meshCombinerPreferences.allInOneParams.specularMapSupport == true)
                {
                    stepsElapsedUntilHereToFinishTheMerge += 1;
                    quantityOfTexturesProcessedsAtHere += 1;
                    Texture2D map = GetSimpleCopyOfTexture(item.Key, meshCombinerPreferences.allInOneParams.specularMapPropertyToFind, mainTextureSizeWithEdges.x, mainTextureSizeWithEdges.y, TextureType.SpecularMap, stepsElapsedUntilHereToFinishTheMerge / stepsNeededToFinishTheMerge, quantityOfTexturesProcessedsAtHere, quantityOfTexturesToBeProcessed);
                    ApplyAtlasInPropertyOfMaterial(material, meshCombinerPreferences.allInOneParams.specularMapPropertyToInsert, map);
                    allTextures2dOfFinalMergedMesh.Add(map);
                }
                if (meshCombinerPreferences.allInOneParams.normalMapSupport == true)
                {
                    stepsElapsedUntilHereToFinishTheMerge += 1;
                    quantityOfTexturesProcessedsAtHere += 1;
                    Texture2D map = GetSimpleCopyOfTexture(item.Key, meshCombinerPreferences.allInOneParams.normalMapPropertyToFind, mainTextureSizeWithEdges.x, mainTextureSizeWithEdges.y, TextureType.NormalMap, stepsElapsedUntilHereToFinishTheMerge / stepsNeededToFinishTheMerge, quantityOfTexturesProcessedsAtHere, quantityOfTexturesToBeProcessed);
                    ApplyAtlasInPropertyOfMaterial(material, meshCombinerPreferences.allInOneParams.normalMapPropertyToInsert, map);
                    allTextures2dOfFinalMergedMesh.Add(map);
                }
                if (meshCombinerPreferences.allInOneParams.normalMap2Support == true)
                {
                    stepsElapsedUntilHereToFinishTheMerge += 1;
                    quantityOfTexturesProcessedsAtHere += 1;
                    Texture2D map = GetSimpleCopyOfTexture(item.Key, meshCombinerPreferences.allInOneParams.normalMap2PropertyFind, mainTextureSizeWithEdges.x, mainTextureSizeWithEdges.y, TextureType.NormalMap, stepsElapsedUntilHereToFinishTheMerge / stepsNeededToFinishTheMerge, quantityOfTexturesProcessedsAtHere, quantityOfTexturesToBeProcessed);
                    ApplyAtlasInPropertyOfMaterial(material, meshCombinerPreferences.allInOneParams.normalMap2PropertyToInsert, map);
                    allTextures2dOfFinalMergedMesh.Add(map);
                }
                if (meshCombinerPreferences.allInOneParams.heightMapSupport == true)
                {
                    stepsElapsedUntilHereToFinishTheMerge += 1;
                    quantityOfTexturesProcessedsAtHere += 1;
                    Texture2D map = GetSimpleCopyOfTexture(item.Key, meshCombinerPreferences.allInOneParams.heightMapPropertyToFind, mainTextureSizeWithEdges.x, mainTextureSizeWithEdges.y, TextureType.HeightMap, stepsElapsedUntilHereToFinishTheMerge / stepsNeededToFinishTheMerge, quantityOfTexturesProcessedsAtHere, quantityOfTexturesToBeProcessed);
                    ApplyAtlasInPropertyOfMaterial(material, meshCombinerPreferences.allInOneParams.heightMapPropertyToInsert, map);
                    allTextures2dOfFinalMergedMesh.Add(map);
                }
                if (meshCombinerPreferences.allInOneParams.occlusionMapSupport == true)
                {
                    stepsElapsedUntilHereToFinishTheMerge += 1;
                    quantityOfTexturesProcessedsAtHere += 1;
                    Texture2D map = GetSimpleCopyOfTexture(item.Key, meshCombinerPreferences.allInOneParams.occlusionMapPropertyToFind, mainTextureSizeWithEdges.x, mainTextureSizeWithEdges.y, TextureType.OcclusionMap, stepsElapsedUntilHereToFinishTheMerge / stepsNeededToFinishTheMerge, quantityOfTexturesProcessedsAtHere, quantityOfTexturesToBeProcessed);
                    ApplyAtlasInPropertyOfMaterial(material, meshCombinerPreferences.allInOneParams.occlusionMapPropertyToInsert, map);
                    allTextures2dOfFinalMergedMesh.Add(map);
                }
                if (meshCombinerPreferences.allInOneParams.detailAlbedoMapSupport == true)
                {
                    stepsElapsedUntilHereToFinishTheMerge += 1;
                    quantityOfTexturesProcessedsAtHere += 1;
                    Texture2D map = GetSimpleCopyOfTexture(item.Key, meshCombinerPreferences.allInOneParams.detailMapPropertyToFind, mainTextureSizeWithEdges.x, mainTextureSizeWithEdges.y, TextureType.DetailMap, stepsElapsedUntilHereToFinishTheMerge / stepsNeededToFinishTheMerge, quantityOfTexturesProcessedsAtHere, quantityOfTexturesToBeProcessed);
                    ApplyAtlasInPropertyOfMaterial(material, meshCombinerPreferences.allInOneParams.detailMapPropertyToInsert, map);
                    allTextures2dOfFinalMergedMesh.Add(map);
                }
                if (meshCombinerPreferences.allInOneParams.detailMaskSupport == true)
                {
                    stepsElapsedUntilHereToFinishTheMerge += 1;
                    quantityOfTexturesProcessedsAtHere += 1;
                    Texture2D map = GetSimpleCopyOfTexture(item.Key, meshCombinerPreferences.allInOneParams.detailMaskPropertyToFind, mainTextureSizeWithEdges.x, mainTextureSizeWithEdges.y, TextureType.DetailMask, stepsElapsedUntilHereToFinishTheMerge / stepsNeededToFinishTheMerge, quantityOfTexturesProcessedsAtHere, quantityOfTexturesToBeProcessed);
                    ApplyAtlasInPropertyOfMaterial(material, meshCombinerPreferences.allInOneParams.detailMaskPropertyToInsert, map);
                    allTextures2dOfFinalMergedMesh.Add(map);
                }

                //Add this submesh into list of final submeshes that will make the final Merged Mesh
                submeshesOfFinalMergedMesh.Add(material, submesh);
            }

            //-------- PROCESSING NON TILED MESHES

            //Organize this meshes into Textures and yours respective submeshes that uses each
            List<TexturesSubMeshes> texturesAndSubMeshes = new List<TexturesSubMeshes>();
            foreach (TemporarySplitedSubMesh item in texturesAndSubMeshesWithoutTilings)
            {
                //Try to find a texture and respective submeshes that already is created that is using this texture
                TexturesSubMeshes textureOfThisSubMesh = GetTheTextureSubMeshesOfMaterial(item.material, texturesAndSubMeshes);

                //If not found
                if (textureOfThisSubMesh == null)
                {
                    //Create another texture and respective submeshes to store it
                    TexturesSubMeshes thisTextureAndSubMesh = new TexturesSubMeshes();

                    //Calculate and get original resolution of main texture of this material
                    Texture2D mainTextureOfThisMaterial = (Texture2D)item.material.GetTexture(meshCombinerPreferences.allInOneParams.mainTexturePropertyToFind);
                    Vector2Int mainTextureSize = Vector2Int.zero;
                    Vector2Int mainTextureSizeWithEdges = Vector2Int.zero;
                    if (mainTextureOfThisMaterial == null)
                        mainTextureSize = new Vector2Int(64, 64);
                    if (mainTextureOfThisMaterial != null)
                        mainTextureSize = new Vector2Int(mainTextureOfThisMaterial.width, mainTextureOfThisMaterial.height);
                    mainTextureSizeWithEdges = new Vector2Int(mainTextureSize.x + (GetEdgesSizeForTextures() * 2), mainTextureSize.y + (GetEdgesSizeForTextures() * 2));

                    //Get validated copyies of all textures of original material for this class
                    thisTextureAndSubMesh.material = item.material;
                    thisTextureAndSubMesh.mainTextureResolution = mainTextureSize;
                    thisTextureAndSubMesh.mainTextureResolutionWithEdges = mainTextureSizeWithEdges;
                    stepsElapsedUntilHereToFinishTheMerge += 1;
                    quantityOfTexturesProcessedsAtHere += 1;
                    thisTextureAndSubMesh.mainTexture = GetValidatedCopyOfTexture(item.material, meshCombinerPreferences.allInOneParams.mainTexturePropertyToFind, mainTextureSizeWithEdges.x, mainTextureSizeWithEdges.y, TextureType.MainTexture, stepsElapsedUntilHereToFinishTheMerge / stepsNeededToFinishTheMerge, quantityOfTexturesProcessedsAtHere, quantityOfTexturesToBeProcessed);
                    if (meshCombinerPreferences.allInOneParams.metallicMapSupport == true)
                    {
                        stepsElapsedUntilHereToFinishTheMerge += 1;
                        quantityOfTexturesProcessedsAtHere += 1;
                        thisTextureAndSubMesh.metallicMap = GetValidatedCopyOfTexture(item.material, meshCombinerPreferences.allInOneParams.metallicMapPropertyToFind, mainTextureSizeWithEdges.x, mainTextureSizeWithEdges.y, TextureType.MetallicMap, stepsElapsedUntilHereToFinishTheMerge / stepsNeededToFinishTheMerge, quantityOfTexturesProcessedsAtHere, quantityOfTexturesToBeProcessed);
                    }
                    if (meshCombinerPreferences.allInOneParams.specularMapSupport == true)
                    {
                        stepsElapsedUntilHereToFinishTheMerge += 1;
                        quantityOfTexturesProcessedsAtHere += 1;
                        thisTextureAndSubMesh.specularMap = GetValidatedCopyOfTexture(item.material, meshCombinerPreferences.allInOneParams.specularMapPropertyToFind, mainTextureSizeWithEdges.x, mainTextureSizeWithEdges.y, TextureType.SpecularMap, stepsElapsedUntilHereToFinishTheMerge / stepsNeededToFinishTheMerge, quantityOfTexturesProcessedsAtHere, quantityOfTexturesToBeProcessed);
                    }
                    if (meshCombinerPreferences.allInOneParams.normalMapSupport == true)
                    {
                        stepsElapsedUntilHereToFinishTheMerge += 1;
                        quantityOfTexturesProcessedsAtHere += 1;
                        thisTextureAndSubMesh.normalMap = GetValidatedCopyOfTexture(item.material, meshCombinerPreferences.allInOneParams.normalMapPropertyToFind, mainTextureSizeWithEdges.x, mainTextureSizeWithEdges.y, TextureType.NormalMap, stepsElapsedUntilHereToFinishTheMerge / stepsNeededToFinishTheMerge, quantityOfTexturesProcessedsAtHere, quantityOfTexturesToBeProcessed);
                    }
                    if (meshCombinerPreferences.allInOneParams.normalMap2Support == true)
                    {
                        stepsElapsedUntilHereToFinishTheMerge += 1;
                        quantityOfTexturesProcessedsAtHere += 1;
                        thisTextureAndSubMesh.normalMap2 = GetValidatedCopyOfTexture(item.material, meshCombinerPreferences.allInOneParams.normalMap2PropertyFind, mainTextureSizeWithEdges.x, mainTextureSizeWithEdges.y, TextureType.NormalMap, stepsElapsedUntilHereToFinishTheMerge / stepsNeededToFinishTheMerge, quantityOfTexturesProcessedsAtHere, quantityOfTexturesToBeProcessed);
                    }
                    if (meshCombinerPreferences.allInOneParams.heightMapSupport == true)
                    {
                        stepsElapsedUntilHereToFinishTheMerge += 1;
                        quantityOfTexturesProcessedsAtHere += 1;
                        thisTextureAndSubMesh.heightMap = GetValidatedCopyOfTexture(item.material, meshCombinerPreferences.allInOneParams.heightMapPropertyToFind, mainTextureSizeWithEdges.x, mainTextureSizeWithEdges.y, TextureType.HeightMap, stepsElapsedUntilHereToFinishTheMerge / stepsNeededToFinishTheMerge, quantityOfTexturesProcessedsAtHere, quantityOfTexturesToBeProcessed);
                    }
                    if (meshCombinerPreferences.allInOneParams.occlusionMapSupport == true)
                    {
                        stepsElapsedUntilHereToFinishTheMerge += 1;
                        quantityOfTexturesProcessedsAtHere += 1;
                        thisTextureAndSubMesh.occlusionMap = GetValidatedCopyOfTexture(item.material, meshCombinerPreferences.allInOneParams.occlusionMapPropertyToFind, mainTextureSizeWithEdges.x, mainTextureSizeWithEdges.y, TextureType.OcclusionMap, stepsElapsedUntilHereToFinishTheMerge / stepsNeededToFinishTheMerge, quantityOfTexturesProcessedsAtHere, quantityOfTexturesToBeProcessed);
                    }
                    if (meshCombinerPreferences.allInOneParams.detailAlbedoMapSupport == true)
                    {
                        stepsElapsedUntilHereToFinishTheMerge += 1;
                        quantityOfTexturesProcessedsAtHere += 1;
                        thisTextureAndSubMesh.detailMap = GetValidatedCopyOfTexture(item.material, meshCombinerPreferences.allInOneParams.detailMapPropertyToFind, mainTextureSizeWithEdges.x, mainTextureSizeWithEdges.y, TextureType.DetailMap, stepsElapsedUntilHereToFinishTheMerge / stepsNeededToFinishTheMerge, quantityOfTexturesProcessedsAtHere, quantityOfTexturesToBeProcessed);
                    }
                    if (meshCombinerPreferences.allInOneParams.detailMaskSupport == true)
                    {
                        stepsElapsedUntilHereToFinishTheMerge += 1;
                        quantityOfTexturesProcessedsAtHere += 1;
                        thisTextureAndSubMesh.detailMask = GetValidatedCopyOfTexture(item.material, meshCombinerPreferences.allInOneParams.detailMaskPropertyToFind, mainTextureSizeWithEdges.x, mainTextureSizeWithEdges.y, TextureType.DetailMask, stepsElapsedUntilHereToFinishTheMerge / stepsNeededToFinishTheMerge, quantityOfTexturesProcessedsAtHere, quantityOfTexturesToBeProcessed);
                    }

                    //Create this mesh data. get all UV values from this submesh
                    TexturesSubMeshes.UserSubMeshes userSubMesh = new TexturesSubMeshes.UserSubMeshes();
                    userSubMesh.parentTexturesSubMeshes = thisTextureAndSubMesh;
                    userSubMesh.combineInstanceForThisSubMesh = item.combineInstance;
                    userSubMesh.originalUvVertices = new Vector2[item.uvMap.Length];
                    for (int v = 0; v < userSubMesh.originalUvVertices.Length; v++)
                        userSubMesh.originalUvVertices[v] = item.uvMap[v];
                    thisTextureAndSubMesh.userSubMeshes.Add(userSubMesh);

                    //Save the created class
                    texturesAndSubMeshes.Add(thisTextureAndSubMesh);
                }

                //If found
                if (textureOfThisSubMesh != null)
                {
                    //Create this mesh data and add to textures that already exists. get all UV values from this submesh
                    TexturesSubMeshes.UserSubMeshes userSubMesh = new TexturesSubMeshes.UserSubMeshes();
                    userSubMesh.parentTexturesSubMeshes = textureOfThisSubMesh;
                    userSubMesh.combineInstanceForThisSubMesh = item.combineInstance;
                    userSubMesh.originalUvVertices = new Vector2[item.uvMap.Length];
                    for (int v = 0; v < userSubMesh.originalUvVertices.Length; v++)
                        userSubMesh.originalUvVertices[v] = item.uvMap[v];
                    textureOfThisSubMesh.userSubMeshes.Add(userSubMesh);
                }
            }

            //Update the progress
            stepsElapsedUntilHereToFinishTheMerge += 1;
            ShowProgressBar("Merging Common Meshes...", true, stepsElapsedUntilHereToFinishTheMerge / stepsNeededToFinishTheMerge);

            //Generate one submesh with X textures per atlas each, after generate, add the submesh generated to list of final submeshes
            List<TexturesSubMeshes> texturesAndSubmeshesThatMakeCurrentAtlas = new List<TexturesSubMeshes>();
            int currentTextureIndexOfCurrentAtlas = 0;
            int totalOfTexturesAndSubMeshesLeft = texturesAndSubMeshes.Count;
            foreach (TexturesSubMeshes item in texturesAndSubMeshes)
            {
                //Increase the counter of current texture in current atlas
                currentTextureIndexOfCurrentAtlas += 1;
                //Add this texture and respective submeshes to be merged on reach the max of textures for this atlas
                texturesAndSubmeshesThatMakeCurrentAtlas.Add(item);
                //Decrease the counter of total textures submeshes left
                totalOfTexturesAndSubMeshesLeft -= 1;

                //If this is the last texture of this atlas, finish this submesh and add to list of finals submeshes (do it too, if the total of textures and submeshes left is zero, but not equal to maxTexturesPerAtlas)
                if (currentTextureIndexOfCurrentAtlas == meshCombinerPreferences.allInOneParams.maxTexturesPerAtlas || totalOfTexturesAndSubMeshesLeft <= 0)
                {
                    //Separate all combine instances
                    List<CombineInstance> allCombineInstancesOfThisAtlas = new List<CombineInstance>();
                    List<TexturesSubMeshes.UserSubMeshes> allUserSubMeshes = new List<TexturesSubMeshes.UserSubMeshes>();
                    int startOfUvVerticesInIndex = 0;
                    foreach (TexturesSubMeshes itemOfAtlas in texturesAndSubmeshesThatMakeCurrentAtlas)
                        foreach (TexturesSubMeshes.UserSubMeshes user in itemOfAtlas.userSubMeshes)
                        {
                            allCombineInstancesOfThisAtlas.Add(user.combineInstanceForThisSubMesh);
                            allUserSubMeshes.Add(user);
                            user.startOfUvVerticesInIndex = startOfUvVerticesInIndex;
                            startOfUvVerticesInIndex += user.originalUvVertices.Length;
                        }

                    //Create the submesh with all submeshes that make this atlas
                    Mesh submesh = new Mesh();
                    if (totalVerticesCount <= MAX_VERTICES_FOR_16BITS_MESH)
                        submesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
                    if (totalVerticesCount > MAX_VERTICES_FOR_16BITS_MESH)
                        submesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                    submesh.CombineMeshes(allCombineInstancesOfThisAtlas.ToArray(), true, true, meshCombinerPreferences.lightmapSupport);

                    //Create a material for this submesh, using the original material as base
                    Material material = GetValidatedCopyOfMaterial(meshCombinerPreferences.allInOneParams.materialToUse, true, true);

                    //Create all atlas for all maps of this submesh
                    AtlasData atlasData = CreateAllAtlas(texturesAndSubmeshesThatMakeCurrentAtlas, GetAtlasMaxResolution(), GetAtlasPadding());

                    //Create the new UV map for this submesh basing in atlas
                    Vector2[] newUvMapForCombinedMesh = new Vector2[submesh.uv.Length];
                    foreach (TexturesSubMeshes.UserSubMeshes userSubmesh in allUserSubMeshes)
                    {
                        //Calculate the percentage that the edge of this texture uses, calculates the size of the uv for each texture, to ignore the edges
                        Vector2 percentEdgeUsageOfCurrentTexture = userSubmesh.parentTexturesSubMeshes.GetEdgesPercentUsageOfThisTextures();

                        //Get index of this main texture submesh in atlas rects
                        int mainTextureIndexInAtlas = atlasData.GetRectIndexOfThatMainTexture(userSubmesh.parentTexturesSubMeshes.mainTexture);

                        //Process all uv vertices of this submesh
                        for (int i = 0; i < userSubmesh.originalUvVertices.Length; i++)
                        {
                            //Create the vertice
                            Vector2 thisVertex = Vector2.zero;

                            //If the UV map of this mesh is not larger than the texture
                            thisVertex.x = Mathf.Lerp(atlasData.atlasRects[mainTextureIndexInAtlas].xMin, atlasData.atlasRects[mainTextureIndexInAtlas].xMax, Mathf.Lerp(percentEdgeUsageOfCurrentTexture.x, 1 - percentEdgeUsageOfCurrentTexture.x, userSubmesh.originalUvVertices[i].x));
                            thisVertex.y = Mathf.Lerp(atlasData.atlasRects[mainTextureIndexInAtlas].yMin, atlasData.atlasRects[mainTextureIndexInAtlas].yMax, Mathf.Lerp(percentEdgeUsageOfCurrentTexture.y, 1 - percentEdgeUsageOfCurrentTexture.y, userSubmesh.originalUvVertices[i].y));

                            //Save this vertice edited in uv map of combined mesh
                            newUvMapForCombinedMesh[i + userSubmesh.startOfUvVerticesInIndex] = thisVertex;
                        }
                    }
                    //Apply the new UV map in this submesh
                    submesh.uv = newUvMapForCombinedMesh;

                    //Apply all atlas too
                    ApplyAtlasInPropertyOfMaterial(material, meshCombinerPreferences.allInOneParams.mainTexturePropertyToInsert, atlasData.mainTextureAtlas);
                    allTextures2dOfFinalMergedMesh.Add(atlasData.mainTextureAtlas);
                    if (meshCombinerPreferences.allInOneParams.metallicMapSupport == true)
                    {
                        ApplyAtlasInPropertyOfMaterial(material, meshCombinerPreferences.allInOneParams.metallicMapPropertyToInsert, atlasData.metallicMapAtlas);
                        allTextures2dOfFinalMergedMesh.Add(atlasData.metallicMapAtlas);
                    }
                    if (meshCombinerPreferences.allInOneParams.specularMapSupport == true)
                    {
                        ApplyAtlasInPropertyOfMaterial(material, meshCombinerPreferences.allInOneParams.specularMapPropertyToInsert, atlasData.specularMapAtlas);
                        allTextures2dOfFinalMergedMesh.Add(atlasData.specularMapAtlas);
                    }
                    if (meshCombinerPreferences.allInOneParams.normalMapSupport == true)
                    {
                        ApplyAtlasInPropertyOfMaterial(material, meshCombinerPreferences.allInOneParams.normalMapPropertyToInsert, atlasData.normalMapAtlas);
                        allTextures2dOfFinalMergedMesh.Add(atlasData.normalMapAtlas);
                    }
                    if (meshCombinerPreferences.allInOneParams.normalMap2Support == true)
                    {
                        ApplyAtlasInPropertyOfMaterial(material, meshCombinerPreferences.allInOneParams.normalMap2PropertyToInsert, atlasData.normalMap2Atlas);
                        allTextures2dOfFinalMergedMesh.Add(atlasData.normalMap2Atlas);
                    }
                    if (meshCombinerPreferences.allInOneParams.heightMapSupport == true)
                    {
                        ApplyAtlasInPropertyOfMaterial(material, meshCombinerPreferences.allInOneParams.heightMapPropertyToInsert, atlasData.heightMapAtlas);
                        allTextures2dOfFinalMergedMesh.Add(atlasData.heightMapAtlas);
                    }
                    if (meshCombinerPreferences.allInOneParams.occlusionMapSupport == true)
                    {
                        ApplyAtlasInPropertyOfMaterial(material, meshCombinerPreferences.allInOneParams.occlusionMapPropertyToInsert, atlasData.occlusionMapAtlas);
                        allTextures2dOfFinalMergedMesh.Add(atlasData.occlusionMapAtlas);
                    }
                    if (meshCombinerPreferences.allInOneParams.detailAlbedoMapSupport == true)
                    {
                        ApplyAtlasInPropertyOfMaterial(material, meshCombinerPreferences.allInOneParams.detailMapPropertyToInsert, atlasData.detailMapAtlas);
                        allTextures2dOfFinalMergedMesh.Add(atlasData.detailMapAtlas);
                    }
                    if (meshCombinerPreferences.allInOneParams.detailMaskSupport == true)
                    {
                        ApplyAtlasInPropertyOfMaterial(material, meshCombinerPreferences.allInOneParams.detailMaskPropertyToInsert, atlasData.detailMaskAtlas);
                        allTextures2dOfFinalMergedMesh.Add(atlasData.detailMaskAtlas);
                    }

                    //If is desired to hightlight UV vertices
                    if (meshCombinerPreferences.allInOneParams.highlightUvVertices == true)
                    {
                        for (int i = 0; i < submesh.uv.Length; i++)
                            atlasData.mainTextureAtlas.SetPixel((int)(atlasData.mainTextureAtlas.width * submesh.uv[i].x), (int)(atlasData.mainTextureAtlas.height * submesh.uv[i].y), Color.yellow);
                        atlasData.mainTextureAtlas.Apply();
                    }

                    //Add this submesh into list of final submeshes that will make the final Merged Mesh
                    submeshesOfFinalMergedMesh.Add(material, submesh);

                    //Reset the counter and list
                    texturesAndSubmeshesThatMakeCurrentAtlas.Clear();
                    currentTextureIndexOfCurrentAtlas = 0;
                }
            }

            //-------- PROCESSING FINAL MERGED MESH

            //Process each final submesh
            List<CombineInstance> finalCombineInstances = new List<CombineInstance>();
            foreach (var item in submeshesOfFinalMergedMesh)
            {
                CombineInstance combineInstanceOfThisSubMesh = new CombineInstance();
                combineInstanceOfThisSubMesh.mesh = item.Value;
                combineInstanceOfThisSubMesh.subMeshIndex = 0;
                combineInstanceOfThisSubMesh.transform = Matrix4x4.identity;
                finalCombineInstances.Add(combineInstanceOfThisSubMesh);
            }

            //Create the final mesh that contains all submeshes divided per material
            Mesh finalMesh = new Mesh();
            if (totalVerticesCount <= MAX_VERTICES_FOR_16BITS_MESH)
                finalMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
            if (totalVerticesCount > MAX_VERTICES_FOR_16BITS_MESH)
                finalMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            finalMesh.CombineMeshes(finalCombineInstances.ToArray(), false, true, meshCombinerPreferences.lightmapSupport);
            finalMesh.RecalculateBounds();
            if (meshCombinerPreferences.lightmapSupport == true)
                Unwrapping.GenerateSecondaryUVSet(finalMesh);

            //Polulate the holder GameObject with the data of combined mesh
            holderMeshFilter.sharedMesh = finalMesh;
            List<Material> materialsForSubMeshes = new List<Material>();
            foreach (var key in submeshesOfFinalMergedMesh)
                materialsForSubMeshes.Add(key.Key);
            holderMeshRenderer.sharedMaterials = materialsForSubMeshes.ToArray();

            //Restore original state of Read/Write for all Textures used in this merge
            stepsElapsedUntilHereToFinishTheMerge += 1;
            ShowProgressBar("Finishing Textures...", true, stepsElapsedUntilHereToFinishTheMerge / stepsNeededToFinishTheMerge);
            RestoreOriginalStateOfReadWriteForAllTexturesUsedInThisMerge(allTexturesThatWillBeUsedAndIfRWIsEnabled, stepsElapsedUntilHereToFinishTheMerge / stepsNeededToFinishTheMerge);

            //Add the MeshCollider if is desired
            if (meshCombinerPreferences.allInOneParams.addMeshCollider == true)
                holderGameObject.AddComponent<MeshCollider>();

            //-------------------------------- END OF MERGE CODE ---------------------------------

            //Set scene dirty and refresh asset data
            AssetDatabase.Refresh();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            //Select and ping the gameobject of merge
            Selection.activeGameObject = holderGameObject;
            EditorGUIUtility.PingObject(holderGameObject);

            //Save the mesh of merge in assets, if desired
            if (meshCombinerPreferences.saveMeshInAssets == true)
            {
                //Show the saving generated data progress bar
                int totalFilesToSave = 1 + allTextures2dOfFinalMergedMesh.Count + holderMeshRenderer.sharedMaterials.Length;
                int totalFilesSaved = 0;
                ShowProgressBar("Saving Generated Data... (" + totalFilesSaved + "/" + totalFilesToSave + ")", true, 1.0f);
                string generatedMeshPath = SaveAssetAsFile("Meshes", holderMeshFilter.sharedMesh, meshCombinerPreferences.nameOfThisMerge, "asset");
                holderManager.pathsAndTypesOfAssetsOfThisMerge.Add(new CombinedMeshesManager.PathAndTypeOfAAsset(CombinedMeshesManager.AssetType.Mesh, generatedMeshPath));
                totalFilesSaved += 1;
                ShowProgressBar("Saving Generated Data... (" + totalFilesSaved + "/" + totalFilesToSave + ")", true, 1.0f);
                //Save each generated texture generated in this merge method
                int quantityOfTexturesSave = 0;
                foreach (Texture2D texture in allTextures2dOfFinalMergedMesh)
                {
                    string generatedTexturePath = SaveAssetAsFile("Atlases", texture, meshCombinerPreferences.nameOfThisMerge + " (Texture " + quantityOfTexturesSave + ")", "asset");
                    holderManager.pathsAndTypesOfAssetsOfThisMerge.Add(new CombinedMeshesManager.PathAndTypeOfAAsset(CombinedMeshesManager.AssetType.Texture, generatedTexturePath));
                    quantityOfTexturesSave += 1;
                    totalFilesSaved += 1;
                    ShowProgressBar("Saving Generated Data... (" + totalFilesSaved + "/" + totalFilesToSave + ")", true, 1.0f);
                }
                //Save each generated material generated in this merge method
                int quantityOfMaterialsSaved = 0;
                foreach (Material material in holderMeshRenderer.sharedMaterials)
                {
                    string generatedMaterialPath = SaveAssetAsFile("Materials", material, meshCombinerPreferences.nameOfThisMerge + " (Material " + quantityOfMaterialsSaved + ")", "mat");
                    holderManager.pathsAndTypesOfAssetsOfThisMerge.Add(new CombinedMeshesManager.PathAndTypeOfAAsset(CombinedMeshesManager.AssetType.Material, generatedMaterialPath));
                    quantityOfMaterialsSaved += 1;
                    totalFilesSaved += 1;
                    ShowProgressBar("Saving Generated Data... (" + totalFilesSaved + "/" + totalFilesToSave + ")", true, 1.0f);
                }
                ShowProgressBar("Saving Generated Data... (" + totalFilesSaved + "/" + totalFilesToSave + ")", true, 1.0f);
            }

            //Run last steps of merge
            ShowProgressBar("Finishing Merge...", true, 1.0f);
            //Do the desired action to all meshes processed by this merge. For example, disable all original GameObjects (save the originals stats of all GameObjects in the manager too)
            DoTheSelectedActionAfterMerge(holderManager, listOfAllMeshesThatWasProcessedsInThisMerge.ToArray());
            //Feed the CombinedMeshesManager in the holder GameObject with all needed data, to be possible a management of merge
            if (meshCombinerPreferences.mergeMethod == MeshCombinerPreferences.MergeMethod.OneMeshPerMaterial)
                holderManager.mergeMethodUsed = CombinedMeshesManager.MergeMethod.OneMeshPerMaterial;
            if (meshCombinerPreferences.mergeMethod == MeshCombinerPreferences.MergeMethod.AllInOne)
                holderManager.mergeMethodUsed = CombinedMeshesManager.MergeMethod.AllInOne;
            if (meshCombinerPreferences.mergeMethod == MeshCombinerPreferences.MergeMethod.JustMaterialColors)
                holderManager.mergeMethodUsed = CombinedMeshesManager.MergeMethod.JustMaterialColors;
            //Save the undo method to use, if user wish
            if (meshCombinerPreferences.afterMerge == MeshCombinerPreferences.AfterMerge.DeactiveOriginalGameObjects)
                holderManager.undoMethod = CombinedMeshesManager.UndoMethod.ReactiveOriginalGameObjects;
            if (meshCombinerPreferences.afterMerge == MeshCombinerPreferences.AfterMerge.DisableOriginalMeshes)
                holderManager.undoMethod = CombinedMeshesManager.UndoMethod.EnableOriginalMeshes;
            if (meshCombinerPreferences.afterMerge == MeshCombinerPreferences.AfterMerge.DoNothing)
                holderManager.undoMethod = CombinedMeshesManager.UndoMethod.DoNothing;
            //Save information if the GameObject of merge is a prefab
            holderManager.thisIsPrefab = meshCombinerPreferences.savePrefabOfThis;

            //Save a prefab, if is desired
            if (meshCombinerPreferences.savePrefabOfThis == true)
                SaveMergeAsPrefab(meshCombinerPreferences.prefabName, holderGameObject);

            //Get finishing time of merge
            DateTime finishingTime = DateTime.Now;
            //Calculate the difference between starting and finishing time
            TimeSpan processingTime = finishingTime - startingTime;

            //Hide progress bar
            ShowProgressBar("", false, 0.0f);

            //Add the log
            logsOfBeforeMerge.Add(new LogOfMerge(MessageType.Info, "The merge has been successfully completed! See merge statistics in the box below.\n\nProcessed in " + processingTime.Minutes + " minutes and " + processingTime.Seconds + " seconds."));

            //Report that the merge is ended
            mergeIsDone = true;
        }

        public void DoCombineMeshes_JustMaterialColors()
        {
            //Show progress bar
            ShowProgressBar("Merging...", true, 1.0f);

            //Create the holder GameObject
            GameObject holderGameObject = new GameObject(meshCombinerPreferences.nameOfThisMerge);
            CombinedMeshesManager holderManager = holderGameObject.AddComponent<CombinedMeshesManager>();
            MeshFilter holderMeshFilter = holderGameObject.AddComponent<MeshFilter>();
            MeshRenderer holderMeshRenderer = holderGameObject.AddComponent<MeshRenderer>();
            if (meshCombinerPreferences.lightmapSupport == false)
                GameObjectUtility.SetStaticEditorFlags(holderGameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.NavigationStatic);
            if (meshCombinerPreferences.lightmapSupport == true)
                GameObjectUtility.SetStaticEditorFlags(holderGameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.NavigationStatic | StaticEditorFlags.ContributeGI);
            holderGameObject.transform.SetParent(bestParentTransformForGameObjectResultOfMerge);
            holderGameObject.transform.SetSiblingIndex(bestSibilingForGameObjectResultOfMerge);

            //Allocate space for a list that stores reference for ALL gameobjects that have meshes that was processed in this merge
            List<MeshRenderer> listOfAllMeshesThatWasProcessedsInThisMerge = new List<MeshRenderer>();

            //Store the time of start of merge
            DateTime startingTime = DateTime.Now;

            //------------------------------- START OF MERGE CODE --------------------------------

            //Allocate the storage
            List<CombineInstance> combinesToMerge = new List<CombineInstance>();
            List<UvDataAndColorOfThisSubmesh> uvDatasToMerge = new List<UvDataAndColorOfThisSubmesh>();

            //Obtains the data for each mesh
            int totalVerticesVerifiedAtHere = 0;
            foreach (GameObjectWithMesh obj in validsGameObjectsSelected)
            {
                //Get this GameObject mesh
                MeshRenderer thisMeshRenderer = obj.meshRenderer;
                MeshFilter thisMeshFilter = obj.meshFilter;

                //Get data for each submesh present in this GameObject
                for (int i = 0; i < thisMeshFilter.sharedMesh.subMeshCount; i++)
                {
                    //Update progress bar
                    ShowProgressBar("Reading Meshes...", true, 1.0f);

                    //Configure the Combine Instances for each submesh or mesh
                    CombineInstance combineInstance = new CombineInstance();
                    combineInstance.mesh = thisMeshFilter.sharedMesh;
                    combineInstance.subMeshIndex = i;
                    combineInstance.transform = obj.gameObject.transform.localToWorldMatrix;
                    combinesToMerge.Add(combineInstance);

                    //Get UV vertices count for this submesh
                    int uvMapSizeOfThisSubMesh = 0;
#if UNITY_2019_3_OR_NEWER
                    //(for Unity 2019.3 or newer)
                    uvMapSizeOfThisSubMesh = combineInstance.mesh.GetSubMesh(combineInstance.subMeshIndex).vertexCount;
#endif
#if !UNITY_2019_3_OR_NEWER
                    //(for Unity 2019.2 or older)
                    uvMapSizeOfThisSubMesh = combineInstance.mesh.EmcGetSubmesh(combineInstance.subMeshIndex).vertexCount;
#endif

                    //Capture and create a storage for all UV data of this submesh
                    UvDataAndColorOfThisSubmesh uvDataOfThisSubmesh = new UvDataAndColorOfThisSubmesh();
                    uvDataOfThisSubmesh.startOfUvVerticesIndex = totalVerticesVerifiedAtHere;
                    uvDataOfThisSubmesh.originalUvVertices = new Vector2[uvMapSizeOfThisSubMesh];
                    uvDataOfThisSubmesh.textureColor = GetTextureFilledWithColorOfMaterial(thisMeshRenderer.sharedMaterials[i], meshCombinerPreferences.justMaterialColorsParams.colorPropertyToFind, 64, 64);
                    uvDatasToMerge.Add(uvDataOfThisSubmesh);

                    //Add the total vertices verified
                    totalVerticesVerifiedAtHere += uvMapSizeOfThisSubMesh;
                }

                //Add this mesh to list of meshes readed
                if (listOfAllMeshesThatWasProcessedsInThisMerge.Contains(obj.meshRenderer) == false)
                    listOfAllMeshesThatWasProcessedsInThisMerge.Add(obj.meshRenderer);
            }

            //Update progress bar
            ShowProgressBar("Merging...", true, 1.0f);

            //Combine all submeshes into one mesh with submeshes with all materials
            Mesh finalMesh = new Mesh();
            if (totalVerticesVerifiedAtHere <= MAX_VERTICES_FOR_16BITS_MESH)
                finalMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
            if (totalVerticesVerifiedAtHere > MAX_VERTICES_FOR_16BITS_MESH)
                finalMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            finalMesh.CombineMeshes(combinesToMerge.ToArray(), true, true, meshCombinerPreferences.lightmapSupport);
            finalMesh.RecalculateBounds();
            if (meshCombinerPreferences.lightmapSupport == true)
                Unwrapping.GenerateSecondaryUVSet(finalMesh);

            //Polulate the holder GameObject with the data of combined mesh
            holderMeshFilter.sharedMesh = finalMesh;
            holderMeshRenderer.sharedMaterials = new Material[] { GetValidatedCopyOfMaterial(meshCombinerPreferences.justMaterialColorsParams.materialToUse, true, true) };

            //Create all atlas using all collected colors
            ColorAtlasData atlasGenerated = CreateColorAtlas(uvDatasToMerge.ToArray(), 512, 0, true);

            //Show progress bar
            ShowProgressBar("Creating New UV Map...", true, 1.0f);

            //Process each submesh UV data and create a new entire UV map for combined mesh
            Vector2[] newUvMapForCombinedMesh = new Vector2[holderMeshFilter.sharedMesh.uv.Length];
            foreach (UvDataAndColorOfThisSubmesh thisUvData in uvDatasToMerge)
            {
                //Change all vertex of UV to positive, where vertex position is major than 1 or minor than 0, because the entire UV will resized to fit in your respective texture in atlas
                for (int i = 0; i < thisUvData.originalUvVertices.Length; i++)
                {
                    if (thisUvData.originalUvVertices[i].x < 0)
                        thisUvData.originalUvVertices[i].x = thisUvData.originalUvVertices[i].x * -1;
                    if (thisUvData.originalUvVertices[i].y < 0)
                        thisUvData.originalUvVertices[i].y = thisUvData.originalUvVertices[i].y * -1;
                }

                //Calculates the highest point of the UV map of each mesh, for know how to reduces to fit in texture atlas, checks which is the largest coordinate found in the list of UV vertices, in X or Y and stores it
                Vector2 highestVertexCoordinatesForThisSubmesh = Vector2.zero;
                for (int i = 0; i < thisUvData.originalUvVertices.Length; i++)
                    highestVertexCoordinatesForThisSubmesh = new Vector2(Mathf.Max(thisUvData.originalUvVertices[i].x, highestVertexCoordinatesForThisSubmesh.x), Mathf.Max(thisUvData.originalUvVertices[i].y, highestVertexCoordinatesForThisSubmesh.y));

                //Calculate the percentage that the edge of textures uses, to center the UV vertices in center of each color
                Vector2 percentEdgeUsageOfCurrentTexture = new Vector2(0.8f, 0.8f);

                //Get index of this texture (color) submesh in atlas rects
                int colorIndexInAtlas = atlasGenerated.GetRectIndexOfThatMainTexture(thisUvData.textureColor);

                //Verify each vertex of UV map, for respective UV map of this mesh
                for (int i = 0; i < thisUvData.originalUvVertices.Length; i++)
                {
                    //Create the vertex
                    Vector2 thisVertex = Vector2.zero;

                    //If the UV map of this mesh is not larger than the texture
                    if (highestVertexCoordinatesForThisSubmesh.x <= 1)
                        thisVertex.x = Mathf.Lerp(atlasGenerated.atlasRects[colorIndexInAtlas].xMin, atlasGenerated.atlasRects[colorIndexInAtlas].xMax, Mathf.Lerp(percentEdgeUsageOfCurrentTexture.x, 1 - percentEdgeUsageOfCurrentTexture.x, thisUvData.originalUvVertices[i].x));
                    if (highestVertexCoordinatesForThisSubmesh.y <= 1)
                        thisVertex.y = Mathf.Lerp(atlasGenerated.atlasRects[colorIndexInAtlas].yMin, atlasGenerated.atlasRects[colorIndexInAtlas].yMax, Mathf.Lerp(percentEdgeUsageOfCurrentTexture.y, 1 - percentEdgeUsageOfCurrentTexture.y, thisUvData.originalUvVertices[i].y));

                    //If the UV map is larger than the texture
                    if (highestVertexCoordinatesForThisSubmesh.x > 1)
                        thisVertex.x = Mathf.Lerp(atlasGenerated.atlasRects[colorIndexInAtlas].xMin, atlasGenerated.atlasRects[colorIndexInAtlas].xMax, Mathf.Lerp(percentEdgeUsageOfCurrentTexture.x, 1 - percentEdgeUsageOfCurrentTexture.x, thisUvData.originalUvVertices[i].x / highestVertexCoordinatesForThisSubmesh.x));
                    if (highestVertexCoordinatesForThisSubmesh.y > 1)
                        thisVertex.y = Mathf.Lerp(atlasGenerated.atlasRects[colorIndexInAtlas].yMin, atlasGenerated.atlasRects[colorIndexInAtlas].yMax, Mathf.Lerp(percentEdgeUsageOfCurrentTexture.y, 1 - percentEdgeUsageOfCurrentTexture.y, thisUvData.originalUvVertices[i].y / highestVertexCoordinatesForThisSubmesh.y));

                    //Add the created vertex to list of new UV map
                    newUvMapForCombinedMesh[i + thisUvData.startOfUvVerticesIndex] = thisVertex;
                }
            }

            //Show progress bar
            ShowProgressBar("Finishing...", true, 1.0f);

            //Apply the new UV map merged using modification of all UV vertex of each submesh, apply all atlas too
            holderMeshFilter.sharedMesh.uv = newUvMapForCombinedMesh;
            ApplyAtlasInPropertyOfMaterial(holderMeshRenderer.sharedMaterials[0], meshCombinerPreferences.justMaterialColorsParams.mainTexturePropertyToInsert, atlasGenerated.colorAtlas);

            //Add the MeshCollider if is desired
            if (meshCombinerPreferences.justMaterialColorsParams.addMeshCollider == true)
                holderGameObject.AddComponent<MeshCollider>();

            //-------------------------------- END OF MERGE CODE ---------------------------------

            //Set scene dirty and refresh asset data
            AssetDatabase.Refresh();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            //Select and ping the gameobject of merge
            Selection.activeGameObject = holderGameObject;
            EditorGUIUtility.PingObject(holderGameObject);

            //Save the mesh of merge in assets, if desired
            if (meshCombinerPreferences.saveMeshInAssets == true)
            {
                ShowProgressBar("Saving Generated Mesh...", true, 1.0f);
                string generatedMeshPath = SaveAssetAsFile("Meshes", holderMeshFilter.sharedMesh, meshCombinerPreferences.nameOfThisMerge, "asset");
                holderManager.pathsAndTypesOfAssetsOfThisMerge.Add(new CombinedMeshesManager.PathAndTypeOfAAsset(CombinedMeshesManager.AssetType.Mesh, generatedMeshPath));
                ShowProgressBar("Saving Generated Data...", true, 1.0f);
                string generatedTexturePath = SaveAssetAsFile("Atlases", atlasGenerated.colorAtlas, meshCombinerPreferences.nameOfThisMerge, "asset");
                holderManager.pathsAndTypesOfAssetsOfThisMerge.Add(new CombinedMeshesManager.PathAndTypeOfAAsset(CombinedMeshesManager.AssetType.Texture, generatedTexturePath));
                string generatedMaterialPath = SaveAssetAsFile("Materials", holderMeshRenderer.sharedMaterials[0], meshCombinerPreferences.nameOfThisMerge, "mat");
                holderManager.pathsAndTypesOfAssetsOfThisMerge.Add(new CombinedMeshesManager.PathAndTypeOfAAsset(CombinedMeshesManager.AssetType.Material, generatedMaterialPath));
            }

            //Run last steps of merge
            ShowProgressBar("Finishing Merge...", true, 1.0f);
            //Do the desired action to all meshes processed by this merge. For example, disable all original GameObjects (save the originals stats of all GameObjects in the manager too)
            DoTheSelectedActionAfterMerge(holderManager, listOfAllMeshesThatWasProcessedsInThisMerge.ToArray());
            //Feed the CombinedMeshesManager in the holder GameObject with all needed data, to be possible a management of merge
            if (meshCombinerPreferences.mergeMethod == MeshCombinerPreferences.MergeMethod.OneMeshPerMaterial)
                holderManager.mergeMethodUsed = CombinedMeshesManager.MergeMethod.OneMeshPerMaterial;
            if (meshCombinerPreferences.mergeMethod == MeshCombinerPreferences.MergeMethod.AllInOne)
                holderManager.mergeMethodUsed = CombinedMeshesManager.MergeMethod.AllInOne;
            if (meshCombinerPreferences.mergeMethod == MeshCombinerPreferences.MergeMethod.JustMaterialColors)
                holderManager.mergeMethodUsed = CombinedMeshesManager.MergeMethod.JustMaterialColors;
            //Save the undo method to use, if user wish
            if (meshCombinerPreferences.afterMerge == MeshCombinerPreferences.AfterMerge.DeactiveOriginalGameObjects)
                holderManager.undoMethod = CombinedMeshesManager.UndoMethod.ReactiveOriginalGameObjects;
            if (meshCombinerPreferences.afterMerge == MeshCombinerPreferences.AfterMerge.DisableOriginalMeshes)
                holderManager.undoMethod = CombinedMeshesManager.UndoMethod.EnableOriginalMeshes;
            if (meshCombinerPreferences.afterMerge == MeshCombinerPreferences.AfterMerge.DoNothing)
                holderManager.undoMethod = CombinedMeshesManager.UndoMethod.DoNothing;
            //Save information if the GameObject of merge is a prefab
            holderManager.thisIsPrefab = meshCombinerPreferences.savePrefabOfThis;

            //Save a prefab, if is desired
            if (meshCombinerPreferences.savePrefabOfThis == true)
                SaveMergeAsPrefab(meshCombinerPreferences.prefabName, holderGameObject);

            //Get finishing time of merge
            DateTime finishingTime = DateTime.Now;
            //Calculate the difference between starting and finishing time
            TimeSpan processingTime = finishingTime - startingTime;

            //Hide progress bar
            ShowProgressBar("", false, 0.0f);

            //Add the log
            logsOfBeforeMerge.Add(new LogOfMerge(MessageType.Info, "The merge has been successfully completed! See merge statistics in the box below.\n\nProcessed in " + processingTime.Minutes + " minutes and " + processingTime.Seconds + " seconds."));

            //Report that the merge is ended
            mergeIsDone = true;
        }

        #region CLASSES_OF_CORE_METHODS
        //Used in One Mesh Per Material

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

        //Used in All In One

        private class TemporarySplitedSubMesh
        {
            //This class stores a Temporary Submesh to combine, to be used more precisely late in the merge process
            public CombineInstance combineInstance;
            public Vector2[] uvMap;
            public Material material;
        }

        private class TexturesSubMeshes
        {
            public class UserSubMeshes
            {
                //This class stores data of a submesh that uses this texture
                public TexturesSubMeshes parentTexturesSubMeshes;
                public CombineInstance combineInstanceForThisSubMesh;
                public Vector2[] originalUvVertices = null;
                public int startOfUvVerticesInIndex = 0;
            }

            //This class stores textures and all submeshes data that uses this texture
            public Material material;
            public Texture2D mainTexture;
            public Texture2D metallicMap;
            public Texture2D specularMap;
            public Texture2D normalMap;
            public Texture2D normalMap2;
            public Texture2D heightMap;
            public Texture2D occlusionMap;
            public Texture2D detailMap;
            public Texture2D detailMask;
            public Vector2Int mainTextureResolution;
            public Vector2Int mainTextureResolutionWithEdges;
            public List<UserSubMeshes> userSubMeshes = new List<UserSubMeshes>();

            //Return the edges percent usage, getting from 0 submesh of this texture
            public Vector2 GetEdgesPercentUsageOfThisTextures()
            {
                int edgePixelsX = (mainTextureResolutionWithEdges.x - mainTextureResolution.x);
                int edgePixelsY = (mainTextureResolutionWithEdges.y - mainTextureResolution.y);
                return new Vector2(((float)edgePixelsX / 2.0f) / mainTextureResolutionWithEdges.x, ((float)edgePixelsY / 2.0f) / mainTextureResolutionWithEdges.y);
            }
        }

        private enum TextureType
        {
            //This enum stores type of texture
            MainTexture,
            MetallicMap,
            SpecularMap,
            NormalMap,
            HeightMap,
            OcclusionMap,
            DetailMap,
            DetailMask
        }

        private class ColorData
        {
            //This class stores a color and your respective name
            public string colorName;
            public Color color;

            public ColorData(string colorName, Color color)
            {
                this.colorName = colorName;
                this.color = color;
            }
        }

        private class AtlasData
        {
            //This class store a atlas data
            public Texture2D mainTextureAtlas = new Texture2D(16, 16);
            public Texture2D metallicMapAtlas = new Texture2D(16, 16);
            public Texture2D specularMapAtlas = new Texture2D(16, 16);
            public Texture2D normalMapAtlas = new Texture2D(16, 16);
            public Texture2D normalMap2Atlas = new Texture2D(16, 16);
            public Texture2D heightMapAtlas = new Texture2D(16, 16);
            public Texture2D occlusionMapAtlas = new Texture2D(16, 16);
            public Texture2D detailMapAtlas = new Texture2D(16, 16);
            public Texture2D detailMaskAtlas = new Texture2D(16, 16);
            public Rect[] atlasRects = new Rect[0];
            public Texture2D[] originalMainTexturesUsedAndOrdenedAccordingToAtlasRect = new Texture2D[0];

            //Return the respective id of rect that the informed texture is posicioned
            public int GetRectIndexOfThatMainTexture(Texture2D texture)
            {
                //Prepare the storage
                int index = -1;

                foreach (Texture2D tex in originalMainTexturesUsedAndOrdenedAccordingToAtlasRect)
                {
                    //Increase de index in onee
                    index += 1;

                    //If the texture informed is equal to original texture used, break this loop and return the respective index
                    if (tex == texture)
                        break;
                }

                //Return the data
                return index;
            }
        }

        //Used in Just Material Colors

        private class UvDataAndColorOfThisSubmesh
        {
            //This class stores all UV data of a submesh
            public Texture2D textureColor;
            public int startOfUvVerticesIndex;
            public Vector2[] originalUvVertices;
        }

        private class ColorAtlasData
        {
            //This class store a atlas data
            public Texture2D colorAtlas = new Texture2D(16, 16);
            public Rect[] atlasRects = new Rect[0];
            public Texture2D[] originalTexturesUsedAndOrdenedAccordingToAtlasRect = new Texture2D[0];

            //Return the respective id of rect that the informed texture is posicioned
            public int GetRectIndexOfThatMainTexture(Texture2D texture)
            {
                //Prepare the storage
                int index = -1;

                foreach (Texture2D tex in originalTexturesUsedAndOrdenedAccordingToAtlasRect)
                {
                    //Increase de index in onee
                    index += 1;

                    //If the texture informed is equal to original texture used, break this loop and return the respective index
                    if (tex == texture)
                        break;
                }

                //Return the data
                return index;
            }
        }

        #endregion

        #region TOOLS_METHODS_FOR_CORE_METHODS
        //API Methods only for Interface Editor

        private void ShowProgressBar(string message, bool show, float progress)
        {
            if (show == true)
                EditorUtility.DisplayProgressBar("A moment" + ((meshCombinerPreferences.lightmapSupport == true) ? " (Using Lightmaps Support Too)" : ""), message, progress);

            if (show == false)
                EditorUtility.ClearProgressBar();
        }

        private string SaveAssetAsFile(string folderNameToSave, UnityEngine.Object assetToSave, string fileName, string fileExtension)
        {
            //Create the directory in project
            if (!AssetDatabase.IsValidFolder("Assets/ThirdParty/MT Assets"))
                AssetDatabase.CreateFolder("Assets", "MT Assets");
            if (!AssetDatabase.IsValidFolder("Assets/ThirdParty/MT Assets/_AssetsData"))
                AssetDatabase.CreateFolder("Assets/ThirdParty/MT Assets", "_AssetsData");
            if (!AssetDatabase.IsValidFolder("Assets/ThirdParty/MT Assets/_AssetsData"))
                AssetDatabase.CreateFolder("Assets/ThirdParty/MT Assets", "_AssetsData");
            if (!AssetDatabase.IsValidFolder("Assets/ThirdParty/MT Assets/_AssetsData/" + folderNameToSave))
                AssetDatabase.CreateFolder("Assets/ThirdParty/MT Assets/_AssetsData", folderNameToSave);

            //If the asset to save is null, cancel
            if (assetToSave == null)
                return "";

            //Get current date
            DateTime dateNow = DateTime.Now;
            string dateNowStr = dateNow.Year.ToString() + dateNow.Month.ToString() + dateNow.Day.ToString() + dateNow.Hour.ToString() + dateNow.Minute.ToString() + dateNow.Second.ToString() + dateNow.Millisecond.ToString();

            //Save the asset
            string fileDirectory = "Assets/ThirdParty/MT Assets/_AssetsData/" + folderNameToSave + "/" + fileName + " (" + dateNowStr + ")." + fileExtension;
            AssetDatabase.CreateAsset(assetToSave, fileDirectory);

            //Save all data and reload
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            //Return the path to saved asset
            return fileDirectory;
        }

        private void SaveMergeAsPrefab(string name, GameObject targetGo)
        {
            //Save the GameObject result of merge, in assets
            if (!AssetDatabase.IsValidFolder("Assets/ThirdParty/MT Assets"))
                AssetDatabase.CreateFolder("Assets", "MT Assets");
            if (!AssetDatabase.IsValidFolder("Assets/ThirdParty/MT Assets/_AssetsData"))
                AssetDatabase.CreateFolder("Assets/ThirdParty/MT Assets", "_AssetsData");
            if (!AssetDatabase.IsValidFolder("Assets/ThirdParty/MT Assets/_AssetsData/Prefabs"))
                AssetDatabase.CreateFolder("Assets/ThirdParty/MT Assets/_AssetsData", "Prefabs");

            if (AssetDatabase.LoadAssetAtPath("Assets/ThirdParty/MT Assets/_AssetsData/Prefabs/" + name + ".prefab", typeof(GameObject)) != null)
                Debug.LogWarning("Prefab \"" + name + "\" already exists in your project files. Therefore, a new file was not created.\n\n");
            if (AssetDatabase.LoadAssetAtPath("Assets/ThirdParty/MT Assets/_AssetsData/Prefabs/" + name + ".prefab", typeof(GameObject)) == null)
            {
#if !UNITY_2018_3_OR_NEWER
                UnityEngine.Object prefab = PrefabUtility.CreatePrefab("Assets/ThirdParty/MT Assets/_AssetsData/Prefabs/" + name + ".prefab", targetGo);
                PrefabUtility.ReplacePrefab(targetGo, prefab, ReplacePrefabOptions.ConnectToPrefab);
#endif
#if UNITY_2018_3_OR_NEWER
                PrefabUtility.SaveAsPrefabAssetAndConnect(targetGo, "Assets/ThirdParty/MT Assets/_AssetsData/Prefabs/" + name + ".prefab", InteractionMode.UserAction);
#endif
                Debug.Log("The prefab \"" + name + "\" was created in your project files! The path to the prefabs that the Easy Mesh Combiner creates is the \"Assets/ThirdParty/MT Assets/_AssetsData/Prefabs\"\n\n");
            }
        }

        //Tools Methods for all Core Methods

        private void DoTheSelectedActionAfterMerge(CombinedMeshesManager combinedMeshesManager, MeshRenderer[] listOfMeshesProcesseds)
        {
            //If action desired, is do nothing, cancel
            if (meshCombinerPreferences.afterMerge == MeshCombinerPreferences.AfterMerge.DoNothing)
                return;

            //Deactive original GameObjects if is desired
            if (meshCombinerPreferences.afterMerge == MeshCombinerPreferences.AfterMerge.DeactiveOriginalGameObjects)
                foreach (MeshRenderer obj in listOfMeshesProcesseds)
                {
                    combinedMeshesManager.originalsGameObjectsWithMesh.Add(new CombinedMeshesManager.OriginalGameObjectWithMesh(obj.gameObject, obj.gameObject.activeSelf, obj, obj.enabled));
                    obj.gameObject.SetActive(false);
                }

            //Disable original mesh filters and renderers if is desired
            if (meshCombinerPreferences.afterMerge == MeshCombinerPreferences.AfterMerge.DisableOriginalMeshes)
                foreach (MeshRenderer obj in listOfMeshesProcesseds)
                {
                    combinedMeshesManager.originalsGameObjectsWithMesh.Add(new CombinedMeshesManager.OriginalGameObjectWithMesh(obj.gameObject, obj.gameObject.activeSelf, obj, obj.enabled));
                    obj.enabled = false;
                }
        }

        private Material GetValidatedCopyOfMaterial(Material targetMaterial, bool copyPropertiesOfTargetMaterial, bool clearAllTextures)
        {
            //Return a copy of target material
            Material material = new Material(targetMaterial.shader);

            //Copy all propertyies, if is desired
            if (copyPropertiesOfTargetMaterial == true)
                material.CopyPropertiesFromMaterial(targetMaterial);

            //Clear all textures, is is desired
            if (clearAllTextures == true)
            {
                if (material.HasProperty("_MainTex") == true)
                    material.SetTexture("_MainTex", null);

                if (material.HasProperty("_BaseMap") == true)
                    material.SetTexture("_BaseMap", null);

                if (material.HasProperty("_MetallicGlossMap") == true)
                    material.SetTexture("_MetallicGlossMap", null);

                if (material.HasProperty("_SpecGlossMap") == true)
                    material.SetTexture("_SpecGlossMap", null);

                if (material.HasProperty("_BumpMap") == true)
                    material.SetTexture("_BumpMap", null);

                if (material.HasProperty("_DetailNormalMap") == true)
                    material.SetTexture("_DetailNormalMap", null);

                if (material.HasProperty("_ParallaxMap") == true)
                    material.SetTexture("_ParallaxMap", null);

                if (material.HasProperty("_OcclusionMap") == true)
                    material.SetTexture("_OcclusionMap", null);

                if (material.HasProperty("_DetailMapSupport") == true)
                    material.SetTexture("_DetailMapSupport", null);

                if (material.HasProperty("_DetailMask") == true)
                    material.SetTexture("_DetailMask", null);

                if (material.HasProperty("_Color") == true)
                    material.SetColor("_Color", Color.white);

                if (material.HasProperty("_BaseColor") == true)
                    material.SetColor("_BaseColor", Color.white);
            }

            return material;
        }

        //Tools methods for All In One merge method

        private bool isTiledTexture(Vector2[] uvOfSubMesh, Material materialOfMesh)
        {
            //Return if the bounds is major than one
            bool isTiled = false;

            //Check if have tiling on uv
            float[] xAxis = new float[uvOfSubMesh.Length];
            float[] yAxis = new float[uvOfSubMesh.Length];
            //Fill all
            for (int i = 0; i < uvOfSubMesh.Length; i++)
            {
                xAxis[i] = uvOfSubMesh[i].x;
                yAxis[i] = uvOfSubMesh[i].y;
            }
            //Return the data size
            float majorX = Mathf.Max(xAxis);
            float majorY = Mathf.Max(yAxis);
            float minorX = Mathf.Min(xAxis);
            float minorY = Mathf.Min(yAxis);
            if (minorX < 0 || minorY < 0 || majorX > 1 || majorY > 1)
                isTiled = true;

            //Check if have tiling on material
            if (materialOfMesh.mainTextureScale.x != 1.0f || materialOfMesh.mainTextureScale.y != 1.0f)
                isTiled = true;

            return isTiled;
        }

        private Dictionary<Texture2D, bool> ExtractReferenceOfAllTexturesThatWillBeUsed(Material[] materialsThatWillBeUsed)
        {
            //This method will return a dictionary of all textures that will be used in this merge, and if RW is enabled in each
            Dictionary<Texture2D, bool> allTextures = new Dictionary<Texture2D, bool>();

            foreach (Material material in materialsThatWillBeUsed)
            {
                //Check for each texture that can be used and add to list
                Texture2D mainTexture = null;
                if (material.HasProperty(meshCombinerPreferences.allInOneParams.mainTexturePropertyToFind) == true)
                    mainTexture = (Texture2D)material.GetTexture(meshCombinerPreferences.allInOneParams.mainTexturePropertyToFind);
                Texture2D metallicMap = null;
                if (meshCombinerPreferences.allInOneParams.metallicMapSupport == true && material.HasProperty(meshCombinerPreferences.allInOneParams.metallicMapPropertyToFind) == true)
                    metallicMap = (Texture2D)material.GetTexture(meshCombinerPreferences.allInOneParams.metallicMapPropertyToFind);
                Texture2D specularMap = null;
                if (meshCombinerPreferences.allInOneParams.specularMapSupport == true && material.HasProperty(meshCombinerPreferences.allInOneParams.specularMapPropertyToFind) == true)
                    specularMap = (Texture2D)material.GetTexture(meshCombinerPreferences.allInOneParams.specularMapPropertyToFind);
                Texture2D normalMap = null;
                if (meshCombinerPreferences.allInOneParams.normalMapSupport == true && material.HasProperty(meshCombinerPreferences.allInOneParams.normalMapPropertyToFind) == true)
                    normalMap = (Texture2D)material.GetTexture(meshCombinerPreferences.allInOneParams.normalMapPropertyToFind);
                Texture2D normal2Map = null;
                if (meshCombinerPreferences.allInOneParams.normalMap2Support == true && material.HasProperty(meshCombinerPreferences.allInOneParams.normalMap2PropertyFind) == true)
                    normal2Map = (Texture2D)material.GetTexture(meshCombinerPreferences.allInOneParams.normalMap2PropertyFind);
                Texture2D heightMap = null;
                if (meshCombinerPreferences.allInOneParams.heightMapSupport == true && material.HasProperty(meshCombinerPreferences.allInOneParams.heightMapPropertyToFind) == true)
                    heightMap = (Texture2D)material.GetTexture(meshCombinerPreferences.allInOneParams.heightMapPropertyToFind);
                Texture2D occlusionMap = null;
                if (meshCombinerPreferences.allInOneParams.occlusionMapSupport == true && material.HasProperty(meshCombinerPreferences.allInOneParams.occlusionMapPropertyToFind) == true)
                    occlusionMap = (Texture2D)material.GetTexture(meshCombinerPreferences.allInOneParams.occlusionMapPropertyToFind);
                Texture2D detailMap = null;
                if (meshCombinerPreferences.allInOneParams.detailAlbedoMapSupport == true && material.HasProperty(meshCombinerPreferences.allInOneParams.detailMapPropertyToFind) == true)
                    detailMap = (Texture2D)material.GetTexture(meshCombinerPreferences.allInOneParams.detailMapPropertyToFind);
                Texture2D detailMask = null;
                if (meshCombinerPreferences.allInOneParams.detailMaskSupport == true && material.HasProperty(meshCombinerPreferences.allInOneParams.detailMaskPropertyToFind) == true)
                    detailMask = (Texture2D)material.GetTexture(meshCombinerPreferences.allInOneParams.detailMaskPropertyToFind);

                //Add the textures to list
                if (mainTexture != null && allTextures.ContainsKey(mainTexture) == false)
                    allTextures.Add(mainTexture, mainTexture.isReadable);
                if (metallicMap != null && allTextures.ContainsKey(metallicMap) == false)
                    allTextures.Add(metallicMap, metallicMap.isReadable);
                if (specularMap != null && allTextures.ContainsKey(specularMap) == false)
                    allTextures.Add(specularMap, specularMap.isReadable);
                if (normalMap != null && allTextures.ContainsKey(normalMap) == false)
                    allTextures.Add(normalMap, normalMap.isReadable);
                if (normal2Map != null && allTextures.ContainsKey(normal2Map) == false)
                    allTextures.Add(normal2Map, normal2Map.isReadable);
                if (heightMap != null && allTextures.ContainsKey(heightMap) == false)
                    allTextures.Add(heightMap, heightMap.isReadable);
                if (occlusionMap != null && allTextures.ContainsKey(occlusionMap) == false)
                    allTextures.Add(occlusionMap, occlusionMap.isReadable);
                if (detailMap != null && allTextures.ContainsKey(detailMap) == false)
                    allTextures.Add(detailMap, detailMap.isReadable);
                if (detailMask != null && allTextures.ContainsKey(detailMask) == false)
                    allTextures.Add(detailMask, detailMask.isReadable);
            }

            //Return the dictionary
            return allTextures;
        }

        private int GetCountOfStepsNeededToProcessEachTextureInThisMerge(int countOfMaterialsThatWillBeUsed)
        {
            //Return a count of quantity of textures that will be processed in this merge
            int count = 1;

            if (meshCombinerPreferences.allInOneParams.metallicMapSupport == true)
                count += 1;
            if (meshCombinerPreferences.allInOneParams.specularMapSupport == true)
                count += 1;
            if (meshCombinerPreferences.allInOneParams.normalMapSupport == true)
                count += 1;
            if (meshCombinerPreferences.allInOneParams.normalMap2Support == true)
                count += 1;
            if (meshCombinerPreferences.allInOneParams.heightMapSupport == true)
                count += 1;
            if (meshCombinerPreferences.allInOneParams.occlusionMapSupport == true)
                count += 1;
            if (meshCombinerPreferences.allInOneParams.detailAlbedoMapSupport == true)
                count += 1;
            if (meshCombinerPreferences.allInOneParams.detailMaskSupport == true)
                count += 1;

            //Multiplie by materials that will be processed
            count = count * countOfMaterialsThatWillBeUsed;

            //Return the count
            return count;
        }

        private void EnableReadWriteForAllTexturesThatWillBeUsedInThisMerge(Dictionary<Texture2D, bool> allTexturesThatWillBeUsed, float progress)
        {
            //This method will enable Read/Write in all textures that will be used in this merge
            int texturesProcessedsAtHere = 0;
            foreach (var item in allTexturesThatWillBeUsed)
            {
                ShowProgressBar("Preparing Textures... (" + texturesProcessedsAtHere + "/" + allTexturesThatWillBeUsed.Count + ")", true, progress);
                TextureImporter textureImporter = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(item.Key));
                if (textureImporter.isReadable == false)
                    textureImporter.isReadable = true;
                AssetDatabase.ImportAsset(textureImporter.assetPath);
                AssetDatabase.Refresh();
                texturesProcessedsAtHere += 1;
                ShowProgressBar("Preparing Textures... (" + texturesProcessedsAtHere + "/" + allTexturesThatWillBeUsed.Count + ")", true, progress);
            }
        }

        private Texture2D GetSimpleCopyOfTexture(Material materialToFindTexture, string propertyToFindTexture, int widthOfCorrespondentMainTexture, int heightOfCorrespondentMainTexture, TextureType textureType, float progress, int texturesProcessedAtHere, int totalTexturesForProcess)
        {
            //Show progress
            ShowProgressBar("Copying Texture " + propertyToFindTexture + " (" + texturesProcessedAtHere + "/" + totalTexturesForProcess + ")", true, progress);

            //-------------------------------------------- Create a refereference to target texture
            //Try to get the texture of material
            Texture2D targetTexture = null;
            materialToFindTexture.EnableKeyword(propertyToFindTexture);

            //If found the property of texture
            if (materialToFindTexture.HasProperty(propertyToFindTexture) == true && materialToFindTexture.GetTexture(propertyToFindTexture) != null)
                targetTexture = (Texture2D)materialToFindTexture.GetTexture(propertyToFindTexture);

            //If not found the property of texture
            if (materialToFindTexture.HasProperty(propertyToFindTexture) == false || materialToFindTexture.GetTexture(propertyToFindTexture) == null)
            {
                //Get the default and neutral color for this texture
                ColorData defaultColor = GetDefaultAndNeutralColorForThisTexture(textureType);
                //Launch log
                logsOfBeforeMerge.Add(new LogOfMerge(MessageType.Warning, "It was not possible to find the texture stored in property \"" + propertyToFindTexture + "\" of material \"" + materialToFindTexture.name + "\", so this Texture/Map was replaced by a " + defaultColor.colorName + " texture. This can affect how the texture or effect maps (such as Normal Maps, etc.) are displayed in the combined model. This can result in some small differences in the combined mesh when compared to the separate original meshes."));
                //Create a fake texture blank
                targetTexture = new Texture2D(widthOfCorrespondentMainTexture, heightOfCorrespondentMainTexture);
                //Create blank pixels
                Color[] colors = new Color[widthOfCorrespondentMainTexture * heightOfCorrespondentMainTexture];
                for (int i = 0; i < colors.Length; i++)
                    colors[i] = defaultColor.color;
                //Apply all pixels in void texture
                targetTexture.SetPixels(0, 0, widthOfCorrespondentMainTexture, heightOfCorrespondentMainTexture, colors, 0);
            }

            //-------------------------------------------- Start the creation of copyied texture
            //Prepare the storage for this texture that will be copyied
            Texture2D thisTexture = null;

            //If the texture is readable
            try
            {
                //-------------------------------------------- Calculate the size of copyied texture
                //Get desired edges size for each texture of atlas
                int edgesSize = GetEdgesSizeForTextures();

                //Calculate a preview of the total and final size of texture...
                int texWidth = targetTexture.width;
                int texHeight = targetTexture.height;
                //Create the texture with size calculated above
                thisTexture = new Texture2D(texWidth, texHeight, TextureFormat.ARGB32, targetTexture.mipmapCount, false);

                //-------------------------------------------- Copy all original pixels from target texture reference
                //Copy all pixels of the target texture
                Color32[] targetTexturePixels = targetTexture.GetPixels32(0);
                //If pink normal maps fix is enabled. If this is a normal map, try to get colors using different decoding (if have a compression format that uses different channels to store colors)
                if (meshCombinerPreferences.allInOneParams.pinkNormalMapsFix == true && textureType == TextureType.NormalMap && targetTexture.format == TextureFormat.DXT5)
                    for (int i = 0; i < targetTexturePixels.Length; i++)
                    {
                        Color c = targetTexturePixels[i];
                        c.r = c.a * 2 - 1;  //red<-alpha (x<-w)
                        c.g = c.g * 2 - 1; //green is always the same (y)
                        Vector2 xy = new Vector2(c.r, c.g); //this is the xy vector
                        c.b = Mathf.Sqrt(1 - Mathf.Clamp01(Vector2.Dot(xy, xy))); //recalculate the blue channel (z)
                        targetTexturePixels[i] = new Color(c.r * 0.5f + 0.5f, c.g * 0.5f + 0.5f, c.b * 0.5f + 0.5f); //back to 0-1 range
                    }

                //-------------------------------------------- Create a copy of target texture
                //Apply the copyied pixels to this texture if is normal texture
                thisTexture.SetPixels32(0, 0, targetTexture.width, targetTexture.height, targetTexturePixels, 0);
            }
            //If the texture is not readable
            catch (UnityException e)
            {
                if (e.Message.StartsWith("Texture '" + targetTexture.name + "' is not readable"))
                {
                    //Get the default and neutral color for this texture
                    ColorData defaultColor = GetDefaultAndNeutralColorForThisTexture(textureType);

                    //Create the texture
                    thisTexture = new Texture2D(widthOfCorrespondentMainTexture, heightOfCorrespondentMainTexture, TextureFormat.ARGB32, false, false);

                    //Create blank pixels
                    Color[] colors = new Color[widthOfCorrespondentMainTexture * heightOfCorrespondentMainTexture];
                    for (int i = 0; i < colors.Length; i++)
                        colors[i] = defaultColor.color;

                    //Apply all pixels in void texture
                    thisTexture.SetPixels(0, 0, widthOfCorrespondentMainTexture, heightOfCorrespondentMainTexture, colors, 0);

                    //Launch logs
                    logsOfBeforeMerge.Add(new LogOfMerge(MessageType.Warning, "It was not possible to combine texture \"" + targetTexture.name + "\" within an atlas, as it is not marked as \"Readable\" in the import settings (\"Read/Write Enabled\"). The texture has been replaced with a " + defaultColor.colorName + " one."));
                }
            }

            //-------------------------------------------- Finally, resize the copy texture to mantain size equal to targe texture with edges
            //If this texture have the size differente of correspondent main texture size, resize it to be equal to main texture 
            if (thisTexture.width != widthOfCorrespondentMainTexture || thisTexture.height != heightOfCorrespondentMainTexture)
                EMCTextureResizer.Bilinear(thisTexture, widthOfCorrespondentMainTexture, heightOfCorrespondentMainTexture);

            //Return the texture 
            return thisTexture;
        }

        private Texture2D GetValidatedCopyOfTexture(Material materialToFindTexture, string propertyToFindTexture, int widthOfCorrespondentMainTexture, int heightOfCorrespondentMainTexture, TextureType textureType, float progress, int texturesProcessedAtHere, int totalTexturesForProcess)
        {
            //Show progress
            ShowProgressBar("Copying Texture " + propertyToFindTexture + " (" + texturesProcessedAtHere + "/" + totalTexturesForProcess + ")", true, progress);

            //-------------------------------------------- Create a refereference to target texture
            //Try to get the texture of material
            Texture2D targetTexture = null;
            materialToFindTexture.EnableKeyword(propertyToFindTexture);

            //If found the property of texture
            if (materialToFindTexture.HasProperty(propertyToFindTexture) == true && materialToFindTexture.GetTexture(propertyToFindTexture) != null)
                targetTexture = (Texture2D)materialToFindTexture.GetTexture(propertyToFindTexture);

            //If not found the property of texture
            if (materialToFindTexture.HasProperty(propertyToFindTexture) == false || materialToFindTexture.GetTexture(propertyToFindTexture) == null)
            {
                //Get the default and neutral color for this texture
                ColorData defaultColor = GetDefaultAndNeutralColorForThisTexture(textureType);
                //Launch log
                logsOfBeforeMerge.Add(new LogOfMerge(MessageType.Warning, "It was not possible to find the texture stored in property \"" + propertyToFindTexture + "\" of material \"" + materialToFindTexture.name + "\", so this Texture/Map was replaced by a " + defaultColor.colorName + " texture. This can affect how the texture or effect maps (such as Normal Maps, etc.) are displayed in the combined model. This can result in some small differences in the combined mesh when compared to the separate original meshes."));
                //Create a fake texture blank
                targetTexture = new Texture2D(widthOfCorrespondentMainTexture, heightOfCorrespondentMainTexture);
                //Create blank pixels
                Color[] colors = new Color[widthOfCorrespondentMainTexture * heightOfCorrespondentMainTexture];
                for (int i = 0; i < colors.Length; i++)
                    colors[i] = defaultColor.color;
                //Apply all pixels in void texture
                targetTexture.SetPixels(0, 0, widthOfCorrespondentMainTexture, heightOfCorrespondentMainTexture, colors, 0);
            }

            //-------------------------------------------- Start the creation of copyied texture
            //Prepare the storage for this texture that will be copyied
            Texture2D thisTexture = null;

            //If the texture is readable
            try
            {
                //-------------------------------------------- Calculate the size of copyied texture
                //Get desired edges size for each texture of atlas
                int edgesSize = GetEdgesSizeForTextures();

                //Calculate a preview of the total and final size of texture...
                int texWidth = 0;
                int texHeight = 0;
                //If is a normal texture
                texWidth = edgesSize + targetTexture.width + edgesSize;
                texHeight = edgesSize + targetTexture.height + edgesSize;
                //Create the texture with size calculated above
                thisTexture = new Texture2D(texWidth, texHeight, TextureFormat.ARGB32, targetTexture.mipmapCount, false);

                //-------------------------------------------- Copy all original pixels from target texture reference
                //Copy all pixels of the target texture
                Color32[] targetTexturePixels = targetTexture.GetPixels32(0);
                //If pink normal maps fix is enabled. If this is a normal map, try to get colors using different decoding (if have a compression format that uses different channels to store colors)
                if (meshCombinerPreferences.allInOneParams.pinkNormalMapsFix == true && textureType == TextureType.NormalMap && targetTexture.format == TextureFormat.DXT5)
                    for (int i = 0; i < targetTexturePixels.Length; i++)
                    {
                        Color c = targetTexturePixels[i];
                        c.r = c.a * 2 - 1;  //red<-alpha (x<-w)
                        c.g = c.g * 2 - 1; //green is always the same (y)
                        Vector2 xy = new Vector2(c.r, c.g); //this is the xy vector
                        c.b = Mathf.Sqrt(1 - Mathf.Clamp01(Vector2.Dot(xy, xy))); //recalculate the blue channel (z)
                        targetTexturePixels[i] = new Color(c.r * 0.5f + 0.5f, c.g * 0.5f + 0.5f, c.b * 0.5f + 0.5f); //back to 0-1 range
                    }

                //-------------------------------------------- Create a copy of target texture
                //Apply the copyied pixels to this texture if is normal texture
                thisTexture.SetPixels32(edgesSize, edgesSize, targetTexture.width, targetTexture.height, targetTexturePixels, 0);

                //-------------------------------------------- Create the edges of copy texture, to support mip maps
                //If the edges size is minor than target texture size, uses the "SetPixels and GetPixels" to guarantee a faster copy
                if (edgesSize <= targetTexture.width && edgesSize <= targetTexture.height)
                {
                    //Prepare the var
                    Color[] copyiedPixels = null;

                    //Copy right border to left of current texture
                    copyiedPixels = thisTexture.GetPixels(thisTexture.width - edgesSize - edgesSize, 0, edgesSize, thisTexture.height, 0);
                    thisTexture.SetPixels(0, 0, edgesSize, thisTexture.height, copyiedPixels, 0);

                    //Copy left(original) border to right of current texture
                    copyiedPixels = thisTexture.GetPixels(edgesSize, 0, edgesSize, thisTexture.height, 0);
                    thisTexture.SetPixels(thisTexture.width - edgesSize, 0, edgesSize, thisTexture.height, copyiedPixels, 0);

                    //Copy bottom (original) border to top of current texture
                    copyiedPixels = thisTexture.GetPixels(0, edgesSize, thisTexture.width, edgesSize, 0);
                    thisTexture.SetPixels(0, thisTexture.height - edgesSize, thisTexture.width, edgesSize, copyiedPixels, 0);

                    //Copy top (original) border to bottom of current texture
                    copyiedPixels = thisTexture.GetPixels(0, thisTexture.height - edgesSize - edgesSize, thisTexture.width, edgesSize, 0);
                    thisTexture.SetPixels(0, 0, thisTexture.width, edgesSize, copyiedPixels, 0);
                }

                //If the edges size is major than target texture size, uses the "SetPixel and GetPixel" to repeat copy of pixels in target texture
                if (edgesSize > targetTexture.width || edgesSize > targetTexture.height)
                {
                    //Show the warning
                    logsOfBeforeMerge.Add(new LogOfMerge(MessageType.Warning, "You have selected a texture border size (" + edgesSize + "px), where the border size is larger than this texture (\"" + targetTexture.name + "\" " + targetTexture.width + "x" + targetTexture.height + "px) size itself, causing this texture to repeat in the atlas. This increased the merging time due to the need for a new algorithm for creating the borders. It is recommended that the size of the edges of the textures in the atlas, does not exceed the size of the textures themselves."));

                    //Copy right (original) border to left of current texture
                    for (int x = 0; x < edgesSize; x++)
                        for (int y = 0; y < thisTexture.height; y++)
                            thisTexture.SetPixel(x, y, targetTexture.GetPixel((targetTexture.width - edgesSize - edgesSize) + x, y));

                    //Copy left(original) border to right of current texture
                    for (int x = thisTexture.width - edgesSize; x < thisTexture.width; x++)
                        for (int y = 0; y < thisTexture.height; y++)
                            thisTexture.SetPixel(x, y, targetTexture.GetPixel(targetTexture.width - x, y));

                    //Copy bottom (original) border to top of current texture
                    for (int x = 0; x < thisTexture.width; x++)
                        for (int y = 0; y < edgesSize; y++)
                            thisTexture.SetPixel(x, y, targetTexture.GetPixel(x, (targetTexture.width - edgesSize) + y));

                    //Copy top (original) border to bottom of current texture
                    for (int x = 0; x < thisTexture.width; x++)
                        for (int y = thisTexture.height - edgesSize; y < thisTexture.height; y++)
                            thisTexture.SetPixel(x, y, targetTexture.GetPixel(x, edgesSize - (targetTexture.height - y)));
                }
            }
            //If the texture is not readable
            catch (UnityException e)
            {
                if (e.Message.StartsWith("Texture '" + targetTexture.name + "' is not readable"))
                {
                    //Get the default and neutral color for this texture
                    ColorData defaultColor = GetDefaultAndNeutralColorForThisTexture(textureType);

                    //Create the texture
                    thisTexture = new Texture2D(widthOfCorrespondentMainTexture, heightOfCorrespondentMainTexture, TextureFormat.ARGB32, false, false);

                    //Create blank pixels
                    Color[] colors = new Color[widthOfCorrespondentMainTexture * heightOfCorrespondentMainTexture];
                    for (int i = 0; i < colors.Length; i++)
                        colors[i] = defaultColor.color;

                    //Apply all pixels in void texture
                    thisTexture.SetPixels(0, 0, widthOfCorrespondentMainTexture, heightOfCorrespondentMainTexture, colors, 0);

                    //Launch logs
                    logsOfBeforeMerge.Add(new LogOfMerge(MessageType.Warning, "It was not possible to combine texture \"" + targetTexture.name + "\" within an atlas, as it is not marked as \"Readable\" in the import settings (\"Read/Write Enabled\"). The texture has been replaced with a " + defaultColor.colorName + " one."));
                }
            }

            //-------------------------------------------- Finally, resize the copy texture to mantain size equal to targe texture with edges
            //If this texture have the size differente of correspondent main texture size, resize it to be equal to main texture 
            if (thisTexture.width != widthOfCorrespondentMainTexture || thisTexture.height != heightOfCorrespondentMainTexture)
                EMCTextureResizer.Bilinear(thisTexture, widthOfCorrespondentMainTexture, heightOfCorrespondentMainTexture);

            //Return the texture 
            return thisTexture;
        }

        private ColorData GetDefaultAndNeutralColorForThisTexture(TextureType textureType)
        {
            //Return the neutral color for texture type
            switch (textureType)
            {
                case TextureType.MainTexture:
                    return new ColorData("RED", Color.red);
                case TextureType.MetallicMap:
                    return new ColorData("BLACK", Color.black);
                case TextureType.SpecularMap:
                    return new ColorData("BLACK", Color.black);
                case TextureType.NormalMap:
                    return new ColorData("PURPLE", new Color(128.0f / 255.0f, 128.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f));
                case TextureType.HeightMap:
                    return new ColorData("BLACK", Color.black);
                case TextureType.OcclusionMap:
                    return new ColorData("WHITE", Color.white);
                case TextureType.DetailMap:
                    return new ColorData("GRAY", Color.gray);
                case TextureType.DetailMask:
                    return new ColorData("WHITE", Color.white);
            }
            return new ColorData("RED", Color.red);
        }

        private int GetEdgesSizeForTextures()
        {
            //If is All In One
            if (meshCombinerPreferences.mergeMethod == MeshCombinerPreferences.MergeMethod.AllInOne)
            {
                switch (meshCombinerPreferences.allInOneParams.mipMapEdgesSize)
                {
                    case MeshCombinerPreferences.MipMapEdgesSize.Pixels0x0:
                        return 0;
                    case MeshCombinerPreferences.MipMapEdgesSize.Pixels16x16:
                        return 16;
                    case MeshCombinerPreferences.MipMapEdgesSize.Pixels32x32:
                        return 32;
                    case MeshCombinerPreferences.MipMapEdgesSize.Pixels64x64:
                        return 64;
                    case MeshCombinerPreferences.MipMapEdgesSize.Pixels128x128:
                        return 128;
                    case MeshCombinerPreferences.MipMapEdgesSize.Pixels256x256:
                        return 256;
                    case MeshCombinerPreferences.MipMapEdgesSize.Pixels512x512:
                        return 512;
                    case MeshCombinerPreferences.MipMapEdgesSize.Pixels1024x1024:
                        return 1024;
                }
            }

            //Return the max resolution
            return 2;
        }

        private int GetAtlasPadding()
        {
            //If is All In One
            if (meshCombinerPreferences.mergeMethod == MeshCombinerPreferences.MergeMethod.AllInOne)
            {
                switch (meshCombinerPreferences.allInOneParams.atlasPadding)
                {
                    case MeshCombinerPreferences.AtlasPadding.Pixels0x0:
                        return 0;
                    case MeshCombinerPreferences.AtlasPadding.Pixels2x2:
                        return 2;
                    case MeshCombinerPreferences.AtlasPadding.Pixels4x4:
                        return 4;
                    case MeshCombinerPreferences.AtlasPadding.Pixels8x8:
                        return 8;
                    case MeshCombinerPreferences.AtlasPadding.Pixels16x16:
                        return 16;
                }
            }

            //Return the max resolution
            return 0;
        }

        private void ApplyAtlasInPropertyOfMaterial(Material targetMaterial, string propertyToInsertTexture, Texture2D atlasTexture)
        {
            //If found the property
            if (targetMaterial.HasProperty(propertyToInsertTexture) == true)
            {
                //Try to enable this different keyword
                if (targetMaterial.IsKeywordEnabled(propertyToInsertTexture) == false)
                    targetMaterial.EnableKeyword(propertyToInsertTexture);

                //Apply the texture
                targetMaterial.SetTexture(propertyToInsertTexture, atlasTexture);

                //Try to enable this different keyword
                if (targetMaterial.IsKeywordEnabled(propertyToInsertTexture) == false)
                    targetMaterial.EnableKeyword(propertyToInsertTexture);

                //Forces enable all keyword, where is necessary
                if (propertyToInsertTexture == "_MetallicGlossMap" && targetMaterial.IsKeywordEnabled("_METALLICGLOSSMAP") == false && meshCombinerPreferences.allInOneParams.metallicMapSupport == true)
                    targetMaterial.EnableKeyword("_METALLICGLOSSMAP");

                if (propertyToInsertTexture == "_SpecGlossMap" && targetMaterial.IsKeywordEnabled("_SPECGLOSSMAP") == false && meshCombinerPreferences.allInOneParams.specularMapSupport == true)
                    targetMaterial.EnableKeyword("_SPECGLOSSMAP");

                if (propertyToInsertTexture == "_BumpMap" && targetMaterial.IsKeywordEnabled("_NORMALMAP") == false && meshCombinerPreferences.allInOneParams.normalMapSupport == true)
                    targetMaterial.EnableKeyword("_NORMALMAP");

                if (propertyToInsertTexture == "_ParallaxMap" && targetMaterial.IsKeywordEnabled("_PARALLAXMAP") == false && meshCombinerPreferences.allInOneParams.heightMapSupport == true)
                    targetMaterial.EnableKeyword("_PARALLAXMAP");

                if (propertyToInsertTexture == "_OcclusionMap" && targetMaterial.IsKeywordEnabled("_OcclusionMap") == false && meshCombinerPreferences.allInOneParams.occlusionMapSupport == true)
                    targetMaterial.EnableKeyword("_OcclusionMap");

                if (propertyToInsertTexture == "_DetailAlbedoMap" && targetMaterial.IsKeywordEnabled("_DETAIL_MULX2") == false && meshCombinerPreferences.allInOneParams.detailAlbedoMapSupport == true)
                    targetMaterial.EnableKeyword("_DETAIL_MULX2");

                if (propertyToInsertTexture == "_DetailNormalMap" && targetMaterial.IsKeywordEnabled("_DETAIL_MULX2") == false && meshCombinerPreferences.allInOneParams.normalMap2Support == true)
                    targetMaterial.EnableKeyword("_DETAIL_MULX2");
            }
            //If not found the property
            if (targetMaterial.HasProperty(propertyToInsertTexture) == false)
                logsOfBeforeMerge.Add(new LogOfMerge(MessageType.Warning, "It was not possible to find and apply the atlas on property \"" + propertyToInsertTexture + "\" of the material to use (\"" + targetMaterial.name + "\"). Therefore, no atlas was applied to this property."));
        }

        private TexturesSubMeshes GetTheTextureSubMeshesOfMaterial(Material material, List<TexturesSubMeshes> listOfTexturesAndSubMeshes)
        {
            //Run a loop to return the texture and respective submeshes that use this material
            foreach (TexturesSubMeshes item in listOfTexturesAndSubMeshes)
                if (item.material == material)
                    return item;

            //If not found a item with this material, return null
            return null;
        }

        private AtlasData CreateAllAtlas(List<TexturesSubMeshes> copyiedTextures, int maxResolution, int paddingBetweenTextures)
        {
            //Create a atlas
            AtlasData atlasData = new AtlasData();
            List<Texture2D> texturesToUse = new List<Texture2D>();

            //Create the base atlas with main textures
            texturesToUse.Clear();
            foreach (TexturesSubMeshes item in copyiedTextures)
                texturesToUse.Add(item.mainTexture);
            atlasData.originalMainTexturesUsedAndOrdenedAccordingToAtlasRect = texturesToUse.ToArray();
            atlasData.atlasRects = atlasData.mainTextureAtlas.PackTextures(texturesToUse.ToArray(), paddingBetweenTextures, maxResolution);

            //Create the metallic atlas if is desired
            if (meshCombinerPreferences.allInOneParams.metallicMapSupport == true)
            {
                texturesToUse.Clear();
                foreach (TexturesSubMeshes item in copyiedTextures)
                    texturesToUse.Add(item.metallicMap);
                atlasData.metallicMapAtlas.PackTextures(texturesToUse.ToArray(), paddingBetweenTextures, maxResolution);
            }

            //Create the specullar atlas if is desired
            if (meshCombinerPreferences.allInOneParams.specularMapSupport == true)
            {
                texturesToUse.Clear();
                foreach (TexturesSubMeshes item in copyiedTextures)
                    texturesToUse.Add(item.specularMap);
                atlasData.specularMapAtlas.PackTextures(texturesToUse.ToArray(), paddingBetweenTextures, maxResolution);
            }

            //Create the normal atlas if is desired
            if (meshCombinerPreferences.allInOneParams.normalMapSupport == true)
            {
                texturesToUse.Clear();
                foreach (TexturesSubMeshes item in copyiedTextures)
                    texturesToUse.Add(item.normalMap);
                atlasData.normalMapAtlas.PackTextures(texturesToUse.ToArray(), paddingBetweenTextures, maxResolution);
            }

            //Create the normal 2 atlas if is desired
            if (meshCombinerPreferences.allInOneParams.normalMap2Support == true)
            {
                texturesToUse.Clear();
                foreach (TexturesSubMeshes item in copyiedTextures)
                    texturesToUse.Add(item.normalMap2);
                atlasData.normalMap2Atlas.PackTextures(texturesToUse.ToArray(), paddingBetweenTextures, maxResolution);
            }

            //Create the height atlas if is desired
            if (meshCombinerPreferences.allInOneParams.heightMapSupport == true)
            {
                texturesToUse.Clear();
                foreach (TexturesSubMeshes item in copyiedTextures)
                    texturesToUse.Add(item.heightMap);
                atlasData.heightMapAtlas.PackTextures(texturesToUse.ToArray(), paddingBetweenTextures, maxResolution);
            }

            //Create the occlusion atlas if is desired
            if (meshCombinerPreferences.allInOneParams.occlusionMapSupport == true)
            {
                texturesToUse.Clear();
                foreach (TexturesSubMeshes item in copyiedTextures)
                    texturesToUse.Add(item.occlusionMap);
                atlasData.occlusionMapAtlas.PackTextures(texturesToUse.ToArray(), paddingBetweenTextures, maxResolution);
            }

            //Create the detail atlas if is desired
            if (meshCombinerPreferences.allInOneParams.detailAlbedoMapSupport == true)
            {
                texturesToUse.Clear();
                foreach (TexturesSubMeshes item in copyiedTextures)
                    texturesToUse.Add(item.detailMap);
                atlasData.detailMapAtlas.PackTextures(texturesToUse.ToArray(), paddingBetweenTextures, maxResolution);
            }

            //Create the detail mask if is desired
            if (meshCombinerPreferences.allInOneParams.detailMaskSupport == true)
            {
                texturesToUse.Clear();
                foreach (TexturesSubMeshes item in copyiedTextures)
                    texturesToUse.Add(item.detailMask);
                atlasData.detailMaskAtlas.PackTextures(texturesToUse.ToArray(), paddingBetweenTextures, maxResolution);
            }

            //Return the object
            return atlasData;
        }

        private int GetAtlasMaxResolution()
        {
            //If is All In One
            if (meshCombinerPreferences.mergeMethod == MeshCombinerPreferences.MergeMethod.AllInOne)
            {
                switch (meshCombinerPreferences.allInOneParams.atlasResolution)
                {
                    case MeshCombinerPreferences.AtlasSize.Pixels32x32:
                        return 32;
                    case MeshCombinerPreferences.AtlasSize.Pixels64x64:
                        return 64;
                    case MeshCombinerPreferences.AtlasSize.Pixels128x128:
                        return 128;
                    case MeshCombinerPreferences.AtlasSize.Pixels256x256:
                        return 256;
                    case MeshCombinerPreferences.AtlasSize.Pixels512x512:
                        return 512;
                    case MeshCombinerPreferences.AtlasSize.Pixels1024x1024:
                        return 1024;
                    case MeshCombinerPreferences.AtlasSize.Pixels2048x2048:
                        return 2048;
                    case MeshCombinerPreferences.AtlasSize.Pixels4096x4096:
                        return 4096;
                    case MeshCombinerPreferences.AtlasSize.Pixels8192x8192:
                        return 8192;
                }
            }

            //Return the max resolution
            return 16;
        }

        private void RestoreOriginalStateOfReadWriteForAllTexturesUsedInThisMerge(Dictionary<Texture2D, bool> allTexturesThatWillBeUsed, float progress)
        {
            //This method will restore original states of Read/Write for all textures used in this merge
            int texturesProcessedsAtHere = 0;
            foreach (var item in allTexturesThatWillBeUsed)
            {
                ShowProgressBar("Finishing Textures... (" + texturesProcessedsAtHere + "/" + allTexturesThatWillBeUsed.Count + ")", true, progress);
                TextureImporter textureImporter = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(item.Key));
                if (textureImporter.isReadable != item.Value)
                    textureImporter.isReadable = item.Value;
                AssetDatabase.ImportAsset(textureImporter.assetPath);
                AssetDatabase.Refresh();
                texturesProcessedsAtHere += 1;
                ShowProgressBar("Finishing Textures... (" + texturesProcessedsAtHere + "/" + allTexturesThatWillBeUsed.Count + ")", true, progress);
            }
        }

        //Tools methods for Just Material Colors merge method

        private Texture2D GetTextureFilledWithColorOfMaterial(Material targetMaterial, string colorPropertyToFind, int width, int height)
        {
            //Prepares the new texture, and color to fill the texture
            Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false, false);
            Color colorToFillTexture = Color.white;

            //If found the property of color
            if (targetMaterial.HasProperty(colorPropertyToFind) == true)
                colorToFillTexture = targetMaterial.GetColor(colorPropertyToFind);

            //If not found the property of color
            if (targetMaterial.HasProperty(colorPropertyToFind) == false)
            {
                //Launch log
                logsOfBeforeMerge.Add(new LogOfMerge(MessageType.Warning, "It was not possible to find the color stored in property \"" + colorPropertyToFind + "\" of material \"" + targetMaterial.name + "\", so this Color was replaced by a GRAY texture."));

                //Set the fake color
                colorToFillTexture = Color.gray;
            }

            //Create all pixels
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = colorToFillTexture;

            //Fill the texture
            texture.SetPixels(0, 0, width, height, pixels, 0);

            //Return the texture
            return texture;
        }

        private ColorAtlasData CreateColorAtlas(UvDataAndColorOfThisSubmesh[] uvDatasAndColors, int maxResolution, int paddingBetweenTextures, bool showProgress)
        {
            //Create a atlas
            ColorAtlasData atlasData = new ColorAtlasData();
            List<Texture2D> texturesToUse = new List<Texture2D>();

            //Create the base atlas with main textures
            if (showProgress == true)
                ShowProgressBar("Creating Colors Atlas...", true, 1.0f);
            texturesToUse.Clear();
            foreach (UvDataAndColorOfThisSubmesh item in uvDatasAndColors)
                texturesToUse.Add(item.textureColor);
            atlasData.originalTexturesUsedAndOrdenedAccordingToAtlasRect = texturesToUse.ToArray();
            atlasData.atlasRects = atlasData.colorAtlas.PackTextures(texturesToUse.ToArray(), paddingBetweenTextures, maxResolution);

            //Return the object
            return atlasData;
        }

        #endregion
    }

    #region MESH_CLASS_EXTENSION
    public static class EMCMeshClassExtension
    {
        /*
         * This is an extension class, which adds extra functions to the Mesh class. For example, counting vertices for each submesh.
         */

        public class Vertices
        {
            List<Vector3> verts = null;
            List<Vector2> uv1 = null;
            List<Vector2> uv2 = null;
            List<Vector2> uv3 = null;
            List<Vector2> uv4 = null;
            List<Vector3> normals = null;
            List<Vector4> tangents = null;
            List<Color32> colors = null;
            List<BoneWeight> boneWeights = null;

            public Vertices()
            {
                verts = new List<Vector3>();
            }

            public Vertices(Mesh aMesh)
            {
                verts = CreateList(aMesh.vertices);
                uv1 = CreateList(aMesh.uv);
                uv2 = CreateList(aMesh.uv2);
                uv3 = CreateList(aMesh.uv3);
                uv4 = CreateList(aMesh.uv4);
                normals = CreateList(aMesh.normals);
                tangents = CreateList(aMesh.tangents);
                colors = CreateList(aMesh.colors32);
                boneWeights = CreateList(aMesh.boneWeights);
            }

            private List<T> CreateList<T>(T[] aSource)
            {
                if (aSource == null || aSource.Length == 0)
                    return null;
                return new List<T>(aSource);
            }

            private void Copy<T>(ref List<T> aDest, List<T> aSource, int aIndex)
            {
                if (aSource == null)
                    return;
                if (aDest == null)
                    aDest = new List<T>();
                aDest.Add(aSource[aIndex]);
            }

            public int Add(Vertices aOther, int aIndex)
            {
                int i = verts.Count;
                Copy(ref verts, aOther.verts, aIndex);
                Copy(ref uv1, aOther.uv1, aIndex);
                Copy(ref uv2, aOther.uv2, aIndex);
                Copy(ref uv3, aOther.uv3, aIndex);
                Copy(ref uv4, aOther.uv4, aIndex);
                Copy(ref normals, aOther.normals, aIndex);
                Copy(ref tangents, aOther.tangents, aIndex);
                Copy(ref colors, aOther.colors, aIndex);
                Copy(ref boneWeights, aOther.boneWeights, aIndex);
                return i;
            }

            public void AssignTo(Mesh aTarget)
            {
                //Removes the limitation of 65k vertices, in case Unity supports.
                if (verts.Count > 65535)
                    aTarget.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                aTarget.SetVertices(verts);
                if (uv1 != null) aTarget.SetUVs(0, uv1);
                if (uv2 != null) aTarget.SetUVs(1, uv2);
                if (uv3 != null) aTarget.SetUVs(2, uv3);
                if (uv4 != null) aTarget.SetUVs(3, uv4);
                if (normals != null) aTarget.SetNormals(normals);
                if (tangents != null) aTarget.SetTangents(tangents);
                if (colors != null) aTarget.SetColors(colors);
                if (boneWeights != null) aTarget.boneWeights = boneWeights.ToArray();
            }
        }

        //Return count of vertices for submesh
        public static Mesh EmcGetSubmesh(this Mesh aMesh, int aSubMeshIndex)
        {
            if (aSubMeshIndex < 0 || aSubMeshIndex >= aMesh.subMeshCount)
                return null;
            int[] indices = aMesh.GetTriangles(aSubMeshIndex);
            Vertices source = new Vertices(aMesh);
            Vertices dest = new Vertices();
            Dictionary<int, int> map = new Dictionary<int, int>();
            int[] newIndices = new int[indices.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                int o = indices[i];
                int n;
                if (!map.TryGetValue(o, out n))
                {
                    n = dest.Add(source, o);
                    map.Add(o, n);
                }
                newIndices[i] = n;
            }
            Mesh m = new Mesh();
            dest.AssignTo(m);
            m.triangles = newIndices;
            return m;
        }
    }
    #endregion

    #region TEXTURE_RESIZER
    public class EMCTextureResizer
    {
        public class ThreadData
        {
            public int start;
            public int end;
            public ThreadData(int s, int e)
            {
                start = s;
                end = e;
            }
        }

        private static Color[] texColors;
        private static Color[] newColors;
        private static int w;
        private static float ratioX;
        private static float ratioY;
        private static int w2;
        private static int finishCount;
        private static Mutex mutex;

        public static void Point(Texture2D tex, int newWidth, int newHeight)
        {
            ThreadedScale(tex, newWidth, newHeight, false);
        }

        public static void Bilinear(Texture2D tex, int newWidth, int newHeight)
        {
            ThreadedScale(tex, newWidth, newHeight, true);
        }

        private static void ThreadedScale(Texture2D tex, int newWidth, int newHeight, bool useBilinear)
        {
            texColors = tex.GetPixels();
            newColors = new Color[newWidth * newHeight];
            if (useBilinear)
            {
                ratioX = 1.0f / ((float)newWidth / (tex.width - 1));
                ratioY = 1.0f / ((float)newHeight / (tex.height - 1));
            }
            else
            {
                ratioX = ((float)tex.width) / newWidth;
                ratioY = ((float)tex.height) / newHeight;
            }
            w = tex.width;
            w2 = newWidth;
            var cores = Mathf.Min(SystemInfo.processorCount, newHeight);
            var slice = newHeight / cores;

            finishCount = 0;
            if (mutex == null)
            {
                mutex = new Mutex(false);
            }
            if (cores > 1)
            {
                int i = 0;
                ThreadData threadData;
                for (i = 0; i < cores - 1; i++)
                {
                    threadData = new ThreadData(slice * i, slice * (i + 1));
                    ParameterizedThreadStart ts = useBilinear ? new ParameterizedThreadStart(BilinearScale) : new ParameterizedThreadStart(PointScale);
                    Thread thread = new Thread(ts);
                    thread.Start(threadData);
                }
                threadData = new ThreadData(slice * i, newHeight);
                if (useBilinear)
                {
                    BilinearScale(threadData);
                }
                else
                {
                    PointScale(threadData);
                }
                while (finishCount < cores)
                {
                    Thread.Sleep(1);
                }
            }
            else
            {
                ThreadData threadData = new ThreadData(0, newHeight);
                if (useBilinear)
                {
                    BilinearScale(threadData);
                }
                else
                {
                    PointScale(threadData);
                }
            }

            tex.Resize(newWidth, newHeight);
            tex.SetPixels(newColors);
            tex.Apply();

            texColors = null;
            newColors = null;
        }

        public static void BilinearScale(System.Object obj)
        {
            ThreadData threadData = (ThreadData)obj;
            for (var y = threadData.start; y < threadData.end; y++)
            {
                int yFloor = (int)Mathf.Floor(y * ratioY);
                var y1 = yFloor * w;
                var y2 = (yFloor + 1) * w;
                var yw = y * w2;

                for (var x = 0; x < w2; x++)
                {
                    int xFloor = (int)Mathf.Floor(x * ratioX);
                    var xLerp = x * ratioX - xFloor;
                    newColors[yw + x] = ColorLerpUnclamped(ColorLerpUnclamped(texColors[y1 + xFloor], texColors[y1 + xFloor + 1], xLerp), ColorLerpUnclamped(texColors[y2 + xFloor], texColors[y2 + xFloor + 1], xLerp), y * ratioY - yFloor);
                }
            }

            mutex.WaitOne();
            finishCount++;
            mutex.ReleaseMutex();
        }

        public static void PointScale(System.Object obj)
        {
            ThreadData threadData = (ThreadData)obj;
            for (var y = threadData.start; y < threadData.end; y++)
            {
                var thisY = (int)(ratioY * y) * w;
                var yw = y * w2;
                for (var x = 0; x < w2; x++)
                {
                    newColors[yw + x] = texColors[(int)(thisY + ratioX * x)];
                }
            }

            mutex.WaitOne();
            finishCount++;
            mutex.ReleaseMutex();
        }

        private static Color ColorLerpUnclamped(Color c1, Color c2, float value)
        {
            return new Color(c1.r + (c2.r - c1.r) * value, c1.g + (c2.g - c1.g) * value, c1.b + (c2.b - c1.b) * value, c1.a + (c2.a - c1.a) * value);
        }
    }
    #endregion
}