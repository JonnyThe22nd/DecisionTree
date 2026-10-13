using System;
using System.Collections.Generic;
using GLTFast;
using UnityEngine;
using UnityEngine.Events;

public class PathModelPresenter : MonoBehaviour
{
    public TreeMenuBuilder builder;
    public float fallbackSize = 0.5f;
    public UnityEvent<GameObject> onModelSpawned;

    WorldSpaceCanvasFollow follow;
    Transform modelRoot;
    TMPro.TextMeshPro pathLabel;
    GameObject current;
    int requestId;
    readonly List<Renderer> hiddenRenderers = new List<Renderer>();
    Vector3 rootOffset;
    float originalSize;

    void Awake()
    {
        if (builder == null) builder = GetComponent<TreeMenuBuilder>();
        follow = GetComponent<WorldSpaceCanvasFollow>();

        modelRoot = new GameObject("PathModelRoot").transform;

        var labelObject = new GameObject("PathLabel");
        labelObject.transform.SetParent(modelRoot, false);
        pathLabel = labelObject.AddComponent<TMPro.TextMeshPro>();
        pathLabel.alignment = TMPro.TextAlignmentOptions.Top;
        pathLabel.fontSize = 0.7f;
        pathLabel.textWrappingMode = TMPro.TextWrappingModes.Normal;
        pathLabel.rectTransform.sizeDelta = new Vector2(1.5f, 0.3f);
        pathLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
        pathLabel.transform.localPosition = new Vector3(0f, -0.05f, 0f);
    }

    void OnEnable()
    {
        builder.PathModelChanged += OnPathModelChanged;
    }

    void OnDisable()
    {
        builder.PathModelChanged -= OnPathModelChanged;
    }

    void OnDestroy()
    {
        RestoreOriginal();
        if (modelRoot != null) Destroy(modelRoot.gameObject);
    }

    void LateUpdate()
    {
        if (follow != null && follow.target != null)
            modelRoot.position = follow.target.position + rootOffset;

        bool showLabel = current != null && builder.CurrentLevel == TreeDetailLevel.Full;
        if (pathLabel.gameObject.activeSelf != showLabel) pathLabel.gameObject.SetActive(showLabel);

        var camera = Camera.main;
        if (showLabel && camera != null)
            pathLabel.transform.rotation = Quaternion.LookRotation(pathLabel.transform.position - camera.transform.position);
    }

    async void OnPathModelChanged(PathModelEventArgs args)
    {
        int id = ++requestId;

        if (!args.Visible || string.IsNullOrEmpty(args.ModelFile))
        {
            ClearModel();
            RestoreOriginal();
            return;
        }

        try
        {
            var gltf = new GltfImport();
            bool loaded = await gltf.Load(new Uri(args.ModelFile).AbsoluteUri);
            if (id != requestId) return;
            if (!loaded)
            {
                Debug.LogWarning($"PathModelPresenter: '{args.ModelFile}' konnte nicht geladen werden.");
                return;
            }

            var instance = new GameObject("PathModel");
            instance.transform.SetParent(modelRoot, false);

            bool instantiated = await gltf.InstantiateMainSceneAsync(instance.transform);
            if (!instantiated || id != requestId)
            {
                Destroy(instance);
                return;
            }

            HideOriginal();
            modelRoot.position = follow != null && follow.target != null ? follow.target.position + rootOffset : modelRoot.position;
            Fit(instance.transform);

            ClearModel();
            current = instance;
            pathLabel.text = PathText(args.Path);
            onModelSpawned?.Invoke(instance);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"PathModelPresenter: Fehler beim Laden von '{args.ModelFile}': {e.Message}");
        }
    }

    void ClearModel()
    {
        if (current != null) Destroy(current);
        current = null;
        pathLabel.text = "";
    }

    void HideOriginal()
    {
        if (hiddenRenderers.Count > 0 || follow == null || follow.target == null) return;

        var renderers = follow.target.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            rootOffset = Vector3.zero;
            originalSize = fallbackSize;
            return;
        }

        Bounds bounds = Combined(renderers);
        rootOffset = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z) - follow.target.position;
        originalSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

        foreach (var renderer in renderers)
        {
            if (!renderer.enabled) continue;
            renderer.enabled = false;
            hiddenRenderers.Add(renderer);
        }
    }

    void RestoreOriginal()
    {
        foreach (var renderer in hiddenRenderers)
        {
            if (renderer != null) renderer.enabled = true;
        }
        hiddenRenderers.Clear();
        rootOffset = Vector3.zero;
    }

    void Fit(Transform instance)
    {
        var renderers = instance.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = Combined(renderers);
        float largest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (largest <= 0f) return;

        float target = originalSize > 0f ? originalSize : fallbackSize;
        instance.localScale = Vector3.one * (target / largest);

        bounds = Combined(renderers);
        instance.position += new Vector3(modelRoot.position.x - bounds.center.x, modelRoot.position.y - bounds.min.y, modelRoot.position.z - bounds.center.z);
    }

    static Bounds Combined(Renderer[] renderers)
    {
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    static string PathText(IReadOnlyList<MenuNode> path)
    {
        var labels = new List<string>();
        foreach (var node in path) labels.Add(node.Label);
        return string.Join(" › ", labels);
    }
}
