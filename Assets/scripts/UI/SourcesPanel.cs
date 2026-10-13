using UnityEngine;
using UnityEngine.UI;

public class SourcesPanel : MonoBehaviour
{
    public TMPro.TextMeshProUGUI titleText;
    public TMPro.TextMeshProUGUI infoText;

    [Header("Liste")]
    public GameObject listRoot;
    public RectTransform listContainer;
    public Button sourceListItemPrefab;
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

        if (titleText != null) titleText.text = (node.IsRoot ? "Baum: " : "Quellen: ") + node.Label;
        if (infoText != null) infoText.text = ArgumentationInfo.Describe(node);
        if (listRoot != null) listRoot.SetActive(!node.IsRoot);
        if (emptyHint != null) emptyHint.gameObject.SetActive(!node.IsRoot && node.Sources.Count == 0);

        foreach (var source in node.Sources)
        {
            Button item = Instantiate(sourceListItemPrefab, listContainer);
            var label = item.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (label != null)
            {
                string author = string.IsNullOrEmpty(source.Author) ? "unbekannt" : source.Author;
                label.text = source.Title + "\n<size=60%>von " + author + " · " + source.AddedAt.ToString("dd.MM.yyyy HH:mm") + "</size>";
            }

            var captured = source;
            item.onClick.AddListener(() =>
            {
                if (viewer != null) viewer.Show(captured);
            });
        }
    }
}
