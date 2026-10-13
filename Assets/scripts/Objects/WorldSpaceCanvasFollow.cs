using UnityEngine;

public class WorldSpaceCanvasFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 0.3f, 0);

    public bool faceCamera = false;

    public bool matchTargetRotation = false;

    void LateUpdate()
    {
        if (target == null) return;

        transform.position = target.position + offset;

        if (faceCamera && Camera.main != null)
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
        else if (matchTargetRotation)
            transform.rotation = target.rotation;
    }
}
