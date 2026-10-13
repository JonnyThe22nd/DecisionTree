using System;
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

    public RectTransform content;

    public RectTransform overlay;

    [Header("Prefabs")]
    public Button nodePrefab;
    public Image linePrefab;
    public Button plusButtonPrefab;
    public Button addSourceButtonPrefab;
    public Button modelButtonPrefab;
    public Button resetViewButtonPrefab;
    public NamingPopup namingPopupPrefab;
    public DecisionPopup decisionPopupPrefab;
    public FilePickerPopup sourcePickerPrefab;
    public FilePickerPopup modelPickerPrefab;
    public SourcesPanel sourcesPanelPrefab;

    [Header("Erstellung")]
    public GameObject createPromptButton;

    [Header("Anker")]
    public RectTransform anchorHandle;

    [Header("Fern-Ansicht")]
    public RectTransform pinView;
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
    [Range(0f, 1f)]
    public float blockedAlpha = 0.35f;

    static readonly Color attachedModelColor = new Color(0.55f, 0.8f, 1f, 1f);

    MenuNode root;
    readonly List<MenuNode> allNodes = new List<MenuNode>();
    int popupCount;
    SourcesPanel sourcesPanelInstance;
    CanvasGroup contentGroup;
    CanvasGroup anchorGroup;
    MenuNode shownModelNode;

    public event Action<PathModelEventArgs> PathModelChanged;

    public string TreeId { get; private set; }

    public bool IsDragging { get; set; }

    public TreeDetailLevel CurrentLevel { get; private set; } = TreeDetailLevel.Full;

    public bool HasTree => root != null;

    public bool IsCreatePromptVisible => createPromptButton != null && createPromptButton.activeSelf;

    public bool IsWindowOpen =>
        popupCount > 0 || (sourcesPanelInstance != null && sourcesPanelInstance.gameObject.activeSelf);

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

        if (contentGroup == null) contentGroup = GetOrAddGroup(content);
        if (anchorGroup == null && anchorHandle != null) anchorGroup = GetOrAddGroup(anchorHandle);
    }

    static CanvasGroup GetOrAddGroup(RectTransform target)
    {
        var group = target.GetComponent<CanvasGroup>();
        return group != null ? group : target.gameObject.AddComponent<CanvasGroup>();
    }

    void Update()
    {
        bool blocked = IsWindowOpen;
        float alpha = blocked ? blockedAlpha : 1f;

        contentGroup.interactable = !blocked;
        contentGroup.blocksRaycasts = !blocked;
        contentGroup.alpha = alpha;

        if (anchorHandle == null) return;

        SetActiveIfChanged(anchorHandle, CurrentLevel == TreeDetailLevel.Full && (HasTree || IsCreatePromptVisible));
        anchorGroup.interactable = !blocked;
        anchorGroup.blocksRaycasts = !blocked;
        anchorGroup.alpha = alpha;
    }

    public void SetCreatePromptVisible(bool visible)
    {
        if (createPromptButton != null && !HasTree && !IsWindowOpen)
            createPromptButton.SetActive(visible);
    }

    public void SetDetailLevel(TreeDetailLevel level, float farScale = 1f)
    {
        if (!HasTree) level = TreeDetailLevel.Full;
        CurrentLevel = level;

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

    T OpenPopup<T>(T prefab) where T : Component
    {
        popupCount++;
        return Instantiate(prefab, overlay);
    }

    void PopupClosed()
    {
        popupCount = Mathf.Max(0, popupCount - 1);
    }

    void EnsureUserName(Action next)
    {
        if (UserIdentity.HasName)
        {
            next();
            return;
        }

        var popup = OpenPopup(namingPopupPrefab);
        popup.Init(
            name =>
            {
                UserIdentity.Name = name;
                next();
            },
            "Dein Name...",
            PopupClosed
        );
    }

    public void PromptCreateRoot()
    {
        if (HasTree || namingPopupPrefab == null || IsWindowOpen) return;

        if (createPromptButton != null) createPromptButton.SetActive(false);

        EnsureUserName(() =>
        {
            var popup = OpenPopup(namingPopupPrefab);
            popup.Init(label => CreateRoot(label), "Name des Baums...", PopupClosed);
        });
    }

    public void SetTreeId(string id)
    {
        if (!string.IsNullOrEmpty(TreeId)) TreeRegistry.Unregister(TreeId, this);
        TreeId = id;
        TreeRegistry.Register(id, this);
    }

    void OnDestroy()
    {
        if (!string.IsNullOrEmpty(TreeId)) TreeRegistry.Unregister(TreeId, this);
    }

    static string NewId()
    {
        return Guid.NewGuid().ToString("N").Substring(0, 12);
    }

    static DateTime ToLocal(long ticks)
    {
        return new DateTime(ticks, DateTimeKind.Utc).ToLocalTime();
    }

    void Submit(TreeOperation op)
    {
        op.TreeId = TreeId;
        if (TreeSync.IsOnline) TreeSync.Instance.Submit(op);
        else ApplyOperation(op);
    }

    public MenuNode FindNode(string id)
    {
        return allNodes.Find(n => n.Id == id);
    }

    public void CreateRoot(string label)
    {
        if (createPromptButton != null) createPromptButton.SetActive(false);

        var follow = GetComponent<WorldSpaceCanvasFollow>();

        Submit(new TreeOperation
        {
            Type = TreeOperationType.CreateRoot,
            NodeId = NewId(),
            Name = label,
            Author = UserIdentity.Name,
            Ticks = DateTime.UtcNow.Ticks,
            Offset = follow != null ? follow.offset : Vector3.zero
        });
    }

    public void SubmitTreeMove()
    {
        var follow = GetComponent<WorldSpaceCanvasFollow>();
        if (follow == null) return;

        Submit(new TreeOperation { Type = TreeOperationType.MoveTree, Offset = follow.offset });
    }

    public void ApplyOperation(TreeOperation op)
    {
        switch (op.Type)
        {
            case TreeOperationType.CreateRoot: ApplyCreateRoot(op); break;
            case TreeOperationType.AddChild: ApplyAddChild(op); break;
            case TreeOperationType.AddSource: ApplyAddSource(op); break;
            case TreeOperationType.AttachModel: ApplyAttachModel(op); break;
            case TreeOperationType.MoveTree: ApplyMoveTree(op); break;
        }
    }

    void ApplyCreateRoot(TreeOperation op)
    {
        if (HasTree) return;

        if (createPromptButton != null) createPromptButton.SetActive(false);

        var follow = GetComponent<WorldSpaceCanvasFollow>();
        if (follow != null && op.Offset != Vector3.zero) follow.offset = op.Offset;

        Build(new MenuNode(op.Name)
        {
            Id = op.NodeId,
            Author = op.Author,
            CreatedAt = ToLocal(op.Ticks)
        });
    }

    void ApplyAddChild(TreeOperation op)
    {
        var parent = FindNode(op.ParentId);
        if (parent == null || FindNode(op.NodeId) != null) return;

        var child = parent.AddChild(op.Name);
        child.Id = op.NodeId;
        child.Kind = op.Kind;
        child.Belief = op.Belief;
        child.Reasoning = op.Reasoning;
        child.Author = op.Author;
        child.CreatedAt = ToLocal(op.Ticks);

        CollectAndInstantiate(child);
        Relayout();
    }

    void ApplyAddSource(TreeOperation op)
    {
        var node = FindNode(op.NodeId);
        if (node == null || node.Sources.Exists(s => s.Title == op.FileName)) return;

        node.Sources.Add(new SourceEntry
        {
            Title = op.FileName,
            FilePath = SourceLibrary.Resolve(op.FileName),
            Author = op.Author,
            AddedAt = ToLocal(op.Ticks)
        });

        if (sourcesPanelInstance != null && sourcesPanelInstance.CurrentNode == node)
            sourcesPanelInstance.RefreshList();
    }

    void ApplyAttachModel(TreeOperation op)
    {
        var node = FindNode(op.NodeId);
        if (node == null) return;

        node.ModelFile = ModelLibrary.Resolve(op.FileName);
        PaintModelButton(node);

        if (op.Author == UserIdentity.Name) ShowModel(node);
    }

    void ApplyMoveTree(TreeOperation op)
    {
        var follow = GetComponent<WorldSpaceCanvasFollow>();
        if (follow == null || IsDragging) return;

        follow.offset = op.Offset;
    }

    public void RequestAddChild(MenuNode parent)
    {
        if (namingPopupPrefab == null || decisionPopupPrefab == null || IsWindowOpen) return;

        EnsureUserName(() =>
        {
            var popup = OpenPopup(decisionPopupPrefab);
            popup.Init(data => AddChild(parent, data), PopupClosed);
        });
    }

    public void AddChild(MenuNode parent, DecisionData data)
    {
        Submit(new TreeOperation
        {
            Type = TreeOperationType.AddChild,
            NodeId = NewId(),
            ParentId = parent.Id,
            Name = data.Name,
            Kind = data.Kind,
            Belief = data.Belief,
            Reasoning = data.Reasoning,
            Author = UserIdentity.Name,
            Ticks = DateTime.UtcNow.Ticks
        });
    }

    public void RequestAddSource(MenuNode node)
    {
        if (sourcePickerPrefab == null || IsWindowOpen) return;

        var popup = OpenPopup(sourcePickerPrefab);
        popup.Init(
            SourceLibrary.ListFiles(),
            "Quelle auswählen",
            "Keine Quellen gefunden. Text- oder Bilddateien hier ablegen:\n" + SourceLibrary.FolderPath,
            path => AddSource(node, path),
            PopupClosed
        );
    }

    public void AddSource(MenuNode node, string filePath)
    {
        string fileName = Path.GetFileName(filePath);
        if (node.Sources.Exists(s => s.Title == fileName)) return;

        ShareFile(SharedFileKind.Source, fileName);
        Submit(new TreeOperation
        {
            Type = TreeOperationType.AddSource,
            NodeId = node.Id,
            FileName = fileName,
            Author = UserIdentity.Name,
            Ticks = DateTime.UtcNow.Ticks
        });
    }

    static void ShareFile(SharedFileKind kind, string fileName)
    {
        if (TreeSync.IsOnline && FileTransfer.Instance != null)
            FileTransfer.Instance.Share(kind, fileName);
    }

    public void RequestOpenSources(MenuNode node)
    {
        if (sourcesPanelInstance == null || IsWindowOpen) return;
        sourcesPanelInstance.Open(node);
    }

    public void RequestAttachModel(MenuNode node)
    {
        if (modelPickerPrefab == null || IsWindowOpen) return;

        var popup = OpenPopup(modelPickerPrefab);
        popup.Init(
            ModelLibrary.ListFiles(),
            "3D-Modell auswählen",
            "Keine Modelle gefunden. .glb-Dateien hier ablegen:\n" + ModelLibrary.FolderPath,
            path => AttachModel(node, path),
            PopupClosed
        );
    }

    public void AttachModel(MenuNode node, string filePath)
    {
        string fileName = Path.GetFileName(filePath);

        ShareFile(SharedFileKind.Model, fileName);
        Submit(new TreeOperation
        {
            Type = TreeOperationType.AttachModel,
            NodeId = node.Id,
            FileName = fileName,
            Author = UserIdentity.Name,
            Ticks = DateTime.UtcNow.Ticks
        });
    }

    void OnModelButton(MenuNode node)
    {
        if (string.IsNullOrEmpty(node.ModelFile))
        {
            RequestAttachModel(node);
            return;
        }

        if (shownModelNode == node) HideModel();
        else ShowModel(node);
    }

    void ShowModel(MenuNode node)
    {
        shownModelNode = node;
        RaisePathModel(node, true);
    }

    public void ResetView()
    {
        HideModel();
    }

    void HideModel()
    {
        var node = shownModelNode;
        shownModelNode = null;
        if (node != null) RaisePathModel(node, false);
    }

    void RaisePathModel(MenuNode node, bool visible)
    {
        PathModelChanged?.Invoke(new PathModelEventArgs
        {
            Leaf = node,
            Path = node.GetPath(),
            ModelFile = node.ModelFile,
            Visible = visible
        });
    }

    static void PaintModelButton(MenuNode node)
    {
        if (node.ModelButtonInstance == null) return;

        Color color = string.IsNullOrEmpty(node.ModelFile) ? Color.white : attachedModelColor;
        var hover = node.ModelButtonInstance.GetComponent<UIHoverEffect>();
        if (hover != null) hover.SetBaseColor(color);

        var image = node.ModelButtonInstance.GetComponent<Image>();
        if (image != null) image.color = color;
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
        HideModel();
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

        if (node.IsRoot && resetViewButtonPrefab != null)
        {
            Button reset = Instantiate(resetViewButtonPrefab, node.VisualInstance);
            var resetRT = reset.GetComponent<RectTransform>();
            resetRT.anchorMin = resetRT.anchorMax = new Vector2(0.5f, 0f);
            resetRT.pivot = new Vector2(0.5f, 1f);
            resetRT.anchoredPosition = new Vector2(0f, -10f);
            reset.onClick.AddListener(ResetView);
        }

        if (!node.IsRoot)
        {
            if (addSourceButtonPrefab != null)
            {
                Button addSource = Instantiate(addSourceButtonPrefab, node.VisualInstance);
                var addSourceRT = addSource.GetComponent<RectTransform>();
                addSourceRT.anchorMin = addSourceRT.anchorMax = new Vector2(1f, 0f);
                addSourceRT.pivot = new Vector2(0f, 0f);
                addSourceRT.anchoredPosition = new Vector2(10f, 0f);
                addSource.onClick.AddListener(() => RequestAddSource(node));
            }

            if (modelButtonPrefab != null)
            {
                Button model = Instantiate(modelButtonPrefab, node.VisualInstance);
                var modelRT = model.GetComponent<RectTransform>();
                modelRT.anchorMin = modelRT.anchorMax = new Vector2(0.5f, 0f);
                modelRT.pivot = new Vector2(0.5f, 1f);
                modelRT.anchoredPosition = new Vector2(0f, -10f);
                model.onClick.AddListener(() => OnModelButton(node));
                node.ModelButtonInstance = modelRT;
                PaintModelButton(node);
            }

            AddBadge(node.VisualInstance, "KindBadge", ArgumentationInfo.Letter(node.Kind), ArgumentationInfo.Color(node.Kind), new Vector2(0f, 1f));
            AddBadge(node.VisualInstance, "BeliefBadge", null, ArgumentationInfo.Color(node.Belief), new Vector2(0f, 0f));
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

    static void AddBadge(RectTransform parent, string badgeName, string letter, Color color, Vector2 anchor)
    {
        var go = new GameObject(badgeName, typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(34f, 34f);
        rt.anchoredPosition = Vector2.zero;

        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        if (letter == null) return;

        var textObject = new GameObject("Letter", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        var textRT = textObject.GetComponent<RectTransform>();
        textRT.SetParent(rt, false);
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = textRT.offsetMax = Vector2.zero;

        var text = textObject.GetComponent<TMPro.TextMeshProUGUI>();
        text.text = letter;
        text.fontSize = 22f;
        text.fontStyle = TMPro.FontStyles.Bold;
        text.alignment = TMPro.TextAlignmentOptions.Center;
        text.color = Color.black;
        text.raycastTarget = false;
    }

    void OnNodeSelected(MenuNode node)
    {
        node.OnSelect?.Invoke();
        RequestOpenSources(node);
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
            if (node.ModelButtonInstance != null)
                node.ModelButtonInstance.gameObject.SetActive(node.IsLeaf || !string.IsNullOrEmpty(node.ModelFile));
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
