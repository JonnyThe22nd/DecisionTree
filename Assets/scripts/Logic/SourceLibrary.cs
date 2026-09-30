using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SourceLibrary
{
    public const string FolderName = "Sources";

    static readonly string[] textExtensions = { ".txt", ".md" };
    static readonly string[] imageExtensions = { ".png", ".jpg", ".jpeg" };

    public static string FolderPath => Path.Combine(Application.persistentDataPath, FolderName);

    public static List<string> ListFiles()
    {
        var result = new List<string>();
        try
        {
            Directory.CreateDirectory(FolderPath);
            foreach (var path in Directory.GetFiles(FolderPath))
            {
                if (IsText(path) || IsImage(path)) result.Add(path);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"SourceLibrary: Quellen-Ordner '{FolderPath}' nicht lesbar: {e.Message}");
        }
        result.Sort(StringComparer.OrdinalIgnoreCase);
        return result;
    }

    public static bool IsText(string path) => HasExtension(path, textExtensions);
    public static bool IsImage(string path) => HasExtension(path, imageExtensions);

    static bool HasExtension(string path, string[] extensions)
    {
        string ext = Path.GetExtension(path);
        foreach (var e in extensions)
        {
            if (string.Equals(ext, e, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    public static string LoadText(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"SourceLibrary: '{path}' nicht lesbar: {e.Message}");
            return null;
        }
    }

    public static Texture2D LoadImage(string path)
    {
        try
        {
            var texture = new Texture2D(2, 2);
            if (texture.LoadImage(File.ReadAllBytes(path))) return texture;
            UnityEngine.Object.Destroy(texture);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"SourceLibrary: '{path}' nicht lesbar: {e.Message}");
        }
        return null;
    }
}
