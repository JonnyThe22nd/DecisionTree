using UnityEngine;
using UnityEngine.UI;

public class SourcesPanel : MonoBehaviour
{
    [Tooltip("Optional -- zeigt den Namen der Entscheidung, deren Quellen gelistet sind")]
    public TMPro.TextMeshProUGUI titleText;

    [Header("Liste")]
    [Tooltip("Content-Objekt der ScrollView (mit Vertical Layout Group), in das die Einträge kommen")]
    public RectTransform listContainer;
    [Tooltip("Button mit TextMeshProUGUI-Kind, eine Zeile pro Quelle")]
    public Button sourceListItemPrefab;
    [Tooltip("Optional -- wird angezeigt, solange die Entscheidung keine Quellen hat")]
    public TMPro.TextMeshProUGUI emptyHint;

    [Header("Anzeige")]
    public SourceViewer viewer;

    [Header("Schließen")]
    public Button closeButton;

    MenuNode node;

    public MenuNode CurrentNode => node;

    void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    public void Open(MenuNode targetNode)
    {
        node = targetNode;
        gameObject.SetActive(true);
        if (viewer != null) viewer.Hide();
        RefreshList();
    }

    public void Close()
    {
        if (viewer != null) viewer.Hide();
        gameObject.SetActive(false);
        node = null;
    }

    public void RefreshList()
    {
        foreach (Transform child in listContainer)
            Destroy(child.gameObject);

        if (node == null) return;

        if (titleText != null) titleText.text = "Quellen: " + node.Label;
        if (emptyHint != null) emptyHint.gameObject.SetActive(node.Sources.Count == 0);

        foreach (var source in node.Sources)
        {
            Button item = Instantiate(sourceListItemPrefab, listContainer);
            var label = item.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (label != null) label.text = source.Title;

            var captured = source;
            item.onClick.AddListener(() =>
            {
                if (viewer != null) viewer.Show(captured);
            });
        }
    }
}
