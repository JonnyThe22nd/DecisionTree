using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Baut und verwaltet einen Organigramm-Menübaum als World-Space-Canvas-UI,
/// inklusive Laufzeit-Erstellung: solange kein Baum existiert, kann über
/// 'createPromptButton' + Namens-Popup ein Root-Node erstellt werden. Danach
/// bekommt JEDER Node (unbegrenzte Tiefe) einen Plus-Button für weitere Kinder
/// sowie einen Quellen-Button, über den Quellen/Annotationen hinzugefügt und
/// eingesehen werden können.
/// </summary>
public class TreeMenuBuilder : MonoBehaviour
{
    [Header("Canvas")]
    public RectTransform canvasRoot;

    [Tooltip("Container, der den ganzen Baum hält und skaliert wird. Wird automatisch " +
             "als Kind von canvasRoot erzeugt, falls hier nichts zugewiesen ist.")]
    public RectTransform content;

    [Header("Prefabs")]
    [Tooltip("WICHTIG: anchorMin/anchorMax NICHT stretchen (z.B. (0.5,0.5))")]
    public Button nodePrefab;
    [Tooltip("anchorMin = anchorMax = (0.5, 0.5), Pivot = (0, 0.5)")]
    public Image linePrefab;
    [Tooltip("Kleiner '+'-Button, wird an JEDEN Node angehängt (keine Tiefenbegrenzung mehr)")]
    public Button plusButtonPrefab;
    [Tooltip("Kleiner 'Quellen'-Button, wird ebenfalls an JEDEN Node angehängt")]
    public Button sourcesButtonPrefab;
    [Tooltip("Panel mit TMP_InputField + Bestätigen-Button zum Benennen neuer Nodes")]
    public NamingPopup namingPopupPrefab;
    [Tooltip("Panel zum Anzeigen/Hinzufügen von Quellen an einem Node. Wird EINMALIG " +
             "instanziert (als Kind von content) und für jeden Node wiederverwendet.")]
    public SourcesPanel sourcesPanelPrefab;

    [Header("Erstellung")]
    [Tooltip("Button (z.B. '+ Baum erstellen'), der sichtbar wird, solange noch kein " +
             "Baum existiert. Wird von ObjectTreeAttachment ein-/ausgeblendet.")]
    public GameObject createPromptButton;

    [Header("Layout")]
    public HierarchicalTreeLayout layout = new HierarchicalTreeLayout();

    [Header("Auto-Fit")]
    [Range(0.1f, 1f)]
    public float fillPercent = 0.85f;
    public float maxScale = 1.5f;

    [Header("Verhalten")]
    public float lineThickness = 4f;

    MenuNode root;
    readonly List<MenuNode> allNodes = new List<MenuNode>();
    bool rootPopupOpen = false;
    SourcesPanel sourcesPanelInstance;

    public bool HasTree => root != null;

    void Awake()
    {
        EnsureContent();

        if (createPromptButton != null)
        {
            var btn = createPromptButton.GetComponentInChildren<Button>(true);
            if (btn != null) btn.onClick.AddListener(PromptCreateRoot);
            else Debug.LogWarning("TreeMenuBuilder: kein Button auf/unter createPromptButton gefunden.");
            createPromptButton.SetActive(false);
        }

        if (sourcesPanelPrefab != null)
        {
            sourcesPanelInstance = Instantiate(sourcesPanelPrefab, content);
            sourcesPanelInstance.gameObject.SetActive(false);
        }
    }

    void EnsureContent()
    {
        if (content != null) return;

        var go = new GameObject("TreeContent", typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(canvasRoot, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        content = rt;
    }

    // Wird von ObjectTreeAttachment aufgerufen, um den Create-Button je nach
    // Hover/Nähe ein- oder auszublenden. Tut nichts mehr, sobald ein Baum existiert.
    public void SetCreatePromptVisible(bool visible)
    {
        if (createPromptButton != null && !HasTree && !rootPopupOpen)
            createPromptButton.SetActive(visible);
    }

    public void PromptCreateRoot()
    {
        if (HasTree || namingPopupPrefab == null || rootPopupOpen) return;

        rootPopupOpen = true;
        if (createPromptButton != null) createPromptButton.SetActive(false);

        var popup = Instantiate(namingPopupPrefab, content);
        popup.Init(
            label => CreateRoot(label),
            "Name des Baums...",
            () => rootPopupOpen = false // läuft IMMER beim Schließen, egal ob bestätigt oder abgebrochen
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
        if (namingPopupPrefab == null) return;

        var popup = Instantiate(namingPopupPrefab, content);
        popup.Init(label =>
        {
            var child = parent.AddChild(label);
            CollectAndInstantiate(child);
            Relayout();
        }, "Name...");
    }

    public void RequestOpenSources(MenuNode node)
    {
        if (sourcesPanelInstance == null) return;
        sourcesPanelInstance.transform.SetAsLastSibling(); // immer ganz oben rendern
        sourcesPanelInstance.Open(node);
    }

    public void Build(MenuNode treeRoot)
    {
        EnsureContent();
        Clear();
        root = treeRoot;
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

        // Plus-Button an JEDEM Node -- keine Tiefenbegrenzung mehr, der Baum darf
        // beliebig tief werden. Als Kind des Node-Buttons, damit er automatisch
        // mitwandert/mitskaliert -- kein separates Layout nötig.
        if (plusButtonPrefab != null)
        {
            Button plus = Instantiate(plusButtonPrefab, node.VisualInstance);
            var plusRT = plus.GetComponent<RectTransform>();
            plusRT.anchorMin = plusRT.anchorMax = new Vector2(1f, 1f);
            plusRT.pivot = new Vector2(0f, 1f);
            plusRT.anchoredPosition = new Vector2(10f, 0f);
            plus.onClick.AddListener(() => RequestAddChild(node));
        }

        // Quellen-Button ebenfalls an JEDEM Node -- unabhängig vom Plus-Button,
        // unter diesem platziert, damit sie sich nicht überlappen.
        if (sourcesButtonPrefab != null)
        {
            Button sourcesBtn = Instantiate(sourcesButtonPrefab, node.VisualInstance);
            var sourcesRT = sourcesBtn.GetComponent<RectTransform>();
            sourcesRT.anchorMin = sourcesRT.anchorMax = new Vector2(1f, 0f);
            sourcesRT.pivot = new Vector2(0f, 0f);
            sourcesRT.anchoredPosition = new Vector2(10f, 0f);
            sourcesBtn.onClick.AddListener(() => RequestOpenSources(node));
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
