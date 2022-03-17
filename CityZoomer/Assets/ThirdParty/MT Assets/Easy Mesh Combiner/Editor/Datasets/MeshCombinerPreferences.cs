using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MTAssets.EasyMeshCombiner.Editor
{
    /*
     * This script is the Dataset of the scriptable object "Preferences". This script saves Easy Mesh Combiner preferences.
     */

    public class MeshCombinerPreferences : ScriptableObject
    {
        public enum AfterMerge
        {
            DisableOriginalMeshes,
            DeactiveOriginalGameObjects,
            DoNothing
        }
        public enum MergeMethod
        {
            OneMeshPerMaterial,
            AllInOne,
            JustMaterialColors
        }
        public enum AtlasSize
        {
            Pixels32x32,
            Pixels64x64,
            Pixels128x128,
            Pixels256x256,
            Pixels512x512,
            Pixels1024x1024,
            Pixels2048x2048,
            Pixels4096x4096,
            Pixels8192x8192
        }
        public enum MipMapEdgesSize
        {
            Pixels0x0,
            Pixels16x16,
            Pixels32x32,
            Pixels64x64,
            Pixels128x128,
            Pixels256x256,
            Pixels512x512,
            Pixels1024x1024,
        }
        public enum AtlasPadding
        {
            Pixels0x0,
            Pixels2x2,
            Pixels4x4,
            Pixels8x8,
            Pixels16x16,
        }
        public enum MergeTiledTextures
        {
            SkipAll,
            LegacyMode
        }

        [System.Serializable]
        public class OneMeshPerMaterialParams
        {
            public bool addMeshCollider = false;
        }
        [System.Serializable]
        public class AllInOneParams
        {
            public Material materialToUse;
            public int maxTexturesPerAtlas = 12;
            public AtlasSize atlasResolution = AtlasSize.Pixels1024x1024;
            public MipMapEdgesSize mipMapEdgesSize = MipMapEdgesSize.Pixels64x64;
            public AtlasPadding atlasPadding = AtlasPadding.Pixels0x0;
            public MergeTiledTextures mergeTiledTextures = MergeTiledTextures.LegacyMode;
            public bool useDefaultMainTextureProperty = true;
            public string mainTexturePropertyToFind = "_MainTex";
            public string mainTexturePropertyToInsert = "_MainTex";
            public bool metallicMapSupport = false;
            public string metallicMapPropertyToFind = "_MetallicGlossMap";
            public string metallicMapPropertyToInsert = "_MetallicGlossMap";
            public bool specularMapSupport = false;
            public string specularMapPropertyToFind = "_SpecGlossMap";
            public string specularMapPropertyToInsert = "_SpecGlossMap";
            public bool normalMapSupport = false;
            public string normalMapPropertyToFind = "_BumpMap";
            public string normalMapPropertyToInsert = "_BumpMap";
            public bool normalMap2Support = false;
            public string normalMap2PropertyFind = "_DetailNormalMap";
            public string normalMap2PropertyToInsert = "_DetailNormalMap";
            public bool heightMapSupport = false;
            public string heightMapPropertyToFind = "_ParallaxMap";
            public string heightMapPropertyToInsert = "_ParallaxMap";
            public bool occlusionMapSupport = false;
            public string occlusionMapPropertyToFind = "_OcclusionMap";
            public string occlusionMapPropertyToInsert = "_OcclusionMap";
            public bool detailAlbedoMapSupport = false;
            public string detailMapPropertyToFind = "_DetailAlbedoMap";
            public string detailMapPropertyToInsert = "_DetailAlbedoMap";
            public bool detailMaskSupport = false;
            public string detailMaskPropertyToFind = "_DetailMask";
            public string detailMaskPropertyToInsert = "_DetailMask";
            public bool pinkNormalMapsFix = true;
            public bool addMeshCollider = false;
            public bool highlightUvVertices = false;
        }
        [System.Serializable]
        public class JustMaterialColorsParams
        {
            public Material materialToUse;
            public bool useDefaultColorProperty = true;
            public string colorPropertyToFind = "_Color";
            public string mainTexturePropertyToInsert = "_MainTex";
            public bool addMeshCollider = false;
        }

        public string projectName;
        public Rect windowPosition;
        public bool representLogsInScene = true;

        public AfterMerge afterMerge = AfterMerge.DisableOriginalMeshes;
        public MergeMethod mergeMethod = MergeMethod.OneMeshPerMaterial;
        public OneMeshPerMaterialParams oneMeshPerMaterialParams = new OneMeshPerMaterialParams();
        public AllInOneParams allInOneParams = new AllInOneParams();
        public JustMaterialColorsParams justMaterialColorsParams = new JustMaterialColorsParams();
        public bool combineChildrens = true;
        public bool combineInactives = false;
        public bool lightmapSupport = false;
        public bool saveMeshInAssets = true;
        public bool savePrefabOfThis = false;
        public string prefabName = "prefab";
        public string nameOfThisMerge = "Combined Meshes";
    }
}