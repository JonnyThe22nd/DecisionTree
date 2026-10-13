using System.Collections.Generic;
using UnityEngine;

public static class TreeRegistry
{
    static readonly Dictionary<string, TreeMenuBuilder> builders = new Dictionary<string, TreeMenuBuilder>();

    public static void Register(string treeId, TreeMenuBuilder builder)
    {
        if (builders.TryGetValue(treeId, out var existing) && existing != null && existing != builder)
            Debug.LogWarning($"TreeRegistry: Baum-ID '{treeId}' ist doppelt vergeben. Objekte brauchen eindeutige Namen.");

        builders[treeId] = builder;
    }

    public static void Unregister(string treeId, TreeMenuBuilder builder)
    {
        if (builders.TryGetValue(treeId, out var existing) && existing == builder)
            builders.Remove(treeId);
    }

    public static void Apply(TreeOperation op)
    {
        if (builders.TryGetValue(op.TreeId, out var builder) && builder != null)
            builder.ApplyOperation(op);
        else
            Debug.LogWarning($"TreeRegistry: kein Baum mit der ID '{op.TreeId}' in dieser Szene.");
    }
}
