#if UNITY_ANDROID
using System.IO;
using System.Xml;
using UnityEditor.Android;

public class QuestSystemKeyboardManifest : IPostGenerateGradleAndroidProject
{
    const string AndroidNamespace = "http://schemas.android.com/apk/res/android";
    const string FeatureName = "oculus.software.overlay_keyboard";

    static readonly string[] permissions =
    {
        "android.permission.RECORD_AUDIO",
        "android.permission.MODIFY_AUDIO_SETTINGS"
    };

    public int callbackOrder => 100;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        string manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
        if (!File.Exists(manifestPath)) return;

        var doc = new XmlDocument();
        doc.Load(manifestPath);

        XmlElement manifest = doc.DocumentElement;
        AddFeature(doc, manifest);
        foreach (var permission in permissions)
            AddPermission(doc, manifest, permission);

        doc.Save(manifestPath);
    }

    static void AddFeature(XmlDocument doc, XmlElement manifest)
    {
        if (HasEntry(manifest, "uses-feature", FeatureName)) return;

        XmlElement feature = doc.CreateElement("uses-feature");
        feature.SetAttribute("name", AndroidNamespace, FeatureName);
        feature.SetAttribute("required", AndroidNamespace, "false");
        manifest.AppendChild(feature);
    }

    static void AddPermission(XmlDocument doc, XmlElement manifest, string permission)
    {
        if (HasEntry(manifest, "uses-permission", permission)) return;

        XmlElement element = doc.CreateElement("uses-permission");
        element.SetAttribute("name", AndroidNamespace, permission);
        manifest.AppendChild(element);
    }

    static bool HasEntry(XmlElement manifest, string tag, string name)
    {
        foreach (XmlNode node in manifest.GetElementsByTagName(tag))
        {
            var attribute = node.Attributes?["name", AndroidNamespace];
            if (attribute != null && attribute.Value == name) return true;
        }
        return false;
    }
}
#endif
