using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[Serializable]
public class SourceEntry
{
    public string Title;
    public string FilePath;
    public string Author;
    public DateTime AddedAt;

    public bool IsImage => SourceLibrary.IsImage(FilePath);
}

public class MenuNode
{
    public string Id = Guid.NewGuid().ToString("N").Substring(0, 12);
    public string Label;
    public MenuNode Parent;
    public List<MenuNode> Children = new List<MenuNode>();
    public UnityAction OnSelect;

    public List<SourceEntry> Sources = new List<SourceEntry>();

    public ArgumentationKind Kind;
    public BeliefValue Belief = BeliefValue.Unknown;
    public string Reasoning;
    public string Author;
    public DateTime CreatedAt;
    public string ModelFile;

    public RectTransform VisualInstance;
    public RectTransform ConnectionToParent;
    public RectTransform ModelButtonInstance;

    public float LayoutX;

    public bool IsExpanded = true;

    public MenuNode(string label, UnityAction onSelect = null)
    {
        Label = label;
        OnSelect = onSelect;
    }

    public MenuNode AddChild(MenuNode child)
    {
        child.Parent = this;
        Children.Add(child);
        return child;
    }

    public MenuNode AddChild(string label, UnityAction onSelect = null)
    {
        var node = new MenuNode(label, onSelect);
        return AddChild(node);
    }

    public bool IsLeaf => Children.Count == 0;

    public bool IsRoot => Parent == null;

    public int Depth
    {
        get
        {
            int d = 0;
            var p = Parent;
            while (p != null) { d++; p = p.Parent; }
            return d;
        }
    }

    public List<MenuNode> GetPath()
    {
        var path = new List<MenuNode>();
        for (var n = this; n != null; n = n.Parent)
            path.Insert(0, n);
        return path;
    }

    public BeliefValue WeakestBelief()
    {
        var weakest = BeliefValue.Certain;
        for (var n = this; n != null && !n.IsRoot; n = n.Parent)
        {
            if (n.Belief < weakest) weakest = n.Belief;
        }
        return weakest;
    }

    public bool IsVisible()
    {
        var p = Parent;
        while (p != null)
        {
            if (!p.IsExpanded) return false;
            p = p.Parent;
        }
        return true;
    }
}
