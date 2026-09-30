using UnityEditor;
using UnityEngine;

public static class SimulatorToggle
{
    const string MenuPath = "DecisionTree/Ohne Headset testen (Simulator)";
    const string SettingsPath = "Assets/XRI/Settings/Resources/XRDeviceSimulatorSettings.asset";
    const string SimulatorPrefabPath = "Assets/Samples/XR Interaction Toolkit/3.3.2/XR Interaction Simulator/XR Interaction Simulator.prefab";

    [MenuItem(MenuPath)]
    static void Toggle()
    {
        var settings = LoadSettings();
        if (settings == null) return;

        var enabled = settings.FindProperty("m_AutomaticallyInstantiateSimulatorPrefab");
        enabled.boolValue = !enabled.boolValue;

        if (enabled.boolValue)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SimulatorPrefabPath);
            if (prefab == null)
            {
                Debug.LogError("SimulatorToggle: XRI-Sample 'XR Interaction Simulator' ist nicht importiert (Package Manager > XR Interaction Toolkit > Samples).");
                return;
            }
            settings.FindProperty("m_SimulatorPrefab").objectReferenceValue = prefab;
            settings.FindProperty("m_AutomaticallyInstantiateInEditorOnly").boolValue = true;
            settings.FindProperty("m_UseClassic").boolValue = false;
        }

        settings.ApplyModifiedProperties();
        AssetDatabase.SaveAssetIfDirty(settings.targetObject);
        Debug.Log("SimulatorToggle: Test ohne Headset ist " + (enabled.boolValue ? "AN" : "AUS") + ".");
    }

    [MenuItem(MenuPath, true)]
    static bool ToggleValidate()
    {
        var settings = LoadSettings();
        Menu.SetChecked(MenuPath, settings != null && settings.FindProperty("m_AutomaticallyInstantiateSimulatorPrefab").boolValue);
        return settings != null;
    }

    static SerializedObject LoadSettings()
    {
        var asset = AssetDatabase.LoadMainAssetAtPath(SettingsPath);
        return asset != null ? new SerializedObject(asset) : null;
    }
}
