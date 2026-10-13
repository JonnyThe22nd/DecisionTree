using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

public enum ConnectionPhase
{
    Offline,
    Connecting,
    Online
}

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }

    public int maxPlayers = 8;
    public VoiceChat voice;

    ISession session;
    bool busy;
    bool localTest;

    public event Action Changed;

    public string Status { get; private set; } = "Nicht verbunden";

    public ConnectionPhase Phase { get; private set; }

    public string Code => session != null ? session.Code : "";

    public int PlayerCount
    {
        get
        {
            if (session != null) return session.PlayerCount;
            var manager = NetworkManager.Singleton;
            return manager != null && manager.IsListening ? manager.ConnectedClientsIds.Count : 0;
        }
    }

    void Awake()
    {
        Instance = this;
        if (voice == null) voice = GetComponent<VoiceChat>();
        Application.runInBackground = true;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    static async Task EnsureServicesAsync()
    {
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
            await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public Task HostAsync(string playerName)
    {
        return EnterAsync(playerName, "Sitzung wird erstellt...", async () =>
            (ISession)await MultiplayerService.Instance.CreateSessionAsync(new SessionOptions { MaxPlayers = maxPlayers }.WithRelayNetwork()));
    }

    public Task JoinAsync(string code, string playerName)
    {
        return EnterAsync(playerName, "Trete der Sitzung bei...", () => MultiplayerService.Instance.JoinSessionByCodeAsync(code));
    }

    async Task EnterAsync(string playerName, string workingText, Func<Task<ISession>> action)
    {
        if (busy || session != null || localTest) return;

        busy = true;
        SetStatus(workingText, ConnectionPhase.Connecting);

        try
        {
            UserIdentity.Name = playerName;
            await EnsureServicesAsync();

            session = await action();
            session.Changed += OnSessionChanged;
            session.PlayerJoined += OnPlayerChanged;
            session.PlayerHasLeft += OnPlayerChanged;
            session.RemovedFromSession += OnSessionEnded;
            session.Deleted += OnSessionEnded;

            SetStatus("Verbunden", ConnectionPhase.Online);

            if (voice != null)
                _ = voice.JoinAsync("dt" + session.Id.Replace("-", ""), playerName);
        }
        catch (Exception e)
        {
            session = null;
            Debug.LogWarning("SessionManager: " + e);
            SetStatus(Describe(e), ConnectionPhase.Offline);
        }
        finally
        {
            busy = false;
        }
    }

    static string Describe(Exception e)
    {
        if (e is ServicesInitializationException || e.Message.Contains("project", StringComparison.OrdinalIgnoreCase))
            return "Unity Services nicht verfügbar. Projekt in den Project Settings unter Services verknüpfen. (" + e.Message + ")";

        return e.Message;
    }

    public async Task LeaveAsync()
    {
        if (busy) return;

        if (voice != null) await voice.LeaveAsync();

        if (session != null)
        {
            var leaving = session;
            session = null;
            try
            {
                await leaving.LeaveAsync();
            }
            catch (Exception e)
            {
                Debug.LogWarning("SessionManager: " + e.Message);
            }
        }
        else if (localTest && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        localTest = false;
        SetStatus("Nicht verbunden", ConnectionPhase.Offline);
    }

    public void StartLocalHost(string playerName)
    {
        UserIdentity.Name = playerName;
        localTest = NetworkManager.Singleton.StartHost();
        SetStatus(localTest ? "Lokaler Host (Test)" : "Lokaler Host konnte nicht starten", localTest ? ConnectionPhase.Online : ConnectionPhase.Offline);
    }

    public void StartLocalClient(string playerName)
    {
        UserIdentity.Name = playerName;
        localTest = NetworkManager.Singleton.StartClient();
        SetStatus(localTest ? "Lokaler Client (Test)" : "Lokaler Client konnte nicht starten", localTest ? ConnectionPhase.Online : ConnectionPhase.Offline);
    }

    void OnSessionChanged()
    {
        Changed?.Invoke();
    }

    void OnPlayerChanged(string playerId)
    {
        Changed?.Invoke();
    }

    async void OnSessionEnded()
    {
        if (session == null) return;

        session = null;
        if (voice != null) await voice.LeaveAsync();
        SetStatus("Sitzung beendet", ConnectionPhase.Offline);
    }

    void SetStatus(string status, ConnectionPhase phase)
    {
        Status = status;
        Phase = phase;
        Changed?.Invoke();
    }
}
