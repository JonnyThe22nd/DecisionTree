using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class AnchorHandle : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    Button button;
    TreeMenuBuilder builder;
    WorldSpaceCanvasFollow follow;
    Transform rayOrigin;
    float distance;
    Vector3 grabOffset;

    void Awake()
    {
        button = GetComponent<Button>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button != null && !button.IsInteractable()) return;

        var canvasFollow = GetComponentInParent<WorldSpaceCanvasFollow>();
        if (canvasFollow == null || canvasFollow.target == null) return;

        var tracked = eventData as TrackedDeviceEventData;
        var provider = tracked != null ? tracked.interactor as IXRRayProvider : null;
        if (provider == null) return;

        rayOrigin = provider.GetOrCreateRayOrigin();
        Vector3 hit = eventData.pointerCurrentRaycast.worldPosition;
        distance = Vector3.Distance(rayOrigin.position, hit);
        grabOffset = canvasFollow.transform.position - hit;
        follow = canvasFollow;

        builder = GetComponentInParent<TreeMenuBuilder>();
        if (builder != null) builder.IsDragging = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Release();
    }

    void Update()
    {
        if (follow == null) return;

        if (rayOrigin == null || follow.target == null)
        {
            Release();
            return;
        }

        Vector3 canvasPosition = rayOrigin.position + rayOrigin.forward * distance + grabOffset;
        follow.offset = canvasPosition - follow.target.position;
    }

    void OnDisable()
    {
        Release();
    }

    void Release()
    {
        if (follow != null && builder != null)
        {
            builder.IsDragging = false;
            builder.SubmitTreeMove();
        }

        follow = null;
        rayOrigin = null;
    }
}
