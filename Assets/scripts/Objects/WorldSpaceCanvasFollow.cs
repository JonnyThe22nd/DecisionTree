using UnityEngine;

public class WorldSpaceCanvasFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 0.3f, 0);

    [Tooltip("Aus = Canvas bleibt fest an der Rotation, mit der sie erstellt wurde " +
             "(dreht sich NICHT zum Spieler). Nur aktivieren, wenn sie dir doch " +
             "immer zugewandt sein soll.")]
    public bool faceCamera = false;

    [Tooltip("Wenn aktiv, übernimmt die Canvas die Rotation des Zielobjekts " +
             "(dreht sich also mit, falls sich das Objekt dreht)")]
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
