using System;
using UnityEngine;
using UnityEngine.UI;

public class NamingPopup : MonoBehaviour
{
    [Tooltip("TMP_InputField, in das der Name eingegeben wird")]
    public TMPro.TMP_InputField inputField;
    public Button confirmButton;
    [Tooltip("Optional -- schließt das Popup ohne Node zu erstellen")]
    public Button cancelButton;
    [Tooltip("Optional -- zeigt einen Platzhalter-Hinweis wie 'Name eingeben...'")]
    public TMPro.TextMeshProUGUI placeholderText;

    Action<string> onConfirm;
    Action onClosed;

    public void Init(Action<string> onConfirmCallback, string placeholder = "Name eingeben...", Action onClosedCallback = null)
    {
        onConfirm = onConfirmCallback;
        onClosed = onClosedCallback;

        if (placeholderText != null) placeholderText.text = placeholder;

        confirmButton.onClick.AddListener(HandleConfirm);
        if (cancelButton != null)
            cancelButton.onClick.AddListener(() => Destroy(gameObject));

        inputField.text = "";
        inputField.Select();
        inputField.ActivateInputField();
    }

    void HandleConfirm()
    {
        string text = inputField.text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        onConfirm?.Invoke(text);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        onClosed?.Invoke();
    }
}
