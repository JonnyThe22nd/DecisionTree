using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class ObjectTreeAttachment : MonoBehaviour
{
    [Header("Canvas")]
    public GameObject treeCanvasPrefab;
    public Vector3 canvasOffset = new Vector3(0, 0.3f, 0);
    public float frontDistance = 0.7f;
    public float frontHeight = 0.3f;

    [Header("Netzwerk")]
    public string treeId;

    [Header("Entfernung")]
    public Transform playerReference;
    public float fullTreeDistance = 3.5f;
    public float nameDistance = 8f;
    public float distanceHysteresis = 0.3f;

    [Header("Hover")]
    public Color hoverColor = new Color(1f, 0.8f, 0.25f, 1f);

    static readonly int baseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int colorId = Shader.PropertyToID("_Color");

    TreeMenuBuilder treeBuilder;
    XRBaseInteractable interactable;
    Renderer[] renderers;
    MaterialPropertyBlock hoverBlock;
    TreeDetailLevel detailLevel = TreeDetailLevel.Full;

    void Awake()
    {
        if (treeCanvasPrefab == null)
        {
            Debug.LogError($"ObjectTreeAttachment auf '{name}': treeCanvasPrefab fehlt.");
            return;
        }

        if (GetComponentInChildren<Collider>() == null)
            Debug.LogWarning($"ObjectTreeAttachment auf '{name}': kein Collider -- das Objekt kann nicht angeklickt werden.");

        renderers = GetComponentsInChildren<Renderer>();
        hoverBlock = new MaterialPropertyBlock();
        hoverBlock.SetColor(baseColorId, hoverColor);
        hoverBlock.SetColor(colorId, hoverColor);

        interactable = GetComponent<XRBaseInteractable>();
        if (interactable == null) interactable = gameObject.AddComponent<XRSimpleInteractable>();
        interactable.selectEntered.AddListener(OnObjectClicked);
        interactable.hoverEntered.AddListener(OnHoverChanged);
        interactable.hoverExited.AddListener(OnHoverChanged);

        GameObject canvasInstance = Instantiate(treeCanvasPrefab);
        treeBuilder = canvasInstance.GetComponentInChildren<TreeMenuBuilder>(true);
        treeBuilder.SetTreeId(string.IsNullOrEmpty(treeId) ? HierarchyPath() : treeId);

        var follow = canvasInstance.GetComponentInChildren<WorldSpaceCanvasFollow>(true);
        if (follow != null)
        {
            follow.target = transform;
            follow.offset = canvasOffset;
        }
        else
        {
            Debug.LogWarning($"ObjectTreeAttachment auf '{name}': kein WorldSpaceCanvasFollow " +
                "im treeCanvasPrefab gefunden -- die Canvas bleibt an der im Prefab hinterlegten Position.");
        }
    }

    string HierarchyPath()
    {
        string path = name;
        for (var t = transform.parent; t != null; t = t.parent)
            path = t.name + "/" + path;
        return path;
    }

    void Update()
    {
        if (treeBuilder == null) return;

        UpdateDetailLevel();
        CheckTriggerClick();
    }

    void UpdateDetailLevel()
    {
        if (playerReference == null && Camera.main != null)
            playerReference = Camera.main.transform;
        if (playerReference == null) return;

        float distance = Vector3.Distance(transform.position, playerReference.position);

        float fullLimit = fullTreeDistance + (detailLevel == TreeDetailLevel.Full ? distanceHysteresis : 0f);
        float nameLimit = nameDistance + (detailLevel != TreeDetailLevel.Pin ? distanceHysteresis : 0f);

        if (distance <= fullLimit) detailLevel = TreeDetailLevel.Full;
        else if (distance <= nameLimit) detailLevel = TreeDetailLevel.Name;
        else detailLevel = TreeDetailLevel.Pin;

        treeBuilder.SetDetailLevel(detailLevel, Mathf.Max(1f, distance / fullTreeDistance));
    }

    void CheckTriggerClick()
    {
        if (interactable == null || !interactable.isHovered || treeBuilder.HasTree) return;

        foreach (var hovering in interactable.interactorsHovering)
        {
            if (hovering is XRBaseInputInteractor input && input.activateInput.ReadWasPerformedThisFrame())
            {
                ToggleCreatePrompt();
                break;
            }
        }
    }

    void OnDestroy()
    {
        if (interactable == null) return;
        interactable.selectEntered.RemoveListener(OnObjectClicked);
        interactable.hoverEntered.RemoveListener(OnHoverChanged);
        interactable.hoverExited.RemoveListener(OnHoverChanged);
    }

    void OnObjectClicked(SelectEnterEventArgs args)
    {
        ToggleCreatePrompt();
    }

    void OnHoverChanged(BaseInteractionEventArgs args)
    {
        foreach (var r in renderers)
        {
            if (r != null) r.SetPropertyBlock(interactable.isHovered ? hoverBlock : null);
        }
    }

    public void ToggleCreatePrompt()
    {
        if (treeBuilder == null || treeBuilder.HasTree) return;

        bool show = !treeBuilder.IsCreatePromptVisible;
        if (show) PlaceInFront();
        treeBuilder.SetCreatePromptVisible(show);
    }

    void PlaceInFront()
    {
        var follow = treeBuilder.GetComponent<WorldSpaceCanvasFollow>();
        if (follow == null) return;

        var viewer = playerReference != null ? playerReference : (Camera.main != null ? Camera.main.transform : null);
        Vector3 toViewer = viewer != null ? viewer.position - transform.position : Vector3.back;
        toViewer.y = 0f;
        if (toViewer.sqrMagnitude < 0.0001f) toViewer = Vector3.back;

        follow.offset = toViewer.normalized * (HorizontalRadius() + frontDistance) + Vector3.up * frontHeight;
    }

    float HorizontalRadius()
    {
        var all = GetComponentsInChildren<Renderer>();
        if (all.Length == 0) return 0f;

        Bounds bounds = all[0].bounds;
        for (int i = 1; i < all.Length; i++) bounds.Encapsulate(all[i].bounds);
        return Mathf.Max(bounds.extents.x, bounds.extents.z);
    }
}
