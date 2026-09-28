using UnityEngine;

/// <summary>
/// Auf ein beliebiges Szenen-Objekt packen, das einen eigenen Menü-Baum bekommen
/// soll. Instanziert automatisch eine eigene Tree-Canvas (aus treeCanvasPrefab),
/// die dem Objekt folgt. Solange kein Baum existiert, wird der "+ Baum erstellen"
/// Button sichtbar, sobald der Spieler nah genug ist -- UNABHÄNGIG davon, ob das
/// Objekt selbst gerade mit dem Ray anvisiert wird. Würde die Sichtbarkeit an
/// den Hover des Objekts gekoppelt sein, könnte man den Button nie anklicken,
/// weil der Ray beim Zielen auf den Button zwangsläufig vom Objekt abrutscht
/// und der Hover-Zustand des Objekts damit verloren geht.
/// </summary>
public class ObjectTreeAttachment : MonoBehaviour
{
    [Header("Trigger-Bedingung")]
    [Tooltip("Maximale Distanz zum Spieler, ab der der Create-Button erscheinen darf")]
    public float proximityDistance = 2f;
    [Tooltip("Wird automatisch auf Camera.main gesetzt, falls leer")]
    public Transform playerReference;

    [Header("Canvas")]
    [Tooltip("Prefab mit Canvas + TreeMenuBuilder + WorldSpaceCanvasFollow + Create-Prompt-Button. Wird pro Objekt einmal instanziert.")]
    public GameObject treeCanvasPrefab;
    [Tooltip("Position der Canvas relativ zum Objekt")]
    public Vector3 canvasOffset = new Vector3(0, 0.3f, 0);

    TreeMenuBuilder treeBuilder;

    void Awake()
    {
        if (playerReference == null && Camera.main != null)
            playerReference = Camera.main.transform;

        if (treeCanvasPrefab == null)
        {
            Debug.LogError($"ObjectTreeAttachment auf '{name}': treeCanvasPrefab fehlt.");
            return;
        }

        GameObject canvasInstance = Instantiate(treeCanvasPrefab);
        treeBuilder = canvasInstance.GetComponentInChildren<TreeMenuBuilder>(true);

        var follow = canvasInstance.GetComponentInChildren<WorldSpaceCanvasFollow>(true);
        if (follow != null)
        {
            // Überschreibt IMMER jeden im Prefab manuell gesetzten Wert.
            follow.target = transform;
            follow.offset = canvasOffset;
        }
        else
        {
            Debug.LogWarning($"ObjectTreeAttachment auf '{name}': kein WorldSpaceCanvasFollow " +
                "im treeCanvasPrefab gefunden -- die Canvas bleibt an der im Prefab hinterlegten Position.");
        }
    }

    void Update()
    {
        if (treeBuilder == null || treeBuilder.HasTree) return;

        bool inRange = playerReference != null &&
            Vector3.Distance(transform.position, playerReference.position) <= proximityDistance;

        treeBuilder.SetCreatePromptVisible(inRange);
    }
}
