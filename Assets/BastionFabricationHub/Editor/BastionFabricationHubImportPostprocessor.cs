using UnityEditor;
using UnityEngine;

namespace BastionFabrication.Editor
{
    internal sealed class BastionFabricationHubImportPostprocessor : AssetPostprocessor
    {
        private const string MeshPath = "Assets/BastionFabricationHub/Meshes/";
        private const string TexturePath = "Assets/BastionFabricationHub/Textures/";

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(MeshPath, System.StringComparison.Ordinal)) return;
            ModelImporter importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
            importer.importAnimation = false;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Medium;
            importer.importNormals = ModelImporterNormals.Import;
        }

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(TexturePath, System.StringComparison.Ordinal)) return;
            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
