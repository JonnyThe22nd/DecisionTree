using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public enum TreeDetailLevel
{
    Pin,
    Name,
    Full
}

public class TreeMenuBuilder : MonoBehaviour
{
    [Header("Canvas")]
    public RectTransform canvasRoot;

    [Tooltip("Container, der den ganzen Baum hält und skaliert wird. Wird automatisch " +
             "als Kind von canvasRoot erzeugt, falls hier nichts zugewiesen ist.")]
    public RectTransform content;

    [Tooltip("Container für Popups und das Quellen-Panel. Liegt über dem Baum und wird " +
             "NICHT mitskaliert, damit Popups auch bei großen Bäumen lesbar bleiben. Wird " +
             "automatisch als Kind von canvasRoot erzeugt, falls hier nichts zugewiesen ist.")]
    public RectTransform overlay;

    [Header("Prefabs")]
    [Tooltip("WICHTIG: anchorMin/anchorMax NICHT stretchen (z.B. (0.5,0.5))")]
    public Button nodePrefab;
    [Tooltip("anchorMin = anchorMax = (0.5, 0.5), Pivot = (0, 0.5)")]
    public Image linePrefab;
    [Tooltip("Kleiner '+'-Button, wird an JEDEN Node angehängt (keine Tiefenbegrenzung)")]
    public Button plusButtonPrefab;
    [Tooltip("Kleiner 'Quelle hinzufügen'-Button, wird an jede Entscheidung angehängt (nicht an die Top-Node)")]
    public Button addSourceButtonPrefab;
    [Tooltip("Panel mit TMP_InputField + Bestätigen-Button zum Benennen neuer Nodes")]
    public NamingPopup namingPopupPrefab;
    [Tooltip("Popup mit der Dateiliste des Quellen-Ordners")]
    public SourcePickerPopup sourcePickerPrefab;
    [Tooltip("Panel mit der Quellenliste einer Entscheidung. Wird EINMALIG instanziert " +
             "(als Kind von overlay) und für jeden Node wiederverwendet.")]
    public SourcesPanel sourcesPanelPrefab;

    [Header("Erstellung")]
    [Tooltip("Button (z.B. '+ Baum erstellen'), der sichtbar wird, solange noch kein " +
             "Baum existiert. Wird von ObjectTreeAttachment ein-/ausgeblendet.")]
    public GameObject createPromptButton;

    [Header("Fern-Ansicht")]
    [Tooltip("Markierung, die aus großer Entfernung statt des Baums gezeigt wird")]
    public RectTransform pinView;
    [Tooltip("Schild mit dem Namen des Baums für mittlere Entfernung")]
    public RectTransform nameView;
    public TMPro.TextMeshProUGUI nameLabel;

    [Header("Layout")]
    public HierarchicalTreeLayout layout = new HierarchicalTreeLayout();

    [Header("Auto-Fit")]
    [Range(0.1f, 1f)]
    public float fillPercent = 0.85f;
    public float maxScale = 1.5f;

    [Header("Verhalten")]
    public float lineThickness = 4f;
    [Tooltip("Deckkraft des Baums, solange ein Fenster offen ist und der Baum gesperrt ist")]
    [Range(0f, 1f)]
    public float blockedAlpha = 0.35f;

    MenuNode root;
    readonly List<MenuNode> allNodes = new List<MenuNode>();
    bool popupOpen = false;
    SourcesPanel sourcesPanelInstance;

    CanvasGroup contentGroup;

    public bool HasTree => root != null;

    public bool IsCreatePromptVisible => createPromptButton != null && createPromptButton.activeSelf;

    void Awake()
    {
        EnsureContainers();

        if (createPromptButton != null)
        {
            var btn = createPromptButton.GetComponentInChildren<Button>(true);
            if (btn != null) btn.onClick.AddListener(PromptCreateRoot);
            else Debug.LogWarning("TreeMenuBuilder: kein Button auf/unter createPromptButton gefunden.");
            createPromptButton.SetActive(false);
        }

        if (sourcesPanelPrefab != null)
        {
            sourcesPanelInstance = Instantiate(sourcesPanelPrefab, overlay);
            sourcesPanelInstance.gameObject.SetActive(false);
        }

        SetDetailLevel(TreeDetailLevel.Full);
    }

