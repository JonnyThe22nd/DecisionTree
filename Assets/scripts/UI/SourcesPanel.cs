using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Zeigt die Quellenliste eines Nodes an und erlaubt das Hinzufügen neuer Quellen.
/// Wird von TreeMenuBuilder EINMALIG instanziert und für jeden Node wiederverwendet
/// (Open(node) baut die Liste jedes Mal neu auf). Klick auf einen Listeneintrag
/// zeigt dessen vollen Inhalt im Detail-Textfeld an.
/// </summary>
public class SourcesPanel : MonoBehaviour
{
    [Header("Liste")]
    [Tooltip("Container mit z.B. einer Vertical Layout Group, in den die Einträge kommen")]
    public RectTransform listContainer;
    [Tooltip("Button mit TextMeshProUGUI-Kind, eine Zeile pro Quelle")]
    public Button sourceListItemPrefab;

    [Header("Detailansicht")]
    [Tooltip("Zeigt den vollen Content der angeklickten Quelle")]
    public TMPro.TextMeshProUGUI detailText;

    [Header("Hinzufügen")]
    public Button addSourceButton;
    public SourceEntryPopup sourceEntryPopupPrefab;
    [Tooltip("Wo das Eingabe-Popup instanziert wird, z.B. TreeMenuBuilder.content")]
    public RectTransform popupParent;

    [Header("Schließen")]
    public Button closeButton;

    MenuNode node;
    bool addPopupOpen;

    void Awake()
    {
        if (addSourceButton != null) addSourceButton.onClick.AddListener(OnAddSourceClicked);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    public void Open(MenuNode targetNode)
    {
        node = targetNode;
        gameObject.SetActive(true);
        RefreshList();
    }

    public void Close()
    {
        gameObject.SetActive(false);
        node = null;
    }

    void RefreshList()
    {
        foreach (Transform child in listContainer)
            Destroy(child.gameObject);

        if (detailText != null) detailText.text = "";

        if (node == null) return;

        foreach (var source in node.Sources)
        {
            Button item = Instantiate(sourceListItemPrefab, listContainer);
            var label = item.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (label != null) label.text = source.Title;

            var captured = source; // lokale Kopie für die Closure
            item.onClick.AddListener(() =>
            {
                if (detailText != null) detailText.text = captured.Content;
            });
        }
    }

    void OnAddSourceClicked()
    {
        if (addPopupOpen || sourceEntryPopupPrefab == null || node == null) return;

        addPopupOpen = true;
        var parent = popupParent != null ? popupParent : (RectTransform)transform;
        var popup = Instantiate(sourceEntryPopupPrefab, parent);
        popup.Init(
            (title, content) =>
            {
                node.Sources.Add(new SourceEntry { Title = title, Content = content });
                RefreshList();
            },
            () => addPopupOpen = false
        );
    }
}
