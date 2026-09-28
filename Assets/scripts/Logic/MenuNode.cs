using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Eine einzelne Quelle/Annotation an einem Node. Titel = Kurzbezeichnung
/// für die Liste, Content = der eigentliche Text/Link/Zitat/Verweis.
/// </summary>
[System.Serializable]
public class SourceEntry
{
    public string Title;
    public string Content;
}

/// <summary>
/// Reiner Datencontainer für einen Menü-Knoten. Kennt nichts von Visualisierung,
/// außer der Referenz auf sein instanziertes UI-Element (wird vom Builder gesetzt).
/// </summary>
public class MenuNode
{
    public string Label;
    public MenuNode Parent;
    public List<MenuNode> Children = new List<MenuNode>();
    public UnityAction OnSelect;

    // Quellen/Annotationen, die zu diesem Node gehören -- unabhängig von Kindern.
    public List<SourceEntry> Sources = new List<SourceEntry>();

    // Wird zur Laufzeit vom TreeMenuBuilder befüllt (das Button-UI-Element)
    public RectTransform VisualInstance;

    // Verbindungslinie zum Parent, dargestellt als gestrecktes UI-Image
    public RectTransform ConnectionToParent;

    // Wird vom Layout-Algorithmus berechnet (in Canvas-lokalen Einheiten, z.B. Pixel)
    public float LayoutX;

    // Für Expand/Collapse (progressive disclosure in VR wichtig!)
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

    // Praktisch für den prozeduralen Aufbau: gibt den neu erzeugten Node zurück
    public MenuNode AddChild(string label, UnityAction onSelect = null)
    {
        var node = new MenuNode(label, onSelect);
        return AddChild(node);
    }

    public bool IsLeaf => Children.Count == 0;

    // 0 = Root, 1 = Kind, 2 = Enkelkind usw. Wird für die Begrenzung
    // "nur bis zur zweiten Ebene können Kinder hinzugefügt werden" genutzt.
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
