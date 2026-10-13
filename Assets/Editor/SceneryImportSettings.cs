using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Safari.EditorTools
{
    /// <summary>
    /// Enforces the scenery import settings required by BUILD-SPEC section 7:
    /// Scale Factor 1, Bake Axis Conversion on, Mesh Compression off, and no
    /// animation or rig import. Also swaps the imported embedded Phong
    /// materials for shared URP Lit materials, which is what stops Kenney
    /// models rendering pink under URP.
    ///
    /// Shared materials are looked up by name from
    /// Assets/Models/Scenery/Materials. Generate them once with
    /// GenerateSceneryMaterials.
    /// </summary>
    public sealed class SceneryImportSettings : AssetPostprocessor
    {
        private const string SceneryRoot = "Assets/Models/Scenery/";
        private const string SharedMaterialFolder = "Assets/Models/Scenery/Materials/";

        private static Dictionary<string, Material> sharedMaterials;

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(SceneryRoot))
                return;

            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
            importer.bakeAxisConversion = true;
            importer.meshCompression = ModelImporterMeshCompression.Off;

            // Scenery has no rigs, so do not pay to import animation data.
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;

            // Nothing reads these meshes from script, so keep them out of RAM.
            importer.isReadable = false;

            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        }

        private void OnPostprocessModel(GameObject root)
        {
            if (!assetPath.StartsWith(SceneryRoot))
                return;

            var lookup = GetSharedMaterials();
            if (lookup == null || lookup.Count == 0)
                return;

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var slots = renderer.sharedMaterials;
                bool changed = false;

                for (int i = 0; i < slots.Length; i++)
                {
                    if (slots[i] == null)
                        continue;

                    if (lookup.TryGetValue(Normalize(slots[i].name), out var shared))
                    {
                        slots[i] = shared;
                        changed = true;
                    }
                }

                if (changed)
                    renderer.sharedMaterials = slots;
            }
        }

        private static Dictionary<string, Material> GetSharedMaterials()
        {
            if (sharedMaterials != null)
                return sharedMaterials;

            sharedMaterials = new Dictionary<string, Material>();

            if (!AssetDatabase.IsValidFolder(SharedMaterialFolder))
                return sharedMaterials;

            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { SharedMaterialFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material != null)
                    sharedMaterials[Normalize(material.name)] = material;
            }

            return sharedMaterials;
        }

        /// <summary>Forces a reimport to see materials generated after the cache was built.</summary>
        internal static void ClearCache()
        {
            sharedMaterials = null;
        }

        /// <summary>FBX embedded material names may arrive as "Material::leafsGreen" or "leafsGreen".</summary>
        internal static string Normalize(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            int marker = name.IndexOf("Material::", System.StringComparison.Ordinal);
            if (marker >= 0)
                name = name.Substring(marker + "Material::".Length);

            return name.Trim().ToLowerInvariant();
        }
    }
}