using UnityEngine;

public static class UserIdentity
{
    const string Key = "DecisionTree.UserName";

    public static bool HasName => !string.IsNullOrEmpty(Name);

    public static string Name
    {
        get => PlayerPrefs.GetString(Key, "");
        set
        {
            PlayerPrefs.SetString(Key, value);
            PlayerPrefs.Save();
        }
    }
}
