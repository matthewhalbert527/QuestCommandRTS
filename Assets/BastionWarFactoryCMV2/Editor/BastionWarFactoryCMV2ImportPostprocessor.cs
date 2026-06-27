
using UnityEditor;

namespace BastionWarFactoryCMV2.Editor
{
    public class BastionWarFactoryCMV2ImportPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if (!assetPath.Contains("BastionWarFactoryCMV2/Meshes/")) return;
            ModelImporter importer = (ModelImporter)assetImporter;
            importer.globalScale = 1.0f;
            importer.useFileScale = false;
            importer.isReadable = true;
            importer.importNormals = ModelImporterNormals.Calculate;
            importer.normalSmoothingAngle = 60f;
            importer.importCameras = false;
            importer.importLights = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
        }
    }
}
