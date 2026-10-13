using System.Collections.Generic;

public class PathModelEventArgs
{
    public MenuNode Leaf;
    public IReadOnlyList<MenuNode> Path;
    public string ModelFile;
    public bool Visible;
}
