using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkAvatar : NetworkBehaviour
{
    public Transform head;
    public Transform leftHand;
    public Transform rightHand;
    public TMPro.TextMeshPro nameLabel;
    public Vector3 labelOffset = new Vector3(0f, 0.3f, 0f);

    readonly NetworkVariable<FixedString64Bytes> displayName = new NetworkVariable<FixedString64Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    Transform rigHead;
    Transform rigLeft;
    Transform rigRight;

    public override void OnNetworkSpawn()
    {
        displayName.OnValueChanged += OnNameChanged;
        nameLabel.text = displayName.Value.ToString();

        if (!IsOwner) return;

        displayName.Value = UserIdentity.Name;

        foreach (var renderer in GetComponentsInChildren<Renderer>())
            renderer.enabled = false;
    }

    public override void OnNetworkDespawn()
    {
        displayName.OnValueChanged -= OnNameChanged;
    }

    void OnNameChanged(FixedString64Bytes previous, FixedString64Bytes current)
    {
        nameLabel.text = current.ToString();
    }

    void LateUpdate()
    {
        if (!IsSpawned) return;

        if (IsOwner) FollowRig();

        var camera = Camera.main;
        nameLabel.transform.position = head.position + labelOffset;
        if (camera != null && !IsOwner)
            nameLabel.transform.rotation = Quaternion.LookRotation(nameLabel.transform.position - camera.transform.position);
    }

    void FollowRig()
    {
        if (rigHead == null && Camera.main != null) rigHead = Camera.main.transform;
        if (rigLeft == null) rigLeft = FindRigPart("Left Controller");
        if (rigRight == null) rigRight = FindRigPart("Right Controller");

        Copy(rigHead, head);
        Copy(rigLeft, leftHand);
        Copy(rigRight, rightHand);
    }

    static Transform FindRigPart(string partName)
    {
        var found = GameObject.Find(partName);
        return found != null ? found.transform : null;
    }

    static void Copy(Transform source, Transform target)
    {
        if (source == null) return;
        target.SetPositionAndRotation(source.position, source.rotation);
    }
}
