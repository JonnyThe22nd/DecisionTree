using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class SessionPanel : MonoBehaviour
{
    public TMPro.TMP_InputField nameField;
    public TMPro.TMP_InputField codeField;
    public Button hostButton;
    public Button joinButton;
    public Button leaveButton;
    public Button muteButton;
    public TMPro.TextMeshProUGUI statusText;
    public TMPro.TextMeshProUGUI codeText;
    public TMPro.TextMeshProUGUI playersText;
    public TMPro.TextMeshProUGUI muteLabel;
    public GameObject lobbyGroup;
    public GameObject sessionGroup;

    float nextRefresh;
    string notice;
    float noticeUntil;
    FileTransfer subscribedTransfer;

    void Start()
    {
        nameField.text = UserIdentity.Name;

        hostButton.onClick.AddListener(OnHost);
        joinButton.onClick.AddListener(OnJoin);
        leaveButton.onClick.AddListener(() => _ = SessionManager.Instance.LeaveAsync());
        muteButton.onClick.AddListener(() =>
        {
            var voice = SessionManager.Instance.voice;
            if (voice != null) voice.SetMuted(!voice.IsMuted);
        });
    }

    void OnDestroy()
    {
        if (subscribedTransfer != null) subscribedTransfer.Notice -= OnNotice;
    }

    void OnHost()
    {
        if (!HasName()) return;
        _ = SessionManager.Instance.HostAsync(nameField.text.Trim());
    }

    void OnJoin()
    {
        if (!HasName()) return;

        string code = codeField.text.Trim().ToUpperInvariant();
        if (code.Length == 0)
        {
            ShowNotice("Bitte den Sitzungs-Code eingeben.");
            return;
        }

        _ = SessionManager.Instance.JoinAsync(code, nameField.text.Trim());
    }

    bool HasName()
    {
        if (nameField.text.Trim().Length > 0) return true;

        ShowNotice("Bitte zuerst einen Namen eingeben.");
        return false;
    }

    void OnNotice(string message)
    {
        ShowNotice(message);
    }

    void ShowNotice(string message)
    {
        notice = message;
        noticeUntil = Time.unscaledTime + 6f;
    }

    void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.25f;

        var manager = SessionManager.Instance;
        if (manager == null) return;

        if (subscribedTransfer == null && FileTransfer.Instance != null)
        {
            subscribedTransfer = FileTransfer.Instance;
            subscribedTransfer.Notice += OnNotice;
        }

        bool online = manager.Phase == ConnectionPhase.Online;
        lobbyGroup.SetActive(!online);
        sessionGroup.SetActive(online);

        var voice = manager.voice;
        string status = manager.Status;
        if (voice != null && !string.IsNullOrEmpty(voice.Status) && online) status += "\n" + voice.Status;
        if (Time.unscaledTime < noticeUntil) status += "\n" + notice;
        statusText.text = status;

        if (!online) return;

        codeText.text = string.IsNullOrEmpty(manager.Code) ? "" : "Code: " + manager.Code;
        muteLabel.text = voice != null && voice.IsMuted ? "Mikrofon: aus" : "Mikrofon: an";
        muteButton.interactable = voice != null && voice.IsActive;
        playersText.text = BuildPlayerList(manager, voice);
    }

    static string BuildPlayerList(SessionManager manager, VoiceChat voice)
    {
        var sb = new StringBuilder();
        sb.Append("Teilnehmer: ").AppendLine(manager.PlayerCount.ToString());

        if (voice == null) return sb.ToString();

        foreach (var participant in voice.GetParticipants())
        {
            string marker = participant.Speaking ? "<color=#7CFC7C>●</color> " : "<color=#888888>○</color> ";
            sb.Append(marker).Append(participant.Name);
            if (participant.IsSelf) sb.Append(" (du)");
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