    void EnsureContainers()
    {
        if (content == null)
        {
            var go = new GameObject("TreeContent", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(canvasRoot, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            content = rt;
        }

        if (overlay == null)
        {
            var go = new GameObject("Overlay", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(canvasRoot, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            overlay = rt;
        }

        overlay.SetAsLastSibling();

        if (contentGroup == null)
        {
            contentGroup = content.GetComponent<CanvasGroup>();
            if (contentGroup == null) contentGroup = content.gameObject.AddComponent<CanvasGroup>();
        }
    }

    public bool IsWindowOpen =>
        popupOpen || (sourcesPanelInstance != null && sourcesPanelInstance.gameObject.activeSelf);

    void Update()
    {
        bool blocked = IsWindowOpen;
        contentGroup.interactable = !blocked;
        contentGroup.blocksRaycasts = !blocked;
        contentGroup.alpha = blocked ? blockedAlpha : 1f;
    }

    public void SetCreatePromptVisible(bool visible)
    {
        if (createPromptButton != null && !HasTree && !popupOpen)
            createPromptButton.SetActive(visible);
    }

    public void SetDetailLevel(TreeDetailLevel level, float farScale = 1f)
    {
        if (!HasTree) level = TreeDetailLevel.Full;

        SetActiveIfChanged(content, level == TreeDetailLevel.Full);
        SetActiveIfChanged(overlay, level == TreeDetailLevel.Full);
        bool showName = HasTree && level == TreeDetailLevel.Name;
        if (nameView != null && showName && !nameView.gameObject.activeSelf)
        {
            nameView.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(nameView);
        }
        SetActiveIfChanged(nameView, showName);
        SetActiveIfChanged(pinView, HasTree && level == TreeDetailLevel.Pin);

        if (nameView != null) nameView.localScale = Vector3.one * farScale;
        if (pinView != null) pinView.localScale = Vector3.one * farScale;
    }

    static void SetActiveIfChanged(RectTransform target, bool active)
    {
        if (target != null && target.gameObject.activeSelf != active)
            target.gameObject.SetActive(active);
    }

    public void PromptCreateRoot()
    {
        if (HasTree || namingPopupPrefab == null || popupOpen) return;

        popupOpen = true;
        if (createPromptButton != null) createPromptButton.SetActive(false);

        var popup = Instantiate(namingPopupPrefab, overlay);
        popup.Init(
            label => CreateRoot(label),
            "Name des Baums...",
            () => popupOpen = false
        );
    }

    public void CreateRoot(string label)
    {
        if (createPromptButton != null) createPromptButton.SetActive(false);

        var newRoot = new MenuNode(label);
        Build(newRoot);
    }

    public void RequestAddChild(MenuNode parent)
    {
        if (namingPopupPrefab == null || popupOpen) return;

        popupOpen = true;
        var popup = Instantiate(namingPopupPrefab, overlay);
        popup.Init(
            label => AddChild(parent, label),
            "Name der Entscheidung...",
            () => popupOpen = false
        );
    }

    public MenuNode AddChild(MenuNode parent, string label)
    {
        var child = parent.AddChild(label);
        CollectAndInstantiate(child);
        Relayout();
        return child;
    }

    public void RequestAddSource(MenuNode node)
    {
        if (sourcePickerPrefab == null || popupOpen) return;

        popupOpen = true;
        var popup = Instantiate(sourcePickerPrefab, overlay);
        popup.Init(
            path => AddSource(node, path),
            () => popupOpen = false
        );
    }

    public void AddSource(MenuNode node, string filePath)
    {
        if (node.Sources.Exists(s => s.FilePath == filePath)) return;

        node.Sources.Add(new SourceEntry { Title = Path.GetFileName(filePath), FilePath = filePath });

        if (sourcesPanelInstance != null && sourcesPanelInstance.CurrentNode == node)
            sourcesPanelInstance.RefreshList();
    }

    public void RequestOpenSources(MenuNode node)
    {
        if (sourcesPanelInstance == null || popupOpen) return;
        sourcesPanelInstance.Open(node);
    }

    public void Build(MenuNode treeRoot)
    {
        EnsureContainers();
        Clear();
        root = treeRoot;
        if (nameLabel != null) nameLabel.text = root.Label;
        CollectAndInstantiate(root);
        Relayout();
    }

    public void Clear()
    {
        if (content == null) return;
        foreach (Transform child in content)
            Destroy(child.gameObject);
        allNodes.Clear();
        root = null;
    }

    void CollectAndInstantiate(MenuNode node)
    {
        allNodes.Add(node);

        Button btn = Instantiate(nodePrefab, content);
        node.VisualInstance = btn.GetComponent<RectTransform>();

        var label = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (label != null) label.text = node.Label;

        btn.onClick.AddListener(() => OnNodeSelected(node));

        if (plusButtonPrefab != null)
        {
            Button plus = Instantiate(plusButtonPrefab, node.VisualInstance);
            var plusRT = plus.GetComponent<RectTransform>();
            plusRT.anchorMin = plusRT.anchorMax = new Vector2(1f, 1f);
            plusRT.pivot = new Vector2(0f, 1f);
            plusRT.anchoredPosition = new Vector2(10f, 0f);
            plus.onClick.AddListener(() => RequestAddChild(node));
        }

        if (addSourceButtonPrefab != null && !node.IsRoot)
        {
            Button addSource = Instantiate(addSourceButtonPrefab, node.VisualInstance);
            var addSourceRT = addSource.GetComponent<RectTransform>();
            addSourceRT.anchorMin = addSourceRT.anchorMax = new Vector2(1f, 0f);
            addSourceRT.pivot = new Vector2(0f, 0f);
            addSourceRT.anchoredPosition = new Vector2(10f, 0f);
            addSource.onClick.AddListener(() => RequestAddSource(node));
        }

        if (node.Parent != null)
        {
            Image line = Instantiate(linePrefab, content);
            line.rectTransform.SetAsFirstSibling();
            node.ConnectionToParent = line.rectTransform;
        }

        foreach (var child in node.Children)
            CollectAndInstantiate(child);
    }

    void OnNodeSelected(MenuNode node)
    {
        node.OnSelect?.Invoke();

        if (!node.IsRoot) RequestOpenSources(node);
    }

    void Relayout()
    {
        layout.Layout(root, Vector2.zero);

        foreach (var node in allNodes)
        {
            bool visible = node.IsVisible();
            node.VisualInstance.gameObject.SetActive(visible);
            if (node.ConnectionToParent != null)
                node.ConnectionToParent.gameObject.SetActive(visible);
        }

        UpdateConnectionLines();
        FitContentToCanvas();
    }

    void UpdateConnectionLines()
    {
        foreach (var node in allNodes)
        {
            if (node.ConnectionToParent == null || node.Parent == null) continue;
            if (!node.VisualInstance.gameObject.activeSelf) continue;

            Vector2 from = node.Parent.VisualInstance.anchoredPosition;
            Vector2 to = node.VisualInstance.anchoredPosition;
            Vector2 delta = to - from;

            var rt = node.ConnectionToParent;
            rt.anchoredPosition = from;
            rt.sizeDelta = new Vector2(delta.magnitude, lineThickness);
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            rt.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }

    void FitContentToCanvas()
    {
        bool any = false;
        float minX = 0, maxX = 0, minY = 0, maxY = 0;

        foreach (var node in allNodes)
        {
            if (!node.VisualInstance.gameObject.activeSelf) continue;
            Vector2 p = node.VisualInstance.anchoredPosition;

            if (!any) { minX = maxX = p.x; minY = maxY = p.y; any = true; }
            else
            {
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
            }
        }
        if (!any) return;

        RectTransform nodeRT = nodePrefab.GetComponent<RectTransform>();
        float nodeWidth = nodeRT.rect.width;
        float nodeHeight = nodeRT.rect.height;

        float boundsWidth = Mathf.Max(maxX - minX + nodeWidth, nodeWidth);
        float boundsHeight = Mathf.Max(maxY - minY + nodeHeight, nodeHeight);

        Rect canvasRect = canvasRoot.rect;
        float availW = canvasRect.width * fillPercent;
        float availH = canvasRect.height * fillPercent;

        float scale = Mathf.Min(availW / boundsWidth, availH / boundsHeight);
        scale = Mathf.Min(scale, maxScale);

        content.localScale = Vector3.one * scale;

        Vector2 center = new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);
        content.anchoredPosition = -center * scale;
    }
}
