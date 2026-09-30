using UnityEngine;

[System.Serializable]
public class HierarchicalTreeLayout
{
    [Tooltip("Horizontaler Abstand zwischen benachbarten Blatt-Nodes, in Canvas-Einheiten (z.B. Pixel)")]
    public float horizontalSpacing = 220f;

    [Tooltip("Vertikaler Abstand zwischen Ebenen, in Canvas-Einheiten. Negativ = Kinder wachsen nach unten.")]
    public float verticalSpacing = -160f;

    float nextX;

    public void Layout(MenuNode root, Vector2 origin)
    {
        nextX = 0f;
        AssignX(root);

        float rootOffset = root.LayoutX;
        AssignPosition(root, origin, 0, rootOffset);
    }

    float AssignX(MenuNode node)
    {
        if (!node.IsExpanded || node.Children.Count == 0)
        {
            node.LayoutX = nextX;
            nextX += horizontalSpacing;
            return node.LayoutX;
        }

        float first = -1f, last = -1f;
        foreach (var child in node.Children)
        {
            float x = AssignX(child);
            if (first < 0) first = x;
            last = x;
        }

        node.LayoutX = (first + last) / 2f;
        return node.LayoutX;
    }

    void AssignPosition(MenuNode node, Vector2 origin, int depth, float centerOffset)
    {
        Vector2 pos = origin + new Vector2(node.LayoutX - centerOffset, depth * verticalSpacing);

        if (node.VisualInstance != null)
            node.VisualInstance.anchoredPosition = pos;

        if (!node.IsExpanded) return;

        foreach (var child in node.Children)
            AssignPosition(child, origin, depth + 1, centerOffset);
    }
}
