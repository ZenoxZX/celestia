using System;
using UnityEditor;
using UnityEngine;

namespace Celestia.Editor
{
    public static class CelestiaSetupMenu
    {
        private const string k_RootName = "Celestia";
        private const string k_SunName = "Sun Light";
        private const string k_MoonName = "Moon Light";

        public static event Action<GameObject, CelestialHandlerBehaviour> RigCreated;

        [MenuItem("GameObject/Celestia/Sky Rig", false, 10)]
        public static void CreateSkyRig(MenuCommand command)
        {
            var root = new GameObject(k_RootName);
            GameObjectUtility.SetParentAndAlign(root, command.context as GameObject);

            WorldClockBehaviour clock = root.AddComponent<WorldClockBehaviour>();
            CelestialHandlerBehaviour handler = root.AddComponent<CelestialHandlerBehaviour>();
            CelestialLightBinderBehaviour binder = root.AddComponent<CelestialLightBinderBehaviour>();

            Light sun = CreateLight(root.transform, k_SunName, 3f);
            Light moon = CreateLight(root.transform, k_MoonName, 0.4f);

            var serializedHandler = new SerializedObject(handler);
            serializedHandler.FindProperty(CelestiaSerializedNames.Clock).objectReferenceValue = clock;
            serializedHandler.ApplyModifiedPropertiesWithoutUndo();

            var serializedBinder = new SerializedObject(binder);
            serializedBinder.FindProperty(CelestiaSerializedNames.Handler).objectReferenceValue = handler;
            serializedBinder.FindProperty(CelestiaSerializedNames.SunLight).objectReferenceValue = sun;
            serializedBinder.FindProperty(CelestiaSerializedNames.MoonLight).objectReferenceValue = moon;
            serializedBinder.ApplyModifiedPropertiesWithoutUndo();

            CelestialSchedulerBehaviour scheduler = root.AddComponent<CelestialSchedulerBehaviour>();
            var serializedScheduler = new SerializedObject(scheduler);
            serializedScheduler.FindProperty(CelestiaSerializedNames.Clock).objectReferenceValue = clock;
            serializedScheduler.FindProperty(CelestiaSerializedNames.Handler).objectReferenceValue = handler;
            serializedScheduler.ApplyModifiedPropertiesWithoutUndo();

            RigCreated?.Invoke(root, handler);

            Undo.RegisterCreatedObjectUndo(root, "Create Celestia Sky Rig");
            Selection.activeGameObject = root;

            Debug.Log("Celestia sky rig created. Assign a CelestialPreset to the CelestialHandler.", root);
        }

        private static Light CreateLight(Transform parent, string lightName, float intensity)
        {
            var go = new GameObject(lightName);
            go.transform.SetParent(parent, false);

            Light light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.shadows = LightShadows.Soft;

            return light;
        }
    }
}
