using System.IO;
using Celestia.PostProcessing;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Celestia.Editor
{
    [InitializeOnLoad]
    public static class CelestiaPostProcessSetup
    {
        private const string k_VolumeName = "Sky Volume";
        private const string k_ProfileFolder = "Assets/Celestia Settings";
        private const string k_ProfileName = "Celestia Sky Profile.asset";

        static CelestiaPostProcessSetup()
        {
            CelestiaSetupMenu.RigCreated -= OnRigCreated;
            CelestiaSetupMenu.RigCreated += OnRigCreated;
        }

        private static void OnRigCreated(GameObject root, CelestialHandler handler)
        {
            var volumeObject = new GameObject(k_VolumeName);
            volumeObject.transform.SetParent(root.transform, false);

            Volume volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = CreateProfileAsset();

            CelestialPostProcessBinder binder =
                volumeObject.AddComponent<CelestialPostProcessBinder>();

            var serialized = new SerializedObject(binder);
            serialized.FindProperty(CelestiaSerializedNames.Handler).objectReferenceValue = handler;
            serialized.FindProperty(CelestiaSerializedNames.Volume).objectReferenceValue = volume;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static VolumeProfile CreateProfileAsset()
        {
            string path = Path.Combine(k_ProfileFolder, k_ProfileName);

            var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (existing != null) return existing;

            if (!AssetDatabase.IsValidFolder(k_ProfileFolder))
            {
                AssetDatabase.CreateFolder("Assets", Path.GetFileName(k_ProfileFolder));
            }

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();

            return profile;
        }
    }
}
