#if UNITY_ANDROID
using System.IO;
using System.Xml;
using UnityEditor.Android;

public class QuestSystemKeyboardManifest : IPostGenerateGradleAndroidProject
{
    const string AndroidNamespace = "http://schemas.android.com/apk/res/android";
    const string FeatureName = "oculus.software.overlay_keyboard";

    public int callbackOrder => 100;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        string manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
        if (!File.Exists(manifestPath)) return;

        var doc = new XmlDocument();
        doc.Load(manifestPath);

        XmlElement manifest = doc.DocumentElement;
        foreach (XmlNode node in manifest.GetElementsByTagName("uses-feature"))
        {
            var name = node.Attributes?["name", AndroidNamespace];
            if (name != null && name.Value == FeatureName) return;
        }

        XmlElement feature = doc.CreateElement("uses-feature");
        feature.SetAttribute("name", AndroidNamespace, FeatureName);
        feature.SetAttribute("required", AndroidNamespace, "false");
        manifest.AppendChild(feature);

        doc.Save(manifestPath);
    }
}
#endif
