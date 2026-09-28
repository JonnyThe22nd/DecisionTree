using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Eingabe-Popup zum Anlegen einer neuen Quelle: Titel (Kurzbezeichnung für die
/// Liste) + Content (der eigentliche Text/Link/Zitat). Wird als Kind der
/// TreeMenuBuilder.content-Canvas instanziert, genau wie NamingPopup.
/// </summary>
public class SourceEntryPopup : MonoBehaviour
{
    [Tooltip("Kurzer Titel/Name der Quelle")]
    public TMPro.TMP_InputField titleField;
    [Tooltip("Der eigentliche Inhalt: Link, Zitat, Notiz, Referenz ...")]
    public TMPro.TMP_InputField contentField;
    public Button confirmButton;
    [Tooltip("Optional -- schließt das Popup ohne Quelle zu erstellen")]
    public Button cancelButton;

    Action<string, string> onConfirm;
    Action onClosed;

    public void Init(Action<string, string> onConfirmCallback, Action onClosedCallback = null)
    {
        onConfirm = onConfirmCallback;
        onClosed = onClosedCallback;

        confirmButton.onClick.AddListener(HandleConfirm);
        if (cancelButton != null)
            cancelButton.onClick.AddListener(() => Destroy(gameObject));

        titleField.text = "";
        contentField.text = "";
        titleField.Select();
        titleField.ActivateInputField();
    }

    void HandleConfirm()
    {
        string title = titleField.text.Trim();
        string content = contentField.text.Trim();
        if (string.IsNullOrEmpty(title)) return;

        onConfirm?.Invoke(title, content);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        onClosed?.Invoke();
    }
}
