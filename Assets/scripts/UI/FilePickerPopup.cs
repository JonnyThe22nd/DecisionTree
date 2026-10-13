using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class FilePickerPopup : MonoBehaviour
{
    public TMPro.TextMeshProUGUI titleText;
    public RectTransform listContainer;
    public Button listItemPrefab;
    public TMPro.TextMeshProUGUI emptyHint;
    public Button cancelButton;

    Action<string> onPicked;
    Action onClosed;

    public void Init(IList<string> files, string title, string emptyMessage, Action<string> onPickedCallback, Action onClosedCallback = null)
    {
        onPicked = onPickedCallback;
        onClosed = onClosedCallback;

        if (titleText != null) titleText.text = title;

        if (cancelButton != null)
            cancelButton.onClick.AddListener(() => Destroy(gameObject));

        if (emptyHint != null)
        {
            emptyHint.gameObject.SetActive(files.Count == 0);
            emptyHint.text = emptyMessage;
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
