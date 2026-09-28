using UnityEngine;

/// <summary>
/// Hält eine World-Space-Canvas an einem Zielobjekt (mit Offset) und dreht sie
/// so, dass sie immer zur Kamera zeigt. An das Canvas-Root-Objekt hängen
/// (also an dasselbe Objekt wie TreeMenuBuilder.canvasRoot).
/// </summary>
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

        // Position folgt dem Objekt (wichtig falls es sich bewegt), Rotation
        // bleibt standardmäßig unangetastet -- die Canvas "klebt" am Objekt,
        // statt sich nach dem Spieler auszurichten.
        transform.position = target.position + offset;

        if (faceCamera && Camera.main != null)
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
        else if (matchTargetRotation)
            transform.rotation = target.rotation;
    }
}
