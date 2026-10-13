using System;
using UnityEngine;
using UnityEngine.UI;

public struct DecisionData
{
    public string Name;
    public ArgumentationKind Kind;
    public BeliefValue Belief;
    public string Reasoning;
}

public class DecisionPopup : MonoBehaviour
{
    public TMPro.TMP_InputField nameField;
    public TMPro.TMP_InputField reasoningField;
    public Button[] kindButtons;
    public Button[] beliefButtons;
    public Button confirmButton;
    public Button cancelButton;

    static readonly Color unselectedColor = Color.white;
    static readonly Color disabledColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    Action<DecisionData> onConfirm;
    Action onClosed;
    ArgumentationKind? kind;
    BeliefValue belief = BeliefValue.Unknown;

    public void Init(Action<DecisionData> onConfirmCallback, Action onClosedCallback = null)
    {
        onConfirm = onConfirmCallback;
        onClosed = onClosedCallback;

        for (int i = 0; i < kindButtons.Length; i++)
        {
            var value = ArgumentationInfo.Kinds[i];
            kindButtons[i].onClick.AddListener(() => SelectKind(value));
        }

        for (int i = 0; i < beliefButtons.Length; i++)
        {
            var value = ArgumentationInfo.Beliefs[i];
            beliefButtons[i].onClick.AddListener(() => SelectBelief(value));
        }

        confirmButton.onClick.AddListener(HandleConfirm);
        if (cancelButton != null)
            cancelButton.onClick.AddListener(() => Destroy(gameObject));

        nameField.onValueChanged.AddListener(_ => Refresh());

        nameField.text = "";
        reasoningField.text = "";
        nameField.Select();
        nameField.ActivateInputField();
        Refresh();
    }

    void SelectKind(ArgumentationKind value)
    {
        kind = value;
        Refresh();
    }

    void SelectBelief(BeliefValue value)
    {
        belief = value;
        Refresh();
    }

    bool IsValid => kind.HasValue && nameField.text.Trim().Length > 0;

    void Refresh()
    {
        for (int i = 0; i < kindButtons.Length; i++)
        {
            var value = ArgumentationInfo.Kinds[i];
            Paint(kindButtons[i], kind == value ? ArgumentationInfo.Color(value) : unselectedColor);
        }

        for (int i = 0; i < beliefButtons.Length; i++)
        {
            var value = ArgumentationInfo.Beliefs[i];
            Paint(beliefButtons[i], belief == value ? ArgumentationInfo.Color(value) : unselectedColor);
        }

        confirmButton.interactable = IsValid;
        Paint(confirmButton, IsValid ? unselectedColor : disabledColor);
    }

    static void Paint(Button button, Color color)
    {
        var hover = button.GetComponent<UIHoverEffect>();
        if (hover != null) hover.SetBaseColor(color);
        if (button.targetGraphic != null) button.targetGraphic.color = color;
    }

    void HandleConfirm()
    {
        if (!IsValid) return;

        onConfirm?.Invoke(new DecisionData
        {
            Name = nameField.text.Trim(),
            Kind = kind.Value,
            Belief = belief,
            Reasoning = reasoningField.text.Trim()
        });
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        onClosed?.Invoke();
    }
}
