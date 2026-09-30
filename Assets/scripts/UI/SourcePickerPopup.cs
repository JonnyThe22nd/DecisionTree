using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class SourcePickerPopup : MonoBehaviour
{
    [Tooltip("Content-Objekt der ScrollView (mit Vertical Layout Group), in das die Einträge kommen")]
    public RectTransform listContainer;
    [Tooltip("Button mit TextMeshProUGUI-Kind, eine Zeile pro Datei")]
    public Button listItemPrefab;
    [Tooltip("Optional -- wird angezeigt, wenn der Quellen-Ordner leer ist (nennt den Ordnerpfad)")]
    public TMPro.TextMeshProUGUI emptyHint;
    [Tooltip("Optional -- schließt das Popup ohne Auswahl")]
    public Button cancelButton;

    Action<string> onPicked;
    Action onClosed;

    public void Init(Action<string> onPickedCallback, Action onClosedCallback = null)
    {
        onPicked = onPickedCallback;
        onClosed = onClosedCallback;

        if (cancelButton != null)
            cancelButton.onClick.AddListener(() => Destroy(gameObject));

        var files = SourceLibrary.ListFiles();

        if (emptyHint != null)
        {
            emptyHint.gameObject.SetActive(files.Count == 0);
            emptyHint.text = "Keine Quellen gefunden. Text- oder Bilddateien hier ablegen:\n" + SourceLibrary.FolderPath;
        }

        foreach (var file in files)
        {
            Button item = Instantiate(listItemPrefab, listContainer);
            var label = item.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (label != null) label.text = Path.GetFileName(file);

            string captured = file;
            item.onClick.AddListener(() =>
            {
                onPicked?.Invoke(captured);
                Destroy(gameObject);
            });
        }
    }

    void OnDestroy()
    {
        onClosed?.Invoke();
    }
}
