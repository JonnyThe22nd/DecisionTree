using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class ModelLibrary
{
    public const string FolderName = "Models";

    static readonly string[] extensions = { ".glb", ".gltf" };

    public static string FolderPath => Path.Combine(Application.persistentDataPath, FolderName);

    public static string Resolve(string fileName) => Path.Combine(FolderPath, Path.GetFileName(fileName));

    public static List<string> ListFiles()
    {
        var result = new List<string>();
        try
        {
            Directory.CreateDirectory(FolderPath);
            foreach (var path in Directory.GetFiles(FolderPath))
            {
                string ext = Path.GetExtension(path);
                foreach (var e in extensions)
                {
                    if (string.Equals(ext, e, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(path);
                        break;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"ModelLibrary: Modell-Ordner '{FolderPath}' nicht lesbar: {e.Message}");
        }
        result.Sort(StringComparer.OrdinalIgnoreCase);
        return result;
    }
}
